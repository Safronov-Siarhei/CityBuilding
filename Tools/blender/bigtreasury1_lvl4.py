"""BigTreasury1-lvl4 -- the Big Treasury (Большая сокровищница) at level 4.

Footprint 4x3 (level 1's). Level 3 (bigtreasury1_lvl3.build) with the strongroom's roof and both
towers' pyramids re-laid in glazed tiles, and a gilded crest along the ridge. Level 4's mark
across the set is the glazed roof: a lattice of the category's dark shade and gold diamonds over
the tiles. Models only -- the game stops at level 3 for now.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402,F401
import kit  # noqa: E402
from details import G_TOP, L_TOP, STOREY, U_TOP, WALL_H  # noqa: E402,F401

OBJ = "BigTreasury1-lvl4"
L3 = kit.borrow("bigtreasury1_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5


def build(lo, hi):
    b = L3['build'](lo, hi)
    d.glazed(b, 0, 0.45, 'x', 1.3, 0.75, U_TOP, 'roof_red_dark')
    R = L3['L2']['RAISE']
    for sx in (-1, 1):
        d.glazed_pyramid(b, sx * 1.58, 0.05, U_TOP + 0.3 + R + 0.06, 0.41, 0.41, hi[2] + R - 0.16,
                         'roof_red_dark', crest=False)
    return b


kit.run(OBJ, build, source_name="BigTreasury1-lvl1", offset=(LVL4_DX, 0, 0),
        collection="Склады")
