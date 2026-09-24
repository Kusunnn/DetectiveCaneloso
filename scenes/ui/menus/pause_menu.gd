extends CanvasLayer
## Menú de pausa (CA-02): reanudar, códice de reglas, opciones y volver al menú principal.
## Basta con instanciarlo en cualquier escena de juego. Se abre y cierra con la acción
## "pause" (Esc / Start) y sigue funcionando con el árbol pausado (process_mode = Always).

@onready var _root: Control = %Root
@onready var _main_screen: Control = %MainScreen
@onready var _resume_button: Button = %ResumeButton
@onready var _codex_button: Button = %CodexButton
@onready var _options_button: Button = %OptionsButton
@onready var _main_menu_button: Button = %MainMenuButton
@onready var _rules_codex: MenuPanel = %RulesCodex
@onready var _options_menu: MenuPanel = %OptionsMenu
@onready var _confirm_dialog: ConfirmDialog = %ConfirmDialog

## Botón que abrió el panel actual, para devolverle el foco al cerrarlo.
var _return_focus: Control


func _ready() -> void:
	_root.hide()
	_resume_button.pressed.connect(resume)
	_codex_button.pressed.connect(_open_panel.bind(_rules_codex, _codex_button))
	_options_button.pressed.connect(_open_panel.bind(_options_menu, _options_button))
	_main_menu_button.pressed.connect(_on_main_menu_pressed)
	for panel: MenuPanel in [_rules_codex, _options_menu]:
		panel.closed.connect(_on_panel_closed)
	UIUtils.connect_hover_focus(_root)
	UIUtils.chain_focus([_resume_button, _codex_button, _options_button, _main_menu_button])


# Los paneles y el diálogo de confirmación son hijos, así que reciben Esc antes
# que este nodo; aquí solo llega cuando está visible la lista principal.
func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("pause") or (_root.visible and event.is_action_pressed("ui_cancel")):
		get_viewport().set_input_as_handled()
		if _root.visible:
			resume()
		else:
			pause()


func pause() -> void:
	get_tree().paused = true
	_root.show()
	_main_screen.show()
	_resume_button.grab_focus()


func resume() -> void:
	_root.hide()
	get_tree().paused = false


func _open_panel(panel: MenuPanel, opened_from: Control) -> void:
	_return_focus = opened_from
	_main_screen.hide()
	panel.open()


func _on_panel_closed() -> void:
	_main_screen.show()
	_return_focus.grab_focus()


func _on_main_menu_pressed() -> void:
	if GameState.has_unsaved_changes:
		var confirmed := await _confirm_dialog.ask(
			"Tienes progreso sin guardar.\n¿Volver al menú principal sin guardar?",
			"Salir sin guardar"
		)
		if not confirmed:
			return
	GameState.go_to_main_menu()
