class_name DetectiveHUD
extends CanvasLayer
## HUD transparente: no contiene escenario ni modifica al jugador.
signal panel_cambiado(abierto: bool, expediente: bool)
signal pista_seleccionada(pista: Pista)
signal detalle_solicitado(pista: Pista)
signal linterna_cambiada(encendida: bool)
@export var caso: String = "03"
@export var archivo: String = "24–B"
@export var objetivo: String = "Inspecciona el escritorio."
@export_range(0.2, 30.0) var segundos_inactividad: float = 3.0
@export var interaccion: String = ""
@export var rueda_habilitada: bool = true
var pistas: Array[Pista] = []
var indice_pista: int = 0
var _quieto: float = 0.0
var _moviendo: bool = false
var _ayuda_alpha: float = 1.0
var _aviso: float = 0.0
var _detalle: bool = false
var diario_abierto: bool = false
var linterna_encendida: bool = false
var _lienzo: Control
var _bold: FontVariation
var _medium: FontVariation
var _papel: ImageTexture
const DISPLAY = preload("res://hud/fonts/BigShouldersDisplay.ttf")
const MONO = preload("res://hud/fonts/IBMPlexMono-Regular.ttf")
const AMBAR = Color("efca08")
const BLANCO = Color("e8ebef")
const GRIS = Color("aeb6bf")
const TINTA = Color("20272b")
const PAPEL = Color("c6c1b1")

func _ready() -> void:
	_bold = _fuente(800)
	_medium = _fuente(500)
	_papel = _crear_papel()
	_lienzo = Control.new()
	_lienzo.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	_lienzo.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_lienzo)
	_lienzo.draw.connect(_dibujar)

func _fuente(peso: int) -> FontVariation:
	var f := FontVariation.new()
	f.base_font = DISPLAY
	f.variation_opentype = {2003265652: float(peso)}
	f.spacing_glyph = 2 if peso == 800 else 1
	return f

## Llamar cada frame desde el jugador con su velocidad real.
func actualizar_movimiento(en_movimiento: bool) -> void:
	_moviendo = en_movimiento
	if en_movimiento:
		_quieto = 0.0

func agregar_pista(pista: Pista) -> bool:
	if pista == null or pista.id == &"":
		return false
	for existente in pistas:
		if existente.id == pista.id:
			return false
	pistas.append(pista)
	_aviso = 3.0
	if pistas.size() == 1:
		pista_seleccionada.emit(pista)
	return true

# Puente para los objetos de pista escritos en C#.
func registrar_pista(id: String, titulo: String, descripcion: String) -> bool:
	var pista := Pista.new()
	pista.id = StringName(id)
	pista.titulo = titulo
	pista.descripcion = descripcion
	return agregar_pista(pista)

func cambiar_pista(paso: int) -> void:
	if pistas.is_empty():
		return
	indice_pista = posmod(indice_pista + paso, pistas.size())
	pista_seleccionada.emit(pistas[indice_pista])

func _process(delta: float) -> void:
	_quieto = 0.0 if _moviendo else _quieto + delta
	var destino := 0.0 if _moviendo or _quieto < segundos_inactividad else 1.0
	# Al iniciar, mantener ayuda visible hasta el primer movimiento.
	if not _moviendo and _ayuda_alpha == 1.0:
		destino = 1.0
	_ayuda_alpha = move_toward(_ayuda_alpha, destino, delta / 0.18)
	_aviso = maxf(0.0, _aviso - delta)
	_lienzo.queue_redraw()

func _input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.pressed and rueda_habilitada and not diario_abierto and pistas.size() > 1:
		if event.button_index == MOUSE_BUTTON_WHEEL_UP or event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			cambiar_pista(-1 if event.button_index == MOUSE_BUTTON_WHEEL_UP else 1)
			get_viewport().set_input_as_handled()
	if event is InputEventKey and event.pressed and not event.echo:
		if event.keycode in [KEY_UP, KEY_LEFT, KEY_DOWN, KEY_RIGHT]:
			if not diario_abierto and pistas.size() > 1:
				cambiar_pista(-1 if event.keycode in [KEY_UP, KEY_LEFT] else 1)
				get_viewport().set_input_as_handled()
			return
		match event.keycode:
			KEY_CTRL:
				_detalle = not _detalle
				diario_abierto = false
				if _detalle and not pistas.is_empty():
					detalle_solicitado.emit(pistas[indice_pista])
			KEY_Q:
				diario_abierto = not diario_abierto
				_detalle = false
			KEY_F:
				linterna_encendida = not linterna_encendida
				linterna_cambiada.emit(linterna_encendida)
			KEY_ESCAPE:
				if not _detalle and not diario_abierto:
					return
				_detalle = false
				diario_abierto = false
			_:
				return
		if event.keycode != KEY_F:
			panel_cambiado.emit(_detalle or diario_abierto, _detalle)
		get_viewport().set_input_as_handled()

func _texto(texto: String, p: Vector2, fuente: Font, tam: int, color: Color, ancho: float = -1) -> void:
	_lienzo.draw_string(fuente, p, texto, HORIZONTAL_ALIGNMENT_LEFT, ancho, tam, color)

func _parrafo(texto: String, p: Vector2, ancho: float, tam: int, color: Color, max_lineas: int = 4) -> void:
	var parrafo := TextParagraph.new()
	parrafo.add_string(texto, MONO, tam)
	parrafo.width = ancho
	parrafo.max_lines_visible = max_lineas
	parrafo.text_overrun_behavior = TextServer.OVERRUN_TRIM_ELLIPSIS
	parrafo.draw(_lienzo.get_canvas_item(), p, color)

func _linea(a: Vector2, b: Vector2, color: Color) -> void:
	_lienzo.draw_line(a, b, color, 1.0, true)

func _crear_papel() -> ImageTexture:
	# Material generado una sola vez: fibra, humedad y dobleces, sin ruido animado.
	var ruido := FastNoiseLite.new()
	ruido.seed = 2409
	ruido.frequency = 0.022
	var img := Image.create(512, 256, false, Image.FORMAT_RGBA8)
	for y in range(256):
		for x in range(512):
			var u := float(x) / 511.0
			var v := float(y) / 255.0
			var n := ruido.get_noise_2d(x, y)
			var grano := ruido.get_noise_2d(x * 19.0, y * 19.0)
			var borde := minf(minf(u, 1.0 - u), minf(v, 1.0 - v))
			var suciedad := exp(-borde * 32.0) * (0.20 + n * 0.18)
			var manchas := maxf(0.0, ruido.get_noise_2d(x * 0.6 + 800, y * 0.6) - 0.05) * 0.28
			var luz := 0.97 + n * 0.075 + grano * 0.04 - suciedad - manchas
			# Valles oscuros y crestas claras en pliegues diagonales y centrales.
			for distancia in [u - 0.48 + sin(v * 8.0) * 0.008, v - 0.55 + u * 0.06, v - u * 0.7 - 0.12, v + u * 0.5 - 0.85]:
				luz -= exp(-absf(distancia) * 155.0) * 0.12
				luz += exp(-absf(distancia - 0.012) * 140.0) * 0.07
			img.set_pixel(x, y, Color(luz, luz * 0.975, luz * 0.91, 1.0))
	return ImageTexture.create_from_image(img)

func _nota(rect: Rect2, color: Color = PAPEL) -> void:
	var puntos := PackedVector2Array()
	var uv := PackedVector2Array()
	# Contorno rasgado estable; el margen interior mantiene legible el texto.
	for lado in range(4):
		for i in range(24):
			var t := float(i) / 24.0
			var corte := 1.2 + absf(sin(i * 13.7 + lado * 3.4)) * 3.5
			if i % 11 == 4:
				corte += 3.0
			var local: Vector2
			match lado:
				0: local = Vector2(t * rect.size.x, corte)
				1: local = Vector2(rect.size.x - corte, t * rect.size.y)
				2: local = Vector2((1.0 - t) * rect.size.x, rect.size.y - corte)
				_: local = Vector2(corte, (1.0 - t) * rect.size.y)
			puntos.append(rect.position + local)
			uv.append(local / rect.size)
	var sombra := PackedVector2Array()
	for punto in puntos:
		sombra.append(punto + Vector2(4, 6))
	_lienzo.draw_colored_polygon(sombra, Color(0, 0, 0, color.a * 0.4))
	_lienzo.draw_polygon(puntos, PackedColorArray([color]), uv, _papel)
	puntos.append(puntos[0])
	_lienzo.draw_polyline(puntos, Color(0.28, 0.24, 0.16, color.a * 0.35), 1.0, true)
	# Cinta vieja semitransparente con extremos cortados.
	var cinta := rect.position + Vector2(rect.size.x * 0.42, -6)
	_lienzo.draw_colored_polygon(PackedVector2Array([cinta + Vector2(2, 0), cinta + Vector2(70, 2), cinta + Vector2(66, 19), cinta + Vector2(0, 17)]), Color(0.49, 0.45, 0.33, color.a * 0.5))

func _dibujar() -> void:
	var size := _lienzo.size
	var escala := minf(size.x / 1600.0, size.y / 900.0)
	_lienzo.draw_set_transform(Vector2.ZERO, 0, Vector2.ONE * escala)
	var w := size.x / escala
	var h := size.y / escala
	# Expediente compacto inspirado en la referencia, sin logotipo.
	var carpeta := Color("c1a259")
	_lienzo.draw_colored_polygon(PackedVector2Array([Vector2(57, 53), Vector2(62, 43), Vector2(79, 43), Vector2(85, 49), Vector2(106, 49), Vector2(106, 83), Vector2(57, 83)]), carpeta.darkened(0.22))
	_lienzo.draw_colored_polygon(PackedVector2Array([Vector2(53, 57), Vector2(112, 57), Vector2(106, 91), Vector2(58, 91)]), carpeta)
	_texto("CASO " + caso, Vector2(133, 59), _bold, 26, BLANCO)
	_linea(Vector2(133, 70), Vector2(335, 70), Color(BLANCO, 0.65))
	_texto("Archivo: " + archivo, Vector2(133, 98), MONO, 16, GRIS)
	# Objetivo en nota independiente.
	_nota(Rect2(w - 420, 42, 378, 137))
	_texto("OBJETIVO ACTUAL", Vector2(w - 398, 78), _bold, 26, TINTA)
	_lienzo.draw_rect(Rect2(w - 398, 87, 110, 4), AMBAR)
	_parrafo(objetivo, Vector2(w - 398, 104), 335, 15, TINTA, 3)
	# Notificación temporal, sin bloquear la vista.
	if _aviso > 0:
		var a := minf(_aviso / 0.3, 1.0)
		_nota(Rect2(w / 2 - 185, 52, 370, 100), Color(PAPEL, a))
		_texto("EXPEDIENTE ACTUALIZADO", Vector2(w / 2 - 157, 91), _bold, 28, Color(TINTA, a))
		_texto("+1 PISTA", Vector2(w / 2 - 42, 129), MONO, 17, Color(TINTA, a))
	# Retícula e interacción opcional.
	_lienzo.draw_circle(Vector2(w / 2, h / 2), 2, Color(BLANCO, 0.65))
	if not interaccion.is_empty():
		_texto("[ E ]  " + interaccion, Vector2(w / 2 - 75, h / 2 + 42), _bold, 26, BLANCO)
	_dibujar_pistas(w, h)
	if _detalle:
		_dibujar_expediente(w, h)
	elif diario_abierto:
		_lienzo.draw_rect(Rect2(0, 0, w, h), Color(0.02, 0.04, 0.06, 0.85))
		var origen := Vector2(w / 2 - 420, h / 2 - 270)
		_nota(Rect2(origen, Vector2(840, 540)))
		_texto("DIARIO", origen + Vector2(35, 55), _bold, 34, TINTA)
		_linea(origen + Vector2(420, 30), origen + Vector2(420, 490), Color(TINTA, 0.22))

func _dibujar_expediente(w: float, h: float) -> void:
	_lienzo.draw_rect(Rect2(0, 0, w, h), Color(0.02, 0.04, 0.06, 0.85))
	var origen := Vector2(w / 2 - 500, h / 2 - 275)
	_nota(Rect2(origen, Vector2(1000, 550)))
	_texto("EXPEDIENTE / PISTAS", origen + Vector2(32, 53), _bold, 34, TINTA)
	_texto("%02d REUNIDAS" % pistas.size(), origen + Vector2(780, 49), MONO, 13, TINTA)
	_linea(origen + Vector2(32, 72), origen + Vector2(966, 72), Color(TINTA, 0.3))
	if pistas.is_empty():
		_texto("Todavía no has reunido pistas.", origen + Vector2(32, 130), MONO, 18, TINTA)
	else:
		# Siete filas por página; todas las evidencias son accesibles con la rueda.
		var inicio := (indice_pista / 7) * 7
		for i in range(inicio, mini(inicio + 7, pistas.size())):
			var fila := origen + Vector2(32, 93 + (i - inicio) * 51)
			if i == indice_pista:
				_lienzo.draw_rect(Rect2(fila, Vector2(340, 42)), Color(TINTA, 0.12))
				_lienzo.draw_rect(Rect2(fila, Vector2(3, 42)), AMBAR)
			_texto("%02d" % (i + 1), fila + Vector2(12, 28), MONO, 13, TINTA)
			_texto(pistas[i].titulo, fila + Vector2(47, 29), _bold, 24, TINTA, 283)
		_linea(origen + Vector2(396, 93), origen + Vector2(396, 472), Color(TINTA, 0.25))
		var pista := pistas[indice_pista]
		_texto(pista.titulo, origen + Vector2(425, 123), _bold, 32, TINTA, 535)
		_parrafo(pista.descripcion, origen + Vector2(425, 146), 530, 18, TINTA, 8)
		if pista.imagen:
			var medida := pista.imagen.get_size()
			medida *= minf(220.0 / medida.x, 130.0 / medida.y)
			_lienzo.draw_texture_rect(pista.imagen, Rect2(origen + Vector2(425, 345), medida), false)
		_texto("%02d / %02d" % [indice_pista + 1, pistas.size()], origen + Vector2(32, 479), MONO, 12, TINTA)

func _dibujar_pistas(w: float, h: float) -> void:
	var p := Vector2(w - 405, h - 220)
	for i in range(mini(pistas.size() - 1, 3), 0, -1):
		_nota(Rect2(p + Vector2(i * 6, -i * 9), Vector2(355, 180)), PAPEL.darkened(i * 0.07))
	_nota(Rect2(p, Vector2(355, 180)))
	var contador := "%02d / %02d" % [indice_pista + 1, pistas.size()] if not pistas.is_empty() else "00 / 00"
	_texto("PISTAS", p + Vector2(22, 39), _bold, 30, TINTA)
	_texto(contador, p + Vector2(246, 36), MONO, 13, TINTA)
	_lienzo.draw_rect(Rect2(p + Vector2(22, 48), Vector2(120, 4)), AMBAR)
	if pistas.is_empty():
		_texto("SIN EVIDENCIA", p + Vector2(22, 94), _bold, 28, TINTA)
		_texto("Explora para reunir pistas.", p + Vector2(22, 124), MONO, 12, TINTA)
	else:
		var pista := pistas[indice_pista]
		var ancho := 210.0 if pista.imagen else 311.0
		_texto(pista.titulo, p + Vector2(22, 93), _bold, 30, TINTA, ancho)
		if pista.imagen:
			_lienzo.draw_rect(Rect2(p + Vector2(248, 57), Vector2(88, 83)), BLANCO)
			var zona := Rect2(p + Vector2(254, 63), Vector2(76, 64))
			var factor := minf(zona.size.x / pista.imagen.get_width(), zona.size.y / pista.imagen.get_height())
			var medida := pista.imagen.get_size() * factor
			_lienzo.draw_texture_rect(pista.imagen, Rect2(zona.position + (zona.size - medida) / 2, medida), false)
	_linea(p + Vector2(22, 141), p + Vector2(333, 141), Color(TINTA, 0.2))
