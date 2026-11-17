extends SceneTree
## Capítulo 2 · 3.1 Comisaría por la mañana (res://Escenas/Caso2/comisaria_manana.tscn).
## Escena NUEVA: solo la zona de los escritorios de Caneloso y Hecdog. No usa nada del Caso 0 (ni Caso0.cs ni sus
## pistas): copia su distribución con el kit modular del Cap. 2 y muebles de librería.
## Uso: <Godot>_console.exe --headless --path game --script res://Herramientas/Caso2/generar_comisaria.gd

const Constructor := preload("res://Herramientas/Caso2/constructor.gd")
const ESCENA := "res://Escenas/Caso2/comisaria_manana.tscn"
const LOBBY := "res://Escenas/Caso2/torre_lobby.tscn"
const PISTAS := "res://Assets/Pistas/Modelos/"
const PROPS := "res://Assets/Caso2/Modelos/props/"
const ANCHO := 8
const FONDO := 6
const ALTO := 2.8
const LUGAR := "Comisaría"

var k: Constructor
var mats_muro: Dictionary

func _initialize() -> void:
	var raiz := Node3D.new()
	raiz.name = "ComisariaManana"
	k = Constructor.new(raiz, "comisaria")

	# Paleta: madera miel, verde oliva, papel; piso de duela
	var muro := k.mat_color("com_muro", Color("d8cba9"), 0.92, Vector2(1, 1), 0.04)
	var zocalo := k.mat_color("com_zocalo", Color("5a3b24"), 0.6, Vector2(1, 6), 0.12)
	var marco := k.mat_color("com_marco", Color("b07a45"), 0.55, Vector2(1, 6), 0.1)
	mats_muro = {"Muro": muro, "Zocalo": zocalo, "Marco": marco}
	var piso := k.malla_kit("piso_tablas", {
		"Duela": k.mat_color("com_duela", Color("8a5a34"), 0.7, Vector2(1, 8), 0.14),
		"Duela2": k.mat_color("com_duela2", Color("7c5030"), 0.7, Vector2(1, 8), 0.14),
	})
	var techo := {"Plafon": k.mat_color("com_plafon", Color("efe8d8"), 0.95, Vector2(1, 1), 0.03)}

	k.cuarto("Cuarto", Vector3.ZERO, ANCHO, FONDO, ALTO,
		{"norte": {1: "ventana", 2: "ventana", 5: "ventana", 6: "ventana"}, "sur": {6: "puerta"}},
		mats_muro, piso, techo)
	_ventanas()
	_muebles()
	_escritorio_caneloso()
	_pared_oeste()
	_rincon_cafe()
	_personajes()
	_luces()
	var esc := k.base_jugable(LUGAR, Vector3(2.4, 0, 4.7), -15.0)
	var salida := _salida()
	esc.set("Salidas", [salida])
	esc.set("Destinos", PackedStringArray([LOBBY + "|Entrada_DesdeComisaria"]))
	k.ambiente_sonoro("AmbienteComisaria", raiz, Vector3.ZERO, "res://Assets/Audio/Ambiente/zumbido_fluorescente.ogg", -24.0)
	k.ambiente_sonoro("RelojPared", raiz, Vector3(4.0, 2.1, 0.1), "res://Assets/Audio/Ambiente/reloj_pared.wav", -16.0, 6.0)
	k.guardar(ESCENA)
	raiz.free()
	quit()

func _ventanas() -> void:
	var v := k.nodo("Ventanas")
	var tablilla := {"Tablilla": k.mat_color("com_persiana", Color("eee6d2"), 0.6, Vector2(1, 1), 0.02)}
	for i in [1, 2, 5, 6]:
		var m: Array = k.en_muro(Vector3.ZERO, ANCHO, FONDO, "norte", i)
		k.persiana(v, m[0], m[1], tablilla)
	# La calle de mañana (la comisaría está en planta baja)
	k.vista_calle(v, Vector3(4.0, 0, -0.2), 0.0, 60.0, 7)

func _salida() -> StaticBody3D:
	var m: Array = k.en_muro(Vector3.ZERO, ANCHO, FONDO, "sur", 6)
	var hoja := {"Hoja": k.mat_color("com_puerta", Color("6b4527"), 0.55, Vector2(1, 6), 0.1)}
	var s := k.puerta_salida(k.raiz.get_node("Cuarto"), "Salida", "Ir a la Torre Miawrio", m[0], m[1], "puerta_hoja_vidrio", hoja)
	# Letrero sobre la puerta
	var letrero := Label3D.new()
	letrero.name = "LetreroSalida"
	letrero.text = "SALIDA"
	letrero.font_size = 48
	letrero.pixel_size = 0.003
	letrero.modulate = Color("2f6b3a")
	letrero.outline_size = 0
	letrero.position = m[0] + Vector3(0, 2.45, -0.02)
	letrero.rotation_degrees.y = 180.0
	k.raiz.get_node("Cuarto").add_child(letrero)
	return s

func _muebles() -> void:
	var m := k.nodo("Muebles")
	# Escritorio de Hecdog (este), bajo la ventana; su silla un poco girada (se acaba de levantar)
	var esc_h := k.prefab("escritorio", m, Vector3(5.75, 0, 1.0), 180.0, 1.0, "EscritorioHecdog")
	k.prefab("silla_escritorio", m, Vector3(5.6, 0, 1.9), 150.0, 1.0, "SillaHecdog")
	var y := k.tope(esc_h)
	k.kenney("furniture-kit", "computerScreen", m, Vector3(6.3, y, 0.75), 165.0, 1.4, "MonitorHecdog")
	k.kenney("furniture-kit", "computerKeyboard", m, Vector3(6.05, y, 1.15), 180.0, 1.4, "TecladoHecdog")
	k.prefab("libros_k", m, Vector3(5.3, y, 0.8), 15.0, 1.0, "CarpetasHecdog")
	k.prefab("taza", m, Vector3(5.05, y, 0.95), -20.0, 1.0, "TazaHecdog")
	k.kenney("furniture-kit", "plantSmall2", m, Vector3(6.55, y, 1.25), 0.0, -1.0, "PlantaHecdog")
	k.prefab("portarretrato", m, Vector3(5.0, y, 1.3), 200.0, 0.8, "FotoHecdog")
	k.prefab("papelera", m, Vector3(6.9, 0, 1.6), 0.0, 1.0, "PapeleraHecdog")
	# Centro: tapete
	k.prefab("tapete_largo", m, Vector3(4.0, 0.004, 3.4), 90.0, 1.0, "Tapete")
	# Pared este: archiveros y caja de expedientes
	k.prefab("ropero", m, Vector3(7.68, 0, 3.6), -90.0, 1.0, "ArchiveroEste")
	var cajas := k.prefab("caja_carton", m, Vector3(7.55, 0, 5.35), 15.0, 1.0, "CajaExpedientes")
	k.prefab("caja_carton_abierta", m, Vector3(7.5, k.tope(cajas), 5.35), -10.0, 0.85, "CajaExpedientes2")
	k.kenney("furniture-kit", "pottedPlant", m, Vector3(0.45, 0, 0.45), 0.0, -1.0, "PlantaRincon")
	k.kenney("furniture-kit", "pottedPlant", m, Vector3(7.5, 0, 0.45), 30.0, -1.0, "PlantaRincon2")
	k.kenney("furniture-kit", "lampSquareCeiling", m, Vector3(2.0, ALTO - 0.5, 2.5), 0.0, -1.0, "LamparaTecho1")
	k.kenney("furniture-kit", "lampSquareCeiling", m, Vector3(5.75, ALTO - 0.5, 2.5), 0.0, -1.0, "LamparaTecho2")

func _escritorio_caneloso() -> void:
	var m := k.nodo("EscritorioCaneloso")
	var esc := k.prefab("escritorio", m, Vector3(2.0, 0, 1.0), 180.0, 1.0, "Escritorio")
	k.prefab("silla_escritorio", m, Vector3(2.0, 0, 1.9), 180.0, 1.0, "Silla")
	var y := k.tope(esc)
	# Radio de policía, café humeante, papeles y la lámpara
	k.prefab("radio_vieja", m, Vector3(1.35, y, 0.8), 195.0, 0.8, "RadioPolicia")
	var taza := k.prefab("taza_cafe", m, Vector3(2.6, y, 1.25), 30.0, 1.0, "Cafe")
	k.vapor(m, Vector3(2.6, k.tope(taza) - 0.01, 1.25))
	k.modelo("res://Assets/Cinematicas/Modelos/polyhaven/desk_lamp_arm_01/desk_lamp_arm_01_1k.gltf", m, Vector3(2.65, y, 0.75), 210.0, 1.0, "Lampara")
	k.prefab("libros_k", m, Vector3(1.5, y, 1.25), -25.0, 1.0, "Carpetas")
	# La invitación (se lee, no es evidencia) y el sobre de Eriz abierto junto a la P01
	var inv: Node3D = load(PISTAS + "invitacion_miawrio/invitacion_miawrio.tscn").instantiate()
	k.pista(m, "invitacion_miawrio", "Invitación de Miawrio",
		"“Miawrio Nyx” tiene el honor de invitar al Detective Caneloso a su despacho corporativo, para revisar con total transparencia el Proyecto Pino Lunar.",
		"", LUGAR, inv, Vector3(1.75, y + 0.001, 1.15), -12.0, {"EsEvidencia": false})
	k.modelo(PISTAS + "sobre_eriz/sobre_eriz.tscn", m, Vector3(2.32, y + 0.001, 0.82), 20.0, 1.0, "SobreEriz")
	var dibujo: Node3D = load(PISTAS + "dibujo_eriz/dibujo_eriz.tscn").instantiate()
	k.pista(m, "dibujo_eriz", "Dibujo de Eriz",
		"Foto que tomo Eriz. Camioneta negra y linternas en el campamento sellado, una semana antes de la subasta.",
		"«Alguien rompió los sellos para revisar una propiedad que todavía no había comprado. O que ya sabía que iba a comprar.»",
		LUGAR, dibujo, Vector3(2.2, y + 0.003, 1.12), 8.0)

func _pared_oeste() -> void:
	var m := k.nodo("ParedOeste")
	k.prefab("librero", m, Vector3(0.3, 0, 2.7), 90.0, 1.0, "Librero")
	k.prefab("perchero_pie", m, Vector3(0.45, 0, 5.4), 0.0, 1.0, "Perchero")
	var c := k.modelo(PROPS + "corcho.glb", m, Vector3(0.0, 1.15, 4.2), 90.0, 1.0, "Corcho")
	c.add_to_group("auditoria_pared", true)
	var r := k.modelo(PROPS + "reloj_pared.glb", m, Vector3(4.0, 2.25, 0.0), 0.0, 1.0, "RelojPared")
	r.add_to_group("auditoria_pared", true)
	var q := k.prefab("cuadro", m, Vector3(3.2, 1.55, FONDO - 0.02), 180.0, 1.0, "Cuadro")
	q.add_to_group("auditoria_pared", true)

func _rincon_cafe() -> void:
	var m := k.nodo("RinconCafe")
	var mueble := k.prefab("buro", m, Vector3(7.78, 0, 2.2), -90.0, 1.0, "MuebleCafe")
	var y := k.tope(mueble)
	k.kenney("furniture-kit", "kitchenCoffeeMachine", m, Vector3(7.75, y, 2.0), -90.0, -1.0, "Cafetera")
	k.prefab("taza", m, Vector3(7.8, y, 2.45), 40.0, 1.0, "TazaLimpia")
	k.prefab("taza", m, Vector3(7.72, y, 2.6), -15.0, 1.0, "TazaLimpia2")

func _personajes() -> void:
	var p := k.nodo("Personajes")
	var taza: Node3D = load("res://Assets/Caso1/Modelos/_prefabs/taza.tscn").instantiate()
	k.personaje("Hecdog", p, Vector3(5.0, 0, 2.4), 20.0, taza)

func _luces() -> void:
	var l := k.nodo("Luces")
	k.entorno(Color("6f9fd8"), Color("dfe8ef"), Color("c9b89a"), 0.5, 1.0)
	# Principal: sol de mañana entrando por las persianas (norte)
	k.sol(l, "Sol", 30.0, 165.0, Color("ffe2b0"), 1.7)
	# Relleno: plafones cálidos (sin sombra)
	k.omni(l, "Plafon1", Vector3(2.0, 2.2, 2.5), Color("ffd59a"), 0.55, 5.0)
	k.omni(l, "Plafon2", Vector3(5.75, 2.2, 2.5), Color("ffd59a"), 0.55, 5.0)
	k.omni(l, "LamparaCaneloso", Vector3(2.55, 1.25, 0.85), Color("ffc27a"), 0.7, 2.2)
	# Contraluz: el cielo de las ventanas
	k.omni(l, "ContraluzVentanas", Vector3(4.0, 1.7, 0.5), Color("e6f0ff"), 0.45, 5.0)
