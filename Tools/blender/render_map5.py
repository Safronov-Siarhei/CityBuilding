"""Review pictures of Map 5. Opens the .blend, adds sun, sky and cameras, renders, never saves.

    blender --background Map-5-Caldera.blend --python render_map5.py -- <out_dir> [only=name,name]
"""
import math
import os
import sys

import bpy
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
out_dir = argv[0]
only = []
for a in argv[1:]:
    if a.startswith("only="):
        only = a[5:].split(",")

sc = bpy.context.scene
for o in sc.objects:
    if o.name.endswith(("-TreesArea", "-RocksArea", "-NoBuildArea", "-PlaceForWaterObjects")) or "-DecorKit-" in o.name:
        o.hide_render = True

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
sc.render.film_transparent = False
sc.view_settings.view_transform = 'Standard'
sc.view_settings.look = 'None'
try:
    sc.eevee.taa_render_samples = 24
    sc.eevee.shadow_ray_count = 2
except AttributeError:
    pass

cam_data = bpy.data.cameras.new("Cam")
cam = bpy.data.objects.new("Cam", cam_data)
sc.collection.objects.link(cam)
sc.camera = cam


def aim(target, pitch, yaw, dist):
    p, y = math.radians(pitch), math.radians(yaw)
    d = Vector((math.sin(y) * math.cos(p), -math.cos(y) * math.cos(p), math.sin(p)))
    cam.location = Vector(target) + d * dist
    cam.rotation_euler = (Vector(target) - cam.location).to_track_quat('-Z', 'Y').to_euler()


# name: (kind, target, pitch, yaw, distance, resolution)
SHOTS = {
    "overview_top": ("ortho", (0, 0, 0), 90, 0, 300, (1800, 1800)),
    "overview_south": ("persp", (0, 0, 0), 42, 0, 205, (1920, 1080)),
    "caldera": ("persp", (0, -10, 0), 48, 0, 110, (1920, 1080)),
    "town": ("game", (-6, -25, 0), 50, 0, 34, (1920, 1080)),
    "lake": ("game", (18, 0, 0), 50, 0, 36, (1920, 1080)),
    "main_base": ("game", (0, 58, 0), 55, 0, 64, (1920, 1080)),
    "north_gorge": ("game", (0, 34, 0), 50, 0, 30, (1920, 1080)),
    "east_gorge": ("game", (50, -8, 0), 50, 0, 34, (1920, 1080)),
    "west_gorge": ("game", (-50, -16, 0), 50, 0, 30, (1920, 1080)),
    "south_beach": ("game", (0, -80, 0), 45, 0, 40, (1920, 1080)),
}

os.makedirs(out_dir, exist_ok=True)
for name, (kind, target, pitch, yaw, dist, res) in SHOTS.items():
    if only and name not in only:
        continue
    sc.render.resolution_x, sc.render.resolution_y = res
    if kind == "ortho":
        cam_data.type = 'ORTHO'
        cam_data.ortho_scale = 202
        cam.location = (0, 0, dist)
        cam.rotation_euler = (0, 0, 0)
        cam_data.clip_end = 1000
    else:
        cam_data.type = 'PERSP'
        cam_data.lens = 31 if kind == "game" else 26
        cam_data.clip_end = 1000
        aim(target, pitch, yaw, dist)
    sc.render.filepath = os.path.join(out_dir, f"map5_{name}.png")
    bpy.ops.render.render(write_still=True)
    print("RENDERED", sc.render.filepath)
