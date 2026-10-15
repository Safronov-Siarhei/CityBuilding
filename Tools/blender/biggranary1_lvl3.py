"""BigGranary1-lvl3 -- the Big Larder (Большая кладовая) at its last upgrade. Footprint 3x2
(level 1's). Front -Y.

Level 2 (biggranary1_lvl2.build) with a louvred vent turret on the ridge, gold finials on the
ridge ends, and the whole yard in front of the loading platform paved.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import U_TOP  # noqa: E402

OBJ = "BigGranary1-lvl3"
L2 = kit.borrow("biggranary1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    x0, x1, y0, y1 = -1.3, 1.3, -0.12, 0.84
    yc, hs = (y0 + y1) / 2, (y1 - y0) / 2
    zr = U_TOP + hs * d.PITCH
    d.louvre(b, -0.5, yc, zr - 0.04)
    for ex in (x0 - 0.09, x1 + 0.09):
        d.finial(b, ex, yc, zr + 0.06)
    d.paving(b, -1.4, 1.4, -0.98, -0.5)
    return b


kit.run(OBJ, build, source_name="BigGranary1-lvl1", offset=(LVL3_DX, 0, 0), collection="Склады")
