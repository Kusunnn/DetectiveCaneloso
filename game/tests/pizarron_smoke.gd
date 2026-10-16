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
	hud.pizarron_espacial._actualizar_vista(1.0)
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
		mover(en_pantalla(Vector2(300 + i * 300, 550)))
		boton(en_pantalla(Vector2(300 + i * 300, 550)), false)
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
	hud.pizarron_espacial._actualizar_vista(1.0)
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
	assert(hud.pizarron_espacial.desplazamiento_objetivo == Vector2(0.55, -1.05))
	assert(hud.pizarron_espacial.camara.rotation == orientacion)
	tablero.Cerrar()
	# La cámara llega gradualmente a su destino sin saltar en el primer cuadro.
	tablero.Abrir()
	var espacial = hud.pizarron_espacial
	espacial.desplazar(Vector2(0.5, 0))
	assert(espacial.desplazamiento.x == 0)
	espacial._actualizar_vista(1.0 / 60)
	assert(espacial.desplazamiento.x > 0 and espacial.desplazamiento.x < 0.5)
	espacial._actualizar_vista(1.0)
	assert(absf(espacial.desplazamiento.x - 0.5) < 0.001)
	tablero.Cerrar()
	tablero.Abrir()
	var relaciones = hud.relaciones_pizarron
	var caso_prueba = load("res://tests/PizarronCasoPrueba.cs").new()
	caso.add_child(caso_prueba)
	caso_prueba.Configurar(tablero, hud)
	hud.registrar_persona("Eriz", "Volvió a las tres.", null)
	relaciones.sincronizar()
	# Arrastrar una persona desde la página del diario, incluso sin retrato.
	tecla(KEY_Q)
	hud._cambiar_seccion("PERSONAS")
	espacial.desplazar(Vector2(0, -1.05))
	espacial._actualizar_vista(1.0)
	await process_frame
	await process_frame
	var origen_persona := Vector2.ZERO
	for zona in hud._zonas_libro:
		if zona.has("persona"):
			origen_persona = hud._origen_diario_tablero() + zona.rect.get_center() * hud.ESCALA_DIARIO_TABLERO
	assert(origen_persona != Vector2.ZERO)
	boton(origen_persona, true)
	mover(en_pantalla(Vector2(1100, 550)))
	boton(en_pantalla(Vector2(1100, 550)), false)
	assert(hud._tarjetas_tablero.has(&"persona:Eriz"))
	tecla(KEY_Q)
	tablero.Cerrar()
	tablero.Abrir()
	hud._tarjetas_tablero[&"prueba_a"] = Vector2(150, 300)
	hud._tarjetas_tablero[&"prueba_b"] = Vector2(1100, 300)
	# Un hilo entre tachuelas crea la deducción central y la nota del caso.
	boton(en_pantalla(relaciones.tachuela(&"prueba_a")), true)
	mover(en_pantalla(relaciones.tachuela(&"prueba_b")))
	boton(en_pantalla(relaciones.tachuela(&"prueba_b")), false)
	assert(relaciones.conexiones.size() == 1)
	var deduccion: StringName = relaciones.conexiones[0].resultado
	assert(deduccion != &"" and hud._tarjetas_tablero.has(deduccion))
	assert(hud.notas.size() == 1)
	var centro: Vector2 = hud._tarjetas_tablero[deduccion] + relaciones.TAM_DEDUCCION / 2
	assert(centro.distance_to(Vector2(720, 377)) < 1)
	relaciones.conectar(&"prueba_b", &"prueba_a")
	assert(relaciones.conexiones.size() == 1 and caso_prueba.Comprobaciones == 1)
	# Mover una fuente conserva la conexión y reposiciona la deducción entre ambas.
	boton(en_pantalla(Vector2(170, 340)), true)
	var pin_original: Vector2 = relaciones.tachuela(&"prueba_a")
	var centro_original: Vector2 = hud._tarjetas_tablero[deduccion]
	mover(en_pantalla(Vector2(170, 540)))
	assert(relaciones.moviendo and relaciones.arrastre == &"prueba_a")
	assert(relaciones.tachuela(&"prueba_a").y > pin_original.y + 150, "El hilo sigue la tarjeta antes de soltar")
	assert(hud._tarjetas_tablero[deduccion].y > centro_original.y + 80, "La deducción central se actualiza durante el arrastre")
	boton(en_pantalla(Vector2(170, 540)), false)
	assert(hud._tarjetas_tablero[deduccion].y > 300)
	# Una persona utiliza únicamente su testimonio obtenido.
	relaciones.conectar(&"persona:Eriz", &"prueba_b")
	assert(relaciones.conexiones.size() == 2 and hud.notas.size() == 2)
	assert(relaciones.conexiones[1].resultado != &"")
	# Las personas también mueven sus hilos mientras sigue pulsado el botón.
	var posicion_persona: Vector2 = hud._tarjetas_tablero[&"persona:Eriz"]
	var pin_persona: Vector2 = relaciones.tachuela(&"persona:Eriz")
	boton(en_pantalla(posicion_persona + Vector2(20, 40)), true)
	mover(en_pantalla(posicion_persona + Vector2(-80, 40)))
	assert(relaciones.tachuela(&"persona:Eriz").x < pin_persona.x - 80)
	tecla(KEY_Q)
	assert(hud._tarjetas_tablero[&"persona:Eriz"] == posicion_persona, "Cancelar restaura la tarjeta y sus conexiones")
	tecla(KEY_Q)
	# Una unión sin regla guarda el hilo sin inventar una conclusión.
	relaciones.conectar(&"persona:Eriz", &"prueba_a")
	assert(relaciones.conexiones.size() == 3 and relaciones.conexiones[2].resultado == &"")
	assert(hud.notas.size() == 2)
	tablero.Cerrar()
	tablero.Abrir()
	assert(relaciones.conexiones.size() == 3 and hud._tarjetas_tablero.has(&"persona:Eriz"))
	# La paleta abre al pulsar el hilo y solo cambia su color visual.
	var conexion: Dictionary = relaciones.conexiones[0]
	var tramo: PackedVector2Array = relaciones._puntos_hilo(relaciones.tachuela(conexion.a), relaciones.tachuela(conexion.resultado))
	var clic_hilo: Vector2 = en_pantalla(tramo[8])
	var comprobaciones: int = caso_prueba.Comprobaciones
	for color in [1, 2, 0]:
		boton(clic_hilo, true)
		boton(clic_hilo, false)
		assert(not relaciones.conexion_paleta.is_empty())
		boton(relaciones._muestra_color(color).get_center(), true)
		boton(relaciones._muestra_color(color).get_center(), false)
		assert(conexion.color == relaciones.COLORES_HILO[color])
		assert(relaciones.conexion_paleta.is_empty())
	assert(caso_prueba.Comprobaciones == comprobaciones and hud.notas.size() == 2)
	boton(clic_hilo, true)
	boton(clic_hilo, false)
	boton(relaciones._muestra_color(1).get_center(), true)
	boton(relaciones._muestra_color(1).get_center(), false)
	tablero.Cerrar()
	tablero.Abrir()
	assert(conexion.color == relaciones.COLORES_HILO[1])
	# Guardado JSON en disco y carga en un HUD nuevo, como al salir y volver a la partida.
	tablero.Cerrar()
	var guardado: Dictionary = hud.capturar_tablero()
	var ruta_guardado := "user://pizarron_prueba_%s.json" % OS.get_process_id()
	var archivo := FileAccess.open(ruta_guardado, FileAccess.WRITE)
	archivo.store_string(JSON.stringify(guardado))
	archivo.close()
	var leido: Dictionary = JSON.parse_string(FileAccess.get_file_as_string(ruta_guardado))
	DirAccess.remove_absolute(ProjectSettings.globalize_path(ruta_guardado))
	var hud_cargado = load("res://hud/hud.tscn").instantiate()
	hud_cargado.name = "HUDCargado"
	caso.add_child(hud_cargado)
	for pista in hud.pistas:
		hud_cargado.registrar_pista(str(pista.id), pista.titulo, pista.descripcion)
	hud_cargado.registrar_persona("Eriz", "Volvió a las tres.", null)
	var cambios := [0]
	hud_cargado.tablero_modificado.connect(func() -> void: cambios[0] += 1)
	hud_cargado.restaurar_tablero(leido)
	assert(hud_cargado._tarjetas_tablero == hud._tarjetas_tablero)
	assert(hud_cargado.relaciones_pizarron.conexiones.size() == 3)
	assert(hud_cargado.relaciones_pizarron.conexiones[0].color == relaciones.COLORES_HILO[1])
	assert(hud_cargado.relaciones_pizarron.tarjetas[deduccion].titulo == relaciones.tarjetas[deduccion].titulo)
	assert(cambios[0] == 0, "Cargar no marca cambios ni vuelve a deducir")
	hud_cargado.relaciones_pizarron.retirar(&"persona:Eriz")
	assert(cambios[0] == 1, "Editar el tablero marca la partida como modificada")
	hud_cargado.restaurar_tablero({})
	assert(hud_cargado._tarjetas_tablero.is_empty() and hud_cargado.relaciones_pizarron.conexiones.is_empty())
	hud_cargado.queue_free()
	tablero.Abrir()
	relaciones.retirar(&"persona:Eriz")
	assert(relaciones.conexiones.size() == 1 and hud.personas.size() == 1)
	assert(hud._tarjetas_tablero.has(deduccion))
	print("PIZARRON_OK: diario, personas arrastrables, hilo por tachuelas, deducción central, reglas C#, duplicados, movimiento, guardado JSON en disco y carga en un HUD nuevo")
	if "--capturar" in OS.get_cmdline_user_args():
		hud._tarjetas_tablero[&"persona:Eriz"] = Vector2(1150, 550)
		relaciones.conectar(&"persona:Eriz", &"prueba_b")
		await process_frame
		await RenderingServer.frame_post_draw
		root.get_texture().get_image().save_png("/tmp/dc_pizarron.png")
	quit()
