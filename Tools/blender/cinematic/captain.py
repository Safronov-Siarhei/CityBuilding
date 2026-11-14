"""Капитан стражи -- the captain of the town guard (mission 2's opening cutscene, the user 2026-11-14): he
meets Ардан at the north gate, hears him out, sends the rider to the capital and hands Ардан the spare
sword from his hip. The same man as the game's SoldierCaptain (soldier_captain.py), on Ардан's rig
(figure.py) with the cutscene set's detail:

a bright steel breastplate with a gold rim, a blue skirt with a gold hem, a wine half cloak and a wine sash
across the chest, broad gold-edged pauldrons, steel greaves over dark cuffed boots, mailed arms with steel
gauntlets (hands free: he points, beckons and gives the sword); the spare sword scabbarded on the left hip;
an open burgonet with a peak, cheek plates and a tall white plume; a weathered face with a grey moustache,
sideburns and a scar over the left cheek.

    blender --background <Buildings-Size1-Models.blend> --python captain.py
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import figure as fg  # noqa: E402
import kit  # noqa: E402

OBJ = "CineCaptain"
SCALE = 1.08

SKIN = 'plaster_pink'
STEEL, STEEL_DARK, MAIL = 'stone_mid', 'iron', 'stone_dark'   # darker than the soldiers' steel: the white plume stands out
BLUE = 'banner_blue'
WINE = 'roof_red_dark'
GOLD = 'gold'
GREY = 'plaster_white'
BOOT, SOLE = 'wood_dark', 'door'
PLUME = 'white'
EYE, PUPIL = 'white', 'window'
fg.plain(SKIN, STEEL, STEEL_DARK, MAIL, BLUE, WINE, GOLD, GREY, BOOT, SOLE, PLUME, EYE, PUPIL)


def foot(b, k):
    x = k * fg.HIP_X
    b.box(x - 0.03, x + 0.03, -0.06, 0.03, 0.0, 0.012, SOLE, bevel=0.004)
    b.box(x - 0.028, x + 0.028, -0.056, 0.028, 0.012, 0.036, BOOT, bevel=0.009)


def shin(b, k):
    x = k * fg.HIP_X
    b.box(x - 0.026, x + 0.026, -0.026, 0.026, 0.03, 0.062, BOOT, bevel=0.008)
    b.box(x - 0.031, x + 0.031, -0.031, 0.031, 0.05, 0.062, BOOT, bevel=0.005)          # the cuff
    b.box(x - 0.025, x + 0.025, -0.029, 0.025, 0.062, 0.1, STEEL, bevel=0.009)          # the greave
    b.box(x - 0.006, x + 0.006, -0.032, -0.027, 0.064, 0.098, STEEL_DARK)               # its ridge


def thigh(b, k):
    x = k * fg.HIP_X
    b.box(x - 0.022, x + 0.022, -0.032, 0.022, 0.09, 0.112, STEEL, bevel=0.007)         # the knee cop
    b.box(x - 0.023, x + 0.023, -0.023, 0.023, 0.105, 0.2, MAIL, bevel=0.008)


def waist(b):
    """The blue skirt to the knee, a gold hem, mail at the front slit; the belt; the spare sword."""
    b.frustum(0, 0, 0.11, 0.255, 0.094, 0.082, BLUE, sides=10)
    b.frustum(0, 0, 0.106, 0.124, 0.096, 0.094, GOLD, sides=10, cap=False)
    b.box(-0.012, 0.012, -0.096, -0.086, 0.11, 0.2, MAIL)
    b.cylinder(0, 0, 0.245, 0.266, 0.086, BOOT, sides=10)
    b.box(-0.017, 0.017, -0.092, -0.084, 0.244, 0.268, GOLD, bevel=0.003)
    # the scabbard down the left hip, the hilt forward and up
    b.rod((-0.088, -0.018, 0.25), (-0.114, 0.026, 0.1), 0.019, BOOT)
    b.rod((-0.114, 0.026, 0.1), (-0.117, 0.031, 0.082), 0.021, GOLD)
    for t in (0.35, 0.7):
        p = (-0.088 + (-0.026) * t, -0.018 + 0.044 * t, 0.25 - 0.15 * t)
        b.box(p[0] - 0.012, p[0] + 0.012, p[1] - 0.012, p[1] + 0.012, p[2] - 0.004, p[2] + 0.004, GOLD)
    b.rod((-0.088, -0.018, 0.25), (-0.08, -0.034, 0.31), 0.011, BOOT)                  # the grip
    b.rod((-0.108, -0.01, 0.255), (-0.068, -0.027, 0.25), 0.011, GOLD)                 # the cross
    b.ball(-0.079, -0.035, 0.312, 0.009, 0.015, GOLD, sides=6)                          # the pommel


SASH = ([(x, -1.0, z) for x, z in [(-0.062, 0.27), (-0.04133333333333333, 0.29033333333333333), (-0.020666666666666667, 0.3106666666666667), (0.0, 0.331), (0.020666666666666667, 0.35133333333333333), (0.04133333333333333, 0.3716666666666667), (0.062, 0.392)]] + [(0.062, -1.0, 1.0), (0.062, 0.0, 1.0), (0.062, 1.0, 1.0)]
        + [(x, 1.0, z) for x, z in [(0.062, 0.392), (0.04133333333333333, 0.3716666666666667), (0.020666666666666667, 0.35133333333333333), (0.0, 0.331), (-0.020666666666666667, 0.3106666666666667), (-0.04133333333333333, 0.29033333333333333), (-0.062, 0.27)]])


def chest(b):
    """The breastplate with its gold rims and ridge, the wine sash and its knot, the half cloak, the
    pauldrons and the gorget."""
    b.obox((0, -0.003, 0.33), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.084, 0.06, 0.064, STEEL, bevel=0.022)
    b.obox((0, -0.003, 0.272), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.086, 0.062, 0.006, GOLD, bevel=0.004)
    b.box(-0.004, 0.004, -0.066, -0.058, 0.276, 0.385, STEEL_DARK)
    b.box(-0.05, 0.05, -0.062, -0.054, 0.378, 0.386, GOLD)                              # the neck rim
    # the sash lies ON the breastplate, round the front, over the right shoulder and down the back
    kit.band_on_box(b, (0, -0.003, 0.33), (0.084, 0.06, 0.064), 0.022, SASH, 0.03, 0.0025, WINE)
    b.ball(-0.062, -0.09, 0.25, 0.013, 0.022, WINE, sides=6)                           # the knot over the belt
    b.obox((-0.057, -0.09, 0.226), (1, 0, 0), (0, 1, 0), (0.25, 0, 1), 0.01, 0.0035, 0.024, WINE)   # its tail
    # the half cloak from the shoulders down the back, to the knee, clear of the skirt; a gold edge at its foot
    b.obox((0, 0.1, 0.24), (1, 0, 0), (0, 1, 0), (0, -0.12, 1), 0.098, 0.01, 0.148, WINE, bevel=0.008)
    b.box(-0.1, 0.1, 0.104, 0.124, 0.092, 0.102, GOLD)
    for k in (-1, 1):
        b.ball(k * 0.098, 0.0, 0.358, 0.052, 0.056, STEEL, sides=8)                     # pauldrons
        b.box(k * 0.098 - 0.047, k * 0.098 + 0.047, -0.006, 0.006, 0.378, 0.385, GOLD)
        b.ball(k * 0.07, 0.086, 0.372, 0.012, 0.016, GOLD, sides=6)                     # the cloak's studs
    b.cylinder(0, 0, 0.39, 0.406, 0.056, STEEL_DARK, sides=8)


def upper_arm(b, k):
    sh, el, _ = fg.joints(k)
    b.rod(sh, el, 0.044, MAIL)


def forearm(b, k):
    """Mail sleeves, steel gauntlets with a gold cuff edge; bare fingers free to point and give."""
    _, el, wr = fg.joints(k)
    d = (wr - el).normalized()
    b.rod(el, wr, 0.042, MAIL)
    b.rod(wr - d * 0.03, wr + d * 0.004, 0.05, STEEL)
    b.rod(wr - d * 0.03, wr - d * 0.022, 0.052, GOLD)
    b.ball(wr.x, wr.y, wr.z - 0.034, 0.02, 0.034, STEEL, sides=6)


def head(b):
    z = 0.425
    b.cylinder(0, 0.0, 0.39, 0.432, 0.025, SKIN, sides=6)
    b.box(-0.043, 0.043, -0.045, 0.041, z - 0.004, z + 0.088, SKIN, bevel=0.018)
    b.box(-0.008, 0.008, -0.058, -0.044, z + 0.024, z + 0.05, SKIN, bevel=0.004)        # a broad nose
    for k in (-1, 1):
        b.box(k * 0.046 - 0.007, k * 0.046 + 0.007, -0.009, 0.019, z + 0.024, z + 0.056, SKIN, bevel=0.003)
        b.box(k * 0.019 - 0.009, k * 0.019 + 0.009, -0.0475, -0.0435, z + 0.046, z + 0.056, EYE)
        b.box(k * 0.017 - 0.0045, k * 0.017 + 0.0045, -0.0485, -0.046, z + 0.047, z + 0.055, PUPIL)
        b.box(k * 0.021 - 0.015, k * 0.021 + 0.015, -0.05, -0.043, z + 0.06, z + 0.069, GREY, bevel=0.002)   # brows
        b.box(k * 0.044 - 0.006, k * 0.044 + 0.006, -0.03, 0.012, z + 0.01, z + 0.06, GREY, bevel=0.003)    # sideburns
        # the long grey moustache, its ends drooping
        b.box(k * 0.022 - 0.02, k * 0.022 + 0.02, -0.056, -0.046, z + 0.014, z + 0.026, GREY, bevel=0.003)
        b.box(k * 0.038 - 0.006, k * 0.038 + 0.006, -0.053, -0.045, z - 0.008, z + 0.018, GREY, bevel=0.002)
    b.box(-0.012, 0.012, -0.0485, -0.0455, z + 0.004, z + 0.009, 'roof_red_dark')       # the mouth
    b.obox((0.027, -0.0465, z + 0.036), (0.5, 0, 0.866), (0, 1, 0), (-0.866, 0, 0.5), 0.017, 0.0025, 0.003, WINE)  # scar
    b.box(-0.046, 0.046, 0.016, 0.047, z + 0.006, z + 0.086, GREY, bevel=0.01)             # grey hair at the back
    # the burgonet: a dome, a rim, the peak over the eyes, cheek plates, the plume's socket, the white plume
    b.dome(0, 0.004, z + 0.084, 0.055, 0.06, [STEEL, STEEL], sides=10)
    b.cylinder(0, 0.004, z + 0.078, z + 0.092, 0.058, STEEL_DARK, sides=10)
    b.obox((0, -0.066, z + 0.084), (1, 0, 0), (0, 1, -0.25), (0, 0.25, 1), 0.048, 0.02, 0.0035, STEEL, bevel=0.003)
    for k in (-1, 1):
        b.box(k * 0.054 - 0.006, k * 0.054 + 0.006, -0.026, 0.03, z + 0.02, z + 0.086, STEEL, bevel=0.004)
        b.box(k * 0.054 - 0.007, k * 0.054 + 0.007, -0.003, 0.003, z + 0.026, z + 0.08, GOLD)
    b.cylinder(0, 0.016, z + 0.128, z + 0.148, 0.01, STEEL_DARK, sides=6)               # the plume's socket
    b.prism([(0.0, 0.0), (0.045, 0.018), (0.1, 0.075), (0.092, 0.12), (0.028, 0.075)], (-0.012, 0.012, z + 0.14),
            (0, 1, 0), (0, 0, 1), 0.024, PLUME, back_cap=True)


PARTS = [("hips", waist), ("chest", chest), ("head", head)] + fg.limbs(
    {"thigh": thigh, "shin": shin, "foot": foot, "upper_arm": upper_arm, "forearm": forearm})


def make(location=(0, 0, 0)):
    return fg.make(OBJ, PARTS, SCALE, location)


if __name__ == "__main__":
    make()
