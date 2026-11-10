extends SceneTree
## Interrogar en el Campamento, sin ir a la estación: declaración, confrontar la frase y romper el candado.
## Uso: --headless --path game --script res://Herramientas/Caso1/probar_interrogar_campamento.gd
## Respalda en disco (user://respaldo_pruebas/) las ranuras, el progreso y la configuración, y los devuelve al final.

const PARTIDAS := "user://partidas/"
const ARCHIVOS := ["user://progreso.json", "user://progreso.json.bak", "user://cinematicas.cfg", "user://configuracion.cfg"]
const RESPALDO := "user://respaldo_pruebas/"
const CAMPAMENTO := "res://Escenas/Caso1/escenario_campamento.tscn"

var _fallos := 0
var _dialogo

func _initialize() -> void:
	if DirAccess.dir_exists_absolute(RESPALDO): _devolver_respaldo()
	_respaldar()
	await process_frame
	var flujo = load("res://tests/FlujoCaso1Prueba.cs").new()
	root.add_child(flujo)
	root.get_node("GestorPartida").NuevaPartida(true)
	for i in 600:
		await process_frame
		if current_scene != null and current_scene.scene_file_path == CAMPAMENTO: break
	await create_timer(2.0).timeout
	await _saltar_intro()
	_dialogo = current_scene.get_node("VentanaDialogo")
	await _jugar(["Ir al campamento"], func(): return not _dialogo.Abierto, 60.0)
	await create_timer(1.0).timeout
	var caso = current_scene.get_node("Caso1")
	var jugador = current_scene.get_node("Jugador")
	var jc = current_scene.get_node("Personajes/JuanCarlos/Colision")
	# La primera vez es solo conversación: su versión, sin pruebas
	jc.Interactuar(jugador)
	var ok := await _jugar([], func(): return not _dialogo.Abierto and flujo.DeclaroCampamento(caso, "juan_carlos"), 40.0)
	_revisar(ok and not _dialogo.PruebasOpciones().contains("Confrontar"), "la primera charla con Juan Carlos es su versión, sin pruebas")
	# Después se elige interrogarlo ahí mismo
	jc.Interactuar(jugador)
	ok = await _jugar(["Interrogar a Juan Carlos"], func(): return _dialogo.PruebasOpciones().contains("Confrontar esta frase"), 40.0)
	_revisar(ok, "luego se puede elegir interrogarlo ahí mismo: " + _dialogo.PruebasOpciones())
	ok = await _jugar(["Terminar"], func(): return not _dialogo.Abierto, 30.0)
	_revisar(ok, "se puede terminar y seguir explorando")
	# Con la leña: confrontar la frase y romper el candado ahí mismo
	jugador.RegistrarEvidencia("lena_intacta", "lena_intacta", "", null, false)
	await create_timer(0.3).timeout
	jc.Interactuar(jugador)
	ok = await _jugar(["Interrogar a Juan Carlos", "Confrontar", "lena_intacta"], func(): return flujo.TieneConclusionCampamento(caso, "juan_carlos_1"), 60.0)
	_revisar(ok, "con la leña se rompe el primer candado de Juan Carlos en el campamento")
	await _jugar(["Terminar"], func(): return not _dialogo.Abierto, 30.0)
	print("Fallos: %d" % _fallos)
	_devolver_respaldo()
	quit(1 if _fallos > 0 else 0)

func _jugar(plan: Array, fin: Callable, segundos := 40.0) -> bool:
	var limite := Time.get_ticks_msec() + segundos * 1000
	var resto := plan.duplicate()
	while Time.get_ticks_msec() < limite:
		if fin.call(): return true
		if _dialogo.PruebasHayOpciones:
			if not resto.is_empty() and _dialogo.PruebasElegir(resto[0]): resto.pop_front()
		else:
			_dialogo.PruebasAvanzar()
		await create_timer(0.05).timeout
	print("    se acabó el tiempo; opciones: ", _dialogo.PruebasOpciones())
	return fin.call()

func _revisar(ok: bool, texto: String) -> void:
	if texto == "": return
	print("  %s %s" % ["✓" if ok else "✗", texto])
	if not ok: _fallos += 1

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
