class_name MenuPanel
extends Control
## Base para pantallas secundarias de menú (opciones, créditos, códice...).
## Se abre con open(), se cierra con el botón %BackButton o con Esc (ui_cancel).

signal closed

## Control que recibe el foco al abrir, para poder navegar con teclado.
@export var default_focus: Control


func _ready() -> void:
	hide()
	var back_button := get_node_or_null(^"%BackButton") as BaseButton
	if back_button:
		back_button.pressed.connect(close)
	UIUtils.connect_hover_focus(self)
	UIUtils.chain_focus(find_children("*", "Control", true, false).filter(
		func(control: Control) -> bool:
			return control is BaseButton or control is Range or control is RichTextLabel
	))


func open() -> void:
	show()
	if default_focus:
		default_focus.grab_focus()


func close() -> void:
	hide()
	closed.emit()


func _unhandled_input(event: InputEvent) -> void:
	if visible and event.is_action_pressed("ui_cancel"):
		get_viewport().set_input_as_handled()
		close()
