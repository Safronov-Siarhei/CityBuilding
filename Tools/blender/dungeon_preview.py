"""Review shots of a dungeon map: one from straight above, and close views at the game camera's
angle on chosen spots, lit dim with a point light at every DungeonLight_* Empty. Never saves.

    blender --background X.blend --python dungeon_preview.py -- <ObjectName> <out_dir> name:x:y [name:x:y ...]

x, y are grid metres (0..100) of the spot a close view looks at.
"""
import math
import os
import sys

import bpy
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
target_name, out_dir = argv[0], argv[1]
spots = []
for a in argv[2:]:
    name, x, y = a.split(":")
    spots.append((name, float(x), float(y)))

sc = bpy.context.scene
target = bpy.data.objects[target_name]
keep = {target_name} | {c.name for c in target.children_recursive}
for o in sc.objects:
    o.hide_render = o.name not in keep

LIGHTS = {  # kind: (colour, watts, radius m)
    'lantern': ((1.0, 0.72, 0.38), 60, 0.1),
    'fire': ((1.0, 0.55, 0.2), 120, 0.2),
    'crystal': ((0.75, 0.4, 1.0), 40, 0.1),
    'mushroom': ((0.4, 0.8, 1.0), 25, 0.1),
    'day': ((1.0, 0.95, 0.85), 400, 1.0),
}
for c in target.children:
    if not c.name.startswith("DungeonLight_"):
        continue
    kind = c.name.split("_")[1]
    colour, watts, radius = LIGHTS.get(kind, LIGHTS['lantern'])
    data = bpy.data.lights.new("L_" + c.name, 'POINT')
    data.color, data.energy, data.shadow_soft_size = colour, watts, radius
    lo = bpy.data.objects.new("L_" + c.name, data)
    sc.collection.objects.link(lo)
    lo.location = c.matrix_world.translation

sun_data = bpy.data.lights.new("PreviewSun", 'SUN')
sun_data.energy = 0.9
sun = bpy.data.objects.new("PreviewSun", sun_data)
sc.collection.objects.link(sun)
sun.rotation_euler = (math.radians(35), 0, math.radians(-30))

world = sc.world or bpy.data.worlds.new("PreviewWorld")
sc.world = world
world.use_nodes = True
bg = world.node_tree.nodes.get("Background")
bg.inputs[0].default_value = (0.02, 0.02, 0.03, 1)
bg.inputs[1].default_value = 1.0

sc.render.engine = 'BLENDER_EEVEE'
sc.render.film_transparent = False
sc.view_settings.view_transform = 'Standard'
sc.view_settings.look = 'None'
try:
    sc.eevee.taa_render_samples = 32
except AttributeError:
    pass

cam_data = bpy.data.cameras.new("PreviewCam")
cam = bpy.data.objects.new("PreviewCam", cam_data)
sc.collection.objects.link(cam)
sc.camera = cam
origin = target.location
os.makedirs(out_dir, exist_ok=True)


def shoot(name, look_at, pitch, yaw, dist, lens=40, res=(1400, 1000), ortho=0.0):
    sc.render.resolution_x, sc.render.resolution_y = res
    if ortho:
        cam_data.type = 'ORTHO'
        cam_data.ortho_scale = ortho
    else:
        cam_data.type = 'PERSP'
        cam_data.lens = lens
    p, y = math.radians(pitch), math.radians(yaw)
    direction = Vector((math.sin(y) * math.cos(p), -math.cos(y) * math.cos(p), math.sin(p)))
    cam.location = look_at + direction * dist
    cam.rotation_euler = (look_at - cam.location).to_track_quat('-Z', 'Y').to_euler()
    cam_data.clip_end = 1000
    sc.render.filepath = os.path.join(out_dir, f"{target_name}_{name}.png")
    bpy.ops.render.render(write_still=True)
    print("RENDERED", sc.render.filepath)


sun_data.energy = 2.2              # the plan reads better in daylight
shoot("top", origin, 89.9, 0, 150, res=(1400, 1400), ortho=102)
sun_data.energy = 0.9
for name, x, y in spots:
    shoot(name, origin + Vector((x - 50, y - 50, 0)), 50, 0, 22)
