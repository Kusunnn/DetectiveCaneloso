# Kit modular de interiores del Capítulo 2 (un solo kit para la Torre Miawrio, la comisaría y el penal).
# Genera módulos de 1 m con el pivote en la base y los exporta a glTF (.glb) en Assets/Caso2/Modelos/kit/.
# Uso: blender --background --factory-startup --python kit_interiores.py -- <carpeta_salida>
#
# Convención (en Godot, Y arriba): cada muro va de x = -0.5 a 0.5, su cara interior está en z = 0 y mira a +Z
# (el cuarto queda hacia +Z); el grosor va hacia -Z. Zócalo y moldura están pegados a la cara interior y tapan
# las uniones entre módulos. Las alturas se generan para 2.8 m (comisaría, penal, servicio), 3.4 m (despacho)
# y 4.0 m (lobby). Los materiales llevan nombre y Godot los cambia por la paleta de cada espacio.
import bpy, bmesh, sys, os, math

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
SALIDA = args[0] if args else os.path.join(os.path.dirname(__file__), "..", "Modelos", "kit")
os.makedirs(SALIDA, exist_ok=True)

GROSOR = 0.12

def limpiar():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    _materiales.clear()

_materiales = {}
def material(nombre, color, rugosidad=0.8, metal=0.0, alfa=1.0):
    if nombre in _materiales: return _materiales[nombre]
    m = bpy.data.materials.new(nombre)
    m.use_nodes = True
    p = m.node_tree.nodes.get("Principled BSDF")
    p.inputs["Base Color"].default_value = (*color, 1.0)
    p.inputs["Roughness"].default_value = rugosidad
    p.inputs["Metallic"].default_value = metal
    if alfa < 1.0:
        p.inputs["Alpha"].default_value = alfa
        m.blend_method = "BLEND"
    _materiales[nombre] = m
    return m

# Coordenadas de Godot (x, y arriba, z hacia el cuarto) → Blender (x, -z, y)
def gb(x, y, z):
    return (x, -z, y)

def caja(bm, x0, y0, z0, x1, y1, z1, indice_material=0, bisel=0.0):
    """Paralelepípedo en coordenadas de Godot, con bisel opcional en las aristas."""
    vs = [bm.verts.new(gb(x, y, z)) for x in (x0, x1) for y in (y0, y1) for z in (z0, z1)]
    # índices: x*4 + y*2 + z
    caras = [(0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)]
    nuevas = []
    for c in caras:
        f = bm.faces.new([vs[i] for i in c])
        f.material_index = indice_material
        nuevas.append(f)
    if bisel > 0:
        aristas = list({e for f in nuevas for e in f.edges})
        bmesh.ops.bevel(bm, geom=aristas, offset=bisel, segments=1, affect="EDGES")
    return nuevas

def objeto(nombre, construir, materiales):
    malla = bpy.data.meshes.new(nombre)
    bm = bmesh.new()
    construir(bm)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(malla)
    bm.free()
    for m in materiales: malla.materials.append(m)
    ob = bpy.data.objects.new(nombre, malla)
    bpy.context.scene.collection.objects.link(ob)
    # UV por proyección de caja (1 unidad = 1 m) para que las texturas no se estiren
    bpy.context.view_layer.objects.active = ob
    ob.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.cube_project(cube_size=1.0)
    bpy.ops.object.mode_set(mode="OBJECT")
    for p in malla.polygons: p.use_smooth = False
    return ob

def exportar(nombre):
    ruta = os.path.join(SALIDA, nombre + ".glb")
    bpy.ops.export_scene.gltf(filepath=ruta, export_format="GLB", use_selection=False, export_apply=True,
                              export_yup=True, export_materials="EXPORT")
    print("exportado", ruta)

MAT_MURO = lambda: material("Muro", (0.82, 0.76, 0.62))
MAT_ZOCALO = lambda: material("Zocalo", (0.36, 0.24, 0.15), 0.6)
MAT_MARCO = lambda: material("Marco", (0.42, 0.28, 0.17), 0.55)
MAT_VIDRIO = lambda: material("Vidrio", (0.75, 0.86, 0.92), 0.05, 0.0, 0.18)

def molduras(bm, x0, x1, alto, i_zocalo):
    caja(bm, x0, 0.0, 0.0, x1, 0.12, 0.022, i_zocalo, 0.004)          # zócalo
    caja(bm, x0, alto - 0.07, 0.0, x1, alto, 0.03, i_zocalo, 0.004)  # moldura de techo

def muro(alto):
    def c(bm):
        caja(bm, -0.5, 0, -GROSOR, 0.5, alto, 0, 0)
        molduras(bm, -0.5, 0.5, alto, 1)
    limpiar()
    objeto(f"muro_{int(alto*100)}", c, [MAT_MURO(), MAT_ZOCALO()])
    exportar(f"muro_{int(alto*100)}")

def muro_medio(alto):
    """Medio módulo (0.5 m) para cerrar medidas que no son enteras."""
    def c(bm):
        caja(bm, -0.25, 0, -GROSOR, 0.25, alto, 0, 0)
        molduras(bm, -0.25, 0.25, alto, 1)
    limpiar()
    objeto(f"muro_medio_{int(alto*100)}", c, [MAT_MURO(), MAT_ZOCALO()])
    exportar(f"muro_medio_{int(alto*100)}")

def muro_ventana(alto, ancho=1.0, y0=0.9, y1=2.2):
    """Muro con hueco de ventana: marco de madera, alféizar y vidrio. La vista va detrás (en la escena)."""
    a = ancho / 2
    h0, h1 = y0, min(y1, alto - 0.25)
    m = 0.06  # ancho del marco
    def c(bm):
        caja(bm, -a, 0, -GROSOR, a, h0, 0, 0)               # antepecho
        caja(bm, -a, h1, -GROSOR, a, alto, 0, 0)            # dintel
        caja(bm, -a, h0, -GROSOR, -a + 0.04, h1, 0, 0)      # jambas de muro (muy delgadas: el marco tapa)
        caja(bm, a - 0.04, h0, -GROSOR, a, h1, 0, 0)
        # marco
        caja(bm, -a + 0.04, h0, -GROSOR - 0.01, -a + 0.04 + m, h1, 0.02, 2, 0.006)
        caja(bm, a - 0.04 - m, h0, -GROSOR - 0.01, a - 0.04, h1, 0.02, 2, 0.006)
        caja(bm, -a + 0.04, h1 - m, -GROSOR - 0.01, a - 0.04, h1, 0.02, 2, 0.006)
        caja(bm, -a + 0.04, h0, -GROSOR - 0.01, a - 0.04, h0 + 0.04, 0.02, 2, 0.006)
        caja(bm, -a - 0.02, h0 - 0.03, 0.0, a + 0.02, h0, 0.07, 2, 0.008)   # alféizar
        caja(bm, -0.012, h0 + 0.04, -0.07, 0.012, h1 - m, -0.05, 2)          # parteluz
        caja(bm, -a + 0.04 + m, h0 + 0.04, -0.065, a - 0.04 - m, h1 - m, -0.055, 3)  # vidrio
        molduras(bm, -a, a, alto, 1)
    limpiar()
    nombre = f"muro_ventana_{int(alto*100)}" + ("" if ancho == 1.0 else f"_{int(ancho*100)}")
    objeto(nombre, c, [MAT_MURO(), MAT_ZOCALO(), MAT_MARCO(), MAT_VIDRIO()])
    exportar(nombre)

def ventanal(alto):
    """Ventanal de piso a techo (lobby y despacho): poste a los lados, travesaño bajo y vidrio grande."""
    def c(bm):
        caja(bm, -0.5, 0, -GROSOR, 0.5, 0.25, 0, 0)            # repisa baja
        caja(bm, -0.5, alto - 0.3, -GROSOR, 0.5, alto, 0, 0)   # dintel
        caja(bm, -0.5, 0.25, -GROSOR, -0.46, alto - 0.3, 0.02, 2, 0.005)   # postes (se juntan con el vecino)
        caja(bm, 0.46, 0.25, -GROSOR, 0.5, alto - 0.3, 0.02, 2, 0.005)
        caja(bm, -0.46, 0.25, -GROSOR, 0.46, 0.3, 0.02, 2, 0.005)          # travesaño bajo
        caja(bm, -0.46, alto - 0.35, -GROSOR, 0.46, alto - 0.3, 0.02, 2, 0.005)
        caja(bm, -0.46, 0.3, -0.066, 0.46, alto - 0.35, -0.056, 3)          # vidrio
        caja(bm, -0.5, 0.0, 0.0, 0.5, 0.12, 0.022, 1, 0.004)                # zócalo
        caja(bm, -0.5, alto - 0.07, 0.0, 0.5, alto, 0.03, 1, 0.004)
    limpiar()
    objeto(f"ventanal_{int(alto*100)}", c, [MAT_MURO(), MAT_ZOCALO(), MAT_MARCO(), MAT_VIDRIO()])
    exportar(f"ventanal_{int(alto*100)}")

def muro_puerta(alto):
    """Hueco de 0.9 × 2.1 m con marco. La hoja va aparte (puerta_hoja) para que gire con Puerta."""
    a, hp, m = 0.45, 2.1, 0.07
    def c(bm):
        caja(bm, -0.5, hp, -GROSOR, 0.5, alto, 0, 0)
        caja(bm, -0.5, 0, -GROSOR, -a, hp, 0, 0)
        caja(bm, a, 0, -GROSOR, 0.5, hp, 0, 0)
        caja(bm, -a - 0.02, 0, -GROSOR - 0.015, -a + m - 0.02, hp + m, 0.02, 2, 0.006)
        caja(bm, a - m + 0.02, 0, -GROSOR - 0.015, a + 0.02, hp + m, 0.02, 2, 0.006)
        caja(bm, -a - 0.02, hp, -GROSOR - 0.015, a + 0.02, hp + m, 0.02, 2, 0.006)
        for x0, x1 in ((-0.5, -a - 0.02), (a + 0.02, 0.5)):
            caja(bm, x0, 0.0, 0.0, x1, 0.12, 0.022, 1, 0.003)
        caja(bm, -0.5, alto - 0.07, 0.0, 0.5, alto, 0.03, 1, 0.004)
    limpiar()
    objeto(f"muro_puerta_{int(alto*100)}", c, [MAT_MURO(), MAT_ZOCALO(), MAT_MARCO()])
    exportar(f"muro_puerta_{int(alto*100)}")

def puerta_hoja(nombre="puerta_hoja", vidrio=False, color=(0.45, 0.30, 0.18)):
    """Hoja de 0.86 × 2.08 × 0.04 con el pivote en la bisagra (x = 0) y en el piso; se abre hacia +Z."""
    an, al, g = 0.86, 2.08, 0.04
    def c(bm):
        caja(bm, 0.0, 0.0, -g / 2, an, al, g / 2, 0, 0.004)
        # tableros en relieve (dos caras)
        for z in (g / 2, -g / 2 - 0.006):
            if vidrio:
                caja(bm, 0.18, 1.1, z, an - 0.18, 1.85, z + 0.006, 2)
            else:
                caja(bm, 0.12, 1.15, z, an - 0.12, 1.9, z + 0.006, 0, 0.002)
            caja(bm, 0.12, 0.18, z, an - 0.12, 0.95, z + 0.006, 0, 0.002)
        # manija en los dos lados
        for z0, z1 in ((g / 2, g / 2 + 0.06), (-g / 2 - 0.06, -g / 2)):
            caja(bm, an - 0.12, 0.98, z0, an - 0.06, 1.0, z1, 1, 0.004)
            caja(bm, an - 0.16, 0.975, z1 - 0.01 if z0 > 0 else z0, an - 0.06, 1.005, z1 if z0 > 0 else z0 + 0.01, 1, 0.003)
    limpiar()
    mats = [material("Hoja", color, 0.55), material("Manija", (0.78, 0.62, 0.30), 0.3, 1.0)]
    if vidrio: mats.append(MAT_VIDRIO())
    objeto(nombre, c, mats)
    exportar(nombre)

def esquinero(alto):
    """Pilar de 0.14 × 0.14 que tapa la unión de dos muros en esquina."""
    def c(bm):
        caja(bm, -0.07, 0, -0.07, 0.07, alto, 0.07, 0, 0.01)
        caja(bm, -0.09, 0, -0.09, 0.09, 0.12, 0.09, 1, 0.004)
        caja(bm, -0.09, alto - 0.07, -0.09, 0.09, alto, 0.09, 1, 0.004)
    limpiar()
    objeto(f"esquinero_{int(alto*100)}", c, [MAT_MURO(), MAT_ZOCALO()])
    exportar(f"esquinero_{int(alto*100)}")

def piso_tablas():
    """1 × 1 m de duela: 5 tablas de 0.2 m con junta, sobre una base oscura. Cara de arriba en y = 0."""
    def c(bm):
        caja(bm, -0.5, -0.04, -0.5, 0.5, -0.004, 0.5, 1)
        for i in range(5):
            x0 = -0.5 + i * 0.2
            caja(bm, x0 + 0.0015, -0.012, -0.5, x0 + 0.2 - 0.0015, 0.0, 0.5, 0 if i % 2 == 0 else 2, 0.0012)
    limpiar()
    objeto("piso_tablas", c, [material("Duela", (0.55, 0.36, 0.20), 0.7),
                              material("Junta", (0.12, 0.08, 0.05), 0.9),
                              material("Duela2", (0.50, 0.33, 0.18), 0.7)])
    exportar("piso_tablas")

def piso_losas():
    """1 × 1 m de losas de 0.5 m (mármol, concreto o loseta, según el material que ponga la escena)."""
    def c(bm):
        caja(bm, -0.5, -0.04, -0.5, 0.5, -0.004, 0.5, 1)
        for i in range(2):
            for j in range(2):
                x0, z0 = -0.5 + i * 0.5, -0.5 + j * 0.5
                caja(bm, x0 + 0.002, -0.012, z0 + 0.002, x0 + 0.498, 0.0, z0 + 0.498, (i + j) % 2 * 2, 0.0015)
    limpiar()
    objeto("piso_losas", c, [material("Losa", (0.88, 0.84, 0.76), 0.25),
                             material("Junta", (0.12, 0.08, 0.05), 0.9),
                             material("Losa2", (0.84, 0.80, 0.72), 0.25)])
    exportar("piso_losas")

def techo_panel():
    """1 × 1 m de plafón (la cara visible mira hacia abajo, en y = 0)."""
    def c(bm):
        caja(bm, -0.5, 0.0, -0.5, 0.5, 0.03, 0.5, 1)
        caja(bm, -0.49, -0.006, -0.49, 0.49, 0.0, 0.49, 0, 0.002)
    limpiar()
    objeto("techo_panel", c, [material("Plafon", (0.93, 0.91, 0.86), 0.9), material("Reticula", (0.80, 0.78, 0.74), 0.8)])
    exportar("techo_panel")

def persiana(ancho=0.8, alto=1.3, abierta=0.6):
    """Persiana veneciana de 0.8 × 1.3 m con las tablillas inclinadas (deja pasar la luz en franjas)."""
    paso = 0.045
    n = int(alto / paso)
    def c(bm):
        caja(bm, -ancho / 2 - 0.01, alto - 0.04, -0.03, ancho / 2 + 0.01, alto, 0.03, 1, 0.004)  # cabezal
        for i in range(n):
            y = alto - 0.06 - i * paso
            ang = abierta
            dy, dz = math.sin(ang) * 0.017, math.cos(ang) * 0.017
            # tablilla de 1.5 mm de grueso, inclinada: 8 vértices
            ny, nz = math.cos(ang) * 0.0008, -math.sin(ang) * 0.0008
            v = [bm.verts.new(gb(x, y + sy * dy + k * ny, sz * dz + k * nz))
                 for x in (-ancho / 2, ancho / 2) for sy, sz in ((-1, -1), (1, 1)) for k in (-1, 1)]
            # índices: x*4 + borde*2 + cara
            for cara in ((0, 2, 6, 4), (1, 5, 7, 3), (0, 4, 5, 1), (2, 3, 7, 6), (0, 1, 3, 2), (4, 6, 7, 5)):
                bm.faces.new([v[i] for i in cara]).material_index = 0
        for x in (-ancho / 3, ancho / 3):   # cordones
            caja(bm, x - 0.002, 0.05, -0.002, x + 0.002, alto - 0.04, 0.002, 2)
        caja(bm, -ancho / 2, 0.02, -0.012, ancho / 2, 0.05, 0.012, 1, 0.003)   # barra de abajo
    limpiar()
    objeto("persiana", c, [material("Tablilla", (0.93, 0.89, 0.80), 0.6),
                           material("Cabezal", (0.85, 0.82, 0.74), 0.5),
                           material("Cordon", (0.7, 0.68, 0.62), 0.9)])
    exportar("persiana")

def cortina_metalica(ancho=1.0, alto=3.0):
    """Persiana metálica de seguridad (estado hackeado): lámina ondulada en franjas de 8 cm."""
    def c(bm):
        caja(bm, -ancho / 2, alto - 0.18, -0.12, ancho / 2, alto, 0.06, 1, 0.01)  # caja del rollo
        y = 0.0
        i = 0
        while y < alto - 0.18:
            z = 0.012 if i % 2 == 0 else 0.0
            caja(bm, -ancho / 2 + 0.01, y, -0.01 + z, ancho / 2 - 0.01, min(y + 0.08, alto - 0.18), z, 0, 0.004)
            y += 0.08
            i += 1
        caja(bm, -ancho / 2 + 0.01, 0.0, -0.02, ancho / 2 - 0.01, 0.05, 0.03, 1, 0.005)
    limpiar()
    objeto("cortina_metalica", c, [material("Lamina", (0.45, 0.47, 0.50), 0.45, 0.8),
                                   material("Rollo", (0.30, 0.31, 0.33), 0.5, 0.6)])
    exportar("cortina_metalica")

for alto in (2.8, 3.4, 4.0):
    muro(alto)
    muro_medio(alto)
    muro_ventana(alto)
    muro_puerta(alto)
    esquinero(alto)
    ventanal(alto)
puerta_hoja()
puerta_hoja("puerta_hoja_vidrio", vidrio=True)
puerta_hoja("puerta_hoja_metal", color=(0.40, 0.42, 0.45))
piso_tablas()
piso_losas()
techo_panel()
persiana()
cortina_metalica()
print("KIT LISTO")
