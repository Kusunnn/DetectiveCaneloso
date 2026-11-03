extends SceneTree
## Juega la resolución del Caso 0 como en una partida (rejugando, sin tocar las ranuras) y revisa la
## cinemática de la cinta: entra después de la frase del comisario, no sale ninguna ayuda ni pensamiento
## encima, y al final el objetivo es «Caso cerrado... por ahora. ¿Quién es «La Garra»?».
## Necesita ventana. Uso: --path game --script res://Herramientas/flujo_real_cinta.gd
## Para grabarla: añadir --write-movie <salida.avi> --fixed-fps 30 --resolution 1920x1080
## Respalda en disco (user://respaldo_pruebas/) las partidas y el progreso y los devuelve al final.

const PARTIDAS := "user://partidas/"
const ARCHIVOS := ["user://progreso.json", "user://progreso.json.bak", "user://cinematicas.cfg"]
const RESPALDO := "user://respaldo_pruebas/"
const OBJETIVO := "Caso cerrado... por ahora. ¿Quién es «La Garra»?"
const RESPUESTAS := ["Porky, el agente de guardia", "grabado dormido", "trampa"]

var _fallos := 0

func _initialize() -> void:
	if DirAccess.dir_exists_absolute(RESPALDO):
		_devolver_respaldo()
	_respaldar()
	await process_frame
	var gestor = root.get_node("GestorPartida")
	gestor.IniciarCapitulo("caso0", true, true)
	await create_timer(4.0).timeout
	var sala: Node3D = current_scene
	var caso = sala.get_node("Caso0")
	var dialogo = sala.get_node("VentanaDialogo")
	await _cerrar_dialogos(dialogo, 6.0)

	var flujo = load("res://tests/FlujoRealCaso0Prueba.cs").new()
	root.add_child(flujo)
	flujo.Resolver(caso)
	var texto: Label = sala.get_node("CanvasLayer/TextoTutorial")
	var vio_cinematica := false
	var encima := 0
	var limite := 0
	var dialogo_al_entrar := true
	while not vio_cinematica or flujo.CinematicaEnCurso:
		limite += 1
		if limite > 30 * 120: break
		if flujo.CinematicaEnCurso:
			if not vio_cinematica:
				dialogo_al_entrar = dialogo.Abierto
			vio_cinematica = true
			# Nada del juego encima: ni pensamientos, ni la guía, ni el HUD
			if texto.is_visible_in_tree() or dialogo.Abierto or sala.get_node("HUD").visible: encima += 1
			await process_frame
			continue
		await _responder(dialogo)
		await create_timer(0.25).timeout
	_revisar(vio_cinematica, "la cinemática sale después de la frase del comisario")
	_revisar(not dialogo_al_entrar, "el diálogo se cierra antes de la cinemática")
	_revisar(encima == 0, "no aparece nada encima de la cinemática (%d cuadros)" % encima)
	await create_timer(1.0).timeout
	# El pensamiento de la pluma y el objetivo final
	for i in 10:
		await _responder(dialogo)
		await create_timer(0.4).timeout
	_revisar(sala.get_node("HUD").get("objetivo") == OBJETIVO, "el objetivo final es «%s»" % OBJETIVO)
	await create_timer(3.0).timeout
	print("RESULTADO: ", "OK" if _fallos == 0 else "%d FALLOS" % _fallos)
	_devolver_respaldo()
	quit(_fallos)

func _responder(dialogo) -> void:
	if not dialogo.Abierto: return
	var botones: Array = dialogo.find_children("*", "Button", true, false)
	for b in botones:
		if not b.is_visible_in_tree(): continue
		for r in RESPUESTAS:
			if r in b.text:
				b.emit_signal("pressed")
				return
	var clic := InputEventMouseButton.new()
	clic.button_index = MOUSE_BUTTON_LEFT
	clic.pressed = true
	Input.parse_input_event(clic)

func _cerrar_dialogos(dialogo, segundos: float) -> void:
	var fin := Time.get_ticks_msec() + segundos * 1000
	while Time.get_ticks_msec() < fin:
		await _responder(dialogo)
		await create_timer(0.4).timeout

func _revisar(ok: bool, que: String) -> void:
	if not ok: _fallos += 1
	print("OK    " if ok else "FALLA ", que)

## Copia en disco las partidas y el progreso antes de tocarlos
func _respaldar() -> void:
	DirAccess.make_dir_recursive_absolute(RESPALDO + "partidas")
	if DirAccess.dir_exists_absolute(PARTIDAS):
		for archivo in DirAccess.get_files_at(PARTIDAS):
			DirAccess.copy_absolute(PARTIDAS + archivo, RESPALDO + "partidas/" + archivo)
	for archivo in ARCHIVOS:
		if FileAccess.file_exists(archivo):
			DirAccess.copy_absolute(archivo, RESPALDO + archivo.get_file())

## Deja todo como estaba antes de la prueba y borra el respaldo
func _devolver_respaldo() -> void:
	if DirAccess.dir_exists_absolute(PARTIDAS):
		for archivo in DirAccess.get_files_at(PARTIDAS):
			DirAccess.remove_absolute(PARTIDAS + archivo)
	for archivo in ARCHIVOS:
		DirAccess.remove_absolute(archivo)
	DirAccess.make_dir_recursive_absolute(PARTIDAS)
	for archivo in DirAccess.get_files_at(RESPALDO + "partidas"):
		DirAccess.copy_absolute(RESPALDO + "partidas/" + archivo, PARTIDAS + archivo)
		DirAccess.remove_absolute(RESPALDO + "partidas/" + archivo)
	for archivo in DirAccess.get_files_at(RESPALDO):
		DirAccess.copy_absolute(RESPALDO + archivo, "user://" + archivo)
		DirAccess.remove_absolute(RESPALDO + archivo)
	DirAccess.remove_absolute(RESPALDO + "partidas")
	DirAccess.remove_absolute(RESPALDO)
