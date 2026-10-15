"""Castle1-lvl4 -- the Town Hall (Ратуша) at level 4.

Footprint 5x5 (level 1's). Level 3 (castle1_lvl3.build) with the hall's roof and the blue cones
of the four corner turrets and the clock tower's spire re-laid in glazed tiles, and a gilded
crest along the hall's ridge. Level 4's mark across the set is the glazed roof: a lattice of the
category's dark shade and gold diamonds over the tiles. Models only -- the game stops at level 3
for now.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402,F401
import kit  # noqa: E402
from details import G_TOP, L_TOP, STOREY, U_TOP, WALL_H  # noqa: E402,F401

OBJ = "Castle1-lvl4"
L3 = kit.borrow("castle1_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5


def build(lo, hi):
    b = L3['build'](lo, hi)
    J = d.JETTY
    d.glazed(b, 0, 1.0, 'x', 1.75 + J + 0.1, 0.9, U_TOP, 'roof_blue_dark')
    t_top = WALL_H + 0.4
    for sx in (-1, 1):
        for sy in (-1, 1):
            d.glazed_pyramid(b, sx * 2.06, sy * 2.06, t_top + 0.06, 0.43, 0.43, t_top + 0.52,
                             'roof_blue_dark', crest=False)
    bell1 = U_TOP + 0.38 + 0.44
    d.glazed_pyramid(b, 0, 0.04, bell1 + 0.01, 0.52, 0.56, bell1 + 0.5, 'roof_blue_dark',
                     crest=False)
    return b


kit.run(OBJ, build, source_name="Castle1-lvl1", offset=(LVL4_DX, 0, 0),
        collection="Город")
