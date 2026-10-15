"""Treasury1-lvl2 -- the Treasury (Сокровищница) after its first upgrade. Footprint 3x2 (level
1's). Front -Y.

Level 1 with its front buttresses carried up into pinnacles with red caps and gold finials,
red banners either side of the gate, and gold-capped stone posts where the path begins.
Level 1, for reference:


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

OBJ = "Treasury1-lvl2"
LVL2_DX = 3.6   # level 2 stands this far +X of level 1


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
    for px in (x0, x1):   # the front buttresses become pinnacles
        b.box(px - 0.075, px + 0.075, y0 - 0.075, y0 + 0.075, U_TOP - 0.06, U_TOP + 0.24, 'stone_mid',
              bevel=0.012)
        b.pyramid(px, y0, U_TOP + 0.24, 0.09, 0.09, U_TOP + 0.5, 'roof_red')
        b.box(px - 0.025, px + 0.025, y0 - 0.025, y0 + 0.025, U_TOP + 0.48, U_TOP + 0.55, 'gold')

    f = Facade(b, '-y', y0)
    d.gate(f, 0, FOOTING)
    crest(f, 0, G_TOP + 0.27)
    for s in (-0.42, 0.42):
        d.banner(f, s, U_TOP - 0.06, 0.36, w=0.12, color='roof_red')
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
    for s in (-0.34, 0.34):   # gold-capped posts where the path begins
        b.box(s - 0.05, s + 0.05, -0.97, -0.87, 0, 0.26, 'stone_light', bevel=0.008)
        b.cylinder(s, -0.92, 0.26, 0.33, 0.04, 'gold', sides=6)
    return b


kit.run(OBJ, build, source_name="Treasury1-lvl1", offset=(LVL2_DX, 0, 0),
        collection="Склады")
