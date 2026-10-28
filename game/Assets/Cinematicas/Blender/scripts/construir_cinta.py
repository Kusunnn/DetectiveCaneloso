# Arma en Blender la cinemática de la cinta (Caso 0) y la exporta para Godot.
#
#   blender -b --python construir_cinta.py                       -> caso0_cinta.blend + Assets/Cinematicas/Caso0/cinta_caso0.glb
#   blender -b --python construir_cinta.py -- --sin-exportar     (solo el .blend)
#   blender -b --python construir_cinta.py -- --choques          (además revisa choques cuadro por cuadro)
#
# Pasos: 1) importa la Sala 1 (sala1.glb, exportada desde Godot con exportar_sala_blender.gd), Porky y el
# maniquí de Quaternius; 2) viste a la figura (gabardina con pesos automáticos y capucha sin rostro);
# 3) la figura: animaciones de Quaternius + coreografía, pies apoyados en el piso e IK de brazos para que las
# manos agarren la rejilla, el reloj y la cuerda; 4) el reloj cae con Rigid Body (horneado cuadro a cuadro);
# 5) Porky, igual que la figura, con las manos en la mesa, el reloj, los botones, la cinta y la puerta;
# 6) tapa de la rejilla, cuerda, puerta, silla y cinta; 7) cámara; 8) revisión de choques; 9) exportar.
#
# El IK es propio (dos huesos, analítico) y se hornea directo en los keyframes: el esqueleto de Porky
# (Animal Crossing) tiene colas de hueso de varios metros y el IK de Blender no sirve con él.
import sys, os, math, json
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from comun import *
import retarget as rt
import coreografia as C

ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
EXPORTAR = "--sin-exportar" not in ARGS
SALIDA_GLB = os.path.join(GAME, "Assets", "Cinematicas", "Caso0", "cinta_caso0.glb")
SALIDA_BLEND = os.path.join(BASE, "caso0_cinta.blend")
PISO = 0.5


def F(t):
    "Cuadro de Blender (empieza en 1) para el segundo t de la cinemática"
    return 1 + int(round(t * FPS))


def T(f):
    return (f - 1) / FPS


def suave(u):
    u = max(0.0, min(1.0, u))
    return u * u * (3 - 2 * u)


def rampa(t, a, b):
    return max(0.0, min(1.0, (t - a) / (b - a))) if b != a else float(t >= a)


def ventana(t, a, b, entra, sale):
    "1 entre a y b, con rampas de entrada y salida"
    return rampa(t, a, a + entra) * (1 - rampa(t, b - sale, b))


def gv(v):
    "Vector de Godot a Blender"
    return g2b(v[0], v[1], v[2])


def en_ruta(ruta, t):
    "Posición (Godot) y giro (grados) en el segundo t"
    if t <= ruta[0][0]: return Vector(ruta[0][1]), ruta[0][2]
    for i in range(1, len(ruta)):
        if t > ruta[i][0]: continue
        a, b = ruta[i - 1], ruta[i]
        u = (t - a[0]) / max(1e-6, b[0] - a[0])
        caer = len(b) > 3 and b[3]
        av = u * u if caer else u
        pa, pb = Vector(a[1]), Vector(b[1])
        ga, gb = math.radians(a[2]), math.radians(b[2])
        d = (gb - ga + math.pi) % (2 * math.pi) - math.pi
        return pa.lerp(pb, av), math.degrees(ga + d * suave(u))
    return Vector(ruta[-1][1]), ruta[-1][2]


# ======================================================================
# 1. Escena
# ======================================================================
bpy.ops.wm.read_factory_settings(use_empty=True)
esc = bpy.context.scene
esc.render.fps = FPS
esc.frame_start, esc.frame_end = 1, F(C.FIN)
CUADROS = range(esc.frame_start, esc.frame_end + 1)

sala = importar(os.path.join(BASE, "sala1.glb"), "Sala")
porky_objs = importar(os.path.join(GAME, "Assets/Models/Personajes/porky.glb"), "Porky")
fig_objs = importar(os.path.join(GAME, "Assets/Animaciones/Quaternius/UAL1_Standard.glb"), "Figura")
ual2_objs = importar(os.path.join(GAME, "Assets/Animaciones/Quaternius/UAL2_Standard.glb"), "Fuentes")
AP = armadura(porky_objs); AP.name = "Porky"
AF = armadura(fig_objs); AF.name = "Figura"
A2 = armadura(ual2_objs); A2.name = "Fuente_UAL2"
for o in list(bpy.data.objects):
    if o.name.startswith("Icosphere"): bpy.data.objects.remove(o)   # forma de huesos que crea el importador
for o in [o for o in bpy.data.objects if o == A2 or o.parent == A2]:
    o.hide_render = True; o.hide_set(True)
for arm in (AP, AF, A2):
    if arm.animation_data: arm.animation_data.action = None
AP.scale = (0.0254 * 6.67,) * 3          # en Godot: porky.glb con escala 6.67 (el glb ya trae 0.0254)
for o in bpy.data.objects:
    if o.parent == AF and o.type == 'MESH': o.name = "Figura_Cuerpo"
bpy.context.view_layer.update()

PK = rt.Esqueleto(AP)
FG = rt.Esqueleto(AF)
U2 = rt.Esqueleto(A2)

U2_ACCIONES = set()
for a in bpy.data.actions:
    try:
        bolsa = a.layers[0].strips[0].channelbag(a.slots[0])
        if any('"pelvis"' in fc.data_path for fc in bolsa.fcurves): U2_ACCIONES.add(a.name)
    except Exception:
        pass


def fuente_de(accion):
    "Las acciones de UAL2 se leen con su esqueleto; las de UAL1 con el de la figura (es el mismo maniquí)"
    a = bpy.data.actions[accion]
    return (U2 if a.name in U2_ACCIONES else FG), a


def objeto(nombre):
    return bpy.data.objects[nombre]


def mundo_de(obj, local_godot):
    "Punto local (coordenadas de Godot del nodo) a mundo de Blender"
    x, y, z = local_godot
    return obj.matrix_world @ Vector((x, -z, y))


GRABADORA = objeto("GrabadoraPista")
RELOJ = objeto("RelojPista")
REJILLA = objeto("RejillaVentilacion")
PUERTA = objeto("Puerta")
SILLA = objeto("SillaSospechoso")
CINTA = next(o for o in bpy.data.objects if o.name.startswith("cassette_player_tape"))

# Puntos de contacto (mundo de Blender)
BOTON_STOP = mundo_de(GRABADORA, (-0.012, 0.052, 0.098))
BOTON_REW = mundo_de(GRABADORA, (-0.036, 0.052, 0.098))
BOTON_EJECT = mundo_de(GRABADORA, (0.012, 0.052, 0.098))
MICROFONO = mundo_de(GRABADORA, (0.03, 0.04, 0.06))
RELOJ_PARED = RELOJ.matrix_world.copy()
RELOJ_CENTRO = RELOJ_PARED.translation.copy()
CINTA_BASE = CINTA.matrix_world.copy()
CUERDA_XZ = (C.REJILLA[0] + 0.05, C.REJILLA[2] + 0.12)
BISAGRA = gv((2.97, 1.55, 3.0))
PUERTA_CERRADA = gv((2.47, 1.55, 2.975))


def cuerda_punto(y):
    return gv((CUERDA_XZ[0], y, CUERDA_XZ[1]))


# ======================================================================
# 2. La figura: gabardina y capucha (no debe leerse como maniquí)
# ======================================================================
def material(nombre, color, rugoso=0.9):
    m = bpy.data.materials.new(nombre)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1)
    bsdf.inputs["Roughness"].default_value = rugoso
    return m


TELA = material("Figura_Tela", (0.012, 0.012, 0.015))
VACIO = material("Figura_Rostro", (0.0, 0.0, 0.0), 1.0)


def hueso_mundo(arm, nombre):
    return arm.matrix_world @ arm.data.bones[nombre].head_local


def vestir():
    for o in bpy.data.objects:
        if o.parent == AF and o.type == 'MESH':
            o.data.materials.clear(); o.data.materials.append(TELA)
    # Gabardina: un cono abierto de los hombros a media pierna, con pesos automáticos para que siga las piernas
    hombros = hueso_mundo(AF, "DEF-spine.003")
    rodillas = hueso_mundo(AF, "DEF-shin.L") - Vector((0, 0, 0.12))
    arriba, abajo = hombros.z + 0.12, rodillas.z
    bpy.ops.mesh.primitive_cone_add(vertices=24, radius1=0.26, radius2=0.19, depth=arriba - abajo,
                                    end_fill_type='NOTHING', location=(0, hombros.y, (arriba + abajo) / 2))
    abrigo = bpy.context.active_object
    abrigo.name = "Figura_Gabardina"
    abrigo.scale = (1.0, 0.7, 1.0)
    bpy.ops.object.transform_apply(scale=True)
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.subdivide(number_cuts=5)
    bpy.ops.object.mode_set(mode='OBJECT')
    # Abierta por delante de la cintura para abajo, como gabardina (no como vestido)
    import bmesh
    bm = bmesh.new(); bm.from_mesh(abrigo.data)
    cintura = hueso_mundo(AF, "DEF-hips").z
    caras = [f for f in bm.faces if f.calc_center_median().y < -0.12 and abs(f.calc_center_median().x) < 0.06
             and f.calc_center_median().z + abrigo.location.z < cintura]
    bmesh.ops.delete(bm, geom=caras, context='FACES')
    bm.to_mesh(abrigo.data); bm.free()
    abrigo.data.materials.append(TELA)
    abrigo.modifiers.new("Grosor", 'SOLIDIFY').thickness = 0.015
    # Capucha: cubre la cabeza; la cara es un hueco negro
    cabeza = hueso_mundo(AF, "DEF-head")
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=16, radius=0.15, location=cabeza + Vector((0, 0.012, 0.1)))
    capucha = bpy.context.active_object
    capucha.name = "Figura_Capucha"
    capucha.scale = (1.0, 1.08, 1.12)
    capucha.data.materials.append(TELA)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=10, radius=0.1, location=cabeza + Vector((0, -0.075, 0.075)))
    rostro = bpy.context.active_object
    rostro.name = "Figura_Rostro"
    rostro.scale = (0.95, 0.55, 1.08)
    rostro.data.materials.append(VACIO)
    for o in (capucha, rostro):
        w = o.matrix_world.copy()
        o.parent = AF; o.parent_type = 'BONE'; o.parent_bone = "DEF-head"
        bpy.context.view_layer.update()
        o.matrix_world = w
    bpy.ops.object.select_all(action='DESELECT')
    abrigo.select_set(True); AF.select_set(True)
    bpy.context.view_layer.objects.active = AF
    bpy.ops.object.parent_set(type='ARMATURE_AUTO')
    for o in (abrigo, capucha, rostro):
        for c in list(o.users_collection): c.objects.unlink(o)
        bpy.data.collections["Figura"].objects.link(o)


vestir()

# ======================================================================
# 3. Herramientas de pose: animaciones, IK, mirar, pies en el piso
# ======================================================================
GIRO_IMPORT = {arm.name: arm.rotation_quaternion.copy() for arm in (AP, AF)}
for arm in (AP, AF): arm.rotation_mode = 'QUATERNION'


def matriz_ruta(arm, ruta, t):
    "Matriz de mundo de la armadura en t: recorrido + el giro con que la trae el glTF + su escala"
    pos, giro = en_ruta(ruta, t)
    q = Quaternion((0, 0, 1), math.radians(giro)) @ GIRO_IMPORT[arm.name]
    return Matrix.Translation(gv(pos)) @ q.to_matrix().to_4x4() @ Matrix.Diagonal((*arm.scale, 1))


def mundo_local(mundo, nombre, x, y, z):
    "Punto relativo al personaje: x a su izquierda, y arriba, z hacia su frente (metros)"
    q = mundo.to_quaternion() @ GIRO_IMPORT[nombre].inverted()
    return mundo.translation + q @ Vector((x, -z, y))


def pose_clips(clips, esq, t):
    "Pose del esqueleto en t según la lista de animaciones, mezclando al cambiar"
    def pose_de(c):
        t0, acc, ini, vel, bucle, _ = c
        fuente, a = fuente_de(acc)
        largo = (a.frame_range[1] - a.frame_range[0]) / FPS
        local = ini + max(0.0, t - t0) * vel
        local = (local % largo) if bucle and largo > 0 else min(local, largo)
        p = fuente.pose(a, a.frame_range[0] + local * FPS)
        return rt.retarget(fuente, p, esq) if fuente is not esq else p
    i = max([k for k, c in enumerate(clips) if c[0] <= t] or [0])
    actual = pose_de(clips[i])
    mezcla = clips[i][5]
    if i > 0 and mezcla > 0 and t < clips[i][0] + mezcla:
        return rt.mezclar(pose_de(clips[i - 1]), actual, suave((t - clips[i][0]) / mezcla))
    return actual


def girar(esq, pose, hueso, eje, grados):
    if grados: rt.girar_rama(esq, pose, hueso, Quaternion(eje, math.radians(grados)))


def mirar(esq, pose, hueso, objetivo_mundo, mundo, peso, limite=60):
    "Gira la cabeza (con lo que cuelga de ella) para que la cara apunte al objetivo"
    if peso <= 0: return
    frente = esq.ejes()[2]
    p = pose[hueso].translation
    giro_actual = pose[hueso].to_quaternion() @ esq.reposo[hueso].to_quaternion().inverted()
    adelante = giro_actual @ frente
    obj = mundo.inverted() @ objetivo_mundo
    q = adelante.rotation_difference((obj - p).normalized())
    rt.girar_rama(esq, pose, hueso, Quaternion(q.axis, min(q.angle, math.radians(limite)) * peso))


def ik(esq, pose, mundo, sup, med, fin, objetivo_mundo, peso, polo_mundo):
    """IK de dos huesos: lleva la cabeza del hueso `fin` (la muñeca) al objetivo.
    sup gira desde el hombro, med desde el codo; el codo apunta hacia el polo."""
    if peso <= 0 or objetivo_mundo is None: return
    inv = mundo.inverted()
    obj = inv @ objetivo_mundo
    polo = inv @ polo_mundo
    nueva = dict(pose)
    a = nueva[sup].translation.copy()
    b, c = nueva[med].translation, nueva[fin].translation
    l1, l2 = (b - a).length, (c - b).length
    d = max(1e-5, min((obj - a).length, (l1 + l2) * 0.998))
    # 1) Doblar el codo para que la distancia hombro-muñeca sea d
    n = (a - b).cross(c - b)
    if n.length < 1e-8: n = (a - b).cross(polo - b)
    n.normalize()
    actual = (a - b).angle(c - b)
    quiere = math.acos(max(-1.0, min(1.0, (l1 * l1 + l2 * l2 - d * d) / (2 * l1 * l2))))
    rt.girar_rama(esq, nueva, med, Quaternion(n, quiere - actual))
    # 2) Apuntar el brazo al objetivo
    c = nueva[fin].translation
    rt.girar_rama(esq, nueva, sup, (c - a).rotation_difference(obj - a))
    # 3) Girar alrededor del eje hombro-objetivo para que el codo mire al polo
    k = (obj - a).normalized()
    e = nueva[med].translation - a
    e_p = e - k * e.dot(k)
    p_p = (polo - a) - k * (polo - a).dot(k)
    if e_p.length > 1e-6 and p_p.length > 1e-6:
        ang = e_p.angle(p_p)
        if e_p.cross(p_p).dot(k) < 0: ang = -ang
        rt.girar_rama(esq, nueva, sup, Quaternion(k, ang))
    pose.update(rt.mezclar(pose, nueva, min(1.0, peso)))
    if peso > 0.95:
        err = (mundo @ nueva[fin].translation - objetivo_mundo).length
        ERRORES_IK.append((round(T_ACTUAL[0], 2), fin, round(err, 3)))


ERRORES_IK = []
T_ACTUAL = [0.0]


def apoyar(pose, mundo, pies, altura_dedo, piso=PISO):
    "Sube o baja la armadura para que el pie más bajo quede en el piso (o en el asiento)"
    z = min((mundo @ pose[p].translation).z for p in pies)
    return Matrix.Translation(Vector((0, 0, piso + altura_dedo - z))) @ mundo


def altura_reposo(arm, huesos):
    return min((arm.matrix_world @ arm.data.bones[h].head_local).z for h in huesos) - arm.matrix_world.translation.z


PIES_PK = ["Toe_L", "Toe_R", "Ankle_L", "Ankle_R"]
PIES_FG = ["DEF-toe.L", "DEF-toe.R", "DEF-foot.L", "DEF-foot.R"]
DEDO_PK = altura_reposo(AP, PIES_PK)
DEDO_FG = altura_reposo(AF, PIES_FG) + 0.015
print("altura de los dedos en reposo:", round(DEDO_PK, 3), round(DEDO_FG, 3))


def construir(arm, esq, clips, ruta, ajustes, apoyado, pies, dedo, piso=lambda t: PISO):
    """Pone la pose en cada cuadro y la guarda como keyframes (huesos y raíz)"""
    print(arm.name, "...")
    for f in CUADROS:
        t = T(f)
        T_ACTUAL[0] = t
        mundo = matriz_ruta(arm, ruta, t)
        pose = pose_clips(clips, esq, t)
        if apoyado(t): mundo = apoyar(pose, mundo, pies, dedo, piso(t))
        pose = ajustes(t, pose, mundo)
        arm.location = mundo.translation
        arm.rotation_quaternion = mundo.to_quaternion()
        arm.keyframe_insert("location", frame=f)
        arm.keyframe_insert("rotation_quaternion", frame=f)
        rt.aplicar(esq, pose, f)


# ======================================================================
# 4. La figura
# ======================================================================
def reloj_en_manos(t):
    "El reloj mientras lo tiene la figura: lo saca del clavo, lo separa y lo baja a la cintura, boca arriba"
    u = suave(rampa(t, C.T_DESCUELGA, C.T_DESCUELGA + 0.4))
    baja = suave(rampa(t, 25.35, 25.9))
    # Lo baja a la cintura mientras da un paso a un lado (hacia +X), lejos del clavo
    pos = RELOJ_CENTRO + Vector((0, 0, 0.035)) * u + Vector((0, -0.24, 0)) * u + Vector((0.6, -0.42, -0.78)) * baja
    giro = Quaternion((1, 0, 0), math.radians(-86 * baja))
    return Matrix.Translation(pos) @ (giro @ RELOJ_PARED.to_quaternion()).to_matrix().to_4x4()


def lado_reloj(m, s, fuera=0.04):
    "Punto en el borde del reloj (s = -1 izquierda, 1 derecha, vistos de frente), un poco hacia la carátula"
    return m.translation + m.to_quaternion() @ Vector((s * 0.15, -fuera, 0))


def ajustes_figura(t, pose, mundo):
    izq, arriba, frente = FG.ejes()
    m = FG.mapa
    # Colgada de la rejilla y trepando: piernas juntas y un poco dobladas, se mecen
    if t < C.T_ATERRIZA - 0.05 or t >= C.T_SUBE:
        for h in ("MusloI", "MusloD"): girar(FG, pose, m[h], izq, 10)
        for h in ("PiernaI", "PiernaD"): girar(FG, pose, m[h], izq, -20)
    if t < C.T_ATERRIZA - 0.05:
        girar(FG, pose, m["MusloI"], izq, 6 * math.sin(t * 2.6))
        girar(FG, pose, m["MusloD"], izq, 6 * math.sin(t * 2.6 + 0.6))
    if t >= C.T_SUBE:
        fase = (t - C.T_SUBE) * 3.0 * 2 * math.pi
        girar(FG, pose, m["MusloI"], izq, 30 + 22 * math.sin(fase))
        girar(FG, pose, m["MusloD"], izq, 30 + 22 * math.sin(fase + math.pi))
    # Susurra al micrófono: se inclina hacia la grabadora; cabeza y hombros se mueven con las palabras
    w = ventana(t, 19.4, 23.0, 0.5, 0.3)
    if w > 0:
        habla = math.sin(t * 9.0) * ventana(t, C.T_SUSURRO, 22.8, 0.2, 0.2)
        girar(FG, pose, m["Columna"], izq, w * 46)
        girar(FG, pose, m["Pecho"], izq, w * 22)
        girar(FG, pose, m["Cuello"], izq, w * (6 + 2.5 * habla))
        girar(FG, pose, m["PechoAlto"], frente, w * 1.5 * habla)
        mirar(FG, pose, m["Cabeza"], MICROFONO, mundo, 0.8 * w, 40)
    # Mira a Porky mientras sostiene el reloj y lo suelta
    w = ventana(t, 25.3, 26.8, 0.4, 0.4)
    if w > 0: mirar(FG, pose, m["Cabeza"], gv((0.0, 1.45, -2.75)), mundo, 0.9 * w, 85)
    # Brazos: rejilla, mesa, reloj, cuerda
    for lado, s in (("L", 1), ("R", -1)):
        sup, med, fin = "DEF-upper_arm." + lado, "DEF-forearm." + lado, "DEF-hand." + lado
        codo = mundo_local(mundo, "Figura", s * 0.7, 1.0, -0.6)
        objetivo, peso = None, 0.0
        if t < C.T_SUELTA + 0.1:
            objetivo = gv((C.REJILLA[0] + s * 0.17, C.TECHO - 0.03, C.REJILLA[2] - 0.2))
            peso = 1 - rampa(t, C.T_SUELTA, C.T_SUELTA + 0.1)
            codo = mundo_local(mundo, "Figura", s * 0.7, 1.6, -0.3)
        elif lado == "L" and 19.5 <= t < 23.0:
            objetivo, peso = gv((0.72, 1.37, -1.5)), ventana(t, 19.5, 23.0, 0.35, 0.3)
        elif 24.55 <= t < C.T_SUELTA_RELOJ + 0.15:
            mr = reloj_en_manos(t) if t >= C.T_DESCUELGA else RELOJ_PARED
            objetivo = lado_reloj(mr, -s)
            peso = rampa(t, 24.55, C.T_DESCUELGA) * (1 - rampa(t, C.T_SUELTA_RELOJ, C.T_SUELTA_RELOJ + 0.15))
        elif t >= C.T_SUBE - 0.15:
            sube = rampa(t, C.T_SUBE, C.T_SE_VA)
            fase = (t - C.T_SUBE) * 3.0 + (0.5 if lado == "R" else 0.0)
            alto = 2.05 + sube * 3.0 + 0.25 * (fase % 1.0)
            objetivo, peso = cuerda_punto(min(alto, C.TECHO + 0.8)), rampa(t, C.T_SUBE - 0.15, C.T_SUBE + 0.1)
            codo = mundo_local(mundo, "Figura", s * 0.6, 1.2, 0.3)
        ik(FG, pose, mundo, sup, med, fin, objetivo, peso, codo)
    return pose


construir(AF, FG, C.ANIM_FIGURA, C.RUTA_FIGURA, ajustes_figura,
          lambda t: C.T_ATERRIZA - 0.02 <= t < C.T_SUBE, PIES_FG, DEDO_FG)

# ======================================================================
# 5. El reloj: Rigid Body (cae, gira, rebota y se para)
# ======================================================================
def simular_reloj():
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=0.145, depth=0.05)
    fis = bpy.context.active_object
    fis.name = "Reloj_Fisica"
    fis.hide_render = True
    fis.display_type = 'WIRE'
    bpy.ops.rigidbody.object_add(type='ACTIVE')
    rb = fis.rigid_body
    rb.mass, rb.friction, rb.restitution = 1.1, 1.0, 0.15
    rb.collision_shape = 'CYLINDER'
    rb.linear_damping, rb.angular_damping = 0.4, 0.95
    eje = Matrix.Rotation(math.radians(90), 4, 'X')          # el eje del cilindro va hacia la carátula
    soltar = F(C.T_SUELTA_RELOJ)
    for f in range(esc.frame_start, soltar + 1):
        t = T(f)
        fis.matrix_world = (reloj_en_manos(t) if t >= C.T_DESCUELGA else RELOJ_PARED) @ eje
        fis.keyframe_insert("location", frame=f); fis.keyframe_insert("rotation_euler", frame=f)
    rb.kinematic = True
    rb.keyframe_insert("kinematic", frame=soltar)
    rb.kinematic = False
    rb.keyframe_insert("kinematic", frame=soltar + 1)
    for nombre, tam, pos in (("Piso_Fisica", (8, 8, 0.2), (0, 1.0, PISO - 0.1)), ("Pared_Fisica", (8, 0.2, 3), (0, 5.1, 2.0))):
        bpy.ops.mesh.primitive_cube_add(size=1, location=pos)
        o = bpy.context.active_object; o.name = nombre; o.scale = tam; o.hide_render = True; o.display_type = 'WIRE'
        bpy.ops.object.transform_apply(scale=True)
        bpy.ops.rigidbody.object_add(type='PASSIVE')
        o.rigid_body.friction, o.rigid_body.restitution = 0.8, 0.3
    w = esc.rigidbody_world
    w.point_cache.frame_start, w.point_cache.frame_end = esc.frame_start, F(30)
    w.substeps_per_frame, w.solver_iterations = 20, 20
    tray = {}
    for f in range(esc.frame_start, F(30) + 1):
        esc.frame_set(f)
        tray[f] = fis.matrix_world.copy() @ eje.inverted()
    return tray


TRAY = simular_reloj()
ULTIMO = max(TRAY)
RELOJ_PISO = TRAY[ULTIMO]
normal = RELOJ_PISO.to_quaternion() @ Vector((0, -1, 0))
golpes = [round(T(f), 2) for f in range(F(C.T_SUELTA_RELOJ) + 1, ULTIMO)
          if (TRAY[f].translation.z - TRAY[f - 1].translation.z) < -0.002 and (TRAY[f + 1].translation.z - TRAY[f].translation.z) >= -0.0005]
quieto = next((round(T(f), 2) for f in range(F(C.T_SUELTA_RELOJ) + 2, ULTIMO)
               if all((TRAY[g].translation - RELOJ_PISO.translation).length < 0.002 for g in range(f, min(ULTIMO, f + 10)))), None)
REPORTE = {"golpes_reloj": golpes, "reloj_quieto": quieto,
           "reloj_piso_godot": (round(RELOJ_PISO.translation.x, 3), round(RELOJ_PISO.translation.z, 3), round(-RELOJ_PISO.translation.y, 3)),
           "caratula_hacia_arriba": round(normal.z, 3)}
print("RELOJ:", REPORTE)

# Porky se para frente al reloj caído
RUTA_PK = []
for p in C.RUTA_PORKY:
    pos = p[1]
    if pos == "RELOJ": pos = (RELOJ_PISO.translation.x - 0.03, PISO, -RELOJ_PISO.translation.y + 0.42)
    RUTA_PK.append((p[0], pos) + tuple(p[2:]))

# ======================================================================
# 6. Porky
# ======================================================================
def en_silla(t):
    "0 en el piso, 1 de pie sobre el asiento de su silla"
    return suave(rampa(t, C.T_SUBE_SILLA + 0.1, C.T_ARRIBA)) * (1 - suave(rampa(t, C.T_BAJA_SILLA, C.T_ABAJO - 0.1)))


def piso_porky(t):
    return PISO + C.ASIENTO * en_silla(t)


SILLA_BASE = SILLA.matrix_world.copy()


def silla_z(zg, xg=None):
    "La silla de Porky movida a (xg, zg) de Godot (mismo giro)"
    m = SILLA_BASE.copy()
    m.translation = Vector((m.translation.x if xg is None else xg, -zg, m.translation.z))
    return m


def silla_en(t):
    "La silla de Porky: la empuja al levantarse, la lleva a la pared, se sube y la regresa a la mesa"
    if t < C.T_PARA: return SILLA_BASE
    if t < C.T_EMPUJA:
        return silla_z(C.SILLA[2] + (C.SILLA_ATRAS[2] - C.SILLA[2]) * suave(rampa(t, C.T_PARA + 0.1, C.T_PARA + 0.7)))
    if t < C.T_SILLA_PARED or C.T_JALA <= t < C.T_SILLA_MESA:
        pos, _ = en_ruta(RUTA_PK, t)
        return silla_z(pos.z - C.ANTE_SILLA, pos.x)
    if t < C.T_JALA: return silla_z(C.SILLA_PARED[2], C.SILLA_PARED[0])
    return silla_z(C.SILLA_ATRAS[2])


def esquina_silla(t, s):
    "Esquina delantera del asiento (s = 1 a la izquierda de Porky), de donde la toma para moverla"
    # La silla del sospechoso está girada 180°: su frente local (-Z de Godot) mira a la mesa
    return silla_en(t) @ Vector((s * 0.2, 0.21, 0.48))


def reloj_porky(t):
    "El reloj desde que Porky lo toma del piso hasta que queda colgado"
    if t < C.T_AGARRA: return RELOJ_PISO
    pos_r, giro_r = en_ruta(RUTA_PK, t)
    adelante = Quaternion((0, 0, 1), math.radians(giro_r)) @ Vector((0, -1, 0))
    pecho = gv((pos_r.x, piso_porky(t) + 0.72, pos_r.z)) + adelante * 0.3
    carga = Matrix.Translation(pecho) @ RELOJ_PARED.to_quaternion().to_matrix().to_4x4()
    levanta = suave(rampa(t, C.T_AGARRA + 0.1, 40.6))
    q = RELOJ_PISO.to_quaternion().slerp(carga.to_quaternion(), levanta)
    pos = RELOJ_PISO.translation.lerp(carga.translation, levanta)
    cuelga = suave(rampa(t, C.T_ARRIBA + 0.1, C.T_CUELGA_RELOJ))
    return Matrix.Translation(pos.lerp(RELOJ_CENTRO, cuelga)) @ q.slerp(RELOJ_PARED.to_quaternion(), cuelga).to_matrix().to_4x4()


def cinta_en(t):
    "La cinta en la grabadora; sube al expulsarla"
    return Matrix.Translation(Vector((0, 0, 0.014 * suave(rampa(t, C.T_EXPULSA, C.T_EXPULSA + 0.2))))) @ CINTA_BASE


def puerta_en(t):
    a = suave(rampa(t, C.T_ABRE_PUERTA + 0.15, C.T_ABRE_PUERTA + 0.75)) * (1 - suave(rampa(t, C.T_CIERRA_PUERTA, C.T_CIERRA_PUERTA + 0.5)))
    giro = Matrix.Rotation(math.radians(-88 * a), 4, 'Z')
    return Matrix.Translation(BISAGRA + giro.to_3x3() @ (PUERTA_CERRADA - BISAGRA)) @ giro


def manija(t):
    "La manija, en el borde libre de la puerta, a la altura de la mano de Porky"
    return puerta_en(t) @ Vector((-0.42, 0.04, -0.2))


def pulso(t, t0):
    return math.exp(-((t - t0) / 0.08) ** 2)


def ajustes_porky(t, pose, mundo):
    izq, arriba, frente = PK.ejes()
    m = PK.mapa
    # Dormido: inclinado sobre la mesa con la cabeza de lado sobre los brazos; respira y cabecea
    dormido = 1 - rampa(t, C.T_DESPIERTA, C.T_DESPIERTA + 0.35)
    golpe = max(0.0, 1 - abs(t - 26.75) / 0.8)
    if dormido > 0:
        resp = math.sin(t * 2 * math.pi / 3.4)
        girar(PK, pose, m["Columna"], izq, dormido * (7 + 1.5 * resp) - 6 * golpe)
        girar(PK, pose, m["Pecho"], izq, dormido * 8)
        girar(PK, pose, m["Cabeza"], frente, dormido * (-24 + 2.5 * math.sin(t * 0.9)) + 10 * golpe)
        girar(PK, pose, m["Cabeza"], izq, dormido * (6 + 2 * resp))
    # Despierta sobresaltado, se endereza y mira alrededor
    if C.T_DESPIERTA <= t < 37.6:
        sacudida = math.exp(-((t - C.T_DESPIERTA - 0.2) / 0.2) ** 2)
        girar(PK, pose, m["Columna"], izq, -10 * sacudida)
        u = rampa(t, C.T_DESPIERTA + 0.5, C.T_DESPIERTA + 2.0)
        girar(PK, pose, m["Cuello"], arriba, 40 * math.sin(u * math.pi * 2) * (1 - rampa(t, 35.5, 35.8)))
    # Al levantarse no se va de frente contra la mesa
    w = ventana(t, C.T_PARA - 0.1, C.T_PARA + 1.4, 0.3, 0.4)
    if w > 0: girar(PK, pose, m["Columna"], izq, -18 * w)
    # Empuja y jala la silla: inclinado hacia ella
    w = max(ventana(t, C.T_EMPUJA - 0.2, C.T_SILLA_PARED + 0.1, 0.25, 0.2), ventana(t, C.T_JALA - 0.2, C.T_SILLA_MESA + 0.1, 0.25, 0.2))
    if w > 0: girar(PK, pose, m["Columna"], izq, 8 * w)
    # Agachado para recoger el reloj: la cabeza no toca la pared
    w = ventana(t, 39.65, 40.6, 0.3, 0.3)
    if w > 0:
        for h in ("MusloI", "MusloD"): girar(PK, pose, m[h], izq, 30 * w)
        for h in ("PiernaI", "PiernaD"): girar(PK, pose, m[h], izq, -55 * w)
        for h in ("PieI", "PieD"): girar(PK, pose, m[h], izq, 10 * w)
        girar(PK, pose, m["Columna"], izq, 22 * w)
        girar(PK, pose, m["Cuello"], izq, -14 * w)
    # Sube y baja de la silla: levanta una rodilla
    for t0, t1, h in ((C.T_SUBE_SILLA, C.T_ARRIBA, "MusloD"), (C.T_BAJA_SILLA, C.T_ABAJO, "MusloI")):
        u = rampa(t, t0, t1)
        if 0 < u < 1: girar(PK, pose, m[h], izq, 45 * math.sin(math.pi * u))
    # Cuelga el reloj: de puntas sobre la silla, la cabeza un poco atrás para no chocar con la pared
    w = ventana(t, C.T_ARRIBA, C.T_BAJA_SILLA, 0.25, 0.2)
    if w > 0:
        for h in ("PieI", "PieD"): girar(PK, pose, m[h], izq, -14 * w)
        girar(PK, pose, m["Cuello"], izq, -18 * w)
    # Ve la luz de REC: se paraliza y se encoge
    w = ventana(t, C.T_SOBRESALTO - 0.1, C.T_SOBRESALTO + 1.1, 0.15, 0.4)
    if w > 0: girar(PK, pose, m["Columna"], izq, -12 * w)
    # Junto a la grabadora se inclina para alcanzar los botones
    w = ventana(t, 47.4, 51.8, 0.35, 0.35)
    if w > 0:
        girar(PK, pose, m["Columna"], izq, 8 * w)
    # Miradas
    w = ventana(t, 36.0, C.T_BAJA_SILLA, 0.4, 0.3)
    if w > 0: mirar(PK, pose, m["Cabeza"], RELOJ_PISO.translation if t < 40.5 else RELOJ_CENTRO, mundo, 0.6 * w)
    w = ventana(t, C.T_VE_REC - 0.3, 51.8, 0.25, 0.3)
    if w > 0: mirar(PK, pose, m["Cabeza"], GRABADORA.matrix_world.translation, mundo, 0.35 * w, 30)
    # Brazos
    for lado, s in (("L", 1), ("R", -1)):
        sup, med, fin = "Arm_1_" + lado, "Arm_2_" + lado, "Wrist_" + lado
        codo = mundo_local(mundo, "Porky", s * 0.5, 0.6, -0.4)
        objetivo, peso = None, 0.0
        if t < C.T_DESPIERTA + 0.35:
            # Los brazos cruzados sobre la mesa, de almohada
            objetivo = gv((s * 0.15, 1.37, -2.52))
            peso = 1 - rampa(t, C.T_DESPIERTA, C.T_DESPIERTA + 0.35)
            codo = mundo_local(mundo, "Porky", s * 0.6, 0.2, 0.3)
        elif C.T_EMPUJA - 0.25 <= t < C.T_SILLA_PARED + 0.15:
            objetivo, peso = esquina_silla(t, s), ventana(t, C.T_EMPUJA - 0.25, C.T_SILLA_PARED + 0.15, 0.25, 0.2)
        elif C.T_JALA - 0.25 <= t < C.T_SILLA_MESA + 0.15:
            objetivo, peso = esquina_silla(t, s), ventana(t, C.T_JALA - 0.25, C.T_SILLA_MESA + 0.15, 0.25, 0.2)
        elif 39.7 <= t < C.T_CUELGA_RELOJ + 0.3:
            # Toma el reloj del piso con las dos manos, lo lleva, se sube y lo cuelga
            objetivo = lado_reloj(reloj_porky(max(t, C.T_AGARRA)), -s)
            peso = rampa(t, 39.7, C.T_AGARRA) * (1 - rampa(t, C.T_CUELGA_RELOJ, C.T_CUELGA_RELOJ + 0.3))
        elif C.T_SOBRESALTO <= t < C.T_SOBRESALTO + 1.2:
            # Las manos a la cabeza
            objetivo = mundo_local(mundo, "Porky", s * 0.19, 0.98, 0.06)
            peso = ventana(t, C.T_SOBRESALTO, C.T_SOBRESALTO + 1.2, 0.2, 0.35)
        elif lado == "R" and 47.4 <= t < 51.8:
            if t < C.T_REBOBINA - 0.05: boton, t0 = BOTON_STOP, C.T_STOP
            elif t < C.T_EXPULSA - 0.1: boton, t0 = BOTON_REW, C.T_REBOBINA
            elif t < C.T_TOMA_CINTA: boton, t0 = BOTON_EJECT, C.T_EXPULSA
            else: boton, t0 = None, 0
            objetivo = (boton + Vector((0, 0, 0.03 - 0.012 * pulso(t, t0)))) if boton else cinta_en(t).translation + Vector((0, 0, 0.03))
            peso = ventana(t, 47.4, 51.8, 0.35, 0.3)
        elif lado == "R" and C.T_ABRE_PUERTA - 0.3 <= t < C.T_ABRE_PUERTA + 0.55:
            # Jala la manija hasta media apertura; la puerta sigue abriéndose sola
            objetivo, peso = manija(min(t, C.T_ABRE_PUERTA + 0.4)), ventana(t, C.T_ABRE_PUERTA - 0.3, C.T_ABRE_PUERTA + 0.55, 0.25, 0.2)
        ik(PK, pose, mundo, sup, med, fin, objetivo, peso, codo)
    return pose


construir(AP, PK, C.ANIM_PORKY, RUTA_PK, ajustes_porky, lambda t: t >= C.T_PARA + 0.6, PIES_PK, DEDO_PK, piso_porky)

# ======================================================================
# 7. Objetos animados (los copia CinematicaCinta.cs a los nodos reales)
# ======================================================================
def anim_objeto(nombre, funcion, evaluar=False):
    e = bpy.data.objects.new(nombre, None)
    esc.collection.objects.link(e)
    e.rotation_mode = 'QUATERNION'
    for f in CUADROS:
        if evaluar: esc.frame_set(f)
        e.matrix_world = funcion(T(f))
        e.keyframe_insert("location", frame=f); e.keyframe_insert("rotation_quaternion", frame=f); e.keyframe_insert("scale", frame=f)
    return e


def reloj_final(t):
    f = F(t)
    if t < C.T_DESCUELGA: return RELOJ_PARED
    if t < C.T_SUELTA_RELOJ: return reloj_en_manos(t)
    if f <= ULTIMO: return TRAY[f]
    if t < C.T_AGARRA: return RELOJ_PISO
    if t < C.T_CUELGA_RELOJ: return reloj_porky(t)
    return RELOJ_PARED


TAPA = REJILLA.matrix_world.copy()


def tapa(t):
    "Vibra, sube y se desliza adentro del ducto; al final regresa mal puesta (ladeada y corrida)"
    vib = (math.sin(t * 61) * 0.004 + math.sin(t * 43) * 0.003) * ventana(t, C.T_REJILLA, C.T_TAPA, 0.1, 0.05)
    sube = suave(rampa(t, C.T_TAPA, C.T_TAPA + 0.2)) * (1 - suave(rampa(t, C.T_CIERRA_TAPA, C.T_CIERRA_TAPA + 0.25)))
    corre = suave(rampa(t, C.T_TAPA + 0.2, C.T_TAPA + 0.5)) * (1 - suave(rampa(t, C.T_CIERRA_TAPA - 0.2, C.T_CIERRA_TAPA)))
    mal = suave(rampa(t, C.T_CIERRA_TAPA, C.T_CIERRA_TAPA + 0.3))
    m = Matrix.Translation(Vector((0.55 * corre + 0.04 * mal, 0, 0.08 * sube + vib - 0.014 * mal))) @ TAPA
    if mal > 0:
        p = m.translation.copy()
        m = Matrix.Translation(p) @ Matrix.Rotation(math.radians(6 * mal), 4, 'Y') @ Matrix.Translation(-p) @ m
    return m


def cinta_final(t):
    if t < C.T_TOMA_CINTA: return cinta_en(t)
    mano = AP.matrix_world @ AP.pose.bones["Hand_R"].matrix
    return Matrix.Translation(mano.translation) @ mano.to_quaternion().to_matrix().to_4x4()


print("objetos...")
anim_objeto("Anim_Reloj", reloj_final)
anim_objeto("Anim_Rejilla", tapa)
anim_objeto("Anim_Puerta", puerta_en)
anim_objeto("Anim_Silla", silla_en)
anim_objeto("Anim_Cinta", cinta_final, evaluar=True)

# La cuerda: un cilindro delgado con el origen arriba; la escala en Z es lo que cuelga
bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=0.012, depth=1, location=(0, 0, -0.5))
cu = bpy.context.active_object; cu.name = "Cuerda"
bpy.ops.object.transform_apply(location=True)
cu.data.materials.append(material("Cuerda", (0.02, 0.02, 0.02)))
cu.rotation_mode = 'QUATERNION'
for f in CUADROS:
    t = T(f)
    baja = suave(rampa(t, C.T_CUERDA, C.T_CUERDA + 0.4)) * (1 - suave(rampa(t, C.T_RECOGE_CUERDA, C.T_RECOGE_CUERDA + 0.45)))
    cu.matrix_world = Matrix.Translation(cuerda_punto(C.TECHO + 0.05)) @ Matrix.Diagonal((1, 1, max(0.001, 3.0 * baja), 1))
    cu.keyframe_insert("location", frame=f); cu.keyframe_insert("scale", frame=f)

# En el .blend los objetos de la sala siguen a sus vacíos animados (para ver la escena completa)
for obj, vac in ((RELOJ, "Anim_Reloj"), (REJILLA, "Anim_Rejilla"), (PUERTA, "Anim_Puerta"), (CINTA, "Anim_Cinta"), (SILLA, "Anim_Silla")):
    c = obj.constraints.new('COPY_TRANSFORMS'); c.target = bpy.data.objects[vac]

# ======================================================================
# 8. Cámara (lente, enfoque y cortes secos entre tomas)
# ======================================================================
cam = bpy.data.objects.new("Camara", bpy.data.cameras.new("Camara"))
esc.collection.objects.link(cam)
esc.camera = cam
cam.data.sensor_fit = 'VERTICAL'
cam.data.clip_start = 0.02
cam.rotation_mode = 'QUATERNION'
cam.data.dof.use_dof = True
cam.data.dof.aperture_fstop = 2.0
inicios = [tm[0] for tm in C.TOMAS] + [C.FIN]
anim_reloj = bpy.data.objects["Anim_Reloj"]
for f in CUADROS:
    t = T(f)
    i = max(k for k, tm in enumerate(C.TOMAS) if tm[0] <= t)
    t0, d0, d1, m0, m1, fov = C.TOMAS[i]
    u = suave((t - t0) / (inicios[i + 1] - t0))
    if d0 is None:
        esc.frame_set(f)
        c = anim_reloj.matrix_world.translation
        desde, hacia = c + Vector((0.05, -0.12 - 0.02 * u, 0.5 - 0.05 * u)), c
    else:
        desde, hacia = gv(d0).lerp(gv(d1), u), gv(m0).lerp(gv(m1), u)
    golpe = math.exp(-(t - 26.5) / 0.12) if t >= 26.5 else 0.0
    desde = desde + Vector((math.sin(t * 0.7), 0, math.sin(t * 0.53 + 1.7))) * 0.005 + Vector((math.sin(t * 63), 0, math.sin(t * 51))) * 0.025 * golpe
    cam.location = desde
    cam.rotation_quaternion = (hacia - desde).to_track_quat('-Z', 'Y')
    cam.data.angle_y = math.radians(fov)
    cam.data.dof.focus_distance = (hacia - desde).length
    cam.keyframe_insert("location", frame=f); cam.keyframe_insert("rotation_quaternion", frame=f)
    cam.data.keyframe_insert("lens", frame=f)
    cam.data.dof.keyframe_insert("focus_distance", frame=f)

# ======================================================================
# 9. Revisión de choques
# ======================================================================
def revisar_choques(paso=2):
    """Vértices de Porky y de la figura dentro de la mesa, de las paredes, bajo el piso o (la figura) en el
    techo fuera de la rejilla. Devuelve [(t, quién, contra qué, vértices)]."""
    a, b = gv((-0.8, 1.255, -1.555)), gv((0.8, 1.285, -2.445))
    mesa = (Vector(map(min, a, b)), Vector(map(max, a, b)))
    mallas = {"Porky": [o for o in bpy.data.objects if o.type == 'MESH' and o.parent == AP],
              "figura": [o for o in bpy.data.objects if o.type == 'MESH' and o.parent == AF and o.name != "Figura_Rostro"]}
    hallados = []
    for f in range(esc.frame_start, esc.frame_end + 1, paso):
        t = T(f)
        esc.frame_set(f)
        dg = bpy.context.evaluated_depsgraph_get()
        for quien, objs in mallas.items():
            cuenta = {}
            for o in objs:
                ev = o.evaluated_get(dg)
                me = ev.to_mesh()
                mw = ev.matrix_world
                for i in range(0, len(me.vertices), 3):
                    p = mw @ me.vertices[i].co
                    gx, gy, gz = p.x, p.z, -p.y
                    if mesa[0].x < p.x < mesa[1].x and mesa[0].y < p.y < mesa[1].y and mesa[0].z < p.z < mesa[1].z:
                        cuenta["mesa"] = cuenta.get("mesa", 0) + 1
                    if gz < -5.0 or gx < -4.0 or gx > 4.0:
                        cuenta["pared"] = cuenta.get("pared", 0) + 1
                    if gy < PISO - 0.02 and gz < 3.0:
                        cuenta["piso"] = cuenta.get("piso", 0) + 1
                    if quien == "figura" and C.T_PIERNAS + 1.0 < t < C.T_SE_VA - 0.4 and gy > C.TECHO + 0.02 and not (abs(gx - C.REJILLA[0]) < 0.3 and abs(gz - C.REJILLA[2]) < 0.35):
                        cuenta["techo"] = cuenta.get("techo", 0) + 1
                ev.to_mesh_clear()
            for k, v in cuenta.items():
                if v >= 3: hallados.append((round(t, 2), quien, k, v))
    return hallados


# Errores de IK: cuánto le falta a la muñeca para llegar (con peso completo)
peores = {}
for t, h, e in ERRORES_IK:
    if e > peores.get(h, (0, 0))[1]: peores[h] = (t, e)
REPORTE["ik_peor"] = peores
REPORTE["ik_lejos"] = [x for x in ERRORES_IK if x[2] > 0.03]
print("IK: peor distancia por muñeca", peores)
lejos = [x for x in ERRORES_IK if x[2] > 0.03]
print("IK con más de 3 cm:", len(lejos), lejos[:20])

if "--choques" in ARGS:
    ch = revisar_choques()
    REPORTE["choques"] = ch
    print("CHOQUES:", ch if ch else "ninguno")
    with open(os.path.join(BASE, "reporte_choques.json"), "w", encoding="utf-8") as fh:
        json.dump(REPORTE, fh, ensure_ascii=False, indent=1)

# ======================================================================
# 10. Guardar y exportar
# ======================================================================
esc.frame_set(1)
# Fuera lo que solo servía para armar: el esqueleto de UAL2 y las acciones de Quaternius que ya no usa nadie
for idb in (AP, AF):
    if idb.animation_data:
        for pista in list(idb.animation_data.nla_tracks): idb.animation_data.nla_tracks.remove(pista)
for o in [o for o in bpy.data.objects if o == A2 or o.parent == A2]: bpy.data.objects.remove(o)
for accion in list(bpy.data.actions):
    if accion.users == 0: bpy.data.actions.remove(accion)
bpy.context.preferences.filepaths.save_version = 0      # sin caso0_cinta.blend1
bpy.context.preferences.filepaths.use_file_compression = True
bpy.ops.wm.save_as_mainfile(filepath=SALIDA_BLEND, compress=True)
print("guardado:", SALIDA_BLEND)

if EXPORTAR:
    os.makedirs(os.path.dirname(SALIDA_GLB), exist_ok=True)
    bpy.ops.object.select_all(action='DESELECT')
    exportar = [AP, AF, cam, cu] + [o for o in bpy.data.objects if o.name.startswith("Anim_")]
    exportar += [o for o in bpy.data.objects if o.parent in (AP, AF) and o.type == 'MESH']
    for o in exportar: o.hide_set(False); o.select_set(True)
    # Una sola animación "Cinta" en Godot: cada acción va a una pista NLA con ese nombre
    for idb in [o for o in exportar] + [cam.data]:
        ad = idb.animation_data
        if ad is None or ad.action is None: continue
        for vieja in list(ad.nla_tracks): ad.nla_tracks.remove(vieja)    # las acciones de Quaternius que trae el glb
        pista = ad.nla_tracks.new()
        pista.name = "Cinta"
        accion = ad.action
        pista.strips.new("Cinta", int(accion.frame_range[0]), accion)
        ad.action = None
    bpy.ops.export_scene.gltf(filepath=SALIDA_GLB, export_format='GLB', use_selection=True,
                              export_animation_mode='NLA_TRACKS', export_force_sampling=True, export_frame_range=True,
                              export_cameras=True, export_lights=False, export_apply=False, export_yup=True)
    print("exportado:", SALIDA_GLB)
