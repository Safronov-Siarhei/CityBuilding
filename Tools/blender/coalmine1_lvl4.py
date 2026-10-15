"""CoalMine1-lvl4 -- the Coal Mine (Угольная шахта) at level 4.

Footprint 2x2 (level 1's). Level 3 (coalmine1_lvl3.build) with the winding shed's roof re-laid
in glazed tiles with a gilded crest. Level 4's mark across the set is the glazed roof: a lattice
of the category's dark shade and gold diamonds over the tiles. Models only -- the game stops at
level 3 for now.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402,F401
import kit  # noqa: E402
from details import G_TOP, L_TOP, STOREY, U_TOP, WALL_H  # noqa: E402,F401

OBJ = "CoalMine1-lvl4"
L3 = kit.borrow("coalmine1_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5


def build(lo, hi):
    b = L3['build'](lo, hi)
    d.glazed_shed(b, 'x', 0.13, 0.93, 0.06, -0.6, 0.72, 0.5, 'roof_slate_dark')
    return b


kit.run(OBJ, build, source_name="CoalMine1-lvl1", offset=(LVL4_DX, 0, 0),
        collection="Производственные здания")
