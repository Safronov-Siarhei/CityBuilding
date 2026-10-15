"""Tavern1-lvl4 -- the Tavern (Таверна) at level 4.

Footprint 2x1 (level 1's). Level 3 (tavern1_lvl3.build) with the violet roof re-laid in glazed
tiles and a gilded crest along the ridge. Level 4's mark across the set is the glazed roof: a
lattice of the category's dark shade and gold diamonds over the tiles. Models only -- the game
stops at level 3 for now.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402,F401
import kit  # noqa: E402
from details import G_TOP, L_TOP, STOREY, U_TOP, WALL_H  # noqa: E402,F401

OBJ = "Tavern1-lvl4"
L3 = kit.borrow("tavern1_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5


def build(lo, hi):
    b = L3['build'](lo, hi)
    d.glazed(b, 0, 0.04, 'x', 0.94, 0.33, L3['L2']['T_TOP'], 'roof_purple_dark')
    return b


kit.run(OBJ, build, source_name="Tavern1-lvl1", offset=(LVL4_DX, 0, 0),
        collection="Развлечения")
