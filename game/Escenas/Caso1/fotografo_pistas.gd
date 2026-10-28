extends SceneTree
# Herramienta: toma la foto de cada pista en su lugar (256 × 256) para el expediente.
# Caso 1 → Assets/Pistas/caso1/<id>.png; Caso 0 (con «caso0») → Assets/Pistas/<id>.png.
# Uso (con ventana, porque hay que renderizar):
#   Godot_v4.7.2-stable_mono_win64_console.exe --path game --script res://Escenas/Caso1/fotografo_pistas.gd
#   ... fotografo_pistas.gd -- caso0
#   ... fotografo_pistas.gd -- lago      (Escenario 2 del Caso 1, HU-20.6)
# Para rehacer solo algunas: ... fotografo_pistas.gd -- foto_grupo botas_tony   (o -- caso0 cinta reloj)

const ESCENA := "res://Escenas/Caso1/escenario_campamento.tscn"
const CARPETA := "res://Assets/Pistas/caso1/"
const ESCENA_CASO0 := "res://Escenas/Interrogatorio/sala_interrogatorio.tscn"
const CARPETA_CASO0 := "res://Assets/Pistas/"
const TOMAS_CASO0 := {
	"grabadora": ["GrabadoraPista", Vector3(0.35, 0.9, 1), 0.55],
	"reloj": ["RelojPista", Vector3(0, 0, 1), 0.7],
	"pluma": ["PlumaSecreta", Vector3(0.3, -1, 0.5), 0.6, 2.2],
	"huellas": ["ZonaPasillo/Huellas", Vector3(0.25, 1, 0.35), 2.8],
	"envoltorio": ["ZonaPasillo/Envoltorio", Vector3(0.3, 1, 0.35), 0.42],
	"libro_turnos": ["ZonaRecepcion/LibroTurnos", Vector3(0.2, 1, 0.45), 0.7],
	"nota_comisario": ["ZonaRecepcion/CajonEscritorio1/Movil/NotaComisario", Vector3(0.1, 1, 0.25), 0.42],
	"cinta": ["ZonaVestuario/Cinta", Vector3(1, 0.4, 0.15), 0.32],
}

# id: [ruta del nodo, dirección desde la que se mira, distancia]
const TOMAS := {
	"interruptor_generador": ["Caseta/Interruptor", Vector3(1, 0.1, 0.25), 0.95],
	"candado_caseta": ["Caseta/Candado", Vector3(1, 0.25, -0.35), 0.55],
	"baul_disfraces": ["Almacen/PistaBaul", Vector3(0.5, 1, 0.2), 1.25],
	"lena_intacta": ["ZonaFogata/Lena", Vector3(-1, 0.55, 0.8), 3.4],
	"libro_guardia_quemado": ["ZonaFogata/Fuego/LibroQuemado", Vector3(0.12, 1, -0.1), 0.95],
	"nota_porky": ["CabanaPorky/NotaPorky", Vector3(0.3, 1, 0.0), 0.65],
	"foto_grupo": ["CabanaPorky/FotoGrupo", Vector3(-1, 0.0, 0), 0.95],
	"recorte_periodico": ["CabanaPorky/Recorte", Vector3(0, 0, 1), 0.85],
	"oferta_compra": ["CabanaPorky/Oferta", Vector3(0.3, 1, 0.45), 0.55, 1.4],
	"taza_delantal": ["Cocina/TazaDelantal", Vector3(-0.7, 0.75, -0.25), 0.45],
	"registro_enfermeria": ["Enfermeria/Escritorio/Modelo/Modelo/desk(Clone)/drawer/Registro", Vector3(0.35, 1, 0.1), 0.6, 0.9],
	"botas_tony": ["CabanaDirector/Trasera/Botas", Vector3(0.4, 0.9, 0.9), 1.25],
	"registro_llamadas": ["Estacionamiento/Coche/RegistroLlamadas", Vector3(-0.94, 0.5, 0.34), 0.7],
}
const ABRIR := ["Almacen/Baul", "CabanaPorky/Almohada", "CabanaPorky/Papelera", "Enfermeria/Escritorio"]

# Escenario 2 (Lago y Bosque). id: [ruta, dirección, distancia, altura sobre el modelo (solo la linterna), fuerza del foco]
const ESCENA_LAGO := "res://Escenas/Caso1/Lago/escenario_lago.tscn"
const TOMAS_LAGO := {
	"huellas_pezuna": ["Sendero/HuellasPezuna", Vector3(0.35, 1, 0.45), 2.4, 0.05, 3.2],
	"huellas_detenidas": ["Sendero/HuellasDetenidas", Vector3(0.35, 1, 0.45), 2.2, 0.05, 3.2],
	"altavoz": ["Bosque/Altavoz", Vector3(0.75, 0.25, 0.6), 0.85, 0.09, 1.8],
	"cornamenta_zancos": ["Bosque/Cornamenta", Vector3(0.35, 1, 0.45), 1.8, 0.1, 2.2],
	"hueco_remo": ["Cobertizo/SoporteRemos", Vector3(-1, 0.25, 0.25), 2.1, 0.7, 2.0],
	"remo_sangre": ["Orilla/Remo", Vector3(0.3, 1, 0.5), 1.6, 0.05, 2.6],
	"silbato_ts": ["Embarcadero/Silbato", Vector3(1, 0.35, 0.15), 0.24, 0.0, 2.6],
	"linterna_porky": ["Embarcadero/Linterna", Vector3(-0.35, 1, 0.3), 1.1, 0.06, 1.2],
	"cuaderno_eriz": ["Orilla/Cuaderno", Vector3(0.15, 1, 0.35), 0.75, 0.02, 1.6],
}
const ABRIR_LAGO := ["Orilla/Mochila", "Bosque/TroncoHueco"]

func _init() -> void:
	await process_frame
	root.size = Vector2i(512, 512)
	var solo := OS.get_cmdline_user_args()
	var caso0 := solo.has("caso0")
	var lago := solo.has("lago")
	solo.erase("caso0")
	solo.erase("lago")
	var tomas: Dictionary = TOMAS_CASO0 if caso0 else (TOMAS_LAGO if lago else TOMAS)
	var carpeta: String = CARPETA_CASO0 if caso0 else CARPETA
	var esc: Node = load(ESCENA_CASO0 if caso0 else (ESCENA_LAGO if lago else ESCENA)).instantiate()
	root.add_child(esc)
	current_scene = esc
	for i in 10:
		await process_frame
	for capa in esc.find_children("*", "CanvasLayer", true, false):
		capa.visible = false
	var caso := esc.get_node("Caso0" if caso0 else ("Caso1Lago" if lago else "Caso1"))
	caso.set_process(false)
	var jugador := esc.get_node("Jugador")
	jugador.set_process(false)
	jugador.set_physics_process(false)
	jugador.visible = false
	if caso0:
		# Cajón y casillero abiertos sin animación (el casillero pide la combinación)
		for c in esc.find_children("*", "", true, false):
			if c.has_signal("Abierto") and c.has_method("RestaurarEstado"):
				c.RestaurarEstado(true, true)
	elif lago:
		for ruta in ABRIR_LAGO:
			esc.get_node(ruta).Interactuar(jugador)
		esc.get_node("Bosque/Altavoz/Estatica").stop()
	else:
		for ruta in ABRIR:
			esc.get_node(ruta).Interactuar(jugador)
		esc.get_node("Estacionamiento/Coche/RegistroLlamadas").visible = true
	for n in get_nodes_in_group("solo_con_linterna"):
		n.visible = true
	await create_timer(1.5).timeout

	var cam := Camera3D.new()
	esc.add_child(cam)
	cam.fov = 45
	cam.current = true
	var foco := SpotLight3D.new()
	cam.add_child(foco)
	foco.spot_range = 8
	foco.spot_angle = 35
	# Los interiores de la comisaría ya tienen luz: con menos foco no se queman los papeles
	foco.light_energy = 1.0 if caso0 else 2.2
	foco.light_color = Color(1, 0.94, 0.82)
	foco.shadow_enabled = true
	foco.position = Vector3(0.25, 0.2, 0)
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(carpeta))
	for id in tomas:
		if not solo.is_empty() and not solo.has(id):
			continue
		var toma: Array = tomas[id]
		var nodo := esc.get_node(toma[0]) as Node3D
		var centro := nodo.global_position
		for hijo in nodo.get_children():
			if hijo is CollisionShape3D:
				centro = hijo.global_position
		# La caja de la linterna llega desde el fondo del agua hasta el muelle: se mira el modelo
		if lago and id == "linterna_porky":
			centro = (nodo.get_node("Modelo") as Node3D).global_position + Vector3.UP * float(toma[3])
		cam.global_position = centro + (toma[1] as Vector3).normalized() * float(toma[2])
		cam.look_at(centro, Vector3.UP if absf((toma[1] as Vector3).normalized().y) < 0.95 else Vector3.FORWARD)
		# El registro de llamadas va dentro del coche: para la foto se esconde la carrocería
		var coche := esc.get_node_or_null("Estacionamiento/Coche/Modelo") as Node3D
		if coche:
			coche.visible = id != "registro_llamadas"
		# Un cuarto valor en la toma cambia la fuerza del foco (para los papeles claros)
		foco.light_energy = float(toma[4]) if lago else (float(toma[3]) if toma.size() > 3 else (1.0 if caso0 else 2.2))
		for i in 20:
			await process_frame
		var imagen := root.get_texture().get_image()
		imagen.resize(256, 256, Image.INTERPOLATE_LANCZOS)
		imagen.save_png(ProjectSettings.globalize_path(carpeta + id + ".png"))
		print("Foto: ", id)
	quit()
