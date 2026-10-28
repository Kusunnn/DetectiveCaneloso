extends SceneTree
## Rehace los retratos de los diálogos (Assets/Retratos/<nombre>.png) con los modelos que hoy usa el juego.
## Eriz, Porky y el comisario se fotografían en su lugar de la comisaría, mirando a la cámara.
## Caneloso (su modelo va oculto en primera persona) y la cinta se fotografían en un estudio con fondo oscuro.
## "menu" hace a Caneloso de cuerpo entero para el menú de inicio (Assets/Menu/caneloso.png): fondo transparente,
## congelado en un cuadro de una animación (para que no salga en pose T) y con la lupa 3D en la mano derecha.
## Necesita ventana. Uso: --path game --script res://Herramientas/retratos.gd -- [carpeta_salida|-] [nombres...]
## Sin carpeta (o con "-") guarda en res://Assets/Retratos/ (y el del menú en res://Assets/Menu/).
## Otra pose para el menú: "menu:<animación>:<segundo>", por ejemplo menu:anim_reposo:2.5.
## Las ranuras del jugador se respaldan y se devuelven al final.

const ESCENA := "res://Escenas/Interrogatorio/sala_interrogatorio.tscn"
const PARTIDAS := "user://partidas/"
const TAM := 512
const CANELOSO := "res://Assets/Models/Caneloso/Archivo/player.tscn"
const LUPA := "res://Assets/Cinematicas/Modelos/polyhaven/magnifying_glass_01/magnifying_glass_01_1k.gltf"
const MENU := "res://Assets/Menu/caneloso.png"
# Pose del menú: animación y segundo en que se congela (de pie, con una mano en el pecho y la derecha abajo, con la lupa)
const POSE_MENU := ["anim_agarrar", 1.2]
# Tamaño de la imagen del menú: FondoMenu.cs la muestra a 540 px de alto, con los pies abajo y centrada en la puerta
# (el ancho es el mínimo; crece si la lupa sobresale)
const TAM_MENU := Vector2i(644, 1079)
# Saturación y brillo de la imagen del menú (más oscura y apagada que los retratos)
const MENU_SATURACION := 0.8
const MENU_BRILLO := 0.55
# Lupa: cuántas veces su tamaño real y cuántos grados se inclina hacia afuera
const LUPA_ESCALA := 2.2
const LUPA_GIRO := -70.0
# Distancia de la muñeca al centro del puño, en metros, y corrección para que el mango pase por dentro del puño
const LUPA_PUNO := 0.09
const LUPA_AJUSTE := Vector3(0, -0.1, 0.05)
# Altura del mango (en el modelo sin agrandar) que queda dentro del puño, desde la punta
const LUPA_AGARRE := 0.045
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
	var pose_menu: Array = POSE_MENU
	for nombre in solo.duplicate():
		if nombre.begins_with("menu:"):
			var partes: PackedStringArray = nombre.split(":")
			pose_menu = [partes[1], float(partes[2]) if partes.size() > 2 else 0.0]
			solo.erase(nombre)
			solo.append("menu")
	var respaldo := {}
	for archivo in DirAccess.get_files_at(PARTIDAS):
		respaldo[archivo] = FileAccess.get_file_as_bytes(PARTIDAS + archivo)
	await process_frame

	var escena: Node = load(ESCENA).instantiate()
	root.add_child(escena)
	current_scene = escena
	await create_timer(2.0).timeout
	paused = false
	_ocultar_capas()
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
		# Por si apareció algún aviso o ventana del juego mientras tanto
		_ocultar_capas()
		await RenderingServer.frame_post_draw
		_guardar(root.get_texture().get_image(), carpeta.path_join(nombre + ".png"))

	escena.visible = false
	if solo.size() == 0 or solo.has("caneloso"):
		await _estudio(CANELOSO, carpeta.path_join("caneloso.png"), true)
	if solo.size() == 0 or solo.has("cinta"):
		await _estudio("res://Assets/Pistas/Modelos/cinta/cinta.tscn", carpeta.path_join("cinta.png"), false)
	if solo.size() == 0 or solo.has("menu"):
		var ruta_menu: String = ProjectSettings.globalize_path(MENU) if args.size() == 0 or args[0] == "-" else carpeta.path_join("menu_caneloso.png")
		await _figura_menu(ruta_menu, pose_menu[0], pose_menu[1])

	for archivo in DirAccess.get_files_at(PARTIDAS):
		DirAccess.remove_absolute(PARTIDAS + archivo)
	for archivo in respaldo:
		var f := FileAccess.open(PARTIDAS + archivo, FileAccess.WRITE)
		f.store_buffer(respaldo[archivo])
	quit()

## Oculta toda la interfaz (HUD, avisos y diálogos de los autoloads) para que no salga en la foto.
func _ocultar_capas() -> void:
	for capa in root.find_children("*", "CanvasLayer", true, false):
		capa.visible = false

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

## Caneloso de cuerpo entero sobre fondo transparente, con la lupa en la mano, encuadrado como la imagen del menú.
func _figura_menu(ruta: String, animacion: String, segundo: float) -> void:
	var tam_ventana := root.size
	root.size = Vector2i(1000, 1400)
	root.transparent_bg = true
	var estudio := Node3D.new()
	root.add_child(estudio)
	var entorno := WorldEnvironment.new()
	entorno.environment = Environment.new()
	entorno.environment.background_mode = Environment.BG_CLEAR_COLOR
	entorno.environment.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	entorno.environment.ambient_light_color = Color(0.55, 0.55, 0.62)
	entorno.environment.ambient_light_energy = 0.45
	entorno.environment.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	estudio.add_child(entorno)
	var modelo: Node3D = load(CANELOSO).instantiate()
	estudio.add_child(modelo)
	_posar(modelo, animacion, segundo)
	await process_frame
	var esqueleto: Skeleton3D = modelo.find_children("*", "Skeleton3D", true, false)[0]
	_poner_lupa(esqueleto)
	var cara := _cara(modelo)
	var pies := _hueso(esqueleto, "mixamorig_LeftToeBase").lerp(_hueso(esqueleto, "mixamorig_RightToeBase"), 0.5)
	var alto := cara.y - pies.y
	# Misma luz que los retratos: cálida desde arriba a la izquierda y un relleno suave
	var foco := SpotLight3D.new()
	estudio.add_child(foco)
	foco.light_color = Color(1.0, 0.86, 0.66)
	foco.light_energy = 3.0
	foco.spot_range = 12.0
	foco.spot_angle = 35
	foco.shadow_enabled = true
	foco.look_at_from_position(cara + Vector3(-1.4, 1.6, 3.0), cara.lerp(pies, 0.4))
	var relleno := OmniLight3D.new()
	estudio.add_child(relleno)
	relleno.position = cara + Vector3(1.5, 0.0, 2.0)
	relleno.light_energy = 0.6
	relleno.omni_range = 8.0
	# Cámara de frente, un poco por encima de la cintura (el modelo mira hacia +Z)
	var cam := Camera3D.new()
	estudio.add_child(cam)
	cam.fov = 30
	cam.near = 0.05
	cam.current = true
	var mira := pies.lerp(cara, 0.55)
	mira.x = pies.x
	mira.z = pies.z
	cam.look_at_from_position(mira + Vector3(0, alto * 0.25, alto * 3.4), mira)
	await create_timer(0.8).timeout
	await RenderingServer.frame_post_draw
	var imagen := root.get_texture().get_image()
	# La cámara proyecta en coordenadas del lienzo, que pueden no medir lo mismo que la textura
	var cadera := cam.unproject_position(_hueso(esqueleto, "mixamorig_Hips")).x * imagen.get_width() / root.get_visible_rect().size.x
	estudio.queue_free()
	root.transparent_bg = false
	root.size = tam_ventana
	await process_frame
	# Recorta a lo que se ve y lo ajusta al alto de la imagen del menú (los pies abajo), con la cadera en el centro:
	# si la lupa sobresale, la imagen se ensancha por los dos lados y Caneloso queda del mismo tamaño y en el mismo lugar
	imagen.convert(Image.FORMAT_RGBA8)
	var usado := imagen.get_used_rect()
	imagen = imagen.get_region(usado)
	var escala := float(TAM_MENU.y) / usado.size.y
	var centro := (cadera - usado.position.x) * escala
	var ancho := maxi(TAM_MENU.x, ceili(2.0 * maxf(centro, usado.size.x * escala - centro)))
	# Colores apagados de cine negro, como la imagen anterior del menú (con fondo transparente el ajuste del entorno no se aplica)
	for y in imagen.get_height():
		for x in imagen.get_width():
			var c := imagen.get_pixel(x, y)
			var gris := c.get_luminance()
			var apagado := Color(gris, gris, gris).lerp(c, MENU_SATURACION) * MENU_BRILLO
			apagado.a = c.a
			imagen.set_pixel(x, y, apagado)
	imagen.resize(maxi(1, roundi(usado.size.x * escala)), TAM_MENU.y, Image.INTERPOLATE_LANCZOS)
	var lienzo := Image.create_empty(ancho, TAM_MENU.y, false, Image.FORMAT_RGBA8)
	lienzo.blend_rect(imagen, Rect2i(Vector2i.ZERO, imagen.get_size()), Vector2i(roundi(ancho / 2.0 - centro), 0))
	lienzo.save_png(ruta)
	print("menú: ", ruta, " (", animacion, " a los ", segundo, " s)")

## Congela al personaje en el segundo dado de una animación (sin esto el modelo sale en pose T).
func _posar(modelo: Node, animacion: String, segundo: float) -> void:
	for animador in modelo.find_children("*", "AnimationPlayer", true, false):
		if animador.has_animation(animacion):
			animador.callback_mode_process = AnimationMixer.ANIMATION_CALLBACK_MODE_PROCESS_MANUAL
			animador.play(animacion)
			animador.seek(segundo, true)
			animador.advance(0)
			return
	push_error("No encontré la animación " + animacion)

## Lupa de Poly Haven en la mano derecha (BoneAttachment3D), con el mango en el puño y el cristal hacia arriba.
func _poner_lupa(esqueleto: Skeleton3D) -> void:
	var mano := BoneAttachment3D.new()
	mano.bone_name = "mixamorig_RightHand"
	esqueleto.add_child(mano)
	var lupa: Node3D = load(LUPA).instantiate()
	mano.add_child(lupa)
	# El cristal del modelo (transmisión) sale blanco y opaco: se cambia por un vidrio azulado casi transparente
	var vidrio := StandardMaterial3D.new()
	vidrio.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	vidrio.albedo_color = Color(0.63, 0.75, 0.84, 0.3)
	vidrio.roughness = 0.05
	vidrio.metallic_specular = 1.0
	for malla: MeshInstance3D in lupa.find_children("*", "MeshInstance3D", true, false):
		for i in malla.mesh.get_surface_count():
			var material := malla.mesh.surface_get_material(i) as BaseMaterial3D
			if material and material.transparency != BaseMaterial3D.TRANSPARENCY_DISABLED:
				malla.set_surface_override_material(i, vidrio)
	# El esqueleto no tiene dedos: el puño queda un poco más allá de la muñeca, siguiendo el antebrazo
	var muneca := _hueso(esqueleto, "mixamorig_RightHand")
	var palma := muneca + (muneca - _hueso(esqueleto, "mixamorig_RightForeArm")).normalized() * LUPA_PUNO
	# El modelo mide 27 cm con el mango abajo; se agranda para que se lea en la silueta y se inclina hacia afuera
	var base := Basis(Vector3.BACK, deg_to_rad(LUPA_GIRO)).scaled(Vector3.ONE * LUPA_ESCALA)
	lupa.global_transform = Transform3D(base, palma + LUPA_AJUSTE - base * Vector3(0, LUPA_AGARRE, 0))

func _hueso(esqueleto: Skeleton3D, nombre: String) -> Vector3:
	return esqueleto.global_transform * esqueleto.get_bone_global_pose(esqueleto.find_bone(nombre)).origin

## Recorta el centro en cuadrado y lo guarda a TAM × TAM.
func _guardar(imagen: Image, ruta: String) -> void:
	var lado := mini(imagen.get_width(), imagen.get_height())
	imagen = imagen.get_region(Rect2i((imagen.get_width() - lado) / 2, (imagen.get_height() - lado) / 2, lado, lado))
	imagen.resize(TAM, TAM, Image.INTERPOLATE_LANCZOS)
	imagen.save_png(ruta)
	print("retrato: ", ruta)
