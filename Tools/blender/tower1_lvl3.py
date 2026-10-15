"""Tower1-lvl3 -- the Tower (Башня), wooden tier, at its last upgrade. Footprint 1x1 (level 1's),
still filling its cell so the palisade butts into it.

Level 2 (tower1_lvl2.build) with a gold finial on the roof's pole and four small blue pennants
flying from the corners of the roof.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402

OBJ = "Tower1-lvl3"
L2 = kit.borrow("tower1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    top = hi[2] + 0.24
    p1 = 1.6 + 0.07
    d.finial(b, 0, 0, top, h=0.12)
    for sx in (-1, 1):
        for sy in (-1, 1):
            d.pennant(b, sx * 0.38, sy * 0.38, p1 + 0.44, h=0.26, color='banner_blue', length=0.1)
    return b


kit.run(OBJ, build, source_name="Tower1-lvl1", offset=(LVL3_DX, 0, 0),
        collection="Оборонительные здания")
