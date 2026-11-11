extends SceneTree
## El interrogatorio y la acusación, jugados por sus menús en la sala de verdad: credibilidad agotada, armar el caso vacío y salir,
## caso incompleto (seguir investigando y acusar de todos modos), careo con un error y Final A, y guardar y cargar en medio.
## Uso: --headless --path game --script res://Herramientas/Caso1/probar_acusacion.gd   (con «-- solo-<nombre>», una parte)
## Respalda en disco (user://respaldo_pruebas/) las ranuras y el progreso, y los devuelve al final.

const PARTIDAS := "user://partidas/"
const ARCHIVOS := ["user://progreso.json", "user://progreso.json.bak", "user://cinematicas.cfg", "user://configuracion.cfg"]
const RESPALDO := "user://respaldo_pruebas/"
const SALA := "res://Escenas/Caso1/Interrogatorio/sala_interrogatorio_caso1.tscn"
const SOLUCION := {"0.0.0": "paco", "0.0.1": "libro", "0.1.0": "nota", "0.1.1": "tony", "1.0.0": "h2300", "1.0.1": "juan_carlos",
	"1.1.0": "tony", "1.1.1": "sendero", "2.0.0": "h2320", "2.0.1": "tony", "2.0.2": "remo", "2.0.3": "cobertizo",
	"3.0.0": "tony", "3.0.1": "libro", "3.0.2": "botas"}

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
	# Las cuentas de errores son las de Detective, sin importar lo que el jugador eligió
	var config = root.get_node("Configuracion")
	var dificultad: String = config.Dificultad
	config.Dificultad = "detective"
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
	if solo == "" or solo == "directo": await _final_a_directo()
	if solo == "" or solo == "ultimo": await _ultimo_candado()
	if solo == "" or solo == "fichas": await _fichas_sospechosos()
	if solo == "" or solo == "rechazo": await _rechazo_tony()
	if solo == "" or solo == "desorden": await _tony_en_desorden()
	print("Fallos: %d" % _fallos)
	config.Dificultad = dificultad
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
			# La Reconstrucción se cierra sola al sellar la última viñeta: mientras, se espera
			if not resto.is_empty() and resto[0] is Callable: resto.pop_front().call(panel)
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
	var ok := await _jugar(["Reconstruir la noche", func(p): p.Pruebas_Salir(), "Seguir investigando"], _libre)
	_revisar(ok and not _flujo.Resuelto(_sala), "sale sin final")
	_revisar(_flujo.ValorCredibilidad(_sala) == c, "salir sin presentar no cuesta credibilidad")

## La pantalla de «Caso cerrado»: rango, frase de Hecdog y volver al menú
func _cierre(final: String) -> void:
	var limite := Time.get_ticks_msec() + 90000
	while Time.get_ticks_msec() < limite and not _flujo.CierreEsperando(_sala):
		await create_timer(0.2).timeout
	var lineas: String = _flujo.LineasCierre(_sala)
	_revisar(_flujo.CierreEsperando(_sala) and lineas.contains("Candados rotos al primer intento") and lineas.contains("Pistas encontradas"), "pantalla de caso cerrado con sus datos")
	if final == "B": _revisar(lineas.contains("equivocado"), "el Final B se califica como error: " + lineas.left(160))
	_flujo.CierreAlMenu(_sala)
	await create_timer(1.5).timeout
	paused = false

func _llenar(p, cuales: Dictionary) -> void:
	for h in cuales: p.Pruebas_Poner(h, cuales[h])

func _caso_incompleto_seguir() -> void:
	print("== Una viñeta con un error y otra bien")
	await _nueva()
	_hablar_con_hecdog()
	var mensaje := [""]
	var llenar := func(p):
		_llenar(p, {"1.0.0": "h2300", "1.0.1": "aya", "1.1.0": "tony", "1.1.1": "sendero"})
		mensaje[0] = p.Pruebas_Mensaje(1)
		_llenar(p, {"0.0.0": "paco", "0.0.1": "libro", "0.1.0": "nota", "0.1.1": "tony"})
		p.Pruebas_Salir()
	var ok := await _jugar(["Reconstruir la noche", llenar, "Seguir investigando"], _libre)
	_revisar(ok and mensaje[0].begins_with("Casi"), "con un error avisa sin decir cuál: «%s»" % mensaje[0])
	_revisar(_flujo.Sellada(_sala, 0) and not _flujo.Sellada(_sala, 1), "la viñeta correcta se sella y la otra no")
	_revisar(_flujo.Hueco(_sala, "1.0.1") == "aya", "lo que puso se queda")
	_revisar(not _flujo.Resuelto(_sala), "sigue sin final")

func _caso_incompleto_acusar() -> void:
	print("== «Acusar ya» a un inocente (Final B)")
	await _nueva()
	_hablar_con_hecdog()
	var ok := await _jugar(["Acusar ya", "Acusar a Aya", "Acusar a Aya"], func(): return _flujo.Resuelto(_sala) and root.get_node_or_null("CinematicaCaso1") == null, 120.0)
	_revisar(ok and _flujo.Final(_sala) == "B", "llega al Final B con Aya")
	await _cierre("B")

func _rechazo_tony() -> void:
	print("== «Acusar ya» a Tony sin la noche completa")
	await _nueva()
	_hablar_con_hecdog()
	var ok := await _jugar(["Acusar ya", "Acusar a Tony", "Acusar a Tony", "Seguir investigando"], _libre)
	_revisar(ok and not _flujo.Resuelto(_sala) and not _flujo.PasoPorAcusacion(_sala), "Hecdog lo frena, sin final ni castigo")

func _caso_completo() -> void:
	print("== Noche completa con candados de Tony: careo con un error y Final A")
	await _nueva()
	_hablar_con_hecdog()
	var plan := ["Reconstruir la noche", func(p): _llenar(p, SOLUCION),
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
	_revisar(_flujo.ActoVisto(_sala, 3) and _flujo.ActoVisto(_sala, 4), "se anunciaron los actos 3 y 4")
	await _cierre("A")

func _final_a_directo() -> void:
	print("== Tony ya sin candados: sellar la noche lleva directo al Final A")
	await _nueva()
	_flujo.RomperTony(_sala)
	_hablar_con_hecdog()
	var ok := await _jugar(["Reconstruir la noche", func(p): _llenar(p, SOLUCION)], func(): return _flujo.Resuelto(_sala) and root.get_node_or_null("CinematicaCaso1") == null, 200.0)
	_revisar(ok and _flujo.Final(_sala) == "A", "llega al Final A sin careo")
	await create_timer(0.5).timeout

## HU-29 con los candados: la ficha de cada sospechoso trae sus candados y Tony no pide «escuchar a los demás»
func _fichas_sospechosos() -> void:
	print("== Fichas de sospechoso con candados")
	await _nueva()
	var fichas: Array = _flujo.Fichas(_sala)
	var tony: Dictionary = fichas.filter(func(f): return f.id == "tony")[0]
	_revisar(fichas.size() == 6 and tony.candados[1] == 5, "seis fichas; Tony con 5 candados")
	_revisar(tony.coartada.is_empty(), "la coartada no aparece antes de la declaración")
	_revisar(not str(tony.consejo).contains("ESCUCHA A LOS DEM"), "Tony no pide escuchar a los demás: " + str(tony.consejo))
	_hablar_con_hecdog()
	var declaro := await _jugar(["Interrogar a Tony"],
		func(): return _flujo.Declaro(_sala, "tony") and _dialogo.PruebasOpciones().contains("Confrontar"), 60.0)
	_revisar(declaro, "Tony declara antes de elegir con qué confrontarlo")
	tony = _flujo.Fichas(_sala).filter(func(f): return f.id == "tony")[0]
	_revisar(tony.contradicciones.size() == 5, "tras hablar con él, sus cinco excusas están en la ficha")
	_revisar(str(tony.consejo) == "TIENES CON QUÉ ROMPER UN CANDADO", "con las pistas, la ficha avisa que tienes con qué: " + str(tony.consejo))

	_revisar(tony.coartada.get("zona") == "Campamento · Caseta del generador" and tony.coartada.get("estado") == "pendiente",
		"la declaración registra la zona y deja la coartada pendiente")
	var ok := await _jugar(["Confrontar", "Foto de Aya de las 23:12"],
		func(): return _flujo.TieneConclusion(_sala, "tony_1"))
	var despues: Dictionary = _flujo.Fichas(_sala).filter(func(f): return f.id == "tony")[0]
	_revisar(ok and despues.coartada.get("estado") == "refutada" and despues.estado != tony.estado,
		"al confrontar a Tony cambia la coartada y su estado de sospecha")

func _ultimo_candado() -> void:
	print("== Con la noche completa, el último candado de Tony lleva al Final A")
	await _nueva()
	_flujo.ReconstruccionCompletaYPrimeraExcusa(_sala)
	for id in ["tony_2", "tony_3", "tony_4"]: _flujo.DarConclusion(_sala, id)
	_hablar_con_hecdog()
	var ok := await _jugar(["Interrogar a Tony", "hueco_remo"], func(): return _flujo.Resuelto(_sala) and root.get_node_or_null("CinematicaCaso1") == null, 200.0)
	_revisar(ok and _flujo.Final(_sala) == "A", "romper el último candado en el interrogatorio llega al Final A")
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
	# En la Reconstrucción
	_flujo.DarTodo(_sala)
	_hablar_con_hecdog()
	ok = await _jugar(["Reconstruir la noche", func(p): p.Pruebas_Poner("0.0.0", "paco")], func(): return _sala.Panel.EsperandoJugador and _flujo.Hueco(_sala, "0.0.0") == "paco")
	_revisar(ok, "llega a la Reconstrucción")
	await _guardar_cargar()
	_revisar(_flujo.Hueco(_sala, "0.0.0") == "paco", "al cargar la Reconstrucción sigue como estaba")
	# En el careo: se conserva lo tumbado
	_flujo.DarTodo(_sala)
	_flujo.ReconstruccionCompletaYPrimeraExcusa(_sala)
	_hablar_con_hecdog()
	ok = await _jugar(["Reconstruir la noche"], func(): return _dialogo.PruebasHayOpciones and _flujo.EnEspera(_sala) and _dialogo.PruebasOpciones().contains("Confrontar"), 80.0)
	_revisar(ok, "llega al careo")
	await _guardar_cargar()
	_revisar(_flujo.TieneConclusion(_sala, "tony_1") and not _flujo.Resuelto(_sala), "al cargar conserva la excusa tumbada y no hay final")

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
