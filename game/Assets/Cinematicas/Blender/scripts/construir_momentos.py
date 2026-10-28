# Arma en Blender los 12 momentos congelados de la cinta (Caso 0) y los exporta para Godot.
#
#   blender -b --python construir_momentos.py                         -> caso0_momentos.blend + Caso0/cinta_momentos.glb
#   blender -b --python construir_momentos.py -- --vista <carpeta> [1,2,..] [--libre]   (renders rápidos, sin exportar)
#
# Parte de caso0_cinta.blend (la sala, Porky y la figura ya vestida), le quita la animación y pone en cada
# cuadro la pose escrita a mano en momentos.py: un giro por hueso (FK, en los ejes del personaje: izquierda,
# arriba, frente) y, donde hay contacto, IK de dos huesos hacia el punto exacto (reloj, cinta, borde de la
# rejilla, botones, asiento). Después cada personaje se apoya: se sube lo justo para no atravesar la mesa, el
# piso o la silla. No hay keyframes por fórmula: cada cuadro es una pose quieta.
#
# Exporta una animación «Momentos» con un fotograma por cuadro (el cuadro k en el segundo k, interpolación
# constante) para Porky, la figura y los vacíos Anim_* de los objetos, y, sin animar, la cámara de inicio y de
# fin de cada cuadro (Cam_k_Ini, Cam_k_Fin), su punto de enfoque (Foco_k), su luz de acento (Luz_k), el polvo
# suspendido y la cuerda.
import sys, os, math, json
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy, bmesh
from mathutils import Vector, Matrix, Quaternion
from mathutils.bvhtree import BVHTree
from comun import BASE, GAME, g2b
import retarget as rt
import momentos as M

ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
VISTA = ARGS[ARGS.index("--vista") + 1] if "--vista" in ARGS else None
LIBRE = "--libre" in ARGS
SOLO = None
for a in ARGS:
    if a.replace(",", "").isdigit(): SOLO = [int(x) for x in a.split(",")]
ENTRADA = os.path.join(BASE, "caso0_cinta.blend")
SALIDA_BLEND = os.path.join(BASE, "caso0_momentos.blend")
SALIDA_GLB = os.path.join(GAME, "Assets", "Cinematicas", "Caso0", "cinta_momentos.glb")
FPS = 30
PISO = M.PISO


def gv(v):
    return g2b(v[0], v[1], v[2])


def cuadro_de(k):
    "Fotograma de Blender del cuadro k (el cuadro k va en el segundo k de la animación Momentos)"
    return 1 + k * FPS


# ======================================================================
# 1. La escena de la versión animada, sin animación
# ======================================================================
bpy.ops.wm.open_mainfile(filepath=ENTRADA)
esc = bpy.context.scene
esc.frame_set(1)
AP, AF = bpy.data.objects["Porky"], bpy.data.objects["Figura"]
# El giro con que el glTF trae cada armadura (en el cuadro 1 Porky mira a +Z y la figura a -Z)
GIRO_IMPORT = {"Porky": AP.rotation_quaternion.copy(),
               "Figura": Quaternion((0, 0, 1), math.radians(-180)) @ AF.rotation_quaternion}
for o in list(bpy.data.objects):
    if o.animation_data: o.animation_data_clear()
    for c in list(o.constraints): o.constraints.remove(c)
for o in list(bpy.data.objects):
    if o.name.startswith(("Anim_", "Camara", "Cuerda")) or o.name.endswith("_Fisica"):
        bpy.data.objects.remove(o)
if esc.rigidbody_world: bpy.ops.rigidbody.world_remove()
for c in list(bpy.data.cameras):
    if c.users == 0: bpy.data.cameras.remove(c)
for arm in (AP, AF):
    arm.rotation_mode = 'QUATERNION'
    for pb in arm.pose.bones:
        pb.rotation_mode = 'QUATERNION'
        pb.matrix_basis = Matrix.Identity(4)
bpy.context.view_layer.update()

PK, FG = rt.Esqueleto(AP), rt.Esqueleto(AF)
ESQ = {"Porky": PK, "Figura": FG}
ARM = {"Porky": AP, "Figura": AF}

OBJ = bpy.data.objects
GRABADORA = OBJ["GrabadoraPista"]
RELOJ = OBJ["RelojPista"]
REJILLA = OBJ["RejillaVentilacion"]
PUERTA = OBJ["Puerta"]
SILLA = OBJ["SillaSospechoso"]
CINTA = next(o for o in OBJ if o.name.startswith("cassette_player_tape"))
PLUMA = OBJ["PlumaSecreta"]
BASE_OBJ = {n: o.matrix_world.copy() for n, o in
            (("Reloj", RELOJ), ("Rejilla", REJILLA), ("Puerta", PUERTA), ("Silla", SILLA), ("Cinta", CINTA), ("Pluma", PLUMA))}


def mundo_de(obj_base, local_godot):
    "Punto local (coordenadas de Godot del nodo, en su pose original) a mundo de Blender"
    x, y, z = local_godot
    return BASE_OBJ[obj_base] @ Vector((x, -z, y))


# Puntos con nombre que usa momentos.py (mundo de Blender)
PUNTOS = {
    "BOTON_STOP": mundo_de("Cinta", (-0.05, 0.016, 0.0)),
    "MICROFONO": gv((0.43, 1.33, -1.6)),
}


# ======================================================================
# 2. Pose: FK por hueso en los ejes del personaje + IK de dos huesos
# ======================================================================
def ejes(esq):
    izq, arriba, frente = esq.ejes()
    return {"izq": izq, "arriba": arriba, "frente": frente}


def nombre_hueso(esq, h):
    return esq.mapa.get(h, h)


def fk(esq, pose, giros):
    """giros: {hueso: [(eje, grados), ...]}; cada giro va en el marco del padre ya girado (como en un
    programa de animación), así que doblar el codo no depende de cómo quedó el hombro."""
    E = ejes(esq)
    acum = {}
    giros = {nombre_hueso(esq, h): g for h, g in giros.items()}
    for b in esq.orden:
        qp = acum[b.parent.name] if b.parent else Quaternion()
        ql = Quaternion()
        for eje, grados in giros.get(b.name, []):
            ql = ql @ Quaternion(E[eje], math.radians(grados))
        if b.name in giros:
            rt.girar_rama(esq, pose, b.name, qp @ ql @ qp.inverted())
        acum[b.name] = qp @ ql


def matriz_personaje(nombre, pos, giro, inclinar=None):
    "Matriz de mundo: posición (Godot), giro en grados alrededor de Y (0 = mira a +Z) e inclinación opcional"
    arm = ARM[nombre]
    q = Quaternion((0, 0, 1), math.radians(giro))
    if inclinar:
        for eje, grados in inclinar:
            # Inclinación del cuerpo entero alrededor de un eje de Godot (x, y o z)
            v = {"x": Vector((1, 0, 0)), "y": Vector((0, 0, 1)), "z": Vector((0, -1, 0))}[eje]
            q = Quaternion(q @ v if eje != "y" else v, math.radians(grados)) @ q
    return Matrix.Translation(gv(pos)) @ (q @ GIRO_IMPORT[nombre]).to_matrix().to_4x4() @ Matrix.Diagonal((*arm.scale, 1))


ERRORES_IK = []


def ik(esq, pose, mundo, sup, med, fin, objetivo, polo, etiqueta):
    """IK de dos huesos: lleva la cabeza de `fin` (la muñeca o el tobillo) al objetivo; el codo (o la
    rodilla) apunta al polo. Guarda cuánto le faltó."""
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


CADENAS = {
    "Porky": {"ManoI": ("Arm_1_L", "Arm_2_L", "Wrist_L"), "ManoD": ("Arm_1_R", "Arm_2_R", "Wrist_R"),
              "PieI": ("Leg_1_L", "Leg_2_L", "Ankle_L"), "PieD": ("Leg_1_R", "Leg_2_R", "Ankle_R")},
    "Figura": {"ManoI": ("DEF-upper_arm.L", "DEF-forearm.L", "DEF-hand.L"), "ManoD": ("DEF-upper_arm.R", "DEF-forearm.R", "DEF-hand.R"),
               "PieI": ("DEF-thigh.L", "DEF-shin.L", "DEF-foot.L"), "PieD": ("DEF-thigh.R", "DEF-shin.R", "DEF-foot.R")},
}


def punto(v, mundo=None, nombre=None):
    "Un punto de momentos.py: tupla de Godot, nombre de PUNTOS o ('local', x, y, z) relativo al personaje"
    if isinstance(v, str): return PUNTOS[v]
    if v[0] == "local":
        q = mundo.to_quaternion() @ GIRO_IMPORT[nombre].inverted()
        return mundo.translation + q @ Vector((v[1], -v[3], v[2]))
    if v[0] == "obj":
        # ("obj", nombre del objeto en este cuadro, punto local de Godot)
        return OBJETOS_CUADRO[v[1]] @ Vector((v[2][0], -v[2][2], v[2][1]))
    return gv(v)


OBJETOS_CUADRO = {}


def choques_con(nombre, prefijos):
    "Triángulos del personaje que se cruzan con los objetos cuyo nombre empieza con alguno de los prefijos"
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    tp = arbol(mallas(ARM[nombre], ("Figura_Rostro",)), dg)[0]
    objs = [o for o in SALA_SOLIDOS if o.name.startswith(prefijos)]
    if nombre == "Porky" and "Figura" in prefijos: objs = mallas(AF, ("Figura_Rostro",))
    if nombre == "Figura" and "Porky" in prefijos: objs = mallas(AP)
    if not objs: return 0
    pares = tp.overlap(arbol(objs, dg)[0])
    if any(o.name.startswith("Techo") for o in objs):
        # Por el hueco de la rejilla sí se pasa (igual que en revisar_choques)
        _, vp, pp = arbol(mallas(ARM[nombre], ("Figura_Rostro",)), dg)
        def en_hueco(i):
            c = sum((vp[j] for j in pp[i]), Vector()) / len(pp[i])
            return abs(c.x - HUECO[0]) < HUECO[2] and abs(-c.y - HUECO[1]) < HUECO[2]
        pares = [p for p in pares if not en_hueco(p[0])]
    return len(pares)


def ajustar(spec, ajuste, paso):
    "Mueve un parámetro de la pose: ('pos', eje) o (hueso, eje de giro)"
    que, eje = ajuste
    if que == "pos":
        i = "xyz".index(eje)
        p = list(spec["pos"]); p[i] += paso; spec["pos"] = tuple(p)
    else:
        lista = spec.setdefault("fk", {}).setdefault(que, [])
        lista.append((eje, paso))


def posar(nombre, spec, k):
    """Pone la pose del personaje para el cuadro k y devuelve su matriz de mundo. Con "contactos" la pose se
    corrige poco a poco (un parámetro a la vez) hasta que deja de atravesar cada objeto: así lo que se apoya
    queda tocando, sin meterse."""
    import copy
    spec = copy.deepcopy(spec)
    mundo = posar_una(nombre, spec, k)
    for prefijos, ajuste, paso, maximo, *modo in spec.get("contactos", []):
        prefijos = tuple(prefijos.split("|"))
        n = 0
        if modo and modo[0] == "tocar":
            # Se acerca hasta tocar (el primer paso que se mete) y regresa ese paso
            while n < maximo and not choques_con(nombre, prefijos):
                ajustar(spec, ajuste, paso)
                mundo = posar_una(nombre, spec, k)
                n += 1
            if n:
                ajustar(spec, ajuste, -paso)
                mundo = posar_una(nombre, spec, k)
        else:
            while n < maximo and choques_con(nombre, prefijos):
                ajustar(spec, ajuste, paso)
                mundo = posar_una(nombre, spec, k)
                n += 1
        if n: print(f"  contacto {nombre} {prefijos[0]}: {ajuste} {n} pasos de {paso} {modo}")
    if VISTA:
        bpy.context.view_layer.update()
        esq = ESQ[nombre]
        for h in ("Cadera", "BrazoI", "ManoI", "BrazoD", "ManoD", "Cabeza", "PieI"):
            b = esq.mapa.get(h)
            if b:
                w = ARM[nombre].matrix_world @ ARM[nombre].pose.bones[b].head
                print(f"    hueso {nombre}.{h}: ({w.x:.2f}, {w.z:.2f}, {-w.y:.2f})")
    return mundo


def posar_una(nombre, spec, k):
    esq, arm = ESQ[nombre], ARM[nombre]
    pose = {n: m.copy() for n, m in esq.reposo.items()}
    fk(esq, pose, spec.get("fk", {}))
    mundo = matriz_personaje(nombre, spec["pos"], spec.get("giro", 0), spec.get("inclinar"))
    for cadena, (objetivo, polo) in spec.get("ik", {}).items():
        sup, med, fin = CADENAS[nombre][cadena]
        ik(esq, pose, mundo, sup, med, fin, punto(objetivo, mundo, nombre), punto(polo, mundo, nombre), f"{k}:{nombre}:{cadena}")
    # Después del IK se ajusta la mano (o el pie) sin mover la muñeca
    if spec.get("fk_final"): fk(esq, pose, spec["fk_final"])
    arm.location = mundo.translation
    arm.rotation_quaternion = mundo.to_quaternion()
    rt.aplicar(esq, pose)
    return mundo


# ======================================================================
# 3. Objetos de la sala en cada cuadro
# ======================================================================
def matriz_objeto(nombre, spec):
    "spec: None (como en el juego), o dict con pos (Godot), giro [(eje, grados)] y desde (otra matriz base)"
    base = BASE_OBJ[nombre]
    if spec is None or "en_mano" in spec: return base.copy()
    q = base.to_quaternion()
    for eje, grados in spec.get("giro", []):
        v = {"x": Vector((1, 0, 0)), "y": Vector((0, 0, 1)), "z": Vector((0, -1, 0))}[eje]
        q = Quaternion(v, math.radians(grados)) @ q
    pos = gv(spec["pos"]) if "pos" in spec else base.translation + gv(spec.get("mover", (0, 0, 0))) - gv((0, 0, 0))
    return Matrix.Translation(pos) @ q.to_matrix().to_4x4()


NODOS = {"Reloj": RELOJ, "Rejilla": REJILLA, "Puerta": PUERTA, "Silla": SILLA, "Cinta": CINTA, "Pluma": PLUMA}


def poner_objeto(nombre, matriz):
    OBJETOS_CUADRO[nombre] = matriz
    NODOS[nombre].matrix_world = matriz


# ======================================================================
# 4. Cámaras, enfoque y luces de acento (vacíos que lee CinematicaCinta.cs)
# ======================================================================
esc.render.resolution_x, esc.render.resolution_y = 1920, 1080
COL_MOMENTOS = bpy.data.collections.new("Momentos")
esc.collection.children.link(COL_MOMENTOS)


def nuevo(nombre, datos=None):
    o = bpy.data.objects.new(nombre, datos)
    COL_MOMENTOS.objects.link(o)
    o.rotation_mode = 'QUATERNION'
    return o


def camara(nombre, desde, mira, lente, giro=0.0):
    "Cámara con lente real (sensor de 36 mm de ancho); giro: alabeo en grados"
    cam = nuevo(nombre, bpy.data.cameras.new(nombre))
    cam.data.sensor_fit = 'HORIZONTAL'
    cam.data.sensor_width = 36
    cam.data.lens = lente
    cam.data.clip_start = 0.02
    d, m = gv(desde), gv(mira)
    q = (m - d).to_track_quat('-Z', 'Y')
    if giro: q = q @ Quaternion((0, 0, 1), math.radians(giro))
    cam.location, cam.rotation_quaternion = d, q
    return cam


def vacio(nombre, pos, mira=None):
    e = nuevo(nombre)
    e.empty_display_type = 'ARROWS'
    e.location = gv(pos)
    if mira is not None: e.rotation_quaternion = (gv(mira) - gv(pos)).to_track_quat('-Z', 'Y')
    return e


# ======================================================================
# 5. Polvo suspendido y cuerda
# ======================================================================
def principled(mat):
    nodos = mat.node_tree.nodes
    bsdf = next((n for n in nodos if n.type == 'BSDF_PRINCIPLED'), None)
    if bsdf is None:
        bsdf = nodos.new("ShaderNodeBsdfPrincipled")
        salida = next((n for n in nodos if n.type == 'OUTPUT_MATERIAL'), None) or nodos.new("ShaderNodeOutputMaterial")
        mat.node_tree.links.new(bsdf.outputs[0], salida.inputs[0])
    return bsdf


def material_simple(nombre, color):
    mat = bpy.data.materials.new(nombre)
    if hasattr(mat, "use_nodes"): mat.use_nodes = True
    principled(mat).inputs["Base Color"].default_value = (*color, 1)
    mat.diffuse_color = (*color, 1)
    return mat


def polvo(nombre, centro, radio, cuantos, semilla):
    "Motas de polvo quietas en el aire (pequeños octaedros claros)"
    import random
    random.seed(semilla)
    bm = bmesh.new()
    for _ in range(cuantos):
        r = radio * random.random() ** 0.6
        a, b = random.uniform(0, 2 * math.pi), random.uniform(-1, 1)
        p = Vector((math.cos(a) * math.sqrt(1 - b * b), math.sin(a) * math.sqrt(1 - b * b), b)) * r
        p.z = -abs(p.z) * 1.6       # cae del ducto: casi todo bajo la rejilla
        tam = random.uniform(0.0025, 0.007)
        geo = bmesh.ops.create_icosphere(bm, subdivisions=0, radius=tam)
        bmesh.ops.translate(bm, verts=geo["verts"], vec=p)
    me = bpy.data.meshes.new(nombre)
    bm.to_mesh(me); bm.free()
    mat = material_simple("Polvo", (0.75, 0.72, 0.66))
    bsdf = principled(mat)
    bsdf.inputs["Emission Color"].default_value = (0.6, 0.6, 0.62, 1)
    bsdf.inputs["Emission Strength"].default_value = 0.4
    me.materials.append(mat)
    o = nuevo(nombre, me)
    o.location = gv(centro)
    return o


def cuerda(nombre, arriba, abajo):
    largo = (gv(arriba) - gv(abajo)).length
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=0.011, depth=largo)
    o = bpy.context.active_object
    o.name = nombre
    for c in list(o.users_collection): c.objects.unlink(o)
    COL_MOMENTOS.objects.link(o)
    o.data.materials.append(material_simple("Cuerda", (0.02, 0.02, 0.02)))
    o.rotation_mode = 'QUATERNION'
    o.location = (gv(arriba) + gv(abajo)) / 2
    o.rotation_quaternion = (gv(arriba) - gv(abajo)).to_track_quat('Z', 'Y')
    return o


# ======================================================================
# 6. Revisión de choques (BVH: triángulos que se cruzan)
# ======================================================================
SALA_SOLIDOS = [o for o in OBJ if o.type == 'MESH' and o.name.startswith((
    "Tablero", "Pata", "Travesano", "Asiento", "Respaldo", "Poste", "Pared", "Techo", "CSGBox", "Zocalo",
    "wall_clock", "cassette_player", "Puerta", "RejillaVentilacion", "VasoAgua", "Cenicero", "MarcoPuerta"))]
HUECO = (M.REJILLA[0], M.REJILLA[2], 0.27)        # en el techo, alrededor de la rejilla, sí se puede pasar


def arbol(objs, dg):
    verts, polis = [], []
    for o in objs:
        ev = o.evaluated_get(dg)
        me = ev.to_mesh()
        mw = ev.matrix_world
        base = len(verts)
        verts.extend(mw @ v.co for v in me.vertices)
        polis.extend([base + i for i in p.vertices] for p in me.polygons)
        ev.to_mesh_clear()
    return BVHTree.FromPolygons(verts, polis, all_triangles=False), verts, polis


def mallas(arm, sin=()):
    return [o for o in OBJ if o.type == 'MESH' and o.parent == arm and o.name not in sin and not o.hide_render]


DONDE = {}


def revisar_choques(k, presentes):
    "Cuántos triángulos de cada personaje se cruzan con la sala, los objetos y el otro personaje"
    dg = bpy.context.evaluated_depsgraph_get()
    res = {}
    arboles = {n: arbol(mallas(ARM[n], ("Figura_Rostro",)), dg) for n in presentes}
    for n in presentes:
        tp, vp, pp = arboles[n]
        for o in SALA_SOLIDOS + [x for x in COL_MOMENTOS.objects if x.name.startswith("Cuerda") and not x.hide_render]:
            to, vo, po = arbol([o], dg)
            pares = tp.overlap(to)
            if o.name.startswith("Techo"):
                # Por el hueco de la rejilla sí se pasa
                def en_hueco(i):
                    c = sum((vp[j] for j in pp[i]), Vector()) / len(pp[i])
                    return abs(c.x - HUECO[0]) < HUECO[2] and abs(-c.y - HUECO[1]) < HUECO[2]
                pares = [p for p in pares if not en_hueco(p[0])]
            if pares:
                res[f"{n} × {o.name}"] = len(pares)
                c = sum((sum((vp[j] for j in pp[i]), Vector()) / len(pp[i]) for i, _ in pares), Vector()) / len(pares)
                DONDE[f"{k}:{n} × {o.name}"] = (round(c.x, 2), round(c.z, 2), round(-c.y, 2))
    if len(presentes) == 2:
        pares = arboles["Porky"][0].overlap(arboles["Figura"][0])
        if pares: res["Porky × Figura"] = len(pares)
    return res


def altura_minima(arm, dentro=None):
    "Altura (Godot) del vértice más bajo del personaje, opcionalmente solo dentro de un rectángulo xz"
    dg = bpy.context.evaluated_depsgraph_get()
    z = 99.0
    for o in mallas(arm, ("Figura_Rostro",)):
        ev = o.evaluated_get(dg)
        me = ev.to_mesh()
        mw = ev.matrix_world
        for v in me.vertices:
            p = mw @ v.co
            if dentro and not (dentro[0] <= p.x <= dentro[1] and dentro[2] <= -p.y <= dentro[3]): continue
            z = min(z, p.z)
        ev.to_mesh_clear()
    return z


# ======================================================================
# 7. Los cuadros
# ======================================================================
FUERA = {"pos": (0, -20, 0), "giro": 0}          # quien no sale en el cuadro se esconde bajo el piso
REPORTE = {"cuadros": {}}
for arm in (AP, AF):
    for o in mallas(arm): o.hide_render = False


def guardar_clave(k):
    f = cuadro_de(k)
    for arm in (AP, AF):
        arm.keyframe_insert("location", frame=f); arm.keyframe_insert("rotation_quaternion", frame=f)
        for pb in arm.pose.bones:
            pb.keyframe_insert("location", frame=f); pb.keyframe_insert("rotation_quaternion", frame=f)
    for nombre, nodo in VACIOS.items():
        nodo.matrix_world = OBJETOS_CUADRO[nombre]
        nodo.keyframe_insert("location", frame=f); nodo.keyframe_insert("rotation_quaternion", frame=f)


VACIOS = {}
for nombre in NODOS:
    VACIOS[nombre] = nuevo("Anim_" + nombre)
    VACIOS[nombre].empty_display_type = 'PLAIN_AXES'

cuadros = [c for c in M.CUADROS if SOLO is None or c["k"] in SOLO]
for c in cuadros:
    k = c["k"]
    print("cuadro", k, c["nombre"])
    for nombre in NODOS:
        poner_objeto(nombre, matriz_objeto(nombre, c.get("objetos", {}).get(nombre)))
    bpy.context.view_layer.update()
    presentes = []
    for nombre in ("Porky", "Figura"):
        spec = c.get(nombre.lower())
        posar(nombre, spec or FUERA, k)
        if spec: presentes.append(nombre)
    bpy.context.view_layer.update()
    # Objetos que van en una mano (la cinta): siguen al hueso de la mano ya posada
    for nombre, spec in c.get("objetos", {}).items():
        if spec and "en_mano" in spec:
            quien, hueso, desplazado = spec["en_mano"]
            arm = ARM[quien]
            m = arm.matrix_world @ arm.pose.bones[ESQ[quien].mapa.get(hueso, hueso)].matrix
            q = m.to_quaternion()
            base_q = BASE_OBJ[nombre].to_quaternion()
            for eje, grados in spec.get("giro", []):
                v = {"x": Vector((1, 0, 0)), "y": Vector((0, 0, 1)), "z": Vector((0, -1, 0))}[eje]
                base_q = Quaternion(v, math.radians(grados)) @ base_q
            poner_objeto(nombre, Matrix.Translation(m.translation + gv(desplazado)) @ base_q.to_matrix().to_4x4())
    bpy.context.view_layer.update()
    # Pies (o lo más bajo del cuerpo) sobre el piso: si algo quedó bajo el piso, sube al personaje lo justo
    for nombre in presentes:
        arm = ARM[nombre]
        z = altura_minima(arm)
        if z < PISO + 0.002 and z > PISO - 0.3:
            arm.location.z += PISO + 0.002 - z
            bpy.context.view_layer.update()
    # Cámara de inicio y de fin, enfoque y luz de acento
    cam = c["camara"]
    ini = camara(f"Cam_{k:02d}_Ini", cam["desde"][0], cam["mira"][0], cam["lente"], cam.get("alabeo", 0))
    fin = camara(f"Cam_{k:02d}_Fin", cam["desde"][1], cam["mira"][1], cam["lente"], cam.get("alabeo", 0))
    vacio(f"Foco_{k:02d}", cam["foco"])
    if "luz" in c: vacio(f"Luz_{k:02d}", c["luz"]["desde"], c["luz"]["mira"])
    extras = []
    if "polvo" in c:
        extras.append(polvo(f"Polvo_{k:02d}", c["polvo"]["centro"], c["polvo"]["radio"], c["polvo"]["cuantos"], k))
    if "cuerda" in c:
        extras.append(cuerda(f"Cuerda_{k:02d}", c["cuerda"][0], c["cuerda"][1]))
    bpy.context.view_layer.update()
    choques = revisar_choques(k, presentes)
    REPORTE["cuadros"][k] = {"nombre": c["nombre"], "choques": choques, "intersecciones": sum(choques.values())}
    print("  choques:", choques if choques else "ninguno")
    for clave, lugar in DONDE.items():
        if clave.startswith(f"{k}:"): print("    en", clave, lugar)
    guardar_clave(k)
    if VISTA:
        os.makedirs(VISTA, exist_ok=True)
        esc.render.engine = 'BLENDER_WORKBENCH'
        esc.display.shading.light = 'MATCAP'
        esc.display.shading.color_type = 'TEXTURE'
        esc.display.shading.show_cavity = True
        esc.render.resolution_x, esc.render.resolution_y = 960, 540
        esc.frame_set(cuadro_de(k))
        for o in COL_MOMENTOS.objects:
            if o.name.startswith(("Polvo_", "Cuerda_")): o.hide_render = o not in extras
        esc.camera = fin
        esc.render.filepath = os.path.join(VISTA, f"cuadro_{k:02d}.png")
        bpy.ops.render.render(write_still=True)
        if LIBRE:
            for i, (d, m) in enumerate(c.get("libres", [])):
                esc.camera = camara(f"Libre_{k}_{i}", d, m, 24)
                esc.render.filepath = os.path.join(VISTA, f"cuadro_{k:02d}_libre{i}.png")
                bpy.ops.render.render(write_still=True)

# Los errores de IK: cuánto le faltó a cada mano o pie para tocar su punto
REPORTE["ik"] = {e: d for e, d in ERRORES_IK}
REPORTE["ik_lejos"] = {e: d for e, d in ERRORES_IK if d > 0.01}
REPORTE["total_intersecciones"] = sum(v["intersecciones"] for v in REPORTE["cuadros"].values())
print("IK a más de 1 cm:", REPORTE["ik_lejos"])
print("TOTAL INTERSECCIONES:", REPORTE["total_intersecciones"])

# Interpolación constante: entre cuadros no hay movimiento
for idb in [AP, AF] + list(VACIOS.values()):
    ad = idb.animation_data
    if ad and ad.action:
        for capa in ad.action.layers:
            for tira in capa.strips:
                for bolsa in tira.channelbags:
                    for fc in bolsa.fcurves:
                        for kp in fc.keyframe_points: kp.interpolation = 'CONSTANT'

if SOLO is None and not VISTA:
    with open(os.path.join(BASE, "reporte_choques_momentos.json"), "w", encoding="utf-8") as fh:
        json.dump(REPORTE, fh, ensure_ascii=False, indent=1)
    esc.frame_start, esc.frame_end = 1, cuadro_de(len(M.CUADROS))
    esc.frame_set(cuadro_de(1))
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=SALIDA_BLEND, compress=True)
    print("guardado:", SALIDA_BLEND)
    # Exportar: personajes, vacíos animados y todo lo de la colección Momentos
    bpy.ops.object.select_all(action='DESELECT')
    exportar = [AP, AF] + list(COL_MOMENTOS.objects)
    exportar += [o for o in OBJ if o.parent in (AP, AF) and o.type == 'MESH']
    for o in exportar: o.hide_set(False); o.select_set(True)
    for idb in [AP, AF] + list(VACIOS.values()):
        ad = idb.animation_data
        if ad is None or ad.action is None: continue
        pista = ad.nla_tracks.new(); pista.name = "Momentos"
        pista.strips.new("Momentos", int(ad.action.frame_range[0]), ad.action)
        ad.action = None
    bpy.ops.export_scene.gltf(filepath=SALIDA_GLB, export_format='GLB', use_selection=True,
                              export_animation_mode='NLA_TRACKS', export_force_sampling=False, export_frame_range=False,
                              export_cameras=True, export_lights=False, export_apply=False, export_yup=True)
    print("exportado:", SALIDA_GLB)
