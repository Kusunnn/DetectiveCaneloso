extends SceneTree
# Prueba la escena real y la entrada H; no guarda partidas ni preferencias.
var fallos := 0

func comprobar(condicion: bool, mensaje: String) -> void:
	if not condicion:
		fallos += 1
		push_error(mensaje)

func pulsar_h() -> void:
	var evento := InputEventKey.new()
	evento.keycode = KEY_H
	evento.physical_keycode = KEY_H
	evento.pressed = true
	Input.parse_input_event(evento)
	await process_frame
	evento = evento.duplicate()
	evento.pressed = false
	Input.parse_input_event(evento)
	await process_frame

func _initialize() -> void:
	call_deferred("probar")

func probar() -> void:
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
	InputMap.action_erase_events("ayuda")
	var tecla := InputEventKey.new()
	tecla.physical_keycode = KEY_H
	InputMap.action_add_event("ayuda", tecla)
	await create_timer(1.2).timeout
	var dialogo = escena.get_node("VentanaDialogo")
	for i in 12:
		if not dialogo.Abierto: break
		var avanzar := InputEventKey.new()
		avanzar.keycode = KEY_SPACE
		avanzar.pressed = true
		Input.parse_input_event(avanzar)
		await create_timer(0.25).timeout
	comprobar(not dialogo.Abierto, "Se cerró la introducción")
	comprobar(jugador.Investigando, "El jugador puede investigar")
	texto.hide()
	await pulsar_h()
	comprobar(caso.CapturarEstado().ayudas.usos == 0, "Antes del umbral no se consume ayuda")
	caso.UmbralAyudaSegundos = 1
	await create_timer(1.1).timeout
	comprobar(not texto.visible, "No hay orientación automática al cumplir el umbral")
	await pulsar_h()
	comprobar(texto.is_visible_in_tree() and "fogata" in texto.text, "H muestra una orientación visible hacia la fogata")
	comprobar(caso.CapturarEstado().ayudas.usos == 1, "H registra una ayuda realmente mostrada")
	texto.hide()
	configuracion.AyudasInvestigacion = false
	await pulsar_h()
	comprobar(not texto.visible and caso.CapturarEstado().ayudas.usos == 1, "Desactivar bloquea texto y consumo")
	print("HU-10 campamento: %d fallos" % fallos)
	quit(0 if fallos == 0 else 1)
