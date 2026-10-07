extends SceneTree
# Herramienta: toma la foto de cada pista del Escenario 1 en su lugar y la guarda en Assets/Pistas/caso1/<id>.png,
# con el mismo estilo que las del Caso 0 (el objeto en su sitio, 256 × 256).
# Uso (con ventana, porque hay que renderizar):
#   Godot_v4.7.2-stable_mono_win64_console.exe --path game --script res://Escenas/Caso1/fotografo_pistas.gd

const ESCENA := "res://Escenas/Caso1/escenario_campamento.tscn"
const CARPETA := "res://Assets/Pistas/caso1/"

# id: [ruta del nodo, dirección desde la que se mira, distancia]
const TOMAS := {
	"interruptor_generador": ["Caseta/Interruptor", Vector3(1, 0.1, 0.25), 0.95],
	"candado_caseta": ["Caseta/Candado", Vector3(1, 0.25, -0.35), 0.55],
	"baul_disfraces": ["Almacen/PistaBaul", Vector3(0.5, 1, 0.2), 1.25],
	"lena_intacta": ["ZonaFogata/Lena", Vector3(-1, 0.55, 0.8), 3.4],
	"libro_guardia_quemado": ["ZonaFogata/Fuego/LibroQuemado", Vector3(0.25, 1, 0.45), 0.85],
	"nota_porky": ["CabanaPorky/NotaPorky", Vector3(-0.35, 1, 0.1), 0.6],
	"foto_grupo": ["CabanaPorky/ParedFotos/FotoGrupo", Vector3(0, -0.05, 1), 0.75],
	"recorte_periodico": ["CabanaPorky/Recorte", Vector3(-1, 0, 0), 0.85],
	"oferta_compra": ["CabanaPorky/Oferta", Vector3(0.2, 1, 0.35), 0.55],
	"taza_delantal": ["Cocina/TazaDelantal", Vector3(-0.7, 0.75, -0.25), 0.45],
	"registro_enfermeria": ["Enfermeria/Escritorio/Modelo/desk(Clone)/drawer/Registro", Vector3(0.35, 1, 0.1), 0.6],
	"botas_tony": ["CabanaDirector/Trasera/Botas", Vector3(0.4, 0.9, 0.9), 1.25],
	"registro_llamadas": ["Estacionamiento/Coche/RegistroLlamadas", Vector3(-1, 0.25, 0.1), 0.7],
}
const ABRIR := ["Almacen/Baul", "CabanaPorky/Almohada", "CabanaPorky/Papelera", "Enfermeria/Escritorio"]

func _init() -> void:
	await process_frame
	root.size = Vector2i(512, 512)
	var esc: Node = load(ESCENA).instantiate()
	root.add_child(esc)
	current_scene = esc
	for i in 10:
		await process_frame
	for capa in esc.find_children("*", "CanvasLayer", true, false):
		capa.visible = false
	var caso := esc.get_node("Caso1")
	caso.set_process(false)
	var jugador := esc.get_node("Jugador")
	jugador.set_process(false)
	jugador.set_physics_process(false)
	jugador.visible = false
	for ruta in ABRIR:
		esc.get_node(ruta).Interactuar(jugador)
	for n in get_nodes_in_group("solo_con_linterna"):
		n.visible = true
	esc.get_node("Estacionamiento/Coche/RegistroLlamadas").visible = true
	await create_timer(1.5).timeout

	var cam := Camera3D.new()
	esc.add_child(cam)
	cam.fov = 45
	cam.current = true
	var foco := SpotLight3D.new()
	cam.add_child(foco)
	foco.spot_range = 8
	foco.spot_angle = 35
	foco.light_energy = 2.2
	foco.light_color = Color(1, 0.94, 0.82)
	foco.shadow_enabled = true
	foco.position = Vector3(0.25, 0.2, 0)
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(CARPETA))
	for id in TOMAS:
		var toma: Array = TOMAS[id]
		var nodo := esc.get_node(toma[0]) as Node3D
		var centro := nodo.global_position
		for hijo in nodo.get_children():
			if hijo is CollisionShape3D:
				centro = hijo.global_position
		cam.global_position = centro + (toma[1] as Vector3).normalized() * float(toma[2])
		cam.look_at(centro, Vector3.UP if absf((toma[1] as Vector3).normalized().y) < 0.95 else Vector3.FORWARD)
		for i in 20:
			await process_frame
		var imagen := root.get_texture().get_image()
		imagen.resize(256, 256, Image.INTERPOLATE_LANCZOS)
		imagen.save_png(ProjectSettings.globalize_path(CARPETA + id + ".png"))
		print("Foto: ", id)
	quit()
