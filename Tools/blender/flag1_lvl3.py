"""Flag1-lvl3 -- the Flag (Флаг) at its last upgrade. Footprint 1x1 (level 1's).

Level 2 (flag1_lvl2.build) with the pole raised by another 0.3 and two small violet pennants on
gold-tipped poles at the back corners of the plinth.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402

OBJ = "Flag1-lvl3"
L2 = kit.borrow("flag1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, (hi[0], hi[1], hi[2] + 0.3))   # level 2 raises the pole by 0.3 again
    for sx in (-1, 1):
        d.pennant(b, sx * 0.33, 0.33, 0.06, h=0.62, color='roof_purple', length=0.14)
    return b


kit.run(OBJ, build, source_name="Flag1-lvl1", offset=(LVL3_DX, 0, 0), collection="Развлечения")
