extends SceneTree
## Graba, jugados de verdad por los menús, los recorridos que se revisan del interrogatorio y la acusación. Necesita ventana.
## Uso: --path game --write-movie <salida.avi> --fixed-fps 24 --resolution 1280x720 --script res://Herramientas/Caso1/video_interrogatorio.gd -- <pov|caso>
##   pov:  entrada en tercera persona → POV → presionar → presentar prueba → salida
##   caso: armar el caso incompleto → seguir investigando → caso completo → careo con un error → Final A
## Respalda en disco (user://respaldo_pruebas/) las ranuras y el progreso, y los devuelve al final.

const PARTIDAS := "user://partidas/"
const ARCHIVOS := ["user://progreso.json", "user://progreso.json.bak", "user://cinematicas.cfg"]
const RESPALDO := "user://respaldo_pruebas/"
const SALA := "res://Escenas/Caso1/Interrogatorio/sala_interrogatorio_caso1.tscn"

var _gestor
var _flujo
var _sala
var _dialogo

func _initialize() -> void:
	_respaldar()
	await process_frame
	_gestor = root.get_node("GestorPartida")
	_flujo = load("res://tests/FlujoCaso1Prueba.cs").new()
	root.add_child(_flujo)
	_gestor.NuevaPartida(true)
	await create_timer(1.0).timeout
	_gestor.CambiarEscenaDelCapitulo(SALA, "EntradaDesdeLago")
	for i in 400:
		await process_frame
		if current_scene != null and current_scene.scene_file_path == SALA: break
	await create_timer(0.8).timeout
	_sala = current_scene.get_node("Caso1Sala")
	_dialogo = current_scene.get_node("VentanaDialogo")
	await _jugar(["Seguir investigando"], func(): return not _flujo.EnDialogo(_sala), 8.0, 0.2)
	if OS.get_cmdline_user_args()[0] == "pov":
		await _pov()
	else:
		await _caso()
	await create_timer(1.0).timeout
	_devolver_respaldo()
	quit()

## Pasa las líneas al ritmo de lectura y elige las opciones con una pausa, para que se vea qué se elige
func _jugar(plan: Array, fin: Callable, segundos := 120.0, lectura := 1.6) -> void:
	var limite := Time.get_ticks_msec() + segundos * 1000
	var resto := plan.duplicate()
	while Time.get_ticks_msec() < limite:
		if fin.call(): return
		var panel = _sala.Panel
		if root.get_node_or_null("CinematicaCaso1") != null:
			await create_timer(0.2).timeout
			continue
		if panel != null and panel.EsperandoJugador:
			await create_timer(1.8).timeout
			if not resto.is_empty() and resto[0] is Callable: await resto.pop_front().call(panel)
			await create_timer(1.0).timeout
		elif _dialogo.PruebasHayOpciones:
			await create_timer(1.3).timeout
			if not resto.is_empty() and resto[0] is String: _dialogo.PruebasElegir(resto.pop_front())
		else:
			await create_timer(lectura).timeout
			# Mientras se leía pudo aparecer un menú: pasar la línea ahí elegiría la primera opción
			if not _dialogo.PruebasHayOpciones: _dialogo.PruebasAvanzar()
		await create_timer(0.1).timeout

func _pov() -> void:
	_flujo.DarPistas(_sala)
	_sala.Hecdog.Interactuar(current_scene.get_node("Jugador"))
	await _jugar(["Interrogar a Miawrio", "PREGUNTAR", "PRESIONAR", "PRESENTAR", "oferta_compra", "Seguir investigando", "Seguir investigando"], func(): return not _flujo.EnDialogo(_sala))

func _caso() -> void:
	_flujo.DarTodo(_sala)
	_sala.Hecdog.Interactuar(current_scene.get_node("Jugador"))
	var incompleto := func(p):
		p.Pruebas_Sospechoso("tony")
		p.Pruebas_Agregar(0, "cuaderno_eriz")
		p.Pruebas_Agregar(1, "remo_sangre")
		await create_timer(1.5).timeout
		p.Pruebas_Presentar()
	var completo := func(p):
		for id in ["foto_aya_2312", "botas_tony"]: p.Pruebas_Agregar(0, id)
		p.Pruebas_Agregar(1, "hueco_remo")
		for id in ["libro_guardia_quemado", "nota_porky", "recorte_periodico"]: p.Pruebas_Agregar(2, id)
		await create_timer(1.5).timeout
		p.Pruebas_Presentar()
	await _jugar(["Armar el caso", incompleto, "Seguir investigando", "Armar el caso", completo,
		"PRESENTAR", "oferta_compra", "PRESENTAR", "Foto de Aya de las 23:12", "PRESENTAR", "botas_tony",
		"PRESENTAR", "silbato_ts", "PRESENTAR", "libro_guardia_quemado", "PRESENTAR", "hueco_remo"],
		func(): return _flujo.Resuelto(_sala) and root.get_node_or_null("CinematicaCaso1") == null, 300.0, 1.2)

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
