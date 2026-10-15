"""FortifiedTower1-lvl4 -- the Fortified Tower (Укреплённая башня) at level 4.

Footprint 2x2 (level 1's). Level 3 (fortifiedtower1_lvl3.build) with the great pyramid roof
re-laid in glazed tiles; the wall joins are untouched. Level 4's mark across the set is the
glazed roof: a lattice of the category's dark shade and gold diamonds over the tiles. Models
only -- the game stops at level 3 for now.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402,F401
import kit  # noqa: E402
from details import G_TOP, L_TOP, STOREY, U_TOP, WALL_H  # noqa: E402,F401

OBJ = "FortifiedTower1-lvl4"
L3 = kit.borrow("fortifiedtower1_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5


def build(lo, hi):
    b = L3['build'](lo, hi)
    d.glazed_pyramid(b, 0, 0, U_TOP + 0.6 + STOREY + 0.06, 0.84, 0.84, hi[2] + STOREY - 0.2,
                     'roof_blue_dark', crest=False)
    return b


kit.run(OBJ, build, source_name="FortifiedTower1-lvl1", offset=(LVL4_DX, 0, 0),
        collection="Оборонительные здания")
