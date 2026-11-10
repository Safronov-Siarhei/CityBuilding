"""Священник -- the village priest (cutscene, 2026-11-10): leads the service in the opening shots,
then rings the alarm bell when the orcs come; we may meet him again in the town.

Same style as Ардан: a dark cassock to the ground buttoned down the front, a white collar band,
a purple stole over the shoulders falling to the knees with gold-fringed ends, a purple sash,
a gold sun medallion on a chain; middle-aged, clean-shaven, short dark hair receding at the brow,
a round kind face; a red prayer book with gold corners in his left hand. Unlike the game's monk
(white habit, blue scapular). On Ардан's rig (figure.py).

    blender --background <Buildings-Size1-Models.blend> --python priest.py
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import figure as fg  # noqa: E402

OBJ = "CinePriest"
SCALE = 1.02

SKIN = 'plaster_pink'
CASSOCK, BUTTON = 'iron', 'window'
COLLAR = 'plaster_white'
STOLE, STOLE_DARK = 'roof_purple', 'roof_purple_dark'
GOLD = 'gold'
HAIR = 'wood_dark'
SHOE = 'window'
BOOK, PAGES = 'roof_red_dark', 'plaster'
EYE, PUPIL = 'white', 'window'
fg.plain(SKIN, CASSOCK, BUTTON, COLLAR, STOLE, STOLE_DARK, GOLD, HAIR, SHOE, BOOK, PAGES, EYE, 'roof_red_dark')


def foot(b, k):
    x = k * fg.HIP_X
    b.box(x - 0.027, x + 0.027, -0.056, 0.026, 0.0, 0.026, SHOE, bevel=0.009)


def shin(b, k):
    x = k * fg.HIP_X
    b.box(x - 0.022, x + 0.022, -0.022, 0.022, 0.024, 0.1, CASSOCK, bevel=0.007)


def thigh(b, k):
    x = k * fg.HIP_X
    b.box(x - 0.023, x + 0.023, -0.023, 0.023, 0.098, 0.2, CASSOCK, bevel=0.008)


SKIRT_Z0, SKIRT_Z1, SKIRT_R0, SKIRT_R1 = 0.028, 0.27, 0.104, 0.083
FLAT = math.cos(math.pi / 8)            # an 8-sided skirt's flat front is this much of its radius


def skirt_front(z):
    """How far forward (-y) the skirt's flat front face is at height z."""
    t = (z - SKIRT_Z0) / (SKIRT_Z1 - SKIRT_Z0)
    return FLAT * (SKIRT_R0 + (SKIRT_R1 - SKIRT_R0) * t)


def on_skirt(b, x, z0, z1, half_w, depth, color, lift=0.0):
    """A flat strip lying on the skirt's sloping front from z0 to z1."""
    y0, y1 = -(skirt_front(z0) + lift), -(skirt_front(z1) + lift)
    along = (0, y1 - y0, z1 - z0)
    length = math.hypot(along[1], along[2])
    ay = (0, -along[2] / length, along[1] / length)                 # out of the cloth, forward
    centre = (x, (y0 + y1) / 2 + ay[1] * depth / 2, (z0 + z1) / 2 + ay[2] * depth / 2)
    b.obox(centre, (1, 0, 0), ay, (0, along[1], along[2]), half_w, depth / 2, length / 2, color)


def waist(b):
    """The cassock's skirt to the ankles, buttons down the front, the stole's two bands lying on the
    skirt down to the knees, the sash over them (2026-11-10: the bands stood off the cloth and the
    belt ran behind them)."""
    b.frustum(0, 0, SKIRT_Z0, SKIRT_Z1, SKIRT_R0, SKIRT_R1, CASSOCK, sides=8)
    for i in range(6):
        on_skirt(b, 0, 0.055 + i * 0.03, 0.062 + i * 0.03, 0.0035, 0.003, BUTTON)
    for k in (-1, 1):
        x = k * 0.03
        on_skirt(b, x, 0.12, 0.25, 0.014, 0.004, STOLE)
        on_skirt(b, x, 0.106, 0.124, 0.015, 0.006, GOLD)                            # the fringe
        on_skirt(b, x, 0.186, 0.198, 0.006, 0.006, GOLD)                            # a little sun
    # the sash: a band round the waist over the stole, its end hanging at his left hip
    b.cylinder(0, 0, 0.236, 0.262, 0.095, STOLE_DARK, sides=8)
    b.box(0.052, 0.07, -0.082, -0.07, 0.16, 0.24, STOLE_DARK, bevel=0.003)
    b.box(0.05, 0.072, -0.084, -0.068, 0.15, 0.162, GOLD, bevel=0.002)


def chest(b):
    b.obox((0, 0, 0.33), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.08, 0.056, 0.06, CASSOCK, bevel=0.02)
    b.obox((0, 0, 0.377), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.087, 0.058, 0.016, CASSOCK, bevel=0.012)
    for i in range(4):
        z = 0.285 + i * 0.03
        b.box(-0.0035, 0.0035, -0.0625, -0.0575, z - 0.0035, z + 0.0035, BUTTON)
    # the stole over the shoulders and down the chest
    for k in (-1, 1):
        x = k * 0.03
        b.box(x - 0.014, x + 0.014, -0.062, -0.056, 0.255, 0.39, STOLE, bevel=0.002)
        b.box(x - 0.014, x + 0.014, -0.056, 0.06, 0.383, 0.396, STOLE, bevel=0.003)
    # the collar: a standing black band with the white tab in front
    b.obox((0, 0.0, 0.402), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.032, 0.03, 0.012, CASSOCK, bevel=0.006)
    b.box(-0.008, 0.008, -0.033, -0.029, 0.396, 0.41, COLLAR)
    # the gold sun medallion between the stole's bands, on a chain round his neck
    for k in (-1, 1):
        pts = [(k * 0.03, -0.035, 0.398), (k * 0.016, -0.06, 0.38), (k * 0.006, -0.064, 0.362), (0.0, -0.065, 0.354)]
        for p0, p1 in zip(pts, pts[1:]):
            b.rod(p0, p1, 0.0035, GOLD)
    b.prism([(0.009 * math.cos(a), 0.342 + 0.009 * math.sin(a)) for a in [i * math.pi / 4 for i in range(8)]],
            (0, -0.062, 0), (1, 0, 0), (0, 0, 1), 0.004, GOLD, back_cap=True)
    for i in range(8):
        a = i * math.pi / 4 + math.pi / 8
        b.box(0.0125 * math.cos(a) - 0.0017, 0.0125 * math.cos(a) + 0.0017, -0.0645, -0.0615,
              0.342 + 0.0125 * math.sin(a) - 0.0017, 0.342 + 0.0125 * math.sin(a) + 0.0017, GOLD)


def upper_arm(b, k):
    sh, el, _ = fg.joints(k)
    b.rod(sh, el, 0.044, CASSOCK)


def forearm(b, k):
    """Sleeves widening to the wrist; the left hand holds the red prayer book against his hip."""
    _, el, wr = fg.joints(k)
    d = (wr - el).normalized()
    b.rod(el, wr - d * 0.012, 0.044, CASSOCK)
    b.rod(wr - d * 0.014, wr + d * 0.006, 0.052, CASSOCK)
    b.ball(wr.x, wr.y, wr.z - 0.034, 0.02, 0.034, SKIN, sides=6)
    if k == 1:
        x, y, z = wr.x + 0.004, wr.y - 0.012, wr.z - 0.03
        b.box(x - 0.012, x + 0.012, y - 0.042, y + 0.012, z - 0.03, z + 0.035, BOOK, bevel=0.003)
        b.box(x - 0.0125, x + 0.0125, y - 0.038, y + 0.008, z - 0.026, z + 0.031, PAGES)
        b.box(x - 0.013, x + 0.013, y - 0.041, y - 0.033, z + 0.026, z + 0.036, GOLD)
        b.box(x - 0.013, x + 0.013, y - 0.041, y - 0.033, z - 0.031, z - 0.021, GOLD)


def head(b):
    z = 0.425
    b.cylinder(0, 0.0, 0.39, 0.432, 0.026, SKIN, sides=6)
    # a rounder, fuller face than the lads'
    b.box(-0.047, 0.047, -0.046, 0.042, z - 0.006, z + 0.088, SKIN, bevel=0.02)
    b.box(-0.007, 0.007, -0.057, -0.044, z + 0.022, z + 0.044, SKIN, bevel=0.004)
    b.box(-0.011, 0.011, -0.0485, -0.0455, z + 0.012, z + 0.015, 'roof_red_dark')    # a calm smile
    for k in (-1, 1):
        b.box(k * 0.0135 - 0.003, k * 0.0135 + 0.003, -0.0485, -0.0455, z + 0.014, z + 0.017, 'roof_red_dark')
        b.box(k * 0.05 - 0.006, k * 0.05 + 0.006, -0.008, 0.018, z + 0.024, z + 0.052, SKIN, bevel=0.003)
        b.box(k * 0.02 - 0.0085, k * 0.02 + 0.0085, -0.0485, -0.0435, z + 0.044, z + 0.057, EYE)
        b.box(k * 0.019 - 0.005, k * 0.019 + 0.005, -0.0495, -0.047, z + 0.045, z + 0.056, PUPIL)
        b.obox((k * 0.021, -0.047, z + 0.065), (1, 0, -0.12 * k), (0, 1, 0), (0.12 * k, 0, 1),
               0.012, 0.0035, 0.003, HAIR)                                            # soft, raised brows
    # short dark hair, receding at the brow
    b.box(-0.049, 0.049, -0.02, 0.046, z + 0.07, z + 0.1, HAIR, bevel=0.012)
    b.box(-0.049, 0.049, 0.0, 0.047, z + 0.02, z + 0.085, HAIR, bevel=0.012)
    for k in (-1, 1):
        b.box(k * 0.048 - 0.006, k * 0.048 + 0.006, -0.03, 0.03, z + 0.035, z + 0.075, HAIR, bevel=0.003)


PARTS = [("hips", waist), ("chest", chest), ("head", head)] + fg.limbs(
    {"thigh": thigh, "shin": shin, "foot": foot, "upper_arm": upper_arm, "forearm": forearm})


def make(location=(0, 0, 0)):
    return fg.make(OBJ, PARTS, SCALE, location)


if __name__ == "__main__":
    make()
