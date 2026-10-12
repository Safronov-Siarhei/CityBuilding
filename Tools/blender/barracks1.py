"""Barracks1-lvl1 -- the Barracks (Казармы). Footprint 3x3. Front is -Y.

A long two-storey quarters along the back -- stone below, half-timber above, blue roof, two
chimneys, blue banners by the door -- and in front a sanded drill yard: straw training dummies,
an archery target, a weapon rack of spears and shields, a banner pole. Two standard storeys.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP, JETTY as J, U_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "Barracks1-lvl1"


def dummy(b, x, y):
    b.box(x - 0.02, x + 0.02, y - 0.02, y + 0.02, 0, 0.42, 'wood_dark')
    b.blob(x, y, 0.16, 0.08, 0.26, 'hay', sides=6)
    b.box(x - 0.16, x + 0.16, y - 0.015, y + 0.015, 0.3, 0.33, 'wood_dark')
    b.blob(x, y, 0.4, 0.05, 0.1, 'burlap', sides=5)


def build(lo, hi):
    b = Builder()
    x0, x1, y0, y1 = -1.36, 1.36, 0.22, 1.32
    ux0, ux1, uy0, uy1 = x0 - J, x1 + J, y0 - J, y1 + J
    yc, hs = (uy0 + uy1) / 2, (uy1 - uy0) / 2
    zr = U_TOP + hs * d.PITCH

    d.footing(b, x0, x1, y0, y1)
    b.box(x0, x1, y0, y1, FOOTING, G_TOP, 'stone_light', bevel=d.WALL_BEVEL)
    b.box(ux0, ux1, uy0, uy1, G_TOP, U_TOP, 'plaster', bevel=d.WALL_BEVEL)
    b.gable_block('x', ux0, ux1, uy0, uy1, U_TOP, zr, 'plaster')
    d.roof(b, 0, yc, 'x', ux1 + 0.09, hs, U_TOP, **d.DEFENCE)
    d.chimney(b, -0.9, 1.0, 1.3, zr + 0.1)
    d.chimney(b, 0.9, 1.0, 1.3, zr + 0.1)

    f = Facade(b, '-y', y0)
    d.arch_door(f, 0)
    for s in (-1.05, -0.55, 0.55, 1.05):
        d.barred_window(f, s, FOOTING)
    d.banner(f, -0.28, G_TOP - 0.04, 0.34, w=0.13, color='banner_blue')
    d.banner(f, 0.28, G_TOP - 0.04, 0.34, w=0.13, color='banner_blue')
    d.step(b, -0.2, 0.2, y0)
    f = Facade(b, '-y', uy0)
    d.timber(f, ux0, ux1, G_TOP, U_TOP, (ux0 + 0.025, -0.8, -0.3, 0.3, 0.8, ux1 - 0.025),
             ((-0.3, 0.3), (0.3, -0.3)))
    for s in (-1.1, -0.55, 0.55, 1.1):
        d.cross_window(f, s, G_TOP)
    f = Facade(b, '+y', uy1)
    d.timber(f, ux0, ux1, G_TOP, U_TOP, ())
    for s in (-0.8, 0.0, 0.8):
        d.plain_window(f, s, G_TOP)
    for nm, plane in (('-x', ux0), ('+x', ux1)):
        f = Facade(b, nm, plane)
        d.timber(f, uy0, uy1, G_TOP, U_TOP, (uy0 + 0.025, yc, uy1 - 0.025))
        d.plain_window(f, yc - 0.28, G_TOP)
        d.gable_frame(f, yc, hs, U_TOP, zr)

    # drill yard
    b.box(-1.42, 1.42, -1.42, y0 - 0.14, 0, 0.012, 'sand')
    for x, y in ((-1.0, -0.6), (-0.6, -0.95), (-1.05, -1.2)):
        dummy(b, x, y)
    # archery target
    b.rod((0.95, -1.05, 0.02), (0.95, -0.98, 0.42), 0.035, 'wood_dark')
    tf = Facade(b, '-y', -1.08)
    for r, c, dep in ((0.17, 'white', 0.02), (0.12, 'roof_red', 0.03), (0.06, 'gold', 0.04)):
        tf.disc(0.95, 0.38, r, dep, c)
    # weapon rack
    rx, ry = 0.35, -0.25
    b.box(rx - 0.32, rx + 0.32, ry - 0.03, ry + 0.03, 0.34, 0.37, 'wood_dark')
    for px in (rx - 0.3, rx + 0.3):
        b.box(px - 0.02, px + 0.02, ry - 0.02, ry + 0.02, 0, 0.4, 'wood_dark')
    for i in range(4):
        sx = rx - 0.2 + i * 0.13
        b.rod((sx, ry - 0.08, 0.01), (sx, ry + 0.01, 0.6), 0.02, 'wood_light')
        b.pyramid(sx, ry + 0.012, 0.6, 0.02, 0.02, 0.68, 'iron')
    rack = Facade(b, '-y', ry - 0.03)
    for s in (rx - 0.18, rx + 0.18):
        rack.disc(s, 0.2, 0.09, 0.02, 'banner_blue')
        rack.disc(s, 0.2, 0.035, 0.03, 'gold')
    # banner pole
    b.box(1.2, 1.23, -0.35, -0.32, 0, 1.15, 'wood_dark')
    b.prism([(1.23, 1.12), (1.23, 0.82), (1.5, 0.97)], (0, -0.335, 0), (1, 0, 0), (0, 0, 1), 0.012,
            'banner_blue', back_cap=True)
    return b


kit.run(OBJ, build)
