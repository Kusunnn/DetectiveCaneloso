extends SceneTree
## Exporta a glTF un modelo del juego tal como lo importa Godot (malla, esqueleto y materiales), para posarlo en
## Blender. Se usa para Caneloso, cuyo .fbx de Mixamo entra a Blender con escalas rotas.
## Uso: --headless --path game --script res://Herramientas/exportar_modelo_blender.gd -- <res://escena> <salida.glb>

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	var modelo: Node3D = load(args[0]).instantiate()
	root.add_child(modelo)
	await process_frame
	# Sin animación: el esqueleto queda en su pose de reposo
	for ap in modelo.find_children("*", "AnimationPlayer", true, false):
		(ap as AnimationPlayer).stop()
		(ap as AnimationPlayer).queue_free()
	await process_frame
	var doc := GLTFDocument.new()
	var estado := GLTFState.new()
	var err := doc.append_from_scene(modelo, estado)
	if err == OK: err = doc.write_to_filesystem(estado, args[1])
	print("exportado: ", args[1], " error=", err)
	quit()
