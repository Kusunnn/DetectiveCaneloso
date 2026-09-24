extends Control
## Fondo animado del menú principal: callejón con una farola que parpadea y una puerta
## en la que, durante los apagones, aparece la silueta de alguien. Se genera por código.
## Diseño: "Detective Caneloso – Inicio" (Claude Design).

## Frecuencia de los parpadeos y apagones de la farola.
@export_enum("baja", "media", "alta") var flicker_intensity := "media"
## PNG opcional para sustituir la silueta dibujada por código.
@export var silhouette_texture: Texture2D

const SHADOW := Color("#050608")
const GRAIN_SHADER := preload("res://scenes/ui/menus/background/film_grain.gdshader")

## Por intensidad: [probabilidad de parpadeo, probabilidad de apagón con silueta].
const FLICKER_LEVELS := {"baja": [0.1, 0.03], "media": [0.22, 0.08], "alta": [0.38, 0.16]}

var _cone: TextureRect
var _glow: TextureRect
var _lamp: Panel
var _darkness: ColorRect
var _door: Control
var _silhouette: Control

## Pasos pendientes de la animación de luz: [nivel de luz 0..1, duración en ms].
var _steps: Array = []
var _step_time_left := 0.0


func _ready() -> void:
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	_build()


func _process(delta: float) -> void:
	_step_time_left -= delta
	if _step_time_left > 0.0:
		return
	if _steps.is_empty():
		_steps = _next_sequence()
	var step: Array = _steps.pop_front()
	_set_light(step[0])
	_step_time_left = step[1] / 1000.0


# ---------- Animación de la luz ----------

func _next_sequence() -> Array:
	var level: Array = FLICKER_LEVELS[flicker_intensity]
	var r := randf()
	if r < level[1]:
		# Apagón largo: la silueta aparece en un punto aleatorio de la puerta.
		var x := randf_range(0.3, 0.7)
		_silhouette.anchor_left = x - 0.24
		_silhouette.anchor_right = x + 0.24
		return [[0.2, 60], [1.0, 70], [0.05, 90], [0.8, 40], [0.0, randf_range(1400, 2800)],
				[0.6, 50], [0.0, 120], [1.0, 80], [0.3, 50], [1.0, 400]]
	if r < level[0]:
		return [[0.3, 50], [1.0, 90], [0.15, 70], [0.95, 60], [0.4, 40], [1.0, 300]]
	return [[randf_range(0.88, 1.0), randf_range(700, 2400)]]


func _set_light(value: float) -> void:
	_lamp.modulate.a = 0.15 + 0.85 * value
	_glow.modulate.a = value
	_cone.modulate.a = value
	_darkness.modulate.a = (1.0 - value) * 0.82
	_silhouette.modulate.a = 0.95 if value < 0.35 else 0.0
	_door.modulate.a = 0.5 + (1.0 - value) * 0.2 + randf() * 0.05


# ---------- Construcción de la escena ----------

func _build() -> void:
	var top := Vector2(0, 0)
	var bottom := Vector2(0, 1)

	# Cielo y suelo
	_texture_rect(self, _gradient([Color("#101318"), Color("#0c0e12"), Color("#08090b")], [0.0, 0.7, 1.0], false, top, bottom), 0, 0, 1, 1)
	_texture_rect(self, _gradient([Color("#07080a"), Color("#0d0f13")], [0.0, 1.0], false, top, bottom), 0, 0.8, 1, 1)
	var horizon := ColorRect.new()
	horizon.color = Color(1, 1, 1, 0.05)
	_place(self, horizon, 0, 0.8, 1, 0.8)
	horizon.offset_bottom = 1

	# Farola: cono de luz, cable, brillo y lámpara
	var light := Color(0.84, 0.87, 0.91)
	_cone = _texture_rect(self, _gradient([Color(light, 0.16), Color(light, 0.05), Color(light, 0.0)], [0.0, 0.45, 1.0], true, Vector2(0.5, 0.24), Vector2(0.5, 0.64)), 0.17, -0.1, 1.07, 1.0)

	var cable := ColorRect.new()
	cable.color = Color("#2a2f36")
	_place(self, cable, 0.62, 0, 0.62, 0.22)
	cable.offset_right = 1

	_glow = _texture_rect(self, _gradient([Color(0.9, 0.93, 0.96, 0.45), Color(0.9, 0.93, 0.96, 0.0)], [0.0, 1.0], true, Vector2(0.5, 0.5), Vector2(1.0, 0.5)), 0.62, 0.22, 0.62, 0.22)
	_glow.offset_left = -90
	_glow.offset_right = 90
	_glow.offset_top = -80
	_glow.offset_bottom = 100

	_lamp = Panel.new()
	var lamp_style := StyleBoxFlat.new()
	lamp_style.bg_color = Color("#f1f4f7")
	lamp_style.corner_radius_top_left = 6
	lamp_style.corner_radius_top_right = 6
	lamp_style.corner_radius_bottom_left = 8
	lamp_style.corner_radius_bottom_right = 8
	_lamp.add_theme_stylebox_override("panel", lamp_style)
	_place(self, _lamp, 0.62, 0.22, 0.62, 0.22)
	_lamp.offset_left = -8
	_lamp.offset_right = 8
	_lamp.offset_bottom = 22

	_darkness = ColorRect.new()
	_darkness.color = Color("#030405")
	_darkness.modulate.a = 0.0
	_place(self, _darkness, 0, 0, 1, 1)

	# Puerta iluminada con la silueta
	var door_grey := Color(0.51, 0.59, 0.67)
	_texture_rect(self, _gradient([Color(door_grey, 0.18), Color(door_grey, 0.0)], [0.0, 1.0], true, Vector2(0.5, 0.5), Vector2(1.0, 0.5)), 0.68, 0.16, 0.93, 0.88)

	_door = Control.new()
	_door.clip_contents = true
	_door.modulate.a = 0.55
	_place(self, _door, 0.74, 0.24, 0.87, 0.8)
	_texture_rect(_door, _gradient([Color("#9fb0c2"), Color("#6f8193"), Color("#4b5968")], [0.0, 0.6, 1.0], false, top, bottom), 0, 0, 1, 1)

	_silhouette = Control.new()
	_silhouette.modulate.a = 0.0
	_place(_door, _silhouette, 0.26, 0.2, 0.74, 1.0)
	if silhouette_texture:
		var tex := _texture_rect(_silhouette, silhouette_texture, 0, 0, 1, 1)
		tex.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	else:
		_silhouette.draw.connect(_draw_silhouette)
		_silhouette.resized.connect(_silhouette.queue_redraw)

	# Reflejo de la puerta en el suelo, viñeta y grano
	_texture_rect(self, _gradient([Color(door_grey, 0.16), Color(door_grey, 0.0)], [0.0, 1.0], false, top, bottom), 0.72, 0.8, 0.89, 1.0)
	_texture_rect(self, _gradient([Color(0, 0, 0, 0), Color(0, 0, 0, 0), Color(0, 0, 0, 0.78)], [0.0, 0.4, 1.0], true, Vector2(0.55, 0.45), Vector2(0.55, 1.15)), 0, 0, 1, 1)

	var grain := ColorRect.new()
	var grain_material := ShaderMaterial.new()
	grain_material.shader = GRAIN_SHADER
	grain.material = grain_material
	_place(self, grain, 0, 0, 1, 1)


func _draw_silhouette() -> void:
	var s := _silhouette.size
	_silhouette.draw_rect(Rect2(s.x * 0.30, s.y * 0.01, s.x * 0.40, s.y * 0.09), SHADOW)  # sombrero
	_draw_ellipse(Rect2(s.x * 0.12, s.y * 0.085, s.x * 0.76, s.y * 0.03))  # ala
	_draw_ellipse(Rect2(s.x * 0.33, s.y * 0.10, s.x * 0.34, s.y * 0.15))  # cabeza
	var y0 := s.y * 0.23
	var h := s.y * 0.77
	var body := PackedVector2Array([Vector2(0.32, 0), Vector2(0.68, 0), Vector2(0.96, 0.18),
			Vector2(0.92, 1), Vector2(0.08, 1), Vector2(0.04, 0.18)])
	for i in body.size():
		body[i] = Vector2(body[i].x * s.x, y0 + body[i].y * h)
	_silhouette.draw_colored_polygon(body, SHADOW)


func _draw_ellipse(rect: Rect2) -> void:
	var points := PackedVector2Array()
	var center := rect.get_center()
	for i in 24:
		var angle := TAU * i / 24.0
		points.append(center + Vector2(cos(angle) * rect.size.x / 2.0, sin(angle) * rect.size.y / 2.0))
	_silhouette.draw_colored_polygon(points, SHADOW)


# ---------- Utilidades ----------

## Añade un control anclado por proporciones (0..1) que no bloquea el ratón.
func _place(parent: Node, control: Control, left: float, top: float, right: float, bottom: float) -> Control:
	control.anchor_left = left
	control.anchor_top = top
	control.anchor_right = right
	control.anchor_bottom = bottom
	control.offset_left = 0
	control.offset_top = 0
	control.offset_right = 0
	control.offset_bottom = 0
	control.mouse_filter = Control.MOUSE_FILTER_IGNORE
	parent.add_child(control)
	return control


func _texture_rect(parent: Node, texture: Texture2D, left: float, top: float, right: float, bottom: float) -> TextureRect:
	var rect := TextureRect.new()
	rect.texture = texture
	rect.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	rect.stretch_mode = TextureRect.STRETCH_SCALE
	_place(parent, rect, left, top, right, bottom)
	return rect


func _gradient(colors: Array, offsets: Array, radial: bool, from: Vector2, to: Vector2) -> GradientTexture2D:
	var gradient := Gradient.new()
	gradient.offsets = PackedFloat32Array(offsets)
	gradient.colors = PackedColorArray(colors)
	var texture := GradientTexture2D.new()
	texture.gradient = gradient
	texture.width = 256
	texture.height = 256
	if radial:
		texture.fill = GradientTexture2D.FILL_RADIAL
	texture.fill_from = from
	texture.fill_to = to
	return texture
