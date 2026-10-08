extends SceneTree
## La misma auditoría desde la consola (sin abrir el editor):
##   <Godot>_console.exe --headless --path game --script res://Herramientas/auditar.gd -- [escena] [salida.md]

func _init() -> void:
	await process_frame
	var args := OS.get_cmdline_user_args()
	var ruta := args[0] if args.size() > 0 else "res://Escenas/Caso1/escenario_campamento.tscn"
	var esc: Node3D = load(ruta).instantiate()
	# Sin scripts de juego que muevan cosas al empezar: solo se mira la escena como quedó guardada
	root.add_child(esc)
	current_scene = esc
	for n in esc.find_children("*", "", true, false):
		n.set_process(false)
	var auditor := AuditorEscenario.new()
	var hallazgos := await auditor.auditar(esc)
	var texto := AuditorEscenario.reporte(hallazgos, ruta)
	if args.size() > 1:
		var f := FileAccess.open(args[1], FileAccess.WRITE)
		f.store_string(texto)
		f.close()
	print(texto.get_slice("\n## ", 0))
	quit()
