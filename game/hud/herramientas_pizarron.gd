extends RefCounted
## Tinta y notas en coordenadas del corcho; las notas guardan sus trazos localmente.
const TAM_NOTA := Vector2(220, 200)
const ROJO := Color("a72d27")
const NEGRO := Color("252321")
var hud: Node
var herramienta := "mano"
var trazos: Array[Dictionary] = []
var postits: Array[Dictionary] = []
var historial: Array[Dictionary] = []
var dibujando := false
var _trazo: Dictionary = {}
var _nota: Dictionary = {}
var _accion: Dictionary = {}
var _nota_moviendo: Dictionary = {}
var _offset := Vector2.ZERO
var _posicion_anterior := Vector2.ZERO

func _init(diario: Node) -> void:
	hud = diario

func cancelar() -> void:
	if not _accion.is_empty() and _accion.segmentos.is_empty(): historial.erase(_accion)
	dibujando = false
	_trazo = {}
	_nota = {}
	_accion = {}
	_nota_moviendo = {}

func _zona_herramienta(indice: int) -> Rect2:
	var escala: float = minf(hud._lienzo.size.x / 1600.0, hud._lienzo.size.y / 900.0)
	var medida: Vector2 = hud._lienzo.size / escala
	return Rect2(medida.x / 2 - 160 + indice * 64, medida.y - 78, 56, 64)

func _sobre_diario(pantalla: Vector2) -> bool:
	if not hud.diario_abierto: return false
	var escala: float = minf(hud._lienzo.size.x / 1600.0, hud._lienzo.size.y / 900.0)
	var local: Vector2 = (pantalla / escala - hud._origen_diario_tablero()) / hud.ESCALA_DIARIO_TABLERO
	return Rect2(202, 52, 1192, 800).has_point(local)

func _nota_en(punto: Vector2) -> Dictionary:
	for i in range(postits.size() - 1, -1, -1):
		if Rect2(postits[i].posicion, TAM_NOTA).has_point(punto): return postits[i]
	return {}

func _sobre_pista(punto: Vector2) -> bool:
	for posicion in hud._tarjetas_tablero.values():
		if Rect2(posicion, hud.TAM_TARJETA).has_point(punto): return true
	return false

func _agregar_punto(pantalla: Vector2) -> void:
	var punto: Vector2 = hud._punto_pizarron(pantalla)
	var valido: bool = not _sobre_diario(pantalla) and hud._zona_pizarron().has_point(punto)
	if not _nota.is_empty():
		valido = valido and Rect2(_nota.posicion, TAM_NOTA).has_point(punto)
		punto -= _nota.posicion
	else:
		valido = valido and _nota_en(punto).is_empty() and not _sobre_pista(punto)
	if not valido:
		_trazo = {}
		return
	if _trazo.is_empty():
		_trazo = {"puntos": PackedVector2Array(), "color": ROJO if herramienta == "rojo" else NEGRO}
		var destino: Array = trazos if _nota.is_empty() else _nota.trazos
		destino.append(_trazo)
		_accion.segmentos.append({"destino": destino, "trazo": _trazo})
	var puntos: PackedVector2Array = _trazo.puntos
	if puntos.is_empty() or puntos[-1].distance_to(punto) >= 1.5:
		puntos.append(punto)
		_trazo.puntos = puntos

func deshacer() -> void:
	cancelar()
	if historial.is_empty(): return
	var accion: Dictionary = historial.pop_back()
	if accion.tipo == "tinta":
		for segmento in accion.segmentos: segmento.destino.erase(segmento.trazo)
	elif accion.tipo == "nota": postits.erase(accion.nota)

func input(evento: InputEvent) -> bool:
	if evento is InputEventKey and evento.pressed and not evento.echo:
		if evento.ctrl_pressed and evento.keycode == KEY_Z:
			deshacer()
			return true
		if evento.keycode in [KEY_Q, KEY_R, KEY_ESCAPE]: cancelar()
		return false
	if evento is InputEventMouseMotion:
		if dibujando:
			_agregar_punto(evento.position)
			return true
		if not _nota_moviendo.is_empty():
			var punto: Vector2 = hud._punto_pizarron(evento.position) - _offset
			_nota_moviendo.posicion = punto.clamp(Vector2.ZERO, Vector2(1600, 900) - TAM_NOTA)
			return true
		return false
	if not evento is InputEventMouseButton: return false
	var escala: float = minf(hud._lienzo.size.x / 1600.0, hud._lienzo.size.y / 900.0)
	var raton: Vector2 = evento.position / escala
	var punto: Vector2 = hud._punto_pizarron(evento.position)
	if evento.button_index == MOUSE_BUTTON_LEFT:
		if evento.pressed:
			for i in range(5):
				if not _zona_herramienta(i).has_point(raton): continue
				cancelar()
				if i == 4: deshacer()
				else: herramienta = ["mano", "rojo", "negro", "nota"][i]
				return true
			if _sobre_diario(evento.position): return false
			if not hud._zona_pizarron().has_point(punto): return herramienta != "mano"
			if herramienta == "nota":
				var nota := {"posicion": (punto - TAM_NOTA / 2).clamp(Vector2.ZERO, Vector2(1600, 900) - TAM_NOTA), "trazos": []}
				postits.append(nota)
				historial.append({"tipo": "nota", "nota": nota})
				herramienta = "mano"
				return true
			if herramienta in ["rojo", "negro"]:
				dibujando = true
				_nota = _nota_en(punto)
				_accion = {"tipo": "tinta", "segmentos": []}
				historial.append(_accion)
				_agregar_punto(evento.position)
				return true
			_nota_moviendo = _nota_en(punto)
			if not _nota_moviendo.is_empty():
				_offset = punto - _nota_moviendo.posicion
				_posicion_anterior = _nota_moviendo.posicion
				postits.erase(_nota_moviendo)
				postits.append(_nota_moviendo)
				return true
		elif dibujando:
			_agregar_punto(evento.position)
			cancelar()
			return true
		elif not _nota_moviendo.is_empty():
			if _sobre_diario(evento.position) or not hud._zona_pizarron().has_point(punto):
				_nota_moviendo.posicion = _posicion_anterior
			cancelar()
			return true
	elif evento.button_index == MOUSE_BUTTON_RIGHT and evento.pressed:
		if herramienta != "mano":
			cancelar()
			herramienta = "mano"
			return true
		if not _sobre_diario(evento.position):
			var nota := _nota_en(punto)
			if not nota.is_empty():
				postits.erase(nota)
				return true
	return false

func _dibujar_trazos(lienzo: Control, lista: Array, offset: Vector2 = Vector2.ZERO) -> void:
	for trazo in lista:
		var puntos: PackedVector2Array = trazo.puntos
		if puntos.is_empty(): continue
		if puntos.size() > 1:
			var desplazados := PackedVector2Array()
			for p in puntos: desplazados.append(p + offset)
			lienzo.draw_polyline(desplazados, trazo.color, 3.5, true)
		lienzo.draw_circle(puntos[0] + offset, 1.75, trazo.color)
		lienzo.draw_circle(puntos[-1] + offset, 1.75, trazo.color)

func dibujar_tinta(lienzo: Control) -> void:
	_dibujar_trazos(lienzo, trazos)

func dibujar_notas(lienzo: Control) -> void:
	for nota in postits:
		var p: Vector2 = nota.posicion
		lienzo.draw_rect(Rect2(p + Vector2(4, 5), TAM_NOTA), Color(0, 0, 0, 0.22))
		lienzo.draw_rect(Rect2(p, TAM_NOTA), Color("f0db78"))
		lienzo.draw_rect(Rect2(p, Vector2(TAM_NOTA.x, 24)), Color("dec45c"))
		lienzo.draw_colored_polygon(PackedVector2Array([p + TAM_NOTA, p + TAM_NOTA - Vector2(20, 0), p + TAM_NOTA - Vector2(0, 20)]), Color("f8e99e"))
		_dibujar_trazos(lienzo, nota.trazos, p)

func dibujar_herramientas(lienzo: Control) -> void:
	for i in range(5):
		var rect := _zona_herramienta(i)
		var activa: bool = i < 4 and herramienta == ["mano", "rojo", "negro", "nota"][i]
		if activa:
			lienzo.draw_style_box(_estilo_seleccion(), rect)
		var p := rect.position
		if i in [1, 2]:
			var color := ROJO if i == 1 else NEGRO
			lienzo.draw_rect(Rect2(p + Vector2(20, 15), Vector2(16, 42)), Color("d3cabb"))
			lienzo.draw_rect(Rect2(p + Vector2(20, 12), Vector2(16, 13)), color)
			lienzo.draw_rect(Rect2(p + Vector2(20, 45), Vector2(16, 9)), color)
			lienzo.draw_colored_polygon(PackedVector2Array([p + Vector2(23, 12), p + Vector2(33, 12), p + Vector2(28, 4)]), color)
			lienzo.draw_line(p + Vector2(23, 28), p + Vector2(23, 42), Color.WHITE, 2)
		elif i == 3:
			lienzo.draw_rect(Rect2(p + Vector2(11, 24), Vector2(34, 30)), Color("b49c45"))
			lienzo.draw_rect(Rect2(p + Vector2(8, 19), Vector2(34, 30)), Color("f0db78"))
			lienzo.draw_line(p + Vector2(25, 27), p + Vector2(25, 41), Color("6a5931"), 2)
			lienzo.draw_line(p + Vector2(18, 34), p + Vector2(32, 34), Color("6a5931"), 2)
		elif i == 0:
			lienzo.draw_colored_polygon(PackedVector2Array([p + Vector2(16, 14), p + Vector2(16, 46), p + Vector2(25, 37), p + Vector2(33, 50), p + Vector2(39, 46), p + Vector2(31, 33), p + Vector2(43, 32)]), Color("f5efdf"))
		else:
			lienzo.draw_arc(p + Vector2(29, 34), 14, -PI * 0.8, PI * 0.65, 24, Color("f5efdf"), 3, true)
			lienzo.draw_colored_polygon(PackedVector2Array([p + Vector2(13, 19), p + Vector2(14, 33), p + Vector2(25, 24)]), Color("f5efdf"))

func _estilo_seleccion() -> StyleBoxFlat:
	var estilo := StyleBoxFlat.new()
	estilo.bg_color = Color(0.10, 0.08, 0.05, 0.65)
	estilo.set_corner_radius_all(8)
	return estilo
