"""Workshop1-lvl2 -- the Workshop (Мастерская) after its first upgrade. Footprint 3x3.

Level 1 (workshop1.build) with quoined stone corners, a louvred vent on the ridge (the forge and
the sawdust), a dormer in each side slope, a rail fence across the yard open in the middle, and a
stack of finished wheels by the timber store.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import detail  # noqa: E402,F401
import kit  # noqa: E402
from details import FOOTING, G_TOP  # noqa: E402,F401
from kit import Facade  # noqa: E402,F401

OBJ = "Workshop1-lvl2"
L1 = kit.borrow("workshop1.py")
LVL2_DX = 3.6   # each level stands this far +X of the one before

X0, X1, Y0, Y1 = -0.78, 0.78, -0.45, 1.32
YC, HS = (Y0 + Y1) / 2, (X1 - X0) / 2
ZR = L1['WALL_TOP'] + HS * d.PITCH


def build(lo, hi):
    b = L1['build'](lo, hi)
    for px, sx in ((X0, -1), (X1, 1)):
        for py, sy in ((Y0, -1), (Y1, 1)):
            d.quoins(b, px, py, sx, sy, FOOTING, G_TOP)
    d.louvre(b, 0.0, 0.85, ZR - 0.08, roof='roof_blue')
    d.dormer(b, '-x', X0, 0.3, L1['WALL_TOP'], h=0.36, depth=0.4, **d.DEFENCE)
    d.rail_fence(b, [(-1.42, -1.42), (-0.32, -1.42)], h=0.22)
    d.rail_fence(b, [(0.32, -1.42), (1.42, -1.42)], h=0.22)
    for i in range(3):
        b.cylinder(1.18, -0.05, i * 0.04, i * 0.04 + 0.035, 0.1, 'wood_dark' if i % 2 else 'wood_mid', sides=10)
    return b


kit.run(OBJ, build, source_name="Barracks1-lvl1", offset=(LVL2_DX, -6.0, 0))
