"""The level-3 dungeon's monsters, «Огненные глубины» (the user, 2026-11-04: ALL NEW models). Facing -y,
standing on the origin, at GAME size (one unit = one map cell; the King is 0.53 tall), jointed like
monsters.py (LegL/LegR or LegFL..BR, Head, ArmStrike/ArmOff, WingL/WingR). Run in the models file:

    blender --background Buildings-Size1-Models.blend --python monsters3.py

- MonsterFireImp: a small red devil, ~0.32 -- horns, a pot belly, claws, a tail with a flame at its tip.
- MonsterLavaHound: a hound of cooled lava, 0.5 long -- charcoal hide cracked with glowing seams, a mane
  of flame, yellow eyes.
- MonsterSalamander: a low fire lizard, 0.48 long -- orange with yellow spots, a crest of flame along its
  back; it spits fire (ranged).
- MonsterObsidianGolem: black-violet glass piled into a giant, 0.75 tall -- fire in its cracks, violet
  shards on its back.
- MonsterEmberCultist: a robed fire priest, 0.5 -- a deep red robe with gold trim, a dark hood with
  burning eyes, a staff with a fireball (ranged).
- MonsterMagmaSpawn: a small golem of magma, 0.36 -- dark crust over glowing lava; the titan sheds these.
Bosses (much bigger than a hero):
- MonsterDragon: a red dragon, ~2.6 long -- gold belly, horns, a spined back, leathery wings (WingL/R),
  four legs, a long tail.
- MonsterLavaTitan: a giant of black rock and running lava, ~2.1 tall -- a crown of flame, lava pouring
  from its fists.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import detail  # noqa: E402
import kit  # noqa: E402
import monsters as m  # noqa: E402
import orcs as o  # noqa: E402
from mathutils import Vector  # noqa: E402

COLLECTION = "Подземелье-3-монстры"

MAGMA, MAGMA_HOT, EMBER = 'fire', 'plaster_yellow', 'roof_red'
CRUST, CHAR = 'window', 'iron'
IMP, IMP_DARK, IMP_BELLY = 'roof_red', 'roof_red_dark', 'roof_orange'
HORN, CLAW = 'plaster', 'stone_light'
SAL, SAL_DARK, SAL_SPOT = 'roof_orange', 'roof_orange_dark', 'plaster_yellow'
OBS, OBS_DARK, OBS_SHARD = 'roof_purple_dark', 'window', 'roof_purple'
ROBE, ROBE_DARK, HOOD = 'roof_red_dark', 'door', 'window'
GOLD = 'gold'
DRAGON, DRAGON_DARK, DRAGON_BELLY, WING = 'roof_red', 'roof_red_dark', 'plaster_yellow', 'roof_orange_dark'
TIMBER = 'wood_dark'

detail.OVERRIDE.update({c: detail.PLAIN for c in (
    MAGMA, MAGMA_HOT, EMBER, CRUST, CHAR, IMP, IMP_DARK, IMP_BELLY, HORN, CLAW, SAL, SAL_DARK, SAL_SPOT,
    OBS, OBS_DARK, OBS_SHARD, ROBE, ROBE_DARK, HOOD, GOLD, DRAGON, DRAGON_DARK, DRAGON_BELLY, WING, TIMBER)})


def ensure_collection():
    coll = bpy.data.collections.get(COLLECTION)
    if coll is None:
        coll = bpy.data.collections.new(COLLECTION)
        bpy.context.scene.collection.children.link(coll)
    return COLLECTION


def scaled(fn, s):
    """A part built at its own size and grown by s about the origin."""
    def build(b):
        b.bm.verts.ensure_lookup_table()
        since = len(b.bm.verts)
        fn(b)
        o.scale_since(b, since, s)
    return build


def flame(b, x, y, z, h, r):
    """A tongue of fire: a red base, an orange body, a yellow tip."""
    b.frustum(x, y, z, z + h * 0.35, r, r * 0.75, EMBER, sides=5)
    b.pyramid(x, y, z + h * 0.35, r * 0.75, r * 0.75, z + h * 0.8, MAGMA, sides=5)
    b.pyramid(x, y + r * 0.1, z + h * 0.5, r * 0.4, r * 0.4, z + h, MAGMA_HOT, sides=4)


# ---------------------------------------------------------------- the fire imp (a biped, 0.32)

def imp_body(b):
    b.obox((0, 0.0, 0.16), (1, 0, 0), (0, 1, 0.15), (0, -0.15, 1), 0.045, 0.038, 0.055, IMP, bevel=0.018)
    b.ball(0, -0.02, 0.12, 0.042, 0.06, IMP_BELLY, sides=7)                                         # pot belly
    o.tube(b, [(0, 0.035, 0.11), (0.0, 0.09, 0.08), (0.02, 0.13, 0.12), (0.01, 0.15, 0.17)],
           [0.014, 0.01, 0.007, 0.004], IMP_DARK, sides=5)                                          # tail
    flame(b, 0.01, 0.15, 0.165, 0.06, 0.016)                                                        # its flame


def imp_leg(b, k):
    x = k * 0.026
    b.rod((x, 0.0, 0.1), (x, 0.02, 0.05), 0.02, IMP)
    b.rod((x, 0.02, 0.05), (x, 0.0, 0.01), 0.015, IMP_DARK)
    b.box(x - 0.014, x + 0.014, -0.03, 0.01, 0.0, 0.012, CHAR, bevel=0.004)                       # hooves


def imp_head(b):
    b.obox((0, -0.02, 0.25), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.035, 0.032, 0.032, IMP, bevel=0.013)
    b.obox((0, -0.05, 0.235), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.02, 0.012, 0.012, IMP_DARK, bevel=0.005)  # snout
    for k in (-1, 1):
        o.horn(b, (k * 0.024, -0.01, 0.275), (k, 0.3, 0), 0.05, 0.01, color=HORN, up=1.6)
        b.box(k * 0.016 - 0.008, k * 0.016 + 0.008, -0.054, -0.048, 0.252, 0.264, MAGMA_HOT)       # eyes
        b.pyramid(k * 0.04, -0.01, 0.245, 0.012, 0.006, 0.27, IMP_DARK, sides=3)                    # pointed ears
    b.box(-0.014, 0.014, -0.064, -0.058, 0.222, 0.228, 'white')                                     # a grin


def imp_arm(k, strike):
    def build(b):
        x = k * 0.05
        b.rod((x, 0.0, 0.2), (x + k * 0.02, -0.02, 0.15), 0.015, IMP)
        b.rod((x + k * 0.02, -0.02, 0.15), (x + k * 0.025, -0.05, 0.11), 0.013, IMP)
        for c in (-1, 0, 1):                                                                        # claws
            b.pyramid(x + k * 0.025 + c * 0.008, -0.06, 0.1, 0.004, 0.004, 0.08, CLAW, sides=3)
        if strike:                                                                                  # a fire in the hand
            b.ball(x + k * 0.025, -0.06, 0.12, 0.016, 0.03, MAGMA, sides=5, top=MAGMA_HOT)
    return build


FIRE_IMP = ("MonsterFireImp", imp_body, [
    ("LegL", lambda b: imp_leg(b, -1), (-0.026, 0.0, 0.1)),
    ("LegR", lambda b: imp_leg(b, 1), (0.026, 0.0, 0.1)),
    ("Head", imp_head, (0.0, -0.01, 0.22)),
    ("ArmStrike", imp_arm(1, True), (0.05, 0.0, 0.2)),
    ("ArmOff", imp_arm(-1, False), (-0.05, 0.0, 0.2)),
])


# ---------------------------------------------------------------- the lava hound (four legs, 0.5 long)

HS = 2.2                                   # the rat's shapes, grown


def hound_body(b):
    s = HS
    b.obox((0, 0.02 * s, 0.06 * s), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.034 * s, 0.07 * s, 0.03 * s, CHAR, bevel=0.012 * s)
    b.obox((0, -0.03 * s, 0.07 * s), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.038 * s, 0.03 * s, 0.032 * s, CRUST, bevel=0.012 * s)
    for (x0, y0, x1, y1) in ((-0.03, -0.02, -0.01, 0.06), (0.02, 0.0, 0.03, 0.07), (-0.005, 0.03, 0.01, 0.08)):
        b.rod((x0 * s, y0 * s, 0.09 * s), (x1 * s, y1 * s, 0.088 * s), 0.004 * s, MAGMA)        # glowing seams
    for i in range(5):                                                                              # the mane of flame
        flame(b, (i % 2 - 0.5) * 0.02 * s, (-0.045 + i * 0.018) * s, 0.085 * s, (0.06 - i * 0.008) * s, 0.012 * s)
    o.tube(b, [(0, 0.09 * s, 0.07 * s), (0, 0.13 * s, 0.08 * s), (0.01 * s, 0.16 * s, 0.1 * s)],
           [0.01 * s, 0.007 * s, 0.0], CHAR, sides=5)                                               # tail
    flame(b, 0.01 * s, 0.16 * s, 0.095 * s, 0.04 * s, 0.008 * s)


def hound_leg(x, y):
    def build(b):
        s = HS
        b.rod((x * s, y * s, 0.05 * s), (x * s, (y - 0.005) * s, 0.008 * s), 0.013 * s, CRUST)
        b.box((x - 0.009) * s, (x + 0.009) * s, (y - 0.018) * s, (y + 0.004) * s, 0.0, 0.008 * s, CHAR)
    return build


def hound_head(b):
    s = HS
    b.obox((0, -0.075 * s, 0.075 * s), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.025 * s, 0.028 * s, 0.024 * s, CHAR, bevel=0.008 * s)
    b.obox((0, -0.108 * s, 0.065 * s), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.016 * s, 0.018 * s, 0.012 * s, CRUST, bevel=0.005 * s)
    b.box(-0.012 * s, 0.012 * s, -0.124 * s, -0.115 * s, 0.054 * s, 0.058 * s, MAGMA)          # glowing jaws
    for k in (-1, 1):
        b.pyramid(k * 0.016 * s, -0.065 * s, 0.095 * s, 0.008 * s, 0.006 * s, 0.12 * s, CRUST, sides=4)  # ears
        b.box((k * 0.013 - 0.004) * s, (k * 0.013 + 0.004) * s, -0.104 * s, -0.1 * s, 0.078 * s, 0.085 * s, MAGMA_HOT)
        b.pyramid(k * 0.008 * s, -0.12 * s, 0.054 * s, 0.002 * s, 0.002 * s, 0.044 * s, 'white', sides=3)  # fangs


LAVA_HOUND = ("MonsterLavaHound", hound_body, [
    ("LegFL", hound_leg(-0.024, -0.03), (-0.024 * HS, -0.03 * HS, 0.05 * HS)),
    ("LegFR", hound_leg(0.024, -0.03), (0.024 * HS, -0.03 * HS, 0.05 * HS)),
    ("LegBL", hound_leg(-0.026, 0.06), (-0.026 * HS, 0.06 * HS, 0.05 * HS)),
    ("LegBR", hound_leg(0.026, 0.06), (0.026 * HS, 0.06 * HS, 0.05 * HS)),
    ("Head", hound_head, (0.0, -0.05 * HS, 0.07 * HS)),
])


# ---------------------------------------------------------------- the salamander (four legs, low, 0.48 long)

SS = 2.0


def sal_body(b):
    s = SS
    b.obox((0, 0.02 * s, 0.035 * s), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.035 * s, 0.075 * s, 0.02 * s, SAL, bevel=0.012 * s)
    b.obox((0, 0.02 * s, 0.022 * s), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.03 * s, 0.065 * s, 0.01 * s, SAL_SPOT, bevel=0.005 * s)
    for (x, y) in ((-0.02, -0.02), (0.018, 0.01), (-0.01, 0.05), (0.015, 0.07)):                    # yellow spots
        b.box((x - 0.007) * s, (x + 0.007) * s, (y - 0.007) * s, (y + 0.007) * s, 0.052 * s, 0.056 * s, SAL_SPOT)
    for i in range(6):                                                                              # a crest of flame
        flame(b, 0.0, (-0.04 + i * 0.022) * s, 0.05 * s, (0.035 - abs(i - 2) * 0.005) * s, 0.008 * s)
    o.tube(b, [(0, 0.09 * s, 0.03 * s), (0.02 * s, 0.14 * s, 0.02 * s), (0.0, 0.19 * s, 0.015 * s), (-0.02 * s, 0.22 * s, 0.012 * s)],
           [0.016 * s, 0.011 * s, 0.006 * s, 0.0], SAL_DARK, sides=5)                              # tail


def sal_leg(x, y):
    def build(b):
        s = SS
        k = 1 if x > 0 else -1
        b.rod((x * s, y * s, 0.03 * s), ((x + k * 0.02) * s, y * s, 0.02 * s), 0.009 * s, SAL_DARK)
        b.rod(((x + k * 0.02) * s, y * s, 0.02 * s), ((x + k * 0.025) * s, (y - 0.005) * s, 0.0), 0.008 * s, SAL_DARK)
    return build


def sal_head(b):
    s = SS
    b.obox((0, -0.075 * s, 0.035 * s), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.026 * s, 0.03 * s, 0.016 * s, SAL, bevel=0.008 * s)
    b.obox((0, -0.11 * s, 0.03 * s), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.018 * s, 0.014 * s, 0.01 * s, SAL_DARK, bevel=0.005 * s)
    b.ball(0, -0.125 * s, 0.028 * s, 0.01 * s, 0.014 * s, MAGMA_HOT, sides=5)                    # the fire in its mouth
    for k in (-1, 1):
        b.ball(k * 0.02 * s, -0.08 * s, 0.05 * s, 0.008 * s, 0.012 * s, CRUST, sides=5)          # bulging eyes
        b.box((k * 0.02 - 0.004) * s, (k * 0.02 + 0.004) * s, -0.089 * s, -0.086 * s, 0.05 * s, 0.056 * s, MAGMA_HOT)


SALAMANDER = ("MonsterSalamander", sal_body, [
    ("LegFL", sal_leg(-0.03, -0.03), (-0.03 * SS, -0.03 * SS, 0.03 * SS)),
    ("LegFR", sal_leg(0.03, -0.03), (0.03 * SS, -0.03 * SS, 0.03 * SS)),
    ("LegBL", sal_leg(-0.03, 0.07), (-0.03 * SS, 0.07 * SS, 0.03 * SS)),
    ("LegBR", sal_leg(0.03, 0.07), (0.03 * SS, 0.07 * SS, 0.03 * SS)),
    ("Head", sal_head, (0.0, -0.055 * SS, 0.035 * SS)),
])


# ---------------------------------------------------------------- golems: obsidian, the magma spawn

OBSIDIAN_GOLEM = m.golem("MonsterObsidianGolem", 1.25, OBS, OBS_DARK, MAGMA, OBS_SHARD, MAGMA, MAGMA)
MAGMA_SPAWN = m.golem("MonsterMagmaSpawn", 0.6, EMBER, CRUST, MAGMA_HOT, MAGMA, MAGMA_HOT, MAGMA_HOT)


# ---------------------------------------------------------------- the ember cultist (a biped in a robe, 0.5)

def cultist_body(b):
    b.frustum(0, 0, 0.04, 0.34, 0.11, 0.06, ROBE, sides=8)                                         # the robe
    b.ring(0, 0, 0.04, 0.07, 0.105, 0.115, [GOLD], sides=8)                                         # gold hem
    b.frustum(0, 0, 0.34, 0.38, 0.065, 0.075, ROBE_DARK, sides=8)                                  # shoulders
    b.obox((0, -0.07, 0.24), (1, 0, 0), (0, 1, 0.3), (0, -0.3, 1), 0.012, 0.004, 0.09, GOLD)       # a gold stole
    b.cylinder(0, 0, 0.2, 0.22, 0.085, CHAR, sides=8)                                              # the belt
    b.obox((0, -0.088, 0.21), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.016, 0.004, 0.016, MAGMA)         # an ember buckle


def cultist_leg(b, k):
    x = k * 0.03
    b.box(x - 0.02, x + 0.02, -0.03, 0.02, 0.0, 0.04, CHAR, bevel=0.006)                         # feet under the hem


def cultist_head(b):
    b.ball(0, -0.005, 0.38, 0.06, 0.11, HOOD, sides=8)                                             # the hood
    b.pyramid(0, 0.02, 0.44, 0.04, 0.04, 0.52, HOOD, sides=6)                                      # its peak
    b.obox((0, -0.05, 0.41), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.035, 0.012, 0.03, CRUST)            # the dark face
    for k in (-1, 1):
        b.box(k * 0.015 - 0.007, k * 0.015 + 0.007, -0.066, -0.06, 0.412, 0.422, MAGMA_HOT)       # burning eyes


def cultist_staff_arm(b):
    b.rod((0.07, 0.0, 0.33), (0.1, -0.03, 0.24), 0.02, ROBE)
    b.rod((0.11, -0.04, 0.0), (0.11, -0.04, 0.55), 0.01, TIMBER)                                   # the staff
    for a in range(3):                                                                              # its iron claw
        ang = a * 2 * math.pi / 3
        b.rod((0.11, -0.04, 0.53), (0.11 + math.cos(ang) * 0.03, -0.04 + math.sin(ang) * 0.03, 0.6), 0.006, CHAR)
    b.ball(0.11, -0.04, 0.56, 0.028, 0.055, MAGMA, sides=6, top=MAGMA_HOT)                         # the fireball


def cultist_off_arm(b):
    b.rod((-0.07, 0.0, 0.33), (-0.09, -0.04, 0.25), 0.02, ROBE)
    b.ball(-0.092, -0.05, 0.235, 0.015, 0.025, MAGMA, sides=5)                                    # fire on the palm


EMBER_CULTIST = ("MonsterEmberCultist", cultist_body, [
    ("LegL", lambda b: cultist_leg(b, -1), (-0.03, 0.0, 0.04)),
    ("LegR", lambda b: cultist_leg(b, 1), (0.03, 0.0, 0.04)),
    ("Head", cultist_head, (0.0, 0.0, 0.36)),
    ("ArmStrike", cultist_staff_arm, (0.07, 0.0, 0.33)),
    ("ArmOff", cultist_off_arm, (-0.07, 0.0, 0.33)),
])


# ---------------------------------------------------------------- the lava titan (a golem grown x3.5)

TS = 3.5
_titan = m.golem("MonsterLavaTitan", TS, CRUST, CHAR, MAGMA, MAGMA, MAGMA_HOT, MAGMA_HOT)


def titan_body(b):
    _titan[1](b)
    s = TS
    for (x, z) in ((-0.1, 0.3), (0.12, 0.35), (0.0, 0.47), (-0.16, 0.44)):                          # lava pouring down
        b.rod((x * s, -0.13 * s, z * s), (x * s, -0.14 * s, (z - 0.12) * s), 0.012 * s, MAGMA)
    for x in (-0.16, 0.0, 0.16):                                                                    # fire on the shoulders
        flame(b, x * s, 0.04 * s, 0.56 * s, 0.14 * s, 0.025 * s)


def titan_head(b):
    s = TS
    m.golem_head(b, s, CRUST, CHAR, MAGMA_HOT)
    for i in range(5):                                                                              # a crown of flame
        a = (i - 2) * 0.35
        flame(b, math.sin(a) * 0.06 * s, (-0.05 + (1 - math.cos(a)) * 0.05) * s, 0.66 * s, (0.12 - abs(i - 2) * 0.02) * s, 0.016 * s)


LAVA_TITAN = ("MonsterLavaTitan", titan_body,
              [p if p[0] != "Head" else ("Head", titan_head, p[2]) for p in _titan[2]])


# ---------------------------------------------------------------- the dragon (four legs and wings, ~2.6 long)

DZ = 0.55                                   # the body's middle height


def dragon_body(b):
    b.obox((0, 0.15, DZ), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.3, 0.55, 0.26, DRAGON, bevel=0.12)
    b.obox((0, 0.12, DZ - 0.17), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.22, 0.45, 0.1, DRAGON_BELLY, bevel=0.06)
    for i in range(7):                                                                              # spines down the back
        y = -0.3 + i * 0.17
        b.pyramid(0, y, DZ + 0.22, 0.035, 0.06, DZ + 0.38 - i * 0.015, HORN, sides=4)
    o.tube(b, [(0, 0.65, DZ), (0.05, 1.0, DZ - 0.1), (0.2, 1.35, DZ - 0.25), (0.35, 1.6, DZ - 0.35), (0.42, 1.75, DZ - 0.38)],
           [0.18, 0.13, 0.09, 0.05, 0.0], DRAGON, sides=7)                                         # the tail
    b.prism([(0, 0), (0.18, 0.12), (0, 0.24)], (0.42, 1.72, DZ - 0.38), (0, 1, 0), (1, 0, 0), 0.03, DRAGON_DARK,
            back_cap=True)                                                                          # its spade
    o.tube(b, [(0, -0.3, DZ + 0.05), (0, -0.48, DZ + 0.25), (0, -0.6, DZ + 0.45)],
           [0.17, 0.13, 0.11], DRAGON, sides=7, tip=False)                                          # the neck


def dragon_leg(x, y, front):
    def build(b):
        k = 1 if x > 0 else -1
        b.ball(x, y, DZ - 0.12, 0.12, 0.25, DRAGON_DARK, sides=6)                                  # the haunch
        b.rod((x, y, DZ - 0.15), (x + k * 0.04, y + (0.06 if front else 0.1), 0.18), 0.075, DRAGON)
        b.rod((x + k * 0.04, y + (0.06 if front else 0.1), 0.18), (x + k * 0.04, y - 0.03, 0.03), 0.06, DRAGON_DARK)
        for c in (-1, 0, 1):                                                                        # claws
            b.pyramid(x + k * 0.04 + c * 0.04, y - 0.1, 0.02, 0.02, 0.02, 0.0, HORN, sides=3)
        b.box(x + k * 0.04 - 0.07, x + k * 0.04 + 0.07, y - 0.1, y + 0.02, 0.0, 0.05, DRAGON_DARK, bevel=0.02)
    return build


def dragon_wing(k):
    def build(b):
        root = Vector((k * 0.22, -0.05, DZ + 0.2))
        elbow = Vector((k * 0.75, 0.05, DZ + 0.75))
        tips = [Vector((k * 1.35, 0.2, DZ + 0.7)), Vector((k * 1.25, 0.55, DZ + 0.35)), Vector((k * 0.9, 0.7, DZ + 0.15)),
                Vector((k * 0.45, 0.55, DZ + 0.15))]
        b.rod(tuple(root), tuple(elbow), 0.05, DRAGON_DARK)
        b.pyramid(elbow.x, elbow.y, elbow.z, 0.03, 0.03, elbow.z + 0.12, HORN, sides=3)            # the thumb claw
        for t in tips[:3]:
            b.rod(tuple(elbow), tuple(t), 0.025, DRAGON_DARK)
        # the membrane: from the body to the elbow and the last finger, then between the fingers
        b.prism([(0, 0), (1, 0), (0, 1)], tuple(root), tuple(elbow - root), tuple(tips[3] - root), 0.015, WING,
                back_cap=True)
        for a, c in zip(tips[:3], tips[1:]):
            b.prism([(0, 0), (1, 0), (0, 1)], tuple(elbow), tuple(a - elbow), tuple(c - elbow), 0.015, WING, back_cap=True)
    return build


def dragon_head(b):
    hy, hz = -0.72, DZ + 0.52
    b.obox((0, hy, hz), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.15, 0.17, 0.13, DRAGON, bevel=0.06)
    b.obox((0, hy - 0.22, hz - 0.02), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.1, 0.14, 0.07, DRAGON, bevel=0.04)     # snout
    b.obox((0, hy - 0.2, hz - 0.12), (1, 0, 0), (0, 1, 0.25), (0, -0.25, 1), 0.085, 0.12, 0.03, DRAGON_BELLY,
           bevel=0.02)                                                                                         # the open jaw
    b.box(-0.06, 0.06, hy - 0.32, hy - 0.26, hz - 0.1, hz - 0.06, MAGMA)                                      # fire in its throat
    for k in (-1, 1):
        o.horn(b, (k * 0.09, hy + 0.08, hz + 0.1), (k * 0.3, 1, 0.2), 0.32, 0.045, color=HORN, up=0.8)        # horns
        b.box(k * 0.1 - 0.03, k * 0.1 + 0.03, hy - 0.13, hy - 0.1, hz + 0.04, hz + 0.08, MAGMA_HOT)            # eyes
        b.obox((k * 0.1, hy - 0.11, hz + 0.1), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.045, 0.04, 0.015, DRAGON_DARK)   # brows
        b.ball(k * 0.05, hy - 0.36, hz + 0.02, 0.015, 0.02, CRUST, sides=4)                                    # nostrils
        for i in range(3):                                                                                     # teeth
            b.pyramid(k * 0.075, hy - 0.3 + i * 0.06, hz - 0.06, 0.012, 0.012, hz - 0.11, 'white', sides=3)
        b.prism([(0, 0), (0.12, 0.05), (0.02, 0.1)], (k * 0.14, hy + 0.05, hz - 0.02), (k, 0.4, 0), (0, 0, 1), 0.012,
                DRAGON_DARK, back_cap=True)                                                                    # cheek frills


DRAGON_SPEC = ("MonsterDragon", dragon_body, [
    ("LegFL", dragon_leg(-0.24, -0.2, True), (-0.24, -0.2, DZ - 0.12)),
    ("LegFR", dragon_leg(0.24, -0.2, True), (0.24, -0.2, DZ - 0.12)),
    ("LegBL", dragon_leg(-0.26, 0.5, False), (-0.26, 0.5, DZ - 0.12)),
    ("LegBR", dragon_leg(0.26, 0.5, False), (0.26, 0.5, DZ - 0.12)),
    ("WingL", dragon_wing(-1), (-0.22, -0.05, DZ + 0.2)),
    ("WingR", dragon_wing(1), (0.22, -0.05, DZ + 0.2)),
    ("Head", dragon_head, (0.0, -0.6, DZ + 0.45)),
])


# ---------------------------------------------------------------- building

def main():
    coll = ensure_collection()
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    base = Vector((o.COLUMN_X + 10.0, 26.0, 0))
    x = 0.0
    for spec in (FIRE_IMP, LAVA_HOUND, SALAMANDER, OBSIDIAN_GOLEM, EMBER_CULTIST, MAGMA_SPAWN, DRAGON_SPEC, LAVA_TITAN):
        m.figure(spec, base + Vector((x, 0, 0)), mat, coll)
        x += 3.5
    kit.file_display_settings()
    bpy.ops.wm.save_mainfile()


if __name__ == "__main__":
    main()
