"""The dungeon's monsters (the user, 2026-10-31): the old mine's own creatures and its two bosses.
Facing -y, standing on the origin, at GAME size (one unit = one map cell; the King is 0.53 tall).
Run in the models file:

    blender --background Buildings-Size1-Models.blend --python monsters.py

- MonsterRat: a pack animal, 0.22 long -- grey, pink tail, red eyes.
- MonsterBat: 0.3 across the wings, hanging in the air at 0.35 -- it hits and flies off.
- MonsterSpider: the hunter, 0.42 across its legs -- brown, a red hourglass, leaps from webs.
- MonsterSpiderling: the queen's brood, a small purple spider (0.22 across).
- MonsterSkeleton: a dead miner, a hero's height -- helmet with a lamp, a pick, rags.
- MonsterKobold: the blaster, 0.4 -- a red lizard-dog in a miner's cap with dynamite and a fuse alight.
- MonsterStoneGolem: the small golem, 0.8 -- boulders and moss, blue crystals for eyes.
- MonsterSpiderQueen: the boss, 2 across -- a black body, a huge purple abdomen marked violet,
  a gold crown of spikes, eight red eyes.
- MonsterSpiderCocoon: the queen sealed in at half health -- a web-wrapped egg 1.2 tall.
- MonsterOreGolem: the boss, 2 tall -- dark rock veined with gold and copper, blue crystals
  on its shoulders, a glowing core.

Jointed like wild.py: legs (LegL/LegR, or four-legged LegFL/LegFR/LegBL/LegBR -- a spider's four
leg groups hold two legs each), Head, ArmStrike/ArmOff, a bat's WingL/WingR -- child parts with
their origins at the joints, moved by Combat/FigureAnimator in the game.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import detail  # noqa: E402
import kit  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402
from mathutils import Vector  # noqa: E402

COLLECTION = "Подземелье-монстры"

FUR, FUR_DARK, FUR_LIGHT = 'roof_slate', 'roof_slate_dark', 'stone_mid'
PINK = 'plaster_pink'
EYE, GLOW, BLACK, TOOTH = 'fire', 'window_light', 'window', 'white'
CHITIN, CHITIN_DARK, MARK = 'wood_dark', 'door', 'banner_red'
BROOD, BROOD_DARK = 'roof_purple', 'roof_purple_dark'
BONE, RAGS, HELMET = 'plaster', 'burlap', 'iron'
TIMBER, LEATHER = 'wood_mid', 'wood_dark'
SCALE, SCALE_DARK, BELLY = 'roof_orange', 'roof_orange_dark', 'plaster_yellow'
DYNAMITE = 'banner_red'
ROCK, ROCK_DARK, ROCK_LIGHT, MOSS = 'stone_mid', 'stone_dark', 'stone_light', 'leaf'
CRYSTAL, GOLD, COPPER = 'water', 'gold', 'roof_orange'
SILK = 'plaster_white'

detail.OVERRIDE.update({c: detail.PLAIN for c in (
    FUR, FUR_DARK, FUR_LIGHT, PINK, EYE, GLOW, BLACK, TOOTH, CHITIN, CHITIN_DARK, MARK, BROOD, BROOD_DARK,
    BONE, RAGS, SCALE, SCALE_DARK, BELLY, ROCK, ROCK_DARK, ROCK_LIGHT, MOSS, CRYSTAL, COPPER, SILK, LEATHER)})


def rock(b, cx, cy, z0, rx, ry, h, color, top=None, sides=6, rot_deg=0.0, taper=0.55):
    """kit's crag, lifted to start at z0 instead of the ground."""
    b.bm.verts.ensure_lookup_table()
    since = len(b.bm.verts)
    b.crag(cx, cy, rx, ry, h, color, top=top, sides=sides, rot_deg=rot_deg, taper=taper)
    b.bm.verts.ensure_lookup_table()
    for v in list(b.bm.verts)[since:]:
        v.co.z += z0


def ensure_collection():
    coll = bpy.data.collections.get(COLLECTION)
    if coll is None:
        coll = bpy.data.collections.new(COLLECTION)
        bpy.context.scene.collection.children.link(coll)
    return COLLECTION


# ---------------------------------------------------------------- the rat (0.22 long, 0.09 high)

def rat_body(b):
    b.obox((0, 0.02, 0.055), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.035, 0.065, 0.032, FUR, bevel=0.015)
    b.obox((0, 0.06, 0.07), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.03, 0.035, 0.02, FUR_DARK, bevel=0.01)   # haunch
    o.tube(b, [(0, 0.08, 0.05), (0.01, 0.13, 0.035), (0.03, 0.18, 0.03), (0.05, 0.21, 0.04)],
           [0.009, 0.007, 0.005, 0.0], PINK, sides=4)                                                  # tail


def rat_leg(b, x, y):
    b.rod((x, y, 0.045), (x, y - 0.004, 0.008), 0.014, FUR_DARK)
    b.box(x - 0.008, x + 0.008, y - 0.016, y + 0.004, 0.0, 0.007, PINK)                               # paw


def rat_head(b):
    b.obox((0, -0.07, 0.065), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.024, 0.026, 0.022, FUR, bevel=0.008)
    b.pyramid(0, -0.096, 0.052, 0.014, 0.012, 0.072, FUR_LIGHT, sides=4)                              # snout
    b.obox((0, -0.125, 0.058), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.007, 0.006, 0.006, PINK)            # nose
    for k in (-1, 1):
        b.obox((k * 0.02, -0.062, 0.093), (1, 0, 0), (0, 0.4, 1), (0, -1, 0.4), 0.013, 0.013, 0.003, PINK)  # ears
        b.box(k * 0.014 - 0.004, k * 0.014 + 0.004, -0.098, -0.094, 0.07, 0.077, EYE)                 # eyes
    b.box(-0.004, 0.004, -0.122, -0.116, 0.042, 0.05, TOOTH)                                          # buck teeth


RAT = ("MonsterRat", rat_body, [
    ("LegFL", lambda b: rat_leg(b, -0.024, -0.025), (-0.024, -0.025, 0.045)),
    ("LegFR", lambda b: rat_leg(b, 0.024, -0.025), (0.024, -0.025, 0.045)),
    ("LegBL", lambda b: rat_leg(b, -0.026, 0.05), (-0.026, 0.05, 0.045)),
    ("LegBR", lambda b: rat_leg(b, 0.026, 0.05), (0.026, 0.05, 0.045)),
    ("Head", rat_head, (0.0, -0.045, 0.065)),
])


# ---------------------------------------------------------------- the bat (0.3 wingspan, flying at 0.35)

BAT_Z = 0.35


def bat_body(b):
    b.ball(0, 0.0, BAT_Z - 0.035, 0.03, 0.07, BROOD_DARK, sides=6, top=FUR_DARK)
    for k in (-1, 1):
        b.rod((k * 0.012, 0.01, BAT_Z - 0.03), (k * 0.015, 0.02, BAT_Z - 0.06), 0.008, BLACK)          # feet


def bat_wing(b, k):
    # three finger bones fanning out, the membrane stretched between them as flat panels
    root = Vector((k * 0.025, 0.0, BAT_Z + 0.005))
    tips = [Vector((k * 0.15, -0.02, BAT_Z + 0.05)), Vector((k * 0.14, 0.04, BAT_Z - 0.005)),
            Vector((k * 0.09, 0.07, BAT_Z - 0.03))]
    elbow = Vector((k * 0.08, -0.03, BAT_Z + 0.04))
    b.rod(tuple(root), tuple(elbow), 0.01, BLACK)
    for t in tips:
        b.rod(tuple(elbow), tuple(t), 0.006, BLACK)
    for a, c in ((tips[0], tips[1]), (tips[1], tips[2])):
        b.prism([(0, 0), (1, 0), (0, 1)], tuple(elbow), tuple(a - elbow), tuple(c - elbow), 0.004, BROOD_DARK,
                back_cap=True)
    b.prism([(0, 0), (1, 0), (0, 1)], tuple(root), tuple(elbow - root), tuple(tips[2] - root), 0.004, BROOD_DARK,
            back_cap=True)


def bat_head(b):
    b.obox((0, -0.035, BAT_Z + 0.015), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.022, 0.02, 0.02, BROOD_DARK, bevel=0.007)
    for k in (-1, 1):
        b.pyramid(k * 0.014, -0.032, BAT_Z + 0.03, 0.009, 0.007, BAT_Z + 0.07, BROOD_DARK, sides=4)  # big ears
        b.box(k * 0.01 - 0.004, k * 0.01 + 0.004, -0.057, -0.054, BAT_Z + 0.017, BAT_Z + 0.024, EYE)
        b.box(k * 0.006 - 0.002, k * 0.006 + 0.002, -0.057, -0.053, BAT_Z - 0.006, BAT_Z + 0.002, TOOTH)  # fangs


BAT = ("MonsterBat", bat_body, [
    ("WingL", lambda b: bat_wing(b, -1), (-0.025, 0.0, BAT_Z + 0.005)),
    ("WingR", lambda b: bat_wing(b, 1), (0.025, 0.0, BAT_Z + 0.005)),
    ("Head", bat_head, (0.0, -0.02, BAT_Z + 0.01)),
])


# ---------------------------------------------------------------- spiders: the hunter, the brood, the queen
# One spider built at a "unit" size; each kind scales it and picks its colours.

def spider_body(b, s, col, dark, mark, crown=False):
    # the cephalothorax in front, the abdomen behind and raised
    b.obox((0, -0.06 * s, 0.1 * s), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.055 * s, 0.06 * s, 0.04 * s, dark,
           bevel=0.02 * s)
    b.ball(0, 0.09 * s, 0.07 * s, 0.1 * s, 0.13 * s, col, sides=8)
    # the mark on the abdomen's back: an hourglass of two pyramids, flat
    b.obox((0, 0.09 * s, 0.195 * s), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.028 * s, 0.045 * s, 0.006 * s, mark)
    b.obox((0, 0.09 * s, 0.199 * s), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.012 * s, 0.02 * s, 0.006 * s, dark)
    b.pyramid(0, 0.19 * s, 0.07 * s, 0.02 * s, 0.02 * s, 0.11 * s, dark, sides=4)                     # spinnerets
    if crown:
        for i in range(5):
            a = math.radians(-60 + i * 30)
            x, y = math.sin(a) * 0.045 * s, -0.08 * s + (1 - math.cos(a)) * 0.02 * s
            b.pyramid(x, y, 0.13 * s, 0.009 * s, 0.009 * s, 0.17 * s + (0.02 * s if i == 2 else 0), GOLD, sides=4)


def spider_legs(b, s, side, front, col, dark):
    """Two legs on one side, front pair or back pair: up from the body to a knee, down to the floor."""
    pairs = ((-0.11, -0.09), (-0.05, -0.065)) if front else ((0.0, -0.045), (0.06, -0.03))
    spread = (1.0, 0.95) if front else (0.95, 1.05)
    for (y0, _), sp, i in zip(pairs, spread, range(2)):
        angle = math.radians((-55 + i * 25) if front else (15 + i * 30))
        out = Vector((side * math.cos(angle), math.sin(angle), 0))
        hip = Vector((side * 0.045 * s, (y0 + 0.04) * s, 0.1 * s))
        knee = hip + out * 0.13 * s * sp + Vector((0, 0, 0.09 * s))
        foot = hip + out * 0.24 * s * sp + Vector((0, 0, -0.1 * s))
        b.rod(tuple(hip), tuple(knee), 0.022 * s, col)
        b.rod(tuple(knee), tuple(foot), 0.016 * s, dark)


def spider_head(b, s, eye, fangs):
    b.obox((0, -0.13 * s, 0.1 * s), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.035 * s, 0.03 * s, 0.03 * s, CHITIN_DARK,
           bevel=0.01 * s)
    for k in (-1, 1):
        for j, (ex, ez, r) in enumerate(((0.012, 0.118, 0.008), (0.026, 0.11, 0.006),
                                         (0.008, 0.132, 0.005), (0.022, 0.13, 0.004))):
            b.box(k * ex * s - r * s, k * ex * s + r * s, -0.162 * s, -0.156 * s, ez * s - r * s, ez * s + r * s, eye)
        o.tube(b, [(k * 0.015 * s, -0.155 * s, 0.085 * s), (k * 0.012 * s, -0.175 * s, 0.05 * s)],
               [0.008 * s, 0.0], fangs, sides=4)


def spider(name, s, col, dark, mark, eye, crown=False):
    hip_z = 0.1 * s
    return (name, lambda b: spider_body(b, s, col, dark, mark, crown), [
        ("LegFL", lambda b: spider_legs(b, s, -1, True, col, dark), (-0.045 * s, -0.05 * s, hip_z)),
        ("LegFR", lambda b: spider_legs(b, s, 1, True, col, dark), (0.045 * s, -0.05 * s, hip_z)),
        ("LegBL", lambda b: spider_legs(b, s, -1, False, col, dark), (-0.045 * s, 0.04 * s, hip_z)),
        ("LegBR", lambda b: spider_legs(b, s, 1, False, col, dark), (0.045 * s, 0.04 * s, hip_z)),
        ("Head", lambda b: spider_head(b, s, eye, TOOTH if not crown else GOLD), (0.0, -0.11 * s, 0.1 * s)),
    ])


SPIDER = spider("MonsterSpider", 0.85, CHITIN, CHITIN_DARK, MARK, EYE)
SPIDERLING = spider("MonsterSpiderling", 0.45, BROOD, BROOD_DARK, 'roof_red', EYE)
QUEEN = spider("MonsterSpiderQueen", 4.0, BROOD_DARK, BLACK, BROOD, EYE, crown=True)


def cocoon(b):
    # a web-wrapped egg standing on a skirt of silk, threads binding it and running to the floor
    b.ball(0, 0, 0.0, 0.62, 0.14, SILK, sides=10)                                                     # skirt of web
    b.ball(0, 0, 0.05, 0.45, 1.15, SILK, sides=9, top='white')
    for z, r in ((0.25, 0.42), (0.5, 0.46), (0.75, 0.4), (0.95, 0.28)):
        b.ring(0, 0, z, z + 0.035, r - 0.01, r + 0.025, ['white'], sides=9, rot_deg=z * 90)
    for i in range(6):
        a = i * math.pi / 3 + 0.3
        top = (math.cos(a) * 0.3, math.sin(a) * 0.3, 0.9)
        foot = (math.cos(a) * 0.95, math.sin(a) * 0.95, 0.0)
        b.rod(top, foot, 0.012, 'white')
    # the queen's shadow through the silk: a darker band and two red eyes
    b.obox((0, -0.37, 0.62), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.2, 0.06, 0.14, BROOD, bevel=0.05)
    for k in (-1, 1):
        b.box(k * 0.07 - 0.025, k * 0.07 + 0.025, -0.44, -0.425, 0.7, 0.74, EYE)


# ---------------------------------------------------------------- bipeds: the skeleton, the kobold, the golems

def skel_body(b):
    b.box(-0.045, 0.045, -0.025, 0.025, 0.2, 0.225, BONE, bevel=0.006)                                # pelvis
    b.frustum(0, 0.0, 0.17, 0.235, 0.06, 0.055, RAGS, sides=6)                                        # rag about the hips
    b.rod((0, 0.012, 0.22), (0, 0.012, 0.36), 0.018, BONE)                                            # spine
    for i, z in enumerate((0.27, 0.3, 0.33)):                                                          # ribs
        w = 0.06 - i * 0.004
        b.obox((0, -0.005, z), (1, 0, 0), (0, 1, 0), (0, 0, 1), w, 0.03, 0.008, BONE, bevel=0.004)
    b.box(-0.08, 0.08, -0.02, 0.025, 0.355, 0.375, BONE, bevel=0.006)                                 # shoulders
    b.rod((-0.07, 0.03, 0.36), (0.03, 0.03, 0.2), 0.012, LEATHER)                                     # strap
    b.box(0.02, 0.06, 0.02, 0.05, 0.18, 0.23, LEATHER, bevel=0.006)                                   # pouch


def skel_leg(b, k):
    x = k * 0.03
    b.rod((x, 0, 0.2), (x, 0.0, 0.1), 0.016, BONE)
    b.rod((x, 0, 0.1), (x, -0.005, 0.02), 0.014, BONE)
    b.box(x - 0.017, x + 0.017, -0.04, 0.012, 0.0, 0.02, LEATHER, bevel=0.005)                        # boot


def skel_head(b):
    o.skull(b, 0, -0.005, 0.385, s=0.085)
    b.ball(0, 0.0, 0.43, 0.052, 0.04, HELMET, sides=8)                                               # miner's helmet
    b.cylinder(0, 0.0, 0.425, 0.434, 0.06, HELMET, sides=8)                                          # brim
    b.box(-0.014, 0.014, -0.058, -0.046, 0.442, 0.462, 'gold')                                        # lamp
    b.box(-0.009, 0.009, -0.062, -0.058, 0.446, 0.458, EYE)
    for k in (-1, 1):
        b.box(k * 0.017 - 0.007, k * 0.017 + 0.007, -0.048, -0.044, 0.398, 0.408, GLOW)              # eye lights


def skel_pick_arm(b):
    b.rod((0.075, 0.0, 0.36), (0.09, -0.02, 0.28), 0.013, BONE)
    b.rod((0.09, -0.02, 0.28), (0.09, -0.07, 0.24), 0.012, BONE)
    # the pick: a haft rising past the shoulder, an iron head across it
    b.rod((0.09, -0.08, 0.2), (0.09, -0.06, 0.48), 0.014, TIMBER)
    o.tube(b, [(0.09, -0.06, 0.47), (0.09, -0.15, 0.45), (0.09, -0.19, 0.4)], [0.012, 0.009, 0.0], HELMET, sides=4)
    o.tube(b, [(0.09, -0.06, 0.47), (0.09, 0.02, 0.46), (0.09, 0.05, 0.43)], [0.011, 0.008, 0.0], HELMET, sides=4)


def skel_off_arm(b):
    b.rod((-0.075, 0.0, 0.36), (-0.09, 0.0, 0.28), 0.013, BONE)
    b.rod((-0.09, 0.0, 0.28), (-0.085, -0.02, 0.21), 0.012, BONE)
    b.ball(-0.085, -0.02, 0.19, 0.012, 0.02, BONE, sides=5)


SKELETON = ("MonsterSkeleton", skel_body, [
    ("LegL", lambda b: skel_leg(b, -1), (-0.03, 0.0, 0.2)),
    ("LegR", lambda b: skel_leg(b, 1), (0.03, 0.0, 0.2)),
    ("Head", skel_head, (0.0, 0.0, 0.37)),
    ("ArmStrike", skel_pick_arm, (0.075, 0.0, 0.36)),
    ("ArmOff", skel_off_arm, (-0.075, 0.0, 0.36)),
])


def kobold_body(b):
    b.obox((0, 0.01, 0.17), (1, 0, 0), (0, 1, 0.2), (0, -0.2, 1), 0.05, 0.04, 0.06, SCALE, bevel=0.018)
    b.obox((0, -0.025, 0.165), (1, 0, 0), (0, 1, 0.2), (0, -0.2, 1), 0.032, 0.012, 0.045, BELLY, bevel=0.008)
    b.cylinder(0, 0.0, 0.12, 0.14, 0.055, LEATHER, sides=7)                                           # belt
    # a bandolier of dynamite across the chest
    b.rod((-0.045, -0.04, 0.23), (0.04, -0.045, 0.13), 0.012, LEATHER)
    for i in range(3):
        t = 0.25 + i * 0.25
        x, z = -0.045 + 0.085 * t, 0.23 - 0.1 * t
        b.cylinder(x, -0.055, z - 0.02, z + 0.02, 0.008, DYNAMITE, sides=5)
    o.tube(b, [(0, 0.04, 0.12), (0.0, 0.1, 0.08), (0.02, 0.15, 0.03)], [0.018, 0.012, 0.0], SCALE, sides=5)  # tail
    b.box(-0.03, 0.03, 0.04, 0.075, 0.15, 0.21, LEATHER, bevel=0.008)                                 # pack


def kobold_leg(b, k):
    x = k * 0.03
    b.rod((x, 0.0, 0.12), (x, 0.015, 0.06), 0.024, SCALE)
    b.rod((x, 0.015, 0.06), (x, -0.005, 0.01), 0.018, SCALE_DARK)
    b.box(x - 0.016, x + 0.016, -0.04, 0.01, 0.0, 0.014, SCALE_DARK, bevel=0.004)


def kobold_head(b):
    b.obox((0, -0.03, 0.27), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.035, 0.035, 0.03, SCALE, bevel=0.012)
    b.obox((0, -0.075, 0.258), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.022, 0.025, 0.016, SCALE, bevel=0.006)  # snout
    for k in (-1, 1):
        b.box(k * 0.02 - 0.007, k * 0.02 + 0.007, -0.066, -0.06, 0.275, 0.287, EYE)
        o.horn(b, (k * 0.026, 0.0, 0.29), (k, 0.6, 0), 0.04, 0.008, color=TOOTH, up=1.2)
        b.box(k * 0.01 - 0.003, k * 0.01 + 0.003, -0.1, -0.096, 0.243, 0.252, TOOTH)
    # a battered miner's cap with a candle stub
    b.ball(0, -0.02, 0.293, 0.04, 0.03, LEATHER, sides=7)
    b.cylinder(0.0, -0.05, 0.315, 0.335, 0.006, SILK, sides=4)
    b.pyramid(0.0, -0.05, 0.335, 0.004, 0.004, 0.348, EYE)


def kobold_throw_arm(b):
    b.rod((0.055, 0.0, 0.22), (0.075, 0.01, 0.27), 0.018, SCALE)
    b.rod((0.075, 0.01, 0.27), (0.08, 0.0, 0.33), 0.016, SCALE)
    # a stick of dynamite held high, the fuse spitting
    b.cylinder(0.08, 0.0, 0.33, 0.39, 0.012, DYNAMITE, sides=5)
    b.rod((0.08, 0.0, 0.39), (0.085, 0.005, 0.41), 0.004, BLACK)
    b.ball(0.087, 0.006, 0.405, 0.012, 0.02, EYE, sides=5)


def kobold_off_arm(b):
    b.rod((-0.055, 0.0, 0.22), (-0.07, -0.01, 0.16), 0.018, SCALE)
    b.rod((-0.07, -0.01, 0.16), (-0.065, -0.04, 0.12), 0.016, SCALE)
    b.ball(-0.065, -0.045, 0.1, 0.014, 0.025, SCALE_DARK, sides=5)


KOBOLD = ("MonsterKobold", kobold_body, [
    ("LegL", lambda b: kobold_leg(b, -1), (-0.03, 0.0, 0.12)),
    ("LegR", lambda b: kobold_leg(b, 1), (0.03, 0.0, 0.12)),
    ("Head", kobold_head, (0.0, -0.02, 0.25)),
    ("ArmStrike", kobold_throw_arm, (0.055, 0.0, 0.22)),
    ("ArmOff", kobold_off_arm, (-0.055, 0.0, 0.22)),
])


def golem_body(b, s, rock_c, dark, vein, crystal, core):
    # boulders piled into a torso: a broad chest, a belly stone, the shoulders as two great rocks
    rock(b, 0, 0.0, 0.25 * s, 0.17 * s, 0.12 * s, 0.14 * s, rock_c, top=dark, sides=6, rot_deg=10, taper=0.9)  # hips
    b.obox((0, 0.0, 0.42 * s), (1, 0, 0), (0, 1, 0.15), (0, -0.15, 1), 0.2 * s, 0.13 * s, 0.13 * s, rock_c,
           bevel=0.04 * s)
    b.obox((0, 0.02, 0.53 * s), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.17 * s, 0.11 * s, 0.04 * s, dark,
           bevel=0.03 * s)
    # veins of ore across the chest
    for (x0, z0, x1, z1) in ((-0.15, 0.36, -0.02, 0.48), (0.03, 0.33, 0.16, 0.45), (-0.06, 0.28, 0.08, 0.3)):
        b.rod((x0 * s, -0.135 * s, z0 * s), (x1 * s, -0.13 * s, z1 * s), 0.02 * s, vein)
    if core:
        b.obox((0, -0.13 * s, 0.4 * s), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.045 * s, 0.01 * s, 0.045 * s, core)
        b.obox((0, -0.135 * s, 0.4 * s), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.022 * s, 0.01 * s, 0.022 * s, 'white')
    # crystals growing out of the back and shoulders
    for (x, y, z, h) in ((-0.12, 0.08, 0.5, 0.16), (0.1, 0.1, 0.52, 0.2), (0.0, 0.11, 0.48, 0.12),
                         (-0.24, 0.0, 0.58, 0.12), (0.25, 0.02, 0.58, 0.14)):
        b.pyramid(x * s, y * s, z * s, 0.025 * s, 0.025 * s, (z + h) * s, crystal, sides=5)
    b.ball(-0.06 * s, 0.05 * s, 0.53 * s, 0.07 * s, 0.05 * s, MOSS, sides=6)                            # moss


def golem_leg(b, k, s, rock_c, dark):
    x = k * 0.1 * s
    rock(b, x, 0.0, 0.04 * s, 0.075 * s, 0.07 * s, 0.24 * s, rock_c, top=rock_c, sides=5, taper=0.8)
    b.box(x - 0.08 * s, x + 0.08 * s, -0.1 * s, 0.06 * s, 0.0, 0.06 * s, dark, bevel=0.02 * s)            # foot


def golem_head(b, s, rock_c, dark, eye):
    b.obox((0, -0.05 * s, 0.6 * s), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.075 * s, 0.07 * s, 0.06 * s, rock_c,
           bevel=0.02 * s)
    b.box(-0.08 * s, 0.08 * s, -0.125 * s, -0.09 * s, 0.63 * s, 0.66 * s, dark)                              # brow
    for k in (-1, 1):
        b.box(k * 0.035 * s - 0.016 * s, k * 0.035 * s + 0.016 * s, -0.128 * s, -0.12 * s, 0.6 * s, 0.625 * s, eye)


def golem_arm(b, k, s, rock_c, dark, vein, strike):
    x = k * 0.22 * s
    rock(b, x, 0.0, 0.46 * s, 0.09 * s, 0.08 * s, 0.11 * s, dark, top=rock_c, sides=5, taper=0.6)    # shoulder stone
    b.rod((x, 0.0, 0.5 * s), (x + k * 0.05 * s, -0.02 * s, 0.32 * s), 0.09 * s, rock_c)
    b.rod((x + k * 0.05 * s, -0.02 * s, 0.32 * s), (x + k * 0.06 * s, -0.06 * s, 0.17 * s), 0.08 * s, rock_c)
    # a fist like a boulder, bigger on the striking side
    r = (0.1 if strike else 0.08) * s
    rock(b, x + k * 0.06 * s, -0.07 * s, 0.05 * s, r, r * 0.9, r * 1.4, dark, top=rock_c, sides=6, taper=0.7)
    b.rod((x + k * 0.03 * s, -0.04 * s, 0.4 * s), (x + k * 0.06 * s, -0.05 * s, 0.24 * s), 0.018 * s, vein)


def golem(name, s, rock, dark, vein, crystal, eye, core):
    shoulder_z = 0.5 * s
    def body(b):
        golem_body(b, s, rock, dark, vein, crystal, core)
    return (name, body, [
        ("LegL", lambda b: golem_leg(b, -1, s, rock, dark), (-0.1 * s, 0.0, 0.27 * s)),
        ("LegR", lambda b: golem_leg(b, 1, s, rock, dark), (0.1 * s, 0.0, 0.27 * s)),
        ("Head", lambda b: golem_head(b, s, rock, dark, eye), (0.0, -0.03 * s, 0.56 * s)),
        ("ArmStrike", lambda b: golem_arm(b, 1, s, rock, dark, vein, True), (0.22 * s, 0.0, shoulder_z)),
        ("ArmOff", lambda b: golem_arm(b, -1, s, rock, dark, vein, False), (-0.22 * s, 0.0, shoulder_z)),
    ])


STONE_GOLEM = golem("MonsterStoneGolem", 1.2, ROCK, ROCK_DARK, MOSS, CRYSTAL, CRYSTAL, None)
ORE_GOLEM = golem("MonsterOreGolem", 3.0, 'roof_slate', 'roof_slate_dark', GOLD, CRYSTAL, EYE, EYE)


# ---------------------------------------------------------------- building and installing (as wild.py)

def part_mesh(name, build_fn, pivot, material):
    b = Builder()
    build_fn(b)
    shift = Vector(pivot)
    for v in b.bm.verts:
        v.co -= shift
    tris = sum(len(f.verts) - 2 for f in b.bm.faces)
    me = b.to_mesh(name)
    me.materials.append(material)
    detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
    return me, tris


def install(name, mesh, parent, location, collection):
    obj = bpy.data.objects.get(name)
    if obj is None:
        obj = bpy.data.objects.new(name, mesh)
        bpy.data.collections[collection].objects.link(obj)
    else:
        old = obj.data
        obj.data = mesh
        if old.users == 0:
            bpy.data.meshes.remove(old)
    obj.parent = parent
    obj.matrix_parent_inverse.identity()
    obj.location = location
    return obj


def figure(spec, location, mat, coll):
    name, body_fn, parts = spec
    mesh, tris = part_mesh(name, body_fn, (0, 0, 0), mat)
    root = install(name, mesh, None, location, coll)
    for part, fn, pivot in parts:
        mesh, t = part_mesh(f"{name}-{part}", fn, pivot, mat)
        install(f"{name}-{part}", mesh, root, Vector(pivot), coll)
        tris += t
    print(f"MODEL {name}: {len(parts) + 1} parts, tris={tris}")


def main():
    coll = ensure_collection()
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    base = Vector((o.COLUMN_X + 10.0, 12.0, 0))
    specs = (RAT, BAT, SPIDER, SPIDERLING, SKELETON, KOBOLD, STONE_GOLEM, QUEEN, ORE_GOLEM)
    x = 0.0
    for spec in specs:
        figure(spec, base + Vector((x, 0, 0)), mat, coll)
        x += 3.0
    mesh, tris = part_mesh("MonsterSpiderCocoon", cocoon, (0, 0, 0), mat)
    install("MonsterSpiderCocoon", mesh, None, base + Vector((x, 0, 0)), coll)
    print(f"MODEL MonsterSpiderCocoon: tris={tris}")
    kit.file_display_settings()
    bpy.ops.wm.save_mainfile()


main()
