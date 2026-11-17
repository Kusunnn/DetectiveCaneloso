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

# ---------- Despacho del piso 30 ----------

CUERO = lambda: mat("Cuero", (0.18, 0.32, 0.25), 0.55)

@pieza
def escritorio_ejecutivo():
    """Escritorio de nogal de 2.2 × 1.0 m, cubierta con cuero verde y cajoneras a los lados. El cajón de abajo a la
    derecha (lado de Miawrio) va aparte: escritorio_cajon. El frente (lado del visitante) mira a +Z."""
    m = Malla()
    m.caja((0, 0.74, 0), (2.2, 0.05, 1.0), NOGAL(), 0.01)
    m.caja((0, 0.769, 0), (1.9, 0.004, 0.75), CUERO())
    for x in (-0.82, 0.82):
        m.caja((x, 0.36, 0), (0.5, 0.72, 0.92), NOGAL(), 0.008)
    m.caja((0, 0.45, 0.43), (1.14, 0.55, 0.03), mat("NogalClaro", (0.48, 0.31, 0.18), 0.5), 0.006)   # faldón
    # Frentes de cajones fijos (lado de Miawrio, -Z) y jaladeras doradas
    for x in (-0.82, 0.82):
        for y in (0.58, 0.38, 0.16):
            if x > 0 and y < 0.2: continue   # hueco del cajón que se abre
            m.caja((x, y, -0.465), (0.44, 0.17, 0.02), mat("NogalClaro", (0.48, 0.31, 0.18), 0.5), 0.005)
            m.caja((x, y, -0.48), (0.12, 0.015, 0.012), DORADO(), 0.002)
    m.caja((0.82, 0.16, -0.2), (0.44, 0.17, 0.5), mat("Hueco", (0.12, 0.08, 0.05), 0.9))
    m.objeto("escritorio_ejecutivo")
    exportar("escritorio_ejecutivo")

@pieza
def escritorio_cajon():
    """Cajón del escritorio ejecutivo (se desliza hacia -Z). Pivote en el centro de su frente, abajo."""
    m = Malla()
    m.caja((0, 0.085, -0.008), (0.44, 0.17, 0.02), mat("NogalClaro", (0.48, 0.31, 0.18), 0.5), 0.005)
    m.caja((0, 0.085, -0.025), (0.12, 0.015, 0.012), DORADO(), 0.002)
    m.caja((0, 0.02, 0.24), (0.4, 0.012, 0.46), mat("FondoCajon", (0.55, 0.4, 0.26), 0.8))
    for x in (-0.195, 0.195):
        m.caja((x, 0.08, 0.24), (0.012, 0.13, 0.46), mat("FondoCajon", (0.55, 0.4, 0.26), 0.8))
    m.caja((0, 0.08, 0.465), (0.4, 0.13, 0.012), mat("FondoCajon", (0.55, 0.4, 0.26), 0.8))
    m.objeto("escritorio_cajon")
    exportar("escritorio_cajon")

@pieza
def archivero_cuerpo():
    """Archivero de nogal de tres cajones (0.5 × 1.05 × 0.6); el de en medio va aparte (archivero_cajon).
    Frente a +Z."""
    m = Malla()
    m.caja((0, 0.525, 0), (0.5, 1.05, 0.6), NOGAL(), 0.008)
    claro = mat("NogalClaro", (0.48, 0.31, 0.18), 0.5)
    for y in (0.85, 0.2):
        m.caja((0, y, 0.305), (0.44, 0.3, 0.02), claro, 0.005)
        m.caja((0, y + 0.08, 0.32), (0.16, 0.02, 0.015), DORADO(), 0.002)
        m.caja((0, y + 0.03, 0.317), (0.1, 0.04, 0.004), mat("Etiqueta", (0.95, 0.92, 0.82), 0.9))
    m.caja((0, 0.525, 0.1), (0.44, 0.3, 0.4), mat("Hueco", (0.12, 0.08, 0.05), 0.9))
    m.objeto("archivero_cuerpo")
    exportar("archivero_cuerpo")

@pieza
def archivero_cajon():
    """Cajón del archivero (sale hacia +Z) con carpetas colgantes. Pivote en el centro de su frente, abajo."""
    m = Malla()
    claro = mat("NogalClaro", (0.48, 0.31, 0.18), 0.5)
    m.caja((0, 0.15, 0.01), (0.44, 0.3, 0.02), claro, 0.005)
    m.caja((0, 0.23, 0.025), (0.16, 0.02, 0.015), DORADO(), 0.002)
    m.caja((0, 0.18, 0.022), (0.1, 0.04, 0.004), mat("Etiqueta", (0.95, 0.92, 0.82), 0.9))
    fondo = mat("FondoCajon", (0.55, 0.4, 0.26), 0.8)
    m.caja((0, 0.02, -0.2), (0.4, 0.012, 0.4), fondo)
    for x in (-0.195, 0.195):
        m.caja((x, 0.12, -0.2), (0.012, 0.22, 0.4), fondo)
    m.caja((0, 0.12, -0.395), (0.4, 0.22, 0.012), fondo)
    colores = [(0.82, 0.68, 0.42), (0.55, 0.7, 0.55), (0.82, 0.68, 0.42), (0.6, 0.65, 0.8), (0.82, 0.68, 0.42)]
    for i, col in enumerate(colores):
        m.caja((0, 0.13, -0.33 + i * 0.05), (0.36, 0.2, 0.006), mat(f"Carpeta{i}", col, 0.9))
    m.objeto("archivero_cajon")
    exportar("archivero_cajon")

@pieza
def maqueta_hotel():
    """Maqueta del hotel junto al lago sobre una base de 0.9 × 0.6 m: agua, orilla, pinos, el hotel y el muelle."""
    m = Malla()
    m.caja((0, 0.03, 0), (0.9, 0.06, 0.6), mat("BaseMaqueta", (0.94, 0.92, 0.86), 0.6), 0.004)
    m.caja((-0.18, 0.062, 0.05), (0.5, 0.004, 0.45), mat("AguaMaqueta", (0.35, 0.6, 0.75), 0.15))
    pasto = mat("PastoMaqueta", (0.55, 0.7, 0.45), 0.8)
    m.caja((0.27, 0.064, 0), (0.34, 0.008, 0.58), pasto)
    blanco = mat("Hotel", (0.97, 0.95, 0.9), 0.5)
    m.caja((0.27, 0.12, -0.08), (0.26, 0.1, 0.16), blanco, 0.003)
    m.caja((0.27, 0.19, -0.1), (0.2, 0.05, 0.12), blanco, 0.003)
    vidrio = mat("VentanaMaqueta", (0.4, 0.55, 0.65), 0.2)
    for i in range(5):
        m.caja((0.17 + i * 0.05, 0.13, 0.002), (0.025, 0.03, 0.002), vidrio)
    m.caja((0.27, 0.235, -0.1), (0.04, 0.03, 0.04), mat("Cruz", (0.85, 0.25, 0.2), 0.5))   # la clínica en la azotea
    m.caja((0.0, 0.068, 0.12), (0.18, 0.006, 0.03), mat("Muelle", (0.6, 0.45, 0.3), 0.7))
    pino = mat("PinoMaqueta", (0.2, 0.42, 0.28), 0.8)
    for (x, z) in ((0.38, 0.2), (0.42, 0.12), (0.12, -0.24), (0.4, -0.24), (0.33, 0.24), (0.15, 0.22)):
        m.cilindro((x, 0.1, z), 0.025, 0.08, pino, 8, "y", 0.0)
    m.objeto("maqueta_hotel")
    exportar("maqueta_hotel")

@pieza
def torre_gato():
    """Torre para gato elegante de 1.5 m: base, postes con cuerda, dos plataformas con cojín y una casita."""
    m = Malla()
    alfombra = mat("Alfombrado", (0.82, 0.76, 0.66), 0.95)
    cuerda = mat("Sisal", (0.72, 0.6, 0.42), 0.95)
    m.caja((0, 0.03, 0), (0.6, 0.06, 0.6), alfombra, 0.01)
    m.cilindro((-0.15, 0.55, -0.1), 0.05, 1.0, cuerda, 12)
    m.cilindro((0.18, 0.35, 0.12), 0.05, 0.6, cuerda, 12)
    m.caja((0.12, 0.68, 0.05), (0.42, 0.04, 0.42), alfombra, 0.01)
    m.caja((0.12, 0.72, 0.05), (0.34, 0.04, 0.34), mat("CojinGato", (0.55, 0.2, 0.25), 0.9), 0.015)
    m.caja((-0.12, 1.08, -0.08), (0.4, 0.04, 0.4), alfombra, 0.01)
    m.caja((-0.12, 1.25, -0.08), (0.36, 0.3, 0.36), alfombra, 0.02)
    m.cilindro((-0.12, 1.24, 0.1), 0.09, 0.02, mat("Hueco", (0.12, 0.08, 0.05), 0.9), 16, "z")
    m.esfera((0.3, 0.75, 0.25), 0.03, mat("Bolita", (0.85, 0.65, 0.28), 0.4), 8)
    m.objeto("torre_gato")
    exportar("torre_gato")

@pieza
def pecera():
    """Pecera de 0.8 × 0.45 × 0.4 m sobre su mueble, con agua, piedras, plantas y peces de colores."""
    m = Malla()
    m.caja((0, 0.4, 0), (0.85, 0.8, 0.45), NOGAL(), 0.008)
    m.caja((0, 1.03, 0), (0.8, 0.45, 0.4), mat("VidrioPecera", (0.75, 0.88, 0.92), 0.05))
    m.caja((0, 1.0, 0), (0.76, 0.36, 0.36), mat("Agua", (0.25, 0.55, 0.65), 0.1, 0, 0.4))
    m.caja((0, 0.83, 0), (0.76, 0.03, 0.36), mat("Grava", (0.85, 0.78, 0.6), 0.9))
    verde = mat("Alga", (0.2, 0.6, 0.35), 0.6)
    for x in (-0.28, -0.1, 0.22):
        m.caja((x, 0.95, -0.08), (0.03, 0.22, 0.03), verde, 0, ("z", 10))
    for i, (x, y, col) in enumerate(((-0.15, 1.05, (1.0, 0.5, 0.15)), (0.1, 0.98, (1.0, 0.8, 0.2)), (0.25, 1.1, (0.95, 0.3, 0.25)))):
        m.esfera((x, y, 0.02), 0.03, mat(f"Pez{i}", col, 0.4, 0, 0.3), 8, (1.6, 0.9, 0.6))
    m.caja((0, 1.27, 0), (0.82, 0.03, 0.42), mat("TapaPecera", (0.1, 0.1, 0.1), 0.4), 0.004)
    m.objeto("pecera")
    exportar("pecera")

@pieza
def tripie_camara():
    """Tripié de fotógrafo (1.5 m) con una cámara réflex encima."""
    m = Malla()
    negro = mat("Negro", (0.06, 0.06, 0.07), 0.4)
    for k in range(3):
        a = math.radians(k * 120)
        x, z = math.cos(a) * 0.28, math.sin(a) * 0.28
        m.caja((x / 2, 0.66, z / 2), (0.022, 1.36, 0.022), negro, 0, ("z", math.degrees(math.atan2(x, 1.3))))
    m.cilindro((0, 1.38, 0), 0.03, 0.1, negro, 10)
    m.caja((0, 1.48, 0), (0.14, 0.09, 0.07), negro, 0.01)
    m.cilindro((0, 1.475, 0.07), 0.034, 0.08, mat("Lente", (0.12, 0.12, 0.13), 0.3), 16, "z")
    m.cilindro((0, 1.475, 0.111), 0.026, 0.004, mat("Cristal", (0.3, 0.4, 0.55), 0.05, 0, 0.3), 16, "z")
    m.caja((-0.045, 1.535, 0), (0.03, 0.02, 0.04), negro)
    m.objeto("tripie_camara")
    exportar("tripie_camara")

@pieza
def camara_mano():
    """Cámara réflex para llevar en la mano (Aya)."""
    m = Malla()
    negro = mat("Negro", (0.06, 0.06, 0.07), 0.4)
    m.caja((0, 0.045, 0), (0.14, 0.09, 0.07), negro, 0.01)
    m.cilindro((0, 0.04, 0.07), 0.034, 0.08, mat("Lente", (0.12, 0.12, 0.13), 0.3), 16, "z")
    m.cilindro((0, 0.04, 0.111), 0.026, 0.004, mat("Cristal", (0.3, 0.4, 0.55), 0.05, 0, 0.3), 16, "z")
    m.caja((0, 0.1, -0.005), (0.04, 0.02, 0.04), negro)
    m.objeto("camara_mano")
    exportar("camara_mano")

@pieza
def telefono_escritorio():
    """Teléfono de escritorio con auricular y teclado (0.22 × 0.08 × 0.2)."""
    m = Malla()
    negro = mat("Negro", (0.07, 0.07, 0.08), 0.35)
    m.caja((0, 0.035, 0), (0.22, 0.07, 0.2), negro, 0.012)
    m.caja((0, 0.07, -0.02), (0.2, 0.035, 0.06), negro, 0.015)
    gris = mat("Tecla", (0.8, 0.8, 0.78), 0.5)
    for i in range(4):
        for j in range(3):
            m.caja((-0.03 + j * 0.03, 0.072, 0.03 + i * 0.022), (0.022, 0.006, 0.016), gris, 0.002)
    m.caja((0.07, 0.072, 0.05), (0.04, 0.005, 0.06), mat("Pantallita", (0.6, 0.75, 0.6), 0.3, 0, 0.6))
    m.objeto("telefono_escritorio")
    exportar("telefono_escritorio")

@pieza
def trofeo():
    """Trofeo dorado de copa (0.3 m) con base de madera."""
    m = Malla()
    m.caja((0, 0.04, 0), (0.12, 0.08, 0.12), NOGAL(), 0.005)
    m.cilindro((0, 0.12, 0), 0.015, 0.08, DORADO(), 12)
    m.cilindro((0, 0.22, 0), 0.07, 0.14, DORADO(), 16, "y", 0.03)
    m.objeto("trofeo")
    exportar("trofeo")

@pieza
def galletas():
    """Plato con galletas (para la mesa baja)."""
    m = Malla()
    m.cilindro((0, 0.008, 0), 0.11, 0.016, mat("Plato", (0.96, 0.95, 0.92), 0.3), 24, "y", 0.08)
    gall = mat("Galleta", (0.78, 0.55, 0.3), 0.8)
    chispa = mat("Chispa", (0.25, 0.14, 0.08), 0.8)
    for (x, z, y) in ((-0.04, 0.0, 0.022), (0.04, 0.02, 0.022), (0.0, -0.04, 0.022), (0.01, 0.0, 0.034)):
        m.cilindro((x, y, z), 0.038, 0.012, gall, 14)
        m.esfera((x + 0.012, y + 0.007, z), 0.005, chispa, 6)
    m.objeto("galletas")
    exportar("galletas")

@pieza
def bocina_techo():
    """Bocina del sistema de voceo, para colgar de la pared (estado hackeado: se ve de dónde sale la voz)."""
    m = Malla()
    gris = mat("Bocina", (0.85, 0.85, 0.82), 0.5)
    m.caja((0, 0, 0.06), (0.26, 0.2, 0.12), gris, 0.015)
    m.cilindro((0, 0, 0.125), 0.08, 0.01, mat("Rejilla", (0.2, 0.2, 0.22), 0.6), 20, "z")
    m.objeto("bocina_techo")
    exportar("bocina_techo")

@pieza
def luz_emergencia():
    """Luz de emergencia roja de pared (caja con dos focos); el brillo lo pone la escena."""
    m = Malla()
    m.caja((0, 0, 0.04), (0.32, 0.12, 0.08), mat("CajaLuz", (0.9, 0.9, 0.88), 0.5), 0.01)
    rojo = mat("FocoRojo", (1.0, 0.12, 0.08), 0.3, 0, 4.0)
    for x in (-0.1, 0.1):
        m.cilindro((x, -0.02, 0.09), 0.04, 0.05, rojo, 12, "z")
    m.objeto("luz_emergencia")
    exportar("luz_emergencia")

# ---------- Escape ----------

CONCRETO = lambda: mat("Concreto", (0.62, 0.61, 0.58), 0.9)
AMARILLO = lambda: mat("AmarilloSeguridad", (0.89, 0.7, 0.24), 0.6)

@pieza
def tramo_escalera():
    """Tramo de escalera de concreto: 1.3 m de ancho, sube 1.5 m en 3.0 m (10 escalones de 15 × 30 cm) hacia -Z,
    con nariz amarilla y barandal del lado +X. Pivote: abajo, al centro del primer escalón (z = 0)."""
    m = Malla()
    for i in range(10):
        y = (i + 1) * 0.15
        z = -i * 0.3
        m.caja((0, y / 2, z - 0.15), (1.3, y, 0.3), CONCRETO())
        m.caja((0, y - 0.01, z - 0.02), (1.3, 0.02, 0.04), AMARILLO())
    # Barandal: postes y pasamanos inclinado
    acero = mat("Barandal", (0.25, 0.27, 0.3), 0.4, 0.8)
    for i in (0, 4, 9):
        y = (i + 1) * 0.15
        m.caja((0.62, y + 0.45, -i * 0.3 - 0.15), (0.04, 0.9, 0.04), acero)
    largo = math.hypot(3.0, 1.5)
    ang = math.degrees(math.atan2(1.5, 3.0))
    m.caja((0.62, 0.15 + 0.9 + 0.75, -1.5), (0.05, 0.05, largo), acero, 0, ("x", ang))
    m.objeto("tramo_escalera")
    exportar("tramo_escalera")

@pieza
def descanso():
    """Descanso de concreto de 3.0 × 1.5 m (losa de 20 cm); la cara de arriba en y = 0."""
    m = Malla()
    m.caja((0, -0.1, 0), (3.0, 0.2, 1.5), CONCRETO())
    m.caja((0, -0.001, -0.74), (3.0, 0.004, 0.02), AMARILLO())
    m.objeto("descanso")
    exportar("descanso")

@pieza
def panel_tarjeta():
    """Lector de tarjeta de pared con piloto (el color lo cambia la escena). Cara a +Z."""
    m = Malla()
    m.caja((0, 0, 0.02), (0.12, 0.18, 0.04), mat("PanelGris", (0.2, 0.21, 0.23), 0.4), 0.008)
    m.caja((0, -0.02, 0.041), (0.08, 0.08, 0.002), mat("Lector", (0.08, 0.08, 0.09), 0.3))
    m.objeto("panel_tarjeta")
    exportar("panel_tarjeta")

@pieza
def mancha_aceite():
    """Mancha de aceite de máquina en el piso (brillante y oscura), 0.5 m. Casi plana: 3 mm."""
    m = Malla()
    aceite = mat("Aceite", (0.05, 0.045, 0.04), 0.05, 0.3)
    for (x, z, r) in ((0, 0, 0.16), (0.12, 0.06, 0.09), (-0.1, -0.05, 0.08), (0.2, -0.08, 0.05), (-0.18, 0.1, 0.04)):
        m.cilindro((x, 0.0015, z), r, 0.003, aceite, 14)
    m.objeto("mancha_aceite")
    exportar("mancha_aceite")

@pieza
def huellas_mojadas():
    """Rastro de huellas de bota mojadas (6 pasos, 1.8 m hacia -Z)."""
    m = Malla()
    agua = mat("Mojado", (0.08, 0.09, 0.1), 0.02, 0.2)
    for i in range(6):
        x = 0.12 if i % 2 else -0.12
        z = -i * 0.32
        m.caja((x, 0.0015, z), (0.1, 0.003, 0.24), agua, 0.02)
        m.caja((x, 0.0015, z - 0.15), (0.09, 0.003, 0.06), agua, 0.02)
    m.objeto("huellas_mojadas")
    exportar("huellas_mojadas")

@pieza
def rack_servidor():
    """Rack de servidores de 0.6 × 2.0 × 1.0 m con frentes de equipos y luces (verde/ámbar emisivo). Frente a +Z."""
    m = Malla()
    negro = mat("Rack", (0.08, 0.085, 0.1), 0.4, 0.5)
    m.caja((0, 1.0, 0), (0.6, 2.0, 1.0), negro, 0.01)
    equipo = mat("Equipo", (0.18, 0.19, 0.21), 0.4, 0.6)
    verde = mat("LedVerde", (0.24, 0.86, 0.52), 0.3, 0, 5.0)
    ambar = mat("LedAmbar", (1.0, 0.65, 0.2), 0.3, 0, 5.0)
    for i in range(14):
        y = 0.18 + i * 0.12
        m.caja((0, y, 0.5), (0.54, 0.1, 0.02), equipo, 0.003)
        for k in range(3):
            m.caja((-0.22 + k * 0.03, y + 0.02, 0.512), (0.012, 0.012, 0.004), verde if (i + k) % 4 else ambar)
        m.caja((0.15, y, 0.512), (0.18, 0.05, 0.004), mat("Rejilla", (0.1, 0.1, 0.11), 0.6))
    m.objeto("rack_servidor")
    exportar("rack_servidor")

@pieza
def rack_disco():
    """El rack del servidor donde se conecta el disco: bahía abierta a la altura del pecho. Frente a +Z."""
    m = Malla()
    negro = mat("Rack", (0.08, 0.085, 0.1), 0.4, 0.5)
    m.caja((0, 1.0, 0), (0.6, 2.0, 1.0), negro, 0.01)
    equipo = mat("Equipo", (0.18, 0.19, 0.21), 0.4, 0.6)
    for i in range(14):
        if i in (8, 9): continue
        m.caja((0, 0.18 + i * 0.12, 0.5), (0.54, 0.1, 0.02), equipo, 0.003)
    m.caja((0, 1.2, 0.3), (0.54, 0.22, 0.4), mat("Hueco", (0.02, 0.02, 0.025), 0.9))
    m.caja((0, 1.12, 0.48), (0.2, 0.01, 0.04), mat("LedRojo", (1.0, 0.15, 0.1), 0.3, 0, 5.0))
    m.objeto("rack_disco")
    exportar("rack_disco")

@pieza
def disco_duro():
    """Disco duro externo (lo que se roba Pulgas)."""
    m = Malla()
    m.caja((0, 0.012, 0), (0.08, 0.024, 0.12), mat("Disco", (0.15, 0.16, 0.18), 0.4, 0.6), 0.004)
    m.caja((0, 0.025, 0.04), (0.012, 0.002, 0.012), mat("LedAzul", (0.3, 0.6, 1.0), 0.3, 0, 4.0))
    m.objeto("disco_duro")
    exportar("disco_duro")

@pieza
def charola_cables():
    """Charola de cables de 2 m colgada del techo, con cables de colores."""
    m = Malla()
    acero = ACERO()
    m.caja((0, 0, 0), (0.3, 0.02, 2.0), acero)
    m.caja((-0.15, 0.04, 0), (0.01, 0.08, 2.0), acero)
    m.caja((0.15, 0.04, 0), (0.01, 0.08, 2.0), acero)
    for k, col in enumerate(((0.2, 0.4, 0.85), (0.85, 0.75, 0.2), (0.2, 0.7, 0.4), (0.8, 0.2, 0.2))):
        m.cilindro((-0.09 + k * 0.06, 0.03, 0), 0.015, 2.0, mat(f"Cable{k}", col, 0.5), 8, "z")
    for z in (-0.9, 0.9):
        m.caja((0, 0.4, z), (0.02, 0.8, 0.02), acero)
    m.objeto("charola_cables")
    exportar("charola_cables")

@pieza
def aire_acondicionado():
    """Unidad de aire de precisión (0.8 × 1.9 × 0.6) con rejillas. Frente a +Z."""
    m = Malla()
    blanco = mat("AireBlanco", (0.88, 0.89, 0.9), 0.5)
    m.caja((0, 0.95, 0), (0.8, 1.9, 0.6), blanco, 0.01)
    rejilla = mat("Rejilla", (0.3, 0.32, 0.35), 0.6)
    for i in range(10):
        m.caja((0, 1.2 + i * 0.05, 0.302), (0.7, 0.012, 0.01), rejilla)
    m.caja((0.25, 0.9, 0.302), (0.16, 0.08, 0.006), mat("PantallaAire", (0.3, 0.7, 1.0), 0.3, 0, 2.0))
    m.objeto("aire_acondicionado")
    exportar("aire_acondicionado")

@pieza
def mesa_acero():
    """Mesa de trabajo de acero inoxidable de cocina (2.0 × 0.9 × 0.8) con entrepaño."""
    m = Malla()
    acero = mat("Inox", (0.78, 0.8, 0.82), 0.45, 0.35)
    m.caja((0, 0.89, 0), (2.0, 0.03, 0.8), acero, 0.006)
    m.caja((0, 0.25, 0), (1.9, 0.02, 0.72), acero)
    for x in (-0.94, 0.94):
        for z in (-0.36, 0.36):
            m.caja((x, 0.44, z), (0.04, 0.88, 0.04), acero)
    m.objeto("mesa_acero")
    exportar("mesa_acero")

@pieza
def bolsa_basura():
    """Bolsa de basura negra amarrada (0.5 m)."""
    m = Malla()
    negro = mat("Bolsa", (0.05, 0.05, 0.06), 0.25)
    m.esfera((0, 0.22, 0), 0.25, negro, 12, (1.0, 0.9, 0.85))
    m.esfera((0, 0.45, 0), 0.06, negro, 8, (0.8, 1.2, 0.8))
    m.objeto("bolsa_basura")
    exportar("bolsa_basura")

@pieza
def columna_estacionamiento():
    """Columna de concreto de 0.6 × 3.0 m con franjas amarillas y negras abajo."""
    m = Malla()
    m.caja((0, 1.5, 0), (0.6, 3.0, 0.6), CONCRETO(), 0.01)
    for i in range(4):
        m.caja((0, 0.1 + i * 0.2, 0), (0.62, 0.1, 0.62), AMARILLO() if i % 2 == 0 else mat("NegroFranja", (0.08, 0.08, 0.08), 0.7))
    m.objeto("columna_estacionamiento")
    exportar("columna_estacionamiento")

@pieza
def lampara_tubo():
    """Lámpara fluorescente de tubo para el techo (1.2 m); el tubo es emisivo."""
    m = Malla()
    m.caja((0, 0.03, 0), (0.14, 0.05, 1.22), mat("Carcasa", (0.85, 0.86, 0.86), 0.5), 0.005)
    m.cilindro((0, 0.0, 0), 0.025, 1.18, mat("Tubo", (0.92, 0.96, 1.0), 0.2, 0, 3.0), 10, "z")
    m.objeto("lampara_tubo")
    exportar("lampara_tubo")

@pieza
def rampa():
    """Rampa de estacionamiento de 4 × 10 m que sube 3 m hacia -Z, con guarnición a los lados."""
    m = Malla()
    ang = math.degrees(math.atan2(3.0, 10.0))
    largo = math.hypot(10.0, 3.0)
    m.caja((0, 1.5 - 0.15, -5.0), (4.0, 0.3, largo), CONCRETO(), 0, ("x", ang))
    for x in (-2.1, 2.1):
        m.caja((x, 1.5 + 0.1, -5.0), (0.2, 0.5, largo), mat("Guarnicion", (0.75, 0.74, 0.7), 0.8), 0, ("x", ang))
    m.objeto("rampa")
    exportar("rampa")

@pieza
def gorra():
    """Gorra de técnico (azul grisáceo) para el marcador temporal de los técnicos de Servicios Pulgar."""
    m = Malla()
    tela = mat("Gorra", (0.24, 0.33, 0.42), 0.8)
    m.esfera((0, 0.0, 0), 0.115, tela, 14, (1.0, 0.7, 1.05))
    m.caja((0, -0.01, 0.12), (0.2, 0.012, 0.12), tela, 0.004)
    m.objeto("gorra")
    exportar("gorra")

@pieza
def cubrebocas():
    """Cubrebocas azul claro (marcador temporal)."""
    m = Malla()
    m.esfera((0, 0, 0), 0.075, mat("Cubrebocas", (0.7, 0.82, 0.9), 0.9), 12, (1.3, 0.8, 0.5))
    m.objeto("cubrebocas")
    exportar("cubrebocas")

@pieza
def tatuaje_rayas():
    """Tres rayas tatuadas para el dorso de la mano de Pulgas (calcomanía de 6 cm)."""
    m = Malla()
    tinta = mat("Tinta", (0.12, 0.1, 0.14), 0.6)
    for i in range(3):
        m.caja((-0.015 + i * 0.015, 0.001, 0), (0.005, 0.002, 0.05), tinta, 0, ("y", 12))
    m.objeto("tatuaje_rayas")
    exportar("tatuaje_rayas")

# ---------- Cierre ----------

@pieza
def cinta_amarilla():
    """Cinta amarilla de la Fiscalía entre dos postes (2.1 m), combada y rota de un lado: el extremo cuelga
    deshilachado. Pivote en el poste izquierdo (x = 0) a la altura del amarre."""
    m = Malla()
    cinta = mat("CintaFiscalia", (0.95, 0.8, 0.12), 0.5)
    negro = mat("LetraCinta", (0.08, 0.08, 0.08), 0.6)
    n = 14
    for i in range(n):
        t0, t1 = i / n, (i + 1) / n
        if t0 > 0.72: break                      # aquí se rompió
        x0, x1 = t0 * 2.1, t1 * 2.1
        y0 = -0.18 * math.sin(t0 * math.pi)
        y1 = -0.18 * math.sin(t1 * math.pi)
        tor = math.sin(t0 * 9) * 12
        m.caja(((x0 + x1) / 2, (y0 + y1) / 2, 0), (x1 - x0 + 0.004, 0.05, 0.002), cinta, 0, ("x", tor))
        if i % 2 == 0:
            m.caja(((x0 + x1) / 2, (y0 + y1) / 2, 0.0015), (0.06, 0.012, 0.001), negro, 0, ("x", tor))
    # Extremo suelto que cuelga del poste derecho, deshilachado en tiras
    for k in range(4):
        m.caja((2.1 - 0.01 * k, -0.25 - k * 0.02, 0.01 * k), (0.008, 0.5 - k * 0.08, 0.002), cinta, 0, ("z", 6 - k * 4))
    for k in range(3):
        m.caja((1.5 + k * 0.012, -0.24 - k * 0.03, 0), (0.006, 0.12, 0.002), cinta, 0, ("z", -10 + k * 9))
    m.objeto("cinta_amarilla")
    exportar("cinta_amarilla")

@pieza
def sello_roto():
    """Sello rojo de clausura de la Fiscalía, roto a la mitad: dos pedazos de papel pegados al poste (cara a +Z)."""
    m = Malla()
    rojo = mat("SelloRojo", (0.75, 0.1, 0.08), 0.8)
    blanco = mat("SelloLetra", (0.95, 0.92, 0.85), 0.8)
    m.caja((0, 0.1, 0.004), (0.12, 0.2, 0.002), rojo, 0, ("z", 4))
    m.caja((0, 0.13, 0.0055), (0.08, 0.02, 0.001), blanco)
    m.caja((0.01, -0.13, 0.004), (0.12, 0.17, 0.002), rojo, 0, ("z", -12))
    m.caja((0.0, -0.1, 0.0055), (0.08, 0.015, 0.001), blanco, 0, ("z", -12))
    m.objeto("sello_roto")
    exportar("sello_roto")

@pieza
def barrotes():
    """Barrotes de acero para la ventana de la celda (0.8 × 1.3 m). Atrás en z = 0."""
    m = Malla()
    acero = mat("Barrote", (0.22, 0.23, 0.25), 0.5, 0.8)
    for i in range(6):
        m.cilindro((-0.35 + i * 0.14, 0.65, 0.02), 0.014, 1.3, acero, 8)
    for y in (0.05, 0.65, 1.25):
        m.caja((0, y, 0.02), (0.8, 0.04, 0.03), acero)
    m.objeto("barrotes")
    exportar("barrotes")

@pieza
def reja_celda():
    """Reja de la celda para un hueco de puerta (0.9 × 2.1 m): marco y barrotes. Atrás en z = 0."""
    m = Malla()
    acero = mat("Barrote", (0.22, 0.23, 0.25), 0.5, 0.8)
    for i in range(7):
        m.cilindro((-0.39 + i * 0.13, 1.05, 0.02), 0.016, 2.08, acero, 8)
    for y in (0.04, 1.0, 2.06):
        m.caja((0, y, 0.02), (0.88, 0.06, 0.04), acero)
    m.caja((0.32, 1.0, 0.05), (0.1, 0.16, 0.05), mat("Cerradura", (0.3, 0.3, 0.32), 0.4, 0.8))
    m.objeto("reja_celda")
    exportar("reja_celda")

@pieza
def libro():
    """Libro de pasta dura cerrado (sin remitente), 0.15 × 0.03 × 0.22 m."""
    m = Malla()
    m.caja((0, 0.015, 0), (0.15, 0.03, 0.22), mat("Pasta", (0.42, 0.16, 0.12), 0.7), 0.003)
    m.caja((0.004, 0.015, 0), (0.142, 0.024, 0.212), mat("Hojas", (0.95, 0.92, 0.84), 0.9))
    m.caja((-0.074, 0.015, 0), (0.006, 0.03, 0.22), mat("Lomo", (0.3, 0.1, 0.08), 0.7))
    m.objeto("libro")
    exportar("libro")

for nombre, f in PIEZAS.items():
    if SOLO and nombre not in SOLO: continue
    limpiar()
    f()
print("PROPS LISTOS")
