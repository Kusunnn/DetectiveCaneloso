# Objetos del Capítulo 2 que no hay en las librerías (Kenney, KayKit, Quaternius, Poly Haven). Low-poly estilizado.
# Pivote en la base (o atrás, en lo que va colgado de la pared: la cara mira a +Z en Godot).
# Uso: blender --background --factory-startup --python props_caso2.py -- <carpeta_salida> [nombre ...]
import bpy, bmesh, sys, os, math
from mathutils import Vector, Matrix

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
SALIDA = args[0] if args else os.path.join(os.path.dirname(__file__), "..", "Modelos", "props")
SOLO = set(args[1:])
os.makedirs(SALIDA, exist_ok=True)

_mats = {}
def limpiar():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    _mats.clear()

def mat(nombre, color, rug=0.7, metal=0.0, emision=0.0):
    if nombre in _mats: return _mats[nombre]
    m = bpy.data.materials.new(nombre)
    m.use_nodes = True
    p = m.node_tree.nodes["Principled BSDF"]
    p.inputs["Base Color"].default_value = (*color, 1)
    p.inputs["Roughness"].default_value = rug
    p.inputs["Metallic"].default_value = metal
    if emision > 0:
        p.inputs["Emission Color"].default_value = (*color, 1)
        p.inputs["Emission Strength"].default_value = emision
    _mats[nombre] = m
    return m

# Coordenadas de Godot (x, y arriba, z hacia el frente) → Blender (x, -z, y)
def gb(x, y, z): return Vector((x, -z, y))

class Malla:
    def __init__(self): self.bm = bmesh.new(); self.mats = []
    def _mi(self, m):
        if m not in self.mats: self.mats.append(m)
        return self.mats.index(m)
    def caja(self, c, t, m, bisel=0.0, rot=None):
        """Caja centrada en c (Godot) de tamaño t; rot = (eje, grados) opcional, sobre su centro."""
        geo = bmesh.ops.create_cube(self.bm, size=1.0)
        vs = geo["verts"]
        bmesh.ops.scale(self.bm, vec=Vector((t[0], t[2], t[1])), verts=vs)
        if rot:
            eje = {"x": "X", "y": "Z", "z": "Y"}[rot[0]]
            ang = math.radians(rot[1]) * (-1 if rot[0] == "z" else 1)
            bmesh.ops.rotate(self.bm, verts=vs, cent=Vector((0, 0, 0)), matrix=Matrix.Rotation(ang, 3, eje))
        bmesh.ops.translate(self.bm, vec=gb(*c), verts=vs)
        caras = list({f for v in vs for f in v.link_faces})
        for f in caras: f.material_index = self._mi(m)
        if bisel > 0:
            bmesh.ops.bevel(self.bm, geom=list({e for f in caras for e in f.edges}), offset=bisel, segments=2, affect="EDGES")
        return vs
    def cilindro(self, c, r, alto, m, lados=16, eje="y", r2=None):
        geo = bmesh.ops.create_cone(self.bm, cap_ends=True, segments=lados, radius1=r, radius2=r if r2 is None else r2, depth=alto)
        vs = geo["verts"]
        if eje == "z":   # a lo largo de Z de Godot (hacia el frente)
            bmesh.ops.rotate(self.bm, verts=vs, cent=Vector((0, 0, 0)), matrix=Matrix.Rotation(math.radians(90), 3, "X"))
        elif eje == "x":
            bmesh.ops.rotate(self.bm, verts=vs, cent=Vector((0, 0, 0)), matrix=Matrix.Rotation(math.radians(90), 3, "Y"))
        bmesh.ops.translate(self.bm, vec=gb(*c), verts=vs)
        for f in {f for v in vs for f in v.link_faces}: f.material_index = self._mi(m)
        return vs
    def esfera(self, c, r, m, seg=12, escala=(1, 1, 1)):
        geo = bmesh.ops.create_uvsphere(self.bm, u_segments=seg, v_segments=max(6, seg // 2), radius=r)
        vs = geo["verts"]
        bmesh.ops.scale(self.bm, vec=Vector((escala[0], escala[2], escala[1])), verts=vs)
        bmesh.ops.translate(self.bm, vec=gb(*c), verts=vs)
        for f in {f for v in vs for f in v.link_faces}: f.material_index = self._mi(m)
        return vs
    def objeto(self, nombre):
        malla = bpy.data.meshes.new(nombre)
        bmesh.ops.recalc_face_normals(self.bm, faces=self.bm.faces)
        self.bm.to_mesh(malla)
        self.bm.free()
        for m in self.mats: malla.materials.append(m)
        ob = bpy.data.objects.new(nombre, malla)
        bpy.context.scene.collection.objects.link(ob)
        bpy.context.view_layer.objects.active = ob
        ob.select_set(True)
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.uv.cube_project(cube_size=1.0)
        bpy.ops.object.mode_set(mode="OBJECT")
        return ob

def exportar(nombre):
    bpy.ops.export_scene.gltf(filepath=os.path.join(SALIDA, nombre + ".glb"), export_format="GLB", export_yup=True, export_apply=True)
    print("exportado", nombre)

PIEZAS = {}
def pieza(f):
    PIEZAS[f.__name__] = f
    return f

@pieza
def corcho():
    """Pizarrón de corcho de 1.0 × 0.7 m con marco, recortes, fotos y chinches (colgado: atrás en z = 0)."""
    m = Malla()
    marco = mat("MarcoCorcho", (0.42, 0.27, 0.15), 0.6)
    corcho_m = mat("Corcho", (0.66, 0.47, 0.30), 0.95)
    m.caja((0, 0.35, 0.012), (1.0, 0.7, 0.024), corcho_m)
    for c, t in (((0, 0.69, 0.02), (1.04, 0.04, 0.04)), ((0, 0.01, 0.02), (1.04, 0.04, 0.04)),
                 ((-0.5, 0.35, 0.02), (0.04, 0.7, 0.04)), ((0.5, 0.35, 0.02), (0.04, 0.7, 0.04))):
        m.caja(c, t, marco, 0.005)
    papeles = [((-0.3, 0.45), (0.2, 0.26), (0.95, 0.93, 0.86), 4), ((-0.05, 0.5), (0.16, 0.12), (0.92, 0.88, 0.6), -6),
               ((0.22, 0.42), (0.22, 0.3), (0.96, 0.95, 0.92), 3), ((-0.2, 0.17), (0.25, 0.16), (0.85, 0.9, 0.95), -2),
               ((0.12, 0.13), (0.12, 0.12), (0.95, 0.75, 0.75), 8), ((0.36, 0.17), (0.14, 0.1), (0.3, 0.32, 0.36), -5)]
    for i, (p, t, col, ang) in enumerate(papeles):
        m.caja((p[0], p[1], 0.026 + i * 0.0006), (t[0], t[1], 0.0012), mat(f"Papel{i}", col, 0.9), 0, ("z", ang))
        m.esfera((p[0], p[1] + t[1] / 2 - 0.02, 0.032), 0.008, mat("Chinche", (0.8, 0.15, 0.12), 0.4), 8)
    # Hilo rojo entre dos papeles
    m.caja((-0.12, 0.47, 0.034), (0.33, 0.003, 0.003), mat("Hilo", (0.75, 0.1, 0.08), 0.8), 0, ("z", -10))
    m.objeto("corcho")
    exportar("corcho")

@pieza
def reloj_pared():
    """Reloj redondo de pared (diámetro 0.32 m), las manecillas a las 10:10 de la mañana."""
    m = Malla()
    m.cilindro((0, 0, 0.02), 0.16, 0.04, mat("CajaReloj", (0.36, 0.22, 0.12), 0.5), 32, "z")
    m.cilindro((0, 0, 0.041), 0.145, 0.002, mat("Caratula", (0.96, 0.93, 0.85), 0.6), 32, "z")
    negro = mat("Manecillas", (0.08, 0.07, 0.07), 0.5)
    for h in range(12):
        a = math.radians(h * 30)
        m.caja((math.sin(a) * 0.125, math.cos(a) * 0.125, 0.043), (0.008, 0.02 if h % 3 else 0.03, 0.002), negro, 0, ("z", -h * 30))
    for ang, largo, ancho in ((-60 + 5, 0.07, 0.01), (60, 0.11, 0.007)):
        a = math.radians(ang)
        m.caja((math.sin(a) * largo / 2, math.cos(a) * largo / 2, 0.046), (ancho, largo, 0.002), negro, 0, ("z", -ang))
    m.cilindro((0, 0, 0.047), 0.008, 0.004, negro, 12, "z")
    m.objeto("reloj_pared")
    exportar("reloj_pared")

@pieza
def vapor():
    """Columna de vapor estilizada (tres volutas) para el café: va con un material transparente en Godot."""
    m = Malla()
    v = mat("Vapor", (1, 1, 1), 1.0)
    for i in range(3):
        for k in range(5):
            t = k / 4
            m.esfera((math.sin(t * 5 + i * 2) * 0.012, 0.02 + t * 0.12 + i * 0.01, math.cos(t * 4 + i) * 0.008), 0.012 * (1 - t * 0.5), v, 8)
    m.objeto("vapor")
    exportar("vapor")

MARMOL = lambda: mat("Marmol", (0.91, 0.87, 0.80), 0.18)
NOGAL = lambda: mat("Nogal", (0.36, 0.22, 0.13), 0.5)
DORADO = lambda: mat("Dorado", (0.85, 0.65, 0.28), 0.25, 1.0)
ACERO = lambda: mat("Acero", (0.62, 0.64, 0.66), 0.3, 1.0)

@pieza
def mostrador():
    """Mostrador de recepción de 3.2 × 1.1 m: frente de nogal con listones, cubierta de mármol y repisa interior."""
    m = Malla()
    m.caja((0, 0.53, 0), (3.2, 1.06, 0.7), NOGAL(), 0.01)                     # cuerpo
    for i in range(9):                                                          # listones del frente
        x = -1.4 + i * 0.35
        m.caja((x, 0.53, 0.36), (0.06, 0.9, 0.03), mat("NogalClaro", (0.48, 0.31, 0.18), 0.5), 0.006)
    m.caja((0, 1.08, 0.05), (3.32, 0.05, 0.86), MARMOL(), 0.008)               # cubierta
    m.caja((0, 0.1, 0.37), (3.2, 0.08, 0.04), DORADO(), 0.004)                 # filo dorado abajo
    m.caja((0, 0.76, -0.45), (3.0, 0.04, 0.4), MARMOL(), 0.006)                # repisa de trabajo (del lado de dentro)
    m.objeto("mostrador")
    exportar("mostrador")

@pieza
def logo_torre():
    """Logo dorado de la Torre Miawrio para la pared: aro con una «M» de gato (orejas). Colgado: atrás en z = 0."""
    m = Malla()
    oro = DORADO()
    for k in range(36):
        a = math.radians(k * 10)
        m.caja((math.cos(a) * 0.55, 0.6 + math.sin(a) * 0.55, 0.03), (0.1, 0.06, 0.06), oro, 0, ("z", k * 10 + 90))
    # «M»: dos patas, dos diagonales y las orejas
    m.caja((-0.25, 0.55, 0.04), (0.08, 0.5, 0.06), oro)
    m.caja((0.25, 0.55, 0.04), (0.08, 0.5, 0.06), oro)
    m.caja((-0.12, 0.62, 0.04), (0.07, 0.36, 0.06), oro, 0, ("z", 30))
    m.caja((0.12, 0.62, 0.04), (0.07, 0.36, 0.06), oro, 0, ("z", -30))
    m.caja((-0.27, 0.86, 0.04), (0.07, 0.16, 0.06), oro, 0, ("z", 20))
    m.caja((0.27, 0.86, 0.04), (0.07, 0.16, 0.06), oro, 0, ("z", -20))
    m.objeto("logo_torre")
    exportar("logo_torre")

@pieza
def elevador():
    """Puertas de elevador en su marco de acero (para un hueco de muro de 1 m): hojas cerradas e indicador de piso
    encima con su pantallita (el número lo pone la escena). Cara hacia +Z, atrás en z = 0."""
    m = Malla()
    acero = ACERO()
    m.caja((-0.47, 1.1, 0.02), (0.06, 2.2, 0.06), acero, 0.008)
    m.caja((0.47, 1.1, 0.02), (0.06, 2.2, 0.06), acero, 0.008)
    m.caja((0, 2.18, 0.02), (1.0, 0.08, 0.06), acero, 0.008)
    hoja = mat("HojaElevador", (0.72, 0.70, 0.66), 0.25, 0.9)
    m.caja((-0.22, 1.07, 0.0), (0.43, 2.12, 0.03), hoja, 0.004)
    m.caja((0.22, 1.07, 0.0), (0.43, 2.12, 0.03), hoja, 0.004)
    m.caja((0, 2.45, 0.02), (0.36, 0.16, 0.04), mat("Indicador", (0.1, 0.1, 0.11), 0.3), 0.01)
    m.caja((0, 2.45, 0.041), (0.26, 0.1, 0.002), mat("PantallaPiso", (0.95, 0.55, 0.2), 0.3, 0, 3.0))
    # Botonera a un lado
    m.caja((0.62, 1.1, 0.01), (0.1, 0.24, 0.02), acero, 0.004)
    m.cilindro((0.62, 1.15, 0.022), 0.018, 0.008, mat("Boton", (0.95, 0.85, 0.6), 0.3, 0, 1.5), 12, "z")
    m.cilindro((0.62, 1.05, 0.022), 0.018, 0.008, mat("Boton", (0.95, 0.85, 0.6), 0.3, 0, 1.5), 12, "z")
    m.objeto("elevador")
    exportar("elevador")

@pieza
def maceta_grande():
    """Maceta alta de cerámica con una planta de hojas grandes (monstera estilizada), 1.7 m."""
    m = Malla()
    m.cilindro((0, 0.3, 0), 0.26, 0.6, mat("Ceramica", (0.88, 0.85, 0.78), 0.4), 20, "y", 0.2)
    m.cilindro((0, 0.6, 0), 0.24, 0.03, mat("Tierra", (0.25, 0.17, 0.1), 0.95), 20)
    verde = mat("Hoja", (0.22, 0.48, 0.28), 0.6)
    tallo = mat("Tallo", (0.3, 0.45, 0.2), 0.7)
    for k in range(9):
        a = math.radians(k * 40 + 10)
        alto = 0.9 + (k % 3) * 0.25
        r = 0.28 + (k % 2) * 0.12
        x, z = math.cos(a) * r, math.sin(a) * r
        m.caja((x * 0.5, 0.6 + alto / 2, z * 0.5), (0.02, alto, 0.02), tallo, 0, ("z", math.degrees(math.atan2(x, 1.0)) * 0.6))
        m.esfera((x, 0.6 + alto, z), 0.2, verde, 10, (1.2, 0.18, 0.8))
    m.objeto("maceta_grande")
    exportar("maceta_grande")

@pieza
def marco_pantalla():
    """Marco negro de pantalla plana de 1.7 × 1.0 m colgado de la pared (la imagen la pone la escena delante)."""
    m = Malla()
    negro = mat("Bisel", (0.05, 0.05, 0.06), 0.35)
    m.caja((0, 0, 0.025), (1.7, 1.0, 0.05), negro, 0.01)
    m.caja((0, -0.48, 0.055), (0.12, 0.02, 0.01), mat("Logo", (0.6, 0.6, 0.62), 0.3, 1.0))
    m.objeto("marco_pantalla")
    exportar("marco_pantalla")

for nombre, f in PIEZAS.items():
    if SOLO and nombre not in SOLO: continue
    limpiar()
    f()
print("PROPS LISTOS")
