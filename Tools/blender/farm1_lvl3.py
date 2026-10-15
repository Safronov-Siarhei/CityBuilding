"""Farm1-lvl3 -- the Farm (Ферма) at its last upgrade. Footprint 1x1 (level 1's). Front -Y.

Level 2 (farm1_lvl2.build) fenced on its right edge as well, a gold finial on the scarecrow's
hat, and a prize pumpkin -- gilded -- beside the other two.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402

OBJ = "Farm1-lvl3"
L2 = kit.borrow("farm1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    d.rail_fence(b, [(0.47, 0.47), (0.47, -0.47)], h=0.2, post_step=0.3)
    d.finial(b, 0.3, 0.3, 0.59, h=0.1)
    b.ball(-0.04, -0.42, 0.035, 0.06, 0.09, 'gold', sides=6)
    b.box(-0.046, -0.034, -0.426, -0.414, 0.12, 0.15, 'leaf')
    return b


kit.run(OBJ, build, source_name="Farm1-lvl1", offset=(LVL3_DX, 0, 0),
        collection="Производство еды")
