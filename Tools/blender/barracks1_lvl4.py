"""Barracks1-lvl4 -- the Barracks (Казармы) at level 4.

Footprint 3x3 (level 1's). Level 3 (barracks1_lvl3.build) with the quarters' roof re-laid in
glazed tiles and a gilded crest along the ridge. Level 4's mark across the set is the glazed
roof: a lattice of the category's dark shade and gold diamonds over the tiles. Models only --
the game stops at level 3 for now.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402,F401
import kit  # noqa: E402
from details import G_TOP, L_TOP, STOREY, U_TOP, WALL_H  # noqa: E402,F401

OBJ = "Barracks1-lvl4"
L3 = kit.borrow("barracks1_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5


def build(lo, hi):
    b = L3['build'](lo, hi)
    d.glazed(b, 0, 0.77, 'x', 1.5, 0.6, U_TOP, 'roof_blue_dark')
    return b


kit.run(OBJ, build, source_name="Barracks1-lvl1", offset=(LVL4_DX, 0, 0),
        collection="Оборонительные здания")
