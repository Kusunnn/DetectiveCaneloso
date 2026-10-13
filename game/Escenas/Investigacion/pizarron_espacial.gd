extends Node3D
## Superficie real del escenario. La cámara se desplaza sin girar ni mover al detective.
const MEDIDA := Vector2(4.4, 2.475)
const RESOLUCION := Vector2(1600, 900)
var hud: Node
var camara: Camera3D
var camara_anterior: Camera3D
var superficie: Control
var desplazamiento := Vector2.ZERO
var abierto := false
var moviendo_vista := false
var _cursor := Vector2.ZERO
var _viewport: SubViewport

func _ready() -> void:
	_viewport = SubViewport.new()
	_viewport.size = Vector2i(RESOLUCION)
	_viewport.disable_3d = true
	_viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	add_child(_viewport)
	superficie = Control.new()
	superficie.size = RESOLUCION
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
	camara.projection = Camera3D.PROJECTION_ORTHOGONAL
	camara.size = 2.5
	camara.position = Vector3(0, 0, 1.9)
	camara.near = 0.05
	add_child(camara)

func abrir() -> void:
	camara_anterior = get_viewport().get_camera_3d()
	desplazamiento = Vector2.ZERO
	camara.position = Vector3(0, 0, 1.9)
	_cursor = get_viewport().get_visible_rect().size / 2
	abierto = true
	_ajustar_encuadre()
	camara.make_current()

func cerrar() -> void:
	abierto = false
	moviendo_vista = false
	if is_instance_valid(camara_anterior): camara_anterior.make_current()
	else: camara.clear_current()

func _ajustar_encuadre() -> void:
	var medida := get_viewport().get_visible_rect().size
	camara.size = maxf(MEDIDA.y + 0.12, (MEDIDA.x + 0.12) / (medida.x / medida.y))

func _process(delta: float) -> void:
	superficie.queue_redraw()
	if not abierto: return
	_ajustar_encuadre()
	# El cursor queda libre para las pistas. Acercarlo a un borde recorre el tablero.
	var medida := get_viewport().get_visible_rect().size
	var movimiento := Vector2.ZERO
	var margen := 42.0
	if not moviendo_vista:
		if _cursor.x < margen: movimiento.x = -1
		elif _cursor.x > medida.x - margen: movimiento.x = 1
		if _cursor.y < margen: movimiento.y = 1
		elif _cursor.y > medida.y - margen: movimiento.y = -1
		desplazar(movimiento * delta * 0.9)

func desplazar(paso: Vector2) -> void:
	desplazamiento = (desplazamiento + paso).clamp(Vector2(-0.55, -1.05), Vector2(0.55, 0.35))
	camara.position = Vector3(desplazamiento.x, desplazamiento.y, 1.9)

func recibir_raton(evento: InputEvent) -> void:
	if evento is InputEventMouseMotion:
		_cursor = evento.position
		if moviendo_vista:
			var alto := get_viewport().get_visible_rect().size.y
			desplazar(Vector2(-evento.relative.x, evento.relative.y) * camara.size / alto)
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
