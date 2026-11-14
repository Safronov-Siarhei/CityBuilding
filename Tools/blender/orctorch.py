"""OrcTorchbearer -- the raid's fire-raiser (the user, 2026-11-14: sandbox fires v2; 1-2 per raid from the
player's Town Hall level 2): runs to the nearest burnable building, 3 s to light it, then the next.
Facing -y, standing on the origin. DRAFT for approval -- not exported, not in the game.

A lean grunt in a hide vest and a scorched leather apron, a rag-wrapped head (no helmet: he works by
fire, not by fighting), a burning torch held high in the right fist, a clay pot of pitch with a
dripping lip in the left, spare rag torches stuck in his belt. Shorter and lighter than the grunt so
he reads at once in a raid as «the one with the fire».
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402

OBJ = "OrcTorchbearer"


def build(lo, hi):
    b = Builder()
    # legs: boots, wraps, knees
    for k in (-1, 1):
        x = k * 0.058
        b.box(x - 0.04, x + 0.04, -0.055, 0.035, 0.0, 0.042, o.TIMBER, bevel=0.01)
        b.box(x - 0.037, x + 0.037, -0.036, 0.036, 0.042, 0.115, o.HIDE, bevel=0.012)
        b.box(x - 0.034, x + 0.034, -0.033, 0.033, 0.115, 0.18, o.SKIN, bevel=0.012)

    # hips, belt, a scorched leather apron to the knees
    b.box(-0.1, 0.1, -0.06, 0.06, 0.16, 0.22, o.HIDE, bevel=0.014)
    b.box(-0.105, 0.105, -0.066, 0.066, 0.2, 0.228, o.TIMBER, bevel=0.008)
    b.box(-0.07, 0.07, -0.078, -0.066, 0.1, 0.22, 'wood_dark')
    b.box(-0.06, 0.06, -0.08, -0.077, 0.1, 0.13, o.RUST)     # the scorched hem
    # spare rag torches in the belt
    for dx in (0.07, 0.095):
        b.rod((dx, 0.04, 0.16), (dx + 0.015, 0.05, 0.3), 0.012, o.TIMBER)
        b.cylinder(dx + 0.015, 0.05, 0.28, 0.32, 0.018, 'burlap', sides=5)

    # a lean torso under a hide vest, open at the front
    b.obox((0, -0.005, 0.3), (1, 0, 0), (0, 0.97, 0.24), (0, -0.24, 0.97), 0.1, 0.066, 0.08,
           o.SKIN, bevel=0.022)
    for k in (-1, 1):
        b.obox((k * 0.06, 0.0, 0.31), (1, 0, 0), (0, 0.97, 0.24), (0, -0.24, 0.97), 0.045, 0.072, 0.085,
               o.HIDE, bevel=0.016)
    b.rod((0.09, -0.08, 0.39), (-0.08, -0.07, 0.24), 0.02, o.RUST)   # the pot's sling

    # head: wrapped in a sooty rag, the jaw and tusks bare, glowing eyes
    hy = -0.055
    o.orc_head(b, hy, z=-0.02)
    b.box(-0.07, 0.07, hy - 0.05, hy + 0.065, 0.455, 0.505, 'burlap', bevel=0.016)
    o.tube(b, [(0.03, hy + 0.06, 0.49), (0.06, hy + 0.12, 0.47), (0.05, hy + 0.16, 0.42)],
           [0.022, 0.016, 0], 'burlap', sides=4)

    # right arm raised high, the torch burning over his head
    b.obox((0.14, -0.02, 0.4), (1, 0, 0), (0, 0.97, 0.24), (0.2, -0.24, 0.95), 0.034, 0.036, 0.075,
           o.SKIN, bevel=0.014)
    b.ball(0.16, -0.04, 0.48, 0.035, 0.05, o.SKIN, sides=6)
    b.rod((0.16, -0.035, 0.42), (0.17, -0.06, 0.64), 0.016, o.TIMBER)
    b.cylinder(0.17, -0.06, 0.62, 0.665, 0.03, 'burlap', sides=6)
    # the flame: a red root, three orange tongues licking up and back, a yellow heart
    b.blob(0.17, -0.06, 0.64, 0.055, 0.07, 'banner_red', sides=6)
    for dx, dy, h, r in ((0.0, 0.0, 0.17, 0.045), (0.03, 0.02, 0.12, 0.03), (-0.028, 0.015, 0.11, 0.028),
                         (0.005, -0.03, 0.1, 0.028)):
        b.blob(0.17 + dx, -0.06 + dy, 0.655, r, h, o.FIRE, sides=5)
    b.blob(0.17, -0.062, 0.66, 0.026, 0.09, 'gold', sides=4)
    b.blob(0.18, -0.03, 0.8, 0.012, 0.03, o.FIRE, sides=4)   # a spark above

    # left arm low and forward: a clay pot of pitch, its lip black and dripping
    b.obox((-0.15, -0.05, 0.29), (1, 0, 0), (0, 0.6, 0.8), (0, -0.8, 0.6), 0.034, 0.07, 0.036,
           o.SKIN, bevel=0.014)
    b.ball(-0.15, -0.12, 0.25, 0.034, 0.05, o.SKIN, sides=6)
    b.ball(-0.15, -0.15, 0.13, 0.06, 0.1, 'roof_orange_dark', sides=7)   # a clay pot
    b.cylinder(-0.15, -0.15, 0.225, 0.245, 0.04, 'wood_dark', sides=7)
    b.box(-0.158, -0.142, -0.205, -0.195, 0.16, 0.23, 'wood_dark')
    return b


o.ensure_collection()
kit.run(OBJ, build, source_name="Flag1-lvl1", offset=(o.COLUMN_X + 2.4, 0, 0),
        collection=o.COLLECTION)
