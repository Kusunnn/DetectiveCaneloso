extends RefCounted
## Capítulo 2: ayudas para armar los escenarios por código (los generadores generar_*.gd las usan).
## Cuartos con el kit modular de Assets/Caso2/Modelos/kit (pisos y techos con MultiMesh), muebles de librería
## apoyados en su superficie, pistas, salidas, luces y lo común de una escena jugable (HUD, jugador, menú).
## Medidas en metros. Convención de los cuartos: la esquina noroeste del piso está en `origen`; X = ancho
## (oeste → este) y Z = fondo (norte → sur).

const KIT := "res://Assets/Caso2/Modelos/kit/"
const PREFAB := "res://Assets/Caso1/Modelos/_prefabs/"
const TEX := "res://Assets/Pistas/Modelos/texturas/"
const MAT := "res://Assets/Caso2/Materiales/"
const MALLAS := "res://Assets/Caso2/Mallas/"

const CAPA_MUNDO := 1
const CAPA_MUEBLES := 2
const CAPA_INTERACTUABLE := 8
const CAPA_NPC := 16
const CAPA_LIMITES := 64

var raiz: Node3D
var nombre_escena: String
var _contador := {}
var _mallas := {}

func _init(r: Node3D, nombre: String) -> void:
	raiz = r
	nombre_escena = nombre
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(MAT))
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(MALLAS + nombre))

# ---------- Nodos ----------

func nodo(nombre: String, padre: Node = null, pos := Vector3.ZERO, rot_y := 0.0) -> Node3D:
	var n := Node3D.new()
	n.name = nombre
	n.position = pos
	n.rotation_degrees.y = rot_y
	(padre if padre else raiz).add_child(n)
	return n

func marcador(nombre: String, padre: Node, pos: Vector3, rot_y := 0.0) -> Marker3D:
	var m := Marker3D.new()
	m.name = nombre
	m.position = pos
	m.rotation_degrees.y = rot_y
	padre.add_child(m)
	return m

func _unico(base: String) -> String:
	_contador[base] = _contador.get(base, 0) + 1
	return base if _contador[base] == 1 else "%s%d" % [base, _contador[base]]

## Instancia un modelo (glb o prefab). `pos` es donde va su base; la rotación se redondea a 15°.
func modelo(ruta: String, padre: Node, pos: Vector3, rot_y := 0.0, escala := 1.0, nombre := "") -> Node3D:
	var escena: PackedScene = load(ruta)
	assert(escena != null, "No existe " + ruta)
	var n: Node3D = escena.instantiate()
	n.name = nombre if nombre != "" else _unico(ruta.get_file().get_basename().to_pascal_case())
	n.position = pos
	n.rotation_degrees.y = snappedf(rot_y, 15.0)
	if escala != 1.0:
		n.scale = Vector3.ONE * escala
	padre.add_child(n)
	return n

func prefab(nombre: String, padre: Node, pos: Vector3, rot_y := 0.0, escala := 1.0, nombre_nodo := "") -> Node3D:
	return modelo(PREFAB + nombre + ".tscn", padre, pos, rot_y, escala, nombre_nodo)

## Caja de colisión invisible (solo colisión; nunca se ve)
func colision(nombre: String, padre: Node, centro: Vector3, tam: Vector3, capa := CAPA_MUNDO, rot_y := 0.0) -> StaticBody3D:
	var c := StaticBody3D.new()
	c.name = nombre
	c.collision_layer = capa
	c.collision_mask = 0
	c.position = centro
	c.rotation_degrees.y = rot_y
	var f := CollisionShape3D.new()
	f.name = "Forma"
	var s := BoxShape3D.new()
	s.size = tam
	f.shape = s
	c.add_child(f)
	padre.add_child(c)
	return c

# ---------- Materiales ----------

func _acg(id: String, mapa: String) -> Texture2D:
	return load(TEX + "%s/%s_%s.jpg" % [id, id, mapa])

func material(nombre: String, props: Dictionary) -> StandardMaterial3D:
	var ruta := MAT + nombre + ".tres"
	var m := StandardMaterial3D.new()
	m.resource_name = nombre
	for k in props:
		m.set(k, props[k])
	ResourceSaver.save(m, ruta)
	m.take_over_path(ruta)
	return m

## Color liso con una veta de ruido suave (para muros pintados, madera estilizada, telas)
func mat_color(nombre: String, color: Color, rugosidad := 0.85, veta := Vector2(1, 1), fuerza := 0.08, metal := 0.0) -> StandardMaterial3D:
	var ruido := FastNoiseLite.new()
	ruido.seed = hash(nombre) % 1000
	ruido.frequency = 0.012
	ruido.fractal_octaves = 4
	var tex := NoiseTexture2D.new()
	tex.width = 256
	tex.height = 256
	tex.seamless = true
	tex.noise = ruido
	var grad := Gradient.new()
	grad.set_color(0, color.darkened(fuerza))
	grad.set_color(1, color.lightened(fuerza * 0.6))
	tex.color_ramp = grad
	return material(nombre, {
		"albedo_texture": tex, "roughness": rugosidad, "metallic": metal,
		"uv1_scale": Vector3(veta.x, veta.y, 1),
	})

func mat_textura(nombre: String, id: String, escala: float, tinte := Color.WHITE, metal := false, triplanar := true) -> StandardMaterial3D:
	var props := {
		"albedo_texture": _acg(id, "Color"), "albedo_color": tinte,
		"normal_enabled": true, "normal_texture": _acg(id, "NormalGL"),
		"roughness_texture": _acg(id, "Roughness"), "roughness": 1.0,
		"uv1_triplanar": triplanar, "uv1_scale": Vector3.ONE * escala,
	}
	if metal:
		props["metallic"] = 0.8
	return material(nombre, props)

func mat_emisivo(nombre: String, color: Color, energia := 2.0) -> StandardMaterial3D:
	return material(nombre, {"albedo_color": color, "emission_enabled": true, "emission": color, "emission_energy_multiplier": energia})

## Malla del kit con sus materiales cambiados por nombre ({"Muro": mat, "Zocalo": mat…}). Se guarda en .res.
func malla_kit(pieza: String, materiales: Dictionary, sufijo := "") -> Mesh:
	var clave := pieza + sufijo
	if _mallas.has(clave):
		return _mallas[clave]
	var escena: PackedScene = load(KIT + pieza + ".glb")
	var inst := escena.instantiate()
	var mi: MeshInstance3D = inst.find_children("*", "MeshInstance3D", true, false)[0]
	var malla: ArrayMesh = mi.mesh.duplicate()
	inst.free()
	for i in malla.get_surface_count():
		var actual := malla.surface_get_material(i)
		var nombre := actual.resource_name if actual else ""
		if materiales.has(nombre):
			malla.surface_set_material(i, materiales[nombre])
	var ruta := MALLAS + nombre_escena + "/" + clave + ".res"
	ResourceSaver.save(malla, ruta)
	malla.take_over_path(ruta)
	_mallas[clave] = malla
	return malla

## Varias copias de una malla (pisos, techos, muros…) unidas en una sola malla estática (.res): una llamada de
## dibujo por material. (Un MultiMesh hecho sin ventana pierde sus transformaciones al guardarse.)
func multimalla(nombre: String, padre: Node, malla: Mesh, transformaciones: Array, sombras := true) -> MeshInstance3D:
	var unida := ArrayMesh.new()
	for s in malla.get_surface_count():
		var st := SurfaceTool.new()
		st.begin(Mesh.PRIMITIVE_TRIANGLES)
		for t in transformaciones:
			st.append_from(malla, s, t)
		st.set_material(malla.surface_get_material(s))
		st.commit(unida)
		unida.surface_set_material(unida.get_surface_count() - 1, malla.surface_get_material(s))
	var ruta := MALLAS + nombre_escena + "/" + _unico(padre.name + "_" + nombre) + ".res"
	ResourceSaver.save(unida, ruta)
	unida.take_over_path(ruta)
	var n := MeshInstance3D.new()
	n.name = nombre
	n.mesh = unida
	n.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON if sombras else GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	padre.add_child(n)
	return n

# ---------- Cuartos ----------

## Cuarto completo: piso, techo, muros con huecos, esquineros y colisiones.
## `huecos` = {"norte": {indice: "ventana"|"ventanal"|"puerta"|"nada"}, "sur": …, "oeste": …, "este": …}
## El índice es el metro del muro contando desde la esquina noroeste (norte/sur) o desde el norte (oeste/este).
## Devuelve el nodo del cuarto; las hojas de puerta y las persianas las pone la escena.
func cuarto(nombre: String, origen: Vector3, ancho: int, fondo: int, alto: float, huecos: Dictionary,
		mats_muro: Dictionary, malla_piso: Mesh, mats_techo: Dictionary, con_techo := true) -> Node3D:
	var c := nodo(nombre, raiz, origen)
	var a := int(round(alto * 100))
	var piso_t := []
	for i in ancho:
		for j in fondo:
			piso_t.append(Transform3D(Basis.IDENTITY, Vector3(i + 0.5, 0, j + 0.5)))
	multimalla("Piso", c, malla_piso, piso_t, false)
	colision("ColisionPiso", c, Vector3(ancho / 2.0, -0.1, fondo / 2.0), Vector3(ancho + 0.4, 0.2, fondo + 0.4))
	if con_techo:
		multimalla("Techo", c, malla_kit("techo_panel", mats_techo), piso_t.map(func(t): return Transform3D(Basis.IDENTITY, t.origin + Vector3(0, alto, 0))), false)
		colision("ColisionTecho", c, Vector3(ancho / 2.0, alto + 0.1, fondo / 2.0), Vector3(ancho + 0.4, 0.2, fondo + 0.4), CAPA_LIMITES)
	# Lados: posición del metro i, giro (la cara interior mira al cuarto) y largo
	var lados := {
		"norte": [func(i): return Vector3(i + 0.5, 0, 0), 0.0, ancho],
		"sur": [func(i): return Vector3(i + 0.5, 0, fondo), 180.0, ancho],
		"oeste": [func(i): return Vector3(0, 0, i + 0.5), 90.0, fondo],
		"este": [func(i): return Vector3(ancho, 0, i + 0.5), -90.0, fondo],
	}
	var por_pieza := {}
	var muros := nodo("Muros", c)
	for lado in lados:
		var pos_de: Callable = lados[lado][0]
		var giro: float = lados[lado][1]
		var largo: int = lados[lado][2]
		var h: Dictionary = huecos.get(lado, {})
		for i in largo:
			var tipo: String = h.get(i, "muro")
			if tipo == "nada":
				continue
			var pieza: String = {"muro": "muro_%d", "ventana": "muro_ventana_%d", "ventanal": "ventanal_%d", "puerta": "muro_puerta_%d"}[tipo] % a
			var t := Transform3D(Basis(Vector3.UP, deg_to_rad(giro)), pos_de.call(i))
			por_pieza.get_or_add(pieza, []).append(t)
			# Colisión del módulo (en la puerta solo el dintel)
			var centro_local := Vector3(0, alto / 2.0, -0.06)
			var tam := Vector3(1.0, alto, 0.12)
			if tipo == "puerta":
				centro_local = Vector3(0, (alto + 2.1) / 2.0, -0.06)
				tam = Vector3(1.0, alto - 2.1, 0.12)
				for lado_x in [-0.475, 0.475]:
					colision("ColisionJamba_%s%d" % [lado, i], muros, t * Vector3(lado_x, 1.05, -0.06), Vector3(0.05, 2.1, 0.12), CAPA_MUNDO, giro)
			colision("Colision_%s%d" % [lado, i], muros, t * centro_local, tam, CAPA_MUNDO, giro)
	for pieza in por_pieza:
		multimalla(pieza.to_pascal_case(), muros, malla_kit(pieza, mats_muro), por_pieza[pieza])
	var esq := []
	for p in [Vector3(0, 0, 0), Vector3(ancho, 0, 0), Vector3(0, 0, fondo), Vector3(ancho, 0, fondo)]:
		esq.append(Transform3D(Basis.IDENTITY, p))
	multimalla("Esquineros", muros, malla_kit("esquinero_%d" % a, mats_muro), esq)
	return c

## Muro interior suelto (divisiones): `largo` módulos desde `inicio`, mirando a `giro`
func tramo_muro(nombre: String, padre: Node, inicio: Vector3, largo: int, giro: float, alto: float, mats_muro: Dictionary, huecos := {}) -> Node3D:
	var n := nodo(nombre, padre)
	var a := int(round(alto * 100))
	var por_pieza := {}
	var dir := Basis(Vector3.UP, deg_to_rad(giro)) * Vector3.RIGHT
	for i in largo:
		var tipo: String = huecos.get(i, "muro")
		var pieza: String = {"muro": "muro_%d", "ventana": "muro_ventana_%d", "ventanal": "ventanal_%d", "puerta": "muro_puerta_%d"}[tipo] % a
		var t := Transform3D(Basis(Vector3.UP, deg_to_rad(giro)), inicio + dir * (i + 0.5))
		por_pieza.get_or_add(pieza, []).append(t)
		if tipo != "puerta":
			colision("Colision%d" % i, n, t * Vector3(0, alto / 2.0, -0.06), Vector3(1.0, alto, 0.12), CAPA_MUNDO, giro)
	for pieza in por_pieza:
		multimalla(pieza.to_pascal_case(), n, malla_kit(pieza, mats_muro), por_pieza[pieza])
	return n

# ---------- Interacción ----------

func _forma_de(n: Node3D, margen := 0.01) -> Array:
	# Caja que envuelve las mallas visibles de `n`, en coordenadas de `n`
	var caja := AABB()
	var primera := true
	for mi in n.find_children("*", "VisualInstance3D", true, false):
		var vi := mi as VisualInstance3D
		var t := _transformacion_relativa(n, vi)
		var b := t * vi.get_aabb()
		caja = b if primera else caja.merge(b)
		primera = false
	return [caja.get_center(), caja.size + Vector3.ONE * margen * 2]

func _transformacion_relativa(desde: Node3D, hasta: Node3D) -> Transform3D:
	var t := Transform3D.IDENTITY
	var n: Node = hasta
	while n != desde and n != null:
		t = (n as Node3D).transform * t
		n = n.get_parent()
	return t

## Altura (en coordenadas de la escena) de la cara de arriba de un mueble: para apoyar cosas encima
func tope(n: Node3D) -> float:
	var forma := _forma_de(n, 0.0)
	var t := _transformacion_relativa(raiz, n)
	var caja := AABB(forma[0] - forma[1] / 2.0, forma[1])
	return (t * caja).end.y

## Caja (en coordenadas de la escena) que ocupa un nodo
func caja_de(n: Node3D) -> AABB:
	var forma := _forma_de(n, 0.0)
	return _transformacion_relativa(raiz, n) * AABB(forma[0] - forma[1] / 2.0, forma[1])

## Pista: StaticBody3D con ObjetoPista en la capa de interacción. `visual` es el modelo (instancia o nodo).
func pista(padre: Node, id: String, titulo: String, descripcion: String, comentario: String, lugar: String,
		visual: Node3D, pos: Vector3, rot_y := 0.0, extra := {}) -> StaticBody3D:
	var p := StaticBody3D.new()
	p.name = "Pista_" + id
	p.set_script(load("res://Escenas/Interrogatorio/ObjetoPista.cs"))
	p.collision_layer = CAPA_INTERACTUABLE
	p.collision_mask = 0
	p.position = pos
	p.rotation_degrees.y = rot_y
	padre.add_child(p)
	visual.name = "Modelo"
	p.add_child(visual)
	var forma := _forma_de(p, 0.015)
	var f := CollisionShape3D.new()
	f.name = "Forma"
	var s := BoxShape3D.new()
	s.size = Vector3(maxf(forma[1].x, 0.08), maxf(forma[1].y, 0.04), maxf(forma[1].z, 0.08))
	f.shape = s
	f.position = forma[0]
	p.add_child(f)
	p.set("Id", id)
	p.set("Titulo", titulo)
	p.set("Descripcion", descripcion)
	p.set("ComentarioAlMirar", comentario)
	p.set("Lugar", lugar)
	for k in extra:
		p.set(k, extra[k])
	return p

## Punto de interacción (salida, elevador, panel…): solo emite Usado; la escena decide.
func punto(padre: Node, nombre: String, accion: String, centro: Vector3, tam: Vector3, rot_y := 0.0) -> StaticBody3D:
	var p := StaticBody3D.new()
	p.name = nombre
	p.set_script(load("res://Escenas/Investigacion/PuntoInteraccion.cs"))
	p.set("Accion", accion)
	p.collision_layer = CAPA_INTERACTUABLE
	p.collision_mask = 0
	p.position = centro
	p.rotation_degrees.y = rot_y
	p.add_to_group("auditoria_invisible_ok", true)
	var f := CollisionShape3D.new()
	f.name = "Forma"
	var s := BoxShape3D.new()
	s.size = tam
	f.shape = s
	p.add_child(f)
	padre.add_child(p)
	return p

# ---------- Luces ----------

func omni(padre: Node, nombre: String, pos: Vector3, color: Color, energia: float, rango: float, sombra := false) -> OmniLight3D:
	var l := OmniLight3D.new()
	l.name = nombre
	l.position = pos
	l.light_color = color
	l.light_energy = energia
	l.omni_range = rango
	l.omni_attenuation = 1.2
	l.shadow_enabled = sombra
	padre.add_child(l)
	return l

func foco(padre: Node, nombre: String, pos: Vector3, mirar: Vector3, color: Color, energia: float, rango: float, angulo: float, sombra := false) -> SpotLight3D:
	var l := SpotLight3D.new()
	l.name = nombre
	padre.add_child(l)
	l.position = pos
	l.look_at_from_position(pos, mirar, Vector3.UP if absf((mirar - pos).normalized().y) < 0.99 else Vector3.FORWARD)
	l.light_color = color
	l.light_energy = energia
	l.spot_range = rango
	l.spot_angle = angulo
	l.spot_attenuation = 0.8
	l.shadow_enabled = sombra
	return l

func sol(padre: Node, nombre: String, elevacion: float, azimut: float, color: Color, energia: float) -> DirectionalLight3D:
	var l := DirectionalLight3D.new()
	l.name = nombre
	l.rotation_degrees = Vector3(-elevacion, azimut, 0)
	l.light_color = color
	l.light_energy = energia
	l.shadow_enabled = true
	l.directional_shadow_mode = DirectionalLight3D.SHADOW_PARALLEL_2_SPLITS
	l.directional_shadow_max_distance = 30.0
	padre.add_child(l)
	return l

func entorno(cielo_arriba: Color, cielo_horizonte: Color, ambiente: Color, energia_ambiente: float, exposicion := 1.0) -> WorldEnvironment:
	var we := WorldEnvironment.new()
	we.name = "Entorno"
	var env := Environment.new()
	var cielo := Sky.new()
	var mc := ProceduralSkyMaterial.new()
	mc.sky_top_color = cielo_arriba
	mc.sky_horizon_color = cielo_horizonte
	mc.ground_horizon_color = cielo_horizonte.darkened(0.2)
	mc.ground_bottom_color = cielo_horizonte.darkened(0.5)
	cielo.sky_material = mc
	env.background_mode = Environment.BG_SKY
	env.sky = cielo
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = ambiente
	env.ambient_light_energy = energia_ambiente
	env.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	env.tonemap_exposure = exposicion
	env.ssao_enabled = true
	env.ssao_radius = 0.6
	env.ssao_intensity = 1.2
	env.glow_enabled = true
	env.glow_intensity = 0.35
	env.glow_bloom = 0.04
	env.adjustment_enabled = true
	env.adjustment_saturation = 1.05
	we.environment = env
	raiz.add_child(we)
	return we

# ---------- Piezas del kit ----------

const KENNEY := "res://Assets/Caso2/Modelos/kenney/"
const ESCALA_MUEBLES := 2.2   # furniture-kit (la misma que los prefabs del Caso 1)
const ESCALA_COCHES := 1.65
const ESCALA_EDIFICIOS := 10.0
const ESCALA_CALLE := 6.0

func kenney(pack: String, pieza: String, padre: Node, pos: Vector3, rot_y := 0.0, escala := -1.0, nombre := "") -> Node3D:
	if escala < 0:
		escala = {"furniture-kit": ESCALA_MUEBLES, "car-kit": ESCALA_COCHES, "city-kit-commercial": ESCALA_EDIFICIOS,
			"city-kit-roads": ESCALA_CALLE}.get(pack, 1.0)
	return modelo(KENNEY + pack + "/" + pieza + ".glb", padre, pos, rot_y, escala, nombre)

## Pieza del kit modular como nodo suelto (hojas de puerta, persianas, cortinas metálicas)
func pieza_kit(pieza: String, padre: Node, pos: Vector3, rot_y := 0.0, materiales := {}, nombre := "") -> MeshInstance3D:
	var n := MeshInstance3D.new()
	n.name = nombre if nombre != "" else _unico(pieza.to_pascal_case())
	n.mesh = malla_kit(pieza, materiales, "_" + str(materiales.hash()) if not materiales.is_empty() else "")
	n.position = pos
	n.rotation_degrees.y = rot_y
	padre.add_child(n)
	return n

## Posición del metro `i` de un lado del cuarto (centro del módulo, en el piso) y el giro de su cara interior
func en_muro(cuarto_origen: Vector3, ancho: int, fondo: int, lado: String, i: int) -> Array:
	match lado:
		"norte": return [cuarto_origen + Vector3(i + 0.5, 0, 0), 0.0]
		"sur": return [cuarto_origen + Vector3(i + 0.5, 0, fondo), 180.0]
		"oeste": return [cuarto_origen + Vector3(0, 0, i + 0.5), 90.0]
		_: return [cuarto_origen + Vector3(ancho, 0, i + 0.5), -90.0]

## Persiana veneciana delante de la ventana del módulo (en el lado interior)
func persiana(padre: Node, pos_muro: Vector3, giro: float, materiales := {}) -> MeshInstance3D:
	var b := Basis(Vector3.UP, deg_to_rad(giro))
	return pieza_kit("persiana", padre, pos_muro + b * Vector3(0, 0.9, 0.05), giro, materiales)

## Puerta de salida a otro escenario: la hoja cerrada en el hueco del muro y un punto de interacción delante
func puerta_salida(padre: Node, nombre: String, accion: String, pos_muro: Vector3, giro: float, hoja := "puerta_hoja", materiales := {}) -> StaticBody3D:
	var b := Basis(Vector3.UP, deg_to_rad(giro))
	pieza_kit(hoja, padre, pos_muro + b * Vector3(-0.43, 0.01, -0.06), giro, materiales, nombre + "Hoja")
	return punto(padre, nombre, accion, pos_muro + b * Vector3(0, 1.05, 0.12), Vector3(0.9, 2.1, 0.2), giro)

## Calle de mañana vista por una ventana: banqueta, calle, coches, postes y edificios en tres capas.
## `base` es el punto de la banqueta pegado a la fachada; `frente` es hacia dónde se mira desde dentro (en grados).
func vista_calle(padre: Node, base: Vector3, frente: float, largo := 40.0, semilla := 1, altura_ojo := 0.0) -> Node3D:
	var v := nodo("VistaCalle", padre, base, frente)
	v.add_to_group("auditoria_fuera_ok", true)
	var azar := RandomNumberGenerator.new()
	azar.seed = semilla
	var mitad := int(largo / 2.0 / ESCALA_CALLE)
	# La vista mira hacia -Z local. Banqueta (0..-3 m), calle (-3..-15), banqueta de enfrente y edificios.
	for i in range(-mitad, mitad + 1):
		var x := i * ESCALA_CALLE
		kenney("city-kit-roads", "tile-low", v, Vector3(x, -0.02 - altura_ojo, -3.0), 0.0, ESCALA_CALLE, "Banqueta%d" % i)
		kenney("city-kit-roads", "road-straight", v, Vector3(x, -0.06 - altura_ojo, -9.0), 90.0, ESCALA_CALLE, "Calle%d" % i)
		kenney("city-kit-roads", "road-straight", v, Vector3(x, -0.06 - altura_ojo, -15.0), 90.0, ESCALA_CALLE, "Calle%db" % i)
		kenney("city-kit-roads", "tile-low", v, Vector3(x, -0.02 - altura_ojo, -21.0), 0.0, ESCALA_CALLE, "BanquetaEnfrente%d" % i)
		if i % 2 == 0:
			kenney("city-kit-roads", "light-square", v, Vector3(x + 1.5, -altura_ojo, -4.6), 180.0, 8.0, "Poste%d" % i)
	var coches := ["sedan", "taxi", "suv", "van", "hatchback-sports", "delivery"]
	for i in 5:
		var x := azar.randf_range(-largo / 2.0, largo / 2.0)
		kenney("car-kit", coches[azar.randi() % coches.size()], v, Vector3(x, -altura_ojo, -7.2 if i % 2 == 0 else -16.5), 90.0 if i % 2 == 0 else -90.0, ESCALA_COCHES, "Coche%d" % i)
	var edificios := ["building-a", "building-b", "building-c", "building-d", "building-e", "building-f", "building-g",
		"building-h", "building-i", "building-j", "building-k", "building-l", "building-m", "building-n"]
	var x0 := -largo / 2.0 - 6.0
	var n := 0
	while x0 < largo / 2.0 + 6.0:
		var e: String = edificios[azar.randi() % edificios.size()]
		kenney("city-kit-commercial", e, v, Vector3(x0 + 5.0, -altura_ojo, -29.0), 180.0, ESCALA_EDIFICIOS, "Edificio%d" % n)
		x0 += 10.0
		n += 1
	for i in 7:
		var e: String = ["building-skyscraper-a", "building-skyscraper-b", "building-skyscraper-c", "building-skyscraper-d", "building-skyscraper-e"][azar.randi() % 5]
		kenney("city-kit-commercial", e, v, Vector3(-48.0 + i * 16.0 + azar.randf_range(-3, 3), -altura_ojo, -55.0 - azar.randf_range(0, 15)), azar.randi_range(0, 3) * 90.0, ESCALA_EDIFICIOS * azar.randf_range(0.9, 1.3), "Torre%d" % i)
	for i in 10:
		kenney("city-kit-commercial", "low-detail-building-%s" % "abcdefghijklmn"[azar.randi() % 14], v, Vector3(-90.0 + i * 20.0, -altura_ojo, -110.0 - azar.randf_range(0, 20)), 0.0, 22.0, "Lejano%d" % i)
	# Sin sombras: la vista es fondo
	for g in v.find_children("*", "GeometryInstance3D", true, false):
		(g as GeometryInstance3D).cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	return v

## Pantalla encendida (noticias, directorio, monitores): una cara plana con la imagen emisiva; mira a +Z local
func pantalla(padre: Node, nombre: String, pos: Vector3, rot_y: float, tam: Vector2, textura: String, energia := 1.0, rot_x := 0.0) -> MeshInstance3D:
	var m := StandardMaterial3D.new()
	var tex: Texture2D = load(textura)
	m.albedo_texture = tex
	m.emission_enabled = true
	m.emission_texture = tex
	m.emission_energy_multiplier = energia
	m.roughness = 0.2
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	st.set_material(m)
	var a := Vector3(-tam.x / 2, -tam.y / 2, 0)
	var b := Vector3(tam.x / 2, -tam.y / 2, 0)
	var c := Vector3(tam.x / 2, tam.y / 2, 0)
	var d := Vector3(-tam.x / 2, tam.y / 2, 0)
	for v in [[a, Vector2(0, 1)], [c, Vector2(1, 0)], [b, Vector2(1, 1)], [a, Vector2(0, 1)], [d, Vector2(0, 0)], [c, Vector2(1, 0)]]:
		st.set_normal(Vector3.BACK)
		st.set_uv(v[1])
		st.add_vertex(v[0])
	var malla := st.commit()
	var ruta := MALLAS + nombre_escena + "/" + _unico("pantalla_" + nombre.to_snake_case()) + ".res"
	ResourceSaver.save(malla, ruta)
	malla.take_over_path(ruta)
	var n := MeshInstance3D.new()
	n.name = nombre
	n.mesh = malla
	n.position = pos
	n.rotation_degrees = Vector3(rot_x, rot_y, 0)
	n.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	padre.add_child(n)
	return n

## Persona de fondo sin modelo propio: el maniquí de Quaternius pintado (FiguraFondo). MARCADOR TEMPORAL.
func figura_fondo(padre: Node, nombre: String, pos: Vector3, rot_y: float, color: Color, animacion := "Idle", escala := 0.78, desfase := 0.0) -> Node3D:
	var f := Node3D.new()
	f.name = nombre
	f.position = pos
	f.rotation_degrees.y = rot_y
	f.set_script(load("res://Escenas/Investigacion/FiguraFondo.cs"))
	padre.add_child(f)
	f.set("ColorCuerpo", color)
	f.set("Animacion", animacion)
	f.set("Desfase", desfase)
	f.add_to_group("marcador_temporal", true)
	f.add_to_group("auditoria_personaje", true)
	var m: Node3D = load("res://Assets/Animaciones/Quaternius/UAL1_Standard.glb").instantiate()
	m.name = "Maniqui"
	m.scale = Vector3.ONE * escala
	f.add_child(m)
	colision("Cuerpo", f, Vector3(0, 0.75, 0), Vector3(0.55, 1.5, 0.45), CAPA_NPC).add_to_group("auditoria_invisible_ok", true)
	return f

## Vapor de una taza: partículas suaves que suben y se desvanecen (no proyectan sombra)
func vapor(padre: Node, pos: Vector3) -> CPUParticles3D:
	var p := CPUParticles3D.new()
	p.name = _unico("Vapor")
	p.position = pos
	p.amount = 14
	p.lifetime = 2.2
	p.preprocess = 2.0
	p.emission_shape = CPUParticles3D.EMISSION_SHAPE_SPHERE
	p.emission_sphere_radius = 0.015
	p.direction = Vector3.UP
	p.spread = 12.0
	p.gravity = Vector3(0, 0.02, 0)
	p.initial_velocity_min = 0.05
	p.initial_velocity_max = 0.09
	p.scale_amount_min = 0.6
	p.scale_amount_max = 1.0
	var curva := Curve.new()
	curva.add_point(Vector2(0, 0.4))
	curva.add_point(Vector2(1, 1.6))
	p.scale_amount_curve = curva
	var g := Gradient.new()
	g.set_color(0, Color(1, 1, 1, 0.0))
	g.add_point(0.25, Color(1, 1, 1, 0.22))
	g.set_color(g.get_point_count() - 1, Color(1, 1, 1, 0.0))
	p.color_ramp = g
	var q := QuadMesh.new()
	q.size = Vector2(0.05, 0.05)
	var m := StandardMaterial3D.new()
	m.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	m.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	m.billboard_mode = BaseMaterial3D.BILLBOARD_PARTICLES
	m.vertex_color_use_as_albedo = true
	var tex := GradientTexture2D.new()
	var gt := Gradient.new()
	gt.set_color(0, Color(1, 1, 1, 1))
	gt.set_color(1, Color(1, 1, 1, 0))
	tex.gradient = gt
	tex.fill = GradientTexture2D.FILL_RADIAL
	tex.fill_from = Vector2(0.5, 0.5)
	tex.fill_to = Vector2(1.0, 0.5)
	m.albedo_texture = tex
	q.material = m
	p.mesh = q
	p.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	padre.add_child(p)
	return p

# ---------- Personajes ----------

## Modelos y ajustes de los personajes, copiados de la sala del Caso 1 (sin pose T: reposo por animación)
const PERSONAJES := {
	"Hecdog": {"ruta": "res://Assets/Models/poli/Poli/Neutral Idle.fbx", "escala": 380.0, "desfase": Vector3(0, 0.004, 0), "anim": "mixamo_com", "ojos": 1.45, "variantes": []},
	"Miawrio": {"ruta": "res://Assets/Models/Miaurio/miaurio.tscn", "escala": 0.004647348, "desfase": Vector3(-1.5081899, 0.0025420994, -0.47148278), "anim": "global/Inactivo", "ojos": 1.36, "variantes": ["global/Tenso"]},
	"Aya": {"ruta": "res://Assets/Models/Aya/Ayas.tscn", "escala": 0.035906643, "desfase": Vector3(-0.0044524237, 0.00513465, -0.9514183), "anim": "Inaya", "ojos": 1.28, "variantes": []},
	"Mimita": {"ruta": "res://Assets/Models/Personajes/mimita_rig.glb", "escala": 9.74026, "desfase": Vector3(0, 0, -0.0029220781), "anim": "Reposo", "ojos": 1.2, "variantes": ["ReposoMirar", "ReposoPeso"]},
}

## Personaje de pie en `pos` mirando hacia `rot_y` (0 = hacia -Z). Solo cuerpo y colisión de NPC: la plática es de
## otra tarea. `objeto` (opcional) es un nodo que lleva en la mano.
func personaje(nombre: String, padre: Node, pos: Vector3, rot_y: float, objeto: Node3D = null) -> Node3D:
	var datos: Dictionary = PERSONAJES[nombre]
	var p := nodo(nombre, padre, pos, rot_y)
	p.add_to_group("auditoria_personaje", true)
	var m: Node3D = load(datos["ruta"]).instantiate()
	m.name = "Modelo"
	m.scale = Vector3.ONE * datos["escala"]
	m.position = datos["desfase"]
	p.add_child(m)
	var a := Node.new()
	a.name = "Animacion"
	a.set_script(load("res://Escenas/Investigacion/AnimacionPersonaje.cs"))
	p.add_child(a)
	a.set("Modelo", m)
	a.set("AlturaOjos", datos["ojos"])
	a.set("Animacion", datos["anim"])
	a.set("Variantes", PackedStringArray(datos["variantes"]))
	if objeto:
		objeto.name = "ObjetoEnMano"
		p.add_child(objeto)
		a.set("ObjetoEnMano", objeto)
	colision("Cuerpo", p, Vector3(0, 0.8, 0), Vector3(0.6, 1.6, 0.5), CAPA_NPC).add_to_group("auditoria_invisible_ok", true)
	return p

# ---------- Lo común de una escena jugable ----------

## HUD, jugador, menú de pausa, ventana de diálogo, música, viñeta y el nodo EscenarioCaso2
func base_jugable(lugar: String, pos_jugador: Vector3, rot_jugador: float, musica := "res://Assets/Audio/Musica/investigacion.ogg") -> Node:
	var hud: Node = load("res://hud/hud.tscn").instantiate()
	hud.name = "HUD"
	raiz.add_child(hud)
	var jugador: Node3D = load("res://Escenas/Interrogatorio/jugador.tscn").instantiate()
	jugador.name = "Jugador"
	jugador.position = pos_jugador + Vector3(0, 1.0, 0)
	jugador.rotation_degrees.y = rot_jugador
	raiz.add_child(jugador)
	var menu: Node = load("res://Escenas/Menus/menu_pausa.tscn").instantiate()
	menu.name = "MenuPausa"
	raiz.add_child(menu)
	var dialogo := CanvasLayer.new()
	dialogo.name = "VentanaDialogo"
	dialogo.set_script(load("res://Escenas/Investigacion/VentanaDialogo.cs"))
	raiz.add_child(dialogo)
	var m := AudioStreamPlayer.new()
	m.name = "MusicaFondo"
	m.stream = load(musica)
	m.set_script(load("res://Escenas/Investigacion/MusicaFondo.cs"))
	raiz.add_child(m)
	var mando := Node.new()
	mando.name = "MandoHud"
	mando.set_script(load("res://Escenas/UI/MandoHud.cs"))
	raiz.add_child(mando)
	var vineta := CanvasLayer.new()
	vineta.name = "Vineta"
	vineta.layer = 1
	var deg := TextureRect.new()
	deg.name = "Degradado"
	deg.set_anchors_preset(Control.PRESET_FULL_RECT)
	deg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	deg.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	var g := Gradient.new()
	g.set_color(0, Color(0, 0, 0, 0))
	g.set_color(1, Color(0.05, 0.03, 0.02, 0.45))
	g.set_offset(0, 0.62)
	var gt := GradientTexture2D.new()
	gt.gradient = g
	gt.fill = GradientTexture2D.FILL_RADIAL
	gt.fill_from = Vector2(0.5, 0.5)
	gt.fill_to = Vector2(1.0, 1.0)
	deg.texture = gt
	vineta.add_child(deg)
	raiz.add_child(vineta)
	var esc := Node.new()
	esc.name = "EscenarioCaso2"
	esc.set_script(load("res://Escenas/Caso2/EscenarioCaso2.cs"))
	raiz.add_child(esc)
	esc.set("Jugador", jugador)
	esc.set("Hud", hud)
	esc.set("Dialogo", dialogo)
	esc.set("Musica", m)
	esc.set("Lugar", lugar)
	return esc

func ambiente_sonoro(nombre: String, padre: Node, pos: Vector3, ruta: String, volumen_db := -12.0, distancia := 0.0) -> Node:
	var n: Node
	if distancia > 0:
		var p3 := AudioStreamPlayer3D.new()
		p3.position = pos
		p3.max_distance = distancia
		p3.volume_db = volumen_db
		p3.stream = load(ruta)
		p3.autoplay = true
		n = p3
	else:
		var p := AudioStreamPlayer.new()
		p.volume_db = volumen_db
		p.stream = load(ruta)
		p.autoplay = true
		n = p
	n.name = nombre
	padre.add_child(n)
	return n

# ---------- Guardar ----------

func guardar(ruta: String) -> void:
	_duenos(raiz, raiz)
	var ps := PackedScene.new()
	var err := ps.pack(raiz)
	assert(err == OK, "No se pudo empaquetar " + ruta)
	ResourceSaver.save(ps, ruta)
	print("  guardada ", ruta)

func _duenos(r: Node, n: Node) -> void:
	for h in n.get_children():
		h.owner = r
		if h.scene_file_path.is_empty():
			_duenos(r, h)
