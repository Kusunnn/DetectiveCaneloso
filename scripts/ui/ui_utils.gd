class_name UIUtils
## Utilidades compartidas por los menús.


## Hace que pasar el ratón por encima de un botón o slider le dé el foco, para que
## teclado y ratón compartan un único elemento resaltado (CA-04).
## Solo reacciona al movimiento real del ratón: si una pantalla aparece bajo un cursor
## quieto, no le roba el foco a quien está usando el teclado.
static func connect_hover_focus(root: Node) -> void:
	for control: Control in root.find_children("*", "Control", true, false):
		if not (control is BaseButton or control is Range) or control.has_meta(&"hover_focus"):
			continue
		control.set_meta(&"hover_focus", true)
		control.gui_input.connect(func(event: InputEvent) -> void:
			if (event is InputEventMouseMotion and not control.has_focus()
					and control.focus_mode != Control.FOCUS_NONE):
				control.grab_focus()
		)


## Encadena el foco de los controles en el orden dado (arriba/abajo y Tab), con vuelta
## al principio, para que el teclado no se escape a controles de otras pantallas (CA-04).
static func chain_focus(controls: Array) -> void:
	var focusable := controls.filter(
		func(control: Control) -> bool: return control.focus_mode != Control.FOCUS_NONE
	)
	for i in focusable.size():
		var control: Control = focusable[i]
		var previous_path := control.get_path_to(focusable[i - 1])
		var next_path := control.get_path_to(focusable[(i + 1) % focusable.size()])
		control.focus_neighbor_top = previous_path
		control.focus_neighbor_bottom = next_path
		control.focus_previous = previous_path
		control.focus_next = next_path
		control.focus_neighbor_left = ^"."
		control.focus_neighbor_right = ^"."
