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
	revisar(sala.get_node_or_null("FiguraCinta") == null, "la figura se borra")

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
	# Dura 41 s; saltarla toma 1.2 s de mantener + el fundido
	revisar(prueba2.Terminada and segundos < 5.0, "mantener Espacio la salta (%.1f s)" % segundos)
	revisar(root.get_viewport().get_camera_3d() == camara_antes, "al saltarla también devuelve la cámara")

	print("Fallos: ", fallos)
	quit(1 if fallos > 0 else 0)
