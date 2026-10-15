"""Barn1-lvl3 -- the Barn (Амбар) at its last upgrade. Footprint 1x1 (level 1's). Front -Y.

Level 2 (barn1_lvl2.build) with gold finials on both ends of the ridge, a sack hanging from the
hoist beam on its way up to the loft, and a paved apron before the doors.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import G_TOP, L_TOP  # noqa: E402

OBJ = "Barn1-lvl3"
L2 = kit.borrow("barn1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    x0, x1, y0, y1 = -0.36, 0.36, -0.26, 0.38
    zr = L_TOP + (x1 - x0) / 2 * d.PITCH
    for ey in (y0 - 0.07, y1 + 0.07):
        d.finial(b, 0, ey, zr + 0.06, h=0.13)
    hy = y0 - 0.12
    b.box(-0.005, 0.005, hy - 0.005, hy + 0.005, G_TOP - 0.02, G_TOP + 0.26, 'burlap')
    d.sack(b, 0, hy, G_TOP - 0.13, r=0.06)
    d.paving(b, -0.2, 0.2, -0.48, y0)
    return b


kit.run(OBJ, build, source_name="Barn1-lvl1", offset=(LVL3_DX, 0, 0), collection="Склады")
