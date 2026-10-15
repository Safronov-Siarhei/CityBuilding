"""GoldMine1-lvl3 -- the Gold Mine (Золотой рудник) at its last upgrade. Footprint 2x2 (level
1's). Front is -Y.

Level 2 (goldmine1_lvl2.build) grown rich: gold finials stand on the portal's top corners, a
gilded guardian on a plinth watches the track, and the ground before the portal is paved.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402

OBJ = "GoldMine1-lvl3"
L2 = kit.borrow("goldmine1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    yf, s = -0.24, -0.05
    for k in (-1, 1):
        d.finial(b, s + k * 0.33, yf + 0.04, 0.82, h=0.14)
    d.paving(b, s - 0.3, s + 0.3, -0.99, yf, z=0.01)
    d.statue(b, 0.32, -0.86, 0, scale=0.75)
    return b


kit.run(OBJ, build, source_name="GoldMine1-lvl1", offset=(LVL3_DX, 0, 0),
        collection="Производственные здания")
