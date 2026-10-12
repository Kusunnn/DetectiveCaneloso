extends SceneTree
## HU-07: prueba automática de la vista de examen y los detalles ocultos (CA-01 a CA-04).
## Recoge todas las evidencias de un caso, las examina, descubre sus detalles, comprueba los estados
## del expediente y que todo vuelve igual al guardar y cargar (sin tocar las ranuras del jugador).
## Uso: --headless --path game --script res://Herramientas/probar_examen.gd -- [caso0|caso1]

const ESCENAS := {
	"caso0": "res://Escenas/Interrogatorio/sala_interrogatorio.tscn",
	"caso1": "res://Escenas/Caso1/escenario_campamento.tscn",
}
const NODO_CASO := {"caso0": "Caso0", "caso1": "Caso1"}

var _fallas := 0

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	var caso: String = args[0] if args.size() > 0 else "caso0"
	# El caso autoguarda al recoger pistas: las ranuras del jugador se respaldan y se devuelven al final
	var respaldo := _respaldar_partidas()
	await _probar(caso)
	_devolver_partidas(respaldo)
	print("")
	print("RESULTADO %s: %s" % [caso, "OK" if _fallas == 0 else "%d FALLAS" % _fallas])
	quit(1 if _fallas > 0 else 0)

const PARTIDAS := "user://partidas/"

func _respaldar_partidas() -> Dictionary:
	var copia := {}
	for archivo in DirAccess.get_files_at(PARTIDAS):
		copia[archivo] = FileAccess.get_file_as_bytes(PARTIDAS + archivo)
	return copia

func _devolver_partidas(copia: Dictionary) -> void:
	for archivo in DirAccess.get_files_at(PARTIDAS):
		if not copia.has(archivo):
			DirAccess.remove_absolute(PARTIDAS + archivo)
	for archivo in copia:
		var f := FileAccess.open(PARTIDAS + archivo, FileAccess.WRITE)
		f.store_buffer(copia[archivo])
		f.close()

func _ok(condicion: bool, texto: String) -> void:
	print(("  [OK]    " if condicion else "  [FALLA] ") + texto)
	if not condicion:
		_fallas += 1

func _esperar(segundos: float) -> void:
	await create_timer(segundos, true).timeout

func _cargar(caso: String) -> Node:
	var escena: Node = load(ESCENAS[caso]).instantiate()
	root.add_child(escena)
	current_scene = escena
	await _esperar(0.5)
	return escena

func _probar(caso: String) -> void:
	var escena := await _cargar(caso)
	var jugador = escena.get_node("Jugador")
	var hud = escena.get_node("HUD")
	# Con la linterna encendida se pueden tomar también las pistas que la necesitan
	hud.linterna_cambiada.emit(true)

	var objetos: Array = get_nodes_in_group("objetos_pista").filter(func(o): return o.EsEvidencia and o.is_visible_in_tree())
	print("== %s: %d evidencias en la escena" % [caso, objetos.size()])

	# Recoger todo
	for objeto in objetos:
		objeto.Interactuar(jugador)
	await _esperar(0.3)
	var pistas: Array = hud.pistas
	_ok(pistas.size() == objetos.size(), "se registraron %d de %d evidencias" % [pistas.size(), objetos.size()])

	# CA-02: recoger nunca descubre un detalle
	var detalles_al_recoger := pistas.filter(func(p): return p.es_detalle())
	_ok(detalles_al_recoger.is_empty(), "CA-02: recoger no crea entradas de detalle")
	var con_detalle := pistas.filter(func(p): return not p.detalles.is_empty())
	_ok(con_detalle.size() >= 1, "%d evidencias esconden detalles" % con_detalle.size())

	# CA-04: recién recogidas, todas están "sin revisar"
	_ok(pistas.all(func(p): return hud._estado_pista(p) == 0), "CA-04: recién recogidas = sin revisar (lupa gris)")
	_ok(hud.contar_examinadas() == Vector2i(0, pistas.size()), "contador EXAMINADAS 0/%d" % pistas.size())

	# CA-01: cada evidencia abre la vista de examen con su modelo y su descripción ampliada
	var evidencias := pistas.duplicate()
	for pista in evidencias:
		jugador.Examinar(pista)
		await _esperar(0.6)
		var vista = escena.get_node_or_null("VistaExamen")
		var abierta: bool = vista != null and vista.Abierta
		var mallas := 0
		if abierta:
			mallas = vista.get_node("Visor/Mundo/Pivote/Escala").find_children("*", "VisualInstance3D", true, false).size()
		_ok(abierta and mallas > 0, "CA-01: «%s» abre la vista (%d mallas)" % [pista.titulo, mallas])
		_ok(paused, "  el juego queda en pausa")
		# Sin girar ni acercar, ningún detalle está a la vista
		if abierta:
			_ok(vista.DetalleALaVista == "", "  al abrirla no se ve ningún detalle")
		# CA-02 / CA-03: descubrir cada detalle girando y acercándose
		for detalle in pista.detalles:
			vista.MirarDetalle(detalle.Id)
			await _esperar(0.15)
			_ok(vista.DetalleALaVista == detalle.Id, "  detalle «%s» a la vista (lupa)" % detalle.Id)
			var tecla := InputEventKey.new()
			tecla.keycode = KEY_E
			tecla.physical_keycode = KEY_E
			tecla.pressed = true
			Input.parse_input_event(tecla)
			await _esperar(2.0)
			var id := "%s.%s" % [pista.id, detalle.Id]
			var entrada = hud.buscar_pista(StringName(id))
			_ok(entrada != null and entrada.origen_id == pista.id, "CA-03: entrada nueva «%s» vinculada a su origen" % id)
		vista.Cerrar()
		await _esperar(0.5)
		_ok(not paused, "  al cerrar se reanuda el juego")
		_ok(pista.examinada, "  queda marcada como examinada")

	# CA-03: volver a examinar no duplica
	var antes: int = hud.pistas.size()
	for pista in con_detalle:
		jugador.Examinar(pista)
		await _esperar(0.6)
		var vista = escena.get_node("VistaExamen")
		for detalle in pista.detalles:
			vista.MirarDetalle(detalle.Id)
			await _esperar(0.15)
			_ok(vista.DetalleALaVista == "", "CA-03: «%s» ya descubierto no vuelve a ofrecerse" % detalle.Id)
		vista.Cerrar()
		await _esperar(0.5)
	_ok(hud.pistas.size() == antes, "CA-03: no se duplican las entradas (%d)" % antes)

	# CA-04: estados distintos
	var estados := {}
	for pista in hud.pistas:
		estados[hud._estado_pista(pista)] = true
	_ok(estados.has(1) and estados.has(2), "CA-04: hay evidencias examinadas (1) y con detalle (2)")
	var cuenta: Vector2i = hud.contar_examinadas()
	_ok(cuenta.x == cuenta.y and cuenta.y == evidencias.size(), "contador EXAMINADAS %d/%d (los detalles no cuentan)" % [cuenta.x, cuenta.y])
	# Los detalles van justo debajo de su evidencia
	var orden_bien := true
	for i in range(hud.pistas.size()):
		var p = hud.pistas[i]
		if p.es_detalle():
			var j := i - 1
			while j >= 0 and hud.pistas[j].es_detalle():
				j -= 1
			if j < 0 or hud.pistas[j].id != p.origen_id:
				orden_bien = false
	_ok(orden_bien, "CA-04: cada detalle queda unido a su evidencia de origen")

	# Guardar y cargar: todo se ve igual
	var caso_nodo = escena.get_node(NODO_CASO[caso])
	var estado = JSON.parse_string(JSON.stringify(caso_nodo.CapturarEstado()))
	var antes_ids: Array = hud.pistas.map(func(p): return [str(p.id), p.examinada, str(p.origen_id)])
	escena.queue_free()
	await _esperar(0.3)
	var nueva := await _cargar(caso)
	nueva.get_node(NODO_CASO[caso]).RestaurarEstado(estado)
	await _esperar(0.3)
	var hud2 = nueva.get_node("HUD")
	var despues_ids: Array = hud2.pistas.map(func(p): return [str(p.id), p.examinada, str(p.origen_id)])
	_ok(despues_ids == antes_ids, "CA-04: al cargar, mismas entradas, mismo orden y mismos estados (%d)" % despues_ids.size())
	_ok(hud2.pistas.all(func(p): return p.imagen != null), "  todas las entradas tienen imagen al cargar")
	for p in hud2.pistas:
		if p.es_detalle():
			DirAccess.remove_absolute("user://detalles/%s.png" % p.id)
	nueva.queue_free()
	await _esperar(0.2)
