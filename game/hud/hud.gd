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
const DURACION_CAMBIO_PISTA := 0.24
var _pista_anterior: int = 0
var _cambio_pista: float = 0.0
var _sentido_pista: int = 1
var _quieto: float = 0.0
var _moviendo: bool = false
var _ayuda_alpha: float = 1.0
var _aviso: float = 0.0
var _detalle: bool = false
var diario_abierto: bool = false
var linterna_encendida: bool = false
## Diario del caso (Q): pistas, deducciones, testimonios y personas.
var notas: Array[String] = []
var personas: Array[Dictionary] = []
const PISTAS_POR_HOJA := 9
var _pista_diario: int = -1
var _seccion: String = "PISTAS"
var _hojas_seccion: Dictionary = {"PISTAS": 0, "DEDUCCIONES": 0, "PERSONAS": 0}
const COLORES_SECCION := {"PISTAS": Color("dbb64f"), "DEDUCCIONES": Color("876348"), "PERSONAS": Color("993f40")}
var _papeles_libro: Array[ImageTexture] = []
var _hoja: int = 0
var _giro: float = 0.0
var _direccion: int = 1
var _zonas_libro: Array[Dictionary] = []

func registrar_persona(nombre: String, resumen: String, retrato: Texture2D) -> void:
	for persona in personas:
		if persona.nombre == nombre:
			return
	personas.append({"nombre": nombre, "resumen": resumen, "imagen": retrato})

func _paginas_libro() -> Array[Dictionary]:
	var paginas: Array[Dictionary] = []
	# Cada hoja de fotos comparte una sola página de detalle.
	for grupo in range(maxi(1, ceili(pistas.size() / float(PISTAS_POR_HOJA)))):
		var inicio := grupo * PISTAS_POR_HOJA
		paginas.append({"seccion": "PISTAS", "titulo": "Pistas reunidas", "galeria": inicio})
		if _pista_diario >= inicio and _pista_diario < mini(inicio + PISTAS_POR_HOJA, pistas.size()):
			var pista := pistas[_pista_diario]
			paginas.append({"seccion": "PISTAS", "titulo": pista.titulo, "texto": pista.descripcion, "imagen": pista.imagen})
		else:
			paginas.append({"seccion": "PISTAS", "titulo": "Una mirada más de cerca", "texto": "Haz clic en una fotografía para consultar la pista." if not pistas.is_empty() else "Las pistas que encuentres aparecerán en la hoja de al lado."})
	var conclusiones := notas.filter(func(n: String) -> bool: return n.begins_with("Deducción: "))
	for conclusion in conclusiones:
		paginas.append({"seccion": "DEDUCCIONES", "titulo": "Una conexión confirmada", "texto": conclusion.trim_prefix("Deducción: ")})
	if conclusiones.is_empty():
		paginas.append({"seccion": "DEDUCCIONES", "titulo": "Todo está por conectar", "texto": "Une las pistas en el tablero [R]. Las deducciones confirmadas quedarán anotadas aquí."})
	for nota in notas:
		if not nota.begins_with("Deducción: "):
			paginas.append({"seccion": "DEDUCCIONES", "titulo": "Notas de investigación", "texto": nota})
	for persona in personas:
		paginas.append({"seccion": "PERSONAS", "titulo": persona.nombre, "texto": persona.resumen, "imagen": persona.imagen})
	if personas.is_empty():
		paginas.append({"seccion": "PERSONAS", "titulo": "Personas del caso", "texto": "Habla con las personas del caso para añadir sus fichas al diario."})
	return paginas.filter(func(pagina: Dictionary) -> bool: return pagina.seccion == _seccion)

func _cambiar_seccion(seccion: String) -> void:
	if seccion == _seccion or not COLORES_SECCION.has(seccion):
		return
	_hojas_seccion[_seccion] = _hoja
	_seccion = seccion
	_hoja = clampi(_hojas_seccion[seccion], 0, (_paginas_libro().size() - 1) / 2)
	_direccion = 1
	_giro = 1.0
	_zonas_libro.clear()

func _pasar_hoja(destino: int) -> void:
	var nueva := clampi(destino, 0, (_paginas_libro().size() - 1) / 2)
	if nueva == _hoja:
		return
	_direccion = 1 if nueva > _hoja else -1
	_hoja = nueva
	_giro = 1.0

var _aviso_titulo := "EXPEDIENTE ACTUALIZADO"
var _aviso_detalle := "+1 PISTA"
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
	for i in range(4):
		_papeles_libro.append(_crear_papel_libro(i))
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
	avisar("EXPEDIENTE ACTUALIZADO", "+1 PISTA")
	if pistas.size() == 1:
		pista_seleccionada.emit(pista)
	return true

## Añade una nota a la libreta (Q) y muestra el aviso. Devuelve false si ya estaba.
func agregar_nota(texto: String) -> bool:
	if texto.is_empty() or notas.has(texto):
		return false
	notas.append(texto)
	avisar("LIBRETA ACTUALIZADA", "+1 NOTA")
	return true

## Aviso superior central (mismo estilo para pistas, notas y deducciones).
func avisar(titulo: String, detalle: String) -> void:
	_aviso_titulo = titulo
	_aviso_detalle = detalle
	_aviso = 3.0

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
	var siguiente := posmod(indice_pista + paso, pistas.size())
	if siguiente == indice_pista:
		return
	_pista_anterior = indice_pista
	_sentido_pista = 1 if paso > 0 else -1
	_cambio_pista = DURACION_CAMBIO_PISTA
	indice_pista = siguiente
	pista_seleccionada.emit(pistas[indice_pista])

func _process(delta: float) -> void:
	_cambio_pista = maxf(0.0, _cambio_pista - delta)
	_giro = move_toward(_giro, 0.0, delta * 4.5)
	_quieto = 0.0 if _moviendo else _quieto + delta
	var destino := 0.0 if _moviendo or _quieto < segundos_inactividad else 1.0
	# Al iniciar, mantener ayuda visible hasta el primer movimiento.
	if not _moviendo and _ayuda_alpha == 1.0:
		destino = 1.0
	_ayuda_alpha = move_toward(_ayuda_alpha, destino, delta / 0.18)
	_aviso = maxf(0.0, _aviso - delta)
	_lienzo.queue_redraw()

func _input(event: InputEvent) -> void:
	if diario_abierto and event is InputEventMouseButton and event.pressed:
		if event.button_index == MOUSE_BUTTON_LEFT:
			var escala := minf(_lienzo.size.x / 1600.0, _lienzo.size.y / 900.0)
			for zona in _zonas_libro:
				if zona.rect.has_point(event.position / escala):
					if zona.has("pista"):
						_pista_diario = zona.pista
					elif zona.has("seccion"):
						_cambiar_seccion(zona.seccion)
					elif zona.destino == -1:
						diario_abierto = false
						panel_cambiado.emit(false, false)
					else:
						_pasar_hoja(zona.destino)
					break
		elif event.button_index in [MOUSE_BUTTON_WHEEL_UP, MOUSE_BUTTON_WHEEL_DOWN]:
			_pasar_hoja(_hoja + (-1 if event.button_index == MOUSE_BUTTON_WHEEL_UP else 1))
		get_viewport().set_input_as_handled()
		return
	if event is InputEventMouseButton and event.pressed and rueda_habilitada and not diario_abierto and pistas.size() > 1:
		if event.button_index == MOUSE_BUTTON_WHEEL_UP or event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			cambiar_pista(-1 if event.button_index == MOUSE_BUTTON_WHEEL_UP else 1)
			get_viewport().set_input_as_handled()
	if event is InputEventKey and event.pressed and not event.echo:
		if event.keycode in [KEY_UP, KEY_LEFT, KEY_DOWN, KEY_RIGHT]:
			if diario_abierto:
				_pasar_hoja(_hoja + (-1 if event.keycode in [KEY_UP, KEY_LEFT] else 1))
				get_viewport().set_input_as_handled()
			elif pistas.size() > 1:
				cambiar_pista(-1 if event.keycode in [KEY_UP, KEY_LEFT] else 1)
				get_viewport().set_input_as_handled()
			return
		match event.keycode:
			KEY_TAB:
				_detalle = not _detalle
				diario_abierto = false
				if _detalle and not pistas.is_empty():
					detalle_solicitado.emit(pistas[indice_pista])
			KEY_Q:
				diario_abierto = not diario_abierto
				if diario_abierto:
					_giro = 1.0
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

## Como _texto, pero si no cabe en el ancho baja la fuente (mínimo 60 %) en lugar de cortar el texto.
func _texto_ajustado(texto: String, p: Vector2, fuente: Font, tam: int, color: Color, ancho: float) -> void:
	var t := tam
	while t > int(tam * 0.6) and fuente.get_string_size(texto, HORIZONTAL_ALIGNMENT_LEFT, -1, t).x > ancho:
		t -= 1
	_texto(texto, p, fuente, t, color, ancho)

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

# Papel de encuadernación: grano fino y tonos suaves, sin arrugas ni manchas.
func _crear_papel_libro(variante: int) -> ImageTexture:
	var ruido := FastNoiseLite.new()
	ruido.seed = 781 + variante * 97
	ruido.frequency = 0.38
	var img := Image.create(256, 256, false, Image.FORMAT_RGBA8)
	var base: Color = [Color("f5f0e4"), Color("f3eddf"), Color("f6f1e7"), Color("f1ebdf")][variante]
	for y in range(256):
		for x in range(256):
			var grano := ruido.get_noise_2d(x, y) * 0.012
			img.set_pixel(x, y, Color(base.r + grano, base.g + grano, base.b + grano))
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
		var ancho_titulo := _bold.get_string_size(_aviso_titulo, HORIZONTAL_ALIGNMENT_LEFT, -1, 28).x
		var ancho_detalle := MONO.get_string_size(_aviso_detalle, HORIZONTAL_ALIGNMENT_LEFT, -1, 17).x
		_texto_ajustado(_aviso_titulo, Vector2(w / 2 - minf(ancho_titulo, 320) / 2, 91), _bold, 28, Color(TINTA, a), 320)
		_texto(_aviso_detalle, Vector2(w / 2 - ancho_detalle / 2, 129), MONO, 17, Color(TINTA, a))
	# Retícula e interacción opcional.
	_lienzo.draw_circle(Vector2(w / 2, h / 2), 2, Color(BLANCO, 0.65))
	if not interaccion.is_empty():
		# Anillo ámbar: la retícula está sobre algo que se puede usar
		_lienzo.draw_arc(Vector2(w / 2, h / 2), 13, 0, TAU, 32, Color(AMBAR, 0.9), 2.0, true)
		_texto("[ E ]  " + interaccion, Vector2(w / 2 - 75, h / 2 + 42), _bold, 26, BLANCO)
	_dibujar_pistas(w, h)
	if _detalle:
		_dibujar_expediente(w, h)
	elif diario_abierto:
		_dibujar_libro(w, h)

func _boton_libro(texto: String, rect: Rect2, destino: int, activo: bool = true) -> void:
	var escala := minf(_lienzo.size.x / 1600.0, _lienzo.size.y / 900.0)
	var hover := activo and rect.has_point(_lienzo.get_local_mouse_position() / escala)
	_lienzo.draw_rect(rect, Color("b99a55") if hover else Color("483b30"))
	_texto(texto, rect.position + Vector2(16, 28), MONO, 15, BLANCO if activo else Color("8c8275"))
	if activo:
		_zonas_libro.append({"rect": rect, "destino": destino})

func _dibujar_libro(w: float, h: float) -> void:
	_zonas_libro.clear()
	var paginas := _paginas_libro()
	_hoja = clampi(_hoja, 0, (paginas.size() - 1) / 2)
	var origen := Vector2(w / 2 - 580, h / 2 - 330)
	_lienzo.draw_rect(Rect2(0, 0, w, h), Color(0.02, 0.03, 0.04, 0.88))
	# Cubierta de cuero, cantos de papel y dos páginas unidas por el lomo.
	_lienzo.draw_rect(Rect2(origen + Vector2(-18, 0), Vector2(1204, 688)), Color(0, 0, 0, 0.35))
	_lienzo.draw_rect(Rect2(origen + Vector2(-16, -16), Vector2(1192, 688)), Color("483329"))
	for canto in range(5, 0, -1):
		_lienzo.draw_rect(Rect2(origen + Vector2(-canto, canto * 2), Vector2(1160 + canto * 2, 650)), Color("a99c80").lightened(canto * 0.025))
	for lado in range(2):
		var p := origen + Vector2(lado * 580, 0)
		var variante := (_hoja * 2 + lado + int(COLORES_SECCION.keys().find(_seccion))) % _papeles_libro.size()
		_lienzo.draw_texture_rect(_papeles_libro[variante], Rect2(p, Vector2(580, 650)), false)
		_lienzo.draw_rect(Rect2(p + Vector2(38, 51), Vector2(54, 3)), COLORES_SECCION[_seccion])
		_texto("CASO " + caso + "  /  " + archivo, p + Vector2(38, 37), MONO, 13, Color("75654e"))
		_linea(p + Vector2(38, 52), p + Vector2(542, 52), Color(TINTA, 0.22))
		var indice := _hoja * 2 + lado
		if indice < paginas.size():
			_dibujar_pagina(p, paginas[indice])
		else:
			_texto("La investigación continúa…", p + Vector2(38, 140), _medium, 30, Color("75654e"))
		_linea(p + Vector2(38, 592), p + Vector2(542, 592), Color(TINTA, 0.22))
		_texto("DETECTIVE CANELOSO", p + Vector2(38, 621), MONO, 12, Color("75654e"))
		_texto("%02d" % (indice + 1), p + Vector2(510, 621), MONO, 14, TINTA)
	for i in range(24):
		_lienzo.draw_rect(Rect2(origen + Vector2(580 - i, 0), Vector2(i * 2, 650)), Color(0.21, 0.15, 0.08, 0.012))
	_linea(origen + Vector2(580, 5), origen + Vector2(580, 645), Color("77664e"))
	# Una hoja se estrecha hacia el lomo al pasar de página.
	if _giro > 0:
		var ancho := 560.0 * _giro
		var x := 580.0 if _direccion > 0 else 580.0 - ancho
		_lienzo.draw_rect(Rect2(origen + Vector2(x, 3), Vector2(ancho, 642)), Color("f3eddf"))
		_lienzo.draw_rect(Rect2(origen + Vector2(x, 3), Vector2(5, 642)), Color(0.2, 0.13, 0.06, 0.2))
	for i in range(3):
		var seccion: String = ["PISTAS", "DEDUCCIONES", "PERSONAS"][i]
		var activa := seccion == _seccion
		var rect := Rect2(origen + Vector2(i * 210, -68 if activa else -56), Vector2(198, 52 if activa else 40))
		var color: Color = COLORES_SECCION[seccion]
		var escala := minf(_lienzo.size.x / 1600.0, _lienzo.size.y / 900.0)
		if rect.has_point(_lienzo.get_local_mouse_position() / escala):
			color = color.lightened(0.1)
		_lienzo.draw_rect(rect, color)
		_texto(seccion, rect.position + Vector2(16, 27), MONO, 15, TINTA if seccion == "PISTAS" else Color("fff5e8"))
		if activa:
			_lienzo.draw_rect(Rect2(rect.position + Vector2(16, 36), Vector2(30, 3)), TINTA if seccion == "PISTAS" else Color("fff5e8"))
		_zonas_libro.append({"rect": rect, "seccion": seccion})
	_boton_libro("CERRAR [Q]", Rect2(origen + Vector2(1000, -60), Vector2(160, 42)), -1)
	_boton_libro("← ANTERIOR", Rect2(origen + Vector2(0, 686), Vector2(174, 42)), _hoja - 1, _hoja > 0)
	_texto("%s  ·  %02d / %02d  ·  ← / →" % [_seccion, _hoja + 1, ceili(paginas.size() / 2.0)], origen + Vector2(310, 714), MONO, 14, BLANCO)
	_boton_libro("SIGUIENTE →", Rect2(origen + Vector2(976, 686), Vector2(184, 42)), _hoja + 1, (_hoja + 1) * 2 < paginas.size())

func _dibujar_pagina(p: Vector2, pagina: Dictionary) -> void:
	_texto(pagina.seccion, p + Vector2(38, 89), MONO, 14, Color("866134"))
	_texto_ajustado(pagina.titulo, p + Vector2(38, 134), _bold, 36, TINTA, 504)
	_lienzo.draw_rect(Rect2(p + Vector2(38, 152), Vector2(70, 3)), COLORES_SECCION[_seccion])
	if pagina.has("galeria"):
		_dibujar_galeria_pistas(p, pagina.galeria)
		return
	var imagen: Texture2D = pagina.get("imagen")
	var texto_y := 188.0
	if imagen:
		var marco := Rect2(p + Vector2(145, 179), Vector2(290, 218))
		_lienzo.draw_rect(Rect2(marco.position + Vector2(4, 5), marco.size), Color(0, 0, 0, 0.17))
		_lienzo.draw_rect(marco, Color("f5efdf"))
		var medida := imagen.get_size()
		medida *= minf(264.0 / medida.x, 192.0 / medida.y)
		_lienzo.draw_texture_rect(imagen, Rect2(marco.get_center() - medida / 2, medida), false)
		texto_y = 422.0
	_parrafo(pagina.texto, p + Vector2(38, texto_y), 504, 17, TINTA, 7 if imagen else 16)

func _dibujar_galeria_pistas(p: Vector2, inicio: int) -> void:
	if pistas.is_empty():
		_parrafo("Todavía no has reunido pistas. Explora la comisaría para empezar tu colección.", p + Vector2(38, 188), 504, 17, TINTA)
		return
	var escala := minf(_lienzo.size.x / 1600.0, _lienzo.size.y / 900.0)
	var raton := _lienzo.get_local_mouse_position() / escala
	for i in range(inicio, mini(inicio + PISTAS_POR_HOJA, pistas.size())):
		var k := i - inicio
		var rect := Rect2(p + Vector2(38 + (k % 3) * 172, 179 + (k / 3) * 124), Vector2(160, 112))
		var seleccionada := i == _pista_diario
		var hover := rect.has_point(raton)
		_lienzo.draw_rect(Rect2(rect.position + Vector2(3, 4), rect.size), Color(0, 0, 0, 0.15))
		_lienzo.draw_rect(rect, Color("e0c48d") if seleccionada or hover else Color("f5efdf"))
		var imagen := pistas[i].imagen
		if imagen:
			var medida := imagen.get_size()
			medida *= minf(144.0 / medida.x, 86.0 / medida.y)
			_lienzo.draw_texture_rect(imagen, Rect2(rect.position + Vector2(80, 49) - medida / 2, medida), false)
		else:
			_parrafo(pistas[i].titulo, rect.position + Vector2(10, 15), 140, 13, TINTA, 4)
		_texto("%02d" % (i + 1), rect.position + Vector2(9, 105), MONO, 11, TINTA)
		if seleccionada:
			_lienzo.draw_rect(rect, Color("a77e3e"), false, 2.0)
		_zonas_libro.append({"rect": rect, "pista": i})
	_texto("%02d PISTAS  ·  CLIC PARA INSPECCIONAR" % pistas.size(), p + Vector2(38, 574), MONO, 12, Color("75654e"))

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
			_texto_ajustado(pistas[i].titulo, fila + Vector2(47, 29), _bold, 24, TINTA, 283)
		_linea(origen + Vector2(396, 93), origen + Vector2(396, 472), Color(TINTA, 0.25))
		var pista := pistas[indice_pista]
		_texto_ajustado(pista.titulo, origen + Vector2(425, 123), _bold, 32, TINTA, 535)
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
	if _cambio_pista > 0.0 and pistas.size() > 1:
		var t := 1.0 - _cambio_pista / DURACION_CAMBIO_PISTA
		var avance := 1.0 - pow(1.0 - t, 3.0)
		var escala := minf(_lienzo.size.x / 1600.0, _lienzo.size.y / 900.0)
		# La nota anterior se despega; la siguiente se acomoda sobre la pila.
		_lienzo.draw_set_transform((p + Vector2(-_sentido_pista * avance * 100, -sin(t * PI) * 22)) * escala, -_sentido_pista * avance * 0.07, Vector2.ONE * escala)
		_dibujar_postit_pista(Vector2.ZERO, _pista_anterior, 1.0 - avance)
		_lienzo.draw_set_transform((p + Vector2(_sentido_pista * (1.0 - avance) * 90, -(1.0 - avance) * 16)) * escala, _sentido_pista * (1.0 - avance) * 0.045, Vector2.ONE * escala)
		_dibujar_postit_pista(Vector2.ZERO, indice_pista, avance)
		_lienzo.draw_set_transform(Vector2.ZERO, 0, Vector2.ONE * escala)
	else:
		_dibujar_postit_pista(p, indice_pista)

func _dibujar_postit_pista(p: Vector2, indice: int, alfa: float = 1.0) -> void:
	_nota(Rect2(p, Vector2(355, 180)), Color(PAPEL, alfa))
	var contador := "%02d / %02d" % [indice + 1, pistas.size()] if not pistas.is_empty() else "00 / 00"
	_texto("PISTAS", p + Vector2(22, 39), _bold, 30, Color(TINTA, alfa))
	_texto(contador, p + Vector2(246, 36), MONO, 13, Color(TINTA, alfa))
	_lienzo.draw_rect(Rect2(p + Vector2(22, 48), Vector2(120, 4)), Color(AMBAR, alfa))
	if pistas.is_empty():
		_texto("SIN EVIDENCIA", p + Vector2(22, 94), _bold, 28, Color(TINTA, alfa))
		_texto("Explora para reunir pistas.", p + Vector2(22, 124), MONO, 12, Color(TINTA, alfa))
	else:
		var pista := pistas[indice]
		var ancho := 210.0 if pista.imagen else 311.0
		_texto_ajustado(pista.titulo, p + Vector2(22, 93), _bold, 30, Color(TINTA, alfa), ancho)
		if pista.imagen:
			_lienzo.draw_rect(Rect2(p + Vector2(248, 57), Vector2(88, 83)), Color(BLANCO, alfa))
			var zona := Rect2(p + Vector2(254, 63), Vector2(76, 64))
			var factor := minf(zona.size.x / pista.imagen.get_width(), zona.size.y / pista.imagen.get_height())
			var medida := pista.imagen.get_size() * factor
			_lienzo.draw_texture_rect(pista.imagen, Rect2(zona.position + (zona.size - medida) / 2, medida), false, Color(1, 1, 1, alfa))
	_linea(p + Vector2(22, 141), p + Vector2(333, 141), Color(TINTA, 0.2 * alfa))
