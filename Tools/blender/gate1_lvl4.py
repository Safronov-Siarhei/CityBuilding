"""Gate1-lvl4 -- the Gate (Ворота) at level 4.

Footprint 1x2 (level 1's). Level 3 (gate1_lvl3.build) with the little roof over the beam and the
piers' caps re-laid in glazed tiles, with a gilded crest; the palisade joins are untouched.
Level 4's mark across the set is the glazed roof: a lattice of the category's dark shade and
gold diamonds over the tiles. Models only -- the game stops at level 3 for now.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402,F401
import kit  # noqa: E402
from details import G_TOP, L_TOP, STOREY, U_TOP, WALL_H  # noqa: E402,F401

OBJ = "Gate1-lvl4"
L3 = kit.borrow("gate1_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5


def build(lo, hi):
    b = L3['build'](lo, hi)
    d.glazed(b, 0, 0, 'y', 0.56, 0.16, 1.08, 'roof_blue_dark', overhang=0.04, t=0.036, w=0.04,
             step=0.13, row=0.1)
    for py in (-0.44, 0.44):
        d.glazed_pyramid(b, 0, py, 1.17, 0.15, 0.11, 1.34, 'roof_blue_dark', w=0.035, step=0.12,
                         row=0.09, crest=False)
    return b


kit.run(OBJ, build, source_name="Gate1-lvl1", offset=(LVL4_DX, 0, 0),
        collection="Оборонительные здания")
