extends "res://Herramientas/Pistas/generar_modelos.gd"
## Capítulo 2: modelos de las pistas y papeles (papel con grosor de ~1 mm, curvatura y reverso con su textura).
## Antes: generar_texturas_caso2.gd y un --import.
## Uso: --headless --path game --script res://Herramientas/Caso2/generar_modelos_caso2.gd
## Salida: res://Assets/Pistas/Modelos/<id>/<id>.tscn (+ mallas .res). Pivote en la base; el frente mira arriba (+Y).

func _initialize() -> void:
	_azar.seed = 2027
	for carpeta in ["materiales", "dibujo_eriz", "invitacion_miawrio", "sobre_eriz", "registro_accesos", "planos_clinica",
			"contrato_ixodes", "estado_cuenta", "registro_llamadas2", "abrecartas", "expediente_filtrado"]:
		DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(BASE + carpeta))
	_dibujo_eriz()
	_invitacion()
	_sobre()
	_registro_accesos()
	_planos()
	_hoja_simple("contrato_ixodes", "ContratoIxodes", "c2_contrato", "c2_contrato_reverso", true)
	_hoja_simple("estado_cuenta", "EstadoCuenta", "c2_estado_cuenta", "c2_contrato_reverso", false)
	_hoja_simple("registro_llamadas2", "RegistroLlamadas2", "c2_registro_llamadas", "c2_contrato_reverso", false)
	_abrecartas()
	_expediente()
	print("Modelos del Capítulo 2 listos")
	quit()

func _brillante(nombre: String, textura: String) -> StandardMaterial3D:
	return _mat(nombre, {"albedo_texture": _gen(textura), "roughness": 0.25, "metallic_specular": 0.6,
		"texture_filter": BaseMaterial3D.TEXTURE_FILTER_LINEAR_WITH_MIPMAPS_ANISOTROPIC})

func _clip(raiz: Node3D, nombre: String, pos: Vector3) -> void:
	var metal := _mat("clip_metal", {"albedo_color": Color(0.75, 0.76, 0.78), "metallic": 1.0, "roughness": 0.3})
	var r := 0.0006
	var pts := [Vector3(0, 0, -0.012), Vector3(0, 0, 0.012), Vector3(0.004, 0, 0.014), Vector3(0.008, 0, 0.012),
		Vector3(0.008, 0, -0.008), Vector3(0.0045, 0, -0.01), Vector3(0.0015, 0, -0.008), Vector3(0.0015, 0, 0.008)]
	var radios := []
	for p in pts:
		radios.append(r)
	var malla := _guardar_malla(M.tubo(pts, radios, 6, metal), BASE + "dibujo_eriz/%s.res" % nombre.to_snake_case())
	_nodo(raiz, nombre, malla, Transform3D(Basis.IDENTITY, pos))

# P01 · El dibujo a carboncillo con la foto borrosa sujeta con un clip; al reverso, la nota de Eriz
func _dibujo_eriz() -> void:
	var frente := _papel("c2_dibujo_frente", "c2_dibujo_frente")
	var reverso := _papel("c2_dibujo_reverso", "c2_dibujo_reverso")
	var forma := func(u: float, v: float) -> float:
		return 0.003 * pow(2.0 * u - 1.0, 2) + 0.004 * pow(v, 3)
	var raiz := Node3D.new()
	raiz.name = "DibujoEriz"
	_nodo(raiz, "Dibujo", _guardar_malla(M.hoja(0.21, 0.297, forma, frente, reverso, 0.0009, 20, 28), BASE + "dibujo_eriz/dibujo.res"))
	var foto_frente := _brillante("c2_foto_camioneta", "c2_foto_camioneta")
	var foto_reverso := _papel("c2_foto_reverso", "c2_foto_reverso")
	var plana := func(u: float, v: float) -> float:
		return 0.0015 * pow(2.0 * u - 1.0, 2)
	var foto := _guardar_malla(M.hoja(0.15, 0.10, plana, foto_frente, foto_reverso, 0.0006, 12, 10), BASE + "dibujo_eriz/foto.res")
	_nodo(raiz, "Foto", foto, Transform3D(Basis(Vector3.UP, deg_to_rad(-8)), Vector3(0.035, 0.0045, -0.085)))
	_clip(raiz, "Clip", Vector3(0.06, 0.006, -0.135))
	_escena(raiz, BASE + "dibujo_eriz/dibujo_eriz.tscn")

# Invitación de Miawrio: cartulina gruesa (1.5 mm) con letras doradas
func _invitacion() -> void:
	var frente := _papel("c2_invitacion", "c2_invitacion", {"metallic_specular": 0.7})
	var reverso := _papel("c2_invitacion_reverso", "c2_invitacion_reverso")
	var forma := func(u: float, v: float) -> float:
		return 0.0012 * pow(2.0 * v - 1.0, 2)
	var raiz := Node3D.new()
	raiz.name = "InvitacionMiawrio"
	# La tarjeta es apaisada: ancho en X
	_nodo(raiz, "Tarjeta", _guardar_malla(M.hoja(0.21, 0.15, forma, frente, reverso, 0.0015, 18, 14), BASE + "invitacion_miawrio/tarjeta.res"))
	_escena(raiz, BASE + "invitacion_miawrio/invitacion_miawrio.tscn")

func _sobre() -> void:
	var frente := _papel("c2_sobre_eriz", "c2_sobre_eriz")
	var reverso := _papel("c2_sobre_reverso", "c2_sobre_eriz", {"albedo_color": Color(0.92, 0.9, 0.86)})
	var forma := func(u: float, v: float) -> float:
		return 0.003 * sin(u * PI) * sin(v * PI)
	var raiz := Node3D.new()
	raiz.name = "SobreEriz"
	_nodo(raiz, "Sobre", _guardar_malla(M.hoja(0.23, 0.16, forma, frente, reverso, 0.0012, 16, 12), BASE + "sobre_eriz/sobre.res"))
	_escena(raiz, BASE + "sobre_eriz/sobre_eriz.tscn")

# P02 · Pantalla de registro de la recepción (monitor delgado con la pantalla encendida)
func _registro_accesos() -> void:
	var carcasa := _mat("c2_monitor", {"albedo_color": Color(0.12, 0.12, 0.13), "roughness": 0.4})
	var pantalla := _mat("c2_pantalla_registro", {"albedo_texture": _gen("c2_registro_accesos"), "emission_enabled": true,
		"emission_texture": _gen("c2_registro_accesos"), "emission_energy_multiplier": 0.9, "roughness": 0.15})
	var raiz := Node3D.new()
	raiz.name = "RegistroAccesos"
	var marco := _guardar_malla(M.caja_redondeada(0.5, 0.32, 0.025, 0.01, 0.003, carcasa), BASE + "registro_accesos/marco.res")
	var inclinado := Basis(Vector3.RIGHT, deg_to_rad(-12))
	_nodo(raiz, "Marco", marco, Transform3D(inclinado, Vector3(0, 0.27, 0)))
	var cara := ArrayMesh.new()
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	st.set_material(pantalla)
	var a := Vector3(-0.235, -0.147, 0.0131)
	var b := Vector3(0.235, -0.147, 0.0131)
	var c := Vector3(0.235, 0.147, 0.0131)
	var d := Vector3(-0.235, 0.147, 0.0131)
	M._tri(st, a, c, b, Vector2(0, 1), Vector2(1, 0), Vector2(1, 1))
	M._tri(st, a, d, c, Vector2(0, 1), Vector2(0, 0), Vector2(1, 0))
	M._cerrar(st, cara)
	_nodo(raiz, "Pantalla", _guardar_malla(cara, BASE + "registro_accesos/pantalla.res"), Transform3D(inclinado, Vector3(0, 0.27, 0)))
	var pie := _guardar_malla(M.torno([Vector2(0.09, 0), Vector2(0.09, 0.008), Vector2(0.02, 0.012), Vector2(0.018, 0.13), Vector2(0.0, 0.13)], 20, carcasa), BASE + "registro_accesos/pie.res")
	_nodo(raiz, "Pie", pie, Transform3D(Basis.IDENTITY, Vector3(0, 0, -0.04)))
	_escena(raiz, BASE + "registro_accesos/registro_accesos.tscn")

# P03 · Planos desplegados, con las orillas que se quieren volver a enrollar
func _planos() -> void:
	var frente := _papel("c2_planos_clinica", "c2_planos_clinica")
	var reverso := _papel("c2_planos_reverso", "c2_contrato_reverso", {"albedo_color": Color(0.85, 0.9, 0.98)})
	var forma := func(u: float, v: float) -> float:
		return 0.03 * pow(maxf(0.0, (absf(u - 0.5) - 0.38) / 0.12), 2) + 0.002 * sin(v * PI * 3.0)
	var raiz := Node3D.new()
	raiz.name = "PlanosClinica"
	_nodo(raiz, "Plano", _guardar_malla(M.hoja(0.84, 0.574, forma, frente, reverso, 0.0006, 48, 30), BASE + "planos_clinica/plano.res"))
	_escena(raiz, BASE + "planos_clinica/planos_clinica.tscn")

func _hoja_simple(carpeta: String, nombre: String, textura: String, textura_reverso: String, engrapada: bool) -> void:
	var frente := _papel(textura, textura)
	var reverso := _papel(textura_reverso, textura_reverso)
	var forma := func(u: float, v: float) -> float:
		return 0.002 * pow(2.0 * u - 1.0, 2) + 0.005 * pow(v, 3) + _azar.randf_range(0.0, 0.0002)
	var raiz := Node3D.new()
	raiz.name = nombre
	_nodo(raiz, "Hoja", _guardar_malla(M.hoja(0.21, 0.297, forma, frente, reverso, 0.0009, 18, 26), BASE + "%s/%s.res" % [carpeta, carpeta]))
	if engrapada:
		# Una segunda hoja debajo y la grapa en la esquina
		_nodo(raiz, "HojaDebajo", _guardar_malla(M.hoja(0.21, 0.297, forma, reverso, reverso, 0.0009, 8, 10), BASE + "%s/debajo.res" % carpeta),
			Transform3D(Basis(Vector3.UP, deg_to_rad(2)), Vector3(0.002, -0.001, 0.002)))
		var metal := _mat("grapa_metal", {"albedo_color": Color(0.8, 0.8, 0.82), "metallic": 1.0, "roughness": 0.35})
		var grapa := _guardar_malla(M.tubo([Vector3(-0.006, 0.0002, 0), Vector3(-0.006, 0.0012, 0), Vector3(0.006, 0.0012, 0), Vector3(0.006, 0.0002, 0)], [0.0004, 0.0004, 0.0004, 0.0004], 5, metal), BASE + "%s/grapa.res" % carpeta)
		_nodo(raiz, "Grapa", grapa, Transform3D(Basis(Vector3.UP, deg_to_rad(45)), Vector3(-0.09, 0.0005, -0.134)))
	_escena(raiz, BASE + "%s/%s.tscn" % [carpeta, carpeta])

# P07 · Abrecartas de bronce con tres puntas curvas (como una garra) y la tarjeta del socio
func _abrecartas() -> void:
	var bronce := _mat("c2_bronce", {"albedo_color": Color(0.72, 0.5, 0.26), "metallic": 1.0, "roughness": 0.32})
	var raiz := Node3D.new()
	raiz.name = "Abrecartas"
	# Mango por revolución, acostado sobre la mesa (a lo largo de X)
	var mango := _guardar_malla(M.torno([Vector2(0.0, 0.0), Vector2(0.008, 0.002), Vector2(0.011, 0.02), Vector2(0.009, 0.06), Vector2(0.012, 0.085), Vector2(0.007, 0.095), Vector2(0.0, 0.097)], 14, bronce), BASE + "abrecartas/mango.res")
	_nodo(raiz, "Mango", mango, Transform3D(Basis(Vector3.BACK, deg_to_rad(-90)), Vector3(-0.1, 0.011, 0)))
	# Tres puntas curvas que salen del mango y se cierran como una garra
	var puntas := ArrayMesh.new()
	for i in 3:
		var abre := (i - 1) * 0.012
		var pts := []
		var radios := []
		for k in 9:
			var t := k / 8.0
			pts.append(Vector3(-0.003 + t * 0.13, 0.011 + sin(t * PI * 0.8) * 0.012 * (1.0 - t * 0.4), abre * (1.0 - t * t) + abre * 0.4 * t))
			radios.append(lerpf(0.0045, 0.0006, t))
		M.tubo(pts, radios, 8, bronce, true, puntas)
	_nodo(raiz, "Puntas", _guardar_malla(puntas, BASE + "abrecartas/puntas.res"))
	# La tarjeta: «Para M., de un socio paciente.»
	var tarjeta_f := _papel("c2_tarjeta_socio", "c2_tarjeta_socio")
	var tarjeta_r := _papel("c2_tarjeta_reverso", "c2_contrato_reverso")
	var forma := func(u: float, v: float) -> float:
		return 0.001 * pow(2.0 * u - 1.0, 2)
	var tarjeta := _guardar_malla(M.hoja(0.09, 0.056, forma, tarjeta_f, tarjeta_r, 0.0008, 8, 6), BASE + "abrecartas/tarjeta.res")
	_nodo(raiz, "Tarjeta", tarjeta, Transform3D(Basis(Vector3.UP, deg_to_rad(12)), Vector3(0.0, 0.0, 0.065)))
	_escena(raiz, BASE + "abrecartas/abrecartas.tscn")

# P08 · Copia del expediente de Pino Lunar: carpeta con sello de la Fiscalía
func _expediente() -> void:
	var portada := _papel("c2_expediente_portada", "c2_expediente_portada")
	var interior := _papel("c2_expediente_hoja", "c2_expediente_hoja")
	var canto := _mat("c2_expediente_canto", {"albedo_color": Color(0.93, 0.9, 0.82), "roughness": 0.95})
	var raiz := Node3D.new()
	raiz.name = "ExpedienteFiltrado"
	var plana := func(u: float, v: float) -> float:
		return 0.0
	# Bloque de hojas y la portada encima (carpeta cerrada)
	_nodo(raiz, "Hojas", _guardar_malla(M.hoja(0.215, 0.3, plana, interior, interior, 0.012, 4, 4), BASE + "expediente_filtrado/hojas.res"))
	var curva := func(u: float, v: float) -> float:
		return 0.0015 * pow(u, 2)
	_nodo(raiz, "Portada", _guardar_malla(M.hoja(0.225, 0.31, curva, portada, canto, 0.0012, 10, 10), BASE + "expediente_filtrado/portada.res"),
		Transform3D(Basis.IDENTITY, Vector3(0.004, 0.0122, 0)))
	_escena(raiz, BASE + "expediente_filtrado/expediente_filtrado.tscn")
