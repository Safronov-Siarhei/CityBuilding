"""Treasury1-lvl1 -- the Treasury (Сокровищница): bars, gold, coins. Footprint 3x2. Front -Y.

A squat two-storey stone strongroom: buttressed corners, an iron-banded arched gate under a gold
crest, barred windows, gold finials on a red roof, braziers either side of the steps. Two
standard storeys.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP, U_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "Treasury1-lvl1"


def crest(f, s, z):
    f.disc(s, z, 0.15, 0.015, 'gold')
    f.disc(s, z, 0.115, 0.025, 'roof_red')
    f.disc(s, z, 0.055, 0.035, 'gold')


def build(lo, hi):
    b = Builder()
    x0, x1, y0, y1 = -1.15, 1.15, -0.42, 0.78
    yc, hs = (y0 + y1) / 2, (y1 - y0) / 2
    zr = U_TOP + hs * d.PITCH

    d.footing(b, x0, x1, y0, y1)
    b.box(x0, x1, y0, y1, FOOTING, U_TOP, 'stone_light', bevel=0.02)
    b.box(x0 - 0.03, x1 + 0.03, y0 - 0.03, y1 + 0.03, G_TOP - 0.03, G_TOP + 0.03, 'stone_mid')
    for px in (x0, x1):
        for py in (y0, y1):
            b.box(px - 0.07, px + 0.07, py - 0.07, py + 0.07, 0, U_TOP - 0.06, 'stone_mid',
                  bevel=0.012)
    b.gable_block('x', x0, x1, y0, y1, U_TOP, zr, 'stone_light')
    d.roof(b, 0, yc, 'x', x1 + 0.1, hs, U_TOP, **d.RED)
    for px in (x0 - 0.04, x1 + 0.04):
        b.box(px - 0.035, px + 0.035, yc - 0.035, yc + 0.035, zr + 0.08, zr + 0.2, 'gold')

    f = Facade(b, '-y', y0)
    d.gate(f, 0, FOOTING)
    crest(f, 0, G_TOP + 0.27)
    for s in (-0.75, 0.75):
        d.barred_window(f, s, FOOTING)
        d.barred_window(f, s, G_TOP)
    for nm, plane, ss in (('+y', y1, (-0.6, 0.0, 0.6)), ('-x', x0, (yc,)), ('+x', x1, (yc,))):
        f = Facade(b, nm, plane)
        for s in ss:
            d.barred_window(f, s, G_TOP)
    for nm, plane in (('-x', x0), ('+x', x1)):
        Facade(b, nm, plane).disc(yc, U_TOP + (zr - U_TOP) * 0.4, 0.08, 0.02, d.GLASS)

    b.box(-0.36, 0.36, y0 - 0.22, y0, 0, 0.05, 'stone_mid', bevel=0.008)
    b.box(-0.36, 0.36, y0 - 0.12, y0, 0.05, FOOTING, 'stone_mid', bevel=0.008)
    d.brazier(b, -0.5, y0 - 0.3)
    d.brazier(b, 0.5, y0 - 0.3)
    b.box(-0.24, 0.24, -0.98, y0 - 0.22, 0, 0.012, 'cobble')
    return b


kit.run(OBJ, build)
