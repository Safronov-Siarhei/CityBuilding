"""FortifiedGate1-lvl4 -- the Fortified Gate (Укреплённые ворота) at level 4.

Footprint 1x3 (level 1's). Level 3 (fortifiedgate1_lvl3.build) with the four turrets' caps and
the watch room's roof re-laid in glazed tiles; the wall joins are untouched. Level 4's mark
across the set is the glazed roof: a lattice of the category's dark shade and gold diamonds over
the tiles. Models only -- the game stops at level 3 for now.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402,F401
import kit  # noqa: E402
from details import G_TOP, L_TOP, STOREY, U_TOP, WALL_H  # noqa: E402,F401

OBJ = "FortifiedGate1-lvl4"
L3 = kit.borrow("fortifiedgate1_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5


def build(lo, hi):
    b = L3['build'](lo, hi)
    for sx in (-1, 1):
        for sy in (-1, 1):
            L2 = L3['L2']
            d.glazed_pyramid(b, sx * (L2['GX'] - 0.1), sy * (L2['GY'] - 0.1), 1.38, 0.2, 0.2, 1.68, 'roof_blue_dark',
                             w=0.04, step=0.13, row=0.1, crest=False)
    z1 = WALL_H + 0.36 + 0.42
    d.glazed_pyramid(b, 0, 0, z1, 0.22, 0.31, z1 + 0.42, 'roof_blue_dark', w=0.045, step=0.14,
                     row=0.11, crest=False)
    return b


kit.run(OBJ, build, source_name="FortifiedGate1-lvl1", offset=(LVL4_DX, 0, 0),
        collection="Оборонительные здания")
