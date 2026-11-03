extends SceneTree
## HU-9.5 CA-03: cuenta cuántas veces sale cada cinemática y cada escena automática del Caso 1, y revisa los NPC.
##  1. Empezar el Caso 1: IntroCaso sale 1 vez, Hecdog recibe 1 vez y el expediente no se abre otra vez.
##  2. «Volver a ver» la presentación dos veces seguidas: solo una a la vez y no cuenta como cinemática nueva.
##  3. Guardar y cargar en el Campamento: no vuelve a salir nada y el registro de vistas viaja en la partida.
##  4. Campamento ↔ Lago tres veces e inspeccionar dos veces la misma pista: nada se repite.
##  5. La estación pide pruebas del Lago antes (Hecdog no presenta a los seis sin ellas).
##  6. Sala: la presentación sale 1 vez; al volver, nada. Los implicados caminan (no se deslizan quietos).
##  7. NPC: Porky no está en el Campamento, nadie en pose T, no se mueven al mismo cuadro, se voltean al hablar.
## Uso: --headless --path game --script res://Herramientas/Caso1/probar_ca03.gd
## Respalda en disco (user://respaldo_pruebas/) las ranuras y el progreso, y los devuelve al final.

const PARTIDAS := "user://partidas/"
const ARCHIVOS := ["user://progreso.json", "user://progreso.json.bak", "user://cinematicas.cfg"]
const RESPALDO := "user://respaldo_pruebas/"
const CAMPAMENTO := "res://Escenas/Caso1/escenario_campamento.tscn"
const LAGO := "res://Escenas/Caso1/Lago/escenario_lago.tscn"
const SALA := "res://Escenas/Caso1/Interrogatorio/sala_interrogatorio_caso1.tscn"

var _fallos := 0
var _gestor
## Cuántas veces salió sola cada cinemática (señal CinematicaReproducida) y cada escena automática al llegar
var _cinematicas := {}
var _escenas := {}

func _initialize() -> void:
	if DirAccess.dir_exists_absolute(RESPALDO):
		_devolver_respaldo()
	_respaldar()
	await process_frame
	_gestor = root.get_node("GestorPartida")
	_gestor.CinematicaReproducida.connect(func(id): _cinematicas[id] = _cinematicas.get(id, 0) + 1)

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

	print("2) Volver a ver la presentación desde el expediente")
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

	print("6) Sala de interrogatorio")
	for vuelta in 2:
		_gestor.CambiarEscenaDelCapitulo(SALA, "EntradaDesdeLago")
		await _esperar_escena(SALA)
		await create_timer(1.5).timeout
		await _contar_escena("sala")
		await _cerrar_dialogos()
		if vuelta == 0:
			await _probar_caminar()
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
	for nombre in ["Eriz", "JuanCarlos"]:
		_revisar(not _en_pose_t(personajes.get_node(nombre)), "%s no está en pose T" % nombre)
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

	print("\nCinemáticas que salieron solas: %s" % _cinematicas)
	print("Escenas automáticas al llegar: %s" % _escenas)
	_revisar(_cinematicas == {"intro_caso01": 1}, "cada cinemática salió una sola vez")
	_revisar(_escenas.get("campamento", 0) == 1 and _escenas.get("sala", 0) == 1 and _escenas.get("lago", 0) == 1,
		"Hecdog recibe una vez en el Campamento y otra en la sala, Caneloso piensa una vez al llegar al Lago; nada se repite al volver")
	print("Fallos: %d" % _fallos)
	_devolver_respaldo()
	quit(1 if _fallos > 0 else 0)

## Un implicado sin animación de caminar entra a la sala: mientras camina mueve las piernas
func _probar_caminar() -> void:
	var caso = current_scene.get_node("Caso1Sala")
	var eriz: Node3D = current_scene.get_node("Implicados/Eriz")
	var anim = eriz.get_node("Animacion")
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

func _en_pose_t(personaje: Node3D) -> bool:
	var esqueleto := _esqueleto(personaje)
	if esqueleto == null:
		return false
	var brazo := esqueleto.find_bone("Arm_1_L")
	return brazo >= 0 and esqueleto.get_bone_pose_rotation(brazo).angle_to(esqueleto.get_bone_rest(brazo).basis.get_rotation_quaternion()) < 0.1

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

func _cerrar_dialogos() -> void:
	for i in 30:
		var dialogo = current_scene.get_node_or_null("VentanaDialogo")
		if dialogo == null or not dialogo.Abierto:
			return
		# Si pregunta algo, elige la última opción (en el guion es la de salir o seguir), con su número
		var opciones := dialogo.find_children("*", "Button", true, false).filter(func(b): return b.is_visible_in_tree() and not b.disabled)
		if opciones.size() > 0:
			var tecla := InputEventKey.new()
			tecla.keycode = KEY_1 + mini(opciones.size(), 9) - 1
			tecla.pressed = true
			Input.parse_input_event(tecla)
			await process_frame
			var soltar := tecla.duplicate()
			soltar.pressed = false
			Input.parse_input_event(soltar)
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
