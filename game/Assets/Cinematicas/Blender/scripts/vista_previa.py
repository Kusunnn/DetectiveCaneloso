# Renderiza cuadros de caso0_cinta.blend para revisar poses y contactos (Workbench, rápido).
#   blender -b caso0_cinta.blend --python vista_previa.py -- <carpeta> [--libre x,y,z:x,y,z] t1 t2 ...
# Con --libre la cámara se pone donde se indica (coordenadas de Godot) en lugar de la cámara de la toma.
import bpy, sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from comun import *

args = sys.argv[sys.argv.index("--") + 1:]
carpeta = args.pop(0)
libre = None
if args and args[0] == "--libre":
    args.pop(0)
    d, h = args.pop(0).split(":")
    libre = (g2b(*map(float, d.split(","))), g2b(*map(float, h.split(","))))
os.makedirs(carpeta, exist_ok=True)
esc = bpy.context.scene
workbench(esc, 960, 540)
esc.display.shading.color_type = 'TEXTURE'
esc.display.shading.show_shadows = True
cam = esc.camera
if libre:
    cam = camara_mirando("Libre", libre[0], libre[1], 24)
    esc.camera = cam
for t in map(float, args):
    esc.frame_set(1 + int(round(t * FPS)))
    esc.render.filepath = os.path.join(carpeta, "c_%05.2f.png" % t)
    bpy.ops.render.render(write_still=True)
print("listo")
