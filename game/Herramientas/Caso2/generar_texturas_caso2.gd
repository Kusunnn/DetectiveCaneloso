extends "res://Herramientas/Pistas/generar_texturas.gd"
## Capítulo 2: texturas de las pistas y papeles del capítulo (mismas fuentes, papel y tintas que el Caso 0 y el 1).
## Los textos de la historia van copiados del documento; lo demás (renglones de relleno, membretes) es decorado.
## Necesita ventana. Uso: --path game --script res://Herramientas/Caso2/generar_texturas_caso2.gd
## Salida: res://Assets/Pistas/Modelos/generadas/c2_*.png|jpg
##
## Fechas (derivadas de la historia, no escritas en ella): el Caso 1 fue el 14–15 de agosto; «dos meses después»
## es mediados de octubre; la subasta, «hace tres semanas», el 26 de septiembre; el contrato «un día antes», el 25;
## la foto de Eriz, «una semana antes de la subasta», el 19.

const TINTA_DORADA := Color(0.78, 0.6, 0.22)
const AZUL_PLANO := Color(0.13, 0.27, 0.52)
const PANTALLA := Color(0.07, 0.1, 0.13)

func _documentos() -> void:
	await _render("c2_dibujo_frente", Vector2i(1024, 1434), PAPEL_VIEJO, _doc_dibujo_frente, Color(1, 0.98, 0.93))
	await _render("c2_dibujo_reverso", Vector2i(1024, 1434), PAPEL_VIEJO, _doc_dibujo_reverso, Color(1, 0.98, 0.93))
	await _render("c2_foto_camioneta", Vector2i(1200, 800), null, _doc_foto_camioneta)
	_desenfocar("c2_foto_camioneta", 4)
	await _render("c2_foto_reverso", Vector2i(1200, 800), PAPEL_BLANCO, _doc_foto_reverso)
	await _render("c2_invitacion", Vector2i(1400, 1000), PAPEL_BLANCO, _doc_invitacion, Color(1, 0.96, 0.86))
	await _render("c2_invitacion_reverso", Vector2i(1400, 1000), PAPEL_BLANCO, _doc_invitacion_reverso, Color(1, 0.96, 0.86))
	await _render("c2_sobre_eriz", Vector2i(1400, 1000), PAPEL_VIEJO, _doc_sobre_eriz, Color(0.93, 0.78, 0.55))
	await _render("c2_registro_accesos", Vector2i(1600, 1000), null, _doc_registro_accesos)
	await _render("c2_noticias", Vector2i(1600, 900), null, _doc_noticias)
	await _render("c2_directorio", Vector2i(900, 1200), null, _doc_directorio)
	await _render("c2_tres_rayas", Vector2i(1600, 900), null, _doc_tres_rayas)
	await _render("c2_grafica", Vector2i(1600, 900), null, _doc_grafica)
	await _render("c2_planos_clinica", Vector2i(2048, 1400), PAPEL_BLANCO, _doc_planos_clinica, Color(0.86, 0.92, 1.0))
	await _render("c2_contrato", Vector2i(1024, 1434), PAPEL_BLANCO, _doc_contrato)
	await _render("c2_contrato_reverso", Vector2i(1024, 1434), PAPEL_BLANCO, _doc_vacio)
	await _render("c2_estado_cuenta", Vector2i(1024, 1434), PAPEL_BLANCO, _doc_estado_cuenta)
	await _render("c2_registro_llamadas", Vector2i(1024, 1434), PAPEL_BLANCO, _doc_registro_llamadas2)
	await _render("c2_tarjeta_socio", Vector2i(800, 500), PAPEL_BLANCO, _doc_tarjeta_socio, Color(1, 0.97, 0.9))
	await _render("c2_expediente_portada", Vector2i(1024, 1434), PAPEL_VIEJO, _doc_expediente_portada, Color(0.98, 0.86, 0.6))
	await _render("c2_expediente_hoja", Vector2i(1024, 1434), PAPEL_BLANCO, _doc_expediente_hoja)

func _imagenes() -> void:
	pass

func _desenfocar(nombre: String, veces: int) -> void:
	var ruta := ProjectSettings.globalize_path(SALIDA + nombre + ".png")
	var img := Image.load_from_file(ruta)
	var w := img.get_width()
	var h := img.get_height()
	for i in veces:
		img.resize(w / 3, h / 3, Image.INTERPOLATE_BILINEAR)
		img.resize(w, h, Image.INTERPOLATE_BILINEAR)
	img.save_png(ruta)

# ---------- Comisaría ----------

# P01 · Dibujo a carboncillo de Caneloso (Eriz)
var _boceto: ImageTexture

# El retrato de Caneloso pasado a carboncillo: sin el fondo, en grises, con los contornos marcados y achurado
func _hacer_boceto() -> ImageTexture:
	var img: Image = (load("res://Assets/Retratos/caneloso.png") as Texture2D).get_image()
	img.decompress()
	img.convert(Image.FORMAT_RGBA8)
	var w := img.get_width()
	var h := img.get_height()
	var fondo := img.get_pixel(4, 4)
	var lum := PackedFloat32Array()
	lum.resize(w * h)
	var mascara := PackedByteArray()
	mascara.resize(w * h)
	for y in h:
		for x in w:
			var p := img.get_pixel(x, y)
			lum[y * w + x] = p.get_luminance()
			var d := Vector3(p.r - fondo.r, p.g - fondo.g, p.b - fondo.b).length()
			mascara[y * w + x] = 1 if d > 0.09 else 0
	var ruido := _ruido(31, 0.08)
	var salida := Image.create(w, h, false, Image.FORMAT_RGBA8)
	for y in range(1, h - 1):
		for x in range(1, w - 1):
			if mascara[y * w + x] == 0:
				salida.set_pixel(x, y, Color(0, 0, 0, 0))
				continue
			var gx := lum[y * w + x + 1] - lum[y * w + x - 1]
			var gy := lum[(y + 1) * w + x] - lum[(y - 1) * w + x]
			var borde := clampf(sqrt(gx * gx + gy * gy) * 4.0, 0.0, 1.0)
			var orilla := 1.0 if (mascara[y * w + x + 1] == 0 or mascara[y * w + x - 1] == 0 or mascara[(y + 1) * w + x] == 0 or mascara[(y - 1) * w + x] == 0) else 0.0
			var sombra := clampf((0.75 - lum[y * w + x]) * 1.2, 0.0, 1.0)
			# Achurado: líneas diagonales que solo aparecen en las sombras
			var rayas := 1.0 if fposmod(x + y + ruido.get_noise_2d(x, y) * 6.0, 7.0) < 2.2 else 0.0
			var a := clampf(maxf(borde, orilla) * 0.9 + sombra * rayas * 0.55 + sombra * 0.15, 0.0, 0.92)
			salida.set_pixel(x, y, Color(0.14, 0.13, 0.13, a))
	return ImageTexture.create_from_image(salida)

func _doc_dibujo_frente(c: Control) -> void:
	if _boceto == null:
		_boceto = _hacer_boceto()
	var carbon := Color(0.16, 0.15, 0.15)
	c.draw_texture_rect(_boceto, Rect2(112, 150, 800, 800), false)
	# Sombra de la base, trazos sueltos alrededor y firma
	for i in 50:
		var x := _azar.randf_range(220, 800)
		c.draw_line(Vector2(x, 965), Vector2(x + 50, 1005), Color(carbon, 0.12), 4, true)
	for i in 30:
		var p := Vector2(_azar.randf_range(130, 900), _azar.randf_range(160, 940))
		c.draw_line(p, p + Vector2(_azar.randf_range(-30, 30), _azar.randf_range(-30, 30)), Color(carbon, 0.06), 3, true)
	_mano(c, ["Eriz"], Vector2(720, 1180), 64, Color(carbon, 0.85), 70, MANO_FIRME, -0.05)
	_manchas(c, Vector2(1024, 1434), 4, Color(0.2, 0.2, 0.2, 0.5), Vector2(20, 60))

func _doc_dibujo_reverso(c: Control) -> void:
	# Texto exacto de la historia
	_mano(c, ["Oye pa, vi gente", "midiendo el lago."], Vector2(170, 620), 92, Color(0.15, 0.15, 0.17, 0.85), 112, MANO, -0.025)
	_manchas(c, Vector2(1024, 1434), 3, Color(0.3, 0.3, 0.3, 0.4), Vector2(20, 60))

# Foto borrosa: de noche, la camioneta negra y linternas junto al portón sellado del campamento
func _doc_foto_camioneta(c: Control) -> void:
	c.draw_rect(Rect2(0, 0, 1200, 800), Color(0.05, 0.07, 0.1))
	c.draw_rect(Rect2(0, 0, 1200, 360), Color(0.07, 0.09, 0.14))
	# Pinos en silueta
	for i in 14:
		var x := i * 95.0 + _azar.randf_range(-20, 20)
		var alto := _azar.randf_range(180, 300)
		c.draw_colored_polygon(PackedVector2Array([Vector2(x - 55, 400), Vector2(x, 400 - alto), Vector2(x + 55, 400)]), Color(0.03, 0.05, 0.05))
	c.draw_rect(Rect2(0, 400, 1200, 400), Color(0.08, 0.08, 0.07))
	# Portón con sellos rojos
	c.draw_rect(Rect2(150, 300, 26, 260), Color(0.2, 0.17, 0.13))
	c.draw_rect(Rect2(470, 300, 26, 260), Color(0.2, 0.17, 0.13))
	c.draw_rect(Rect2(176, 360, 294, 16), Color(0.22, 0.18, 0.14))
	c.draw_rect(Rect2(176, 450, 294, 16), Color(0.22, 0.18, 0.14))
	for x in [205.0, 330.0, 430.0]:
		c.draw_rect(Rect2(x, 355, 30, 120), Color(0.7, 0.08, 0.06, 0.9))
	# Camioneta negra
	c.draw_rect(Rect2(600, 360, 430, 170), Color(0.02, 0.02, 0.025))
	c.draw_rect(Rect2(640, 300, 300, 80), Color(0.02, 0.02, 0.025))
	c.draw_rect(Rect2(660, 315, 120, 50), Color(0.1, 0.12, 0.15))
	c.draw_rect(Rect2(800, 315, 120, 50), Color(0.1, 0.12, 0.15))
	c.draw_circle(Vector2(690, 535), 42, Color(0.01, 0.01, 0.01))
	c.draw_circle(Vector2(960, 535), 42, Color(0.01, 0.01, 0.01))
	c.draw_circle(Vector2(1022, 420), 14, Color(1, 0.9, 0.6, 0.9))
	# Linternas y siluetas
	for p in [Vector2(250, 470), Vector2(380, 455)]:
		c.draw_colored_polygon(PackedVector2Array([p, p + Vector2(-140, 260), p + Vector2(60, 260)]), Color(1, 0.92, 0.6, 0.22))
		c.draw_circle(p, 9, Color(1, 0.95, 0.75))
		c.draw_rect(Rect2(p.x - 18, p.y - 120, 36, 140), Color(0.02, 0.02, 0.02, 0.9))
		c.draw_circle(p + Vector2(0, -130), 20, Color(0.02, 0.02, 0.02, 0.9))
	# Fecha de la cámara
	c.draw_string(IMPRESO, Vector2(900, 760), "19 09", HORIZONTAL_ALIGNMENT_LEFT, -1, 46, Color(1, 0.55, 0.15, 0.9))
	# Grano
	for i in 2500:
		c.draw_rect(Rect2(_azar.randf_range(0, 1200), _azar.randf_range(0, 800), 2, 2), Color(1, 1, 1, _azar.randf_range(0.02, 0.08)))

func _doc_foto_reverso(c: Control) -> void:
	for i in 8:
		c.draw_string(IMPRESO, Vector2(80 + i * 140, 400 + (i % 2) * 40), "PHOTO PAPER", HORIZONTAL_ALIGNMENT_LEFT, -1, 20, Color(0.5, 0.5, 0.55, 0.35))

# Invitación en papel grueso con letras doradas (texto exacto de la historia)
func _doc_invitacion(c: Control) -> void:
	c.draw_rect(Rect2(40, 40, 1320, 920), Color(TINTA_DORADA, 0.9), false, 6)
	c.draw_rect(Rect2(64, 64, 1272, 872), Color(TINTA_DORADA, 0.6), false, 2)
	_dorado(c, PERIODICO_NEGRITA, "Miawrio Nyx", Vector2(700, 230), 96)
	var texto := "tiene el honor de invitar al Detective Caneloso a su despacho corporativo, para revisar con total transparencia el Proyecto Pino Lunar."
	c.draw_multiline_string(PERIODICO, Vector2(170, 380), texto, HORIZONTAL_ALIGNMENT_CENTER, 1060, 52, -1, Color(0.42, 0.3, 0.12))
	_dorado(c, PERIODICO, "Torre Miawrio · Piso 30", Vector2(700, 820), 38)

func _dorado(c: Control, fuente: Font, texto: String, centro: Vector2, tam: int) -> void:
	var ancho := fuente.get_string_size(texto, HORIZONTAL_ALIGNMENT_LEFT, -1, tam).x
	var p := centro - Vector2(ancho / 2.0, 0)
	c.draw_string(fuente, p + Vector2(2, 2), texto, HORIZONTAL_ALIGNMENT_LEFT, -1, tam, Color(0.35, 0.24, 0.08, 0.6))
	c.draw_string(fuente, p, texto, HORIZONTAL_ALIGNMENT_LEFT, -1, tam, TINTA_DORADA)
	c.draw_string(fuente, p + Vector2(-1, -1), texto, HORIZONTAL_ALIGNMENT_LEFT, -1, tam, Color(1, 0.9, 0.55, 0.35))

func _doc_invitacion_reverso(c: Control) -> void:
	c.draw_circle(Vector2(700, 500), 120, Color(TINTA_DORADA, 0.85))
	c.draw_circle(Vector2(700, 500), 104, Color(0.98, 0.94, 0.84))
	_dorado(c, PERIODICO_NEGRITA, "MN", Vector2(700, 530), 96)

func _doc_sobre_eriz(c: Control) -> void:
	_mano(c, ["Det. Caneloso"], Vector2(460, 520), 96, Color(0.18, 0.16, 0.2, 0.85), 100, MANO_FIRME, -0.03)
	c.draw_rect(Rect2(1120, 80, 200, 240), Color(0.75, 0.3, 0.2, 0.55))
	_manchas(c, Vector2(1400, 1000), 4, Color(0.45, 0.3, 0.15, 0.6), Vector2(30, 90))

# ---------- Lobby ----------

# P02 · Registro de accesos en la pantalla de la recepción. Las tres líneas raras son las de la historia.
func _doc_registro_accesos(c: Control) -> void:
	c.draw_rect(Rect2(0, 0, 1600, 1000), PANTALLA)
	c.draw_rect(Rect2(0, 0, 1600, 110), Color(0.12, 0.17, 0.22))
	c.draw_string(PERIODICO_NEGRITA, Vector2(50, 75), "TORRE MIAWRIO · REGISTRO DE ACCESOS", HORIZONTAL_ALIGNMENT_LEFT, -1, 46, Color(0.9, 0.78, 0.45))
	var cab := Color(0.6, 0.7, 0.78)
	var y := 175.0
	for col in [["HORA", 50], ["NOMBRE", 330], ["CONCEPTO", 760], ["PISO", 1430]]:
		c.draw_string(IMPRESO, Vector2(col[1], y), col[0], HORIZONTAL_ALIGNMENT_LEFT, -1, 30, cab)
	c.draw_line(Vector2(40, y + 18), Vector2(1560, y + 18), Color(cab, 0.5), 2)
	var filas := [
		["Ayer 18:40", "Mensajería Rápida", "Entrega de paquete", "PB", false],
		["Ayer 21:00", "Mimita", "Ixodes – entrevista", "30", true],
		["Hoy 08:05", "Personal de limpieza", "Turno matutino", "1-30", false],
		["Hoy 08:55", "Aya", "Invitada de: Inversiones Ixodes", "30", true],
		["Hoy 09:58", "Técnico", "Servicios Pulgar", "23", true],
		["Hoy 10:20", "Proveedor de café", "Entrega", "30", false],
	]
	y += 90.0
	for f in filas:
		if f[4]:
			c.draw_rect(Rect2(40, y - 48, 1520, 70), Color(0.95, 0.25, 0.2, 0.22))
			c.draw_rect(Rect2(40, y - 48, 8, 70), Color(0.95, 0.3, 0.2))
		var color := Color(0.97, 0.95, 0.9) if f[4] else Color(0.75, 0.8, 0.84)
		c.draw_string(IMPRESO, Vector2(60, y), f[0], HORIZONTAL_ALIGNMENT_LEFT, -1, 34, color)
		c.draw_string(IMPRESO, Vector2(330, y), f[1], HORIZONTAL_ALIGNMENT_LEFT, -1, 34, color)
		c.draw_string(IMPRESO, Vector2(760, y), f[2], HORIZONTAL_ALIGNMENT_LEFT, -1, 34, color)
		c.draw_string(IMPRESO, Vector2(1430, y), f[3], HORIZONTAL_ALIGNMENT_LEFT, -1, 34, color)
		y += 110.0
	c.draw_string(IMPRESO, Vector2(50, 960), "Recepción · Planta baja", HORIZONTAL_ALIGNMENT_LEFT, -1, 24, Color(cab, 0.6))

# Pantalla de noticias del lobby (de fondo): el video viral del ciervo, como cuenta Hecdog
func _doc_noticias(c: Control) -> void:
	c.draw_rect(Rect2(0, 0, 1600, 900), Color(0.1, 0.16, 0.28))
	# «Imagen» del noticiero: el lago de noche con la silueta del ciervo de luz
	c.draw_rect(Rect2(60, 60, 1480, 620), Color(0.05, 0.08, 0.14))
	c.draw_rect(Rect2(60, 470, 1480, 210), Color(0.08, 0.13, 0.2))
	for i in 10:
		var x := 80.0 + i * 150.0
		c.draw_colored_polygon(PackedVector2Array([Vector2(x - 60, 480), Vector2(x, 260 + (i % 3) * 30), Vector2(x + 60, 480)]), Color(0.03, 0.06, 0.07))
	c.draw_circle(Vector2(800, 400), 70, Color(0.95, 0.9, 0.6, 0.25))
	c.draw_circle(Vector2(800, 400), 34, Color(1, 0.95, 0.7, 0.8))
	c.draw_line(Vector2(780, 360), Vector2(740, 300), Color(1, 0.95, 0.7, 0.8), 6)
	c.draw_line(Vector2(820, 360), Vector2(860, 300), Color(1, 0.95, 0.7, 0.8), 6)
	c.draw_rect(Rect2(60, 60, 170, 56), Color(0.85, 0.15, 0.12))
	c.draw_string(PERIODICO_NEGRITA, Vector2(78, 102), "EN VIVO", HORIZONTAL_ALIGNMENT_LEFT, -1, 34, Color.WHITE)
	c.draw_rect(Rect2(0, 690, 1600, 120), Color(0.92, 0.9, 0.84))
	c.draw_string(PERIODICO_NEGRITA, Vector2(40, 768), "VIDEO DEL CIERVO DE LUZ: 4 MILLONES DE VISTAS", HORIZONTAL_ALIGNMENT_LEFT, -1, 54, Color(0.1, 0.12, 0.2))
	c.draw_rect(Rect2(0, 810, 1600, 90), Color(0.75, 0.12, 0.1))
	c.draw_string(IMPRESO, Vector2(40, 868), "Campamento Pino Lunar: rematado en subasta pública  ·  Clausura de la Fiscalía sigue vigente", HORIZONTAL_ALIGNMENT_LEFT, -1, 30, Color.WHITE)

# Directorio de la torre junto a los elevadores
func _doc_directorio(c: Control) -> void:
	c.draw_rect(Rect2(0, 0, 900, 1200), Color(0.16, 0.11, 0.07))
	c.draw_rect(Rect2(24, 24, 852, 1152), Color(0.85, 0.65, 0.28), false, 4)
	c.draw_string(PERIODICO_NEGRITA, Vector2(90, 140), "TORRE MIAWRIO", HORIZONTAL_ALIGNMENT_LEFT, -1, 76, Color(0.9, 0.72, 0.36))
	c.draw_line(Vector2(90, 180), Vector2(810, 180), Color(0.85, 0.65, 0.28), 3)
	var pisos := [["30", "Miawrio Nyx · Dirección"], ["29", "Sala de juntas"], ["24 – 28", "Oficinas"], ["23", "Sistemas"], ["2 – 22", "Oficinas"], ["1", "Servicios"], ["PB", "Recepción"], ["E", "Estacionamiento"]]
	var y := 290.0
	for p in pisos:
		c.draw_string(PERIODICO_NEGRITA, Vector2(90, y), p[0], HORIZONTAL_ALIGNMENT_LEFT, -1, 48, Color(0.95, 0.85, 0.6))
		c.draw_string(PERIODICO, Vector2(300, y), p[1], HORIZONTAL_ALIGNMENT_LEFT, -1, 44, Color(0.92, 0.88, 0.8))
		y += 112.0

# Las pantallas hackeadas: tres rayas rojas, como garras, sobre negro
func _doc_tres_rayas(c: Control) -> void:
	c.draw_rect(Rect2(0, 0, 1600, 900), Color(0.03, 0.02, 0.025))
	for i in 3:
		var x := 560.0 + i * 190.0
		var pts := PackedVector2Array()
		for k in 21:
			var t := k / 20.0
			pts.append(Vector2(x + sin(t * PI) * 60.0 - t * 120.0, 150.0 + t * 600.0))
		for g in [[34.0, 0.25], [22.0, 0.5], [12.0, 1.0]]:
			c.draw_polyline(pts, Color(1.0, 0.1, 0.08, g[1]), g[0], true)
	for y in range(0, 900, 6):
		c.draw_line(Vector2(0, y), Vector2(1600, y), Color(0, 0, 0, 0.18), 2)

# Pantalla del despacho en estado normal: la gráfica del Proyecto Pino Lunar
func _doc_grafica(c: Control) -> void:
	c.draw_rect(Rect2(0, 0, 1600, 900), Color(0.08, 0.13, 0.12))
	c.draw_string(PERIODICO_NEGRITA, Vector2(60, 90), "PROYECTO PINO LUNAR", HORIZONTAL_ALIGNMENT_LEFT, -1, 56, Color(0.9, 0.78, 0.45))
	c.draw_string(IMPRESO, Vector2(60, 140), "Avance de obra · Hotel y clínica comunitaria", HORIZONTAL_ALIGNMENT_LEFT, -1, 28, Color(0.7, 0.8, 0.75))
	var pts := PackedVector2Array()
	for i in 12:
		pts.append(Vector2(100 + i * 125, 760 - i * 40 - (i % 3) * 25))
	for i in 12:
		c.draw_rect(Rect2(80 + i * 125, 790 - (i * 30 + 60), 40, i * 30 + 60), Color(0.3, 0.55, 0.45, 0.6))
	c.draw_polyline(pts, Color(0.95, 0.8, 0.4), 6, true)
	for p in pts:
		c.draw_circle(p, 9, Color(0.95, 0.8, 0.4))
	c.draw_line(Vector2(60, 800), Vector2(1560, 800), Color(0.7, 0.8, 0.75, 0.6), 3)

# ---------- Despacho ----------

# P03 · Planos de la clínica, con «Para M.» a mano en la esquina
func _doc_planos_clinica(c: Control) -> void:
	c.draw_rect(Rect2(0, 0, 2048, 1400), Color(AZUL_PLANO, 0.92))
	var linea := Color(0.88, 0.94, 1.0, 0.9)
	for x in range(0, 2048, 64):
		c.draw_line(Vector2(x, 0), Vector2(x, 1400), Color(1, 1, 1, 0.06), 1)
	for y in range(0, 1400, 64):
		c.draw_line(Vector2(0, y), Vector2(2048, y), Color(1, 1, 1, 0.06), 1)
	c.draw_string(PERIODICO_NEGRITA, Vector2(110, 130), "CLÍNICA COMUNITARIA GRATUITA", HORIZONTAL_ALIGNMENT_LEFT, -1, 64, linea)
	c.draw_string(PERIODICO, Vector2(110, 190), "Planta baja del nuevo hotel · Proyecto Pino Lunar", HORIZONTAL_ALIGNMENT_LEFT, -1, 34, Color(linea, 0.8))
	var r := Rect2(110, 260, 1500, 1000)
	c.draw_rect(r, linea, false, 6)
	# Doce consultorios en dos filas
	for i in 6:
		for fila in 2:
			var cr := Rect2(r.position.x + i * 250, r.position.y + fila * 760, 250, 240)
			c.draw_rect(cr, linea, false, 3)
			c.draw_string(IMPRESO, cr.position + Vector2(20, 130), "Consultorio %d" % (i + 1 + fila * 6), HORIZONTAL_ALIGNMENT_LEFT, -1, 24, linea)
	var farmacia := Rect2(r.position.x + 40, r.position.y + 330, 560, 360)
	c.draw_rect(farmacia, linea, false, 3)
	c.draw_string(IMPRESO, farmacia.position + Vector2(30, 190), "Farmacia gratuita", HORIZONTAL_ALIGNMENT_LEFT, -1, 34, linea)
	var partos := Rect2(r.position.x + 900, r.position.y + 330, 560, 360)
	c.draw_rect(partos, linea, false, 3)
	c.draw_string(IMPRESO, partos.position + Vector2(30, 190), "Sala de partos", HORIZONTAL_ALIGNMENT_LEFT, -1, 34, linea)
	c.draw_string(IMPRESO, Vector2(r.position.x + 640, r.position.y + 520), "Espera", HORIZONTAL_ALIGNMENT_LEFT, -1, 30, Color(linea, 0.8))
	# Rótulo de la esquina y la dedicatoria a mano (con la letra de Miawrio)
	c.draw_rect(Rect2(1650, 1080, 340, 260), linea, false, 3)
	c.draw_string(IMPRESO, Vector2(1670, 1130), "Escala 1:100", HORIZONTAL_ALIGNMENT_LEFT, -1, 24, linea)
	_mano(c, ["Para M."], Vector2(1690, 1260), 96, Color(1, 0.95, 0.8, 0.95), 100, MANO_FIRME, -0.06)

# P04 · Contrato de financiamiento con Ixodes. Firma «Lic. R. Garrido»; fechado un día antes de la subasta.
func _doc_contrato(c: Control) -> void:
	var tinta := Color(0.1, 0.1, 0.12)
	c.draw_string(PERIODICO_NEGRITA, Vector2(110, 150), "CONTRATO DE FINANCIAMIENTO", HORIZONTAL_ALIGNMENT_LEFT, -1, 52, tinta)
	c.draw_line(Vector2(110, 180), Vector2(914, 180), tinta, 2)
	var cuerpo := ["Entre INVERSIONES IXODES S.A. de C.V. («el Fondo»),", "representada por su apoderado, y el señor", "Miawrio Nyx («el Acreditado»).", "",
		"Objeto: financiar la compra del inmueble", "«Campamento Pino Lunar» en la subasta pública", "del 26 de septiembre.", "",
		"El Fondo entregará el monto total en un plazo", "de 48 horas a partir de la adjudicación.", "", "Ciudad, a 25 de septiembre."]
	_maquina(c, cuerpo, Vector2(110, 260), 30, 54, Color(tinta, 0.9), IMPRESO)
	c.draw_rect(Rect2(110, 875, 360, 50), Color(1, 0.85, 0.1, 0.3))
	c.draw_line(Vector2(110, 1180), Vector2(470, 1180), tinta, 2)
	c.draw_line(Vector2(560, 1180), Vector2(914, 1180), tinta, 2)
	_firma(c, "R. Garrido", Vector2(130, 1160), 54, TINTA_AZUL)
	c.draw_string(IMPRESO, Vector2(110, 1220), "Lic. R. Garrido · Apoderado", HORIZONTAL_ALIGNMENT_LEFT, -1, 24, tinta)
	_firma(c, "Miawrio", Vector2(600, 1160), 50, TINTA_NEGRA)
	c.draw_string(IMPRESO, Vector2(560, 1220), "Miawrio Nyx", HORIZONTAL_ALIGNMENT_LEFT, -1, 24, tinta)

# P05 · Estado de cuenta: ocho depósitos de ocho empresas distintas en el mismo minuto
func _doc_estado_cuenta(c: Control) -> void:
	var tinta := Color(0.1, 0.1, 0.12)
	c.draw_rect(Rect2(0, 0, 1024, 150), Color(0.16, 0.3, 0.26))
	c.draw_string(PERIODICO_NEGRITA, Vector2(70, 95), "ESTADO DE CUENTA", HORIZONTAL_ALIGNMENT_LEFT, -1, 50, Color(0.95, 0.92, 0.85))
	_maquina(c, ["Titular: Miawrio Nyx · Proyecto Pino Lunar", "Periodo: septiembre", "", "FECHA  HORA   ORIGEN                   ABONO"], Vector2(70, 230), 24, 44, tinta, IMPRESO)
	var empresas := ["Inmobiliaria Arce", "Logística Delta", "Consultores Bravo", "Servicios Ámbar", "Textiles Orión", "Agroindustrias Sur", "Transportes Lince", "Distribuidora Nácar"]
	var y := 420.0
	for i in empresas.size():
		c.draw_rect(Rect2(66, y - 30, 900, 44), Color(1, 0.85, 0.1, 0.18 if i % 2 == 0 else 0.1))
		c.draw_string(IMPRESO, Vector2(70, y), "27/09  12:07  %-24s %s" % [empresas[i], "$ 1,500,000.00"], HORIZONTAL_ALIGNMENT_LEFT, -1, 24, tinta)
		y += 56.0
	c.draw_line(Vector2(70, y), Vector2(954, y), tinta, 2)
	c.draw_string(IMPRESO, Vector2(70, y + 50), "Total abonado en un minuto:      $ 12,000,000.00", HORIZONTAL_ALIGNMENT_LEFT, -1, 26, tinta)

# P06 · Registro de llamadas de Miawrio: número oculto, siempre a las 23:00; la noche del crimen de 23:00 a 23:40
func _doc_registro_llamadas2(c: Control) -> void:
	var lineas := ["REGISTRO DE LLAMADAS", "Línea: Miawrio Nyx · celular", "", "FECHA   HORA   NÚMERO        DURACIÓN", "--------------------------------------",
		"02/08   23:00  Número oculto  00:12:10", "09/08   23:00  Número oculto  00:18:44", "14/08   23:00  Número oculto  00:40:02",
		"21/08   23:00  Número oculto  00:09:31", "04/09   23:00  Número oculto  00:21:15", "18/09   23:00  Número oculto  00:15:02",
		"25/09   23:00  Número oculto  00:33:47", "12/10   23:00  Número oculto  00:11:20"]
	_maquina(c, lineas, Vector2(90, 180), 27, 64, Color(0.1, 0.1, 0.12, 0.92), IMPRESO)
	# La noche del crimen de Porky (14/08, de 23:00 a 23:40)
	c.draw_rect(Rect2(86, 180 + 7 * 64 - 32, 860, 44), Color(1, 0.85, 0.1, 0.35))

# P07 · Tarjeta del abrecartas (texto exacto)
func _doc_tarjeta_socio(c: Control) -> void:
	c.draw_rect(Rect2(20, 20, 760, 460), Color(TINTA_DORADA, 0.7), false, 3)
	_mano(c, ["Para M., de un", "socio paciente."], Vector2(110, 210), 78, Color(0.12, 0.1, 0.12, 0.9), 96, MANO_LIBRE, -0.02)

# P08 · Copia del expediente de Pino Lunar con el sello de la Fiscalía
func _doc_expediente_portada(c: Control) -> void:
	var tinta := Color(0.2, 0.14, 0.08)
	c.draw_string(PERIODICO_NEGRITA, Vector2(90, 200), "FISCALÍA DEL ESTADO", HORIZONTAL_ALIGNMENT_LEFT, -1, 54, tinta)
	c.draw_string(PERIODICO, Vector2(90, 270), "Expediente · Campamento Pino Lunar", HORIZONTAL_ALIGNMENT_LEFT, -1, 40, tinta)
	c.draw_string(PERIODICO, Vector2(90, 330), "Clausura y Aseguramiento Prolongado", HORIZONTAL_ALIGNMENT_LEFT, -1, 32, tinta)
	c.draw_set_transform(Vector2(520, 820), -0.18)
	c.draw_arc(Vector2.ZERO, 190, 0, TAU, 64, Color(0.65, 0.1, 0.08, 0.75), 10)
	c.draw_arc(Vector2.ZERO, 160, 0, TAU, 64, Color(0.65, 0.1, 0.08, 0.75), 4)
	c.draw_string(PERIODICO_NEGRITA, Vector2(-150, -10), "FISCALÍA", HORIZONTAL_ALIGNMENT_LEFT, -1, 52, Color(0.65, 0.1, 0.08, 0.8))
	c.draw_string(PERIODICO_NEGRITA, Vector2(-150, 50), "CONFIDENCIAL", HORIZONTAL_ALIGNMENT_LEFT, -1, 40, Color(0.65, 0.1, 0.08, 0.8))
	c.draw_set_transform(Vector2.ZERO)
	_manchas(c, Vector2(1024, 1434), 4, Color(0.4, 0.3, 0.15, 0.6))

func _doc_expediente_hoja(c: Control) -> void:
	_maquina(c, ["COPIA", "", "Balística ........... abierta", "Huellas ............. abierta", "Criminalística ...... abierta", "", "Embarcadero: acordonado", "Cabañas: sellos rojos"], Vector2(110, 220), 30, 62, TINTA_MAQUINA, IMPRESO)
