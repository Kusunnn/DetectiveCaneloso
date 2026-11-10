extends SceneTree
## Imprime dónde están las zonas, los NPC y las pistas del Campamento y del Lago (para ubicar a los personajes).
## Uso: --headless --path game --script res://Herramientas/Caso1/posiciones_caso1.gd

func _initialize() -> void:
	for ruta in ["res://Escenas/Caso1/escenario_campamento.tscn", "res://Escenas/Caso1/Lago/escenario_lago.tscn"]:
		var escena: Node = load(ruta).instantiate()
		for nodo in escena.find_children("*", "Node3D", true, false):
			if not (nodo is Node3D): continue
			var tipo := ""
			if nodo is Area3D: tipo = "ZONA"
			elif nodo.get_parent() != null and nodo.get_parent().name == "Personajes": tipo = "NPC"
			elif nodo.has_method("Interactuar") and str(nodo.get("Id")) != "" and nodo.get("Id") != null: tipo = "PISTA " + str(nodo.get("Id"))
			elif nodo is Marker3D: tipo = "MARCA"
			if tipo == "": continue
			var t: Transform3D = Transform3D.IDENTITY
			var n: Node = nodo
			while n != null and n != escena:
				if n is Node3D: t = n.transform * t
				n = n.get_parent()
			print("%s | %s | %s | %.1f, %.1f, %.1f" % [ruta.get_file(), tipo, nodo.name, t.origin.x, t.origin.y, t.origin.z])
		escena.free()
	quit()
