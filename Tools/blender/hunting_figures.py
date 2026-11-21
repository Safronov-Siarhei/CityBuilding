"""The hunt's two figures (the user, 2026-11-21, card hunting.md: «у охотника должна быть своя модель»), as pure
builders, jointed like the soldiers (soldiers.py) and the wildlife (fauna.py) so FigureAnimator moves them:

- FigureHunter: a resident from the Hunter's Hut -- NOT a soldier: no kingdom blue, no gold. A brown felt hat with a
  white feather, a fur cape over the shoulders, a leather jerkin over an olive tunic, a knife on the belt, a quiver of
  white-fletched arrows on the back, a dark yew bow. Parts as the archer's: Torso, LegL/LegR, Head, ArmDraw (right,
  draws and looses), ArmBow (left) -- the archer's rig, so the archer's shot animation fits as it is.
- FaunaBoar: a wild boar at the deer's scale (0.5 long, 0.3 at the withers): dark bristly body, a black crest along
  the spine, a long pink-tipped snout, white tusks. Parts LegFL/LegFR/LegBL/LegBR, Head (it lowers to charge).

The deer and the hare already exist (fauna.py). Not installed into the models file until the user approves:
render with render_hunting.py.
"""
import math

import orcs as o
from kit import Builder
from mathutils import Vector

SKIN, SPOT = 'plaster_pink', 'window'
LEATHER, WOOD, WOOD_LIGHT = 'wood_dark', 'wood_mid', 'wood_light'
FUR, FUR_DARK = 'burlap', 'wood_mid'
TUNIC, HOSE = 'wood_light', 'leaf'
HAT = 'wood_dark'
FEATHER = 'white'
STEEL = 'stone_light'


# ------------------------------------------------------------------------------------------------ the hunter

def face(b, z, hair=WOOD):
    """The soldiers' plain face (soldiers.face) with a short beard: the hunter is older than the archer."""
    b.cylinder(0, 0.0, z - 0.03, z, 0.026, SKIN, sides=6)
    b.box(-0.042, 0.042, -0.044, 0.04, z - 0.005, z + 0.085, SKIN, bevel=0.016)
    b.box(-0.007, 0.007, -0.055, -0.042, z + 0.025, z + 0.047, SKIN, bevel=0.003)
    for k in (-1, 1):
        b.box(k * 0.019 - 0.007, k * 0.019 + 0.007, -0.047, -0.041, z + 0.05, z + 0.059, SPOT)
        b.box(k * 0.019 - 0.01, k * 0.019 + 0.01, -0.048, -0.041, z + 0.063, z + 0.068, hair)
    b.box(-0.04, 0.04, -0.05, -0.03, z - 0.008, z + 0.028, hair, bevel=0.008)          # the beard
    b.box(-0.044, 0.044, -0.02, 0.042, z + 0.01, z + 0.07, hair, bevel=0.012)         # hair at the back


def boots(b, k):
    x = k * 0.045
    b.box(x - 0.032, x + 0.032, -0.055, 0.03, 0.0, 0.028, LEATHER, bevel=0.009)
    b.box(x - 0.03, x + 0.03, -0.03, 0.03, 0.028, 0.12, LEATHER, bevel=0.009)
    b.box(x - 0.033, x + 0.033, -0.033, 0.033, 0.1, 0.12, FUR, bevel=0.006)          # fur turn-down
    b.box(x - 0.025, x + 0.025, -0.025, 0.025, 0.12, 0.19, HOSE, bevel=0.009)


def hunter_body(b):
    """An olive tunic, a leather jerkin, a belt with a knife, a fur cape on the shoulders, the quiver on the back."""
    b.frustum(0, 0, 0.14, 0.28, 0.088, 0.077, TUNIC, sides=8)
    b.cylinder(0, 0, 0.27, 0.29, 0.081, LEATHER, sides=8)
    b.box(-0.013, 0.013, -0.09, -0.078, 0.266, 0.294, STEEL)                          # buckle
    b.rod((0.05, -0.08, 0.27), (0.07, -0.075, 0.2), 0.018, WOOD)                       # knife hilt and sheath
    b.rod((0.07, -0.075, 0.2), (0.08, -0.07, 0.15), 0.022, LEATHER)
    b.obox((0, 0, 0.34), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.08, 0.056, 0.055, WOOD, bevel=0.02)
    for k in (-1, 1):                                                                  # the jerkin's laced front
        b.box(k * 0.02 - 0.004, k * 0.02 + 0.004, -0.063, -0.054, 0.29, 0.38, LEATHER)
    # the fur cape: a ring of tufts over the shoulders and down the back
    b.obox((0, 0.01, 0.395), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.1, 0.066, 0.022, FUR, bevel=0.016)
    b.obox((0, 0.055, 0.3), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.082, 0.012, 0.09, FUR_DARK, bevel=0.01)
    for i in range(7):
        a = math.pi * (i / 6)
        b.ball(math.cos(a) * 0.092, -0.01 + math.sin(a) * 0.05, 0.38, 0.022, 0.035, FUR, sides=5)
    # the quiver on the back, leaning to the right shoulder; white fletching
    base, top = Vector((-0.04, 0.08, 0.2)), Vector((0.05, 0.09, 0.43))
    b.rod(base, top, 0.045, LEATHER)
    b.rod(base + Vector((0, 0, 0.02)), base + Vector((0.004, 0.0, 0.035)), 0.05, FUR)
    for dx, dy in ((-0.012, -0.006), (0.01, 0.006), (0.0, -0.01), (0.014, -0.004)):
        tip = top + Vector((dx, dy, 0.07))
        b.rod(top + Vector((dx, dy, 0.0)), tip, 0.006, WOOD_LIGHT)
        b.box(tip.x - 0.008, tip.x + 0.008, tip.y - 0.003, tip.y + 0.003, tip.z - 0.03, tip.z, FEATHER)
    b.rod((0.07, -0.06, 0.4), (-0.06, -0.062, 0.27), 0.012, LEATHER)                   # the strap


def hunter_head(b):
    """A bearded face under a brown felt hat: a round crown, a wide brim turned up at one side, a white feather."""
    face(b, 0.42)
    b.frustum(0, 0.0, 0.49, 0.56, 0.06, 0.048, HAT, sides=8)
    b.cylinder(0, 0.0, 0.48, 0.5, 0.095, HAT, sides=10)
    b.cylinder(0, 0.0, 0.5, 0.515, 0.062, FUR, sides=8)                                # the band
    b.obox((0.075, 0.0, 0.51), (math.cos(0.5), 0, math.sin(0.5)), (0, 1, 0), (-math.sin(0.5), 0, math.cos(0.5)),
           0.03, 0.06, 0.008, HAT)                                                     # the brim turned up
    o.tube(b, [(0.05, 0.02, 0.52), (0.09, 0.06, 0.6), (0.1, 0.11, 0.64)], [0.014, 0.012, 0.0], FEATHER, sides=4)


def hunter_bow_arm(b):
    """The left arm held out, a dark yew bow canted out at the top (the archer's, darker and plainer)."""
    sx = -0.095
    fist = Vector((-0.06, -0.2, 0.36))
    b.rod((sx, 0.0, 0.385), (-0.085, -0.1, 0.375), 0.04, TUNIC)
    b.rod((-0.085, -0.1, 0.375), fist + Vector((0, 0.02, 0)), 0.036, LEATHER)
    b.ball(fist.x, fist.y, fist.z - 0.02, 0.022, 0.04, SKIN, sides=6)
    cant = math.radians(30)

    def at(dz, dy):
        return fist + Vector((-math.sin(cant) * dz, dy, math.cos(cant) * dz))

    pts = [(-0.25, 0.0), (-0.17, -0.04), (-0.06, -0.06), (0.06, -0.06), (0.17, -0.04), (0.25, 0.0)]
    prev = None
    for dz, dy in pts:
        p = at(dz, dy)
        if prev is not None:
            b.rod(prev, p, 0.022, WOOD)
        prev = p
    b.box(fist.x - 0.014, fist.x + 0.014, fist.y - 0.072, fist.y - 0.05, fist.z - 0.03, fist.z + 0.03, FUR)
    b.rod(at(-0.245, 0.0), at(0.245, 0.0), 0.005, 'white')


def hunter_draw_arm(b):
    b.rod((0.095, 0.0, 0.385), (0.12, -0.02, 0.4), 0.04, TUNIC)
    b.rod((0.12, -0.02, 0.4), (0.03, -0.1, 0.4), 0.036, TUNIC)
    b.ball(0.02, -0.12, 0.38, 0.02, 0.036, SKIN, sides=6)
    b.rod((0.02, -0.12, 0.4), (-0.06, -0.33, 0.37), 0.006, WOOD_LIGHT)
    b.pyramid(-0.064, -0.34, 0.363, 0.009, 0.009, 0.38, STEEL, sides=4)
    b.box(0.015, 0.025, -0.13, -0.11, 0.39, 0.41, FEATHER)


HUNTER = ("FigureHunter", 0.98, hunter_body, [
    ("LegL", lambda b: boots(b, -1), (-0.045, 0.0, 0.19)),
    ("LegR", lambda b: boots(b, 1), (0.045, 0.0, 0.19)),
    ("Head", hunter_head, (0.0, 0.0, 0.41)),
    ("ArmDraw", hunter_draw_arm, (0.095, 0.0, 0.385)),
    ("ArmBow", hunter_bow_arm, (-0.095, 0.0, 0.385)),
])


# ------------------------------------------------------------------------------------------------ the boar

BOAR, BOAR_DARK, CREST = 'wood_dark', 'door', 'window'
SNOUT, TUSK, HOOF = 'plaster_pink', 'white', 'iron'


def oval(b, c, hx, hy, hz, color, bevel):
    b.obox(c, (1, 0, 0), (0, 1, 0), (0, 0, 1), hx, hy, hz, color, bevel=bevel, skip=())


def boar_body(b):
    """A heavy wedge: high, broad shoulders falling to the rump; a black bristle crest along the spine."""
    o.tube(b, [(0, -0.17, 0.22), (0, -0.07, 0.235), (0, 0.06, 0.225), (0, 0.17, 0.2), (0, 0.21, 0.19)],
           [0.075, 0.095, 0.088, 0.07, 0.0], BOAR, sides=7, tip=True)
    for i in range(7):
        y = -0.14 + i * 0.045
        z = 0.325 - i * 0.014
        b.pyramid(0, y, z - 0.02, 0.014, 0.024, z + 0.05 - i * 0.004, CREST, sides=4)
    o.tube(b, [(0, 0.2, 0.22), (0, 0.24, 0.18), (0.01, 0.25, 0.13)], [0.012, 0.008, 0.0], BOAR_DARK, sides=4)  # tail


def boar_leg(b, x, y):
    b.rod((x, y, 0.18), (x, y, 0.07), 0.034, BOAR)
    b.rod((x, y, 0.07), (x, y, 0.02), 0.022, CREST)
    b.box(x - 0.014, x + 0.014, y - 0.02, y + 0.01, 0.0, 0.024, HOOF, bevel=0.004)


def boar_head(b):
    """A long wedge of a head, small eyes, ears up, a pink snout disc and curling white tusks."""
    oval(b, (0, -0.21, 0.22), 0.055, 0.06, 0.055, BOAR, 0.025)
    oval(b, (0, -0.28, 0.19), 0.035, 0.05, 0.032, BOAR, 0.015)
    b.hcylinder('y', (0, -0.33, 0.185), 0.012, 0.026, SNOUT, sides=7)
    for k in (-1, 1):
        b.box(k * 0.009 - 0.004, k * 0.009 + 0.004, -0.344, -0.338, 0.18, 0.19, SPOT)    # nostrils
        # eyes: set on the brow's front corners so they show from the front and the side, a glint in each
        b.box(k * 0.038 - 0.012, k * 0.038 + 0.012, -0.268, -0.238, 0.232, 0.256, SPOT, bevel=0.004)
        b.box(k * 0.038 + k * 0.004 - 0.004, k * 0.038 + k * 0.004 + 0.004, -0.271, -0.266, 0.246, 0.253, 'white')
        o.tube(b, [(k * 0.035, -0.19, 0.26), (k * 0.055, -0.18, 0.31)], [0.018, 0.0], BOAR_DARK, sides=4)  # ears
        o.tube(b, [(k * 0.03, -0.31, 0.17), (k * 0.05, -0.33, 0.2), (k * 0.045, -0.31, 0.235)],
               [0.009, 0.007, 0.0], TUSK, sides=4)


BOAR_SPEC = ("FaunaBoar", 1.0, boar_body, [
    ("LegFL", lambda b: boar_leg(b, -0.05, -0.1), (-0.05, -0.1, 0.18)),
    ("LegFR", lambda b: boar_leg(b, 0.05, -0.1), (0.05, -0.1, 0.18)),
    ("LegBL", lambda b: boar_leg(b, -0.05, 0.13), (-0.05, 0.13, 0.18)),
    ("LegBR", lambda b: boar_leg(b, 0.05, 0.13), (0.05, 0.13, 0.18)),
    ("Head", boar_head, (0.0, -0.16, 0.22)),
])

SPECS = (HUNTER, BOAR_SPEC)


def whole(spec):
    """One Builder with every part in place (for the review renders; the game gets jointed parts)."""
    _, scale, body, parts = spec
    b = Builder()
    body(b)
    for _, fn, _ in parts:
        fn(b)
    if scale != 1.0:
        o.scale_since(b, 0, scale)
    return b
