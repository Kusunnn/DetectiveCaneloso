extends SceneTree
## HU-20.6: arma res://Escenas/Caso1/Interrogatorio/sala_interrogatorio_caso1.tscn a partir de la sala del Caso 0
## (sin tocarla): quita todo lo que es de la historia del Caso 0 (su guion, sus pistas, el pasillo, la recepción y
## los vestidores) y agrega lo del Caso 1: los seis implicados que pasan uno por uno, Hecdog junto a la puerta,
## el corcho con fotos y estambre rojo, la mesa con las pruebas y la puerta para volver.
## Headless. Uso: --headless --path game --script res://Herramientas/Caso1/sala_caso1.gd

const ORIGEN := "res://Escenas/Interrogatorio/sala_interrogatorio.tscn"
const SALIDA := "res://Escenas/Caso1/Interrogatorio/sala_interrogatorio_caso1.tscn"
const PISO := 0.5
const ANIM := "res://Escenas/Investigacion/AnimacionPersonaje.cs"
const PUNTO := "res://Escenas/Investigacion/PuntoInteraccion.cs"
const CARTEL := "res://Escenas/Investigacion/CartelPared.cs"
const CAPA_INTER := 8
const CAPA_NPC := 16
const CAPA_BLOQUEO := 32
const CAPA_MUEBLES := 2

# Lo que solo existe por la historia del Caso 0
const QUITAR := ["Caso0", "GuiaTutorial", "PuntoErizFinal", "PuntoPorkyFinal", "SillaAcusacion", "GrabadoraPista", "RelojPista",
	"PlumaSecreta", "ZonaPasillo", "ZonaRecepcion", "ZonaVestuario", "CanvasLayer", "FocoReloj", "PuertaSalaColision"]

# nodo: [nombre, modelo, alto en el modelo, centro (x, pies, z) en el modelo, "alto" (m) o "esc" (escala fija), valor, animación de reposo]
# Mismas medidas que la vista previa de personajes del Campamento (_generador_caso1/personajes.py)
const IMPLICADOS := {
	"JuanCarlos": ["Juan Carlos", "res://Assets/Models/Personajes/lala.glb", 0.259, Vector3(0.0, 0.0, -0.0855), "alto", 1.65, ""],
	"Aya": ["Aya", "res://Assets/Models/Aya/Ayas.tscn", 44.56, Vector3(0.124, -0.143, 26.497), "alto", 1.6, "Inaya"],
	"Miawrio": ["Miawrio", "res://Assets/Models/Miaurio/miaurio.tscn", 365.8, Vector3(324.527, -0.547, 101.452), "alto", 1.7, "global/Inactivo"],
	"Eriz": ["Eriz", "res://Assets/Models/Personajes/eriz.glb", 0.353, Vector3(0.0, 0.0, -0.0016), "esc", 4.77, ""],
	"Mimita": ["Mimita", "res://Assets/Models/Personajes/miami.glb", 0.154, Vector3(0.0, 0.0, 0.0003), "alto", 1.5, ""],
	"Tony": ["Tony", "res://Assets/Models/Tony/Tony.tscn", 2.768, Vector3(0.008, -0.0015, -0.035), "alto", 1.8, "global/InacTony"],
}

var _raiz: Node3D

func _initialize() -> void:
	_raiz = load(ORIGEN).instantiate()
	_raiz.name = "SalaInterrogatorioCaso1"
	for nombre in QUITAR:
		var n := _raiz.get_node_or_null(nombre)
		if n:
			_raiz.remove_child(n)
			n.free()
		else:
			push_warning("No encontré " + nombre)

	# La mesa del centro deja de ser una «pista» del Caso 0: queda como mueble
	var mesa := _raiz.get_node("MesaPista")
	mesa.set_script(null)
	mesa.name = "Mesa"
	# Rótulos del Caso 0 cambiados por los de este caso
	var rotulo := _raiz.get_node("RotuloExpediente") as Label3D
	rotulo.text = "EXP. 0412 · CAMP. PINO LUNAR\nLa noche del ciervo de luz"
	_raiz.get_node("CartelEmpleadoMes").set("Texto", "EMPLEADO DEL MES\n(sin asignar)")
	_raiz.get_node("HojaLimpieza").set("Texto", "SALA 1 · LIMPIEZA\nPendiente")

	_puerta()
	_marcadores()
	_hecdog()
	var implicados := _nodo(Node3D.new(), "Implicados", _raiz)
	for clave in IMPLICADOS:
		_implicado(clave, IMPLICADOS[clave], implicados)
	_corcho()
	_mesa_evidencias()
	_caso()

	DirAccess.make_dir_recursive_absolute(SALIDA.get_base_dir())
	var escena := PackedScene.new()
	print("pack: ", error_string(escena.pack(_raiz)))
	print("guardar: ", error_string(ResourceSaver.save(escena, SALIDA)))
	_raiz.free()
	quit()

# ---------- utilidades ----------

func _nodo(n: Node, nombre: String, padre: Node, pos := Vector3.ZERO, giro_y := 0.0) -> Node:
	n.name = nombre
	if n is Node3D:
		n.position = pos
		n.rotation_degrees = Vector3(0, giro_y, 0)
	padre.add_child(n)
	n.owner = _raiz
	return n

func _instancia(ruta: String, nombre: String, padre: Node, pos := Vector3.ZERO, giro_y := 0.0, escala := 1.0) -> Node3D:
	var n: Node3D = load(ruta).instantiate()
	_nodo(n, nombre, padre, pos, giro_y)
	n.scale = Vector3.ONE * escala
	return n

func _caja_malla(nombre: String, padre: Node, pos: Vector3, tam: Vector3, color: Color, aspereza := 0.85) -> MeshInstance3D:
	var mi := MeshInstance3D.new()
	var caja := BoxMesh.new()
	caja.size = tam
	var m := StandardMaterial3D.new()
	m.albedo_color = color
	m.roughness = aspereza
	caja.material = m
	mi.mesh = caja
	return _nodo(mi, nombre, padre, pos)

func _cuerpo(nombre: String, padre: Node, pos: Vector3, tam: Vector3, capa: int, script_ruta := "") -> StaticBody3D:
	var b := StaticBody3D.new()
	b.collision_layer = capa
	b.collision_mask = 0
	if script_ruta != "": b.set_script(load(script_ruta))
	_nodo(b, nombre, padre, pos)
	var cs := CollisionShape3D.new()
	var forma := BoxShape3D.new()
	forma.size = tam
	cs.shape = forma
	_nodo(cs, "Forma", b)
	return b

# ---------- partes ----------

## La puerta de la sala queda cerrada (en el Caso 0 daba al pasillo) y es la salida
func _puerta() -> void:
	var hoja := _raiz.get_node("Detalles/Puerta") as Node3D
	hoja.transform = Transform3D(Basis.IDENTITY, Vector3(2.5, 1.55, 3.06))
	var salida := _cuerpo("SalidaSala", _raiz, Vector3(2.5, 1.55, 3.0), Vector3(1.0, 2.1, 0.3), CAPA_INTER | CAPA_BLOQUEO, PUNTO)
	salida.set("Accion", "Salir de la sala")
	salida.add_to_group("auditoria_invisible_ok", true)

func _marcadores() -> void:
	# Por donde llega el detective (desde el Lago o el Campamento)
	_nodo(Marker3D.new(), "EntradaDesdeLago", _raiz, Vector3(2.4, PISO + 1.05, 1.9), 0.0)
	_nodo(Marker3D.new(), "EntradaDesdeCampamento", _raiz, Vector3(2.4, PISO + 1.05, 1.9), 0.0)
	# Los implicados entran por la puerta y se quedan detrás de la silla del sospechoso, de frente al detective
	_nodo(Marker3D.new(), "PuntoEntradaImplicado", _raiz, Vector3(2.5, PISO, 2.45), 180.0)
	_nodo(Marker3D.new(), "PuntoSilla", _raiz, Vector3(0.0, PISO, -3.45), 0.0)
	_nodo(Marker3D.new(), "PuntoHecdog", _raiz, Vector3(1.55, PISO, 2.55), 180.0)

## Hecdog, de guardia junto a la puerta (mismo modelo y escala que el comisario del Caso 0)
func _hecdog() -> void:
	var g := _nodo(Node3D.new(), "Hecdog", _raiz, Vector3(1.55, PISO, 2.55), 200.0)
	g.add_to_group("auditoria_personaje", true)
	var modelo := _instancia("res://Assets/Models/poli/Poli/Neutral Idle.fbx", "Modelo", g, Vector3(0, 0.004, 0), 0.0, 380.0)
	var anim := Node.new()
	anim.set_script(load(ANIM))
	_nodo(anim, "Animacion", g)
	anim.set("Modelo", modelo)
	anim.set("Animacion", "mixamo_com")
	var b := _cuerpo("Colision", g, Vector3(0, 0.95, 0), Vector3(0.6, 1.9, 0.6), CAPA_NPC | CAPA_INTER, PUNTO)
	b.position = Vector3.ZERO
	(b.get_node("Forma") as CollisionShape3D).position = Vector3(0, 0.95, 0)
	b.set("Accion", "Hablar con Hecdog")
	b.add_to_group("auditoria_invisible_ok", true)
	_letrero(g, "Hecdog", 2.15)

func _implicado(clave: String, d: Array, padre: Node) -> void:
	var g := _nodo(Node3D.new(), clave, padre, Vector3(0.0, PISO, -3.45), 0.0) as Node3D
	g.add_to_group("auditoria_personaje", true)
	g.visible = false
	var alto: float = d[5] if d[4] == "alto" else d[2] * d[5]
	var escala: float = d[5] / d[2] if d[4] == "alto" else d[5]
	if ResourceLoader.exists(d[1]):
		var c: Vector3 = d[3]
		var modelo := _instancia(d[1], "Modelo", g, -c * escala, 0.0, escala)
		var anim := Node.new()
		anim.set_script(load(ANIM))
		_nodo(anim, "Animacion", g)
		anim.set("Modelo", modelo)
		anim.set("AlturaOjos", snappedf(alto * 0.8, 0.01))
		if d[6] != "": anim.set("Animacion", d[6])
	else:
		_marcador_sin_modelo(g, d[0], alto)
	var b := _cuerpo("Colision", g, Vector3.ZERO, Vector3(0.64, alto, 0.64), 0, PUNTO)
	(b.get_node("Forma") as CollisionShape3D).position = Vector3(0, alto / 2, 0)
	b.set("Accion", "Hablar con " + d[0])
	b.add_to_group("auditoria_invisible_ok", true)
	_letrero(g, d[0], alto + 0.25)

## Un personaje sin modelo todavía: una silueta de cartón con su retrato (si existe) y su nombre, sin inventar un modelo
func _marcador_sin_modelo(g: Node3D, nombre: String, alto: float) -> void:
	var mi := MeshInstance3D.new()
	var q := QuadMesh.new()
	q.size = Vector2(0.8, alto)
	var m := StandardMaterial3D.new()
	var retrato := "res://Assets/Retratos/%s.png" % nombre.to_lower()
	if ResourceLoader.exists(retrato): m.albedo_texture = load(retrato)
	else: m.albedo_color = Color(0.25, 0.22, 0.2)
	q.material = m
	mi.mesh = q
	_nodo(mi, "SinModelo", g, Vector3(0, alto / 2, 0))

func _letrero(g: Node3D, texto: String, alto: float) -> void:
	var l := Label3D.new()
	l.text = texto
	l.pixel_size = 0.0028
	l.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	l.font_size = 40
	l.outline_size = 10
	l.modulate = Color(1, 0.93, 0.75)
	l.visible = false  # La interfaz de diálogo lo enciende si hace falta (vista previa: solo en el editor)
	_nodo(l, "Nombre", g, Vector3(0, alto, 0))

## Corcho en la pared del fondo: el marco y el corcho; las fotos y el estambre rojo los pone el caso al entrar
func _corcho() -> void:
	var c := _nodo(Node3D.new(), "Corcho", _raiz, Vector3(0.0, 2.05, -4.985), 0.0) as Node3D
	c.add_to_group("auditoria_pared", true)
	_caja_malla("Marco", c, Vector3(0, 0, 0.012), Vector3(2.7, 1.45, 0.024), Color(0.3, 0.2, 0.12))
	_caja_malla("Superficie", c, Vector3(0, 0, 0.026), Vector3(2.56, 1.31, 0.008), Color(0.62, 0.45, 0.28), 1.0)
	_nodo(Node3D.new(), "Fotos", c, Vector3(0, 0, 0.032))

## Mesa auxiliar con las pruebas recogidas (las acomoda el caso según el diario)
func _mesa_evidencias() -> void:
	var g := _nodo(Node3D.new(), "MesaEvidencias", _raiz, Vector3(-2.75, PISO, -4.5), 0.0) as Node3D
	var madera := Color(0.33, 0.22, 0.14)
	_caja_malla("Cubierta", g, Vector3(0, 0.78, 0), Vector3(1.9, 0.05, 0.7), madera)
	for x in [-0.88, 0.88]:
		for z in [-0.3, 0.3]:
			_caja_malla("Pata%d%d" % [int(x * 10), int(z * 10)], g, Vector3(x, 0.38, z), Vector3(0.05, 0.76, 0.05), madera)
	_caja_malla("Mantel", g, Vector3(0, 0.81, 0), Vector3(1.86, 0.008, 0.66), Color(0.12, 0.13, 0.15), 0.95)
	var b := _cuerpo("Colision", g, Vector3(0, 0.4, 0), Vector3(1.9, 0.8, 0.7), CAPA_MUEBLES)
	_nodo(Node3D.new(), "Pruebas", g, Vector3(0, 0.815, 0))
	var cartel := Node3D.new()
	cartel.set_script(load(CARTEL))
	_nodo(cartel, "Rotulo", g, Vector3(0, 0.83, 0.33))
	cartel.rotation_degrees = Vector3(-75, 0, 0)
	cartel.set("Texto", "PRUEBAS · CASO 01")
	cartel.set("Ancho", 0.42)
	cartel.set("TamFuente", 26)
	cartel.add_to_group("auditoria_colgado", true)

func _caso() -> void:
	var caso := Node.new()
	caso.set_script(load("res://Escenas/Caso1/Interrogatorio/SalaInterrogatorioCaso1.cs"))
	_nodo(caso, "Caso1Sala", _raiz)
	caso.set("Jugador", _raiz.get_node("Jugador"))
	caso.set("Hud", _raiz.get_node("HUD"))
	caso.set("Dialogo", _raiz.get_node("VentanaDialogo"))
	caso.set("Tablero", _raiz.get_node("TableroDeduccion"))
	caso.set("Musica", _raiz.get_node("MusicaFondo"))
	caso.set("Entorno", _raiz.get_node("WorldEnvironment"))
	caso.set("Salida", _raiz.get_node("SalidaSala"))
	caso.set("Hecdog", _raiz.get_node("Hecdog/Colision"))
	caso.set("Implicados", _raiz.get_node("Implicados"))
	caso.set("PuntoSilla", _raiz.get_node("PuntoSilla"))
	caso.set("PuntoEntradaImplicado", _raiz.get_node("PuntoEntradaImplicado"))
	caso.set("Corcho", _raiz.get_node("Corcho/Fotos"))
	caso.set("MesaEvidencias", _raiz.get_node("MesaEvidencias/Pruebas"))
