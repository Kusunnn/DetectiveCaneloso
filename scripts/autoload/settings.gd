extends Node
## Preferencias del jugador, guardadas en user://settings.cfg.

const PATH := "user://settings.cfg"

var master_volume := 1.0:
	set(value):
		master_volume = clampf(value, 0.0, 1.0)
		AudioServer.set_bus_volume_db(0, linear_to_db(master_volume))

var fullscreen := false:
	set(value):
		fullscreen = value
		DisplayServer.window_set_mode(
			DisplayServer.WINDOW_MODE_FULLSCREEN if value else DisplayServer.WINDOW_MODE_WINDOWED
		)


func _ready() -> void:
	var config := ConfigFile.new()
	if config.load(PATH) == OK:
		master_volume = config.get_value("audio", "master_volume", master_volume)
		fullscreen = config.get_value("video", "fullscreen", fullscreen)


func save_settings() -> void:
	var config := ConfigFile.new()
	config.set_value("audio", "master_volume", master_volume)
	config.set_value("video", "fullscreen", fullscreen)
	config.save(PATH)
