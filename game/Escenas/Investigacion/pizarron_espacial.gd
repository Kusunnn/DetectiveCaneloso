extends Node3D
## Superficie real del escenario. La cámara se desplaza sin girar ni mover al detective.
const MEDIDA := Vector2(4.4, 2.475)
const RESOLUCION := Vector2(1600, 900)
var hud: Node
var camara: Camera3D
var camara_anterior: Camera3D
var superficie: Control
var desplazamiento := Vector2.ZERO
var desplazamiento_objetivo := Vector2.ZERO
var _distancia := 2.5
var abierto := false
var moviendo_vista := false
var _cursor := Vector2.ZERO
var _viewport: SubViewport

func _ready() -> void:
	_viewport = SubViewport.new()
	_viewport.size = Vector2i(RESOLUCION * 2)
	_viewport.disable_3d = true
	_viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	add_child(_viewport)
	superficie = Control.new()
	superficie.size = RESOLUCION * 2
	superficie.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_viewport.add_child(superficie)
	superficie.draw.connect(func() -> void: hud.dibujar_superficie_pizarron(superficie))
	var marco := MeshInstance3D.new()
	var caja := BoxMesh.new()
	caja.size = Vector3(MEDIDA.x + 0.14, MEDIDA.y + 0.14, 0.10)
	marco.mesh = caja
	var madera := StandardMaterial3D.new()
	madera.albedo_color = Color("73553b")
	marco.material_override = madera
	marco.position.z = -0.065
	add_child(marco)
	var panel := MeshInstance3D.new()
	var quad := QuadMesh.new()
	quad.size = MEDIDA
	panel.mesh = quad
	var material := StandardMaterial3D.new()
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.albedo_texture = _viewport.get_texture()
	panel.material_override = material
	add_child(panel)
	camara = Camera3D.new()
	camara.projection = Camera3D.PROJECTION_PERSPECTIVE
	camara.fov = 55
	camara.position = Vector3(0, 0, 1.9)
	camara.near = 0.05
	add_child(camara)

func abrir() -> void:
	camara_anterior = get_viewport().get_camera_3d()
	desplazamiento = Vector2.ZERO
	desplazamiento_objetivo = Vector2.ZERO
	camara.rotation = Vector3.ZERO
	_cursor = get_viewport().get_visible_rect().size / 2
	abierto = true
	_ajustar_encuadre()
	_actualizar_vista(1.0)
	camara.make_current()

func cerrar() -> void:
	abierto = false
	moviendo_vista = false
	if is_instance_valid(camara_anterior): camara_anterior.make_current()
	else: camara.clear_current()

func _ajustar_encuadre() -> void:
	var medida := get_viewport().get_visible_rect().size
	var alto := maxf(MEDIDA.y + 0.18, (MEDIDA.x + 0.18) / (medida.x / medida.y))
	_distancia = alto / (2 * tan(deg_to_rad(camara.fov / 2)))

func _process(delta: float) -> void:
	superficie.queue_redraw()
	if not abierto: return
	_ajustar_encuadre()
	# El cursor queda libre para las pistas. Acercarlo a un borde recorre el tablero.
	var medida := get_viewport().get_visible_rect().size
	var movimiento := Vector2.ZERO
	var margen := 70.0
	if not moviendo_vista and not (hud.relaciones_pizarron.hilo_desde != &"" or hud.relaciones_pizarron.moviendo or hud.relaciones_pizarron._nota_tinta != &""):
		# La velocidad crece hacia el borde para evitar arranques bruscos.
		movimiento.x = -clampf((margen - _cursor.x) / margen, 0, 1) + clampf((_cursor.x - medida.x + margen) / margen, 0, 1)
		movimiento.y = clampf((margen - _cursor.y) / margen, 0, 1) - clampf((_cursor.y - medida.y + margen) / margen, 0, 1)
		desplazar(movimiento.limit_length() * delta * 0.9)
	if (hud.relaciones_pizarron.hilo_desde != &"" or hud.relaciones_pizarron.moviendo or hud.relaciones_pizarron._nota_tinta != &""):
		# Mantener la vista estable al tender un hilo o arrastrar una tarjeta.
		desplazamiento_objetivo = desplazamiento
	_actualizar_vista(delta)

func desplazar(paso: Vector2) -> void:
	desplazamiento_objetivo = (desplazamiento_objetivo + paso).clamp(Vector2(-0.55, -1.05), Vector2(0.55, 0.35))

func _actualizar_vista(delta: float) -> void:
	desplazamiento = desplazamiento.lerp(desplazamiento_objetivo, 1.0 - exp(-delta * 12.0))
	camara.position = Vector3(desplazamiento.x, desplazamiento.y, _distancia)
	# Pequeña inclinación de la mirada: el corcho conserva profundidad al recorrerlo.
	camara.rotation = Vector3(-desplazamiento.y * 0.035, desplazamiento.x * 0.08, 0)

func recibir_raton(evento: InputEvent) -> void:
	if evento is InputEventMouseMotion:
		_cursor = evento.position
		if moviendo_vista:
			var alto := get_viewport().get_visible_rect().size.y
			desplazar(Vector2(-evento.relative.x, evento.relative.y) * (2 * _distancia * tan(deg_to_rad(camara.fov / 2))) / alto)
	elif evento is InputEventMouseButton and evento.button_index == MOUSE_BUTTON_MIDDLE:
		moviendo_vista = evento.pressed

func pantalla_a_pizarron(pantalla: Vector2) -> Vector2:
	var origen := to_local(camara.project_ray_origin(pantalla))
	var direccion := global_basis.inverse() * camara.project_ray_normal(pantalla)
	if absf(direccion.z) < 0.001: return Vector2(-1, -1)
	var punto := origen - direccion * (origen.z / direccion.z)
	return Vector2((punto.x / MEDIDA.x + 0.5) * RESOLUCION.x, (0.5 - punto.y / MEDIDA.y) * RESOLUCION.y)

func pizarron_a_pantalla(punto: Vector2) -> Vector2:
	var local := Vector3((punto.x / RESOLUCION.x - 0.5) * MEDIDA.x, (0.5 - punto.y / RESOLUCION.y) * MEDIDA.y, 0)
	return camara.unproject_position(to_global(local))
