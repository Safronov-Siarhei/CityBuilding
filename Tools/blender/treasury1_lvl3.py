"""Treasury1-lvl3 -- the Treasury (Сокровищница) at its last upgrade. Footprint 3x2 (level
1's). Front -Y.

Level 2 (treasury1_lvl2.build) crowned with a glazed lantern under a gold cone on the ridge, and
two gilded guardians on plinths either side of the path.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import U_TOP  # noqa: E402

OBJ = "Treasury1-lvl3"
L2 = kit.borrow("treasury1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    y0, y1 = -0.42, 0.78
    yc, hs = (y0 + y1) / 2, (y1 - y0) / 2
    zr = U_TOP + hs * d.PITCH
    d.cupola(b, 0, yc, zr + 0.04, r=0.14, h=0.24, roof='gold')
    for sx in (-1, 1):
        d.statue(b, sx * 0.78, -0.8, 0, scale=0.9)
    return b


kit.run(OBJ, build, source_name="Treasury1-lvl1", offset=(LVL3_DX, 0, 0), collection="Склады")
