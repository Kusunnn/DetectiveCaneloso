extends SceneTree
## HU-07: genera las texturas de las pistas hechas en el proyecto (papeles con texto, foto, etiquetas, huellas,
## grietas, pluma, manchas...). Los textos se dibujan con fuentes reales sobre la textura de papel de ambientCG.
## Necesita ventana (hay que renderizar). Uso: --path game --script res://Herramientas/Pistas/generar_texturas.gd
## Salida: res://Assets/Pistas/Modelos/generadas/*.png

const SALIDA := "res://Assets/Pistas/Modelos/generadas/"
# Lo que solo sirve de paso (las hojas antes de quemarlas) no se guarda en el proyecto
const TEMPORAL := "user://texturas_pistas/"
const INTERMEDIOS := ["quemado_hoja_a", "quemado_hoja_b", "quemado_hoja_c", "quemado_reverso", "quemado_reverso_nombre"]
const PAPEL_BLANCO := "res://Assets/Pistas/Modelos/texturas/Paper005/Paper005_Color.jpg"
const PAPEL_VIEJO := "res://Assets/Pistas/Modelos/texturas/Paper006/Paper006_Color.jpg"
const CARTON := "res://Assets/Pistas/Modelos/texturas/Cork002/Cork002_Color.jpg"

var MAQUINA: Font
var MANO: FontVariation
var MANO_FIRME: FontVariation
var MANO_LIBRE: Font
var PERIODICO: Font
var PERIODICO_NEGRITA: Font
var IMPRESO: Font

const TINTA_AZUL := Color(0.12, 0.17, 0.42, 0.9)
const TINTA_NEGRA := Color(0.08, 0.07, 0.07, 0.92)
const TINTA_MAQUINA := Color(0.1, 0.1, 0.11, 0.9)
const TINTA_ROJA := Color(0.62, 0.1, 0.08, 0.85)
const LAPIZ := Color(0.22, 0.22, 0.24, 0.75)

var _vista: SubViewport
var _lienzo: Control
var _azar := RandomNumberGenerator.new()

func _initialize() -> void:
	DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(SALIDA))
	DirAccess.make_dir_recursive_absolute(TEMPORAL)
	_azar.seed = 714
	MAQUINA = load("res://Assets/Fuentes/SpecialElite-Regular.ttf")
	MANO = FontVariation.new()
	MANO.base_font = load("res://Assets/Fuentes/Caveat-Variable.ttf")
	MANO.variation_opentype = {2003265652: 500.0}
	MANO_FIRME = FontVariation.new()
	MANO_FIRME.base_font = MANO.base_font
	MANO_FIRME.variation_opentype = {2003265652: 700.0}
	MANO_LIBRE = load("res://Assets/Fuentes/HomemadeApple-Regular.ttf")
	PERIODICO = load("res://Assets/Fuentes/OldStandard-Regular.ttf")
	PERIODICO_NEGRITA = load("res://Assets/Fuentes/OldStandard-Bold.ttf")
	IMPRESO = load("res://Assets/Fuentes/IBMPlexMono-Regular.ttf")

	_vista = SubViewport.new()
	_vista.transparent_bg = true
	_vista.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	root.add_child(_vista)
	_lienzo = Control.new()
	_vista.add_child(_lienzo)

	await _documentos()
	_imagenes()
	print("Texturas listas")
	quit()

# ---------- Dibujo de documentos ----------

# Dibuja con la función dada sobre un lienzo de tam y guarda el PNG. fondo: ruta de papel o null (transparente)
func _render(nombre: String, tam: Vector2i, fondo, dibujar: Callable, tinte := Color.WHITE) -> void:
	_vista.size = tam
	_lienzo.size = Vector2(tam)
	for c in _lienzo.get_signal_connection_list("draw"):
		_lienzo.draw.disconnect(c.callable)
	var papel: Texture2D = load(fondo) if fondo != null else null
	_lienzo.draw.connect(func():
		if papel:
			# Color de fondo del papel y, encima, el grano de la textura de ambientCG (recortada, sin repetir)
			var base := Color(0.95, 0.94, 0.9) if fondo == PAPEL_BLANCO else Color(0.9, 0.85, 0.74)
			_lienzo.draw_rect(Rect2(Vector2.ZERO, Vector2(tam)), base * tinte)
			var escala := maxf(float(tam.x) / papel.get_width(), float(tam.y) / papel.get_height())
			_lienzo.draw_texture_rect(papel, Rect2(Vector2.ZERO, papel.get_size() * escala), false, Color(tinte, 0.32 if fondo != CARTON else 0.85))
		dibujar.call(_lienzo))
	_lienzo.queue_redraw()
	for i in 3:
		await process_frame
	var img := _vista.get_texture().get_image()
	img.convert(Image.FORMAT_RGBA8)
	if nombre in INTERMEDIOS:
		img.save_png(TEMPORAL + nombre + ".png")
	elif fondo == null:
		img.save_png(ProjectSettings.globalize_path(SALIDA + nombre + ".png"))
	else:
		# Papel sin transparencia: en JPG pesa diez veces menos
		img.convert(Image.FORMAT_RGB8)
		img.save_jpg(ProjectSettings.globalize_path(SALIDA + nombre + ".jpg"), 0.9)
	print("  ", nombre)

# Texto escrito a mano: cada renglón con una leve inclinación y la línea base un poco irregular
func _mano(c: Control, lineas: PackedStringArray, origen: Vector2, tam: int, color: Color, interlineado: float, fuente: Font = null, inclinacion := -0.012) -> void:
	var f: Font = fuente if fuente else MANO
	for i in lineas.size():
		var p := origen + Vector2(_azar.randf_range(-4, 4), i * interlineado + _azar.randf_range(-2, 2))
		c.draw_set_transform(p, inclinacion + _azar.randf_range(-0.01, 0.01))
		# Un segundo trazo muy tenue: la tinta se corre un poco
		c.draw_string(f, Vector2(0.8, 0.6), lineas[i], HORIZONTAL_ALIGNMENT_LEFT, -1, tam, Color(color, color.a * 0.35))
		c.draw_string(f, Vector2.ZERO, lineas[i], HORIZONTAL_ALIGNMENT_LEFT, -1, tam, color)
	c.draw_set_transform(Vector2.ZERO)

# Texto a máquina: letras con presión desigual
func _maquina(c: Control, lineas: PackedStringArray, origen: Vector2, tam: int, interlineado: float, color := TINTA_MAQUINA, fuente: Font = null) -> void:
	var f: Font = fuente if fuente else MAQUINA
	for i in lineas.size():
		var x := 0.0
		for letra in lineas[i]:
			var tinta := Color(color, color.a * _azar.randf_range(0.7, 1.0))
			c.draw_char(f, origen + Vector2(x, i * interlineado + _azar.randf_range(-0.8, 0.8)), letra, tam, tinta)
			x += f.get_char_size(letra.unicode_at(0), tam).x

func _renglones(c: Control, rect: Rect2, paso: float, color: Color, margen := -1.0) -> void:
	var y := rect.position.y
	while y < rect.end.y:
		c.draw_line(Vector2(rect.position.x, y), Vector2(rect.end.x, y), color, 2.0)
		y += paso
	if margen > 0:
		c.draw_line(Vector2(rect.position.x + margen, rect.position.y - paso), Vector2(rect.position.x + margen, rect.end.y), Color(0.75, 0.25, 0.25, 0.6), 2.5)

# Pliegue de papel: una línea más oscura y otra clara al lado
func _pliegue(c: Control, desde: Vector2, hasta: Vector2) -> void:
	c.draw_line(desde, hasta, Color(0, 0, 0, 0.12), 3.0)
	c.draw_line(desde + Vector2(0, 3), hasta + Vector2(0, 3), Color(1, 1, 1, 0.18), 3.0)

# Manchas suaves (humedad, café, dedos)
func _manchas(c: Control, tam: Vector2, cuantas: int, color: Color, radio := Vector2(30, 140)) -> void:
	for i in cuantas:
		var centro := Vector2(_azar.randf() * tam.x, _azar.randf() * tam.y)
		var r := _azar.randf_range(radio.x, radio.y)
		for k in 6:
			c.draw_circle(centro + Vector2(_azar.randf_range(-r, r), _azar.randf_range(-r, r)) * 0.3, r * (1.0 - k * 0.13), Color(color, color.a * 0.18))

# Firma: un garabato con la fuente más suelta
func _firma(c: Control, texto: String, p: Vector2, tam: int, color: Color) -> void:
	c.draw_set_transform(p, -0.08)
	c.draw_string(MANO_LIBRE, Vector2.ZERO, texto, HORIZONTAL_ALIGNMENT_LEFT, -1, tam, color)
	c.draw_set_transform(Vector2.ZERO)

# Tachón: rayas apretadas de un lado a otro hasta que no se lee
func _tachon(c: Control, rect: Rect2, color: Color) -> void:
	var y := rect.position.y
	while y < rect.end.y:
		var p0 := Vector2(rect.position.x + _azar.randf_range(-6, 6), y + _azar.randf_range(-3, 3))
		var p1 := Vector2(rect.end.x + _azar.randf_range(-6, 6), y + _azar.randf_range(-3, 3) + 6)
		c.draw_line(p0, p1, color, _azar.randf_range(5, 9), true)
		y += 4.5
	for i in 18:
		var a := Vector2(_azar.randf_range(rect.position.x, rect.end.x), rect.position.y)
		var b := Vector2(_azar.randf_range(rect.position.x, rect.end.x), rect.end.y)
		c.draw_line(a, b, color, 4.0, true)

# ---------- Documentos de cada pista ----------

const TINTE_VIEJO := Color(0.95, 0.9, 0.8)
const TINTE_AMARILLO := Color(0.96, 0.86, 0.62)
var _foto: ImageTexture

func _documentos() -> void:
	await _render("nota_comisario_frente", Vector2i(1024, 1434), PAPEL_BLANCO, _doc_nota_comisario_frente)
	await _render("nota_comisario_reverso", Vector2i(1024, 1434), PAPEL_BLANCO, _doc_nota_comisario_reverso)
	await _render("libro_turnos_paginas", Vector2i(2048, 1400), PAPEL_VIEJO, _doc_libro_turnos, Color(1, 0.97, 0.9))
	await _render("quemado_hoja_a", Vector2i(1400, 1000), PAPEL_VIEJO, _doc_quemado_a, TINTE_VIEJO)
	await _render("quemado_hoja_b", Vector2i(1400, 1000), PAPEL_VIEJO, _doc_quemado_b, TINTE_VIEJO)
	await _render("quemado_hoja_c", Vector2i(1400, 1000), PAPEL_VIEJO, _doc_quemado_c, TINTE_VIEJO)
	await _render("quemado_reverso", Vector2i(1400, 1000), PAPEL_VIEJO, _doc_quemado_reverso, TINTE_VIEJO)
	await _render("quemado_reverso_nombre", Vector2i(1400, 1000), PAPEL_VIEJO, _doc_quemado_reverso_nombre, TINTE_VIEJO)
	await _render("nota_porky_frente", Vector2i(1024, 1024), PAPEL_BLANCO, _doc_nota_porky_frente)
	await _render("nota_porky_reverso", Vector2i(1024, 1024), PAPEL_BLANCO, _doc_nota_porky_reverso)
	var svg := Image.new()
	svg.load_svg_from_string(FileAccess.get_file_as_string("res://Assets/Caso1/Texturas/foto_grupo_hd.svg"), 1.6)
	_foto = ImageTexture.create_from_image(svg)
	await _render("foto_grupo_arte", Vector2i(2048, 1198), null, _doc_foto_arte)
	await _render("foto_grupo_reverso", Vector2i(1536, 1024), CARTON, _doc_foto_reverso, Color(0.95, 0.85, 0.7))
	await _render("recorte_frente", Vector2i(1100, 1500), PAPEL_VIEJO, _doc_recorte_frente, TINTE_AMARILLO)
	await _render("recorte_reverso", Vector2i(1100, 1500), PAPEL_VIEJO, _doc_recorte_reverso, TINTE_AMARILLO)
	await _render("oferta_frente", Vector2i(1275, 1650), PAPEL_BLANCO, _doc_oferta_frente)
	await _render("oferta_reverso", Vector2i(1275, 1650), PAPEL_BLANCO, _doc_oferta_reverso)
	await _render("registro_enfermeria_paginas", Vector2i(2048, 1434), PAPEL_BLANCO, _doc_registro_enfermeria)
	await _render("registro_enfermeria_surcos", Vector2i(1024, 160), null, _doc_registro_surcos)
	await _render("registro_llamadas_frente", Vector2i(1240, 1754), PAPEL_BLANCO, _doc_registro_llamadas)
	await _render("hoja_reverso", Vector2i(1024, 1434), PAPEL_BLANCO, _doc_vacio)
	await _render("inventario_baul", Vector2i(1024, 1434), PAPEL_VIEJO, _doc_inventario, Color(0.97, 0.92, 0.82))
	await _render("cinta_etiqueta", Vector2i(1024, 384), PAPEL_BLANCO, _doc_cinta_etiqueta)
	await _render("grabadora_etiqueta", Vector2i(1024, 560), PAPEL_BLANCO, _doc_grabadora_etiqueta, Color(1, 0.97, 0.85))
	await _render("envoltorio_papel", Vector2i(1024, 1024), PAPEL_BLANCO, _doc_envoltorio, Color(1, 0.98, 0.92))

func _doc_vacio(_c: Control) -> void:
	pass

# Caso 0 · Nota del comisario: memorándum a máquina, doblado en tres, firmado «B.»
func _doc_nota_comisario_frente(c: Control) -> void:
	_maquina(c, ["COMISARÍA CENTRAL", "MEMORÁNDUM INTERNO"], Vector2(90, 150), 44, 56)
	c.draw_line(Vector2(90, 250), Vector2(934, 250), Color(TINTA_MAQUINA, 0.7), 3)
	_maquina(c, ["Para: archivo / Sala 1", "Asunto: guardia nocturna"], Vector2(90, 330), 34, 50)
	_maquina(c, ["Dejar la grabadora de la Sala 1", "en REC esta noche.", "", "Veremos quién duerme en la", "guardia."], Vector2(90, 520), 40, 62)
	_mano(c, ["— B."], Vector2(600, 940), 92, TINTA_AZUL, 0)
	_pliegue(c, Vector2(0, 478), Vector2(1024, 478))
	_pliegue(c, Vector2(0, 956), Vector2(1024, 956))
	_manchas(c, Vector2(1024, 1434), 3, Color(0.55, 0.45, 0.3, 0.5))

func _doc_nota_comisario_reverso(c: Control) -> void:
	# Se transparenta un poco lo escrito al frente (al revés)
	c.draw_set_transform(Vector2(1024, 0), 0, Vector2(-1, 1))
	_maquina(c, ["Dejar la grabadora de la Sala 1", "en REC esta noche."], Vector2(90, 520), 40, 62, Color(TINTA_MAQUINA, 0.06))
	c.draw_set_transform(Vector2.ZERO)
	_pliegue(c, Vector2(0, 478), Vector2(1024, 478))
	_pliegue(c, Vector2(0, 956), Vector2(1024, 956))

# Caso 0 · Libro de turnos: dos páginas con renglones, firmas y la nota del margen
func _doc_libro_turnos(c: Control) -> void:
	for lado in 2:
		var x0 := lado * 1024.0
		_renglones(c, Rect2(x0 + 70, 260, 900, 1080), 64, Color(0.35, 0.45, 0.65, 0.35), 110)
		for col in [360.0, 560.0, 760.0]:
			c.draw_line(Vector2(x0 + col, 196), Vector2(x0 + col, 1340), Color(0.35, 0.45, 0.65, 0.3), 2)
	_maquina(c, ["LIBRO DE TURNOS · GUARDIA NOCTURNA"], Vector2(120, 140), 34, 0, Color(0.15, 0.2, 0.35, 0.8))
	_maquina(c, ["Fecha    Agente        Entrada  Salida  Firma"], Vector2(150, 236), 24, 0, Color(0.15, 0.2, 0.35, 0.8))
	_mano(c, ["11/IX   Agente Ruiz       22:00    2:00", "11/IX   Porky (agente)    2:00     6:00", "12/IX   Agente Ruiz       22:00    2:00"], Vector2(150, 316), 46, TINTA_NEGRA, 64)
	_firma(c, "Ruiz", Vector2(840, 312), 34, TINTA_NEGRA)
	_firma(c, "Porky", Vector2(830, 376), 34, TINTA_NEGRA)
	_firma(c, "Ruiz", Vector2(840, 440), 34, TINTA_NEGRA)
	_maquina(c, ["Fecha    Agente        Entrada  Salida  Firma"], Vector2(1174, 236), 24, 0, Color(0.15, 0.2, 0.35, 0.8))
	_mano(c, ["12/IX   Eriz (conserje)    18:00    1:00", "13/IX   Guardia: Porky       2:00     4:00", "13/IX   Guardia:"], Vector2(1174, 316), 46, TINTA_NEGRA, 64)
	_firma(c, "Eriz", Vector2(1870, 312), 34, TINTA_AZUL)
	_firma(c, "Porky", Vector2(1856, 376), 34, TINTA_NEGRA)
	# Nota del comisario en el margen, con otra letra y en rojo
	c.draw_set_transform(Vector2(1110, 1250), -1.45)
	c.draw_string(MANO_FIRME, Vector2.ZERO, "Grabadora Sala 1: NO TOCAR", HORIZONTAL_ALIGNMENT_LEFT, -1, 50, TINTA_ROJA)
	c.draw_set_transform(Vector2.ZERO)
	_manchas(c, Vector2(2048, 1400), 5, Color(0.5, 0.4, 0.25, 0.6))
	_sombra_lomo(c, 1024, 1400)

func _sombra_lomo(c: Control, x: float, alto: float) -> void:
	for i in 40:
		var a := 0.003 * (40 - i)
		c.draw_line(Vector2(x - i, 0), Vector2(x - i, alto), Color(0, 0, 0, a), 1)
		c.draw_line(Vector2(x + i, 0), Vector2(x + i, alto), Color(0, 0, 0, a), 1)

# Caso 1 · Libro de guardia quemado: tres hojas (frente) y el reverso con el nombre
func _hoja_guardia(c: Control, lineas: PackedStringArray, origen: Vector2, renglones := 0.3) -> void:
	_renglones(c, Rect2(60, 140, 1280, 820), 70, Color(0.35, 0.45, 0.65, renglones))
	if not lineas.is_empty():
		_mano(c, lineas, origen, 66, TINTA_AZUL, 70)

func _doc_quemado_a(c: Control) -> void:
	_hoja_guardia(c, ["Guardia nocturna, 14 de agosto", "Ronda de las 22:00 sin novedad.", "Cabañas 1 a 3 en silencio."], Vector2(250, 260))

func _doc_quemado_b(c: Control) -> void:
	_hoja_guardia(c, ["...la cabaña 4 a las 23:00...", "...olor a alcohol...", "...no contesta."], Vector2(260, 330))

func _doc_quemado_c(c: Control) -> void:
	_hoja_guardia(c, ["Revisión de lámparas y botes.", "Inventario del muelle."], Vector2(260, 400))

func _doc_quemado_reverso(c: Control) -> void:
	_hoja_guardia(c, [], Vector2.ZERO, 0.25)

# El reverso se dibuja al revés: en la malla usa las mismas UV que el frente (para que el borde quemado
# coincida) y al voltear la hoja el texto se lee derecho
func _doc_quemado_reverso_nombre(c: Control) -> void:
	_renglones(c, Rect2(60, 140, 1280, 820), 70, Color(0.35, 0.45, 0.65, 0.25))
	c.draw_set_transform(Vector2(1400, 0), 0, Vector2(-1, 1))
	_mano(c, ["...de guardia:", "monitor auxiliar..."], Vector2(300, 420), 70, TINTA_AZUL, 74)
	c.draw_set_transform(Vector2.ZERO)

# Caso 1 · Nota de Porky: letra redonda, doblada a la mitad, tinta corrida
func _doc_nota_porky_frente(c: Control) -> void:
	_renglones(c, Rect2(60, 160, 904, 820), 86, Color(0.4, 0.5, 0.7, 0.3), 90)
	_mano(c, ["Esta noche se lo digo.", "Ya tengo el libro.", "", "Por Paco."], Vector2(140, 290), 92, TINTA_AZUL, 104, MANO_FIRME, -0.02)
	_manchas(c, Vector2(1024, 1024), 2, Color(0.15, 0.2, 0.45, 0.8), Vector2(20, 60))
	_pliegue(c, Vector2(512, 0), Vector2(512, 1024))

func _doc_nota_porky_reverso(c: Control) -> void:
	_renglones(c, Rect2(60, 160, 904, 820), 86, Color(0.4, 0.5, 0.7, 0.18))
	_pliegue(c, Vector2(512, 0), Vector2(512, 1024))

# Caso 1 · Foto del grupo: la foto sepia con borde blanco sobre el paspartú del marco (con las iniciales del silbato)
func _doc_foto_arte(c: Control) -> void:
	c.draw_rect(Rect2(0, 0, 2048, 1198), Color(0.93, 0.91, 0.86))
	var alto := 1060.0
	var ancho := alto * 1.164
	var rect := Rect2((2048 - ancho) / 2, (1198 - alto) / 2, ancho, alto)
	c.draw_rect(rect.grow(6), Color(0, 0, 0, 0.18))
	c.draw_texture_rect(_foto, rect, false)
	# Iniciales en el silbato: solo se leen acercándose
	var silbato := rect.position + Vector2(0.186, 0.513) * rect.size
	c.draw_string(IMPRESO, silbato + Vector2(-16, 6), "G.S.", HORIZONTAL_ALIGNMENT_LEFT, -1, 18, Color(0.15, 0.11, 0.07))

func _doc_foto_reverso(c: Control) -> void:
	c.draw_rect(Rect2(0, 0, 1536, 1024), Color(0.62, 0.5, 0.36, 0.55))
	_mano(c, ["Lago Espejo · 14 de agosto", "", "Para Paco, que nunca", "le tuvo miedo al lago.", "", "                        — G. S."], Vector2(380, 300), 70, Color(0.14, 0.12, 0.3, 0.9), 76, MANO, -0.03)

# Caso 1 · Recorte de periódico amarillento (frente y otra nota al reverso)
func _doc_recorte_frente(c: Control) -> void:
	var tinta := Color(0.1, 0.09, 0.08)
	c.draw_string(PERIODICO_NEGRITA, Vector2(60, 110), "EL ECO DEL LAGO", HORIZONTAL_ALIGNMENT_LEFT, -1, 52, tinta)
	c.draw_line(Vector2(60, 140), Vector2(1040, 140), tinta, 3)
	c.draw_string(PERIODICO, Vector2(60, 172), "Sábado 16 de agosto · Sección Regional", HORIZONTAL_ALIGNMENT_LEFT, -1, 26, tinta)
	c.draw_multiline_string(PERIODICO_NEGRITA, Vector2(60, 270), "Muere ahogado un campista de 12 años en el Lago Espejo", HORIZONTAL_ALIGNMENT_LEFT, 980, 70, -1, tinta)
	c.draw_multiline_string(PERIODICO, Vector2(60, 470), "¿Nadie cuidaba el lago? La guardia no notó que Paco salió de su cabaña de noche.", HORIZONTAL_ALIGNMENT_LEFT, 980, 36, -1, tinta)
	# Foto borrosa del muelle
	c.draw_rect(Rect2(60, 580, 460, 300), Color(0.32, 0.3, 0.27))
	c.draw_rect(Rect2(60, 760, 460, 120), Color(0.22, 0.21, 0.2))
	c.draw_rect(Rect2(140, 700, 260, 22), Color(0.16, 0.15, 0.14))
	c.draw_string(PERIODICO, Vector2(60, 915), "El muelle del Campamento Pino Lunar.", HORIZONTAL_ALIGNMENT_LEFT, -1, 22, tinta)
	var cuerpo := "El menor fue encontrado la mañana del viernes cerca del muelle. Según los monitores, se había ido a dormir a las 21:30 con el resto de la cabaña 4. Nadie lo vio salir. La dirección del campamento informó que la guardia de esa noche hizo sus rondas con normalidad. La familia pidió que se investigue. Las autoridades no descartan ninguna línea."
	c.draw_multiline_string(PERIODICO, Vector2(560, 610), cuerpo, HORIZONTAL_ALIGNMENT_LEFT, 480, 25, -1, tinta)
	c.draw_multiline_string(PERIODICO, Vector2(60, 980), "Es la primera tragedia en los 30 años del campamento. El grupo de la cabaña 4 regresará a casa el lunes.", HORIZONTAL_ALIGNMENT_LEFT, 980, 25, -1, tinta)
	# «La guardia» subrayado con pluma roja
	var antes := PERIODICO.get_string_size("¿Nadie cuidaba el lago? ", HORIZONTAL_ALIGNMENT_LEFT, -1, 36).x
	var largo := PERIODICO.get_string_size("La guardia", HORIZONTAL_ALIGNMENT_LEFT, -1, 36).x
	c.draw_line(Vector2(60 + antes, 484), Vector2(60 + antes + largo, 482), TINTA_ROJA, 5, true)
	c.draw_line(Vector2(65 + antes, 492), Vector2(55 + antes + largo, 491), TINTA_ROJA, 3, true)
	_manchas(c, Vector2(1100, 1500), 4, Color(0.55, 0.42, 0.2, 0.7))

func _doc_recorte_reverso(c: Control) -> void:
	var tinta := Color(0.1, 0.09, 0.08)
	c.draw_string(PERIODICO_NEGRITA, Vector2(60, 120), "CLASIFICADOS", HORIZONTAL_ALIGNMENT_LEFT, -1, 46, tinta)
	var avisos := ["Se vende lancha de remos, buen estado. Informes en el muelle.", "Clases de guitarra a domicilio. Precios módicos.", "Rento cuarto amueblado cerca de la plaza.", "Se busca cocinero para la temporada de verano.", "Compro antigüedades y relojes de pared."]
	for i in avisos.size():
		c.draw_multiline_string(PERIODICO, Vector2(60, 220 + i * 230), avisos[i], HORIZONTAL_ALIGNMENT_LEFT, 960, 34, -1, tinta)
		c.draw_line(Vector2(60, 390 + i * 230), Vector2(1040, 390 + i * 230), Color(tinta, 0.6), 2)
	_manchas(c, Vector2(1100, 1500), 4, Color(0.55, 0.42, 0.2, 0.7))

# Caso 1 · Oferta de compra (se rompe en cuatro pedazos en el modelo)
func _doc_oferta_frente(c: Control) -> void:
	c.draw_string(PERIODICO_NEGRITA, Vector2(100, 150), "Desarrollos Valle Alto, S. A.", HORIZONTAL_ALIGNMENT_LEFT, -1, 48, Color(0.12, 0.2, 0.32))
	c.draw_line(Vector2(100, 180), Vector2(1175, 180), Color(0.12, 0.2, 0.32), 4)
	_maquina(c, ["OFERTA DE COMPRA"], Vector2(100, 290), 46, 0)
	_maquina(c, ["Inmueble: Cabaña 4 del Campamento", "Pino Lunar, a la orilla del Lago", "Espejo. Ocupante: Porky.", "", "Monto ofrecido: el indicado en el", "anexo A, a cambio de desocupar la", "cabaña antes de la firma de la", "compraventa del campamento.", "", "La oferta vence en 15 días a partir", "de la firma de este documento."], Vector2(100, 420), 34, 54)
	_maquina(c, ["Atentamente,"], Vector2(100, 1160), 34, 0)
	_firma(c, "Miawrio", Vector2(160, 1300), 64, TINTA_AZUL)
	c.draw_line(Vector2(100, 1330), Vector2(620, 1330), Color(0.1, 0.1, 0.1, 0.7), 2)
	_maquina(c, ["Miawrio · Representante legal"], Vector2(100, 1380), 28, 0)

func _doc_oferta_reverso(c: Control) -> void:
	c.draw_set_transform(Vector2(1275, 0), 0, Vector2(-1, 1))
	_maquina(c, ["OFERTA DE COMPRA"], Vector2(100, 290), 46, 0, Color(TINTA_MAQUINA, 0.05))
	c.draw_set_transform(Vector2.ZERO)

# Caso 1 · Registro de la enfermería: libreta abierta, la línea de las 23:20 tachada
func _doc_registro_enfermeria(c: Control) -> void:
	for lado in 2:
		_renglones(c, Rect2(lado * 1024 + 60, 200, 900, 1180), 76, Color(0.4, 0.5, 0.7, 0.35), 100)
	_mano(c, ["13 de agosto", "09:30 picadura de abeja", "13:10 dolor de estómago", "16:40 raspón en codo", "21:50 todo en calma"], Vector2(180, 182), 62, TINTA_AZUL, 76)
	_mano(c, ["REGISTRO — 14 de agosto", "22:10 raspón en rodilla", "23:20 se escuchó algo en el lago", "00:40 todo en calma"], Vector2(1204, 182), 62, TINTA_AZUL, 76)
	_tachon(c, Rect2(1196, 300, 800, 70), Color(0.05, 0.06, 0.12, 0.97))
	_sombra_lomo(c, 1024, 1434)

# Los surcos que deja la pluma bajo el tachón: solo se ven con la linterna (capa aparte)
func _doc_registro_surcos(c: Control) -> void:
	c.draw_string(MANO, Vector2(24, 96), "23:20 se escuchó algo en el lago", HORIZONTAL_ALIGNMENT_LEFT, -1, 62, Color(0.96, 0.92, 0.82, 0.92))

# Caso 1 · Registro de llamadas: hoja impresa
func _doc_registro_llamadas(c: Control) -> void:
	_maquina(c, ["REGISTRO DE LLAMADAS", "Línea: Miawrio · 55 1 ••• •• 48", "Periodo: 14 al 15 de agosto", "", "FECHA   HORA   NÚMERO            DURACIÓN", "-----------------------------------------", "14/08   18:05  55 1 204 77 10     00:02:31", "14/08   21:40  55 1 330 12 08     00:00:47", "14/08   23:00  55 1 871 56 23     00:40:02", "15/08   00:12  55 1 204 77 10     00:03:05", "15/08   00:31  55 1 330 12 08     00:00:40"], Vector2(110, 180), 28, 62, Color(0.1, 0.1, 0.12, 0.92), IMPRESO)
	# La llamada de las 23:00 a las 23:40: la coartada de Miawrio
	c.draw_rect(Rect2(110, 676 - 34, 1020, 46), Color(1, 0.85, 0.1, 0.35))

# Caso 1 · Inventario del baúl
func _doc_inventario(c: Control) -> void:
	_renglones(c, Rect2(70, 300, 880, 1060), 90, Color(0.4, 0.5, 0.7, 0.3), 90)
	_maquina(c, ["INVENTARIO · DISFRACES", "Fogón de los cuentos"], Vector2(100, 150), 40, 56)
	_mano(c, ["Capa roja                ✓", "Corona dorada            ✓", "Máscara de búho          ✓", "Cornamenta de ciervo   ✗", "", "Sacó:  ____________"], Vector2(180, 380), 64, TINTA_NEGRA, 90)
	_manchas(c, Vector2(1024, 1434), 3, Color(0.5, 0.4, 0.25, 0.6))

# Etiquetas: la de la cinta y la de debajo de la grabadora
func _doc_cinta_etiqueta(c: Control) -> void:
	c.draw_rect(Rect2(0, 0, 1024, 70), Color(0.75, 0.2, 0.15, 0.8))
	_renglones(c, Rect2(40, 190, 944, 160), 72, Color(0.3, 0.3, 0.4, 0.4))
	_mano(c, ["Confesión · T. Garras", "Sala 1 · 12/IX"], Vector2(60, 170), 84, TINTA_NEGRA, 86, MANO_FIRME, -0.01)

func _doc_grabadora_etiqueta(c: Control) -> void:
	_mano(c, ["SALA 1", "NO DETENER hasta", "el cambio de turno.", "            — B."], Vector2(90, 130), 92, TINTA_AZUL, 108, MANO_FIRME, -0.015)

func _doc_envoltorio(c: Control) -> void:
	c.draw_rect(Rect2(0, 0, 1024, 1024), Color(0.98, 0.95, 0.86, 0.35))
	c.draw_set_transform(Vector2(340, 440), -0.2)
	c.draw_arc(Vector2(150, 0), 150, 0, TAU, 48, Color(0.6, 0.15, 0.1, 0.55), 8)
	c.draw_string(PERIODICO_NEGRITA, Vector2(40, -10), "TORTERÍA", HORIZONTAL_ALIGNMENT_LEFT, -1, 46, Color(0.6, 0.15, 0.1, 0.6))
	c.draw_string(PERIODICO, Vector2(62, 44), "Jamón · Queso", HORIZONTAL_ALIGNMENT_LEFT, -1, 34, Color(0.6, 0.15, 0.1, 0.6))
	c.draw_set_transform(Vector2.ZERO)
	_manchas(c, Vector2(1024, 1024), 6, Color(0.7, 0.55, 0.25, 0.9), Vector2(20, 70))

# ---------- Imágenes procedurales (sin texto) ----------

func _guardar(img: Image, nombre: String) -> void:
	img.save_png(ProjectSettings.globalize_path(SALIDA + nombre + ".png"))
	print("  ", nombre)

func _ruido(semilla: int, frecuencia: float, tipo := FastNoiseLite.TYPE_SIMPLEX_SMOOTH) -> FastNoiseLite:
	var n := FastNoiseLite.new()
	n.seed = semilla
	n.frequency = frecuencia
	n.noise_type = tipo
	n.fractal_octaves = 4
	return n

# Bordes quemados: alfa recortada con ruido y una franja de carbón alrededor
func _quemar(nombre_base: String, nombre: String, semilla: int) -> void:
	var img := Image.load_from_file(TEMPORAL + nombre_base + ".png")
	img.convert(Image.FORMAT_RGBA8)
	var w := img.get_width()
	var h := img.get_height()
	var n := _ruido(semilla, 0.004)
	var fino := _ruido(semilla + 7, 0.03)
	for y in h:
		for x in w:
			var u := float(x) / w
			var v := float(y) / h
			var borde := minf(minf(u, 1.0 - u), minf(v, 1.0 - v) * 1.4)
			var q := borde + n.get_noise_2d(x, y) * 0.09 + fino.get_noise_2d(x, y) * 0.02 - 0.05
			var c := img.get_pixel(x, y)
			if q < 0.0:
				img.set_pixel(x, y, Color(0, 0, 0, 0))
			elif q < 0.05:
				# Carbón: negro con brasa naranja apagada justo en la orilla
				var t := q / 0.05
				var carbon := Color(0.05, 0.035, 0.025).lerp(Color(0.35, 0.12, 0.04), clampf(1.0 - t * 3.0, 0, 1) * 0.5)
				img.set_pixel(x, y, Color(carbon, 1))
			elif q < 0.14:
				var t := (q - 0.05) / 0.09
				img.set_pixel(x, y, c.lerp(Color(0.32, 0.2, 0.1), (1.0 - t) * 0.85))
	_guardar(img, nombre)

# Mapa normal a partir de una imagen de alturas (en gris)
func _normal_desde_alturas(alturas: Image, fuerza: float) -> Image:
	var w := alturas.get_width()
	var h := alturas.get_height()
	var nrm := Image.create_empty(w, h, false, Image.FORMAT_RGB8)
	for y in h:
		for x in w:
			var izq := alturas.get_pixel(maxi(x - 1, 0), y).r
			var der := alturas.get_pixel(mini(x + 1, w - 1), y).r
			var arr := alturas.get_pixel(x, maxi(y - 1, 0)).r
			var aba := alturas.get_pixel(x, mini(y + 1, h - 1)).r
			var v := Vector3((izq - der) * fuerza, (arr - aba) * fuerza, 1.0).normalized()
			nrm.set_pixel(x, y, Color(v.x * 0.5 + 0.5, v.y * 0.5 + 0.5, v.z * 0.5 + 0.5))
	return nrm

func _imagenes() -> void:
	# Hojas quemadas del libro de guardia
	_quemar("quemado_hoja_a", "quemado_hoja_a_q", 11)
	_quemar("quemado_hoja_b", "quemado_hoja_b_q", 12)
	_quemar("quemado_hoja_c", "quemado_hoja_c_q", 13)
	# Cada reverso con la misma semilla que su frente: el borde quemado coincide
	_quemar("quemado_reverso", "quemado_reverso_a_q", 11)
	_quemar("quemado_reverso", "quemado_reverso_b_q", 12)
	_quemar("quemado_reverso_nombre", "quemado_reverso_nombre_q", 13)

	# Huella de bota reglamentaria: suela con tacos, en barro (albedo con alfa y normal)
	var w := 384
	var h := 1024
	var alto := Image.create_empty(w, h, false, Image.FORMAT_RGBA8)
	alto.fill(Color(0, 0, 0, 1))
	var forma := func(x: float, y: float) -> float:
		# Puntera ancha, cintura angosta y tacón: el contorno de una bota
		var ancho := 0.0
		if y < 0.06 or y > 0.97:
			return 0.0
		elif y < 0.5:
			ancho = 0.42 * sqrt(clampf(1.0 - pow((y - 0.3) / 0.25, 2), 0, 1))
		elif y < 0.64:
			ancho = 0.42 * sqrt(clampf(1.0 - pow(0.2 / 0.25, 2), 0, 1)) * (1.0 - (y - 0.5) / 0.14 * 0.25)
		else:
			ancho = 0.34 * sqrt(clampf(1.0 - pow((y - 0.8) / 0.17, 2), 0, 1))
		return 1.0 if absf(x - 0.5 + (0.5 - y) * 0.06) < ancho else 0.0
	var barro := _ruido(31, 0.02)
	for y in h:
		for x in w:
			var u := float(x) / w
			var v := float(y) / h
			if forma.call(u, v) < 0.5:
				continue
			# Tacos en barras cruzadas, un canal al centro y la cintura lisa
			var taco := 1.0 if (int(v * 30) % 2 == 0) else 0.2
			if absf(u - 0.5) < 0.03:
				taco = 0.2
			if v > 0.52 and v < 0.64:
				taco = 0.05
			var a := clampf(0.75 + barro.get_noise_2d(x, y) * 0.5, 0, 1)
			alto.set_pixel(x, y, Color(taco, taco, taco, a))
	var albedo := Image.create_empty(w, h, false, Image.FORMAT_RGBA8)
	for y in h:
		for x in w:
			var p := alto.get_pixel(x, y)
			var c := Color(0.14, 0.09, 0.05).lerp(Color(0.36, 0.25, 0.14), p.r)
			albedo.set_pixel(x, y, Color(c, p.a * (0.35 + p.r * 0.6)))
	_guardar(albedo, "huella_albedo")
	_guardar(_normal_desde_alturas(alto, 4.0), "huella_normal")

	# Cristal estrellado del reloj: grietas que salen de un punto de golpe
	var grieta := Image.create_empty(1024, 1024, false, Image.FORMAT_RGBA8)
	grieta.fill(Color(1, 1, 1, 0))
	var golpe := Vector2(640, 380)
	var lineas: Array = []
	for i in 14:
		var ang := i * TAU / 14 + _azar.randf_range(-0.2, 0.2)
		var p := golpe
		var largo := _azar.randf_range(250, 620)
		var paso := 0.0
		while paso < largo:
			var q := p + Vector2.from_angle(ang + _azar.randf_range(-0.35, 0.35)) * 18
			lineas.append([p, q])
			p = q
			paso += 18
	for i in 4:
		var r := 60.0 + i * 70
		var prev := golpe + Vector2(r, 0)
		for k in range(1, 37):
			var q := golpe + Vector2.from_angle(k * TAU / 36) * (r + _azar.randf_range(-12, 12))
			if _azar.randf() < 0.6:
				lineas.append([prev, q])
			prev = q
	for l in lineas:
		var a: Vector2 = l[0]
		var b: Vector2 = l[1]
		var pasos := int(a.distance_to(b))
		for s in pasos:
			var p := a.lerp(b, float(s) / pasos)
			for dy in range(-1, 2):
				for dx in range(-1, 2):
					var x := int(p.x) + dx
					var y := int(p.y) + dy
					if x >= 0 and y >= 0 and x < 1024 and y < 1024:
						# Línea oscura con un filo claro: se nota sobre la carátula blanca
						grieta.set_pixel(x, y, Color(0.25, 0.25, 0.28, 0.8) if dx == 0 and dy == 0 else Color(1, 1, 1, 0.45))
	# Polvo encima del cristal
	var polvo := _ruido(41, 0.01)
	for y in 1024:
		for x in 1024:
			var c := grieta.get_pixel(x, y)
			var d := clampf(polvo.get_noise_2d(x, y) * 0.5 + 0.2, 0, 0.22)
			if c.a < d:
				grieta.set_pixel(x, y, Color(0.75, 0.72, 0.66, d))
	for y in 1024:
		for x in 1024:
			if Vector2(x - 512, y - 512).length() > 505:
				grieta.set_pixel(x, y, Color(1, 1, 1, 0))
	_guardar(grieta, "reloj_grietas")

	# Pluma negra: raquis y barbas (alfa) con un brillo azulado
	var pw := 512
	var ph := 2048
	var pluma := Image.create_empty(pw, ph, false, Image.FORMAT_RGBA8)
	pluma.fill(Color(0, 0, 0, 0))
	var irregular := _ruido(51, 0.02)
	for y in ph:
		var v := float(y) / ph
		# v = 0 punta, v = 1 base (el cañón queda desnudo al final)
		var t := clampf(v / 0.84, 0, 1)
		var media := pow(sin(t * PI), 0.6) * 0.3 * (1.0 if v < 0.84 else 0.0) * (0.75 + 0.25 * t)
		for x in pw:
			var u := float(x) / pw - 0.5
			var dist := absf(u)
			if dist < 0.012:
				pluma.set_pixel(x, y, Color(0.12, 0.11, 0.1, 1))
				continue
			var limite := media * (1.0 + irregular.get_noise_2d(x * 0.2, y) * 0.25)
			if dist > limite:
				continue
			# Barbas diagonales: rayas finas
			var barba := fposmod((y + dist * pw * 1.6) * 0.5, 1.0)
			if barba < 0.25 and dist > limite * 0.85:
				continue
			var brillo := 0.14 + 0.12 * (1.0 - dist / maxf(limite, 0.001)) + (0.06 if barba < 0.5 else 0.0)
			pluma.set_pixel(x, y, Color(brillo * 0.7, brillo * 0.8, brillo * 1.2, 1))
	_guardar(pluma, "pluma")

	# Manchas de chocolate de la taza (alrededor del borde, para el mapeo cilíndrico)
	var taza := Image.create_empty(1024, 512, false, Image.FORMAT_RGBA8)
	taza.fill(Color(0.94, 0.92, 0.87, 1))
	# Escurrimientos: gotas que bajan del borde, delgadas y que terminan en punta
	for i in 4:
		var x0 := _azar.randi_range(0, 1023)
		var largo := _azar.randf_range(50, 170)
		var ancho := _azar.randf_range(6, 12)
		for y in int(largo):
			var t := float(y) / largo
			var media := ancho * (1.0 - t * 0.7) + (ancho * 0.6 if t > 0.85 else 0.0)
			for dx in range(-int(media), int(media) + 1):
				var x := (x0 + dx + int(sin(y * 0.05) * 3)) % 1024
				taza.set_pixel(x, y, taza.get_pixel(x, y).lerp(Color(0.32, 0.18, 0.1), 0.7 * (1.0 - t * 0.6)))
	# Anillo de chocolate seco justo bajo el borde
	for x in 1024:
		for y in range(0, 10):
			taza.set_pixel(x, y, taza.get_pixel(x, y).lerp(Color(0.35, 0.2, 0.12), 0.35 * (1.0 - y / 10.0)))
	var manchas := _ruido(61, 0.03)
	for y in 512:
		for x in 1024:
			var m := manchas.get_noise_2d(x, y)
			if m > 0.62:
				taza.set_pixel(x, y, taza.get_pixel(x, y).lerp(Color(0.5, 0.35, 0.22), 0.15))
	_guardar(taza, "taza_manchas")

	# Canto de un bloque de hojas (rayas finas)
	var canto := Image.create_empty(64, 256, false, Image.FORMAT_RGB8)
	for y in 256:
		var c := 0.86 + (0.06 if y % 3 == 0 else 0.0) - (0.08 if y % 11 == 0 else 0.0)
		for x in 64:
			canto.set_pixel(x, y, Color(c, c * 0.97, c * 0.9))
	_guardar(canto, "canto_hojas")

	# Ojo de la llave del candado
	var ojo := Image.create_empty(256, 256, false, Image.FORMAT_RGBA8)
	ojo.fill(Color(0, 0, 0, 0))
	for y in 256:
		for x in 256:
			var p := Vector2(x - 128, y - 100)
			if p.length() < 34 or (absf(p.x) < 14 and p.y > 0 and p.y < 110):
				ojo.set_pixel(x, y, Color(0.02, 0.02, 0.02, 1))
			elif p.length() < 44 or (absf(p.x) < 22 and p.y > 0 and p.y < 118):
				ojo.set_pixel(x, y, Color(0.35, 0.27, 0.12, 0.8))
	_guardar(ojo, "candado_ojo")
