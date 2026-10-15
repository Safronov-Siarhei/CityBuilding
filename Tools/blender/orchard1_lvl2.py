"""Orchard1-lvl2 -- the Orchard (Фруктовый сад) after its first upgrade. Footprint 3x3 (level
1's). Front is -Y.

Level 1 with three straw beehives on a bench along the left fence and a cider press with its
barrel near the gate. Level 1, for reference:


Fruit trees in rows on darker grass inside a low rail fence with a gap for the gate: round
crowns hung with red apples and yellow pears. At the front left the picker's hut with a straw
roof; baskets of fruit, a ladder against a tree, crates.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "Orchard1-lvl2"
LVL2_DX = 3.6   # level 2 stands this far +X of level 1


def fruit_tree(b, x, y, fruit, seed=0, r=0.3):
    b.cylinder(x, y, 0, 0.36, 0.05, 'wood_mid', sides=6)
    b.blob(x, y, 0.26, r, r * 1.7, 'leaf', sides=7)
    b.blob(x + 0.08, y - 0.06, 0.5, r * 0.62, r * 1.05, 'grass_dark', sides=6)
    for i in range(6):
        a = math.radians(200 + seed * 23 + i * 61)
        z = 0.26 + r * 1.7 * (0.32 + 0.12 * (i % 3))
        rr = r * (0.92 - 0.1 * (i % 3))
        b.blob(x + rr * math.cos(a), y + rr * math.sin(a), z - 0.035, 0.04, 0.07, fruit, sides=4)


def build(lo, hi):
    b = Builder()
    b.box(-1.42, 1.42, -1.42, 1.42, 0, 0.006, 'grass_dark')
    gate = 0.28
    d.rail_fence(b, [(-gate, -1.44), (-1.44, -1.44), (-1.44, 1.44), (1.44, 1.44), (1.44, -1.44),
                     (gate, -1.44)], post_step=0.36)

    trees = [(-0.95, 0.95), (0.0, 0.95), (0.95, 0.95), (-0.95, 0.1), (0.0, 0.1), (0.95, 0.1),
             (0.5, -0.78), (1.0, -0.82)]
    for i, (x, y) in enumerate(trees):
        fruit_tree(b, x, y, 'roof_red' if i % 3 != 1 else 'plaster_yellow', seed=i)

    # the picker's hut
    x0, x1, y0, y1 = -1.3, -0.62, -1.2, -0.62
    yc, hs = (y0 + y1) / 2, (y1 - y0) / 2
    d.footing(b, x0, x1, y0, y1)
    b.box(x0, x1, y0, y1, FOOTING, G_TOP, 'wood_light', bevel=d.WALL_BEVEL)
    for px in (x0, x1):
        for py in (y0, y1):
            b.box(px - 0.035, px + 0.035, py - 0.035, py + 0.035, FOOTING, G_TOP, 'wood_dark')
    zr = G_TOP + hs * d.PITCH
    b.gable_block('x', x0, x1, y0, y1, G_TOP, zr, 'wood_mid')
    d.roof(b, (x0 + x1) / 2, yc, 'x', (x1 - x0) / 2 + 0.09, hs, G_TOP, **d.STRAW)
    f = Facade(b, '-y', y0)
    d.plank_door(f, -0.96)
    d.step(b, -1.1, -0.82, y0)
    d.shutter_window(Facade(b, '+x', x1), yc, FOOTING)

    # harvest: baskets, crates, a ladder against a tree
    for x, y, c in ((-0.45, -1.05, 'roof_red'), (-0.3, -1.18, 'plaster_yellow'),
                    (-0.35, -0.9, 'roof_red')):
        d.basket(b, x, y, c)
    d.crate(b, -0.5, -0.55, 0.16, 12)
    # beehives on a bench along the left fence
    b.box(-1.36, -1.18, 0.3, 0.74, 0.1, 0.13, 'wood_mid')
    for py in (0.33, 0.74):
        b.box(-1.3, -1.24, py - 0.03, py + 0.03, 0, 0.1, 'wood_dark')
    for y in (0.4, 0.52, 0.64):
        b.ball(-1.27, y, 0.13, 0.055, 0.13, 'roof_straw', sides=6, top='roof_straw_dark')
        b.box(-1.21, -1.2, y - 0.012, y + 0.012, 0.15, 0.17, 'window')
    # a cider press by the gate
    cx, cy = -0.12, -0.72
    b.box(cx - 0.16, cx + 0.16, cy - 0.12, cy + 0.12, 0, 0.12, 'wood_mid', bevel=0.01)
    b.cylinder(cx, cy, 0.12, 0.3, 0.09, 'wood_light', sides=8)
    b.cylinder(cx, cy, 0.2, 0.22, 0.095, 'iron', sides=8)
    for k in (-1, 1):
        b.box(cx + k * 0.13 - 0.02, cx + k * 0.13 + 0.02, cy - 0.02, cy + 0.02, 0.12, 0.5, 'wood_dark')
    b.box(cx - 0.15, cx + 0.15, cy - 0.025, cy + 0.025, 0.46, 0.52, 'wood_dark')
    b.box(cx - 0.012, cx + 0.012, cy - 0.012, cy + 0.012, 0.3, 0.46, 'iron')
    b.box(cx - 0.12, cx + 0.12, cy - 0.01, cy + 0.01, 0.4, 0.42, 'wood_light')
    d.barrel(b, cx + 0.28, cy + 0.02, r=0.07, h=0.18)
    for k in (-1, 1):
        b.rod((0.32 + k * 0.08, -0.55, 0.01), (0.4 + k * 0.08, -0.72, 0.62), 0.025, 'wood_light')
    for t in (0.2, 0.4, 0.6, 0.8):
        b.rod((0.24 + 0.08 * t, -0.55 - 0.17 * t, 0.62 * t), (0.4 + 0.08 * t, -0.55 - 0.17 * t,
                                                                0.62 * t), 0.018, 'wood_light')
    return b


kit.run(OBJ, build, source_name="Orchard1-lvl1", offset=(LVL2_DX, 0, 0),
        collection="Производство еды")
