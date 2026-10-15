"""Granary1-lvl3 -- the Larder (Кладовая) at its last upgrade. Footprint 2x1 (level 1's).
Front is -Y.

Level 2 (granary1_lvl2.build) with a louvred vent turret on the ridge to keep the food dry,
gold finials on the ridge ends, and the front paved where the baskets stand.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import L_TOP  # noqa: E402

OBJ = "Granary1-lvl3"
L2 = kit.borrow("granary1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    x0, x1, y0, y1 = -0.82, 0.82, -0.28, 0.34
    yc, hs = (y0 + y1) / 2, (y1 - y0) / 2
    zr = L_TOP + hs * d.PITCH
    d.louvre(b, 0.3, yc, zr - 0.04, half=0.09, h=0.26)
    for ex in (x0 - 0.09, x1 + 0.09):
        d.finial(b, ex, yc, zr + 0.06, h=0.13)
    d.paving(b, -0.9, 0.95, -0.49, y0 - 0.02)
    return b


kit.run(OBJ, build, source_name="Granary1-lvl1", offset=(LVL3_DX, 0, 0), collection="Склады")
