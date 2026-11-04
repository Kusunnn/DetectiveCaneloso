extends "res://Herramientas/Caso1/probar_tomar_pistas.gd"
## Nada visible que no responda. Para cada evidencia del Caso 0 y del Caso 1 busca un punto ALCANZABLE a pie, lleva allí al
## jugador con la física encendida, apunta con la retícula, pulsa [E] y comprueba que quedó en el diario.
## Lo alcanzable se calcula recorriendo el piso desde donde empieza el jugador, en una cuadrícula de 35 cm, con el tamaño del agachado
## jugador y sin escalones de más de 45 cm. Las escenas no tienen NavigationRegion3D, y hornearla en el lago pide demasiada
## memoria. Las puertas y los contenedores se dan por abiertos: que se abran es otra prueba (probar_escenarios.gd).
## Necesita ventana. Respalda las partidas en disco y las devuelve al final.
## Uso: --path game --script res://Herramientas/Caso1/probar_pistas.gd -- <reporte.md> [solo-caso0|solo-campamento|solo-lago]

const CELDA := 0.35
const ESCALON := 0.45
const MAXIMO_CELDAS := 160000
const ALCANCE := 2.7

var _filas: Array[String] = []
var _visibles_sin_tomar := 0

func _initialize() -> void:
	if DirAccess.dir_exists_absolute(RESPALDO):
		_devolver_respaldo()
	_respaldar()
	await process_frame
	_gestor = root.get_node("GestorPartida")
	var args := OS.get_cmdline_user_args()
	var solo := ""
	for a in args:
		if a.begins_with("solo-"): solo = a
	if solo == "" or solo == "solo-caso0":
		_gestor.IniciarCapitulo("caso0", true, true)
		await _esperar_escena("res://Escenas/Interrogatorio/sala_interrogatorio.tscn")
		await _quitar_lo_de_encima()
		await _revisar_escena("Caso 0")
	if solo == "" or solo == "solo-campamento" or solo == "solo-lago":
		_gestor.NuevaPartida(true)
		await _esperar_escena(CAMPAMENTO)
		await _quitar_lo_de_encima()
		if solo != "solo-lago":
			await _revisar_escena("Campamento")
		if solo != "solo-campamento":
			_gestor.CambiarEscenaDelCapitulo(LAGO, "EntradaDesdeCampamento")
			await _esperar_escena(LAGO)
			await _quitar_lo_de_encima()
			await _revisar_escena("Lago")
	var texto := "# Reporte de pistas\n\nVisibles que no se pueden tomar: **%d**\n\n" % _visibles_sin_tomar
	texto += "| id | escena | ¿se toma? | si todavía no se puede, ¿lo explica? | causa | corrección |\n|---|---|---|---|---|---|\n"
	texto += "\n".join(_filas) + "\n"
	if args.size() > 0 and not args[0].begins_with("solo-"):
		var archivo := FileAccess.open(args[0], FileAccess.WRITE)
		if archivo: archivo.store_string(texto)
	print(texto)
	print("Fallos: %d" % _visibles_sin_tomar)
	_devolver_respaldo()
	quit(1 if _visibles_sin_tomar > 0 else 0)

func _revisar_escena(titulo: String) -> void:
	print("== ", titulo)
	var escena = current_scene
	var jugador = escena.find_child("Jugador", true, false)
	var hud = jugador.get("_hud") if jugador.get("_hud") != null else escena.find_child("HUD", true, false)
	if hud == null: hud = root.get_node_or_null("HUD")
	_abrir_todo(escena, jugador)
	await create_timer(0.3).timeout
	await _cerrar_dialogos()
	var piso := await _recorrer_piso(jugador)
	print("  piso alcanzable: %d celdas" % piso.size())
	var ids := {}
	for pista in get_nodes_in_group("objetos_pista"):
		if not escena.is_ancestor_of(pista) or not pista.EsEvidencia: continue
		var id: String = pista.IdPista
		if ids.has(id):
			_fila(id, titulo, false, "—", "Id duplicado", "Darle un Id propio")
			continue
		ids[id] = true
		await _revisar_pista(pista, jugador, hud, piso, titulo)

## Puertas y contenedores abiertos, sin animación
func _abrir_todo(escena, _jugador_) -> void:
	for n in escena.find_children("*", "Node3D", true, false):
		if not n.has_method("RestaurarEstado"): continue
		var script_ = n.get_script()
		var ruta: String = script_.resource_path if script_ else ""
		if ruta.ends_with("Contenedor.cs"): n.RestaurarEstado(true, false)
		elif ruta.ends_with("Puerta.cs") or ruta.ends_with("PuntoInteraccionPuerta.cs"): n.RestaurarEstado(false, true)

## Recorre el piso en cuadrícula desde donde está el jugador; devuelve los puntos de sus pies
func _recorrer_piso(jugador) -> Array[Vector3]:
	var espacio: PhysicsDirectSpaceState3D = jugador.get_world_3d().direct_space_state
	var mascara: int = jugador.collision_mask
	var forma := CapsuleShape3D.new()
	forma.radius = 0.2
	forma.height = 0.8
	var cuerpo := PhysicsShapeQueryParameters3D.new()
	cuerpo.shape = forma
	cuerpo.collision_mask = mascara
	cuerpo.exclude = [jugador.get_rid()]
	var inicio: Vector3 = jugador.global_position + Vector3.DOWN
	var vistas := {}
	var cola: Array[Vector3] = [inicio]
	var salida: Array[Vector3] = []
	var vueltas := 0
	var frente := 0
	while frente < cola.size() and salida.size() < MAXIMO_CELDAS:
		var p: Vector3 = cola[frente]
		frente += 1
		for d: Vector2 in [Vector2(1, 0), Vector2(-1, 0), Vector2(0, 1), Vector2(0, -1)]:
			var x := p.x + d.x * CELDA
			var z := p.z + d.y * CELDA
			var llave := Vector3i(roundi(x / CELDA), roundi(p.y / 1.5), roundi(z / CELDA))
			if vistas.has(llave): continue
			vistas[llave] = true
			var rayo := PhysicsRayQueryParameters3D.create(Vector3(x, p.y + ESCALON + 0.05, z), Vector3(x, p.y - 1.2, z), mascara)
			rayo.exclude = [jugador.get_rid()]
			var r := espacio.intersect_ray(rayo)
			if r.is_empty(): continue
			var pie: Vector3 = r["position"]
			if pie.y - p.y > ESCALON: continue
			cuerpo.transform = Transform3D(Basis.IDENTITY, pie + Vector3(0, 0.52, 0))
			if not espacio.intersect_shape(cuerpo, 1).is_empty(): continue
			salida.append(pie)
			cola.append(pie)
		vueltas += 1
		if vueltas % 800 == 0:
			await process_frame
	return salida

func _revisar_pista(pista, jugador, hud, piso: Array[Vector3], titulo: String) -> void:
	var id: String = pista.IdPista
	var punto := _centro(pista)
	var cercanos := piso.filter(func(p): return Vector2(p.x - punto.x, p.z - punto.z).length() < ALCANCE and absf(p.y - punto.y) < 3.0)
	if cercanos.is_empty():
		_fila(id, titulo, false, _explica(pista, null), "Zona bloqueada: no hay piso alcanzable a %.1f m" % ALCANCE, "Revisar paso o colisión que cierra la zona")
		return
	cercanos.sort_custom(func(a, b): return a.distance_squared_to(punto) < b.distance_squared_to(punto))
	# Lo que está dentro de algo: se abre como lo haría el jugador
	var caso = current_scene.find_child("Caso1", true, false)
	if caso == null: caso = current_scene.find_child("Caso1Lago", true, false)
	var dentro: int = caso.PistasDentro.find(pista) if caso != null and caso.get("PistasDentro") != null else -1
	if dentro >= 0:
		var contenedor = caso.ContenedoresConPista[dentro]
		contenedor.RestaurarEstado(false, false)
		contenedor.Interactuar(jugador)
		await create_timer(1.2).timeout
		await _cerrar_dialogos()
	# Solo los puntos desde donde se ve la pista (o lo que la contiene), sin una pared de por medio; los más cercanos primero
	var espacio: PhysicsDirectSpaceState3D = jugador.get_world_3d().direct_space_state
	var visibles: Array = []
	for pie: Vector3 in cercanos:
		var rayo := PhysicsRayQueryParameters3D.create(pie + Vector3.UP * (0.8 if pista.SoloAgachado else 1.5), punto, jugador.collision_mask | 8 | 16 | 32)
		rayo.exclude = [jugador.get_rid()]
		var golpe: Dictionary = espacio.intersect_ray(rayo)
		if golpe.is_empty() or golpe["collider"].has_method("RestaurarEstado") or pista.is_ancestor_of(golpe["collider"]) or golpe["collider"] == pista:
			visibles.append(pie)
		if visibles.size() >= 60: break
	# Pegada a la pared el rayo de arriba puede fallar: entonces decide la retícula, desde puntos repartidos
	if not visibles.is_empty(): cercanos = visibles
	var paso := maxi(1, cercanos.size() / (12 if not visibles.is_empty() else 36))
	var aviso := "—"
	var encontro := ""
	# Si exige agacharse, de pie y en el punto más cercano debe decirlo
	if pista.SoloAgachado:
		jugador.velocity = Vector3.ZERO
		jugador.global_position = cercanos[0] + Vector3.UP * 1.02
		await physics_frame
		await physics_frame
		_apuntar(jugador, punto)
		await physics_frame
		await process_frame
		await process_frame
		aviso = _explica(pista, hud)
	var ultimo_tapa := ""
	for i in range(0, cercanos.size(), paso):
		var pie: Vector3 = cercanos[i]
		jugador.velocity = Vector3.ZERO
		jugador.global_position = pie + Vector3.UP * 1.02
		await physics_frame
		await physics_frame
		_apuntar(jugador, punto)
		await physics_frame
		await process_frame
		if pista.SoloAgachado:
			Input.action_press("agacharse")
			await create_timer(0.35).timeout
			_apuntar(jugador, punto)
			await physics_frame
			await process_frame
		if pista.RequiereLinterna and not hud.linterna_encendida:
			hud.linterna_encendida = true
			hud.linterna_cambiada.emit(true)
			await physics_frame
		var texto: String = str(hud.get("interaccion"))
		# Debe ser ella la que está en la retícula: el texto puede venir de otra cosa que quedó en medio
		if (texto == "Inspeccionar" or texto == "Recoger") and jugador.ApuntadoNodo == pista:
			encontro = texto
			break
		ultimo_tapa = _lo_que_tapa(jugador, punto)
		Input.action_release("agacharse")
	if encontro == "":
		Input.action_release("agacharse")
		_fila(id, titulo, false, aviso, ultimo_tapa if ultimo_tapa != "" else "Distancia de interacción o capa de colisión", "Ajustar el colisionador o acercarla")
		return
	await _accion("interactuar")
	await create_timer(0.3).timeout
	await _pasar_cinematica()
	await _cerrar_dialogos()
	# Si la tecla se perdió (una cinemática o un pensamiento encima), otra vez, como haría el jugador
	if not pista.Encontrada:
		_apuntar(jugador, punto)
		await create_timer(0.4).timeout
		await _accion("interactuar")
		await create_timer(0.4).timeout
		await _pasar_cinematica()
		await _cerrar_dialogos()
	Input.action_release("agacharse")
	await create_timer(0.25).timeout
	var registrada := false
	for p in hud.pistas:
		if str(p.id) == id: registrada = true
	if not (pista.Encontrada and registrada):
		_fila(id, titulo, false, aviso, "Responde a la retícula pero no se registró", "Revisar script e Id")
		return
	if pista.RequiereLinterna: aviso = "sí: «Está demasiado oscuro… [F]»"
	_fila(id, titulo, true, aviso, "—", "—")

func _apuntar(jugador, punto: Vector3) -> void:
	var ahora := Vector3(punto.x - jugador.global_position.x, 0, punto.z - jugador.global_position.z)
	jugador.global_rotation = Vector3(0, atan2(-ahora.x, -ahora.z), 0)
	var camara: Camera3D = jugador.get_viewport().get_camera_3d()
	var hacia: Vector3 = (punto - camara.global_position).normalized()
	camara.rotation.x = asin(clamp(hacia.y, -1.0, 1.0))

func _explica(pista, hud) -> String:
	if not pista.SoloAgachado: return "—"
	if hud == null: return "no se comprobó"
	return "sí: «%s»" % str(hud.get("interaccion")) if str(hud.get("interaccion")).contains("gach") else "NO: de pie no dice nada"

func _lo_que_tapa(jugador, punto: Vector3) -> String:
	var camara: Camera3D = jugador.get_viewport().get_camera_3d()
	var consulta := PhysicsRayQueryParameters3D.create(camara.global_position, punto)
	consulta.exclude = [jugador.get_rid()]
	consulta.collision_mask = 1 | 2 | 8 | 16 | 32
	var r: Dictionary = jugador.get_world_3d().direct_space_state.intersect_ray(consulta)
	if r.is_empty(): return "Distancia de interacción"
	var nodo = r.get("collider")
	return "Otro colisionador tapa el rayo: %s" % nodo.name

func _fila(id: String, escena: String, toma: bool, explica: String, causa: String, correccion: String) -> void:
	if not toma: _visibles_sin_tomar += 1
	_filas.append("| %s | %s | %s | %s | %s | %s |" % [id, escena, "sí" if toma else "**NO**", explica, causa, correccion])
	print("  %s %s" % ["✓" if toma else "✗", _filas[-1]])
