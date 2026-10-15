"""Farm1-lvl4 -- the Farm (Ферма) at level 4.

Footprint 1x1 (level 1's). Level 3 (farm1_lvl3.build) with a gold weathervane on a pole at the
back of the field. Level 4's mark across the set is the glazed roof: a lattice of the category's
dark shade and gold diamonds over the tiles. Models only -- the game stops at level 3 for now.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402,F401
import kit  # noqa: E402
from details import G_TOP, L_TOP, STOREY, U_TOP, WALL_H  # noqa: E402,F401

OBJ = "Farm1-lvl4"
L3 = kit.borrow("farm1_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5


def build(lo, hi):
    b = L3['build'](lo, hi)
    b.box(-0.32, -0.29, 0.39, 0.42, 0.035, 0.72, 'wood_dark')
    d.weathervane(b, -0.305, 0.405, 0.72, h=0.2)
    return b


kit.run(OBJ, build, source_name="Farm1-lvl1", offset=(LVL4_DX, 0, 0),
        collection="Производство еды")
