class_name AuditorEscenario
extends RefCounted
## Revisión automática de un escenario 3D: objetos flotando o enterrados, objetos encimados, mallas sin colisión,
## colisiones invisibles, objetos fuera del mapa, escalas raras y pistas o contenedores a los que no se llega.
## Lo usan Herramientas/AuditoriaEscenario.gd (EditorScript, en el editor) y Herramientas/auditar.gd (consola).
##
## Grupos que cambian la revisión de un objeto (se ponen en la escena):
##   auditoria_colgado   → cuelga del techo o de un poste (no se revisa si flota)
##   auditoria_pared     → va pegado a una pared (se revisa la distancia a la pared, no al piso)
##   auditoria_estructura→ muros, techos y pisos de construcciones (pueden tocarse entre sí)
##   auditoria_fuera_ok  → puede estar fuera del área jugable (bosque lejano, lago, cielo)
##   auditoria_invisible_ok → colisión sin malla a propósito (bordes del mapa, rampas)
##   auditoria_personaje → personaje con esqueleto (se revisa solo su colisión)

const CAPA_AUDITORIA := 1 << 19          # Capa temporal con las mallas convertidas en colisión
const MASCARA_JUGADOR := 1 | 2 | 16 | 32 | 64
const TOLERANCIA := 0.02                 # 2 cm
const ALTURA_ATRAVESABLE := 0.35         # Una malla más baja que esto se puede pisar o es decorado de suelo
const ALCANCE_INTERACCION := 2.6         # Distancia desde el borde de la zona caminable hasta el objeto

var hallazgos: Array[Dictionary] = []
var _raiz: Node3D
var _espacio: PhysicsDirectSpaceState3D
var _cuerpos_auditoria := {}             # MeshInstance3D -> StaticBody3D temporal
var _objetos: Array[Node3D] = []
var _limites := AABB()

## Revisa la escena ya instanciada dentro de un árbol (con su propio mundo 3D). Hay que esperar con await.
func auditar(raiz: Node3D) -> Array[Dictionary]:
	_raiz = raiz
	hallazgos.clear()
	var arbol := raiz.get_tree()
	_preparar_colisiones_de_mallas()
	for i in 4:
		await arbol.physics_frame
	_espacio = raiz.get_world_3d().direct_space_state
	_limites = _calcular_limites()
	_objetos = _buscar_objetos(raiz)
	_revisar_apoyo()
	_revisar_solapes()
	_revisar_colisiones()
	_revisar_limites()
	_revisar_escalas()
	await _revisar_alcance()
	_limpiar()
	return hallazgos

# ---------------------------------------------------------------- Preparación

func _preparar_colisiones_de_mallas() -> void:
	for m in _raiz.find_children("*", "MeshInstance3D", true, false):
		var malla := m as MeshInstance3D
		if malla.mesh == null or not malla.is_visible_in_tree() or _dentro_de_personaje(malla): continue
		var forma := malla.mesh.create_trimesh_shape()
		if forma == null: continue
		var cuerpo := StaticBody3D.new()
		cuerpo.collision_layer = CAPA_AUDITORIA
		cuerpo.collision_mask = 0
		var cs := CollisionShape3D.new()
		cs.shape = forma
		cuerpo.add_child(cs)
		malla.add_child(cuerpo)
		_cuerpos_auditoria[malla] = cuerpo

func _limpiar() -> void:
	for c in _cuerpos_auditoria.values():
		if is_instance_valid(c): c.queue_free()
	_cuerpos_auditoria.clear()

## Área jugable: la unión de las formas del nodo "Limites" (paredes invisibles del borde)
func _calcular_limites() -> AABB:
	var limites := _raiz.get_node_or_null("Limites")
	var caja := AABB()
	var primero := true
	if limites:
		for cs in limites.find_children("*", "CollisionShape3D", true, false):
			var a := _aabb_forma(cs)
			if primero: caja = a; primero = false
			else: caja = caja.merge(a)
	return caja

## Un "objeto" es un modelo instanciado (.glb/.tscn) o una malla suelta que no es parte de otro modelo.
func _buscar_objetos(nodo: Node) -> Array[Node3D]:
	var lista: Array[Node3D] = []
	for hijo in nodo.get_children():
		# Un personaje con esqueleto: su AABB es la de la pose de origen, no la visible; solo cuenta su colisión
		if hijo.is_in_group("auditoria_personaje"): continue
		if hijo is MultiMeshInstance3D or hijo is GPUParticles3D or hijo is Light3D or hijo is CharacterBody3D: continue
		if hijo is Node3D and (_es_modelo(hijo) or _es_compuesto(hijo)):
			# Un objeto compuesto (una bota hecha de piezas, una pista con su malla) se revisa entero
			lista.append(hijo)
		elif hijo is MeshInstance3D and not hijo.get_class() == "Label3D":
			lista.append(hijo)
			lista.append_array(_buscar_objetos(hijo))
		else:
			lista.append_array(_buscar_objetos(hijo))
	return lista

## Objeto armado con piezas: un grupo marcado para acomodarse o una pista con sus mallas
func _es_compuesto(n: Node) -> bool:
	if n.scene_file_path.ends_with(".tscn"): return false
	for g in ["apoyar_suelo", "apoyar_pared", "auditoria_colgado"]:
		if n.is_in_group(g): return true
	return _script_es(n, "ObjetoPista.cs") or _script_es(n, "Contenedor.cs")

func _es_pared(o: Node) -> bool:
	return _en_grupo(o, "auditoria_pared") or _en_grupo(o, "apoyar_pared") or _es_cartel_vertical(o)

func _es_modelo(n: Node) -> bool:
	return n.scene_file_path.ends_with(".glb") or n.scene_file_path.ends_with(".gltf")

# ---------------------------------------------------------------- 1. Flotando o enterrados

func _revisar_apoyo() -> void:
	for o in _objetos:
		if _en_grupo(o, "auditoria_colgado") or _en_grupo(o, "auditoria_estructura") or _en_grupo(o, "auditoria_fuera_ok"): continue
		var caja := _aabb_global(o)
		if caja.size == Vector3.ZERO or caja.size.y < 0.005: continue
		var propios := _rids_propios(o)
		if _es_pared(o):
			_revisar_pared(o, caja, propios)
			continue
		var base := Vector3(caja.get_center().x, caja.position.y, caja.get_center().z)
		# Solo cuentan las superficies visibles (mallas): una colisión puede ser más grande que su mueble.
		# El rayo sale un poco arriba de la base para notar lo que quedó metido en su apoyo. Se prueban el centro
		# y cuatro puntos hacia las esquinas: una canoa sobre dos troncos o una mesa sobre sus patas sí se apoyan.
		var golpe := {}
		var mejor := INF
		for off in [Vector2.ZERO, Vector2(0.35, 0.35), Vector2(-0.35, 0.35), Vector2(0.35, -0.35), Vector2(-0.35, -0.35),
				Vector2(0, 0.25), Vector2(0, -0.25), Vector2(0.25, 0), Vector2(-0.25, 0)]:
			var punto := base + Vector3(off.x * caja.size.x, 0, off.y * caja.size.z)
			var g := _rayo(punto + Vector3.UP * 0.3, punto + Vector3.DOWN * 3.0, CAPA_AUDITORIA, propios)
			if g.is_empty(): continue
			var dd: float = base.y - (g.position as Vector3).y
			if absf(dd) < absf(mejor): mejor = dd; golpe = g
		if golpe.is_empty():
			# ¿Quedó tan metido en el suelo que el rayo ya salió por debajo? Se prueba desde más arriba
			golpe = _rayo(base + Vector3.UP * 2.5, base, CAPA_AUDITORIA, propios)
			if not golpe.is_empty():
				_agregar("Enterrado", o, "Base %.2f m por debajo de «%s»" % [golpe.position.y - base.y, _nombre_de(golpe.collider)])
			else:
				_agregar("Flotando", o, "No hay nada debajo hasta 3 m (base a %.2f m)" % base.y)
			continue
		var d: float = base.y - golpe.position.y
		# Algo guardado bajo la tapa o la almohada de un contenedor está escondido a propósito
		if d < 0 and _dentro_de_contenedor(golpe.collider): continue
		if d > TOLERANCIA:
			_agregar("Flotando", o, "Base %.2f m por encima de «%s»" % [d, _nombre_de(golpe.collider)])
		elif d < -0.05:
			_agregar("Enterrado", o, "Base %.2f m por debajo de «%s»" % [-d, _nombre_de(golpe.collider)])

func _revisar_pared(o: Node3D, caja: AABB, propios: Array[RID]) -> void:
	var centro := caja.get_center()
	var mejor := INF
	for dir in [o.global_basis.z.normalized(), -o.global_basis.z.normalized(), o.global_basis.x.normalized(), -o.global_basis.x.normalized()]:
		var g := _rayo(centro, centro + dir * 0.6, CAPA_AUDITORIA | 1, propios)
		if not g.is_empty():
			mejor = minf(mejor, centro.distance_to(g.position) - _media_profundidad(caja, dir))
	if mejor == INF:
		_agregar("Flotando", o, "Va en una pared, pero no hay pared a menos de 60 cm")
	elif mejor > TOLERANCIA:
		_agregar("Flotando", o, "Separado %.2f m de la pared" % mejor)

## Los carteles (CartelPared) parados van en una pared; los acostados (notas sobre una mesa) se apoyan
func _es_cartel_vertical(o: Node3D) -> bool:
	var p := o.get_parent()
	if p == null or not _script_es(p, "CartelPared.cs"): return false
	return absf((p as Node3D).global_basis.z.normalized().y) < 0.7

func _media_profundidad(caja: AABB, dir: Vector3) -> float:
	return absf(dir.x) * caja.size.x * 0.5 + absf(dir.y) * caja.size.y * 0.5 + absf(dir.z) * caja.size.z * 0.5

# ---------------------------------------------------------------- 2. Encimados o atravesados

func _revisar_solapes() -> void:
	var cajas := []
	for o in _objetos:
		var c := _aabb_global(o)
		if c.size.length() < 0.05: continue
		cajas.append([o, c])
	for i in cajas.size():
		for j in range(i + 1, cajas.size()):
			var a: Node3D = cajas[i][0]; var b: Node3D = cajas[j][0]
			if a.is_ancestor_of(b) or b.is_ancestor_of(a): continue
			# Piezas de un mismo objeto compuesto (una farola: poste y foco) pueden estar una dentro de otra
			if a.get_parent() == b.get_parent() and a.get_parent() != _raiz and a.get_parent().get_parent() != _raiz: continue
			if _en_grupo(a, "auditoria_estructura") and _en_grupo(b, "auditoria_estructura"): continue
			# Lo colgado y lo pegado a una pared toca a propósito la estructura y lo que cuelga junto
			if _en_grupo(a, "auditoria_colgado") or _en_grupo(b, "auditoria_colgado"): continue
			if (_es_pared(a) and _en_grupo(b, "auditoria_estructura")) or (_es_pared(b) and _en_grupo(a, "auditoria_estructura")): continue
			# La copa de un árbol cubre lo que está debajo; solo cuenta su tronco (que tiene colisión propia)
			if _en_grupo(a, "auditoria_vegetacion") or _en_grupo(b, "auditoria_vegetacion"): continue
			var ca: AABB = cajas[i][1]; var cb: AABB = cajas[j][1]
			if not ca.intersects(cb): continue
			var inter := ca.intersection(cb)
			var menor := minf(ca.get_volume(), cb.get_volume())
			if menor <= 0.0005: continue
			var proporcion := inter.get_volume() / menor
			# Un objeto pequeño dentro de una repisa, un baúl o sobre una mesa es contenido, no un choque
			if minf(ca.size.y, cb.size.y) < 0.3: continue
			if proporcion > 0.5 and inter.size.y > 0.05:
				_agregar("Encimados", a, "%d %% dentro de «%s»" % [int(proporcion * 100), _ruta(b)])

# ---------------------------------------------------------------- 3. Colisiones

func _revisar_colisiones() -> void:
	# Mallas que el jugador podría atravesar
	for o in _objetos:
		if _en_grupo(o, "auditoria_colgado") or _es_pared(o): continue
		var caja := _aabb_global(o)
		if caja.position.y > 0.35: continue  # Encima de un mueble: el jugador no llega a pisarlo
		if caja.size.y < ALTURA_ATRAVESABLE or maxf(caja.size.x, caja.size.z) < 0.3: continue
		if caja.position.y > 1.9: continue  # Por encima de la cabeza (techos, letreros altos)
		if not _hay_colision_en(caja.grow(-0.05), MASCARA_JUGADOR):
			_agregar("Sin colisión", o, "Malla de %.1f × %.1f × %.1f m que se puede atravesar" % [caja.size.x, caja.size.y, caja.size.z])
	# Colisiones invisibles: formas que bloquean al jugador sin una malla que las explique
	var cajas_malla := []
	for o in _objetos: cajas_malla.append(_aabb_global(o))
	for cs in _raiz.find_children("*", "CollisionShape3D", true, false):
		var cuerpo := cs.get_parent() as CollisionObject3D
		if cuerpo == null or (cuerpo.collision_layer & MASCARA_JUGADOR) == 0 or cs.disabled: continue
		if _en_grupo(cuerpo, "auditoria_invisible_ok") or _en_grupo(cs, "auditoria_invisible_ok"): continue
		if cs.shape is WorldBoundaryShape3D: continue
		var a := _aabb_forma(cs)
		if a.size.x > 60 or a.size.z > 60: continue  # El suelo
		var cubierta := false
		for c in cajas_malla:
			if (c as AABB).intersects(a.grow(-0.02)): cubierta = true; break
		if not cubierta:
			_agregar("Colisión invisible", cs, "Forma de %.1f × %.1f × %.1f m sin malla" % [a.size.x, a.size.y, a.size.z])

func _hay_colision_en(caja: AABB, mascara: int) -> bool:
	var q := PhysicsShapeQueryParameters3D.new()
	var forma := BoxShape3D.new()
	forma.size = caja.size.max(Vector3.ONE * 0.02)
	q.shape = forma
	q.transform = Transform3D(Basis(), caja.get_center())
	q.collision_mask = mascara
	return not _espacio.intersect_shape(q, 1).is_empty()

# ---------------------------------------------------------------- 4. Fuera del mapa

func _revisar_limites() -> void:
	if _limites.size == Vector3.ZERO:
		_agregar("Aviso", _raiz, "No hay nodo «Limites»: no se revisa si hay objetos fuera del mapa")
		return
	var area := _limites.grow(0.5)
	for o in _objetos:
		if _en_grupo(o, "auditoria_fuera_ok"): continue
		var c := _aabb_global(o).get_center()
		if c.x < area.position.x or c.x > area.end.x or c.z < area.position.z or c.z > area.end.z:
			_agregar("Fuera del mapa", o, "Centro en (%.1f, %.1f)" % [c.x, c.z])

# ---------------------------------------------------------------- 5. Escalas

func _revisar_escalas() -> void:
	var por_modelo := {}
	for o in _objetos:
		if not _es_modelo(o): continue
		var s := o.global_basis.get_scale()
		var mx := maxf(s.x, maxf(s.y, s.z)); var mn := minf(s.x, minf(s.y, s.z))
		if mn > 0 and mx / mn > 1.1 and not _en_grupo(o, "auditoria_estructura"):
			_agregar("Escala no uniforme", o, "Escala (%.2f, %.2f, %.2f)" % [s.x, s.y, s.z])
		var k := o.scene_file_path
		if not por_modelo.has(k): por_modelo[k] = []
		por_modelo[k].append([o, s.y])
	for k in por_modelo:
		var lista: Array = por_modelo[k]
		if lista.size() < 2: continue
		var ys := lista.map(func(x): return x[1])
		var mn: float = ys.min(); var mx: float = ys.max()
		if mn > 0 and mx / mn > 1.35 and not _en_grupo(lista[0][0], "auditoria_estructura"):
			_agregar("Escala dispareja", lista[0][0], "%s: alto entre ×%.2f y ×%.2f en %d copias" % [k.get_file(), mn, mx, lista.size()])

# ---------------------------------------------------------------- 6. Alcance (NavigationMesh)

func _revisar_alcance() -> void:
	var jugador := _raiz.get_node_or_null("Jugador") as Node3D
	if jugador == null:
		_agregar("Aviso", _raiz, "No hay nodo «Jugador»: no se revisa el alcance")
		return
	# Las puertas cuentan como abiertas (el caso las desbloquea)
	var puertas := {}
	for n in _raiz.find_children("*", "StaticBody3D", true, false):
		if _script_es(n, "Puerta.cs"):
			puertas[n] = n.collision_layer
			n.collision_layer = 0
	var nav := NavigationMesh.new()
	nav.agent_radius = 0.3                    # La cápsula del jugador
	nav.agent_height = 1.0                    # Agachado: la cápsula baja a 0.9 m
	nav.agent_max_climb = 0.2
	nav.agent_max_slope = 35.0
	nav.cell_size = 0.1
	nav.cell_height = 0.1
	nav.geometry_parsed_geometry_type = NavigationMesh.PARSED_GEOMETRY_STATIC_COLLIDERS
	nav.geometry_collision_mask = MASCARA_JUGADOR
	if _limites.size != Vector3.ZERO:
		nav.filter_baking_aabb = AABB(_limites.position - Vector3(0, 2, 0), _limites.size + Vector3(0, 6, 0))
	var fuente := NavigationMeshSourceGeometryData3D.new()
	NavigationServer3D.parse_source_geometry_data(nav, fuente, _raiz)
	NavigationServer3D.bake_from_source_geometry_data(nav, fuente)
	for p in puertas: p.collision_layer = puertas[p]
	if nav.get_polygon_count() == 0:
		_agregar("Aviso", _raiz, "No se pudo hornear la malla de navegación")
		return
	# Un mapa de navegación propio, con la misma celda que la malla horneada
	var mapa := NavigationServer3D.map_create()
	NavigationServer3D.map_set_cell_size(mapa, nav.cell_size)
	NavigationServer3D.map_set_cell_height(mapa, nav.cell_height)
	NavigationServer3D.map_set_active(mapa, true)
	var region_rid := NavigationServer3D.region_create()
	NavigationServer3D.region_set_map(region_rid, mapa)
	NavigationServer3D.region_set_navigation_mesh(region_rid, nav)
	for i in 12:
		await _raiz.get_tree().physics_frame
		await _raiz.get_tree().process_frame
	var inicio := NavigationServer3D.map_get_closest_point(mapa, jugador.global_position)
	var jugador_piso := inicio.y
	for n in _raiz.find_children("*", "StaticBody3D", true, false):
		if not (_script_es(n, "ObjetoPista.cs") or _script_es(n, "Contenedor.cs")): continue
		if not n.is_visible_in_tree(): continue
		var objetivo: Vector3 = (n as Node3D).global_position
		for c in n.get_children():
			if c is CollisionShape3D: objetivo = c.global_position
		# Se buscan puntos del piso alrededor del objeto (no la parte de arriba de un mueble, que también es
		# «caminable» para la malla) y se pide un camino desde el inicio del jugador hasta alguno de ellos
		var alcanzable := false
		var mas_cerca := INF
		for radio in [0.4, 0.8, 1.2, 1.7, 2.3]:
			for k in 12:
				var a := TAU * k / 12.0
				var candidato := Vector3(objetivo.x + cos(a) * radio, jugador_piso, objetivo.z + sin(a) * radio)
				var cerca := NavigationServer3D.map_get_closest_point(mapa, candidato)
				if cerca.y > jugador_piso + 0.45: continue  # encima de un mueble
				var plano := Vector2(cerca.x - objetivo.x, cerca.z - objetivo.z).length()
				mas_cerca = minf(mas_cerca, plano)
				if plano > ALCANCE_INTERACCION: continue
				var camino := NavigationServer3D.map_get_path(mapa, inicio, cerca, true)
				if not camino.is_empty() and camino[camino.size() - 1].distance_to(cerca) < 0.5:
					alcanzable = true
					break
			if alcanzable: break
		if not alcanzable:
			_agregar("Inalcanzable", n, "Sin camino desde el inicio del jugador (piso caminable más cercano a %.1f m)" % mas_cerca)
	NavigationServer3D.free_rid(region_rid)
	NavigationServer3D.free_rid(mapa)

# ---------------------------------------------------------------- Utilidades

func _rayo(desde: Vector3, hasta: Vector3, mascara: int, excluir: Array[RID]) -> Dictionary:
	var q := PhysicsRayQueryParameters3D.create(desde, hasta, mascara, excluir)
	q.hit_back_faces = true
	return _espacio.intersect_ray(q)

func _rids_propios(o: Node) -> Array[RID]:
	var lista: Array[RID] = []
	for m in o.find_children("*", "MeshInstance3D", true, false):
		if _cuerpos_auditoria.has(m): lista.append(_cuerpos_auditoria[m].get_rid())
	if o is MeshInstance3D and _cuerpos_auditoria.has(o): lista.append(_cuerpos_auditoria[o].get_rid())
	# Las colisiones del propio objeto (si es un cuerpo o está dentro de uno) tampoco cuentan como apoyo
	var p: Node = o
	for i in 3:
		if p == null: break
		if p is CollisionObject3D: lista.append((p as CollisionObject3D).get_rid())
		p = p.get_parent()
	# Las otras piezas del mismo objeto compuesto (las partes de una bota, el foco de una farola) no son su apoyo
	var padre := o.get_parent()
	if padre != _raiz and padre.get_parent() != _raiz:
		for m in padre.find_children("*", "MeshInstance3D", true, false):
			if _cuerpos_auditoria.has(m): lista.append(_cuerpos_auditoria[m].get_rid())
	for c in o.find_children("*", "CollisionObject3D", true, false):
		if not _cuerpos_auditoria.values().has(c): lista.append((c as CollisionObject3D).get_rid())
	return lista

func _aabb_global(o: Node3D) -> AABB:
	var caja := AABB()
	var primero := true
	var mallas := o.find_children("*", "MeshInstance3D", true, false)
	if o is MeshInstance3D: mallas.append(o)
	for m in mallas:
		var mi := m as MeshInstance3D
		if mi.mesh == null or not mi.is_visible_in_tree(): continue
		var a := mi.global_transform * mi.get_aabb()
		if primero: caja = a; primero = false
		else: caja = caja.merge(a)
	return caja

func _aabb_forma(cs: CollisionShape3D) -> AABB:
	if cs.shape == null: return AABB(cs.global_position, Vector3.ZERO)
	var malla := cs.shape.get_debug_mesh()
	return cs.global_transform * malla.get_aabb() if malla else AABB(cs.global_position, Vector3.ZERO)

func _en_grupo(n: Node, grupo: String) -> bool:
	var p := n
	while p != null and p != _raiz.get_parent():
		if p.is_in_group(grupo): return true
		p = p.get_parent()
	return false

func _dentro_de_contenedor(o: Object) -> bool:
	var p := o as Node
	while p != null and p != _raiz:
		if _script_es(p, "Contenedor.cs"): return true
		p = p.get_parent()
	return false

func _dentro_de_personaje(n: Node) -> bool:
	var p := n.get_parent()
	while p != null and p != _raiz:
		if p is CharacterBody3D: return true
		p = p.get_parent()
	return false

func _script_es(n: Node, archivo: String) -> bool:
	var s = n.get_script()
	return s != null and str(s.resource_path).ends_with(archivo)

func _nombre_de(o: Object) -> String:
	if not (o is Node): return str(o)
	var n := o as Node
	# Los cuerpos temporales de la auditoría cuelgan de su malla: se nombra la malla
	if n is CollisionObject3D and (n as CollisionObject3D).collision_layer == CAPA_AUDITORIA:
		n = n.get_parent()
	return _ruta(n)

func _ruta(n: Node) -> String:
	return str(_raiz.get_path_to(n))

func _agregar(tipo: String, n: Node, detalle: String) -> void:
	var ruta := _ruta(n)
	hallazgos.append({"tipo": tipo, "zona": ruta.get_slice("/", 0), "nodo": ruta, "detalle": detalle})

## Reporte en Markdown, agrupado por tipo
static func reporte(lista: Array[Dictionary], escena: String) -> String:
	var tipos := ["Flotando", "Enterrado", "Encimados", "Sin colisión", "Colisión invisible", "Fuera del mapa", "Escala no uniforme", "Escala dispareja", "Inalcanzable", "Aviso"]
	var texto := "# Auditoría de %s\n\n%s\n\n" % [escena, Time.get_datetime_string_from_system(false, true)]
	texto += "| Tipo | Cantidad |\n|---|---|\n"
	for t in tipos:
		texto += "| %s | %d |\n" % [t, lista.filter(func(h): return h.tipo == t).size()]
	for t in tipos:
		var de_tipo := lista.filter(func(h): return h.tipo == t)
		if de_tipo.is_empty(): continue
		texto += "\n## %s (%d)\n\n| Zona | Nodo | Detalle |\n|---|---|---|\n" % [t, de_tipo.size()]
		for h in de_tipo:
			texto += "| %s | `%s` | %s |\n" % [h.zona, h.nodo, h.detalle]
	return texto
