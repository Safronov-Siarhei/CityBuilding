"""Renders the cutscene cast for the user's approval: a lineup (with the King, so the style can be
compared) and, for each named figure, front / three-quarter / back / face. Never saves.

    blender --background <Buildings-Size1-Models.blend> --python render_cast.py -- <out dir> CineElder CinePriest
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import ardan  # noqa: E402
import elder  # noqa: E402
import priest  # noqa: E402
from mathutils import Vector  # noqa: E402

BUILDERS = {"CineArdan": lambda: ardan.make(Vector((0, 0, 0))), "CineElder": elder.make, "CinePriest": priest.make}

argv = sys.argv[sys.argv.index("--") + 1:]
out, solos = argv[0], argv[1:]
os.makedirs(out, exist_ok=True)
for build in BUILDERS.values():
    build()

sc = bpy.context.scene
lineup = ["CineElder", "CineArdan", "CinePriest", "HeroKing"]
for i, n in enumerate(lineup):
    bpy.data.objects[n].location = (i * 0.5, 0, 0)
sc.render.engine = 'BLENDER_EEVEE'
sc.render.resolution_x, sc.render.resolution_y = 1200, 900
sc.view_settings.view_transform = 'Standard'
for name, energy, rx, rz in (("CastSun", 3.5, 50, -25), ("CastFill", 1.0, 70, 150)):
    light = bpy.data.objects.new(name, bpy.data.lights.new(name, 'SUN'))
    light.data.energy = energy
    sc.collection.objects.link(light)
    light.rotation_euler = (math.radians(rx), 0, math.radians(rz))
w = sc.world or bpy.data.worlds.new("W")
sc.world = w
w.use_nodes = True
w.node_tree.nodes["Background"].inputs[0].default_value = (0.8, 0.82, 0.88, 1)
cam = bpy.data.objects.new("CastCam", bpy.data.cameras.new("CastCam"))
sc.collection.objects.link(cam)
sc.camera = cam


def show(names):
    keep = set()
    for n in names:
        o = bpy.data.objects[n]
        keep |= {o.name} | {c.name for c in o.children_recursive}
    for o in sc.objects:
        if o.type in ('MESH', 'CURVE', 'EMPTY', 'ARMATURE'):
            o.hide_render = o.name not in keep


def shot(name, at, offset, lens):
    at = Vector(at)
    cam.location = at + Vector(offset)
    cam.data.lens = lens
    cam.rotation_euler = (at - cam.location).to_track_quat('-Z', 'Y').to_euler()
    sc.render.filepath = os.path.join(out, name + ".png")
    bpy.ops.render.render(write_still=True)
    print("RENDERED", sc.render.filepath)


show(lineup)
shot("cast_lineup", (0.75, 0, 0.3), (0.6, -3.2, 0.8), 55)
for n in solos:
    show([n])
    a = bpy.data.objects[n].location
    short = n.replace("Cine", "").lower()
    shot(short + "_front", a + Vector((0, 0, 0.29)), (0, -1.9, 0.25), 80)
    shot(short + "_three_quarter", a + Vector((0, 0, 0.29)), (1.25, -1.45, 0.4), 80)
    shot(short + "_back", a + Vector((0, 0, 0.29)), (-0.8, 1.7, 0.4), 80)
    shot(short + "_face", a + Vector((0, -0.02, 0.48)), (0.25, -0.75, 0.08), 85)
