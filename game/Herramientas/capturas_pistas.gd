extends SceneTree
## HU-07: captura cada pista de un caso en el escenario y en la vista de examen (para comparar antes y después).
## Necesita ventana. Uso: --path game --script res://Herramientas/capturas_pistas.gd -- <caso0|caso1> <carpeta> [prefijo]
## Las ranuras del jugador se respaldan y se devuelven al final (el caso autoguarda al recoger pistas).

const ESCENAS := {
	"caso0": "res://Escenas/Interrogatorio/sala_interrogatorio.tscn",
	"caso1": "res://Escenas/Caso1/escenario_campamento.tscn",
}
# id: [dirección desde la que se mira, distancia]
const TOMAS := {
	"grabadora": [Vector3(0.35, 0.9, 1.0), 0.75],
	"reloj": [Vector3(0, 0, 1), 0.9],
	"pluma": [Vector3(0.2, -1, 0.15), 0.35],
	"huellas": [Vector3(0.25, 1, 0.35), 3.2],
	"envoltorio": [Vector3(0.3, 1, 0.35), 0.6],
	"libro_turnos": [Vector3(0.2, 1, 0.45), 0.75],
	"nota_comisario": [Vector3(0.1, 1, 0.25), 0.5],
	"cinta": [Vector3(1, 0.4, 0.15), 0.45],
	"interruptor_generador": [Vector3(1, 0.1, 0.25), 0.95],
	"candado_caseta": [Vector3(1, 0.25, -0.35), 0.55],
	"baul_disfraces": [Vector3(0.5, 1, 0.2), 1.25],
	"lena_intacta": [Vector3(-1, 0.55, 0.8), 3.4],
	"libro_guardia_quemado": [Vector3(0.12, 1, -0.1), 0.95],
	"nota_porky": [Vector3(0.3, 1, 0.0), 0.65],
	"foto_grupo": [Vector3(-1, 0.0, 0), 0.95],
	"recorte_periodico": [Vector3(0, 0, 1), 0.85],
	"oferta_compra": [Vector3(0.2, 1, 0.35), 0.55],
	"taza_delantal": [Vector3(-0.7, 0.75, -0.25), 0.45],
	"registro_enfermeria": [Vector3(0.35, 1, 0.1), 0.6],
	"botas_tony": [Vector3(0.4, 0.9, 0.9), 1.25],
}
const PARTIDAS := "user://partidas/"

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	var caso: String = args[0]
	var salida: String = args[1]
	var prefijo: String = args[2] if args.size() > 2 else ""
	DirAccess.make_dir_recursive_absolute(salida)
	var respaldo := {}
	for archivo in DirAccess.get_files_at(PARTIDAS):
		respaldo[archivo] = FileAccess.get_file_as_bytes(PARTIDAS + archivo)
	DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
	DisplayServer.window_set_size(Vector2i(1280, 720))
	root.size = Vector2i(1280, 720)
	await process_frame

	var escena: Node = load(ESCENAS[caso]).instantiate()
	root.add_child(escena)
	current_scene = escena
	await create_timer(1.5).timeout
	var jugador = escena.get_node("Jugador")
	var hud = escena.get_node("HUD")
	var objetos: Array = get_nodes_in_group("objetos_pista").filter(func(o): return o.EsEvidencia and o.is_visible_in_tree() and TOMAS.has(o.IdPista))

	# 1) En el escenario: contenedores abiertos, lo de la linterna visible y sin interfaz
	for c in escena.find_children("*", "Node", true, false):
		if c.has_signal("Abierto") and c.has_method("RestaurarEstado"):
			c.RestaurarEstado(true, true)
	for n in get_nodes_in_group("solo_con_linterna"):
		n.visible = true
	var capas: Array = escena.find_children("*", "CanvasLayer", true, false).filter(func(c): return c.visible)
	for capa in capas:
		capa.visible = false
	jugador.visible = false
	var camara_jugador: Camera3D = jugador.get_viewport().get_camera_3d()
	var cam := Camera3D.new()
	escena.add_child(cam)
	cam.fov = 45
	cam.near = 0.02
	cam.current = true
	var foco := SpotLight3D.new()
	cam.add_child(foco)
	foco.spot_range = 8
	foco.spot_angle = 35
	foco.light_energy = 2.2
	foco.light_color = Color(1, 0.94, 0.82)
	foco.shadow_enabled = true
	foco.position = Vector3(0.25, 0.2, 0)
	for o in objetos:
		var toma: Array = TOMAS[o.IdPista]
		var centro := _centro(o)
		var dir := (toma[0] as Vector3).normalized()
		cam.global_position = centro + dir * float(toma[1])
		cam.look_at(centro, Vector3.UP if absf(dir.y) < 0.95 else Vector3.FORWARD)
		for i in 15:
			await process_frame
		root.get_texture().get_image().save_png(salida.path_join("%s%s_escenario.png" % [prefijo, o.IdPista]))
		print("Escenario: ", o.IdPista)
	cam.queue_free()
	if camara_jugador:
		camara_jugador.current = true
	for capa in capas:
		capa.visible = true
	var dialogo := escena.get_node_or_null("VentanaDialogo")
	if dialogo:
		dialogo.visible = false

	# 2) En la vista de examen
	jugador.Bloquear(false)
	hud.linterna_cambiada.emit(true)
	for o in objetos:
		o.Interactuar(jugador)
	await create_timer(0.5).timeout
	for o in objetos:
		var pista = hud.buscar_pista(StringName(o.IdPista))
		if pista == null:
			continue
		jugador.Examinar(pista)
		await create_timer(1.0, true).timeout
		root.get_texture().get_image().save_png(salida.path_join("%s%s_examen.png" % [prefijo, o.IdPista]))
		print("Examen: ", o.IdPista)
		escena.get_node("VistaExamen").Cerrar()
		await create_timer(0.5, true).timeout

	for archivo in DirAccess.get_files_at(PARTIDAS):
		if not respaldo.has(archivo):
			DirAccess.remove_absolute(PARTIDAS + archivo)
	for archivo in respaldo:
		var f := FileAccess.open(PARTIDAS + archivo, FileAccess.WRITE)
		f.store_buffer(respaldo[archivo])
		f.close()
	quit()

# Centro de lo que se ve de la pista (sus mallas y sus Visuales)
func _centro(o: Node3D) -> Vector3:
	var caja := AABB()
	var primera := true
	var visuales: Array = o.find_children("*", "VisualInstance3D", true, false)
	for v in o.Visuales:
		if v:
			visuales.append(v)
			visuales.append_array(v.find_children("*", "VisualInstance3D", true, false))
	for v in visuales:
		var c: AABB = v.global_transform * v.get_aabb()
		if c.size == Vector3.ZERO:
			continue
		caja = c if primera else caja.merge(c)
		primera = false
	return caja.get_center() if not primera else o.global_position
