extends SceneTree
## Ejecutar con renderizado. Respalda y restaura las preferencias del jugador.
const RUTA := 'user://configuracion.cfg'
var fallas := 0
func _initialize() -> void:
	call_deferred('probar')
func revisar(condicion: bool, mensaje: String) -> void:
	print(('[OK] ' if condicion else '[FALLA] ') + mensaje)
	if not condicion: fallas += 1
func tecla(codigo: Key) -> void:
	for pulsada in [true, false]:
		var evento := InputEventKey.new()
		evento.keycode = codigo
		evento.physical_keycode = codigo
		evento.pressed = pulsada
		Input.parse_input_event(evento)
		await process_frame
func probar() -> void:
	var existia := FileAccess.file_exists(RUTA)
	var respaldo := FileAccess.get_file_as_bytes(RUTA) if existia else PackedByteArray()
	var config = root.get_node('Configuracion')
	config.RestaurarTeclas()
	var opciones = load('res://Escenas/Menus/menu_opciones.tscn').instantiate()
	root.add_child(opciones)
	opciones.Abrir()
	var filas: Array = opciones.find_children('*', 'Button', true, false).filter(func(b): return b.has_meta('accion'))
	revisar(filas.size() == 13, 'Las filas del menú de main incluyen todas las acciones')
	var diario = filas.filter(func(b): return b.get_meta('accion') == 'diario')[0]
	diario.pressed.emit()
	await tecla(KEY_F3)
	revisar(config.TeclaDe('diario') == KEY_Q, 'No se puede reasignar un atajo reservado')
	await tecla(KEY_ESCAPE)
	revisar(opciones.visible, 'Esc cancela la captura sin cerrar Opciones')
	diario.pressed.emit()
	await tecla(KEY_U)
	revisar(config.TeclaDe('diario') == KEY_U, 'PanelTeclas cambia la tecla usando la fila existente')
	config.AsignarTecla('expediente', KEY_I)
	config.AsignarTecla('linterna', KEY_O)
	var tamano = opciones.find_child('TamanoTexto', true, false)
	tamano.item_selected.emit(2)
	opciones.Cerrar()
	var archivo := ConfigFile.new()
	archivo.load(RUTA)
	revisar(is_equal_approx(float(archivo.get_value('accesibilidad', 'escala_texto')), 1.3), 'Se guarda escala_texto')
	revisar(archivo.get_section_keys('teclas').size() == 3, 'Se guardan solo las tres teclas diferentes de project.godot')
	var recargada = load('res://Escenas/Autoload/Configuracion.cs').new()
	root.add_child(recargada)
	revisar(is_equal_approx(float(recargada.EscalaTexto), 1.3) and recargada.TeclaDe('diario') == KEY_U, 'La configuración se recupera desde el archivo')
	var hud = load('res://hud/hud.tscn').instantiate()
	root.add_child(hud)
	await tecla(KEY_Q)
	revisar(not hud.diario_abierto, 'La tecla anterior del diario deja de activarlo')
	await tecla(KEY_U)
	revisar(hud.diario_abierto, 'La tecla nueva abre el diario')
	await tecla(KEY_ESCAPE)
	await tecla(KEY_I)
	revisar(hud._expediente.abierto, 'El expediente usa su acción reasignada')
	await tecla(KEY_I)
	await tecla(KEY_O)
	revisar(hud.linterna_encendida, 'La linterna usa su acción reasignada')
	revisar(hud._tam(20) == 26, 'El HUD aplica el tamaño del texto')
	recargada.RestaurarTeclas()
	recargada.Guardar()
	archivo.clear()
	archivo.load(RUTA)
	revisar(not archivo.has_section('teclas'), 'Restablecer elimina las teclas personalizadas del archivo')
	if existia:
		var f := FileAccess.open(RUTA, FileAccess.WRITE)
		f.store_buffer(respaldo)
		f.close()
	else:
		DirAccess.remove_absolute(RUTA)
	print('CONFIGURACION: ', 'OK' if fallas == 0 else '%d FALLAS' % fallas)
	quit(0 if fallas == 0 else 1)
