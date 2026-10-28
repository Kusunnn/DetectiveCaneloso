extends SceneTree
## Reproduce una secuencia de imágenes (una por cuadro, a 30 fps) con un audio opcional, para grabarla con
## Movie Maker. Sirve para armar la comparación lado a lado de las dos versiones de la cinemática de la cinta.
## Uso: --path game --write-movie <salida.avi> --fixed-fps 30 --script res://Herramientas/reproducir_secuencia.gd
##      -- <carpeta con las imágenes> [audio.wav]

var _archivos: PackedStringArray
var _carpeta: String
var _vista: TextureRect
var _i := 0

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	_carpeta = args[0]
	var lista := Array(DirAccess.get_files_at(_carpeta)).filter(func(f: String): return f.ends_with(".jpg") or f.ends_with(".png"))
	lista.sort()
	_archivos = PackedStringArray(lista)
	_vista = TextureRect.new()
	_vista.set_anchors_preset(Control.PRESET_FULL_RECT)
	_vista.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	_vista.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	root.add_child.call_deferred(_vista)
	if args.size() > 1:
		var audio := AudioStreamPlayer.new()
		audio.stream = AudioStreamWAV.load_from_file(args[1])
		audio.autoplay = true
		root.add_child.call_deferred(audio)

func _process(_delta: float) -> bool:
	if _i >= _archivos.size():
		return true
	var imagen := Image.load_from_file(_carpeta.path_join(_archivos[_i]))
	_vista.texture = ImageTexture.create_from_image(imagen)
	_i += 1
	return false
