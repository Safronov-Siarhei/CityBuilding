"""Windmill1-lvl4 -- the Windmill (Ветряк) at level 4.

Footprint 2x2 (level 1's). Level 3 (windmill1_lvl3.build) with the thatched cap and the porch
roof re-laid in glazed tiles (red diamonds, since gold vanishes on thatch), a gilded crest on
the porch and a gold band round the foot of the cap. Level 4's mark across the set is the glazed
roof: a lattice of the category's dark shade and gold diamonds over the tiles. Models only --
the game stops at level 3 for now.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402,F401
import kit  # noqa: E402
from details import G_TOP, L_TOP, STOREY, U_TOP, WALL_H  # noqa: E402,F401

OBJ = "Windmill1-lvl4"
L3 = kit.borrow("windmill1_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5


def build(lo, hi):
    b = L3['build'](lo, hi)
    d.glazed_shed(b, 'x', -0.3, 0.3, -0.2, -0.66, 0.86, 0.64, 'roof_straw_dark',
                  accent='roof_red')
    d.glazed_dome(b, 0.0, 0.22, 1.85 + 0.06, 0.42 + 0.1, 0.62, 'roof_straw_dark',
                  accent='roof_red', w=0.05, step=0.17, row=0.13)
    b.ring(0.0, 0.22, 1.85 + 0.02, 1.85 + 0.06, 0.42 + 0.05, 0.42 + 0.09, ['gold'], sides=8)
    return b


kit.run(OBJ, build, source_name="Windmill1-lvl1", offset=(LVL4_DX, 0, 0),
        collection="Производство еды")
