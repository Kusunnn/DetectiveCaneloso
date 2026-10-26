extends SceneTree
## Escenas reales: el tablero con hilos se guarda y Esc cierra una sola pantalla.
func _initialize() -> void:
	call_deferred('probar')
func tecla(codigo: Key) -> void:
	for pulsada in [true, false]:
		var evento := InputEventKey.new()
		evento.keycode = codigo
		evento.physical_keycode = codigo
		evento.pressed = pulsada
		Input.parse_input_event(evento)
		await process_frame
func probar() -> void:
	for datos in [
		['res://Escenas/Interrogatorio/sala_interrogatorio.tscn', 'Caso0'],
		['res://Escenas/Caso1/escenario_campamento.tscn', 'Caso1']
	]:
		var escena = load(datos[0]).instantiate()
		root.add_child(escena)
		current_scene = escena
		await process_frame
		await process_frame
		escena.get_node('Jugador').set_physics_process(false)
		# Terminar el diálogo inicial antes de probar los controles de investigación.
		await create_timer(1.0).timeout
		var dialogo = escena.get_node('VentanaDialogo')
		for paso in range(30):
			if not dialogo.Abierto: break
			await tecla(KEY_SPACE)
			await create_timer(0.2).timeout
		assert(not dialogo.Abierto)
		var hud = escena.get_node('HUD')
		var menu = escena.get_node('MenuPausa')
		var caso = escena.get_node(datos[1])
		await tecla(KEY_R)
		assert(hud.tablero_abierto and hud.pizarron_espacial.visible)
		var relaciones = hud.relaciones_pizarron
		relaciones.desbloquear('notas')
		relaciones.guardar_nota(&'', 'Primera hipótesis', Vector2(100, 100))
		relaciones.guardar_nota(&'', 'Segunda hipótesis', Vector2(700, 300))
		var ids = hud._tarjetas_tablero.keys()
		relaciones.conectar(ids[0], ids[1])
		assert(relaciones.conexiones.size() == 1)
		await tecla(KEY_ESCAPE)
		assert(not hud.tablero_abierto and not menu.Abierto)
		var estado = JSON.parse_string(JSON.stringify(caso.CapturarEstado()))
		assert(estado.tablero.hilos.size() == 1)
		hud.restaurar_tablero({})
		caso.RestaurarEstado(estado)
		assert(hud.relaciones_pizarron.conexiones.size() == 1)
		estado.erase('tablero')
		caso.RestaurarEstado(estado)
		assert(hud.relaciones_pizarron.conexiones.is_empty())
		await tecla(KEY_ESCAPE)
		assert(menu.Abierto and paused)
		await tecla(KEY_ESCAPE)
		assert(not menu.Abierto and not paused)
		# La libreta y el cierre del tablero respetan las teclas reasignadas.
		var config = root.get_node('Configuracion')
		var original_diario = config.TeclaDe('diario')
		var original_tablero = config.TeclaDe('deducir')
		config.AsignarTecla('diario', KEY_U)
		config.AsignarTecla('deducir', KEY_P)
		await tecla(KEY_P)
		assert(hud.tablero_abierto)
		await tecla(KEY_Q)
		assert(not hud.diario_abierto)
		await tecla(KEY_U)
		assert(hud.diario_abierto)
		await tecla(KEY_P)
		assert(not hud.tablero_abierto and not menu.Abierto)
		config.AsignarTecla('diario', original_diario)
		config.AsignarTecla('deducir', original_tablero)
		print('INTERFAZ_CASO_OK: ', datos[1])
		escena.queue_free()
		await process_frame
	quit()
