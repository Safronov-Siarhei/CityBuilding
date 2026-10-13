"""FisherHut1-lvl1 -- the Fisher's Hut (Рыбацкая хижина). Footprint 1x1. Front is -Y.

A small plank hut with a straw roof; beside it a drying rack hung with fish, a net drying over a
frame, a basket and a barrel of the catch, and an oar.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "FisherHut1-lvl1"
FISH = 'water'


def fish(b, x, y, z_top):
    b.zbox(x, y, z_top - 0.13, z_top - 0.03, 0.012, 0.03, 0, FISH, bevel=0.006)
    b.pyramid(x, y, z_top - 0.155, 0.01, 0.035, z_top - 0.12, FISH)
    b.box(x - 0.004, x + 0.004, y - 0.004, y + 0.004, z_top - 0.03, z_top, 'burlap')


def build(lo, hi):
    b = Builder()
    x0, x1, y0, y1 = -0.42, 0.14, -0.08, 0.36
    yc, hs = (y0 + y1) / 2, (y1 - y0) / 2
    d.footing(b, x0, x1, y0, y1)
    b.box(x0, x1, y0, y1, FOOTING, G_TOP, 'wood_light', bevel=d.WALL_BEVEL)
    for nm, plane, s0, s1 in (('-y', y0, x0, x1), ('+x', x1, y0, y1)):
        f = Facade(b, nm, plane)
        for z in (0.28, 0.46):
            f.rect(s0, s1, z - 0.01, z + 0.01, 0.006, 'wood_mid')
    for px in (x0, x1):
        for py in (y0, y1):
            b.box(px - 0.035, px + 0.035, py - 0.035, py + 0.035, FOOTING, G_TOP, 'wood_dark')
    zr = G_TOP + hs * d.PITCH
    b.gable_block('x', x0, x1, y0, y1, G_TOP, zr, 'wood_mid')
    d.roof(b, (x0 + x1) / 2, yc, 'x', (x1 - x0) / 2 + 0.06, hs, G_TOP, overhang=0.05, **d.STRAW)
    f = Facade(b, '-y', y0)
    d.plank_door(f, -0.27)
    d.plain_window(Facade(b, '+x', x1), yc, FOOTING, sill='wood_dark')

    # drying rack along the right edge, hung with fish
    rx = 0.36
    for y in (-0.36, 0.3):
        b.box(rx - 0.02, rx + 0.02, y - 0.02, y + 0.02, 0, 0.5, 'wood_dark')
    b.box(rx - 0.015, rx + 0.015, -0.4, 0.34, 0.47, 0.5, 'wood_mid')
    for y in (-0.24, -0.12, 0.0, 0.12, 0.2):
        fish(b, rx, y, 0.47)

    # net over a frame at the front left, catch and oar
    b.prism([(-0.42, 0.0), (-0.42, 0.3), (-0.18, 0.3), (-0.12, 0.0)], (0, -0.36, 0), (1, 0, 0),
            (0, 0, 1), 0.012, 'burlap', back_cap=True)
    for x in (-0.38, -0.3, -0.22):
        b.box(x - 0.004, x + 0.004, -0.37, -0.346, 0.0, 0.3, 'wood_mid')
    for z in (0.1, 0.2):
        b.box(-0.42, -0.14, -0.37, -0.346, z - 0.004, z + 0.004, 'wood_mid')
    d.basket(b, 0.05, -0.36, FISH)
    d.barrel(b, 0.08, -0.17, r=0.07, h=0.18, top='water')
    b.rod((-0.46, 0.2, 0.01), (-0.46, -0.05, 0.6), 0.02, 'wood_light')
    b.zbox(-0.46, 0.2, 0.01, 0.12, 0.01, 0.035, 0, 'wood_light')
    return b


kit.run(OBJ, build)
