extends SceneTree
## HU-20.6: capturas y FPS de un escenario desde varios puntos de vista, con la escena completa (luces, niebla,
## partículas) y sin la interfaz. Necesita ventana.
## Uso: --path game --script res://Herramientas/Caso1/capturas_escenario.gd -- <escena.tscn> <carpeta> nombre=px,py,pz,tx,ty,tz [más vistas...] [--sin-linterna] [--visible=nodo]
## Escribe <carpeta>/<nombre>.png y <carpeta>/rendimiento.txt (FPS y ms de CPU/GPU por vista, sin vsync).

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	var carpeta: String = args[1]
	DirAccess.make_dir_recursive_absolute(carpeta)
	DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
	DisplayServer.window_set_size(Vector2i(1600, 900))
	DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_DISABLED)
	var escena: Node = load(args[0]).instantiate()
	root.add_child(escena)
	current_scene = escena
	var cam := Camera3D.new()
	cam.fov = 70
	cam.near = 0.05
	escena.add_child(cam)
	# La linterna del detective (en el juego se prende con [F]); «--sin-linterna» la deja apagada
	if not args.has("--sin-linterna"):
		var luz := SpotLight3D.new()
		luz.spot_range = 18.0
		luz.spot_angle = 26.0
		luz.light_energy = 5.0
		luz.light_color = Color(1, 0.93, 0.8)
		luz.shadow_enabled = true
		cam.add_child(luz)
		luz.position = Vector3(0.2, -0.15, 0)
	await create_timer(1.5).timeout
	_ocultar_interfaz(escena)
	cam.current = true
	var jugador := escena.get_node_or_null("Jugador")
	# «--visible=Ruta/Del/Nodo» muestra algo que empieza oculto (por ejemplo, un implicado de la sala)
	for a in args:
		if a.begins_with("--visible="):
			var n := escena.get_node_or_null(a.get_slice("=", 1))
			if n: n.visible = true
	var rid := root.get_viewport_rid()
	RenderingServer.viewport_set_measure_render_time(rid, true)
	var reporte := PackedStringArray()
	for vista in args.slice(2):
		if vista.begins_with("--"): continue
		var nombre: String = vista.get_slice("=", 0)
		var v := vista.get_slice("=", 1).split_floats(",")
		var pos := Vector3(v[0], v[1], v[2])
		cam.look_at_from_position(pos, Vector3(v[3], v[4], v[5]), Vector3.UP)
		# El jugador va con la cámara para que se active lo que depende de él (áreas, animaciones que lo miran)
		if jugador: jugador.global_position = pos - Vector3(0, 1.6, 0)
		await create_timer(1.2).timeout
		_ocultar_interfaz(escena)
		var cpu := 0.0
		var gpu := 0.0
		var t0 := Time.get_ticks_usec()
		for i in 60:
			await process_frame
			cpu += RenderingServer.viewport_get_measured_render_time_cpu(rid)
			gpu += RenderingServer.viewport_get_measured_render_time_gpu(rid)
		var fps := 60.0 / ((Time.get_ticks_usec() - t0) / 1e6)
		var img := root.get_viewport().get_texture().get_image()
		img.save_png(carpeta.path_join(nombre + ".png"))
		var linea := "%s  FPS≈%d  cpu=%.2f ms  gpu=%.2f ms" % [nombre, roundi(fps), cpu / 60.0, gpu / 60.0]
		print(linea)
		reporte.append(linea)
	var f := FileAccess.open(carpeta.path_join("rendimiento.txt"), FileAccess.WRITE)
	f.store_string("\n".join(reporte) + "\n")
	f.close()
	quit()

func _ocultar_interfaz(nodo: Node) -> void:
	for hijo in nodo.get_children():
		if hijo is CanvasLayer and hijo.name != "Vineta": hijo.visible = false
		_ocultar_interfaz(hijo)
