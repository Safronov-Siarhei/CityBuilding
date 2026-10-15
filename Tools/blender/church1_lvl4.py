"""Church1-lvl4 -- the Church (Церковь) at level 4.

Footprint 3x3 (level 1's). Level 3 (church1_lvl3.build) with the nave's and the chapel's roofs
and the spire re-laid in glazed tiles, with gilded crests along the ridges. Level 4's mark
across the set is the glazed roof: a lattice of the category's dark shade and gold diamonds over
the tiles. Models only -- the game stops at level 3 for now.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402,F401
import kit  # noqa: E402
from details import G_TOP, L_TOP, STOREY, U_TOP, WALL_H  # noqa: E402,F401

OBJ = "Church1-lvl4"
L3 = kit.borrow("church1_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5


def build(lo, hi):
    b = L3['build'](lo, hi)
    d.glazed(b, 0, 0.4, 'y', 1.05, 0.66, U_TOP, 'roof_purple_dark')
    d.glazed(b, 1.03, 0.55, 'x', 0.37, 0.3, G_TOP + 0.06, 'roof_purple_dark', w=0.05, step=0.16,
             row=0.13)
    bell1 = U_TOP + 0.62 + 0.42
    d.glazed_pyramid(b, 0, -0.88, bell1 + 0.05, 0.44, 0.44, hi[2] + 0.6 - 0.16, 'roof_purple_dark',
                     crest=False)
    return b


kit.run(OBJ, build, source_name="Church1-lvl1", offset=(LVL4_DX, 0, 0),
        collection="Развлечения")
