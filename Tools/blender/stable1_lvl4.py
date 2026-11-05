"""Stable1-lvl4 -- the Stable (Конюшня) at level 4. Footprint 3x3.

Level 3 (stable1_lvl3.build) with the stable roof and both stall lean-tos re-laid in glazed tiles
(the set's level-4 mark: a lattice of dark and gold diamonds) and a gilded crest along the ridge.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import detail  # noqa: E402,F401
import kit  # noqa: E402
from details import FOOTING, G_TOP, L_TOP  # noqa: E402,F401
from kit import Facade  # noqa: E402,F401

OBJ = "Stable1-lvl4"
L3 = kit.borrow("stable1_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5

X0, X1, Y0, Y1 = -1.36, 1.36, 0.3, 1.32   # the stable block (stable1.build)
YC, HS = (Y0 + Y1) / 2, (Y1 - Y0) / 2
ZR = L_TOP + HS * d.PITCH


def build(lo, hi):
    b = L3['build'](lo, hi)
    d.glazed(b, 0.0, YC, 'x', X1 + 0.04, HS, L_TOP, 'roof_blue_dark')
    for xa, xb in ((X0 + 0.02, -0.32), (0.32, X1 - 0.02)):
        d.glazed_shed(b, 'x', xa - 0.04, xb + 0.04, Y0, -0.22, G_TOP + 0.08, 0.66, 'roof_blue_dark')
    return b


kit.run(OBJ, build, source_name="Barracks1-lvl1", offset=(LVL4_DX, -12.0, 0))
