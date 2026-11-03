# Utilidades comunes de los scripts de Blender de la cinemática de la cinta (Caso 0).
import bpy, os, math, mathutils
from mathutils import Vector, Matrix, Quaternion

BASE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))     # Assets/Cinematicas/Blender
GAME = os.path.abspath(os.path.join(BASE, "..", "..", ".."))          # game/
FPS = 30

def g2b(x, y, z):
    "Punto de Godot (Y arriba, -Z al frente) a Blender (Z arriba)"
    return Vector((x, -z, y))

def importar(ruta, coleccion):
    antes = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=ruta)
    nuevos = [o for o in bpy.data.objects if o not in antes]
    col = bpy.data.collections.get(coleccion) or bpy.data.collections.new(coleccion)
    if col.name not in bpy.context.scene.collection.children: bpy.context.scene.collection.children.link(col)
    for o in nuevos:
        for c in list(o.users_collection): c.objects.unlink(o)
        col.objects.link(o)
    return nuevos

def armadura(objetos):
    return next(o for o in objetos if o.type == 'ARMATURE')

def workbench(escena, ancho=960, alto=540):
    escena.render.engine = 'BLENDER_WORKBENCH'
    escena.display.shading.light = 'STUDIO'
    escena.display.shading.color_type = 'MATERIAL'
    escena.render.resolution_x, escena.render.resolution_y = ancho, alto

def camara_mirando(nombre, desde, hacia, lente=35):
    cam = bpy.data.objects.get(nombre)
    if cam is None:
        cam = bpy.data.objects.new(nombre, bpy.data.cameras.new(nombre))
        bpy.context.scene.collection.objects.link(cam)
    cam.data.lens = lente
    cam.location = desde
    cam.rotation_euler = (hacia - desde).to_track_quat('-Z', 'Y').to_euler()
    return cam
