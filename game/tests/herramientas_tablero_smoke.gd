extends SceneTree
# Prueba la colección con respaldo del perfil, sin revelar las ubicaciones.
const COLECCION := "user://coleccion_tablero.cfg"
var fallas := 0

func _initialize() -> void:
	call_deferred("probar")

func revisar(condicion: bool, mensaje: String) -> void:
	if not condicion:
		fallas += 1
		push_error(mensaje)

func probar() -> void:
	var existia := FileAccess.file_exists(COLECCION)
	var respaldo := FileAccess.get_file_as_bytes(COLECCION) if existia else PackedByteArray()
	if existia: DirAccess.remove_absolute(COLECCION)
	var escena := Node3D.new()
	root.add_child(escena)
	var hud = load("res://hud/hud.tscn").instantiate()
	escena.add_child(hud)
	var jugador = load("res://Escenas/Interrogatorio/jugador.tscn").instantiate()
	escena.add_child(jugador)
	jugador.set_physics_process(false)
	var gestor = load("res://Escenas/Investigacion/TableroDeduccion.cs").new()
	escena.add_child(gestor)
	await process_frame
	revisar(hud.relaciones_pizarron.herramientas() == ["mano", "hilo"], "Las herramientas deben empezar ocultas")
	for tipo in ["notas", "marcador"]:
		var recurso := "res://Escenas/Investigacion/%s_coleccionable.tscn" % tipo
		var objeto = load(recurso).instantiate()
		escena.add_child(objeto)
		revisar(objeto.visible and objeto.collision_layer == 8, "El coleccionable nuevo se puede recoger")
		objeto.Interactuar(jugador)
		revisar(not objeto.visible and objeto.collision_layer == 0, "Recoger oculta el objeto y su colisión")
		revisar(hud.relaciones_pizarron.desbloqueadas.has(tipo), "Recoger habilita la herramienta")
		var repetido = load(recurso).instantiate()
		escena.add_child(repetido)
		revisar(not repetido.visible and repetido.collision_layer == 0, "Una herramienta encontrada no reaparece")
	# Un HUD nuevo recibe la colección persistente al cambiar de caso.
	var otro := Node3D.new()
	root.add_child(otro)
	var hud_nuevo = load("res://hud/hud.tscn").instantiate()
	otro.add_child(hud_nuevo)
	otro.add_child(load("res://Escenas/Investigacion/TableroDeduccion.cs").new())
	await process_frame
	revisar(hud_nuevo.relaciones_pizarron.herramientas() == ["mano", "hilo", "nota", "negro"], "Las herramientas deben sobrevivir al cambio de caso")
	DirAccess.remove_absolute(COLECCION)
	for ruta in ["res://Escenas/Caso0/zona_recepcion.tscn", "res://Escenas/Caso1/cabana_director.tscn"]:
		var zona = load(ruta).instantiate()
		escena.add_child(zona)
		var encontrados := 0
		var secreto: Node3D
		for nodo in zona.find_children("*", "StaticBody3D", true, false):
			if nodo.get_script() and nodo.get_script().resource_path.ends_with("HerramientaTablero.cs"):
				encontrados += 1
				secreto = nodo
		revisar(encontrados == 1, "Cada caso debe contener su coleccionable opcional")
		if secreto:
			var contenedor := secreto.get_parent().get_parent()
			if contenedor.has_method("RestaurarEstado"): contenedor.RestaurarEstado(true, true)
			await physics_frame
			await physics_frame
			var alcanzable := false
			for direccion in [Vector3(0, 1, 0), Vector3(0.6, 0.6, 0.6), Vector3(-0.6, 0.6, -0.6), Vector3(-0.6, 0.6, 0.6), Vector3(0.6, 0.6, -0.6)]:
				var consulta := PhysicsRayQueryParameters3D.create(secreto.global_position + direccion, secreto.global_position, 59)
				var impacto: Dictionary = secreto.get_world_3d().direct_space_state.intersect_ray(consulta)
				if impacto.get("collider") == secreto: alcanzable = true
			revisar(alcanzable, "El coleccionable debe ser alcanzable con el rayo de interacción")
		zona.free()
	if existia:
		var archivo := FileAccess.open(COLECCION, FileAccess.WRITE)
		archivo.store_buffer(respaldo)
		archivo.close()
	else: DirAccess.remove_absolute(COLECCION)
	await create_timer(0.6).timeout
	escena.free()
	otro.free()
	await process_frame
	print("COLECCION_TABLERO: %s" % ("OK" if fallas == 0 else "%d fallas" % fallas))
	quit(0 if fallas == 0 else 1)
