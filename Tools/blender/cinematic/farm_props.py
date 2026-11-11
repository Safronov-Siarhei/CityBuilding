"""Country props for the opening film's outskirts (the user, 2026-11-11: «добавь стоги сена возле
дорог, кусты, загоны с животными»). Built from the game's approved pieces -- the Pig Farm's pigs, the
details' hay bales, rail fence and trough, the village's hens and washing line -- in the same kit and
grown by figure.SET_SCALE. Renders go to the user for approval before Unity sees them.

    blender --background <Buildings-Size1-Models.blend> --python farm_props.py -- render <out dir>
"""
import math
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))
import details as d  # noqa: E402
import figure as fg  # noqa: E402
import kit  # noqa: E402
from kit import Builder  # noqa: E402

kit.run = lambda *a, **k: None     # pigfarm1 runs kit.run on import (it would install and save)
import pigfarm1  # noqa: E402
import village  # noqa: E402

fg.plain('hay', 'roof_straw', 'roof_straw_dark', 'wood_light', 'wood_mid', 'wood_dark', 'plaster_pink',
         'plaster_white', 'roof_red', 'plaster_yellow', 'water', 'leaf', 'grass')


def haystack():
    """A domed haystack round a pole, the hay in rough layers, a little fallen hay at its foot."""
    b = Builder()
    rnd = random.Random(3)
    b.box(-0.012, 0.012, -0.012, 0.012, 0, 0.62, 'wood_mid')                        # the pole
    # a round-shouldered mound: a wide body, a rounder top, a straw cap tied at the pole
    b.blob(0, 0, 0.0, 0.3, 0.42, 'hay', sides=10)
    b.blob(0, 0, 0.12, 0.25, 0.4, 'roof_straw', sides=10)
    b.blob(0, 0, 0.3, 0.14, 0.26, 'hay', sides=8)
    b.cylinder(0, 0, 0.47, 0.5, 0.035, 'roof_straw_dark', sides=6)
    for k in range(7):
        a = rnd.uniform(0, 6.28)
        rr = rnd.uniform(0.27, 0.36)
        b.blob(math.cos(a) * rr, math.sin(a) * rr, 0.01, 0.06, 0.015, 'roof_straw_dark', sides=4)
    return b


def hay_bales():
    """Bales stacked by the road: three on the ground, two on top, one fallen."""
    b = Builder()
    for x in (-0.2, 0.0, 0.2):
        d.hay_bale(b, x, 0.0, 90)
    for x in (-0.1, 0.1):
        d.hay_bale(b, x, 0.0, 90, z0=0.15)
    d.hay_bale(b, 0.42, 0.12, 30)
    return b


def pig_pen():
    """A rail-fenced pen: pigs, a trough, a mud wallow, a gate, a little hay."""
    b = Builder()
    w, h = 0.8, 0.6
    gate = 0.12
    d.rail_fence(b, [(-gate, -h), (-w, -h), (-w, h), (w, h), (w, -h), (gate, -h)], post_step=0.4)
    b.cylinder(0.3, 0.2, 0.0, 0.008, 0.26, 'wood_dark', sides=10)                       # the wallow
    d.trough(b, -0.55, -0.2, 0.42)
    for x, y, a, mud in ((0.25, 0.15, 30, True), (-0.35, 0.2, 100, False), (0.1, -0.3, 200, False), (-0.45, -0.25, -20, True)):
        pigfarm1.pig(b, x, y, a, size=0.8, muddy=mud)
    d.hay_bale(b, 0.6, 0.42, 0)
    return b


def hen_yard():
    """A low wattle-and-rail run with hens pecking, a coop box and a water dish."""
    b = Builder()
    w, h = 0.5, 0.38
    d.rail_fence(b, [(-w, -h), (-w, h), (w, h), (w, -h), (-w, -h)], post_step=0.25)
    b.box(0.22, 0.46, 0.12, 0.34, 0, 0.2, 'wood_mid', bevel=0.006)                     # the coop
    d.shed_roof(b, 'x', 0.2, 0.48, 0.1, 0.36, 0.26, 0.2, **d.STRAW)
    b.box(0.31, 0.37, 0.11, 0.12, 0.02, 0.1, 'wood_dark')                                # its little door
    b.cylinder(-0.3, 0.2, 0, 0.02, 0.05, 'water', sides=8)
    for i, (x, y, f) in enumerate(((-0.2, -0.1, 1), (0.05, 0.05, -1), (0.1, -0.2, 1), (-0.32, 0.05, -1), (-0.05, -0.25, 1))):
        village.hen(b, x, y, f)
        village.hen(b, x + 0.1, y + 0.12, -f)
    return b


def washing():
    """A washing line between two posts, the village's own."""
    b = Builder()
    village.washing_line(b, -0.4, 0.4, 0.0)
    return b


SETS = {
    "CineHaystack": haystack,
    "CineHayBales": hay_bales,
    "CinePigPen": pig_pen,
    "CineHenYard": hen_yard,
    "CineWashing": washing,
}


def make(name, location=(0, 0, 0)):
    return fg.install_static(name, SETS[name](), location=location)


def render(out):
    """The five pieces in a row with Ардан for scale, then each closer."""
    import bpy
    from mathutils import Vector
    import ardan
    os.makedirs(out, exist_ok=True)
    placed = {"CineHaystack": (-3.6, 0, 0), "CineHayBales": (-1.8, 0, 0), "CinePigPen": (0.6, 0.2, 0),
              "CineHenYard": (3.4, 0.2, 0), "CineWashing": (5.4, 0.4, 0)}
    objs = [make(n, loc) for n, loc in placed.items()]
    objs.append(ardan.make(Vector((-0.9, -1.2, 0))))
    sc = bpy.context.scene
    keep = {o.name for o in objs}
    for o in sc.objects:
        o.hide_render = o.name not in keep
    sc.render.engine = 'BLENDER_EEVEE'
    sc.render.resolution_x, sc.render.resolution_y = 1600, 900
    sc.view_settings.view_transform = 'Standard'
    for name, energy, rx, rz in (("SetSun", 3.5, 50, -25), ("SetFill", 1.0, 70, 150)):
        light = bpy.data.objects.new(name, bpy.data.lights.new(name, 'SUN'))
        light.data.energy = energy
        sc.collection.objects.link(light)
        light.rotation_euler = (math.radians(rx), 0, math.radians(rz))
    w = sc.world or bpy.data.worlds.new("W")
    sc.world = w
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.8, 0.82, 0.88, 1)
    cam = bpy.data.objects.new("SetCam", bpy.data.cameras.new("SetCam"))
    sc.collection.objects.link(cam)
    sc.camera = cam
    shots = [("all", (0.9, 0.2, 0.5), (0.0, -11.0, 5.0), 38)]
    for n, (x, y, _) in placed.items():
        shots.append((n.replace("Cine", "").lower(), (x, y, 0.4), (1.6, -2.8, 1.6), 40))
    for name, at, off, lens in shots:
        at = Vector(at)
        cam.location = at + Vector(off)
        cam.data.lens = lens
        cam.rotation_euler = (at - cam.location).to_track_quat('-Z', 'Y').to_euler()
        sc.render.filepath = os.path.join(out, name + ".png")
        bpy.ops.render.render(write_still=True)


if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if argv and argv[0] == "render":
        render(argv[1])
