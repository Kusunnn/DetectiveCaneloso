extends SceneTree
## Capítulo 2: toma la foto de cada pista en su lugar (256 × 256) para el expediente y el diario.
## Salida: res://Assets/Pistas/caso2/<id>.png. Necesita ventana. Los cajones con pista se abren para la foto.
## Uso: <Godot>_console.exe --path game --script res://Herramientas/Caso2/fotografo_pistas_caso2.gd [-- id ...]

const CARPETA := "res://Assets/Pistas/caso2/"
const ESCENAS := ["res://Escenas/Caso2/comisaria_manana.tscn", "res://Escenas/Caso2/torre_lobby.tscn", "res://Escenas/Caso2/torre_despacho.tscn"]
# Desde dónde se mira cada pista (dirección y distancia extra); por defecto, desde arriba y un poco de frente
# [dirección, distancia extra, en qué ejes: "mundo", "pista" (los de la pista) o "padre" (los del cajón)]
const TOMAS := {
	"registro_accesos": [Vector3(0.0, 0.3, 1.0), 0.6, "pista"],
	"planos_clinica": [Vector3(0.15, 1.0, 0.35), 0.5, "mundo"],
	"estado_cuenta": [Vector3(0.0, 1.0, 0.9), 0.15, "padre"],
	"expediente_filtrado": [Vector3(0.0, 1.0, -0.9), 0.15, "padre"],
}

func _init() -> void:
	await process_frame
	root.size = Vector2i(512, 512)
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(CARPETA))
	var solo := OS.get_cmdline_user_args()
	for ruta in ESCENAS:
		var esc: Node = load(ruta).instantiate()
		root.add_child(esc)
		current_scene = esc
		for i in 8:
			await process_frame
		for capa in esc.find_children("*", "CanvasLayer", true, false):
			capa.visible = false
		var jugador := esc.get_node("Jugador")
		jugador.set_process(false)
		jugador.set_physics_process(false)
		jugador.visible = false
		# Abre los cajones y muestra lo que tienen dentro
		for c in esc.find_children("*", "StaticBody3D", true, false):
			if c.get_script() and str(c.get_script().resource_path).ends_with("Contenedor.cs"):
				var parte: Node3D = c.get("Parte")
				if parte:
					parte.position += c.get("Desplazamiento")
				for p in c.find_children("Pista_*", "", true, false):
					p.visible = true
		for p in esc.find_children("Pista_*", "StaticBody3D", true, false):
			var id: String = p.get("Id")
			if not p.get("EsEvidencia") or (not solo.is_empty() and not id in solo):
				continue
			await _foto(esc, p, id)
		esc.queue_free()
		await process_frame
	print("Fotos del Capítulo 2 listas")
	quit()

func _foto(esc: Node, pista: Node3D, id: String) -> void:
	var caja := AABB()
	var primera := true
	for vi in pista.find_children("*", "VisualInstance3D", true, false):
		var b: AABB = (vi as VisualInstance3D).global_transform * (vi as VisualInstance3D).get_aabb()
		caja = b if primera else caja.merge(b)
		primera = false
	var centro := caja.get_center()
	var toma: Array = TOMAS.get(id, [Vector3(0.15, 1.0, 0.45), 0.0, "mundo"])
	var ejes := Basis.IDENTITY
	if toma[2] == "pista":
		ejes = pista.global_transform.basis.orthonormalized()
	elif toma[2] == "padre":
		ejes = (pista.get_parent() as Node3D).global_transform.basis.orthonormalized()
	var dir: Vector3 = (ejes * (toma[0] as Vector3)).normalized()
	var distancia := caja.size.length() * 1.15 + 0.12 + float(toma[1])
	var cam := Camera3D.new()
	cam.fov = 40.0
	esc.add_child(cam)
	cam.look_at_from_position(centro + dir * distancia, centro, Vector3.UP if absf(dir.y) < 0.98 else Vector3.FORWARD)
	cam.make_current()
	var luz := OmniLight3D.new()
	luz.light_energy = 0.5
	luz.omni_range = distancia * 3.0
	esc.add_child(luz)
	luz.global_position = centro + dir * distancia * 0.8 + Vector3(0.1, 0.2, 0)
	for i in 6:
		await process_frame
	var img := root.get_texture().get_image()
	img.resize(256, 256, Image.INTERPOLATE_LANCZOS)
	img.save_png(ProjectSettings.globalize_path(CARPETA + id + ".png"))
	print("  ", id)
	cam.queue_free()
	luz.queue_free()
	await process_frame
