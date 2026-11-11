extends SceneTree
## Instinto de detective en el Campamento: colores, contenedores, duración, recarga y dificultad.
## Uso: --headless --path game --script res://Herramientas/Caso1/probar_instinto.gd
## Con ventana (sin --headless) además guarda una captura: -- <carpeta de salida>
## Respalda en disco (user://respaldo_pruebas/) las ranuras y el progreso, y los devuelve al final.

const PARTIDAS := "user://partidas/"
const ARCHIVOS := ["user://progreso.json", "user://progreso.json.bak", "user://cinematicas.cfg", "user://configuracion.cfg"]
const RESPALDO := "user://respaldo_pruebas/"
const CAMPAMENTO := "res://Escenas/Caso1/escenario_campamento.tscn"

var _fallos := 0

func _initialize() -> void:
	if DirAccess.dir_exists_absolute(RESPALDO): _devolver_respaldo()
	_respaldar()
	await process_frame
	var config = root.get_node("Configuracion")
	var dificultad: String = config.Dificultad
	var gestor = root.get_node("GestorPartida")
	gestor.NuevaPartida(true)
	for i in 400:
		await process_frame
		if current_scene != null and current_scene.scene_file_path == CAMPAMENTO: break
	await create_timer(2.0).timeout
	await _saltar_intro()
	# La presentación de Hecdog pausa el juego: se pasa antes de medir tiempos
	var dialogo = current_scene.get_node("VentanaDialogo")
	for i in 600:
		if not dialogo.Abierto and not paused: break
		dialogo.PruebasAvanzar()
		await create_timer(0.05).timeout
	_revisar(not paused, "el juego no está en pausa")
	var contorno = current_scene.get_node("ContornoPistas")
	var jugador = current_scene.get_node("Jugador")
	config.Dificultad = "detective"
	_revisar(contorno.Instinto, "el Campamento tiene instinto")
	var antes: Dictionary = contorno.Brillando()
	_revisar(antes.amarillo == 0 and antes.azul == 0, "sin instinto no brilla nada de colores")
	_revisar(contorno.Activar(), "se activa")
	await process_frame
	var b: Dictionary = contorno.Brillando()
	print("    brillando: ", b)
	_revisar(b.amarillo > 0, "amarillo: pistas sin tomar")
	_revisar(b.azul > 0 and b.iconos > 0, "azul con ícono: lo que pide linterna o agacharse")
	_revisar(b.contenedores >= 4, "brillan los contenedores con una pista adentro (baúl, almohada, papelera, escritorio) y el delantal")
	_revisar(not contorno.Activar(), "no se activa dos veces seguidas")
	var lena = _pista("lena_intacta")
	var libro = _pista("libro_guardia_quemado")
	if not DisplayServer.get_name().contains("headless") and lena != null:
		var salida: String = OS.get_cmdline_user_args()[0] if OS.get_cmdline_user_args().size() > 0 else "user://"
		for i in 400:
			if not dialogo.Abierto: break
			dialogo.PruebasAvanzar()
			await create_timer(0.05).timeout
		var centro: Vector3 = (lena.global_position + libro.global_position) / 2.0
		jugador.global_position = centro + (lena.global_position - libro.global_position).normalized().rotated(Vector3.UP, PI / 2) * 3.5
		jugador.look_at(Vector3(centro.x, jugador.global_position.y, centro.z))
		contorno.Activar()
		await create_timer(0.4).timeout
		await RenderingServer.frame_post_draw
		root.get_texture().get_image().save_png(salida.trim_suffix("/") + "/instinto_fogata.png")
	# Tomar una pista: pasa a gris
	if libro != null:
		libro.MarcarEncontrada()
		await create_timer(0.4).timeout
		_revisar(contorno.Brillando().gris > 0, "lo ya revisado brilla en gris")
	await create_timer(4.2).timeout
	_revisar(not contorno.InstintoActivo and contorno.RecargaRestante > 0, "dura unos segundos y luego se recarga")
	_revisar(not contorno.Activar(), "durante la recarga no se activa")
	config.Dificultad = "historia"
	await create_timer(10.5).timeout
	_revisar(contorno.Activar(), "después de la recarga se activa")
	await create_timer(4.3).timeout
	_revisar(contorno.RecargaRestante <= 0 and contorno.Activar(), "en Historia no hay recarga")
	await create_timer(4.3).timeout
	config.Dificultad = "experto"
	await create_timer(0.4).timeout
	_revisar(not contorno.Activar(), "en Experto no hay instinto")
	_revisar(contorno.Brillando().ambar == 0, "en Experto tampoco hay contorno ámbar")
	config.Dificultad = dificultad
	print("Fallos: %d" % _fallos)
	_devolver_respaldo()
	quit(1 if _fallos > 0 else 0)

func _pista(id: String):
	for p in get_nodes_in_group("objetos_pista"):
		if p.IdPista == id: return p
	return null

func _revisar(ok: bool, texto: String) -> void:
	print("  %s %s" % ["✓" if ok else "✗", texto])
	if not ok: _fallos += 1

func _respaldar() -> void:
	DirAccess.make_dir_recursive_absolute(RESPALDO + "partidas/")
	if DirAccess.dir_exists_absolute(PARTIDAS):
		for f in DirAccess.get_files_at(PARTIDAS): DirAccess.copy_absolute(PARTIDAS + f, RESPALDO + "partidas/" + f)
	for archivo in ARCHIVOS:
		if FileAccess.file_exists(archivo): DirAccess.copy_absolute(archivo, RESPALDO + archivo.get_file())
	# La marca va al final y dice qué archivos no existían: solo esos se borran al devolver. Sin marca no se toca nada.
	var marca := FileAccess.open(RESPALDO + "completo", FileAccess.WRITE)
	if marca: marca.store_string("\n".join(ARCHIVOS.filter(func(a): return not FileAccess.file_exists(a))))

## Deja las ranuras y el progreso como estaban antes de la prueba y borra el respaldo (carpetas incluidas)
func _devolver_respaldo() -> void:
	if not FileAccess.file_exists(RESPALDO + "completo"): return
	var faltaban := FileAccess.get_file_as_string(RESPALDO + "completo").split("\n")
	if DirAccess.dir_exists_absolute(PARTIDAS):
		for f in DirAccess.get_files_at(PARTIDAS): DirAccess.remove_absolute(PARTIDAS + f)
	else:
		DirAccess.make_dir_recursive_absolute(PARTIDAS)
	for f in DirAccess.get_files_at(RESPALDO + "partidas/"): DirAccess.copy_absolute(RESPALDO + "partidas/" + f, PARTIDAS + f)
	for archivo in ARCHIVOS:
		if FileAccess.file_exists(RESPALDO + archivo.get_file()): DirAccess.copy_absolute(RESPALDO + archivo.get_file(), archivo)
		elif FileAccess.file_exists(archivo) and archivo in faltaban: DirAccess.remove_absolute(archivo)
	for f in DirAccess.get_files_at(RESPALDO + "partidas/"): DirAccess.remove_absolute(RESPALDO + "partidas/" + f)
	DirAccess.remove_absolute(RESPALDO + "partidas")
	for f in DirAccess.get_files_at(RESPALDO): DirAccess.remove_absolute(RESPALDO + f)
	DirAccess.remove_absolute(RESPALDO)

func _saltar_intro() -> void:
	for vez in 3:
		var intro: Node = null
		for hijo in root.get_children():
			if hijo.get_script() and str(hijo.get_script().resource_path).ends_with("IntroCaso.cs") and not hijo.is_queued_for_deletion(): intro = hijo
		if intro == null: return
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
