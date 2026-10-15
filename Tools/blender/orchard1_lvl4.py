"""Orchard1-lvl4 -- the Orchard (Фруктовый сад) at level 4.

Footprint 3x3 (level 1's). Level 3 (orchard1_lvl3.build) with the picker's hut's thatch re-laid
in glazed tiles with a gilded crest, and golden apples on the middle tree of the back row. Level
4's mark across the set is the glazed roof: a lattice of the category's dark shade and gold
diamonds over the tiles. Models only -- the game stops at level 3 for now.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402,F401
import kit  # noqa: E402
from details import G_TOP, L_TOP, STOREY, U_TOP, WALL_H  # noqa: E402,F401

OBJ = "Orchard1-lvl4"
L3 = kit.borrow("orchard1_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5


def build(lo, hi):
    b = L3['build'](lo, hi)
    d.glazed(b, -0.96, -0.91, 'x', 0.43, 0.29, G_TOP, 'roof_straw_dark', accent='roof_red',
             w=0.05, step=0.16, row=0.13)
    import math
    for i in range(6):
        a = math.radians(30 + 60 * i)
        b.blob(0.27 * math.cos(a), 0.95 + 0.27 * math.sin(a), 0.42 + 0.08 * (i % 3), 0.045, 0.075,
               'gold', sides=4)
    return b


kit.run(OBJ, build, source_name="Orchard1-lvl1", offset=(LVL4_DX, 0, 0),
        collection="Производство еды")
