"""Workshop1-lvl5 -- the Workshop (Мастерская) at level 5. Footprint 3x3.

Level 4 (workshop1_lvl4.build) with every roof slope gilded (details.gild_roofs): gold tiles
under the dark lattice -- the set's level-5 «золотая кровля».
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import detail  # noqa: E402,F401
import kit  # noqa: E402
from details import FOOTING, G_TOP  # noqa: E402,F401
from kit import Facade  # noqa: E402,F401

OBJ = "Workshop1-lvl5"
L4 = kit.borrow("workshop1_lvl4.py")
LVL5_DX = L4['LVL4_DX'] * 4 / 3
detail.OVERRIDE['gold'] = detail.TILES   # gold reads as tiles on a slope (this model only)


def build(lo, hi):
    b = L4['build'](lo, hi)
    d.gild_roofs(b)
    return b


kit.run(OBJ, build, source_name="Barracks1-lvl1", offset=(LVL5_DX, -6.0, 0))
