"""BigBarn1-lvl4 -- the Big Barn (Большой амбар) at level 4. Footprint 2x2 (level 1's).

Level 3 (bigbarn1_lvl3.build) with the roof and the lean-to re-laid in glazed tiles, with gilded
crests. Level 4's mark across the set is the glazed roof: a lattice of the category's dark shade
and gold diamonds over the tiles. Models only -- the game stops at level 3 for now.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402,F401
import kit  # noqa: E402
from details import G_TOP, L_TOP, STOREY, U_TOP, WALL_H  # noqa: E402,F401

OBJ = "BigBarn1-lvl4"
L3 = kit.borrow("bigbarn1_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5


def build(lo, hi):
    b = L3['build'](lo, hi)
    d.glazed(b, -0.1, 0.17, 'y', 0.76, 0.52, U_TOP, 'roof_red_dark')
    d.glazed_shed(b, 'y', -0.16, 0.9, 0.42, 0.9, 0.9, 0.6, 'roof_red_dark')
    return b


kit.run(OBJ, build, source_name="BigBarn1-lvl1", offset=(LVL4_DX, 0, 0),
        collection="Склады")
