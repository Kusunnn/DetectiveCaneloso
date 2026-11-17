extends SceneTree
## Capítulo 2 · 3.2 Torre Miawrio · Lobby (res://Escenas/Caso2/torre_lobby.tscn), 12 × 10 m, techo de 4 m.
## Mármol crema, nogal y dorado. Ventanales a la calle de mañana (norte), recepción al centro y elevadores al fondo.
## Uso: <Godot>_console.exe --headless --path game --script res://Herramientas/Caso2/generar_lobby.gd

const Constructor := preload("res://Herramientas/Caso2/constructor.gd")
const ESCENA := "res://Escenas/Caso2/torre_lobby.tscn"
const COMISARIA := "res://Escenas/Caso2/comisaria_manana.tscn"
const DESPACHO := "res://Escenas/Caso2/torre_despacho.tscn"
const PISTAS := "res://Assets/Pistas/Modelos/"
const PROPS := "res://Assets/Caso2/Modelos/props/"
const GEN := "res://Assets/Pistas/Modelos/generadas/"
const ANCHO := 12
const FONDO := 10
const ALTO := 4.0
const LUGAR := "Torre Miawrio · Lobby"

var k: Constructor

func _initialize() -> void:
	var raiz := Node3D.new()
	raiz.name = "TorreLobby"
	k = Constructor.new(raiz, "lobby")

	var mats_muro := {
		"Muro": k.mat_color("lob_muro", Color("e8dcc4"), 0.85, Vector2(1, 1), 0.035),
		"Zocalo": k.mat_color("lob_zocalo", Color("6b4527"), 0.5, Vector2(1, 6), 0.12),
		"Marco": k.mat_color("lob_marco", Color("2b2622"), 0.4, Vector2(1, 1), 0.04),
	}
	var piso := k.malla_kit("piso_losas", {
		"Losa": k.mat_color("lob_marmol", Color("e9e1d0"), 0.18, Vector2(2, 2), 0.06),
		"Losa2": k.mat_color("lob_marmol2", Color("e2d8c4"), 0.18, Vector2(2, 2), 0.06),
	})
	var techo := {"Plafon": k.mat_color("lob_plafon", Color("f3eee2"), 0.9, Vector2(1, 1), 0.02)}
	var ventanales := {}
	for i in ANCHO:
		ventanales[i] = "ventanal"
	ventanales[5] = "puerta"
	ventanales[6] = "puerta"
	k.cuarto("Cuarto", Vector3.ZERO, ANCHO, FONDO, ALTO,
		{"norte": ventanales, "sur": {3: "puerta", 5: "puerta"}}, mats_muro, piso, techo)

	_fachada()
	_recepcion()
	_sala_espera()
	_fondo()
	_luces()
	var esc := k.base_jugable(LUGAR, Vector3(6.0, 0, 1.6), 180.0)
	var elevador := k.punto(k.raiz, "Elevador", "Subir al piso 30", Vector3(5.5, 1.1, FONDO - 0.15), Vector3(1.0, 2.2, 0.3))
	var salida := k.punto(k.raiz, "Salida", "Volver a la comisaría", Vector3(6.0, 1.05, 0.12), Vector3(1.8, 2.1, 0.25))
	esc.set("Salidas", [elevador, salida])
	esc.set("Destinos", PackedStringArray([DESPACHO + "|Entrada_DesdeElevador", COMISARIA + "|Entrada_DesdeTorre"]))
	k.marcador("Entrada_DesdeComisaria", k.raiz, Vector3(6.0, 0, 1.6), 180.0)
	k.marcador("Entrada_DesdeElevador", k.raiz, Vector3(5.5, 0, 8.8), 0.0)
	k.ambiente_sonoro("AmbienteLobby", raiz, Vector3.ZERO, "res://Assets/Audio/Ambiente/zumbido_sala.ogg", -26.0)
	k.guardar(ESCENA)
	raiz.free()
	quit()

func _fachada() -> void:
	var f := k.nodo("Fachada")
	# Puertas de vidrio de la entrada y la calle de mañana al otro lado de los ventanales
	var hoja := {"Hoja": k.mat_color("lob_puerta", Color("2b2622"), 0.4, Vector2(1, 1), 0.04)}
	for i in [5, 6]:
		var m: Array = k.en_muro(Vector3.ZERO, ANCHO, FONDO, "norte", i)
		k.pieza_kit("puerta_hoja_vidrio", f, m[0] + Vector3(-0.43, 0.01, -0.06), 0.0, hoja, "PuertaVidrio%d" % i)
	k.vista_calle(f, Vector3(6.0, 0, -0.3), 0.0, 70.0, 11)
	var tapete := k.kenney("furniture-kit", "rugDoormat", f, Vector3(6.0, 0.003, 0.9), 0.0, 3.0, "TapeteEntrada")

func _recepcion() -> void:
	var r := k.nodo("Recepcion")
	var mostrador := k.modelo(PROPS + "mostrador.glb", r, Vector3(6.0, 0, 5.6), 180.0, 1.0, "Mostrador")
	k.colision("ColisionMostrador", r, Vector3(6.0, 0.55, 5.6), Vector3(3.3, 1.1, 0.9), Constructor.CAPA_MUEBLES)
	var y := k.tope(mostrador)
	# P02 · Registro de accesos: la pantalla del mostrador, del lado del visitante
	var registro: Node3D = load(PISTAS + "registro_accesos/registro_accesos.tscn").instantiate()
	k.pista(r, "registro_accesos", "Registro de accesos",
		"Registro de accesos. Tres líneas raras: Aya entró como «Invitada de: Inversiones Ixodes»; Mimita entró ayer a las 21:00 como «Ixodes – entrevista»; un técnico de «Servicios Pulgar» subió hoy al piso 23, donde está el servidor.",
		"«Un libro de visitas dice más que un testigo. No sabe mentir.»", LUGAR, registro, Vector3(6.6, y, 5.35), 195.0)
	# Libro de visitas abierto junto a la pantalla, una campanita y una planta chica
	k.prefab("libros_k", r, Vector3(5.1, y, 5.4), 175.0, 0.9, "LibroVisitas")
	k.kenney("furniture-kit", "plantSmall1", r, Vector3(7.3, y, 5.5), 0.0, 3.0, "PlantaMostrador")
	# Recepcionista (marcador temporal: no hay modelo)
	k.figura_fondo(r, "Recepcionista", Vector3(6.2, 0, 6.35), 180.0, Color("2c3a52"), "Idle", 0.78)
	k.prefab("silla_escritorio", r, Vector3(5.2, 0, 6.5), 200.0, 1.0, "SillaRecepcion")

func _sala_espera() -> void:
	var s := k.nodo("SalaEspera")
	for lado in [[2.4, 90.0], [9.6, -90.0]]:
		var x: float = lado[0]
		k.prefab("tapete", s, Vector3(x, 0.004, 3.3), 0.0, 1.3, "Tapete%d" % int(x))
		var mesa := k.prefab("mesa_baja", s, Vector3(x, 0, 3.3), 90.0, 0.8, "MesaBaja%d" % int(x))
		k.prefab("sillon", s, Vector3(x - 1.3, 0, 3.3), 90.0, 1.0, "SillonA%d" % int(x))
		k.prefab("sillon", s, Vector3(x + 1.3, 0, 3.3), -90.0, 1.0, "SillonB%d" % int(x))
		var y := k.tope(mesa)
		k.prefab("libros_k", s, Vector3(x - 0.1, y, 3.2), 30.0, 0.8, "Revistas%d" % int(x))
		k.kenney("furniture-kit", "plantSmall3", s, Vector3(x + 0.25, y, 3.45), 0.0, 3.0, "PlantaMesa%d" % int(x))
	for p in [Vector3(0.6, 0, 0.7), Vector3(11.4, 0, 0.7), Vector3(0.6, 0, 9.3), Vector3(11.4, 0, 6.3)]:
		k.kenney("furniture-kit", "pottedPlant", s, p, p.x * 37.0, 3.4)

func _fondo() -> void:
	var f := k.nodo("ParedFondo")
	# Elevadores en los huecos 3 y 5 del muro sur
	for i in [3, 5]:
		var m: Array = k.en_muro(Vector3.ZERO, ANCHO, FONDO, "sur", i)
		k.modelo(PROPS + "elevador.glb", f, m[0], m[1], 1.0, "Elevador%d" % (1 if i == 3 else 2))
		var piso := Label3D.new()
		piso.name = "PisoElevador%d" % (1 if i == 3 else 2)
		piso.text = "12" if i == 3 else "PB"
		piso.font_size = 64
		piso.pixel_size = 0.0016
		piso.modulate = Color("ffd9a0")
		piso.outline_size = 0
		piso.position = m[0] + Vector3(0, 2.45, -0.045)
		piso.rotation_degrees.y = 180.0
		f.add_child(piso)
	# Logo dorado y directorio
	var logo := k.modelo(PROPS + "logo_torre.glb", f, Vector3(9.0, 1.6, FONDO), 180.0, 1.1, "LogoTorre")
	logo.add_to_group("auditoria_pared", true)
	var nombre := Label3D.new()
	nombre.name = "NombreTorre"
	nombre.text = "TORRE MIAWRIO"
	nombre.font = load("res://Assets/Fuentes/OldStandard-Bold.ttf")
	nombre.font_size = 96
	nombre.pixel_size = 0.004
	nombre.modulate = Color("c9a04a")
	nombre.outline_size = 0
	nombre.position = Vector3(9.0, 1.25, FONDO - 0.03)
	nombre.rotation_degrees.y = 180.0
	f.add_child(nombre)
	k.pantalla(f, "Directorio", Vector3(1.6, 1.6, FONDO - 0.03), 180.0, Vector2(0.75, 1.0), GEN + "c2_directorio.png", 0.35)
	for c in [[Vector3(ANCHO - 0.02, 1.9, 3.3), -90.0], [Vector3(ANCHO - 0.02, 1.9, 7.0), -90.0]]:
		k.prefab("cuadro_grande", f, c[0], c[1], 1.0).add_to_group("auditoria_pared", true)
	k.prefab("tapete_largo", f, Vector3(6.0, 0.004, 3.0), 0.0, 1.5, "TapeteCentral")
	k.prefab("banca_parque", f, Vector3(0.6, 0, 7.4), 90.0, 1.0, "Banca")
	# Pantalla de noticias en la pared oeste, sobre la sala de espera
	k.modelo(PROPS + "marco_pantalla.glb", f, Vector3(0.0, 2.45, 3.3), 90.0, 1.0, "TelevisionNoticias").add_to_group("auditoria_pared", true)
	k.pantalla(f, "Noticias", Vector3(0.056, 2.45, 3.3), 90.0, Vector2(1.6, 0.9), GEN + "c2_noticias.png", 0.8)

func _luces() -> void:
	var l := k.nodo("Luces")
	k.entorno(Color("6f9fd8"), Color("e4ecf2"), Color("d6c7aa"), 0.5, 0.9)
	k.sol(l, "Sol", 35.0, 170.0, Color("ffe6bd"), 1.4)
	for p in [Vector3(3.0, 3.7, 3.0), Vector3(9.0, 3.7, 3.0), Vector3(3.0, 3.7, 7.5), Vector3(9.0, 3.7, 7.5)]:
		k.omni(l, "Plafon", p, Color("ffd9a0"), 0.6, 6.5)
	k.omni(l, "LuzMostrador", Vector3(6.0, 2.6, 5.4), Color("ffcf8a"), 0.9, 4.0)
	k.omni(l, "BrilloNoticias", Vector3(0.8, 2.4, 3.3), Color("9fb8ff"), 0.5, 3.0)
	k.omni(l, "ContraluzLogo", Vector3(9.0, 3.4, FONDO - 0.6), Color("ffd28a"), 0.7, 3.5)
	for x in [4.5, 7.5]:
		k.kenney("furniture-kit", "lampSquareCeiling", l, Vector3(x, ALTO - 0.5, 5.6), 0.0, 2.2, "Colgante%d" % int(x))
