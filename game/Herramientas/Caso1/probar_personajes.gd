extends SceneTree
## Revisa, sin ventana, que cada implicado quede en su lugar con su objeto: de qué tamaño quedó el objeto, a qué
## altura (en la mano, cerca de la cadera o el pecho), qué tan lejos del cuerpo, y que el personaje no esté metido
## en otra cosa. Campamento.
## Uso: --headless --path game --script res://Herramientas/Caso1/probar_personajes.gd
## La escena autoguarda al cargar: respalda en disco las ranuras, el progreso y la configuración, y los devuelve al final.

const PARTIDAS := "user://partidas/"
const ARCHIVOS := ["user://progreso.json", "user://progreso.json.bak", "user://cinematicas.cfg", "user://configuracion.cfg"]
const RESPALDO := "user://respaldo_pruebas/"

var _fallos := 0

func _initialize() -> void:
	if DirAccess.dir_exists_absolute(RESPALDO): _devolver_respaldo()
	_respaldar()
	root.get_node("GestorPartida").set_process(false)
	for datos in [["res://Escenas/Caso1/escenario_campamento.tscn", ["JuanCarlos", "Aya", "Miaurio", "Miami", "Tony", "Eriz"]]]:
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
	_devolver_respaldo()
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

func _respaldar() -> void:
	DirAccess.make_dir_recursive_absolute(RESPALDO + "partidas/")
	if DirAccess.dir_exists_absolute(PARTIDAS):
		for f in DirAccess.get_files_at(PARTIDAS): DirAccess.copy_absolute(PARTIDAS + f, RESPALDO + "partidas/" + f)
	for archivo in ARCHIVOS:
		if FileAccess.file_exists(archivo): DirAccess.copy_absolute(archivo, RESPALDO + archivo.get_file())
	# La marca va al final y dice qué archivos no existían: solo esos se borran al devolver. Sin marca no se toca nada.
	var marca := FileAccess.open(RESPALDO + "completo", FileAccess.WRITE)
	if marca: marca.store_string("\n".join(ARCHIVOS.filter(func(a): return not FileAccess.file_exists(a))))

## Deja las ranuras y el progreso como estaban antes de la prueba y borra el respaldo (carpetas incluidas)
func _devolver_respaldo() -> void:
	if not FileAccess.file_exists(RESPALDO + "completo"): return
	var faltaban := FileAccess.get_file_as_string(RESPALDO + "completo").split("\n")
	if DirAccess.dir_exists_absolute(PARTIDAS):
		for f in DirAccess.get_files_at(PARTIDAS): DirAccess.remove_absolute(PARTIDAS + f)
	else:
		DirAccess.make_dir_recursive_absolute(PARTIDAS)
	for f in DirAccess.get_files_at(RESPALDO + "partidas/"): DirAccess.copy_absolute(RESPALDO + "partidas/" + f, PARTIDAS + f)
	for archivo in ARCHIVOS:
		if FileAccess.file_exists(RESPALDO + archivo.get_file()): DirAccess.copy_absolute(RESPALDO + archivo.get_file(), archivo)
		elif FileAccess.file_exists(archivo) and archivo in faltaban: DirAccess.remove_absolute(archivo)
	for f in DirAccess.get_files_at(RESPALDO + "partidas/"): DirAccess.remove_absolute(RESPALDO + "partidas/" + f)
	DirAccess.remove_absolute(RESPALDO + "partidas")
	for f in DirAccess.get_files_at(RESPALDO): DirAccess.remove_absolute(RESPALDO + f)
	DirAccess.remove_absolute(RESPALDO)
