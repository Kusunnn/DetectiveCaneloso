extends SceneTree
## Capturas del menú de inicio con Caneloso: iluminado, a contraluz, cuadros del ciclo de reposo y rendimiento.
## Uso (con ventana): --path game --script res://Herramientas/capturas_menu.gd -- <carpeta> [ancho alto] [antes.png]
## Con antes.png, Caneloso es esa imagen fija (para la captura del ANTES) y no se graba el ciclo.
var carpeta := ""
var textura_antes: Texture2D

func _initialize() -> void:
	call_deferred("capturar")

func guardar(nombre: String) -> void:
	await process_frame
	await process_frame
	root.get_texture().get_image().save_png(carpeta.path_join(nombre + ".png"))
	print("captura ", nombre)

func capturar() -> void:
	var args := OS.get_cmdline_user_args()
	carpeta = args[0]
	DirAccess.make_dir_recursive_absolute(carpeta)
	var tam := Vector2i(1920, 1080)
	if args.size() >= 3:
		tam = Vector2i(int(args[1]), int(args[2]))
	DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
	DisplayServer.window_set_size(tam)
	DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_DISABLED)
	await process_frame
	var menu: Node = load("res://Escenas/Menus/menu_inicio.tscn").instantiate()
	var fondo: Node = menu.get_node("Fondo")
	var antes := args.size() >= 4
	if antes:
		textura_antes = ImageTexture.create_from_image(Image.load_from_file(args[3]))
		fondo.set("TexturaSilueta", textura_antes)
	fondo.set("IntensidadParpadeo", "alta")
	root.add_child(menu)
	current_scene = menu
	await create_timer(1.5).timeout
	var prefijo := "antes" if antes else "despues"

	# Espera un momento iluminado y un apagón (la farola es aleatoria)
	var iluminado := false
	var contraluz := false
	var limite := Time.get_ticks_msec() + 60000
	while not (iluminado and contraluz) and Time.get_ticks_msec() < limite:
		await process_frame
		var luz := _luz_figura(fondo)
		if not iluminado and luz > 0.97:
			await guardar(prefijo + "_iluminado")
			iluminado = true
		elif not contraluz and luz < 0.05:
			await create_timer(0.25).timeout
			if _luz_figura(fondo) < 0.05:
				await guardar(prefijo + "_contraluz")
				contraluz = true
	if not antes:
		# Ciclo de reposo: 3 s a 15 cuadros por segundo, con la farola tranquila
		fondo.set("IntensidadParpadeo", "baja")
		await create_timer(3.0).timeout
		while _luz_figura(fondo) < 0.85:
			await process_frame
		for i in 45:
			var t0 := Time.get_ticks_msec()
			await guardar("ciclo_%02d" % i)
			while Time.get_ticks_msec() - t0 < 66:
				await process_frame
		# Rendimiento
		var rid := root.get_viewport_rid()
		RenderingServer.viewport_set_measure_render_time(rid, true)
		await create_timer(2.0).timeout
		var cpu := 0.0
		var gpu := 0.0
		for i in 120:
			await process_frame
			cpu += RenderingServer.viewport_get_measured_render_time_cpu(rid)
			gpu += RenderingServer.viewport_get_measured_render_time_gpu(rid)
		print("RENDIMIENTO %dx%d FPS=%d cpu=%.2f ms gpu=%.2f ms memoria_video=%.1f MB memoria_texturas=%.1f MB memoria_estatica=%.1f MB" % [
			tam.x, tam.y, Engine.get_frames_per_second(), cpu / 120.0, gpu / 120.0,
			Performance.get_monitor(Performance.RENDER_VIDEO_MEM_USED) / 1048576.0,
			Performance.get_monitor(Performance.RENDER_TEXTURE_MEM_USED) / 1048576.0,
			Performance.get_monitor(Performance.MEMORY_STATIC) / 1048576.0])
	quit()

# Brillo con que se dibuja a Caneloso (1 = iluminado, ~0 = silueta)
func _luz_figura(fondo: Node) -> float:
	var g: CanvasItem = fondo.find_child("Figura", true, false)
	if g != null:
		return g.modulate.r
	for n in fondo.find_children("*", "TextureRect", true, false):
		if (n as TextureRect).texture == textura_antes:
			return (n as TextureRect).modulate.r
	return 1.0
