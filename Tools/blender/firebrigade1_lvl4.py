"""FireBrigade1-lvl4 -- the Fire Brigade (Пожарная бригада) at level 4.

Footprint 2x2 (level 1's). Level 3 (firebrigade1_lvl3.build) with the engine house's roof and
the lookout tower's pyramid re-laid in glazed tiles, and a gilded crest along the ridge. Level
4's mark across the set is the glazed roof: a lattice of the category's dark shade and gold
diamonds over the tiles. Models only -- the game stops at level 3 for now.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402,F401
import kit  # noqa: E402
from details import G_TOP, L_TOP, STOREY, U_TOP, WALL_H  # noqa: E402,F401

OBJ = "FireBrigade1-lvl4"
L3 = kit.borrow("firebrigade1_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5


def build(lo, hi):
    b = L3['build'](lo, hi)
    d.glazed(b, -0.28, 0.16, 'y', 0.75, 0.58, G_TOP, 'roof_orange_dark')
    R = L3['L2']['RAISE']
    d.glazed_pyramid(b, 0.64, 0.38, 1.27 + R, 0.32, 0.32, hi[2] - 0.02 + R, 'roof_orange_dark',
                     crest=False)
    return b


kit.run(OBJ, build, source_name="FireBrigade1-lvl1", offset=(LVL4_DX, 0, 0),
        collection="Город")
