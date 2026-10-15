"""Laboratory1-lvl3 -- the Laboratory (Лаборатория) at its last upgrade. Footprint 4x4 (level
1's). Front -Y.

Level 2 (laboratory1_lvl2.build) crowned: a glazed lantern under a violet cone on the academy's
ridge, gold finials on its gable ends, and a gold armillary sphere on a column in the yard.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import U_TOP  # noqa: E402

OBJ = "Laboratory1-lvl3"
L2 = kit.borrow("laboratory1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    x0, x1, y0, y1 = -1.85, 0.5, 0.2, 1.85
    yc, hs = (y0 + y1) / 2, (y1 - y0) / 2
    zr = U_TOP + hs * d.PITCH
    d.cupola(b, (x0 + x1) / 2, yc, zr + 0.04, r=0.15, h=0.26, roof='roof_purple')
    for ex in (x0 - 0.1, x1 + 0.1):
        d.finial(b, ex, yc, zr + 0.06)
    d.armillary(b, -1.35, -1.6, 0.015)
    return b


kit.run(OBJ, build, source_name="Laboratory1-lvl1", offset=(LVL3_DX, 0, 0), collection="Город")
