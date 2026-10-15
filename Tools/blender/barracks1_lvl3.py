"""Barracks1-lvl3 -- the Barracks (Казармы) at its last upgrade. Footprint 3x3 (level 1's).
Front is -Y.

Level 2 (barracks1_lvl2.build) with a stone gateway into the drill yard -- two piers under blue
caps with gold finials, a lintel and a blue banner -- in the gap of the rail fence, gold finials
on the quarters' ridge ends and a gold weathervane on the ridge.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import JETTY as J, U_TOP  # noqa: E402
from kit import Facade  # noqa: E402

OBJ = "Barracks1-lvl3"
L2 = kit.borrow("barracks1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    x0, x1, y0, y1 = -1.36, 1.36, 0.22, 1.32
    ux1, uy0, uy1 = x1 + J, y0 - J, y1 + J
    yc, hs = (uy0 + uy1) / 2, (uy1 - uy0) / 2
    zr = U_TOP + hs * d.PITCH
    for ex in (-ux1 - 0.05, ux1 + 0.05):
        d.finial(b, ex, yc, zr + 0.06)
    d.weathervane(b, 0.0, yc, zr + 0.07, h=0.26)

    # the gateway in the gap of the yard fence
    gy, ph = -1.4, 0.66
    for px in (-0.36, 0.36):
        b.box(px - 0.07, px + 0.07, gy - 0.07, gy + 0.07, 0, ph, 'stone_light', bevel=0.01)
        b.box(px - 0.085, px + 0.085, gy - 0.085, gy + 0.085, ph, ph + 0.04, 'stone_mid')
        b.pyramid(px, gy, ph + 0.04, 0.09, 0.09, ph + 0.24, 'roof_blue')
        d.finial(b, px, gy, ph + 0.23, h=0.1)
    b.box(-0.3, 0.3, gy - 0.05, gy + 0.05, ph - 0.1, ph - 0.02, 'wood_dark')
    d.banner(Facade(b, '-y', gy - 0.05), 0, ph - 0.1, 0.22, w=0.14, color='banner_blue')
    return b


kit.run(OBJ, build, source_name="Barracks1-lvl1", offset=(LVL3_DX, 0, 0),
        collection="Оборонительные здания")
