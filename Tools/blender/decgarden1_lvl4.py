"""DecGarden1-lvl4 -- the Decorative Garden (Декоративный сад) at level 4.

Footprint 3x1 (level 1's). Level 3 (decgarden1_lvl3.build) with gold balls on the corners of the
hedge border and on the four posts of the rose arches. Level 4's mark across the set is the
glazed roof: a lattice of the category's dark shade and gold diamonds over the tiles. Models
only -- the game stops at level 3 for now.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402,F401
import kit  # noqa: E402
from details import G_TOP, L_TOP, STOREY, U_TOP, WALL_H  # noqa: E402,F401

OBJ = "DecGarden1-lvl4"
L3 = kit.borrow("decgarden1_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5


def build(lo, hi):
    b = L3['build'](lo, hi)
    for x in (-1.41, 1.41):
        for y in (-0.41, 0.41):
            b.ball(x, y, 0.17, 0.04, 0.07, 'gold', sides=6)
    for ax in (-0.72, 0.72):
        for ay in (-0.13, 0.13):
            b.ball(ax, ay, 0.5, 0.03, 0.05, 'gold', sides=6)
    return b


kit.run(OBJ, build, source_name="DecGarden1-lvl1", offset=(LVL4_DX, 0, 0),
        collection="Развлечения")
