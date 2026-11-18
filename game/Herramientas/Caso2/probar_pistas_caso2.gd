extends "res://Herramientas/Caso1/probar_pistas.gd"
## Capítulo 2: la misma prueba que probar_pistas.gd (nada visible que no responda) en los escenarios con pistas:
## comisaría, lobby y despacho. Lleva al jugador a pie hasta cada pista, apunta, pulsa [E] y revisa el diario.
## Necesita ventana. Respalda las partidas en disco y las devuelve al final.
## Uso: --path game --script res://Herramientas/Caso2/probar_pistas_caso2.gd -- <reporte.md>

const ESCENAS_CASO2 := {
	"Comisaría": "res://Escenas/Caso2/comisaria_manana.tscn",
	"Lobby": "res://Escenas/Caso2/torre_lobby.tscn",
	"Despacho": "res://Escenas/Caso2/torre_despacho.tscn",
}

## Los cajones se abren sin animación (RestaurarEstado no emite «Abierto»): se hace lo mismo que EscenarioCaso2 al
## abrirlos de verdad, mostrar la pista de adentro y encender su colisión.
func _abrir_todo(escena, jugador_) -> void:
	super._abrir_todo(escena, jugador_)
	for c in escena.find_children("*", "StaticBody3D", true, false):
		var s = c.get_script()
		if s and str(s.resource_path).ends_with("Contenedor.cs"):
			for p in c.find_children("Pista_*", "StaticBody3D", true, false):
				p.visible = true
				p.collision_layer = 8

func _initialize() -> void:
	if DirAccess.dir_exists_absolute(RESPALDO):
		_devolver_respaldo()
	_respaldar()
	await process_frame
	_gestor = root.get_node("GestorPartida")
	var args := OS.get_cmdline_user_args()
	for titulo in ESCENAS_CASO2:
		change_scene_to_file(ESCENAS_CASO2[titulo])
		await _esperar_escena(ESCENAS_CASO2[titulo])
		await _quitar_lo_de_encima()
		await _revisar_escena(titulo)
	var texto := "# Reporte de pistas · Capítulo 2\n\nVisibles que no se pueden tomar: **%d**\n\n" % _visibles_sin_tomar
	texto += "| id | escena | ¿se toma? | si todavía no se puede, ¿lo explica? | causa | corrección |\n|---|---|---|---|---|---|\n"
	texto += "\n".join(_filas) + "\n"
	if args.size() > 0:
		var archivo := FileAccess.open(args[0], FileAccess.WRITE)
		if archivo: archivo.store_string(texto)
	print(texto)
	print("Fallos: %d" % _visibles_sin_tomar)
	_devolver_respaldo()
	quit(1 if _visibles_sin_tomar > 0 else 0)
