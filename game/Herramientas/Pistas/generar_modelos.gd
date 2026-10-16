extends SceneTree
## HU-07: genera los modelos de las pistas hechas en el proyecto (papeles, libros, candado, taza, delantal, leña,
## pluma, envoltorio, huellas, contenido del baúl y palanca del interruptor) con materiales PBR.
## Antes: generar_texturas.gd y un --import. Uso: --headless --path game --script res://Herramientas/Pistas/generar_modelos.gd
## Salida: res://Assets/Pistas/Modelos/<pista>/<pista>.tscn (+ mallas .res) y materiales en Modelos/materiales/.
## Medidas reales en metros; pivote en la base (o atrás, en lo que va colgado de la pared).

const M := preload("res://Herramientas/Pistas/mallas.gd")
const BASE := "res://Assets/Pistas/Modelos/"
const HACHA := "res://Assets/Pistas/Modelos/polyhaven/hatchet/hatchet_1k.gltf"

var _azar := RandomNumberGenerator.new()

func _initialize() -> void:
	_azar.seed = 2026
	for carpeta in ["materiales", "nota_comisario", "nota_porky", "registro_llamadas", "recorte_periodico", "oferta_compra",
			"libro_guardia_quemado", "libro_turnos", "registro_enfermeria", "envoltorio", "pluma", "candado_caseta",
			"taza_delantal", "delantal", "lena_intacta", "huellas", "baul_disfraces", "interruptor_generador"]:
		DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(BASE + carpeta))
	_nota_comisario()
	_nota_porky()
	_registro_llamadas()
	_recorte()
	_oferta()
	_libro_quemado()
	_libro_turnos()
	_registro_enfermeria()
	_envoltorio()
	_pluma()
	_candado()
	_taza()
	_delantal()
	_lena()
	_huellas()
	_baul_contenido()
	_palanca()
	_materiales_poly_haven()
	print("Modelos listos")
	quit()

# ---------- Utilidades ----------

func _acg(id: String, mapa: String) -> Texture2D:
	return load(BASE + "texturas/%s/%s_%s.jpg" % [id, id, mapa])

func _gen(nombre: String) -> Texture2D:
	var jpg := BASE + "generadas/%s.jpg" % nombre
	return load(jpg) if ResourceLoader.exists(jpg) else load(BASE + "generadas/%s.png" % nombre)

func _mat(nombre: String, props: Dictionary) -> StandardMaterial3D:
	var m := StandardMaterial3D.new()
	m.resource_name = nombre
	for k in props:
		m.set(k, props[k])
	var ruta := BASE + "materiales/%s.tres" % nombre
	ResourceSaver.save(m, ruta)
	m.take_over_path(ruta)
	return m

# Papel: el documento generado como albedo, con el relieve y la aspereza del papel de ambientCG
func _papel(nombre: String, albedo: String, extra := {}) -> StandardMaterial3D:
	var props := {
		"albedo_texture": _gen(albedo),
		"normal_enabled": true, "normal_texture": _acg("Paper005", "NormalGL"), "normal_scale": 0.5,
		"roughness_texture": _acg("Paper005", "Roughness"), "roughness": 1.0,
		"texture_filter": BaseMaterial3D.TEXTURE_FILTER_LINEAR_WITH_MIPMAPS_ANISOTROPIC,
	}
	props.merge(extra, true)
	return _mat(nombre, props)

# Material de ambientCG con proyección triplanar (para piezas sin UV pensadas, como metal o madera)
func _triplanar(nombre: String, id: String, escala: float, metal := false, extra := {}) -> StandardMaterial3D:
	var props := {
		"albedo_texture": _acg(id, "Color"),
		"normal_enabled": true, "normal_texture": _acg(id, "NormalGL"),
		"roughness_texture": _acg(id, "Roughness"), "roughness": 1.0,
		"uv1_triplanar": true, "uv1_scale": Vector3.ONE * escala, "uv1_world_triplanar": false,
	}
	if metal:
		props["metallic_texture"] = _acg(id, "Metalness")
		props["metallic"] = 1.0
	props.merge(extra, true)
	return _mat(nombre, props)

# Óxido en manchas encima de un metal (textura de detalle con una máscara de ruido)
func _con_oxido(m: StandardMaterial3D, cantidad: float) -> void:
	var ruido := FastNoiseLite.new()
	ruido.seed = _azar.randi()
	ruido.frequency = 0.02
	ruido.fractal_octaves = 5
	var mascara := NoiseTexture2D.new()
	mascara.width = 256
	mascara.height = 256
	mascara.seamless = true
	mascara.noise = ruido
	var rampa := Gradient.new()
	rampa.set_color(0, Color.BLACK)
	rampa.set_color(1, Color.WHITE)
	rampa.set_offset(0, 1.0 - cantidad)
	rampa.set_offset(1, 1.0 - cantidad + 0.15)
	mascara.color_ramp = rampa
	m.detail_enabled = true
	m.detail_mask = mascara
	m.detail_albedo = _acg("Metal021", "Color")
	m.detail_normal = _acg("Metal021", "NormalGL")
	m.detail_blend_mode = BaseMaterial3D.BLEND_MODE_MIX
	ResourceSaver.save(m, m.resource_path)

func _guardar_malla(malla: Mesh, ruta: String) -> Mesh:
	ResourceSaver.save(malla, ruta)
	malla.take_over_path(ruta)
	return malla

func _nodo(padre: Node, nombre: String, malla: Mesh = null, t := Transform3D.IDENTITY) -> Node3D:
	var n: Node3D = MeshInstance3D.new() if malla else Node3D.new()
	n.name = nombre
	if malla:
		(n as MeshInstance3D).mesh = malla
	n.transform = t
	padre.add_child(n)
	return n

# Marcador de un detalle oculto: +Z hacia afuera, +Y hacia arriba del texto
func _marcador(padre: Node, id: String, posicion: Vector3, normal: Vector3, arriba: Vector3) -> void:
	var m := Marker3D.new()
	m.name = "Detalle_" + id
	var z := normal.normalized()
	var y := arriba.normalized()
	m.transform = Transform3D(Basis(y.cross(z), y, z), posicion)
	padre.add_child(m)

func _escena(raiz: Node, ruta: String) -> void:
	_duenos(raiz, raiz)
	var ps := PackedScene.new()
	ps.pack(raiz)
	ResourceSaver.save(ps, ruta)
	print("  ", ruta)
	raiz.free()

func _duenos(raiz: Node, n: Node) -> void:
	for h in n.get_children():
		h.owner = raiz
		# Lo que viene instanciado (el hacha de Poly Haven) se guarda como instancia, sin sus nodos internos
		if h.scene_file_path.is_empty():
			_duenos(raiz, h)

func _tent(x: float, centro: float, ancho: float) -> float:
	return maxf(0.0, 1.0 - absf(x - centro) / ancho)

# ---------- Papeles ----------

func _nota_comisario() -> void:
	var frente := _papel("nota_comisario_frente", "nota_comisario_frente")
	var reverso := _papel("nota_comisario_reverso", "nota_comisario_reverso")
	# Doblada en tres y vuelta a abrir: dos pliegues que levantan la hoja
	var forma := func(u: float, v: float) -> float:
		return 0.006 * _tent(v, 0.333, 0.2) + 0.0035 * _tent(v, 0.667, 0.16) + 0.0015 * pow(2.0 * u - 1.0, 2)
	var malla := _guardar_malla(M.hoja(0.148, 0.207, forma, frente, reverso, 0.0008, 20, 36), BASE + "nota_comisario/nota_comisario.res")
	var raiz := Node3D.new()
	raiz.name = "NotaComisario"
	_nodo(raiz, "Hoja", malla)
	_escena(raiz, BASE + "nota_comisario/nota_comisario.tscn")

func _nota_porky() -> void:
	var frente := _papel("nota_porky_frente", "nota_porky_frente")
	var reverso := _papel("nota_porky_reverso", "nota_porky_reverso")
	# Doblada a la mitad: el pliegue abajo y las orillas un poco levantadas
	var forma := func(u: float, v: float) -> float:
		return 0.011 * absf(u - 0.5) * 2.0 + 0.002 * sin(v * PI)
	var malla := _guardar_malla(M.hoja(0.15, 0.15, forma, frente, reverso, 0.0008, 32, 24), BASE + "nota_porky/nota_porky.res")
	var raiz := Node3D.new()
	raiz.name = "NotaPorky"
	_nodo(raiz, "Hoja", malla)
	_escena(raiz, BASE + "nota_porky/nota_porky.tscn")

func _registro_llamadas() -> void:
	var frente := _papel("registro_llamadas_frente", "registro_llamadas_frente")
	var reverso := _papel("hoja_reverso", "hoja_reverso")
	var forma := func(u: float, v: float) -> float:
		return 0.006 * pow(v, 3) + 0.002 * pow(2.0 * u - 1.0, 2)
	var malla := _guardar_malla(M.hoja(0.21, 0.297, forma, frente, reverso), BASE + "registro_llamadas/registro_llamadas.res")
	var raiz := Node3D.new()
	raiz.name = "RegistroLlamadas"
	_nodo(raiz, "Hoja", malla)
	_escena(raiz, BASE + "registro_llamadas/registro_llamadas.tscn")

# Recorte de periódico clavado en un corcho (colgado de la pared: el frente mira a +Z)
func _recorte() -> void:
	var frente := _papel("recorte_frente", "recorte_frente")
	var reverso := _papel("recorte_reverso", "recorte_reverso")
	var corcho := _triplanar("corcho", "Cork002", 3.0)
	var raiz := Node3D.new()
	raiz.name = "Recorte"
	_nodo(raiz, "Corcho", _guardar_malla(M.caja(Vector3(0.6, 0.5, 0.025), corcho), BASE + "recorte_periodico/corcho.res"))
	var marco := _triplanar("madera_marco", "TreeEnd003", 6.0, false, {"albedo_color": Color(0.55, 0.4, 0.28)})
	var listones := ArrayMesh.new()
	for l in [[Vector3(0.62, 0.02, 0.03), Vector3(0, 0.26, 0)], [Vector3(0.62, 0.02, 0.03), Vector3(0, -0.26, 0)], [Vector3(0.02, 0.5, 0.03), Vector3(0.31, 0, 0)], [Vector3(0.02, 0.5, 0.03), Vector3(-0.31, 0, 0)]]:
		var st := SurfaceTool.new()
		st.begin(Mesh.PRIMITIVE_TRIANGLES)
		st.set_material(marco)
		M._caja(st, l[0], l[1])
		M._cerrar(st, listones)
	_nodo(raiz, "Marco", _guardar_malla(listones, BASE + "recorte_periodico/marco.res"))
	var forma := func(u: float, v: float) -> float:
		return 0.004 * pow(v, 2) + 0.002 * pow(2.0 * u - 1.0, 2)
	var hoja := _guardar_malla(M.hoja(0.3, 0.409, forma, frente, reverso), BASE + "recorte_periodico/recorte.res")
	# Rx(90°): el frente (+Y) mira a +Z y lo de arriba de la hoja (-Z) queda arriba (+Y)
	_nodo(raiz, "Recorte", hoja, Transform3D(Basis(Vector3.RIGHT, PI / 2), Vector3(0.04, 0.0, 0.0128)))
	var tachuela := _mat("tachuela", {"albedo_color": Color(0.75, 0.08, 0.06), "roughness": 0.3, "clearcoat_enabled": true})
	var cabeza := SphereMesh.new()
	cabeza.radius = 0.006
	cabeza.height = 0.008
	cabeza.material = tachuela
	for p in [Vector3(-0.09, 0.19, 0.019), Vector3(0.17, 0.19, 0.019)]:
		_nodo(raiz, "Tachuela", cabeza, Transform3D(Basis(), p))
	_escena(raiz, BASE + "recorte_periodico/recorte_periodico.tscn")

# Oferta de compra rota en cuatro pedazos con orillas rasgadas, tirados uno encima de otro
func _oferta() -> void:
	var frente := _papel("oferta_frente", "oferta_frente")
	var reverso := _papel("oferta_reverso", "oferta_reverso")
	var centro := Vector2(0.47, 0.53)
	var rasgado := func(desde: Vector2, hasta: Vector2, pasos: int) -> PackedVector2Array:
		var pts := PackedVector2Array()
		var normal := (hasta - desde).orthogonal().normalized()
		for i in pasos + 1:
			var t := float(i) / pasos
			var jitter := 0.0 if i == 0 or i == pasos else _azar.randf_range(-0.012, 0.012)
			pts.append(desde.lerp(hasta, t) + normal * jitter)
		return pts
	var arriba: PackedVector2Array = rasgado.call(Vector2(0.5, 0.0), centro, 14)
	var abajo: PackedVector2Array = rasgado.call(centro, Vector2(0.44, 1.0), 14)
	var izq: PackedVector2Array = rasgado.call(Vector2(0.0, 0.55), centro, 14)
	var der: PackedVector2Array = rasgado.call(centro, Vector2(1.0, 0.5), 14)
	var invertir := func(p: PackedVector2Array) -> PackedVector2Array:
		var r := p.duplicate()
		r.reverse()
		return r
	var piezas := []
	# Arriba a la izquierda, arriba a la derecha, abajo a la derecha, abajo a la izquierda
	var ai := PackedVector2Array([Vector2(0, 0)])
	ai.append_array(arriba)
	ai.append_array(invertir.call(izq).slice(1))
	piezas.append(ai)
	var ad := PackedVector2Array([Vector2(1, 0), Vector2(1, 0.5)])
	ad.append_array(invertir.call(der).slice(1))
	ad.append_array(invertir.call(arriba).slice(1, arriba.size() - 1))
	piezas.append(ad)
	var bd := PackedVector2Array([Vector2(1, 1), Vector2(0.44, 1)])
	bd.append_array(invertir.call(abajo).slice(1))
	bd.append_array(der.slice(1, der.size() - 1))
	piezas.append(bd)
	var bi := PackedVector2Array([Vector2(0, 1), Vector2(0, 0.55)])
	bi.append_array(izq.slice(1))
	bi.append_array(abajo.slice(1, abajo.size() - 1))
	piezas.append(bi)
	var raiz := Node3D.new()
	raiz.name = "Oferta"
	var lugares := [[Vector3(-0.03, 0.0, -0.02), 0.25], [Vector3(0.05, 0.0012, 0.0), -0.4], [Vector3(0.02, 0.0024, 0.06), 0.9], [Vector3(-0.06, 0.0036, 0.05), -0.15]]
	for i in piezas.size():
		var contorno: PackedVector2Array = piezas[i]
		var c := Vector2.ZERO
		for p in contorno:
			c += p
		c /= contorno.size()
		var malla := _guardar_malla(M.pieza(contorno, 0.216, 0.279, frente, reverso), BASE + "oferta_compra/pedazo_%d.res" % i)
		# Cada pedazo se centra en su propio centro y se acomoda revuelto
		var desplazar := Vector3(-(c.x - 0.5) * 0.216, 0, -(c.y - 0.5) * 0.279)
		var t := Transform3D(Basis(Vector3.UP, lugares[i][1]), lugares[i][0]) * Transform3D(Basis(), desplazar)
		_nodo(raiz, "Pedazo%d" % i, malla, t)
	_escena(raiz, BASE + "oferta_compra/oferta_compra.tscn")

# Libro de guardia quemado: tres hojas sueltas con borde quemado; el reverso de la tercera tiene el nombre
func _libro_quemado() -> void:
	var raiz := Node3D.new()
	raiz.name = "LibroQuemado"
	var hojas := [["a", "quemado_hoja_a_q", "quemado_reverso_a_q", Vector3(-0.06, 0.0, 0.01), 0.3],
		["b", "quemado_hoja_b_q", "quemado_reverso_b_q", Vector3(-0.03, 0.004, -0.06), -0.5],
		["c", "quemado_hoja_c_q", "quemado_reverso_nombre_q", Vector3(0.1, 0.008, 0.05), 1.2]]
	var quemado := {"transparency": BaseMaterial3D.TRANSPARENCY_ALPHA_SCISSOR, "alpha_scissor_threshold": 0.5}
	for h in hojas:
		var frente := _papel("quemado_%s_frente" % h[0], h[1], quemado)
		var reverso := _papel("quemado_%s_reverso" % h[0], h[2], quemado)
		var semilla := _azar.randf() * 10.0
		# El papel quemado se enrosca en las orillas
		var forma := func(u: float, v: float) -> float:
			return 0.006 * (pow(2.0 * u - 1.0, 4) + pow(2.0 * v - 1.0, 4)) + 0.0015 * sin(u * 9.0 + semilla)
		# El reverso usa las mismas UV que el frente: así coinciden los bordes quemados
		var malla := _guardar_malla(M.hoja(0.2, 0.1429, forma, frente, reverso, 0.0006, 28, 20, false, false), BASE + "libro_guardia_quemado/hoja_%s.res" % h[0])
		var nodo := _nodo(raiz, "Hoja_" + h[0], malla, Transform3D(Basis(Vector3.UP, h[4]), h[3]))
		if h[0] == "c":
			# El nombre está en el reverso: se ve volteando la hoja (la textura del reverso se dibujó al revés)
			_marcador(nodo, "antonio", Vector3(0.004, -0.0002, -0.006), Vector3.DOWN, Vector3.FORWARD)
	_escena(raiz, BASE + "libro_guardia_quemado/libro_guardia_quemado.tscn")

# Libro de turnos abierto (doble página con las firmas)
func _libro_turnos() -> void:
	var paginas := _papel("libro_turnos_paginas", "libro_turnos_paginas")
	var tapa := _triplanar("piel_cafe", "Leather037", 4.0, false, {"albedo_color": Color(0.55, 0.35, 0.22)})
	var canto := _mat("canto_hojas", {"albedo_texture": _gen("canto_hojas"), "roughness": 0.9})
	var raiz := Node3D.new()
	raiz.name = "LibroTurnos"
	_nodo(raiz, "Libro", _guardar_malla(M.libro(0.21, 0.29, 0.018, paginas, tapa, canto), BASE + "libro_turnos/libro_turnos.res"))
	_escena(raiz, BASE + "libro_turnos/libro_turnos.tscn")

# Registro de la enfermería: libreta abierta con la línea tachada; con la linterna se ven los surcos
func _registro_enfermeria() -> void:
	var paginas := _papel("registro_enfermeria_paginas", "registro_enfermeria_paginas")
	var tapa := _triplanar("piel_verde", "Leather037", 4.0, false, {"albedo_color": Color(0.3, 0.38, 0.32)})
	var canto := load(BASE + "materiales/canto_hojas.tres")
	var raiz := Node3D.new()
	raiz.name = "RegistroEnfermeria"
	_nodo(raiz, "Libreta", _guardar_malla(M.libro(0.15, 0.21, 0.008, paginas, tapa, canto), BASE + "registro_enfermeria/libreta.res"))
	var surcos := QuadMesh.new()
	surcos.size = Vector2(0.15, 0.0234)
	surcos.material = _mat("registro_surcos", {"albedo_texture": _gen("registro_enfermeria_surcos"), "transparency": BaseMaterial3D.TRANSPARENCY_ALPHA,
		"shading_mode": BaseMaterial3D.SHADING_MODE_UNSHADED})
	# Rx(-90°): el cuadro mira hacia arriba y lo de arriba del texto queda hacia la parte de arriba de la hoja
	var nodo := _nodo(raiz, "Surcos", surcos, Transform3D(Basis(Vector3.RIGHT, -PI / 2), Vector3(0.0979, 0.0118, -0.0584)))
	nodo.add_to_group("examen_linterna", true)
	nodo.visible = false
	_marcador(raiz, "tachon", Vector3(0.072, 0.012, -0.0559), Vector3.UP, Vector3.FORWARD)
	_escena(raiz, BASE + "registro_enfermeria/registro_enfermeria.tscn")

# Inventario del baúl (va con el contenido del baúl)
func _inventario() -> Mesh:
	var frente := _papel("inventario_baul", "inventario_baul")
	var reverso := load(BASE + "materiales/hoja_reverso.tres")
	var forma := func(u: float, v: float) -> float:
		return 0.003 * pow(2.0 * u - 1.0, 2) + 0.002 * v
	return _guardar_malla(M.hoja(0.15, 0.21, forma, frente, reverso), BASE + "baul_disfraces/inventario.res")

# Envoltura de sándwich arrugada, con migas
func _envoltorio() -> void:
	var papel := _papel("envoltorio", "envoltorio_papel", {"roughness": 0.45, "roughness_texture": null})
	var ruido := FastNoiseLite.new()
	ruido.seed = 77
	ruido.frequency = 3.5
	ruido.fractal_octaves = 3
	var forma := func(u: float, v: float) -> float:
		var crestas := absf(ruido.get_noise_2d(u, v))
		var orilla := maxf(absf(u - 0.5), absf(v - 0.5)) * 2.0
		return 0.012 * crestas + 0.01 * pow(orilla, 3) + 0.004
	var raiz := Node3D.new()
	raiz.name = "Envoltorio"
	_nodo(raiz, "Papel", _guardar_malla(M.hoja(0.22, 0.16, forma, papel, papel, 0.0004, 44, 32), BASE + "envoltorio/envoltorio.res"))
	var miga := SphereMesh.new()
	miga.radius = 0.004
	miga.height = 0.005
	miga.radial_segments = 8
	miga.rings = 4
	miga.material = _mat("miga", {"albedo_color": Color(0.72, 0.55, 0.33), "roughness": 0.95})
	for i in 7:
		var p := Vector3(_azar.randf_range(-0.07, 0.07), 0.016, _azar.randf_range(-0.05, 0.05))
		_nodo(raiz, "Miga%d" % i, miga, Transform3D(Basis().scaled(Vector3.ONE * _azar.randf_range(0.5, 1.3)), p))
	_escena(raiz, BASE + "envoltorio/envoltorio.tscn")

# Pluma negra de ave: barbas (textura con alfa) y cañón
func _pluma() -> void:
	var barbas := _mat("pluma", {"albedo_texture": _gen("pluma"), "transparency": BaseMaterial3D.TRANSPARENCY_ALPHA_SCISSOR,
		"alpha_scissor_threshold": 0.4, "cull_mode": BaseMaterial3D.CULL_DISABLED, "roughness": 0.6, "metallic_specular": 0.3, "albedo_color": Color(0.7, 0.72, 0.8), "rim_enabled": true, "rim": 0.25, "rim_tint": 0.8})
	var forma := func(u: float, v: float) -> float:
		return 0.012 * sin(v * PI) + 0.004 * (u - 0.5) * v
	var raiz := Node3D.new()
	raiz.name = "Pluma"
	_nodo(raiz, "Barbas", _guardar_malla(M.hoja(0.055, 0.22, forma, barbas, barbas, 0.0002, 8, 40, false), BASE + "pluma/barbas.res"))
	var puntos := []
	var radios := []
	for i in 21:
		var v := float(i) / 20.0 * 1.12
		puntos.append(Vector3(0, 0.012 * sin(minf(v, 1.0) * PI) + 0.0003, (v - 0.5) * 0.22))
		radios.append(lerpf(0.0003, 0.0013, clampf(v, 0, 1)))
	var canon := _mat("pluma_canon", {"albedo_color": Color(0.16, 0.14, 0.12), "roughness": 0.35})
	_nodo(raiz, "Canon", _guardar_malla(M.tubo(puntos, radios, 8, canon), BASE + "pluma/canon.res"))
	_escena(raiz, BASE + "pluma/pluma.tscn")

# ---------- Objetos ----------

# Candado de latón abierto, con el arco de acero fuera del cuerpo y óxido en las orillas
func _candado() -> void:
	var laton := _triplanar("laton_oxidado", "Metal048B", 14.0, true)
	_con_oxido(laton, 0.3)
	var acero := _triplanar("acero_oxidado", "Metal038", 14.0, true)
	_con_oxido(acero, 0.35)
	var raiz := Node3D.new()
	raiz.name = "Candado"
	_nodo(raiz, "Cuerpo", _guardar_malla(M.caja_redondeada(0.045, 0.038, 0.018, 0.007, 0.0015, laton), BASE + "candado_caseta/cuerpo.res"), Transform3D(Basis(), Vector3(0, 0.019, 0)))
	# Arco en U: patas en x = 0 (la que se queda) y x = 0.027; está levantado y girado 90° sobre la pata fija
	var puntos := []
	var radios := []
	for y in [0.026, 0.04, 0.052]:
		puntos.append(Vector3(0, y, 0))
	for k in range(1, 12):
		var ang := PI - k * PI / 12
		puntos.append(Vector3(0.0135 + cos(ang) * 0.0135, 0.052 + sin(ang) * 0.0135, 0))
	for y in [0.052, 0.045]:
		puntos.append(Vector3(0.027, y, 0))
	for p in puntos:
		radios.append(0.0035)
	var arco := _guardar_malla(M.tubo(puntos, radios, 12, acero), BASE + "candado_caseta/arco.res")
	_nodo(raiz, "Arco", arco, Transform3D(Basis(Vector3.UP, -PI / 2), Vector3(-0.0135, 0.009, 0)))
	var ojo := QuadMesh.new()
	ojo.size = Vector2(0.012, 0.012)
	ojo.material = _mat("candado_ojo", {"albedo_texture": _gen("candado_ojo"), "transparency": BaseMaterial3D.TRANSPARENCY_ALPHA_SCISSOR, "roughness": 0.6, "metallic": 0.5})
	_nodo(raiz, "OjoLlave", ojo, Transform3D(Basis(), Vector3(0, 0.013, 0.0092)))
	_escena(raiz, BASE + "candado_caseta/candado_caseta.tscn")

# Taza de cerámica con chocolate a la mitad y manchas que escurrieron por fuera
func _taza() -> Mesh:
	var ceramica := _mat("ceramica_taza", {"albedo_texture": _gen("taza_manchas"), "roughness": 0.15, "clearcoat_enabled": true, "clearcoat": 0.6})
	var perfil := [Vector2(0.0, 0.0), Vector2(0.030, 0.0), Vector2(0.034, 0.002), Vector2(0.0375, 0.008), Vector2(0.0395, 0.02),
		Vector2(0.041, 0.088), Vector2(0.0412, 0.094), Vector2(0.040, 0.0965), Vector2(0.0385, 0.0968), Vector2(0.0372, 0.095),
		Vector2(0.0365, 0.088), Vector2(0.0355, 0.015), Vector2(0.03, 0.009), Vector2(0.0, 0.008)]
	var malla := M.torno(perfil, 40, ceramica, 0.1)
	var puntos := []
	var radios := []
	for i in 17:
		var a := lerpf(0.6 * PI, -0.6 * PI, float(i) / 16.0)
		puntos.append(Vector3(0.040 + 0.024 * cos(a), 0.051 + 0.027 * sin(a), 0))
		radios.append(0.0052)
	M.tubo(puntos, radios, 10, ceramica, false, malla)
	var chocolate := _mat("chocolate", {"albedo_color": Color(0.2, 0.1, 0.05), "roughness": 0.22, "clearcoat_enabled": true})
	M.torno([Vector2(0.0358, 0.0527), Vector2(0.03, 0.0522), Vector2(0.0, 0.052)], 40, chocolate, 0.1, malla)
	_guardar_malla(malla, BASE + "taza_delantal/taza.res")
	var raiz := Node3D.new()
	raiz.name = "Taza"
	_nodo(raiz, "Taza", malla)
	_escena(raiz, BASE + "taza_delantal/taza.tscn")
	return malla

# Delantal de cocina colgado de un gancho (el origen es el gancho)
func _delantal() -> void:
	var tela := _triplanar("tela_delantal", "Fabric061", 6.0, false, {"albedo_color": Color(0.78, 0.36, 0.3), "cull_mode": BaseMaterial3D.CULL_DISABLED})
	var dentro := func(u: float, v: float) -> bool:
		var media := 0.0
		if v < 0.3:
			media = 0.21
		elif v < 0.44:
			media = lerpf(0.21, 0.47, (v - 0.3) / 0.14)
		else:
			media = 0.47 + (v - 0.44) * 0.04
		return absf(u - 0.5) <= media + 0.001 and v <= 0.985
	var relieve := func(u: float, v: float) -> float:
		var pliegues := 0.012 * sin(u * 26.0 + v * 2.0) * smoothstep(0.35, 1.0, v)
		return pliegues + 0.025 * pow(v, 2) - 0.03 * pow(absf(u - 0.5) * 2.0, 2) * v
	var malla := M.tela(0.56, 0.8, dentro, relieve, tela, 44, 60)
	var bolsillo := _triplanar("tela_bolsillo", "Fabric061", 6.0, false, {"albedo_color": Color(0.62, 0.27, 0.23), "cull_mode": BaseMaterial3D.CULL_DISABLED})
	var raiz := Node3D.new()
	raiz.name = "Delantal"
	_nodo(raiz, "Tela", _guardar_malla(malla, BASE + "delantal/tela.res"), Transform3D(Basis(), Vector3(0, -0.9, 0)))
	var malla_bolsillo := M.tela(0.22, 0.13, func(u, v): return true, func(u, v): return 0.004 + 0.003 * sin(u * PI), bolsillo, 10, 6)
	_nodo(raiz, "Bolsillo", _guardar_malla(malla_bolsillo, BASE + "delantal/bolsillo.res"), Transform3D(Basis(), Vector3(0, -0.62, relieve.call(0.5, 0.62) + 0.002)))
	var cinta := _mat("cinta_delantal", {"albedo_color": Color(0.85, 0.82, 0.74), "roughness": 0.9})
	var cuello := [Vector3(-0.115, -0.1, 0.0), Vector3(-0.08, -0.04, 0.004), Vector3(-0.03, -0.006, 0.008), Vector3(0, 0, 0.009), Vector3(0.03, -0.006, 0.008), Vector3(0.08, -0.04, 0.004), Vector3(0.115, -0.1, 0.0)]
	var radios := []
	for p in cuello:
		radios.append(0.004)
	_nodo(raiz, "Cuello", _guardar_malla(M.tubo(cuello, radios, 6, cinta), BASE + "delantal/cuello.res"))
	for lado in [-1.0, 1.0]:
		var tira := [Vector3(lado * 0.27, -0.45, 0.01), Vector3(lado * 0.3, -0.5, 0.02), Vector3(lado * 0.29, -0.62, 0.02), Vector3(lado * 0.27, -0.74, 0.015)]
		var r := []
		for p in tira:
			r.append(0.004)
		_nodo(raiz, "Tira", _guardar_malla(M.tubo(tira, r, 6, cinta), BASE + "delantal/tira_%d.res" % int(lado)))
	var metal := _triplanar("gancho", "Metal038", 20.0, true)
	var gancho := [Vector3(0, 0.02, -0.02), Vector3(0, 0.0, -0.02), Vector3(0, -0.006, -0.005), Vector3(0, 0.004, 0.01)]
	_nodo(raiz, "Gancho", _guardar_malla(M.tubo(gancho, [0.003, 0.003, 0.003, 0.003], 8, metal), BASE + "delantal/gancho.res"))
	_escena(raiz, BASE + "delantal/delantal.tscn")

# Leña partida y apilada, sin tocar, con el hacha recargada
func _lena() -> void:
	var corteza := _mat("corteza", {"albedo_texture": _acg("Bark012", "Color"), "normal_enabled": true, "normal_texture": _acg("Bark012", "NormalGL"),
		"roughness_texture": _acg("Bark012", "Roughness"), "roughness": 1.0})
	var corte := _mat("anillos", {"albedo_texture": _acg("TreeEnd003", "Color"), "normal_enabled": true, "normal_texture": _acg("TreeEnd003", "NormalGL"),
		"roughness_texture": _acg("TreeEnd003", "Roughness"), "roughness": 1.0})
	var astilla := _mat("astilla", {"albedo_texture": _acg("TreeEnd003", "Color"), "normal_enabled": true, "normal_texture": _acg("TreeEnd003", "NormalGL"),
		"roughness": 0.9, "uv1_scale": Vector3(0.12, 0.08, 1), "uv1_offset": Vector3(0.42, 0.45, 0)})
	var raiz := Node3D.new()
	raiz.name = "Lena"
	var filas := [5, 4, 3]
	var y := 0.0
	var n := 0
	for f in filas.size():
		var radio_fila := 0.0
		for i in filas[f]:
			var radio := _azar.randf_range(0.06, 0.08)
			radio_fila = maxf(radio_fila, radio)
			var tipo := _azar.randi_range(0, 2)
			var arco := TAU if tipo == 0 else (PI if tipo == 1 else PI / 2)
			var malla := _guardar_malla(M.tronco(radio, _azar.randf_range(0.38, 0.44), 0.0, arco, corteza, corte, astilla), BASE + "lena_intacta/tronco_%d.res" % n)
			var x: float = (i - (filas[f] - 1) / 2.0) * 0.165 + _azar.randf_range(-0.01, 0.01)
			var giro := Basis(Vector3.FORWARD, _azar.randf() * TAU)
			_nodo(raiz, "Tronco%d" % n, malla, Transform3D(Basis(Vector3.UP, _azar.randf_range(-0.06, 0.06)) * giro, Vector3(x, y + radio * 0.9, _azar.randf_range(-0.02, 0.02))))
			n += 1
		y += radio_fila * 1.6
	var hacha: Node3D = load(HACHA).instantiate()
	hacha.name = "Hacha"
	hacha.transform = Transform3D(Basis(Vector3.FORWARD, -0.35) * Basis(Vector3.UP, PI / 2), Vector3(0.5, 0.0, 0.1))
	raiz.add_child(hacha)
	_escena(raiz, BASE + "lena_intacta/lena_intacta.tscn")

# Huella de bota (calcomanía) y el molde de concreto con dos huellas para la vista de examen
func _huellas() -> void:
	var huella := Decal.new()
	huella.name = "Huella"
	huella.size = Vector3(0.11, 0.06, 0.29)
	huella.texture_albedo = _gen("huella_albedo")
	huella.texture_normal = _gen("huella_normal")
	huella.normal_fade = 0.3
	_escena(huella, BASE + "huellas/huella.tscn")
	var raiz := Node3D.new()
	raiz.name = "MoldeHuellas"
	var concreto := _triplanar("concreto_pasillo", "Concrete034", 2.0, false, {"albedo_color": Color(0.5, 0.48, 0.45)})
	_nodo(raiz, "Piso", _guardar_malla(M.caja(Vector3(0.42, 0.04, 0.62), concreto, Vector3(0, 0.02, 0)), BASE + "huellas/piso.res"))
	var escena_huella: PackedScene = load(BASE + "huellas/huella.tscn")
	for p in [[Vector3(-0.08, 0.04, 0.1), 0.05], [Vector3(0.08, 0.04, -0.12), -0.04]]:
		var h: Node3D = escena_huella.instantiate()
		h.transform = Transform3D(Basis(Vector3.UP, p[1]), p[0])
		raiz.add_child(h)
	_escena(raiz, BASE + "huellas/huellas_molde.tscn")

# Contenido del baúl de disfraces: capa doblada, corona e inventario (la cornamenta ya no está)
func _baul_contenido() -> void:
	var raiz := Node3D.new()
	raiz.name = "ContenidoBaul"
	var terciopelo := _triplanar("terciopelo", "Fabric061", 5.0, false, {"albedo_color": Color(0.55, 0.08, 0.1), "cull_mode": BaseMaterial3D.CULL_DISABLED})
	var ruido := FastNoiseLite.new()
	ruido.seed = 5
	ruido.frequency = 2.5
	var capa := M.tela(0.42, 0.32, func(u, v): return true, func(u, v): return 0.03 * absf(ruido.get_noise_2d(u, v)) + 0.02 * pow(absf(u - 0.5) * 2, 3), terciopelo, 30, 24)
	_nodo(raiz, "Capa", _guardar_malla(capa, BASE + "baul_disfraces/capa.res"), Transform3D(Basis(Vector3.RIGHT, -PI / 2), Vector3(-0.04, 0.0, 0.16)))
	var oro := _triplanar("oro_corona", "Metal048B", 10.0, true, {"albedo_color": Color(1.0, 0.82, 0.45)})
	var corona := M.torno([Vector2(0.075, 0.0), Vector2(0.078, 0.0), Vector2(0.078, 0.04), Vector2(0.075, 0.04), Vector2(0.075, 0.0)], 36, oro, 0.05)
	var pico := CylinderMesh.new()
	pico.top_radius = 0.0
	pico.bottom_radius = 0.011
	pico.height = 0.035
	pico.radial_segments = 4
	pico.material = oro
	var nodo_corona := _nodo(raiz, "Corona", _guardar_malla(corona, BASE + "baul_disfraces/corona.res"), Transform3D(Basis(Vector3.RIGHT, 0.25), Vector3(0.13, 0.035, -0.06)))
	var gema := SphereMesh.new()
	gema.radius = 0.006
	gema.height = 0.012
	gema.material = _mat("gema", {"albedo_color": Color(0.6, 0.05, 0.08), "roughness": 0.05, "metallic": 0.2})
	for k in 8:
		var ang := k * TAU / 8
		_nodo(nodo_corona, "Pico%d" % k, pico, Transform3D(Basis(), Vector3(cos(ang) * 0.0765, 0.0575, sin(ang) * 0.0765)))
		_nodo(nodo_corona, "Gema%d" % k, gema, Transform3D(Basis(), Vector3(cos(ang + 0.39) * 0.079, 0.02, sin(ang + 0.39) * 0.079)))
	_nodo(raiz, "Inventario", _inventario(), Transform3D(Basis(Vector3.UP, 0.35), Vector3(-0.12, 0.03, -0.05)))
	_escena(raiz, BASE + "baul_disfraces/contenido.tscn")

# Palanca principal del interruptor, bajada (se monta al costado de la caja de Poly Haven)
func _palanca() -> void:
	var metal := _triplanar("metal_palanca", "Metal021", 12.0, true)
	var plastico := _mat("mango_rojo", {"albedo_color": Color(0.6, 0.06, 0.04), "roughness": 0.35, "clearcoat_enabled": true})
	var raiz := Node3D.new()
	raiz.name = "Palanca"
	_nodo(raiz, "Base", _guardar_malla(M.caja(Vector3(0.035, 0.08, 0.03), metal), BASE + "interruptor_generador/base.res"))
	var brazo := [Vector3(0.02, 0.0, 0.0), Vector3(0.03, -0.03, 0.02), Vector3(0.035, -0.08, 0.04)]
	_nodo(raiz, "Brazo", _guardar_malla(M.tubo(brazo, [0.005, 0.005, 0.005], 10, metal), BASE + "interruptor_generador/brazo.res"))
	var mango := [Vector3(0.035, -0.075, 0.038), Vector3(0.036, -0.1, 0.048), Vector3(0.037, -0.12, 0.055)]
	_nodo(raiz, "Mango", _guardar_malla(M.tubo(mango, [0.009, 0.010, 0.009], 12, plastico), BASE + "interruptor_generador/mango.res"))
	_escena(raiz, BASE + "interruptor_generador/palanca.tscn")

# Materiales de los adornos que se agregan a los modelos de Poly Haven (etiquetas, grietas, foto)
func _materiales_poly_haven() -> void:
	_papel("etiqueta_grabadora", "grabadora_etiqueta")
	_papel("etiqueta_cinta", "cinta_etiqueta", {"roughness": 0.5, "roughness_texture": null})
	_mat("reloj_grietas", {"albedo_texture": _gen("reloj_grietas"), "transparency": BaseMaterial3D.TRANSPARENCY_ALPHA, "roughness": 0.1, "metallic_specular": 0.8})
	_mat("foto_arte", {"albedo_texture": _gen("foto_grupo_arte"), "roughness": 0.55, "texture_filter": BaseMaterial3D.TEXTURE_FILTER_LINEAR_WITH_MIPMAPS_ANISOTROPIC})
	_mat("foto_reverso", {"albedo_texture": _gen("foto_grupo_reverso"), "roughness": 0.95, "normal_enabled": true, "normal_texture": _acg("Cork002", "NormalGL"), "normal_scale": 0.3})
	# El vidrio del marco de Poly Haven es opaco en Godot: uno transparente deja ver la foto
	_mat("vidrio_marco", {"albedo_color": Color(0.9, 0.95, 1.0, 0.08), "transparency": BaseMaterial3D.TRANSPARENCY_ALPHA, "roughness": 0.04, "metallic_specular": 0.9})
	_mat("luz_rec", {"albedo_color": Color(1, 0.1, 0.05), "emission_enabled": true, "emission": Color(1, 0.12, 0.05), "emission_energy_multiplier": 3.0})
	_mat("barro", {"albedo_color": Color(0.24, 0.17, 0.1), "roughness": 0.95})
	_mat("junco", {"albedo_color": Color(0.36, 0.42, 0.2), "roughness": 0.8, "cull_mode": BaseMaterial3D.CULL_DISABLED})
