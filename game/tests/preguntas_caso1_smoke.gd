extends SceneTree
# Godot .NET --headless --path game --script res://tests/preguntas_caso1_smoke.gd
func _initialize() -> void:
	var prueba = load("res://tests/PreguntasCaso1Prueba.cs").new()
	var correcto: bool = prueba.Ejecutar()
	quit(0 if correcto else 1)
