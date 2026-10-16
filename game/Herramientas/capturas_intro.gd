extends SceneTree
## Captura cuadros de la cinemática de inicio (IntroCaso) en los segundos indicados.
## Necesita ventana. Uso: --path game --script res://Herramientas/capturas_intro.gd -- <carpeta> [caso] [segundos...]

const TIEMPOS := [3.5, 7.2, 9.75, 12.6, 15.5, 19.5, 23.5, 28.0, 32.0, 37.0, 41.5, 46.5, 52.5]

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	var carpeta: String = args[0]
	var caso: String = args[1] if args.size() > 1 else "01"
	var tiempos: Array = TIEMPOS if args.size() <= 2 else Array(args.slice(2)).map(func(s): return float(s))
	DirAccess.make_dir_recursive_absolute(carpeta)
	await process_frame
	var intro = load("res://Escenas/Cinematicas/IntroCaso.tscn").instantiate()
	intro.Caso = caso
	intro.Manual = true
	root.add_child(intro)
	await create_timer(1.0).timeout
	for t in tiempos:
		intro.IrA(t)
		await create_timer(0.7).timeout
		await RenderingServer.frame_post_draw
		var ruta := carpeta.path_join("intro_%s_%05.1f.png" % [caso, t])
		root.get_texture().get_image().save_png(ruta)
		print("cuadro: ", ruta)
	paused = false
	quit()
