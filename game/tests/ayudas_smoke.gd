extends SceneTree
# Godot .NET --headless --path game --script res://tests/ayudas_smoke.gd
func _initialize() -> void:
	var prueba = load("res://tests/AyudasPrueba.cs").new()
	var correcto: bool = prueba.Ejecutar()
	prueba.free()
	quit(0 if correcto else 1)
