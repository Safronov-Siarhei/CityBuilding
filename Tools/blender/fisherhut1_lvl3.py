"""FisherHut1-lvl3 -- the Fisher's Hut (Рыбацкая хижина) at its last upgrade. Footprint 1x1
(level 1's). Front is -Y.

Level 2 (fisherhut1_lvl2.build) with a gold weathervane on the ridge, gold finials on its ends
and a prize catch -- a gilded fish -- hung at the end of the drying rack.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import G_TOP  # noqa: E402

OBJ = "FisherHut1-lvl3"
L2 = kit.borrow("fisherhut1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    x0, x1, y0, y1 = -0.42, 0.14, -0.08, 0.36
    yc, hs = (y0 + y1) / 2, (y1 - y0) / 2
    zr = G_TOP + hs * d.PITCH
    d.weathervane(b, (x0 + x1) / 2 + 0.05, yc, zr + 0.06, h=0.18)
    for ex in (x0 - 0.01, x1 + 0.02):
        d.finial(b, ex, yc, zr + 0.05, h=0.1)
    rx, y, zt = 0.36, -0.33, 0.47
    b.zbox(rx, y, zt - 0.16, zt - 0.03, 0.014, 0.04, 0, 'gold', bevel=0.006)
    b.pyramid(rx, y, zt - 0.19, 0.012, 0.045, zt - 0.15, 'gold')
    b.box(rx - 0.004, rx + 0.004, y - 0.004, y + 0.004, zt - 0.03, zt, 'burlap')
    return b


kit.run(OBJ, build, source_name="FisherHut1-lvl1", offset=(LVL3_DX, 0, 0),
        collection="Производство еды")
