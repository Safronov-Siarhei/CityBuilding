"""Colosseum1-lvl4 -- the Colosseum (Коллизей) at level 4.

Footprint 4x6 (level 1's). Level 3 (colosseum1_lvl3.build) with a gilded crest all round the rim
of the velarium and six more gilded statues round the attic, so that they ring the whole arena.
Level 4's mark across the set is the glazed roof: a lattice of the category's dark shade and
gold diamonds over the tiles. Models only -- the game stops at level 3 for now.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402,F401
import kit  # noqa: E402
from details import G_TOP, L_TOP, STOREY, U_TOP, WALL_H  # noqa: E402,F401

OBJ = "Colosseum1-lvl4"
L3 = kit.borrow("colosseum1_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5


def build(lo, hi):
    b = L3['build'](lo, hi)
    attic = L3['L2']['T_TOP'] + 0.2
    d.ring_crest(b, L3['L2']['ellipse'](1.9 - 0.15, 2.88 - 0.15), attic + 0.13, step=0.25)
    import math
    for a in (0, 50, 130, 180, 230, 310):   # the statues of levels 2 and 3 stand at 90 and 270
        t = math.radians(a)
        sx, sy = (1.9 - 0.15) * math.cos(t), (2.88 - 0.15) * math.sin(t)
        b.cylinder(sx, sy, attic, attic + 0.08, 0.07, 'stone_mid', sides=6)
        b.blob(sx, sy, attic + 0.08, 0.06, 0.22, 'gold', sides=6)
        b.blob(sx, sy, attic + 0.28, 0.035, 0.07, 'gold', sides=5)
    return b


kit.run(OBJ, build, source_name="Colosseum1-lvl1", offset=(LVL4_DX, 0, 0),
        collection="Развлечения")
