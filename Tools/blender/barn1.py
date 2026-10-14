"""Barn1-lvl1 -- the Barn (Амбар), grain store. Footprint 1x1. Front is -Y.

A small plank barn with its gable to the street: red X-braced double doors, a hay loft hatch
over them with a hoist beam, a steep red roof, sacks of grain by the door. One and a half
standard storeys (details.L_TOP).
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP, L_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "Barn1-lvl1"


def build(lo, hi):
    b = Builder()
    x0, x1, y0, y1 = -0.36, 0.36, -0.26, 0.38
    hs = (x1 - x0) / 2
    zr = L_TOP + hs * d.PITCH

    d.footing(b, x0, x1, y0, y1)
    b.box(x0, x1, y0, y1, FOOTING, L_TOP, 'wood_light', bevel=d.WALL_BEVEL)
    for px in (x0, x1):
        for py in (y0, y1):
            b.box(px - 0.035, px + 0.035, py - 0.035, py + 0.035, FOOTING, L_TOP, 'wood_dark')
    b.gable_block('y', y0, y1, x0, x1, L_TOP, zr, 'wood_mid')
    d.roof(b, 0, (y0 + y1) / 2, 'y', (y1 - y0) / 2 + 0.09, hs, L_TOP, **d.RED)

    f = Facade(b, '-y', y0)
    d.barn_door(f, 0)
    d.hay_loft(f, 0, G_TOP + 0.02, w=0.2, h=0.2)
    f = Facade(b, '+x', x1)
    f.rect(0.0, 0.12, 0.42, 0.54, 0.01, d.GLASS)

    d.sack(b, -0.3, -0.4)
    d.sack(b, -0.18, -0.41)
    d.sack(b, -0.25, -0.37, 0.13, r=0.06)
    return b


kit.run(OBJ, build)
