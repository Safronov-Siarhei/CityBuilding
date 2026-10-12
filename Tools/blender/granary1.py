"""Granary1-lvl1 -- the Larder (Кладовая), food store. Footprint 2x1. Front is -Y.

A low stone storehouse under a red roof: a plank door, shuttered windows, timber gables with a
vent, baskets of apples, sacks and a barrel along the front. One standard storey.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "Granary1-lvl1"


def basket(b, x, y, fruit='roof_red'):
    b.cylinder(x, y, 0, 0.08, 0.065, 'wood_light', sides=6)
    b.blob(x, y, 0.06, 0.055, 0.06, fruit, sides=5)


def build(lo, hi):
    b = Builder()
    x0, x1, y0, y1 = -0.82, 0.82, -0.28, 0.34
    yc, hs = (y0 + y1) / 2, (y1 - y0) / 2
    zr = G_TOP + hs * d.PITCH

    d.footing(b, x0, x1, y0, y1)
    b.box(x0, x1, y0, y1, FOOTING, G_TOP, 'stone_light', bevel=d.WALL_BEVEL)
    b.gable_block('x', x0, x1, y0, y1, G_TOP, zr, 'wood_mid')
    d.roof(b, 0, yc, 'x', x1 + 0.09, hs, G_TOP, **d.RED)
    for nm, plane in (('-x', x0), ('+x', x1)):
        f = Facade(b, nm, plane)
        f.rect(yc - 0.05, yc + 0.05, G_TOP + 0.06, G_TOP + 0.16, 0.012, 'window')

    f = Facade(b, '-y', y0)
    d.plank_door(f, -0.4)
    for s in (0.15, 0.55):
        d.shutter_window(f, s, FOOTING)
    d.plain_window(Facade(b, '+y', y1), 0.0, FOOTING)

    for x, c in ((-0.72, 'roof_red'), (-0.58, 'roof_green'), (0.88 - 0.2, 'plaster_yellow')):
        basket(b, x, -0.4, c)
    d.sack(b, 0.36, -0.42)
    d.barrel(b, 0.85, -0.38, r=0.07, h=0.18)
    return b


kit.run(OBJ, build)
