"""Flag1-lvl4 -- the Flag (Флаг) at level 4.

Footprint 1x1 (level 1's). Level 3 (flag1_lvl3.build) with the pole raised by another 0.3 and a
gold band round the plinth. Level 4's mark across the set is the glazed roof: a lattice of the
category's dark shade and gold diamonds over the tiles. Models only -- the game stops at level 3
for now.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402,F401
import kit  # noqa: E402
from details import G_TOP, L_TOP, STOREY, U_TOP, WALL_H  # noqa: E402,F401

OBJ = "Flag1-lvl4"
L3 = kit.borrow("flag1_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5


def build(lo, hi):
    b = L3['build'](lo, (hi[0], hi[1], hi[2] + 0.3))   # the pole rises 0.3 again
    b.box(-0.305, 0.305, -0.305, 0.305, 0.115, 0.14, 'gold')
    return b


kit.run(OBJ, build, source_name="Flag1-lvl1", offset=(LVL4_DX, 0, 0),
        collection="Развлечения")
