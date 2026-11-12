"""Full-figure pictures of every soldier at every level for the reference (Design/Reference.html), one
Blender session, never saves:

    blender --background Buildings-Size1-Models.blend --python render_unit_cards.py -- <out_dir> [size]

Writes <Object>.png (transparent, front three-quarter, the whole figure framed) for each Soldier* and
Siege* object in the file: SoldierSpearman.png, SoldierSpearman-lvl2.png, ... The reference generator
(Tools/docs/reference.py) embeds them from Design/reference/units/. Re-run after soldiers.py
or the siege machines change.
"""
import math
import os
import sys

import bpy
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
out_dir = argv[0]
size = int(argv[1]) if len(argv) > 1 else 160
os.makedirs(out_dir, exist_ok=True)

sc = bpy.context.scene
names = sorted(o.name for o in bpy.data.objects
               if o.parent is None and o.type == 'MESH' and not o.name.endswith("-size")
               and (o.name.startswith("Soldier") or o.name.startswith("Siege")))

for name, energy, rot in (("CardSun", 3.5, (50, 0, -25)), ("CardFill", 1.2, (60, 0, 150))):
    ld = bpy.data.lights.new(name, 'SUN')
    ld.energy = energy
    lo = bpy.data.objects.new(name, ld)
    sc.collection.objects.link(lo)
    lo.rotation_euler = tuple(math.radians(a) for a in rot)
world = sc.world or bpy.data.worlds.new("World")
sc.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[1].default_value = 0.6
sc.render.engine = 'BLENDER_EEVEE'
sc.render.film_transparent = True
sc.render.resolution_x = sc.render.resolution_y = size
sc.view_settings.view_transform = 'Standard'
cam_data = bpy.data.cameras.new("CardCam")
cam_data.type = 'ORTHO'
cam = bpy.data.objects.new("CardCam", cam_data)
sc.collection.objects.link(cam)
sc.camera = cam


def bounds(obj):
    pts = []
    for o in [obj] + list(obj.children_recursive):
        if o.type == 'MESH':
            pts += [o.matrix_world @ Vector(c) for c in o.bound_box]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return lo, hi


for name in names:
    obj = bpy.data.objects[name]
    keep = {name} | {c.name for c in obj.children_recursive}
    for o in sc.objects:
        o.hide_render = o.type == 'MESH' and o.name not in keep
    lo, hi = bounds(obj)
    centre = (lo + hi) / 2
    span = max(hi.x - lo.x, hi.y - lo.y, hi.z - lo.z)
    cam_data.ortho_scale = span * 1.15
    yaw, pitch = math.radians(-25), math.radians(15)
    direction = Vector((math.sin(yaw) * math.cos(pitch), -math.cos(yaw) * math.cos(pitch), math.sin(pitch)))
    cam.location = centre + direction * (span * 4)
    cam.rotation_euler = (centre - cam.location).to_track_quat('-Z', 'Y').to_euler()
    cam_data.clip_end = span * 10
    sc.render.filepath = os.path.join(out_dir, name + ".png")
    bpy.ops.render.render(write_still=True)
    print("CARD", name)
