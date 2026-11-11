extends SceneTree
## HU-9.5 CA-03: video de evidencia para el tablero. La cinta se dispara, se guarda, se carga y no se repite;
## «Volver a ver» funciona desde el tablero y regresa a él; y los NPC con sus variantes de reposo. Lleva un letrero
## con lo que se está probando. Necesita ventana.
## Uso: --path game --write-movie <salida.avi> --fixed-fps 30 --resolution 1920x1080 --script res://Herramientas/Caso1/video_ca03.gd
## Para 1080p, un game/override.cfg temporal con el override de la ventana en 1920×1080.
## Respalda en disco (user://respaldo_pruebas/) las ranuras y el progreso, y los devuelve al final.

const PARTIDAS := "user://partidas/"
const ARCHIVOS := ["user://progreso.json", "user://progreso.json.bak", "user://cinematicas.cfg"]
const RESPALDO := "user://respaldo_pruebas/"
const CASO0 := "res://Escenas/Interrogatorio/sala_interrogatorio.tscn"
const MENU := "res://Escenas/Menus/menu_inicio.tscn"
const CAMPAMENTO := "res://Escenas/Caso1/escenario_campamento.tscn"
const RESPUESTAS := ["Porky, el agente de guardia", "grabado dormido", "trampa"]
const NPC := ["Miami", "Tony", "Eriz", "Aya", "JuanCarlos", "Miaurio"]
const SEGUNDOS_NPC := 7.0

var _gestor
var _flujo_real
var _flujo
var _letrero: Label
var _veces := 0

func _initialize() -> void:
	if DirAccess.dir_exists_absolute(RESPALDO):
		_devolver_respaldo()
	_respaldar()
	await process_frame
	_gestor = root.get_node("GestorPartida")
	_gestor.CinematicaReproducida.connect(func(id): if id == "caso0_cinta": _veces += 1)
	_flujo = load("res://tests/FlujoCaso1Prueba.cs").new()
	root.add_child(_flujo)
	_crear_letrero()

	_decir("Caso 0: se resuelve el caso y se dispara la cinta")
	_gestor.IniciarCapitulo("caso0", false, true)
	await _esperar_escena(CASO0)
	await _presentacion()
	_flujo_real = load("res://tests/FlujoRealCaso0Prueba.cs").new()
	root.add_child(_flujo_real)
	_flujo_real.Resolver(current_scene.get_node("Caso0"))
	await _hasta_la_cinta()
	_decir("La cinta se reproduce (veces: %d). Se adelanta para el video" % _veces)
	await create_timer(6.0).timeout
	for i in 3:
		await _clic()
		await create_timer(2.5).timeout
	# Hasta el final, cuadro por cuadro
	for i in 30:
		if not _flujo_real.CinematicaEnCurso: break
		await _clic()
		await create_timer(0.4).timeout
	_decir("Termina y el cierre del caso guarda la partida")
	await _cerrar_caso()
	await create_timer(2.0).timeout

	_decir("Se carga la partida guardada: la cinta NO vuelve a salir (veces: %d). Sigue el Caso 1" % _veces)
	_gestor.CargarRanura(0, false)
	await create_timer(7.0).timeout
	_decir("Empieza el Caso 1 con su presentación, por primera vez. La cinta sigue en %d" % _veces)
	await create_timer(5.0).timeout
	for i in 20:
		if _intros() == 0: break
		await _saltar_intro()
		await create_timer(0.3).timeout
	await create_timer(1.0).timeout

	_decir("Menú → Capítulos → Caso 0: «Volver a ver la cinta» (solo si ya se vio)")
	_gestor.IrAlMenuInicio()
	await _esperar_escena(MENU)
	var tablero
	for hijo in current_scene.get_children():
		if hijo.get_script() and str(hijo.get_script().resource_path).ends_with("TableroCapitulos.cs"): tablero = hijo
	tablero.Abrir(null)
	await create_timer(1.0).timeout
	_flujo.AbrirDetalle(tablero, "caso0")
	await create_timer(3.0).timeout
	var boton: Button
	for b in tablero.find_children("*", "Button", true, false):
		if b.text.contains("Volver a ver la cinta") and b.is_visible_in_tree(): boton = b
	boton.grab_focus()
	_decir("Este es el botón nuevo: «Volver a ver la cinta»")
	await create_timer(4.0).timeout
	boton.emit_signal("pressed")
	_decir("Volver a verla: no da pistas ni guarda (veces en la partida: %d)" % _veces)
	await create_timer(8.0).timeout
	for i in 3:
		await _clic()
		await create_timer(2.5).timeout
	while _ver_cinta():
		await _clic()
		await create_timer(0.4).timeout
	_decir("Regresa al mismo detalle del tablero; la partida no cambió (veces: %d)" % _veces)
	await create_timer(4.0).timeout

	_gestor.NuevaPartida(true)
	await _esperar_escena(CAMPAMENTO)
	var personajes: Node3D
	for i in 40:
		await _saltar_intro()
		if _intros() == 0: break
		await create_timer(0.3).timeout
	await create_timer(1.0).timeout
	var dialogo = current_scene.get_node("VentanaDialogo")
	for i in 60:
		if not dialogo.Abierto and i > 10: break
		if dialogo.Abierto: await _clic()
		await create_timer(0.3).timeout
	personajes = current_scene.get_node("Personajes")
	for capa in current_scene.find_children("*", "CanvasLayer", true, false):
		if capa.layer < 100: capa.visible = false
	var camara := Camera3D.new()
	current_scene.add_child(camara)
	var luz := SpotLight3D.new()
	luz.spot_range = 18.0
	luz.spot_angle = 30.0
	luz.light_energy = 5.0
	luz.light_color = Color(1, 0.93, 0.8)
	camara.add_child(luz)
	camara.make_current()
	for nombre in NPC:
		var npc: Node3D = personajes.get_node(nombre)
		_decir("NPC: %s con sus variantes de reposo (Mimita ya no está en pose T)" % ("Mimita" if nombre == "Miami" else nombre))
		var frente := npc.global_basis.z.normalized()
		camara.look_at_from_position(npc.global_position + frente * 2.6 + Vector3.UP * 1.2, npc.global_position + Vector3.UP * 0.9, Vector3.UP)
		await create_timer(SEGUNDOS_NPC).timeout
	_devolver_respaldo()
	quit()

func _crear_letrero() -> void:
	var capa := CanvasLayer.new()
	capa.layer = 200
	root.add_child(capa)
	var fondo := ColorRect.new()
	fondo.color = Color(0, 0, 0, 0.72)
	fondo.anchor_right = 1.0
	fondo.offset_top = 1000
	fondo.offset_bottom = 1080
	fondo.mouse_filter = Control.MOUSE_FILTER_IGNORE
	capa.add_child(fondo)
	_letrero = Label.new()
	_letrero.anchor_right = 1.0
	_letrero.offset_top = 1012
	_letrero.offset_bottom = 1070
	_letrero.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_letrero.add_theme_font_size_override("font_size", 34)
	_letrero.mouse_filter = Control.MOUSE_FILTER_IGNORE
	capa.add_child(_letrero)

func _decir(texto: String) -> void:
	_letrero.text = texto
	print(texto)

func _presentacion() -> void:
	var dialogo = current_scene.get_node("VentanaDialogo")
	for i in 60:
		if dialogo.Abierto: break
		await create_timer(0.5).timeout
	for i in 60:
		if not dialogo.Abierto: break
		await _clic()
		await create_timer(0.35).timeout
	await create_timer(0.5).timeout

func _hasta_la_cinta() -> void:
	var dialogo = current_scene.get_node("VentanaDialogo")
	for i in 300:
		if _flujo_real.CinematicaEnCurso: return
		if dialogo.Abierto:
			var elegida := false
			for b in dialogo.find_children("*", "Button", true, false):
				if not b.is_visible_in_tree(): continue
				for r in RESPUESTAS:
					if r in b.text:
						b.emit_signal("pressed")
						elegida = true
						break
				if elegida: break
			if not elegida: await _clic()
		await create_timer(0.25).timeout

func _cerrar_caso() -> void:
	await create_timer(1.5).timeout
	var dialogo = current_scene.get_node("VentanaDialogo")
	for i in 60:
		if _flujo_real.Terminado: break
		if dialogo.Abierto: await _clic()
		await create_timer(0.3).timeout

func _clic() -> void:
	var clic := InputEventMouseButton.new()
	clic.button_index = MOUSE_BUTTON_LEFT
	clic.pressed = true
	clic.position = Vector2(400, 400)
	Input.parse_input_event(clic)
	await process_frame
	var suelta := clic.duplicate()
	suelta.pressed = false
	Input.parse_input_event(suelta)

func _ver_cinta() -> bool:
	for hijo in root.get_children():
		if hijo.get_script() and str(hijo.get_script().resource_path).ends_with("VerCinta.cs"): return true
	return false

func _intros() -> int:
	var n := 0
	for hijo in root.get_children():
		if hijo.get_script() and str(hijo.get_script().resource_path).ends_with("IntroCaso.cs") and not hijo.is_queued_for_deletion():
			n += 1
	return n

func _saltar_intro() -> void:
	if _intros() == 0:
		return
	var e := InputEventKey.new()
	e.keycode = KEY_SPACE
	e.physical_keycode = KEY_SPACE
	e.pressed = true
	Input.parse_input_event(e)
	await create_timer(1.8).timeout
	var s := e.duplicate()
	s.pressed = false
	Input.parse_input_event(s)
	await create_timer(0.2).timeout

func _esperar_escena(ruta: String, recargar := false) -> void:
	var anterior = current_scene
	for i in 400:
		await process_frame
		var lista: bool = current_scene != null and current_scene.scene_file_path == ruta and current_scene.is_node_ready()
		if lista and (not recargar or current_scene != anterior):
			break
	await create_timer(0.5).timeout

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
