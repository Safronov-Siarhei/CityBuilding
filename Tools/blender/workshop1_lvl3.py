"""Workshop1-lvl3 -- the Workshop (Мастерская) at level 3. Footprint 3x3.

Level 2 (workshop1_lvl2.build) crowned: gold finials on the gable ends, a gold weathervane on the
louvre, a stone gateway into the yard in the gap of the fence -- two piers under blue caps with
gold finials, a lintel with a blue banner -- and a paved way from it to the doors.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import detail  # noqa: E402,F401
import kit  # noqa: E402
from details import FOOTING, G_TOP  # noqa: E402,F401
from kit import Facade  # noqa: E402,F401

OBJ = "Workshop1-lvl3"
L2 = kit.borrow("workshop1_lvl2.py")
L1 = L2['L1']
LVL3_DX = 2 * L2['LVL2_DX']

X0, X1, Y0, Y1 = -0.78, 0.78, -0.45, 1.32
YC, HS = (Y0 + Y1) / 2, (X1 - X0) / 2
ZR = L1['WALL_TOP'] + HS * d.PITCH


def build(lo, hi):
    b = L2['build'](lo, hi)
    for ey in (Y0 - 0.06, Y1 + 0.06):
        d.finial(b, 0.0, ey, ZR + 0.05)
    d.weathervane(b, 0.0, 0.85, ZR + 0.38, h=0.22)
    gy, ph = -1.38, 0.62
    for px in (-0.36, 0.36):
        b.box(px - 0.065, px + 0.065, gy - 0.065, gy + 0.065, 0, ph, 'stone_light', bevel=0.01)
        b.box(px - 0.08, px + 0.08, gy - 0.08, gy + 0.08, ph, ph + 0.04, 'stone_mid')
        b.pyramid(px, gy, ph + 0.04, 0.085, 0.085, ph + 0.22, 'roof_blue')
        d.finial(b, px, gy, ph + 0.21, h=0.1)
    b.box(-0.3, 0.3, gy - 0.045, gy + 0.045, ph - 0.1, ph - 0.02, 'wood_dark')
    d.banner(Facade(b, '-y', gy - 0.045), 0, ph - 0.1, 0.2, w=0.13, color='banner_blue')
    d.paving(b, -0.3, 0.3, -1.36, Y0 - 0.1, z=0.016)
    return b


kit.run(OBJ, build, source_name="Barracks1-lvl1", offset=(LVL3_DX, -6.0, 0))
