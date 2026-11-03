extends SceneTree
## Captura cuadros de la cinemática de la cinta (Caso 0) con Manual + IrA en los segundos indicados.
## Necesita ventana. Uso: --path game --resolution 1920x1080 --script res://Herramientas/capturas_cinta.gd -- <carpeta> <prefijo> <segundos...>

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	var carpeta: String = args[0]
	var prefijo: String = args[1]
	var tiempos: Array = Array(args.slice(2)).map(func(s): return float(s))
	DirAccess.make_dir_recursive_absolute(carpeta)
	DisplayServer.window_set_size(Vector2i(1920, 1080))
	var sala: Node3D = load("res://Escenas/Interrogatorio/sala_interrogatorio.tscn").instantiate()
	root.add_child(sala)
	await create_timer(1.5).timeout
	# Fuera la tarjeta de título y los avisos del inicio del caso
	for n in root.find_children("*", "CanvasLayer", true, false):
		if n.layer >= 100: n.visible = false
	var prueba = load("res://tests/CinematicaCintaPrueba.cs").new()
	root.add_child(prueba)
	prueba.Empezar(sala, true)
	await create_timer(1.0).timeout
	for t in tiempos:
		prueba.Cinematica.IrA(t)
		# Las mezclas de animación avanzan con el tiempo real: se deja que terminen
		await create_timer(0.6).timeout
		await RenderingServer.frame_post_draw
		var ruta := carpeta.path_join("%s_%05.2f.png" % [prefijo, t])
		root.get_texture().get_image().save_png(ruta)
		print("cuadro: ", ruta)
	quit()
