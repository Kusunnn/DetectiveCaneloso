extends SceneTree
## Toma la foto del expediente del Caso 1: la orilla del lago de noche, con las canoas y el brillo azul.
## Necesita ventana. Uso: --path game --script res://Herramientas/foto_expediente.gd -- <salida.png> [toma]
## Sin toma, guarda todas como <salida>_<n>.png. Las ranuras del jugador se respaldan y se devuelven al final.

const ESCENA := "res://Escenas/Caso1/escenario_campamento.tscn"
const PARTIDAS := "user://partidas/"
# [posición de la cámara, punto al que mira, fov]
const TOMAS := [
	[Vector3(2.5, 1.6, -33.0), Vector3(7.5, -0.2, -54.0), 40.0],
	[Vector3(10.5, 1.4, -34.0), Vector3(6.5, -0.2, -55.0), 38.0],
	[Vector3(4.0, 3.0, -38.0), Vector3(7.5, -0.4, -56.0), 45.0],
	[Vector3(1.0, 0.9, -42.0), Vector3(8.0, 0.0, -54.0), 50.0],
]

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	var salida: String = args[0]
	var solo := int(args[1]) if args.size() > 1 else -1
	var respaldo := {}
	for archivo in DirAccess.get_files_at(PARTIDAS):
		respaldo[archivo] = FileAccess.get_file_as_bytes(PARTIDAS + archivo)
	DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
	DisplayServer.window_set_size(Vector2i(900, 900))
	root.size = Vector2i(900, 900)
	await process_frame

	var escena: Node = load(ESCENA).instantiate()
	root.add_child(escena)
	current_scene = escena
	await create_timer(1.5).timeout
	for capa in escena.find_children("*", "CanvasLayer", true, false):
		capa.visible = false
	escena.get_node("Jugador").visible = false
	# El letrero de «Próximamente» es del juego, no del lugar
	for n in ["SalidaSendero/Arco/PosteProximamente", "SalidaSendero/Arco/Proximamente"]:
		if escena.has_node(n):
			escena.get_node(n).visible = false
	var cam := Camera3D.new()
	escena.add_child(cam)
	cam.current = true
	for i in TOMAS.size():
		if solo >= 0 and i != solo:
			continue
		var t: Array = TOMAS[i]
		cam.fov = t[2]
		cam.look_at_from_position(t[0], t[1])
		await create_timer(0.8).timeout
		await RenderingServer.frame_post_draw
		var img := root.get_texture().get_image()
		var lado := mini(img.get_width(), img.get_height())
		img = img.get_region(Rect2i((img.get_width() - lado) / 2, (img.get_height() - lado) / 2, lado, lado))
		img.resize(900, 900, Image.INTERPOLATE_LANCZOS)
		img = _revelar(img)
		var ruta := salida if solo >= 0 else salida.get_basename() + "_%d.png" % i
		img.save_png(ruta)
		print("foto: ", ruta)

	for archivo in DirAccess.get_files_at(PARTIDAS):
		DirAccess.remove_absolute(PARTIDAS + archivo)
	for archivo in respaldo:
		var f := FileAccess.open(PARTIDAS + archivo, FileAccess.WRITE)
		f.store_buffer(respaldo[archivo])
	quit()

## Le da aspecto de foto de archivo: un poco de contraste, viñeta y grano.
func _revelar(img: Image) -> Image:
	img.convert(Image.FORMAT_RGB8)
	var w := img.get_width()
	var h := img.get_height()
	var azar := RandomNumberGenerator.new()
	azar.seed = 47
	for y in h:
		for x in w:
			var c := img.get_pixel(x, y)
			var d := Vector2(x - w * 0.5, y - h * 0.5).length() / (w * 0.7)
			var vineta := 1.0 - clampf(d * d, 0.0, 1.0) * 0.55
			var grano := azar.randf_range(-0.035, 0.035)
			img.set_pixel(x, y, Color(
				clampf(((c.r - 0.5) * 1.12 + 0.52) * vineta + grano, 0, 1),
				clampf(((c.g - 0.5) * 1.12 + 0.51) * vineta + grano, 0, 1),
				clampf(((c.b - 0.5) * 1.12 + 0.5) * vineta + grano, 0, 1)))
	return img
