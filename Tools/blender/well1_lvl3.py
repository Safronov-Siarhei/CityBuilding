"""Well1-lvl3 -- the Well (Колодец) at its last upgrade. Footprint 1x1 (level 1's).

Level 2 (well1_lvl2.build) on a square of flagstones, with gold finials on the roof's ridge ends
and a small gold weathervane between them.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402

OBJ = "Well1-lvl3"
L2 = kit.borrow("well1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    d.paving(b, -0.48, 0.48, -0.48, 0.48, z=0.012)
    ridge = 0.86 - 0.06
    for sx in (-1, 1):
        d.finial(b, sx * 0.44, 0, ridge + 0.03, h=0.12)
    d.weathervane(b, 0, 0, ridge + 0.05, h=0.17)
    return b


kit.run(OBJ, build, source_name="Well1-lvl1", offset=(LVL3_DX, 0, 0), collection="Город")
