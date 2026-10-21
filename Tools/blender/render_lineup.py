"""All finished models in one row with game-sized citizens in front of each, for checking that
the set reads as one family. Never saves.

    blender --background X.blend --python render_lineup.py -- <out.png> Name1 Name2 ...
"""
import math
import os
import sys

import bpy
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
out, names = argv[0], argv[1:]
sc = bpy.context.scene
for o in sc.objects:
    o.hide_render = True

# a citizen exactly as CitizenVisualsManager builds one (body 0.3x0.5x0.3, head 0.22 cube),
# drawn at UnitSize.Scale like the game does
UNIT = 0.625
skin = bpy.data.materials.new("LineupSkin")
skin.use_nodes = True
skin.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.9, 0.62, 0.45, 1)
cloth = bpy.data.materials.new("LineupCloth")
cloth.use_nodes = True
cloth.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.2, 0.35, 0.8, 1)


def citizen(x, y):
    for size, z, mat in (((0.3, 0.3, 0.5), 0.25, cloth), ((0.22, 0.22, 0.22), 0.61, skin)):
        bpy.ops.mesh.primitive_cube_add(size=1, location=(x, y, z * UNIT))
        c = bpy.context.active_object
        c.scale = tuple(v * UNIT for v in size)
        c.data.materials.append(mat)


x = 0.0
for n in names:
    o = bpy.data.objects[n]
    w = o.dimensions.x
    x += w / 2
    o.location = (x, 0, 0)
    o.hide_render = False
    for part in o.children_recursive:      # a jointed figure's legs, arms and head
        part.hide_render = False
    citizen(x - 0.25, -o.dimensions.y / 2 - 0.35)
    x += w / 2 + 0.8
total = x - 0.8

bpy.ops.mesh.primitive_plane_add(size=1, location=(total / 2, 0, -0.001))
ground = bpy.context.active_object
ground.scale = (total + 20, 30, 1)
gm = bpy.data.materials.new("LineupGround")
gm.use_nodes = True
gm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.42, 0.62, 0.16, 1)
ground.data.materials.append(gm)

sun_data = bpy.data.lights.new("LineupSun", 'SUN')
sun_data.energy = 2.4
sun = bpy.data.objects.new("LineupSun", sun_data)
sc.collection.objects.link(sun)
sun.rotation_euler = (math.radians(45), 0, math.radians(-35))
world = sc.world or bpy.data.worlds.new("LineupWorld")
sc.world = world
world.use_nodes = True
bg = world.node_tree.nodes.get("Background")
bg.inputs[0].default_value = (0.55, 0.72, 0.95, 1)
bg.inputs[1].default_value = 0.55

sc.render.engine = 'BLENDER_EEVEE'
sc.render.resolution_x, sc.render.resolution_y = 2400, 900
sc.view_settings.view_transform = 'Standard'
sc.view_settings.look = 'None'

cam_data = bpy.data.cameras.new("LineupCam")
cam_data.type = 'ORTHO'
cam_data.ortho_scale = total + 1.0
cam = bpy.data.objects.new("LineupCam", cam_data)
sc.collection.objects.link(cam)
sc.camera = cam
target = Vector((total / 2, 0, 0.8))
pitch = math.radians(20)
cam.location = target + Vector((0, -math.cos(pitch), math.sin(pitch))) * 30
cam.rotation_euler = (target - cam.location).to_track_quat('-Z', 'Y').to_euler()
sc.render.filepath = out
bpy.ops.render.render(write_still=True)
print("RENDERED", out)
