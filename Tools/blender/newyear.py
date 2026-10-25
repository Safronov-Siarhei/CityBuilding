"""The New Year map's own models (the user, 2026-10-25): the decorated tree by the castle, the gift
boxes, a snowman, and two orcs of that map -- the Thief, who runs off with the gifts, and the Ice
Shaman, the midnight boss who freezes towers.

    NYTree       a tall decorated fir on a 2x2 plot: tiers of needles, baubles, a gold garland, a star
    NYGift1..3   a gift box, red / blue / green with a gold ribbon and bow, 0.3 m
    NYSnowman    three snowballs, a coal face, a carrot nose, a red scarf and a bucket hat
    OrcThief     a lean grunt in a red cap with a white trim, a sack over the shoulder
    OrcIceShaman a hulking orc in a pale blue robe and hood, a staff of ice, icicles on his shoulders

Orcs face -y like the others and stand in the orc column; the rest stand beside the Flag.

    blender --background Buildings-Size1-Models.blend --python newyear.py
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402
from mathutils import Vector  # noqa: E402

NEEDLES, NEEDLES_DARK = 'leaf', 'grass_dark'
SNOW = 'plaster_white'
GOLD = 'gold'
RED = 'banner_red'
ICE, ICE_DARK = 'water', 'roof_slate'


# ------------------------------------------------------------------------------------ the tree

def tree(lo, hi):
    """Five tiers of needles narrowing up to a gold star; red, blue and gold baubles on the tiers'
    rims; a gold garland spiralling round; a trunk in a red-wrapped tub."""
    b = Builder()
    b.cylinder(0, 0, 0.0, 0.22, 0.3, RED, sides=8)                      # the tub
    b.cylinder(0, 0, 0.18, 0.24, 0.32, GOLD, sides=8)
    b.cylinder(0, 0, 0.22, 0.45, 0.1, 'wood_dark', sides=6)             # the trunk
    tiers = [(0.4, 0.95, 1.05), (0.8, 0.8, 1.35), (1.2, 0.64, 1.7), (1.6, 0.47, 2.05), (1.95, 0.3, 2.4)]
    colours = (RED, 'banner_blue', GOLD, 'white')
    for i, (z0, r, apex) in enumerate(tiers):
        sides = 9
        b.pyramid(0, 0, z0, r, r, apex, NEEDLES if i % 2 == 0 else NEEDLES_DARK, sides=sides,
                  rot_deg=i * 20)
        # baubles round the tier's rim
        for k in range(sides):
            a = math.radians(i * 20 + 360 * (k + 0.5) / sides)
            rr = r * 0.82
            b.ball(math.cos(a) * rr, math.sin(a) * rr, z0 + 0.02, 0.05, 0.09, colours[(k + i) % len(colours)],
                   sides=6)
    # the garland: a gold rod spiralling up the cone
    prev = None
    for s in range(0, 37):
        t = s / 36
        z = 0.5 + t * 1.75
        r = 0.9 * (1 - t) + 0.12
        a = t * math.pi * 6
        p = Vector((math.cos(a) * r, math.sin(a) * r, z))
        if prev is not None:
            b.rod(prev, p, 0.025, GOLD)
        prev = p
    # the star
    top = 2.42
    pts = []
    for k in range(10):
        a = math.pi / 2 + k * math.pi / 5
        rr = 0.17 if k % 2 == 0 else 0.07
        pts.append((math.cos(a) * rr, top + 0.12 + math.sin(a) * rr))
    b.prism(pts, (0, 0.02, 0), (1, 0, 0), (0, 0, 1), 0.04, GOLD, back_cap=True)
    # snow on the lowest tier's rim and round the tub
    b.cylinder(0, 0, 0.0, 0.03, 0.6, SNOW, sides=10)
    return b


# ------------------------------------------------------------------------------------ the gifts

def gift(colour):
    def build(lo, hi):
        b = Builder()
        s = 0.15
        b.box(-s, s, -s, s, 0.0, 0.24, colour, bevel=0.015)
        b.box(-s - 0.01, s + 0.01, -0.025, 0.025, 0.0, 0.25, GOLD)        # ribbon both ways
        b.box(-0.025, 0.025, -s - 0.01, s + 0.01, 0.0, 0.25, GOLD)
        for k in (-1, 1):                                                 # the bow
            b.obox((k * 0.05, 0, 0.28), (1, 0, 0.5 * k), (0, 1, 0), (-0.5 * k, 0, 1), 0.05, 0.02, 0.03, GOLD,
                   bevel=0.01)
        b.ball(0, 0, 0.24, 0.025, 0.05, GOLD, sides=6)
        return b
    return build


# ------------------------------------------------------------------------------------ the snowman

def snowman(lo, hi):
    b = Builder()
    b.ball(0, 0, 0.0, 0.2, 0.34, SNOW, sides=9)
    b.ball(0, 0, 0.28, 0.15, 0.26, SNOW, sides=9)
    b.ball(0, 0, 0.5, 0.11, 0.2, SNOW, sides=8)
    for k in (-1, 1):
        b.box(k * 0.035 - 0.012, k * 0.035 + 0.012, -0.11, -0.09, 0.62, 0.64, 'window')   # coal eyes
    for z in (0.33, 0.4, 0.47):
        b.box(-0.012, 0.012, -0.155, -0.135, z, z + 0.024, 'window')                     # coal buttons
    o.tube(b, [(0, -0.1, 0.59), (0, -0.2, 0.585)], [0.022, 0.0], 'roof_orange', sides=5)  # carrot
    b.cylinder(0, 0, 0.5, 0.545, 0.12, RED, sides=9)                                    # scarf
    b.box(0.04, 0.09, -0.12, -0.08, 0.36, 0.52, RED)
    b.cylinder(0, 0, 0.68, 0.79, 0.08, 'iron', sides=8)                                 # bucket
    b.rod((0.15, 0, 0.42), (0.3, 0.02, 0.56), 0.02, 'wood_dark')                         # twig arms
    b.rod((-0.15, 0, 0.42), (-0.29, 0.02, 0.53), 0.02, 'wood_dark')
    return b


# ------------------------------------------------------------------------------------ the orcs

def thief(lo, hi):
    """A lean grunt, a little shorter, in a red cap with a white trim and bobble; a bulging sack
    over the right shoulder, the left arm swinging forward -- he is always running."""
    b = Builder()
    o.legs_and_hips(b, wide=0.88)
    b.obox((0, -0.005, 0.3), (1, 0, 0), (0, 0.97, 0.24), (0, -0.24, 0.97), 0.095, 0.066, 0.08,
           o.SKIN, bevel=0.022)
    b.obox((0, 0.0, 0.36), (1, 0, 0), (0, 0.97, 0.24), (0, -0.24, 0.97), 0.105, 0.075, 0.03,
           RED, bevel=0.015)                                                              # red jerkin
    hy = -0.055
    o.orc_head(b, hy, z=-0.02)
    # the cap: a red cone falling to one side, a white trim, a white bobble
    b.cylinder(0, hy + 0.01, 0.49, 0.52, 0.075, SNOW, sides=8)
    o.tube(b, [(0, hy + 0.01, 0.52), (0.02, hy + 0.03, 0.6), (0.09, hy + 0.06, 0.62)],
           [0.068, 0.04, 0.0], RED, sides=7)
    b.ball(0.1, hy + 0.06, 0.59, 0.03, 0.055, SNOW, sides=6)
    # the sack, over the right shoulder
    b.ball(0.09, 0.1, 0.33, 0.12, 0.2, 'burlap', sides=8)
    b.rod((0.11, 0.0, 0.42), (0.1, 0.07, 0.5), 0.02, 'wood_dark')
    b.box(0.07, 0.11, 0.06, 0.12, 0.5, 0.53, GOLD)                                       # a ribbon peeping out
    # arms
    b.obox((0.14, -0.01, 0.36), (1, 0, 0), (0, 0.94, -0.34), (0, 0.34, 0.94), 0.034, 0.036, 0.07,
           o.SKIN, bevel=0.012)
    b.ball(0.14, 0.03, 0.43, 0.034, 0.05, o.SKIN, sides=6)
    b.obox((-0.14, -0.06, 0.3), (1, 0, 0), (0, 0.6, 0.8), (0, -0.8, 0.6), 0.033, 0.07, 0.035,
           o.SKIN, bevel=0.012)
    b.ball(-0.14, -0.13, 0.27, 0.034, 0.05, o.SKIN, sides=6)
    o.scale_since(b, 0, 0.95)
    return b


def shaman(lo, hi):
    """Bigger than a brute: a long pale-blue robe to the ground, a white fur collar, a hood with
    two icy horns, icicles hanging off the shoulders, a tall staff crowned with ice crystals."""
    b = Builder()
    b.frustum(0, 0, 0.0, 0.36, 0.17, 0.12, ICE, sides=9)                          # the robe
    b.frustum(0, 0, 0.0, 0.04, 0.175, 0.172, SNOW, sides=9)                       # its snowy hem
    b.box(-0.02, 0.02, -0.165, -0.12, 0.02, 0.34, ICE_DARK)                      # a dark seam
    b.obox((0, 0, 0.44), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.15, 0.1, 0.1, ICE, bevel=0.03)
    b.cylinder(0, 0, 0.5, 0.56, 0.16, SNOW, sides=9)                              # fur collar
    hy = -0.06
    o.orc_head(b, hy, z=0.07)
    # the hood and its horns of ice
    b.dome(0, hy + 0.03, 0.6, 0.1, 0.1, [ICE, ICE], sides=8)
    for k in (-1, 1):
        o.horn(b, (k * 0.08, hy + 0.04, 0.66), (k, 0, 0), 0.09, 0.02, color=ICE_DARK, up=1.2)
    # icicles off the shoulders
    for k in (-1, 1):
        for dx in (0.0, 0.05, 0.1):
            x = k * (0.12 + dx * 0.6)
            o.tube(b, [(x, 0.0, 0.5), (x, 0.0, 0.42 - dx)], [0.018, 0.0], 'white', sides=4)
    # arms: the right holds the staff, the left raised to cast
    b.obox((0.19, -0.02, 0.4), (1, 0, 0), (0, 0.94, -0.34), (0, 0.34, 0.94), 0.045, 0.045, 0.09, ICE,
           bevel=0.015)
    b.ball(0.2, -0.04, 0.3, 0.04, 0.06, o.SKIN, sides=6)
    b.rod((0.21, -0.05, 0.0), (0.21, -0.05, 0.86), 0.03, 'wood_light')
    for k in range(5):
        a = math.radians(72 * k)
        tip = Vector((0.21 + math.cos(a) * 0.07, -0.05 + math.sin(a) * 0.07, 0.98))
        o.tube(b, [(0.21, -0.05, 0.84), tip], [0.03, 0.0], ICE, sides=4)
    o.tube(b, [(0.21, -0.05, 0.84), (0.21, -0.05, 1.02)], [0.035, 0.0], 'white', sides=5)
    b.obox((-0.19, -0.06, 0.48), (1, 0, 0), (0, 0.6, 0.8), (0, -0.8, 0.6), 0.045, 0.09, 0.045, ICE,
           bevel=0.015)
    b.ball(-0.2, -0.13, 0.54, 0.04, 0.06, o.SKIN, sides=6)
    b.ball(-0.2, -0.15, 0.6, 0.045, 0.07, 'white', sides=6)                       # frost in the palm
    return b


kit.run("NYTree", tree, source_name="Flag1-lvl1", offset=(-6.0, 0, 0), collection="Новый год")
for n, colour in enumerate((RED, 'banner_blue', 'roof_green'), 1):
    kit.run(f"NYGift{n}", gift(colour), source_name="Flag1-lvl1", offset=(-3.5 + n * 0.5, -1.5, 0),
            collection="Новый год")
kit.run("NYSnowman", snowman, source_name="Flag1-lvl1", offset=(-2.5, 0, 0), collection="Новый год")
o.ensure_collection()
kit.run("OrcThief", thief, source_name="Flag1-lvl1", offset=(o.COLUMN_X + 13.0, 0, 0), collection=o.COLLECTION)
kit.run("OrcIceShaman", shaman, source_name="Flag1-lvl1", offset=(o.COLUMN_X + 14.0, 0, 0), collection=o.COLLECTION)
