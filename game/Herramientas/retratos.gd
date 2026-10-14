extends SceneTree
## Rehace los retratos de los diálogos (Assets/Retratos/<nombre>.png) con los modelos que hoy usa el juego.
## Eriz, Porky y el comisario se fotografían en su lugar de la comisaría, mirando a la cámara.
## Caneloso (su modelo va oculto en primera persona) y la cinta se fotografían en un estudio con fondo oscuro.
## Necesita ventana. Uso: --path game --script res://Herramientas/retratos.gd -- [carpeta_salida|-] [nombres...]
## Sin carpeta (o con "-") guarda en res://Assets/Retratos/. Las ranuras del jugador se respaldan y se devuelven al final.

const ESCENA := "res://Escenas/Interrogatorio/sala_interrogatorio.tscn"
const PARTIDAS := "user://partidas/"
const TAM := 512
# nombre: [nodo, distancia de la cámara, altura extra de la cámara, altura del punto que se mira, giro en grados]
const PERSONAJES := {
	"eriz": ["Eriz", 2.5, 0.3, 0.0, -28.0],
	"porky": ["Porky", 2.4, 0.95, 0.35, 0.0],
	"comisario": ["Comisario", 3.1, 0.0, -0.25, 0.0],
}

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	var carpeta: String = args[0] if args.size() > 0 and args[0] != "-" else ProjectSettings.globalize_path("res://Assets/Retratos")
	var solo: Array = Array(args.slice(1)) if args.size() > 1 else []
	var respaldo := {}
	for archivo in DirAccess.get_files_at(PARTIDAS):
		respaldo[archivo] = FileAccess.get_file_as_bytes(PARTIDAS + archivo)
	await process_frame

	var escena: Node = load(ESCENA).instantiate()
	root.add_child(escena)
	current_scene = escena
	await create_timer(2.0).timeout
	paused = false
	for capa in escena.find_children("*", "CanvasLayer", true, false):
		capa.visible = false
	for n in root.get_children():
		if n is CanvasLayer:
			n.visible = false
	var jugador: Node3D = escena.get_node("Jugador")
	var cam := Camera3D.new()
	escena.add_child(cam)
	cam.current = true
	cam.fov = 32
	cam.near = 0.02
	# Luz suave de retrato junto a la cámara, para que la cara no quede en sombra
	var luz := OmniLight3D.new()
	cam.add_child(luz)
	luz.position = Vector3(0.6, 0.4, 0.2)
	luz.light_color = Color(1.0, 0.9, 0.78)
	luz.light_energy = 1.2
	luz.omni_range = 5.0

	for nombre in PERSONAJES:
		if solo.size() > 0 and not solo.has(nombre):
			continue
		var datos: Array = PERSONAJES[nombre]
		var objetivo := escena.find_child(datos[0], true, false) as Node3D
		if objetivo == null:
			push_error("No encontré " + datos[0])
			continue
		# Sin las zetas de dormido de Porky
		var sueno := objetivo.get_node_or_null("Sueno") as Node3D
		if sueno:
			sueno.visible = false
		var cara := _cara(objetivo)
		var frente := objetivo.global_basis.z
		frente.y = 0
		frente = frente.normalized().rotated(Vector3.UP, deg_to_rad(datos[4]))
		var posicion: Vector3 = cara + frente * datos[1] + Vector3.UP * datos[2]
		# El personaje voltea hacia el jugador: se pone donde está la cámara
		jugador.global_position = posicion - Vector3.UP * 1.4
		cam.look_at_from_position(posicion, cara + Vector3.UP * datos[3])
		await create_timer(1.6).timeout
		await RenderingServer.frame_post_draw
		_guardar(root.get_texture().get_image(), carpeta.path_join(nombre + ".png"))

	escena.visible = false
	if solo.size() == 0 or solo.has("caneloso"):
		await _estudio("res://Assets/Models/Caneloso/Archivo/player.tscn", carpeta.path_join("caneloso.png"), true)
	if solo.size() == 0 or solo.has("cinta"):
		await _estudio("res://Assets/Pistas/Modelos/cinta/cinta.tscn", carpeta.path_join("cinta.png"), false)

	for archivo in DirAccess.get_files_at(PARTIDAS):
		DirAccess.remove_absolute(PARTIDAS + archivo)
	for archivo in respaldo:
		var f := FileAccess.open(PARTIDAS + archivo, FileAccess.WRITE)
		f.store_buffer(respaldo[archivo])
	quit()

## Centro de la cara: los huesos de la cabeza si hay esqueleto, la malla Cabeza si es de piezas, o el centro de las mallas.
func _cara(nodo: Node3D) -> Vector3:
	for esqueleto in nodo.find_children("*", "Skeleton3D", true, false):
		# Mixamo (Caneloso): entre la base de la cabeza y la punta
		var base: int = esqueleto.find_bone("mixamorig_Head")
		var punta: int = esqueleto.find_bone("mixamorig_HeadTop_End")
		if base >= 0 and punta >= 0:
			return esqueleto.global_transform * esqueleto.get_bone_global_pose(base).origin.lerp(esqueleto.get_bone_global_pose(punta).origin, 0.45)
		var hueso: int = esqueleto.find_bone("Head")
		if hueso >= 0:
			return esqueleto.global_transform * esqueleto.get_bone_global_pose(hueso).origin + Vector3.UP * 0.15
	var cabeza := nodo.find_child("Cabeza", true, false) as Node3D
	if cabeza:
		return cabeza.global_position
	return _caja(nodo).get_center()

func _caja(nodo: Node3D) -> AABB:
	var caja := AABB()
	var primero := true
	for m in nodo.find_children("*", "MeshInstance3D", true, false):
		var a: AABB = m.global_transform * m.get_aabb()
		caja = a if primero else caja.merge(a)
		primero = false
	return caja

## Estudio de fondo oscuro con luz cálida: un personaje (retrato de la cara) o un objeto (sobre una mesa).
func _estudio(ruta_modelo: String, ruta: String, personaje: bool) -> void:
	var estudio := Node3D.new()
	root.add_child(estudio)
	var entorno := WorldEnvironment.new()
	entorno.environment = Environment.new()
	entorno.environment.background_mode = Environment.BG_COLOR
	entorno.environment.background_color = Color(0.09, 0.08, 0.075)
	entorno.environment.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	entorno.environment.ambient_light_color = Color(0.55, 0.55, 0.62)
	entorno.environment.ambient_light_energy = 0.45
	entorno.environment.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	estudio.add_child(entorno)
	var modelo: Node3D = load(ruta_modelo).instantiate()
	estudio.add_child(modelo)
	await process_frame
	var foco := SpotLight3D.new()
	estudio.add_child(foco)
	foco.light_color = Color(1.0, 0.86, 0.66)
	foco.spot_range = 8.0
	foco.spot_angle = 35
	foco.shadow_enabled = true
	var cam := Camera3D.new()
	estudio.add_child(cam)
	cam.fov = 32
	cam.near = 0.01
	cam.current = true
	if personaje:
		var cara := _cara(modelo)
		foco.light_energy = 3.0
		foco.look_at_from_position(cara + Vector3(-1.2, 1.4, 2.2), cara)
		var relleno := OmniLight3D.new()
		estudio.add_child(relleno)
		relleno.position = cara + Vector3(1.5, 0.3, 1.5)
		relleno.light_energy = 0.6
		relleno.omni_range = 6.0
		# El modelo mira hacia +Z
		cam.look_at_from_position(cara + Vector3(0, 0.05, 2.3), cara + Vector3(0, -0.12, 0))
	else:
		var mesa := MeshInstance3D.new()
		var plano := PlaneMesh.new()
		plano.size = Vector2(2, 2)
		var madera := StandardMaterial3D.new()
		madera.albedo_color = Color(0.22, 0.17, 0.13)
		madera.roughness = 0.8
		plano.material = madera
		mesa.mesh = plano
		estudio.add_child(mesa)
		var caja := _caja(modelo)
		modelo.position.y -= caja.position.y
		var centro := caja.get_center() - Vector3(0, caja.position.y, 0)
		var tam := caja.size.length()
		foco.light_energy = 1.4
		foco.look_at_from_position(centro + Vector3(-0.3, 0.8, 0.4) * tam * 2.5, centro)
		cam.look_at_from_position(centro + Vector3(0.25, 0.8, 0.9).normalized() * tam * 1.15, centro)
	await create_timer(0.8).timeout
	await RenderingServer.frame_post_draw
	_guardar(root.get_texture().get_image(), ruta)
	estudio.queue_free()
	await process_frame

## Recorta el centro en cuadrado y lo guarda a TAM × TAM.
func _guardar(imagen: Image, ruta: String) -> void:
	var lado := mini(imagen.get_width(), imagen.get_height())
	imagen = imagen.get_region(Rect2i((imagen.get_width() - lado) / 2, (imagen.get_height() - lado) / 2, lado, lado))
	imagen.resize(TAM, TAM, Image.INTERPOLATE_LANCZOS)
	imagen.save_png(ruta)
	print("retrato: ", ruta)
