"""Review pictures of one archipelago island (islands.py): straight down and at the game's angle.
Opens the .blend, adds sun, sky and cameras, renders, never saves.

    blender --background Isl-start1.blend --python render_island.py -- <out_dir> <name>
"""
import math
import os
import sys

import bpy
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
out_dir, name = argv[0], argv[1]

sc = bpy.context.scene
for o in sc.objects:
    if o.name.endswith(("-TreesArea", "-RocksArea", "-NoBuildArea", "-PlaceForWaterObjects")) or "-DecorKit-" in o.name:
        o.hide_render = True

# the frame's size from the water, which spans it
frame = max((max(o.dimensions.x, o.dimensions.y) for o in sc.objects if o.name.endswith("-Water")), default=200.0)

sun_data = bpy.data.lights.new("Sun", 'SUN')
sun_data.energy = 3.2
sun_data.angle = math.radians(4)
sun = bpy.data.objects.new("Sun", sun_data)
sc.collection.objects.link(sun)
sun.rotation_euler = (math.radians(48), 0, math.radians(-32))
world = sc.world or bpy.data.worlds.new("World")
sc.world = world
world.use_nodes = True
bg = world.node_tree.nodes.get("Background")
bg.inputs[0].default_value = (0.62, 0.78, 0.98, 1)
bg.inputs[1].default_value = 0.75
sc.render.engine = 'BLENDER_EEVEE'
sc.view_settings.view_transform = 'Standard'
sc.view_settings.look = 'None'
try:
    sc.eevee.taa_render_samples = 16
except AttributeError:
    pass

cam_data = bpy.data.cameras.new("Cam")
cam = bpy.data.objects.new("Cam", cam_data)
sc.collection.objects.link(cam)
sc.camera = cam
os.makedirs(out_dir, exist_ok=True)

sc.render.resolution_x = sc.render.resolution_y = 900
cam_data.type = 'ORTHO'
cam_data.ortho_scale = frame
cam.location = (0, 0, 300)
cam.rotation_euler = (0, 0, 0)
cam_data.clip_end = 1000
sc.render.filepath = os.path.join(out_dir, f"{name}_top.png")
bpy.ops.render.render(write_still=True)

sc.render.resolution_x, sc.render.resolution_y = 1600, 900
cam_data.type = 'PERSP'
cam_data.lens = 28
p, y = math.radians(45), 0.0
d = Vector((math.sin(y) * math.cos(p), -math.cos(y) * math.cos(p), math.sin(p)))
cam.location = d * frame * 0.95
cam.rotation_euler = (-cam.location).to_track_quat('-Z', 'Y').to_euler()
sc.render.filepath = os.path.join(out_dir, f"{name}_view.png")
bpy.ops.render.render(write_still=True)
print("RENDERED", name)
