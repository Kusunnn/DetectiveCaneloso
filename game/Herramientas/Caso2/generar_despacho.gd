extends SceneTree
## Capítulo 2 · 3.3 Torre Miawrio · Despacho del piso 30 (res://Escenas/Caso2/torre_despacho.tscn), 14 × 10 m, 3.4 m.
## Lujoso pero tierno: nogal, verde botella, latón y alfombra vino; ventanal de piso a techo con la ciudad de mañana.
## Aquí están 6 pistas y el punto de interrogatorio. Personajes: Miawrio, Aya (con su cámara), Mimita y Hecdog.
## Uso: <Godot>_console.exe --headless --path game --script res://Herramientas/Caso2/generar_despacho.gd

const Constructor := preload("res://Herramientas/Caso2/constructor.gd")
const ESCENA := "res://Escenas/Caso2/torre_despacho.tscn"
const LOBBY := "res://Escenas/Caso2/torre_lobby.tscn"
const PISTAS := "res://Assets/Pistas/Modelos/"
const PROPS := "res://Assets/Caso2/Modelos/props/"
const GEN := "res://Assets/Pistas/Modelos/generadas/"
const ANCHO := 14
const FONDO := 10
const ALTO := 3.4
const LUGAR := "Torre Miawrio · Despacho"

var k: Constructor
var luces_normal: Node3D

func _initialize() -> void:
	var raiz := Node3D.new()
	raiz.name = "TorreDespacho"
	k = Constructor.new(raiz, "despacho")

	var mats_muro := {
		"Muro": k.mat_color("des_muro", Color("eadbc0"), 0.88, Vector2(1, 1), 0.035),
		"Zocalo": k.mat_color("des_zocalo", Color("5c3a22"), 0.5, Vector2(1, 6), 0.12),
		"Marco": k.mat_color("des_marco", Color("3a2a1e"), 0.4, Vector2(1, 1), 0.04),
	}
	var piso := k.malla_kit("piso_tablas", {
		"Duela": k.mat_color("des_duela", Color("6e4528"), 0.55, Vector2(1, 8), 0.14),
		"Duela2": k.mat_color("des_duela2", Color("633d23"), 0.55, Vector2(1, 8), 0.14),
	})
	var techo := {"Plafon": k.mat_color("des_plafon", Color("f1ebdd"), 0.9, Vector2(1, 1), 0.02)}
	var norte := {}
	for i in ANCHO:
		norte[i] = "ventanal"
	k.cuarto("Cuarto", Vector3.ZERO, ANCHO, FONDO, ALTO, {"norte": norte, "sur": {1: "puerta", 10: "puerta"}}, mats_muro, piso, techo)
	k.vista_alta(k.raiz.get_node("Cuarto"), Vector3(7.0, 0, -0.3), 0.0, 90.0, 30)

	luces_normal = k.nodo("LucesNormal")
	_escritorio()
	_zona_estar()
	_mesa_proyectos()
	_librero_archivero()
	_rincon_aya()
	_paredes()
	_personajes()
	_puntos()
	_luces()
	var esc := k.base_jugable(LUGAR, Vector3(10.5, 0, 8.9), 0.0)
	var elevador := k.punto(k.raiz, "Elevador", "Bajar al lobby", Vector3(10.5, 1.05, FONDO - 0.12), Vector3(0.9, 2.1, 0.25))
	esc.set("Salidas", [elevador])
	esc.set("Destinos", PackedStringArray([LOBBY + "|Entrada_DesdeElevador"]))
	var cajones: Array = [k.raiz.get_node("Escritorio/CajonExpediente"), k.raiz.get_node("LibreroArchivero/ArchiveroCajon")]
	var dentro: Array = [k.raiz.get_node("Escritorio/CajonExpediente/Parte/Pista_expediente_filtrado"), k.raiz.get_node("LibreroArchivero/ArchiveroCajon/Parte/Pista_estado_cuenta")]
	esc.set("ContenedoresConPista", cajones)
	esc.set("PistasDentro", dentro)
	k.marcador("Entrada_DesdeElevador", k.raiz, Vector3(10.5, 0, 8.9), 0.0)
	k.ambiente_sonoro("RelojDespacho", raiz, Vector3(7.0, 2.6, FONDO - 0.1), "res://Assets/Audio/Ambiente/reloj_pared.wav", -14.0, 9.0)
	k.ambiente_sonoro("CiudadLejana", raiz, Vector3.ZERO, "res://Assets/Audio/Ambiente/zumbido_sala.ogg", -28.0)
	_extra(esc)
	k.guardar(ESCENA)
	raiz.free()
	quit()

# Estado HACKEADO: luces apagadas, persianas metálicas abajo, pantallas con tres rayas rojas, luces de emergencia
# rojas y las bocinas a la vista. Sigue legible: contraluz rojo, luz fría de las pantallas y la pecera encendida.
func _extra(_esc: Node) -> void:
	var normal := k.nodo("SoloNormal")
	var hack := k.nodo("SoloHackeado")
	# Pantalla grande de la pared este: la gráfica del proyecto / las tres rayas
	k.modelo(PROPS + "marco_pantalla.glb", k.raiz.get_node("Paredes"), Vector3(ANCHO, 1.9, 8.2), -90.0, 1.0, "PantallaPared").add_to_group("auditoria_pared", true)
	k.pantalla(normal, "PantallaGrafica", Vector3(ANCHO - 0.056, 1.9, 8.2), -90.0, Vector2(1.6, 0.9), GEN + "c2_grafica.png", 0.7)
	k.pantalla(hack, "PantallaTito", Vector3(ANCHO - 0.056, 1.9, 8.2), -90.0, Vector2(1.6, 0.9), GEN + "c2_tres_rayas.png", 1.6)
	# El monitor del escritorio también: una cara emisiva delante de la pantalla
	k.pantalla(hack, "MonitorTito", Vector3(7.36, 0.771 + 0.27, 1.93), 180.0, Vector2(0.5, 0.33), GEN + "c2_tres_rayas.png", 1.6)
	# Persianas metálicas bajadas por fuera del ventanal
	var lamina := {"Lamina": k.mat_color("des_lamina", Color("6d7278"), 0.45, Vector2(1, 1), 0.06, 0.7)}
	for i in ANCHO:
		var c := k.pieza_kit("cortina_metalica", hack, Vector3(i + 0.5, 0.1, -0.2), 0.0, lamina, "Cortina%d" % i)
		c.scale = Vector3(1.0, 1.1, 1.0)
	# Luces de emergencia rojas (las dos de las esquinas con sombra) y relleno frío de las pantallas
	var luces := k.nodo("LucesHackeado", hack)
	var puntos := [[Vector3(0.0, 2.9, 5.0), 90.0, true], [Vector3(ANCHO, 2.9, 4.0), -90.0, true], [Vector3(4.0, 2.9, FONDO), 180.0, false], [Vector3(10.0, 2.9, FONDO), 180.0, false]]
	for p in puntos:
		var pos: Vector3 = p[0]
		k.modelo(PROPS + "luz_emergencia.glb", hack, pos, p[1], 1.0, "LuzEmergencia").add_to_group("auditoria_pared", true)
		var dentro: Vector3 = pos + Basis(Vector3.UP, deg_to_rad(p[1])) * Vector3(0, -0.1, 0.4)
		k.omni(luces, "Emergencia", dentro, Color("ff2a2a"), 1.4, 8.0, p[2])
	k.omni(luces, "FrioPantalla", Vector3(ANCHO - 0.8, 1.9, 8.2), Color("7fa8ff"), 1.3, 7.0)
	k.omni(luces, "FrioMonitor", Vector3(7.6, 1.3, 1.5), Color("7fa8ff"), 0.6, 3.5)
	k.omni(luces, "Pecera", Vector3(13.2, 1.1, 3.6), Color("7fd6ff"), 0.8, 3.0)
	k.omni(luces, "ContraluzVentanal", Vector3(7.0, 2.4, 0.5), Color("8fa0ff"), 0.8, 8.0)
	var estado := Node.new()
	estado.name = "EstadoLuces"
	estado.set_script(load("res://Escenas/Investigacion/EstadoLuces.cs"))
	k.raiz.add_child(estado)
	estado.set("SoloNormal", [luces_normal, normal])
	estado.set("SoloHackeado", [hack])
	estado.set("Entorno", k.raiz.get_node("Entorno"))
	estado.set("AmbienteHackeado", Color("4a4f6e"))
	estado.set("EnergiaAmbienteHackeado", 0.4)

func _escritorio() -> void:
	var e := k.nodo("Escritorio")
	var mesa := k.modelo(PROPS + "escritorio_ejecutivo.glb", e, Vector3(7.0, 0, 2.3), 0.0, 1.0, "EscritorioMiawrio")
	k.colision("ColisionEscritorio", e, Vector3(7.0, 0.38, 2.3), Vector3(2.2, 0.76, 1.0), Constructor.CAPA_MUEBLES)
	k.prefab("silla_escritorio", e, Vector3(7.0, 0, 1.35), 0.0, 1.1, "SillaMiawrio")
	k.kenney("furniture-kit", "chairModernCushion", e, Vector3(6.4, 0, 3.4), 165.0, 2.2, "SillaVisita1")
	k.kenney("furniture-kit", "chairModernCushion", e, Vector3(7.6, 0, 3.4), 195.0, 2.2, "SillaVisita2")
	var y := 0.771
	k.kenney("furniture-kit", "computerScreen", e, Vector3(7.65, y, 1.95), 180.0, 1.5, "Monitor")
	k.kenney("furniture-kit", "computerKeyboard", e, Vector3(7.35, y, 2.1), 0.0, 1.5, "Teclado")
	k.modelo(PROPS + "telefono_escritorio.glb", e, Vector3(6.15, y, 2.05), 200.0, 1.0, "Telefono")
	k.modelo("res://Assets/Cinematicas/Modelos/polyhaven/desk_lamp_arm_01/desk_lamp_arm_01_1k.gltf", e, Vector3(8.0, y, 1.95), 150.0, 1.0, "Lampara")
	k.prefab("portarretrato", e, Vector3(5.95, y, 1.95), 160.0, 0.8, "FotoMimita")
	k.prefab("portarretrato", e, Vector3(8.0, y, 2.6), 200.0, 0.7, "FotoGraduacion")
	k.prefab("taza_cafe", e, Vector3(7.95, y, 2.25), 0.0, 1.0, "CafeMiawrio")
	# P04 · Contrato (bajo la lámpara), P06 · Registro de llamadas (junto al teléfono), P07 · Abrecartas
	var contrato: Node3D = load(PISTAS + "contrato_ixodes/contrato_ixodes.tscn").instantiate()
	k.pista(e, "contrato_ixodes", "Contrato de financiamiento con Ixodes",
		"Contrato de financiamiento con Ixodes. Lo firma un apoderado, «Lic. R. Garrido». Está fechado un día antes de la subasta.",
		"«Nadie firma el financiamiento de una compra que todavía no ganó. A menos que la subasta ya estuviera ganada.»",
		LUGAR, contrato, Vector3(7.25, y + 0.001, 2.5), 172.0)
	var llamadas: Node3D = load(PISTAS + "registro_llamadas2/registro_llamadas2.tscn").instantiate()
	k.pista(e, "registro_llamadas2", "Registro de llamadas de Miawrio",
		"Registro de llamadas de Miawrio. Número oculto, siempre a las 23:00. Incluye la noche del crimen de Porky, de 23:00 a 23:40.",
		"«La coartada de Miawrio de hace dos meses. Cuarenta minutos al teléfono... y nunca le pregunté con quién.»",
		LUGAR, llamadas, Vector3(6.45, y + 0.001, 2.45), 190.0)
	var abrecartas: Node3D = load(PISTAS + "abrecartas/abrecartas.tscn").instantiate()
	k.pista(e, "abrecartas", "Abrecartas en forma de garra",
		"Abrecartas en forma de garra. Regalo de Ixodes con tarjeta: «Para M., de un socio paciente.»",
		"«Bronce, tres puntas curvas. Un regalo raro para alguien que va a construir una clínica.»",
		LUGAR, abrecartas, Vector3(7.85, y + 0.001, 2.6), 160.0)
	# P08 · El cajón de abajo a la derecha (del lado de Miawrio) con la copia del expediente
	var parte := Node3D.new()
	var cajon: Node3D = load(PROPS + "escritorio_cajon.glb").instantiate()
	cajon.name = "Cajon"
	parte.add_child(cajon)
	k.contenedor(e, "CajonExpediente", "Cajón", Vector3(7.82, 0.075, 1.835), 180.0, parte, Vector3(0, 0, -0.32),
		Vector3(0.46, 0.19, 0.06), Vector3(0, 0.085, -0.01), "Una carpeta con el sello de la Fiscalía.")
	var exp: Node3D = load(PISTAS + "expediente_filtrado/expediente_filtrado.tscn").instantiate()
	k.pista(parte, "expediente_filtrado", "Copia del expediente de Pino Lunar",
		"Copia del expediente de Pino Lunar en el cajón, con sello de la Fiscalía.",
		"«Esto es confidencial. Miawrio no tenía cómo conseguirlo... alguien adentro se lo dio.»",
		LUGAR, exp, Vector3(0, 0.03, 0.22), 90.0)

func _zona_estar() -> void:
	var z := k.nodo("ZonaEstar")
	k.prefab("tapete", z, Vector3(3.0, 0.004, 5.0), 90.0, 1.6, "Alfombra")
	var mesa := k.prefab("mesa_baja", z, Vector3(3.0, 0, 5.0), 0.0, 0.85, "MesaBaja")
	var y := k.tope(mesa)
	k.modelo(PROPS + "galletas.glb", z, Vector3(2.75, y, 4.95), 0.0, 1.0, "Galletas")
	k.prefab("taza_cafe", z, Vector3(3.3, y, 4.8), 40.0, 1.0, "Cafe1")
	k.prefab("taza_cafe", z, Vector3(3.35, y, 5.2), -20.0, 1.0, "Cafe2")
	k.kenney("furniture-kit", "loungeSofa", z, Vector3(1.0, 0, 6.05), 90.0, 2.2, "Sofa")
	k.kenney("furniture-kit", "pillow", z, Vector3(1.25, 0.5, 4.6), 80.0, 2.2, "Cojin1")
	k.modelo(PROPS + "torre_gato.glb", z, Vector3(0.6, 0, 1.6), 20.0, 1.0, "TorreGato")
	k.kenney("furniture-kit", "pottedPlant", z, Vector3(0.6, 0, 3.0), 10.0, 3.2, "PlantaGrande")
	k.prefab("lampara_pie", z, Vector3(0.6, 0, 7.3), 0.0, 1.0, "LamparaPie")

func _mesa_proyectos() -> void:
	var m := k.nodo("MesaProyectos")
	var mesa := k.prefab("mesa_larga", m, Vector3(11.0, 0, 6.2), 0.0, 1.0, "Mesa")
	var y := k.tope(mesa)
	k.modelo(PROPS + "maqueta_hotel.glb", m, Vector3(11.55, y, 6.3), -10.0, 1.0, "MaquetaHotel")
	var planos: Node3D = load(PISTAS + "planos_clinica/planos_clinica.tscn").instantiate()
	k.pista(m, "planos_clinica", "Planos de la clínica",
		"Planos de la clínica. Clínica comunitaria gratuita dentro del nuevo hotel. En la esquina, con la letra de Miawrio: «Para M.»",
		"«Doce consultorios, farmacia gratuita, sala de partos... Esto no lo dibuja un tiburón. Lo dibuja alguien enamorado.»",
		LUGAR, planos, Vector3(10.45, y + 0.002, 6.2), 5.0)
	k.prefab("lampara_mesa", m, Vector3(12.25, y, 5.7), 0.0, 1.0, "LamparaProyectos")
	k.prefab("silla", m, Vector3(11.0, 0, 7.3), 180.0, 1.0, "SillaProyectos")

func _librero_archivero() -> void:
	var l := k.nodo("LibreroArchivero")
	for i in 3:
		var lib := k.prefab("repisa_larga" if i != 1 else "vitrina", l, Vector3(0.45, 0, 8.4 - i * 1.25), 90.0, 0.9, "Librero%d" % i)
		var yt := k.tope(lib)
		if i != 1:
			k.modelo(PROPS + "trofeo.glb", l, Vector3(0.4, yt, 8.4 - i * 1.25), 90.0, 1.0, "Trofeo%d" % i)
	# P05 · Estado de cuenta, en el cajón de en medio del archivero (junto al librero)
	k.modelo(PROPS + "archivero_cuerpo.glb", l, Vector3(1.3, 0, 9.6), 180.0, 1.0, "Archivero")
	k.colision("ColisionArchivero", l, Vector3(1.3, 0.525, 9.6), Vector3(0.5, 1.05, 0.6), Constructor.CAPA_MUEBLES)
	var parte := Node3D.new()
	var cajon: Node3D = load(PROPS + "archivero_cajon.glb").instantiate()
	cajon.name = "Cajon"
	parte.add_child(cajon)
	k.contenedor(l, "ArchiveroCajon", "Archivero", Vector3(1.3, 0.375, 9.29), 180.0, parte, Vector3(0, 0, 0.32),
		Vector3(0.46, 0.32, 0.06), Vector3(0, 0.15, 0.01), "Estados de cuenta del Proyecto Pino Lunar.")
	var estado: Node3D = load(PISTAS + "estado_cuenta/estado_cuenta.tscn").instantiate()
	k.pista(parte, "estado_cuenta", "Estado de cuenta",
		"Estado de cuenta. Ocho depósitos de ocho empresas distintas, todos en el mismo minuto, que se juntan en una sola cuenta.",
		"«Ocho cuentas que se juntan en un solo cuerpo. Ya había visto este dibujo antes...»",
		LUGAR, estado, Vector3(0, 0.235, -0.2), 0.0)

func _rincon_aya() -> void:
	var a := k.nodo("RinconAya")
	k.modelo(PROPS + "tripie_camara.glb", a, Vector3(2.4, 0, 1.0), 150.0, 1.0, "Tripie")
	k.prefab("caja_carton_abierta", a, Vector3(3.2, 0, 0.6), 20.0, 0.7, "EstucheEquipo")

func _paredes() -> void:
	var p := k.nodo("Paredes")
	k.modelo(PROPS + "pecera.glb", p, Vector3(13.6, 0, 3.6), -90.0, 1.0, "Pecera")
	k.colision("ColisionPecera", p, Vector3(13.6, 0.65, 3.6), Vector3(0.45, 1.3, 0.85), Constructor.CAPA_MUEBLES)
	for c in [[Vector3(13.98, 1.7, 6.2), -90.0, "cuadro_grande"], [Vector3(4.5, 1.7, FONDO - 0.02), 180.0, "cuadro"],
			[Vector3(5.6, 1.6, FONDO - 0.02), 180.0, "cuadro_chico"], [Vector3(0.02, 1.9, 4.0), 90.0, "cuadro"]]:
		k.prefab(c[2], p, c[0], c[1], 1.0).add_to_group("auditoria_pared", true)
	k.modelo(PROPS + "reloj_pared.glb", p, Vector3(7.0, 2.6, FONDO), 180.0, 1.0, "Reloj").add_to_group("auditoria_pared", true)
	k.modelo(PROPS + "logo_torre.glb", p, Vector3(13.0, 2.0, FONDO), 180.0, 0.55, "LogoChico").add_to_group("auditoria_pared", true)
	k.kenney("furniture-kit", "pottedPlant", p, Vector3(13.4, 0, 9.4), 30.0, 3.0, "PlantaEsquina")
	k.kenney("furniture-kit", "pottedPlant", p, Vector3(13.4, 0, 0.6), 60.0, 3.0, "PlantaVentana")
	# Bocinas del voceo del edificio: se ven desde el principio (la voz de Tito saldrá de ahí)
	for b in [[Vector3(0.0, 3.0, 2.0), 90.0], [Vector3(ANCHO, 3.0, 8.0), -90.0], [Vector3(7.0, 3.05, FONDO), 180.0]]:
		k.modelo(PROPS + "bocina_techo.glb", p, b[0], b[1], 1.0, "Bocina").add_to_group("auditoria_pared", true)
	# Puerta de las escaleras de emergencia: cerrada (la abre la tarjeta de Mimita en las escaleras)
	var m: Array = k.en_muro(Vector3.ZERO, ANCHO, FONDO, "sur", 1)
	var hoja := k.pieza_kit("puerta_hoja_metal", p, m[0] + Basis(Vector3.UP, deg_to_rad(m[1])) * Vector3(-0.43, 0.01, -0.06), m[1], {}, "PuertaEscalerasHoja")
	var letrero := Label3D.new()
	letrero.name = "LetreroEscaleras"
	letrero.text = "ESCALERAS DE EMERGENCIA"
	letrero.font_size = 40
	letrero.pixel_size = 0.0025
	letrero.modulate = Color("2f8a4a")
	letrero.outline_size = 0
	letrero.position = m[0] + Vector3(0, 2.35, -0.03)
	letrero.rotation_degrees.y = 180.0
	p.add_child(letrero)
	# Hoja de la puerta del elevador
	var m2: Array = k.en_muro(Vector3.ZERO, ANCHO, FONDO, "sur", 10)
	k.modelo(PROPS + "elevador.glb", p, m2[0], m2[1], 1.0, "ElevadorPuertas")

func _personajes() -> void:
	var per := k.nodo("Personajes")
	k.personaje("Miawrio", per, Vector3(8.55, 0, 1.45), 25.0)
	k.personaje("Aya", per, Vector3(3.1, 0, 1.6), 35.0)
	k.personaje("Mimita", per, Vector3(2.4, 0, 8.4), 150.0)
	k.personaje("Hecdog", per, Vector3(3.6, 0, 8.9), 200.0)

func _puntos() -> void:
	var pt := k.nodo("PuntosMecanicas")
	# Interrogatorio: dos sillones frente a frente junto a la mesa baja (plano medio, como la sala del Caso 1)
	var pi := k.nodo("PuntoInterrogatorio", pt)
	k.prefab("sillon", pi, Vector3(3.0, 0, 3.75), 0.0, 1.0, "SillonImplicado")
	k.prefab("sillon", pi, Vector3(3.0, 0, 6.25), 180.0, 1.0, "SillonCaneloso")
	k.marcador("PuntoSilla", pi, Vector3(3.0, 0, 3.75), 180.0)
	k.marcador("PuntoCaneloso", pi, Vector3(3.0, 0, 6.25), 0.0)
	k.marcador("PuntoEntradaImplicado", pi, Vector3(4.6, 0, 3.4), -90.0)
	var cam := Marker3D.new()
	cam.name = "CamaraPlanoMedio"
	cam.position = Vector3(3.0, 1.15, 5.55)
	pi.add_child(cam)
	k.marcador("PuntoTablero", pt, Vector3(11.0, 0, 7.6), 0.0)
	k.marcador("PuntoEspera_Mimita", pt, Vector3(2.4, 0, 8.4), 160.0)
	k.marcador("PuntoEspera_Hecdog", pt, Vector3(3.6, 0, 8.9), 200.0)
	k.marcador("PuntoAya", pt, Vector3(3.1, 0, 1.6), 210.0)
	var puerta := StaticBody3D.new()
	puerta.name = "PuertaEscaleras"
	puerta.set_script(load("res://Escenas/Investigacion/Puerta.cs"))
	puerta.collision_layer = Constructor.CAPA_INTERACTUABLE | Constructor.CAPA_MUNDO
	puerta.position = Vector3(1.5, 1.05, FONDO - 0.12)
	var f := CollisionShape3D.new()
	f.name = "Forma"
	var s := BoxShape3D.new()
	s.size = Vector3(0.9, 2.1, 0.2)
	f.shape = s
	puerta.add_child(f)
	pt.add_child(puerta)
	puerta.set("Bloqueada", true)
	puerta.set("MensajeBloqueada", "Está cerrada.")

func _luces() -> void:
	var l := luces_normal
	k.entorno(Color("6f9fd8"), Color("dbe6ee"), Color("d4c2a2"), 0.45, 0.95)
	k.sol(l, "Sol", 28.0, 160.0, Color("ffe6c0"), 1.35)
	k.omni(l, "LamparaEscritorio", Vector3(7.95, 1.3, 2.05), Color("ffc27a"), 0.9, 2.6)
	k.omni(l, "LamparaPie", Vector3(0.6, 1.6, 7.3), Color("ffc27a"), 0.9, 4.0)
	k.omni(l, "LamparaProyectos", Vector3(12.25, 1.3, 5.7), Color("ffc27a"), 0.7, 3.0)
	for p in [Vector3(3.0, 3.1, 5.0), Vector3(7.0, 3.1, 4.0), Vector3(11.0, 3.1, 6.2), Vector3(7.0, 3.1, 8.0)]:
		k.omni(l, "Plafon", p, Color("ffd9a0"), 0.5, 6.0)
	k.omni(l, "BrilloPecera", Vector3(13.3, 1.1, 3.6), Color("7fd6ff"), 0.6, 2.2)
	for x in [3.0, 7.0, 11.0]:
		k.kenney("furniture-kit", "lampSquareCeiling", l, Vector3(x, ALTO - 0.5, 5.0 if x != 7.0 else 4.0), 0.0, 2.2, "Colgante%d" % int(x))
