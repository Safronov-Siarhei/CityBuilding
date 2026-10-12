"""Warehouse1-lvl1 -- the Warehouse (Склад): timber, stone and ore. Footprint 2x2. Front -Y.

A plank storehouse on a stone footing with wide doors and a loft, red roof, and in front the
yard where the goods wait: a pile of logs, stacked cut stone, a heap of ore. One and a half
standard storeys.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP, L_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "Warehouse1-lvl1"


def build(lo, hi):
    b = Builder()
    x0, x1, y0, y1 = -0.78, 0.78, -0.08, 0.8
    yc, hs = (y0 + y1) / 2, (y1 - y0) / 2
    zr = L_TOP + hs * d.PITCH

    d.footing(b, x0, x1, y0, y1)
    b.box(x0, x1, y0, y1, FOOTING, L_TOP, 'wood_light', bevel=d.WALL_BEVEL)
    for nm, plane, s0, s1 in (('-y', y0, x0, x1), ('-x', x0, y0, y1), ('+x', x1, y0, y1)):
        f = Facade(b, nm, plane)
        a = s0 + 0.12
        while a < s1 - 0.08:
            f.rect(a - 0.008, a + 0.008, FOOTING, L_TOP, 0.006, 'wood_mid')
            a += 0.24
    for px in (x0, x1):
        for py in (y0, y1):
            b.box(px - 0.04, px + 0.04, py - 0.04, py + 0.04, FOOTING, L_TOP, 'wood_dark')
    b.gable_block('x', x0, x1, y0, y1, L_TOP, zr, 'wood_mid')
    d.roof(b, 0, yc, 'x', x1 + 0.09, hs, L_TOP, **d.RED)
    d.chimney(b, -0.5, 0.55, 1.0, zr + 0.08)

    f = Facade(b, '-y', y0)
    d.barn_door(f, -0.15, color='door')
    d.hay_loft(f, 0.45, G_TOP + 0.02, w=0.2, h=0.2)
    d.plain_window(Facade(b, '+x', x1), yc, FOOTING)
    d.plain_window(Facade(b, '-x', x0), yc, FOOTING)

    b.box(-0.4, 0.1, -0.98, y0, 0, 0.012, 'sand')
    d.log_pile(b, -0.68, -0.52, length=0.42, axis='y')
    d.stone_pile(b, 0.32, -0.42)
    d.ore_heap(b, 0.62, -0.74)
    d.crate(b, 0.62, -0.24, 0.16, 15)
    return b


kit.run(OBJ, build)
