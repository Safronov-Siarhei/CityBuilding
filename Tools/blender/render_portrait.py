"""A hero's portrait for the HUD: the figure's head and shoulders, front three-quarter, on a
transparent background. Opens the models file, renders, never saves.

    blender --background Buildings-Size1-Models.blend --python render_portrait.py -- <Obj> <out.png> [size]

The HUD reads it from Assets/_Project/UI/Icons (e.g. PortraitKing.png), where every PNG is a UI sprite.
"""
import math
import os
import sys

import bpy
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
target_name, out_path = argv[0], argv[1]
size = int(argv[2]) if len(argv) > 2 else 256

sc = bpy.context.scene
target = bpy.data.objects[target_name]
keep = {target_name} | {c.name for c in target.children_recursive}
for o in sc.objects:
    o.hide_render = o.name not in keep

sun_data = bpy.data.lights.new("PortraitSun", 'SUN')
sun_data.energy = 3.5
sun = bpy.data.objects.new("PortraitSun", sun_data)
sc.collection.objects.link(sun)
sun.rotation_euler = (math.radians(50), 0, math.radians(-25))
fill_data = bpy.data.lights.new("PortraitFill", 'SUN')
fill_data.energy = 1.2
fill = bpy.data.objects.new("PortraitFill", fill_data)
sc.collection.objects.link(fill)
fill.rotation_euler = (math.radians(70), 0, math.radians(150))

world = sc.world or bpy.data.worlds.new("World")
sc.world = world
world.use_nodes = True
bg = world.node_tree.nodes.get("Background")
bg.inputs[0].default_value = (0.8, 0.8, 0.85, 1)
bg.inputs[1].default_value = 0.6

sc.render.engine = 'BLENDER_EEVEE'
sc.render.film_transparent = True
sc.render.image_settings.file_format = 'PNG'
sc.render.image_settings.color_mode = 'RGBA'
sc.view_settings.view_transform = 'Standard'
sc.render.resolution_x = sc.render.resolution_y = size

cam_data = bpy.data.cameras.new("PortraitCam")
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 0.42
cam = bpy.data.objects.new("PortraitCam", cam_data)
sc.collection.objects.link(cam)
sc.camera = cam
# the head and the crown: about the top third of the figure, seen from the front (-y), a little
# to the side and from a little above
head = target.matrix_world.translation + Vector((0, 0, 0.58))
d = Vector((math.sin(math.radians(-20)), -math.cos(math.radians(-20)), 0.25)).normalized()
cam.location = head + d * 3.0
cam.rotation_euler = (head - cam.location).to_track_quat('-Z', 'Y').to_euler()

os.makedirs(os.path.dirname(os.path.abspath(out_path)), exist_ok=True)
sc.render.filepath = out_path
bpy.ops.render.render(write_still=True)
print("RENDERED", out_path)
