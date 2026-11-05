extends SceneTree
## Capturas del interrogatorio con candados, con ventana: frases, pruebas relacionadas, corazonada y diario.
## Uso: --path game --script res://Herramientas/Caso1/capturas_candados.gd -- <carpeta de salida>
## Respalda en disco (user://respaldo_pruebas/) las ranuras y el progreso, y los devuelve al final.

const PARTIDAS := "user://partidas/"
const ARCHIVOS := ["user://progreso.json", "user://progreso.json.bak", "user://cinematicas.cfg"]
const RESPALDO := "user://respaldo_pruebas/"
const SALA := "res://Escenas/Caso1/Interrogatorio/sala_interrogatorio_caso1.tscn"

var _gestor
var _flujo
var _sala
var _dialogo
var _salida := "user://capturas_candados/"

func _initialize() -> void:
	if DirAccess.dir_exists_absolute(RESPALDO): _devolver_respaldo()
	_respaldar()
	await process_frame
	var args := OS.get_cmdline_user_args()
	if args.size() > 0: _salida = args[0].trim_suffix("/") + "/"
	DirAccess.make_dir_recursive_absolute(_salida)
	root.size = Vector2i(1280, 720)
	_gestor = root.get_node("GestorPartida")
	_flujo = load("res://tests/FlujoCaso1Prueba.cs").new()
	root.add_child(_flujo)
	_gestor.NuevaPartida(true)
	await process_frame
	await create_timer(1.5).timeout
	_gestor.CambiarEscenaDelCapitulo(SALA, "EntradaDesdeLago")
	for i in 300:
		await process_frame
		if current_scene != null and current_scene.scene_file_path == SALA: break
	await create_timer(1.5).timeout
	_sala = current_scene.get_node("Caso1Sala")
	_dialogo = current_scene.get_node("VentanaDialogo")
	_flujo.DarPistas(_sala)
	await _jugar(["Seguir investigando"], func(): return not _flujo.EnDialogo(_sala))
	_sala.Hecdog.Interactuar(current_scene.get_node("Jugador"))
	await _jugar([], func(): return _dialogo.PruebasHayOpciones)
	await _foto("01_menu_hecdog")
	await _jugar(["Interrogar a Miawrio"], func(): return _dialogo.PruebasHayOpciones and _flujo.EnEspera(_sala))
	await _foto("02_frase")
	await _jugar(["Confrontar", "Confrontar"], func(): return _flujo.Errores(_sala, "MI-1") == 2 and _dialogo.PruebasHayOpciones and _dialogo.PruebasAyuda.contains("Frase 3/"))
	await _foto("03_corazonada")
	await _jugar(["Confrontar"], func(): return _dialogo.PruebasOpciones().contains("Mejor no"))
	await _foto("04_relacionadas")
	await _jugar(["oferta_compra"], func(): return _flujo.TieneConclusion(_sala, "miawrio_1"), 20.0, true)
	await create_timer(0.25).timeout
	await _foto("05_candado_cayendo")
	await _jugar(["Terminar", "Mejor no"], func(): return _dialogo.PruebasOpciones().contains("Interrogar a Juan Carlos"))
	await _jugar(["Interrogar a Tony"], func(): return _dialogo.PruebasHayOpciones and _flujo.EnEspera(_sala), 40.0)
	await _foto("06_tony")
	print("Capturas en ", ProjectSettings.globalize_path(_salida))
	_devolver_respaldo()
	quit()

func _foto(nombre: String) -> void:
	await create_timer(0.6).timeout
	await RenderingServer.frame_post_draw
	root.get_texture().get_image().save_png(_salida + nombre + ".png")
	print(nombre, " · ", _dialogo.PruebasOpciones())

func _jugar(plan: Array, fin: Callable, segundos := 40.0, sin_avanzar := false) -> bool:
	var limite := Time.get_ticks_msec() + segundos * 1000
	var resto := plan.duplicate()
	while Time.get_ticks_msec() < limite:
		if fin.call(): return true
		if root.get_node_or_null("CinematicaCaso1") != null:
			_flujo.SaltarCinematica(root.get_node("CinematicaCaso1"))
		elif _dialogo.PruebasHayOpciones:
			if not resto.is_empty() and _dialogo.PruebasElegir(resto[0]): resto.pop_front()
		elif not sin_avanzar:
			_dialogo.PruebasAvanzar()
		await create_timer(0.03).timeout
	print("  se acabó el tiempo: ", _dialogo.PruebasOpciones())
	return false

func _respaldar() -> void:
	DirAccess.make_dir_recursive_absolute(RESPALDO + "partidas/")
	for archivo in ARCHIVOS:
		if FileAccess.file_exists(archivo): DirAccess.copy_absolute(archivo, RESPALDO + archivo.get_file())
	if DirAccess.dir_exists_absolute(PARTIDAS):
		for f in DirAccess.get_files_at(PARTIDAS): DirAccess.copy_absolute(PARTIDAS + f, RESPALDO + "partidas/" + f)
	var marca := FileAccess.open(RESPALDO + "completo", FileAccess.WRITE)
	if marca: marca.store_string("ok")

func _devolver_respaldo() -> void:
	if not FileAccess.file_exists(RESPALDO + "completo"): return
	if DirAccess.dir_exists_absolute(PARTIDAS):
		for f in DirAccess.get_files_at(PARTIDAS): DirAccess.remove_absolute(PARTIDAS + f)
	for f in DirAccess.get_files_at(RESPALDO + "partidas/"): DirAccess.copy_absolute(RESPALDO + "partidas/" + f, PARTIDAS + f)
	for archivo in ARCHIVOS:
		if FileAccess.file_exists(RESPALDO + archivo.get_file()): DirAccess.copy_absolute(RESPALDO + archivo.get_file(), archivo)
		elif FileAccess.file_exists(archivo): DirAccess.remove_absolute(archivo)
	for f in DirAccess.get_files_at(RESPALDO + "partidas/"): DirAccess.remove_absolute(RESPALDO + "partidas/" + f)
	for f in DirAccess.get_files_at(RESPALDO): DirAccess.remove_absolute(RESPALDO + f)
