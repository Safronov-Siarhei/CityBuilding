"""Tavern1-lvl3 -- the Tavern (Таверна) at its last upgrade. Footprint 2x1 (level 1's).
Front is -Y.

Level 2 (tavern1_lvl2.build) on a feast night: bunting strung along the front under the first
jetty, gold finials on the ridge ends and a gold weathervane on the ridge, the doorstep paved.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import JETTY as J, U_TOP  # noqa: E402

OBJ = "Tavern1-lvl3"
L2 = kit.borrow("tavern1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    x0, x1, y0, y1 = -0.8, 0.8, -0.24, 0.32
    ux1, uy0, uy1 = x1 + J, y0 - J, y1 + J
    yc, hs = (y0 + y1) / 2, (uy1 - uy0) / 2
    zr = L2['T_TOP'] + hs * d.PITCH
    for ex in (-ux1 - 0.09, ux1 + 0.09):
        d.finial(b, ex, yc, zr + 0.06)
    d.weathervane(b, -0.3, yc, zr + 0.07, h=0.26)
    d.bunting(b, (-ux1, uy0 - 0.03, U_TOP - 0.02), (ux1, uy0 - 0.03, U_TOP - 0.02), sag=0.07,
              step=0.1)
    d.paving(b, -0.95, 0.95, -0.49, y0)
    return b


kit.run(OBJ, build, source_name="Tavern1-lvl1", offset=(LVL3_DX, 0, 0), collection="Развлечения")
