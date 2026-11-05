# Caneloso del menú de inicio: pose C «Listo para el caso», luces de la farola y la puerta, render en Cycles.
# Arma la escena desde cero con el modelo del juego (Assets/Cinematicas/Blender/caneloso.glb, exportado con
# Herramientas/exportar_modelo_blender.gd) y la lupa de Poly Haven; no toca el modelo del juego.
#
# Uso (desde la carpeta game/):
#   blender -b --factory-startup --python Assets/Menu/Blender/caneloso_menu.py -- <modo> [salida]
# Modos:
#   vista   renders rápidos (Eevee) de la pose desde varias cámaras, para revisarla
#   prueba  render de prueba en Cycles a media resolución (iluminado; la silueta sale de su alfa)
#   escena  solo guarda caneloso_menu.blend
import bpy, bmesh, sys, os, math
from mathutils import Vector, Matrix, Quaternion, Euler

BASE = os.path.dirname(os.path.abspath(__file__))                      # Assets/Menu/Blender
GAME = os.path.abspath(os.path.join(BASE, "..", "..", ".."))           # game/
MODELO = os.path.join(GAME, "Assets", "Cinematicas", "Blender", "caneloso.glb")
LUPA = os.path.join(GAME, "Assets", "Cinematicas", "Modelos", "polyhaven", "magnifying_glass_01", "magnifying_glass_01_1k.gltf")

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
MODO = args[0] if args else "vista"
SALIDA = args[1] if len(args) > 1 else os.path.join(BASE, "render")

# Lienzo final (el doble de la imagen anterior de 752×1079)
ANCHO, ALTO = 1504, 2158

# ======================================================================
# Pose C. Ejes del personaje antes de girarlo: +X = su izquierda (derecha de la pantalla), -Y = al frente,
# Z = arriba, piso en z = 0. Los lados «de la pantalla» siguen las referencias aprobadas: jarra a la
# izquierda de la pantalla (su brazo derecho) y lupa a la derecha (su brazo izquierdo).
# ======================================================================
POSE = {
    "giro_cuerpo": 18.0,          # 3/4 hacia la derecha de la pantalla
    "cadera_mover": (-0.025, 0.0, -0.022),
    "cadera": [("y", 5.0)],       # sube la cadera del lado de la pierna de apoyo (su derecha)
    "columna": [("y", -4.0), ("x", 2.0)],
    "columna2": [("y", -3.0), ("z", -4.0), ("x", 2.0)],
    "clavicula_jarra": [("y", 3.0)],   # sube el hombro del brazo en jarra
    "cuello": [("z", -5.0)],
    "cabeza": [("y", -8.0), ("z", -6.0), ("x", -3.0)],   # inclinada hacia su derecha, lejos de la lupa
    # Pies: tobillo (posición) y giro de la punta
    "pie_apoyo": {"pos": (-0.095, -0.01, 0.045), "giro": -8.0},
    "pie_libre": {"pos": (0.175, -0.075, 0.06), "giro": 22.0},
    # Brazo en jarra (su derecho): la mano en la cadera, el codo afuera
    "mano_jarra": {"pos": (-0.225, 0.02, 0.55), "polo": (-0.9, 0.25, 0.5), "giro": (0.0, -115.0, 0.0)},
    # Brazo de la lupa (su izquierdo): el puño a la altura del pecho
    "mano_lupa": {"pos": (0.35, -0.12, 0.70), "polo": (0.55, 0.3, 0.25), "giro": (0.0, 0.0, 0.0)},
    # Lupa: inclinación hacia afuera, hacia atrás/adelante y escala
    "lupa": {"afuera": 36.0, "atras": -8.0, "girar": -12.0, "escala": 1.75, "agarre": 0.055},
}


def limpiar():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def importar_caneloso():
    antes = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=MODELO)
    nuevos = [o for o in bpy.data.objects if o not in antes]
    arm = next(o for o in nuevos if o.type == 'ARMATURE')
    mw = arm.matrix_world.copy()
    arm.parent = None
    arm.matrix_world = mw
    arm.name = "Caneloso"
    for o in nuevos:
        if o != arm and o.parent != arm:
            bpy.data.objects.remove(o)
    for pb in arm.pose.bones:
        pb.rotation_mode = 'QUATERNION'
        pb.matrix_basis = Matrix.Identity(4)
    bpy.context.view_layer.update()
    # El importador deja los huesos de 1 m; para el IK, la cola de cada hueso de brazos y piernas va a la
    # cabeza de su hijo (cambiar el reposo no mueve la malla con la pose en cero)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='EDIT')
    eb = arm.data.edit_bones
    for lado in ("Left", "Right"):
        for a, b in (("Arm", "ForeArm"), ("ForeArm", "Hand"), ("UpLeg", "Leg"), ("Leg", "Foot")):
            eb[f"mixamorig_{lado}{a}"].tail = eb[f"mixamorig_{lado}{b}"].head
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.context.view_layer.update()
    # Piso en z = 0: lo más bajo de los zapatos
    dg = bpy.context.evaluated_depsgraph_get()
    piso = min((o.matrix_world @ v.co).z for o in mallas(arm) for v in o.evaluated_get(dg).to_mesh().vertices)
    arm.location.z -= piso
    bpy.context.view_layer.update()
    return arm


def mallas(arm):
    return [o for o in bpy.data.objects if o.type == 'MESH' and o.parent == arm]


def nombrar_mallas(arm):
    "Nombres legibles según lo que es cada malla (por su textura y su lugar)"
    nombres = {"Walking (1)_0": "Piel", "Walking (1)_2": "Cara", "Walking (1)_4": "Hocico",
               "Walking (1)_6": "Copete", "Walking (1)_8": "Pantalon", "Walking (1)_10": "Traje",
               "Walking (1)_12": "Boina"}
    for o in mallas(arm):
        for n in o.active_material.node_tree.nodes:
            if n.type == 'TEX_IMAGE' and n.image and n.image.name in nombres:
                o.name = nombres[n.image.name]
                break


def suavizar(arm):
    "Une los vértices repetidos en las costuras y suaviza por ángulo (sin tocar el modelo del juego)"
    for o in mallas(arm):
        bm = bmesh.new(); bm.from_mesh(o.data)
        # 0.2 mm de mundo (las mallas cuelgan de la armadura, que viene escalada)
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0002 / o.matrix_world.to_scale().x)
        bm.to_mesh(o.data); bm.free()
    bpy.ops.object.select_all(action='DESELECT')
    for o in mallas(arm): o.select_set(True)
    bpy.context.view_layer.objects.active = mallas(arm)[0]
    bpy.ops.object.shade_auto_smooth(angle=math.radians(60))


# ---------- Pose ----------
EJES = {"x": Vector((1, 0, 0)), "y": Vector((0, 1, 0)), "z": Vector((0, 0, 1))}


def girar(arm, hueso, giros):
    "Gira el hueso (y su rama) alrededor de su cabeza, en ejes del personaje"
    pb = arm.pose.bones[hueso]
    for eje, grados in giros:
        m = pb.matrix.copy()
        h = m.translation.copy()
        R = Matrix.Rotation(math.radians(grados), 4, EJES[eje])
        pb.matrix = Matrix.Translation(h) @ R @ Matrix.Translation(-h) @ m
        bpy.context.view_layer.update()


def vacio(nombre, pos, padre=None, tam=0.03):
    e = bpy.data.objects.new(nombre, None)
    e.empty_display_size = tam
    bpy.context.scene.collection.objects.link(e)
    e.matrix_world = Matrix.Translation(pos) if not isinstance(pos, Matrix) else pos
    if padre:
        mw = e.matrix_world.copy(); e.parent = padre; e.matrix_world = mw
    return e


def mundo_hueso(arm, hueso, reposo=True):
    b = arm.data.bones[hueso] if reposo else arm.pose.bones[hueso]
    m = b.matrix_local if reposo else b.matrix
    return arm.matrix_world @ m


def cadena_ik(arm, raiz, medio, fin, objetivo, polo, nombre, giro_mano=None):
    """IK de dos huesos con objetivo y polo; busca el ángulo de polo que deja el codo (o la rodilla) del lado
    del polo. Con giro_mano, la mano (o el pie) copia la orientación del objetivo."""
    cn = arm.pose.bones[medio].constraints.new('IK')
    cn.name = "IK_" + nombre
    cn.target = objetivo
    cn.pole_target = polo
    cn.chain_count = 2
    mejor = None
    for paso in range(-12, 12):
        cn.pole_angle = math.radians(paso * 15)
        bpy.context.view_layer.update()
        codo = (arm.matrix_world @ arm.pose.bones[medio].head)
        d = (codo - polo.matrix_world.translation).length
        if mejor is None or d < mejor[0]: mejor = (d, paso * 15)
    cn.pole_angle = math.radians(mejor[1])
    if giro_mano is not None:
        cr = arm.pose.bones[fin].constraints.new('COPY_ROTATION')
        cr.name = "Rot_" + nombre
        cr.target = giro_mano
    bpy.context.view_layer.update()
    err = (arm.matrix_world @ arm.pose.bones[fin].head - objetivo.matrix_world.translation).length
    print(f"IK {nombre}: polo {mejor[1]}°, error {err:.4f} m")


def posar(arm, raiz):
    P = POSE
    hips = arm.pose.bones["mixamorig_Hips"]
    m = hips.matrix.copy()
    # mover en espacio de armadura: convertir metros de mundo a unidades de la armadura
    esc = arm.matrix_world.to_scale().x
    m.translation += Vector(P["cadera_mover"]) / esc
    hips.matrix = m
    bpy.context.view_layer.update()
    girar(arm, "mixamorig_Hips", P["cadera"])
    girar(arm, "mixamorig_Spine", P["columna"])
    girar(arm, "mixamorig_Spine2", P["columna2"])
    girar(arm, "mixamorig_RightShoulder", P["clavicula_jarra"])
    girar(arm, "mixamorig_Neck", P["cuello"])
    girar(arm, "mixamorig_Head", P["cabeza"])

    def orient(hueso, euler_grados, extra_z=0.0):
        "Orientación de reposo del hueso girada en ejes del personaje"
        r = mundo_hueso(arm, hueso).to_quaternion()
        e = Euler([math.radians(a) for a in euler_grados], 'XYZ').to_quaternion()
        q = Quaternion((0, 0, 1), math.radians(extra_z)) @ e @ r
        return q.to_matrix().to_4x4()

    # Piernas: IK con la rodilla al frente; los pies planos, girados hacia afuera
    for lado, clave, pre in (("R", "pie_apoyo", "Right"), ("L", "pie_libre", "Left")):
        spec = P[clave]
        pos = Vector(spec["pos"])
        obj = vacio(f"IK_Pie{lado}", Matrix.Translation(pos) @ orient(f"mixamorig_{pre}Foot", (0, 0, 0), spec["giro"]), raiz)
        polo = vacio(f"Polo_Rodilla{lado}", pos + Vector((0.03 if lado == "L" else -0.02, -0.6, 0.35)), raiz)
        cadena_ik(arm, None, f"mixamorig_{pre}Leg", f"mixamorig_{pre}Foot", obj, polo, "Pie" + lado, giro_mano=obj)

    # Brazos
    for lado, clave, pre in (("R", "mano_jarra", "Right"), ("L", "mano_lupa", "Left")):
        spec = P[clave]
        pos = Vector(spec["pos"])
        obj = vacio(f"IK_Mano{lado}", Matrix.Translation(pos) @ orient(f"mixamorig_{pre}Hand", spec["giro"]), raiz)
        polo = vacio(f"Polo_Codo{lado}", Vector(spec["polo"]), raiz)
        cadena_ik(arm, None, f"mixamorig_{pre}ForeArm", f"mixamorig_{pre}Hand", obj, polo, "Mano" + lado, giro_mano=obj)


def centro_puno(arm, lado):
    "Centro del puño: promedio de los vértices de la piel que mueve la mano"
    piel = bpy.data.objects["Piel"]
    dg = bpy.context.evaluated_depsgraph_get()
    ev = piel.evaluated_get(dg).to_mesh()
    grupos = {g.index for g in piel.vertex_groups if g.name.startswith(f"mixamorig_{lado}Hand")}
    pts = [piel.matrix_world @ ev.vertices[v.index].co for v in piel.data.vertices
           if sum(g.weight for g in v.groups if g.group in grupos) > 0.5]
    return sum(pts, Vector()) / len(pts)


def poner_lupa(arm, raiz):
    antes = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=LUPA)
    lupa = next(o for o in bpy.data.objects if o not in antes and o.type == 'MESH')
    lupa.name = "Lupa"
    L = POSE["lupa"]
    centro = centro_puno(arm, "Left")
    q = (Quaternion((0, 0, 1), math.radians(L["girar"])) @ Quaternion((1, 0, 0), math.radians(L["atras"]))
         @ Quaternion((0, 1, 0), math.radians(L["afuera"])))
    eje = q @ Vector((0, 0, 1))
    s = L["escala"]
    lupa.matrix_world = Matrix.Translation(centro - eje * L["agarre"] * s) @ q.to_matrix().to_4x4() @ Matrix.Scale(s, 4)
    # La lupa sigue a la mano (Child Of al hueso de la mano izquierda)
    mw = lupa.matrix_world.copy()
    c = lupa.constraints.new('CHILD_OF')
    c.target = arm
    c.subtarget = "mixamorig_LeftHand"
    bpy.context.view_layer.update()
    c.inverse_matrix = (arm.matrix_world @ arm.pose.bones["mixamorig_LeftHand"].matrix).inverted()
    lupa.matrix_world = mw
    bpy.context.view_layer.update()
    print("lupa: puño", tuple(round(v, 3) for v in centro), "vidrio", tuple(round(v, 3) for v in lupa.matrix_world @ Vector((0, 0, 0.2))))
    return lupa


# ---------- Cara ----------
# Los ojos, las cejas y la boca vienen pintados en texturas de 256 y 512 px. Para el key art se borran de una
# copia de esas texturas y se reponen como calcomanías limpias pegadas a la superficie (hijas del hueso de la
# cabeza): así la ceja del lado de la lupa puede subir, la boca puede ser una media sonrisa y los ojos cerrados
# del parpadeo quedan en su propio objeto.
CARA = {
    "ceja_lupa_sube": 0.014, "ceja_lupa_arco": 0.010,     # la ceja del lado de la lupa (su izquierda, +X)
    "ceja_otra_baja": 0.002, "ceja_otra_inclina": -0.006, # la otra, un poco más baja hacia el centro
    "grosor_ceja": 0.013, "grosor_boca": 0.0095, "grosor_cerrado": 0.009,
    "despegue": 0.0022,
}


def imagen_de(obj):
    return next(n.image for n in obj.active_material.node_tree.nodes if n.type == 'TEX_IMAGE' and n.image)


def pixeles(im):
    import numpy as np
    a = np.array(im.pixels[:], dtype=np.float32).reshape(im.size[1], im.size[0], im.channels)
    return a


def borrar_rasgos(obj, caja=None):
    """Copia la textura de la malla sin los trazos oscuros (y el brillo blanco dentro de los ojos), rellenando
    con la piel de alrededor. Devuelve la máscara de lo borrado (en píxeles, origen abajo a la izquierda)."""
    import numpy as np
    im = imagen_de(obj)
    a = pixeles(im)
    lum = a[..., :3] @ np.array([0.299, 0.587, 0.114], dtype=np.float32)
    oscuro = lum < 0.42
    sombra = lum < 0.66   # el halo grisáceo que rodea los trazos
    if caja:
        x0, y0, x1, y1 = caja
        sel = np.zeros_like(oscuro); sel[y0:y1, x0:x1] = True
        oscuro &= sel

    def dilatar(m, n):
        for _ in range(n):
            m = m | np.roll(m, 1, 0) | np.roll(m, -1, 0) | np.roll(m, 1, 1) | np.roll(m, -1, 1)
        return m
    mascara = dilatar(oscuro, 3) | (dilatar(oscuro, 7) & (lum > 0.82)) | (dilatar(oscuro, 12) & sombra)
    mascara = dilatar(mascara, 1)
    # Relleno por difusión desde el borde
    rgb = a[..., :3].copy()
    rgb[mascara] = 0
    peso = (~mascara).astype(np.float32)
    acum, pesos = rgb * peso[..., None], peso.copy()
    for _ in range(400):
        acum2 = sum(np.roll(acum, s, ax) for s in (1, -1) for ax in (0, 1)) / 4
        pesos2 = sum(np.roll(pesos, s, ax) for s in (1, -1) for ax in (0, 1)) / 4
        acum = np.where(mascara[..., None], acum2, rgb)
        pesos = np.where(mascara, pesos2, 1.0)
    relleno = acum / np.maximum(pesos, 1e-4)[..., None]
    a[..., :3] = np.where(mascara[..., None], relleno, a[..., :3])
    nueva = bpy.data.images.new(im.name + "_sin_rasgos", im.size[0], im.size[1], alpha=True)
    nueva.pixels[:] = a.ravel()
    nueva.pack()
    for n in obj.active_material.node_tree.nodes:
        if n.type == 'TEX_IMAGE' and n.image == im:
            n.image = nueva
    return oscuro


def puntos_de_mascara(obj, mascara):
    "Lleva cada píxel marcado de la textura a su punto 3D en la malla posada (por coordenadas baricéntricas)"
    import numpy as np
    dg = bpy.context.evaluated_depsgraph_get()
    ev = obj.evaluated_get(dg).to_mesh()
    ev.calc_loop_triangles()
    uv = ev.uv_layers.active.data
    h, w = mascara.shape
    ys, xs = np.nonzero(mascara)
    pu, pv = (xs + 0.5) / w, (ys + 0.5) / h
    pts = []
    mw = obj.matrix_world
    for tri in ev.loop_triangles:
        (ax, ay), (bx, by), (cx, cy) = (tuple(uv[l].uv) for l in tri.loops)
        det = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy)
        if abs(det) < 1e-12: continue
        dentro = (pu >= min(ax, bx, cx)) & (pu <= max(ax, bx, cx)) & (pv >= min(ay, by, cy)) & (pv <= max(ay, by, cy))
        if not dentro.any(): continue
        u, v = pu[dentro], pv[dentro]
        l1 = ((by - cy) * (u - cx) + (cx - bx) * (v - cy)) / det
        l2 = ((cy - ay) * (u - cx) + (ax - cx) * (v - cy)) / det
        l3 = 1 - l1 - l2
        ok = (l1 >= -0.01) & (l2 >= -0.01) & (l3 >= -0.01)
        A, B, C = (mw @ ev.vertices[i].co for i in tri.vertices)
        for a1, a2, a3 in zip(l1[ok], l2[ok], l3[ok]):
            pts.append(A * a1 + B * a2 + C * a3)
    obj.evaluated_get(dg).to_mesh_clear()
    return pts


def superficie(objs):
    from mathutils.bvhtree import BVHTree
    dg = bpy.context.evaluated_depsgraph_get()
    vs, ps = [], []
    for o in objs:
        ev = o.evaluated_get(dg).to_mesh()
        base = len(vs)
        vs += [o.matrix_world @ v.co for v in ev.vertices]
        ps += [[base + i for i in p.vertices] for p in ev.polygons]
        o.evaluated_get(dg).to_mesh_clear()
    return BVHTree.FromPolygons(vs, ps)


def material_tinta(nombre, color, emision=0.0, rugosidad=0.35):
    m = bpy.data.materials.get(nombre) or bpy.data.materials.new(nombre)
    m.use_nodes = True
    p = m.node_tree.nodes["Principled BSDF"]
    p.inputs["Base Color"].default_value = (*color, 1)
    p.inputs["Roughness"].default_value = rugosidad
    if emision:
        p.inputs["Emission Color"].default_value = (*color, 1)
        p.inputs["Emission Strength"].default_value = emision
    return m


class Plano:
    "Plano tangente a la cara en un punto: u hacia su izquierda (+X), v hacia arriba de la cabeza"
    def __init__(self, bvh, centro, lado, arriba):
        q, n, _, _ = bvh.find_nearest(centro)
        self.bvh, self.c, self.n = bvh, q, n.normalized()
        self.u = (lado - self.n * lado.dot(self.n)).normalized()
        self.v = self.n.cross(self.u).normalized()
        if self.v.dot(arriba) < 0: self.v = -self.v

    def pegar(self, u, v, despegue):
        p = self.c + self.u * u + self.v * v + self.n * 0.01
        q, n, _, _ = self.bvh.find_nearest(p)
        return q + n.normalized() * despegue


def malla_calco(nombre, plano, contornos, despegue, material, arm):
    "Polígonos planos (listas de puntos u, v) pegados a la superficie, hijos del hueso de la cabeza"
    vs, fs = [], []
    for cont in contornos:
        base = len(vs)
        vs += [plano.pegar(u, v, despegue) for u, v in cont]
        if len(cont) == 4 or cont[0] == "cinta":
            pass
        fs.append(list(range(base, base + len(cont))))
    me = bpy.data.meshes.new(nombre)
    me.from_pydata(vs, [], fs)
    me.materials.append(material)
    for p in me.polygons: p.use_smooth = True
    ob = bpy.data.objects.new(nombre, me)
    bpy.context.scene.collection.objects.link(ob)
    pegar_a_cabeza(ob, arm)
    return ob


def malla_cinta(nombre, plano, puntos, grosores, despegue, material, arm):
    "Trazo con grosor variable (cejas, boca, ojos cerrados) como tira de cuadros pegada a la superficie"
    vs, fs = [], []
    for i, ((u, v), g) in enumerate(zip(puntos, grosores)):
        a = Vector(puntos[max(i - 1, 0)]); b = Vector(puntos[min(i + 1, len(puntos) - 1)])
        t = (b - a).normalized()
        nn = Vector((-t.y, t.x)) * g / 2
        vs += [plano.pegar(u + nn.x, v + nn.y, despegue), plano.pegar(u - nn.x, v - nn.y, despegue)]
        if i: fs.append([2 * i - 2, 2 * i - 1, 2 * i + 1, 2 * i])
    me = bpy.data.meshes.new(nombre)
    me.from_pydata(vs, [], fs)
    me.materials.append(material)
    ob = bpy.data.objects.new(nombre, me)
    bpy.context.scene.collection.objects.link(ob)
    pegar_a_cabeza(ob, arm)
    return ob


def pegar_a_cabeza(ob, arm):
    mw = ob.matrix_world.copy()
    ob.parent = arm
    ob.parent_type = 'BONE'
    ob.parent_bone = "mixamorig_Head"
    bpy.context.view_layer.update()
    ob.matrix_world = mw


def elipse(cu, cv, a, b, n=28, giro=0.0):
    return [(cu + a * math.cos(t) * math.cos(giro) - b * math.sin(t) * math.sin(giro),
             cv + a * math.cos(t) * math.sin(giro) + b * math.sin(t) * math.cos(giro))
            for t in (2 * math.pi * i / n for i in range(n))]


def arco(u0, u1, v0, alto, n=14, inclina=0.0):
    "Trazo de u0 a u1; alto > 0 lo arquea hacia arriba (como ceja), < 0 hacia abajo"
    pts = []
    for i in range(n):
        s = i / (n - 1)
        x = u0 + (u1 - u0) * s
        pts.append((x, v0 + alto * (1 - (2 * s - 1) ** 2) + inclina * (2 * s - 1)))
    return pts


def grosor_afilado(n, g):
    return [g * (0.45 + 0.55 * math.sin(math.pi * (i / (n - 1)))) for i in range(n)]


def hacer_cara(arm):
    import numpy as np
    C = CARA
    cara, hocico = bpy.data.objects["Cara"], bpy.data.objects["Hocico"]
    oscuro_cara = borrar_rasgos(cara)
    im_h = imagen_de(hocico)
    # La boca está arriba a la izquierda de la textura del hocico (la nariz es geometría aparte)
    oscuro_boca = borrar_rasgos(hocico, caja=(0, im_h.size[1] - 70, 140, im_h.size[1]))
    bpy.context.view_layer.update()
    # Ojos y cejas solo sobre la cara: con el hocico, los puntos de abajo saltaban a él
    bvh = superficie([cara])
    bvh_boca = superficie([cara, hocico])

    cabeza = arm.matrix_world @ arm.pose.bones["mixamorig_Head"].matrix
    arriba = (cabeza.to_quaternion() @ Vector((0, 1, 0))).normalized()
    pts = puntos_de_mascara(cara, oscuro_cara)
    centro = sum(pts, Vector()) / len(pts)
    lado = (cabeza.to_quaternion() @ Vector((1, 0, 0))).normalized()
    if lado.x < 0: lado = -lado
    plano = Plano(bvh, centro, lado, arriba)

    def uv(p):
        d = p - plano.c
        return d.dot(plano.u), d.dot(plano.v)
    datos = np.array([uv(p) for p in pts])
    tinta = material_tinta("Tinta", (0.01, 0.008, 0.006), rugosidad=0.9)   # cejas y boca: trazo mate
    tinta.node_tree.nodes["Principled BSDF"].inputs["Specular IOR Level"].default_value = 0.0
    ojo_m = material_tinta("OjoNegro", (0.012, 0.009, 0.007), rugosidad=0.7)
    ojo_m.node_tree.nodes["Principled BSDF"].inputs["Specular IOR Level"].default_value = 0.0   # que la farola no lo aclare (el brillo es aparte)
    brillo = material_tinta("BrilloOjo", (1, 1, 1), emision=2.0)
    objetos = {"abiertos": [], "cerrados": [], "cejas": {}}
    for nombre, signo in (("I", 1), ("D", -1)):   # I = su izquierda (+u, lado de la lupa)
        lado_pts = datos[datos[:, 0] * signo > 0]
        vs = np.sort(lado_pts[:, 1])
        corte = vs[np.argmax(np.diff(vs))] if len(vs) > 1 else vs[0]
        ojo = lado_pts[lado_pts[:, 1] <= corte]
        ceja = lado_pts[lado_pts[:, 1] > corte]
        cu, cv = ojo[:, 0].mean(), ojo[:, 1].mean()
        a = (ojo[:, 0].max() - ojo[:, 0].min()) / 2 * 0.95
        b = (ojo[:, 1].max() - ojo[:, 1].min()) / 2 * 0.95
        print(f"ojo {nombre}: centro ({cu:.3f}, {cv:.3f}) radios ({a:.3f}, {b:.3f}); ceja {len(ceja)} px")
        objetos["abiertos"].append(malla_calco(f"Ojo{nombre}", plano, [elipse(cu, cv, a, b)], C["despegue"], ojo_m, arm))
        objetos["abiertos"].append(malla_calco(f"BrilloOjo{nombre}", plano,
                                               [elipse(cu + a * 0.32, cv + b * 0.38, a * 0.30, a * 0.30, 16)],
                                               C["despegue"] * 1.8, brillo, arm))
        # Ojo cerrado: un trazo curvo a la altura del centro del ojo
        pc = arco(cu - a * 1.15, cu + a * 1.15, cv - b * 0.15, -b * 0.35)
        oc = malla_cinta(f"OjoCerrado{nombre}", plano, pc, grosor_afilado(len(pc), C["grosor_cerrado"]), C["despegue"], tinta, arm)
        objetos["cerrados"].append(oc)
        # Ceja: sobre la original; la del lado de la lupa, arriba y más arqueada
        # La original queda casi bajo el flequillo: la nueva va un poco más abajo, sobre la frente
        bv = min(ceja[:, 1].mean() if len(ceja) else 1.0, cv + b + 0.024)
        bu = ceja[:, 0].mean() if len(ceja) else cu
        ancho = max(a * 1.25, (ceja[:, 0].max() - ceja[:, 0].min()) / 2 if len(ceja) else 0)
        if signo > 0:
            pts_c = arco(bu - ancho, bu + ancho, bv + C["ceja_lupa_sube"], C["ceja_lupa_arco"])
        else:
            pts_c = arco(bu - ancho, bu + ancho, bv - C["ceja_otra_baja"], 0.004, inclina=C["ceja_otra_inclina"])
        objetos["cejas"][nombre] = malla_cinta(f"Ceja{nombre}", plano, pts_c, grosor_afilado(len(pts_c), C["grosor_ceja"]),
                                               C["despegue"], tinta, arm)
    # Boca: media sonrisa, la comisura del lado de la lupa más arriba
    pb_ = puntos_de_mascara(hocico, oscuro_boca)
    cb = sum(pb_, Vector()) / len(pb_) if pb_ else centro - plano.v * 0.12
    pm = Plano(bvh_boca, cb, lado, arriba)
    boca = []
    for i in range(16):
        s = i / 15
        x = -0.034 + 0.072 * s
        boca.append((x, -0.004 + 0.011 * s ** 2.2 - 0.004 * math.sin(math.pi * s)))
    objetos["boca"] = malla_cinta("Boca", pm, boca, grosor_afilado(len(boca), C["grosor_boca"]), C["despegue"], tinta, arm)
    for o in objetos["cerrados"]:
        o.hide_render = True
        o.hide_viewport = True
    return objetos


def girar_todo(raiz):
    raiz.rotation_euler = (0, 0, math.radians(POSE["giro_cuerpo"]))
    bpy.context.view_layer.update()


def construir():
    limpiar()
    esc = bpy.context.scene
    raiz = bpy.data.objects.new("Raiz", None)
    esc.collection.objects.link(raiz)
    arm = importar_caneloso()
    nombrar_mallas(arm)
    # La textura de la boina es de 4096 px: a 2048 se ve igual en el lienzo y ahorra memoria
    for im in bpy.data.images:
        if im.size[0] > 2048: im.scale(2048, 2048)
    suavizar(arm)
    arm.parent = raiz
    posar(arm, raiz)
    lupa = poner_lupa(arm, raiz)
    hacer_cara(arm)
    girar_todo(raiz)
    return arm, lupa


# ---------- Cámaras y render ----------
def camara(nombre, desde, hacia, lente=85):
    cam = bpy.data.objects.new(nombre, bpy.data.cameras.new(nombre))
    bpy.context.scene.collection.objects.link(cam)
    cam.data.lens = lente
    cam.location = desde
    cam.rotation_euler = (hacia - desde).to_track_quat('-Z', 'Y').to_euler()
    return cam


def vista(salida):
    esc = bpy.context.scene
    esc.render.engine = 'BLENDER_WORKBENCH'
    esc.display.shading.light = 'STUDIO'
    esc.display.shading.color_type = 'TEXTURE'
    esc.display.shading.show_shadows = True
    esc.render.film_transparent = False
    w = bpy.data.worlds.new("Gris"); esc.world = w; w.color = (0.25, 0.26, 0.28)
    sol = bpy.data.objects.new("Sol", bpy.data.lights.new("Sol", 'SUN')); esc.collection.objects.link(sol)
    sol.rotation_euler = (math.radians(40), math.radians(-20), math.radians(-30)); sol.data.energy = 3.5
    tomas = {
        "frente": (Vector((0, -9, 1.0)), Vector((0, 0, 0.92)), 135, 800, 1000),
        "lado": (Vector((9, 0, 1.0)), Vector((0, 0, 0.92)), 135, 800, 1000),
        "arriba": (Vector((0, -2, 9)), Vector((0, 0, 0.9)), 70, 800, 800),
        "cara": (Vector((0.4, -5, 1.25)), Vector((0.02, 0, 1.22)), 135, 900, 700),
        "manos": (Vector((0.1, -4, 0.75)), Vector((0.05, 0, 0.62)), 85, 900, 700),
    }
    for nombre, (desde, hacia, lente, w_, h_) in tomas.items():
        cam = camara("Cam_" + nombre, desde, hacia, lente)
        esc.camera = cam
        esc.render.resolution_x, esc.render.resolution_y = w_, h_
        esc.render.filepath = f"{salida}_{nombre}.png"
        bpy.ops.render.render(write_still=True)


# ---------- Luces y render en Cycles ----------
def luz_area(nombre, desde, hacia, color, energia, tam):
    l = bpy.data.lights.new(nombre, 'AREA')
    l.color, l.energy, l.size = color, energia, tam
    l.shape = 'DISK'
    ob = bpy.data.objects.new(nombre, l)
    bpy.context.scene.collection.objects.link(ob)
    ob.location = desde
    ob.rotation_euler = (Vector(hacia) - Vector(desde)).to_track_quat('-Z', 'Y').to_euler()
    return ob


LUCES = {
    # Farola arriba a la izquierda: cálida y suave; alta pero no tanto como para que la boina tape los ojos
    "Clave_Farola": ((-2.3, -2.6, 2.9), (0.0, 0.0, 1.15), (1.0, 0.74, 0.47), 230.0, 1.6),
    # Relleno bajo y frontal: levanta la sombra de la boina sobre los ojos
    "Relleno_Bajo": ((0.4, -3.2, 0.7), (0.0, 0.0, 1.2), (1.0, 0.88, 0.75), 38.0, 1.4),
    # Contorno frío desde la puerta (atrás): borde de la boina, orejas, hombros y aro de la lupa
    "Contorno_Izq": ((-1.8, 1.7, 1.5), (0.0, 0.0, 1.1), (0.62, 0.76, 1.0), 1700.0, 0.8),
    "Contorno_Der": ((1.9, 1.6, 1.2), (0.15, 0.0, 0.95), (0.62, 0.76, 1.0), 1700.0, 0.8),
    # Relleno tenue del lado opuesto a la farola
    "Relleno_Opuesto": ((2.7, -1.4, 1.6), (0.0, 0.0, 1.0), (0.72, 0.8, 0.95), 22.0, 1.5),
}


def luces():
    esc = bpy.context.scene
    w = bpy.data.worlds.new("Callejon"); esc.world = w
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.018, 0.022, 0.03, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 1.0
    for nombre, (desde, hacia, color, energia, tam) in LUCES.items():
        luz_area(nombre, desde, hacia, color, energia, tam)


def vidrio_lupa(lupa):
    "Vidrio real: transmisión, refracción leve y especular; el aro de latón y el mango conservan su textura"
    for i, m in enumerate(lupa.data.materials):
        if m and "lense" in m.name:
            v = bpy.data.materials.new("Vidrio_Lupa")
            v.use_nodes = True
            p = v.node_tree.nodes["Principled BSDF"]
            p.inputs["Base Color"].default_value = (0.94, 0.97, 1.0, 1)
            p.inputs["Transmission Weight"].default_value = 1.0
            p.inputs["IOR"].default_value = 1.18
            p.inputs["Roughness"].default_value = 0.02
            p.inputs["Specular IOR Level"].default_value = 0.8
            lupa.data.materials[i] = v


def ajustar_camara(cam, objs, margen_abajo=0.004, pies_x=0.49):
    "Lente y desplazamiento para que el personaje llene el alto y los pies queden donde estaban"
    from bpy_extras.object_utils import world_to_camera_view
    esc = bpy.context.scene
    dg = bpy.context.evaluated_depsgraph_get()
    pts, pies = [], []
    for o in objs:
        ev = o.evaluated_get(dg).to_mesh()
        for v in ev.vertices:
            w = o.matrix_world @ v.co
            pts.append(w)
            if w.z < 0.03: pies.append(w)
        o.evaluated_get(dg).to_mesh_clear()
    for _ in range(4):
        bpy.context.view_layer.update()
        ps = [world_to_camera_view(esc, cam, p) for p in pts]
        alto = max(p.y for p in ps) - min(p.y for p in ps)
        cam.data.lens *= (1.0 - margen_abajo - 0.006) / alto
    bpy.context.view_layer.update()
    ps = [world_to_camera_view(esc, cam, p) for p in pts]
    fp = [world_to_camera_view(esc, cam, p) for p in pies]
    fx = (min(p.x for p in fp) + max(p.x for p in fp)) / 2
    aspecto = esc.render.resolution_x / esc.render.resolution_y
    # El desplazamiento se mide en el lado mayor del cuadro (aquí, el alto)
    cam.data.shift_y += min(p.y for p in ps) - margen_abajo
    cam.data.shift_x += (fx - pies_x) * aspecto
    bpy.context.view_layer.update()
    ps = [world_to_camera_view(esc, cam, p) for p in pts]
    print("encuadre: x %.3f..%.3f  y %.3f..%.3f  lente %.1f" % (min(p.x for p in ps), max(p.x for p in ps),
          min(p.y for p in ps), max(p.y for p in ps), cam.data.lens))


def cycles(muestras, escala):
    esc = bpy.context.scene
    esc.render.engine = 'CYCLES'
    prefs = bpy.context.preferences.addons["cycles"].preferences
    # CANELOSO_CPU=1 renderiza en CPU (si la GPU se queda sin memoria)
    for tipo in (() if os.environ.get("CANELOSO_CPU") else ("OPTIX", "CUDA", "HIP", "ONEAPI")):
        try:
            prefs.compute_device_type = tipo
            prefs.get_devices()
            if any(d.type == tipo for d in prefs.devices):
                for d in prefs.devices: d.use = True
                esc.cycles.device = 'GPU'
                print("Cycles en", tipo)
                break
        except TypeError:
            continue
    esc.cycles.samples = muestras
    # Por mosaicos: el PC tiene poca memoria libre
    esc.cycles.use_auto_tile = True
    esc.cycles.tile_size = 512
    esc.cycles.use_denoising = True
    # OIDN se queda sin memoria en este PC (en GPU y en CPU); el de OptiX pide mucho menos
    esc.cycles.denoiser = 'OPTIX' if esc.cycles.device == 'GPU' else 'OPENIMAGEDENOISE'
    esc.cycles.use_fast_gi = False
    esc.render.film_transparent = True
    esc.cycles.film_transparent_glass = True
    esc.cycles.filter_width = 1.2
    esc.view_settings.view_transform = 'AgX'
    try:
        esc.view_settings.look = 'AgX - Medium High Contrast'
    except TypeError:
        print("looks:", [l.name for l in bpy.types.ColorManagedViewSettings.bl_rna.properties["look"].enum_items])
    esc.render.resolution_x, esc.render.resolution_y = ANCHO, ALTO
    esc.render.resolution_percentage = escala
    esc.render.image_settings.file_format = 'PNG'
    esc.render.image_settings.color_mode = 'RGBA'
    esc.render.image_settings.color_depth = '16'


def camara_menu(arm, lupa):
    esc = bpy.context.scene
    esc.render.resolution_x, esc.render.resolution_y = ANCHO, ALTO
    cam = camara("Camara_Menu", Vector((0.0, -7.5, 0.72)), Vector((0.0, 0.0, 0.9)), 85)
    esc.camera = cam
    objs = mallas(arm) + [lupa] + [o for o in bpy.data.objects if o.parent == arm and o.type == 'MESH' and not o.hide_render]
    ajustar_camara(cam, list(dict.fromkeys(objs)))
    return cam


# ---------- Capas para la animación recortable ----------
SALIDA_CAPAS = os.path.join(GAME, "Assets", "Menu", "Caneloso")
ORDEN_CAPAS = ["cuerpo", "oreja_lejana", "cabeza", "ojos_abiertos", "ojos_cerrados", "ceja_arriba",
               "oreja_cercana", "brazo_lupa", "mascara_vidrio"]
PADRES = {"ojos_abiertos": "cabeza", "ojos_cerrados": "cabeza", "ceja_arriba": "cabeza", "mascara_vidrio": "brazo_lupa"}
CORTE_OREJA = 0.36     # metros desde el centro de la cabeza: más afuera es oreja


def separar(obj, nombre, quiere_cara):
    "Copia de la malla con solo las caras para las que quiere_cara(polígono) es cierto (misma armadura)"
    nuevo = obj.copy()
    nuevo.data = obj.data.copy()
    nuevo.name = nombre
    for c in obj.users_collection: c.objects.link(nuevo)
    bm = bmesh.new(); bm.from_mesh(nuevo.data)
    bm.faces.ensure_lookup_table()
    fuera = [f for f in bm.faces if not quiere_cara(obj.data.polygons[f.index])]
    bmesh.ops.delete(bm, geom=fuera, context='FACES')
    bm.to_mesh(nuevo.data); bm.free()
    return nuevo


def grupo_dominante(obj, poly):
    pesos = {}
    for vi in poly.vertices:
        for g in obj.data.vertices[vi].groups:
            n = obj.vertex_groups[g.group].name.replace("mixamorig_", "")
            pesos[n] = pesos.get(n, 0) + g.weight
    return max(pesos, key=pesos.get) if pesos else ""


def partir_mallas(arm):
    """Divide la piel y el traje en las piezas de las capas. Devuelve {capa: [objetos]}; la piel y el traje
    originales quedan fuera de cámara (siguen dando sombra)."""
    piel, traje = bpy.data.objects["Piel"], bpy.data.objects["Traje"]
    cab = arm.matrix_world @ arm.pose.bones["mixamorig_Head"].matrix
    cab_inv = cab.inverted()
    esc_cab = cab.to_scale().x
    # Lado de la oreja lejana (+1: su izquierda) según la cámara
    camara = bpy.context.scene.camera.matrix_world.translation
    lejos = 1 if (cab @ Vector((0.45 / esc_cab, 0, 0)) - camara).length > (cab @ Vector((-0.45 / esc_cab, 0, 0)) - camara).length else -1
    # Cortes rectos (no por caras enteras, que dejan dientes): la oreja lejana trae carne extra que queda bajo
    # la cabeza; la cabeza trae carne extra bajo la oreja cercana
    cortes = {"cab_lejos": CORTE_OREJA, "cab_cerca": CORTE_OREJA + 0.06, "oreja_lejos": CORTE_OREJA - 0.04, "oreja_cerca": CORTE_OREJA}
    reposo = arm.matrix_world @ arm.data.bones["mixamorig_Head"].matrix_local
    inv = piel.matrix_world.inverted()
    bm = bmesh.new(); bm.from_mesh(piel.data)
    for d in sorted(set(cortes.values())):
        for signo in (1, -1):
            co = inv @ (reposo @ Vector((signo * d / esc_cab, 0, 0)))
            no = (inv.to_3x3() @ (reposo.to_3x3() @ Vector((1, 0, 0)))).normalized()
            geom = bm.verts[:] + bm.edges[:] + bm.faces[:]
            bmesh.ops.bisect_plane(bm, geom=geom, plane_co=co, plane_no=no)
    bm.to_mesh(piel.data); bm.free()
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    ev = piel.evaluated_get(dg).to_mesh()
    centros = [piel.matrix_world @ sum((ev.vertices[i].co for i in p.vertices), Vector()) / len(p.vertices) for p in piel.data.polygons]
    piel.evaluated_get(dg).to_mesh_clear()

    def de_cabeza(p): return grupo_dominante(piel, p) in ("Head", "Neck")

    def lateral(p):
        # x del centro de la cara en ejes de la cabeza, en metros
        return (cab_inv @ centros[p.index]).x * esc_cab

    def es_mano(p, lado): return grupo_dominante(piel, p).startswith(f"{lado}Hand")
    def x(p): return lateral(p) * lejos   # > 0 hacia la oreja lejana
    piezas = {
        "Piel_Cabeza": separar(piel, "Piel_Cabeza", lambda p: de_cabeza(p) and -cortes["cab_cerca"] <= x(p) <= cortes["cab_lejos"]),
        "Oreja_Lejana": separar(piel, "Oreja_Lejana", lambda p: de_cabeza(p) and x(p) >= cortes["oreja_lejos"]),
        "Oreja_Cercana": separar(piel, "Oreja_Cercana", lambda p: de_cabeza(p) and x(p) <= -cortes["oreja_cerca"]),
        "Mano_Lupa": separar(piel, "Mano_Lupa", lambda p: es_mano(p, "Left") or grupo_dominante(piel, p).startswith("LeftForeArm")),
        "Piel_Cuerpo": separar(piel, "Piel_Cuerpo", lambda p: not de_cabeza(p) and not es_mano(p, "Left")
                               and not grupo_dominante(piel, p).startswith("LeftForeArm")),
    }
    def de_brazo(p): return grupo_dominante(traje, p).startswith(("LeftArm", "LeftForeArm", "LeftHand"))
    piezas["Manga_Lupa"] = separar(traje, "Manga_Lupa", de_brazo)
    piezas["Traje_Cuerpo"] = separar(traje, "Traje_Cuerpo", lambda p: not de_brazo(p))
    for o in (piel, traje):
        o.visible_camera = False
    bpy.context.view_layer.update()
    lejana, cercana = piezas["Oreja_Lejana"], piezas["Oreja_Cercana"]
    for n, o in piezas.items():
        print("pieza", n, len(o.data.polygons), "caras")
    print("oreja lejana: su", "izquierda" if lejos > 0 else "derecha")
    O = bpy.data.objects
    capas = {
        "cuerpo": [O["Pantalon"], piezas["Traje_Cuerpo"], piezas["Piel_Cuerpo"]],
        "oreja_lejana": [lejana],
        "cabeza": [piezas["Piel_Cabeza"], O["Cara"], O["Hocico"], O["Copete"], O["Boina"], O["Boca"]],
        "ojos_abiertos": [O["OjoI"], O["OjoD"], O["BrilloOjoI"], O["BrilloOjoD"], O["CejaD"]],
        "ojos_cerrados": [O["OjoCerradoI"], O["OjoCerradoD"], O["CejaD"]],
        "ceja_arriba": [O["CejaI"]],
        "oreja_cercana": [cercana],
        "brazo_lupa": [piezas["Mano_Lupa"], piezas["Manga_Lupa"], O["Lupa"]],
        "mascara_vidrio": [O["Lupa"]],
    }
    return capas


def punto_px(p):
    from bpy_extras.object_utils import world_to_camera_view
    esc = bpy.context.scene
    v = world_to_camera_view(esc, esc.camera, p)
    return [round(v.x * ANCHO, 1), round((1 - v.y) * ALTO, 1)]


def pivotes(arm, capas):
    def hueso(n): return arm.matrix_world @ arm.pose.bones[n].head
    pies = (hueso("mixamorig_LeftFoot") + hueso("mixamorig_RightFoot")) / 2
    pies.z = 0.0

    def raiz_oreja(o):
        dg = bpy.context.evaluated_depsgraph_get()
        e = o.evaluated_get(dg).to_mesh()
        ws = sorted((o.matrix_world @ v.co for v in e.vertices), key=lambda w: -w.z)
        o.evaluated_get(dg).to_mesh_clear()
        arriba = ws[:max(1, len(ws) // 7)]
        return sum(arriba, Vector()) / len(arriba)
    cuello = hueso("mixamorig_Neck")
    return {
        "pies": punto_px(pies),
        "cadera": punto_px(hueso("mixamorig_Hips")),
        "cuerpo": punto_px(hueso("mixamorig_Hips")),   # respira con pivote en la cadera
        "oreja_lejana": punto_px(raiz_oreja(capas["oreja_lejana"][0])),
        "oreja_cercana": punto_px(raiz_oreja(capas["oreja_cercana"][0])),
        "cabeza": punto_px(cuello), "ojos_abiertos": punto_px(cuello), "ojos_cerrados": punto_px(cuello),
        "ceja_arriba": punto_px(cuello),
        "brazo_lupa": punto_px(hueso("mixamorig_LeftArm")), "mascara_vidrio": punto_px(hueso("mixamorig_LeftArm")),
    }


def recortar(ruta):
    "Recorta el PNG a su caja útil (alfa > 0) con 2 px de margen; devuelve offset y tamaño (origen arriba)"
    import numpy as np
    im = bpy.data.images.load(ruta)
    w, h = im.size
    a = np.array(im.pixels[:], dtype=np.float32).reshape(h, w, 4)
    ys, xs = np.nonzero(a[..., 3] > 1.0 / 255)
    x0, x1 = max(0, xs.min() - 2), min(w, xs.max() + 3)
    y0, y1 = max(0, ys.min() - 2), min(h, ys.max() + 3)
    c = a[y0:y1, x0:x1]
    nueva = bpy.data.images.new(os.path.basename(ruta), x1 - x0, y1 - y0, alpha=True)
    nueva.pixels[:] = c.ravel()
    nueva.filepath_raw = ruta
    nueva.file_format = 'PNG'
    nueva.save()
    bpy.data.images.remove(im); bpy.data.images.remove(nueva)
    # filas de Blender van de abajo hacia arriba
    return [int(x0), int(h - y1)], [int(x1 - x0), int(y1 - y0)]


def render_capas(arm, lupa, muestras):
    import json
    esc = bpy.context.scene
    capas = partir_mallas(arm)
    piv = pivotes(arm, capas)
    cycles(muestras, 100)
    esc.render.image_settings.color_depth = '8'
    todos = set(o for lista in capas.values() for o in lista)
    os.makedirs(SALIDA_CAPAS, exist_ok=True)
    vidrio = next(m for m in lupa.data.materials if m.name == "Vidrio_Lupa")
    blanco = material_tinta("MascaraBlanca", (1, 1, 1), emision=1.0)
    blanco.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0, 0, 0, 1)
    datos = {"lienzo": [ANCHO, ALTO], "pies": piv["pies"], "cadera": piv["cadera"], "capas": []}
    for orden, nombre in enumerate(ORDEN_CAPAS):
        for o in todos: o.visible_camera = o in capas[nombre]
        for o in capas["ojos_cerrados"]:
            o.hide_render = o not in capas[nombre] and o.name.startswith("OjoCerrado")
        if nombre == "mascara_vidrio":
            # Solo el vidrio, blanco puro; el resto de la lupa no se ve
            mats = list(lupa.data.materials)
            negro_t = bpy.data.materials.new("Invisible"); negro_t.use_nodes = True
            nt = negro_t.node_tree; nt.nodes.clear()
            out = nt.nodes.new("ShaderNodeOutputMaterial"); tr = nt.nodes.new("ShaderNodeBsdfTransparent")
            nt.links.new(tr.outputs[0], out.inputs[0])
            for i, m in enumerate(mats): lupa.data.materials[i] = blanco if m == vidrio else negro_t
            esc.cycles.samples = 16
        ruta = os.path.join(SALIDA_CAPAS, nombre + ".png")
        esc.render.filepath = ruta
        bpy.ops.render.render(write_still=True)
        if nombre == "mascara_vidrio":
            for i, m in enumerate(mats): lupa.data.materials[i] = m
            esc.cycles.samples = muestras
        offset, tam = recortar(ruta)
        datos["capas"].append({"nombre": nombre, "archivo": nombre + ".png", "offset": offset, "tam": tam,
                               "pivote": piv[nombre], "padre": PADRES.get(nombre), "orden": orden})
        print("capa", nombre, offset, tam)
    # Imagen completa (respaldo y retrato): todo visible menos los ojos cerrados
    # (con la piel y el traje enteros: las piezas se traslapan)
    for o in todos: o.visible_camera = not o.name.startswith(("Piel_", "Oreja_", "Mano_Lupa", "Manga_", "Traje_"))
    for o in (bpy.data.objects["Piel"], bpy.data.objects["Traje"]): o.visible_camera = True
    for o in capas["ojos_cerrados"]:
        if o.name.startswith("OjoCerrado"): o.hide_render = True
    esc.render.filepath = os.path.join(GAME, "Assets", "Menu", "caneloso.png")
    bpy.ops.render.render(write_still=True)
    with open(os.path.join(SALIDA_CAPAS, "capas.json"), "w", encoding="utf-8") as f:
        json.dump(datos, f, indent=1, ensure_ascii=False)
    print("capas.json listo")


def retrato(muestras):
    "Retrato del juego (HUD, diario, diálogos): busto con la misma luz, 512×512 sobre fondo oscuro"
    esc = bpy.context.scene
    cycles(muestras, 100)
    esc.render.resolution_x = esc.render.resolution_y = 512
    esc.render.film_transparent = False
    esc.render.image_settings.color_mode = 'RGB'
    esc.render.image_settings.color_depth = '8'
    # Del pecho a la boina, con la cabeza al centro
    cam = camara("Camara_Retrato", Vector((0.15, -7.5, 1.05)), Vector((-0.02, 0.0, 1.2)), 205)
    esc.camera = cam
    esc.render.filepath = os.path.join(GAME, "Assets", "Retratos", "caneloso.png")
    bpy.ops.render.render(write_still=True)


def boina_mate():
    "La boina es de fieltro: casi sin brillo (con el de fábrica la farola la deja blanquecina)"
    for n in bpy.data.objects["Boina"].active_material.node_tree.nodes:
        if n.type == 'BSDF_PRINCIPLED':
            n.inputs["Roughness"].default_value = 0.9
            n.inputs["Specular IOR Level"].default_value = 0.15
            if n.inputs["Roughness"].links:
                bpy.data.objects["Boina"].active_material.node_tree.links.remove(n.inputs["Roughness"].links[0])


if __name__ == "__main__":
    arm, lupa = construir()
    boina_mate()
    if MODO == "vista":
        vista(SALIDA)
    else:
        vidrio_lupa(lupa)
        luces()
        camara_menu(arm, lupa)
        if MODO == "prueba":
            cycles(96, 50)
            esc = bpy.context.scene
            esc.render.filepath = SALIDA + "_prueba.png"
            bpy.ops.render.render(write_still=True)
        if MODO == "capas":
            render_capas(arm, lupa, 160)
        if MODO == "retrato":
            retrato(160)
        if MODO in ("escena", "capas"):
            bpy.ops.file.pack_all()
            bpy.ops.wm.save_as_mainfile(filepath=os.path.join(BASE, "caneloso_menu.blend"), compress=True)
