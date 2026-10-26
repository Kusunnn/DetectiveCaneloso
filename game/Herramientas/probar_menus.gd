extends SceneTree
## Ejecutar con renderizado: Godot --path game --script res://Herramientas/probar_menus.gd
## Regresión: una sola pantalla recibe pausa/cancelar y el HUD no cambia bajo la pausa.
var fallas := 0

func comprobar(condicion: bool, mensaje: String) -> void:
	print(('[OK] ' if condicion else '[FALLA] ') + mensaje)
	if not condicion:
		fallas += 1

func tecla(codigo: Key) -> void:
	for presionada in [true, false]:
		var evento := InputEventKey.new()
		evento.keycode = codigo
		evento.physical_keycode = codigo
		evento.pressed = presionada
		Input.parse_input_event(evento)
		await process_frame

func _initialize() -> void:
	call_deferred('probar')

func probar() -> void:
	var escena := Node.new()
	root.add_child(escena)
	current_scene = escena
	var hud = load('res://hud/hud.tscn').instantiate()
	var menu = load('res://Escenas/Menus/menu_pausa.tscn').instantiate()
	escena.add_child(hud)
	escena.add_child(menu)
	await process_frame
	comprobar(not menu.Abierto, 'La pausa empieza cerrada')
	await tecla(KEY_ESCAPE)
	comprobar(menu.Abierto and paused, 'Esc abre la pausa')
	await tecla(KEY_Q)
	comprobar(not hud.diario_abierto, 'El diario no se abre bajo la pausa')
	menu.get_node('%BotonOpciones').pressed.emit()
	comprobar(menu.get_node('%MenuOpciones').visible and not menu.get_node('%PantallaPrincipal').visible, 'Opciones oculta el menú principal')
	await tecla(KEY_ESCAPE)
	comprobar(menu.Abierto and not menu.get_node('%MenuOpciones').visible and menu.get_node('%PantallaPrincipal').visible, 'Esc vuelve de Opciones sin cerrar la pausa')
	await tecla(KEY_ESCAPE)
	comprobar(not menu.Abierto and not paused, 'Otro Esc reanuda el juego')
	paused = true
	await tecla(KEY_ESCAPE)
	comprobar(not menu.Abierto and paused, 'La pausa no invade otra pantalla modal')
	paused = false
	await tecla(KEY_Q)
	comprobar(hud.diario_abierto, 'Q abre el diario')
	await tecla(KEY_ESCAPE)
	comprobar(not hud.diario_abierto and not menu.Abierto, 'Esc cierra solamente el diario')
	print('RESULTADO: %s' % ('OK' if fallas == 0 else '%d FALLAS' % fallas))
	quit(0 if fallas == 0 else 1)
