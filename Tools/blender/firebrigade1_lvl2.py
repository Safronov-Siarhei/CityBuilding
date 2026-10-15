"""FireBrigade1-lvl2 -- the Fire Brigade (Пожарная бригада) after its first upgrade.
Footprint 2x2 (level 1's). Front is -Y.

Level 1 with quoined corners on the engine house, the lookout tower raised by RAISE with a red
pennant on its roof, a lamp by the doors, a hose reel and a water trough in the yard.
Level 1, for reference:


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

OBJ = "FireBrigade1-lvl2"
RAISE = 0.3      # how much taller the lookout tower stands than at level 1
LVL2_DX = 2.6   # level 2 stands this far +X of level 1


def build(lo, hi):
    b = Builder()
    top = hi[2]

    # engine house, ridge along y so the gable faces the street; one standard storey
    x0, x1, y0, y1 = -0.86, 0.3, -0.5, 0.82
    xc, hs = (x0 + x1) / 2, (x1 - x0) / 2
    zr = G_TOP + hs * d.PITCH
    d.footing(b, x0, x1, y0, y1)
    b.box(x0, x1, y0, y1, FOOTING, G_TOP, 'stone_light', bevel=d.WALL_BEVEL)
    for px, sx in ((x0, -1), (x1, 1)):
        for py, sy in ((y0, -1), (y1, 1)):
            d.quoins(b, px, py, sx, sy, FOOTING, G_TOP)
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
    d.wall_lantern(front, xc + 0.45, FOOTING + 0.36)
    for s in (0.0, 0.5):
        d.arch_window(Facade(b, '-x', x0), s, FOOTING)
    d.arch_window(Facade(b, '+y', y1), xc, FOOTING)
    d.arch_window(Facade(b, '+x', x1), -0.25, FOOTING)

    # lookout tower with the alarm bell
    tx, ty, th = 0.64, 0.38, 0.25
    R = RAISE
    b.box(tx - th, tx + th, ty - th, ty + th, 0, 0.98 + R, 'stone_light', bevel=0.02)
    b.box(tx - th - 0.04, tx + th + 0.04, ty - th - 0.04, ty + th + 0.04, 0.94 + R, 1.02 + R, 'stone_mid')
    for px in (tx - th + 0.03, tx + th - 0.03):
        for py in (ty - th + 0.03, ty + th - 0.03):
            b.box(px - 0.03, px + 0.03, py - 0.03, py + 0.03, 1.02 + R, 1.25 + R, 'wood_dark')
    for nm, plane, s in (('-y', ty - th - 0.01, tx), ('+y', ty + th + 0.01, tx),
                         ('-x', tx - th - 0.01, ty), ('+x', tx + th + 0.01, ty)):
        Facade(b, nm, plane).rect(s - th, s + th, 1.08 + R, 1.11 + R, 0.025, 'wood_mid')
    b.cylinder(tx, ty, 1.08 + R, 1.19 + R, 0.08, 'gold')
    b.box(tx - th - 0.02, tx + th + 0.02, ty - th - 0.02, ty + th + 0.02, 1.24 + R, 1.27 + R, 'wood_dark')
    b.pyramid(tx, ty, 1.27 + R, th + 0.07, th + 0.07, top - 0.02 + R, 'roof_orange')
    b.box(tx - 0.01, tx + 0.01, ty - 0.01, ty + 0.01, top - 0.06 + R, top + 0.2 + R, 'wood_dark')
    b.prism([(tx + 0.01, top + 0.19 + R), (tx + 0.01, top + 0.07 + R), (tx + 0.2, top + 0.13 + R)],
            (0, ty + 0.005, 0), (1, 0, 0), (0, 0, 1), 0.01, 'banner_red', back_cap=True)
    d.slit(Facade(b, '-y', ty - th), tx, 0.85, 1.05)
    tf = Facade(b, '-y', ty - th)
    d.slit(tf, tx, 0.45, 0.68)
    d.slit(Facade(b, '+x', tx + th), ty, 0.45, 0.68)

    # ladder up the tower's front
    for sx in (-0.09, 0.09):
        b.rod((tx + sx, ty - th - 0.2, 0.02), (tx + sx, ty - th - 0.02, 0.9 + R), 0.025, 'wood_light')
    for i in range(5):
        t = (i + 1) / 6
        b.box(tx - 0.09, tx + 0.09, ty - th - 0.2 + 0.18 * t - 0.01, ty - th - 0.2 + 0.18 * t + 0.01,
              (0.9 + R) * t - 0.01, (0.9 + R) * t + 0.01, 'wood_light', skip=())

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
    # a hose reel on its stand, and a trough to fill the buckets from
    rx, ry = 0.7, -0.4
    for sy in (-0.09, 0.09):
        b.box(rx - 0.02, rx + 0.02, ry + sy - 0.012, ry + sy + 0.012, 0, 0.27, 'wood_dark')
        b.hcylinder('y', (rx, ry + sy * 0.8, 0.22), 0.012, 0.12, 'banner_red', sides=8)
    b.hcylinder('y', (rx, ry, 0.22), 0.065, 0.085, 'burlap', sides=8)
    d.trough(b, 0.45, 0.92, -0.84)
    return b


kit.run(OBJ, build, source_name="FireBrigade1-lvl1", offset=(LVL2_DX, 0, 0),
        collection="Город")
