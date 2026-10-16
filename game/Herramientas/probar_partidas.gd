extends SceneTree
## Prueba el flujo de partidas con ventana: ranuras, «Continuar» tras el tutorial, «Omitir tutorial»,
## el aviso al salir al menú y la cinemática del Capítulo 1.
## Uso: --path game --script res://Herramientas/probar_partidas.gd
## Respalda en disco (user://respaldo_pruebas/) y devuelve las ranuras, el progreso de capítulos y el registro
## de cinemáticas. Si una prueba anterior se cortó, primero devuelve ese respaldo.

const PARTIDAS := "user://partidas/"
const ARCHIVOS := ["user://progreso.json", "user://progreso.json.bak", "user://cinematicas.cfg"]
const RESPALDO := "user://respaldo_pruebas/"
const SALA := "res://Escenas/Interrogatorio/sala_interrogatorio.tscn"
const CAMPAMENTO := "res://Escenas/Caso1/escenario_campamento.tscn"

var _fallos := 0
var _gestor

func _initialize() -> void:
	if DirAccess.dir_exists_absolute(RESPALDO):
		_devolver_respaldo()
	_respaldar()
	_limpiar()
	await process_frame
	_gestor = root.get_node("GestorPartida")

	print("1) Ranuras: cada una conserva su partida")
	_gestor.IniciarCapitulo("caso0", false, true)
	await create_timer(3.5).timeout
	await _cerrar_dialogos()
	var a := Vector3(1.5, 1.0, 2.0)
	var b := Vector3(-2.0, 1.0, 1.0)
	_jugador().global_position = a
	await create_timer(0.3).timeout
	_revisar(_gestor.Guardar(1, false, ""), "guarda la ranura 1 en el tutorial")
	_jugador().global_position = b
	await create_timer(0.3).timeout
	_revisar(_gestor.Guardar(2, false, ""), "guarda la ranura 2 en el tutorial")
	_gestor.IniciarCapitulo("capitulo1", false, true)
	await create_timer(3.5).timeout
	_revisar(_intro() != null, "la cinemática sale al empezar el Capítulo 1")
	await _saltar_intro()
	await _cerrar_dialogos()
	var c: Vector3 = _jugador().global_position + Vector3(2.0, 0, 1.0)
	_jugador().global_position = c
	await create_timer(0.3).timeout
	_revisar(_gestor.Guardar(3, false, ""), "guarda la ranura 3 en el Capítulo 1")
	_revisar(_capitulo(1) == "caso0" and _capitulo(2) == "caso0" and _capitulo(3) == "capitulo1", "cada ranura dice su capítulo")
	for prueba in [[1, SALA, a], [2, SALA, b], [3, CAMPAMENTO, c]]:
		_revisar(_gestor.CargarRanura(prueba[0], false), "carga la ranura %d" % prueba[0])
		await create_timer(3.5).timeout
		await _cerrar_dialogos()
		_revisar(current_scene.scene_file_path == prueba[1], "  la ranura %d abre su escena" % prueba[0])
		var distancia: float = _jugador().global_position.distance_to(prueba[2])
		_revisar(distancia < 0.3, "  la ranura %d vuelve a su posición (%.2f m)" % [prueba[0], distancia])
		_revisar(_intro() == null, "  al cargar no sale la cinemática")

	print("2) «Continuar» después de terminar el tutorial")
	_gestor.CargarRanura(1, false)
	await create_timer(3.5).timeout
	await _cerrar_dialogos()
	_gestor.Guardar(0, false, "")
	# La partida guardada al cerrar el caso queda en la fase Epílogo
	_poner_fase(PARTIDAS + "ranura_0.json", "Epílogo")
	_gestor.ContinuarPartida()
	await create_timer(3.5).timeout
	_revisar(current_scene.scene_file_path == CAMPAMENTO, "«Continuar» lleva al Capítulo 1, no al tutorial")
	_revisar(_intro() != null, "  y empieza con la cinemática")
	await _saltar_intro()

	print("3) «Omitir tutorial»")
	_gestor.IrAlMenuInicio()
	await create_timer(1.5).timeout
	_gestor.NuevaPartida(true)
	await create_timer(3.5).timeout
	_revisar(current_scene.scene_file_path == CAMPAMENTO, "«Omitir tutorial» empieza en el Capítulo 1")
	_revisar(_intro() != null, "  con la cinemática")
	await _saltar_intro()
	await _cerrar_dialogos()

	print("4) Aviso al volver al menú principal")
	_gestor.HayCambiosSinGuardar = false
	_menu_principal()
	await create_timer(0.5).timeout
	var dialogo := _dialogo_salida()
	_revisar(dialogo != null and dialogo.visible, "aparece el aviso aunque no haya cambios sin guardar")
	_boton(dialogo, "Cancelar").emit_signal("pressed")
	await create_timer(0.8).timeout
	_revisar(current_scene.scene_file_path == CAMPAMENTO, "«Cancelar» sigue en la partida")
	_menu_principal()
	await create_timer(0.5).timeout
	_boton(dialogo, "Salir sin guardar").emit_signal("pressed")
	await create_timer(1.5).timeout
	_revisar(current_scene.scene_file_path == "res://Escenas/Menus/menu_inicio.tscn", "«Salir sin guardar» vuelve al menú")

	_devolver_respaldo()
	print("RESULTADO: ", "OK" if _fallos == 0 else "%d FALLOS" % _fallos)
	paused = false
	quit(_fallos)

## Copia en disco las partidas y el progreso antes de tocarlos
func _respaldar() -> void:
	DirAccess.make_dir_recursive_absolute(RESPALDO + "partidas")
	for archivo in DirAccess.get_files_at(PARTIDAS):
		DirAccess.copy_absolute(PARTIDAS + archivo, RESPALDO + "partidas/" + archivo)
	for archivo in ARCHIVOS:
		if FileAccess.file_exists(archivo):
			DirAccess.copy_absolute(archivo, RESPALDO + archivo.get_file())

## Deja todo como estaba antes de la prueba y borra el respaldo
func _devolver_respaldo() -> void:
	_limpiar()
	for archivo in DirAccess.get_files_at(RESPALDO + "partidas"):
		DirAccess.copy_absolute(RESPALDO + "partidas/" + archivo, PARTIDAS + archivo)
		DirAccess.remove_absolute(RESPALDO + "partidas/" + archivo)
	for archivo in DirAccess.get_files_at(RESPALDO):
		DirAccess.copy_absolute(RESPALDO + archivo, "user://" + archivo)
		DirAccess.remove_absolute(RESPALDO + archivo)
	DirAccess.remove_absolute(RESPALDO + "partidas")
	DirAccess.remove_absolute(RESPALDO)

func _capitulo(ranura: int) -> String:
	var envoltorio: Dictionary = JSON.parse_string(FileAccess.get_file_as_string(PARTIDAS + "ranura_%d.json" % ranura))
	return JSON.parse_string(envoltorio["datos"]).get("capitulo", "")

func _limpiar() -> void:
	for archivo in DirAccess.get_files_at(PARTIDAS):
		DirAccess.remove_absolute(PARTIDAS + archivo)
	for archivo in ARCHIVOS:
		DirAccess.remove_absolute(archivo)

## Cambia la fase del resumen de una partida respetando su envoltorio (suma md5 de los datos)
func _poner_fase(ruta: String, fase: String) -> void:
	var envoltorio: Dictionary = JSON.parse_string(FileAccess.get_file_as_string(ruta))
	var datos: Dictionary = JSON.parse_string(envoltorio["datos"])
	datos["resumen"]["fase"] = fase
	var cuerpo := JSON.stringify(datos)
	envoltorio["datos"] = cuerpo
	envoltorio["suma"] = cuerpo.md5_text()
	FileAccess.open(ruta, FileAccess.WRITE).store_string(JSON.stringify(envoltorio))

func _jugador() -> Node3D:
	return current_scene.get_node("Jugador")

func _intro() -> Node:
	for hijo in root.get_children():
		if hijo.get_script() and str(hijo.get_script().resource_path).ends_with("IntroCaso.cs") and not hijo.is_queued_for_deletion():
			return hijo
	return null

func _saltar_intro() -> void:
	if _intro() == null:
		return
	var e := InputEventKey.new()
	e.keycode = KEY_SPACE
	e.physical_keycode = KEY_SPACE
	e.pressed = true
	Input.parse_input_event(e)
	await create_timer(1.8).timeout
	var s := e.duplicate()
	s.pressed = false
	Input.parse_input_event(s)
	await create_timer(1.5).timeout

## Cierra el diálogo de apertura del capítulo para poder guardar
func _cerrar_dialogos() -> void:
	for i in 12:
		var dialogo = current_scene.get_node_or_null("VentanaDialogo")
		if dialogo == null or not dialogo.Abierto:
			return
		var clic := InputEventMouseButton.new()
		clic.button_index = MOUSE_BUTTON_LEFT
		clic.pressed = true
		clic.position = Vector2(400, 400)
		Input.parse_input_event(clic)
		await process_frame
		var suelta := clic.duplicate()
		suelta.pressed = false
		Input.parse_input_event(suelta)
		await create_timer(0.4).timeout

func _dialogo_salida() -> Control:
	for nodo in _gestor.find_children("*", "Control", true, false):
		if nodo.get_script() and str(nodo.get_script().resource_path).ends_with("DialogoSalida.cs"):
			return nodo
	return null

## Lo mismo que el botón «Menú principal» del menú de pausa
func _menu_principal() -> void:
	var menu := current_scene.get_node("MenuPausa")
	(menu.find_child("BotonMenuInicio", true, false) as Button).emit_signal("pressed")

func _boton(padre: Node, texto: String) -> Button:
	for b in padre.find_children("*", "Button", true, false):
		if b.text == texto:
			return b
	return null

func _revisar(ok: bool, que: String) -> void:
	print(("  ✓ " if ok else "  ✗ ") + que)
	if not ok: _fallos += 1
