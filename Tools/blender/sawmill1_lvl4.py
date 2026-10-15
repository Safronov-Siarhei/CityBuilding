"""Sawmill1-lvl4 -- the Sawmill (Лесопилка) at level 4. Footprint 2x2 (level 1's). Front -Y.

Level 3 (sawmill1_lvl3.build) with its roof re-laid in glazed tiles -- a lattice of dark slate
and gold diamonds over the slate -- a gilded crest along the ridge, and a guild pennant on a tall
pole at the yard's corner.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import L_TOP  # noqa: E402

OBJ = "Sawmill1-lvl4"
L3 = kit.borrow("sawmill1_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5


def build(lo, hi):
    b = L3['build'](lo, hi)
    x1, y0, y1 = 0.84, 0.3, 0.86
    yc, hs = (y0 + y1) / 2, (y1 - y0) / 2
    d.glazed(b, 0, yc, 'x', x1 + 0.09, hs, L_TOP, 'roof_slate_dark')
    b.box(-0.92, -0.84, 0.12, 0.2, 0, 0.05, 'stone_mid')
    d.pennant(b, -0.88, 0.16, 0.05, h=1.15, color='banner_red', length=0.2)
    return b


kit.run(OBJ, build, source_name="Sawmill1-lvl1", offset=(LVL4_DX, 0, 0),
        collection="Производственные здания")
