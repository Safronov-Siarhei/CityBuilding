"""Barn1-lvl2 -- the Barn (Амбар) after its first upgrade. Footprint 1x1 (level 1's). Front -Y.

Level 1 on a dressed stone course, with a weathervane on the ridge, a lantern by the doors and
more grain waiting outside: a second pile of sacks and a crate. Level 1, for reference:


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

OBJ = "Barn1-lvl2"
LVL2_DX = 1.6   # level 2 stands this far +X of level 1


def build(lo, hi):
    b = Builder()
    x0, x1, y0, y1 = -0.36, 0.36, -0.26, 0.38
    hs = (x1 - x0) / 2
    zr = L_TOP + hs * d.PITCH

    d.footing(b, x0, x1, y0, y1)
    b.box(x0 - 0.012, x1 + 0.012, y0 - 0.012, y1 + 0.012, FOOTING, 0.28, 'stone_light',
          bevel=d.WALL_BEVEL)
    b.box(x0, x1, y0, y1, 0.28, L_TOP, 'wood_light', bevel=d.WALL_BEVEL)
    for px in (x0, x1):
        for py in (y0, y1):
            b.box(px - 0.035, px + 0.035, py - 0.035, py + 0.035, 0.28, L_TOP, 'wood_dark')
    b.gable_block('y', y0, y1, x0, x1, L_TOP, zr, 'wood_mid')
    d.roof(b, 0, (y0 + y1) / 2, 'y', (y1 - y0) / 2 + 0.09, hs, L_TOP, **d.RED)
    d.weathervane(b, 0, (y0 + y1) / 2, zr + 0.07, h=0.2)

    f = Facade(b, '-y', y0)
    d.barn_door(f, 0)
    d.hay_loft(f, 0, G_TOP + 0.02, w=0.2, h=0.2)
    d.wall_lantern(f, 0.27, FOOTING + 0.36)
    f = Facade(b, '+x', x1)
    f.rect(0.0, 0.12, 0.42, 0.54, 0.01, d.GLASS)

    d.sack(b, -0.3, -0.4)
    d.sack(b, -0.18, -0.41)
    d.sack(b, -0.25, -0.37, 0.13, r=0.06)
    d.sack(b, 0.26, -0.42)
    d.sack(b, 0.38, -0.36, r=0.06)
    d.crate(b, 0.42, 0.1, 0.13, 20)
    return b


kit.run(OBJ, build, source_name="Barn1-lvl1", offset=(LVL2_DX, 0, 0),
        collection="Склады")
