"""Mission 3b «Клетка», the opening scene (storyboard v3, APPROVED by the user 2026-11-23): the King's column with the
cage cart rides a forest road; a felled tree blocks it; ~20 monsters pour out of the trees on both sides. A set of its
own, far from any town in Unity, like the glade. Renders go to the user for approval before Unity sees them.

Laid out in FIGURE units (Ардан 0.6 tall), the column riding to +y:
  - a dirt road ROAD_HALF wide each side, straight through the ambush stretch (|y| < STRAIGHT) and bending away at
    both ends, so no shot looks down an endless corridor;
  - grass verges with ferns, the game's bushes and stones -- room for the fight between the road and the trees;
  - a DENSE forest of the game's trees (figure.SET_SCALE, like the glade) close to both verges, a few dead ones;
  - the FELLED TREE across the road at BLOCK_Y: its fresh-cut stump on the left verge with wood chips, the trunk
    lying over the road, its crown in the right verge (the monsters felled it for the ambush).

    blender --background <a COPY of Buildings-Size1-Models.blend> --python forest_road.py -- render <out dir>
"""
import math
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))
import figure as fg  # noqa: E402
from kit import Builder  # noqa: E402

S = fg.SET_SCALE
OBJ = "CineForestRoad"
ROAD_HALF = 0.9           # two riders abreast with room to turn; the cart (0.63 wide) with room to pass a rider
VERGE = 1.25              # grass between the road's edge and the first trees: where the fight spills over
STRAIGHT = 10.0           # the straight stretch round the ambush
BEND = 0.045              # past it the road bends: x = ±BEND * (|y| - STRAIGHT)^2
REACH = 26.0              # the set's length each way along y
BLOCK_Y = 5.6             # the felled tree across the road
fg.plain('grass', 'grass_dark', 'burlap', 'sand', 'leaf', 'wood_dark', 'wood_mid', 'wood_light', 'stone_mid',
         'stone_dark', 'hay')


def road_x(y):
    """The road's centre at y: straight through the ambush, bending right ahead (+y) and left behind (-y)."""
    d = abs(y) - STRAIGHT
    if d <= 0:
        return 0.0
    return BEND * d * d * (1 if y > 0 else -1)


def _path(step=0.5):
    n = int(2 * REACH / step)
    return [(road_x(-REACH + i * step), -REACH + i * step) for i in range(n + 1)]


PATH = None


def road_dist(x, y):
    """How far (x, y) is from the road's centre line."""
    global PATH
    if PATH is None:
        PATH = _path(0.25)
    return min(math.hypot(x - px, y - py) for px, py in PATH)


def ground(b):
    big = 45
    b.box(-big, big, -big, big, -0.05, 0.0, 'grass_dark')
    pts = _path(0.5)
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
        dx, dy = x1 - x0, y1 - y0
        ln = math.hypot(dx, dy)
        ux, uy = dx / ln, dy / ln
        # the lighter worn verge, then the dirt on it (overlapping pieces, no seams on the bends)
        b.obox((cx, cy, 0.002), (-uy, ux, 0), (ux, uy, 0), (0, 0, 1), ROAD_HALF + 0.35, ln / 2 + 0.06, 0.002, 'grass')
        b.obox((cx, cy, 0.006), (-uy, ux, 0), (ux, uy, 0), (0, 0, 1), ROAD_HALF, ln / 2 + 0.05, 0.002, 'burlap')
    rnd = random.Random(7)
    for _ in range(70):                                         # mud, darker earth, pebbles on the road
        y = rnd.uniform(-REACH + 1, REACH - 1)
        x = road_x(y) + rnd.uniform(-ROAD_HALF + 0.15, ROAD_HALF - 0.15)
        if rnd.random() < 0.55:
            b.blob(x, y, 0.006, rnd.uniform(0.07, 0.18), 0.01, 'wood_dark', sides=8)
        else:
            b.blob(x, y, 0.006, rnd.uniform(0.02, 0.04), 0.025, 'stone_mid', sides=5)
    for _ in range(260):                                        # grass tufts along both edges
        y = rnd.uniform(-REACH, REACH)
        side = rnd.choice((-1, 1))
        x = road_x(y) + side * rnd.uniform(ROAD_HALF - 0.05, ROAD_HALF + VERGE)
        for _ in range(3):
            h = rnd.uniform(0.05, 0.13)
            b.obox((x + rnd.uniform(-0.05, 0.05), y + rnd.uniform(-0.05, 0.05), h / 2), (1, 0, 0), (0, 1, 0),
                   (rnd.uniform(-0.3, 0.3), rnd.uniform(-0.3, 0.3), 1), 0.008, 0.008, h / 2,
                   'leaf' if rnd.random() < 0.5 else 'grass')
    for _ in range(70):                                         # ferns in the verges and under the first trees
        y = rnd.uniform(-REACH, REACH)
        side = rnd.choice((-1, 1))
        x = road_x(y) + side * rnd.uniform(ROAD_HALF + 0.3, ROAD_HALF + VERGE + 1.0)
        fern(b, x, y, rnd.uniform(0.18, 0.3), rnd.uniform(0, 6.283))
    for _ in range(46):                                         # wild bushes in the verges: the monsters' cover
        y = rnd.uniform(-REACH, REACH)
        if abs(y - BLOCK_Y) < 1.6:
            continue
        x = road_x(y) + rnd.choice((-1, 1)) * rnd.uniform(ROAD_HALF + 0.55, ROAD_HALF + VERGE + 0.4)
        bush(b, x, y, rnd.uniform(0.25, 0.42), rnd)


def fern(b, x, y, size, turn):
    """A fern: 6-8 fronds arching out from the middle, each a narrow tilted blade."""
    rnd = random.Random(int(x * 1000 + y * 37))
    n = rnd.randint(6, 8)
    for i in range(n):
        a = turn + i * 6.283 / n + rnd.uniform(-0.2, 0.2)
        ux, uy = math.cos(a), math.sin(a)
        rise = rnd.uniform(0.45, 0.8)
        L = size * rnd.uniform(0.8, 1.1)
        c = (x + ux * L * 0.45, y + uy * L * 0.45, L * rise * 0.45 + 0.01)
        ln = math.hypot(1, rise)
        b.obox(c, (-uy, ux, 0), (ux / ln, uy / ln, rise / ln), (0, 0, 1), 0.035 * size / 0.25, L * 0.5, 0.004,
               'leaf' if i % 2 else 'grass')


def log(b, x0, y0, r, length, ang, color, sides=12):
    """A round log lying on the ground from (x0, y0) along the heading `ang` (radians): kit's hcylinder along x,
    its vertices turned about its start."""
    m = len(b.bm.verts)
    b.hcylinder('x', (length / 2, 0.0, r), length / 2, r, color, sides=sides)
    c, s_ = math.cos(ang), math.sin(ang)
    for v in list(b.bm.verts)[m:]:
        x, y = v.co.x, v.co.y
        v.co.x, v.co.y = x0 + x * c - y * s_, y0 + x * s_ + y * c


def bush(b, x, y, size, rnd):
    """A wild leafy bush: three or four overlapping leaf clumps."""
    for i in range(rnd.randint(3, 4)):
        a = rnd.uniform(0, 6.283)
        d = size * rnd.uniform(0.1, 0.45)
        b.ball(x + math.cos(a) * d, y + math.sin(a) * d, 0.0, size * rnd.uniform(0.5, 0.75),
               size * rnd.uniform(0.6, 0.9), 'leaf' if i % 2 else 'grass', sides=7)


def felled_tree(b):
    """The trunk across the road at BLOCK_Y: cut on the left verge (a fresh pale stump, chips round it), lying
    slightly askew, the crown's broken branches and leaves in the right verge."""
    sx, sy = -(ROAD_HALF + 0.75), BLOCK_Y - 0.25
    r = 0.15
    # the stump: bark, the fresh pale cut on top, a notch, chips
    b.cylinder(sx, sy, 0.0, 0.28, r * 1.05, 'wood_dark', sides=10)
    b.cylinder(sx, sy, 0.28, 0.29, r * 0.95, 'wood_light', sides=10)
    for a in (0.3, 1.4, 2.6, 3.9, 5.1):
        b.obox((sx + math.cos(a) * r * 1.1, sy + math.sin(a) * r * 1.1, 0.03), (math.cos(a), math.sin(a), 0),
               (-math.sin(a), math.cos(a), 0), (0, 0, 1), 0.05, 0.035, 0.03, 'wood_dark')     # root flares
    rnd = random.Random(3)
    for _ in range(26):
        a, d = rnd.uniform(0, 6.283), rnd.uniform(0.2, 0.75)
        b.obox((sx + math.cos(a) * d, sy + math.sin(a) * d, 0.006), (math.cos(a), math.sin(a), 0),
               (-math.sin(a), math.cos(a), 0), (0, 0, 1), rnd.uniform(0.02, 0.045), 0.012, 0.005, 'wood_light')
    # the trunk: from just past the stump over the road, rising to rest on its crown
    ang = math.radians(8)                           # askew across the road
    ux, uy = math.cos(ang), math.sin(ang)
    x0, y0 = sx + 0.32 * ux, sy + 0.32 * uy
    length = 2 * ROAD_HALF + 1.9
    x1, y1 = x0 + ux * length, y0 + uy * length
    log(b, x0, y0, r, length, ang, 'wood_dark')                                   # a ROUND trunk
    log(b, x0 - ux * 0.006, y0 - uy * 0.006, r * 0.92, 0.012, ang, 'wood_light')      # the cut end, pale
    for i in range(9):                                                                # bark ridges
        t = rnd.uniform(0.05, 0.95)
        px, py = x0 + ux * length * t, y0 + uy * length * t
        b.rod((px - ux * 0.12, py - uy * 0.12, r * 1.97), (px + ux * 0.12, py + uy * 0.12, r * 1.97), 0.02,
              'wood_mid')
    # the crown, crushed into the right verge: branches out sideways and up, leaf clumps on them
    for i in range(11):
        t = 0.72 + 0.28 * i / 10
        px, py = x0 + ux * length * t, y0 + uy * length * t
        side = 1 if i % 2 else -1
        bx = px + (-uy) * side * rnd.uniform(0.4, 0.9) + ux * rnd.uniform(0.1, 0.5)
        by = py + ux * side * rnd.uniform(0.4, 0.9) + uy * rnd.uniform(0.1, 0.5)
        bz = rnd.uniform(0.15, 0.75)
        b.rod((px, py, r * 1.6), (bx, by, bz), 0.05, 'wood_mid')
        b.ball(bx, by, bz - 0.12, rnd.uniform(0.22, 0.32), rnd.uniform(0.3, 0.42),
               'leaf' if i % 3 else 'grass', sides=7)
    b.ball(x1 + ux * 0.25, y1 + uy * 0.25, 0.05, 0.42, 0.55, 'leaf', sides=8)
    # a branch snapped off on the road
    b.rod((0.35, BLOCK_Y - 0.55, 0.03), (0.75, BLOCK_Y - 0.9, 0.05), 0.035, 'wood_mid')
    b.ball(0.8, BLOCK_Y - 0.95, 0.0, 0.14, 0.16, 'leaf', sides=6)


def road():
    b = Builder()
    ground(b)
    felled_tree(b)
    return b


def _copy(name, x, y, turn_deg, scale, tag="Road"):
    import bpy
    src = bpy.data.objects[name]
    o = src.copy()
    o.name = f"{tag} {name} {x:.2f},{y:.2f}"
    o.location = (x, y, 0.0)
    o.rotation_euler = (0, 0, math.radians(turn_deg))
    o.scale = (scale,) * 3
    o.hide_render = False
    o.hide_viewport = False
    fg.ensure_collection().objects.link(o)
    return o


def _woods():
    """The forest both sides: trees packed TREE_GAP apart from the verge's edge out to ~9 units, thickest by the road
    (the monsters come out of it); ~1 in 14 dead; stones in the verges (the wild bushes are in the road's mesh)."""
    rnd = random.Random(21)
    out, placed = [], []
    kinds = ("Tree3", "Tree10", "Tree2", "Tree1", "Tree5", "Tree7", "Tree4", "Tree8")
    dead = ("OrcDeadTree1-A", "OrcDeadTree1-B", "OrcDeadTree1-C")
    edge = ROAD_HALF + VERGE
    gap = 0.8
    tries = 0
    while len(placed) < 560 and tries < 50000:
        tries += 1
        y = rnd.uniform(-REACH - 2, REACH + 2)
        side = rnd.choice((-1, 1))
        off = edge + 12.0 * rnd.random() ** 1.6                   # thickest at the edge
        x = road_x(y) + side * off
        if road_dist(x, y) < edge:
            continue
        if any((x - px) ** 2 + (y - py) ** 2 < gap * gap for px, py in placed):
            continue
        placed.append((x, y))
        if rnd.random() < 0.07:
            out.append((rnd.choice(dead), x, y, rnd.uniform(0, 360), rnd.uniform(0.5, 0.7)))
        else:
            out.append((kinds[len(placed) % len(kinds)], x, y, rnd.uniform(0, 360), rnd.uniform(0.9, 1.35)))
    for i in range(22):                                           # stones
        y = rnd.uniform(-REACH, REACH)
        x = road_x(y) + rnd.choice((-1, 1)) * rnd.uniform(ROAD_HALF + 0.2, edge + 0.6)
        out.append(("Stone%d" % (1 + i % 10), x, y, rnd.uniform(0, 360), rnd.uniform(0.15, 0.26)))
    return out


def make():
    objs = [fg.install_static(OBJ, road(), scale=1.0)]
    for name, x, y, turn, k in _woods():
        objs.append(_copy(name, x, y, turn, S * k if not name.startswith("Stone") else k))
    return objs


def render(out):
    """The set with the column's cart, horses where the riders will sit (scale) and a few monsters at the trees."""
    import bpy
    from mathutils import Vector
    import cage_cart
    import cavalry
    import horse
    import orcs_cast
    os.makedirs(out, exist_ok=True)
    objs = make()
    folk = []
    # the column (stopped short of the block): 4 vanguard in two rows, the King's white horse + the standard-bearer,
    # the cart, 15 riders behind in pairs (the user: 20 riders)
    rows = [(BLOCK_Y - 1.2, 2), (BLOCK_Y - 1.95, 2), (BLOCK_Y - 2.9, 2)]
    y = BLOCK_Y - 2.9 - 2.3
    cart_y = y
    y -= 1.3
    for i in range(8):
        rows.append((y, 2 if i < 7 else 1))
        y -= 0.75
    n = 0
    for ry, k in rows:
        for j in range(k):
            x = (-0.32 + 0.64 * j) if k == 2 else 0.0
            name = "CineKingHorse" if (ry == rows[2][0] and j == 0) else \
                ("CineWarhorseGrey" if n % 3 == 1 else "CineWarhorse")
            folk.append(cavalry.make_horse(name, (x, ry, 0), obj="RoadHorse%d" % n))
            folk[-1].rotation_euler = (0, 0, math.radians(180))
            n += 1
    folk.append(cage_cart.make("CineCageCart", (0, cart_y, 0)))
    folk[-1].rotation_euler = (0, 0, math.radians(180))
    folk.append(orcs_cast.make("CineOrcCaptive", (0.0, cart_y - 0.1, cage_cart.SCALE * cage_cart.BED_Z)))
    folk[-1].rotation_euler = (0, 0, math.radians(180))
    keep = horse.BLUE, horse.GOLD
    horse.BLUE = horse.GOLD = horse.TACK
    try:
        for i, x in enumerate((-0.16, 0.16)):
            folk.append(cavalry.make_horse("CineWarhorse", (x, cart_y + cage_cart.SCALE * (cage_cart.HALF_L + 0.66), 0),
                                           obj="RoadCartHorse%d" % i))
            folk[-1].rotation_euler = (0, 0, math.radians(180))
    finally:
        horse.BLUE, horse.GOLD = keep
    for name, x, y_, turn in (("CineOrcSpear", -2.25, 0.6, -80), ("CineOrcTorch", 2.3, -1.4, 95),
                              ("CineOrcSpear", 2.2, 2.4, 100), ("CineOrcTorch", -2.4, -3.2, -100)):
        o = orcs_cast.make(name, (x, y_, 0))
        o.rotation_euler = (0, 0, math.radians(turn))
        folk.append(o)
    sc = bpy.context.scene
    keep_names = {o.name for o in objs}
    for o in folk:
        keep_names |= {o.name} | {c.name for c in o.children_recursive}
    for o in sc.objects:
        if o.type in ('MESH', 'CURVE', 'EMPTY', 'ARMATURE'):
            o.hide_render = o.name not in keep_names
    sc.render.engine = 'BLENDER_EEVEE'
    sc.render.resolution_x, sc.render.resolution_y = 1600, 900
    sc.view_settings.view_transform = 'Standard'
    for name, energy, rx, rz in (("SetSun", 3.0, 45, -30), ("SetFill", 0.9, 60, 150)):
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
    for name, at, frm, lens in (("road_wide", (0, -1.0, 0), (9.0, -16.0, 9.0), 26),
                                ("road_column", (-0.2, -0.5, 0.35), (0.8, -4.6, 0.65), 26),
                                ("road_block", (0, BLOCK_Y, 0.2), (0.6, BLOCK_Y - 4.5, 1.2), 30),
                                ("road_stump", (-(ROAD_HALF + 0.75), BLOCK_Y - 0.25, 0.2), (-0.6, BLOCK_Y - 2.0, 0.9), 35),
                                ("road_top", (0, 0, 0), (0.01, -0.5, 30.0), 28)):
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
