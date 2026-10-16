extends SceneTree
## HU-07: imprime la caja de cada malla de una pista en coordenadas de la propia pista.
## Sirve para colocar los marcadores "Detalle_<id>" de la vista de examen.
## Uso: --headless --path game --script res://Herramientas/medir_pistas.gd -- <escena.tscn> <ruta del ObjetoPista>

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	if args.size() < 2:
		print("Uso: -- <escena.tscn> <ruta del ObjetoPista>")
		quit()
		return
	var escena: Node = load(args[0]).instantiate()
	root.add_child(escena)
	await process_frame
	await process_frame
	var pista: Node3D = escena.get_node(args[1])
	var inversa := pista.global_transform.affine_inverse()
	for nodo in pista.find_children("*", "VisualInstance3D", true, false):
		var visual := nodo as VisualInstance3D
		var caja := inversa * visual.global_transform * visual.get_aabb()
		print("%s  pos=%s  fin=%s" % [pista.get_path_to(visual), caja.position, caja.end])
	quit()
