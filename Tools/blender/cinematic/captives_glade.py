"""The level-3 prologue «Пленные», shot 6 (the user, 2026-11-22: «поляна с орками и клетки с пленными на ней, в одной из
клеток сидит старик»): a forest glade where the monsters keep the townsfolk they dragged off. A set of its own, far from
the town in Unity, like the capital and the bridge. Renders go to the user for approval before Unity sees them.

Laid out in FIGURE units (Ардан 0.6 tall), the camera's side at -y:
  - a trodden dirt clearing in a ring of the game's trees (figure.SET_SCALE, like the bridge set), dead trees and
    stones at its edge;
  - the monsters' camp from the game's own approved orc decor, grown to the cine monsters' size (GAME_ORC):
    the campfire with its spit and seated orcs in the middle, a war tent, a meat rack, a hide rack, a totem,
    skull stakes, a war drum;
  - four prisoners' cages in an arc at the back (CineCaptiveCage: orccage1's cage2 and cage1 without their
    game prisoners, grown to the townsfolk's size), the cine townsfolk inside; the OLD MAN (CineElder) alone
    in the third one, gripping the bars -- the shot's push-in ends on him;
  - the townsfolk's loot by the tent: sacks, a barrel, a cart wheel;
  - about ten monsters (the user): two at the fire, two cine guards, and the game's orcs (CAMP) -- the CHIEF as the
    mini-boss before his bone throne, a brute, grunts, a thrower, a torchbearer.

    blender --background <a COPY of Buildings-Size1-Models.blend> --python captives_glade.py -- render <out dir>
"""
import math
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))
import figure as fg  # noqa: E402
import kit  # noqa: E402
from kit import Builder  # noqa: E402

S = fg.SET_SCALE
GAME_ORC = 0.66 / 0.58        # a game orc (0.58) grown to a cine monster's height (0.66)
GAME_FOLK = 0.6 / 0.45        # a game citizen (0.45) grown to the cine townsfolk's (0.6)
OBJ, CAGE_OBJ = "CineCaptivesGlade", "CineCaptiveCage"
CLEARING = 3.4
fg.plain('grass', 'grass_dark', 'burlap', 'sand', 'leaf', 'wood_dark', 'wood_mid', 'wood_light', 'stone_mid',
         'stone_dark', 'iron', 'hay')

# the cages: (x, y, turn to face the fire in degrees, kind, who is inside [(figure, dx, dy)])
CAGES = [
    (-2.0, 1.55, -28, 2, [("CinePeasant", -0.12, 0.05)]),
    (-0.7, 2.05, -9, 1, [("CinePeasantWoman", -0.13, 0.02), ("CineGirl", 0.15, -0.05)]),
    (0.7, 2.05, 9, 2, [("CineElder", 0.0, -0.12)]),
    (2.0, 1.55, 28, 1, [("CineMerchant", 0.12, 0.05), ("CineBoy", -0.15, -0.03)]),
]
ELDER_CAGE = 2

# the game's decor: (object, x, y, turn, scale)
DECOR = [
    ("OrcCampfire1", 0.0, 0.1, 15, GAME_ORC),
    ("OrcWarTent1-lvl1", -3.0, -0.2, 70, GAME_ORC),
    ("OrcMeatRack1", 3.3, 1.7, -100, GAME_ORC),
    ("OrcHideRack1", -2.9, 1.5, 110, GAME_ORC),
    ("OrcTotem1-lvl1", 0.0, 3.1, 180, GAME_ORC),
    ("OrcSkullStake1", -2.3, -1.6, 10, GAME_ORC * 0.7),
    ("OrcSkullStake1", 2.4, -1.5, -15, GAME_ORC * 0.7),
    ("OrcWarDrum1", 1.7, -1.4, -30, GAME_ORC * 0.55),
    ("OrcBoneThrone1", 2.45, 0.85, -60, GAME_ORC),
    ("OrcDeadTree1-A", -3.6, 2.9, 30, GAME_ORC),
    ("OrcDeadTree1-C", 3.8, 2.6, 200, GAME_ORC),
]

# guards standing: (figure, x, y, facing degrees about z; 0 = facing -y, to the camera)
GUARDS = [("CineOrcSpear", -1.25, 1.0, 20), ("CineOrcTorch", 1.35, 0.95, -25)]

# the rest of the camp, the game's own orc models grown like the decor (the user, 2026-11-22: about TEN monsters in
# all, the CHIEF as a mini-boss): (object, x, y, turn, scale). With the two at the fire and the two guards: 10.
# The chief (the game's OrcChief, ~1.7x a monster) stands before his bone throne on the right.
CAMP = [
    ("OrcChief", 2.2, 0.55, -55, GAME_ORC),
    ("OrcBrute", 3.0, 0.1, -80, GAME_ORC),
    ("OrcGrunt", -3.35, 0.7, -75, GAME_ORC),
    ("OrcThrower", -2.0, -0.35, -60, GAME_ORC),
    ("OrcTorchbearer", -0.25, -1.45, 170, GAME_ORC),
    ("OrcGrunt", 0.95, -0.95, 140, GAME_ORC),
]


def cage_mesh(kind):
    """orccage1's cage (kind 1: lashed poles and a rope door; kind 2: sharpened poles, iron bands, a padlock)
    with no prisoners of its own, in GAME units; the caller grows it to the townsfolk's size."""
    oc = _orccage1()
    b = Builder()
    (oc['cage2'] if kind == 2 else oc['cage1'])(b, prisoners=0)
    return b


def _quiet(path, name):
    """A model script's namespace with its module-level main() (which rebuilds its models and SAVES the open .blend)
    cut out and kit.run silenced; scripts it borrows load the same way."""
    import re
    with open(path, encoding='utf-8') as fh:
        src = re.sub(r"(?m)^main\(\)\s*$", "", fh.read())
    ns = {'__file__': path, '__name__': 'borrowed_' + name}
    real_run, real_borrow = kit.run, kit.borrow
    kit.run = lambda *a, **k: None
    kit.borrow = lambda script: _quiet(os.path.join(os.path.dirname(HERE), script), os.path.splitext(script)[0])
    try:
        exec(compile(src, path, 'exec'), ns)
    finally:
        kit.run, kit.borrow = real_run, real_borrow
    return ns


def _orccage1():
    """orccage1.py's builders. Importing it (or kit.borrow) runs its main() and that of orcstone1.py it borrows:
    they rebuild their models and SAVE the open .blend (it happened once, 2026-11-22; the repo copy was restored
    from the Drive). Render on a COPY of the .blend anyway."""
    return _quiet(os.path.join(os.path.dirname(HERE), "orccage1.py"), "orccage1")


def ground(b):
    big = 40
    b.box(-big, big, -big, big, -0.05, 0.0, 'grass_dark')
    b.cylinder(0, 0.4, 0.0, 0.004, CLEARING, 'grass', sides=24)               # a lighter trodden ring
    b.cylinder(0, 0.4, 0.004, 0.008, CLEARING - 0.6, 'burlap', sides=22)      # the bare earth
    rnd = random.Random(5)
    for _ in range(14):                                                       # ash and mud
        a, r = rnd.uniform(0, 6.283), rnd.uniform(0.4, CLEARING - 0.4)
        if rnd.random() < 0.5:                                                # soft dark ash and mud, never flat
            b.blob(r * math.cos(a), 0.4 + r * math.sin(a), 0.004, rnd.uniform(0.08, 0.2), 0.012, 'wood_dark', sides=9)
    for _ in range(60):                                                       # grass tufts round the edge
        a = rnd.uniform(0, 6.283)
        r = rnd.uniform(CLEARING - 0.5, CLEARING + 1.2)
        x, y = r * math.cos(a), 0.4 + r * math.sin(a)
        for _ in range(4):
            h = rnd.uniform(0.06, 0.14)
            b.obox((x + rnd.uniform(-0.05, 0.05), y + rnd.uniform(-0.05, 0.05), h / 2), (1, 0, 0), (0, 1, 0),
                   (rnd.uniform(-0.3, 0.3), rnd.uniform(-0.3, 0.3), 1), 0.008, 0.008, h / 2,
                   'leaf' if rnd.random() < 0.5 else 'grass')


def track(b):
    """The way in: a trodden dirt track from the clearing out through the forest to -y, grass at its edges (ruts drew as
    road markings: dropped)."""
    y0, y1 = -14.0, 0.4 - CLEARING + 0.7
    b.box(-PATH_HALF, PATH_HALF, y0, y1, 0.004, 0.008, 'burlap')
    rnd = random.Random(9)
    for _ in range(40):
        y = rnd.uniform(y0, y1)
        x = rnd.choice((-1, 1)) * rnd.uniform(PATH_HALF - 0.1, PATH_HALF + 0.15)
        h = rnd.uniform(0.05, 0.12)
        b.obox((x, y, h / 2), (1, 0, 0), (0, 1, 0), (rnd.uniform(-0.3, 0.3), 0, 1), 0.008, 0.008, h / 2, 'leaf')


def loot(b):
    """The townsfolk's goods thrown down by the tent: sacks, a barrel on its side, a cart wheel, a chest."""
    for x, y, r in ((-2.3, -1.0, 0.12), (-2.05, -1.2, 0.1), (-2.45, -1.3, 0.11), (-2.2, -0.75, 0.09)):
        b.blob(x, y, 0.0, r, r * 1.4, 'burlap', sides=7)
        b.cylinder(x, y, r * 1.25, r * 1.38, r * 0.35, 'hay', sides=6)
    b.hcylinder('x', (-1.75, -1.45, 0.1), 0.13, 0.1, 'wood_mid', sides=10)
    for dx in (-0.09, 0.09):
        b.hcylinder('x', (-1.75 + dx, -1.45, 0.1), 0.012, 0.106, 'iron', sides=10)
    b.box(-2.75, -2.45, -1.6, -1.4, 0.0, 0.16, 'wood_dark', bevel=0.01)          # a chest, its lid off
    b.box(-2.74, -2.46, -1.59, -1.41, 0.16, 0.17, 'gold')
    import cage_cart
    m = len(b.bm.verts)
    cage_cart.wheel(b, 0.0, 0.0, r=0.2)                                           # a cart wheel, lying flat
    for v in list(b.bm.verts)[m:]:
        x, y, z = v.co.x, v.co.y, v.co.z - 0.2
        v.co.x, v.co.y, v.co.z = -1.55 + z, -0.95 + y, 0.02 + x


def glade():
    b = Builder()
    ground(b)
    track(b)
    loot(b)
    return b


def _copy(name, x, y, turn_deg, scale):
    import bpy
    src = bpy.data.objects[name]
    o = src.copy()
    o.name = f"Glade {name} {x:g},{y:g}"
    o.location = (x, y, 0.0)
    o.rotation_euler = (0, 0, math.radians(turn_deg))
    o.scale = (scale,) * 3
    o.hide_render = False
    o.hide_viewport = False
    fg.ensure_collection().objects.link(o)
    return o


PATH_HALF = 1.0                # the one way into the glade: a track out through the forest to -y (the camera's side)


def _trees():
    """A DENSE forest all round the glade (the user, 2026-11-22: «густой лес, проход с одной стороны, некоторые деревья
    сухие»): trees packed at least TREE_GAP apart from the clearing's edge out to 13 units, a gap only for the track at
    -y; about one in six is a dead tree (the game's OrcDeadTree1, grey-blue and bare)."""
    rnd = random.Random(11)
    out, placed = [], []
    kinds = ("Tree3", "Tree10", "Tree2", "Tree1", "Tree5", "Tree7", "Tree4", "Tree8")
    dead = ("OrcDeadTree1-A", "OrcDeadTree1-B", "OrcDeadTree1-C")
    gap = 0.75
    tries = 0
    while len(placed) < 230 and tries < 20000:
        tries += 1
        a = rnd.uniform(0, 6.283)
        r = CLEARING + 1.1 + (13.0 - CLEARING) * rnd.random() ** 1.4      # thickest at the edge, clear of the camp
        x, y = r * math.cos(a), 0.4 + r * math.sin(a)
        if y < 0.0 and abs(x) < PATH_HALF + 1.7:          # the track: no tree within a canopy's reach of it (the user)
            continue
        if any((x - px) ** 2 + (y - py) ** 2 < gap * gap for px, py in placed):
            continue
        placed.append((x, y))
        if rnd.random() < 0.17:
            out.append((rnd.choice(dead), x, y, rnd.uniform(0, 360), rnd.uniform(0.5, 0.7)))
        else:
            out.append((kinds[len(placed) % len(kinds)], x, y, rnd.uniform(0, 360), rnd.uniform(0.85, 1.3)))
    for i in range(12):                                                           # stones at the edge
        a = rnd.uniform(0, 6.283)
        r = rnd.uniform(CLEARING - 0.2, CLEARING + 0.6)
        x, y = r * math.cos(a), 0.4 + r * math.sin(a)
        if y < 0.0 and abs(x) < 2.5:                                                # not in front of the camera
            continue
        out.append(("Stone%d" % (1 + i % 10), x, y, rnd.uniform(0, 360), 0.22))
    return out


def make():
    import bpy
    objs = [fg.install_static(OBJ, glade(), scale=1.0)]
    for name, x, y, turn, k in DECOR:
        objs.append(_copy(name, x, y, turn, k))
    for name, x, y, turn, k in CAMP:
        objs.append(_copy(name, x, y, turn, k))
    for name, x, y, turn, k in _trees():
        objs.append(_copy(name, x, y, turn, S * k))
    cages = {}
    for i, (x, y, turn, kind, inside) in enumerate(CAGES):
        if kind not in cages:
            cages[kind] = fg.install_static(f"{CAGE_OBJ}{kind}", cage_mesh(kind), scale=GAME_FOLK)
            cages[kind].hide_render = True
        o = cages[kind].copy()
        o.name = f"Glade cage {i}"
        o.location = (x, y, 0.0)
        o.rotation_euler = (0, 0, math.radians(turn + 180))     # the cage's door (-y) faces the fire
        o.hide_render = False
        fg.ensure_collection().objects.link(o)
        objs.append(o)
    return objs


def _figure(name, location, turn_deg):
    import cavalry  # noqa: F401  (figure modules register their shared materials)
    import elder
    import orcs_cast
    import townsfolk
    if name == "CineElder":
        o = elder.make(location)
    elif name.startswith("CineOrc"):
        o = orcs_cast.make(name, location)
    else:
        o = townsfolk.make(name, location)
    o.rotation_euler = (0, 0, math.radians(turn_deg))
    return o


def render(out):
    import bpy
    from mathutils import Vector
    os.makedirs(out, exist_ok=True)
    objs = make()
    folk = []
    floor = 0.09 * GAME_FOLK                      # the cages' floors
    for i, (x, y, turn, kind, inside) in enumerate(CAGES):
        a = math.radians(turn + 180)
        for name, dx, dy in inside:
            # the cage's local (dx, dy) with -y its door, turned with the cage
            lx, ly = dx, -dy
            px = x + lx * math.cos(a) - ly * math.sin(a)
            py = y + lx * math.sin(a) + ly * math.cos(a)
            folk.append(_figure(name, (px, py, floor), turn))
    for name, x, y, turn in GUARDS:
        folk.append(_figure(name, (x, y, 0.0), turn))
    sc = bpy.context.scene
    keep = {o.name for o in objs}
    for o in folk:
        keep |= {o.name} | {c.name for c in o.children_recursive}
    for o in sc.objects:
        if o.type in ('MESH', 'CURVE', 'EMPTY', 'ARMATURE'):
            o.hide_render = o.name not in keep
    sc.render.engine = 'BLENDER_EEVEE'
    sc.render.resolution_x, sc.render.resolution_y = 1600, 900
    sc.view_settings.view_transform = 'Standard'
    for name, energy, rx, rz in (("SetSun", 3.0, 42, -35), ("SetFill", 0.9, 60, 150)):
        light = bpy.data.objects.new(name, bpy.data.lights.new(name, 'SUN'))
        light.data.energy = energy
        light.data.angle = math.radians(2)
        sc.collection.objects.link(light)
        light.rotation_euler = (math.radians(rx), 0, math.radians(rz))
    w = sc.world or bpy.data.worlds.new("W")
    sc.world = w
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.55, 0.72, 1.0, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 0.8
    cam = bpy.data.objects.new("SetCam", bpy.data.cameras.new("SetCam"))
    cam.data.clip_end = 300
    sc.collection.objects.link(cam)
    sc.camera = cam
    ex, ey, _, _, _ = CAGES[ELDER_CAGE]
    for name, at, frm, lens in (("glade_wide", (0, 1.0, 0.3), (0.0, -6.0, 2.6), 28),
                                ("glade_high", (0, 0.2, 0), (-4.0, -11.0, 12.0), 26),
                                ("glade_side", (0, 1.0, 0.4), (2.9, -2.0, 1.4), 26),
                                ("glade_elder", (ex, ey, 0.55), (ex - 0.25, ey - 1.6, 0.65), 40),
                                ("glade_chief", (2.2, 0.55, 0.7), (0.4, -1.6, 1.0), 35)):
        at, frm = Vector(at), Vector(frm)
        cam.location = frm
        cam.data.lens = lens
        cam.rotation_euler = (at - frm).to_track_quat('-Z', 'Y').to_euler()
        sc.render.filepath = os.path.join(out, name + ".png")
        bpy.ops.render.render(write_still=True)
        print("RENDERED", sc.render.filepath)


if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if argv and argv[0] == "render":
        render(argv[1])
    else:
        make()
