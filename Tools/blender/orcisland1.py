"""The orcs' gloomy island, its own pieces (the user's concept, 2026-11-06: a giant beast's
skeleton as the main base's heart, a dead swamp round it -- Design/concepts/orcisland_*.png).
FOR APPROVAL: built into the Models file, not exported, not in the game.

    OrcSkullGate1     10x12  the beast's skull lying in front of the main gate, its snout to the
                             front: the way in runs between the lower jaws and under the raised
                             upper one, on under the cranium's arch to the gate (a 2-cell corridor,
                             3.4 high). Its sockets glow.
    OrcRib1-A, -B     2x2    one rib: a foot in the ground outside the base's side wall, arching
                             up 10 and over 11 cells inward (+x); B is snapped, its tip at its foot
    OrcVertebra1      2x2    a vertebra half sunk in the ground: the spine and the tail, scaled
    DeadTree1-A/B/C   1x1    the swamp's dead trees, bare and crooked, moss hanging off them
    OrcSkullStake1    1x1    a stake with a skull and a rag, along the ways in
    Wreck1            2x6    a wrecked ship's ribs and keel on the shore, its mast snapped
    Crow1                    a crow on the wing (circling the skeleton)

    blender --background "<Models>.blend" --python orcisland1.py
"""
import math
import os
import random
import sys

import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402

BONE, BONE_OLD, BONE_DIRT = 'burlap', 'stone_mid', 'stone_dark'   # dirty bone, not the camps' clean plaster
HOLLOW = o.HAIR                         # near-black: sockets, nostrils
EYE = 'fire'
MOSS, MOSS_DARK = 'leaf', 'leaf'     # the palette's darkest green; tufts kept few
WOOD, WOOD_LIGHT = o.ROCK, o.ROCK   # grey dead wood: slate draws plain (wood gets planks)
ROW_Y = -150.0                          # the island's row in the Models file


def lump(b, centre, size, color, rot=(0.0, 0.0, 0.0), seed=0, sides=7, rings=4, jitter=0.1, under=None):
    """A faceted ellipsoid (a bone's knuckle, a cranium): `rings` bands of `sides`, jittered by
    `seed`. `under` colours the lower half (dirt where it lies in the swamp)."""
    r = random.Random(seed)
    turn = Matrix.Rotation(rot[2], 3, 'Z') @ Matrix.Rotation(rot[1], 3, 'Y') @ Matrix.Rotation(rot[0], 3, 'X')
    c = Vector(centre)
    top = b.bm.verts.new(c + turn @ Vector((0, 0, size[2])))
    bot = b.bm.verts.new(c + turn @ Vector((0, 0, -size[2])))
    bands = []
    for k in range(1, rings):
        phi = math.pi * k / rings
        band = []
        for i in range(sides):
            th = 2 * math.pi * (i + 0.5 * (k % 2)) / sides
            j = 1 + r.uniform(-jitter, jitter)
            p = Vector((math.sin(phi) * math.cos(th) * size[0], math.sin(phi) * math.sin(th) * size[1], math.cos(phi) * size[2])) * j
            band.append(b.bm.verts.new(c + turn @ p))
        bands.append(band)
    low = under or color
    for i in range(sides):
        j = (i + 1) % sides
        b._face([bands[0][i], bands[0][j], top], color)
        b._face([bands[-1][j], bands[-1][i], bot], low)
    for k in range(len(bands) - 1):
        col = color if k < (len(bands) - 1) / 2 else low
        for i in range(sides):
            j = (i + 1) % sides
            if k % 2 == 0:
                b._face([bands[k][i], bands[k + 1][i], bands[k][j]], col)
                b._face([bands[k][j], bands[k + 1][i], bands[k + 1][j]], col)
            else:
                b._face([bands[k][i], bands[k + 1][i], bands[k + 1][j]], col)
                b._face([bands[k][i], bands[k + 1][j], bands[k][j]], col)


def moss_tufts(b, pts, seed=0):
    r = random.Random(seed)
    for x, y in pts:
        b.blob(x, y, -0.02, r.uniform(0.2, 0.35), r.uniform(0.18, 0.3), r.choice((MOSS, MOSS_DARK)), sides=5)


# -- the skull gate --------------------------------------------------------------------------------

def build_skull(lo, hi):
    b = Builder()
    for s in (-1, 1):
        lump(b, (s * 3.0, 3.8, 2.4), (2.0, 2.6, 2.6), BONE, seed=11 + s, under=BONE_OLD)        # the cranium's sides
        lump(b, (s * 2.4, 1.7, 6.2), (1.7, 1.0, 0.55), BONE_OLD, rot=(0, s * 0.35, s * 0.2), seed=17 + s, sides=6, rings=3)  # brows
        lump(b, (s * 3.4, 0.4, 3.6), (0.9, 2.0, 0.8), BONE, seed=19 + s, sides=6, rings=3)      # cheekbones
        lump(b, (s * 2.6, -1.0, 0.45), (0.85, 4.6, 0.6), BONE, rot=(0, 0, s * -0.12), seed=13 + s, sides=6,
             under=BONE_DIRT)                                                                   # the lower jaw on the ground
        lump(b, (s * 2.3, 1.3, 5.1), (1.25, 0.55, 0.75), HOLLOW, rot=(0, s * -0.4, 0), seed=20 + s, sides=6, rings=3)  # sockets
        lump(b, (s * 2.3, 0.9, 5.0), (0.3, 0.18, 0.22), EYE, seed=30 + s, sides=5, rings=2)     # the glow in them
        lump(b, (s * 0.7, -5.5, 5.2), (0.35, 0.3, 0.3), HOLLOW, seed=22 + s, sides=5, rings=2)  # nostrils
        for k in range(5):              # upper teeth along the jaw's sides, the middle left open
            t = Vector((s * (2.2 - 0.12 * k), 1.0 - k * 1.4, 3.4 + k * 0.28))
            o.tube(b, [t, t + Vector((0, 0, -0.7)), t + Vector((0, -0.1, -1.5))], [0.26, 0.18, 0.0], BONE_OLD)
        for k in range(5):              # lower teeth up off the jaw
            t = Vector((s * (2.4 - 0.1 * k), 2.0 - k * 1.5, 0.8))
            o.tube(b, [t, t + Vector((0, 0, 0.6)), t + Vector((0, 0.05, 1.2))], [0.22, 0.15, 0.0], BONE_OLD)
        t = Vector((s * 2.2, -5.0, 0.6))  # tusks off the jaw's front
        o.tube(b, [t, t + Vector((s * 0.6, -1.0, 1.4)), t + Vector((s * 0.5, -1.4, 3.0)), t + Vector((s * -0.1, -1.1, 4.0))],
               [0.5, 0.4, 0.25, 0.0], BONE, sides=6)
        t = Vector((s * 4.0, 4.5, 6.2))   # horns sweeping out and back
        o.tube(b, [t, t + Vector((s * 2.2, 0.4, 1.4)), t + Vector((s * 4.0, 2.0, 1.8)),
                   t + Vector((s * 5.0, 3.8, 0.9)), t + Vector((s * 5.1, 5.0, -0.5))],
               [0.9, 0.7, 0.5, 0.3, 0.0], BONE_OLD, sides=7)
        o.war_banner(b, s * 4.6, -0.4, 2.6, w=0.5, drop=1.2)              # rust banners off the cheeks
        o.skull_chain(b, s * 1.6, -4.6, 3.2, links=3, s=0.16)              # small skulls hung from the jaw
    lump(b, (0, 4.0, 5.4), (4.6, 3.2, 1.9), BONE, seed=15, under=BONE_OLD)                     # the dome over the way
    lump(b, (0, -1.8, 4.2), (2.1, 4.4, 0.9), BONE, rot=(0.2, 0, 0), seed=16, under=BONE_OLD)   # the upper jaw, raised
    # the swamp has taken its foot: moss round it, a crack of dirt
    moss_tufts(b, [(-4.6, 4.2), (4.8, 3.0), (-3.9, -3.8), (3.6, -4.9), (-5.0, 1.0), (5.1, 5.6), (-1.6, 5.9), (2.0, 6.2)], seed=3)
    for x in (-1.0, 1.0):                                                                       # torches at the way in
        o.torch(b, x * 1.4, -6.2, h=0.9)
    return b


# -- a rib, a vertebra ----------------------------------------------------------------------------

def rib(snapped):
    def build(lo, hi):
        b = Builder()
        h, reach = 10.5, 11.5
        ts = (0.0, 0.15, 0.3, 0.45, 0.6, 0.75, 0.88, 1.0)
        if snapped:
            ts = ts[:5]
        pts, radii = [], []
        for t in ts:
            a = t * math.pi * 0.78
            pts.append(Vector((reach * (1 - math.cos(a)) / (1 - math.cos(math.pi * 0.78)),
                               0.5 * math.sin(t * 3.1), -0.4 + h * math.sin(a))))
            radii.append(0.78 - 0.6 * t)
        if not snapped:
            radii[-1] = 0.0
        o.tube(b, pts, radii, BONE, sides=7, tip=not snapped)
        # bands of old bone and the swamp's dirt up its foot
        lump(b, (0.2, 0.0, 0.3), (1.0, 0.95, 0.6), BONE_DIRT, seed=41, sides=7, rings=3)
        moss_tufts(b, [(-0.7, -0.5), (0.6, 0.7), (-0.4, 0.8), (0.8, -0.6)], seed=42)
        if snapped:                     # the broken tip lies at its foot
            o.tube(b, [Vector((-0.6, -1.2, 0.25)), Vector((-1.6, -2.4, 0.35)), Vector((-2.4, -3.3, 0.2))],
                   [0.32, 0.24, 0.0], BONE_OLD, sides=6)
        else:                           # a chain of skulls hangs off its top
            top = pts[5]
            o.skull_chain(b, top.x, top.y, top.z - 0.5, links=4, s=0.2)
        return b
    return build


def build_vertebra(lo, hi):
    b = Builder()
    lump(b, (0, 0, 0.15), (0.95, 0.75, 0.6), BONE, seed=51, under=BONE_DIRT)                   # the body, half sunk
    o.tube(b, [Vector((0, 0.1, 0.5)), Vector((0, 0.4, 1.3)), Vector((0, 0.9, 2.0))], [0.32, 0.2, 0.0], BONE_OLD, sides=6)  # the spike
    for s in (-1, 1):                                                                           # the side processes
        o.tube(b, [Vector((s * 0.6, 0, 0.35)), Vector((s * 1.2, 0.1, 0.45)), Vector((s * 1.6, 0.25, 0.3))],
               [0.2, 0.14, 0.0], BONE, sides=5)
    moss_tufts(b, [(-0.8, -0.6), (0.7, 0.6)], seed=52)
    return b


# -- the swamp's dress ----------------------------------------------------------------------------

def dead_tree(seed, h):
    def build(lo, hi):
        r = random.Random(seed)
        b = Builder()
        lean = Vector((r.uniform(-0.25, 0.25), r.uniform(-0.25, 0.25), 0))
        trunk = [Vector((0, 0, -0.05)), Vector((0, 0, h * 0.35)) + lean * 0.3, Vector((0, 0, h * 0.7)) + lean * 0.7, Vector((0, 0, h)) + lean]
        o.tube(b, trunk, [0.2, 0.14, 0.09, 0.0], WOOD, sides=6)
        for k in range(3):              # roots splayed into the mud
            a = 2 * math.pi * k / 3 + r.uniform(-0.3, 0.3)
            d = Vector((math.cos(a), math.sin(a), 0))
            o.tube(b, [Vector((0, 0, 0.25)), d * 0.35 + Vector((0, 0, 0.05)), d * 0.6 + Vector((0, 0, -0.05))], [0.09, 0.06, 0.0], WOOD, sides=4)
        for k in range(r.randint(3, 4)):  # crooked bare branches
            t = r.uniform(0.4, 0.85)
            start = trunk[1].lerp(trunk[3], (t - 0.35) / 0.65)
            a = r.uniform(0, 2 * math.pi)
            d = Vector((math.cos(a), math.sin(a), r.uniform(0.3, 0.9))).normalized()
            L = r.uniform(0.6, 1.2) * h / 3.5
            mid = start + d * L * 0.5 + Vector((0, 0, r.uniform(-0.1, 0.15)))
            end = start + d * L + Vector((0, 0, r.uniform(-0.3, 0.25)))
            o.tube(b, [start, mid, end], [0.06, 0.04, 0.0], WOOD_LIGHT, sides=4)
            if r.random() < 0.7:        # moss hanging off it
                o.tube(b, [mid, mid + Vector((0, 0, -0.35)), mid + Vector((0.03, 0, -0.8))], [0.05, 0.035, 0.0], MOSS_DARK, sides=3)
        if seed % 2 == 0:               # a shelf fungus on the trunk
            p = trunk[1].lerp(trunk[2], 0.4)
            b.zbox(p.x + 0.17, p.y, p.z, p.z + 0.06, 0.12, 0.16, 0, BONE_OLD)
        return b
    return build


def build_stake(lo, hi):
    b = Builder()
    o.stake(b, 0, 0, -0.05, 1.5, r=0.06, color=o.TIMBER)
    o.skull(b, 0, 0, 1.38, s=0.26)
    o.tube(b, [Vector((0.06, 0.0, 1.15)), Vector((0.22, -0.05, 0.85)), Vector((0.18, -0.1, 0.55))], [0.06, 0.05, 0.0], o.HIDE, sides=3)  # a rag
    return b


def build_wreck(lo, hi):
    b = Builder()
    o.tube(b, [Vector((0, -3.0, 0.1)), Vector((0, 0, -0.05)), Vector((0, 2.8, 0.5))], [0.16, 0.18, 0.0], WOOD, sides=5)   # the keel
    for k in range(8):                  # ribs, the stern ones broken off
        y = -2.6 + k * 0.7
        top = 1.2 if k < 5 else 0.6
        for s in (-1, 1):
            o.tube(b, [Vector((0, y, 0.05)), Vector((s * 0.85, y, 0.35)), Vector((s * 1.05, y + 0.05, top))],
                   [0.08, 0.07, 0.0 if k >= 5 else 0.05], WOOD_LIGHT, sides=4, tip=k >= 5)
    for s in (-1, 1):                   # what is left of the planking along one side
        b.obox((s * 0.95, -1.4, 0.55), (0, 1, 0), (s * 0.3, 0, 1), (0, 0, 1), 1.3, 0.04, 0.22, WOOD, skip=())
    o.tube(b, [Vector((0, 0.4, 0.1)), Vector((0.3, 0.7, 1.6)), Vector((0.55, 0.9, 2.6))], [0.1, 0.08, 0.06], WOOD, sides=5, tip=False)  # the snapped mast
    o.tube(b, [Vector((0.55, 0.9, 2.2)), Vector((0.9, 1.2, 1.2)), Vector((1.0, 1.4, 0.4))], [0.06, 0.05, 0.0], o.HIDE, sides=3)  # a torn sail
    moss_tufts(b, [(-0.6, 2.4), (0.7, -2.6)], seed=71)
    return b


def build_crow(lo, hi):
    b = Builder()
    lump(b, (0, 0, 0), (0.07, 0.17, 0.07), HOLLOW, seed=81, sides=5, rings=3)
    lump(b, (0, 0.17, 0.03), (0.05, 0.06, 0.05), HOLLOW, seed=82, sides=5, rings=2)
    o.tube(b, [Vector((0, 0.21, 0.03)), Vector((0, 0.29, 0.02))], [0.02, 0.0], 'iron', sides=3)
    for s in (-1, 1):
        b._face([b.bm.verts.new((s * 0.04, 0.08, 0.02)), b.bm.verts.new((s * 0.36, -0.02, 0.1)),
                 b.bm.verts.new((s * 0.3, -0.12, 0.08)), b.bm.verts.new((s * 0.04, -0.08, 0.02))][::s], HOLLOW)
    b._face([b.bm.verts.new((0, -0.15, 0.0)), b.bm.verts.new((-0.08, -0.32, 0.0)), b.bm.verts.new((0.08, -0.32, 0.0))], HOLLOW)
    return b


def main():
    o.ensure_collection()
    src = "Laboratory1-lvl1"
    o.place("OrcSkullGate1", build_skull, src, 30.0, ROW_Y)
    o.place("OrcRib1-A", rib(False), src, 44.0, ROW_Y)
    o.place("OrcRib1-B", rib(True), src, 44.0, ROW_Y - 8.0)
    o.place("OrcVertebra1", build_vertebra, src, 30.0, ROW_Y - 12.0)
    for k, (name, h) in enumerate((("DeadTree1-A", 3.6), ("DeadTree1-B", 2.8), ("DeadTree1-C", 4.2))):
        o.place(name, dead_tree(4 + k, h), src, 34.0 + k * 2.5, ROW_Y - 12.0)
    o.place("OrcSkullStake1", build_stake, src, 42.0, ROW_Y - 12.0)
    o.place("Wreck1", build_wreck, src, 30.0, ROW_Y - 20.0)
    o.place("Crow1", build_crow, src, 36.0, ROW_Y - 20.0)
    bpy.ops.wm.save_mainfile()


main()
