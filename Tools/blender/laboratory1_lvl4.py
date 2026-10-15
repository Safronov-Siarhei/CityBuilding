"""Laboratory1-lvl4 -- the Laboratory (Лаборатория) at level 4.

Footprint 4x4 (level 1's). Level 3 (laboratory1_lvl3.build) with the academy's roof and the
observatory dome re-laid in glazed tiles, a gilded crest along the academy's ridge, and a gold
band round the foot of the dome. Level 4's mark across the set is the glazed roof: a lattice of
the category's dark shade and gold diamonds over the tiles. Models only -- the game stops at
level 3 for now.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402,F401
import kit  # noqa: E402
from details import G_TOP, L_TOP, STOREY, U_TOP, WALL_H  # noqa: E402,F401

OBJ = "Laboratory1-lvl4"
L3 = kit.borrow("laboratory1_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5


def build(lo, hi):
    b = L3['build'](lo, hi)
    d.glazed(b, -0.675, 1.025, 'x', 1.275, 0.825, U_TOP, 'roof_orange_dark')
    b.ring(1.2, 1.1, U_TOP + 0.6, U_TOP + 0.64, 0.6, 0.67, ['gold'], sides=8)
    dome_base = U_TOP + 0.62
    d.glazed_dome(b, 1.2, 1.1, dome_base, 0.6, hi[2] + 0.24 - dome_base - 0.12,
                  'roof_purple_dark')
    return b


kit.run(OBJ, build, source_name="Laboratory1-lvl1", offset=(LVL4_DX, 0, 0),
        collection="Город")
