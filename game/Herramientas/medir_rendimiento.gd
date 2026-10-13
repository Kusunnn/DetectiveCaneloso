extends SceneTree
## Mide cuánto tarda en dibujarse una escena (CPU y GPU por cuadro), sin vsync. Necesita ventana.
## Uso: --path game --script res://Herramientas/medir_rendimiento.gd -- <escena.tscn>
func _initialize() -> void:
	DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
	DisplayServer.window_set_size(Vector2i(1600, 900))
	DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_DISABLED)
	var escena: Node = load(OS.get_cmdline_user_args()[0]).instantiate()
	root.add_child(escena)
	current_scene = escena
	var rid := root.get_viewport_rid()
	RenderingServer.viewport_set_measure_render_time(rid, true)
	await create_timer(4.0).timeout
	var cpu := 0.0
	var gpu := 0.0
	for i in 60:
		await process_frame
		cpu += RenderingServer.viewport_get_measured_render_time_cpu(rid)
		gpu += RenderingServer.viewport_get_measured_render_time_gpu(rid)
	print("%s  FPS=%d  cpu=%.2f ms  gpu=%.2f ms (promedio de 60 cuadros)" % [OS.get_cmdline_user_args()[0], Engine.get_frames_per_second(), cpu / 60.0, gpu / 60.0])
	quit()
