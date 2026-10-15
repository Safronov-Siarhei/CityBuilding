"""Quarry1-lvl4 -- the Quarry (Каменоломня) at level 4.

Footprint 2x2 (level 1's). Level 3 (quarry1_lvl3.build) with the tool lean-to re-laid in glazed
tiles with a gilded crest, and a second, smaller obelisk in the yard. Level 4's mark across the
set is the glazed roof: a lattice of the category's dark shade and gold diamonds over the tiles.
Models only -- the game stops at level 3 for now.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402,F401
import kit  # noqa: E402
from details import G_TOP, L_TOP, STOREY, U_TOP, WALL_H  # noqa: E402,F401

OBJ = "Quarry1-lvl4"
L3 = kit.borrow("quarry1_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5


def build(lo, hi):
    b = L3['build'](lo, hi)
    d.glazed_shed(b, 'y', -0.36, 0.29, 0.92, 0.64, 0.62, 0.44, 'roof_slate_dark')
    L3['obelisk'](b, -0.6, -0.88, h=0.56)
    return b


kit.run(OBJ, build, source_name="Quarry1-lvl1", offset=(LVL4_DX, 0, 0),
        collection="Производственные здания")
