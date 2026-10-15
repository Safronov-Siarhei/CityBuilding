"""Manor1-lvl4 -- the Manor (Особняк) at level 4.

Footprint 3x3 (level 1's). Level 3 (manor1_lvl3.build) with both wings' roofs re-laid in glazed
tiles and gilded crests along their ridges (the side wing's stops where its ridge runs under the
main roof). Level 4's mark across the set is the glazed roof: a lattice of the category's dark
shade and gold diamonds over the tiles. Models only -- the game stops at level 3 for now.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402,F401
import kit  # noqa: E402
from details import G_TOP, L_TOP, STOREY, U_TOP, WALL_H  # noqa: E402,F401

OBJ = "Manor1-lvl4"
L3 = kit.borrow("manor1_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5


def build(lo, hi):
    b = L3['build'](lo, hi)
    J = d.JETTY
    d.glazed(b, 0, 0.725, 'x', 1.34 + J + 0.09, 0.625, U_TOP, 'roof_orange_dark')
    d.glazed_roof(b, 0.91, -0.3575, 'y', 1.0825, 0.48, U_TOP, 'roof_orange_dark')
    d.ridge_crest(b, 0.91, -0.495, 'y', 0.945, 0.48, U_TOP)
    return b


kit.run(OBJ, build, source_name="Manor1-lvl1", offset=(LVL4_DX, 0, 0),
        collection="Город")
