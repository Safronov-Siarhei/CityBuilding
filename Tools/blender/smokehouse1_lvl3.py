"""Smokehouse1-lvl3 -- the Smokehouse (Коптильня) at its last upgrade. Footprint 1x2 (level
1's). Front is -Y.

Level 2 (smokehouse1_lvl2.build) with a red banner with a gold mark beside the door, a gold
finial on the front gable, and the working yard in front paved.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import G_TOP  # noqa: E402
from kit import Facade  # noqa: E402

OBJ = "Smokehouse1-lvl3"
L2 = kit.borrow("smokehouse1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    x0, x1, y0 = -0.36, 0.36, 0.0
    zr = G_TOP + (x1 - x0) / 2 * d.PITCH
    d.finial(b, 0, y0 - 0.06, zr + 0.06, h=0.13)
    d.banner(Facade(b, '-y', y0), -0.24, G_TOP - 0.03, 0.26, w=0.1, color='banner_red')
    d.paving(b, -0.46, 0.46, -0.98, y0 - 0.04, z=0.01)
    return b


kit.run(OBJ, build, source_name="Smokehouse1-lvl1", offset=(LVL3_DX, 0, 0),
        collection="Производство еды")
