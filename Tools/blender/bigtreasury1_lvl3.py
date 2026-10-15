"""BigTreasury1-lvl3 -- the Big Treasury (Большая сокровищница) at its last upgrade. Footprint
4x3 (level 1's). Front -Y.

Level 2 (bigtreasury1_lvl2.build) with the towers raised again (RAISE 0.3 -> 0.55), gold finials
on the strongroom's ridge ends, and two gilded guardians on plinths either side of the path.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import U_TOP  # noqa: E402

OBJ = "BigTreasury1-lvl3"
L2 = kit.borrow("bigtreasury1_lvl2.py")
L2['RAISE'] = 0.55        # level 2's build reads it as a global
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    x0, x1, y0, y1 = -1.2, 1.2, -0.3, 1.2
    yc, hs = (y0 + y1) / 2, (y1 - y0) / 2
    zr = U_TOP + hs * d.PITCH
    for ex in (x0 - 0.1, x1 + 0.1):
        d.finial(b, ex, yc, zr + 0.06, h=0.2)
    for sx in (-1, 1):
        d.statue(b, sx * 0.72, -1.1, 0, scale=1.1)
    return b


kit.run(OBJ, build, source_name="BigTreasury1-lvl1", offset=(LVL3_DX, 0, 0),
        collection="Склады")
