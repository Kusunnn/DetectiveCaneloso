extends MenuPanel
## Opciones del juego. Se usa tanto en el menú principal como en el de pausa.

@onready var _volume_slider: HSlider = %VolumeSlider
@onready var _fullscreen_check: CheckButton = %FullscreenCheck


func _ready() -> void:
	super()
	_volume_slider.value_changed.connect(func(value: float) -> void: Settings.master_volume = value)
	_fullscreen_check.toggled.connect(func(on: bool) -> void: Settings.fullscreen = on)


func open() -> void:
	_volume_slider.set_value_no_signal(Settings.master_volume)
	_fullscreen_check.set_pressed_no_signal(Settings.fullscreen)
	super()


func close() -> void:
	Settings.save_settings()
	super()
