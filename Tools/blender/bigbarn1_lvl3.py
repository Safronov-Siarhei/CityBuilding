"""BigBarn1-lvl3 -- the Big Barn (Большой амбар) at its last upgrade. Footprint 2x2 (level 1's).
Front is -Y.

Level 2 (bigbarn1_lvl2.build) with a louvred vent turret on the ridge behind the weathervane,
gold finials on both gable ends, and the yard where the cart stands paved.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import U_TOP  # noqa: E402

OBJ = "BigBarn1-lvl3"
L2 = kit.borrow("bigbarn1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    x0, x1, y0, y1 = -0.62, 0.42, -0.5, 0.84
    xc, hs = (x0 + x1) / 2, (x1 - x0) / 2
    zr = U_TOP + hs * d.PITCH
    d.louvre(b, xc, 0.42, zr - 0.04)
    for ey in (y0 - 0.09, y1 + 0.09):
        d.finial(b, xc, ey, zr + 0.06)
    d.paving(b, -0.95, 0.0, -0.98, -0.55)
    return b


kit.run(OBJ, build, source_name="BigBarn1-lvl1", offset=(LVL3_DX, 0, 0), collection="Склады")
