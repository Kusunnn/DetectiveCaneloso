# Pasa las animaciones de Quaternius (UAL1/UAL2, CC0) a otro esqueleto, igual que AnimadorPersonaje en Godot:
# cada hueso común gira lo mismo que en el maniquí respecto a su pose T. Las curvas se leen directo de la
# acción (sin evaluar la escena), así que se puede pedir cualquier cuadro.
from mathutils import Matrix, Quaternion, Vector

H = ["Cadera", "Columna", "Pecho", "PechoAlto", "Cuello", "Cabeza", "HombroI", "BrazoI", "AntebrazoI", "ManoI",
     "HombroD", "BrazoD", "AntebrazoD", "ManoD", "MusloI", "PiernaI", "PieI", "DedosI", "MusloD", "PiernaD", "PieD", "DedosD"]
MAPAS = {
    "ual1": ["DEF-hips", "DEF-spine.001", "DEF-spine.002", "DEF-spine.003", "DEF-neck", "DEF-head",
             "DEF-shoulder.L", "DEF-upper_arm.L", "DEF-forearm.L", "DEF-hand.L", "DEF-shoulder.R", "DEF-upper_arm.R", "DEF-forearm.R", "DEF-hand.R",
             "DEF-thigh.L", "DEF-shin.L", "DEF-foot.L", "DEF-toe.L", "DEF-thigh.R", "DEF-shin.R", "DEF-foot.R", "DEF-toe.R"],
    "ual2": ["pelvis", "spine_01", "spine_02", "spine_03", "neck_01", "Head",
             "clavicle_l", "upperarm_l", "lowerarm_l", "hand_l", "clavicle_r", "upperarm_r", "lowerarm_r", "hand_r",
             "thigh_l", "calf_l", "foot_l", "ball_l", "thigh_r", "calf_r", "foot_r", "ball_r"],
    "animal": ["Skl_Root", "Spine_1", "Spine_2", "Spine_3", "Neck", "Head",
               "Clavicle_L", "Arm_1_L", "Arm_2_L", "Hand_L", "Clavicle_R", "Arm_1_R", "Arm_2_R", "Hand_R",
               "Leg_1_L", "Leg_2_L", "Ankle_L", "Toe_L", "Leg_1_R", "Leg_2_R", "Ankle_R", "Toe_R"],
}


def tipo(arm):
    nombres = arm.data.bones
    if "DEF-hips" in nombres: return "ual1"
    if "pelvis" in nombres: return "ual2"
    return "animal"


def orden(arm):
    "Huesos con los padres antes que los hijos"
    res, vistos = [], set()
    def poner(b):
        if b.name in vistos: return
        if b.parent: poner(b.parent)
        vistos.add(b.name); res.append(b)
    for b in arm.data.bones: poner(b)
    return res


class Esqueleto:
    def __init__(self, arm):
        self.arm = arm
        self.tipo = tipo(arm)
        self.orden = orden(arm)
        self.reposo = {b.name: b.matrix_local.copy() for b in self.orden}          # espacio de la armadura
        self.mapa = {h: MAPAS[self.tipo][i] for i, h in enumerate(H) if MAPAS[self.tipo][i] in arm.data.bones}
        self._curvas = {}
        ej = self.ejes()
        self.cadera = self.reposo[self.mapa["Cadera"]].translation.copy()
        pies = (self.reposo[self.mapa["PieI"]].translation + self.reposo[self.mapa["PieD"]].translation) / 2
        self.alto_cadera = (self.cadera - pies).dot(ej[1])

    def ejes(self):
        r = self.reposo; m = self.mapa
        izq = r[m["MusloI"]].translation - r[m["MusloD"]].translation
        arriba = (r[m["Cabeza"]].translation - r[m["Cadera"]].translation).normalized()
        izq = (izq - arriba * izq.dot(arriba)).normalized()
        return izq, arriba, izq.cross(arriba)

    def curvas(self, accion):
        if accion.name in self._curvas: return self._curvas[accion.name]
        bolsa = accion.layers[0].strips[0].channelbag(accion.slots[0])
        por = {}
        for fc in bolsa.fcurves:
            ruta = fc.data_path
            if not ruta.startswith('pose.bones["'): continue
            hueso = ruta.split('"')[1]
            prop = ruta.rsplit('.', 1)[1]
            por.setdefault(hueso, {}).setdefault(prop, {})[fc.array_index] = fc
        self._curvas[accion.name] = por
        return por

    def pose(self, accion, cuadro):
        "Matrices de pose (espacio de la armadura) de cada hueso en ese cuadro de la acción"
        cur = self.curvas(accion)
        res = {}
        for b in self.orden:
            c = cur.get(b.name, {})
            loc = Vector([c["location"][i].evaluate(cuadro) if "location" in c and i in c["location"] else 0.0 for i in range(3)])
            if "rotation_quaternion" in c:
                q = Quaternion([c["rotation_quaternion"][i].evaluate(cuadro) if i in c["rotation_quaternion"] else (1.0 if i == 0 else 0.0) for i in range(4)]).normalized()
            else:
                q = Quaternion()
            base = Matrix.Translation(loc) @ q.to_matrix().to_4x4()
            if b.parent:
                rel = self.reposo[b.parent.name].inverted() @ self.reposo[b.name]
                res[b.name] = res[b.parent.name] @ rel @ base
            else:
                res[b.name] = self.reposo[b.name] @ base
        return res


def retarget(fuente, pose_fuente, destino, extra=None, wf=Quaternion(), wd=Quaternion()):
    """Pose del destino (matrices en su espacio de armadura) a partir de la pose de la fuente.
    wf y wd: giro de cada armadura en su pose de importación (las dos de glTF miran igual: identidad).
    No se usa matrix_world porque la armadura del destino gira con su recorrido."""
    escala = destino.alto_cadera / fuente.alto_cadera
    res = {}
    for b in destino.orden:
        rel = destino.reposo[b.parent.name].inverted() @ destino.reposo[b.name] if b.parent else destino.reposo[b.name]
        padre = res[b.parent.name] if b.parent else Matrix.Identity(4)
        mat = padre @ rel
        h = next((k for k, v in destino.mapa.items() if v == b.name), None)
        if h and h in fuente.mapa:
            nf = fuente.mapa[h]
            delta = (wf @ pose_fuente[nf].to_quaternion()) @ (wf @ fuente.reposo[nf].to_quaternion()).inverted()
            giro = wd.inverted() @ delta @ wd @ destino.reposo[b.name].to_quaternion()
            pos = mat.translation
            if h == "Cadera":
                mov = pose_fuente[nf].translation - fuente.reposo[nf].translation
                mov = wd.inverted() @ (wf @ mov) * escala
                pos = destino.reposo[b.name].translation + mov
            mat = Matrix.Translation(pos) @ giro.to_matrix().to_4x4()
        if extra and b.name in extra:
            mat = extra[b.name](mat)
        res[b.name] = mat
    return res


def aplicar(destino, pose, cuadro=None):
    "Pone la pose en los huesos (matrix_basis) y, si se da cuadro, la guarda como keyframe"
    for b in destino.orden:
        pb = destino.arm.pose.bones[b.name]
        rel = destino.reposo[b.parent.name].inverted() @ destino.reposo[b.name] if b.parent else destino.reposo[b.name]
        padre = pose[b.parent.name] if b.parent else Matrix.Identity(4)
        base = (padre @ rel).inverted() @ pose[b.name]
        pb.rotation_mode = 'QUATERNION'
        pb.matrix_basis = base
        if cuadro is not None:
            pb.keyframe_insert("rotation_quaternion", frame=cuadro)
            pb.keyframe_insert("location", frame=cuadro)


def girar_rama(destino, pose, hueso, giro):
    "Gira un hueso y todos sus hijos alrededor de la cabeza del hueso (giro en espacio de la armadura)"
    p = pose[hueso].translation.copy()
    m = Matrix.Translation(p) @ giro.to_matrix().to_4x4() @ Matrix.Translation(-p)
    raiz = destino.arm.data.bones[hueso]
    for b in [raiz] + list(raiz.children_recursive):
        pose[b.name] = m @ pose[b.name]


def mezclar(a, b, u):
    "Mezcla dos poses (u = 0 → a, 1 → b)"
    res = {}
    for n in a:
        la, lb = a[n].translation, b[n].translation
        qa, qb = a[n].to_quaternion(), b[n].to_quaternion()
        res[n] = Matrix.Translation(la.lerp(lb, u)) @ qa.slerp(qb, u).to_matrix().to_4x4()
    return res
