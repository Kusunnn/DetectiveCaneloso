extends SceneTree
## Apoya los objetos de una escena en su superficie real y guarda la escena.
##   apoyar_suelo → baja (o sube) el objeto hasta lo que tenga debajo: piso, mesa, repisa, tronco...
##   apoyar_pared → lo empuja hacia atrás (su -Z) hasta tocar la pared, a 5 mm
## Las superficies son las mallas visibles (se convierten en colisión temporal). Si no hay nada debajo, el suelo es Y = 0.
## Uso: <Godot>_console.exe --headless --path game --script res://Herramientas/ajustar_apoyos.gd -- <escena.tscn> [más escenas]

const CAPA := 1 << 19

var _cuerpos := {}

func _init() -> void:
	await process_frame
	for ruta in OS.get_cmdline_user_args():
		await _ajustar(ruta)
	quit()

func _ajustar(ruta: String) -> void:
	var esc: Node3D = load(ruta).instantiate()
	root.add_child(esc)
	_cuerpos.clear()
	for m in esc.find_children("*", "MeshInstance3D", true, false):
		var mi := m as MeshInstance3D
		if mi.mesh == null or not mi.is_visible_in_tree(): continue
		var forma := mi.mesh.create_trimesh_shape()
		if forma == null: continue
		var cuerpo := StaticBody3D.new()
		cuerpo.collision_layer = CAPA
		var cs := CollisionShape3D.new(); cs.shape = forma
		cuerpo.add_child(cs)
		mi.add_child(cuerpo, false, Node.INTERNAL_MODE_BACK)
		_cuerpos[mi] = cuerpo
	for i in 3: await physics_frame
	var espacio := esc.get_world_3d().direct_space_state
	var movidos: Array[Node3D] = []
	# Primero las paredes (cambia X/Z) y luego los apoyos (cambia Y), de lo más bajo a lo más alto,
	# así lo que va encima de una mesa se apoya en la mesa ya acomodada
	for n in get_nodes_in_group("apoyar_pared"):
		if esc.is_ancestor_of(n) and _a_pared(n, espacio): movidos.append(n)
	var suelo := get_nodes_in_group("apoyar_suelo").filter(func(x): return esc.is_ancestor_of(x))
	suelo.sort_custom(func(a, b): return _caja(a).position.y < _caja(b).position.y)
	for n in suelo:
		await physics_frame
		if _a_suelo(n, espacio) and not movidos.has(n): movidos.append(n)
	for c in _cuerpos.values(): c.queue_free()
	# Se cambia solo la línea «transform» de cada nodo movido en el .tscn (volver a empaquetar la escena
	# perdería los nodos agregados dentro de modelos instanciados)
	_escribir_transformaciones(ruta, esc, movidos)
	print("%s: %d objetos acomodados" % [ruta, movidos.size()])
	esc.queue_free()
	await process_frame

func _escribir_transformaciones(ruta: String, esc: Node, movidos: Array[Node3D]) -> void:
	if movidos.is_empty(): return
	var cambios := {}
	for n in movidos:
		var rel := str(esc.get_path_to(n))
		var nombre := rel.get_file() if rel.contains("/") else rel
		var padre := rel.get_base_dir() if rel.contains("/") else "."
		cambios['[node name="%s"' % nombre + "|" + padre] = "transform = " + var_to_str(n.transform)
	var lineas := FileAccess.get_file_as_string(ruta).split("\n")
	var salida := PackedStringArray()
	var pendiente := ""
	for l in lineas:
		if l.begins_with("[node "):
			if pendiente != "": salida.append(pendiente)  # el nodo no tenía transform: se agrega
			pendiente = ""
			var nombre := l.get_slice('"', 1)
			var padre := l.get_slice('parent="', 1).get_slice('"', 0) if l.contains('parent="') else ""
			var clave := '[node name="%s"' % nombre + "|" + padre
			if cambios.has(clave): pendiente = cambios[clave]
			salida.append(l)
			continue
		if pendiente != "" and l.begins_with("transform = "):
			salida.append(pendiente); pendiente = ""
			continue
		if pendiente != "" and l.strip_edges() == "":
			salida.append(pendiente); pendiente = ""
		salida.append(l)
	if pendiente != "": salida.append(pendiente)
	var f := FileAccess.open(ruta, FileAccess.WRITE)
	f.store_string("\n".join(salida))

func _a_suelo(n: Node3D, espacio: PhysicsDirectSpaceState3D) -> bool:
	var caja := _caja(n)
	if caja.size == Vector3.ZERO: return false
	var base := Vector3(caja.get_center().x, caja.position.y, caja.get_center().z)
	var q := PhysicsRayQueryParameters3D.create(base + Vector3.UP * 0.25, base + Vector3.DOWN * 4.0, CAPA, _propios(n))
	q.hit_back_faces = false
	var g := espacio.intersect_ray(q)
	var destino := 0.0 if g.is_empty() else (g.position as Vector3).y
	var d := destino - base.y
	if absf(d) < 0.002: return false
	n.global_position.y += d
	return true

func _a_pared(n: Node3D, espacio: PhysicsDirectSpaceState3D) -> bool:
	var caja := _caja(n)
	if caja.size == Vector3.ZERO: return false
	var atras := -n.global_basis.z.normalized()
	atras.y = 0; atras = atras.normalized()
	var profundidad := absf(atras.x) * caja.size.x * 0.5 + absf(atras.z) * caja.size.z * 0.5
	var centro := caja.get_center()
	var q := PhysicsRayQueryParameters3D.create(centro, centro + atras * 1.5, CAPA, _propios(n))
	var g := espacio.intersect_ray(q)
	if g.is_empty():
		push_warning("Sin pared detrás de " + str(n.get_path()))
		return false
	var d: float = centro.distance_to(g.position) - profundidad - 0.005
	if absf(d) < 0.002: return false
	n.global_position += atras * d
	return true

func _propios(n: Node) -> Array[RID]:
	var lista: Array[RID] = []
	for m in n.find_children("*", "MeshInstance3D", true, false):
		if _cuerpos.has(m): lista.append(_cuerpos[m].get_rid())
	if _cuerpos.has(n): lista.append(_cuerpos[n].get_rid())
	return lista

func _caja(n: Node) -> AABB:
	var caja := AABB(); var primero := true
	var mallas := n.find_children("*", "MeshInstance3D", true, false)
	if n is MeshInstance3D: mallas.append(n)
	for m in mallas:
		var mi := m as MeshInstance3D
		if mi.mesh == null or not mi.is_visible_in_tree(): continue
		var a := mi.global_transform * mi.get_aabb()
		if primero: caja = a; primero = false
		else: caja = caja.merge(a)
	return caja
