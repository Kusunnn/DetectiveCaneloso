extends SceneTree
## HU-9.5 CA-03: cuenta cuántas veces sale cada cinemática y cada escena automática del Caso 1, y revisa los NPC.
##  1. Empezar el Caso 1: IntroCaso sale 1 vez, Hecdog recibe 1 vez y el expediente no se abre otra vez.
##  2. «Volver a ver» la presentación dos veces seguidas: solo una a la vez y no cuenta como cinemática nueva.
##  3. Guardar y cargar en el Campamento: no vuelve a salir nada y el registro de vistas viaja en la partida.
##  4. Campamento ↔ Lago tres veces e inspeccionar dos veces la misma pista: nada se repite.
##  5. La estación pide pruebas del Lago antes (Hecdog no presenta a los seis sin ellas).
##  6. Sala: la presentación sale 1 vez; al volver, nada. Los implicados caminan (no se deslizan quietos).
##  7. NPC: Porky no está en el Campamento, nadie en pose T, no se mueven al mismo cuadro, se voltean al hablar.
##  0. Caso 0 con la cinta: se dispara 1 vez, se adelanta, se guarda y al cargar no vuelve a salir; al repetir se salta.
##  4b. Todas las pistas del Campamento y del Lago, dos veces: nada se dispara dos veces.
##  6b. Hablar dos veces con cada implicado: ninguna línea se repite igual. Mimita camina con su animación.
##  7b. Acercarse y alejarse de cada NPC y hablar dos veces: no se dispara nada solo.
##  8. Acusación y final: una sola vez. Al volver a la sala no se repite.
##  9. «Volver a ver» la cinta desde el tablero: una a la vez, sin efectos en la partida; regresa a la misma pantalla.
## Para revisar una parte: «-- solo-caso0», «-- solo-sala», «-- solo-ver», «-- solo-final-a» o «-- solo-final-b» después del script.
## Al final imprime el conteo de cada cinemática, escena, pensamiento y reacción.
## Uso: --headless --path game --script res://Herramientas/Caso1/probar_ca03.gd
## Respalda en disco (user://respaldo_pruebas/) las ranuras y el progreso, y los devuelve al final.

const PARTIDAS := "user://partidas/"
const ARCHIVOS := ["user://progreso.json", "user://progreso.json.bak", "user://cinematicas.cfg"]
const RESPALDO := "user://respaldo_pruebas/"
const CAMPAMENTO := "res://Escenas/Caso1/escenario_campamento.tscn"
const LAGO := "res://Escenas/Caso1/Lago/escenario_lago.tscn"
const SALA := "res://Escenas/Caso1/Interrogatorio/sala_interrogatorio_caso1.tscn"
const CASO0 := "res://Escenas/Interrogatorio/sala_interrogatorio.tscn"
const MENU := "res://Escenas/Menus/menu_inicio.tscn"
const CINTA := "caso0_cinta"
const RESPUESTAS_CASO0 := ["Porky, el agente de guardia", "grabado dormido", "trampa"]
const RANURA_AUTO := 0
const YA_EXAMINADA := "Ya lo examiné"
const MUY_OSCURO := "Está demasiado oscuro"
## La pregunta del menú de pruebas: sale cada vez que se abre, es la que da pie a elegir
const MENU_EVIDENCIAS := "¿Qué le enseño?"
const IMPLICADOS := ["JuanCarlos", "Aya", "Miawrio", "Eriz", "Mimita", "Tony"]
const NPC_CAMPAMENTO := ["Eriz", "JuanCarlos", "Miami", "Tony", "Aya", "Miaurio"]

var _fallos := 0
var _gestor
## Cuántas veces salió sola cada cinemática (señal CinematicaReproducida) y cada escena automática al llegar
var _cinematicas := {}
var _escenas := {}
## Cuántas veces salió cada pensamiento, línea de diálogo y reacción
var _eventos := {"pensamiento": {}, "linea": {}, "reaccion": {}}
var _flujo
## Lo que dicen los objetos que no son evidencia al mirarlos
var _descripciones := {}

func _initialize() -> void:
	if DirAccess.dir_exists_absolute(RESPALDO):
		_devolver_respaldo()
	_respaldar()
	await process_frame
	_gestor = root.get_node("GestorPartida")
	_gestor.CinematicaReproducida.connect(func(id): _cinematicas[id] = _cinematicas.get(id, 0) + 1)
	_gestor.EventoMostrado.connect(func(tipo, texto): _eventos[tipo][texto] = _eventos[tipo].get(texto, 0) + 1)
	_flujo = load("res://tests/FlujoCaso1Prueba.cs").new()
	root.add_child(_flujo)

	if not (OS.get_cmdline_user_args().has("solo-sala") or OS.get_cmdline_user_args().has("solo-final-a") or OS.get_cmdline_user_args().has("solo-final-b")):
		await _probar_caso0()
	if OS.get_cmdline_user_args().has("solo-caso0"):
		_devolver_respaldo()
		quit(0)
		return
	_cinematicas.clear()
	for tipo in _eventos: _eventos[tipo].clear()

	print("1) Empezar el Caso 1")
	_gestor.NuevaPartida(true)
	await _esperar_escena(CAMPAMENTO)
	await create_timer(3.0).timeout
	_revisar(_intros() == 1, "IntroCaso se reproduce al empezar")
	await _saltar_intro()
	await create_timer(1.5).timeout
	await _contar_escena("campamento")
	await _cerrar_dialogos()
	await create_timer(0.5).timeout
	_revisar(not _expediente().abierto, "después de la cinemática, Hecdog no vuelve a abrir el expediente")
	_revisar(_gestor.CinematicaVista("intro_caso01"), "la presentación queda en el registro de la partida")

	for final in ["solo-final-a", "solo-final-b"]:
		if OS.get_cmdline_user_args().has(final):
			await _probar_un_final(final == "solo-final-a")
			print("Conteo: %s" % [_cinematicas])
			print("Fallos: %d" % _fallos)
			_devolver_respaldo()
			quit(1 if _fallos > 0 else 0)
			return

	if OS.get_cmdline_user_args().has("solo-ver"):
		await _probar_ver_cinta()
		print("Fallos: %d" % _fallos)
		_devolver_respaldo()
		quit(1 if _fallos > 0 else 0)
		return

	if OS.get_cmdline_user_args().has("solo-sala"):
		_gestor.CambiarEscenaDelCapitulo(SALA, "EntradaDesdeLago")
		await _esperar_escena(SALA)
		await create_timer(1.5).timeout
		await _cerrar_dialogos(200)
		await _hablar_con_implicados()
		_devolver_respaldo()
		quit(0)
		return

	print("2) Volver a ver la presentación desde el expediente")
	var objetivo_antes = current_scene.get_node("HUD").get("objetivo")
	var notas_antes: int = current_scene.get_node("HUD").get("notas").size()
	_expediente().ver_presentacion()
	_expediente().ver_presentacion()
	await process_frame
	_revisar(_intros() == 1, "dos clics seguidos ponen una sola cinemática")
	await _saltar_intro()
	_expediente().ver_presentacion()
	await process_frame
	_revisar(_intros() <= 1, "en el fundido final no se apila otra")
	await _saltar_intro()
	await create_timer(1.5).timeout
	_revisar(_cinematicas.get("intro_caso01", 0) == 1, "volver a verla no cuenta como cinemática nueva")
	_revisar(current_scene.get_node("HUD").get("objetivo") == objetivo_antes and current_scene.get_node("HUD").get("notas").size() == notas_antes,
		"volver a verla no cambia el objetivo ni las notas")

	print("3) Guardar y cargar en el Campamento")
	var caso = get_nodes_in_group("estado_partida")[0]
	for i in 20:
		if caso.PuedeGuardar: break
		await _cerrar_dialogos()
		await create_timer(0.3).timeout
	if not caso.PuedeGuardar:
		print("    (no puede guardar: diálogo %s, en diálogo %s, tablero %s)" % [current_scene.get_node("VentanaDialogo").Abierto, caso.get("_enDialogo"), caso.Tablero.Abierto])
	_revisar(_gestor.Guardar(3, false, ""), "guarda la ranura 3")
	_gestor.CargarRanura(3, false)
	await _esperar_escena(CAMPAMENTO, true)
	await create_timer(1.5).timeout
	_revisar(_intros() == 0, "al cargar no vuelve a salir IntroCaso")
	await _contar_escena("campamento")
	await _cerrar_dialogos()
	_revisar(_gestor.CinematicaVista("intro_caso01"), "el registro de cinemáticas vistas viaja en la partida")

	print("4) Campamento ↔ Lago tres veces y la misma pista dos veces")
	for vuelta in 3:
		_gestor.CambiarEscenaDelCapitulo(LAGO, "EntradaDesdeCampamento")
		await _esperar_escena(LAGO)
		await create_timer(1.0).timeout
		await _contar_escena("lago")
		await _cerrar_dialogos()
		_gestor.CambiarEscenaDelCapitulo(CAMPAMENTO, "LlegadaDesdeLago")
		await _esperar_escena(CAMPAMENTO)
		await create_timer(1.0).timeout
		await _contar_escena("campamento")
		await _cerrar_dialogos()
	var interruptor = _buscar_pista("interruptor_generador")
	for vez in 2:
		interruptor.Interactuar(_jugador())
		await create_timer(0.6).timeout
		await _cerrar_dialogos()
	_revisar(_intros() == 0, "inspeccionar dos veces una pista no dispara cinemáticas")

	print("4b) Todas las pistas del Campamento, dos veces")
	await _recoger_pistas()
	_revisar(_cinematicas.get("caso1_libro", 0) == 1 and _cinematicas.get("caso1_foto", 0) == 1, "el libro quemado y la foto salen una vez")
	var conteo_antes: Dictionary = _cinematicas.duplicate()
	for i in 20:
		if get_nodes_in_group("estado_partida")[0].PuedeGuardar: break
		await create_timer(0.3).timeout
	_gestor.Guardar(3, false, "")
	_gestor.CargarRanura(3, false)
	await _esperar_escena(CAMPAMENTO, true)
	await create_timer(2.0).timeout
	await _cerrar_dialogos()
	_revisar(_cinematicas == conteo_antes and _gestor.CinematicaVista("caso1_libro"), "al guardar y cargar no vuelven a salir")
	await _recoger_pistas()
	_revisar(_cinematicas == conteo_antes, "volver a mirar el libro y la foto después de cargar no las repite")

	print("5) La estación pide pruebas del Lago")
	_gestor.CambiarEscenaDelCapitulo(LAGO, "EntradaDesdeCampamento")
	await _esperar_escena(LAGO)
	await create_timer(1.0).timeout
	await _cerrar_dialogos()
	current_scene.get_node("Caso1Lago").IrEstacion.Interactuar(_jugador())
	await create_timer(1.0).timeout
	_revisar(current_scene.get_node("VentanaDialogo").Abierto, "sin pruebas del Lago, Caneloso dice que todavía no")
	await _cerrar_dialogos()
	await create_timer(1.0).timeout
	_revisar(current_scene.scene_file_path == LAGO, "y no se va a la estación")
	await _recoger_pistas()

	print("6) Sala de interrogatorio")
	for vuelta in 2:
		_gestor.CambiarEscenaDelCapitulo(SALA, "EntradaDesdeLago")
		await _esperar_escena(SALA)
		await create_timer(1.5).timeout
		await _contar_escena("sala")
		await _cerrar_dialogos(200)
		if vuelta == 0:
			await _probar_caminar()
			await _hablar_con_implicados()
		_gestor.CambiarEscenaDelCapitulo(LAGO, "EntradaDesdeSala")
		await _esperar_escena(LAGO)
		await create_timer(1.0).timeout
		await _cerrar_dialogos()

	print("7) NPC del Campamento")
	_gestor.CambiarEscenaDelCapitulo(CAMPAMENTO, "LlegadaDesdeLago")
	await _esperar_escena(CAMPAMENTO)
	await create_timer(1.5).timeout
	await _cerrar_dialogos()
	var personajes: Node3D = current_scene.get_node("Personajes")
	_revisar(personajes.get_node_or_null("Porky") == null, "Porky (la víctima) no está en el Campamento")
	for nombre in ["Eriz", "JuanCarlos", "Miami"]:
		_revisar(not _en_pose_t(personajes.get_node(nombre)), "%s no está en pose T" % nombre)
	_revisar(personajes.get_node("Miami/Animacion").Variantes.size() > 0, "Mimita intercala otras animaciones de reposo")
	var tiempos := []
	var velocidades := []
	for nombre in ["Tony", "Aya", "Miaurio"]:
		var reproductor: AnimationPlayer = null
		for p in personajes.get_node(nombre + "/Modelo").find_children("*", "AnimationPlayer", true, false):
			if p.is_playing(): reproductor = p
		tiempos.append(snappedf(reproductor.current_animation_position, 0.01))
		velocidades.append(snappedf(reproductor.speed_scale, 0.001))
		_revisar(personajes.get_node(nombre + "/Animacion").Variantes.size() > 0, "%s intercala otra animación de reposo" % nombre)
	_revisar(_distintos(tiempos) and not (velocidades[0] == velocidades[1] and velocidades[1] == velocidades[2]),
		"Tony, Aya y Miawrio no van al mismo cuadro ni a la misma velocidad %s %s" % [tiempos, velocidades])
	var tony: Node3D = personajes.get_node("Tony")
	var giro_antes := tony.global_rotation.y
	tony.get_node("Colision").MirarA(tony.global_position + tony.global_basis.x * 3.0)
	await create_timer(0.6).timeout
	_revisar(absf(angle_difference(giro_antes, tony.global_rotation.y)) > 0.5,
		"al hablar, Tony se voltea completo hacia el detective (%.2f → %.2f, pausa %s)" % [giro_antes, tony.global_rotation.y, paused])

	await _acercarse_a_npc(personajes)

	print("8) Acusación y final")
	await _probar_final()

	_revisar(_cinematicas.values().all(func(v): return v == 1), "cada cinemática salió una sola vez: %s" % [_cinematicas])
	for id in ["intro_caso01", "caso1_libro", "caso1_foto", "caso1_llegada", "caso1_silbato"]:
		_revisar(_cinematicas.get(id, 0) == 1, "%s salió una vez" % id)
	_revisar(_cinematicas.has("caso1_final_a") or _cinematicas.has("caso1_final_b"), "salió uno de los dos finales")
	print("\nCinemáticas que salieron solas: %s" % _cinematicas)
	print("Escenas automáticas al llegar: %s" % _escenas)
	_imprimir_conteo()
	_revisar(_escenas.get("campamento", 0) == 1 and _escenas.get("sala", 0) == 1 and _escenas.get("lago", 0) <= 1,
		"Hecdog recibe una vez en el Campamento y otra en la sala, la llegada al Lago es una cinemática de una sola vez; nada se repite al volver")
	var repetidos := _repetidos("pensamiento")
	_revisar(repetidos.is_empty(), "ningún pensamiento sale dos veces %s" % [repetidos])
	repetidos = _repetidos("reaccion")
	_revisar(repetidos.is_empty(), "ninguna reacción sale dos veces %s" % [repetidos])

	print("9) Volver a ver la cinta desde el tablero")
	await _probar_ver_cinta()
	print("Fallos: %d" % _fallos)
	_devolver_respaldo()
	quit(1 if _fallos > 0 else 0)

## Caso 0 de la resolución al cierre con la cinta real, guardar, cargar, y repetir saltándola
func _probar_caso0() -> void:
	print("0) Caso 0 y la cinta")
	_gestor.IniciarCapitulo("caso0", false, true)
	await _esperar_escena(CASO0)
	await _presentacion_caso0()
	await _hasta_la_cinta()
	_revisar(_flujo_real.CinematicaEnCurso, "la cinta se dispara al resolver el caso")
	_revisar(_cinematicas.get(CINTA, 0) == 1, "una sola vez")
	# Adelantar por cuadro con clics, hasta el final
	Engine.time_scale = 4.0
	for i in 40:
		if not _flujo_real.CinematicaEnCurso: break
		await _clic()
		await create_timer(0.4).timeout
	Engine.time_scale = 1.0
	_revisar(not _flujo_real.CinematicaEnCurso, "adelantar con clic la lleva hasta el final")
	await _cerrar_caso0()
	_revisar(_gestor.CinematicaVistaEnPartidaGuardada(CINTA), "la partida guardada al cerrar recuerda que ya se vio")
	_gestor.CargarRanura(RANURA_AUTO, false)
	await _esperar_escena(CASO0, true)
	await create_timer(4.0).timeout
	await _cerrar_dialogos()
	_revisar(_cinematicas.get(CINTA, 0) == 1 and not _flujo_real.CinematicaEnCurso, "al cargar después del cierre no vuelve a salir")

	# Repetir el capítulo: esa partida es aparte y la cinta se puede saltar
	_gestor.IniciarCapitulo("caso0", true, true)
	await _esperar_escena(CASO0, true)
	await _presentacion_caso0()
	await _hasta_la_cinta()
	var e := InputEventKey.new()
	e.keycode = KEY_SPACE
	e.physical_keycode = KEY_SPACE
	e.pressed = true
	Input.parse_input_event(e)
	await create_timer(3.5).timeout
	var s := e.duplicate()
	s.pressed = false
	Input.parse_input_event(s)
	_revisar(not _flujo_real.CinematicaEnCurso, "mantener Espacio salta la cinta")
	await _cerrar_caso0()

var _flujo_real

## Espera a que pase la tarjeta del título y el diálogo del inicio del caso
func _presentacion_caso0() -> void:
	var dialogo = current_scene.get_node("VentanaDialogo")
	for i in 60:
		await _saltar_intro()
		if dialogo.Abierto: break
		await create_timer(0.5).timeout
	await _cerrar_dialogos(100)
	await create_timer(0.5).timeout

## Lleva el caso a la resolución y responde hasta que empieza la cinta
func _hasta_la_cinta() -> void:
	_flujo_real = load("res://tests/FlujoRealCaso0Prueba.cs").new()
	root.add_child(_flujo_real)
	_flujo_real.Resolver(current_scene.get_node("Caso0"))
	var dialogo = current_scene.get_node("VentanaDialogo")
	for i in 300:
		if _flujo_real.CinematicaEnCurso: return
		if dialogo.Abierto:
			var elegida := false
			for b in dialogo.find_children("*", "Button", true, false):
				if not b.is_visible_in_tree(): continue
				for r in RESPUESTAS_CASO0:
					if r in b.text:
						b.emit_signal("pressed")
						elegida = true
						break
				if elegida: break
			if not elegida: await _clic()
		await create_timer(0.25).timeout

## Después de la cinta: el pensamiento, el objetivo y el guardado del cierre
func _cerrar_caso0() -> void:
	await create_timer(1.5).timeout
	var dialogo = current_scene.get_node("VentanaDialogo")
	for i in 60:
		if _flujo_real.Terminado: break
		if dialogo.Abierto: await _clic()
		await create_timer(0.3).timeout
	await create_timer(1.0).timeout

func _clic() -> void:
	var clic := InputEventMouseButton.new()
	clic.button_index = MOUSE_BUTTON_LEFT
	clic.pressed = true
	clic.position = Vector2(400, 400)
	Input.parse_input_event(clic)
	await process_frame
	var suelta := clic.duplicate()
	suelta.pressed = false
	Input.parse_input_event(suelta)

## Recoge todas las pistas del escenario actual, dos veces: la segunda vez no se dispara nada nuevo
func _recoger_pistas() -> void:
	var pistas := get_nodes_in_group("objetos_pista").filter(func(n): return current_scene.is_ancestor_of(n))
	# Con la linterna prendida, como las buscaría el jugador (algunas no se ven sin ella)
	var hud = current_scene.get_node("HUD")
	if not hud.linterna_encendida:
		hud.linterna_encendida = true
		hud.linterna_cambiada.emit(true)
	for pista in pistas:
		if not pista.EsEvidencia: _descripciones[pista.Descripcion] = true
	for vez in 2:
		var antes: Dictionary = _eventos["pensamiento"].duplicate()
		for pista in pistas:
			if not is_instance_valid(pista) or not pista.is_inside_tree(): continue
			pista.Interactuar(_jugador())
			await create_timer(0.35).timeout
			await _cerrar_dialogos()
		if vez == 1:
			# «Ya lo examiné: ...» es la respuesta a volver a mirarla; lo que no debe salir es otra vez su comentario
			var nuevos := 0
			for texto in _eventos["pensamiento"]:
				if _eventos["pensamiento"][texto] > antes.get(texto, 0) and not _responde_a_mirar(texto): nuevos += 1
			_revisar(nuevos == 0, "en %s, la segunda vuelta por las %d pistas no dispara pensamientos (%d)" % [current_scene.name, pistas.size(), nuevos])
	_revisar(_intros() == 0, "las pistas no vuelven a disparar la presentación")
	_revisar(root.get_node_or_null("CinematicaCaso1") == null, "no queda ninguna cinemática encima")

## Dos conversaciones con cada implicado: ninguna línea de la primera se repite en la segunda
func _hablar_con_implicados() -> void:
	var caso = current_scene.get_node("Caso1Sala")
	var repetidas := {}
	var sentados := {}
	caso.ImplicadoSentado.connect(func(quien): sentados[quien] = true)
	for clave in IMPLICADOS:
		var implicado: Node3D = current_scene.get_node("Implicados/" + clave)
		var punto = implicado.get_node("Colision")
		var primera := {}
		for vez in 2:
			var antes: Dictionary = _eventos["linea"].duplicate()
			await _hablar(punto, 16.0)
			for linea in _eventos["linea"]:
				if _eventos["linea"][linea] > antes.get(linea, 0):
					if vez == 0: primera[linea] = true
					elif primera.has(linea) and not linea.begins_with("CANELOSO") and not linea.ends_with(MENU_EVIDENCIAS): repetidas[linea] = true
		_revisar(sentados.has(clave), "%s se sienta en la silla al hablar con él" % clave)
		_revisar(not _en_pose_t(implicado), "%s no está en pose T en la sala" % clave)
		await _retirar(caso)
	print("    líneas repetidas al hablar dos veces: %s" % [repetidas.keys()])
	_revisar(repetidas.is_empty(), "hablar dos veces con cada implicado no repite ninguna línea suya")

## Habla con alguien y espera a que empiece su primera línea (puede venir caminando) y a que termine la conversación
func _hablar(punto, espera: float) -> void:
	await _cerrar_dialogos(200)
	var lineas := _total("linea")
	punto.Interactuar(_jugador())
	var limite := Time.get_ticks_msec() + espera * 1000
	while _total("linea") == lineas and Time.get_ticks_msec() < limite:
		await create_timer(0.2).timeout
	await _cerrar_dialogos(200)
	await create_timer(0.6).timeout
	await _cerrar_dialogos(200)

func _retirar(caso) -> void:
	if caso.ImplicadoActual != "":
		caso.Retirar()
	for i in 60:
		if caso.ImplicadoActual == "": return
		await create_timer(0.25).timeout

## Ir y venir cerca de cada NPC no dispara nada; hablar dos veces no repite sus líneas
func _acercarse_a_npc(personajes: Node3D) -> void:
	print("7b) Acercarse, alejarse y hablar con cada NPC")
	var jugador = _jugador()
	var repetidas := {}
	for nombre in NPC_CAMPAMENTO:
		var npc: Node3D = personajes.get_node(nombre)
		# Un pensamiento de zona puede salir aquí la primera vez que se pisa: lo revisa el conteo final (uno solo)
		var antes_reaccion: int = _total("reaccion")
		for vez in 3:
			jugador.global_position = npc.global_position + Vector3(1.5, 0, 0)
			await create_timer(0.3).timeout
			jugador.global_position = npc.global_position + Vector3(12, 0, 0)
			await create_timer(0.3).timeout
		_revisar(_total("reaccion") == antes_reaccion and not current_scene.get_node("VentanaDialogo").Abierto,
			"acercarse tres veces a %s no dispara nada" % nombre)
		var punto = npc.get_node("Colision")
		var primera := {}
		for vez in 2:
			var antes: Dictionary = _eventos["linea"].duplicate()
			await _hablar(punto, 3.0)
			for linea in _eventos["linea"]:
				if _eventos["linea"][linea] > antes.get(linea, 0):
					if vez == 0: primera[linea] = true
					elif primera.has(linea) and not linea.begins_with("CANELOSO") and not linea.ends_with(MENU_EVIDENCIAS): repetidas[linea] = true
	print("    líneas repetidas en el Campamento: %s" % [repetidas.keys()])
	_revisar(repetidas.is_empty(), "hablar dos veces con cada NPC del Campamento no repite sus líneas")

## El final sale una vez y al volver a la sala ya no vuelve nada
func _probar_final() -> void:
	_gestor.CambiarEscenaDelCapitulo(SALA, "EntradaDesdeLago")
	await _esperar_escena(SALA)
	await create_timer(1.0).timeout
	await _cerrar_dialogos()
	var sala = current_scene.get_node("Caso1Sala")
	_flujo.Acusar(sala, false)
	await create_timer(0.5).timeout
	# Sin ventana, el autoguardado del final falla al tomar la miniatura: por eso se mira el guion y no el final de la tarea
	for i in 120:
		if _flujo.Resuelto(sala) and root.get_node_or_null("CinematicaCaso1") == null and i > 3: break
		await _cerrar_dialogos(5)
		await create_timer(0.3).timeout
	_revisar(_flujo.Resuelto(sala), "la acusación llega a un final")
	var lineas := _total("linea")
	_gestor.CambiarEscenaDelCapitulo(LAGO, "EntradaDesdeSala")
	await _esperar_escena(LAGO)
	await create_timer(1.0).timeout
	await _cerrar_dialogos()
	_gestor.CambiarEscenaDelCapitulo(SALA, "EntradaDesdeLago")
	await _esperar_escena(SALA)
	await create_timer(1.5).timeout
	_revisar(_total("linea") == lineas and not current_scene.get_node("VentanaDialogo").Abierto, "con el caso resuelto, volver a la sala no repite nada")

## Acusar a Tony (reconstrucción y final A) o a Juan Carlos (final B), sin jugar todo el capítulo
func _probar_un_final(a_tony: bool) -> void:
	_gestor.CambiarEscenaDelCapitulo(SALA, "EntradaDesdeLago")
	await _esperar_escena(SALA)
	await create_timer(1.0).timeout
	await _cerrar_dialogos(200)
	var sala = current_scene.get_node("Caso1Sala")
	_flujo.Acusar(sala, a_tony)
	var dialogo = current_scene.get_node("VentanaDialogo")
	for i in 300:
		if _flujo.AcusacionTerminada: break
		await _pasar_cinematica()
		if _flujo.AcusacionTerminada: break
		var opciones: Array = dialogo.find_children("*", "Button", true, false).filter(func(b): return b.is_visible_in_tree() and not b.disabled)
		if opciones.size() > 0:
			var tecla := InputEventKey.new()
			tecla.keycode = KEY_1 + (opciones.size() - 1 if a_tony else 0)
			tecla.pressed = true
			Input.parse_input_event(tecla)
			await process_frame
			var suelta := tecla.duplicate()
			suelta.pressed = false
			Input.parse_input_event(suelta)
		else:
			await _clic()
		await create_timer(0.35).timeout
	_revisar(_flujo.Resuelto(sala), "la acusación llega a su final")
	if a_tony:
		_revisar(_cinematicas.get("caso1_2320", 0) == 1, "la reconstrucción de las 23:20 sale una vez")
		_revisar(_cinematicas.get("caso1_final_a", 0) == 1, "el final A sale una vez")
	else:
		_revisar(not _cinematicas.has("caso1_2320"), "con acusación equivocada no se ve la reconstrucción")
		_revisar(_cinematicas.get("caso1_final_b", 0) == 1, "el final B sale una vez")
	_revisar(root.get_node_or_null("CinematicaCaso1") == null, "no queda ninguna cinemática encima")
	_gestor.CambiarEscenaDelCapitulo(LAGO, "EntradaDesdeSala")
	await _esperar_escena(LAGO)
	await create_timer(1.0).timeout
	await _cerrar_dialogos()
	_gestor.CambiarEscenaDelCapitulo(SALA, "EntradaDesdeLago")
	await _esperar_escena(SALA)
	await create_timer(1.5).timeout
	var total_final: int = _cinematicas.get("caso1_final_a", 0) + _cinematicas.get("caso1_final_b", 0)
	_revisar(total_final == 1, "al volver a la sala el final no se repite")

func _probar_ver_cinta() -> void:
	var caso = get_nodes_in_group("estado_partida")[0]
	for i in 20:
		if caso.PuedeGuardar: break
		await create_timer(0.3).timeout
	await create_timer(1.1).timeout
	_revisar(_gestor.Guardar(3, false, ""), "guarda la ranura 3")
	_revisar(not _gestor.CinematicaVistaEnPartidaGuardada(CINTA), "sin haberla visto en esta partida, no se ofrece")
	await create_timer(1.1).timeout
	_gestor.RegistrarCinematica(CINTA)
	_cinematicas.erase(CINTA)
	_revisar(_gestor.Guardar(3, false, ""), "guarda la ranura 3 con la cinta vista")
	var ruta := PARTIDAS + "ranura_3.json"
	var bytes := FileAccess.get_file_as_bytes(ruta)
	_gestor.IrAlMenuInicio()
	await _esperar_escena(MENU)
	var menu = current_scene
	var tablero
	for hijo in menu.get_children():
		if hijo.get_script() and str(hijo.get_script().resource_path).ends_with("TableroCapitulos.cs"): tablero = hijo
	tablero.Abrir(null)
	await process_frame
	_flujo.AbrirDetalle(tablero, "caso0")
	await process_frame
	var boton: Button = null
	for b in tablero.find_children("*", "Button", true, false):
		if b.text.contains("Volver a ver la cinta") and b.is_visible_in_tree(): boton = b
	_revisar(boton != null, "el tablero ofrece «Volver a ver la cinta» en el Caso 0")
	if boton == null: return
	boton.grab_focus()
	var eventos_antes: int = _total("pensamiento") + _total("reaccion")
	boton.emit_signal("pressed")
	boton.emit_signal("pressed")
	await process_frame
	await process_frame
	_revisar(_ver_cinta() == 1, "dos clics seguidos ponen una sola")
	_revisar(menu.process_mode == Node.PROCESS_MODE_DISABLED, "el menú queda quieto debajo")
	await create_timer(1.0).timeout
	_revisar(get_nodes_in_group("estado_partida").is_empty(), "el Caso 0 no arranca: no hay partida que guardar")
	Engine.time_scale = 4.0
	var ver = _instancia_ver()
	var fin := [false]
	if ver != null:
		ver.Terminada.connect(func(): fin[0] = true)
	var apiladas := 0
	for i in 40:
		if fin[0]: break
		await _clic()
		# Hasta en el fundido final, otro clic en el botón no pone otra encima
		boton.emit_signal("pressed")
		await process_frame
		if _ver_cinta() > 1: apiladas += 1
		await create_timer(0.4).timeout
	Engine.time_scale = 1.0
	_revisar(apiladas == 0, "otro clic en el botón no pone otra encima, ni en el fundido final")
	_revisar(fin[0], "termina al adelantarla con clic")
	await create_timer(1.0).timeout
	_revisar(_ver_cinta() == 0, "al salir no queda nada encima")
	_revisar(menu.process_mode != Node.PROCESS_MODE_DISABLED and current_scene == menu and tablero.visible and boton.is_visible_in_tree(), "regresa al mismo detalle del tablero")
	_revisar(FileAccess.get_file_as_bytes(ruta) == bytes, "la partida guardada no cambia")
	_revisar(not _cinematicas.has(CINTA), "no cuenta como cinemática nueva")
	_revisar(_total("pensamiento") + _total("reaccion") == eventos_antes, "no salen pensamientos ni reacciones")

func _ver_cinta() -> int:
	var n := 0
	for hijo in root.get_children():
		if hijo.get_script() and str(hijo.get_script().resource_path).ends_with("VerCinta.cs") and not hijo.is_queued_for_deletion():
			n += 1
	return n

func _instancia_ver():
	for hijo in root.get_children():
		if hijo.get_script() and str(hijo.get_script().resource_path).ends_with("VerCinta.cs"): return hijo
	return null

func _total(tipo: String) -> int:
	var n := 0
	for v in _eventos[tipo].values(): n += v
	return n

func _repetidos(tipo: String) -> Array:
	return _eventos[tipo].keys().filter(func(k): return _eventos[tipo][k] > 1 and not _responde_a_mirar(k))

## Lo que Caneloso dice porque el jugador volvió a mirar algo: tiene motivo cada vez
func _responde_a_mirar(texto: String) -> bool:
	return texto.begins_with(YA_EXAMINADA) or texto.begins_with(MUY_OSCURO) or _descripciones.has(texto)

func _imprimir_conteo() -> void:
	for tipo in ["pensamiento", "reaccion"]:
		print("\n%s: %d distintos, %d en total" % [tipo, _eventos[tipo].size(), _total(tipo)])
		for texto in _eventos[tipo]:
			print("  %dx %s" % [_eventos[tipo][texto], texto])
	print("\nlíneas de diálogo: %d distintas, %d en total; más de una vez:" % [_eventos["linea"].size(), _total("linea")])
	for texto in _repetidos("linea"):
		print("  %dx %s" % [_eventos["linea"][texto], texto])

## Un implicado sin animación de caminar entra a la sala: mientras camina mueve las piernas
func _probar_caminar() -> void:
	var caso = current_scene.get_node("Caso1Sala")
	var eriz: Node3D = current_scene.get_node("Implicados/Eriz")
	var anim = eriz.get_node("Animacion")
	await _retirar(caso)
	caso.Traer("Eriz")
	await create_timer(0.4).timeout
	var esqueleto: Skeleton3D = _esqueleto(eriz)
	var pierna := esqueleto.find_bone("Leg_1_L")
	var antes := esqueleto.get_bone_pose_rotation(pierna)
	await create_timer(0.25).timeout
	var despues := esqueleto.get_bone_pose_rotation(pierna)
	_revisar(anim.Caminando and antes.angle_to(despues) > 0.02, "Eriz camina moviendo las piernas (no se desliza)")
	await caso.ImplicadoSentado
	_revisar(not anim.Caminando, "al llegar a la silla deja de caminar")
	caso.Retirar()
	await caso.ImplicadoRetirado
	# Mimita tiene su propia animación de caminar y vuelve a su reposo al llegar
	var mimita: Node3D = current_scene.get_node("Implicados/Mimita")
	var reproductor: AnimationPlayer = mimita.get_node("Modelo").find_children("*", "AnimationPlayer", true, false)[0]
	caso.Traer("Mimita")
	await create_timer(0.4).timeout
	_revisar(str(reproductor.current_animation).contains("Caminar"), "Mimita entra con su animación de caminar")
	_revisar(not _en_pose_t(mimita), "Mimita no está en pose T al caminar")
	await caso.ImplicadoSentado
	_revisar(reproductor.current_animation == "Reposo", "al llegar a la silla, Mimita vuelve a su reposo")
	_revisar(not _en_pose_t(mimita), "Mimita no está en pose T en la sala")
	caso.Retirar()
	await caso.ImplicadoRetirado

## Pose T: el codo a la altura del hombro. Vale con cualquier reposo, porque mide dónde quedó el brazo.
func _en_pose_t(personaje: Node3D) -> bool:
	var esqueleto := _esqueleto(personaje)
	if esqueleto == null:
		return true
	var hombro := esqueleto.find_bone("Arm_1_L")
	var codo := esqueleto.find_bone("Arm_2_L")
	if hombro < 0 or codo < 0:
		return false
	var dir := esqueleto.global_transform.basis * (esqueleto.get_bone_global_pose(codo).origin - esqueleto.get_bone_global_pose(hombro).origin)
	return dir.normalized().y > -0.5

func _esqueleto(personaje: Node3D) -> Skeleton3D:
	var mejor: Skeleton3D = null
	for s in personaje.get_node("Modelo").find_children("*", "Skeleton3D", true, false):
		if mejor == null or s.get_bone_count() > mejor.get_bone_count():
			mejor = s
	return mejor

func _distintos(valores: Array) -> bool:
	for i in valores.size():
		for k in range(i + 1, valores.size()):
			if is_equal_approx(valores[i], valores[k]):
				return false
	return true

## Si al llegar se abrió solo un diálogo (Hecdog recibe, presenta...), cuenta una escena automática
func _contar_escena(donde: String) -> void:
	await create_timer(0.5).timeout
	var dialogo = current_scene.get_node_or_null("VentanaDialogo")
	if dialogo != null and dialogo.Abierto:
		_escenas[donde] = _escenas.get(donde, 0) + 1

func _intros() -> int:
	var n := 0
	for hijo in root.get_children():
		if hijo.get_script() and str(hijo.get_script().resource_path).ends_with("IntroCaso.cs") and not hijo.is_queued_for_deletion():
			n += 1
	return n

func _expediente():
	return current_scene.get_node("HUD")._expediente

## Espera a que la escena nueva esté lista (recargar: la misma ruta que la actual, otra instancia)
func _esperar_escena(ruta: String, recargar := false) -> void:
	var anterior = current_scene
	for i in 400:
		await process_frame
		var lista: bool = current_scene != null and current_scene.scene_file_path == ruta and current_scene.is_node_ready()
		if lista and (not recargar or current_scene != anterior):
			break
	await create_timer(0.5).timeout

func _jugador():
	return current_scene.find_child("Jugador", true, false)

func _buscar_pista(id: String):
	for n in get_nodes_in_group("objetos_pista"):
		if n.IdPista == id and current_scene.is_ancestor_of(n): return n
	return null

func _saltar_intro() -> void:
	if _intros() == 0:
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
	await create_timer(0.2).timeout

## Si hay una cinemática del Caso 1 en curso, la adelanta con clics hasta que termina
func _pasar_cinematica() -> void:
	for i in 60:
		if root.get_node_or_null("CinematicaCaso1") == null:
			return
		await _clic()
		await create_timer(0.3).timeout

func _cerrar_dialogos(intentos := 30) -> void:
	await _pasar_cinematica()
	for i in intentos:
		await _pasar_cinematica()
		var dialogo = current_scene.get_node_or_null("VentanaDialogo")
		if dialogo == null or not dialogo.Abierto:
			return
		# Si pregunta algo, sale como el jugador: Esc elige la salida de la conversación (Seguir investigando...)
		var opciones := dialogo.find_children("*", "Button", true, false).filter(func(b): return b.is_visible_in_tree() and not b.disabled)
		if opciones.size() > 0:
			var cancelar := InputEventAction.new()
			cancelar.action = "ui_cancel"
			cancelar.pressed = true
			Input.parse_input_event(cancelar)
			await process_frame
			var soltar := cancelar.duplicate()
			soltar.pressed = false
			Input.parse_input_event(soltar)
			await create_timer(0.4).timeout
			# Sin salida (una pregunta que hay que contestar): la última opción, con su número
			if dialogo.Abierto and is_instance_valid(opciones[0]):
				var tecla := InputEventKey.new()
				tecla.keycode = KEY_1 + mini(opciones.size(), 9) - 1
				tecla.pressed = true
				Input.parse_input_event(tecla)
				await process_frame
				var suelta_tecla := tecla.duplicate()
				suelta_tecla.pressed = false
				Input.parse_input_event(suelta_tecla)
				await create_timer(0.4).timeout
			continue
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

func _revisar(ok: bool, que: String) -> void:
	print(("  ✓ " if ok else "  ✗ ") + que)
	if not ok: _fallos += 1

func _respaldar() -> void:
	DirAccess.make_dir_recursive_absolute(RESPALDO + "partidas")
	for archivo in DirAccess.get_files_at(PARTIDAS):
		DirAccess.copy_absolute(PARTIDAS + archivo, RESPALDO + "partidas/" + archivo)
	for archivo in ARCHIVOS:
		if FileAccess.file_exists(archivo):
			DirAccess.copy_absolute(archivo, RESPALDO + archivo.get_file())

## Deja las ranuras y el progreso como estaban antes de la prueba y borra el respaldo
func _devolver_respaldo() -> void:
	for archivo in DirAccess.get_files_at(PARTIDAS):
		DirAccess.remove_absolute(PARTIDAS + archivo)
	for archivo in ARCHIVOS:
		DirAccess.remove_absolute(archivo)
	for archivo in DirAccess.get_files_at(RESPALDO + "partidas"):
		DirAccess.copy_absolute(RESPALDO + "partidas/" + archivo, PARTIDAS + archivo)
		DirAccess.remove_absolute(RESPALDO + "partidas/" + archivo)
	for archivo in DirAccess.get_files_at(RESPALDO):
		DirAccess.copy_absolute(RESPALDO + archivo, "user://" + archivo)
		DirAccess.remove_absolute(RESPALDO + archivo)
	DirAccess.remove_absolute(RESPALDO + "partidas")
	DirAccess.remove_absolute(RESPALDO)
