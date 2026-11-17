extends SceneTree
## Capítulo 2 · 3.5 Cierre (solo lo mínimo): fachada de la torre de noche, celda de Tony en el penal y el embarcadero
## sellado (escena heredada del Lago del Caso 1; la original no se toca).
## Uso: <Godot>_console.exe --headless --path game --script res://Herramientas/Caso2/generar_cierre.gd [-- escena ...]

const Constructor := preload("res://Herramientas/Caso2/constructor.gd")
const PROPS := "res://Assets/Caso2/Modelos/props/"
const C2 := "res://Escenas/Caso2/"
const LAGO := "res://Escenas/Caso1/Lago/escenario_lago.tscn"

var k: Constructor

func _initialize() -> void:
	var solo := OS.get_cmdline_user_args()
	for e in [["fachada", _fachada], ["celda", _celda], ["embarcadero", _embarcadero]]:
		if solo.is_empty() or e[0] in solo:
			(e[1] as Callable).call()
	quit()

# ---------- Fachada de la torre y banqueta, de noche ----------

func _fachada() -> void:
	var raiz := Node3D.new()
	raiz.name = "TorreFachadaNoche"
	k = Constructor.new(raiz, "fachada")
	var mats := {
		"Muro": k.mat_color("fac_muro", Color("cfc6b4"), 0.8, Vector2(1, 1), 0.05),
		"Zocalo": k.mat_color("fac_zocalo", Color("3a2f26"), 0.6, Vector2(1, 1), 0.05),
		"Marco": k.mat_color("fac_marco", Color("1e1c1a"), 0.4, Vector2(1, 1), 0.04, 0.5),
		"Vidrio": k.material("fac_vidrio", {"albedo_color": Color(0.95, 0.8, 0.55), "emission_enabled": true, "emission": Color(1.0, 0.78, 0.45), "emission_energy_multiplier": 0.6, "roughness": 0.1}),
	}
	# La torre: planta baja (ventanales y la entrada) y pisos de oficinas encima; la cara mira a -Z (a la calle)
	var torre := k.nodo("Torre")
	var huecos := {}
	for i in 16:
		huecos[i] = "ventanal"
	huecos[7] = "puerta"
	huecos[8] = "puerta"
	k.tramo_muro("PlantaBaja", torre, Vector3(16, 0, 0), 16, 180.0, 4.0, mats, huecos)
	var ventanas := {}
	for i in 16:
		ventanas[i] = "ventana" if i % 2 == 0 else "muro"
	for nivel in 5:
		k.tramo_muro("Piso%d" % nivel, torre, Vector3(16, 4.0 + nivel * 3.4, 0), 16, 180.0, 3.4, mats, ventanas)
	k.modelo(PROPS + "logo_torre.glb", torre, Vector3(8.0, 4.4, -0.05), 180.0, 1.6, "Logo").add_to_group("auditoria_pared", true)
	var nombre := Label3D.new()
	nombre.name = "NombreTorre"
	nombre.text = "TORRE MIAWRIO"
	nombre.font = load("res://Assets/Fuentes/OldStandard-Bold.ttf")
	nombre.font_size = 120
	nombre.pixel_size = 0.006
	nombre.modulate = Color("e0b65a")
	nombre.outline_size = 0
	nombre.position = Vector3(8.0, 4.15, -0.08)
	nombre.rotation_degrees.y = 180.0
	torre.add_child(nombre)
	# Banqueta, calle y la ciudad de noche enfrente (la vista mira hacia +Z desde la calle, así que se gira)
	var calle := k.vista_calle(k.raiz, Vector3(8.0, 0, -0.1), 0.0, 60.0, 23)
	calle.name = "Calle"
	var b := k.nodo("Banqueta")
	k.prefab("banca_parque", b, Vector3(4.5, 0, -1.6), 0.0, 1.0, "Banca")
	k.kenney("furniture-kit", "pottedPlant", b, Vector3(6.2, 0, -0.7), 0.0, 3.4, "Maceta1")
	k.kenney("furniture-kit", "pottedPlant", b, Vector3(9.8, 0, -0.7), 0.0, 3.4, "Maceta2")
	k.kenney("car-kit", "police", b, Vector3(12.5, 0, -7.2), 90.0, 1.65, "Patrulla")
	# Farolas que parpadean
	var luces := k.nodo("Luces")
	for x in [2.0, 14.0]:
		k.kenney("city-kit-roads", "light-curved", b, Vector3(x, 0, -2.8), 180.0, 8.0, "Farola")
		var luz := k.omni(luces, "Farola", Vector3(x, 4.2, -3.4), Color("ffc47a"), 1.6, 9.0, x == 2.0)
		luz.set_script(load("res://Escenas/Investigacion/LuzParpadeante.cs"))
	k.omni(luces, "Vitrinas", Vector3(8.0, 2.2, -1.2), Color("ffcf8a"), 1.0, 10.0)
	k.omni(luces, "Luna", Vector3(8.0, 12.0, -14.0), Color("8fa6d8"), 0.8, 30.0)
	var d := DirectionalLight3D.new()
	d.name = "LuzLuna"
	d.rotation_degrees = Vector3(-40, 20, 0)
	d.light_color = Color("7f95c8")
	d.light_energy = 0.35
	d.shadow_enabled = true
	luces.add_child(d)
	var env := k.entorno(Color("0c1226"), Color("1c2440"), Color("4a5578"), 0.45, 1.0)
	# Encuadres armados para las dos escenas (sin cinemática: solo dónde va cada quien y la cámara)
	var enc := k.nodo("Encuadres")
	k.marcador("PuntoMiawrio", enc, Vector3(7.4, 0, -2.3), 160.0)
	k.marcador("PuntoMimita", enc, Vector3(8.2, 0, -2.3), 200.0)
	k.marcador("PuntoAya", enc, Vector3(10.5, 0, -1.8), 200.0)
	k.marcador("PuntoCaneloso", enc, Vector3(11.4, 0, -3.2), 120.0)
	var c1 := Camera3D.new()
	c1.name = "Encuadre_MiawrioMimita"
	enc.add_child(c1)
	c1.look_at_from_position(Vector3(7.8, 1.2, -5.6), Vector3(7.8, 0.8, -2.3))
	c1.fov = 50.0
	c1.current = false
	var c2 := Camera3D.new()
	c2.name = "Encuadre_Aya"
	enc.add_child(c2)
	c2.look_at_from_position(Vector3(12.6, 1.4, -4.6), Vector3(10.8, 1.1, -1.8))
	c2.fov = 50.0
	c2.current = false
	var per := k.nodo("Personajes")
	k.personaje("Miawrio", per, Vector3(7.4, 0, -2.3), 160.0)
	k.personaje("Mimita", per, Vector3(8.2, 0, -2.3), 200.0)
	k.personaje("Aya", per, Vector3(10.5, 0, -1.8), 200.0)
	k.base_jugable("Torre Miawrio · Afuera", Vector3(11.4, 0, -3.2), 120.0)
	k.marcador("Entrada_Cierre", k.raiz, Vector3(11.4, 0, -3.2), 120.0)
	k.colision("LimiteCalle", k.raiz, Vector3(8.0, 1.0, -9.0), Vector3(40.0, 2.0, 0.4), Constructor.CAPA_LIMITES).add_to_group("auditoria_invisible_ok", true)
	k.colision("PisoCalle", k.raiz, Vector3(8.0, -0.1, -5.0), Vector3(60.0, 0.2, 14.0))
	k.ambiente_sonoro("Ciudad", raiz, Vector3.ZERO, "res://Assets/Audio/Ambiente/zumbido_sala.ogg", -26.0)
	k.guardar(C2 + "torre_fachada_noche.tscn")
	raiz.free()

# ---------- Celda de Tony en el penal ----------

func _celda() -> void:
	var raiz := Node3D.new()
	raiz.name = "PenalCelda"
	k = Constructor.new(raiz, "celda")
	var mats := {
		"Muro": k.mat_textura("cel_muro", "Concrete034", 0.7, Color(0.78, 0.8, 0.76)),
		"Zocalo": k.mat_color("cel_zocalo", Color("5d6660"), 0.8, Vector2(1, 1), 0.05),
		"Marco": k.mat_color("cel_marco", Color("3a3f44"), 0.5, Vector2(1, 1), 0.04, 0.5),
	}
	var piso := k.malla_kit("piso_losas", {"Losa": k.mat_textura("cel_losa", "Concrete034", 0.5, Color(0.7, 0.7, 0.68)), "Losa2": k.mat_textura("cel_losa2", "Concrete034", 0.5, Color(0.66, 0.66, 0.64))})
	k.cuarto("Cuarto", Vector3.ZERO, 3, 4, 2.8, {"norte": {1: "ventana"}, "sur": {1: "puerta"}}, mats, piso, {"Plafon": k.mat_color("cel_plafon", Color("cfcfc8"), 0.95, Vector2(1, 1), 0.03)})
	var m := k.nodo("Celda")
	k.modelo(PROPS + "barrotes.glb", m, Vector3(1.5, 0.9, 0.0), 0.0, 1.0, "Barrotes").add_to_group("auditoria_pared", true)
	k.modelo(PROPS + "reja_celda.glb", m, Vector3(1.5, 0.0, 4.0), 180.0, 1.0, "Reja")
	k.colision("ColisionReja", m, Vector3(1.5, 1.05, 3.97), Vector3(0.9, 2.1, 0.06))
	var catre := k.prefab("cama_sin_almohada", m, Vector3(0.6, 0, 2.0), 0.0, 0.9, "Catre")
	k.prefab("mesa_chica", m, Vector3(2.45, 0, 1.0), 0.0, 0.85, "Mesa")
	k.prefab("banco", m, Vector3(2.4, 0, 1.75), 0.0, 1.0, "Banco")
	var y := k.tope(m.get_node("Mesa"))
	# El libro sin remitente, con la tarjeta entre las páginas (se lee en la escena, no es pista)
	k.modelo(PROPS + "libro.glb", m, Vector3(2.45, y, 1.0), 20.0, 1.0, "Libro")
	k.prefab("taza", m, Vector3(2.65, y, 0.85), 0.0, 1.0, "Taza")
	# La vista por la ventana: el muro del penal y el cielo
	var fuera := k.nodo("Fuera")
	fuera.add_to_group("auditoria_fuera_ok", true)
	k.tramo_muro("MuroPenal", fuera, Vector3(-6, 0, -6), 15, 0.0, 4.0, mats)
	var per := k.nodo("Personajes")
	var libro: Node3D = load(PROPS + "libro.glb").instantiate()
	k.personaje("Tony", per, Vector3(1.4, 0, 2.7), 20.0)
	var l := k.nodo("Luces")
	k.omni(l, "Foco", Vector3(1.5, 2.55, 2.0), Color("ffe2b0"), 0.9, 5.0, true)
	k.omni(l, "Ventana", Vector3(1.5, 1.6, 0.4), Color("cfe0ff"), 0.6, 4.0)
	k.sol(l, "Sol", 40.0, 160.0, Color("ffe0b0"), 1.0)
	k.entorno(Color("6f8fc8"), Color("c8d2dc"), Color("9aa0a0"), 0.45, 1.0)
	k.base_jugable("Penal estatal · Celda", Vector3(1.5, 0, 3.3), 0.0)
	k.marcador("Entrada_Cierre", k.raiz, Vector3(1.5, 0, 3.3), 0.0)
	k.guardar(C2 + "penal_celda.tscn")
	raiz.free()

# ---------- Embarcadero sellado (heredado del Lago del Caso 1) ----------

func _embarcadero() -> void:
	var base: PackedScene = load(LAGO)
	var r: Node3D = base.instantiate(PackedScene.GEN_EDIT_STATE_MAIN_INHERITED)
	r.name = "EmbarcaderoSellado"
	k = Constructor.new(r, "embarcadero")
	# El guion del Caso 1 no corre: su nodo pasa a ser el escenario del Cap. 2
	var guion := r.get_node("Caso1Lago")
	guion.set_script(load("res://Escenas/Caso2/EscenarioCaso2.cs"))
	guion.set("Jugador", r.get_node("Jugador"))
	guion.set("Hud", r.get_node("HUD"))
	guion.set("Dialogo", r.get_node("VentanaDialogo"))
	guion.set("Musica", r.get_node("MusicaFondo"))
	guion.set("Lugar", "Campamento Pino Lunar · Embarcadero")
	var v := Node.new()
	v.name = "VarianteEmbarcadero"
	v.set_script(load("res://Escenas/Caso2/VarianteEmbarcadero.cs"))
	r.add_child(v)
	(r.get_node("Jugador") as Node3D).position = Vector3(8.0, 1.4, -3.0)
	# Sellos rotos y la cinta amarilla deshilachada a la entrada del muelle
	var s := k.nodo("Sellos")
	s.add_to_group("caso2", true)
	for x in [6.95, 9.05]:
		k.prefab("poste_madera", s, Vector3(x, 0.3, -7.0), 0.0, 1.0, "Poste")
		k.modelo(PROPS + "sello_roto.glb", s, Vector3(x, 1.0, -6.93), 0.0, 1.0, "Sello")
	k.modelo(PROPS + "cinta_amarilla.glb", s, Vector3(6.95, 1.15, -6.98), 0.0, 1.0, "CintaAmarilla")
	k.modelo(PROPS + "cinta_amarilla.glb", s, Vector3(6.95, 0.7, -6.98), 0.0, 1.0, "CintaAmarilla2")
	k.marcador("Entrada_Cierre", s, Vector3(8.0, 0.4, -3.0), 0.0)
	k.marcador("PuntoSilueta", s, Vector3(8.0, 0.35, -30.0), 0.0)
	# Solo lo nuevo se adueña de la escena: lo heredado se queda como en el Lago original
	for n in [v, s]:
		n.owner = r
		_duenos_nuevos(r, n)
	var ps := PackedScene.new()
	ps.pack(r)
	ResourceSaver.save(ps, C2 + "embarcadero_sellado.tscn")
	print("  guardada ", C2 + "embarcadero_sellado.tscn")
	r.free()

func _duenos_nuevos(r: Node, n: Node) -> void:
	for h in n.get_children():
		h.owner = r
		if h.scene_file_path.is_empty():
			_duenos_nuevos(r, h)
