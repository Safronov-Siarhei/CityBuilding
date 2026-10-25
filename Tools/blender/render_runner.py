"""A jointed figure running, side on, in two strides -- the loading screen's runner along its
progress plank (the user, 2026-10-25). Legs and arms swing about their joints as FigureAnimator
swings them in the game; transparent ground. Never saves.

    blender --background Buildings-Size1-Models.blend --python render_runner.py -- <out_prefix> <Figure> [size]

Writes <out_prefix>_A.png and <out_prefix>_B.png (opposite strides), facing right.
"""
import math
import sys

import bpy
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
prefix, name = argv[0], argv[1]
size = int(argv[2]) if len(argv) > 2 else 256
sc = bpy.context.scene
for ob in sc.objects:
    ob.hide_render = True
fig = bpy.data.objects[name]
fig.location = (0, 0, 0)
fig.rotation_euler = (0, 0, math.radians(90))     # faces -y in the file: turned to face +x, the screen's right
fig.hide_render = False
parts = {c.name.split("-")[-1]: c for c in fig.children_recursive}
for c in fig.children_recursive:
    c.hide_render = False
base = {k: c.rotation_euler.copy() for k, c in parts.items()}

sun_data = bpy.data.lights.new("RunSun", 'SUN')
sun_data.energy = 3.0
sun = bpy.data.objects.new("RunSun", sun_data)
sc.collection.objects.link(sun)
sun.rotation_euler = (math.radians(50), 0, math.radians(-30))
world = sc.world or bpy.data.worlds.new("RunWorld")
sc.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.85, 0.85, 0.9, 1)
world.node_tree.nodes["Background"].inputs[1].default_value = 0.7
sc.render.engine = 'BLENDER_EEVEE'
sc.render.film_transparent = True
sc.render.resolution_x = sc.render.resolution_y = size
sc.render.image_settings.color_mode = 'RGBA'
sc.view_settings.view_transform = 'Standard'
sc.view_settings.look = 'None'

zs = [(p.matrix_world @ Vector(c)).z for p in [fig] + list(fig.children_recursive) if p.type == 'MESH' for c in p.bound_box]
top = max(zs)
cam_data = bpy.data.cameras.new("RunCam")
cam_data.type = 'ORTHO'
cam_data.ortho_scale = top * 1.25
cam = bpy.data.objects.new("RunCam", cam_data)
sc.collection.objects.link(cam)
sc.camera = cam
target = Vector((0, 0, top * 0.5))
cam.location = target + Vector((0, -10, 0.6))
cam.rotation_euler = (target - cam.location).to_track_quat('-Z', 'Y').to_euler()


def stride(sign):
    swing = {"LegL": 32, "LegR": -32, "ArmSword": -28, "ArmShield": 28}
    for k, deg in swing.items():
        if k in parts:
            r = base[k].copy()
            r.x += math.radians(deg * sign)
            parts[k].rotation_euler = r


for tag, sign in (("A", 1), ("B", -1)):
    stride(sign)
    # a run leans forward
    fig.rotation_euler = (0, math.radians(8), math.radians(90))
    sc.render.filepath = f"{prefix}_{tag}.png"
    bpy.ops.render.render(write_still=True)
    print("RENDERED", sc.render.filepath)
