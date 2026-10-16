extends RefCounted
## Tarjetas del diario y conexiones confirmadas por el guion del caso.
const TAM_DEDUCCION := Vector2(280, 180)
const HILO := Color("a92c26")
const COLORES_HILO := [HILO, Color("285e9a"), Color("252321")]
var conexion_paleta: Dictionary = {}
var origen_paleta := Vector2.ZERO
var _consumir_suelta := false
var hud: Node
var tarjetas: Dictionary = {}
var conexiones: Array[Dictionary] = []
var herramienta := "mano"
var arrastre: StringName = &""
var desde_tablero := false
var offset := Vector2.ZERO
var inicio := Vector2.ZERO
var posicion_original := Vector2.ZERO
var cursor := Vector2.ZERO
var moviendo := false
var hilo_desde: StringName = &""
var punto_hilo := Vector2.ZERO

func _init(diario: Node) -> void:
	hud = diario

func sincronizar() -> void:
	for pista in hud.pistas:
		tarjetas[pista.id] = {"tipo": "PISTA", "titulo": pista.titulo, "imagen": pista.imagen}
	for persona in hud.personas:
		tarjetas[StringName("persona:" + persona.nombre)] = {"tipo": "PERSONA", "titulo": persona.nombre, "imagen": persona.imagen}

func cancelar() -> void:
	conexion_paleta = {}
	_consumir_suelta = false
	if arrastre != &"" and desde_tablero and hud._tarjetas_tablero.has(arrastre):
		hud._tarjetas_tablero[arrastre] = posicion_original
		actualizar_centros()
	arrastre = &""
	moviendo = false
	hilo_desde = &""

func medida(id: StringName) -> Vector2:
	return TAM_DEDUCCION if tarjetas.get(id, {}).get("tipo", "") == "DEDUCCION" else hud.TAM_TARJETA

func tachuela(id: StringName) -> Vector2:
	return hud._tarjetas_tablero[id] + Vector2(medida(id).x / 2, 7)

func tarjeta_en(punto: Vector2) -> StringName:
	var ids: Array = hud._tarjetas_tablero.keys()
	ids.reverse()
	for id in ids:
		if Rect2(hud._tarjetas_tablero[id], medida(id)).has_point(punto): return id
	return &""

func _zona_herramienta(indice: int) -> Rect2:
	var escala: float = minf(hud._lienzo.size.x / 1600.0, hud._lienzo.size.y / 900.0)
	var medida_pantalla: Vector2 = hud._lienzo.size / escala
	return Rect2(medida_pantalla.x / 2 - 64 + indice * 64, medida_pantalla.y - 78, 56, 64)

func conectar(a: StringName, b: StringName) -> void:
	if a == b or not hud._tarjetas_tablero.has(a) or not hud._tarjetas_tablero.has(b): return
	var existente: Dictionary = {}
	for conexion in conexiones:
		if (conexion.a == a and conexion.b == b) or (conexion.a == b and conexion.b == a):
			if conexion.resultado != &"": return
			existente = conexion
	var resultado := ""
	if is_instance_valid(hud.gestor_tablero): resultado = hud.gestor_tablero.call("ProbarRelacion", str(a), str(b))
	var id_resultado: StringName = &""
	if not resultado.is_empty():
		id_resultado = StringName("deduccion:" + resultado.sha256_text())
		tarjetas[id_resultado] = {"tipo": "DEDUCCION", "titulo": resultado, "imagen": null}
		# Si las fuentes están juntas, abrir espacio para que la conclusión y sus hilos se vean.
		var centro_a: Vector2 = hud._tarjetas_tablero[a] + medida(a) / 2
		var centro_b: Vector2 = hud._tarjetas_tablero[b] + medida(b) / 2
		if centro_a.distance_to(centro_b) < 500:
			var centro := (centro_a + centro_b) / 2
			var direccion := (centro_b - centro_a).normalized()
			if direccion.is_zero_approx(): direccion = Vector2.RIGHT
			hud._tarjetas_tablero[a] = (centro - direccion * 250 - medida(a) / 2).clamp(Vector2.ZERO, Vector2(1600, 900) - medida(a))
			hud._tarjetas_tablero[b] = (centro + direccion * 250 - medida(b) / 2).clamp(Vector2.ZERO, Vector2(1600, 900) - medida(b))
		var posicion: Vector2 = (hud._tarjetas_tablero[a] + medida(a) / 2 + hud._tarjetas_tablero[b] + medida(b) / 2) / 2 - TAM_DEDUCCION / 2
		hud._tarjetas_tablero[id_resultado] = posicion.clamp(Vector2.ZERO, Vector2(1600, 900) - TAM_DEDUCCION)
	if existente.is_empty(): conexiones.append({"a": a, "b": b, "resultado": id_resultado, "color": HILO})
	else: existente.resultado = id_resultado
	hud.tablero_modificado.emit()

func actualizar_centros() -> void:
	for conexion in conexiones:
		if conexion.resultado == &"": continue
		if not hud._tarjetas_tablero.has(conexion.resultado): continue
		if not hud._tarjetas_tablero.has(conexion.a) or not hud._tarjetas_tablero.has(conexion.b): continue
		var centro: Vector2 = (hud._tarjetas_tablero[conexion.a] + medida(conexion.a) / 2 + hud._tarjetas_tablero[conexion.b] + medida(conexion.b) / 2) / 2
		hud._tarjetas_tablero[conexion.resultado] = (centro - TAM_DEDUCCION / 2).clamp(Vector2.ZERO, Vector2(1600, 900) - TAM_DEDUCCION)

func retirar(id: StringName) -> void:
	var estaba: bool = hud._tarjetas_tablero.has(id)
	hud._tarjetas_tablero.erase(id)
	conexiones = conexiones.filter(func(c: Dictionary) -> bool: return c.a != id and c.b != id and c.resultado != id)
	cancelar()
	if estaba: hud.tablero_modificado.emit()

func input(evento: InputEvent) -> void:
	var escala: float = minf(hud._lienzo.size.x / 1600.0, hud._lienzo.size.y / 900.0)
	if is_instance_valid(hud.pizarron_espacial): hud.pizarron_espacial.recibir_raton(evento)
	if evento is InputEventKey and evento.pressed and not evento.echo:
		match evento.keycode:
			KEY_R, KEY_ESCAPE: hud.cerrar_tablero()
			KEY_Q:
				hud.diario_abierto = not hud.diario_abierto
				cancelar()
			KEY_LEFT, KEY_UP:
				if hud.diario_abierto: hud._pasar_hoja(hud._hoja - 1)
			KEY_RIGHT, KEY_DOWN:
				if hud.diario_abierto: hud._pasar_hoja(hud._hoja + 1)
		return
	if evento is InputEventMouseMotion:
		cursor = evento.position / escala
		punto_hilo = hud._punto_pizarron(evento.position)
		if arrastre != &"" and cursor.distance_to(inicio) > 6: moviendo = true
		if moviendo and desde_tablero:
			hud._tarjetas_tablero[arrastre] = (punto_hilo - offset).clamp(Vector2.ZERO, Vector2(1600, 900) - medida(arrastre))
			actualizar_centros()
		return
	if not evento is InputEventMouseButton: return
	var raton: Vector2 = evento.position / escala
	var punto: Vector2 = hud._punto_pizarron(evento.position)
	var local: Vector2 = (raton - hud._origen_diario_tablero()) / hud.ESCALA_DIARIO_TABLERO
	var sobre_diario: bool = hud.diario_abierto and Rect2(202, 52, 1192, 800).has_point(local)
	var id := tarjeta_en(punto) if not sobre_diario else &""
	if evento.button_index == MOUSE_BUTTON_LEFT and not evento.pressed and _consumir_suelta:
		_consumir_suelta = false
		return
	if not conexion_paleta.is_empty() and evento.pressed:
		if evento.button_index == MOUSE_BUTTON_LEFT:
			for i in range(3):
				if _muestra_color(i).has_point(raton) and conexion_paleta.get("color", HILO) != COLORES_HILO[i]:
					conexion_paleta.color = COLORES_HILO[i]
					hud.tablero_modificado.emit()
			_consumir_suelta = true
		conexion_paleta = {}
		return
	if evento.button_index == MOUSE_BUTTON_LEFT:
		if evento.pressed:
			for i in range(2):
				if _zona_herramienta(i).has_point(raton):
					cancelar()
					herramienta = "mano" if i == 0 else "hilo"
					return
			if hud.diario_abierto:
				for zona in hud._zonas_libro:
					if not zona.rect.has_point(local): continue
					if zona.has("pista"):
						hud._pista_diario = zona.pista
						arrastre = hud.pistas[zona.pista].id
					elif zona.has("persona"): arrastre = StringName("persona:" + zona.persona)
					elif zona.has("seccion"): hud._cambiar_seccion(zona.seccion)
					elif zona.destino == -1: hud.diario_abierto = false
					else: hud._pasar_hoja(zona.destino)
					if arrastre != &"":
						desde_tablero = false
						inicio = raton
						cursor = raton
						offset = medida(arrastre) / 2
					return
			if sobre_diario: return
			if id == &"":
				if hilo_desde == &"":
					var conexion := _conexion_en(evento.position)
					if not conexion.is_empty():
						conexion_paleta = conexion
						var tam_pantalla: Vector2 = hud._lienzo.size / escala
						origen_paleta = (raton + Vector2(10, 10)).clamp(Vector2(8, 8), tam_pantalla - Vector2(152, 60))
						_consumir_suelta = true
				else: hilo_desde = &""
				return
			if herramienta == "hilo" or punto.distance_to(tachuela(id)) < 14 or hilo_desde != &"":
				if hilo_desde != &"" and hilo_desde != id:
					conectar(hilo_desde, id)
					hilo_desde = &""
				else: hilo_desde = id
				punto_hilo = punto
				return
			arrastre = id
			desde_tablero = true
			posicion_original = hud._tarjetas_tablero[id]
			inicio = raton
			cursor = raton
			offset = punto - hud._tarjetas_tablero[id]
		else:
			if hilo_desde != &"":
				if id != &"" and id != hilo_desde:
					conectar(hilo_desde, id)
					hilo_desde = &""
				return
			if arrastre != &"" and moviendo and not sobre_diario and hud._zona_pizarron().has_point(punto):
				var posicion: Vector2 = (punto - offset).clamp(Vector2.ZERO, Vector2(1600, 900) - medida(arrastre))
				var cambio: bool = not hud._tarjetas_tablero.has(arrastre) or (desde_tablero and posicion != posicion_original) or (not desde_tablero and hud._tarjetas_tablero[arrastre] != posicion)
				hud._tarjetas_tablero.erase(arrastre)
				hud._tarjetas_tablero[arrastre] = posicion
				actualizar_centros()
				if cambio: hud.tablero_modificado.emit()
			elif arrastre != &"" and desde_tablero:
				hud._tarjetas_tablero[arrastre] = posicion_original
				actualizar_centros()
			arrastre = &""
			moviendo = false
	elif evento.button_index == MOUSE_BUTTON_RIGHT and evento.pressed and not sobre_diario:
		if hilo_desde != &"": hilo_desde = &""
		elif id != &"": retirar(id)
	elif sobre_diario and evento.pressed and evento.button_index in [MOUSE_BUTTON_WHEEL_UP, MOUSE_BUTTON_WHEEL_DOWN]:
		hud._pasar_hoja(hud._hoja + (-1 if evento.button_index == MOUSE_BUTTON_WHEEL_UP else 1))

func _puntos_hilo(a: Vector2, b: Vector2) -> PackedVector2Array:
	var puntos := PackedVector2Array()
	for i in range(17):
		var t := i / 16.0
		puntos.append(a.lerp(b, t) + Vector2(0, sin(t * PI) * 10))
	return puntos

func _hilo(lienzo: Control, a: Vector2, b: Vector2, color: Color = HILO) -> void:
	var puntos := _puntos_hilo(a, b)
	var sombra := PackedVector2Array()
	for p in puntos: sombra.append(p + Vector2(2, 3))
	lienzo.draw_polyline(sombra, Color(0, 0, 0, 0.24), 4, true)
	lienzo.draw_polyline(puntos, color, 3, true)

func dibujar_hilos(lienzo: Control) -> void:
	actualizar_centros()
	for conexion in conexiones:
		if not hud._tarjetas_tablero.has(conexion.a) or not hud._tarjetas_tablero.has(conexion.b): continue
		if conexion.resultado != &"" and hud._tarjetas_tablero.has(conexion.resultado):
			_hilo(lienzo, tachuela(conexion.a), tachuela(conexion.resultado), conexion.get("color", HILO))
			_hilo(lienzo, tachuela(conexion.resultado), tachuela(conexion.b), conexion.get("color", HILO))
		else: _hilo(lienzo, tachuela(conexion.a), tachuela(conexion.b), conexion.get("color", HILO))
	if hilo_desde != &"" and hud._tarjetas_tablero.has(hilo_desde): _hilo(lienzo, tachuela(hilo_desde), punto_hilo)

func dibujar_tarjeta(lienzo: Control, id: StringName, posicion: Vector2) -> void:
	if not tarjetas.has(id): return
	var tarjeta: Dictionary = tarjetas[id]
	var tam := medida(id)
	lienzo.draw_rect(Rect2(posicion + Vector2(4, 6), tam), Color(0, 0, 0, 0.25))
	lienzo.draw_rect(Rect2(posicion, tam), Color("ecdaa0") if tarjeta.tipo == "DEDUCCION" else Color("f5efdf"))
	var anterior: Control = hud._lienzo
	hud._lienzo = lienzo
	if tarjeta.tipo == "DEDUCCION":
		hud._texto("DEDUCCIÓN", posicion + Vector2(14, 30), hud._bold, 24, hud.TINTA)
		hud._parrafo(tarjeta.titulo, posicion + Vector2(14, 42), tam.x - 28, 14, hud.TINTA, 8)
	elif tarjeta.imagen:
		var textura: Texture2D = tarjeta.imagen
		var medida_imagen := textura.get_size()
		medida_imagen *= minf(170.0 / medida_imagen.x, 96.0 / medida_imagen.y)
		lienzo.draw_texture_rect(textura, Rect2(posicion + Vector2(95, 58) - medida_imagen / 2, medida_imagen), false)
		hud._parrafo(tarjeta.titulo, posicion + Vector2(10, 112), 170, 13, hud.TINTA, 2)
	else: hud._parrafo(tarjeta.titulo, posicion + Vector2(12, 28), tam.x - 24, 17, hud.TINTA, 4)
	hud._tachuela(posicion + Vector2(tam.x / 2, 7))
	hud._lienzo = anterior

func dibujar_overlay(lienzo: Control) -> void:
	if moviendo and not desde_tablero and arrastre != &"": dibujar_tarjeta(lienzo, arrastre, cursor - medida(arrastre) / 2)
	for i in range(2):
		var rect := _zona_herramienta(i)
		var p := rect.position
		if (i == 0 and herramienta == "mano") or (i == 1 and herramienta == "hilo"):
			lienzo.draw_rect(rect, Color(0.1, 0.08, 0.05, 0.6))
		if i == 0:
			lienzo.draw_colored_polygon(PackedVector2Array([p + Vector2(16, 14), p + Vector2(16, 46), p + Vector2(25, 37), p + Vector2(33, 50), p + Vector2(39, 46), p + Vector2(31, 33), p + Vector2(43, 32)]), Color("f5efdf"))
		else:
			lienzo.draw_rect(Rect2(p + Vector2(13, 18), Vector2(30, 32)), HILO)
			lienzo.draw_rect(Rect2(p + Vector2(9, 13), Vector2(38, 6)), Color("e0c595"))
			lienzo.draw_rect(Rect2(p + Vector2(9, 49), Vector2(38, 6)), Color("e0c595"))
			for k in range(5): lienzo.draw_line(p + Vector2(14, 23 + k * 5), p + Vector2(42, 20 + k * 5), Color("da6454"), 1, true)

	dibujar_paleta(lienzo)

func _muestra_color(indice: int) -> Rect2:
	return Rect2(origen_paleta + Vector2(8 + indice * 44, 8), Vector2(36, 36))

func _conexion_en(pantalla: Vector2) -> Dictionary:
	if not is_instance_valid(hud.pizarron_espacial): return {}
	var mas_cercana: Dictionary = {}
	var distancia := 9.0
	for conexion in conexiones:
		if not hud._tarjetas_tablero.has(conexion.a) or not hud._tarjetas_tablero.has(conexion.b): continue
		var destinos: Array[Vector2] = [tachuela(conexion.a)]
		if conexion.resultado != &"" and hud._tarjetas_tablero.has(conexion.resultado): destinos.append(tachuela(conexion.resultado))
		destinos.append(tachuela(conexion.b))
		for j in range(destinos.size() - 1):
			var puntos := _puntos_hilo(destinos[j], destinos[j + 1])
			for k in range(puntos.size() - 1):
				var a: Vector2 = hud.pizarron_espacial.pizarron_a_pantalla(puntos[k])
				var b: Vector2 = hud.pizarron_espacial.pizarron_a_pantalla(puntos[k + 1])
				var cercano := Geometry2D.get_closest_point_to_segment(pantalla, a, b)
				var d := pantalla.distance_to(cercano)
				if d < distancia:
					distancia = d
					mas_cercana = conexion
	return mas_cercana

func dibujar_paleta(lienzo: Control) -> void:
	if conexion_paleta.is_empty(): return
	var estilo := StyleBoxFlat.new()
	estilo.bg_color = Color("ede3ce")
	estilo.set_corner_radius_all(8)
	estilo.shadow_size = 4
	lienzo.draw_style_box(estilo, Rect2(origen_paleta, Vector2(140, 52)))
	for i in range(3):
		var muestra := _muestra_color(i)
		lienzo.draw_circle(muestra.get_center(), 13, COLORES_HILO[i])
		if conexion_paleta.get("color", HILO) == COLORES_HILO[i]:
			lienzo.draw_arc(muestra.get_center(), 16, 0, TAU, 32, Color("8a754f"), 2, true)

func capturar_estado() -> Dictionary:
	var colocadas: Array[Dictionary] = []
	for id in hud._tarjetas_tablero:
		if not tarjetas.has(id): continue
		var posicion: Vector2 = hud._tarjetas_tablero[id]
		var tarjeta: Dictionary = tarjetas[id]
		colocadas.append({"id": str(id), "x": posicion.x, "y": posicion.y, "tipo": tarjeta.tipo, "titulo": tarjeta.titulo})
	var hilos: Array[Dictionary] = []
	for conexion in conexiones:
		hilos.append({"a": str(conexion.a), "b": str(conexion.b), "resultado": str(conexion.resultado), "color": conexion.get("color", HILO).to_html()})
	return {"version": 1, "tarjetas": colocadas, "hilos": hilos}

func restaurar_estado(estado: Dictionary) -> void:
	cancelar()
	hud._tarjetas_tablero.clear()
	conexiones.clear()
	tarjetas.clear()
	sincronizar()
	# Texturas y retratos proceden del diario restaurado; el JSON solo contiene datos.
	for datos in estado.get("tarjetas", []):
		var id := StringName(datos.get("id", ""))
		if id == &"": continue
		if datos.get("tipo", "") == "DEDUCCION":
			tarjetas[id] = {"tipo": "DEDUCCION", "titulo": str(datos.get("titulo", "")), "imagen": null}
		if not tarjetas.has(id): continue
		var posicion := Vector2(float(datos.get("x", 0)), float(datos.get("y", 0)))
		if not posicion.is_finite(): continue
		hud._tarjetas_tablero[id] = posicion.clamp(Vector2.ZERO, Vector2(1600, 900) - medida(id))
	for datos in estado.get("hilos", []):
		var a := StringName(datos.get("a", ""))
		var b := StringName(datos.get("b", ""))
		var resultado := StringName(datos.get("resultado", ""))
		if a == b or not hud._tarjetas_tablero.has(a) or not hud._tarjetas_tablero.has(b): continue
		if resultado != &"" and not hud._tarjetas_tablero.has(resultado): continue
		var color := HILO
		for permitido in COLORES_HILO:
			if datos.get("color", "") == permitido.to_html(): color = permitido
		conexiones.append({"a": a, "b": b, "resultado": resultado, "color": color})
