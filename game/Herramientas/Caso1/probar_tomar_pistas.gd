extends SceneTree
## Toma y examina cada pista del Caso 1 como un jugador: se pone frente a ella, apunta con la retícula, pulsa [E], pasa la
## cinemática si la hay, pulsa [E] otra vez para examinarla y cierra con Esc. Si la retícula no la alcanza, también lo dice.
## Necesita ventana (sin ella no se captura el ratón y [E] no hace nada). Uso: --path game --script res://Herramientas/Caso1/probar_tomar_pistas.gd
## Con «-- solo-caso0» o «-- solo-campamento», solo esa parte.
## Respalda en disco (user://respaldo_pruebas/) las ranuras y el progreso, y los devuelve al final.

const PARTIDAS := "user://partidas/"
const ARCHIVOS := ["user://progreso.json", "user://progreso.json.bak", "user://cinematicas.cfg"]
const RESPALDO := "user://respaldo_pruebas/"
const CAMPAMENTO := "res://Escenas/Caso1/escenario_campamento.tscn"
const LAGO := "res://Escenas/Caso1/Lago/escenario_lago.tscn"

var _gestor
var _fallos := 0

func _initialize() -> void:
	if DirAccess.dir_exists_absolute(RESPALDO):
		_devolver_respaldo()
	_respaldar()
	await process_frame
	_gestor = root.get_node("GestorPartida")
	if OS.get_cmdline_user_args().has("solo-caso0"):
		_gestor.IniciarCapitulo("caso0", true, true)
		await _esperar_escena("res://Escenas/Interrogatorio/sala_interrogatorio.tscn")
		await _quitar_lo_de_encima()
		# Con la combinación y los contenedores abiertos, como cuando el jugador llega a ellos
		var jugador_c0 = current_scene.find_child("Jugador", true, false)
		jugador_c0.DarObjeto("combinacion_porky", "")
		for n in current_scene.find_children("*", "StaticBody3D", true, false):
			if n.get("EstaAbierto") == false:
				n.Interactuar(jugador_c0)
		await create_timer(2.5).timeout
		await _cerrar_dialogos()
		await _probar_escena("CASO 0")
		print("Fallos: %d" % _fallos)
		_devolver_respaldo()
		quit(1 if _fallos > 0 else 0)
		return
	_gestor.NuevaPartida(true)
	await _esperar_escena(CAMPAMENTO)
	await _quitar_lo_de_encima()
	await _probar_escena("CAMPAMENTO")
	if OS.get_cmdline_user_args().has("solo-campamento"):
		print("Fallos: %d" % _fallos)
		_devolver_respaldo()
		quit(1 if _fallos > 0 else 0)
		return
	_gestor.CambiarEscenaDelCapitulo(LAGO, "EntradaDesdeCampamento")
	await _esperar_escena(LAGO)
	await _quitar_lo_de_encima()
	await _probar_escena("LAGO")
	print("Fallos: %d" % _fallos)
	_devolver_respaldo()
	quit(1 if _fallos > 0 else 0)

func _probar_escena(titulo: String) -> void:
	print("== ", titulo)
	var escena = current_scene
	var jugador = escena.find_child("Jugador", true, false)
	var hud = escena.get_node("HUD")
	if not hud.linterna_encendida:
		hud.linterna_encendida = true
		hud.linterna_cambiada.emit(true)
	var pistas := get_nodes_in_group("objetos_pista").filter(func(n): return escena.is_ancestor_of(n))
	if OS.get_cmdline_user_args().has("al-reves"): pistas.reverse()
	for pista in pistas:
		if not pista.EsEvidencia: continue
		await _probar_pista(pista, jugador, hud)

func _probar_pista(pista, jugador, hud) -> void:
	var id: String = pista.IdPista
	# Las que están dentro de algo: primero se abre como lo haría el jugador
	var caso = current_scene.find_child("Caso1", true, false)
	if caso == null: caso = current_scene.find_child("Caso1Lago", true, false)
	var indice: int = caso.PistasDentro.find(pista) if caso != null else -1
	if indice >= 0:
		var contenedor = caso.ContenedoresConPista[indice]
		contenedor.Interactuar(jugador)
		await create_timer(1.2).timeout
		await _cerrar_dialogos()
	var capa_inicial: int = pista.collision_layer
	var punto := _centro(pista)
	if pista.SoloAgachado:
		Input.action_press("agacharse")
		await create_timer(0.5).timeout
	# Se pone frente a la pista, a la altura de un jugador, y mira a su centro
	# Un primer intento que se cayó del escenario no cuenta: vuelve a pararlo arriba
	jugador.velocity = Vector3.ZERO
	# Quieto en el aire: colocado sobre un mueble, la física lo sacaría de lugar. Lo que se prueba es la retícula
	jugador.set_physics_process(false)
	var mejor := await _colocar_y_apuntar(jugador, pista, punto, hud)
	var apunta: bool = mejor != ""
	if not apunta:
		jugador.set_physics_process(true)
		Input.action_release("agacharse")
		_fallos += 1
		print("  ✗ %s: la retícula no la alcanza (capa %d, posición %s)" % [id, capa_inicial, punto.snapped(Vector3.ONE * 0.01)])
		_que_tapa(jugador, punto)
		return
	await _accion("interactuar")
	jugador.set_physics_process(true)
	await create_timer(0.3).timeout
	await _pasar_cinematica()
	await _cerrar_dialogos()
	Input.action_release("agacharse")
	await create_timer(0.3).timeout
	var tomada: bool = pista.Encontrada
	if not tomada:
		# ¿Se perdió la tecla o de verdad no se puede? Una segunda vez, como haría el jugador
		print("      %s: primera [E] sin efecto; modo ratón %d, bloqueado %s, oferta examen, apuntaba «%s»" % [id, Input.mouse_mode, jugador.get("_bloqueado"), str(hud.get("interaccion"))])
		await _accion("interactuar")
		await create_timer(0.5).timeout
		await _pasar_cinematica()
		await _cerrar_dialogos()
		tomada = pista.Encontrada
	if not tomada:
		_fallos += 1
		print("  ✗ %s: [E] no la tomó (decía «%s»)" % [id, mejor])
		_que_tapa(jugador, punto)
		return
	# Examinar desde el diario, y con [E] si sigue la oferta
	var recurso = null
	for p in hud.pistas:
		if str(p.id) == id: recurso = p
	jugador.Examinar(recurso)
	await create_timer(0.5).timeout
	var abierta: bool = jugador.Examinando
	if not abierta:
		_fallos += 1
		print("  ✗ %s: no se abre la vista de examen" % id)
		return
	var cerrar := InputEventKey.new()
	cerrar.keycode = KEY_ESCAPE
	cerrar.physical_keycode = KEY_ESCAPE
	cerrar.pressed = true
	Input.parse_input_event(cerrar)
	await process_frame
	var soltar := cerrar.duplicate()
	soltar.pressed = false
	Input.parse_input_event(soltar)
	await create_timer(0.8).timeout
	if jugador.Examinando:
		_fallos += 1
		print("  ✗ %s: Esc no cierra la vista de examen" % id)
		return
	print("  ✓ %s (decía «%s»)" % [id, mejor])

## Qué hay entre los ojos y la pista
func _que_tapa(jugador, punto: Vector3) -> void:
	var camara: Camera3D = jugador.get_viewport().get_camera_3d()
	var consulta := PhysicsRayQueryParameters3D.create(camara.global_position, punto)
	consulta.exclude = [jugador.get_rid()]
	consulta.collision_mask = 1 | 2 | 8 | 16 | 32
	var r: Dictionary = jugador.get_world_3d().direct_space_state.intersect_ray(consulta)
	print("      ojos %s; tapa: %s" % [camara.global_position.snapped(Vector3.ONE * 0.01), r.get("collider").get_path() if r.has("collider") else "nada"])

func _centro(pista) -> Vector3:
	for n in pista.find_children("*", "CollisionShape3D", true, false):
		return n.global_position
	return pista.global_position

## Prueba varios lados: devuelve lo que dice el HUD al apuntarla, o "" si ninguno sirve
func _colocar_y_apuntar(jugador, pista, punto: Vector3, hud) -> String:
	var camara: Camera3D = jugador.find_child("*", true, false) if false else jugador.get_viewport().get_camera_3d()
	for lado in [pista.global_basis.z, -pista.global_basis.z, pista.global_basis.x, -pista.global_basis.x, Vector3(0.7, 0, 0.7), Vector3(-0.7, 0, 0.7), Vector3(0.7, 0, -0.7), Vector3(-0.7, 0, -0.7)]:
		for distancia in [1.2, 0.8, 1.7, 2.2]:
			var ojos: Vector3 = punto + Vector3(lado.x, 0, lado.z).normalized() * distancia
			ojos.y = punto.y + (0.1 if punto.y > 1.0 else 1.2)
			jugador.velocity = Vector3.ZERO
			jugador.global_position = ojos - Vector3(0, camara.global_position.y - jugador.global_position.y, 0)
			var plano := Vector3(punto.x - ojos.x, 0, punto.z - ojos.z)
			jugador.global_rotation = Vector3(0, atan2(-plano.x, -plano.z), 0)
			await physics_frame
			var hacia: Vector3 = (punto - camara.global_position).normalized()
			camara.rotation.x = asin(clamp(hacia.y, -1.0, 1.0))
			await physics_frame
			await physics_frame
			await process_frame
			# Cae al piso o se aparta de lo que tenía encima: deja que se asiente y vuelve a apuntar desde donde quedó
			await create_timer(0.5).timeout
			# Colocado dentro de un mueble, el empujón lo saca por debajo del piso: se prueba otro lugar
			if jugador.global_position.y < punto.y - 3.0:
				continue
			var ahora := Vector3(punto.x - jugador.global_position.x, 0, punto.z - jugador.global_position.z)
			jugador.global_rotation = Vector3(0, atan2(-ahora.x, -ahora.z), 0)
			await physics_frame
			var hacia2: Vector3 = (punto - camara.global_position).normalized()
			camara.rotation.x = asin(clamp(hacia2.y, -1.0, 1.0))
			await physics_frame
			await physics_frame
			await process_frame
			var texto: String = str(hud.get("interaccion"))
			if texto == "Inspeccionar" or texto == "Recoger":
				return texto
	return ""

func _accion(nombre: String) -> void:
	Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
	var e := InputEventAction.new()
	e.action = nombre
	e.pressed = true
	Input.parse_input_event(e)
	await process_frame
	var s := e.duplicate()
	s.pressed = false
	Input.parse_input_event(s)
	await process_frame

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

func _pasar_cinematica() -> void:
	for i in 60:
		if root.get_node_or_null("CinematicaCaso1") == null:
			return
		await _clic()
		await create_timer(0.3).timeout

func _cerrar_dialogos() -> void:
	var dialogo = current_scene.get_node_or_null("VentanaDialogo")
	for i in 40:
		await _pasar_cinematica()
		if dialogo == null or not dialogo.Abierto:
			return
		await _clic()
		await create_timer(0.3).timeout

## La presentación y lo primero que dicen al llegar
func _quitar_lo_de_encima() -> void:
	await create_timer(3.0).timeout
	for i in 40:
		var hay := false
		for hijo in root.get_children():
			if hijo.get_script() and str(hijo.get_script().resource_path).ends_with("IntroCaso.cs") and not hijo.is_queued_for_deletion(): hay = true
		if not hay: break
		var e := InputEventKey.new()
		e.keycode = KEY_SPACE
		e.physical_keycode = KEY_SPACE
		e.pressed = true
		Input.parse_input_event(e)
		await create_timer(1.8).timeout
		var s := e.duplicate()
		s.pressed = false
		Input.parse_input_event(s)
		await create_timer(0.3).timeout
	await create_timer(1.5).timeout
	var dialogo = current_scene.get_node_or_null("VentanaDialogo")
	for i in 80:
		await _pasar_cinematica()
		if dialogo == null or not dialogo.Abierto: break
		var opciones: Array = dialogo.find_children("*", "Button", true, false).filter(func(b): return b.is_visible_in_tree() and not b.disabled)
		if opciones.size() > 0:
			var tecla := InputEventKey.new()
			tecla.keycode = KEY_1 + mini(opciones.size(), 9) - 1
			tecla.pressed = true
			Input.parse_input_event(tecla)
			await process_frame
			var suelta := tecla.duplicate()
			suelta.pressed = false
			Input.parse_input_event(suelta)
		else:
			await _clic()
		await create_timer(0.35).timeout
	await create_timer(1.0).timeout

func _esperar_escena(ruta: String) -> void:
	for i in 400:
		await process_frame
		if current_scene != null and current_scene.scene_file_path == ruta and current_scene.is_node_ready():
			break
	await create_timer(0.5).timeout

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
