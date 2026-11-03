extends SceneTree
## HU-9.5 CA-01: graba seguidas las cinemáticas del Caso 1, con los textos del guion, para revisarlas sin jugar. Necesita ventana.
## Uso: --path game --write-movie <salida.avi> --fixed-fps 24 --resolution 1920x1080 --script res://Herramientas/Caso1/video_cinematicas.gd
## Después de «--», los nombres de las que se quieren grabar; sin nombres, todas.
## No toca las partidas: no pasa por GestorPartida.

const CUALES := ["libro", "foto", "llegada", "silbato", "reconstruccion", "final_a", "final_b_porche", "final_b_lago"]

func _initialize() -> void:
	var pistas: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://Datos/Dialogos/caso1_pistas.json"))["pistas"]
	var acusacion: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://Datos/Dialogos/caso1_acusacion.json"))["nodos"]
	var final_a: Array = acusacion["final_a"]["lineas"]
	var final_b: Array = acusacion["final_b"]["lineas"]
	var textos := {
		"libro": [pistas["libro_guardia_quemado"]["comentario"], ""],
		"foto": [pistas["foto_grupo"]["comentario"], ""],
		"llegada": ["El sendero del Lago Espejo.", "Aquí abajo vieron al «ciervo de luz». Y aquí encontraron a Porky."],
		"silbato": [pistas["silbato_gs"]["comentario"], ""],
		"reconstruccion": [pistas["cuaderno_eriz"]["comentario"], ""],
		"final_a": [final_a[final_a.size() - 1]["texto"], ""],
		"final_b_porche": [final_b[0]["texto"], ""],
		"final_b_lago": [final_b[1]["texto"], final_b[2]["texto"]],
	}
	await process_frame
	DisplayServer.window_set_size(Vector2i(1920, 1080))
	var args := OS.get_cmdline_user_args()
	for cual in (args if args.size() > 0 else CUALES):
		print(cual)
		var prueba = load("res://tests/CinematicasCaso1Prueba.cs").new()
		root.add_child(prueba)
		await process_frame
		var escena: Node = null
		var ruta: String = prueba.Escena(cual)
		if ruta != "":
			escena = load(ruta).instantiate()
			# Solo el decorado: que el caso no dispare nada por su cuenta
			for n in ["Caso1", "Caso1Lago"]:
				if escena.has_node(n): escena.get_node(n).SoloVer = true
			root.add_child(escena)
			current_scene = escena
			await create_timer(1.5).timeout
			for n in root.find_children("*", "CanvasLayer", false, false):
				n.visible = false
			var dialogo = escena.get_node_or_null("VentanaDialogo")
			if dialogo: dialogo.visible = false
		prueba.Empezar(cual, textos[cual][0], textos[cual][1])
		prueba.Cinematica.Manual = false
		while root.get_node_or_null("CinematicaCaso1") != null:
			await process_frame
		if escena != null:
			current_scene = null
			escena.queue_free()
		prueba.queue_free()
		await create_timer(0.5).timeout
	quit()
