"""Gate1-lvl3 -- the Gate (Ворота), wooden tier, at its last upgrade. Footprint 1x1 (level 1's);
the palisade comes in from both ends exactly as at level 1.

Level 2 (gate1_lvl2.build) with blue pennants on gold-tipped poles over both piers and a gilded
shield on each pier's faces.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from kit import Facade  # noqa: E402

OBJ = "Gate1-lvl3"
L2 = kit.borrow("gate1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    post_h = d.PALISADE_H + 0.42
    for py in (-L2['POST_Y'], L2['POST_Y']):
        d.pennant(b, 0, py, post_h + 0.17, h=0.28, color='banner_blue', length=0.16)
        for sx in (-1, 1):
            f = Facade(b, '-x' if sx < 0 else '+x', sx * 0.13)
            f.disc(py, 0.84, 0.07, 0.015, 'banner_blue')
            f.disc(py, 0.84, 0.035, 0.022, 'gold')
    return b


kit.run(OBJ, build, source_name="Gate1-lvl1", offset=(LVL3_DX, 0, 0),
        collection="Оборонительные здания")
