@tool
extends EditorScript
## Auditoría del escenario abierto en el editor (o del campamento si no hay ninguno abierto).
## Uso: abre este archivo en el editor de scripts y pulsa Archivo → Ejecutar (Ctrl+Mayús+X).
## El reporte sale en la consola y se guarda en res://Herramientas/reportes/.

const ESCENA_POR_DEFECTO := "res://Escenas/Caso1/escenario_campamento.tscn"

func _run() -> void:
	var abierta := get_editor_interface().get_edited_scene_root()
	var ruta := abierta.scene_file_path if abierta and abierta is Node3D else ESCENA_POR_DEFECTO
	# Se revisa una copia en un mundo aparte, para no tocar la escena que se está editando
	var visor := SubViewport.new()
	visor.own_world_3d = true
	visor.size = Vector2i(4, 4)
	visor.render_target_update_mode = SubViewport.UPDATE_DISABLED
	get_editor_interface().get_base_control().add_child(visor)
	var copia: Node3D = load(ruta).instantiate()
	visor.add_child(copia)
	var auditor := AuditorEscenario.new()
	var hallazgos := await auditor.auditar(copia)
	var texto := AuditorEscenario.reporte(hallazgos, ruta)
	print(texto)
	_guardar(texto, ruta)
	visor.queue_free()

func _guardar(texto: String, escena: String) -> void:
	DirAccess.make_dir_recursive_absolute("res://Herramientas/reportes")
	var nombre := "res://Herramientas/reportes/auditoria_%s_%s.md" % [escena.get_file().get_basename(), Time.get_datetime_string_from_system().replace(":", "-")]
	var f := FileAccess.open(nombre, FileAccess.WRITE)
	f.store_string(texto)
	print("Reporte guardado en ", nombre)
