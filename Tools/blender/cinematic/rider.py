"""Гонец -- the rider (mission 2's opening cutscene, the user 2026-11-14: «гнедой и лёгкий гонец»): the captain calls
him, «Скачи в столицу! Скажи королю: орки у ворот!», he swings up onto the bay (horse.py) and gallops off over
the causeway. Later scenes (the capital) reuse him.

Same style as Ардан and the captain, on Ардан's rig (figure.py). Light, built for speed -- no plate, no lance:
a brown leather jerkin over a blue tunic, a short blue riding cloak with its hood down and a gold clasp,
grey riding breeches in tall brown boots, leather gloves; a steel nasal helmet; a short sword on the left
hip (+x); the DISPATCH -- a leather message tube with gold caps -- on a strap across his back from the right
shoulder; a young face with a short dark beard.

    blender --background <Buildings-Size1-Models.blend> --python rider.py
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import figure as fg  # noqa: E402
import kit  # noqa: E402

OBJ = "CineRider"
SCALE = 1.0

SKIN = 'plaster_pink'
BLUE, BLUE_DARK = 'banner_blue', 'roof_blue_dark'
JERKIN, LEATHER = 'wood_mid', 'wood_dark'
BREECHES = 'stone_mid'
BOOT, SOLE = 'door', 'iron'
STEEL, STEEL_DARK = 'stone_mid', 'iron'
GOLD = 'gold'
HAIR = 'wood_dark'
EYE, PUPIL = 'white', 'window'
fg.plain(SKIN, BLUE, BLUE_DARK, JERKIN, LEATHER, BREECHES, BOOT, SOLE, STEEL, STEEL_DARK, GOLD, HAIR, EYE, PUPIL)

CHEST = ((0, 0, 0.33), (0.078, 0.054, 0.06), 0.02)
# the dispatch strap: from the left hip (+x) up the front, over the right shoulder (-x), down the back
STRAP = ([(0.058 - 0.116 * i / 6, -1.0, 0.272 + 0.112 * i / 6) for i in range(7)]
         + [(-0.058, -1.0, 1.0), (-0.058, 0.0, 1.0), (-0.058, 1.0, 1.0)]
         + [(-0.058 + 0.116 * i / 6, 1.0, 0.384 - 0.112 * i / 6) for i in range(7)])


def foot(b, k):
    x = k * fg.HIP_X
    b.box(x - 0.028, x + 0.028, -0.058, 0.028, 0.0, 0.012, SOLE, bevel=0.004)
    b.box(x - 0.026, x + 0.026, -0.054, 0.026, 0.012, 0.036, BOOT, bevel=0.009)


def shin(b, k):
    """Tall riding boots to the knee, a turned-down top."""
    x = k * fg.HIP_X
    b.box(x - 0.026, x + 0.026, -0.026, 0.026, 0.03, 0.1, BOOT, bevel=0.008)
    b.box(x - 0.029, x + 0.029, -0.032, 0.029, 0.086, 0.104, BOOT, bevel=0.005)


def thigh(b, k):
    x = k * fg.HIP_X
    b.box(x - 0.024, x + 0.024, -0.024, 0.024, 0.098, 0.2, BREECHES, bevel=0.008)


def waist(b):
    """The tunic's split skirt (for the saddle) under the jerkin's hem, the belt, the short sword."""
    for k in (-1, 1):
        b.obox((k * 0.04, 0, 0.215), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.042, 0.075, 0.05, BLUE, bevel=0.012)
    b.frustum(0, 0, 0.215, 0.262, 0.084, 0.078, JERKIN, sides=10)
    b.cylinder(0, 0, 0.25, 0.268, 0.081, LEATHER, sides=10)
    b.box(-0.014, 0.014, -0.087, -0.079, 0.249, 0.27, GOLD, bevel=0.003)
    # the short sword on the left hip (+x)
    b.rod((0.086, -0.012, 0.25), (0.106, 0.03, 0.12), 0.017, LEATHER)
    b.rod((0.106, 0.03, 0.12), (0.108, 0.034, 0.108), 0.019, STEEL_DARK)
    b.rod((0.086, -0.012, 0.25), (0.08, -0.026, 0.3), 0.01, LEATHER)
    b.rod((0.1, -0.006, 0.256), (0.068, -0.02, 0.25), 0.01, STEEL)
    b.ball(0.079, -0.027, 0.302, 0.008, 0.013, STEEL, sides=6)


def chest(b):
    """The leather jerkin laced at the front over the blue tunic, the short riding cloak and its hood, the
    dispatch strap lying on the jerkin and the tube on his back."""
    c, h, r = CHEST
    b.obox(c, (1, 0, 0), (0, 1, 0), (0, 0, 1), h[0], h[1], h[2], JERKIN, bevel=r)
    b.box(-0.012, 0.012, -0.058, -0.052, 0.28, 0.385, BLUE)                              # the tunic at the lacing
    for z in (0.3, 0.325, 0.35):
        b.box(-0.016, 0.016, -0.06, -0.056, z, z + 0.004, LEATHER)                       # the laces
    kit.band_on_box(b, c, h, r, STRAP, 0.018, 0.0025, LEATHER)
    # the message tube across the back, gold caps
    b.rod((-0.07, 0.07, 0.4), (0.06, 0.07, 0.27), 0.03, LEATHER)
    for t in ((-0.07, 0.07, 0.4), (0.06, 0.07, 0.27)):
        b.ball(t[0], t[1], t[2] - 0.012, 0.018, 0.024, GOLD, sides=6)
    # the short riding cloak: over the shoulders, to the waist at the back, the hood bunched at the neck
    b.obox((0, 0.004, 0.378), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.092, 0.064, 0.026, BLUE, bevel=0.014)
    b.obox((0, 0.062, 0.32), (1, 0, 0), (0, 1, 0), (0, 0.1, 1), 0.086, 0.01, 0.07, BLUE, bevel=0.008)
    b.box(-0.09, 0.09, 0.06, 0.074, 0.248, 0.258, BLUE_DARK)                             # its hem
    b.obox((0, 0.05, 0.4), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.05, 0.024, 0.024, BLUE_DARK, bevel=0.012)  # the hood
    b.ball(0.05, -0.06, 0.39, 0.011, 0.016, GOLD, sides=6)                               # the clasp
    b.cylinder(0, 0, 0.388, 0.404, 0.046, BLUE_DARK, sides=8)                            # the collar


def upper_arm(b, k):
    sh, el, _ = fg.joints(k)
    b.rod(sh, el, 0.042, BLUE)
    b.rod(sh, sh + (el - sh) * 0.35, 0.05, BLUE)                                         # the cloak over the shoulder


def forearm(b, k):
    """Blue sleeves, long leather gloves to mid-forearm."""
    _, el, wr = fg.joints(k)
    d = (wr - el).normalized()
    b.rod(el, wr, 0.039, BLUE)
    b.rod(wr - d * 0.035, wr + d * 0.002, 0.046, LEATHER)
    b.ball(wr.x, wr.y, wr.z - 0.032, 0.02, 0.032, LEATHER, sides=6)


def head(b):
    z = 0.425
    b.cylinder(0, 0.0, 0.39, 0.432, 0.024, SKIN, sides=6)
    b.box(-0.041, 0.041, -0.043, 0.04, z - 0.002, z + 0.085, SKIN, bevel=0.018)
    for k in (-1, 1):
        b.box(k * 0.044 - 0.006, k * 0.044 + 0.006, -0.008, 0.016, z + 0.026, z + 0.054, SKIN, bevel=0.003)
        b.box(k * 0.018 - 0.009, k * 0.018 + 0.009, -0.0465, -0.0425, z + 0.044, z + 0.055, EYE)
        b.box(k * 0.016 - 0.0045, k * 0.016 + 0.0045, -0.0475, -0.045, z + 0.045, z + 0.054, PUPIL)
        b.box(k * 0.019 - 0.012, k * 0.019 + 0.012, -0.048, -0.042, z + 0.059, z + 0.066, HAIR, bevel=0.002)
        b.box(k * 0.043 - 0.005, k * 0.043 + 0.005, -0.02, 0.03, z + 0.03, z + 0.075, HAIR, bevel=0.003)   # hair at the sides
    # the short dark beard round the jaw, the mouth
    b.box(-0.04, 0.04, -0.047, -0.01, z - 0.012, z + 0.022, HAIR, bevel=0.01)
    b.box(-0.011, 0.011, -0.0495, -0.0465, z + 0.012, z + 0.016, 'roof_red_dark')
    b.box(-0.042, 0.042, 0.02, 0.046, z + 0.01, z + 0.08, HAIR, bevel=0.01)                  # hair at the back
    # the nasal helmet: a conical cap, a rim, the nose guard -- the nose under it
    b.frustum(0, 0.0, z + 0.07, z + 0.12, 0.048, 0.02, STEEL, sides=8)
    b.frustum(0, 0.0, z + 0.12, z + 0.14, 0.02, 0.004, STEEL, sides=8)
    b.cylinder(0, 0.0, z + 0.064, z + 0.078, 0.05, STEEL_DARK, sides=8)
    b.box(-0.006, 0.006, -0.056, -0.047, z + 0.024, z + 0.072, STEEL)                        # the nasal
    b.box(-0.005, 0.005, -0.05, -0.042, z + 0.024, z + 0.04, SKIN, bevel=0.002)             # the nose tip below it


PARTS = [("hips", waist), ("chest", chest), ("head", head)] + fg.limbs(
    {"thigh": thigh, "shin": shin, "foot": foot, "upper_arm": upper_arm, "forearm": forearm})


def make(location=(0, 0, 0)):
    return fg.make(OBJ, PARTS, SCALE, location)


if __name__ == "__main__":
    make()
