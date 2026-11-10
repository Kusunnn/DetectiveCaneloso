extends SceneTree
## Captura a cada implicado en su lugar del Campamento y del Lago, con su objeto (con ventana).
## Uso: --path game --script res://Herramientas/Caso1/capturas_personajes.gd -- <carpeta de salida>
## Respalda en disco (user://respaldo_pruebas/) las ranuras, el progreso y la configuración, y los devuelve al final.

const PARTIDAS := "user://partidas/"
const ARCHIVOS := ["user://progreso.json", "user://progreso.json.bak", "user://cinematicas.cfg", "user://configuracion.cfg"]
const RESPALDO := "user://respaldo_pruebas/"
const CAMPAMENTO := "res://Escenas/Caso1/escenario_campamento.tscn"
const LAGO := "res://Escenas/Caso1/Lago/escenario_lago.tscn"

var _salida := "user://capturas_personajes/"

func _initialize() -> void:
	if DirAccess.dir_exists_absolute(RESPALDO): _devolver_respaldo()
	_respaldar()
	var args := OS.get_cmdline_user_args()
	if args.size() > 0: _salida = args[0].trim_suffix("/") + "/"
	DirAccess.make_dir_recursive_absolute(_salida)
	await process_frame
	var gestor = root.get_node("GestorPartida")
	gestor.NuevaPartida(true)
	await _esperar_escena(CAMPAMENTO)
	await create_timer(2.0).timeout
	await _saltar_intro()
	await _cerrar_dialogos()
	for nombre in ["JuanCarlos", "Aya", "Miaurio", "Miami", "Tony", "Eriz"]:
		await _foto("campamento_" + nombre, current_scene.get_node("Personajes/" + nombre))
	gestor.CambiarEscenaDelCapitulo(LAGO, "EntradaDesdeCampamento")
	await _esperar_escena(LAGO)
	await create_timer(2.0).timeout
	await _cerrar_dialogos()
	await create_timer(1.0).timeout
	for nombre in ["Eriz", "Aya"]:
		var npc = current_scene.get_node_or_null("Personajes/" + nombre)
		if npc == null:
			for n in root.find_children(nombre, "Node3D", true, false):
				if n.get_parent().name == "Personajes": npc = n
		if npc: await _foto("lago_" + nombre, npc)
		else: print("no encontré a ", nombre, " en el Lago")
	print("Capturas en ", ProjectSettings.globalize_path(_salida))
	_devolver_respaldo()
	quit()

func _foto(nombre: String, npc: Node3D) -> void:
	var jugador: Node3D = current_scene.get_node("Jugador")
	var frente: Vector3 = npc.global_transform.basis.z.normalized()
	jugador.global_position = npc.global_position + frente * 2.4
	jugador.look_at(Vector3(npc.global_position.x, jugador.global_position.y, npc.global_position.z))
	await _cerrar_dialogos()
	await create_timer(0.8).timeout
	await RenderingServer.frame_post_draw
	root.get_texture().get_image().save_png(_salida + nombre + ".png")
	print(nombre, " en ", npc.global_position, " · objeto: ", npc.find_child("Objeto", true, false) != null)

func _esperar_escena(ruta: String) -> void:
	for i in 600:
		await process_frame
		if current_scene != null and current_scene.scene_file_path == ruta: return

func _cerrar_dialogos() -> void:
	var dialogo = current_scene.get_node_or_null("VentanaDialogo")
	var quieto := 0
	for i in 600:
		if dialogo == null or (not dialogo.Abierto and not paused):
			quieto += 1
			if quieto > 30: return
		else:
			quieto = 0
			dialogo.PruebasAvanzar()
		await create_timer(0.05).timeout

func _saltar_intro() -> void:
	for vez in 3:
		var intro: Node = null
		for hijo in root.get_children():
			if hijo.get_script() and str(hijo.get_script().resource_path).ends_with("IntroCaso.cs") and not hijo.is_queued_for_deletion(): intro = hijo
		if intro == null: return
		var e := InputEventKey.new()
		e.keycode = KEY_SPACE
		e.physical_keycode = KEY_SPACE
		e.pressed = true
		Input.parse_input_event(e)
		await create_timer(1.8).timeout
		var s := e.duplicate()
		s.pressed = false
		Input.parse_input_event(s)
		await create_timer(1.5).timeout

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
