extends Node
# Para ver la cinemática final del Caso 0 sin jugar el caso: abre ver_cinematica_cinta.tscn y dale F6.
# Al terminar se puede repetir con [R].

var sala: Node3D
var prueba

func _ready() -> void:
	sala = load("res://Escenas/Interrogatorio/sala_interrogatorio.tscn").instantiate()
	add_child(sala)
	for i in 40:
		await get_tree().process_frame
	ver()

func ver() -> void:
	# Fuera la tarjeta de título y los avisos del inicio del caso
	for n in get_tree().root.find_children("*", "CanvasLayer", true, false):
		if n.layer >= 100: n.visible = false
	if prueba != null: prueba.queue_free()
	prueba = load("res://tests/CinematicaCintaPrueba.cs").new()
	add_child(prueba)
	prueba.Empezar(sala, false)

func _unhandled_input(evento: InputEvent) -> void:
	if evento is InputEventKey and evento.pressed and evento.keycode == KEY_R and prueba != null and prueba.Terminada:
		ver()
