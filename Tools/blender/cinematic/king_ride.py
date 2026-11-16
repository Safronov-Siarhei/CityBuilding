"""Король верхом -- the King for mission 2's last scene «Подкрепление» (the user, 2026-11-16: «по мосту несётся
король и огромное количество кавалерии»): the SAME dress as the approved CineKing (king.py) so the player knows
him, cut for the saddle -- the tunic's skirt split (the frustum skirt ran through the horse), the red cape shorter,
blown back over the horse's croup -- and a drawn SWORD in the right hand. Renders first (cavalry.py render).
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import figure as fg  # noqa: E402
import king as k_  # noqa: E402
from mathutils import Vector  # noqa: E402

OBJ = "CineKingRide"
SCALE = k_.SCALE
STEEL, STEEL_DARK = 'stone_light', 'iron'
fg.plain(STEEL, STEEL_DARK)


def waist(b):
    """The tunic split for the saddle, gold hems and the gold band; the belt with its buckle."""
    for k in (-1, 1):
        b.obox((k * 0.044, 0, 0.2), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.046, 0.088, 0.058, k_.ROYAL, bevel=0.012)
        b.box(k * 0.044 - 0.046, k * 0.044 + 0.046, -0.089, 0.089, 0.138, 0.15, k_.GOLD)
    b.box(-0.013, 0.013, -0.093, -0.086, 0.15, 0.25, k_.GOLD)
    b.cylinder(0, 0, 0.243, 0.266, 0.086, k_.BOOT, sides=12)
    b.box(-0.02, 0.02, -0.094, -0.083, 0.24, 0.27, k_.GOLD, bevel=0.004)
    b.box(-0.009, 0.009, -0.097, -0.093, 0.248, 0.262, k_.JEWEL_RED)
    # the sword's empty scabbard on the left hip
    b.rod((0.094, -0.01, 0.25), (0.118, 0.05, 0.1), 0.018, k_.CAPE_DARK)
    b.rod((0.116, 0.046, 0.108), (0.118, 0.05, 0.1), 0.02, k_.GOLD)


def chest(b):
    """king.chest with the cape cut short and blown back: the tunic, the chain and stone, the ermine, the cape."""
    b.obox((0, -0.002, 0.33), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.084, 0.06, 0.064, k_.ROYAL, bevel=0.022)
    b.box(-0.012, 0.012, -0.064, -0.056, 0.27, 0.37, k_.GOLD)
    for k in (-1, 1):
        b.rod((k * 0.05, -0.06, 0.382), (k * 0.004, -0.068, 0.352), 0.005, k_.GOLD)
    b.box(-0.006, 0.006, -0.073, -0.066, 0.346, 0.354, k_.GOLD, bevel=0.002)
    r = 0.7071
    b.obox((0, -0.07, 0.333), (r, 0, r), (0, 1, 0), (-r, 0, r), 0.012, 0.004, 0.012, k_.STONE, bevel=0.002)
    b.box(-0.0018, 0.0018, -0.0755, -0.073, 0.324, 0.342, k_.RUNE)
    b.obox((0, 0.004, 0.384), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.118, 0.078, 0.026, k_.ERMINE, bevel=0.02)
    for x, y in ((-0.08, -0.068), (-0.02, -0.077), (0.04, -0.073), (0.088, -0.058),
                 (-0.1, 0.0), (0.1, 0.01), (-0.05, 0.075), (0.05, 0.08)):
        b.box(x - 0.007, x + 0.007, y - 0.005, y + 0.005, 0.406, 0.413, k_.SPOT)
    # the cape: from under the mantle back over the croup, streaming in the gallop's wind
    up = Vector((0, -0.3, 0.95)).normalized()             # from the cape's foot up to the shoulders: it hangs back
    foot = Vector((0, 0.07, 0.39)) - up * 0.27
    b.prism([(-0.112, 0.27), (0.112, 0.27), (0.16, 0.02), (0.06, 0.0), (-0.06, 0.0), (-0.16, 0.02)],
            tuple(foot), (1, 0, 0), tuple(up), 0.014, k_.CAPE, back_cap=True)
    b.cylinder(0, 0, 0.39, 0.406, 0.05, k_.ROYAL_DARK, sides=8)


def forearm(b, k):
    """king.forearm; in the right hand (k < 0) a drawn sword along the hand's line, gold cross-guard."""
    k_.forearm(b, k)
    if k < 0:
        _, el, wr = fg.joints(k)
        d = (wr - el).normalized()
        hand = wr + d * 0.03
        b.rod(hand - d * 0.035, hand + d * 0.02, 0.018, k_.CAPE_DARK)                    # the grip
        b.ball((hand - d * 0.04).x, (hand - d * 0.04).y, (hand - d * 0.04).z, 0.012, 0.016, k_.GOLD, sides=6)
        g = hand + d * 0.028
        b.rod(g - Vector((0.04, 0, 0)), g + Vector((0.04, 0, 0)), 0.012, k_.GOLD)        # the guard
        b.rod(g, g + d * 0.36, 0.022, STEEL)                                              # the blade
        b.rod(g + d * 0.36, g + d * 0.4, 0.008, STEEL_DARK)


PARTS = [("hips", waist), ("chest", chest), ("head", k_.head)] + fg.limbs(
    {"thigh": k_.thigh, "shin": k_.shin, "foot": k_.foot, "upper_arm": k_.upper_arm, "forearm": forearm})


def make(location=(0, 0, 0)):
    return fg.make(OBJ, PARTS, SCALE, location)
