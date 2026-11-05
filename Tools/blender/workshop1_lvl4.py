"""Workshop1-lvl4 -- the Workshop (Мастерская) at level 4. Footprint 3x3.

Level 3 (workshop1_lvl3.build) with the hall's roof and the timber store's lean-to re-laid in
glazed tiles (the set's level-4 mark: a lattice of dark and gold diamonds) and a gilded crest
along the ridge.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import detail  # noqa: E402,F401
import kit  # noqa: E402
from details import FOOTING, G_TOP  # noqa: E402,F401
from kit import Facade  # noqa: E402,F401

OBJ = "Workshop1-lvl4"
L3 = kit.borrow("workshop1_lvl3.py")
L1 = L3['L1']
LVL4_DX = L3['LVL3_DX'] * 1.5

X0, X1, Y0, Y1 = -0.78, 0.78, -0.45, 1.32
YC, HS = (Y0 + Y1) / 2, (X1 - X0) / 2
ZR = L1['WALL_TOP'] + HS * d.PITCH


def build(lo, hi):
    b = L3['build'](lo, hi)
    d.glazed(b, 0.0, YC, 'y', (Y1 - Y0) / 2 + 0.08, HS + 0.02, L1['WALL_TOP'], 'roof_blue_dark')
    d.glazed_shed(b, 'y', 0.06, 1.34, X1, 1.38, 0.88, 0.6, 'roof_blue_dark')
    return b


kit.run(OBJ, build, source_name="Barracks1-lvl1", offset=(LVL4_DX, -6.0, 0))
