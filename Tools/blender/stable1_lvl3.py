"""Stable1-lvl3 -- the Stable (Конюшня) at level 3. Footprint 3x3.

Level 2 (stable1_lvl2.build) crowned: gold finials on the gable ends, a gold weathervane on the
ridge, gold caps on the stall posts, a paved apron before the doors, and a stone gateway in the gap
of the fence -- two piers under blue caps with gold finials, a lintel with a blue banner.
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

OBJ = "Stable1-lvl3"
L2 = kit.borrow("stable1_lvl2.py")
L1 = L2['L1']
LVL3_DX = 2 * L2['LVL2_DX']

X0, X1, Y0, Y1 = -1.36, 1.36, 0.3, 1.32   # the stable block (stable1.build)
YC, HS = (Y0 + Y1) / 2, (Y1 - Y0) / 2
ZR = L_TOP + HS * d.PITCH


def build(lo, hi):
    b = L2['build'](lo, hi)
    for ex in (X0 - 0.06, X1 + 0.06):
        d.finial(b, ex, YC, ZR + 0.05)
    d.weathervane(b, 0.0, YC, ZR + 0.05, h=0.22)
    # gold caps on the stall posts (stable1.stall_row: two rows of three posts, front at -0.16)
    for xa, xb in ((X0 + 0.02, -0.32), (0.32, X1 - 0.02)):
        for k in range(3):
            px = xa + k * (xb - xa) / 2
            b.pyramid(px, -0.16, 0.67, 0.035, 0.035, 0.73, 'gold', sides=4)
    d.paving(b, -0.27, 0.27, -0.62, Y0, z=0.016)
    gy, ph = -1.38, 0.62
    for px in (-0.36, 0.36):
        b.box(px - 0.065, px + 0.065, gy - 0.065, gy + 0.065, 0, ph, 'stone_light', bevel=0.01)
        b.box(px - 0.08, px + 0.08, gy - 0.08, gy + 0.08, ph, ph + 0.04, 'stone_mid')
        b.pyramid(px, gy, ph + 0.04, 0.085, 0.085, ph + 0.22, 'roof_blue')
        d.finial(b, px, gy, ph + 0.21, h=0.1)
    b.box(-0.3, 0.3, gy - 0.045, gy + 0.045, ph - 0.1, ph - 0.02, 'wood_dark')
    d.banner(Facade(b, '-y', gy - 0.045), 0, ph - 0.1, 0.2, w=0.13, color='banner_blue')
    return b


kit.run(OBJ, build, source_name="Barracks1-lvl1", offset=(LVL3_DX, -12.0, 0))
