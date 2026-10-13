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

func probar() -> void:
	var caso := Node.new()
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
	assert(tablero.Abierto and hud.diario_abierto)
	assert(hud._pistas_sugeridas == ids)
	var origenes: Array[Vector2] = []
	for zona in hud._zonas_libro:
		if zona.has("pista"):
			origenes.append(hud.ORIGEN_DIARIO_TABLERO + zona.rect.get_center() * hud.ESCALA_DIARIO_TABLERO)
	assert(origenes.size() == 2, "Deben existir dos fotografías arrastrables")
	# Un clic inspecciona sin añadir tarjetas ni generar deducciones.
	boton(origenes[0], true)
	boton(origenes[0], false)
	assert(hud._tarjetas_tablero.is_empty())
	# Dos pistas se pueden soltar y mover sin duplicarlas.
	for i in range(2):
		boton(origenes[i], true)
		mover(Vector2(900 + i * 220, 350))
		boton(Vector2(900 + i * 220, 350), false)
	assert(hud._tarjetas_tablero.size() == 2)
	assert(hud.notas.is_empty(), "El pizarrón no debe deducir automáticamente")
	var id = hud.pistas[0].id
	var anterior: Vector2 = hud._tarjetas_tablero[id]
	boton(anterior + Vector2(20, 20), true)
	mover(Vector2(1050, 550))
	boton(Vector2(1050, 550), false)
	assert(hud._tarjetas_tablero[id] != anterior)
	assert(hud._tarjetas_tablero.size() == 2)
	# Un destino inválido conserva la ubicación anterior.
	anterior = hud._tarjetas_tablero[id]
	boton(anterior + Vector2(20, 20), true)
	mover(Vector2(20, 20))
	boton(Vector2(20, 20), false)
	assert(hud._tarjetas_tablero[id] == anterior)
	# Volver a arrastrar la misma foto reposiciona una sola tarjeta.
	boton(origenes[0], true)
	mover(Vector2(1500, 750))
	boton(Vector2(1500, 750), false)
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
	boton(hud._tarjetas_tablero[id] + Vector2(20, 20), true, MOUSE_BUTTON_RIGHT)
	assert(hud._tarjetas_tablero.size() == 1 and hud.pistas.size() == 2)
	tecla(KEY_ESCAPE)
	assert(not tablero.Abierto and not hud.diario_abierto)
	print("PIZARRON_OK: puente C#, guía, arrastre, límites, duplicados, retiro, Q/R/Esc y persistencia")
	quit()
