extends SceneTree
## HU-20.6: modelos de las pistas del Escenario 2 (Lago y Bosque). Cada uno se guarda en
## res://Assets/Pistas/Modelos/<pista>/<pista>.tscn, igual que las pistas de HU-07, y se usa en el escenario
## y en la vista de examen. Los de Poly Haven (altavoz, linterna) se ajustan aquí; el resto se arma con mallas.
## Headless. Uso: --headless --path game --script res://Herramientas/Caso1/modelos_pistas_lago.gd

const BASE := "res://Assets/Pistas/Modelos/"
const PH := "res://Assets/Caso1/Lago/Modelos/polyhaven/"
const TEX := "res://Assets/Caso1/Lago/Texturas/"
const CAVEAT := "res://Assets/Fuentes/Caveat-Variable.ttf"

func _initialize() -> void:
	_guardar("silbato_ts", _silbato())
	_guardar("altavoz", _altavoz())
	_guardar("cornamenta_zancos", _cornamenta_zancos())
	_guardar("hueco_remo", _soporte_remos())
	_guardar("remo_sangre", _remo_sangre())
	_guardar("linterna_porky", _linterna())
	_guardar("cuaderno_eriz", _cuaderno())
	_guardar("huellas_pezuna", _huellas(4, false))
	_guardar("huellas_detenidas", _huellas(2, true))
	quit()

func _guardar(nombre: String, raiz: Node3D) -> void:
	raiz.name = camel(nombre)
	_poseer(raiz, raiz)
	var escena := PackedScene.new()
	escena.pack(raiz)
	DirAccess.make_dir_recursive_absolute(BASE + nombre)
	var ruta := BASE + nombre + "/" + nombre + ".tscn"
	print(nombre, ": ", error_string(ResourceSaver.save(escena, ruta)))
	raiz.free()

func _poseer(nodo: Node, dueno: Node) -> void:
	for hijo in nodo.get_children():
		hijo.owner = dueno
		if hijo.scene_file_path == "":
			_poseer(hijo, dueno)

static func camel(n: String) -> String:
	var r := ""
	for p in n.split("_"):
		r += p.substr(0, 1).to_upper() + p.substr(1)
	return r

# ---------- utilidades ----------

func _mat(color: Color, aspereza := 0.8, metal := 0.0) -> StandardMaterial3D:
	var m := StandardMaterial3D.new()
	m.albedo_color = color
	m.roughness = aspereza
	m.metallic = metal
	return m

func _malla(padre: Node3D, nombre: String, malla: Mesh, material: Material, pos := Vector3.ZERO, giro := Vector3.ZERO, escala := Vector3.ONE) -> MeshInstance3D:
	var mi := MeshInstance3D.new()
	mi.name = nombre
	mi.mesh = malla
	mi.material_override = material
	mi.position = pos
	mi.rotation_degrees = giro
	mi.scale = escala
	padre.add_child(mi)
	return mi

func _cilindro(arriba: float, abajo: float, alto: float, lados := 12) -> CylinderMesh:
	var c := CylinderMesh.new()
	c.top_radius = arriba
	c.bottom_radius = abajo
	c.height = alto
	c.radial_segments = lados
	c.rings = 1
	return c

func _caja(tam: Vector3) -> BoxMesh:
	var b := BoxMesh.new()
	b.size = tam
	return b

func _instancia(padre: Node3D, ruta: String, nombre: String, pos := Vector3.ZERO, giro := Vector3.ZERO, escala := 1.0) -> Node3D:
	var n: Node3D = load(ruta).instantiate()
	n.name = nombre
	n.position = pos
	n.rotation_degrees = giro
	n.scale = Vector3.ONE * escala
	padre.add_child(n)
	return n

func _texto(padre: Node3D, nombre: String, texto: String, pos: Vector3, giro: Vector3, tam: int, color: Color, pixel := 0.0004, ancho := 0.0) -> Label3D:
	var l := Label3D.new()
	l.name = nombre
	l.text = texto
	l.position = pos
	l.rotation_degrees = giro
	l.font = load(CAVEAT)
	l.font_size = tam
	l.pixel_size = pixel
	l.modulate = color
	l.outline_size = 0
	l.double_sided = false
	l.shaded = true
	if ancho > 0.0:
		l.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		l.width = ancho / pixel
	l.horizontal_alignment = HORIZONTAL_ALIGNMENT_LEFT
	l.vertical_alignment = VERTICAL_ALIGNMENT_TOP
	padre.add_child(l)
	return l

# ---------- pistas ----------

## Silbato metálico de monitor con «T. S.» grabado y el cordón rojo roto (unos 6 cm)
func _silbato() -> Node3D:
	var r := Node3D.new()
	var cromo := _mat(Color(0.78, 0.78, 0.75), 0.28, 0.9)
	var cuerpo := _malla(r, "Cuerpo", _cilindro(0.0105, 0.0105, 0.034, 20), cromo, Vector3(0, 0.0105, 0), Vector3(0, 0, 90))
	cuerpo.name = "Cuerpo"
	_malla(r, "Boquilla", _caja(Vector3(0.026, 0.008, 0.012)), cromo, Vector3(0.027, 0.016, 0))
	_malla(r, "Ventana", _caja(Vector3(0.006, 0.004, 0.0125)), _mat(Color(0.05, 0.05, 0.05), 0.6), Vector3(0.012, 0.0205, 0))
	var anillo := TorusMesh.new()
	anillo.inner_radius = 0.0035
	anillo.outer_radius = 0.0055
	_malla(r, "Argolla", anillo, cromo, Vector3(-0.021, 0.011, 0), Vector3(90, 0, 0))
	var cordon := _mat(Color(0.62, 0.06, 0.05), 0.95)
	# Cordón: un tramo que sale de la argolla y termina deshilachado
	var puntos := [Vector3(-0.026, 0.011, 0), Vector3(-0.05, 0.004, 0.012), Vector3(-0.08, 0.002, 0.03), Vector3(-0.1, 0.002, 0.026)]
	for i in puntos.size() - 1:
		var a: Vector3 = puntos[i]
		var b: Vector3 = puntos[i + 1]
		var tramo := _malla(r, "Cordon%d" % i, _cilindro(0.0016, 0.0016, a.distance_to(b), 6), cordon, (a + b) / 2)
		tramo.look_at_from_position((a + b) / 2, b, Vector3.UP)
		tramo.rotate_object_local(Vector3.RIGHT, PI / 2)
	for k in 4:
		var hilo := _malla(r, "Hilo%d" % k, _cilindro(0.0004, 0.0004, 0.008, 4), cordon, Vector3(-0.104 - k * 0.0015, 0.002, 0.024 + k * 0.0018), Vector3(0, 0, 70 + k * 12))
		hilo.name = "Hilo%d" % k
	# Iniciales grabadas en el costado del cuerpo
	var iniciales := _texto(r, "Iniciales", "T. S.", Vector3(-0.004, 0.0107, 0.0106), Vector3(0, 0, 0), 64, Color(0.12, 0.12, 0.12), 0.00011)
	iniciales.font = null
	iniciales.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	var marcador := Marker3D.new()
	marcador.name = "Detalle_iniciales"
	marcador.position = Vector3(-0.004, 0.0107, 0.012)
	r.add_child(marcador)
	return r

## Altavoz portátil (Poly Haven Boombox, a escala chica) con cable y cinta para sujetarlo al árbol
func _altavoz() -> Node3D:
	var r := Node3D.new()
	_instancia(r, PH + "boombox/boombox_1k.gltf", "Bocina", Vector3.ZERO, Vector3.ZERO, 0.42)
	var cinta := _mat(Color(0.06, 0.06, 0.06), 0.55)
	_malla(r, "Cinta", _caja(Vector3(0.035, 0.18, 0.1)), cinta, Vector3(-0.09, 0.088, 0))
	var cable := _mat(Color(0.03, 0.03, 0.03), 0.5)
	# Cable que cuelga del costado (del árbol baja hasta un cable de extensión entre las raíces)
	_malla(r, "Cable", _cilindro(0.003, 0.003, 0.35, 6), cable, Vector3(0.16, -0.12, -0.02), Vector3(0, 0, 8))
	return r

## Cornamenta del disfraz (pintada de plateado) sobre una diadema y dos zancos con pezuña en la punta
func _cornamenta_zancos() -> Node3D:
	var r := Node3D.new()
	var plata := _mat(Color(0.72, 0.73, 0.76), 0.35, 0.65)
	var cornamenta := Node3D.new()
	cornamenta.name = "Cornamenta"
	cornamenta.position = Vector3(0.1, 0.05, -0.3)
	cornamenta.scale = Vector3.ONE * 1.5
	r.add_child(cornamenta)
	var diadema := TorusMesh.new()
	diadema.inner_radius = 0.085
	diadema.outer_radius = 0.1
	_malla(cornamenta, "Diadema", diadema, _mat(Color(0.18, 0.12, 0.08), 0.8), Vector3(0, 0, 0), Vector3(0, 0, 0))
	for lado in [-1.0, 1.0]:
		var base := Vector3(lado * 0.09, 0.0, 0.0)
		_rama(cornamenta, "Rama%s" % ("I" if lado < 0 else "D"), base, Vector3(lado * 0.18, 0.12, 0.42), 0.014, plata, 0, lado)
	var madera := _mat(Color(0.42, 0.29, 0.17), 0.85)
	var pezuna := _mat(Color(0.08, 0.06, 0.05), 0.7)
	for i in 2:
		var z := Node3D.new()
		z.name = "Zanco%d" % i
		z.position = Vector3(-0.35, 0.03, 0.12 + i * 0.16)
		z.rotation_degrees = Vector3(0, 0, 90)
		r.add_child(z)
		_malla(z, "Palo", _cilindro(0.022, 0.025, 1.2, 10), madera, Vector3(0, 0.6, 0))
		_malla(z, "Estribo", _caja(Vector3(0.14, 0.025, 0.1)), madera, Vector3(0.05, 0.85, 0))
		_malla(z, "Pezuna", _cilindro(0.045, 0.06, 0.07, 10), pezuna, Vector3(0, 0.035, 0))
		_malla(z, "Ranura", _caja(Vector3(0.008, 0.075, 0.12)), _mat(Color(0.02, 0.02, 0.02), 0.9), Vector3(0, 0.03, 0))
	# Mechones de pelo de gato a rayas atorados en la diadema
	var pelo := _mat(Color(0.75, 0.5, 0.22), 0.95)
	for k in 5:
		_malla(cornamenta, "Pelo%d" % k, _cilindro(0.0012, 0.0006, 0.035, 4), pelo if k % 2 == 0 else _mat(Color(0.25, 0.16, 0.08), 0.95),
			Vector3(0.05 + k * 0.006, -0.008, 0.095), Vector3(80, k * 15, 30))
	return r

func _rama(padre: Node3D, nombre: String, desde: Vector3, hasta: Vector3, grosor: float, material: Material, nivel: int, lado: float) -> void:
	var tramo := _malla(padre, nombre, _cilindro(grosor * 0.6, grosor, desde.distance_to(hasta), 8), material, (desde + hasta) / 2)
	tramo.look_at_from_position((desde + hasta) / 2, hasta, Vector3.UP if absf((hasta - desde).normalized().y) < 0.95 else Vector3.FORWARD)
	tramo.rotate_object_local(Vector3.RIGHT, PI / 2)
	if nivel >= 2:
		return
	for k in 2:
		var t := 0.45 + k * 0.3
		var p := desde.lerp(hasta, t)
		var dir := (hasta - desde).normalized()
		var lateral := Vector3(lado * 0.6, 0.5 + k * 0.2, 0.2 - k * 0.3).normalized()
		var largo := desde.distance_to(hasta) * (0.45 - nivel * 0.1)
		_rama(padre, nombre + "_%d" % k, p, p + (lateral * 0.7 + dir * 0.3).normalized() * largo, grosor * 0.6, material, nivel + 1, lado)

## Soporte de remos de madera con tres remos y un hueco vacío (sin polvo donde estaba el que falta)
func _soporte_remos() -> Node3D:
	var r := Node3D.new()
	var madera := _mat(Color(0.36, 0.25, 0.15), 0.85)
	_malla(r, "Larguero", _caja(Vector3(1.3, 0.08, 0.12)), madera, Vector3(0, 1.25, 0))
	_malla(r, "Base", _caja(Vector3(1.3, 0.08, 0.2)), madera, Vector3(0, 0.12, 0.02))
	for x in [-0.6, 0.6]:
		_malla(r, "Pata%s" % ("I" if x < 0 else "D"), _caja(Vector3(0.08, 1.32, 0.12)), madera, Vector3(x, 0.66, 0))
	var remo := _mat(Color(0.55, 0.38, 0.2), 0.7)
	for i in 4:
		var x := -0.42 + i * 0.28
		_malla(r, "Diente%d" % i, _caja(Vector3(0.03, 0.08, 0.14)), madera, Vector3(x - 0.06, 1.25, 0.02))
		_malla(r, "Diente%db" % i, _caja(Vector3(0.03, 0.08, 0.14)), madera, Vector3(x + 0.06, 1.25, 0.02))
		if i == 2:
			# El hueco: marca limpia en la madera polvorienta
			_malla(r, "MarcaHueco", _caja(Vector3(0.08, 0.005, 0.1)), _mat(Color(0.5, 0.36, 0.22), 0.6), Vector3(x, 0.162, 0.02))
			continue
		var remo_n := Node3D.new()
		remo_n.name = "Remo%d" % i
		remo_n.position = Vector3(x, 0.16, 0.02)
		r.add_child(remo_n)
		_malla(remo_n, "Mango", _cilindro(0.018, 0.018, 1.0, 8), remo, Vector3(0, 0.75, 0))
		_malla(remo_n, "Pala", _caja(Vector3(0.16, 0.42, 0.02)), remo, Vector3(0, 0.22, 0))
		_malla(remo_n, "Puno", _caja(Vector3(0.09, 0.03, 0.03)), remo, Vector3(0, 1.26, 0))
	var polvo := _mat(Color(0.62, 0.58, 0.5), 0.95)
	_malla(r, "Polvo", _caja(Vector3(1.18, 0.003, 0.16)), polvo, Vector3(0, 0.161, 0.02))
	var marcador := Marker3D.new()
	marcador.name = "Detalle_hueco"
	marcador.position = Vector3(0.14, 0.3, 0.12)
	r.add_child(marcador)
	return r

## Remo de canoa con sangre en la pala, tirado entre los juncos
func _remo_sangre() -> Node3D:
	var r := Node3D.new()
	var madera := _mat(Color(0.5, 0.35, 0.19), 0.75)
	var remo := Node3D.new()
	remo.name = "Remo"
	remo.rotation_degrees = Vector3(90, 0, 0)
	remo.position = Vector3(0, 0.025, 0)
	r.add_child(remo)
	_malla(remo, "Mango", _cilindro(0.018, 0.018, 1.0, 8), madera, Vector3(0, 0.75, 0))
	_malla(remo, "Pala", _caja(Vector3(0.16, 0.42, 0.02)), madera, Vector3(0, 0.22, 0))
	_malla(remo, "Puno", _caja(Vector3(0.09, 0.03, 0.03)), madera, Vector3(0, 1.26, 0))
	var sangre := StandardMaterial3D.new()
	sangre.albedo_texture = load(TEX + "sangre.png")
	sangre.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	sangre.roughness = 0.4
	var q := QuadMesh.new()
	q.size = Vector2(0.17, 0.2)
	for lado in [1.0, -1.0]:
		var m := _malla(remo, "Sangre%s" % ("A" if lado > 0 else "B"), q, sangre, Vector3(0, 0.33, 0.0105 * lado), Vector3(0, 0 if lado > 0 else 180, 0))
		m.name = "Sangre%s" % ("A" if lado > 0 else "B")
	var marcador := Marker3D.new()
	marcador.name = "Detalle_sangre"
	marcador.position = Vector3(0, 0.05, -0.33)
	marcador.rotation_degrees = Vector3(-90, 0, 0)
	r.add_child(marcador)
	return r

## Linterna de Porky (Poly Haven Vintage Flashlight), encendida
func _linterna() -> Node3D:
	var r := Node3D.new()
	_instancia(r, PH + "vintage_flashlight/vintage_flashlight_1k.gltf", "Linterna")
	var lente := StandardMaterial3D.new()
	lente.albedo_color = Color(1, 0.9, 0.7)
	lente.emission_enabled = true
	lente.emission = Color(1, 0.85, 0.55)
	lente.emission_energy_multiplier = 6.0
	var disco := _cilindro(0.041, 0.041, 0.004, 20)
	# El haz (SpotLight3D) lo pone el escenario: una luz dentro del modelo agrandaría su caja en la vista de examen
	_malla(r, "Lente", disco, lente, Vector3(0, 0.096, 0.155), Vector3(90, 0, 0))
	return r

## Cuaderno de dibujo de Eriz, abierto en la página de esa noche
func _cuaderno() -> Node3D:
	var r := Node3D.new()
	var tapa := _mat(Color(0.12, 0.2, 0.28), 0.8)
	var papel := _mat(Color(0.93, 0.9, 0.82), 0.95)
	_malla(r, "TapaIzq", _caja(Vector3(0.155, 0.004, 0.215)), tapa, Vector3(-0.08, 0.002, 0), Vector3(0, 0, 3))
	_malla(r, "TapaDer", _caja(Vector3(0.155, 0.004, 0.215)), tapa, Vector3(0.08, 0.002, 0), Vector3(0, 0, -3))
	_malla(r, "HojasIzq", _caja(Vector3(0.148, 0.01, 0.205)), papel, Vector3(-0.077, 0.009, 0), Vector3(0, 0, 3))
	_malla(r, "HojasDer", _caja(Vector3(0.148, 0.01, 0.205)), papel, Vector3(0.077, 0.009, 0), Vector3(0, 0, -3))
	var espiral := _mat(Color(0.6, 0.6, 0.62), 0.35, 0.8)
	for k in 9:
		var a := TorusMesh.new()
		a.inner_radius = 0.005
		a.outer_radius = 0.007
		_malla(r, "Espiral%d" % k, a, espiral, Vector3(0, 0.012, -0.09 + k * 0.0225), Vector3(0, 0, 90))
	var tinta := Color(0.12, 0.14, 0.32)
	# Página izquierda: un dibujo rápido del lago (rayas) y la luna
	_texto(r, "Dibujo", "~ ~ ~  )  ~ ~\n  ~ ~ ~ ~ ~\n   |=====|", Vector3(-0.14, 0.0165, -0.085), Vector3(-90, 0, 0), 40, tinta, 0.0004)
	_texto(r, "Fecha", "14 ago.  Lago Espejo", Vector3(-0.14, 0.0165, 0.04), Vector3(-90, 0, 0), 34, tinta, 0.0004)
	# Página derecha: las notas de la noche
	_texto(r, "Notas", "23:00  se fue la luz en\ntodo el campamento.\n23:05  ¿bramidos en el\nbosque??\n23:20: perturbación luminosa\nfuerte junto al embarcadero.\n(¿un pez?)",
		Vector3(0.014, 0.0178, -0.09), Vector3(-90, 0, 0), 30, tinta, 0.0004)
	var marcador := Marker3D.new()
	marcador.name = "Detalle_nota"
	marcador.position = Vector3(0.075, 0.015, -0.02)
	marcador.rotation_degrees = Vector3(-90, 0, 0)
	r.add_child(marcador)
	return r

## Tramo de lodo del sendero con huellas de pezuña hondas; «detenidas»: las últimas dos y después nada
func _huellas(cuantas: int, detenidas: bool) -> Node3D:
	var r := Node3D.new()
	var tierra := StandardMaterial3D.new()
	tierra.albedo_texture = load("res://Assets/Caso1/Texturas/suelo_tierra_color.jpg")
	tierra.normal_enabled = true
	tierra.normal_texture = load("res://Assets/Caso1/Texturas/suelo_tierra_normal.jpg")
	tierra.uv1_scale = Vector3(0.6, 0.6, 0.6)
	tierra.uv1_triplanar = true
	tierra.roughness = 0.95
	var largo := 2.6 if not detenidas else 2.2
	_malla(r, "Lodo", _caja(Vector3(0.9, 0.06, largo)), tierra, Vector3(0, 0.03, 0))
	var textura: Texture2D = load(TEX + "huella_pezuna.png")
	for i in cuantas:
		var d := Decal.new()
		d.name = "Huella%d" % i
		d.texture_albedo = textura
		d.size = Vector3(0.24, 0.2, 0.3)
		# Paso demasiado largo para un venado: 1.1 m entre huellas
		var z := largo / 2 - 0.35 - i * 1.1 * (0.6 if detenidas else 0.55)
		d.position = Vector3(0.14 if i % 2 == 0 else -0.14, 0.06, z)
		d.rotation_degrees = Vector3(0, 180, 0)
		d.upper_fade = 0.2
		d.lower_fade = 0.2
		r.add_child(d)
	return r
