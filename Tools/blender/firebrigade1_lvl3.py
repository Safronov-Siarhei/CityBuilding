"""FireBrigade1-lvl3 -- the Fire Brigade (Пожарная бригада) at its last upgrade. Footprint 2x2
(level 1's). Front is -Y.

Level 2 (firebrigade1_lvl2.build) with the lookout tower raised again (RAISE 0.3 -> 0.55) so it
watches over a grown town, a gold finial on its pennant pole and on both ends of the engine
house's ridge, and the forecourt where the water cart stands paved.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import G_TOP  # noqa: E402

OBJ = "FireBrigade1-lvl3"
L2 = kit.borrow("firebrigade1_lvl2.py")
L2['RAISE'] = 0.55        # level 2's build reads it as a global
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    R = L2['RAISE']
    x0, x1, y0, y1 = -0.86, 0.3, -0.5, 0.82
    xc, hs = (x0 + x1) / 2, (x1 - x0) / 2
    zr = G_TOP + hs * d.PITCH
    for ey in (y0 - 0.09, y1 + 0.09):
        d.finial(b, xc, ey, zr + 0.06)
    d.finial(b, 0.64, 0.38, hi[2] + 0.2 + R, h=0.12)
    d.paving(b, x0, x1, -0.98, y0 - 0.02)
    return b


kit.run(OBJ, build, source_name="FireBrigade1-lvl1", offset=(LVL3_DX, 0, 0), collection="Город")
