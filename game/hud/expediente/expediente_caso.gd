class_name ExpedienteCaso
extends Control
## Expediente del caso (diseño "Detective Caneloso – Expediente") y su parte fija del HUD:
## carpeta del caso, objetivo actual y guía de teclas.
## Lo instancia hud.gd, que le pasa las teclas: TAB abre/cierra, ESC cierra,
## ESPACIO baja por el informe (SHIFT + ESPACIO sube). También se abre con clic en la carpeta.
## El contenido de cada caso está en res://hud/expediente/casos/caso_XX.tres (InformeCaso).

signal expediente_abierto
signal expediente_cerrado

const RUTA_CASOS := "res://hud/expediente/casos/caso_%s.tres"

const AMBAR := Color("#EFCA08")
const TEXTO := Color("#E8EBEF")
const GRIS := Color("#AEB6BF")
const GRIS2 := Color("#8A939E")
const GRIS3 := Color("#6B747F")
const NUM := Color("#5F6873")
const ROJO := Color("#E0463C")
const ROJO_T := Color("#B8342C")
const PAPEL := Color("#E6DFCF")
const PAPEL2 := Color("#EFE9DA")
const TINTA := Color("#211D17")
const TINTA2 := Color("#6B6356")
const TINTA3 := Color("#4A443A")
const LINEA := Color("#9A9080")

## Fuentes del diseño. Big Shoulders es variable: cada "archivo" del diseño es un grosor.
## Si falta una fuente (Courier Prime, Special Elite), se usa IBM Plex Mono.
const CARPETA_FUENTES := "res://Assets/Fuentes/"
const FUENTE_VARIABLE := "BigShouldersDisplay-Variable.ttf"
const FUENTE_RESPALDO := "IBMPlexMono-Regular.ttf"
const PESOS := {
	"BigShouldersDisplay-Medium.ttf": 500,
	"BigShouldersDisplay-ExtraBold.ttf": 800,
	"BigShouldersDisplay-Black.ttf": 900,
}

## Teclas que muestra la guía de la esquina inferior izquierda.
const TECLAS := [["Q", "Libreta"], ["F", "Linterna"], ["R", "Tablero"], ["TAB", "Expediente"]]

var abierto := false
var informe: InformeCaso
var _caso := ""

var overlay: Control
var marco: PanelContainer
var scroll: ScrollContainer
var lbl_espacio: Label
var desliz := 40.0
var tween: Tween
var tween_scroll: Tween

var _lbl_caso: Label
var _lbl_archivo: Label
var _lbl_objetivo: Label
var _punto_aviso: Panel
var _fuentes := {}


func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	_construir_hud()
	_construir_expediente()
	_aplicar(false, false)


## Carga el informe del caso indicado ("00", "01"...). Si aún no existe, muestra un aviso.
func cargar_caso(caso: String) -> void:
	if caso == _caso and overlay:
		return
	_caso = caso
	var ruta := RUTA_CASOS % caso
	informe = load(ruta) as InformeCaso if ResourceLoader.exists(ruta) else null
	if _lbl_caso:
		_lbl_caso.text = "CASO " + caso
	if overlay:
		var estaba_abierto := abierto
		overlay.queue_free()
		overlay = null
		_construir_expediente()
		_aplicar(estaba_abierto, false)
	# Expediente nuevo: punto rojo en la carpeta hasta que se abra
	if _punto_aviso:
		_punto_aviso.visible = informe != null and not abierto


## Textos de la carpeta y del objetivo actual.
func actualizar_hud(caso: String, archivo: String, objetivo_actual: String) -> void:
	if not _lbl_caso:
		return
	_lbl_caso.text = "CASO " + caso
	_lbl_archivo.text = "Archivo: " + archivo
	_lbl_objetivo.text = objetivo_actual


func abrir() -> void: _aplicar(true)
func cerrar() -> void: _aplicar(false)
func alternar() -> void: _aplicar(not abierto)


func bajar(subir := false) -> void:
	var bar := scroll.get_v_scroll_bar()
	var paso := scroll.size.y * 0.8
	var fin := bar.max_value - bar.page
	var actual := float(scroll.scroll_vertical)
	var destino := actual - paso if subir else (0.0 if actual >= fin - 4 else actual + paso)
	destino = clamp(destino, 0.0, fin)
	if tween_scroll: tween_scroll.kill()
	tween_scroll = create_tween()
	tween_scroll.tween_property(scroll, "scroll_vertical", int(destino), 0.35).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)


func _actualizar_espacio(_v = 0) -> void:
	if not lbl_espacio or not is_instance_valid(scroll): return
	var bar := scroll.get_v_scroll_bar()
	var al_final := scroll.scroll_vertical >= bar.max_value - bar.page - 4
	lbl_espacio.text = "VOLVER ARRIBA" if al_final else "BAJAR"


func _aplicar(v: bool, animar := true) -> void:
	var cambio := v != abierto
	abierto = v
	if v:
		overlay.visible = true
		_punto_aviso.visible = false
	overlay.mouse_filter = Control.MOUSE_FILTER_STOP if v else Control.MOUSE_FILTER_IGNORE
	if tween: tween.kill()
	var a := 1.0 if v else 0.0
	var d := 0.0 if v else 40.0
	if not animar:
		overlay.modulate.a = a
		overlay.visible = v
		_set_desliz(d)
		return
	tween = create_tween().set_parallel()
	tween.tween_property(overlay, "modulate:a", a, 0.26)
	tween.tween_method(_set_desliz, desliz, d, 0.42).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
	# Cerrado del todo: oculto, para que sus botones no reciban clics
	tween.chain().tween_callback(func(): overlay.visible = abierto)
	if cambio:
		(expediente_abierto if v else expediente_cerrado).emit()


# ---------- utilidades ----------

func _fuente(archivo_ttf: String, espaciado := 0) -> Font:
	var clave := "%s|%d" % [archivo_ttf, espaciado]
	if _fuentes.has(clave):
		return _fuentes[clave]
	var fv := FontVariation.new()
	if PESOS.has(archivo_ttf):
		fv.base_font = _cargar_fuente(FUENTE_VARIABLE)
		fv.variation_opentype = {TextServerManager.get_primary_interface().name_to_tag("wght"): PESOS[archivo_ttf]}
	elif ResourceLoader.exists(CARPETA_FUENTES + archivo_ttf):
		fv.base_font = _cargar_fuente(archivo_ttf)
	else:
		fv.base_font = _cargar_fuente(FUENTE_RESPALDO)
	fv.spacing_glyph = espaciado
	_fuentes[clave] = fv
	return fv


func _cargar_fuente(archivo_ttf: String) -> Font:
	var ruta := CARPETA_FUENTES + archivo_ttf
	return load(ruta) if ResourceLoader.exists(ruta) else ThemeDB.fallback_font


func _label(t: String, f: Font, tam: int, c: Color, envolver := false) -> Label:
	var l := Label.new()
	l.text = t
	l.add_theme_font_override("font", f)
	l.add_theme_font_size_override("font_size", tam)
	l.add_theme_color_override("font_color", c)
	l.mouse_filter = Control.MOUSE_FILTER_IGNORE
	if envolver:
		l.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		l.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		l.custom_minimum_size.x = 60
	return l


func _rect(c: Color, tam: Vector2) -> ColorRect:
	var r := ColorRect.new()
	r.color = c
	r.custom_minimum_size = tam
	r.mouse_filter = Control.MOUSE_FILTER_IGNORE
	return r


func _caja(bg: Color, m := Vector4.ZERO, borde := 0, borde_color := Color.TRANSPARENT) -> StyleBoxFlat:
	var s := StyleBoxFlat.new()
	s.bg_color = bg
	s.content_margin_left = m.x
	s.content_margin_top = m.y
	s.content_margin_right = m.z
	s.content_margin_bottom = m.w
	if borde > 0:
		s.set_border_width_all(borde)
		s.border_color = borde_color
	return s


func _panel(estilo: StyleBox) -> PanelContainer:
	var p := PanelContainer.new()
	p.add_theme_stylebox_override("panel", estilo)
	p.mouse_filter = Control.MOUSE_FILTER_IGNORE
	return p


func _vbox(sep: int) -> VBoxContainer:
	var v := VBoxContainer.new()
	v.add_theme_constant_override("separation", sep)
	v.mouse_filter = Control.MOUSE_FILTER_IGNORE
	return v


func _hbox(sep: int) -> HBoxContainer:
	var h := HBoxContainer.new()
	h.add_theme_constant_override("separation", sep)
	h.mouse_filter = Control.MOUSE_FILTER_IGNORE
	return h


func _rotar(c: Control, grados: float) -> void:
	c.rotation_degrees = grados
	c.resized.connect(func(): c.pivot_offset = c.size / 2.0)


func _tecla(texto: String, color: Color, tam := 11) -> PanelContainer:
	var t := _panel(_caja(Color.TRANSPARENT, Vector4(8, 4, 8, 4), 2, color))
	t.add_child(_label(texto, _fuente("CourierPrime-Regular.ttf"), tam, color))
	return t


# ---------- HUD ----------

func _construir_hud() -> void:
	var mono := _fuente("CourierPrime-Regular.ttf")

	var carpeta := _hbox(22)
	carpeta.position = Vector2(48, 36)
	carpeta.mouse_filter = Control.MOUSE_FILTER_STOP
	carpeta.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND
	carpeta.gui_input.connect(func(e):
		if e is InputEventMouseButton and e.pressed and e.button_index == MOUSE_BUTTON_LEFT:
			_aplicar(true))
	add_child(carpeta)

	var icono := Control.new()
	icono.custom_minimum_size = Vector2(54, 44)
	icono.mouse_filter = Control.MOUSE_FILTER_IGNORE
	icono.add_child(_rect(Color("#C99E05"), Vector2(22, 10)))
	var cuerpo := _rect(AMBAR, Vector2(54, 38))
	cuerpo.position.y = 6
	icono.add_child(cuerpo)
	_punto_aviso = Panel.new()
	var sp := _caja(ROJO, Vector4.ZERO, 3, Color("#0c0e12"))
	sp.set_corner_radius_all(9)
	_punto_aviso.add_theme_stylebox_override("panel", sp)
	_punto_aviso.size = Vector2(16, 16)
	_punto_aviso.position = Vector2(45, -7)
	_punto_aviso.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_punto_aviso.visible = false
	icono.add_child(_punto_aviso)
	carpeta.add_child(icono)

	var datos := _vbox(10)
	_lbl_caso = _label("CASO " + _caso, mono, 18, TEXTO)
	datos.add_child(_lbl_caso)
	datos.add_child(_rect(TEXTO, Vector2(200, 1.5)))
	_lbl_archivo = _label("Archivo: ", mono, 15, TEXTO)
	datos.add_child(_lbl_archivo)
	datos.add_child(_label("[ TAB ] ABRIR EXPEDIENTE", _fuente("CourierPrime-Regular.ttf", 2), 12, GRIS2))
	carpeta.add_child(datos)

	var obj := _vbox(12)
	obj.anchor_left = 1.0
	obj.anchor_right = 1.0
	obj.offset_left = -390
	obj.offset_right = -48
	obj.offset_top = 36
	add_child(obj)
	obj.add_child(_label("OBJETIVO ACTUAL", _fuente("CourierPrime-Bold.ttf", 1), 17, AMBAR))
	obj.add_child(_rect(TEXTO, Vector2(0, 1.5)))
	_lbl_objetivo = _label("", mono, 17, TEXTO, true)
	obj.add_child(_lbl_objetivo)

	var teclas := _vbox(14)
	teclas.anchor_top = 1.0
	teclas.anchor_bottom = 1.0
	teclas.offset_left = 48
	teclas.offset_top = -44 - TECLAS.size() * 46
	teclas.offset_bottom = -44
	teclas.alignment = BoxContainer.ALIGNMENT_END
	add_child(teclas)
	for par in TECLAS:
		var fila := _hbox(22)
		var t := _tecla(par[0], TEXTO, 14)
		t.custom_minimum_size = Vector2(36, 32)
		fila.add_child(t)
		fila.add_child(_label(par[1], mono, 16, TEXTO))
		teclas.add_child(fila)


# ---------- Expediente ----------

func _construir_expediente() -> void:
	overlay = Control.new()
	overlay.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(overlay)
	var fondo := ColorRect.new()
	fondo.color = Color(0.012, 0.016, 0.02, 0.84)
	fondo.set_anchors_preset(Control.PRESET_FULL_RECT)
	fondo.mouse_filter = Control.MOUSE_FILTER_IGNORE
	overlay.add_child(fondo)

	var sm := _caja(Color("#0E1014"), Vector4.ZERO, 1, Color(1, 1, 1, 0.08))
	sm.shadow_color = Color(0, 0, 0, 0.7)
	sm.shadow_size = 60
	sm.shadow_offset = Vector2(0, 30)
	marco = PanelContainer.new()
	marco.add_theme_stylebox_override("panel", sm)
	overlay.add_child(marco)
	overlay.resized.connect(_layout)

	var cols := _hbox(0)
	marco.add_child(cols)
	cols.add_child(_barra_lateral())
	cols.add_child(_rect(Color(1, 1, 1, 0.06), Vector2(1, 0)))

	var area := MarginContainer.new()
	area.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	for lado in ["left", "top", "right", "bottom"]:
		area.add_theme_constant_override("margin_" + lado, 36)
	cols.add_child(area)
	area.add_child(_hoja())
	_layout()


func _set_desliz(v: float) -> void:
	desliz = v
	_layout()


func _layout() -> void:
	if not marco or not is_instance_valid(marco): return
	var s := overlay.size
	var t := Vector2(min(1320.0, s.x * 0.94), min(820.0, s.y * 0.92))
	marco.size = t
	marco.position = (s - t) / 2.0 + Vector2(0, desliz)


func _boton_atajo(tecla: String, texto: String, accion: Callable) -> Array:
	var fila := _hbox(12)
	fila.mouse_filter = Control.MOUSE_FILTER_STOP
	fila.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND
	var t := _tecla(tecla, GRIS)
	var l := _label(texto, _fuente("CourierPrime-Regular.ttf", 2), 12, GRIS)
	l.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	fila.add_child(t)
	fila.add_child(l)
	fila.gui_input.connect(func(e):
		if e is InputEventMouseButton and e.pressed and e.button_index == MOUSE_BUTTON_LEFT:
			accion.call())
	fila.mouse_entered.connect(func(): l.add_theme_color_override("font_color", AMBAR))
	fila.mouse_exited.connect(func(): l.add_theme_color_override("font_color", GRIS))
	return [fila, l]


func _barra_lateral() -> Control:
	var mono := _fuente("CourierPrime-Regular.ttf")
	var m := MarginContainer.new()
	m.custom_minimum_size.x = 280
	m.add_theme_constant_override("margin_left", 32)
	m.add_theme_constant_override("margin_right", 32)
	m.add_theme_constant_override("margin_top", 44)
	m.add_theme_constant_override("margin_bottom", 32)
	var v := _vbox(40)
	m.add_child(v)

	var cab := _vbox(12)
	var et := _hbox(10)
	var dot := _rect(ROJO, Vector2(6, 6))
	dot.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	et.add_child(dot)
	et.add_child(_label("EXPEDIENTE", _fuente("CourierPrime-Regular.ttf", 3), 11, GRIS2))
	cab.add_child(et)
	cab.add_child(_label("CASO " + _caso, _fuente("BigShouldersDisplay-Black.ttf"), 72, AMBAR))
	cab.add_child(_label("Archivo " + _caso, mono, 14, GRIS))
	var rl := _rect(ROJO, Vector2(40, 2))
	rl.size_flags_horizontal = Control.SIZE_SHRINK_BEGIN
	cab.add_child(rl)
	v.add_child(cab)

	var tab_m := MarginContainer.new()
	tab_m.add_theme_constant_override("margin_left", 10)
	var tab := _hbox(18)
	var n := _label("01", mono, 11, NUM)
	n.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	tab.add_child(n)
	tab.add_child(_label("CONTEXTO", _fuente("BigShouldersDisplay-ExtraBold.ttf", 3), 30, ROJO))
	tab_m.add_child(tab)
	v.add_child(tab_m)

	var esp := Control.new()
	esp.size_flags_vertical = Control.SIZE_EXPAND_FILL
	v.add_child(esp)

	var pie := _vbox(22)
	pie.add_child(_label("La verdad\nsiempre deja rastro.", mono, 13, GRIS3))
	var b_esp := _boton_atajo("ESPACIO", "BAJAR", func(): bajar())
	lbl_espacio = b_esp[1]
	pie.add_child(b_esp[0])
	pie.add_child(_boton_atajo("TAB", "CERRAR", cerrar)[0])
	v.add_child(pie)
	return m


func _seccion(num: int, titulo_sec: String) -> Control:
	var v := _vbox(8)
	var h := _hbox(12)
	h.add_child(_label("%02d" % num, _fuente("SpecialElite-Regular.ttf", 1), 11, ROJO_T))
	h.add_child(_label(titulo_sec, _fuente("SpecialElite-Regular.ttf", 3), 13, TINTA))
	v.add_child(h)
	v.add_child(_rect(TINTA, Vector2(0, 1.5)))
	return v


func _etiqueta(t: String) -> Label:
	return _label(t, _fuente("SpecialElite-Regular.ttf", 3), 11, TINTA2)


func _vineta(t: String, tam := 15) -> Control:
	var h := _hbox(12)
	var g := _label("—", _fuente("SpecialElite-Regular.ttf"), tam, TINTA)
	g.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	h.add_child(g)
	h.add_child(_label(t, _fuente("SpecialElite-Regular.ttf"), tam, TINTA, true))
	return h


func _hoja() -> Control:
	var maq := _fuente("SpecialElite-Regular.ttf")

	var sh := _caja(PAPEL)
	sh.shadow_color = Color(0, 0, 0, 0.55)
	sh.shadow_size = 24
	sh.shadow_offset = Vector2(0, 18)
	var hoja := _panel(sh)
	_rotar(hoja, -0.35)

	scroll = ScrollContainer.new()
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	scroll.get_v_scroll_bar().value_changed.connect(_actualizar_espacio)
	scroll.get_v_scroll_bar().changed.connect(_actualizar_espacio)
	hoja.add_child(scroll)

	var pad := MarginContainer.new()
	pad.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	pad.add_theme_constant_override("margin_left", 48)
	pad.add_theme_constant_override("margin_right", 48)
	pad.add_theme_constant_override("margin_top", 44)
	pad.add_theme_constant_override("margin_bottom", 44)
	scroll.add_child(pad)

	var doc := _vbox(40)
	pad.add_child(doc)

	# Encabezado
	var enc := _vbox(6)
	enc.add_child(_label("EXPEDIENTE N.º " + _caso, _fuente("SpecialElite-Regular.ttf", 3), 13, TINTA2))
	doc.add_child(enc)

	if informe == null:
		enc.add_child(_label("SIN EXPEDIENTE", _fuente("SpecialElite-Regular.ttf", 1), 36, TINTA))
		doc.add_child(_label("Todavía no hay un informe para este caso.", maq, 16, TINTA, true))
		return hoja

	var tit := MarginContainer.new()
	tit.size_flags_horizontal = Control.SIZE_SHRINK_BEGIN
	var marca := _rect(Color(AMBAR, 0.75), Vector2(0, 10))
	marca.size_flags_vertical = Control.SIZE_SHRINK_END
	tit.add_child(marca)
	tit.add_child(_label(informe.titulo, _fuente("SpecialElite-Regular.ttf", 1), 36, TINTA))
	enc.add_child(tit)

	var franja := _vbox(18)
	franja.add_child(_rect(LINEA, Vector2(0, 1.5)))
	var datos := _hbox(32)
	for par in [["LUGAR", informe.lugar], ["FECHA DEL HECHO", informe.fecha], [informe.etiqueta_victima, informe.victima]]:
		if par[1] == "":
			continue
		var c := _vbox(6)
		c.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		c.add_child(_etiqueta(par[0]))
		c.add_child(_label(par[1], maq, 16, TINTA, true))
		datos.add_child(c)
	franja.add_child(datos)
	franja.add_child(_rect(LINEA, Vector2(0, 1.5)))
	doc.add_child(franja)

	# Dos columnas
	var cols := _hbox(52)
	doc.add_child(cols)

	var izq := _vbox(30)
	izq.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	izq.size_flags_stretch_ratio = 0.56
	izq.custom_minimum_size.x = 260
	cols.add_child(izq)

	var foto_wrap := Control.new()
	foto_wrap.mouse_filter = Control.MOUSE_FILTER_IGNORE
	foto_wrap.custom_minimum_size.y = 14
	izq.add_child(foto_wrap)

	var sp := _caja(Color("#F4F1EA"), Vector4(14, 14, 14, 16))
	sp.shadow_color = Color(0, 0, 0, 0.35)
	sp.shadow_size = 16
	sp.shadow_offset = Vector2(0, 14)
	var polaroid := _panel(sp)
	_rotar(polaroid, -2.5)
	izq.add_child(polaroid)
	var pv := _vbox(14)
	polaroid.add_child(pv)
	var ar := AspectRatioContainer.new()
	ar.ratio = 1.0
	ar.mouse_filter = Control.MOUSE_FILTER_IGNORE
	ar.custom_minimum_size.y = 260
	pv.add_child(ar)
	if informe.foto:
		var tr := TextureRect.new()
		tr.texture = informe.foto
		tr.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
		tr.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_COVERED
		ar.add_child(tr)
	else:
		var ph := _panel(_caja(Color("#17191E")))
		var fl := _label("FOTO", _fuente("CourierPrime-Regular.ttf", 3), 11, GRIS3)
		fl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		fl.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
		ph.add_child(fl)
		ar.add_child(ph)
	var pie := _label(informe.pie_foto, maq, 13, TINTA)
	pie.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	pv.add_child(pie)

	if informe.sello != "":
		var sello_p := _panel(_caja(Color.TRANSPARENT, Vector4(14, 4, 14, 4), 3, ROJO_T))
		sello_p.add_child(_label(informe.sello, _fuente("BigShouldersDisplay-Black.ttf", 3), 24, ROJO_T))
		sello_p.modulate.a = 0.82
		sello_p.z_index = 2
		_rotar(sello_p, -9)
		foto_wrap.add_child(sello_p)
		foto_wrap.resized.connect(func(): sello_p.position = Vector2(foto_wrap.size.x - sello_p.size.x + 6, -10))

	if not informe.examen.is_empty():
		var ficha := _panel(_caja(PAPEL2, Vector4(18, 18, 18, 20)))
		_rotar(ficha, 0.8)
		var fv := _vbox(14)
		fv.add_child(_etiqueta("EXAMEN PRELIMINAR"))
		for t in informe.examen:
			fv.add_child(_vineta(t, 14))
		ficha.add_child(fv)
		izq.add_child(ficha)

	var meta := _hbox(20)
	for par in [["A CARGO", informe.a_cargo, TINTA], ["ESTADO", informe.estado, ROJO_T]]:
		var c := _vbox(6)
		c.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		c.add_child(_etiqueta(par[0]))
		c.add_child(_label(par[1], maq, 13, par[2]))
		meta.add_child(c)
	izq.add_child(meta)

	var der := _vbox(34)
	der.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	cols.add_child(der)

	var num := 0
	if informe.hallazgo != "":
		num += 1
		var s1 := _vbox(14)
		s1.add_child(_seccion(num, "HALLAZGO"))
		var hl := _label(informe.hallazgo, maq, 16, TINTA, true)
		hl.add_theme_constant_override("line_spacing", 8)
		s1.add_child(hl)
		der.add_child(s1)

	if informe.circunstancias != "" or not informe.linea_tiempo.is_empty() or not informe.notas.is_empty():
		num += 1
		var s2 := _vbox(18)
		s2.add_child(_seccion(num, "CIRCUNSTANCIAS"))
		if informe.circunstancias != "":
			var ci := _label(informe.circunstancias, maq, 16, TINTA, true)
			ci.add_theme_constant_override("line_spacing", 8)
			s2.add_child(ci)
		var tl := _vbox(18)
		for entrada in informe.linea_tiempo:
			var p := entrada.split("|")
			var resaltar := p.size() > 2 and p[2] == "!"
			var fila := _hbox(14)
			var hora := _label(p[0], maq, 15, ROJO_T if resaltar else TINTA)
			hora.custom_minimum_size.x = 104
			hora.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
			hora.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
			fila.add_child(hora)
			var pd := _caja(ROJO_T if resaltar else PAPEL, Vector4.ZERO, 2, ROJO_T if resaltar else TINTA)
			pd.set_corner_radius_all(6)
			var punto := _panel(pd)
			punto.custom_minimum_size = Vector2(10, 10)
			punto.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
			var pm := MarginContainer.new()
			pm.add_theme_constant_override("margin_top", 6)
			pm.add_child(punto)
			fila.add_child(pm)
			fila.add_child(_label(p[1] if p.size() > 1 else "", maq, 15, TINTA, true))
			tl.add_child(fila)
		s2.add_child(tl)
		for t in informe.notas:
			s2.add_child(_vineta(t))
		der.add_child(s2)

	if informe.antecedente != "":
		var ant := _panel(_caja(Color(AMBAR, 0.22), Vector4(18, 14, 18, 14), 1, LINEA))
		var av := _vbox(8)
		av.add_child(_etiqueta("ANTECEDENTE"))
		av.add_child(_label(informe.antecedente, maq, 15, TINTA, true))
		ant.add_child(av)
		der.add_child(ant)

	# Personas
	if not informe.personas.is_empty():
		num += 1
		var s3 := _vbox(16)
		s3.add_child(_seccion(num, informe.titulo_personas))
		var grid := GridContainer.new()
		grid.columns = 3
		grid.add_theme_constant_override("h_separation", 12)
		grid.add_theme_constant_override("v_separation", 12)
		for entrada in informe.personas:
			grid.add_child(_ficha_persona(entrada.split("|"), maq))
		s3.add_child(grid)
		doc.add_child(s3)

	# Objetivo
	if informe.objetivo != "":
		var ob := _panel(_caja(Color.TRANSPARENT, Vector4(22, 18, 22, 18), 2, TINTA))
		var obv := _vbox(10)
		obv.add_child(_label("OBJETIVO", _fuente("SpecialElite-Regular.ttf", 3), 11, ROJO_T))
		obv.add_child(_label(informe.objetivo, maq, 17, TINTA, true))
		ob.add_child(obv)
		doc.add_child(ob)

	return hoja


func _ficha_persona(p: PackedStringArray, maq: Font) -> Control:
	var card := _panel(_caja(PAPEL2, Vector4(16, 14, 16, 14), 1, Color("#CFC6B2")))
	card.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	var cv := _vbox(10)
	var top := _hbox(12)
	var nom := _label(p[0], maq, 18, TINTA)
	nom.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	top.add_child(nom)
	if p.size() > 1 and p[1] != "":
		top.add_child(_label(p[1] + " años", maq, 13, TINTA3))
	cv.add_child(top)
	var hoy := _vbox(4)
	hoy.add_child(_label("HOY", _fuente("SpecialElite-Regular.ttf", 3), 10, TINTA2))
	hoy.add_child(_label(p[2] if p.size() > 2 else "", maq, 13, TINTA, true))
	cv.add_child(hoy)
	if informe.etiqueta_pasado != "":
		cv.add_child(_rect(Color("#B3A995"), Vector2(0, 1)))
		var antes := _vbox(4)
		antes.add_child(_label(informe.etiqueta_pasado, _fuente("SpecialElite-Regular.ttf", 3), 10, TINTA2))
		antes.add_child(_label(p[3] if p.size() > 3 else "—", maq, 13, TINTA3, true))
		cv.add_child(antes)
	card.add_child(cv)
	return card
