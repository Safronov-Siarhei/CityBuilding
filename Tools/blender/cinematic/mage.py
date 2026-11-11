"""Маг -- the mage at the bridge (mission 1, the user, 2026-11-11): «Беги в город, я их задержу»,
then he strikes the bridge down under the chasing orc and stays on the bank facing the horde.

Same style as Ардан and the old man, on Ардан's rig (figure.py). Not the old man again: a man in his
prime, upright, a short dark beard. A long deep-blue robe with a gold hem and a gold-studded belt,
a purple shoulder cape with a high stiff collar, a broad-brimmed pointed blue hat with a gold band,
and a staff whose forked head holds a violet crystal -- the colour of the portal's magic, but his.

    blender --background <Buildings-Size1-Models.blend> --python mage.py
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import figure as fg  # noqa: E402

OBJ = "CineMage"
SCALE = 1.04

SKIN = 'plaster_pink'
ROBE, ROBE_DARK = 'roof_blue', 'roof_blue_dark'
CAPE, CAPE_DARK = 'roof_purple', 'roof_purple_dark'
GOLD = 'gold'
HAIR = 'wood_dark'
BOOT, SOLE = 'iron', 'door'
STAFF, STAFF_DARK = 'wood_light', 'wood_mid'
GEM = 'roof_purple'
EYE, PUPIL = 'white', 'window'
fg.plain(SKIN, ROBE, ROBE_DARK, CAPE, CAPE_DARK, GOLD, HAIR, BOOT, SOLE, STAFF, STAFF_DARK, EYE, PUPIL, 'roof_red_dark',
         'white')


def foot(b, k):
    x = k * fg.HIP_X
    b.box(x - 0.029, x + 0.029, -0.058, 0.03, 0.0, 0.012, SOLE, bevel=0.004)
    b.box(x - 0.027, x + 0.027, -0.054, 0.028, 0.012, 0.034, BOOT, bevel=0.009)
    b.box(x - 0.012, x + 0.012, -0.064, -0.05, 0.016, 0.03, BOOT, bevel=0.004)          # a pointed toe


def shin(b, k):
    x = k * fg.HIP_X
    b.box(x - 0.025, x + 0.025, -0.025, 0.025, 0.03, 0.1, BOOT, bevel=0.008)


def thigh(b, k):
    x = k * fg.HIP_X
    b.box(x - 0.023, x + 0.023, -0.023, 0.023, 0.098, 0.2, ROBE_DARK, bevel=0.008)


def waist(b):
    """The robe's long skirt nearly to the ground, a gold hem, the gold-studded belt and its buckle."""
    b.frustum(0, 0, 0.045, 0.27, 0.108, 0.084, ROBE, sides=8)
    b.frustum(0, 0, 0.04, 0.064, 0.11, 0.108, GOLD, sides=8, cap=False)
    b.box(-0.022, 0.022, -0.106, -0.088, 0.06, 0.25, ROBE_DARK)                         # the front panel
    b.box(-0.006, 0.006, -0.108, -0.104, 0.064, 0.25, GOLD)                              # its gold stripe
    b.cylinder(0, 0, 0.245, 0.262, 0.088, 'wood_dark', sides=8)
    b.box(-0.016, 0.016, -0.095, -0.086, 0.242, 0.265, GOLD, bevel=0.003)                # the buckle
    for x in (-0.06, 0.06):
        b.box(x - 0.006, x + 0.006, -0.08, -0.07, 0.248, 0.259, GOLD)                    # studs
    # a pouch and a little bound book at the belt
    b.box(0.07, 0.098, -0.04, -0.01, 0.2, 0.245, 'wood_mid', bevel=0.005)
    b.box(-0.1, -0.072, -0.03, 0.01, 0.19, 0.24, CAPE_DARK, bevel=0.004)
    b.box(-0.098, -0.074, -0.031, -0.028, 0.196, 0.234, GOLD)


def chest(b):
    b.obox((0, 0, 0.33), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.08, 0.056, 0.06, ROBE, bevel=0.02)
    b.box(-0.022, 0.022, -0.06, -0.052, 0.27, 0.37, ROBE_DARK)
    b.box(-0.006, 0.006, -0.062, -0.058, 0.27, 0.37, GOLD)
    # the purple shoulder cape, a little longer at the back, and its high stiff collar
    b.obox((0, 0.004, 0.37), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.094, 0.066, 0.032, CAPE, bevel=0.016)
    b.obox((0, 0.05, 0.31), (1, 0, 0), (0, 1, 0), (0, 0.15, 1), 0.086, 0.018, 0.07, CAPE, bevel=0.01)
    b.box(-0.094, 0.094, -0.07, 0.07, 0.336, 0.344, GOLD)                                 # the cape's gold edge
    b.box(-0.05, 0.05, 0.03, 0.05, 0.39, 0.45, CAPE_DARK, bevel=0.006)                    # the collar, behind the head
    for k in (-1, 1):
        b.obox((k * 0.052, -0.006, 0.42), (0, 1, 0), (1, 0, 0), (0, 0, 1), 0.04, 0.007, 0.03, CAPE_DARK, bevel=0.004)
    b.ball(0, -0.064, 0.372, 0.013, 0.02, GEM, sides=6)                                   # the clasp
    b.box(-0.026, 0.026, -0.058, -0.05, 0.372, 0.398, 'plaster', bevel=0.004)


def upper_arm(b, k):
    sh, el, _ = fg.joints(k)
    b.rod(sh, el, 0.045, ROBE)


def forearm(b, k):
    """Wide sleeves with gold cuffs; the right hand grips the staff."""
    _, el, wr = fg.joints(k)
    d = (wr - el).normalized()
    b.rod(el, wr + d * 0.004, 0.044, ROBE)
    b.rod(wr - d * 0.014, wr + d * 0.004, 0.056, ROBE_DARK)
    b.rod(wr - d * 0.004, wr + d * 0.006, 0.058, GOLD)
    b.ball(wr.x, wr.y, wr.z - 0.034, 0.02, 0.034, SKIN, sides=6)
    if k == -1:
        # the staff, taller than he is: a straight shaft bound in gold, a forked head round a crystal
        x, y = wr.x - 0.006, wr.y - 0.016
        b.rod((x, y, 0.0), (x, y, 0.47), 0.016, STAFF)
        for z in (0.22, 0.3, 0.44):
            b.box(x - 0.011, x + 0.011, y - 0.011, y + 0.011, z, z + 0.012, GOLD)
        for s in (-1, 1):
            b.rod((x, y, 0.47), (x + s * 0.028, y, 0.51), 0.012, STAFF_DARK)
            b.rod((x + s * 0.028, y, 0.51), (x + s * 0.016, y, 0.56), 0.01, STAFF_DARK)
        b.ball(x, y, 0.478, 0.022, 0.07, GEM, sides=6)


def head(b):
    z = 0.425
    b.cylinder(0, 0.0, 0.39, 0.432, 0.025, SKIN, sides=6)
    b.box(-0.042, 0.042, -0.044, 0.041, z - 0.004, z + 0.09, SKIN, bevel=0.018)
    b.box(-0.007, 0.007, -0.056, -0.043, z + 0.026, z + 0.05, SKIN, bevel=0.004)
    for k in (-1, 1):
        b.box(k * 0.046 - 0.007, k * 0.046 + 0.007, -0.009, 0.019, z + 0.024, z + 0.058, SKIN, bevel=0.003)
        # keen eyes under level dark brows
        b.box(k * 0.019 - 0.009, k * 0.019 + 0.009, -0.0475, -0.0435, z + 0.048, z + 0.059, EYE)
        b.box(k * 0.017 - 0.0045, k * 0.017 + 0.0045, -0.0485, -0.046, z + 0.049, z + 0.058, PUPIL)
        b.box(k * 0.021 - 0.014, k * 0.021 + 0.014, -0.049, -0.043, z + 0.064, z + 0.071, HAIR, bevel=0.002)
        # dark hair to the jaw under the hat
        b.box(k * 0.045 - 0.006, k * 0.045 + 0.006, -0.012, 0.046, z + 0.02, z + 0.085, HAIR, bevel=0.004)
    b.box(-0.046, 0.046, 0.02, 0.048, z + 0.0, z + 0.085, HAIR, bevel=0.01)
    # a short, neat dark beard and moustache
    b.box(-0.036, 0.036, -0.05, -0.016, z - 0.026, z + 0.024, HAIR, bevel=0.01)
    b.obox((0, -0.052, z + 0.024), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.022, 0.005, 0.005, HAIR, bevel=0.002)
    b.box(-0.01, 0.01, -0.0535, -0.05, z + 0.01, z + 0.015, 'roof_red_dark')
    # the hat: a broad brim, a gold band, a tall cone bent back at the tip
    b.frustum(0, 0.004, z + 0.088, z + 0.097, 0.1, 0.094, ROBE_DARK, sides=10)
    b.frustum(0, 0.004, z + 0.097, z + 0.118, 0.054, 0.05, GOLD, sides=10, cap=False)
    b.frustum(0, 0.004, z + 0.097, z + 0.19, 0.052, 0.026, ROBE, sides=10)
    b.frustum(0, 0.016, z + 0.19, z + 0.235, 0.026, 0.004, ROBE, sides=8)


PARTS = [("hips", waist), ("chest", chest), ("head", head)] + fg.limbs(
    {"thigh": thigh, "shin": shin, "foot": foot, "upper_arm": upper_arm, "forearm": forearm})


def make(location=(0, 0, 0)):
    return fg.make(OBJ, PARTS, SCALE, location)


if __name__ == "__main__":
    make()
