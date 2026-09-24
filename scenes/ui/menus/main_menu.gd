extends Control
## Menú principal (CA-01): nueva partida, continuar, opciones, créditos y salir.
## Diseño: "Detective Caneloso – Inicio" (Claude Design). Colores, tamaños y estilos
## están en la escena; aquí solo se cargan las fuentes y la animación de foco.

const FONTS_DIR := "res://assets/fonts/"
## Desplazamiento (px) de la opción seleccionada.
const FOCUS_SLIDE := 10
const DISABLED_ALPHA := 0.35

@onready var _main_screen: Control = %MainScreen
@onready var _option_list: Control = %OptionList
@onready var _new_game_button: Button = %NewGameButton
@onready var _continue_button: Button = %ContinueButton
@onready var _options_button: Button = %OptionsButton
@onready var _credits_button: Button = %CreditsButton
@onready var _quit_button: Button = %QuitButton
@onready var _options_menu: MenuPanel = %OptionsMenu
@onready var _credits_screen: MenuPanel = %CreditsScreen
@onready var _confirm_dialog: ConfirmDialog = %ConfirmDialog

## Botón que abrió el panel actual, para devolverle el foco al cerrarlo.
var _return_focus: Control


func _ready() -> void:
	_apply_fonts()
	var buttons: Array[Button] = [
		_new_game_button, _continue_button, _options_button, _credits_button, _quit_button
	]
	for button in buttons:
		var row := _row_of(button)
		button.focus_entered.connect(_slide.bind(row, FOCUS_SLIDE))
		button.focus_exited.connect(_slide.bind(row, 0))

	_new_game_button.pressed.connect(GameState.new_game)
	_continue_button.pressed.connect(GameState.continue_game)
	_options_button.pressed.connect(_open_panel.bind(_options_menu, _options_button))
	_credits_button.pressed.connect(_open_panel.bind(_credits_screen, _credits_button))
	_quit_button.pressed.connect(_on_quit_pressed)
	for panel: MenuPanel in [_options_menu, _credits_screen]:
		panel.closed.connect(_on_panel_closed)

	var can_continue := GameState.has_save()
	_continue_button.disabled = not can_continue
	_continue_button.focus_mode = Control.FOCUS_ALL if can_continue else Control.FOCUS_NONE
	_continue_button.mouse_default_cursor_shape = (
		Control.CURSOR_POINTING_HAND if can_continue else Control.CURSOR_ARROW
	)
	_row_of(_continue_button).modulate.a = 1.0 if can_continue else DISABLED_ALPHA
	UIUtils.connect_hover_focus(self)
	UIUtils.chain_focus(buttons)
	(_continue_button if can_continue else _new_game_button).grab_focus()


## Carga las fuentes del diseño; si faltan en assets/fonts/, usa la de Godot.
## Big Shoulders Display es una fuente variable: el grosor se elige con "wght".
func _apply_fonts() -> void:
	var display := "BigShouldersDisplay-Variable.ttf"
	%Kicker.add_theme_font_override("font", _font(display, 16, 500))  # Medium
	%Title.add_theme_font_override("font", _font(display, 2, 900))  # Black
	_option_list.theme.set_font("font", "Button", _font(display, 3, 800))  # ExtraBold
	_option_list.theme.set_font("font", "Label", _font("IBMPlexMono-Regular.ttf", 1))


func _font(file: String, glyph_spacing: int, weight := 0) -> Font:
	var path := FONTS_DIR + file
	var font := FontVariation.new()
	font.base_font = load(path) if ResourceLoader.exists(path) else ThemeDB.fallback_font
	font.spacing_glyph = glyph_spacing
	if weight > 0:
		font.variation_opentype = {TextServerManager.get_primary_interface().name_to_tag("wght"): weight}
	return font


## Fila (MarginContainer) que contiene al botón junto a su número.
func _row_of(button: Button) -> MarginContainer:
	return button.get_parent().get_parent()


func _slide(row: MarginContainer, x: int) -> void:
	row.create_tween().tween_property(row, "theme_override_constants/margin_left", x, 0.18) \
			.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_OUT)


func _open_panel(panel: MenuPanel, opened_from: Control) -> void:
	_return_focus = opened_from
	_main_screen.hide()
	panel.open()


func _on_panel_closed() -> void:
	_main_screen.show()
	_return_focus.grab_focus()


func _on_quit_pressed() -> void:
	if await _confirm_dialog.ask("¿Seguro que quieres salir del juego?", "Salir"):
		get_tree().quit()
