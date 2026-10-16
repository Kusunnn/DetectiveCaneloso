extends SceneTree
## Renderiza un modelo desde cuatro lados (frente, tres cuartos, arriba y atrás) en una sola imagen.
## Sirve para revisar los modelos de las pistas sin abrir el editor. Necesita ventana.
## Uso: --path game --script res://Herramientas/vista_previa_modelo.gd -- <salida.png> <escena o modelo> [más modelos...]
## Con varios modelos, cada uno ocupa una fila.

const TAM := 384

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	var salida: String = args[0]
	var modelos := args.slice(1)
	DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
	var vista := SubViewport.new()
	vista.size = Vector2i(TAM, TAM)
	vista.own_world_3d = true
	vista.transparent_bg = false
	vista.msaa_3d = Viewport.MSAA_4X
	vista.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	root.add_child(vista)
	var entorno := WorldEnvironment.new()
	var env := Environment.new()
	env.background_mode = Environment.BG_COLOR
	env.background_color = Color(0.32, 0.31, 0.3)
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color(0.45, 0.44, 0.42)
	env.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	entorno.environment = env
	vista.add_child(entorno)
	for datos in [[Vector3(-2, 2.5, 3), 1.6, Color(1, 0.92, 0.82), true], [Vector3(2.2, 1.5, -3), 1.4, Color(0.75, 0.82, 1), false], [Vector3(2.5, -0.5, 2), 0.4, Color(1, 1, 1), false]]:
		var luz := DirectionalLight3D.new()
		luz.light_energy = datos[1]
		luz.light_color = datos[2]
		luz.shadow_enabled = datos[3]
		vista.add_child(luz)
		luz.look_at_from_position(datos[0], Vector3.ZERO)
	var cam := Camera3D.new()
	cam.fov = 30
	cam.near = 0.005
	vista.add_child(cam)
	cam.current = true

	var hoja := Image.create_empty(TAM * 4, TAM * modelos.size(), false, Image.FORMAT_RGBA8)
	for fila in modelos.size():
		var recurso = load(modelos[fila])
		var modelo: Node3D = recurso.instantiate()
		vista.add_child(modelo)
		await process_frame
		var caja := _caja(modelo)
		var centro := caja.get_center()
		var radio := maxf(caja.size.length() / 2.0, 0.01)
		var distancia := radio / tan(deg_to_rad(cam.fov / 2.0)) * 1.1
		var lados := [Vector3(0, 0.15, 1), Vector3(0.8, 0.6, 0.9), Vector3(0.001, 1, 0.08), Vector3(0, 0.2, -1)]
		for col in lados.size():
			var dir: Vector3 = lados[col].normalized()
			cam.look_at_from_position(centro + dir * distancia, centro, Vector3.UP if absf(dir.y) < 0.95 else Vector3.FORWARD)
			for i in 6:
				await process_frame
			var img := vista.get_texture().get_image()
			img.convert(Image.FORMAT_RGBA8)
			hoja.blit_rect(img, Rect2i(0, 0, TAM, TAM), Vector2i(col * TAM, fila * TAM))
		print("%s  caja=%s" % [modelos[fila], caja])
		modelo.queue_free()
	hoja.save_png(salida)
	quit()

func _caja(raiz: Node3D) -> AABB:
	var caja := AABB()
	var primera := true
	for v in raiz.find_children("*", "VisualInstance3D", true, false):
		if not v.is_visible_in_tree():
			continue
		var c: AABB = v.global_transform * v.get_aabb()
		if c.size == Vector3.ZERO:
			continue
		caja = c if primera else caja.merge(c)
		primera = false
	return caja
