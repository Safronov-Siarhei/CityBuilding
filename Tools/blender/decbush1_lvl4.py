"""DecBush1-lvl4 -- the Decorative Bush (Декоративный куст) at level 4.

Footprint 1x1 (level 1's). Level 3 (decbush1_lvl3.build) with a gold band round the planter's
rim and gold balls on the front urns. Level 4's mark across the set is the glazed roof: a
lattice of the category's dark shade and gold diamonds over the tiles. Models only -- the game
stops at level 3 for now.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402,F401
import kit  # noqa: E402
from details import G_TOP, L_TOP, STOREY, U_TOP, WALL_H  # noqa: E402,F401

OBJ = "DecBush1-lvl4"
L3 = kit.borrow("decbush1_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5


def build(lo, hi):
    b = L3['build'](lo, hi)
    b.box(-0.446, 0.446, -0.446, 0.446, 0.08, 0.1, 'gold')
    for x in (-0.37, 0.37):
        b.ball(x, -0.37, 0.3, 0.035, 0.06, 'gold', sides=6)
    return b


kit.run(OBJ, build, source_name="DecBush1-lvl1", offset=(LVL4_DX, 0, 0),
        collection="Развлечения")
