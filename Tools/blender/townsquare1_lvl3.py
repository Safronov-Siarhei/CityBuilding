"""TownSquare1-lvl3 -- the Town Square (Городская площадь) at its last upgrade. Footprint 2x2
(level 1's).

Level 2 (townsquare1_lvl2.build) on festival day: a gilded statue crowns the fountain and strings
of coloured bunting run between the four corner lamps.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402

OBJ = "TownSquare1-lvl3"
L2 = kit.borrow("townsquare1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    d.statue(b, 0, 0, 0.99, scale=0.55)
    z = 0.62 + 0.06
    corners = [(-0.8, -0.8), (0.8, -0.8), (0.8, 0.8), (-0.8, 0.8)]
    for (ax, ay), (bx, by) in zip(corners, corners[1:] + corners[:1]):
        d.bunting(b, (ax, ay, z), (bx, by, z), sag=0.1, step=0.12)
    return b


kit.run(OBJ, build, source_name="TownSquare1-lvl1", offset=(LVL3_DX, 0, 0),
        collection="Развлечения")
