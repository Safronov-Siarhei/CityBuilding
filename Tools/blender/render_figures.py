"""Jointed figures (heroes, soldiers, orcs) side by side at one scale, seen from the front and a
little above, for checking that they read as one cast. Never saves.

    blender --background X.blend --python render_figures.py -- <out.png> Name1 Name2 ...
"""
import math
import sys

import bpy
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
out, names = argv[0], [n for n in argv[1:] if n in bpy.data.objects]
sc = bpy.context.scene
for ob in sc.objects:
    ob.hide_render = True

STEP = float(__import__("os").environ.get("FIG_STEP", "0.42"))
for i, n in enumerate(names):
    ob = bpy.data.objects[n]
    ob.location = (i * STEP, 0, 0)
    ob.hide_render = False
    for part in ob.children_recursive:
        part.hide_render = False
total = (len(names) - 1) * STEP

bpy.ops.mesh.primitive_plane_add(size=1, location=(total / 2, 0, -0.001))
ground = bpy.context.active_object
ground.scale = (total + 20, 30, 1)
gm = bpy.data.materials.new("FiguresGround")
gm.use_nodes = True
gm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.42, 0.62, 0.16, 1)
ground.data.materials.append(gm)

sun_data = bpy.data.lights.new("FiguresSun", 'SUN')
sun_data.energy = 2.6
sun = bpy.data.objects.new("FiguresSun", sun_data)
sc.collection.objects.link(sun)
sun.rotation_euler = (math.radians(40), 0, math.radians(-30))
world = sc.world or bpy.data.worlds.new("FiguresWorld")
sc.world = world
world.use_nodes = True
bg = world.node_tree.nodes.get("Background")
bg.inputs[0].default_value = (0.55, 0.72, 0.95, 1)
bg.inputs[1].default_value = 0.6

sc.render.engine = 'BLENDER_EEVEE'
sc.render.resolution_x, sc.render.resolution_y = 2400, 900
sc.view_settings.view_transform = 'Standard'
sc.view_settings.look = 'None'

cam_data = bpy.data.cameras.new("FiguresCam")
cam_data.type = 'ORTHO'
cam_data.ortho_scale = total + STEP
cam = bpy.data.objects.new("FiguresCam", cam_data)
sc.collection.objects.link(cam)
sc.camera = cam
target = Vector((total / 2, 0, 0.36))
pitch = math.radians(18)
cam.location = target + Vector((0, -math.cos(pitch), math.sin(pitch))) * 30
cam.rotation_euler = (target - cam.location).to_track_quat('-Z', 'Y').to_euler()
sc.render.filepath = out
bpy.ops.render.render(write_still=True)
print("RENDERED", out)
