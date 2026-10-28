extends SceneTree
## HU-20.6: arma el terreno del Escenario 2 (Lago y Bosque) desde la rejilla de alturas que exporta
## _generador_caso1/lago_mapa.py: una malla con color por vértice (R camino, G arena, B pasto) y su HeightMapShape3D.
## Headless. Uso: --headless --path game --script res://Herramientas/Caso1/terreno_lago.gd -- <alturas.json>

const SALIDA := "res://Escenas/Caso1/Lago/"

func _initialize() -> void:
	var datos: Dictionary = JSON.parse_string(FileAccess.get_file_as_string(OS.get_cmdline_user_args()[0]))
	var x0 := int(datos.x0)
	var z0 := int(datos.z0)
	var ancho := int(datos.ancho)
	var fondo := int(datos.fondo)
	var alturas: Array = datos.alturas
	var colores: Array = datos.colores
	DirAccess.make_dir_recursive_absolute(SALIDA)

	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	for j in fondo:
		for i in ancho:
			var k := j * ancho + i
			st.set_color(Color(colores[k * 3], colores[k * 3 + 1], colores[k * 3 + 2]))
			st.set_uv(Vector2(x0 + i, z0 + j))
			st.add_vertex(Vector3(x0 + i, alturas[k], z0 + j))
	for j in fondo - 1:
		for i in ancho - 1:
			var a := j * ancho + i
			var b := a + 1
			var c := a + ancho
			var d := c + 1
			st.add_index(a); st.add_index(b); st.add_index(c)
			st.add_index(b); st.add_index(d); st.add_index(c)
	st.generate_normals()
	st.generate_tangents()
	var malla := st.commit()
	print("malla: ", error_string(ResourceSaver.save(malla, SALIDA + "terreno_lago.res")))

	var forma := HeightMapShape3D.new()
	forma.map_width = ancho
	forma.map_depth = fondo
	var mapa := PackedFloat32Array()
	mapa.resize(ancho * fondo)
	for k in ancho * fondo:
		mapa[k] = alturas[k]
	forma.map_data = mapa
	print("forma: ", error_string(ResourceSaver.save(forma, SALIDA + "terreno_lago_forma.res")))
	quit()
