extends SceneTree
## Revisa, sin ventana, que cada implicado quede en su lugar con su objeto: de qué tamaño quedó el objeto, a qué
## altura (en la mano, cerca de la cadera o el pecho), qué tan lejos del cuerpo, y que el personaje no esté metido
## en otra cosa. Campamento y Lago.
## Uso: --headless --path game --script res://Herramientas/Caso1/probar_personajes.gd

var _fallos := 0

func _initialize() -> void:
	for datos in [["res://Escenas/Caso1/escenario_campamento.tscn", ["JuanCarlos", "Aya", "Miaurio", "Miami", "Tony", "Eriz"]],
			["res://Escenas/Caso1/Lago/escenario_lago.tscn", ["Eriz", "Aya"]]]:
		var escena: Node = load(datos[0]).instantiate()
		root.add_child(escena)
		for i in 20: await physics_frame
		await create_timer(0.5).timeout
		print("== ", datos[0].get_file())
		for nombre in datos[1]:
			var npc: Node3D = escena.get_node_or_null("Personajes/" + nombre)
			if npc == null:
				_revisar(false, "%s está en la escena" % nombre)
				continue
			var objeto: Node3D = npc.find_child("Objeto", true, false)
			_revisar(objeto != null, "%s tiene su objeto" % nombre)
			if objeto == null: continue
			var caja := _caja(objeto)
			var centro := caja.get_center()
			var altura := centro.y - npc.global_position.y
			var lejos := Vector2(centro.x - npc.global_position.x, centro.z - npc.global_position.z).length()
			var mayor := maxf(caja.size.x, maxf(caja.size.y, caja.size.z))
			print("    %s en %s · objeto de %.2f m, a %.2f m de alto y %.2f m del cuerpo" % [nombre, npc.global_position, mayor, altura, lejos])
			_revisar(mayor > 0.05 and mayor < 2.0, "%s: el objeto tiene un tamaño creíble" % nombre)
			_revisar(altura > -0.2 and altura < 1.8 and lejos < 1.2, "%s: el objeto está junto a su cuerpo" % nombre)
			_revisar(not _metido(npc), "%s no está metido en una pared o un mueble" % nombre)
		escena.queue_free()
		await process_frame
	print("Fallos: %d" % _fallos)
	quit(1 if _fallos > 0 else 0)

func _caja(nodo: Node3D) -> AABB:
	var caja := AABB()
	var primera := true
	for m in nodo.find_children("*", "MeshInstance3D", true, false) + ([nodo] if nodo is MeshInstance3D else []):
		var c: AABB = m.global_transform * m.get_aabb()
		caja = c if primera else caja.merge(c)
		primera = false
	return caja

func _metido(npc: Node3D) -> bool:
	var espacio := npc.get_world_3d().direct_space_state
	var consulta := PhysicsShapeQueryParameters3D.new()
	var capsula := CapsuleShape3D.new()
	capsula.radius = 0.3
	capsula.height = 1.4
	consulta.shape = capsula
	consulta.collision_mask = 1
	var excluir: Array[RID] = []
	for c in npc.find_children("*", "CollisionObject3D", true, false): excluir.append(c.get_rid())
	consulta.exclude = excluir
	consulta.transform = Transform3D(Basis.IDENTITY, npc.global_position + Vector3.UP * 0.95)
	return espacio.intersect_shape(consulta, 1).size() > 0

func _revisar(ok: bool, texto: String) -> void:
	print("  %s %s" % ["✓" if ok else "✗", texto])
	if not ok: _fallos += 1
