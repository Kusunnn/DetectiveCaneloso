extends SceneTree
# Ejecutar con Godot .NET --headless --path game --script res://tests/cinematica_cinta_smoke.gd.
# HU-9.5: los personajes del Caso 0 tienen animaciones y la cinemática final de la cinta
# corre completa, se puede saltar y deja la sala como estaba.
var sala: Node3D
var fallos := 0

func _initialize() -> void:
	call_deferred("probar")

func revisar(condicion: bool, mensaje: String) -> void:
	if condicion:
		print("OK   ", mensaje)
	else:
		fallos += 1
		printerr("FALLA ", mensaje)

func cuadros(n: int) -> void:
	for i in n:
		await process_frame

func probar() -> void:
	sala = load("res://Escenas/Interrogatorio/sala_interrogatorio.tscn").instantiate()
	root.add_child(sala)
	await cuadros(10)
	var caso = sala.get_node("Caso0")

	# Animaciones: cada personaje tiene su animador listo y su modelo se mueve
	for ruta in ["ZonaRecepcion/Porky", "ZonaPasillo/Eriz", "ZonaRecepcion/Comisario"]:
		var npc = sala.get_node(ruta)
		var animador = npc.Animador
		revisar(animador != null, ruta + " tiene AnimadorPersonaje")
		if animador == null: continue
		revisar(animador.Tiene("Idle") and animador.Tiene("Idle_No") and animador.Tiene("Yes"), ruta + " encuentra las animaciones de las dos librerías")
		var esqueleto: Skeleton3D = npc.find_children("*", "Skeleton3D", true, false)[0]
		var antes := esqueleto.get_bone_pose_rotation(esqueleto.find_bone("Head") if esqueleto.find_bone("Head") >= 0 else 5)
		npc.Gesto("Idle_No")
		await cuadros(20)
		var despues := esqueleto.get_bone_pose_rotation(esqueleto.find_bone("Head") if esqueleto.find_bone("Head") >= 0 else 5)
		revisar(not antes.is_equal_approx(despues), ruta + " se mueve al negar con la cabeza")

	# Cinemática completa en tiempo real (acelerado)
	var prueba = load("res://tests/CinematicaCintaPrueba.cs").new()
	root.add_child(prueba)
	var camara_antes := root.get_viewport().get_camera_3d()
	var reloj_antes: Transform3D = caso.RelojSala.global_transform
	var hud_antes: bool = sala.get_node("HUD").visible
	Engine.time_scale = 8.0
	prueba.Empezar(sala, false)
	await cuadros(2)
	revisar(prueba.Cinematica != null, "la cinemática empieza")
	revisar(root.get_viewport().get_camera_3d() != camara_antes, "usa su propia cámara")
	revisar(not caso.Jugador.visible, "el detective no sale en el recuerdo")
	# Nada de pensamientos ni avisos encima de la cinemática
	caso.Jugador.MostrarPensamiento("Prueba de pensamiento", 3.0)
	var texto_tutorial: Label = sala.get_node("CanvasLayer/TextoTutorial")
	revisar(texto_tutorial.text != "“Prueba de pensamiento”", "no aparece ningún pensamiento encima")
	revisar(AudioServer.get_bus_index("Cinta") >= 0, "el audio pasa por el bus de la cinta")
	var limite := 0
	while not prueba.Terminada and limite < 2000:
		await process_frame
		limite += 1
	Engine.time_scale = 1.0
	revisar(prueba.Terminada, "la cinemática termina sola")
	await cuadros(3)
	revisar(root.get_viewport().get_camera_3d() == camara_antes, "devuelve la cámara del jugador")
	revisar(caso.Jugador.visible, "el detective vuelve a verse")
	revisar(caso.RelojSala.global_transform.is_equal_approx(reloj_antes), "el reloj vuelve a la pared")
	revisar(caso.Porky.global_transform.origin.is_equal_approx(caso.PuntoPorkyFinal.global_transform.origin), "Porky vuelve a su lugar")
	revisar(not caso.Porky.Animador.Controlado, "Porky vuelve a su reposo")
	revisar(sala.get_node("HUD").visible == hud_antes, "el HUD vuelve como estaba")
	revisar(sala.get_node_or_null("CinematicaCintaBlender") == null, "la animación de Blender se borra")
	var grabadora = caso.GrabadoraSala
	revisar(grabadora.find_child("Contador", true, false).text == "03:12", "el contador queda en 03:12")
	revisar(not grabadora.find_child("cassette_player_tape", true, false).visible, "la grabadora queda sin cinta")
	revisar(caso.RelojSala.get_node("Modelo/Grietas").visible, "el reloj queda con el cristal rajado")
	revisar(sala.get_node("FocoReloj").visible, "vuelve el foco del reloj")
	revisar(not sala.get_node("PlumaSecreta/Marcas").visible, "la pluma vuelve a verse solo con linterna")
	revisar(AudioServer.get_bus_index("Cinta") < 0, "se quita el bus de la cinta")
	await create_timer(3.0 / Engine.time_scale).timeout
	revisar(AudioServer.get_bus_volume_db(AudioServer.get_bus_index("Musica")) > -20.0, "la música regresa con un fundido")

	# Adelantar con clic de toma en toma: el tic-tac se corta aunque se salte el golpe del reloj
	var prueba3 = load("res://tests/CinematicaCintaPrueba.cs").new()
	root.add_child(prueba3)
	prueba3.Empezar(sala, false)
	await cuadros(2)
	var cine = prueba3.Cinematica
	cine.IrA(22.0)
	await cuadros(2)
	var tictac_antes := false
	for n in cine.get_children():
		if n is AudioStreamPlayer and n.stream != null and n.stream.resource_path.ends_with("reloj_pared.wav"): tictac_antes = n.playing
	var clic := InputEventMouseButton.new()
	clic.button_index = MOUSE_BUTTON_LEFT
	clic.pressed = true
	for i in 3:
		Input.parse_input_event(clic)
		await cuadros(3)
	var tictac_despues := true
	for n in cine.get_children():
		if n is AudioStreamPlayer and n.stream != null and n.stream.resource_path.ends_with("reloj_pared.wav"): tictac_despues = n.playing
	revisar(tictac_antes and not tictac_despues, "al adelantar más allá del golpe se corta el tic-tac")
	Engine.time_scale = 8.0
	limite = 0
	while not prueba3.Terminada and limite < 2000:
		await process_frame
		limite += 1
	Engine.time_scale = 1.0
	revisar(prueba3.Terminada, "termina después de adelantar")

	# Saltar: mantener Espacio
	var prueba2 = load("res://tests/CinematicaCintaPrueba.cs").new()
	root.add_child(prueba2)
	prueba2.Empezar(sala, false)
	await cuadros(2)
	var tecla := InputEventKey.new()
	tecla.keycode = KEY_SPACE
	tecla.physical_keycode = KEY_SPACE
	tecla.pressed = true
	Input.parse_input_event(tecla)
	var inicio := Time.get_ticks_msec()
	limite = 0
	while not prueba2.Terminada and limite < 3000:
		await process_frame
		limite += 1
	tecla.pressed = false
	Input.parse_input_event(tecla)
	var segundos := (Time.get_ticks_msec() - inicio) / 1000.0
	# Dura 60 s; saltarla toma 1.2 s de mantener + el fundido
	revisar(prueba2.Terminada and segundos < 5.0, "mantener Espacio la salta (%.1f s)" % segundos)
	revisar(root.get_viewport().get_camera_3d() == camara_antes, "al saltarla también devuelve la cámara")

	print("Fallos: ", fallos)
	quit(1 if fallos > 0 else 0)
