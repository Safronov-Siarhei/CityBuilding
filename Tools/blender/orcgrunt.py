"""OrcGrunt -- the orc rank-and-file raider, facing -y, standing on the origin. orcs.ORC_H tall
(0.58 m against a citizen's 0.45).

Hunched and broad: short hide-wrapped legs, a rust loincloth and a war belt, a green torso
wider than it is deep, iron pauldrons with bone spikes, a heavy jaw with tusks, a black
topknot, iron bracers, and a one-handed axe held ready in the right hand.

Not a building: it borrows the Flag's box and stands in the orc column. No animation (the
user's decision 2026-10-14) -- the game moves it whole.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402

OBJ = "OrcGrunt"


def build(lo, hi):
    b = Builder()
    # legs: boots, hide wraps, a bit of green knee
    for k in (-1, 1):
        x = k * 0.065
        b.box(x - 0.045, x + 0.045, -0.06, 0.04, 0.0, 0.045, o.TIMBER, bevel=0.01)
        b.box(x - 0.042, x + 0.042, -0.04, 0.04, 0.045, 0.12, o.HIDE, bevel=0.012)
        b.box(x - 0.038, x + 0.038, -0.036, 0.036, 0.12, 0.185, o.SKIN, bevel=0.012)

    # hips, belt, loincloth
    b.box(-0.11, 0.11, -0.065, 0.065, 0.16, 0.22, o.HIDE, bevel=0.014)
    b.box(-0.115, 0.115, -0.07, 0.07, 0.205, 0.235, o.TIMBER, bevel=0.008)
    b.box(-0.055, 0.055, -0.08, -0.07, 0.09, 0.22, o.RUST)
    b.box(-0.05, 0.05, 0.07, 0.08, 0.1, 0.22, o.RUST)
    o.skull(b, 0, -0.08, 0.2, s=0.045)

    # torso: a barrel chest wider at the top, leaning forward
    b.obox((0, -0.005, 0.3), (1, 0, 0), (0, 0.97, 0.24), (0, -0.24, 0.97), 0.12, 0.075, 0.08,
           o.SKIN, bevel=0.025)
    b.obox((0, -0.02, 0.37), (1, 0, 0), (0, 0.97, 0.24), (0, -0.24, 0.97), 0.14, 0.08, 0.05,
           o.SKIN, bevel=0.025)
    # a rust strap across the chest
    b.rod((-0.12, -0.1, 0.4), (0.1, -0.085, 0.24), 0.028, o.RUST)

    # pauldrons with bone spikes
    for k in (-1, 1):
        x = k * 0.155
        b.ball(x, -0.02, 0.33, 0.075, 0.1, o.METAL, sides=6)
        o.horn(b, (x, -0.02, 0.42), (k, 0, 0), 0.07, 0.016, up=1.4)

    # head: sunk between the shoulders, jutting forward, heavy jaw, tusks, brow, topknot
    hy = -0.06
    b.box(-0.062, 0.062, hy - 0.06, hy + 0.055, 0.41, 0.52, o.SKIN, bevel=0.02)
    b.box(-0.074, 0.074, hy - 0.072, hy + 0.03, 0.39, 0.445, o.SKIN_LIGHT, bevel=0.015)
    b.box(-0.066, 0.066, hy - 0.072, hy - 0.05, 0.47, 0.49, o.HAIR)
    for k in (-1, 1):
        b.box(k * 0.03 - 0.014, k * 0.03 + 0.014, hy - 0.066, hy - 0.058, 0.452, 0.466, o.FIRE)
        o.horn(b, (k * 0.045, hy - 0.07, 0.432), (k * 0.2, -1, 0), 0.04, 0.012, up=1.5)
        b.box(k * 0.07 - 0.012, k * 0.07 + 0.012, hy - 0.01, hy + 0.02, 0.45, 0.49, o.SKIN_LIGHT)
    o.tube(b, [(0, hy + 0.035, 0.515), (0, hy + 0.06, o.ORC_H - 0.02), (0, hy + 0.1, o.ORC_H)],
           [0.026, 0.02, 0], o.HAIR, sides=5)

    # arms: thick upper arms, iron bracers, fists
    for k in (-1, 1):
        x = k * 0.19
        b.obox((x, -0.02, 0.29), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.04, 0.042, 0.06, o.SKIN,
               bevel=0.015)
        b.box(x - 0.038, x + 0.038, -0.07, 0.0, 0.17, 0.235, o.METAL, bevel=0.01)
        b.ball(x, -0.05, 0.13, 0.04, 0.06, o.SKIN, sides=6)

    # the axe in the right fist: haft up and forward, a bearded iron head with a bright edge
    b.rod((0.19, 0.0, 0.07), (0.2, -0.17, 0.4), 0.022, o.TIMBER)
    hx = 0.205
    b.prism([(-0.13, 0.33), (-0.15, 0.4), (-0.24, 0.44), (-0.26, 0.31), (-0.2, 0.27)],
            (hx - 0.008, 0, 0), (0, 1, 0), (0, 0, 1), 0.016, o.METAL, back_cap=True)
    b.prism([(-0.235, 0.44), (-0.258, 0.44), (-0.278, 0.31), (-0.255, 0.31)],
            (hx - 0.006, 0, 0), (0, 1, 0), (0, 0, 1), 0.012, 'stone_light', back_cap=True)
    b.box(hx - 0.016, hx + 0.016, -0.17, -0.13, 0.38, 0.42, o.RUST)
    return b


def round_shield(b, x, y, z, r=0.1):
    """A round wooden shield on the left forearm, facing out (-x), a rust rim and an iron boss."""
    pts = [(y + r * math.cos(2 * math.pi * i / 10), z + r * math.sin(2 * math.pi * i / 10))
           for i in range(10)]
    b.prism(pts, (x, 0, 0), (0, 1, 0), (0, 0, 1), 0.022, o.TIMBER_LIGHT, back_cap=True)
    for i in range(10):
        a0, a1 = 2 * math.pi * i / 10, 2 * math.pi * (i + 1) / 10
        b.rod((x - 0.002, y + r * math.cos(a0), z + r * math.sin(a0)),
              (x - 0.002, y + r * math.cos(a1), z + r * math.sin(a1)), 0.018, o.RUST)
    b.ball(x - 0.002, y, z - 0.03, 0.03, 0.06, o.METAL, sides=6)


def build2(lo, hi):
    """Level 2: an iron cap with a nasal over the topknot, a round shield on the left arm and
    an iron plate on the chest strap."""
    b = build(lo, hi)
    hy = -0.06
    b.box(-0.068, 0.068, hy - 0.066, hy + 0.06, 0.48, 0.53, o.METAL, bevel=0.015)
    b.box(-0.012, 0.012, hy - 0.078, hy - 0.062, 0.43, 0.51, o.METAL)
    b.box(-0.07, 0.07, hy - 0.068, hy + 0.062, 0.475, 0.49, o.RUST)
    round_shield(b, -0.255, -0.03, 0.22)
    b.obox((0.0, -0.11, 0.33), (0.7, 0, -0.7), (0, 1, 0), (0.7, 0, 0.7), 0.05, 0.008, 0.04,
           o.METAL)
    return b


def build3(lo, hi):
    """Level 3: a horned iron helm, an iron breastplate, a rust cape, greaves and a second
    blade on the axe."""
    b = build2(lo, hi)
    hy = -0.06
    b.box(-0.072, 0.072, hy - 0.07, hy + 0.064, 0.5, 0.56, o.METAL, bevel=0.018)
    for k in (-1, 1):
        o.horn(b, (k * 0.068, hy, 0.53), (k, 0.1, 0), 0.1, 0.02, up=1.4, segments=4)
    b.obox((0, -0.085, 0.31), (1, 0, 0), (0, 0.97, 0.24), (0, -0.24, 0.97), 0.11, 0.012, 0.075,
           o.METAL, bevel=0.01)
    b.obox((0, -0.098, 0.31), (1, 0, 0), (0, 0.97, 0.24), (0, -0.24, 0.97), 0.02, 0.004, 0.065,
           o.RUST)
    b.prism([(-0.13, 0.42), (0.13, 0.42), (0.16, 0.1), (0.05, 0.13), (-0.05, 0.1), (-0.16, 0.12)],
            (0, 0.075, 0), (1, 0, 0), (0, 0, 1), 0.012, o.RUST, back_cap=True)
    for k in (-1, 1):
        b.box(k * 0.065 - 0.044, k * 0.065 + 0.044, -0.065, -0.03, 0.05, 0.13, o.METAL)
    hx = 0.205
    b.prism([(-0.13, 0.33), (-0.15, 0.4), (-0.24, 0.44), (-0.26, 0.31), (-0.2, 0.27)],
            (hx - 0.008, -0.34, 0), (0, -1, 0), (0, 0, 1), 0.016, o.METAL, back_cap=True)
    return b


o.ensure_collection()
kit.run(OBJ, build, source_name="Flag1-lvl1", offset=(o.COLUMN_X, 0, 0), collection=o.COLLECTION,
        save=False)
o.place("OrcGrunt-lvl2", build2, "Flag1-lvl1", o.GRID_X[2], -55.0)
o.place("OrcGrunt-lvl3", build3, "Flag1-lvl1", o.GRID_X[3], -55.0)
import bpy  # noqa: E402
bpy.ops.wm.save_mainfile()
