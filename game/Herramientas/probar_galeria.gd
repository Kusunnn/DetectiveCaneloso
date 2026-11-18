extends SceneTree
# Prueba corta de la Galería: abre el visor, recorre algunos modelos de cada pestaña y guarda capturas.
# No toca partidas ni configuración (la galería no escribe en user://).
# Uso: godot --path game -s res://Herramientas/probar_galeria.gd -- <carpeta_capturas>

const MUESTRA := {
	"PERSONAJES": [0, 2, 6, 7, 8],
	"PISTAS": [1, 12, 30],
	"ESCENARIOS": [1, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17],
}

func _initialize() -> void:
	_correr.call_deferred()

func _correr() -> void:
	var args := OS.get_cmdline_user_args()
	var carpeta: String = args[0] if args.size() > 0 else OS.get_user_data_dir()
	var galeria: Control = load("res://Escenas/Menus/Galeria.cs").new()
	root.add_child(galeria)
	await process_frame
	galeria.call("Abrir")
	var lista: ItemList = galeria.find_children("*", "ItemList", true, false)[0]
	var botones := galeria.find_children("*", "Button", true, false)
	for pestana in MUESTRA:
		for b in botones:
			if b.text == pestana:
				b.emit_signal("pressed")
		await process_frame
		for i in MUESTRA[pestana]:
			if i >= lista.item_count or not lista.is_item_selectable(i):
				print("SALTO ", pestana, " ", i)
				continue
			lista.select(i)
			lista.emit_signal("item_selected", i)
			for f in (60 if pestana == "ESCENARIOS" else 20):
				await process_frame
			var nombre := lista.get_item_text(i).strip_edges()
			var img := root.get_texture().get_image()
			var ruta := carpeta.path_join("%s_%02d.png" % [pestana.to_lower(), i])
			img.save_png(ruta)
			print("CAPTURA ", nombre, " -> ", ruta, "  memoria: ", Performance.get_monitor(Performance.MEMORY_STATIC) / 1048576.0, " MB")
	# Quitar techo (T) en la recepción
	lista.select(1)
	lista.emit_signal("item_selected", 1)
	await process_frame
	var tecla := InputEventKey.new()
	tecla.keycode = KEY_T
	tecla.pressed = true
	for f in 60:
		await process_frame
	Input.parse_input_event(tecla)
	for f in 20:
		await process_frame
	root.get_texture().get_image().save_png(carpeta.path_join("escenarios_sin_techo.png"))
	print("CAPTURA sin techo")
	# Animaciones: personaje -> etiquetas a probar
	var animaciones: ItemList = galeria.find_children("*", "ItemList", true, false)[1]
	for b in botones:
		if b.text == "PERSONAJES":
			b.emit_signal("pressed")
	await process_frame
	var pruebas := {0: ["Caminar", "Macarena"], 1: ["Comer"], 3: ["Caminar", "Sentado"], 6: ["Alterado", "Caminar"], 5: ["Caminar"]}
	for p in pruebas:
		lista.select(p)
		lista.emit_signal("item_selected", p)
		for f in 10:
			await process_frame
		var nombres := []
		for k in animaciones.item_count:
			nombres.append(animaciones.get_item_text(k).strip_edges())
		print("ANIMACIONES ", lista.get_item_text(p).strip_edges(), ": ", nombres)
		for etiqueta in pruebas[p]:
			var k := nombres.find(etiqueta)
			if k < 0:
				print("FALTA ", etiqueta)
				continue
			animaciones.select(k)
			animaciones.emit_signal("item_selected", k)
			for f in 45:
				await process_frame
			var ruta := carpeta.path_join("anim_%d_%s.png" % [p, etiqueta])
			root.get_texture().get_image().save_png(ruta)
			print("CAPTURA ", lista.get_item_text(p).strip_edges(), " / ", etiqueta)
	galeria.call("Cerrar")
	await process_frame
	print("FIN_PRUEBA")
	quit()
