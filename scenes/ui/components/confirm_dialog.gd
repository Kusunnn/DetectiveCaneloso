class_name ConfirmDialog
extends Control
## Diálogo de confirmación reutilizable para acciones destructivas (CA-03).
## Uso:  if await confirm_dialog.ask("¿Salir sin guardar?", "Salir"): ...
## El foco empieza en "Cancelar" y Esc también cancela, para que lo seguro sea lo fácil.

signal _answered(accepted: bool)

@onready var _message: Label = %Message
@onready var _confirm_button: Button = %ConfirmButton
@onready var _cancel_button: Button = %CancelButton

var _previous_focus: Control


func _ready() -> void:
	hide()
	_confirm_button.pressed.connect(_answer.bind(true))
	_cancel_button.pressed.connect(_answer.bind(false))
	_trap_focus()
	UIUtils.connect_hover_focus(self)


func ask(message: String, confirm_text := "Sí", cancel_text := "Cancelar") -> bool:
	_message.text = message
	_confirm_button.text = confirm_text
	_cancel_button.text = cancel_text
	_previous_focus = get_viewport().gui_get_focus_owner()
	show()
	_cancel_button.grab_focus()
	return await _answered


func _answer(accepted: bool) -> void:
	hide()
	if is_instance_valid(_previous_focus) and _previous_focus.is_visible_in_tree():
		_previous_focus.grab_focus()
	_answered.emit(accepted)


func _unhandled_input(event: InputEvent) -> void:
	if visible and event.is_action_pressed("ui_cancel"):
		get_viewport().set_input_as_handled()
		_answer(false)


## Evita que el teclado salte a los botones del menú que queda detrás.
func _trap_focus() -> void:
	var cancel_path := _cancel_button.get_path()
	var confirm_path := _confirm_button.get_path()
	for button: Button in [_cancel_button, _confirm_button]:
		button.focus_neighbor_top = button.get_path()
		button.focus_neighbor_bottom = button.get_path()
	_cancel_button.focus_neighbor_left = cancel_path
	_cancel_button.focus_neighbor_right = confirm_path
	_confirm_button.focus_neighbor_left = cancel_path
	_confirm_button.focus_neighbor_right = confirm_path
	_cancel_button.focus_next = confirm_path
	_cancel_button.focus_previous = confirm_path
	_confirm_button.focus_next = cancel_path
	_confirm_button.focus_previous = cancel_path
