"""PigFarm1-lvl3 -- the Pig Farm (Свиноферма) at its last upgrade. Footprint 3x4 (level 1's).
Front is -Y.

Level 2 (pigfarm1_lvl2.build) with a louvred vent turret under a thatched cap on the sty's
ridge, gold finials on its ends, and stone piers with gold balls at the pen's gate.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import L_TOP  # noqa: E402

OBJ = "PigFarm1-lvl3"
L2 = kit.borrow("pigfarm1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    x0, x1, y0, y1 = -1.3, 1.3, 0.95, 1.85
    yc, hs = (y0 + y1) / 2, (y1 - y0) / 2
    zr = L_TOP + hs * d.PITCH
    d.louvre(b, -0.5, yc, zr - 0.04, roof='roof_straw')
    for ex in (x0 - 0.09, x1 + 0.09):
        d.finial(b, ex, yc, zr + 0.06)
    for sx in (-1, 1):
        d.pier(b, sx * 0.24, -1.92, h=0.32, w=0.05)
    return b


kit.run(OBJ, build, source_name="PigFarm1-lvl1", offset=(LVL3_DX, 0, 0),
        collection="Производство еды")
