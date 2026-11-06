"""CONCEPT of the orcs' gloomy island (the user, 2026-11-06: "C as its heart + B round it"):
a swamp island (islands.py with ISL_SWAMP=1) dressed for review -- a gloomy palette on the same
terrain cells, the main base laid out from its real plan and models, a giant beast's skeleton as
its heart (the skull over the front gate, ribs over the sides, the spine running off behind),
dead trees, stakes with skulls, wrecks on the beaches and a storm cloud. Renders, never saves.

    ISL_SWAMP=1 blender --background --factory-startup --python islands.py -- <dir>/Isl-orc1.blend orc 1
    blender --background <dir>/Isl-orc1.blend --python orcisland_concept.py -- <models.blend> <out_dir> <main_x> <main_y>
"""
import math
import os
import random
import sys

import bmesh
import bpy
from mathutils import Vector, Matrix

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import terrain  # noqa: E402

argv = sys.argv[sys.argv.index("--") + 1:]
MODELS, OUT_DIR = argv[0], argv[1]
MX, MY = float(argv[2]), float(argv[3])
rnd = random.Random(7)
sc = bpy.context.scene

# -- the gloomy palette: the same cells, swamp and ash colours ------------------------------------
GLOOM = {
    "grass_light": "6E7A4A", "grass": "5F6E40", "grass_mid": "566539", "grass_lush": "4C5C33",
    "grass_dark": "3F4E2C", "forest_floor": "343F26", "moss": "4F5A30", "grass_dry": "7A7448",
    "sand": "7D705A", "sand_wet": "5E5444", "dirt": "5C4A36", "dirt_dark": "45372A",
    "path": "6E5E46", "clay": "5E4636", "mud": "3E3226", "pebble": "77756C",
    "rock_light": "7E7C78", "rock": "6A6864", "rock_mid": "5A5855", "rock_dark": "4A4846",
    "rock_deep": "3B3A39", "water_shallow": "4E6A4C", "water": "3C5642", "water_deep": "2C4036",
    "foam": "7E8F74", "leaf": "3A4A2C", "leaf_light": "51603A", "flower_white": "A8A898",
    "flower_yellow": "9A8F4A", "flower_purple": "6E5A7A", "flower_red": "7A3A32", "flower_pink": "7A5A62",
    "bark": "4A3A2C", "bark_dark": "33281F", "reed": "6E6A3E", "cattail": "4A3424",
    "lily": "3E5A32", "mushroom_cap": "8A6A3A", "mushroom_stem": "B0A890", "birch_bark": "8A877C",
    "grass_b": "64733F", "grass_light_b": "737F4D", "grass_mid_b": "5A693C", "grass_lush_b": "505F35",
    "grass_dark_b": "43522E", "forest_floor_b": "38432A", "grass_dry_b": "7F784B", "stony": "6B675A",
}


def rgb(h):
    return tuple(int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4))


def gloom_palette():
    img = bpy.data.images.get(terrain.PALETTE_IMAGE)
    if img is None:
        for m in bpy.data.materials:
            if m.use_nodes:
                for n in m.node_tree.nodes:
                    if n.type == 'TEX_IMAGE' and n.image is not None:
                        img = n.image
    w = terrain.COLS * terrain.CELL
    px = list(img.pixels)
    for i, (name, _) in enumerate(terrain.TERRAIN_PALETTE):
        c, r = i % terrain.COLS, i // terrain.COLS
        col = rgb(GLOOM.get(name, "555555"))
        for yy in range(terrain.CELL):
            for xx in range(terrain.CELL):
                x, y = c * terrain.CELL + xx, (terrain.ROWS - 1 - r) * terrain.CELL + yy
                k = (y * w + x) * 4
                px[k:k + 4] = [col[0], col[1], col[2], 1.0]
    img.pixels = px
    img.update()


def flat_mat(name, hexcol, emit=0.0, rough=0.9):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes.get("Principled BSDF")
    b.inputs["Base Color"].default_value = (*rgb(hexcol), 1)
    b.inputs["Roughness"].default_value = rough
    if emit:
        b.inputs["Emission Color"].default_value = (*rgb(hexcol), 1)
        b.inputs["Emission Strength"].default_value = emit
    return m


BONE = flat_mat("Bone", "A3967A")
BONE_DARK = flat_mat("BoneDark", "7A6D55")
HOLLOW = flat_mat("Hollow", "16110E")
EYE = flat_mat("EyeGlow", "C0331E", emit=6.0)
DEADWOOD = flat_mat("Deadwood", "3E342B")
MOSS_HANG = flat_mat("HangingMoss", "56603A")
STAKE = flat_mat("Stake", "4E3A28")
HULL = flat_mat("Hull", "4A3B2C")
CLOUD = flat_mat("StormCloud", "34313A")
FIRE = flat_mat("Fire", "FF7A1E", emit=3.0)


def obj_from_bm(name, bm, mat, coll=None):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    for p in me.polygons:
        p.use_smooth = False
    me.materials.append(mat)
    o = bpy.data.objects.new(name, me)
    (coll or sc.collection).objects.link(o)
    return o


def ground_z(x, y):
    """The terrain's height under a point (the island's own meshes), or None over water."""
    hit, loc, _, _, ob, _ = sc.ray_cast(bpy.context.evaluated_depsgraph_get(), Vector((x, y, 60)), Vector((0, 0, -1)))
    if not hit or ob is None or not ("-Ground-" in ob.name or "-Relief-" in ob.name):
        return None
    return loc.z


# -- a tapered, bent bone (or branch): rings along a path ------------------------------------------

def tube(bm, pts, radii, sides=6, mat_index=0):
    rings = []
    for i, p in enumerate(pts):
        a = pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]
        a.normalize()
        up = Vector((0, 0, 1)) if abs(a.z) < 0.9 else Vector((1, 0, 0))
        u = a.cross(up).normalized()
        v = a.cross(u).normalized()
        ring = []
        for k in range(sides):
            t = 2 * math.pi * k / sides
            ring.append(bm.verts.new(p + (u * math.cos(t) + v * math.sin(t)) * radii[i]))
        rings.append(ring)
    for r0, r1 in zip(rings, rings[1:]):
        for k in range(sides):
            bm.faces.new((r0[k], r0[(k + 1) % sides], r1[(k + 1) % sides], r1[k]))
    tip = bm.verts.new(pts[-1] + (pts[-1] - pts[-2]).normalized() * radii[-1] * 1.5)
    for k in range(sides):
        bm.faces.new((rings[-1][k], rings[-1][(k + 1) % sides], tip))
    bm.faces.new(list(reversed(rings[0])))


def blob(bm, centre, size, seed, subdiv=2, jitter=0.12, rot=(0, 0, 0)):
    r = random.Random(seed)
    res = bmesh.ops.create_icosphere(bm, subdivisions=subdiv, radius=1.0)
    turn = Matrix.Rotation(rot[2], 3, 'Z') @ Matrix.Rotation(rot[1], 3, 'Y') @ Matrix.Rotation(rot[0], 3, 'X')
    for v in res["verts"]:
        v.co = turn @ (Vector((v.co.x * size[0], v.co.y * size[1], v.co.z * size[2])) * (1 + r.uniform(-jitter, jitter))) + Vector(centre)
    return res["verts"]


# -- the main base from its real plan (OrcBaseLayout.Main, cells, +y = its back) -------------------

def main_plan():
    pieces = []

    def wall(x, z, along):
        pieces.append(("OrcStoneWall1-Straight", x, z, 1, 1, along))

    def hline(x0, x1, z, gate):
        for x in range(x0, x1 + 1):
            if gate and x in (-1, 0):
                continue
            wall(x, z, 'x')
        if gate:
            pieces.append(("OrcStoneGate1", -1, z, 2, 1, 'x'))

    def vline(x, z0, z1):
        for z in range(z0, z1 + 1):
            wall(x, z, 'y')

    hline(-13, 12, -8, True)
    hline(-14, 13, -17, True)
    vline(-14, -16, -15)
    vline(13, -16, -15)
    hline(-19, -15, -15, False)
    hline(14, 18, -15, False)
    hline(-19, 18, -23, True)
    vline(-19, -22, -16)
    vline(18, -22, -16)
    P = lambda m, x, z, w, h: pieces.append((m, x, z, w, h, 'x'))  # noqa: E731
    P("OrcMainPortal1", -2, -2, 4, 4)
    P("OrcMainTotem1", -5, 0, 1, 1)
    P("OrcMainTotem1", 4, 0, 1, 1)
    P("OrcMainBrutePit1", -12, -2, 2, 2)
    P("OrcMainBrutePit1", 10, -2, 2, 2)
    P("OrcMainStorehouse1", -13, -5, 2, 2)
    P("OrcMainStorehouse1", 11, -5, 2, 2)
    P("OrcStoneTower1", -9, -7, 2, 2)
    P("OrcStoneTower1", 7, -7, 2, 2)
    P("OrcMainCage1", -7, -4, 1, 1)
    P("OrcMainCage1", 6, -4, 1, 1)
    P("OrcMainTreasury1", -11, 0, 2, 2)
    P("OrcChiefHouse1", -2, -13, 3, 3)
    P("OrcMainThrowerRange1", -6, -11, 2, 2)
    P("OrcMainThrowerRange1", 3, -11, 2, 2)
    P("OrcMainStorehouse1", -9, -11, 2, 2)
    P("OrcMainStorehouse1", 7, -11, 2, 2)
    P("OrcStoneTower1", -11, -16, 2, 2)
    P("OrcStoneTower1", 9, -16, 2, 2)
    P("OrcMainCage1", 2, -9, 1, 1)
    P("OrcMainBarracks1", -16, -17, 2, 2)
    P("OrcMainBarracks1", 14, -17, 2, 2)
    for x, z in ((-18, -17), (16, -17), (-18, -22), (-9, -22), (7, -22), (16, -22)):
        P("OrcStoneTower1", x, z, 2, 2)
    P("OrcMainStorehouse1", -13, -22, 2, 2)
    P("OrcMainStorehouse1", 10, -22, 2, 2)
    return pieces


def lay_base():
    names = sorted({p[0] for p in main_plan()} | {"OrcGrunt-lvl3", "OrcBrute-lvl2", "OrcChief-Boss", "OrcThrower"})
    with bpy.data.libraries.load(MODELS, link=False) as (src, dst):
        dst.objects = [n for n in names if n in src.objects]
    protos = {o.name: o for o in dst.objects if o is not None}
    coll = bpy.data.collections.new("Base")
    sc.collection.children.link(coll)
    for name, x, z, w, h, along in main_plan():
        src = protos.get(name)
        if src is None:
            print("MISSING", name)
            continue
        o = bpy.data.objects.new(f"b_{name}", src.data)
        coll.objects.link(o)
        o.location = (MX + x + w / 2, MY + z + h / 2, 0.0)
        # the wall piece's model runs along its own y
        o.rotation_euler = (0, 0, math.pi / 2 if along == 'x' and name.startswith('OrcStoneWall') else 0.0)
    # a garrison standing about, so the scale reads
    for name, n in (("OrcGrunt-lvl3", 22), ("OrcBrute-lvl2", 6), ("OrcThrower", 8), ("OrcChief-Boss", 1)):
        src = protos.get(name)
        if src is None:
            continue
        for _ in range(n):
            o = bpy.data.objects.new(f"g_{name}", src.data)
            coll.objects.link(o)
            gx, gz = (-0.5, -15.0) if n == 1 else (rnd.uniform(-17, 16), rnd.uniform(-22, -3))
            o.location = (MX + gx, MY + gz, 0.0)
            o.rotation_euler = (0, 0, rnd.uniform(-0.6, 0.6))
    # braziers along the walls: the only warm light on the island
    for x, z in ((-3, -24), (2, -24), (-3, -18), (2, -18), (-3, -9), (2, -9), (-6, 2), (5, 2), (-19, -24), (18, -24)):
        bm = bmesh.new()
        blob(bm, (MX + x, MY + z, 1.1), (0.22, 0.22, 0.35), x * 31 + z, subdiv=1)
        obj_from_bm("Brazier", bm, FIRE, coll)


# -- the beast's skeleton -------------------------------------------------------------------------

def skeleton():
    coll = bpy.data.collections.new("Skeleton")
    sc.collection.children.link(coll)
    gy = MY - 23.0                 # the front wall's line; the gate between x -1 and 1
    # a beast's skull lying in front of the gate, its snout to the south: the way in runs between
    # its lower jaws and under the raised upper one, then under the cranium's arch to the gate
    bm = bmesh.new()
    for side in (-1, 1):
        blob(bm, (MX + side * 3.0, gy - 3.2, 2.4), (2.0, 2.6, 2.6), 11 + side, subdiv=1, jitter=0.1)  # the cranium's sides
        blob(bm, (MX + side * 2.4, gy - 5.3, 6.2), (1.7, 1.0, 0.55), 17 + side, subdiv=1, jitter=0.1,
             rot=(0, side * 0.35, side * 0.2))                                                  # brow ridges, frowning
        blob(bm, (MX + side * 3.4, gy - 6.6, 3.6), (0.9, 2.0, 0.8), 19 + side, subdiv=1, jitter=0.1)  # cheekbones
        blob(bm, (MX + side * 2.6, gy - 8.0, 0.45), (0.85, 4.6, 0.6), 13 + side, jitter=0.07,
             rot=(0, 0, side * -0.12))                                                          # the lower jaw on the ground
    blob(bm, (MX, gy - 3.0, 5.4), (4.6, 3.2, 1.9), 15, subdiv=1, jitter=0.08)                             # the cranium's dome over the way
    blob(bm, (MX, gy - 8.8, 4.2), (2.1, 4.4, 0.9), 16, subdiv=1, jitter=0.08, rot=(0.2, 0, 0))            # the upper jaw, raised
    obj_from_bm("Skull", bm, BONE, coll)
    bm = bmesh.new()
    for side in (-1, 1):
        blob(bm, (MX + side * 2.3, gy - 5.7, 5.1), (1.3, 0.6, 0.8), 20 + side, subdiv=1,
             rot=(0, side * -0.4, 0))                                                           # eye sockets, slanted
        blob(bm, (MX + side * 0.7, gy - 12.5, 5.2), (0.35, 0.3, 0.3), 22 + side, subdiv=1)      # nostrils
    obj_from_bm("SkullHollows", bm, HOLLOW, coll)
    bm = bmesh.new()
    for side in (-1, 1):
        blob(bm, (MX + side * 2.3, gy - 6.1, 5.0), (0.28, 0.18, 0.2), 30 + side, subdiv=1)
    obj_from_bm("SkullEyes", bm, EYE, coll)
    bm = bmesh.new()
    for side in (-1, 1):
        for k in range(5):             # upper teeth along the jaw's sides, the middle left open
            top = Vector((MX + side * (2.2 - 0.12 * k), gy - 6.0 - k * 1.4, 3.4 + k * 0.28))
            tube(bm, [top, top + Vector((0, 0, -0.7)), top + Vector((0, -0.1, -1.5))], [0.26, 0.18, 0.06], sides=5)
        for k in range(5):             # lower teeth up off the jaw
            b = Vector((MX + side * (2.4 - 0.1 * k), gy - 5.0 - k * 1.5, 0.8))
            tube(bm, [b, b + Vector((0, 0, 0.6)), b + Vector((0, 0.05, 1.2))], [0.22, 0.15, 0.05], sides=5)
        b = Vector((MX + side * 2.2, gy - 12.0, 0.6))      # tusks off the jaw's front
        tube(bm, [b, b + Vector((side * 0.6, -1.0, 1.4)), b + Vector((side * 0.5, -1.4, 3.0)), b + Vector((side * -0.1, -1.1, 4.0))],
             [0.5, 0.4, 0.25, 0.08], sides=6)
        b = Vector((MX + side * 4.0, gy - 2.5, 6.2))       # horns sweeping out and back
        tube(bm, [b, b + Vector((side * 2.2, 0.4, 1.4)), b + Vector((side * 4.0, 2.0, 1.8)),
                  b + Vector((side * 5.0, 3.8, 0.9)), b + Vector((side * 5.1, 5.0, -0.5))],
             [0.9, 0.7, 0.5, 0.3, 0.1], sides=7)
    obj_from_bm("Teeth", bm, BONE_DARK, coll)
    # the ribs: arcs from the ground outside the side walls, up and in over the base, their tips
    # apart so the base is seen between them
    bm = bmesh.new()
    for z in (-12.0, -7.5, -3.0, 1.5, 6.0):
        height = 10.5 - abs(z + 3.0) * 0.3
        for side in (-1, 1):
            pts = []
            for t in (0.0, 0.15, 0.3, 0.45, 0.6, 0.75, 0.88, 1.0):
                a = t * math.pi * 0.78                   # a little past the top of the arc: the tip bends down
                pts.append(Vector((MX + side * (21.0 - 11.5 * (1 - math.cos(a)) / (1 - math.cos(math.pi * 0.78))),
                                   MY + z + 0.5 * math.sin(t * 3.1), -0.3 + height * math.sin(a))))
            tube(bm, pts, [0.75, 0.68, 0.6, 0.52, 0.44, 0.36, 0.28, 0.16], sides=6)
    # the spine lying along the ground from the back wall off into the swamp, ending in a tail
    for i in range(18):
        t = i / 17.0
        cx = MX + 5.0 * math.sin(t * 2.4)
        cy = MY + 11.0 + i * 2.6
        r = 1.5 - 1.0 * t
        blob(bm, (cx, cy, r * 0.2), (r, r * 0.7, r * 0.6), 100 + i, subdiv=1, jitter=0.1)
        tube(bm, [Vector((cx, cy, r * 0.5)), Vector((cx, cy + 0.5, r * 1.3)), Vector((cx, cy + 1.1, r * 1.9))],
             [r * 0.35, r * 0.22, r * 0.06], sides=5)
    obj_from_bm("RibsAndSpine", bm, BONE, coll)


# -- the swamp's dress ----------------------------------------------------------------------------

def dead_tree(seed):
    r = random.Random(seed)
    bm = bmesh.new()
    h = r.uniform(3.0, 5.5)
    lean = Vector((r.uniform(-0.4, 0.4), r.uniform(-0.4, 0.4), 0))
    pts = [Vector((0, 0, -0.2)), Vector((0, 0, h * 0.35)) + lean * 0.3, Vector((0, 0, h * 0.7)) + lean * 0.7, Vector((0, 0, h)) + lean]
    tube(bm, pts, [0.32, 0.22, 0.14, 0.05], sides=5)
    for _ in range(r.randint(2, 4)):
        t = r.uniform(0.45, 0.85)
        start = pts[1].lerp(pts[3], (t - 0.35) / 0.65)
        a = r.uniform(0, 2 * math.pi)
        d = Vector((math.cos(a), math.sin(a), r.uniform(0.2, 0.9)))
        L = r.uniform(0.8, 1.8)
        tube(bm, [start, start + d * L * 0.5, start + d * L + Vector((0, 0, r.uniform(-0.4, 0.3)))], [0.1, 0.07, 0.02], sides=4)
    me_tree = bm
    o = obj_from_bm(f"DeadTree{seed}", me_tree, DEADWOOD)
    # hanging moss in a few
    if r.random() < 0.6:
        bm = bmesh.new()
        for _ in range(r.randint(2, 4)):
            p = Vector((r.uniform(-0.6, 0.6), r.uniform(-0.6, 0.6), h * r.uniform(0.55, 0.85))) + lean * 0.6
            tube(bm, [p, p + Vector((0, 0, -0.6)), p + Vector((0.05, 0, -1.3))], [0.07, 0.05, 0.01], sides=3)
        m = obj_from_bm(f"Moss{seed}", bm, MOSS_HANG)
        m.parent = o
    return o


def stake(seed):
    r = random.Random(seed)
    bm = bmesh.new()
    h = r.uniform(1.4, 2.2)
    tube(bm, [Vector((0, 0, -0.2)), Vector((0, 0, h * 0.6)), Vector((r.uniform(-0.1, 0.1), 0, h))], [0.07, 0.06, 0.03], sides=4)
    o = obj_from_bm(f"Stake{seed}", bm, STAKE)
    bm = bmesh.new()
    blob(bm, (0, 0, h - 0.08), (0.2, 0.24, 0.2), seed, subdiv=1)
    s = obj_from_bm(f"StakeSkull{seed}", bm, BONE)
    s.parent = o
    return o


def instance(proto, x, y, z, rot, scale=1.0):
    o = bpy.data.objects.new(proto.name + "_i", proto.data)
    sc.collection.objects.link(o)
    o.location = (x, y, z)
    o.rotation_euler = (0, 0, rot)
    o.scale = (scale, scale, scale)
    for ch in proto.children:
        c = bpy.data.objects.new(ch.name + "_i", ch.data)
        sc.collection.objects.link(c)
        c.parent = o
    return o


def in_base(x, y, margin):
    return MX - 22 - margin < x < MX + 22 + margin and MY - 30 - margin < y < MY + 12 + margin


def dress():
    trees = [dead_tree(s) for s in range(8)]
    stakes = [stake(100 + s) for s in range(4)]
    for p in trees + stakes:
        p.location = (0, 0, -500)
    half = 85
    placed = 0
    for _ in range(9000):
        x, y = rnd.uniform(-half, half), rnd.uniform(-half, half)
        if in_base(x, y, 2):
            continue
        z = ground_z(x, y)
        if z is None or z < -0.3:
            continue
        if rnd.random() < 0.55:
            continue
        instance(rnd.choice(trees), x, y, z, rnd.uniform(0, 6.28), rnd.uniform(0.8, 1.3))
        placed += 1
        if placed > 420:
            break
    # stakes: a line along the way in from the south, then scattered at the front
    for k in range(26):
        t = k / 25.0
        for side in (-1, 1):
            x, y = MX + side * (5.5 + 1.5 * math.sin(t * 6)), MY - 33 - t * 22
            z = ground_z(x, y)
            if z is not None and z > -0.3:
                instance(rnd.choice(stakes), x, y, z, rnd.uniform(0, 6.28))
    # wrecks on the shore
    for wx, wy, rot in ((MX + 11.4, MY + 15.2, 0.6), (38.9, -46.9, 2.2)):
        bm = bmesh.new()
        for k in range(7):
            a = math.pi * (0.15 + 0.7 * k / 6)
            b = Vector((math.cos(a) * 1.4, -3 + k * 0.05, math.sin(a) * -1.2 + 0.8))
            tube(bm, [b, b + Vector((0, 2.5, 0.1)), b + Vector((0, 4.8, 0.6 + 0.2 * k))], [0.12, 0.11, 0.06], sides=4)
        tube(bm, [Vector((0, -3, -0.4)), Vector((0, 0, -0.5)), Vector((0, 3, 0.2))], [0.25, 0.25, 0.2], sides=5)
        tube(bm, [Vector((0, 0.5, -0.3)), Vector((0.4, 0.8, 3.0)), Vector((0.9, 1.0, 4.6))], [0.15, 0.12, 0.06], sides=5)
        o = obj_from_bm("Wreck", bm, HULL)
        o.location = (wx, wy, -0.35)
        o.rotation_euler = (0.25, 0.15, rot)
    # the storm cloud over the island
    bm = bmesh.new()
    for k in range(24):
        a = rnd.uniform(0, 2 * math.pi)
        d = 38 * math.sqrt(rnd.random())
        blob(bm, (MX * 0.4 + math.cos(a) * d, MY * 0.4 + 8 + math.sin(a) * d * 0.7, 50 + rnd.uniform(-2, 3)),
             (rnd.uniform(9, 14), rnd.uniform(8, 12), rnd.uniform(2.2, 3.6)), 300 + k, subdiv=2, jitter=0.18)
    c = obj_from_bm("StormCloud", bm, CLOUD)
    c.visible_shadow = False


# -- render ---------------------------------------------------------------------------------------

def render():
    for o in sc.objects:
        if o.name.endswith(("-TreesArea", "-RocksArea", "-NoBuildArea", "-PlaceForWaterObjects")) or "-DecorKit-" in o.name:
            o.hide_render = True
    sun_data = bpy.data.lights.new("Sun", 'SUN')
    sun_data.energy = 2.4
    sun_data.color = (0.86, 0.88, 1.0)
    sun_data.angle = math.radians(4)
    sun = bpy.data.objects.new("Sun", sun_data)
    sc.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(48), 0, math.radians(-32))
    world = sc.world or bpy.data.worlds.new("World")
    sc.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    bg.inputs[0].default_value = (0.36, 0.40, 0.46, 1)
    bg.inputs[1].default_value = 0.6
    sc.render.engine = 'BLENDER_EEVEE'
    sc.view_settings.view_transform = 'Standard'
    try:
        sc.eevee.taa_render_samples = 16
        sc.eevee.use_bloom = True
    except AttributeError:
        pass
    cam_data = bpy.data.cameras.new("Cam")
    cam = bpy.data.objects.new("Cam", cam_data)
    sc.collection.objects.link(cam)
    sc.camera = cam
    cam_data.clip_end = 2000
    os.makedirs(OUT_DIR, exist_ok=True)
    cloud = bpy.data.objects.get("StormCloud")

    def shot(name, w, h, loc, target, lens=None, ortho=None):
        sc.render.resolution_x, sc.render.resolution_y = w, h
        if ortho:
            cam_data.type = 'ORTHO'
            cam_data.ortho_scale = ortho
        else:
            cam_data.type = 'PERSP'
            cam_data.lens = lens
        cam.location = Vector(loc)
        cam.rotation_euler = (Vector(target) - cam.location).to_track_quat('-Z', 'Y').to_euler()
        sc.render.filepath = os.path.join(OUT_DIR, name)
        bpy.ops.render.render(write_still=True)

    cloud.hide_render = True
    shot("orcisland_top.png", 1000, 1000, (0, 0, 300), (0, 0, 0), ortho=150)
    cloud.hide_render = False
    shot("orcisland_far.png", 1600, 900, (0, -175, 120), (0, 0, 8), lens=30)
    cloud.hide_render = True
    shot("orcisland_game.png", 1600, 900, (MX, MY - 62, 50), (MX, MY - 10, 0), lens=30)
    shot("orcisland_gate.png", 1600, 900, (MX + 16, MY - 60, 22), (MX, MY - 18, 2), lens=32)
    print("RENDERED concept")


gloom_palette()
lay_base()
skeleton()
dress()
render()
