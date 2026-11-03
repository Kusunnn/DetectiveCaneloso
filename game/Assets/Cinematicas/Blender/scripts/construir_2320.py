# La escena de las 23:20 del Caso 1 (Tony golpea a Porky) en cuadros congelados, con el método de la cinta del Caso 0:
# una pose quieta por cuadro, escrita a mano (FK en los ejes del personaje e IK de dos huesos donde hay contacto).
#
#   blender -b --python construir_2320.py                 -> Caso1/reconstruccion_2320.glb + reporte_choques_2320.json
#   blender -b --python construir_2320.py -- --vista <carpeta>     (renders de revisión, sin exportar)
#
# Antes (solo si cambian los modelos), desde Godot con Herramientas/exportar_modelo_blender.gd y luego limpiar_glb.py:
#   res://Assets/Models/Tony/Tony.tscn -> caso1_tony.glb ; res://Assets/Pistas/Modelos/remo_sangre/remo_sangre.tscn -> caso1_remo.glb
#
# Solo salen Tony, Porky y dos vacíos que marcan dónde van el remo y la linterna (Anim_Remo, Anim_Linterna): el lago, el
# muelle, las luces y la cámara son los del juego (LibretosCaso1.Reconstruccion). Coordenadas de Godot en los datos.
# La animación «Momentos» trae el cuadro k en el segundo k.
import sys, os, math, json
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
from mathutils import Vector, Matrix, Quaternion
from mathutils.bvhtree import BVHTree
from comun import BASE, GAME, g2b
import retarget as rt

ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
VISTA = ARGS[ARGS.index("--vista") + 1] if "--vista" in ARGS else None
SALIDA = os.path.join(GAME, "Assets", "Cinematicas", "Caso1")
ESCALA = {"Tony": 0.6503, "Porky": 5.4}       # la misma escala que tienen en el juego
FPS = 30
PISO = 0.35                                    # la cara de arriba del tablado (Muelle/ColTablado)
X_MUELLE = 8.0                                 # el centro del muelle
PUNTA = -34.0                                  # donde se acaba


def gv(v):
    return g2b(v[0], v[1], v[2])


def cuadro_de(k):
    return 1 + k * FPS


# ---------- Los modelos ----------
bpy.ops.wm.read_factory_settings(use_empty=True)
esc = bpy.context.scene


def importar(ruta, nombre):
    "Importa y deja solo la armadura y sus mallas, sin padre y con su escala del juego"
    antes = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=ruta)
    nuevos = [o for o in bpy.data.objects if o not in antes]
    arm = next(o for o in nuevos if o.type == 'ARMATURE')
    bpy.context.view_layer.update()
    mw = arm.matrix_world.copy()
    arm.parent = None
    arm.matrix_world = mw
    arm.name = nombre
    for o in nuevos:
        if o != arm and o.parent != arm: bpy.data.objects.remove(o)
    for o in list(arm.children):
        if o.type != 'MESH' or o.name.startswith("Icosphere"): bpy.data.objects.remove(o)
    # Sus animaciones importadas traen escala y pisarían las poses
    if arm.animation_data: arm.animation_data_clear()
    arm.rotation_mode = 'QUATERNION'
    arm.scale = mw.to_scale() * ESCALA[nombre]
    for pb in arm.pose.bones:
        pb.rotation_mode = 'QUATERNION'
        pb.matrix_basis = Matrix.Identity(4)
    for acc in list(bpy.data.actions): bpy.data.actions.remove(acc)
    return arm


AT = importar(os.path.join(BASE, "caso1_tony.glb"), "Tony")
AP = importar(os.path.join(GAME, "Assets", "Models", "Personajes", "porky.glb"), "Porky")
bpy.context.view_layer.update()
ARM = {"Tony": AT, "Porky": AP}
ESQ = {n: rt.Esqueleto(a) for n, a in ARM.items()}
# En reposo los dos miran a +Z de Godot
GIRO_IMPORT = {n: a.matrix_world.to_quaternion() for n, a in ARM.items()}
CADENAS = {
    "Porky": {"ManoI": ("Arm_1_L", "Arm_2_L", "Wrist_L"), "ManoD": ("Arm_1_R", "Arm_2_R", "Wrist_R"),
              "PieI": ("Leg_1_L", "Leg_2_L", "Ankle_L"), "PieD": ("Leg_1_R", "Leg_2_R", "Ankle_R")},
    "Tony": {"ManoI": ("mixamorig_LeftArm", "mixamorig_LeftForeArm", "mixamorig_LeftHand"),
             "ManoD": ("mixamorig_RightArm", "mixamorig_RightForeArm", "mixamorig_RightHand"),
             "PieI": ("mixamorig_LeftUpLeg", "mixamorig_LeftLeg", "mixamorig_LeftFoot"),
             "PieD": ("mixamorig_RightUpLeg", "mixamorig_RightLeg", "mixamorig_RightFoot")},
}

# El remo como sólido, solo para revisar choques (en el juego es la pieza de la pista, sin sangre)
antes = set(bpy.data.objects)
bpy.ops.import_scene.gltf(filepath=os.path.join(BASE, "caso1_remo.glb"))
for o in [o for o in bpy.data.objects if o not in antes]:
    if o.name.startswith(("Sangre", "Detalle")): bpy.data.objects.remove(o)
bpy.context.view_layer.update()
REMO_MALLAS = [o for o in bpy.data.objects if o not in antes and o.type == 'MESH']
REMO_RAIZ = next(o for o in bpy.data.objects if o not in antes and o.parent is None)
# El remo está acostado a lo largo de Y (de Blender): el puño en y = -1.26 y la punta de la pala en y = -0.01
REMO_PUNO, REMO_PUNTA = Vector((0, -1.26, 0.025)), Vector((0, -0.01, 0.025))

ANIM_REMO = bpy.data.objects.new("Anim_Remo", None)
ANIM_LINTERNA = bpy.data.objects.new("Anim_Linterna", None)
for o in (ANIM_REMO, ANIM_LINTERNA):
    esc.collection.objects.link(o)
    o.rotation_mode = 'QUATERNION'

# El muelle como sólido: las tablas donde se paran
bpy.ops.mesh.primitive_cube_add(size=1, location=gv((X_MUELLE, PISO - 0.1, -20.75)))
TABLADO = bpy.context.object
TABLADO.name = "Tablado"
TABLADO.scale = (2.4, 26.5, 0.2)
bpy.context.view_layer.update()


# ---------- Pose ----------
def ejes(esq):
    izq, arriba, frente = esq.ejes()
    return {"izq": izq, "arriba": arriba, "frente": frente}


def fk(esq, pose, giros):
    "giros: {hueso: [(eje, grados), ...]}; cada giro en el marco del padre ya girado"
    E = ejes(esq)
    acum = {}
    giros = {esq.mapa.get(h, h): g for h, g in giros.items()}
    for b in esq.orden:
        qp = acum[b.parent.name] if b.parent else Quaternion()
        ql = Quaternion()
        for eje, grados in giros.get(b.name, []):
            ql = ql @ Quaternion(E[eje], math.radians(grados))
        if b.name in giros:
            rt.girar_rama(esq, pose, b.name, qp @ ql @ qp.inverted())
        acum[b.name] = qp @ ql


def matriz_personaje(nombre, pos, giro):
    arm = ARM[nombre]
    q = Quaternion((0, 0, 1), math.radians(giro))
    return Matrix.Translation(gv(pos)) @ (q @ GIRO_IMPORT[nombre]).to_matrix().to_4x4() @ Matrix.Diagonal((*arm.scale, 1))


ERRORES_IK = []


def ik(esq, pose, mundo, sup, med, fin, objetivo, polo, etiqueta):
    "IK de dos huesos: la cabeza de `fin` va al objetivo y el codo o la rodilla apunta al polo"
    inv = mundo.inverted()
    obj, pl = inv @ objetivo, inv @ polo
    a = pose[sup].translation.copy()
    b, c = pose[med].translation, pose[fin].translation
    l1, l2 = (b - a).length, (c - b).length
    d = max(1e-5, min((obj - a).length, (l1 + l2) * 0.999))
    n = (a - b).cross(c - b)
    if n.length < 1e-8: n = (a - b).cross(pl - b)
    n.normalize()
    actual = (a - b).angle(c - b)
    quiere = math.acos(max(-1.0, min(1.0, (l1 * l1 + l2 * l2 - d * d) / (2 * l1 * l2))))
    rt.girar_rama(esq, pose, med, Quaternion(n, quiere - actual))
    c = pose[fin].translation
    rt.girar_rama(esq, pose, sup, (c - a).rotation_difference(obj - a))
    k = (obj - a).normalized()
    e = pose[med].translation - a
    e_p = e - k * e.dot(k)
    p_p = (pl - a) - k * (pl - a).dot(k)
    if e_p.length > 1e-6 and p_p.length > 1e-6:
        ang = e_p.angle(p_p)
        if e_p.cross(p_p).dot(k) < 0: ang = -ang
        rt.girar_rama(esq, pose, sup, Quaternion(k, ang))
    err = (mundo @ pose[fin].translation - objetivo).length
    ERRORES_IK.append((etiqueta, round(err, 4)))
    return err


def local(pos, giro, x, y, z):
    "Punto de Godot a partir de la posición de un personaje: x a su izquierda, y arriba, z hacia su frente"
    g = math.radians(giro)
    return (pos[0] + x * math.cos(g) + z * math.sin(g), pos[1] + y, pos[2] - x * math.sin(g) + z * math.cos(g))


def posar(nombre, spec, k):
    esq, arm = ESQ[nombre], ARM[nombre]
    pose = {n: m.copy() for n, m in esq.reposo.items()}
    fk(esq, pose, spec.get("fk", {}))
    mundo = matriz_personaje(nombre, spec["pos"], spec.get("giro", 0))
    for cadena, (objetivo, polo) in spec.get("ik", {}).items():
        sup, med, fin = CADENAS[nombre][cadena]
        ik(esq, pose, mundo, sup, med, fin, gv(objetivo), gv(polo), f"{k}:{nombre}:{cadena}")
    if spec.get("fk_final"): fk(esq, pose, spec["fk_final"])
    arm.location = mundo.translation
    arm.rotation_quaternion = mundo.to_quaternion()
    rt.aplicar(esq, pose)
    bpy.context.view_layer.update()


def mallas(arm):
    return [o for o in arm.children if o.type == 'MESH']


def vertices(objs):
    dg = bpy.context.evaluated_depsgraph_get()
    vs, ps = [], []
    for o in objs:
        ev = o.evaluated_get(dg)
        me = ev.to_mesh()
        me.calc_loop_triangles()
        base = len(vs)
        vs.extend(ev.matrix_world @ v.co for v in me.vertices)
        ps.extend(tuple(base + i for i in t.vertices) for t in me.loop_triangles)
        ev.to_mesh_clear()
    return vs, ps


def arbol(objs):
    vs, ps = vertices(objs)
    return BVHTree.FromPolygons(vs, ps, all_triangles=True), vs, ps


def apoyar(nombre):
    "Baja o sube al personaje para que su punto más bajo quede justo sobre las tablas"
    arm = ARM[nombre]
    vs, _ = vertices(mallas(arm))
    z = min(v.z for v in vs)
    arm.location.z += PISO + 0.002 - z
    bpy.context.view_layer.update()
    return z


def mundo_hueso(nombre, hueso):
    arm = ARM[nombre]
    return arm.matrix_world @ arm.pose.bones[hueso].head


def poner_remo(desde, hacia):
    "El remo va de `desde` (el puño) a `hacia` (la pala), en Godot"
    a, b = gv(desde), gv(hacia)
    q = (b - a).normalized().to_track_quat('Y', 'Z')
    m = Matrix.Translation(a - q @ REMO_PUNO) @ q.to_matrix().to_4x4()
    REMO_RAIZ.matrix_world = m
    ANIM_REMO.matrix_world = m
    bpy.context.view_layer.update()


def sobre_remo(desde, hacia, t):
    a, b = Vector(desde), Vector(hacia)
    return tuple(a + (b - a).normalized() * t)


# ---------- Los cuadros: 2 (la silueta levanta el remo; también la revelación) y 4 (retrocede) ----------
PORKY_POS = (X_MUELLE - 0.12, PISO, PUNTA + 1.05)
PORKY_GIRO = 180.0
TONY_POS = (X_MUELLE - 0.02, PISO, PUNTA + 2.15)
TONY_GIRO = 180.0
TONY_ATRAS = (X_MUELLE + 0.28, PISO, PUNTA + 3.25)


def tony_golpe():
    p, g = TONY_POS, TONY_GIRO
    # Tiene los brazos de 0.32 m y una cabeza de 1 m: el remo no cabe sobre la cabeza. Lo levanta a dos manos frente al
    # pecho, en diagonal hacia su derecha, con la pala por encima y por fuera de la cabeza.
    puno = local(p, g, 0.10, 0.78, 0.22)
    pala = local(p, g, -1.10, 1.22, 0.36)
    return puno, pala, {
        "pos": p, "giro": g,
        "fk": {"Columna": [("arriba", -10)], "Pecho": [("arriba", -8)], "Cuello": [("arriba", 8)], "Cabeza": [("izq", 4), ("arriba", 10)]},
        "ik": {"ManoI": (sobre_remo(puno, pala, 0.05), local(p, g, 0.5, 0.65, -0.1)),
               "ManoD": (sobre_remo(puno, pala, 0.14), local(p, g, -0.6, 0.55, -0.1)),
               "PieI": (local(p, g, 0.17, 0.045, 0.10), local(p, g, 0.22, 0.4, 0.5)),
               "PieD": (local(p, g, -0.17, 0.045, -0.14), local(p, g, -0.22, 0.4, 0.5))},
    }


def tony_retrocede():
    p, g = TONY_ATRAS, TONY_GIRO
    # Da el paso atrás con el remo caído en la derecha y la izquierda hacia el pecho, donde traía el silbato
    puno = local(p, g, -0.33, 0.66, 0.12)
    pala = local(p, g, -0.50, 0.16, 1.28)
    return puno, pala, {
        "pos": p, "giro": g,
        "fk": {"Columna": [("izq", -6)], "Pecho": [("izq", -4)], "Cabeza": [("izq", 10), ("arriba", -10)]},
        "ik": {"ManoD": (sobre_remo(puno, pala, 0.02), local(p, g, -0.6, 0.6, -0.2)),
               "ManoI": (local(p, g, 0.14, 0.70, 0.20), local(p, g, 0.5, 0.5, 0.0)),
               "PieI": (local(p, g, 0.15, 0.06, 0.12), local(p, g, 0.2, 0.4, 0.5)),
               "PieD": (local(p, g, -0.15, 0.045, -0.16), local(p, g, -0.2, 0.4, 0.5))},
    }


def porky_de_espaldas():
    p, g = PORKY_POS, PORKY_GIRO
    return {
        "pos": p, "giro": g,
        "fk": {"Neck": [("izq", 6)], "Head": [("izq", 8)]},
        "ik": {"ManoD": (local(p, g, -0.20, 0.47, 0.17), local(p, g, -0.55, 0.3, -0.2))},
    }


FUERA = {"pos": (0, -30, 0), "giro": 0}
CUADROS = {
    2: {"nombre": "La silueta levanta el remo", "tony": tony_golpe, "porky": porky_de_espaldas},
    4: {"nombre": "La silueta retrocede", "tony": tony_retrocede, "porky": None},
}

REPORTE = {"cuadros": {}}


def revisar(k, con_porky):
    "Triángulos que se cruzan. El remo solo se revisa lejos de las manos, que sí lo tocan"
    res = {}
    tt, _, _ = arbol(mallas(AT))
    tr, vr, pr = arbol(REMO_MALLAS)
    ta, _, _ = arbol([TABLADO])
    manos = [mundo_hueso("Tony", "mixamorig_RightHand"), mundo_hueso("Tony", "mixamorig_LeftHand")]
    fuera_de_manos = 0
    lejos = []
    _, vt, pt = arbol(mallas(AT))
    for i, j in tt.overlap(tr):
        c = sum((vt[x] for x in pt[i]), Vector()) / 3
        if min((c - m).length for m in manos) > 0.14:
            fuera_de_manos += 1
            lejos.append(c)
    res["Tony-Remo (lejos de las manos)"] = fuera_de_manos
    if lejos:
        cen = sum(lejos, Vector()) / len(lejos)
        print("   remo choca cerca de (Godot):", round(cen.x, 2), round(cen.z, 2), round(-cen.y, 2), "manos", [(round(m.x, 2), round(m.z, 2), round(-m.y, 2)) for m in manos])
    res["Tony-Tablado"] = len(tt.overlap(ta))
    res["Remo-Tablado"] = len(tr.overlap(ta))
    if con_porky:
        tp, _, _ = arbol(mallas(AP))
        res["Porky-Tablado"] = len(tp.overlap(ta))
        res["Tony-Porky"] = len(tt.overlap(tp))
        res["Remo-Porky"] = len(tr.overlap(tp))
    return {n: v for n, v in res.items() if v}


def contactos(k):
    "A qué distancia quedan los pies de las tablas y las manos del remo"
    res = {}
    for n, hueso in (("PieI", "mixamorig_LeftFoot"), ("PieD", "mixamorig_RightFoot")):
        res[n] = round(mundo_hueso("Tony", hueso).z - PISO, 3)
    return res


for k, c in CUADROS.items():
    puno, pala, spec = c["tony"]()
    poner_remo(puno, pala)
    posar("Tony", spec, k)
    apoyar("Tony")
    if c["porky"]:
        posar("Porky", c["porky"](), k)
        apoyar("Porky")
        # La linterna, en la mano de Porky y apuntando al agua
        muneca = mundo_hueso("Porky", "Wrist_R")
        haz = gv(local(PORKY_POS, PORKY_GIRO, -0.22, -0.35, 1.3)) - muneca
        ANIM_LINTERNA.matrix_world = Matrix.Translation(muneca) @ haz.normalized().to_track_quat('-Z', 'Y').to_matrix().to_4x4()
    else:
        posar("Porky", FUERA, k)
        ANIM_LINTERNA.matrix_world = Matrix.Translation(gv((0, -30, 0)))
    bpy.context.view_layer.update()
    choques = revisar(k, c["porky"] is not None)
    REPORTE["cuadros"][k] = {"nombre": c["nombre"], "choques": choques, "intersecciones": sum(choques.values()),
                             "altura_pies_tony": contactos(k)}
    print(f"cuadro {k}: choques {choques or 'ninguno'}; pies {contactos(k)}")
    f = cuadro_de(k)
    for arm in ARM.values():
        arm.keyframe_insert("location", frame=f); arm.keyframe_insert("rotation_quaternion", frame=f)
        for pb in arm.pose.bones:
            pb.keyframe_insert("location", frame=f); pb.keyframe_insert("rotation_quaternion", frame=f)
    for o in (ANIM_REMO, ANIM_LINTERNA):
        o.keyframe_insert("location", frame=f); o.keyframe_insert("rotation_quaternion", frame=f)
    if VISTA:
        esc.frame_set(f)
        esc.render.engine = 'BLENDER_WORKBENCH'
        esc.display.shading.light = 'STUDIO'
        esc.display.shading.color_type = 'MATERIAL'
        esc.render.resolution_x, esc.render.resolution_y = 800, 800
        cam = bpy.data.objects.get("Revision") or bpy.data.objects.new("Revision", bpy.data.cameras.new("Revision"))
        if cam.name not in esc.collection.objects: esc.collection.objects.link(cam)
        esc.camera = cam
        cam.data.lens = 50
        centro = gv((X_MUELLE, 0.95, PUNTA + 1.9))
        for nombre, desde in (("lado", (X_MUELLE + 5.5, 1.2, PUNTA + 1.9)), ("atras", (X_MUELLE + 0.4, 1.2, PUNTA + 7.5)),
                              ("frente", (X_MUELLE + 1.0, 1.0, PUNTA - 4.5))):
            d = gv(desde)
            cam.location = d
            cam.rotation_mode = 'QUATERNION'
            cam.rotation_quaternion = (centro - d).to_track_quat('-Z', 'Y')
            esc.render.filepath = os.path.join(VISTA, f"pose_{k}_{nombre}.png")
            bpy.ops.render.render(write_still=True)

malos = [e for e in ERRORES_IK if e[1] > 0.01]
print("IK que no llegó (más de 1 cm):", malos or "ninguno")
REPORTE["ik_sin_llegar"] = malos
REPORTE["total_intersecciones"] = sum(v["intersecciones"] for v in REPORTE["cuadros"].values())
print("TOTAL INTERSECCIONES:", REPORTE["total_intersecciones"])

for arm in ARM.values():
    for fc in arm.animation_data.action.layers[0].strips[0].channelbag(arm.animation_data.action.slots[0]).fcurves:
        for p in fc.keyframe_points: p.interpolation = 'CONSTANT'

if not VISTA:
    os.makedirs(SALIDA, exist_ok=True)
    with open(os.path.join(BASE, "reporte_choques_2320.json"), "w", encoding="utf-8") as fh:
        json.dump(REPORTE, fh, ensure_ascii=False, indent=1)
    # Lo de revisión no se exporta
    for o in REMO_MALLAS + [TABLADO]: bpy.data.objects.remove(o)
    bpy.ops.object.select_all(action='DESELECT')
    exportar = list(ARM.values()) + [o for a in ARM.values() for o in mallas(a)] + [ANIM_REMO, ANIM_LINTERNA]
    for o in exportar: o.select_set(True)
    for idb in list(ARM.values()) + [ANIM_REMO, ANIM_LINTERNA]:
        ad = idb.animation_data
        pista = ad.nla_tracks.new(); pista.name = "Momentos"
        pista.strips.new("Momentos", int(ad.action.frame_range[0]), ad.action)
        ad.action = None
    salida = os.path.join(SALIDA, "reconstruccion_2320.glb")
    bpy.ops.export_scene.gltf(filepath=salida, export_format='GLB', use_selection=True, export_animation_mode='NLA_TRACKS',
                              export_force_sampling=False, export_frame_range=False, export_cameras=False, export_lights=False,
                              export_apply=False, export_yup=True)
    print("exportado:", salida)
