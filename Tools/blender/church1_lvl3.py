"""Church1-lvl3 -- the Church (Церковь) at its last upgrade. Footprint 3x3 (level 1's).
Front is -Y.

Level 2 (church1_lvl2.build) with the spire raised by another 0.3, four violet pinnacles with
gold finials round its foot on the corners of the bell tower, a gold finial on the nave's back
gable, and the path paved.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import U_TOP  # noqa: E402

OBJ = "Church1-lvl3"
L2 = kit.borrow("church1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, (hi[0], hi[1], hi[2] + 0.3))   # level 2 raises the spire 0.3 again
    tx, ty, th = 0.0, -0.88, 0.4
    bell1 = U_TOP + 0.62 + 0.42
    for sx in (-1, 1):
        for sy in (-1, 1):
            px, py = tx + sx * (th - 0.05), ty + sy * (th - 0.05)
            b.box(px - 0.05, px + 0.05, py - 0.05, py + 0.05, bell1, bell1 + 0.14, 'stone_mid')
            b.pyramid(px, py, bell1 + 0.14, 0.065, 0.065, bell1 + 0.42, 'roof_purple')
            d.finial(b, px, py, bell1 + 0.41, h=0.1)
    x1, y1 = 0.66, 1.35
    d.finial(b, 0, y1 + 0.1, U_TOP + x1 * d.PITCH + 0.06, h=0.18)
    d.paving(b, -0.2, 0.2, -1.48, ty - th - 0.14, z=0.016)
    return b


kit.run(OBJ, build, source_name="Church1-lvl1", offset=(LVL3_DX, 0, 0), collection="Развлечения")
