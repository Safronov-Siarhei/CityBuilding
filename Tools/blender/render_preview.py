"""Review shots of one model. Opens the .blend, stages ground, sun and camera, renders, and
never saves -- the file the user edits is left exactly as the model script wrote it.

    blender --background X.blend --python render_preview.py -- <ObjectName> <out_dir> [with=Obj1,Obj2]
"""
import math
import os
import sys

import bpy
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
target_name, out_dir = argv[0], argv[1]
extra = []
for a in argv[2:]:
    if a.startswith("with="):
        extra = [s for s in a[5:].split(",") if s]

sc = bpy.context.scene
target = bpy.data.objects[target_name]
for o in sc.objects:
    o.hide_render = o.name not in [target_name] + extra

# ground, light, sky
bpy.ops.mesh.primitive_plane_add(size=60, location=(target.location.x, target.location.y, -0.001))
ground = bpy.context.active_object
gm = bpy.data.materials.new("PreviewGround")
gm.use_nodes = True
gm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.42, 0.62, 0.16, 1)
gm.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 1.0
ground.data.materials.append(gm)

sun_data = bpy.data.lights.new("PreviewSun", 'SUN')
sun_data.energy = 2.4
sun_data.angle = math.radians(3)
sun = bpy.data.objects.new("PreviewSun", sun_data)
sc.collection.objects.link(sun)
sun.rotation_euler = (math.radians(45), 0, math.radians(-35))

world = sc.world or bpy.data.worlds.new("PreviewWorld")
sc.world = world
world.use_nodes = True
bg = world.node_tree.nodes.get("Background")
bg.inputs[0].default_value = (0.55, 0.72, 0.95, 1)
bg.inputs[1].default_value = 0.55

sc.render.engine = 'BLENDER_EEVEE'
sc.render.resolution_x, sc.render.resolution_y = 1400, 1000
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

dims = target.dimensions
centre = target.location + Vector((0, 0, dims.z * 0.4))
span = max(dims.x, dims.y, dims.z)

shots = [
    ("game_front", 50, -30, 2.4, 40),   # the in-game angle: 50 deg down
    ("game_back", 50, 150, 2.4, 40),
    ("low_front", 25, -20, 2.1, 40),
]
if extra:
    shots.append(("scale", 50, -30, 3.4, 40))

os.makedirs(out_dir, exist_ok=True)
for name, pitch, yaw, dist_k, lens in shots:
    cam_data.lens = lens
    for o in sc.objects:
        if o.name in extra:
            o.hide_render = name != "scale"
    p, y = math.radians(pitch), math.radians(yaw)
    direction = Vector((math.sin(y) * math.cos(p), -math.cos(y) * math.cos(p), math.sin(p)))
    look_at = centre if name != "scale" else centre + Vector((0, -1.5, 0))
    cam.location = look_at + direction * span * dist_k
    cam.rotation_euler = (look_at - cam.location).to_track_quat('-Z', 'Y').to_euler()
    sc.render.filepath = os.path.join(out_dir, f"{target_name}_{name}.png")
    bpy.ops.render.render(write_still=True)
    print("RENDERED", sc.render.filepath)
