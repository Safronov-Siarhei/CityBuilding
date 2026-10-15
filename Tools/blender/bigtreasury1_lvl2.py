"""BigTreasury1-lvl2 -- the Big Treasury (Большая сокровищница) after its first upgrade.
Footprint 4x3 (level 1's). Front is -Y.

Level 1 with the towers raised by RAISE and flying red pennants, and a gilded cupola on the
ridge of the strongroom. Level 1, for reference:

The Treasury grown up: the same buttressed stone strongroom, longer, flanked by two square
towers with red pyramid roofs and gold finials, a grand iron-banded gate under the gold crest,
red-and-gold banners, barred windows, braziers either side of a broad flight of steps.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP, U_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "BigTreasury1-lvl2"
RAISE = 0.3      # how much taller the towers stand than at level 1
LVL2_DX = 4.6   # level 2 stands this far +X of level 1


def crest(f, s, z):
    f.disc(s, z, 0.15, 0.015, 'gold')
    f.disc(s, z, 0.115, 0.025, 'roof_red')
    f.disc(s, z, 0.055, 0.035, 'gold')


def build(lo, hi):
    b = Builder()
    top = hi[2] + RAISE

    # the strongroom
    x0, x1, y0, y1 = -1.2, 1.2, -0.3, 1.2
    yc, hs = (y0 + y1) / 2, (y1 - y0) / 2
    zr = U_TOP + hs * d.PITCH
    d.footing(b, x0, x1, y0, y1)
    b.box(x0, x1, y0, y1, FOOTING, U_TOP, 'stone_light', bevel=0.02)
    b.box(x0 - 0.03, x1 + 0.03, y0 - 0.03, y1 + 0.03, G_TOP - 0.03, G_TOP + 0.03, 'stone_mid')
    for px in (-0.45, 0.45):
        b.box(px - 0.07, px + 0.07, y0 - 0.07, y0 + 0.05, 0, U_TOP - 0.06, 'stone_mid', bevel=0.012)
    b.gable_block('x', x0, x1, y0, y1, U_TOP, zr, 'stone_light')
    d.roof(b, 0, yc, 'x', x1 + 0.1, hs, U_TOP, **d.RED)
    # a gilded cupola on the ridge
    b.cylinder(0, yc, zr - 0.1, zr + 0.24, 0.17, 'stone_light', sides=8, bevel=0.01)
    b.cylinder(0, yc, zr + 0.22, zr + 0.27, 0.2, 'stone_mid', sides=8)
    b.dome(0, yc, zr + 0.27, 0.19, 0.24, ['gold', 'plaster_yellow'])
    b.box(-0.03, 0.03, yc - 0.03, yc + 0.03, zr + 0.5, zr + 0.56, 'gold')
    f = Facade(b, '-y', y0)
    d.gate(f, 0, FOOTING)
    crest(f, 0, G_TOP + 0.27)
    d.banner(f, -0.25, U_TOP - 0.06, 0.4, w=0.12, color='roof_red')
    d.banner(f, 0.25, U_TOP - 0.06, 0.4, w=0.12, color='roof_red')
    for s in (-0.8, 0.8):
        d.barred_window(f, s, FOOTING)
        d.barred_window(f, s, G_TOP)
    f = Facade(b, '+y', y1)
    for s in (-0.8, 0.0, 0.8):
        d.barred_window(f, s, G_TOP)

    # the two towers on the front corners
    t_top = U_TOP + 0.3 + RAISE
    for sx in (-1, 1):
        cx, cy, th = sx * 1.58, 0.05, 0.36
        b.box(cx - th, cx + th, cy - th, cy + th, 0, t_top, 'stone_light', bevel=0.025)
        b.box(cx - th - 0.05, cx + th + 0.05, cy - th - 0.05, cy + th + 0.05, t_top - 0.08,
              t_top + 0.06, 'stone_mid', bevel=0.015)
        b.pyramid(cx, cy, t_top + 0.06, th + 0.05, th + 0.05, top - 0.16, 'roof_red')
        b.box(cx - 0.012, cx + 0.012, cy - 0.012, cy + 0.012, top - 0.2, top - 0.05, 'wood_dark')
        b.box(cx - 0.035, cx + 0.035, cy - 0.035, cy + 0.035, top - 0.07, top, 'gold')
        b.box(cx - 0.008, cx + 0.008, cy - 0.008, cy + 0.008, top, top + 0.22, 'wood_dark')
        b.prism([(cx + 0.008, top + 0.21), (cx + 0.008, top + 0.09), (cx + 0.2, top + 0.15)],
                (0, cy + 0.005, 0), (1, 0, 0), (0, 0, 1), 0.01, 'roof_red', back_cap=True)
        tf = Facade(b, '-y', cy - th)
        d.barred_window(tf, cx, FOOTING, w=0.16)
        d.barred_window(tf, cx, G_TOP, w=0.16)
        d.slit(Facade(b, '+x' if sx > 0 else '-x', cx + sx * th), cy, G_TOP + 0.2, G_TOP + 0.44)

    # steps, braziers, paving
    b.box(-0.5, 0.5, y0 - 0.3, y0, 0, 0.05, 'stone_mid', bevel=0.008)
    b.box(-0.5, 0.5, y0 - 0.16, y0, 0.05, FOOTING, 'stone_mid', bevel=0.008)
    for s in (-0.66, 0.66):
        d.brazier(b, s, y0 - 0.4)
    b.box(-0.34, 0.34, -1.48, y0 - 0.3, 0, 0.012, 'cobble')
    return b


kit.run(OBJ, build, source_name="BigTreasury1-lvl1", offset=(LVL2_DX, 0, 0),
        collection="Склады")
