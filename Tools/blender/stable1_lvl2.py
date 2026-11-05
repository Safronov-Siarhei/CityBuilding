"""Stable1-lvl2 -- the Stable (Конюшня) after its first upgrade. Footprint 3x3.

Level 1 (stable1.build) with quoined stone corners at the back, two louvred vents on the ridge
(a stable must breathe), lanterns either side of the blue doors, a stone mounting block by the
doors, side rails closing the yard and a blue horse blanket drying on the fence.
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

OBJ = "Stable1-lvl2"
L1 = kit.borrow("stable1.py")
LVL2_DX = 3.6   # each level stands this far +X of the one before

X0, X1, Y0, Y1 = -1.36, 1.36, 0.3, 1.32   # the stable block (stable1.build)
YC, HS = (Y0 + Y1) / 2, (Y1 - Y0) / 2
ZR = L_TOP + HS * d.PITCH


def build(lo, hi):
    b = L1['build'](lo, hi)
    for px, sx in ((X0, -1), (X1, 1)):
        d.quoins(b, px, Y1, sx, 1, FOOTING, G_TOP)
    for x in (-0.7, 0.7):
        d.louvre(b, x, YC, ZR - 0.08, roof='roof_blue')
    f = Facade(b, '-y', Y0)
    for s in (-0.28, 0.28):
        d.wall_lantern(f, s, FOOTING + 0.3)
    # the mounting block: three stone steps beside the doors
    for i in range(3):
        b.box(-0.55, -0.37, -0.5 + i * 0.05, -0.32, 0, 0.06 + i * 0.05, 'stone_light', bevel=0.008)
    # side rails closing the yard
    d.rail_fence(b, [(-1.42, -1.42), (-1.42, -0.3)], h=0.24)
    d.rail_fence(b, [(1.42, -1.42), (1.42, -0.3)], h=0.24)
    # a blue horse blanket with a gold edge drying over the right rail
    b.box(0.55, 0.85, -1.47, -1.37, 0.12, 0.3, 'banner_blue', bevel=0.008)
    b.box(0.55, 0.85, -1.475, -1.365, 0.11, 0.13, 'gold')
    return b


kit.run(OBJ, build, source_name="Barracks1-lvl1", offset=(LVL2_DX, -12.0, 0))
