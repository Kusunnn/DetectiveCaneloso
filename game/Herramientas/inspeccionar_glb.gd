extends SceneTree
## Imprime el árbol de nodos y las animaciones de una escena importada (.glb/.tscn).
## Uso: --headless --path game --script res://Herramientas/inspeccionar_glb.gd -- res://ruta.glb

func _initialize() -> void:
	var escena: Node = load(OS.get_cmdline_user_args()[0]).instantiate()
	_imprimir(escena, 0)
	for ap in escena.find_children("*", "AnimationPlayer", true, false):
		for nombre in ap.get_animation_list():
			var a: Animation = ap.get_animation(nombre)
			print("animación ", nombre, " largo=", a.length, " pistas=", a.get_track_count())
			for i in min(6, a.get_track_count()): print("   ", a.track_get_path(i))
	quit()

func _imprimir(n: Node, nivel: int) -> void:
	if nivel > 3: return
	var extra := ""
	if n is Node3D: extra = " pos=" + str((n as Node3D).position.snapped(Vector3.ONE * 0.01))
	print("  ".repeat(nivel), n.name, " (", n.get_class(), ")", extra)
	for h in n.get_children(): _imprimir(h, nivel + 1)
