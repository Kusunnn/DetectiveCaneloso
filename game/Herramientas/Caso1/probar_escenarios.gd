extends SceneTree
## HU-20.6: prueba el Capítulo 1 repartido en escenarios: Campamento ↔ Lago ↔ sala de interrogatorio con el progreso
## (pistas, objetos, contenedores, puertas, el tablero y lo recorrido en los diálogos del guion), el cobertizo cerrado que abre el llavero de Porky, guardar y cargar en el
## Lago, y que en el muelle y la orilla no se cae al agua (de pie y agachado).
## Uso: --headless --path game --script res://Herramientas/Caso1/probar_escenarios.gd
## Respalda en disco (user://respaldo_pruebas/) las ranuras y el progreso, y los devuelve al final.

const PARTIDAS := "user://partidas/"
const ARCHIVOS := ["user://progreso.json", "user://progreso.json.bak", "user://cinematicas.cfg"]
const RESPALDO := "user://respaldo_pruebas/"
const CAMPAMENTO := "res://Escenas/Caso1/escenario_campamento.tscn"
const LAGO := "res://Escenas/Caso1/Lago/escenario_lago.tscn"
const SALA := "res://Escenas/Caso1/Interrogatorio/sala_interrogatorio_caso1.tscn"

var _fallos := 0
var _gestor

func _initialize() -> void:
	if DirAccess.dir_exists_absolute(RESPALDO):
		_devolver_respaldo()
	_respaldar()
	await process_frame
	_gestor = root.get_node("GestorPartida")

	print("1) Campamento: una pista y el llavero de Porky")
	_gestor.IniciarCapitulo("capitulo1", false, true)
	await _esperar_escena(CAMPAMENTO)
	await create_timer(3.0).timeout
	await _saltar_intro()
	await create_timer(1.5).timeout
	await _cerrar_dialogos()
	var interruptor = _buscar_pista("interruptor_generador")
	interruptor.Interactuar(_jugador())
	_jugador().DarObjeto("llavero_porky", "")
	await create_timer(0.5).timeout
	_revisar(_pistas().has("interruptor_generador"), "la pista del Campamento queda en el diario")
	var relaciones = current_scene.get_node("HUD").relaciones_pizarron
	relaciones.desbloquear("notas")
	relaciones.guardar_nota(&"", "Hipótesis del campamento", Vector2(200, 200))
	var dialogos_campamento: Dictionary = _estado().dialogos
	_revisar(dialogos_campamento.get("nodos_completados", {}).has("caso1_introduccion"), "la presentación de Hecdog queda en lo recorrido del guion")

	print("2) Del Campamento al Lago")
	_gestor.CambiarEscenaDelCapitulo(LAGO, "EntradaDesdeCampamento")
	await _esperar_escena(LAGO)
	await create_timer(1.0).timeout
	await _cerrar_dialogos()
	var entrada: Node3D = current_scene.find_child("EntradaDesdeCampamento", true, false)
	_revisar(_jugador().global_position.distance_to(entrada.global_position) < 0.6, "aparece en la entrada del sendero")
	_revisar(_pistas().has("interruptor_generador"), "trae la pista del Campamento")
	_revisar(_jugador().TieneObjeto("llavero_porky"), "trae el llavero de Porky")
	var recorrido: Array = _estado().dialogos.get("nodos_completados", {}).get("caso1_introduccion", [])
	_revisar(dialogos_campamento.nodos_completados.caso1_introduccion.all(func(n): return n in recorrido), "trae lo recorrido en los diálogos")
	_revisar(_estado().tablero.tarjetas.size() == 1, "trae la nota del tablero")
	var puerta = current_scene.get_node("Cobertizo/Edificio/PuertaCobertizo")
	_revisar(not puerta.Bloqueada, "el llavero abre el cobertizo de botes")
	var silbato = _buscar_pista("silbato_ts")
	silbato.Interactuar(_jugador())
	var mochila = current_scene.get_node("Orilla/Mochila")
	var cuaderno = _buscar_pista("cuaderno_eriz")
	_revisar(not cuaderno.visible and cuaderno.collision_layer == 0, "el cuaderno no se ve hasta abrir la mochila")
	mochila.Interactuar(_jugador())
	await create_timer(1.2).timeout
	_revisar(cuaderno.visible and cuaderno.collision_layer != 0, "al abrir la mochila aparece el cuaderno")
	puerta.Interactuar(_jugador())
	await create_timer(1.2).timeout
	_revisar(puerta.EstaAbierta, "la puerta del cobertizo se abre")
	_revisar(_pistas().has("silbato_ts"), "la pista del Lago queda en el diario")

	print("3) Del Lago a la sala de interrogatorio")
	_gestor.CambiarEscenaDelCapitulo(SALA, "EntradaDesdeLago")
	await _esperar_escena(SALA)
	await create_timer(1.0).timeout
	var caso = current_scene.get_node("Caso1Sala")
	var dialogo = current_scene.get_node("VentanaDialogo")
	_revisar(dialogo.Abierto, "la primera vez Hecdog recibe al detective en la sala")
	await _cerrar_dialogos()
	_revisar(_estado().dialogos.get("nodos_completados", {}).has("caso1_sala"), "la llegada a la sala queda en lo recorrido")
	_revisar(_pistas().has("silbato_ts") and _pistas().has("interruptor_generador"), "la sala tiene las pistas de los dos escenarios")
	_revisar(current_scene.get_node("MesaEvidencias/Pruebas").get_child_count() >= 2, "las pruebas están en la mesa auxiliar")
	_revisar(current_scene.get_node("Corcho/Fotos").get_child_count() >= 7, "el corcho tiene las fotos y el estambre")
	var sentados := []
	caso.ImplicadoSentado.connect(func(n): sentados.append(n))
	caso.Traer("Tony")
	await caso.ImplicadoSentado
	var tony: Node3D = current_scene.get_node("Implicados/Tony")
	_revisar(tony.visible and tony.global_position.distance_to(current_scene.get_node("PuntoSilla").global_position) < 0.1 and sentados == ["Tony"],
		"Tony entra y llega a la silla del sospechoso")
	caso.Traer("Aya")
	await caso.ImplicadoSentado
	_revisar(not tony.visible and current_scene.get_node("Implicados/Aya").visible, "al traer a Aya, Tony sale primero")
	caso.Retirar()
	await caso.ImplicadoRetirado
	_revisar(caso.ImplicadoActual in [null, ""] and not current_scene.get_node("Implicados/Aya").visible, "Aya se retira")

	print("4) De la sala al Lago: todo sigue como se dejó")
	_gestor.CambiarEscenaDelCapitulo(LAGO, "EntradaDesdeSala")
	await _esperar_escena(LAGO)
	await create_timer(1.0).timeout
	await _cerrar_dialogos()
	_revisar(current_scene.get_node("Orilla/Mochila").EstaAbierto, "la mochila sigue abierta")
	_revisar(_buscar_pista("cuaderno_eriz").visible, "el cuaderno sigue a la vista")
	_revisar(_buscar_pista("silbato_ts").Encontrada, "el silbato ya está examinado")
	_revisar(current_scene.get_node("Cobertizo/Edificio/PuertaCobertizo").EstaAbierta, "el cobertizo sigue abierto")

	print("5) Guardar en el Lago y cargar")
	await create_timer(0.5).timeout
	var donde: Vector3 = _jugador().global_position
	_revisar(_gestor.Guardar(3, false, ""), "guarda la ranura 3 en el Lago")
	_gestor.CargarRanura(3, false)
	await _esperar_escena(LAGO, true)
	await create_timer(1.5).timeout
	await _cerrar_dialogos()
	_revisar(_jugador().global_position.distance_to(donde) < 0.8, "carga en el Lago, donde se guardó")
	_revisar(_pistas().has("interruptor_generador") and _pistas().has("silbato_ts"), "con las pistas de los dos escenarios")
	_revisar(_estado().dialogos.get("nodos_completados", {}).has("caso1_sala"), "y con lo recorrido en la sala")
	_revisar(_estado().tablero.tarjetas.size() == 1, "y con el tablero")
	_revisar(current_scene.get_node("Orilla/Mochila").EstaAbierto, "y la mochila abierta")

	print("6) Del Lago al Campamento")
	_gestor.CambiarEscenaDelCapitulo(CAMPAMENTO, "LlegadaDesdeLago")
	await _esperar_escena(CAMPAMENTO)
	await create_timer(1.0).timeout
	var llegada: Node3D = current_scene.find_child("LlegadaDesdeLago", true, false)
	_revisar(_jugador().global_position.distance_to(llegada.global_position) < 0.6, "aparece junto al arco del sendero")
	_revisar(_buscar_pista("interruptor_generador").Encontrada, "el interruptor sigue examinado")
	_revisar(_pistas().has("silbato_ts"), "trae la pista del Lago")

	print("7) Muelle y orilla: no se cae al agua")
	_gestor.CambiarEscenaDelCapitulo(LAGO, "EntradaDesdeCampamento")
	await _esperar_escena(LAGO)
	await create_timer(1.0).timeout
	await _cerrar_dialogos()
	var j: Node3D = _jugador()
	j.global_position = Vector3(8.0, 0.35 + 1.1, -20.0)
	j.rotation = Vector3.ZERO
	await create_timer(0.8).timeout
	_revisar(j.global_position.y > 0.35 + 0.8, "de pie sobre el muelle (y = %.2f)" % j.global_position.y)
	await _caminar("mover_derecha", 2.0)
	_revisar(j.global_position.x < 9.3 and j.global_position.y > 1.0, "no se sale por el lado este (x = %.2f)" % j.global_position.x)
	await _caminar("mover_izquierda", 3.0)
	_revisar(j.global_position.x > 6.7 and j.global_position.y > 1.0, "no se sale por el lado oeste (x = %.2f)" % j.global_position.x)
	await _caminar("mover_adelante", 6.0)
	_revisar(j.global_position.z > -34.2 and j.global_position.y > 1.0, "no se sale por la punta (z = %.2f)" % j.global_position.z)
	Input.action_press("agacharse")
	await create_timer(0.8).timeout
	await _caminar("mover_derecha", 1.5)
	var y_agachado: float = j.global_position.y
	Input.action_release("agacharse")
	_revisar(y_agachado > 0.35 and j.global_position.x < 9.3, "agachado también se queda en el muelle (y = %.2f)" % y_agachado)
	await create_timer(0.8).timeout
	for x in [-30.0, -10.0, 2.0, 20.0, 30.0]:
		j.global_position = Vector3(x, 1.5, -2.0)
		j.rotation = Vector3.ZERO
		await create_timer(0.5).timeout
		await _caminar("mover_adelante", 4.0)
		_revisar(j.global_position.y > 0.4, "en la orilla (x = %.0f) se detiene antes del agua (z = %.2f, y = %.2f)" % [x, j.global_position.z, j.global_position.y])

	print("\nFallos: %d" % _fallos)
	_devolver_respaldo()
	quit(1 if _fallos > 0 else 0)

# ---------- utilidades ----------

func _esperar_escena(ruta: String, desde_cero := false) -> void:
	for i in 200:
		await process_frame
		if current_scene and current_scene.scene_file_path == ruta and current_scene.is_node_ready():
			break
	await create_timer(0.3).timeout

func _estado() -> Dictionary:
	return JSON.parse_string(JSON.stringify(get_nodes_in_group("estado_partida")[0].CapturarEstado()))

func _jugador():
	return current_scene.get_node("Jugador")

func _pistas() -> Array:
	var ids := []
	for p in current_scene.get_node("HUD").get("pistas"):
		ids.append(str(p.id))
	return ids

func _buscar_pista(id: String):
	for n in get_nodes_in_group("objetos_pista"):
		if n.IdPista == id and current_scene.is_ancestor_of(n): return n
	return null

func _saltar_intro() -> void:
	var intro: Node = null
	for hijo in root.get_children():
		if hijo.get_script() and str(hijo.get_script().resource_path).ends_with("IntroCaso.cs") and not hijo.is_queued_for_deletion():
			intro = hijo
	if intro == null:
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
	await create_timer(1.5).timeout

func _caminar(accion: String, segundos: float) -> void:
	Input.action_press(accion)
	await create_timer(segundos).timeout
	Input.action_release(accion)
	await create_timer(0.3).timeout

func _cerrar_dialogos() -> void:
	for i in 14:
		var dialogo = current_scene.get_node_or_null("VentanaDialogo")
		if dialogo == null or not dialogo.Abierto:
			return
		var clic := InputEventMouseButton.new()
		clic.button_index = MOUSE_BUTTON_LEFT
		clic.pressed = true
		clic.position = Vector2(400, 400)
		Input.parse_input_event(clic)
		await process_frame
		var suelta := clic.duplicate()
		suelta.pressed = false
		Input.parse_input_event(suelta)
		await create_timer(0.4).timeout

func _revisar(ok: bool, que: String) -> void:
	print(("  ✓ " if ok else "  ✗ ") + que)
	if not ok: _fallos += 1

func _respaldar() -> void:
	DirAccess.make_dir_recursive_absolute(RESPALDO + "partidas")
	for archivo in DirAccess.get_files_at(PARTIDAS):
		DirAccess.copy_absolute(PARTIDAS + archivo, RESPALDO + "partidas/" + archivo)
	for archivo in ARCHIVOS:
		if FileAccess.file_exists(archivo):
			DirAccess.copy_absolute(archivo, RESPALDO + archivo.get_file())

## Deja las ranuras y el progreso como estaban antes de la prueba y borra el respaldo
func _devolver_respaldo() -> void:
	for archivo in DirAccess.get_files_at(PARTIDAS):
		DirAccess.remove_absolute(PARTIDAS + archivo)
	for archivo in ARCHIVOS:
		DirAccess.remove_absolute(archivo)
	for archivo in DirAccess.get_files_at(RESPALDO + "partidas"):
		DirAccess.copy_absolute(RESPALDO + "partidas/" + archivo, PARTIDAS + archivo)
		DirAccess.remove_absolute(RESPALDO + "partidas/" + archivo)
	for archivo in DirAccess.get_files_at(RESPALDO):
		DirAccess.copy_absolute(RESPALDO + archivo, "user://" + archivo)
		DirAccess.remove_absolute(RESPALDO + archivo)
	DirAccess.remove_absolute(RESPALDO + "partidas")
	DirAccess.remove_absolute(RESPALDO)
