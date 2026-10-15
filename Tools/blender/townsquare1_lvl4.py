"""TownSquare1-lvl4 -- the Town Square (Городская площадь) at level 4.

Footprint 2x2 (level 1's). Level 3 (townsquare1_lvl3.build) with a gold mosaic ring round the
basin and gold finials on the four lamps. Level 4's mark across the set is the glazed roof: a
lattice of the category's dark shade and gold diamonds over the tiles. Models only -- the game
stops at level 3 for now.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402,F401
import kit  # noqa: E402
from details import G_TOP, L_TOP, STOREY, U_TOP, WALL_H  # noqa: E402,F401

OBJ = "TownSquare1-lvl4"
L3 = kit.borrow("townsquare1_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5


def build(lo, hi):
    b = L3['build'](lo, hi)
    b.ring(0, 0, 0.035, 0.045, 0.64, 0.7, ['gold', 'plaster_pink'], sides=16)
    for sx in (-1, 1):
        for sy in (-1, 1):
            d.finial(b, sx * 0.8, sy * 0.8, 0.76, h=0.1)
    return b


kit.run(OBJ, build, source_name="TownSquare1-lvl1", offset=(LVL4_DX, 0, 0),
        collection="Развлечения")
