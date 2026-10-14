extends SceneTree
## Prueba la cinemática de inicio dentro del flujo de capítulos (con ventana).
## Uso: --path game --script res://Herramientas/probar_intro.gd -- <carpeta_capturas>
## 1) El Caso 0 empieza sin cinemática. 2) El Capítulo 1 la muestra la primera vez, con el juego en pausa.
## 3) Mantener [Espacio] la salta, el juego sigue y aparece el aviso del expediente. 4) Al repetir el capítulo ya vista, no vuelve a salir.
## Respalda y devuelve las ranuras y el registro de cinemáticas vistas.

const PARTIDAS := "user://partidas/"
const VISTAS := "user://cinematicas.cfg"

var _fallos := 0

func _initialize() -> void:
	var carpeta: String = OS.get_cmdline_user_args()[0]
	DirAccess.make_dir_recursive_absolute(carpeta)
	var respaldo := {}
	for archivo in DirAccess.get_files_at(PARTIDAS):
		respaldo[archivo] = FileAccess.get_file_as_bytes(PARTIDAS + archivo)
	var vistas := FileAccess.get_file_as_bytes(VISTAS) if FileAccess.file_exists(VISTAS) else PackedByteArray()
	DirAccess.remove_absolute(VISTAS)
	await process_frame
	var gestor = root.get_node("GestorPartida")

	gestor.IniciarCapitulo("caso0", true, true)
	await create_timer(4.0).timeout
	_revisar(_intro() == null, "el Caso 0 empieza sin cinemática")

	gestor.IniciarCapitulo("capitulo1", true, false)
	await create_timer(4.0).timeout
	var intro = _intro()
	_revisar(intro != null, "el Capítulo 1 muestra la cinemática")
	_revisar(paused, "el juego queda en pausa durante la cinemática")
	await create_timer(4.0).timeout
	await _captura(carpeta.path_join("flujo_1_cinematica.png"))
	_tecla(KEY_SPACE, true)
	await create_timer(1.8).timeout
	_tecla(KEY_SPACE, false)
	await create_timer(1.5).timeout
	_revisar(not paused, "al saltarla, el juego sigue")
	await _captura(carpeta.path_join("flujo_2_aviso.png"))
	await create_timer(5.5).timeout
	_revisar(_intro() == null, "la cinemática se libera al terminar")

	gestor.IniciarCapitulo("capitulo1", true, false)
	await create_timer(5.0).timeout
	_revisar(_intro() == null, "al repetir el capítulo ya no sale")

	for archivo in DirAccess.get_files_at(PARTIDAS):
		DirAccess.remove_absolute(PARTIDAS + archivo)
	for archivo in respaldo:
		var f := FileAccess.open(PARTIDAS + archivo, FileAccess.WRITE)
		f.store_buffer(respaldo[archivo])
	DirAccess.remove_absolute(VISTAS)
	if vistas.size() > 0:
		FileAccess.open(VISTAS, FileAccess.WRITE).store_buffer(vistas)
	print("RESULTADO: ", "OK" if _fallos == 0 else "%d FALLOS" % _fallos)
	paused = false
	quit(_fallos)

func _intro() -> Node:
	for hijo in root.get_children():
		if hijo.get_script() and str(hijo.get_script().resource_path).ends_with("IntroCaso.cs") and not hijo.is_queued_for_deletion():
			return hijo
	return null

func _tecla(tecla: Key, presionada: bool) -> void:
	var e := InputEventKey.new()
	e.keycode = tecla
	e.physical_keycode = tecla
	e.pressed = presionada
	Input.parse_input_event(e)

func _captura(ruta: String) -> void:
	await RenderingServer.frame_post_draw
	root.get_texture().get_image().save_png(ruta)

func _revisar(ok: bool, que: String) -> void:
	print(("  ✓ " if ok else "  ✗ ") + que)
	if not ok: _fallos += 1
