extends SceneTree
## El interrogatorio y la acusación, jugados por sus menús en la sala de verdad: credibilidad agotada, armar el caso vacío y salir,
## caso incompleto (seguir investigando y acusar de todos modos), careo con un error y Final A, y guardar y cargar en medio.
## Uso: --headless --path game --script res://Herramientas/Caso1/probar_acusacion.gd   (con «-- solo-<nombre>», una parte)
## Respalda en disco (user://respaldo_pruebas/) las ranuras y el progreso, y los devuelve al final.

const PARTIDAS := "user://partidas/"
const ARCHIVOS := ["user://progreso.json", "user://progreso.json.bak", "user://cinematicas.cfg"]
const RESPALDO := "user://respaldo_pruebas/"
const SALA := "res://Escenas/Caso1/Interrogatorio/sala_interrogatorio_caso1.tscn"
const CASO_OPORTUNIDAD := ["cuaderno_eriz", "foto_aya_2312", "botas_tony"]
const CASO_MEDIO := ["remo_sangre", "hueco_remo"]
const CASO_MOTIVO := ["libro_guardia_quemado", "nota_porky", "recorte_periodico"]

var _fallos := 0
var _gestor
var _flujo
var _sala
var _dialogo

func _initialize() -> void:
	if DirAccess.dir_exists_absolute(RESPALDO): _devolver_respaldo()
	_respaldar()
	await process_frame
	_gestor = root.get_node("GestorPartida")
	_flujo = load("res://tests/FlujoCaso1Prueba.cs").new()
	root.add_child(_flujo)
	var args := OS.get_cmdline_user_args()
	var solo: String = args[0].trim_prefix("solo-") if args.size() > 0 else ""
	if solo == "" or solo == "credibilidad": await _credibilidad_agotada()
	if solo == "" or solo == "vacio": await _caso_vacio()
	if solo == "" or solo == "incompleto": await _caso_incompleto_seguir()
	if solo == "" or solo == "guardar": await _guardar_en_medio()
	if solo == "" or solo == "final-b": await _caso_incompleto_acusar()
	if solo == "" or solo == "final-a": await _caso_completo()
	if solo == "" or solo == "desorden": await _tony_en_desorden()
	print("Fallos: %d" % _fallos)
	_devolver_respaldo()
	quit(1 if _fallos > 0 else 0)

func _revisar(ok: bool, texto: String) -> void:
	print("  %s %s" % ["✓" if ok else "✗", texto])
	if not ok: _fallos += 1

func _nueva(dar_todo := true) -> void:
	_gestor.NuevaPartida(true)
	await _esperar(current_scene != null)
	await create_timer(1.5).timeout
	_gestor.CambiarEscenaDelCapitulo(SALA, "EntradaDesdeLago")
	for i in 200:
		await process_frame
		if current_scene != null and current_scene.scene_file_path == SALA: break
	await create_timer(1.0).timeout
	_sala = current_scene.get_node("Caso1Sala")
	_dialogo = current_scene.get_node("VentanaDialogo")
	if dar_todo: _flujo.DarTodo(_sala)
	# La presentación de Hecdog la primera vez
	await create_timer(1.0).timeout
	await _jugar(["Seguir investigando"], func(): return not _flujo.EnDialogo(_sala), 30.0)

func _esperar(_c) -> void:
	await process_frame

## Va pasando líneas y eligiendo las opciones del plan, en orden, hasta que la condición se cumple. Devuelve si se cumplió
func _jugar(plan: Array, fin: Callable, segundos := 90.0) -> bool:
	var limite := Time.get_ticks_msec() + segundos * 1000
	var resto := plan.duplicate()
	while Time.get_ticks_msec() < limite:
		if fin.call(): return true
		var panel = _sala.Panel
		if root.get_node_or_null("CinematicaCaso1") != null:
			_flujo.SaltarCinematica(root.get_node("CinematicaCaso1"))
		elif panel != null and panel.EsperandoJugador:
			if resto.is_empty(): return fin.call()
			var paso = resto.pop_front()
			if paso is Callable: paso.call(panel)
		elif _dialogo.PruebasHayOpciones:
			if resto.is_empty():
				if fin.call(): return true
			elif resto[0] is String:
				var f: String = resto.pop_front()
				if not _dialogo.PruebasElegir(f):
					print("    no encontré la opción «%s» entre %s" % [f, _dialogo.PruebasOpciones()])
					return false
		else:
			_dialogo.PruebasAvanzar()
		await create_timer(0.03).timeout
	print("    se acabó el tiempo; opciones: %s, plan: %s" % [_dialogo.PruebasOpciones(), resto.size()])
	return false

func _hablar_con_hecdog() -> void:
	_sala.Hecdog.Interactuar(current_scene.get_node("Jugador"))

func _libre() -> bool:
	return not _flujo.EnDialogo(_sala)

func _credibilidad_agotada() -> void:
	print("== Errores sin castigo, corazonada y tres pruebas")
	await _nueva(false)
	_flujo.DarPistas(_sala)
	_flujo.Credibilidad(_sala, 2)
	_hablar_con_hecdog()
	# Dos veces la frase que no es: a la segunda, la corazonada la marca y arranca ahí
	var ok := await _jugar(["Interrogar a Miawrio", "Confrontar", "Confrontar"], func(): return _flujo.Errores(_sala, "MI-1") == 2 and _dialogo.PruebasHayOpciones and _dialogo.PruebasAyuda.contains("Frase 3/"))
	_revisar(ok, "dos errores marcan la frase correcta")
	# Tercer error con una prueba que no es
	ok = await _jugar(["Confrontar", "Ver todas", "lena_intacta"], func(): return _flujo.Errores(_sala, "MI-1") == 3 and _dialogo.PruebasHayOpciones)
	_revisar(ok and _flujo.ValorCredibilidad(_sala) == 2, "la credibilidad no baja del mínimo y no corta el interrogatorio")
	ok = await _jugar(["Confrontar"], func(): return _dialogo.PruebasOpciones().contains("Mejor no"))
	_revisar(ok and _dialogo.PruebasOpciones().split(" | ").size() == 4, "después de 3 errores quedan solo 3 pruebas: " + _dialogo.PruebasOpciones())
	ok = await _jugar(["oferta_compra"], func(): return _flujo.TieneConclusion(_sala, "miawrio_1"))
	_revisar(ok, "la prueba correcta rompe el candado")
	ok = await _jugar(["registro_llamadas", "Seguir investigando"], func(): return _flujo.TieneConclusion(_sala, "miawrio_oyo_cita") and _libre())
	_revisar(ok, "con un solo candado va directo a la prueba y termina")
	_revisar(not _flujo.Resuelto(_sala) and not _flujo.PasoPorAcusacion(_sala), "no lleva a acusar")
	# Tony desde el inicio, sin pruebas: dice qué falta
	await _nueva(false)
	_hablar_con_hecdog()
	ok = await _jugar(["Interrogar a Tony"], func(): return _dialogo.PruebasHayOpciones and _flujo.EnEspera(_sala) and _dialogo.PruebasAyuda.contains("camino al lago"), 60.0)
	_revisar(ok, "a Tony se le puede hablar desde el inicio y su candado dice qué falta")

func _caso_vacio() -> void:
	print("== Pedirle pistas a Hecdog, y armar el caso vacío y salir")
	await _nueva()
	_hablar_con_hecdog()
	var antes: int = _flujo.UsosAyudas(_sala)
	var pidio := await _jugar(["Pedirle una pista", "Pedirle una pista", "Seguir investigando"], _libre)
	_revisar(pidio and _flujo.UsosAyudas(_sala) == antes + 2, "Hecdog da dos niveles de pista y quedan en el cuaderno")
	_hablar_con_hecdog()
	var c: int = _flujo.ValorCredibilidad(_sala)
	var ok := await _jugar(["Armar el caso", func(p): p.Pruebas_Salir(), "Seguir investigando"], _libre)
	_revisar(ok and not _flujo.Resuelto(_sala), "sale sin final")
	_revisar(_flujo.ValorCredibilidad(_sala) == c, "salir sin presentar no cuesta credibilidad")

func _caso_incompleto_seguir() -> void:
	print("== Caso incompleto y seguir investigando")
	await _nueva()
	_hablar_con_hecdog()
	var c: int = _flujo.ValorCredibilidad(_sala)
	var ok := await _jugar(["Armar el caso", func(p): p.Pruebas_Sospechoso("tony"); p.Pruebas_Agregar(0, "cuaderno_eriz"); p.Pruebas_Presentar(), "Seguir investigando", "Seguir investigando"], _libre)
	_revisar(ok and not _flujo.Resuelto(_sala), "sigue sin final")
	_revisar(_flujo.ValorCredibilidad(_sala) == c - 1, "una revisión con errores cuesta 1 de credibilidad")
	_revisar(_flujo.CasoTiene(_sala, 0, "cuaderno_eriz"), "lo que puso en el caso se queda")

func _caso_incompleto_acusar() -> void:
	print("== Caso incompleto y acusar de todos modos (Final B)")
	await _nueva()
	_hablar_con_hecdog()
	var ok := await _jugar(["Armar el caso", func(p): p.Pruebas_Sospechoso("aya"); p.Pruebas_Presentar(), "Acusar de todos modos", "Acusar a Aya"], func(): return _flujo.Resuelto(_sala) and root.get_node_or_null("CinematicaCaso1") == null, 120.0)
	_revisar(ok and _flujo.Final(_sala) == "B", "llega al Final B con Aya")
	await create_timer(0.5).timeout

func _caso_completo() -> void:
	print("== Caso completo, careo con un error y Final A")
	await _nueva()
	_hablar_con_hecdog()
	var c: Array = [0]
	var armar := func(p):
		p.Pruebas_Sospechoso("tony")
		for id in CASO_OPORTUNIDAD: p.Pruebas_Agregar(0, id)
		for id in CASO_MEDIO: p.Pruebas_Agregar(1, id)
		for id in CASO_MOTIVO: p.Pruebas_Agregar(2, id)
		c[0] = _flujo.ValorCredibilidad(_sala)
		p.Pruebas_Presentar()
	var plan := ["Armar el caso", armar,
		"Confrontar", "oferta_compra",        # Error: Tony se burla y baja la credibilidad
		"Confrontar", "Foto de Aya de las 23:12",
		"Confrontar", "botas_tony",
		"Confrontar", "silbato_ts",
		"Confrontar", "libro_guardia_quemado",
		"hueco_remo"]                         # Queda una sola: directo a la prueba
	var ok := await _jugar(plan, func(): return _flujo.Resuelto(_sala) and root.get_node_or_null("CinematicaCaso1") == null, 240.0)
	_revisar(ok and _flujo.Final(_sala) == "A", "llega al Final A")
	_revisar(_flujo.TieneConclusion(_sala, "tony_5"), "cayeron los cinco candados de Tony")
	_revisar(_flujo.ValorCredibilidad(_sala) >= 1, "con un error, la credibilidad no llegó a 0")
	_revisar(_gestor.CinematicaVista("caso1_2320") and _gestor.CinematicaVista("caso1_2320_revelacion"), "se vieron la escena de las 23:20 y la revelación")
	await create_timer(0.5).timeout

## Los candados de Tony caen en cualquier orden: primero el remo (5.º), luego las botas (2.º), y al final los demás
func _tony_en_desorden() -> void:
	print("== Tony en desorden")
	await _nueva()
	_hablar_con_hecdog()
	var ok := await _jugar(["Interrogar a Tony", "Frase anterior", "Confrontar", "hueco_remo"], func(): return _flujo.TieneConclusion(_sala, "tony_5"), 90.0)
	_revisar(ok and not _flujo.TieneConclusion(_sala, "tony_1"), "el 5.º candado cae primero")
	ok = await _jugar(["Siguiente frase", "Confrontar", "botas_tony"], func(): return _flujo.TieneConclusion(_sala, "tony_2"))
	_revisar(ok and not _flujo.TieneConclusion(_sala, "tony_1"), "luego el 2.º, sin el 1.º")
	ok = await _jugar(["Confrontar", "Foto de Aya de las 23:12", "Confrontar", "silbato_ts", "libro_guardia_quemado"],
		func(): return _flujo.TieneConclusion(_sala, "tony_4") and _flujo.TieneConclusion(_sala, "tony_1") and _flujo.TieneConclusion(_sala, "tony_3"), 90.0)
	_revisar(ok, "los cinco caen en otro orden")
	_revisar(not _flujo.Resuelto(_sala), "sin la Reconstrucción no hay final")

## Guarda en la ranura de prueba, carga y regresa a la sala
func _guardar_cargar() -> void:
	_revisar(_gestor.PuedeGuardar, "se puede guardar mientras el juego espera al jugador")
	var ranura := 3
	_revisar(_gestor.Guardar(ranura, false, ""), "guarda")
	await create_timer(0.3).timeout
	_gestor.CargarRanura(ranura, false)
	for i in 400:
		await process_frame
		if current_scene != null and current_scene.scene_file_path == SALA and current_scene.has_node("Caso1Sala") and current_scene.get_node("Caso1Sala") != _sala: break
	await create_timer(1.2).timeout
	_sala = current_scene.get_node("Caso1Sala")
	_dialogo = current_scene.get_node("VentanaDialogo")

func _guardar_en_medio() -> void:
	print("== Guardar y cargar en medio")
	await _nueva(false)
	_flujo.DarPistas(_sala)
	# En el interrogatorio
	_flujo.Credibilidad(_sala, 4)
	_hablar_con_hecdog()
	var ok := await _jugar(["Interrogar a Miawrio"], func(): return _dialogo.PruebasHayOpciones and _flujo.EnEspera(_sala))
	_revisar(ok, "llega al menú del interrogatorio")
	await _guardar_cargar()
	_revisar(_flujo.ValorCredibilidad(_sala) == 4 and _sala.ImplicadoActual == "", "al cargar vuelve a la sala con su credibilidad")
	# En «Armar el caso»
	_flujo.DarTodo(_sala)
	_hablar_con_hecdog()
	ok = await _jugar(["Armar el caso", func(p): p.Pruebas_Sospechoso("eriz"); p.Pruebas_Agregar(1, "remo_sangre")], func(): return _sala.Panel.EsperandoJugador and _flujo.Sospechoso(_sala) == "eriz")
	_revisar(ok, "llega a la pantalla del caso")
	await _guardar_cargar()
	_revisar(_flujo.Sospechoso(_sala) == "eriz" and _flujo.CasoTiene(_sala, 1, "remo_sangre"), "al cargar el caso sigue como estaba")
	# En el careo: se conserva lo tumbado
	_flujo.DarTodo(_sala)
	_flujo.CasoCompletoYPrimeraExcusa(_sala)
	_hablar_con_hecdog()
	ok = await _jugar(["Armar el caso", func(p): p.Pruebas_Presentar()], func(): return _dialogo.PruebasHayOpciones and _flujo.EnEspera(_sala) and _dialogo.PruebasOpciones().contains("Confrontar"), 80.0)
	_revisar(ok, "llega al careo")
	await _guardar_cargar()
	_revisar(_flujo.TieneConclusion(_sala, "tony_1") and not _flujo.Resuelto(_sala), "al cargar conserva la excusa tumbada y no hay final")

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
