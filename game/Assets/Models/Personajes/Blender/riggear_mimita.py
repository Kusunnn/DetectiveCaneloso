# HU-9.5 CA-03 (N-10): le pone esqueleto a Mimita (miami.glb venía sin huesos y en pose T), le baja los brazos como
# pose de reposo y le hace sus animaciones. No toca la malla más que para bajar los brazos, ni las texturas.
# Los huesos se llaman como los que busca AnimacionPersonaje (Arm_1_L, Leg_1_L, Spine_2, Neck, Ear_1_L, Tail_1...).
# Uso: blender --background --factory-startup --python riggear_mimita.py -- <miami.glb> <salida.glb> [<prefijo de pruebas>]
import bmesh
import bpy
import math
import sys
from mathutils import Quaternion, Vector

FPS = 24
ANGULO_BRAZOS = 58.0  # Bajo la horizontal: la mano queda fuera de la falda


def huesos():
    # nombre, padre, cabeza, cola (Blender: el personaje mira hacia -Y; su izquierda es +X)
    lista = [
        ("Hips", None, (0, 0, 0.030), (0, 0, 0.040)),
        ("Spine_2", "Hips", (0, 0, 0.040), (0, 0, 0.050)),
        ("Spine_3", "Spine_2", (0, 0, 0.050), (0, 0, 0.060)),
        ("Neck", "Spine_3", (0, 0, 0.060), (0, 0, 0.066)),
        ("Head", "Neck", (0, 0, 0.066), (0, 0, 0.110)),
        ("Tail_1", "Hips", (0, 0.012, 0.034), (0, 0.028, 0.034)),
    ]
    for lado, s in (("L", 1), ("R", -1)):
        lista += [
            (f"Arm_1_{lado}", "Spine_3", (s * 0.0105, 0, 0.0575), (s * 0.0235, 0, 0.0575)),
            (f"Arm_2_{lado}", f"Arm_1_{lado}", (s * 0.0235, 0, 0.0575), (s * 0.0345, 0, 0.0575)),
            (f"Hand_{lado}", f"Arm_2_{lado}", (s * 0.0345, 0, 0.0575), (s * 0.0395, 0, 0.0575)),
            (f"Leg_1_{lado}", "Hips", (s * 0.0068, 0, 0.0290), (s * 0.0068, 0, 0.0150)),
            (f"Leg_2_{lado}", f"Leg_1_{lado}", (s * 0.0068, 0, 0.0150), (s * 0.0068, 0, 0.0005)),
            (f"Ear_1_{lado}", "Head", (s * 0.0160, 0.004, 0.1130), (s * 0.0165, 0.006, 0.1330)),
            (f"Ear_2_{lado}", f"Ear_1_{lado}", (s * 0.0165, 0.006, 0.1330), (s * 0.0170, 0.008, 0.1537)),
        ]
    return lista


def mezcla(valor, centro, ancho):
    # 0 antes de centro-ancho, 1 después de centro+ancho
    t = max(0.0, min(1.0, (valor - centro + ancho) / (2 * ancho)))
    return t * t * (3 - 2 * t)


def pesos_de(p, isla):
    # La malla ya viene separada por partes: cada isla pertenece a una parte del cuerpo
    minx, maxx, miny, maxy, minz, maxz = isla
    x, y, z = p
    lado = "L" if (minx + maxx) > 0 else "R"
    if maxz < 0.0325 and minz < 0.001:  # Pierna (desde la cadera hasta el pie)
        arriba = mezcla(z, 0.0150, 0.002)
        cadera = mezcla(z, 0.0270, 0.002)
        return {f"Leg_2_{lado}": 1 - arriba, f"Leg_1_{lado}": arriba * (1 - cadera), "Hips": arriba * cadera}
    if minz > 0.05 and maxz < 0.0635 and maxx - minx < 0.035:  # Brazo
        ax = abs(x)
        codo = mezcla(ax, 0.0235, 0.002)
        mano = mezcla(ax, 0.0345, 0.001)
        return {f"Arm_1_{lado}": 1 - codo, f"Arm_2_{lado}": codo * (1 - mano), f"Hand_{lado}": codo * mano}
    if minz > 0.11:  # Oreja
        punta = mezcla(z, 0.1330, 0.003)
        base = mezcla(z, 0.1165, 0.002)
        return {"Head": 1 - base, f"Ear_1_{lado}": base * (1 - punta), f"Ear_2_{lado}": base * punta}
    if minz > 0.06:  # Cabeza y pelo
        return {"Head": 1.0}
    if miny > 0.01:  # Cola
        return {"Tail_1": 1.0}
    # Cuerpo: blusa y falda
    a = mezcla(z, 0.0400, 0.003)
    b = mezcla(z, 0.0500, 0.003)
    c = mezcla(z, 0.0610, 0.002)
    return {"Hips": 1 - a, "Spine_2": a * (1 - b), "Spine_3": a * b * (1 - c), "Neck": a * b * c}


def pesar(obj):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bm.verts.ensure_lookup_table()
    grupos = {}
    visto = set()
    for inicial in bm.verts:
        if inicial.index in visto:
            continue
        pila = [inicial]
        visto.add(inicial.index)
        isla = []
        while pila:
            v = pila.pop()
            isla.append(v)
            for e in v.link_edges:
                o = e.other_vert(v)
                if o.index not in visto:
                    visto.add(o.index)
                    pila.append(o)
        c = [v.co for v in isla]
        limites = (min(p.x for p in c), max(p.x for p in c), min(p.y for p in c), max(p.y for p in c), min(p.z for p in c), max(p.z for p in c))
        for v in isla:
            for hueso, peso in pesos_de(v.co, limites).items():
                if peso <= 0.001:
                    continue
                grupo = grupos.get(hueso) or obj.vertex_groups.new(name=hueso)
                grupos[hueso] = grupo
                grupo.add([v.index], peso, "REPLACE")
    bm.free()


def rotar(pb, eje, grados):
    # Giro alrededor de un eje del espacio del esqueleto, pasado al espacio local del hueso en reposo
    reposo = pb.bone.matrix_local.to_quaternion()
    return reposo.inverted() @ Quaternion(Vector(eje), math.radians(grados)) @ reposo


def poner(arm, frame, giros, cadera=0.0):
    for pb in arm.pose.bones:
        q = Quaternion()
        for eje, grados in giros.get(pb.name, []):
            q = rotar(pb, eje, grados) @ q
        pb.rotation_mode = "QUATERNION"
        pb.rotation_quaternion = q
        pb.keyframe_insert("rotation_quaternion", frame=frame, group=pb.name)
    h = arm.pose.bones["Hips"]
    h.location = h.bone.matrix_local.to_quaternion().inverted() @ Vector((0, 0, cadera))
    h.keyframe_insert("location", frame=frame, group="Hips")


X, Y, Z = (1, 0, 0), (0, 1, 0), (0, 0, 1)


def reposo_base(t):
    # t en radianes: respira, mece los brazos y la cola
    r = math.sin(t)
    return {
        "Spine_2": [(X, 1.3 * r)],
        "Spine_3": [(X, 0.9 * r)],
        "Neck": [(X, -1.2 * r)],
        "Arm_1_L": [(X, 2.2 * r), (Y, -1.5 * r)],
        "Arm_1_R": [(X, 2.2 * r), (Y, 1.5 * r)],
        "Tail_1": [(Z, 8 * r)],
    }


def clip(arm, nombre, duracion, funcion, pasos=12):
    accion = bpy.data.actions.new(nombre)
    accion.use_fake_user = True
    arm.animation_data_create()
    arm.animation_data.action = accion
    for i in range(pasos + 1):
        u = i / pasos  # El último cuadro es igual al primero: el ciclo cierra
        giros, cadera = funcion(u)
        poner(arm, 1 + round(u * duracion * FPS), giros, cadera)
    arm.animation_data.action = None
    return accion


def clip_reposo(u):
    t = u * math.tau * 2
    g = reposo_base(t)
    # Un tic de orejas a medio ciclo
    tic = math.sin(max(0.0, min(1.0, (u - 0.62) / 0.1)) * math.pi)
    g["Ear_1_L"] = [(Y, 7 * tic)]
    g["Ear_1_R"] = [(Y, -7 * tic)]
    g["Head"] = [(Y, 1.6 * math.sin(u * math.tau))]
    return g, 0.00012 * math.sin(t)


def clip_mirar(u):
    # Mira hacia un lado, luego hacia el otro, y vuelve
    t = u * math.tau * 3
    g = reposo_base(t)
    lado = math.sin(u * math.tau)
    lado = math.copysign(abs(lado) ** 0.6, lado)
    g["Neck"] = g["Neck"] + [(Z, 24 * lado)]
    return g, 0.00012 * math.sin(t)


def clip_peso(u):
    # Carga el peso de una pierna a otra y se acomoda
    t = u * math.tau * 3
    g = reposo_base(t)
    lado = math.sin(u * math.tau)
    g["Hips"] = [(Y, 3.5 * lado)]
    g["Spine_2"] = g["Spine_2"] + [(Y, -2.5 * lado)]
    g["Neck"] = g["Neck"] + [(Y, -1.5 * lado)]
    g["Leg_1_L"] = [(Y, -3.5 * lado)]
    g["Leg_1_R"] = [(Y, -3.5 * lado)]
    return g, -0.0003 * abs(lado)


def clip_caminar(u):
    # Pasos cortos y rápidos: con piernas de 0.3 m, 4 pasos por segundo dan los 1.1 m/s de la sala
    f = u * math.tau
    s = math.sin(f)
    rodilla_i = max(0.0, math.sin(f + 1.2))
    rodilla_d = max(0.0, math.sin(f + 1.2 + math.pi))
    g = {
        "Leg_1_L": [(X, -30 * s)],
        "Leg_1_R": [(X, 30 * s)],
        "Leg_2_L": [(X, 28 * rodilla_i)],
        "Leg_2_R": [(X, 28 * rodilla_d)],
        "Arm_1_L": [(X, 24 * s)],
        "Arm_1_R": [(X, -24 * s)],
        "Hips": [(Z, 5 * s), (Y, 2 * math.sin(f * 2))],
        "Spine_3": [(Z, -5 * s), (X, -3)],
        "Neck": [(X, 3)],
        "Ear_1_L": [(X, 5 * math.cos(f * 2))],
        "Ear_1_R": [(X, 5 * math.cos(f * 2))],
        "Tail_1": [(Z, 14 * s)],
    }
    return g, 0.0009 * abs(math.cos(f))


def renders(arm, acciones, prefijo):
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.display.shading.light = "FLAT"
    sc.display.shading.color_type = "TEXTURE"
    sc.render.resolution_x = sc.render.resolution_y = 500
    cam = bpy.data.cameras.new("c")
    cam.type = "ORTHO"
    cam.ortho_scale = 0.17
    co = bpy.data.objects.new("c", cam)
    sc.collection.objects.link(co)
    sc.camera = co
    vistas = {"frente": ((0, -1, 0.078), (math.pi / 2, 0, 0)), "lado": ((1, 0, 0.078), (math.pi / 2, 0, math.pi / 2))}
    for nombre, cuadros, accion in (("reposo", [1], None), ("mirar", [37], "ReposoMirar"), ("caminar", [4, 10], "Caminar")):
        arm.animation_data.action = acciones[accion] if accion else None
        for c in cuadros:
            sc.frame_set(c)
            bpy.context.view_layer.update()
            for vista, (loc, rot) in vistas.items():
                co.location, co.rotation_euler = loc, rot
                sc.render.filepath = f"{prefijo}_{nombre}_{c:03d}_{vista}.png"
                bpy.ops.render.render(write_still=True)
    arm.animation_data.action = None


def main():
    args = sys.argv[sys.argv.index("--") + 1 :]
    entrada, salida = args[0], args[1]
    prueba = args[2] if len(args) > 2 else None
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=entrada)
    bpy.context.view_layer.update()
    mallas = [o for o in bpy.data.objects if o.type == "MESH"]
    for o in mallas:
        o.data.transform(o.matrix_world)
        o.parent = None
        o.matrix_world.identity()
    for o in [o for o in bpy.data.objects if o.type == "EMPTY"]:
        bpy.data.objects.remove(o)

    datos = bpy.data.armatures.new("Mimita")
    arm = bpy.data.objects.new("Mimita", datos)
    bpy.context.scene.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="EDIT")
    for nombre, padre, cabeza, cola in huesos():
        e = datos.edit_bones.new(nombre)
        e.head, e.tail = cabeza, cola
        e.use_deform = True
        e.parent = datos.edit_bones[padre] if padre else None
    bpy.ops.object.mode_set(mode="OBJECT")

    for o in mallas:
        pesar(o)
        o.parent = arm
        o.modifiers.new("Esqueleto", "ARMATURE").object = arm

    # Brazos abajo: la pose de reposo pasa a ser la de la malla, así nunca puede salir en pose T
    bpy.ops.object.mode_set(mode="POSE")
    for lado, s in (("L", 1), ("R", -1)):
        pb = arm.pose.bones[f"Arm_1_{lado}"]
        pb.rotation_mode = "QUATERNION"
        pb.rotation_quaternion = rotar(pb, Y, s * ANGULO_BRAZOS)
        codo = arm.pose.bones[f"Arm_2_{lado}"]
        codo.rotation_mode = "QUATERNION"
        codo.rotation_quaternion = rotar(codo, X, -12)
    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.context.view_layer.update()
    for o in mallas:
        bpy.ops.object.select_all(action="DESELECT")
        bpy.context.view_layer.objects.active = o
        o.select_set(True)
        bpy.ops.object.modifier_apply(modifier="Esqueleto")
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")
    bpy.ops.pose.armature_apply(selected=False)
    bpy.ops.object.mode_set(mode="OBJECT")
    for o in mallas:
        o.modifiers.new("Esqueleto", "ARMATURE").object = arm
    for pb in arm.pose.bones:
        pb.rotation_mode = "QUATERNION"
        pb.rotation_quaternion = Quaternion()

    bpy.context.scene.render.fps = FPS
    acciones = {
        "Reposo": clip(arm, "Reposo", 4.0, clip_reposo, 24),
        "ReposoMirar": clip(arm, "ReposoMirar", 6.0, clip_mirar, 36),
        "ReposoPeso": clip(arm, "ReposoPeso", 6.0, clip_peso, 36),
        "Caminar": clip(arm, "Caminar", 0.5, clip_caminar, 12),
    }

    if prueba:
        renders(arm, acciones, prueba)

    for nombre, accion in acciones.items():
        pista = arm.animation_data.nla_tracks.new()
        pista.name = nombre
        pista.strips.new(nombre, 1, accion)
    bpy.ops.object.select_all(action="DESELECT")
    bpy.ops.export_scene.gltf(
        filepath=salida,
        export_format="GLB",
        export_yup=True,
        export_skins=True,
        export_animations=True,
        export_animation_mode="NLA_TRACKS",
        export_force_sampling=True,
    )


main()
