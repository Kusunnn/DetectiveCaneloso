extends SceneTree
## Mide el audio de la cinemática de la cinta (Caso 0): graba el bus Master a WAV y anota cada 0.5 s
## el pico de cada bus (para saber de dónde viene cada capa) y los FPS con el tiempo de GPU por cuadro. Necesita ventana (sin ella no hay audio).
## Uso: --path game --script res://Herramientas/medir_audio_cinta.gd -- <salida.wav> <salida.csv> [musica_intensidad]

var _grabadora: AudioEffectRecord
var _csv: FileAccess
var _prueba
var _t := 0.0
var _siguiente := 0.0
var _midiendo := false

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	# Sin vsync: los FPS medidos son los que el equipo puede dar
	DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_DISABLED)
	var wav: String = args[0]
	_csv = FileAccess.open(args[1], FileAccess.WRITE)
	var buses := []
	for i in AudioServer.bus_count: buses.append(AudioServer.get_bus_name(i))
	_csv.store_line("t,fps,gpu_ms," + ",".join(buses))
	RenderingServer.viewport_set_measure_render_time(root.get_viewport_rid(), true)
	var sala: Node3D = load("res://Escenas/Interrogatorio/sala_interrogatorio.tscn").instantiate()
	root.add_child(sala)
	await create_timer(1.5).timeout
	for n in root.find_children("*", "CanvasLayer", true, false):
		if n.layer >= 100: n.visible = false
	var caso = sala.get_node("Caso0")
	# Como en el juego: al tener la cinta la música está al máximo (EmpezarResolucion)
	if args.size() > 2 and caso.Musica != null: caso.Musica.Intensidad = float(args[2])
	await create_timer(3.5).timeout
	_grabadora = AudioEffectRecord.new()
	AudioServer.add_bus_effect(0, _grabadora)
	_grabadora.set_recording_active(true)
	_prueba = load("res://tests/CinematicaCintaPrueba.cs").new()
	root.add_child(_prueba)
	_prueba.Empezar(sala, false)
	_midiendo = true
	while not _prueba.Terminada:
		await process_frame
	await create_timer(1.0).timeout
	_midiendo = false
	_grabadora.set_recording_active(false)
	_grabadora.get_recording().save_to_wav(wav)
	_csv.close()
	print("listo: ", wav)
	quit()

func _process(delta: float) -> bool:
	if not _midiendo: return false
	_t += delta
	if _t >= _siguiente:
		_siguiente += 0.5
		var fila := ["%.2f" % _t, str(Engine.get_frames_per_second()), "%.2f" % RenderingServer.viewport_get_measured_render_time_gpu(root.get_viewport_rid())]
		for i in AudioServer.bus_count:
			fila.append("%.1f" % max(AudioServer.get_bus_peak_volume_left_db(i, 0), AudioServer.get_bus_peak_volume_right_db(i, 0)))
		_csv.store_line(",".join(fila))
	return false
