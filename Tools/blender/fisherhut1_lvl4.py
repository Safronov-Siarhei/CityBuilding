"""FisherHut1-lvl4 -- the Fisher's Hut (Рыбацкая хижина) at level 4.

Footprint 1x1 (level 1's). Level 3 (fisherhut1_lvl3.build) with the roof re-laid in glazed tiles
and a gilded crest along the ridge. Level 4's mark across the set is the glazed roof: a lattice
of the category's dark shade and gold diamonds over the tiles. Models only -- the game stops at
level 3 for now.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402,F401
import kit  # noqa: E402
from details import G_TOP, L_TOP, STOREY, U_TOP, WALL_H  # noqa: E402,F401

OBJ = "FisherHut1-lvl4"
L3 = kit.borrow("fisherhut1_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5


def build(lo, hi):
    b = L3['build'](lo, hi)
    d.glazed(b, -0.14, 0.14, 'x', 0.34, 0.22, G_TOP, 'roof_straw_dark', accent='roof_red',
             overhang=0.05, w=0.045, step=0.15, row=0.12)
    return b


kit.run(OBJ, build, source_name="FisherHut1-lvl1", offset=(LVL4_DX, 0, 0),
        collection="Производство еды")
