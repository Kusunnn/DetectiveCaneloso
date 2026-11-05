extends SceneTree
## HU-9.5 CA-01: captura cuadros de una cinemática del Caso 1, con cuatro por plano. Necesita ventana.
## Uso: --path game --resolution 1920x1080 --script res://Herramientas/Caso1/capturas_cinematicas.gd -- <carpeta> <cual>
## cual: libro, foto, llegada, silbato, reconstruccion, final_a, final_b_porche o final_b_lago

const COMENTARIO := "«Guardia nocturna, 14 de agosto... monitor auxiliar... olor a alcohol...». No quemaron una hoja: quemaron el libro completo."
const PERIODICO := "Sobre el agua flota un periódico: «¿Atrapan por fin asesino de hace 10 años? Campamento cierra sus puertas para ser vendido después de una racha de muertes». La hoja se empapa y se hunde."

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	var carpeta: String = args[0]
	var cual: String = args[1]
	DirAccess.make_dir_recursive_absolute(carpeta)
	DisplayServer.window_set_size(Vector2i(1920, 1080))
	var prueba = load("res://tests/CinematicasCaso1Prueba.cs").new()
	root.add_child(prueba)
	var ruta: String = prueba.Escena(cual)
	if ruta != "":
		var escena: Node = load(ruta).instantiate()
		for n in ["Caso1", "Caso1Lago"]:
			if escena.has_node(n): escena.get_node(n).SoloVer = true
		root.add_child(escena)
		current_scene = escena
		await create_timer(2.5).timeout
		var dialogo = escena.get_node_or_null("VentanaDialogo")
		if dialogo: dialogo.visible = false
		for n in root.find_children("*", "CanvasLayer", false, false):
			n.visible = false
	await process_frame
	prueba.Empezar(cual, COMENTARIO, PERIODICO)
	await create_timer(2.5).timeout
	var inicios: Array = Array(prueba.Inicios)
	for i in inicios.size() - 1:
		var desde: float = inicios[i]
		var dur: float = inicios[i + 1] - desde
		for k in 4:
			var t: float = desde + dur * (0.08 + 0.28 * k)
			prueba.IrA(t)
			await create_timer(0.5).timeout
			prueba.IrA(t)
			await RenderingServer.frame_post_draw
			var imagen := root.get_texture().get_image()
			imagen.resize(960, 540)
			imagen.save_jpg(carpeta.path_join("%s_p%d_%d.jpg" % [cual, i, k]), 0.85)
	print("listo ", cual)
	quit()
