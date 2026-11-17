extends SceneTree
## Pega cada cartel (CartelPared) a la superficie que tiene detrás: ni flotando delante ni hundido en la pared.
## Mide con rayos la pared (o la tabla del letrero de poste) bajo varios puntos del cartel y lo acerca o lo aleja
## para que su parte de atrás quede a 2 mm de lo que más sobresale. Escribe las posiciones nuevas en las escenas
## donde está cada cartel (la zona, no el escenario). Solo carteles parados con fondo; los acostados no se tocan.
## Uso: --headless --path game --script res://Herramientas/apoyar_carteles.gd [-- --probar]
## «--probar» solo informa qué movería.

const ESCENARIOS := [
	"res://Escenas/Caso1/escenario_campamento.tscn",
	"res://Escenas/Caso1/Lago/escenario_lago.tscn",
	"res://Escenas/Caso1/Interrogatorio/sala_interrogatorio_caso1.tscn",
]
const SEPARACION := 0.002
## Hasta dónde se busca la pared por delante del cartel: uno metido del todo en un muro de troncos queda detrás de su cara
const ALCANCE_FRENTE := 0.6
const LETRERO_POSTE := "sign.glb"

func _initialize() -> void:
	var probar := OS.get_cmdline_user_args().has("--probar")
	root.get_node("GestorPartida").set("Rejugando", true)
	var cambios := {}   # escena → [[ruta del nodo, posición local nueva]]
	for ruta in ESCENARIOS:
		var escena: Node3D = load(ruta).instantiate()
		root.add_child(escena)
		var todos := escena.find_children("*", "Node3D", true, false).filter(func(n): return n.get_script() != null and str(n.get_script().resource_path).ends_with("CartelPared.cs"))
		# Muchas piezas de las cabañas (ventanas, marcos, tablones) se ven pero no chocan: para medir contra lo que
		# se VE, las mallas cercanas a un cartel reciben una colisión temporal (no se guarda nada del escenario)
		_colisiones_temporales(escena, todos)
		for i in 4: await physics_frame
		for c in todos:
			if not c.get("ConFondo") or absf(c.global_basis.z.normalized().y) > 0.7: continue
			var nuevo = _posicion_apoyada(escena, c)
			if nuevo == null: continue
			var mover: float = (nuevo - c.global_position).dot(c.global_basis.z.normalized())
			if absf(mover) < 0.004: continue
			# Más de 40 cm hacia adelante: lo que encontró enfrente es un mueble, no la pared
			if mover > 0.4:
				print("  sin tocar %s: lo de enfrente está a %.2f m" % [c.name, mover])
				continue
			var duenio: Node = c.owner if c.owner != null else escena
			var archivo: String = duenio.scene_file_path
			# Solo escenas del Caso 1: lo que viene del Caso 0 (el ambiente de la sala) se queda como está
			if not archivo.begins_with("res://Escenas/Caso1/"): continue
			var local: Vector3 = (c.get_parent() as Node3D).global_transform.affine_inverse() * nuevo
			print("%-60s %+.3f m  (%s)" % [archivo.get_file() + ":" + str(duenio.get_path_to(c)), mover, "se despega" if mover > 0 else "se pega"])
			if not cambios.has(archivo): cambios[archivo] = []
			cambios[archivo].append([str(duenio.get_path_to(c)), local])
		escena.free()
		await process_frame
	if not probar:
		for archivo in cambios: _escribir(archivo, cambios[archivo])
	print("carteles movidos: %d" % cambios.values().reduce(func(a, l): return a + l.size(), 0))
	quit()

func _colisiones_temporales(escena: Node3D, carteles: Array) -> void:
	var zonas := []
	for c in carteles: zonas.append(AABB(c.global_position - Vector3.ONE * 1.5, Vector3.ONE * 3.0))
	for m in escena.find_children("*", "MeshInstance3D", true, false):
		if m.mesh == null or m.has_meta("generado_cartel") or m.get_parent().get_script() != null and str(m.get_parent().get_script().resource_path).ends_with("CartelPared.cs"): continue
		var a: AABB = m.global_transform * m.get_aabb()
		if a.size.length() > 14.0: continue
		for z in zonas:
			if a.intersects(z):
				m.create_trimesh_collision()
				for h in m.get_children():
					if h is StaticBody3D: h.collision_layer = 1
				break

## Dónde debe quedar el origen del cartel; null si no hay nada detrás a menos de 50 cm
func _posicion_apoyada(escena: Node3D, c: Node3D):
	var frente: Vector3 = c.global_basis.z.normalized()
	var x: Vector3 = c.global_basis.x.normalized()
	var y: Vector3 = c.global_basis.y.normalized()
	var caja := _caja_local(c)
	if caja.size == Vector3.ZERO: return null
	var escala: float = c.global_basis.z.length()
	var atras: float = -caja.position.z * escala   # cuánto sale el cartel hacia atrás de su origen
	# Letrero de poste (modelo sin colisión): la cara de la tabla sale del modelo
	var poste := _letrero_cercano(escena, c)
	if poste != null:
		var cara: float = _cara_frontal(poste, frente)
		if cara == INF: return null
		return c.global_position + frente * (cara - (c.global_position - poste.global_position).dot(frente) + atras + SEPARACION)
	var espacio := escena.get_world_3d().direct_space_state
	var mas_cerca := -INF
	for i in 7:
		for j in 7:
			var p: Vector3 = c.global_position + x * (caja.position.x + caja.size.x * (0.06 + 0.88 * i / 6.0)) * escala \
				+ y * (caja.position.y + caja.size.y * (0.06 + 0.88 * j / 6.0)) * escala
			var q := PhysicsRayQueryParameters3D.create(p + frente * ALCANCE_FRENTE, p - frente * 0.5)
			q.collision_mask = 1 | 2
			var r := espacio.intersect_ray(q)
			if r.is_empty() or (r["normal"] as Vector3).dot(frente) < 0.6: continue
			mas_cerca = maxf(mas_cerca, (r["position"] - c.global_position).dot(frente))
	if mas_cerca == -INF: return null
	return c.global_position + frente * (mas_cerca + atras + SEPARACION)

func _caja_local(c: Node3D) -> AABB:
	var caja := AABB()
	var hay := false
	for h in c.get_children():
		if h is MeshInstance3D and h.has_meta("generado_cartel"):
			var a: AABB = h.transform * h.get_aabb()
			caja = a if not hay else caja.merge(a)
			hay = true
	return caja if hay else AABB()

func _letrero_cercano(escena: Node3D, c: Node3D) -> Node3D:
	for n in escena.find_children("*", "Node3D", true, false):
		if not str(n.scene_file_path).ends_with(LETRERO_POSTE): continue
		var d: Vector3 = n.global_position - c.global_position
		if Vector2(d.x, d.z).length() < 0.6 and absf(n.global_basis.z.normalized().dot(c.global_basis.z.normalized())) > 0.9: return n
	return null

## Distancia, sobre «frente», desde el origen del modelo hasta la cara de su tabla que mira hacia el cartel
func _cara_frontal(poste: Node3D, frente: Vector3) -> float:
	var maximo := -INF
	for m in poste.find_children("*", "MeshInstance3D", true, false):
		var a: AABB = m.get_aabb()
		for k in 8:
			var punto: Vector3 = m.global_transform * a.get_endpoint(k)
			maximo = maxf(maximo, (punto - poste.global_position).dot(frente))
	return maximo if maximo > -INF else INF

## Cambia el origen del transform de cada nodo en el .tscn (respeta el resto del archivo tal cual)
func _escribir(archivo: String, lista: Array) -> void:
	var texto := FileAccess.get_file_as_string(archivo)
	for par in lista:
		var ruta: String = par[0]
		var p: Vector3 = par[1]
		var nombre := ruta.get_file()
		var padre := ruta.get_base_dir()
		var cabecera := '[node name="%s"' % nombre
		var desde := 0
		var inicio := -1
		while true:
			var i := texto.find(cabecera, desde)
			if i < 0: break
			var fin_cab := texto.find("]", i)
			var cab := texto.substr(i, fin_cab - i)
			var su_padre := "." if padre == "" else padre
			if cab.contains('parent="%s"' % su_padre) or (su_padre == "." and not cab.contains("parent=")):
				inicio = i
				break
			desde = fin_cab
		if inicio < 0:
			push_warning("No encontré %s en %s" % [ruta, archivo])
			continue
		var siguiente := texto.find("\n[", inicio + 1)
		if siguiente < 0: siguiente = texto.length()
		var bloque := texto.substr(inicio, siguiente - inicio)
		var rx := RegEx.create_from_string("transform = Transform3D\\(([^)]*)\\)")
		var m := rx.search(bloque)
		if m == null:
			push_warning("%s en %s no tiene transform" % [ruta, archivo])
			continue
		var n := m.get_string(1).split(",")
		n[9] = " " + _num(p.x)
		n[10] = " " + _num(p.y)
		n[11] = " " + _num(p.z)
		var nuevo_bloque := bloque.replace(m.get_string(0), "transform = Transform3D(%s)" % ",".join(n).strip_edges())
		texto = texto.substr(0, inicio) + nuevo_bloque + texto.substr(siguiente)
	var f := FileAccess.open(archivo, FileAccess.WRITE)
	f.store_string(texto)
	f.close()

func _num(v: float) -> String:
	return str(snappedf(v, 0.0001))
