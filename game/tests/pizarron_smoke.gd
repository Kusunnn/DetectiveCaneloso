extends SceneTree
# Ejecutar con Godot .NET --headless --path game --script res://tests/pizarron_smoke.gd.
var hud: Node
var tablero: Node
var escala: float

func _initialize() -> void:
	call_deferred("probar")

func tecla(codigo: Key) -> void:
	var evento := InputEventKey.new()
	evento.keycode = codigo
	evento.pressed = true
	hud._input(evento)

func boton(posicion: Vector2, pulsado: bool, codigo: MouseButton = MOUSE_BUTTON_LEFT) -> void:
	var evento := InputEventMouseButton.new()
	evento.position = posicion * escala
	evento.button_index = codigo
	evento.pressed = pulsado
	hud._input(evento)

func mover(posicion: Vector2) -> void:
	var evento := InputEventMouseMotion.new()
	evento.position = posicion * escala
	hud._input(evento)

func en_pantalla(punto: Vector2) -> Vector2:
	return hud.pizarron_espacial.pizarron_a_pantalla(punto) / escala

func probar() -> void:
	var caso := Node3D.new()
	var vista_anterior := Camera3D.new()
	caso.add_child(vista_anterior)
	root.add_child(caso)
	hud = load("res://hud/hud.tscn").instantiate()
	caso.add_child(hud)
	tablero = load("res://Escenas/Investigacion/TableroDeduccion.cs").new()
	caso.add_child(tablero)
	await process_frame
	escala = minf(hud._lienzo.size.x / 1600.0, hud._lienzo.size.y / 900.0)
	hud.registrar_pista("prueba_a", "Grabadora", "Primera pista")
	hud.registrar_pista("prueba_b", "Reloj", "Segunda pista")
	var ids: Array[String] = ["prueba_a", "prueba_b"]
	tablero.Sugerir("Arrastra las pistas al pizarrón.", ids)
	tablero.Abrir()
	await process_frame
	await process_frame
	assert(tablero.Abierto and not hud.diario_abierto)
	assert(hud._pistas_sugeridas == ids)
	assert(root.get_camera_3d() == hud.pizarron_espacial.camara)
	tecla(KEY_Q)
	assert(hud.diario_abierto)
	hud.pizarron_espacial.desplazar(Vector2(0, -1.05))
	await process_frame
	await process_frame
	var origenes: Array[Vector2] = []
	for zona in hud._zonas_libro:
		if zona.has("pista"):
			origenes.append(hud._origen_diario_tablero() + zona.rect.get_center() * hud.ESCALA_DIARIO_TABLERO)
	assert(origenes.size() == 2, "Deben existir dos fotografías arrastrables")
	# Un clic inspecciona sin añadir tarjetas ni generar deducciones.
	boton(origenes[0], true)
	boton(origenes[0], false)
	assert(hud._tarjetas_tablero.is_empty())
	# Dos pistas se pueden soltar y mover sin duplicarlas.
	for i in range(2):
		boton(origenes[i], true)
		mover(en_pantalla(Vector2(300 + i * 300, 650)))
		boton(en_pantalla(Vector2(300 + i * 300, 650)), false)
	assert(hud._tarjetas_tablero.size() == 2)
	assert(hud.notas.is_empty(), "El pizarrón no debe deducir automáticamente")
	var id = hud.pistas[0].id
	var anterior: Vector2 = hud._tarjetas_tablero[id]
	boton(en_pantalla(anterior + Vector2(20, 20)), true)
	mover(en_pantalla(Vector2(1100, 600)))
	boton(en_pantalla(Vector2(1100, 600)), false)
	assert(hud._tarjetas_tablero[id] != anterior)
	assert(hud._tarjetas_tablero.size() == 2)
	# Un destino inválido conserva la ubicación anterior.
	anterior = hud._tarjetas_tablero[id]
	boton(en_pantalla(anterior + Vector2(20, 20)), true)
	mover(Vector2(-1000, -1000))
	boton(Vector2(-1000, -1000), false)
	assert(hud._tarjetas_tablero[id] == anterior)
	# Volver a arrastrar la misma foto reposiciona una sola tarjeta.
	boton(origenes[0], true)
	mover(en_pantalla(Vector2(1580, 660)))
	boton(en_pantalla(Vector2(1580, 660)), false)
	assert(hud._tarjetas_tablero.size() == 2)
	var rect := Rect2(hud._tarjetas_tablero[id], hud.TAM_TARJETA)
	assert(hud._zona_pizarron().encloses(rect))
	tecla(KEY_Q)
	assert(not hud.diario_abierto and tablero.Abierto)
	tecla(KEY_Q)
	assert(hud.diario_abierto and tablero.Abierto)
	tecla(KEY_R)
	assert(not tablero.Abierto)
	tablero.Abrir()
	assert(hud._tarjetas_tablero.size() == 2)
	hud.pizarron_espacial.desplazar(Vector2(0, -1.05))
	boton(en_pantalla(hud._tarjetas_tablero[id] + Vector2(20, 20)), true, MOUSE_BUTTON_RIGHT)
	assert(hud._tarjetas_tablero.size() == 1 and hud.pistas.size() == 2)
	tecla(KEY_ESCAPE)
	assert(not tablero.Abierto and not hud.diario_abierto)
	assert(root.get_camera_3d() == vista_anterior)
	assert(hud.pizarron_espacial.is_inside_tree(), "El pizarrón permanece en el escenario")
	# La cámara mantiene su orientación y respeta los límites incluso con movimientos grandes.
	tablero.Abrir()
	var orientacion: Vector3 = hud.pizarron_espacial.camara.rotation
	hud.pizarron_espacial.desplazar(Vector2(100, -100))
	assert(hud.pizarron_espacial.desplazamiento == Vector2(0.55, -1.05))
	assert(hud.pizarron_espacial.camara.rotation == orientacion)
	tablero.Cerrar()
	# Marcadores, tinta local de post-its y deshacer a través de la entrada del ratón.
	tablero.Abrir()
	var utiles = hud.herramientas_pizarron
	boton(utiles._zona_herramienta(1).get_center(), true)
	boton(utiles._zona_herramienta(1).get_center(), false)
	assert(utiles.herramienta == "rojo")
	boton(en_pantalla(Vector2(200, 180)), true)
	mover(en_pantalla(Vector2(260, 205)))
	mover(en_pantalla(Vector2(350, 230)))
	boton(en_pantalla(Vector2(350, 230)), false)
	assert(utiles.trazos.size() == 1 and utiles.trazos[0].puntos.size() >= 3)
	assert(utiles.trazos[0].color == utiles.ROJO and not utiles.dibujando)
	boton(utiles._zona_herramienta(2).get_center(), true)
	boton(utiles._zona_herramienta(2).get_center(), false)
	boton(en_pantalla(Vector2(700, 280)), true)
	boton(en_pantalla(Vector2(700, 280)), false)
	assert(utiles.trazos.size() == 2 and utiles.trazos[1].puntos.size() == 1)
	assert(utiles.trazos[1].color == utiles.NEGRO)
	boton(utiles._zona_herramienta(3).get_center(), true)
	boton(utiles._zona_herramienta(3).get_center(), false)
	boton(en_pantalla(Vector2(900, 250)), true)
	boton(en_pantalla(Vector2(900, 250)), false)
	assert(utiles.postits.size() == 1 and utiles.herramienta == "mano")
	var nota: Dictionary = utiles.postits[0]
	boton(utiles._zona_herramienta(2).get_center(), true)
	boton(utiles._zona_herramienta(2).get_center(), false)
	boton(en_pantalla(nota.posicion + Vector2(20, 40)), true)
	mover(en_pantalla(nota.posicion + Vector2(100, 100)))
	mover(en_pantalla(Vector2(100, 800)))
	boton(en_pantalla(Vector2(100, 800)), false)
	assert(nota.trazos.size() == 1 and utiles.trazos.size() == 2)
	var puntos_originales: PackedVector2Array = nota.trazos[0].puntos.duplicate()
	boton(utiles._zona_herramienta(0).get_center(), true)
	boton(utiles._zona_herramienta(0).get_center(), false)
	boton(en_pantalla(nota.posicion + Vector2(10, 10)), true)
	mover(en_pantalla(Vector2(1200, 350)))
	boton(en_pantalla(Vector2(1200, 350)), false)
	assert(nota.posicion.distance_to(Vector2(1190, 340)) < 1.0)
	assert(nota.trazos[0].puntos == puntos_originales, "La tinta debe seguir pegada a la nota")
	tablero.Cerrar()
	tablero.Abrir()
	assert(utiles.trazos.size() == 2 and utiles.postits.size() == 1)
	boton(utiles._zona_herramienta(4).get_center(), true)
	boton(utiles._zona_herramienta(4).get_center(), false)
	assert(nota.trazos.is_empty() and utiles.trazos.size() == 2)
	var deshacer := InputEventKey.new()
	deshacer.keycode = KEY_Z
	deshacer.ctrl_pressed = true
	deshacer.pressed = true
	hud._input(deshacer)
	assert(utiles.postits.is_empty() and utiles.trazos.size() == 2)
	tablero.Cerrar()
	print("PIZARRON_OK: pizarrón 3D, cámara limitada y restaurada, diario abajo, arrastre, límites, duplicados, retiro, tinta roja/negra, post-its móviles, deshacer y persistencia")
	if "--capturar" in OS.get_cmdline_user_args():
		tablero.Abrir()
		hud._tarjetas_tablero[hud.pistas[0].id] = Vector2(300, 400)
		hud._tarjetas_tablero[hud.pistas[1].id] = Vector2(950, 550)
		utiles.postits.append({"posicion": Vector2(1000, 240), "trazos": [{"puntos": PackedVector2Array([Vector2(30, 60), Vector2(100, 160), Vector2(170, 60)]), "color": utiles.NEGRO}]})
		await process_frame
		await RenderingServer.frame_post_draw
		root.get_texture().get_image().save_png("/tmp/dc_pizarron.png")
		tecla(KEY_Q)
		hud.pizarron_espacial.desplazar(Vector2(0, -0.9))
		await process_frame
		await RenderingServer.frame_post_draw
		root.get_texture().get_image().save_png("/tmp/dc_pizarron_diario.png")
	quit()
