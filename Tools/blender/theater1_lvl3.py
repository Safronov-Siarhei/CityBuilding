"""Theater1-lvl3 -- the Theatre (Театр) at its last upgrade. Footprint 4x4 (level 1's).
Front is -Y.

Level 2 (theater1_lvl2.build) on a première: two strings of bunting cross the open yard from
mast to mast, a gold finial tops the portico's pediment, and the forecourt is paved.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import G_TOP  # noqa: E402

OBJ = "Theater1-lvl3"
L2 = kit.borrow("theater1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    top = hi[2]
    cx, cy, r_mid = 0.0, 0.05, (1.78 + 1.12) / 2
    masts = []
    for i in (1, 3, 5, 7):
        a = math.radians(22.5 + 45 * i + 22.5)
        masts.append((cx + r_mid * math.cos(a), cy + r_mid * math.sin(a), top - 0.3))
    d.bunting(b, masts[0], masts[2], sag=0.3, step=0.13)
    d.bunting(b, masts[1], masts[3], sag=0.3, step=0.13)
    pz = G_TOP + 0.08
    d.finial(b, 0, -1.92 - 0.02, pz + 0.38 * 0.7 + 0.04, h=0.16)
    d.paving(b, -0.85, 0.85, -1.98, -1.62)
    return b


kit.run(OBJ, build, source_name="Theater1-lvl1", offset=(LVL3_DX, 0, 0),
        collection="Развлечения")
