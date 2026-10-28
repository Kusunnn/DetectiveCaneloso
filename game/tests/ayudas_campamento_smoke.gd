extends SceneTree
# Prueba el cuaderno en la escena real; no guarda partidas ni preferencias.
var fallos := 0

func comprobar(condicion: bool, mensaje: String) -> void:
	if not condicion:
		fallos += 1
		push_error(mensaje)

func _initialize() -> void:
	call_deferred("probar")

func probar() -> void:
	# Aislar el cuaderno: no ejecutar el autoguardado periódico ni generar miniaturas headless.
	root.get_node("GestorPartida").set_process(false)
	var escena = load("res://Escenas/Caso1/escenario_campamento.tscn").instantiate()
	# Falla antes de ejecutar la escena si falta el componente que muestra la ayuda.
	var texto: Label = escena.get_node_or_null("CanvasLayer/TextoTutorial")
	comprobar(texto != null, "El campamento debe tener un texto donde mostrar las orientaciones")
	if texto == null:
		escena.free()
		quit(1)
		return
	root.add_child(escena)
	current_scene = escena
	var caso = escena.get_node("Caso1")
	var jugador = escena.get_node("Jugador")
	jugador.set_physics_process(false)
	var configuracion = root.get_node("Configuracion")
	configuracion.AyudasInvestigacion = true
	await create_timer(1.2).timeout
	var dialogo = escena.get_node("VentanaDialogo")
	for i in 80:
		if not dialogo.Abierto: break
		var avanzar := InputEventMouseButton.new()
		avanzar.button_index = MOUSE_BUTTON_LEFT
		avanzar.pressed = true
		Input.parse_input_event(avanzar)
		await process_frame
		var soltar = avanzar.duplicate()
		soltar.pressed = false
		Input.parse_input_event(soltar)
		var elegir := InputEventKey.new()
		elegir.keycode = KEY_3
		elegir.pressed = true
		Input.parse_input_event(elegir)
		await create_timer(0.25).timeout
	comprobar(not dialogo.Abierto, "Se cerró la introducción")

	paused = true
	caso.PedirPista()
	comprobar(caso.CapturarEstado().ayudas.entradas.size() == 1, "Pedir desde pausa no requiere esperar")
	if caso.CapturarEstado().ayudas.entradas.is_empty():
		paused = false
		quit(1)
		return
	comprobar("fogata" in str(caso.CapturarEstado().ayudas.entradas[0].texto), "La orientación apunta a la fogata")
	caso.PedirPista()
	comprobar(caso.CapturarEstado().ayudas.usos == 1, "Releer no consume otra ayuda")
	var estado = caso.CapturarEstado()
	comprobar(estado.ayudas.entradas.size() == 1, "El historial se incluye en el guardado real")
	configuracion.AyudasInvestigacion = false
	comprobar("Opciones" in caso.PedirPista() and caso.CapturarEstado().ayudas.usos == 1, "Desactivar impide solicitar, conserva el cuaderno")
	comprobar("fogata" in str(caso.CapturarEstado().ayudas.entradas[0].texto), "Se puede releer con las ayudas desactivadas")
	paused = false
	print("HU-10 campamento: %d fallos" % fallos)
	quit(0 if fallos == 0 else 1)
