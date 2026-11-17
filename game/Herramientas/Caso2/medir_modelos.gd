extends SceneTree
## Imprime el tamaño (AABB) de los modelos que se le pasen, para escalarlos respecto a Caneloso.
## Uso: --headless --path game --script res://Herramientas/Caso2/medir_modelos.gd -- <ruta o carpeta> [...]

func _initialize() -> void:
	for arg in OS.get_cmdline_user_args():
		var rutas: Array = []
		if arg.ends_with(".glb") or arg.ends_with(".gltf") or arg.ends_with(".tscn"):
			rutas.append(arg)
		else:
			for f in DirAccess.get_files_at(arg):
				if f.ends_with(".glb") or f.ends_with(".gltf"):
					rutas.append(arg.path_join(f))
		for r in rutas:
			var e: PackedScene = load(r)
			if e == null:
				continue
			var n: Node = e.instantiate()
			var caja := _caja(n, Transform3D.IDENTITY)
			print("%-40s tam=(%.2f, %.2f, %.2f) min=(%.2f, %.2f, %.2f)" % [r.get_file(), caja.size.x, caja.size.y, caja.size.z, caja.position.x, caja.position.y, caja.position.z])
			n.free()
	quit()

func _caja(n: Node, t: Transform3D) -> AABB:
	var caja := AABB()
	var hay := false
	var tt: Transform3D = t * ((n as Node3D).transform if n is Node3D else Transform3D.IDENTITY)
	if n is VisualInstance3D:
		caja = tt * (n as VisualInstance3D).get_aabb()
		hay = true
	for h in n.get_children():
		var b := _caja(h, tt)
		if b.size != Vector3.ZERO:
			caja = b if not hay else caja.merge(b)
			hay = true
	return caja
