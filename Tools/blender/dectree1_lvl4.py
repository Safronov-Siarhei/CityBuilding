"""DecTree1-lvl4 -- the Decorative Tree (Декоративное дерево) at level 4.

Footprint 1x1 (level 1's). Level 3 (dectree1_lvl3.build) with a second scatter of gilded leaves
through the crown and a gold finial at its top. Level 4's mark across the set is the glazed
roof: a lattice of the category's dark shade and gold diamonds over the tiles. Models only --
the game stops at level 3 for now.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402,F401
import kit  # noqa: E402
from details import G_TOP, L_TOP, STOREY, U_TOP, WALL_H  # noqa: E402,F401

OBJ = "DecTree1-lvl4"
L3 = kit.borrow("dectree1_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5


def build(lo, hi):
    b = L3['build'](lo, hi)
    for x, y, z in ((-0.36, 0.08, 1.02), (0.2, 0.3, 1.2), (-0.22, -0.3, 1.34), (0.3, -0.1, 1.5),
                    (-0.1, 0.2, 1.68)):
        b.blob(x, y, z, 0.055, 0.08, 'gold', sides=4)
    d.finial(b, 0.02, 0, hi[2] - 0.04, h=0.14)
    return b


kit.run(OBJ, build, source_name="DecTree1-lvl1", offset=(LVL4_DX, 0, 0),
        collection="Развлечения")
