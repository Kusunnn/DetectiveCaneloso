extends SceneTree
## HU-20.6: retratos de diálogo de los personajes del Caso 1 que todavía no tenían (Assets/Retratos/<nombre>.png),
## tomados con sus modelos en la sala de interrogatorio del Capítulo 1, igual que Herramientas/retratos.gd hace con
## los del Caso 0. El nombre del archivo es el que busca VentanaDialogo.HablantePorNombre (minúsculas, sin acentos).
## Necesita ventana. Uso: --path game --script res://Herramientas/Caso1/retratos_caso1.gd -- [carpeta_salida|-] [nodos...]

const ESCENA := "res://Escenas/Caso1/Interrogatorio/sala_interrogatorio_caso1.tscn"
const TAM := 512
# nodo de la escena: [archivo, altura de la cara sobre los pies, distancia de la cámara]
# (la altura sale del alto de cada modelo en la sala; los esqueletos de estos modelos no traen un hueso de cabeza común)
const PERSONAJES := {
	"Implicados/JuanCarlos": ["juan carlos", 1.3, 3.7],
	"Implicados/Aya": ["aya", 1.28, 2.2],
	"Implicados/Miawrio": ["miawrio", 1.28, 2.4],
	"Implicados/Mimita": ["mimita", 1.05, 2.0],
	"Implicados/Tony": ["tony", 1.42, 2.4],
	"Hecdog": ["hecdog", 1.62, 2.4],
}

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	var carpeta: String = args[0] if args.size() > 0 and args[0] != "-" else ProjectSettings.globalize_path("res://Assets/Retratos")
	var solo: Array = Array(args.slice(1)) if args.size() > 1 else []
	DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
	DisplayServer.window_set_size(Vector2i(1200, 900))
	var escena: Node = load(ESCENA).instantiate()
	root.add_child(escena)
	current_scene = escena
	await create_timer(2.0).timeout
	for capa in escena.find_children("*", "CanvasLayer", true, false):
		capa.visible = false
	var jugador: Node3D = escena.get_node("Jugador")
	var cam := Camera3D.new()
	escena.add_child(cam)
	cam.current = true
	cam.fov = 32
	cam.near = 0.02
	var luz := OmniLight3D.new()
	cam.add_child(luz)
	luz.position = Vector3(0.6, 0.4, 0.2)
	luz.light_color = Color(1.0, 0.9, 0.78)
	luz.light_energy = 1.4
	luz.omni_range = 5.0
	var silla: Node3D = escena.get_node("PuntoSilla")
	# Que la mesa y las sillas no tapen la cara
	for mueble in ["Mesa", "SillaDetective", "SillaSospechoso"]:
		(escena.get_node(mueble) as Node3D).visible = false
	for ruta in PERSONAJES:
		var datos: Array = PERSONAJES[ruta]
		if solo.size() > 0 and not solo.has(datos[0]): continue
		var p := escena.get_node(ruta) as Node3D
		for otro in escena.get_node("Implicados").get_children(): otro.visible = false
		p.visible = true
		if ruta.begins_with("Implicados/"):
			p.global_position = silla.global_position
			p.global_rotation = Vector3(0, silla.global_rotation.y, 0)
		await create_timer(0.6).timeout
		var cara := p.global_position + Vector3.UP * float(datos[1])
		var frente := p.global_basis.z
		frente.y = 0
		frente = frente.normalized()
		var pos: Vector3 = cara + frente * float(datos[2]) + Vector3.UP * 0.05
		jugador.global_position = pos - Vector3.UP * 1.4
		cam.look_at_from_position(pos, cara)
		await create_timer(1.4).timeout
		await RenderingServer.frame_post_draw
		_guardar(root.get_texture().get_image(), carpeta.path_join(datos[0] + ".png"))
		if ruta.begins_with("Implicados/"): p.visible = false
	quit()

func _guardar(imagen: Image, ruta: String) -> void:
	var lado := mini(imagen.get_width(), imagen.get_height())
	imagen = imagen.get_region(Rect2i((imagen.get_width() - lado) / 2, (imagen.get_height() - lado) / 2, lado, lado))
	imagen.resize(TAM, TAM, Image.INTERPOLATE_LANCZOS)
	imagen.save_png(ruta)
	print("retrato: ", ruta)
