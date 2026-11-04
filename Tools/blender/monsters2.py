"""The level-2 dungeon's monsters, «Затопленные гномьи чертоги» (the user's roster, 2026-11-04).
Facing -y, standing on the origin, at GAME size (one unit = one map cell; the King is 0.53 tall),
jointed like monsters.py (LegL/LegR or LegFL..BR, Head, ArmStrike/ArmOff). Run in the models file:

    blender --background Buildings-Size1-Models.blend --python monsters2.py

The known ones made stronger (same joints as their level-1 kin, a new look):
- MonsterDrownedRat: the rat, soaked -- dark wet fur, weed on its back, a barnacle, cyan eyes.
- MonsterDwarfSkeleton: a dwarf's skeleton in rusted armour -- horned helm, a ghostly beard, an axe and
  a round shield (the armour holds to half health in the game).
- MonsterStoneGuard: the dwarf king's raised guard -- dark carved stone with a stone beard, gold
  bands, cyan runes and eyes, moss and weed (the gnomes' own guardian is light stone: never confused).
New:
- MonsterDrownedDwarf: a gnome risen from the flooded passages -- grey-blue skin, a rusted helmet,
  weed in the beard and on the shoulders, arms reaching ahead.
- MonsterEel: lives only in the water -- an arched body rising from the surface ring, a dark blue back
  with cyan stripes, a fin crest, jaws open. Its origin is the WATER SURFACE.
- MonsterForgeSpirit: a living flame in a floating iron brazier -- coals round its rim, a fireball in
  one hand. Hovers (no legs).
Bosses (much bigger than a hero):
- MonsterDwarfKing: the cursed dwarf king, ~1.7 tall -- rusted plate with gold trim, a violet cape,
  a long grey beard braided with gold, a spiked crown with cyan gems, a great warhammer.
- MonsterCanalSerpent: the serpent of the main canal -- a neck rising 1.5 out of the water, two coils
  arching behind it and a tail fin, ~3.6 long; green with a sand belly and cyan fins. Origin = the
  WATER SURFACE; only the Head is jointed.
"""
import math
import os
import sys
from contextlib import contextmanager

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import detail  # noqa: E402
import gnomes as g  # noqa: E402
import kit  # noqa: E402
import monsters as m  # noqa: E402
import orcs as o  # noqa: E402
from mathutils import Vector  # noqa: E402

COLLECTION = "Подземелье-2-монстры"

WET_FUR, WET_FUR_DARK = 'roof_slate_dark', 'iron'
WEED, WEED_DARK = 'leaf', 'grass_dark'
CURSE = 'water'                       # the drowned curse: cyan eyes, runes, gems
DROWNED_SKIN, DROWNED_SKIN_DARK = 'roof_slate', 'roof_slate_dark'
RUST, RUST_DARK = 'roof_orange_dark', 'wood_dark'
BONE = 'plaster'
GHOST_BEARD = 'stone_light'
ARMOUR, ARMOUR_DARK = 'iron', 'roof_slate_dark'
GOLD = 'gold'
CAPE = 'roof_purple_dark'
GUARD_STONE, GUARD_DARK = 'roof_slate', 'roof_slate_dark'
EEL_BACK, EEL_BELLY, EEL_FIN = 'roof_blue_dark', 'sand', 'roof_orange'
FLAME, FLAME_CORE, FLAME_HOT = 'fire', 'plaster_yellow', 'roof_red'
SERPENT, SERPENT_DARK, SERPENT_BELLY, SERPENT_FIN = 'roof_green', 'grass_dark', 'sand', 'water'
FOAM = 'white'
TIMBER = 'wood_mid'

detail.OVERRIDE.update({c: detail.PLAIN for c in (
    WET_FUR, WET_FUR_DARK, WEED, WEED_DARK, CURSE, DROWNED_SKIN, DROWNED_SKIN_DARK, RUST, RUST_DARK, BONE,
    GHOST_BEARD, ARMOUR, ARMOUR_DARK, GOLD, CAPE, EEL_BACK, EEL_BELLY, EEL_FIN, FLAME, FLAME_CORE, FLAME_HOT,
    SERPENT, SERPENT_DARK, SERPENT_BELLY, SERPENT_FIN, FOAM, TIMBER, 'plaster_pink', 'white', 'window')})


@contextmanager
def colours(module, **swap):
    """Builds a level-1 part in new colours: the module's colour names swapped for the call."""
    old = {k: getattr(module, k) for k in swap}
    for k, v in swap.items():
        setattr(module, k, v)
    try:
        yield
    finally:
        for k, v in old.items():
            setattr(module, k, v)


def verts(b):
    b.bm.verts.ensure_lookup_table()
    return len(b.bm.verts)


def scaled(fn, s):
    """A part built at its own size and grown by s about the origin (the king is a gnome grown)."""
    def build(b):
        since = verts(b)
        fn(b)
        o.scale_since(b, since, s)
    return build


def fin(b, pts, origin, u, v, depth, color):
    b.prism(pts, origin, u, v, depth, color, back_cap=True)


def disc(b, x, y, z, r, color, sides=8, u=(1, 0, 0), v=(0, 0, 1), depth=0.01):
    """A flat round plate (a shield, a gem) in the (u, v) plane, centred at (x, y, z)."""
    u, v = Vector(u), Vector(v)
    n = u.cross(v).normalized()
    pts = [(r * math.cos(2 * math.pi * i / sides), r * math.sin(2 * math.pi * i / sides)) for i in range(sides)]
    b.prism(pts, tuple(Vector((x, y, z)) - n * depth / 2), tuple(u), tuple(v), depth, color, back_cap=True)


def ripple(b, x, y, r, sides=10):
    """A foam ring on the water where a body breaks the surface."""
    b.ring(x, y, 0.0, 0.012, r, r + 0.035 + r * 0.15, [FOAM], sides=sides)


def weed_strand(b, p, drop, r=0.008, color=WEED):
    x, y, z = p
    o.tube(b, [(x, y, z), (x + 0.004, y + 0.003, z - drop * 0.5), (x - 0.003, y + 0.006, z - drop)],
           [r, r * 0.8, 0.0], color, sides=4)


def ensure_collection():
    coll = bpy.data.collections.get(COLLECTION)
    if coll is None:
        coll = bpy.data.collections.new(COLLECTION)
        bpy.context.scene.collection.children.link(coll)
    return COLLECTION


# ---------------------------------------------------------------- the drowned rat (the rat's joints)

RAT_COLOURS = dict(FUR=WET_FUR, FUR_DARK=WET_FUR_DARK, FUR_LIGHT=DROWNED_SKIN, EYE=CURSE)


def drowned_rat_body(b):
    with colours(m, **RAT_COLOURS):
        m.rat_body(b)
    # weed draped over the back and trailing, a barnacle on the haunch, drips under the belly
    b.obox((0.0, 0.01, 0.088), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.022, 0.05, 0.004, WEED, bevel=0.003)
    for k in (-1, 1):
        weed_strand(b, (k * 0.033, 0.0, 0.075), 0.04, r=0.006)
        weed_strand(b, (k * 0.03, 0.04, 0.072), 0.035, r=0.005, color=WEED_DARK)
    b.pyramid(0.025, 0.06, 0.085, 0.01, 0.01, 0.1, 'stone_light', sides=5)


def drowned_rat_head(b):
    with colours(m, **RAT_COLOURS):
        m.rat_head(b)
    weed_strand(b, (0.012, -0.07, 0.088), 0.03, r=0.005)


def drowned_rat_leg(x, y):
    def build(b):
        with colours(m, **RAT_COLOURS):
            m.rat_leg(b, x, y)
    return build


DROWNED_RAT = ("MonsterDrownedRat", drowned_rat_body, [
    ("LegFL", drowned_rat_leg(-0.024, -0.025), (-0.024, -0.025, 0.045)),
    ("LegFR", drowned_rat_leg(0.024, -0.025), (0.024, -0.025, 0.045)),
    ("LegBL", drowned_rat_leg(-0.026, 0.05), (-0.026, 0.05, 0.045)),
    ("LegBR", drowned_rat_leg(0.026, 0.05), (0.026, 0.05, 0.045)),
    ("Head", drowned_rat_head, (0.0, -0.045, 0.065)),
])


# ---------------------------------------------------------------- the dwarf skeleton (a gnome's height, 0.42)

def dskel_body(b):
    b.box(-0.05, 0.05, -0.025, 0.025, 0.12, 0.145, BONE, bevel=0.006)                              # pelvis
    b.frustum(0, 0.0, 0.1, 0.16, 0.07, 0.066, RUST_DARK, sides=7)                                  # a mail skirt
    b.rod((0, 0.012, 0.14), (0, 0.012, 0.24), 0.018, BONE)                                         # spine
    for i, z in enumerate((0.17, 0.195)):                                                           # ribs under it
        b.obox((0, -0.004, z), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.06 - i * 0.004, 0.032, 0.008, BONE, bevel=0.004)
    # a rusted breastplate over the upper ribs, gold-rimmed, a rune of the curse on it
    b.obox((0, -0.01, 0.235), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.07, 0.042, 0.03, RUST, bevel=0.012)
    b.obox((0, -0.05, 0.236), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.06, 0.004, 0.004, GOLD)
    b.obox((0, -0.054, 0.222), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.012, 0.004, 0.012, CURSE)
    for k in (-1, 1):                                                                               # pauldrons
        b.ball(k * 0.065, 0.0, 0.235, 0.034, 0.04, ARMOUR, sides=6)
    weed_strand(b, (-0.05, -0.04, 0.25), 0.07)


def dskel_leg(b, k):
    x = k * 0.034
    b.rod((x, 0, 0.13), (x, 0.0, 0.07), 0.015, BONE)
    b.rod((x, 0, 0.07), (x, -0.004, 0.04), 0.013, BONE)
    b.box(x - 0.028, x + 0.028, -0.05, 0.022, 0.0, 0.046, ARMOUR, bevel=0.008)                    # iron boots


def dskel_head(b):
    o.skull(b, 0, -0.005, 0.255, s=0.085)
    # the horned iron helm, a nasal, the beard that outlived the face
    b.ball(0, 0.0, 0.3, 0.056, 0.05, ARMOUR, sides=8)
    b.cylinder(0, 0.0, 0.296, 0.308, 0.06, RUST, sides=8)
    b.box(-0.006, 0.006, -0.062, -0.054, 0.27, 0.31, ARMOUR)
    for k in (-1, 1):
        o.horn(b, (k * 0.05, 0.0, 0.325), (k, 0.0, 0.0), 0.06, 0.012, color=BONE, up=1.4)
        b.box(k * 0.017 - 0.007, k * 0.017 + 0.007, -0.05, -0.044, 0.268, 0.278, CURSE)          # eye lights
    o.tube(b, [(0, -0.045, 0.255), (0, -0.06, 0.21), (0, -0.065, 0.17), (0, -0.058, 0.13)],
           [0.04, 0.036, 0.024, 0.0], GHOST_BEARD, sides=6)


def dskel_axe_arm(b):
    b.rod((0.07, 0.0, 0.235), (0.085, -0.02, 0.17), 0.013, BONE)
    b.rod((0.085, -0.02, 0.17), (0.085, -0.06, 0.14), 0.012, BONE)
    # the axe: a haft up past the shoulder, a bearded blade on it
    b.rod((0.085, -0.07, 0.1), (0.085, -0.05, 0.34), 0.012, TIMBER)
    fin(b, [(0, 0), (0.07, -0.03), (0.08, 0.04), (0.03, 0.06), (0, 0.04)], (0.081, -0.05, 0.28), (0, -1, 0),
        (0, 0, 1), 0.008, ARMOUR)


def dskel_shield_arm(b):
    b.rod((-0.07, 0.0, 0.235), (-0.085, -0.01, 0.17), 0.013, BONE)
    b.rod((-0.085, -0.01, 0.17), (-0.08, -0.04, 0.13), 0.012, BONE)
    # a round shield held before it: dark wood, a rusted rim and boss, a gold rune
    disc(b, -0.08, -0.07, 0.16, 0.075, RUST_DARK, sides=10, depth=0.012)
    disc(b, -0.08, -0.078, 0.16, 0.028, ARMOUR, sides=8, depth=0.012)
    disc(b, -0.08, -0.085, 0.16, 0.012, GOLD, sides=6, depth=0.008)


DWARF_SKELETON = ("MonsterDwarfSkeleton", dskel_body, [
    ("LegL", lambda b: dskel_leg(b, -1), (-0.034, 0.0, 0.13)),
    ("LegR", lambda b: dskel_leg(b, 1), (0.034, 0.0, 0.13)),
    ("Head", dskel_head, (0.0, 0.0, 0.25)),
    ("ArmStrike", dskel_axe_arm, (0.07, 0.0, 0.235)),
    ("ArmOff", dskel_shield_arm, (-0.07, 0.0, 0.235)),
])


# ---------------------------------------------------------------- the stone guard (the small golem's joints, 0.8)

GUARD_S = 1.2
_guard = m.golem("MonsterStoneGuard", GUARD_S, GUARD_STONE, GUARD_DARK, GOLD, CURSE, CURSE, CURSE)


def guard_body(b):
    _guard[1](b)
    s = GUARD_S
    # carved runes glowing on the shoulders' stones, weed hanging off the chest
    for k in (-1, 1):
        b.obox((k * 0.12 * s, -0.135 * s, 0.47 * s), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.02 * s, 0.006 * s,
               0.02 * s, CURSE)
        weed_strand(b, (k * 0.17 * s, -0.12 * s, 0.5 * s), 0.12 * s, r=0.012 * s)


def guard_head(b):
    s = GUARD_S
    m.golem_head(b, s, GUARD_STONE, GUARD_DARK, CURSE)
    # the dwarven face: a carved stone beard banded with gold, a flat stone helm
    o.tube(b, [(0, -0.115 * s, 0.6 * s), (0, -0.13 * s, 0.55 * s), (0, -0.13 * s, 0.49 * s), (0, -0.12 * s, 0.45 * s)],
           [0.07 * s, 0.065 * s, 0.045 * s, 0.0], GUARD_DARK, sides=6)
    for z in (0.53, 0.49):
        b.obox((0, -0.13 * s, z * s), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.05 * s * (1.2 - (0.53 - z) * 6), 0.035 * s,
               0.008 * s, GOLD)
    b.box(-0.085 * s, 0.085 * s, -0.12 * s, 0.07 * s, 0.66 * s, 0.69 * s, GUARD_STONE, bevel=0.01 * s)


GUARD = ("MonsterStoneGuard", guard_body,
         [p if p[0] != "Head" else ("Head", guard_head, p[2]) for p in _guard[2]])


# ---------------------------------------------------------------- the drowned dwarf (a gnome's joints, 0.42)

DROWNED_GNOME = dict(SKIN=DROWNED_SKIN, SKIN_DARK=DROWNED_SKIN_DARK, EYE=CURSE, LEATHER=RUST_DARK, BRASS=RUST)


def ddwarf_body(b):
    with colours(g, **DROWNED_GNOME):
        g.gnome_body(b, 'roof_blue_dark', apron=RUST_DARK)
    for k in (-1, 1):                                                                               # weed on the shoulders
        b.obox((k * 0.06, 0.0, 0.272), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.026, 0.03, 0.005, WEED, bevel=0.003)
        weed_strand(b, (k * 0.075, -0.01, 0.265), 0.09, r=0.007)
        weed_strand(b, (k * 0.05, 0.03, 0.26), 0.07, r=0.006, color=WEED_DARK)
    b.pyramid(0.03, -0.075, 0.2, 0.012, 0.01, 0.215, 'stone_light', sides=5)                      # a barnacle


def ddwarf_head(b):
    with colours(g, **DROWNED_GNOME):
        g.gnome_face(b, 'stone_mid')
    b.ball(0, 0.0, 0.335, 0.07, 0.06, RUST, sides=8)                                               # the rusted helmet
    b.cylinder(0, 0.0, 0.33, 0.342, 0.08, RUST_DARK, sides=8)
    b.box(-0.018, 0.018, -0.078, -0.064, 0.35, 0.378, ARMOUR)                                       # a dead lamp
    weed_strand(b, (0.03, -0.072, 0.24), 0.08, r=0.007)                                            # weed in the beard
    weed_strand(b, (-0.05, -0.02, 0.37), 0.1, r=0.008)                                             # and off the helmet


def ddwarf_arm(k):
    def build(b):
        g.ARM_COLOUR[0] = 'roof_blue_dark'
        with colours(g, **DROWNED_GNOME):
            g.gnome_arm(b, k, (k * 0.06, -0.15, 0.24))                                            # reaching ahead
        g.ARM_COLOUR[0] = g.JERKIN
    return build


def ddwarf_leg(k):
    def build(b):
        with colours(g, TROUSERS='roof_blue_dark', BOOT=RUST_DARK):
            g.gnome_leg(b, k)
    return build


DROWNED_DWARF = ("MonsterDrownedDwarf", ddwarf_body, [
    ("LegL", ddwarf_leg(-1), (-0.036, 0.0, 0.13)),
    ("LegR", ddwarf_leg(1), (0.036, 0.0, 0.13)),
    ("Head", ddwarf_head, (0.0, 0.0, 0.25)),
    ("ArmStrike", ddwarf_arm(1), (0.075, 0.0, 0.245)),
    ("ArmOff", ddwarf_arm(-1), (-0.075, 0.0, 0.245)),
])


# ---------------------------------------------------------------- the eel (origin = the water surface)

def eel_path(steps=14):
    """The body: out of the water behind, arching up and forward to the neck."""
    pts = []
    for i in range(steps + 1):
        t = i / steps
        y = 0.22 - 0.36 * t
        z = -0.02 + 0.3 * math.sin(t * math.pi * 0.62)
        x = 0.025 * math.sin(t * math.pi * 2)
        pts.append(Vector((x, y, z)))
    return pts


def eel_body(b):
    pts = eel_path()
    radii = [0.022 + 0.012 * math.sin(i / (len(pts) - 1) * math.pi * 0.9) for i in range(len(pts))]
    o.tube(b, [tuple(p) for p in pts], radii, EEL_BACK, sides=7, tip=False)
    for i in range(1, len(pts) - 1, 2):                                                             # cyan stripes, belly
        p = pts[i]
        b.obox(tuple(p + Vector((0, 0, radii[i] * 0.85))), (1, 0, 0), (0, 1, 0), (0, 0, 1), radii[i] * 0.7, 0.008,
               0.004, CURSE)
        b.ball(p.x, p.y, p.z - radii[i] * 1.05, radii[i] * 0.55, radii[i] * 0.5, EEL_BELLY, sides=5)
    for i in range(2, len(pts) - 2, 2):                                                             # the fin crest
        p = pts[i]
        fin(b, [(0, 0), (0.035, 0.0), (0.01, 0.03)], tuple(p + Vector((0, 0, radii[i] * 0.8))), (0, -1, 0), (0, 0, 1),
            0.006, EEL_FIN)
    ripple(b, pts[0].x, pts[0].y + 0.01, 0.045)


def eel_head(b):
    p = eel_path()[-1]
    x, y, z = p.x, p.y - 0.03, p.z
    b.obox((x, y, z + 0.012), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.03, 0.045, 0.018, EEL_BACK, bevel=0.012)   # upper jaw
    b.obox((x, y - 0.01, z - 0.025), (1, 0, 0), (0, 1, 0.35), (0, -0.35, 1), 0.024, 0.04, 0.008, EEL_BELLY,
           bevel=0.006)                                                                                   # lower, open
    for i in range(3):
        for k in (-1, 1):
            b.pyramid(x + k * 0.018, y - 0.035 + i * 0.018, z - 0.004, 0.004, 0.004, z - 0.022, 'white', sides=3)
    for k in (-1, 1):
        b.box(x + k * 0.022 - 0.006, x + k * 0.022 + 0.006, y - 0.02, y - 0.008, z + 0.02, z + 0.032, 'plaster_yellow')
        fin(b, [(0, 0), (0.035, 0.012), (0.01, 0.03)], (x + k * 0.03, y + 0.02, z), (k, 0.5, 0), (0, 0, 1), 0.005,
            EEL_FIN)                                                                                       # gill fins


EEL_HEAD_AT = tuple(eel_path()[-1])
EEL = ("MonsterEel", eel_body, [("Head", eel_head, EEL_HEAD_AT)])


# ---------------------------------------------------------------- the forge spirit (hovers, 0.45 to the flame's tip)

HOVER = 0.1


def spirit_body(b):
    z = HOVER
    # the floating brazier: an iron bowl, a gold band, coals glowing on its rim, three short legs of chain
    b.frustum(0, 0, z, z + 0.06, 0.04, 0.085, ARMOUR, sides=8)
    b.ring(0, 0, z + 0.05, z + 0.065, 0.08, 0.092, [GOLD], sides=8)
    for i in range(7):
        a = i * 2 * math.pi / 7 + 0.2
        b.ball(math.cos(a) * 0.065, math.sin(a) * 0.065, z + 0.055, 0.022, 0.028,
               'window' if i % 2 else FLAME_HOT, sides=5, top=FLAME if i % 2 == 0 else None)
    for i in range(3):
        a = i * 2 * math.pi / 3 + 0.5
        b.rod((math.cos(a) * 0.05, math.sin(a) * 0.05, z + 0.01), (math.cos(a) * 0.03, math.sin(a) * 0.03, z - 0.05),
              0.006, ARMOUR)
    # the flame body: three tongues, red outside, orange, a yellow heart
    b.frustum(0, 0, z + 0.05, z + 0.2, 0.072, 0.06, FLAME_HOT, sides=7)
    b.frustum(0, 0, z + 0.2, z + 0.27, 0.06, 0.04, FLAME, sides=7)
    b.obox((0, -0.05, z + 0.15), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.035, 0.02, 0.055, FLAME_CORE, bevel=0.015, skip=())
    for k, h in ((-1, 0.17), (1, 0.14)):
        b.pyramid(k * 0.045, 0.02, z + 0.16, 0.025, 0.025, z + 0.16 + h, FLAME, sides=5)


def spirit_head(b):
    z = HOVER + 0.27
    b.ball(0, 0, z - 0.02, 0.05, 0.08, FLAME, sides=7, top=FLAME_CORE)
    b.pyramid(0, 0.01, z + 0.04, 0.03, 0.03, z + 0.17, FLAME_CORE, sides=5)                         # the flame's tip
    b.pyramid(-0.02, 0.02, z + 0.03, 0.02, 0.02, z + 0.12, FLAME, sides=5)
    for k in (-1, 1):                                                                               # coal-dark eyes
        b.box(k * 0.02 - 0.009, k * 0.02 + 0.009, -0.052, -0.044, z + 0.02, z + 0.034, 'window')
    b.box(-0.014, 0.014, -0.05, -0.044, z - 0.004, z + 0.004, 'window')                             # a grin


def spirit_arm(k, ball):
    def build(b):
        z = HOVER + 0.2
        o.tube(b, [(k * 0.06, 0.0, z), (k * 0.1, -0.03, z - 0.02), (k * 0.12, -0.07, z + 0.01)],
               [0.022, 0.016, 0.01], FLAME, sides=5)
        if ball:                                                                                    # the fireball
            b.ball(k * 0.125, -0.085, z - 0.005, 0.03, 0.06, FLAME_CORE, sides=6, top=FLAME)
            b.pyramid(k * 0.125, -0.08, z + 0.045, 0.016, 0.016, z + 0.09, FLAME, sides=4)
    return build


FORGE_SPIRIT = ("MonsterForgeSpirit", spirit_body, [
    ("Head", spirit_head, (0.0, 0.0, HOVER + 0.25)),
    ("ArmStrike", spirit_arm(1, True), (0.06, 0.0, HOVER + 0.2)),
    ("ArmOff", spirit_arm(-1, False), (-0.06, 0.0, HOVER + 0.2)),
])


# ---------------------------------------------------------------- the cursed dwarf king (a gnome grown x4.2)

KING_S = 4.2


def king_body_unit(b):
    b.frustum(0, 0.0, 0.12, 0.27, 0.082, 0.064, ARMOUR, sides=8)                                   # plate over the barrel
    b.cylinder(0, 0.0, 0.15, 0.175, 0.085, GOLD, sides=8)                                          # a gold belt
    b.obox((0, -0.084, 0.162), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.022, 0.006, 0.02, CURSE)          # its cursed gem
    b.obox((0, -0.07, 0.22), (1, 0, 0), (0, 0.2, 0.98), (0, -0.98, 0.2), 0.05, 0.04, 0.006, RUST)   # a rusted breastplate
    b.obox((0, -0.077, 0.22), (1, 0, 0), (0, 0.2, 0.98), (0, -0.98, 0.2), 0.012, 0.03, 0.004, GOLD)
    b.frustum(0, 0.0, 0.08, 0.13, 0.09, 0.082, RUST_DARK, sides=8)                                  # a mail skirt
    # the violet cape from the shoulders to the floor, behind
    b.prism([(-0.09, 0.0), (0.09, 0.0), (0.07, 0.26), (-0.07, 0.26)], (0, 0.075, 0.005), (1, 0, 0), (0, 0.12, 1),
            0.012, CAPE, back_cap=True)
    b.obox((0, 0.072, 0.26), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.085, 0.012, 0.012, GOLD)            # the cape's clasp bar
    for k in (-1, 1):                                                                               # great pauldrons
        b.ball(k * 0.065, 0.0, 0.235, 0.04, 0.045, ARMOUR, sides=7, top=RUST)
        b.pyramid(k * 0.075, 0.0, 0.27, 0.008, 0.008, 0.3, GOLD, sides=4)
    weed_strand(b, (0.06, -0.06, 0.21), 0.08, r=0.006)


def king_leg_unit(k):
    def build(b):
        x = k * 0.04
        b.rod((x, 0, 0.13), (x, 0, 0.045), 0.026, ARMOUR_DARK)
        b.box(x - 0.032, x + 0.032, -0.058, 0.025, 0.0, 0.05, ARMOUR, bevel=0.008)
        b.box(x - 0.033, x + 0.033, -0.06, -0.05, 0.03, 0.05, GOLD)
    return build


def king_head_unit(b):
    with colours(g, SKIN=DROWNED_SKIN, SKIN_DARK=DROWNED_SKIN_DARK, EYE=CURSE):
        g.gnome_face(b, GHOST_BEARD)
    # the long beard braided with gold rings, down to the knees
    o.tube(b, [(0, -0.07, 0.15), (0, -0.07, 0.11), (0, -0.065, 0.08)], [0.02, 0.016, 0.0], GHOST_BEARD, sides=5)
    for z in (0.18, 0.13):
        b.cylinder(0, -0.072, z - 0.007, z + 0.007, 0.026 if z > 0.15 else 0.019, GOLD, sides=6)
    # the crown: a gold band, five spikes, cyan gems
    b.ring(0, 0.0, 0.33, 0.36, 0.055, 0.066, [GOLD], sides=10)
    for i in range(5):
        a = math.radians(-90 + (i - 2) * 36)
        x, y = math.cos(a) * 0.06, math.sin(a) * 0.06
        b.pyramid(x, y, 0.36, 0.012, 0.012, 0.4 + (0.02 if i == 2 else 0), GOLD, sides=4)
        if i % 2 == 0:
            b.box(x - 0.008, x + 0.008, y - 0.006, y, 0.338, 0.354, CURSE)
    b.ball(0, 0.0, 0.33, 0.052, 0.03, CAPE, sides=8)                                                # velvet crown cap


def king_hammer_arm_unit(b):
    g.ARM_COLOUR[0] = ARMOUR
    with colours(g, SKIN=ARMOUR_DARK):
        g.gnome_arm(b, 1, (0.095, -0.06, 0.18))
    g.ARM_COLOUR[0] = g.JERKIN
    # the warhammer held up beside the head: a long haft, an iron head banded with gold, a spike
    b.rod((0.095, -0.07, 0.08), (0.125, 0.0, 0.47), 0.012, TIMBER)
    b.obox((0.128, 0.006, 0.5), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.028, 0.05, 0.028, ARMOUR, bevel=0.008, skip=())
    for yy in (-0.03, 0.04):
        b.obox((0.128, yy, 0.5), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.03, 0.007, 0.03, GOLD, skip=())
    b.pyramid(0.128, 0.006, 0.528, 0.01, 0.01, 0.56, ARMOUR, sides=4)
    b.obox((0.128, -0.046, 0.5), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.016, 0.004, 0.016, CURSE, skip=())


def king_off_arm_unit(b):
    g.ARM_COLOUR[0] = ARMOUR
    with colours(g, SKIN=ARMOUR_DARK):
        g.gnome_arm(b, -1, (-0.1, -0.03, 0.17))                                                     # a gauntlet clenched
    g.ARM_COLOUR[0] = g.JERKIN
    b.ball(-0.1, -0.03, 0.145, 0.026, 0.04, ARMOUR, sides=6, top=GOLD)


def king_spec():
    s = KING_S
    return ("MonsterDwarfKing", scaled(king_body_unit, s), [
        ("LegL", scaled(king_leg_unit(-1), s), (-0.04 * s, 0.0, 0.13 * s)),
        ("LegR", scaled(king_leg_unit(1), s), (0.04 * s, 0.0, 0.13 * s)),
        ("Head", scaled(king_head_unit, s), (0.0, 0.0, 0.25 * s)),
        ("ArmStrike", scaled(king_hammer_arm_unit, s), (0.075 * s, 0.0, 0.245 * s)),
        ("ArmOff", scaled(king_off_arm_unit, s), (-0.075 * s, 0.0, 0.245 * s)),
    ])


DWARF_KING = king_spec()


# ---------------------------------------------------------------- the canal serpent (origin = the water surface)

NECK = [Vector((0.0, -0.1, -0.1)), Vector((0.0, -0.25, 0.5)), Vector((0.0, -0.45, 1.1)), Vector((0.0, -0.6, 1.45))]


def arch(b, y0, y1, h, r, color=SERPENT):
    """A coil of the body arching out of the water between y0 and y1, its foam rings."""
    pts, radii = [], []
    for i in range(11):
        t = i / 10
        pts.append((0.06 * math.sin(t * math.pi * 2), y0 + (y1 - y0) * t, -0.08 + (h + 0.08) * math.sin(t * math.pi)))
        radii.append(r * (0.85 + 0.15 * math.sin(t * math.pi)))
    o.tube(b, pts, radii, color, sides=8, tip=False)
    for i in range(2, 9, 2):                                                                        # the crest, the belly
        x, y, z = pts[i]
        fin(b, [(0, 0), (0.12, 0.0), (0.03, 0.11)], (x, y, z + radii[i] * 0.85), (0, -1, 0), (0, 0, 1), 0.012,
            SERPENT_FIN)
    ripple(b, pts[0][0], y0, r * 1.1)
    ripple(b, pts[-1][0], y1, r * 1.1)


def serpent_body(b):
    o.tube(b, [tuple(p) for p in NECK], [0.17, 0.15, 0.13, 0.12], SERPENT, sides=9, tip=False)
    for seg in range(3):                                                                            # belly plates, crest
        for f in (0.3, 0.75):
            p = NECK[seg].lerp(NECK[seg + 1], f)
            if p.z < 0.05:
                continue
            b.obox(tuple(p + Vector((0, -0.12, 0))), (1, 0, 0), (0, 0.4, 0.9), (0, -0.9, 0.4), 0.08, 0.06, 0.02,
                   SERPENT_BELLY, bevel=0.015)
            fin(b, [(0, 0), (0.16, 0.0), (0.03, 0.15)], tuple(p + Vector((0, 0.12, 0.03))), (0, 1, 0.4), (0, -0.3, 1),
                0.014, SERPENT_FIN)
    ripple(b, 0.0, -0.1, 0.2, sides=12)
    arch(b, 0.35, 1.3, 0.45, 0.14)
    arch(b, 1.6, 2.4, 0.32, 0.11)
    # the tail fin standing up out of the water at the end
    fin(b, [(0, 0), (0.18, 0.05), (0.32, 0.28), (0.12, 0.12), (0.05, 0.3)], (0.0, 2.7, -0.02), (0, 1, 0), (0, 0, 1),
        0.016, SERPENT_FIN)
    o.tube(b, [(0, 2.55, -0.05), (0, 2.7, 0.06)], [0.07, 0.03], SERPENT_DARK, sides=6, tip=False)
    ripple(b, 0.0, 2.62, 0.07)


def serpent_head(b):
    hx, hy, hz = NECK[-1]
    b.obox((0, hy - 0.12, hz + 0.08), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.13, 0.19, 0.08, SERPENT, bevel=0.05)  # upper jaw
    b.obox((0, hy - 0.16, hz - 0.06), (1, 0, 0), (0, 1, 0.3), (0, -0.3, 1), 0.1, 0.16, 0.035, SERPENT_BELLY,
           bevel=0.03)                                                                                        # lower, open
    for i in range(4):
        for k in (-1, 1):
            b.pyramid(k * 0.08, hy - 0.26 + i * 0.06, hz + 0.01, 0.01, 0.01, hz - 0.05, 'white', sides=3)
    for k in (-1, 1):
        b.ball(k * 0.1, hy - 0.06, hz + 0.12, 0.035, 0.045, CURSE, sides=5)                         # eyes
        b.obox((k * 0.1, hy - 0.06, hz + 0.175), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.04, 0.03, 0.012, SERPENT_DARK)  # brows
        fin(b, [(0, 0), (0.2, 0.08), (0.05, 0.16)], (k * 0.12, hy + 0.02, hz + 0.05), (k, 0.5, 0), (0, 0, 1), 0.012,
            SERPENT_FIN)                                                                                       # gill frills
        o.tube(b, [(k * 0.02, hy - 0.3, hz + 0.02), (k * 0.06, hy - 0.36, hz - 0.08), (k * 0.07, hy - 0.34, hz - 0.2)],
               [0.012, 0.009, 0.0], SERPENT_DARK, sides=4)                                         # whiskers
    for i in range(3):                                                                              # a crest of spines
        b.pyramid(0, hy + 0.02 + i * 0.07, hz + 0.14, 0.02, 0.03, hz + 0.3 - i * 0.04, SERPENT_FIN, sides=4)


CANAL_SERPENT = ("MonsterCanalSerpent", serpent_body, [("Head", serpent_head, tuple(NECK[-1]))])


# ---------------------------------------------------------------- building

def main():
    coll = ensure_collection()
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    base = Vector((o.COLUMN_X + 10.0, 20.0, 0))
    x = 0.0
    for spec in (DROWNED_RAT, DWARF_SKELETON, GUARD, DROWNED_DWARF, EEL, FORGE_SPIRIT, DWARF_KING, CANAL_SERPENT):
        m.figure(spec, base + Vector((x, 0, 0)), mat, coll)
        x += 3.0
    kit.file_display_settings()
    bpy.ops.wm.save_mainfile()


if __name__ == "__main__":
    main()
