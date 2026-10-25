extends SceneTree
## Exporta a glTF la Sala 1 (paredes, piso, techo, mesa, sillas, lámpara, grabadora, reloj, rejilla, puerta)
## en sus posiciones reales, para animar la cinemática de la cinta en Blender.
## Queda en Assets/Cinematicas/Blender (con .gdignore: Godot no la importa).
## Uso: --headless --path game --script res://Herramientas/exportar_sala_blender.gd -- <salida.glb>

const NODOS := ["CSGBox3D", "Sala", "MesaPista", "SillaDetective", "SillaSospechoso", "Lampara", "GrabadoraPista", "RelojPista", "Detalles", "PlumaSecreta", "Zocalos", "AmbienteSala"]

func _initialize() -> void:
	var salida: String = OS.get_cmdline_user_args()[0]
	var sala: Node3D = load("res://Escenas/Interrogatorio/sala_interrogatorio.tscn").instantiate()
	root.add_child(sala)
	await process_frame
	await process_frame
	var raiz := Node3D.new()
	raiz.name = "Sala1"
	for nombre in NODOS:
		var n: Node3D = sala.get_node_or_null(nombre)
		if n == null: continue
		var copia: Node3D
		if n is CSGShape3D:
			copia = MeshInstance3D.new()
			copia.mesh = n.bake_static_mesh()
		else:
			copia = Node3D.new()
		copia.name = nombre
		raiz.add_child(copia)
		copia.global_transform = n.global_transform
		_copiar(n, copia)
	root.add_child(raiz)
	var doc := GLTFDocument.new()
	var estado := GLTFState.new()
	var err := doc.append_from_scene(raiz, estado)
	if err == OK: err = doc.write_to_filesystem(estado, salida)
	print("exportado: ", salida, " error=", err)
	quit()

# Copia solo lo que se ve: mallas (las CSG se hornean) con su material, conservando la jerarquía
func _copiar(origen: Node, destino: Node3D) -> void:
	for hijo in origen.get_children():
		if not (hijo is Node3D): continue
		var nuevo: Node3D
		if hijo is CSGShape3D and hijo.is_root_shape():
			var malla: ArrayMesh = hijo.bake_static_mesh()
			nuevo = MeshInstance3D.new()
			nuevo.mesh = malla
		elif hijo is CSGShape3D:
			continue
		elif hijo is MeshInstance3D and hijo.mesh != null:
			nuevo = MeshInstance3D.new()
			nuevo.mesh = hijo.mesh
			if hijo.material_override != null:
				for s in hijo.mesh.get_surface_count(): nuevo.set_surface_override_material(s, hijo.material_override)
		else:
			nuevo = Node3D.new()
		nuevo.name = hijo.name
		destino.add_child(nuevo)
		nuevo.transform = hijo.transform
		nuevo.visible = true
		_copiar(hijo, nuevo)
