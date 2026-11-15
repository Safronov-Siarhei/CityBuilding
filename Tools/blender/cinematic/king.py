"""Король -- the King for the campaign's cutscenes (mission 2 scene 2 «Гонец у короля», the user 2026-11-15: the
SAME King as the sandbox's playable hero, heroking.py, never named). On Ардан's rig (figure.py) with the cutscene
set's detail, the same dress as the hero so the player knows him:

riding boots with turned-down cuffs, royal purple hose, a long purple tunic with a gold hem and a gold band down
its front, a gold-buckled belt, a gold chain of state with a medallion, an ermine mantle (black-tipped) over the
shoulders, a red cape falling behind to the heels (dark lining at its foot), purple sleeves with gold cuffs and
bare hands (he sits, rises and gives orders: no sword or shield in the throne room); a full brown beard pointed at
the chin, a moustache, brown hair to the collar, and the gold crown with five points, red and blue stones.

    blender --background <Buildings-Size1-Models.blend> --python king.py
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import figure as fg  # noqa: E402

OBJ = "CineKing"
SCALE = 1.12          # a little taller than the captain (1.08): the King stands out among his court

ROYAL, ROYAL_DARK = 'roof_purple', 'roof_purple_dark'
CAPE, CAPE_DARK = 'banner_red', 'roof_red_dark'
GOLD = 'gold'
ERMINE = 'plaster_white'
SPOT = 'window'
SKIN = 'plaster_pink'
HAIR = 'wood_mid'
BOOT, SOLE = 'wood_dark', 'door'
JEWEL_RED, JEWEL_BLUE = 'roof_red', 'roof_blue'
EYE, PUPIL = 'white', 'window'
fg.plain(ROYAL, ROYAL_DARK, CAPE, CAPE_DARK, GOLD, ERMINE, SKIN, HAIR, BOOT, SOLE, JEWEL_RED, JEWEL_BLUE, EYE, PUPIL)


def foot(b, k):
    x = k * fg.HIP_X
    b.box(x - 0.03, x + 0.03, -0.06, 0.03, 0.0, 0.012, SOLE, bevel=0.004)
    b.box(x - 0.028, x + 0.028, -0.056, 0.028, 0.012, 0.036, BOOT, bevel=0.009)


def shin(b, k):
    """The riding boot's shaft and its turned-down cuff."""
    x = k * fg.HIP_X
    b.box(x - 0.026, x + 0.026, -0.026, 0.026, 0.03, 0.09, BOOT, bevel=0.008)
    b.box(x - 0.031, x + 0.031, -0.031, 0.031, 0.078, 0.1, HAIR, bevel=0.005)           # the cuff
    b.box(x - 0.022, x + 0.022, -0.022, 0.022, 0.095, 0.112, ROYAL_DARK, bevel=0.006)


def thigh(b, k):
    x = k * fg.HIP_X
    b.box(x - 0.023, x + 0.023, -0.023, 0.023, 0.105, 0.2, ROYAL_DARK, bevel=0.009)     # the hose


def waist(b):
    """The tunic's long skirt (below the knee) with its gold hem and the gold band down the front; the belt."""
    b.frustum(0, 0, 0.088, 0.255, 0.098, 0.082, ROYAL, sides=12)
    b.frustum(0, 0, 0.086, 0.104, 0.1, 0.097, GOLD, sides=12, cap=False)
    b.box(-0.013, 0.013, -0.098, -0.087, 0.095, 0.25, GOLD)
    b.cylinder(0, 0, 0.243, 0.266, 0.086, BOOT, sides=12)
    b.box(-0.02, 0.02, -0.094, -0.083, 0.24, 0.27, GOLD, bevel=0.004)                   # the buckle
    b.box(-0.009, 0.009, -0.097, -0.093, 0.248, 0.262, JEWEL_RED)


def chest(b):
    """The tunic's chest, the chain of state and medallion, the ermine mantle and the red cape to the heels."""
    b.obox((0, -0.002, 0.33), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.084, 0.06, 0.064, ROYAL, bevel=0.022)
    b.box(-0.012, 0.012, -0.064, -0.056, 0.27, 0.37, GOLD)                              # the band up the front
    # the chain of state: two runs over the chest to a medallion with a blue stone
    for k in (-1, 1):
        b.rod((k * 0.055, -0.062, 0.38), (k * 0.012, -0.068, 0.322), 0.007, GOLD)
    b.cylinder(0, -0.07, 0.296, 0.324, 0.016, GOLD, sides=8)
    b.box(-0.007, 0.007, -0.075, -0.071, 0.304, 0.316, JEWEL_BLUE)
    # the ermine mantle over the shoulders, black tips scattered over it
    b.obox((0, 0.004, 0.384), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.118, 0.078, 0.026, ERMINE, bevel=0.02)
    for x, y in ((-0.08, -0.068), (-0.02, -0.077), (0.04, -0.073), (0.088, -0.058),
                 (-0.1, 0.0), (0.1, 0.01), (-0.05, 0.075), (0.05, 0.08)):
        b.box(x - 0.007, x + 0.007, y - 0.005, y + 0.005, 0.406, 0.413, SPOT)
    # the cape from under the mantle down the back to the heels, leaning back to clear the skirt; dark lining edge
    lean = (0, -0.12, 1)
    b.prism([(-0.112, 0.39), (0.112, 0.39), (0.15, 0.015), (0.06, 0.0), (-0.06, 0.0), (-0.15, 0.015)],
            (0, 0.11, 0), (1, 0, 0), lean, 0.014, CAPE, back_cap=True)
    b.prism([(-0.152, 0.03), (0.152, 0.03), (0.148, 0.0), (-0.148, 0.0)],
            (0, 0.112, 0), (1, 0, 0), lean, 0.018, CAPE_DARK, back_cap=True)
    b.cylinder(0, 0, 0.39, 0.406, 0.05, ROYAL_DARK, sides=8)                            # the collar


def upper_arm(b, k):
    sh, el, _ = fg.joints(k)
    b.rod(sh, el, 0.044, ROYAL)


def forearm(b, k):
    """Purple sleeves with a gold cuff, bare hands with a ring."""
    _, el, wr = fg.joints(k)
    d = (wr - el).normalized()
    b.rod(el, wr, 0.041, ROYAL)
    b.rod(wr - d * 0.026, wr - d * 0.012, 0.047, GOLD)
    b.ball(wr.x, wr.y, wr.z - 0.03, 0.018, 0.03, SKIN, sides=6)
    if k > 0:
        b.ball(wr.x, wr.y, wr.z - 0.03, 0.0195, 0.006, GOLD, sides=6)                   # the signet ring


def head(b):
    """Face, the full brown beard and moustache, hair to the collar, the five-pointed crown with its stones."""
    z = 0.425
    b.cylinder(0, 0.0, 0.39, 0.432, 0.025, SKIN, sides=6)
    b.box(-0.043, 0.043, -0.045, 0.041, z - 0.004, z + 0.088, SKIN, bevel=0.018)
    b.box(-0.008, 0.008, -0.058, -0.044, z + 0.03, z + 0.054, SKIN, bevel=0.004)        # the nose
    for k in (-1, 1):
        b.box(k * 0.046 - 0.007, k * 0.046 + 0.007, -0.009, 0.019, z + 0.028, z + 0.056, SKIN, bevel=0.003)
        b.box(k * 0.019 - 0.009, k * 0.019 + 0.009, -0.0475, -0.0435, z + 0.05, z + 0.06, EYE)
        b.box(k * 0.017 - 0.0045, k * 0.017 + 0.0045, -0.0485, -0.046, z + 0.051, z + 0.059, PUPIL)
        b.box(k * 0.021 - 0.015, k * 0.021 + 0.015, -0.05, -0.043, z + 0.064, z + 0.072, HAIR, bevel=0.002)  # brows
    # the full beard from ear to ear, pointed at the chin; the moustache over it
    b.box(-0.047, 0.047, -0.054, 0.004, z - 0.012, z + 0.03, HAIR, bevel=0.012)
    b.prism([(-0.032, z - 0.006), (0.032, z - 0.006), (0.0, z - 0.05)], (0, -0.056, 0), (1, 0, 0), (0, 0, 1), 0.036,
            HAIR, back_cap=True)
    b.box(-0.028, 0.028, -0.057, -0.049, z + 0.022, z + 0.032, HAIR, bevel=0.003)
    b.box(-0.011, 0.011, -0.0565, -0.053, z + 0.012, z + 0.017, 'roof_red_dark')       # the mouth
    # hair under the crown and to the collar behind
    b.box(-0.047, 0.047, -0.018, 0.047, z + 0.05, z + 0.096, HAIR, bevel=0.014)
    b.box(-0.046, 0.046, 0.016, 0.051, z - 0.012, z + 0.07, HAIR, bevel=0.012)
    # the crown: a gold band, a purple cap inside it, five points each with a stone
    b.cylinder(0, -0.002, z + 0.088, z + 0.118, 0.052, GOLD, sides=10)
    b.dome(0, -0.002, z + 0.112, 0.046, 0.032, [ROYAL, ROYAL_DARK], sides=10)
    for i in range(5):
        a = -math.pi / 2 + i * 2 * math.pi / 5
        px, py = math.cos(a) * 0.048, -0.002 + math.sin(a) * 0.048
        b.pyramid(px, py, z + 0.118, 0.011, 0.011, z + 0.154, GOLD)
        stone = JEWEL_RED if i % 2 == 0 else JEWEL_BLUE
        b.box(px * 1.06 - 0.006, px * 1.06 + 0.006, py * 1.06 - 0.006, py * 1.06 + 0.006, z + 0.098, z + 0.11, stone)


PARTS = [("hips", waist), ("chest", chest), ("head", head)] + fg.limbs(
    {"thigh": thigh, "shin": shin, "foot": foot, "upper_arm": upper_arm, "forearm": forearm})


def make(location=(0, 0, 0)):
    return fg.make(OBJ, PARTS, SCALE, location)


if __name__ == "__main__":
    make()
