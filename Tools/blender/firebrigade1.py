"""FireBrigade1-lvl1 -- the Fire Brigade (Пожарная бригада). Footprint 2x2. Front is -Y.

A stone engine house with its gable to the street and big red doors, a lookout tower beside it
with a bell under a pointed roof, a water cart in front, a row of red fire buckets and a ladder
against the wall.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "FireBrigade1-lvl1"


def build(lo, hi):
    b = Builder()
    top = hi[2]

    # engine house, ridge along y so the gable faces the street; one standard storey
    x0, x1, y0, y1 = -0.86, 0.3, -0.5, 0.82
    xc, hs = (x0 + x1) / 2, (x1 - x0) / 2
    zr = G_TOP + hs * d.PITCH
    d.footing(b, x0, x1, y0, y1)
    b.box(x0, x1, y0, y1, FOOTING, G_TOP, 'stone_light', bevel=d.WALL_BEVEL)
    b.gable_block('y', y0, y1, x0, x1, G_TOP, zr, 'plaster')
    d.roof(b, xc, (y0 + y1) / 2, 'y', (y1 - y0) / 2 + 0.09, hs, G_TOP)

    front = Facade(b, '-y', y0)
    dh = d.DOOR_H
    front.rect(xc - 0.36, xc + 0.36, FOOTING, FOOTING + dh + 0.04, 0.015, 'wood_dark')
    for s in (xc - 0.17, xc + 0.17):  # the two door leaves, braced in a Z
        front.rect(s - 0.16, s + 0.16, FOOTING, FOOTING + dh, 0.03, 'banner_red')
        front.rect(s - 0.16, s + 0.16, FOOTING + dh * 0.46, FOOTING + dh * 0.54, 0.04, 'wood_dark')
        front.beam(s - 0.13, FOOTING + 0.03, s + 0.13, FOOTING + dh * 0.46, 0.035, 0.04, 'wood_dark')
    cz = G_TOP + (zr - G_TOP) * 0.4
    front.disc(xc, cz, 0.13, 0.015, 'gold')
    front.disc(xc, cz, 0.1, 0.025, 'banner_red')
    front.poly([(xc - 0.05, cz - 0.07), (xc + 0.05, cz - 0.07), (xc + 0.035, cz + 0.01),
                (xc, cz + 0.07), (xc - 0.035, cz + 0.01)], 0.03, 'gold')
    for s in (0.0, 0.5):
        d.arch_window(Facade(b, '-x', x0), s, FOOTING)
    d.arch_window(Facade(b, '+y', y1), xc, FOOTING)
    d.arch_window(Facade(b, '+x', x1), -0.25, FOOTING)

    # lookout tower with the alarm bell
    tx, ty, th = 0.64, 0.38, 0.25
    b.box(tx - th, tx + th, ty - th, ty + th, 0, 0.98, 'stone_light', bevel=0.02)
    b.box(tx - th - 0.04, tx + th + 0.04, ty - th - 0.04, ty + th + 0.04, 0.94, 1.02, 'stone_mid')
    for px in (tx - th + 0.03, tx + th - 0.03):
        for py in (ty - th + 0.03, ty + th - 0.03):
            b.box(px - 0.03, px + 0.03, py - 0.03, py + 0.03, 1.02, 1.25, 'wood_dark')
    for nm, plane, s in (('-y', ty - th - 0.01, tx), ('+y', ty + th + 0.01, tx),
                         ('-x', tx - th - 0.01, ty), ('+x', tx + th + 0.01, ty)):
        Facade(b, nm, plane).rect(s - th, s + th, 1.08, 1.11, 0.025, 'wood_mid')
    b.cylinder(tx, ty, 1.08, 1.19, 0.07, 'gold')
    b.box(tx - th - 0.02, tx + th + 0.02, ty - th - 0.02, ty + th + 0.02, 1.24, 1.27, 'wood_dark')
    b.pyramid(tx, ty, 1.27, th + 0.07, th + 0.07, top - 0.02, 'roof_orange')
    tf = Facade(b, '-y', ty - th)
    d.slit(tf, tx, 0.45, 0.68)
    d.slit(Facade(b, '+x', tx + th), ty, 0.45, 0.68)

    # ladder up the tower's front
    for sx in (-0.09, 0.09):
        b.rod((tx + sx, ty - th - 0.2, 0.02), (tx + sx, ty - th - 0.02, 0.9), 0.025, 'wood_light')
    for i in range(5):
        t = (i + 1) / 6
        b.box(tx - 0.09, tx + 0.09, ty - th - 0.2 + 0.18 * t - 0.01, ty - th - 0.2 + 0.18 * t + 0.01,
              0.9 * t - 0.01, 0.9 * t + 0.01, 'wood_light', skip=())

    # yard: water cart, buckets by the door
    cx, cy = -0.28, -0.8
    b.box(cx - 0.2, cx + 0.2, cy - 0.11, cy + 0.11, 0.1, 0.14, 'wood_mid')
    b.hcylinder('x', (cx, cy, 0.25), 0.15, 0.1, 'wood_light', sides=8)
    for sx in (-0.11, 0.11):
        b.box(cx + sx - 0.012, cx + sx + 0.012, cy - 0.105, cy + 0.105, 0.14, 0.36, 'iron')
    for sy in (-0.13, 0.13):
        b.hcylinder('y', (cx, cy + sy, 0.1), 0.02, 0.1, 'wood_dark', sides=8)
    b.rod((cx + 0.2, cy, 0.12), (cx + 0.5, cy, 0.05), 0.03, 'wood_dark')
    for x in (0.12, 0.24, 0.36):
        d.bucket(b, x, -0.6, 0, color='banner_red')
    return b


kit.run(OBJ, build)
