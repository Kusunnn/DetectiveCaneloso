extends Control
## Escena de juego PROVISIONAL, solo para probar el menú de pausa.
## Sustituir por el juego real (manteniendo una instancia de PauseMenu).


func _ready() -> void:
	%SaveButton.pressed.connect(GameState.save_game)
	%MarkDirtyButton.pressed.connect(func() -> void: GameState.has_unsaved_changes = true)
