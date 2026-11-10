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


def waist(b):
    """The cassock's skirt to the ankles, buttons down the front, the sash, the stole's ends."""
    b.frustum(0, 0, 0.028, 0.27, 0.104, 0.083, CASSOCK, sides=8)
    for i in range(7):
        z = 0.06 + i * 0.03
        b.box(-0.0035, 0.0035, -0.103 + i * 0.0029, -0.098 + i * 0.0029, z - 0.0035, z + 0.0035, BUTTON)
    b.cylinder(0, 0, 0.238, 0.262, 0.087, STOLE, sides=8)
    b.box(0.03, 0.05, -0.092, -0.082, 0.17, 0.25, STOLE_DARK, bevel=0.003)          # the sash's end
    # the stole's two bands down to the knees, gold-fringed
    for k in (-1, 1):
        x = k * 0.03
        b.box(x - 0.014, x + 0.014, -0.098, -0.089, 0.12, 0.27, STOLE, bevel=0.002)
        b.box(x - 0.015, x + 0.015, -0.1, -0.088, 0.105, 0.122, GOLD, bevel=0.002)
        b.box(x - 0.006, x + 0.006, -0.1005, -0.097, 0.2, 0.212, GOLD)              # a little sun


def chest(b):
    b.obox((0, 0, 0.33), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.08, 0.056, 0.06, CASSOCK, bevel=0.02)
    b.obox((0, 0, 0.377), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.087, 0.058, 0.016, CASSOCK, bevel=0.012)
    for i in range(4):
        z = 0.285 + i * 0.03
        b.box(-0.0035, 0.0035, -0.0625, -0.0575, z - 0.0035, z + 0.0035, BUTTON)
    # the stole over the shoulders and down the chest
    for k in (-1, 1):
        x = k * 0.03
        b.box(x - 0.014, x + 0.014, -0.062, -0.056, 0.27, 0.39, STOLE, bevel=0.002)
        b.box(x - 0.014, x + 0.014, -0.056, 0.06, 0.383, 0.396, STOLE, bevel=0.003)
    # the collar: a standing black band with the white tab in front
    b.obox((0, 0.0, 0.402), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.032, 0.03, 0.012, CASSOCK, bevel=0.006)
    b.box(-0.008, 0.008, -0.033, -0.029, 0.396, 0.41, COLLAR)
    # the gold sun medallion on its chain
    b.rod((-0.02, -0.05, 0.4), (0.0, -0.064, 0.345), 0.003, GOLD)
    b.rod((0.02, -0.05, 0.4), (0.0, -0.064, 0.345), 0.003, GOLD)
    b.prism([(0.014 * math.cos(a), 0.33 + 0.014 * math.sin(a)) for a in [i * math.pi / 4 for i in range(8)]],
            (0, -0.063, 0), (1, 0, 0), (0, 0, 1), 0.005, GOLD, back_cap=True)
    for i in range(8):
        a = i * math.pi / 4
        b.box(0.02 * math.cos(a) - 0.002, 0.02 * math.cos(a) + 0.002, -0.066, -0.063,
              0.33 + 0.02 * math.sin(a) - 0.002, 0.33 + 0.02 * math.sin(a) + 0.002, GOLD)


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
