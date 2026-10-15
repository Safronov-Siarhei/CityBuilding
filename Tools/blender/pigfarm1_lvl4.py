"""PigFarm1-lvl4 -- the Pig Farm (Свиноферма) at level 4.

Footprint 3x4 (level 1's). Level 3 (pigfarm1_lvl3.build) with the sty's roof and the shelter's
lean-to re-laid in glazed tiles, with gilded crests. Level 4's mark across the set is the glazed
roof: a lattice of the category's dark shade and gold diamonds over the tiles. Models only --
the game stops at level 3 for now.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402,F401
import kit  # noqa: E402
from details import G_TOP, L_TOP, STOREY, U_TOP, WALL_H  # noqa: E402,F401

OBJ = "PigFarm1-lvl4"
L3 = kit.borrow("pigfarm1_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5


def build(lo, hi):
    b = L3['build'](lo, hi)
    d.glazed(b, 0, 1.4, 'x', 1.39, 0.45, L_TOP, 'roof_straw_dark', accent='roof_red')
    d.glazed_shed(b, 'y', -1.9, -1.26, 1.38, 0.76, 0.58, 0.42, 'roof_straw_dark',
                  accent='roof_red')
    return b


kit.run(OBJ, build, source_name="PigFarm1-lvl1", offset=(LVL4_DX, 0, 0),
        collection="Производство еды")
