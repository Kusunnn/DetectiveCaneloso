extends SceneTree
## Capítulo 2 · 3.4 Escape: escenas cortas para el rastreo con olfato y las puertas de «Tito pregunta».
## Escaleras de emergencia, piso 23 (cuarto del servidor), cocina, cuarto de basura y estacionamiento.
## Solo escenarios y puntos preparados (PanelTarjeta, PuertaTito_N, Cruce_N, ServidorDisco, Tecnico_A/B, Patrulla):
## ninguna mecánica. Las salidas que dependen de una mecánica empiezan apagadas.
## Uso: <Godot>_console.exe --headless --path game --script res://Herramientas/Caso2/generar_escape.gd [-- escena ...]

const Constructor := preload("res://Herramientas/Caso2/constructor.gd")
const PROPS := "res://Assets/Caso2/Modelos/props/"
const C2 := "res://Escenas/Caso2/"
const DESPACHO := C2 + "torre_despacho.tscn"
const ESCALERAS := C2 + "torre_escaleras.tscn"
const PISO23 := C2 + "torre_piso23.tscn"
const COCINA := C2 + "torre_cocina.tscn"
const BASURA := C2 + "torre_basura.tscn"
const ESTACIONAMIENTO := C2 + "torre_estacionamiento.tscn"

var k: Constructor

func _initialize() -> void:
	var solo := OS.get_cmdline_user_args()
	for e in [["escaleras", _escaleras], ["piso23", _piso23], ["cocina", _cocina], ["basura", _basura], ["estacionamiento", _estacionamiento]]:
		if solo.is_empty() or e[0] in solo:
			(e[1] as Callable).call()
	quit()

func _empezar(nombre: String, carpeta: String) -> Node3D:
	var raiz := Node3D.new()
	raiz.name = nombre
	k = Constructor.new(raiz, carpeta)
	return raiz

func _terminar(raiz: Node3D, ruta: String) -> void:
	k.guardar(ruta)
	raiz.free()

func _letrero(padre: Node, texto: String, pos: Vector3, rot_y: float, tam := 160, color := Color("e3b23c")) -> Label3D:
	var l := Label3D.new()
	l.name = "Letrero_" + texto.validate_node_name()
	l.text = texto
	l.font = load("res://Assets/Fuentes/OldStandard-Bold.ttf")
	l.font_size = tam
	l.pixel_size = 0.004
	l.modulate = color
	l.outline_size = 0
	l.position = pos
	l.rotation_degrees.y = rot_y
	padre.add_child(l)
	return l

func _mats_servicio() -> Dictionary:
	return {
		"Muro": k.mat_color("serv_muro", Color("c9c6bd"), 0.9, Vector2(1, 1), 0.05),
		"Zocalo": k.mat_color("serv_zocalo", Color("8a8a85"), 0.8, Vector2(1, 1), 0.05),
		"Marco": k.mat_color("serv_marco", Color("4a5058"), 0.5, Vector2(1, 1), 0.04, 0.4),
	}

func _piso_concreto(nombre: String) -> Mesh:
	return k.malla_kit("piso_losas", {
		"Losa": k.mat_textura(nombre + "_losa", "Concrete034", 0.5, Color(0.85, 0.84, 0.8)),
		"Losa2": k.mat_textura(nombre + "_losa2", "Concrete034", 0.5, Color(0.8, 0.79, 0.75)),
	})

func _techo_servicio() -> Dictionary:
	return {"Plafon": k.mat_color("serv_plafon", Color("e4e2dc"), 0.95, Vector2(1, 1), 0.02)}

# ---------- Escaleras de emergencia (3 × 6 m, tres tramos, del piso 30 al 23) ----------

func _escaleras() -> void:
	var raiz := _empezar("TorreEscaleras", "escaleras")
	var mats := _mats_servicio()
	k.cuarto("Cuarto", Vector3.ZERO, 3, 6, 4.0, {}, mats, _piso_concreto("esc"), {}, false)
	var arriba := k.nodo("MurosAltos")
	k.tramo_muro("Norte", arriba, Vector3(0, 4.0, 0), 3, 0.0, 4.0, mats)
	k.tramo_muro("Sur", arriba, Vector3(3, 4.0, 6), 3, 180.0, 4.0, mats)
	k.tramo_muro("Oeste", arriba, Vector3(0, 4.0, 6), 6, 90.0, 4.0, mats)
	k.tramo_muro("Este", arriba, Vector3(3, 4.0, 0), 6, -90.0, 4.0, mats)
	k.techo(arriba, "Techo", 0, 0, 3, 6, 8.0, _techo_servicio())
	var e := k.nodo("Escalera")
	var ang := rad_to_deg(atan2(1.5, 3.0))
	var largo := sqrt(3.0 * 3.0 + 1.5 * 1.5)
	# Tramos: T3 (0 → 1.5, sube al sur, carril oeste), T2 (1.5 → 3.0, sube al norte, carril este), T1 (3.0 → 4.5, sur)
	for t in [["Tramo3", Vector3(0.65, 0.0, 1.5), 180.0], ["Tramo2", Vector3(2.35, 1.5, 4.5), 0.0], ["Tramo1", Vector3(0.65, 3.0, 1.5), 180.0]]:
		var pos: Vector3 = t[1]
		k.modelo(PROPS + "tramo_escalera.glb", e, pos, t[2], 1.0, t[0])
		var sube_sur: bool = t[2] == 180.0
		var centro := pos + Vector3(0, 0.75 - 0.1, 1.5 if sube_sur else -1.5)
		var col := k.colision("Colision" + t[0], e, centro, Vector3(1.3, 0.2, largo))
		col.rotation_degrees.x = -ang if sube_sur else ang
	for d in [["DescansoA", Vector3(1.5, 3.0, 0.75)], ["DescansoB", Vector3(1.5, 1.5, 5.25)], ["DescansoArriba", Vector3(1.5, 4.5, 5.25)]]:
		var p: Vector3 = d[1]
		k.modelo(PROPS + "descanso.glb", e, p, 0.0, 1.0, d[0])
		k.colision("Colision" + d[0], e, p - Vector3(0, 0.1, 0), Vector3(3.0, 0.2, 1.5))
	# Bajo la escalera no se camina (el hueco bajo el tramo 2 y el descanso B)
	k.colision("BajoEscalera", e, Vector3(2.15, 0.7, 3.75), Vector3(1.7, 1.4, 4.5), Constructor.CAPA_LIMITES).add_to_group("auditoria_invisible_ok", true)
	# Puertas: piso 30 arriba (con el panel de la tarjeta), piso 24 en el descanso B (Tito) y piso 23 abajo
	var pz := k.nodo("Puertas")
	k.pieza_kit("puerta_hoja_metal", pz, Vector3(0.75 - 0.43, 4.51, 5.94), 180.0, {}, "PuertaPiso30")
	var panel := k.modelo(PROPS + "panel_tarjeta.glb", pz, Vector3(1.55, 5.7, 6.0), 180.0, 1.0, "PanelTarjeta")
	panel.add_to_group("auditoria_pared", true)
	var piloto := OmniLight3D.new()
	piloto.name = "Piloto"
	piloto.light_color = Color("ff3020")
	piloto.light_energy = 0.4
	piloto.omni_range = 0.6
	piloto.position = Vector3(0, 0.06, 0.08)
	panel.add_child(piloto)
	var volver := k.punto(pz, "Salida", "Volver al piso 30", Vector3(0.75, 5.55, 5.85), Vector3(0.9, 2.1, 0.2))
	k.puerta_tito(pz, 1, Vector3(2.25, 1.5, 6.0), 180.0)
	k.pieza_kit("puerta_hoja_metal", pz, Vector3(1.5 - 0.43, 0.01, 0.06), 0.0, {}, "PuertaPiso23")
	var bajar := k.punto(pz, "SalidaPiso23", "Ir al piso 23", Vector3(1.5, 1.05, 0.15), Vector3(0.9, 2.1, 0.2))
	_letrero(pz, "30", Vector3(2.3, 6.4, 5.97), 180.0)
	_letrero(pz, "24", Vector3(0.9, 3.4, 5.97), 180.0)
	_letrero(pz, "23", Vector3(0.6, 2.0, 0.03), 0.0)
	_letrero(pz, "SALIDA", Vector3(1.5, 2.35, 0.03), 0.0, 60, Color("3ad16a"))
	k.cruce(k.raiz, 1, Vector3(2.35, 3.0, 0.9), 0.0, "mancha_aceite", Vector3(-1.7, 0, 1.0), Vector3(0, -1.5, 3.5))
	# Luces: normal (plafones) y hackeado (emergencia roja en cada descanso)
	var normal := k.nodo("LucesNormal")
	var hack := k.nodo("LucesHackeado")
	for y in [2.6, 5.6, 7.6]:
		k.kenney("furniture-kit", "lampSquareCeiling", normal, Vector3(1.5, y - 0.4 if y < 7 else 7.5, 3.0), 0.0, 2.2) if y > 7 else null
		k.omni(normal, "Plafon", Vector3(1.5, y, 3.0), Color("f2efe6"), 0.9, 5.0, y == 5.6)
	for p in [[Vector3(0.0, 2.6, 0.75), 90.0], [Vector3(3.0, 4.1, 5.25), -90.0], [Vector3(0.0, 5.6, 4.5), 90.0]]:
		var pos: Vector3 = p[0]
		k.modelo(PROPS + "luz_emergencia.glb", hack, pos, p[1], 1.0, "LuzEmergencia").add_to_group("auditoria_pared", true)
		k.omni(hack, "Emergencia", pos + Basis(Vector3.UP, deg_to_rad(p[1])) * Vector3(0, -0.1, 0.5), Color("ff2a2a"), 1.6, 6.0, pos.y > 5.0)
	k.omni(hack, "SalidaVerde", Vector3(1.5, 2.2, 0.4), Color("3ad16a"), 0.6, 3.0)
	var env := k.entorno(Color("2a2f3a"), Color("3a3f4a"), Color("b8b4aa"), 0.45, 1.0)
	env.environment.background_mode = Environment.BG_COLOR
	env.environment.background_color = Color("101216")
	k.estado_luces([normal], [hack], env, true)
	var esc := k.base_jugable("Torre Miawrio · Escaleras", Vector3(1.5, 4.5, 5.3), 0.0)
	esc.set("Salidas", [volver, bajar])
	esc.set("Destinos", PackedStringArray([DESPACHO + "|Entrada_DesdeElevador", PISO23 + "|Entrada_DesdeEscaleras"]))
	k.marcador("Entrada_DesdeDespacho", k.raiz, Vector3(1.5, 4.5, 5.3), 0.0)
	k.marcador("Entrada_DesdePiso23", k.raiz, Vector3(1.5, 0, 0.9), 180.0)
	k.ambiente_sonoro("Ambiente", raiz, Vector3.ZERO, "res://Assets/Audio/Ambiente/zumbido_fluorescente.ogg", -22.0)
	_terminar(raiz, ESCALERAS)

# ---------- Piso 23 · Cuarto del servidor (8 × 6 m) y el pasillo con vidrio ----------

func _piso23() -> void:
	var raiz := _empezar("TorrePiso23", "piso23")
	var mats := _mats_servicio()
	k.cuarto("Cuarto", Vector3.ZERO, 8, 8, 2.8, {"oeste": {7: "puerta"}, "este": {7: "puerta"}}, mats, _piso_concreto("p23"), _techo_servicio())
	# Mampara de vidrio entre el cuarto (norte) y el pasillo (sur, z 6–8): el reflejo de la foto de Aya
	var mampara := {"Muro": mats["Muro"], "Zocalo": mats["Zocalo"], "Marco": k.mat_color("p23_marco", Color("2b2f36"), 0.4, Vector2(1, 1), 0.04, 0.5),
		"Vidrio": k.material("p23_vidrio", {"albedo_color": Color(0.7, 0.85, 0.95, 0.22), "transparency": BaseMaterial3D.TRANSPARENCY_ALPHA, "roughness": 0.02, "metallic": 0.6})}
	k.tramo_muro("Mampara", k.raiz, Vector3(8, 0, 6), 8, 180.0, 2.8, mampara, {0: "ventanal", 1: "ventanal", 2: "ventanal", 3: "ventanal", 4: "ventanal", 5: "puerta", 6: "ventanal", 7: "ventanal"})
	var r := k.nodo("Racks")
	for i in 4:
		k.modelo(PROPS + "rack_servidor.glb", r, Vector3(1.2 + i * 0.62, 0, 1.0), 0.0, 1.0, "RackA%d" % i)
		k.modelo(PROPS + "rack_servidor.glb", r, Vector3(1.2 + i * 0.62, 0, 4.3), 180.0, 1.0, "RackB%d" % i)
	for i in 2:
		k.modelo(PROPS + "rack_servidor.glb", r, Vector3(5.6 + i * 0.62, 0, 4.3), 180.0, 1.0, "RackC%d" % i)
	k.colision("ColisionRacksA", r, Vector3(2.13, 1.0, 1.0), Vector3(2.5, 2.0, 1.0), Constructor.CAPA_MUEBLES)
	k.colision("ColisionRacksB", r, Vector3(3.3, 1.0, 4.3), Vector3(4.9, 2.0, 1.0), Constructor.CAPA_MUEBLES)
	# ServidorDisco: el rack de la bahía abierta, donde se conecta el disco
	var sd := k.nodo("ServidorDisco")
	k.modelo(PROPS + "rack_disco.glb", sd, Vector3(6.2, 0, 1.0), 0.0, 1.0, "Rack")
	k.modelo(PROPS + "disco_duro.glb", sd, Vector3(6.2, 1.11, 1.32), 0.0, 1.0, "Disco")
	k.colision("ColisionServidor", sd, Vector3(6.2, 1.0, 1.0), Vector3(0.6, 2.0, 1.0), Constructor.CAPA_MUEBLES)
	k.marcador("PuntoDisco", sd, Vector3(6.2, 0, 1.9), 180.0)
	k.modelo(PROPS + "aire_acondicionado.glb", r, Vector3(7.55, 0, 2.6), -90.0, 1.0, "AireAcondicionado")
	for z in [1.0, 4.3]:
		k.modelo(PROPS + "charola_cables.glb", r, Vector3(3.0, 2.35, z), 90.0, 1.0)
		k.modelo(PROPS + "charola_cables.glb", r, Vector3(5.0, 2.35, z), 90.0, 1.0)
	k.prefab("caja_carton", r, Vector3(0.4, 0, 5.5), 20.0, 1.0, "CajaRefacciones")
	k.prefab("silla_escritorio", r, Vector3(6.8, 0, 2.6), 60.0, 1.0, "SillaTecnico")
	# Pasillo: PuertaTito_2 al fondo (este) hacia la cocina; entrada desde las escaleras (oeste)
	var pz := k.nodo("Puertas")
	k.pieza_kit("puerta_hoja_metal", pz, Vector3(0.06, 0.01, 7.5 + 0.43), 90.0, {}, "PuertaEscaleras")
	var volver := k.punto(pz, "Salida", "Volver a las escaleras", Vector3(0.15, 1.05, 7.5), Vector3(0.2, 2.1, 0.9), 90.0)
	var tito := k.puerta_tito(pz, 2, Vector3(8.0, 0, 7.5), -90.0)
	var a_cocina := k.punto(tito, "Salida", "Ir a la cocina", Vector3(7.85, 1.05, 7.5), Vector3(0.2, 2.1, 0.9), -90.0)
	a_cocina.collision_layer = 0
	_letrero(pz, "23", Vector3(0.03, 2.2, 6.6), 90.0, 120)
	var normal := k.nodo("LucesNormal")
	var hack := k.nodo("LucesHackeado")
	for x in [2.0, 6.0]:
		k.modelo(PROPS + "lampara_tubo.glb", normal, Vector3(x, 2.73, 2.6), 90.0, 1.0)
		k.omni(normal, "Tubo", Vector3(x, 2.5, 2.6), Color("e6f0ff"), 0.9, 6.0, x == 2.0)
	k.omni(normal, "Pasillo", Vector3(4.0, 2.5, 7.0), Color("e6f0ff"), 0.6, 5.0)
	k.omni(k.raiz, "LedsRacks", Vector3(3.0, 1.2, 2.6), Color("3ddc84"), 0.4, 4.0)
	for p in [[Vector3(0.0, 2.3, 3.0), 90.0], [Vector3(8.0, 2.3, 3.0), -90.0]]:
		var pos: Vector3 = p[0]
		k.modelo(PROPS + "luz_emergencia.glb", hack, pos, p[1], 1.0, "LuzEmergencia").add_to_group("auditoria_pared", true)
		k.omni(hack, "Emergencia", pos + Basis(Vector3.UP, deg_to_rad(p[1])) * Vector3(0, -0.1, 0.5), Color("ff2a2a"), 1.5, 7.0, true)
	k.omni(hack, "FrioPasillo", Vector3(4.0, 2.4, 7.0), Color("7fa8ff"), 0.5, 5.0)
	var env := k.entorno(Color("1e2430"), Color("2a3240"), Color("aeb8c8"), 0.45, 1.0)
	env.environment.background_mode = Environment.BG_COLOR
	env.environment.background_color = Color("0e1116")
	k.estado_luces([normal], [hack], env, true)
	var esc := k.base_jugable("Torre Miawrio · Piso 23", Vector3(0.9, 0, 7.0), -90.0)
	esc.set("Salidas", [volver, a_cocina])
	esc.set("Destinos", PackedStringArray([ESCALERAS + "|Entrada_DesdePiso23", COCINA + "|Entrada_DesdePiso23"]))
	k.marcador("Entrada_DesdeEscaleras", k.raiz, Vector3(0.9, 0, 7.0), -90.0)
	k.ambiente_sonoro("ZumbidoServidor", raiz, Vector3(3.0, 1.2, 2.6), "res://Assets/Audio/Ambiente/zumbido_sala.ogg", -10.0, 12.0)
	_terminar(raiz, PISO23)

# ---------- Cocina del edificio (6 × 5 m) ----------

func _cocina() -> void:
	var raiz := _empezar("TorreCocina", "cocina")
	var mats := {
		"Muro": k.mat_color("coc_muro", Color("e9e6dc"), 0.6, Vector2(4, 4), 0.06),
		"Zocalo": k.mat_color("coc_zocalo", Color("9aa3a8"), 0.5, Vector2(1, 1), 0.05),
		"Marco": k.mat_color("coc_marco", Color("6d747a"), 0.4, Vector2(1, 1), 0.04, 0.5),
	}
	var piso := k.malla_kit("piso_losas", {"Losa": k.mat_color("coc_losa", Color("b9b2a3"), 0.4, Vector2(2, 2), 0.06), "Losa2": k.mat_color("coc_losa2", Color("a9a293"), 0.4, Vector2(2, 2), 0.06)})
	k.cuarto("Cuarto", Vector3.ZERO, 6, 5, 2.8, {"oeste": {2: "puerta"}, "este": {2: "puerta"}, "sur": {3: "puerta"}}, mats, piso, _techo_servicio())
	var m := k.nodo("Cocina")
	k.prefab("estufa", m, Vector3(1.6, 0, 0.95), 180.0, 1.0, "Estufa")
	k.prefab("cocina_tarja", m, Vector3(3.2, 0, 0.55), 180.0, 1.0, "Tarja")
	k.prefab("refri", m, Vector3(4.75, 0, 0.9), 180.0, 1.0, "Refri")
	for x in [1.0, 2.0, 3.2]:
		k.prefab("repisa_cocina", m, Vector3(x, 1.5, 0.16), 0.0, 1.0).add_to_group("auditoria_pared", true)
	var mesa := k.modelo(PROPS + "mesa_acero.glb", m, Vector3(3.0, 0, 2.6), 0.0, 1.0, "MesaAcero")
	k.colision("ColisionMesa", m, Vector3(3.0, 0.45, 2.6), Vector3(2.0, 0.9, 0.8), Constructor.CAPA_MUEBLES)
	var y := 0.905
	k.prefab("olla_grande", m, Vector3(2.3, y, 2.5), 0.0, 1.0, "Olla")
	k.prefab("olla", m, Vector3(2.9, y, 2.75), 30.0, 1.0, "Olla2")
	k.prefab("tabla_picar", m, Vector3(3.6, y, 2.5), 10.0, 1.0, "Tabla")
	k.prefab("sarten", m, Vector3(3.9, y, 2.8), -40.0, 1.0, "Sarten")
	k.prefab("huacal_papas", m, Vector3(0.5, 0, 4.4), 15.0, 1.0, "Huacal")
	k.prefab("escurridor", m, Vector3(3.55, k.tope(m.get_node("Tarja")) - 0.15, 0.42), 0.0, 1.0, "Escurridor")
	# Cruce_2: la puerta de servicio (este) que todavía se mueve
	var pz := k.nodo("Puertas")
	var hoja := k.pieza_kit("puerta_hoja_metal", pz, Vector3(5.94, 0.01, 2.5 - 0.43), -90.0, {}, "PuertaQueSeMueve")
	hoja.rotation_degrees.y = -90.0 + 28.0
	k.cruce(k.raiz, 2, Vector3(5.6, 0, 2.5), 0.0, "", Vector3(0.8, 0, 0), Vector3(-2.6, 0, 2.4))
	k.raiz.get_node("Cruce_2/Rastro").set_meta("rastro", "puerta que todavía se mueve: PuertaQueSeMueve")
	var volver := k.punto(pz, "Salida", "Volver al piso 23", Vector3(0.15, 1.05, 2.5), Vector3(0.2, 2.1, 0.9), 90.0)
	k.pieza_kit("puerta_hoja_metal", pz, Vector3(0.06, 0.01, 2.5 + 0.43), 90.0, {}, "PuertaPasillo")
	var tito := k.puerta_tito(pz, 3, Vector3(3.5, 0, 5.0), 180.0)
	var a_basura := k.punto(tito, "Salida", "Ir al cuarto de basura", Vector3(3.5, 1.05, 4.85), Vector3(0.9, 2.1, 0.2), 180.0)
	a_basura.collision_layer = 0
	var l := k.nodo("Luces")
	for x in [1.5, 4.5]:
		k.modelo(PROPS + "lampara_tubo.glb", l, Vector3(x, 2.73, 2.5), 0.0, 1.0)
		k.omni(l, "Tubo", Vector3(x, 2.5, 2.5), Color("f6f2e8"), 0.9, 5.5, x == 1.5)
	k.omni(l, "Rojo", Vector3(3.5, 2.3, 4.6), Color("ff3a30"), 0.6, 3.5)
	var env := k.entorno(Color("2a2f3a"), Color("3a3f4a"), Color("c8c2b4"), 0.5, 1.0)
	env.environment.background_mode = Environment.BG_COLOR
	env.environment.background_color = Color("101216")
	var esc := k.base_jugable("Torre Miawrio · Cocina", Vector3(0.9, 0, 2.5), -90.0)
	esc.set("Salidas", [volver, a_basura])
	esc.set("Destinos", PackedStringArray([PISO23 + "|Entrada_DesdeCocina", BASURA + "|Entrada_DesdeCocina"]))
	k.marcador("Entrada_DesdePiso23", k.raiz, Vector3(0.9, 0, 2.5), -90.0)
	k.ambiente_sonoro("Ambiente", raiz, Vector3.ZERO, "res://Assets/Audio/Ambiente/zumbido_fluorescente.ogg", -20.0)
	_terminar(raiz, COCINA)

# ---------- Cuarto de basura (4 × 4 m) ----------

func _basura() -> void:
	var raiz := _empezar("TorreBasura", "basura")
	var mats := _mats_servicio()
	k.cuarto("Cuarto", Vector3.ZERO, 4, 4, 2.8, {"norte": {1: "puerta"}, "sur": {2: "puerta"}}, mats, _piso_concreto("bas"), _techo_servicio())
	var m := k.nodo("Basura")
	k.kenney("city-kit-roads", "dumpster", m, Vector3(0.75, 0, 2.6), 0.0, 4.5, "Contenedor1")
	k.kenney("city-kit-roads", "dumpster", m, Vector3(3.3, 0, 1.6), 0.0, 4.5, "Contenedor2")
	k.colision("ColisionContenedor1", m, Vector3(0.75, 0.5, 2.6), Vector3(1.3, 1.0, 1.7), Constructor.CAPA_MUEBLES)
	k.colision("ColisionContenedor2", m, Vector3(3.3, 0.5, 1.6), Vector3(1.3, 1.0, 1.7), Constructor.CAPA_MUEBLES)
	for b in [Vector3(0.45, 0, 0.6), Vector3(0.9, 0, 0.45), Vector3(0.5, 0, 3.6), Vector3(3.5, 0, 2.9)]:
		k.modelo(PROPS + "bolsa_basura.glb", m, b, b.x * 70.0, 1.0)
	k.prefab("caja_carton_abierta", m, Vector3(3.6, 0, 0.4), 30.0, 0.9, "Caja")
	k.prefab("cubeta", m, Vector3(3.6, 0, 3.6), 0.0, 1.0, "Cubeta")
	# Cruce_3: huellas mojadas que salen hacia la puerta del estacionamiento
	k.cruce(k.raiz, 3, Vector3(2.2, 0, 3.1), 180.0, "huellas_mojadas", Vector3(0.3, 0, 1.6), Vector3(-0.8, 0, -2.2))
	var pz := k.nodo("Puertas")
	k.pieza_kit("puerta_hoja_metal", pz, Vector3(1.5 - 0.43, 0.01, 0.06), 0.0, {}, "PuertaCocina")
	var volver := k.punto(pz, "Salida", "Volver a la cocina", Vector3(1.5, 1.05, 0.15), Vector3(0.9, 2.1, 0.2))
	var tito := k.puerta_tito(pz, 4, Vector3(2.5, 0, 4.0), 180.0)
	var a_est := k.punto(tito, "Salida", "Ir al estacionamiento", Vector3(2.5, 1.05, 3.85), Vector3(0.9, 2.1, 0.2), 180.0)
	a_est.collision_layer = 0
	var l := k.nodo("Luces")
	k.modelo(PROPS + "lampara_tubo.glb", l, Vector3(2.0, 2.73, 2.0), 0.0, 1.0)
	k.omni(l, "Tubo", Vector3(2.0, 2.5, 2.0), Color("e8f0d8"), 0.8, 5.0, true)
	k.omni(l, "Rojo", Vector3(2.5, 2.3, 3.6), Color("ff3a30"), 0.5, 3.0)
	var env := k.entorno(Color("2a2f3a"), Color("3a3f4a"), Color("b0b4a8"), 0.5, 1.0)
	env.environment.background_mode = Environment.BG_COLOR
	env.environment.background_color = Color("101216")
	var esc := k.base_jugable("Torre Miawrio · Cuarto de basura", Vector3(1.5, 0, 0.8), 180.0)
	esc.set("Salidas", [volver, a_est])
	esc.set("Destinos", PackedStringArray([COCINA + "|Entrada_DesdeBasura", ESTACIONAMIENTO + "|Entrada_DesdeBasura"]))
	k.marcador("Entrada_DesdeCocina", k.raiz, Vector3(1.5, 0, 0.8), 180.0)
	_terminar(raiz, BASURA)

# ---------- Estacionamiento (20 × 12 m y la rampa) ----------

func _estacionamiento() -> void:
	var raiz := _empezar("TorreEstacionamiento", "estacionamiento")
	var mats := {
		"Muro": k.mat_textura("est_muro", "Concrete034", 0.6, Color(0.82, 0.81, 0.77)),
		"Zocalo": k.mat_color("est_zocalo", Color("e3b23c"), 0.6, Vector2(1, 1), 0.04),
		"Marco": k.mat_color("est_marco", Color("4a5058"), 0.5, Vector2(1, 1), 0.04),
	}
	var norte := {}
	for i in range(20, 24):
		norte[i] = "nada"
	k.cuarto("Cuarto", Vector3.ZERO, 24, 12, 3.0, {"norte": norte, "oeste": {6: "puerta"}}, mats, _piso_concreto("est"), {}, false)
	var techo_m := {"Plafon": k.mat_textura("est_techo", "Concrete034", 0.5, Color(0.7, 0.7, 0.68))}
	k.techo(k.raiz, "Techo", 0, 0, 20, 12, 3.0, techo_m)
	k.techo(k.raiz, "TechoRampa", 20, 4, 4, 8, 3.0, techo_m)
	var r := k.nodo("Rampa")
	k.modelo(PROPS + "rampa.glb", r, Vector3(22.0, 0, 10.0), 0.0, 1.0, "Rampa")
	var col := k.colision("ColisionRampa", r, Vector3(22.0, 1.5 - 0.15, 5.0), Vector3(4.0, 0.3, sqrt(109.0)))
	col.rotation_degrees.x = rad_to_deg(atan2(3.0, 10.0))
	# Afuera, al final de la rampa: la calle de mañana
	k.vista_calle(r, Vector3(22.0, 3.0, -0.3), 0.0, 50.0, 17)
	# Columnas, líneas de cajones y coches
	var c := k.nodo("Columnas")
	for x in [4.0, 10.0, 16.0]:
		for z in [4.0, 8.0]:
			k.modelo(PROPS + "columna_estacionamiento.glb", c, Vector3(x, 0, z), 0.0, 1.0)
			k.colision("Col_%d_%d" % [int(x), int(z)], c, Vector3(x, 1.5, z), Vector3(0.6, 3.0, 0.6))
	var autos := k.nodo("Coches")
	var modelos := ["sedan", "suv", "hatchback-sports", "van", "taxi", "suv-luxury", "sedan-sports", "delivery"]
	var i := 0
	for x in [1.6, 4.4, 7.2, 12.8, 15.6, 18.4]:
		k.kenney("car-kit", modelos[i % modelos.size()], autos, Vector3(x, 0, 1.6), 0.0 if i % 2 == 0 else 180.0, 1.65, "Coche%d" % i)
		k.colision("ColCoche%d" % i, autos, Vector3(x, 0.75, 1.6), Vector3(2.5, 1.5, 4.3), Constructor.CAPA_MUEBLES)
		i += 1
	for x in [1.6, 7.2, 12.8, 18.4]:
		k.kenney("car-kit", modelos[i % modelos.size()], autos, Vector3(x, 0, 10.4), 180.0 if i % 2 == 0 else 0.0, 1.65, "Coche%d" % i)
		k.colision("ColCoche%d" % i, autos, Vector3(x, 0.75, 10.4), Vector3(2.5, 1.5, 4.3), Constructor.CAPA_MUEBLES)
		i += 1
	# Patrulla de Hecdog: entra por la rampa
	var pat := k.nodo("Patrulla")
	k.kenney("car-kit", "police", pat, Vector3(22.0, 0, 10.6), 180.0, 1.65, "Coche")
	k.colision("ColisionPatrulla", pat, Vector3(22.0, 0.75, 10.6), Vector3(2.5, 1.5, 5.2), Constructor.CAPA_MUEBLES)
	k.marcador("PuntoEntradaPatrulla", pat, Vector3(22.0, 3.0, -1.0), 180.0)
	# Técnicos de «Servicios Pulgar» (marcadores temporales: no hay modelo de Pulgas)
	var t := k.nodo("Tecnicos")
	_tecnico(t, "Tecnico_A", Vector3(8.5, 0, 6.2), 210.0, false, 0.0)
	_tecnico(t, "Tecnico_B", Vector3(13.0, 0, 6.6), 160.0, true, 0.7)
	k.prefab("caja_carton", t, Vector3(9.2, 0, 6.0), 10.0, 1.0, "CajaServicio")
	# Cruce_4: aceite y huellas en la puerta de servicio (oeste)
	k.cruce(k.raiz, 4, Vector3(1.0, 0, 6.5), -90.0, "mancha_aceite", Vector3(0, 0, -3.0), Vector3(3.0, 0, 0))
	k.modelo(PROPS + "huellas_mojadas.glb", k.raiz.get_node("Cruce_4"), Vector3(0.5, 0, -0.6), 0.0, 1.0, "Huellas")
	var pz := k.nodo("Puertas")
	k.pieza_kit("puerta_hoja_metal", pz, Vector3(0.06, 0.01, 6.5 + 0.43), 90.0, {}, "PuertaServicio")
	var volver := k.punto(pz, "Salida", "Volver al cuarto de basura", Vector3(0.15, 1.05, 6.5), Vector3(0.2, 2.1, 0.9), 90.0)
	# Luces: tubos en filas y la luz de día que baja por la rampa
	var l := k.nodo("Luces")
	for x in [3.0, 9.0, 15.0]:
		for z in [3.0, 9.0]:
			k.modelo(PROPS + "lampara_tubo.glb", l, Vector3(x, 2.93, z), 0.0, 1.0)
			k.omni(l, "Tubo", Vector3(x, 2.7, z), Color("e8f0e0"), 0.9, 7.0, x == 9.0 and z == 3.0)
	k.sol(l, "Sol", 30.0, 175.0, Color("ffe6c0"), 1.2)
	k.omni(l, "Rampa", Vector3(22.0, 2.5, 3.0), Color("fff0d8"), 1.0, 8.0)
	k.entorno(Color("6f9fd8"), Color("dbe6ee"), Color("a8aaa6"), 0.5, 1.0)
	var esc := k.base_jugable("Torre Miawrio · Estacionamiento", Vector3(1.0, 0, 6.5), -90.0)
	esc.set("Salidas", [volver])
	esc.set("Destinos", PackedStringArray([BASURA + "|Entrada_DesdeEstacionamiento"]))
	k.marcador("Entrada_DesdeBasura", k.raiz, Vector3(1.0, 0, 6.5), -90.0)
	k.ambiente_sonoro("Eco", raiz, Vector3.ZERO, "res://Assets/Audio/Ambiente/zumbido_sala.ogg", -22.0)
	_terminar(raiz, ESTACIONAMIENTO)

## Técnico de «Servicios Pulgar»: maniquí con uniforme, gorra y cubrebocas; Pulgas además con las tres rayas
func _tecnico(padre: Node, nombre: String, pos: Vector3, rot_y: float, pulgas: bool, desfase: float) -> void:
	var f := k.figura_fondo(padre, nombre, pos, rot_y, Color("3d5266"), "Idle", 0.78, desfase)
	var gorra: Node3D = load(PROPS + "gorra.glb").instantiate()
	gorra.name = "Gorra"
	gorra.set_meta("hueso", "DEF-head")
	gorra.position = Vector3(0, 0.2, 0.0)
	f.add_child(gorra)
	var cubre: Node3D = load(PROPS + "cubrebocas.glb").instantiate()
	cubre.name = "Cubrebocas"
	cubre.set_meta("hueso", "DEF-head")
	cubre.position = Vector3(0, 0.07, 0.1)
	f.add_child(cubre)
	var logo := Label3D.new()
	logo.name = "Uniforme"
	logo.text = "Servicios Pulgar"
	logo.font_size = 36
	logo.pixel_size = 0.0018
	logo.modulate = Color("e3b23c")
	logo.outline_size = 0
	logo.position = Vector3(0, 1.15, -0.13)
	logo.rotation_degrees.y = 180.0
	f.add_child(logo)
	if pulgas:
		var tatuaje: Node3D = load(PROPS + "tatuaje_rayas.glb").instantiate()
		tatuaje.name = "Tatuaje"
		tatuaje.set_meta("hueso", "DEF-hand.R")
		tatuaje.position = Vector3(0, 0.05, 0.03)
		f.add_child(tatuaje)
		f.set_meta("pulgas", true)
