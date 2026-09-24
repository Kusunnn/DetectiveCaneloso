extends Node
## Estado global de la partida y navegación entre escenas.
## El guardado es provisional: solo escribe un marcador para habilitar "Continuar".

const SAVE_PATH := "user://savegame.json"
const MAIN_MENU_SCENE := "res://scenes/ui/menus/main_menu.tscn"
const GAME_SCENE := "res://scenes/game/game.tscn"

## true si hay progreso que se perdería al salir sin guardar.
## El juego debe ponerlo a true cuando cambie algo que haya que guardar.
var has_unsaved_changes := false


func has_save() -> bool:
	return FileAccess.file_exists(SAVE_PATH)


func new_game() -> void:
	has_unsaved_changes = true
	_change_scene(GAME_SCENE)


func continue_game() -> void:
	# TODO: cargar los datos de SAVE_PATH cuando exista el sistema de guardado.
	has_unsaved_changes = false
	_change_scene(GAME_SCENE)


func save_game() -> void:
	# TODO: guardar el estado real de la partida.
	var file := FileAccess.open(SAVE_PATH, FileAccess.WRITE)
	file.store_string(JSON.stringify({"saved_at": Time.get_datetime_string_from_system()}))
	has_unsaved_changes = false


func go_to_main_menu() -> void:
	has_unsaved_changes = false
	_change_scene(MAIN_MENU_SCENE)


func _change_scene(path: String) -> void:
	get_tree().paused = false
	get_tree().change_scene_to_file(path)
