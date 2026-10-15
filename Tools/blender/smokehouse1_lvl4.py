"""Smokehouse1-lvl4 -- the Smokehouse (Коптильня) at level 4.

Footprint 1x2 (level 1's). Level 3 (smokehouse1_lvl3.build) with the roof re-laid in glazed
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

OBJ = "Smokehouse1-lvl4"
L3 = kit.borrow("smokehouse1_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5


def build(lo, hi):
    b = L3['build'](lo, hi)
    d.glazed(b, 0, 0.45, 'y', 0.52, 0.36, G_TOP, 'roof_straw_dark', accent='roof_red')
    return b


kit.run(OBJ, build, source_name="Smokehouse1-lvl1", offset=(LVL4_DX, 0, 0),
        collection="Производство еды")
