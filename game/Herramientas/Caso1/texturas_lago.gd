extends SceneTree
## HU-20.6: texturas hechas a mano para el Escenario 2 (Lago y Bosque): huella de pezuña, mancha de sangre,
## red de pesca y tierra del sendero. Headless. Uso: --headless --path game --script res://Herramientas/Caso1/texturas_lago.gd

const CARPETA := "res://Assets/Caso1/Lago/Texturas/"

func _initialize() -> void:
	DirAccess.make_dir_recursive_absolute(CARPETA)
	_pezuna().save_png(CARPETA + "huella_pezuna.png")
	_sangre().save_png(CARPETA + "sangre.png")
	_red().save_png(CARPETA + "red_pesca.png")
	_patrulla().save_png(CARPETA + "colormap_patrulla.png")
	print("texturas listas en ", CARPETA)
	quit()

## Paleta del coche de Kenney con el rojo de la carrocería cambiado por azul marino: la patrulla de Hecdog
## (el coche rojo es el de Juan Carlos)
func _patrulla() -> Image:
	var img := Image.load_from_file("res://Assets/Caso1/Modelos/coches/Textures/colormap.png")
	img.convert(Image.FORMAT_RGBA8)
	for celda in [Rect2i(128, 384, 64, 128), Rect2i(384, 128, 64, 128)]:
		for y in range(celda.position.y, celda.end.y):
			for x in range(celda.position.x, celda.end.x):
				var c := img.get_pixel(x, y)
				var l := c.get_luminance()
				img.set_pixel(x, y, Color(0.05 + l * 0.15, 0.1 + l * 0.2, 0.28 + l * 0.45))
	return img

## Huella de pezuña partida, honda (oscura al centro, borde levantado más claro), sobre transparente
func _pezuna() -> Image:
	var tam := 256
	var img := Image.create_empty(tam, tam, false, Image.FORMAT_RGBA8)
	img.fill(Color(0, 0, 0, 0))
	var azar := RandomNumberGenerator.new()
	azar.seed = 7
	for y in tam:
		for x in tam:
			var p := Vector2(x, y) / tam - Vector2(0.5, 0.5)
			var a := 0.0
			# Dos pezuñas en forma de gota, separadas por una ranura
			for lado in [-1.0, 1.0]:
				var q := p - Vector2(lado * 0.11, 0.02)
				q.x *= 1.0 + 0.9 * clampf(-q.y * 2.5, 0.0, 1.0)  # más angosta arriba
				var d := Vector2(q.x / 0.1, q.y / 0.3).length()
				a = maxf(a, 1.0 - smoothstep(0.75, 1.0, d))
			var borde := 0.0
			for lado2 in [-1.0, 1.0]:
				var r := p - Vector2(lado2 * 0.11, 0.02)
				var d2 := Vector2(r.x / 0.13, r.y / 0.34).length()
				borde = maxf(borde, smoothstep(0.85, 1.0, d2) * (1.0 - smoothstep(1.0, 1.2, d2)))
			var ruido := azar.randf_range(-0.08, 0.08)
			if a > 0.01:
				img.set_pixel(x, y, Color(0.07 + ruido * 0.3, 0.05, 0.035, a * 0.92))
			elif borde > 0.01:
				img.set_pixel(x, y, Color(0.36 + ruido, 0.29 + ruido, 0.2, borde * 0.55))
	return img

## Mancha de sangre seca: centro denso y gotas alrededor
func _sangre() -> Image:
	var tam := 256
	var img := Image.create_empty(tam, tam, false, Image.FORMAT_RGBA8)
	img.fill(Color(0, 0, 0, 0))
	var azar := RandomNumberGenerator.new()
	azar.seed = 23
	var gotas := [[Vector2(0.5, 0.5), 0.26]]
	for i in 22:
		var ang := azar.randf() * TAU
		var dist := azar.randf_range(0.18, 0.42)
		gotas.append([Vector2(0.5, 0.5) + Vector2(cos(ang), sin(ang)) * dist, azar.randf_range(0.015, 0.05)])
	for y in tam:
		for x in tam:
			var p := Vector2(x, y) / tam
			var a := 0.0
			for g in gotas:
				var deformada: float = (p - g[0]).length() + sin(p.x * 40.0 + p.y * 33.0) * 0.012
				a = maxf(a, 1.0 - smoothstep(g[1] * 0.85, g[1], deformada))
			if a > 0.01:
				var oscuro := azar.randf_range(0.0, 0.06)
				img.set_pixel(x, y, Color(0.26 - oscuro, 0.02, 0.02, a * 0.95))
	return img

## Red de pesca vieja: rombos de cuerda con nudos y algunos hilos rotos
func _red() -> Image:
	var tam := 256
	var img := Image.create_empty(tam, tam, false, Image.FORMAT_RGBA8)
	img.fill(Color(0, 0, 0, 0))
	var azar := RandomNumberGenerator.new()
	azar.seed = 5
	var paso := 32.0
	for y in tam:
		for x in tam:
			var u := fmod(x + y, paso) / paso
			var v := fmod(x - y + tam, paso) / paso
			var hilo := minf(minf(u, 1.0 - u), minf(v, 1.0 - v))
			if hilo < 0.06:
				var celda := Vector2i(floori((x + y) / paso), floori((x - y + tam) / paso))
				# Algunos hilos rotos
				if (celda.x * 7 + celda.y * 13) % 11 == 0 and u < 0.06:
					continue
				var tono := 0.35 + azar.randf_range(-0.05, 0.05)
				img.set_pixel(x, y, Color(tono, tono * 0.88, tono * 0.7, 1.0))
	return img
