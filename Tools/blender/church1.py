"""Church1-lvl1 -- the Church (Церковь). Footprint 3x3. Front is -Y.

A stone nave running back from the street under a violet roof, tall lancet windows between
buttresses, a rose window in the back gable, and a square bell tower on the front: arched
door, clock-less belfry with a gold bell, a tall violet spire with a gold cross. A path and
two trees in front.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP, U_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "Church1-lvl1"
LANCET_H = 0.62   # a church window spans most of the nave's height


def build(lo, hi):
    b = Builder()
    top = hi[2]

    # nave: one tall storey (two standard ones), ridge along y
    x0, x1, y0, y1 = -0.66, 0.66, -0.55, 1.35
    hs = (x1 - x0) / 2
    zr = U_TOP + hs * d.PITCH
    d.footing(b, x0, x1, y0, y1)
    b.box(x0, x1, y0, y1, FOOTING, U_TOP, 'stone_light', bevel=0.02)
    b.gable_block('y', y0, y1, x0, x1, U_TOP, zr, 'stone_light')
    d.roof(b, 0, (y0 + y1) / 2, 'y', (y1 - y0) / 2 + 0.1, hs, U_TOP, **d.PURPLE)
    for nm, plane, sx in (('-x', x0, -1), ('+x', x1, 1)):
        f = Facade(b, nm, plane)
        for s in (-0.15, 0.45, 1.05):
            f.arch(s, FOOTING + 0.25, d.ARCH_W, LANCET_H, 0.01, d.GLASS)
            f.rect(s - 0.12, s + 0.12, FOOTING + 0.22, FOOTING + 0.25, 0.03, 'stone_mid')
        for s in (0.15, 0.75):  # buttresses between the windows
            bx0, bx1 = (x1, x1 + 0.1) if sx > 0 else (x0 - 0.1, x0)
            b.box(bx0, bx1, s - 0.05, s + 0.05, 0, U_TOP - 0.1, 'stone_mid')
    back = Facade(b, '+y', y1)
    back.disc(0, U_TOP + 0.22, 0.2, 0.015, 'stone_mid')
    back.disc(0, U_TOP + 0.22, 0.16, 0.025, 'roof_purple')
    back.disc(0, U_TOP + 0.22, 0.07, 0.035, 'gold')
    back.arch(0, FOOTING + 0.25, d.ARCH_W, LANCET_H, 0.01, d.GLASS)

    # bell tower on the front
    tx, ty, th = 0.0, -0.88, 0.4
    stone_top = U_TOP + 0.62
    bell0, bell1 = stone_top + 0.08, stone_top + 0.42
    d.footing(b, tx - th, tx + th, ty - th, ty + th)
    b.box(tx - th, tx + th, ty - th, ty + th, FOOTING, stone_top, 'stone_light', bevel=0.025)
    b.box(tx - th - 0.04, tx + th + 0.04, ty - th - 0.04, ty + th + 0.04, G_TOP - 0.03, G_TOP + 0.03,
          'stone_mid')
    b.box(tx - th - 0.05, tx + th + 0.05, ty - th - 0.05, ty + th + 0.05, stone_top - 0.04, bell0,
          'stone_mid', bevel=0.015)
    b.box(tx - th + 0.03, tx + th - 0.03, ty - th + 0.03, ty + th - 0.03, bell0, bell1, 'stone_light')
    for nm, plane, s in (('-y', ty - th + 0.03, tx), ('+y', ty + th - 0.03, tx),
                         ('-x', tx - th + 0.03, ty), ('+x', tx + th - 0.03, ty)):
        Facade(b, nm, plane).arch(s, bell0 + 0.05, 0.26, 0.26, 0.01, 'window')
    b.box(tx - 0.08, tx + 0.08, ty - 0.08, ty + 0.08, bell0 + 0.1, bell0 + 0.24, 'gold')
    b.box(tx - th, tx + th, ty - th, ty + th, bell1, bell1 + 0.05, 'stone_mid')
    cross = top - 0.16
    b.pyramid(tx, ty, bell1 + 0.05, th + 0.04, th + 0.04, cross, 'roof_purple')
    b.box(tx - 0.015, tx + 0.015, ty - 0.015, ty + 0.015, cross - 0.05, top, 'gold')
    b.box(tx - 0.06, tx + 0.06, ty - 0.015, ty + 0.015, top - 0.08, top - 0.05, 'gold')

    f = Facade(b, '-y', ty - th)
    d.gate(f, tx, FOOTING, w=0.32, h=0.54)
    f.disc(tx, U_TOP - 0.15, 0.13, 0.015, 'stone_mid')
    f.disc(tx, U_TOP - 0.15, 0.1, 0.025, 'roof_purple')
    d.arch_window(f, tx, U_TOP, w=0.14)
    for nm, plane in (('-x', tx - th), ('+x', tx + th)):
        d.arch_window(Facade(b, nm, plane), ty, G_TOP)
    b.box(tx - 0.3, tx + 0.3, ty - th - 0.14, ty - th, 0, 0.05, 'stone_mid', bevel=0.008)

    # churchyard
    b.box(-0.2, 0.2, -1.48, ty - th - 0.14, 0, 0.012, 'sand')
    d.tree(b, -0.95, -1.05)
    d.tree(b, 0.95, -0.9, r=0.2)
    for x, y in ((-0.95, 0.3), (0.98, 1.0)):
        d.bush(b, x, y, r=0.1)
    return b


kit.run(OBJ, build)
