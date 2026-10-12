extends SceneTree
## HU-07: capturas de la vista de examen para la entrega (necesita ventana, porque hay que renderizar).
## Uso: --path game --script res://Herramientas/capturas_examen.gd -- <carpeta de salida>
## Las ranuras del jugador se respaldan y se devuelven al final (el caso autoguarda al recoger pistas).

const PARTIDAS := "user://partidas/"
var _salida := ""

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	_salida = args[0] if args.size() > 0 else ProjectSettings.globalize_path("user://capturas_hu07")
	DirAccess.make_dir_recursive_absolute(_salida)
	var respaldo := {}
	for archivo in DirAccess.get_files_at(PARTIDAS):
		respaldo[archivo] = FileAccess.get_file_as_bytes(PARTIDAS + archivo)
	DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
	# Sin vsync para medir lo que de verdad cuesta cada cuadro
	DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_DISABLED)
	DisplayServer.window_set_size(Vector2i(1600, 900))
	root.size = Vector2i(1600, 900)
	await _caso0()
	await _caso1()
	for archivo in DirAccess.get_files_at(PARTIDAS):
		if not respaldo.has(archivo):
			DirAccess.remove_absolute(PARTIDAS + archivo)
	for archivo in respaldo:
		var f := FileAccess.open(PARTIDAS + archivo, FileAccess.WRITE)
		f.store_buffer(respaldo[archivo])
		f.close()
	quit()

func _tiempos() -> String:
	var rid := root.get_viewport_rid()
	return "  (pantalla: cpu %.2f ms, gpu %.2f ms; proceso %.2f ms)" % [RenderingServer.viewport_get_measured_render_time_cpu(rid), RenderingServer.viewport_get_measured_render_time_gpu(rid), Performance.get_monitor(Performance.TIME_PROCESS) * 1000.0]

func _esperar(segundos: float) -> void:
	await create_timer(segundos, true).timeout

func _foto(nombre: String) -> void:
	await process_frame
	await process_frame
	root.get_texture().get_image().save_png(_salida.path_join(nombre + ".png"))
	print("Captura: ", nombre)

func _cargar(ruta: String) -> Node:
	var escena: Node = load(ruta).instantiate()
	root.add_child(escena)
	current_scene = escena
	await _esperar(1.0)
	# Sin diálogos de presentación encima
	var dialogo := escena.get_node_or_null("VentanaDialogo")
	if dialogo:
		dialogo.visible = false
	return escena

func _tecla(codigo: Key) -> void:
	var tecla := InputEventKey.new()
	tecla.keycode = codigo
	tecla.physical_keycode = codigo
	tecla.pressed = true
	Input.parse_input_event(tecla)
	await process_frame
	tecla = tecla.duplicate()
	tecla.pressed = false
	Input.parse_input_event(tecla)

func _caso0() -> void:
	var escena := await _cargar("res://Escenas/Interrogatorio/sala_interrogatorio.tscn")
	var jugador = escena.get_node("Jugador")
	var hud = escena.get_node("HUD")
	jugador.Bloquear(false)
	hud.linterna_cambiada.emit(true)
	for ruta in ["GrabadoraPista", "RelojPista", "Zonas/Pasillo/Huellas"]:
		var nodo = escena.get_node_or_null(ruta)
		if nodo == null:
			nodo = get_nodes_in_group("objetos_pista").filter(func(o): return o.name == ruta.get_file()).front()
		nodo.Interactuar(jugador)
	await _esperar(0.5)
	RenderingServer.viewport_set_measure_render_time(root.get_viewport_rid(), true)
	await _esperar(1.5)
	print("FPS del juego sin la vista: ", Engine.get_frames_per_second(), _tiempos())
	var grabadora = hud.buscar_pista(&"grabadora")
	jugador.Examinar(grabadora)
	await _esperar(1.0)
	await _foto("01_vista_examen_grabadora")
	var mundo: SubViewport = escena.get_node("VistaExamen/Visor/Mundo")
	RenderingServer.viewport_set_measure_render_time(mundo.get_viewport_rid(), true)
	await _esperar(1.5)
	print("FPS con la vista abierta: ", Engine.get_frames_per_second(), _tiempos(), "  vista 3D: cpu %.2f ms, gpu %.2f ms" % [RenderingServer.viewport_get_measured_render_time_cpu(mundo.get_viewport_rid()), RenderingServer.viewport_get_measured_render_time_gpu(mundo.get_viewport_rid())])
	var vista = escena.get_node("VistaExamen")
	vista.MirarDetalle("etiqueta")
	await _esperar(0.4)
	await _foto("02_detalle_a_la_vista")
	await _tecla(KEY_E)
	await _esperar(2.2)
	await _foto("03_detalle_descubierto")
	vista.Cerrar()
	await _esperar(0.6)
	# El reloj se examina sin detalle; las huellas se quedan solo recogidas
	jugador.Examinar(hud.buscar_pista(&"reloj"))
	await _esperar(0.8)
	await _foto("04_vista_examen_reloj")
	vista.Cerrar()
	await _esperar(0.6)
	hud.indice_pista = 0
	await _tecla(KEY_TAB)
	await _esperar(0.5)
	await _foto("05_expediente_estados")
	hud.indice_pista = 1
	await _esperar(0.3)
	await _foto("06_expediente_detalle")
	await _tecla(KEY_TAB)
	escena.queue_free()
	await _esperar(0.5)

func _caso1() -> void:
	var escena := await _cargar("res://Escenas/Caso1/escenario_campamento.tscn")
	var jugador = escena.get_node("Jugador")
	var hud = escena.get_node("HUD")
	jugador.Bloquear(false)
	hud.linterna_cambiada.emit(true)
	for o in get_nodes_in_group("objetos_pista"):
		if o.IdPista in ["foto_grupo", "registro_enfermeria", "botas_tony", "libro_guardia_quemado"]:
			o.Interactuar(jugador)
	await _esperar(0.5)
	var vista
	for par in [["foto_grupo", "dedicatoria"], ["foto_grupo", "silbato"], ["registro_enfermeria", "tachon"], ["botas_tony", "juncos"], ["libro_guardia_quemado", "antonio"]]:
		jugador.Examinar(hud.buscar_pista(StringName(par[0])))
		await _esperar(1.0)
		vista = escena.get_node("VistaExamen")
		if par[1] != "silbato":
			await _foto("1%d_%s" % [0 if par[1] == "dedicatoria" else 2, par[0]])
		vista.MirarDetalle(par[1])
		await _esperar(0.5)
		await _foto("1x_%s_%s_a_la_vista" % [par[0], par[1]])
		await _tecla(KEY_E)
		await _esperar(2.2)
		await _foto("1x_%s_%s_descubierto" % [par[0], par[1]])
		vista.Cerrar()
		await _esperar(0.6)
	await _tecla(KEY_TAB)
	await _esperar(0.5)
	await _foto("19_expediente_caso1")
	escena.queue_free()
	await _esperar(0.5)
