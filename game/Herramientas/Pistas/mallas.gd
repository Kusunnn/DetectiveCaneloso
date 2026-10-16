## HU-07: funciones para construir las mallas de las pistas hechas en el proyecto (papeles, libros, tubos,
## piezas por revolución, troncos...). Todas devuelven ArrayMesh con normales y tangentes listas para PBR.
## Convención: el papel está acostado en el plano XZ con el frente hacia +Y; u crece hacia +X y v hacia +Z
## (la parte de arriba de la hoja queda hacia -Z). Las caras de frente se arman en sentido horario.

# Agrega un triángulo (posiciones y UV)
static func _tri(st: SurfaceTool, a: Vector3, b: Vector3, c: Vector3, ua: Vector2, ub: Vector2, uc: Vector2) -> void:
	st.set_uv(ua)
	st.add_vertex(a)
	st.set_uv(ub)
	st.add_vertex(b)
	st.set_uv(uc)
	st.add_vertex(c)

static func _cerrar(st: SurfaceTool, malla: ArrayMesh) -> void:
	st.generate_normals()
	st.generate_tangents()
	st.commit(malla)

# Hoja de papel con relieve: altura(u, v) da la forma (dobleces, curvatura). Frente, reverso y cantos.
# uv_reverso_espejo: el reverso se lee bien al voltear la hoja de lado (sobre el eje Z).
static func hoja(ancho: float, largo: float, altura: Callable, mat_frente: Material, mat_reverso: Material,
		grosor := 0.0008, nx := 24, nz := 32, cantos := true, uv_reverso_espejo := true, malla: ArrayMesh = null) -> ArrayMesh:
	if malla == null:
		malla = ArrayMesh.new()
	var p := func(u: float, v: float, arriba: bool) -> Vector3:
		return Vector3((u - 0.5) * ancho, altura.call(u, v) + (grosor if arriba else 0.0), (v - 0.5) * largo)
	var frente := SurfaceTool.new()
	frente.begin(Mesh.PRIMITIVE_TRIANGLES)
	frente.set_material(mat_frente)
	var reverso := SurfaceTool.new()
	reverso.begin(Mesh.PRIMITIVE_TRIANGLES)
	reverso.set_material(mat_reverso)
	for i in nx:
		for j in nz:
			var u0 := float(i) / nx
			var u1 := float(i + 1) / nx
			var v0 := float(j) / nz
			var v1 := float(j + 1) / nz
			var a: Vector3 = p.call(u0, v0, true)
			var b: Vector3 = p.call(u1, v0, true)
			var c: Vector3 = p.call(u1, v1, true)
			var d: Vector3 = p.call(u0, v1, true)
			_tri(frente, a, b, c, Vector2(u0, v0), Vector2(u1, v0), Vector2(u1, v1))
			_tri(frente, a, c, d, Vector2(u0, v0), Vector2(u1, v1), Vector2(u0, v1))
			var ra: Vector3 = p.call(u0, v0, false)
			var rb: Vector3 = p.call(u1, v0, false)
			var rc: Vector3 = p.call(u1, v1, false)
			var rd: Vector3 = p.call(u0, v1, false)
			var su0 := 1.0 - u0 if uv_reverso_espejo else u0
			var su1 := 1.0 - u1 if uv_reverso_espejo else u1
			_tri(reverso, ra, rc, rb, Vector2(su0, v0), Vector2(su1, v1), Vector2(su1, v0))
			_tri(reverso, ra, rd, rc, Vector2(su0, v0), Vector2(su0, v1), Vector2(su1, v1))
	_cerrar(frente, malla)
	_cerrar(reverso, malla)
	if cantos and grosor > 0.0:
		var canto := SurfaceTool.new()
		canto.begin(Mesh.PRIMITIVE_TRIANGLES)
		canto.set_material(mat_reverso)
		var borde: Array[Vector2] = []
		for i in nx:
			borde.append(Vector2(float(i) / nx, 0))
		for j in nz:
			borde.append(Vector2(1, float(j) / nz))
		for i in nx:
			borde.append(Vector2(1.0 - float(i) / nx, 1))
		for j in nz:
			borde.append(Vector2(0, 1.0 - float(j) / nz))
		for k in borde.size():
			var q0: Vector2 = borde[k]
			var q1: Vector2 = borde[(k + 1) % borde.size()]
			var a: Vector3 = p.call(q0.x, q0.y, true)
			var b: Vector3 = p.call(q1.x, q1.y, true)
			var c: Vector3 = p.call(q1.x, q1.y, false)
			var d: Vector3 = p.call(q0.x, q0.y, false)
			_tri(canto, a, c, b, q0, q1, q1)
			_tri(canto, a, d, c, q0, q0, q1)
		_cerrar(canto, malla)
	return malla

# Pieza de papel con un contorno cualquiera (en UV 0..1 del documento entero), con grosor
static func pieza(contorno: PackedVector2Array, ancho: float, largo: float, mat_frente: Material, mat_reverso: Material, grosor := 0.0008) -> ArrayMesh:
	var malla := ArrayMesh.new()
	var indices := Geometry2D.triangulate_polygon(contorno)
	var pos := func(q: Vector2, y: float) -> Vector3:
		return Vector3((q.x - 0.5) * ancho, y, (q.y - 0.5) * largo)
	var frente := SurfaceTool.new()
	frente.begin(Mesh.PRIMITIVE_TRIANGLES)
	frente.set_material(mat_frente)
	var reverso := SurfaceTool.new()
	reverso.begin(Mesh.PRIMITIVE_TRIANGLES)
	reverso.set_material(mat_reverso)
	# Orientación: si el contorno va al revés, se invierten los triángulos para que el frente mire arriba
	# (Godot cuenta el giro con Y hacia arriba; en UV la v crece hacia abajo, por eso se niega)
	var horario := not Geometry2D.is_polygon_clockwise(contorno)
	for t in range(0, indices.size(), 3):
		var ia := indices[t]
		var ib := indices[t + 1]
		var ic := indices[t + 2]
		if not horario:
			var tmp := ib
			ib = ic
			ic = tmp
		var a := contorno[ia]
		var b := contorno[ib]
		var c := contorno[ic]
		_tri(frente, pos.call(a, grosor), pos.call(b, grosor), pos.call(c, grosor), a, b, c)
		_tri(reverso, pos.call(a, 0.0), pos.call(c, 0.0), pos.call(b, 0.0), Vector2(1 - a.x, a.y), Vector2(1 - c.x, c.y), Vector2(1 - b.x, b.y))
	_cerrar(frente, malla)
	_cerrar(reverso, malla)
	var canto := SurfaceTool.new()
	canto.begin(Mesh.PRIMITIVE_TRIANGLES)
	canto.set_material(mat_reverso)
	for k in contorno.size():
		var q0 := contorno[k]
		var q1 := contorno[(k + 1) % contorno.size()]
		if not horario:
			var tmp := q0
			q0 = q1
			q1 = tmp
		var a: Vector3 = pos.call(q0, grosor)
		var b: Vector3 = pos.call(q1, grosor)
		var c: Vector3 = pos.call(q1, 0.0)
		var d: Vector3 = pos.call(q0, 0.0)
		_tri(canto, a, c, b, q0, q1, q1)
		_tri(canto, a, d, c, q0, q0, q1)
	_cerrar(canto, malla)
	return malla

# Libro o libreta abierta: tapas, bloque de hojas curvado hacia el lomo y cantos
static func libro(ancho_pagina: float, largo: float, grueso: float, mat_paginas: Material, mat_tapa: Material, mat_canto: Material) -> ArrayMesh:
	var malla := ArrayMesh.new()
	var tapa := 0.003
	var forma := func(t: float) -> float:
		return 0.55 + 0.45 * sin(minf(t / 0.35, 1.0) * PI / 2.0) - 0.07 * maxf(0.0, (t - 0.6) / 0.4)
	var alto := func(u: float) -> float:
		return tapa + grueso * forma.call(absf(u - 0.5) * 2.0)
	# Hojas: superficie de arriba (la textura es la doble página)
	var paginas := SurfaceTool.new()
	paginas.begin(Mesh.PRIMITIVE_TRIANGLES)
	paginas.set_material(mat_paginas)
	var n := 48
	var m := 8
	var punto := func(u: float, v: float) -> Vector3:
		return Vector3((u - 0.5) * 2.0 * ancho_pagina, alto.call(u), (v - 0.5) * largo)
	for i in n:
		for j in m:
			var u0 := float(i) / n
			var u1 := float(i + 1) / n
			var v0 := float(j) / m
			var v1 := float(j + 1) / m
			_tri(paginas, punto.call(u0, v0), punto.call(u1, v0), punto.call(u1, v1), Vector2(u0, v0), Vector2(u1, v0), Vector2(u1, v1))
			_tri(paginas, punto.call(u0, v0), punto.call(u1, v1), punto.call(u0, v1), Vector2(u0, v0), Vector2(u1, v1), Vector2(u0, v1))
	_cerrar(paginas, malla)
	# Cantos del bloque de hojas (rayitas)
	var canto := SurfaceTool.new()
	canto.begin(Mesh.PRIMITIVE_TRIANGLES)
	canto.set_material(mat_canto)
	for i in n:
		var u0 := float(i) / n
		var u1 := float(i + 1) / n
		for lado in [0.0, 1.0]:
			var a: Vector3 = punto.call(u0, lado)
			var b: Vector3 = punto.call(u1, lado)
			var c := Vector3(b.x, tapa, b.z)
			var d := Vector3(a.x, tapa, a.z)
			var hu0: float = alto.call(u0) / 0.03
			var hu1: float = alto.call(u1) / 0.03
			if lado == 0.0:
				_tri(canto, a, c, b, Vector2(u0, 0), Vector2(u1, hu1), Vector2(u1, 0))
				_tri(canto, a, d, c, Vector2(u0, 0), Vector2(u0, hu0), Vector2(u1, hu1))
			else:
				_tri(canto, a, b, c, Vector2(u0, 0), Vector2(u1, 0), Vector2(u1, hu1))
				_tri(canto, a, c, d, Vector2(u0, 0), Vector2(u1, hu1), Vector2(u0, hu0))
	for lado in [0.0, 1.0]:
		var a: Vector3 = punto.call(lado, 0.0)
		var b: Vector3 = punto.call(lado, 1.0)
		var c := Vector3(b.x, tapa, b.z)
		var d := Vector3(a.x, tapa, a.z)
		if lado == 0.0:
			_tri(canto, a, b, c, Vector2(0, 0), Vector2(4, 0), Vector2(4, 1))
			_tri(canto, a, c, d, Vector2(0, 0), Vector2(4, 1), Vector2(0, 1))
		else:
			_tri(canto, a, c, b, Vector2(0, 0), Vector2(4, 1), Vector2(4, 0))
			_tri(canto, a, d, c, Vector2(0, 0), Vector2(0, 1), Vector2(4, 1))
	_cerrar(canto, malla)
	# Tapas: una caja delgada un poco más grande que las hojas
	var caja := SurfaceTool.new()
	caja.begin(Mesh.PRIMITIVE_TRIANGLES)
	caja.set_material(mat_tapa)
	_caja(caja, Vector3(ancho_pagina * 2.0 + 0.012, tapa, largo + 0.01), Vector3(0, tapa / 2.0, 0))
	_cerrar(caja, malla)
	return malla

# Caja (6 caras) con UV de 0 a 1 por cara
static func _caja(st: SurfaceTool, tam: Vector3, centro: Vector3) -> void:
	var h := tam / 2.0
	var caras := [
		[Vector3(0, 1, 0), Vector3(1, 0, 0), Vector3(0, 0, 1)],
		[Vector3(0, -1, 0), Vector3(1, 0, 0), Vector3(0, 0, -1)],
		[Vector3(0, 0, 1), Vector3(1, 0, 0), Vector3(0, -1, 0)],
		[Vector3(0, 0, -1), Vector3(-1, 0, 0), Vector3(0, -1, 0)],
		[Vector3(1, 0, 0), Vector3(0, 0, -1), Vector3(0, -1, 0)],
		[Vector3(-1, 0, 0), Vector3(0, 0, 1), Vector3(0, -1, 0)],
	]
	for cara in caras:
		var nrm: Vector3 = cara[0]
		var der: Vector3 = cara[1]
		var aba: Vector3 = cara[2]
		var c := centro + nrm * h
		var dx := der * h
		var dy := aba * h
		var a := c - dx - dy
		var b := c + dx - dy
		var cc := c + dx + dy
		var d := c - dx + dy
		_tri(st, a, b, cc, Vector2(0, 0), Vector2(1, 0), Vector2(1, 1))
		_tri(st, a, cc, d, Vector2(0, 0), Vector2(1, 1), Vector2(0, 1))

static func caja(tam: Vector3, mat: Material, centro := Vector3.ZERO) -> ArrayMesh:
	var malla := ArrayMesh.new()
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	st.set_material(mat)
	_caja(st, tam, centro)
	_cerrar(st, malla)
	return malla

# Tubo a lo largo de una línea de puntos (radio puede variar por punto)
static func tubo(puntos: Array, radios: Array, lados: int, mat: Material, tapas := true, malla: ArrayMesh = null) -> ArrayMesh:
	if malla == null:
		malla = ArrayMesh.new()
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	st.set_material(mat)
	var anillos: Array = []
	var arriba := Vector3.UP
	for i in puntos.size():
		var p: Vector3 = puntos[i]
		var dir: Vector3 = ((puntos[mini(i + 1, puntos.size() - 1)] as Vector3) - (puntos[maxi(i - 1, 0)] as Vector3)).normalized()
		if absf(dir.dot(arriba)) > 0.95:
			arriba = Vector3.FORWARD
		var x := dir.cross(arriba).normalized()
		var y := x.cross(dir).normalized()
		var anillo: Array = []
		var r: float = radios[i]
		for k in lados + 1:
			var ang := k * TAU / lados
			anillo.append(p + (x * cos(ang) + y * sin(ang)) * r)
		anillos.append(anillo)
	for i in puntos.size() - 1:
		for k in lados:
			var a: Vector3 = anillos[i][k]
			var b: Vector3 = anillos[i][k + 1]
			var c: Vector3 = anillos[i + 1][k + 1]
			var d: Vector3 = anillos[i + 1][k]
			var u0 := float(k) / lados
			var u1 := float(k + 1) / lados
			var v0 := float(i) / (puntos.size() - 1)
			var v1 := float(i + 1) / (puntos.size() - 1)
			_tri(st, a, b, c, Vector2(u0, v0), Vector2(u1, v0), Vector2(u1, v1))
			_tri(st, a, c, d, Vector2(u0, v0), Vector2(u1, v1), Vector2(u0, v1))
	if tapas:
		for extremo in [0, puntos.size() - 1]:
			var centro: Vector3 = puntos[extremo]
			for k in lados:
				var a: Vector3 = anillos[extremo][k]
				var b: Vector3 = anillos[extremo][k + 1]
				if extremo == 0:
					_tri(st, centro, b, a, Vector2(0.5, 0.5), Vector2(1, 0), Vector2(0, 0))
				else:
					_tri(st, centro, a, b, Vector2(0.5, 0.5), Vector2(0, 0), Vector2(1, 0))
	_cerrar(st, malla)
	return malla

# Pieza por revolución alrededor de Y: perfil = puntos (radio, altura) de abajo hacia arriba
static func torno(perfil: Array, lados: int, mat: Material, alto_uv := 0.1, malla: ArrayMesh = null) -> ArrayMesh:
	if malla == null:
		malla = ArrayMesh.new()
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	st.set_material(mat)
	for i in perfil.size() - 1:
		var p0: Vector2 = perfil[i]
		var p1: Vector2 = perfil[i + 1]
		for k in lados:
			var a0 := k * TAU / lados
			var a1 := (k + 1) * TAU / lados
			var a := Vector3(cos(a0) * p0.x, p0.y, sin(a0) * p0.x)
			var b := Vector3(cos(a1) * p0.x, p0.y, sin(a1) * p0.x)
			var c := Vector3(cos(a1) * p1.x, p1.y, sin(a1) * p1.x)
			var d := Vector3(cos(a0) * p1.x, p1.y, sin(a0) * p1.x)
			var u0 := float(k) / lados
			var u1 := float(k + 1) / lados
			var v0 := 1.0 - p0.y / alto_uv
			var v1 := 1.0 - p1.y / alto_uv
			_tri(st, a, b, c, Vector2(u0, v0), Vector2(u1, v0), Vector2(u1, v1))
			_tri(st, a, c, d, Vector2(u0, v0), Vector2(u1, v1), Vector2(u0, v1))
	_cerrar(st, malla)
	return malla

# Tronco partido: un sector de cilindro (de ang0 a ang1) a lo largo de Z, con corteza, cortes y astillado
static func tronco(radio: float, largo: float, ang0: float, ang1: float, mat_corteza: Material, mat_corte: Material, mat_astilla: Material, lados := 18) -> ArrayMesh:
	var malla := ArrayMesh.new()
	var corteza := SurfaceTool.new()
	corteza.begin(Mesh.PRIMITIVE_TRIANGLES)
	corteza.set_material(mat_corteza)
	var irregular := func(ang: float) -> float:
		return radio * (1.0 + 0.04 * sin(ang * 7.0) + 0.03 * sin(ang * 13.0 + 1.0))
	for k in lados:
		var a0 := lerpf(ang0, ang1, float(k) / lados)
		var a1 := lerpf(ang0, ang1, float(k + 1) / lados)
		var r0: float = irregular.call(a0)
		var r1: float = irregular.call(a1)
		var p0 := Vector3(cos(a0) * r0, sin(a0) * r0, 0)
		var p1 := Vector3(cos(a1) * r1, sin(a1) * r1, 0)
		var u0 := (a0 - ang0) * radio / 0.3
		var u1 := (a1 - ang0) * radio / 0.3
		var a := p0 + Vector3(0, 0, -largo / 2)
		var b := p1 + Vector3(0, 0, -largo / 2)
		var c := p1 + Vector3(0, 0, largo / 2)
		var d := p0 + Vector3(0, 0, largo / 2)
		_tri(corteza, a, c, b, Vector2(u0, 0), Vector2(u1, largo / 0.3), Vector2(u1, 0))
		_tri(corteza, a, d, c, Vector2(u0, 0), Vector2(u0, largo / 0.3), Vector2(u1, largo / 0.3))
	_cerrar(corteza, malla)
	# Cortes de los extremos (anillos del árbol)
	var corte := SurfaceTool.new()
	corte.begin(Mesh.PRIMITIVE_TRIANGLES)
	corte.set_material(mat_corte)
	var parcial := ang1 - ang0 < TAU - 0.01
	for extremo in [-1.0, 1.0]:
		var z: float = extremo * largo / 2
		var centro := Vector3(0, 0, z)
		for k in lados:
			var a0 := lerpf(ang0, ang1, float(k) / lados)
			var a1 := lerpf(ang0, ang1, float(k + 1) / lados)
			var r0: float = irregular.call(a0)
			var r1: float = irregular.call(a1)
			var p0 := Vector3(cos(a0) * r0, sin(a0) * r0, z)
			var p1 := Vector3(cos(a1) * r1, sin(a1) * r1, z)
			var uv := func(p: Vector3) -> Vector2:
				return Vector2(0.5 + p.x / radio * 0.47, 0.5 - p.y / radio * 0.47)
			if extremo > 0:
				_tri(corte, centro, p1, p0, Vector2(0.5, 0.5), uv.call(p1), uv.call(p0))
			else:
				_tri(corte, centro, p0, p1, Vector2(0.5, 0.5), uv.call(p0), uv.call(p1))
	_cerrar(corte, malla)
	# Caras de la astilla (donde se partió el tronco)
	if parcial:
		var astilla := SurfaceTool.new()
		astilla.begin(Mesh.PRIMITIVE_TRIANGLES)
		astilla.set_material(mat_astilla)
		for borde in [[ang0, true], [ang1, false]]:
			var ang: float = borde[0]
			var r: float = irregular.call(ang)
			var fuera := Vector3(cos(ang) * r, sin(ang) * r, 0)
			var a := Vector3(0, 0, -largo / 2)
			var b := fuera + Vector3(0, 0, -largo / 2)
			var c := fuera + Vector3(0, 0, largo / 2)
			var d := Vector3(0, 0, largo / 2)
			if borde[1]:
				_tri(astilla, a, c, b, Vector2(0, 0), Vector2(r / 0.15, largo / 0.3), Vector2(r / 0.15, 0))
				_tri(astilla, a, d, c, Vector2(0, 0), Vector2(0, largo / 0.3), Vector2(r / 0.15, largo / 0.3))
			else:
				_tri(astilla, a, b, c, Vector2(0, 0), Vector2(r / 0.15, 0), Vector2(r / 0.15, largo / 0.3))
				_tri(astilla, a, c, d, Vector2(0, 0), Vector2(r / 0.15, largo / 0.3), Vector2(0, largo / 0.3))
		_cerrar(astilla, malla)
	return malla

# Caja con esquinas redondeadas (contorno en XY) extruida en Z, con un bisel en el frente y atrás
static func caja_redondeada(ancho: float, alto: float, fondo: float, radio: float, bisel: float, mat: Material, segmentos := 6) -> ArrayMesh:
	var malla := ArrayMesh.new()
	var contorno := func(encoge: float) -> PackedVector2Array:
		var pts := PackedVector2Array()
		var w := ancho / 2.0 - encoge
		var h := alto / 2.0 - encoge
		var r := maxf(radio - encoge, 0.0005)
		for esquina in [[Vector2(w - r, h - r), 0.0], [Vector2(-w + r, h - r), PI / 2], [Vector2(-w + r, -h + r), PI], [Vector2(w - r, -h + r), 1.5 * PI]]:
			for s in segmentos + 1:
				var ang: float = esquina[1] + s * (PI / 2) / segmentos
				pts.append(esquina[0] + Vector2(cos(ang), sin(ang)) * r)
		return pts
	var lleno: PackedVector2Array = contorno.call(0.0)
	var chico: PackedVector2Array = contorno.call(bisel)
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	st.set_material(mat)
	var anillos := [[chico, -fondo / 2], [lleno, -fondo / 2 + bisel], [lleno, fondo / 2 - bisel], [chico, fondo / 2]]
	var uv := func(p: Vector3) -> Vector2:
		return Vector2(p.x / ancho + 0.5, 0.5 - p.y / alto)
	for i in anillos.size() - 1:
		var c0: PackedVector2Array = anillos[i][0]
		var c1: PackedVector2Array = anillos[i + 1][0]
		var z0: float = anillos[i][1]
		var z1: float = anillos[i + 1][1]
		for k in c0.size():
			var k1 := (k + 1) % c0.size()
			var a := Vector3(c0[k].x, c0[k].y, z0)
			var b := Vector3(c0[k1].x, c0[k1].y, z0)
			var c := Vector3(c1[k1].x, c1[k1].y, z1)
			var d := Vector3(c1[k].x, c1[k].y, z1)
			_tri(st, a, c, b, uv.call(a), uv.call(c), uv.call(b))
			_tri(st, a, d, c, uv.call(a), uv.call(d), uv.call(c))
	for lado in [-1.0, 1.0]:
		var z: float = lado * fondo / 2
		var idx := Geometry2D.triangulate_polygon(chico)
		for t in range(0, idx.size(), 3):
			var a := Vector3(chico[idx[t]].x, chico[idx[t]].y, z)
			var b := Vector3(chico[idx[t + 1]].x, chico[idx[t + 1]].y, z)
			var c := Vector3(chico[idx[t + 2]].x, chico[idx[t + 2]].y, z)
			if lado > 0:
				_tri(st, a, c, b, uv.call(a), uv.call(c), uv.call(b))
			else:
				_tri(st, a, b, c, uv.call(a), uv.call(b), uv.call(c))
	_cerrar(st, malla)
	return malla

# Tela colgada o acostada: una malla en una cuadrícula que solo conserva los triángulos dentro de la silueta
static func tela(ancho: float, alto: float, dentro: Callable, relieve: Callable, mat: Material, nx := 40, ny := 56) -> ArrayMesh:
	var malla := ArrayMesh.new()
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	st.set_material(mat)
	var p := func(u: float, v: float) -> Vector3:
		return Vector3((u - 0.5) * ancho, (1.0 - v) * alto, relieve.call(u, v))
	for i in nx:
		for j in ny:
			var u0 := float(i) / nx
			var u1 := float(i + 1) / nx
			var v0 := float(j) / ny
			var v1 := float(j + 1) / ny
			if not (dentro.call(u0, v0) and dentro.call(u1, v0) and dentro.call(u1, v1) and dentro.call(u0, v1)):
				continue
			var a: Vector3 = p.call(u0, v0)
			var b: Vector3 = p.call(u1, v0)
			var c: Vector3 = p.call(u1, v1)
			var d: Vector3 = p.call(u0, v1)
			_tri(st, a, b, c, Vector2(u0, v0), Vector2(u1, v0), Vector2(u1, v1))
			_tri(st, a, c, d, Vector2(u0, v0), Vector2(u1, v1), Vector2(u0, v1))
	_cerrar(st, malla)
	return malla
