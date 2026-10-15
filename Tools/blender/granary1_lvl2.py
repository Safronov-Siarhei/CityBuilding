"""Granary1-lvl2 -- the Larder (Кладовая) after its first upgrade. Footprint 2x1 (level 1's).
Front is -Y.

Level 1 raised to a storey and a half with a loft hatch over the door, quoined corners, and a
red awning on two posts over the windows and the baskets. Level 1, for reference:


A low stone storehouse under a red roof: a plank door, shuttered windows, timber gables with a
vent, baskets of apples, sacks and a barrel along the front. One standard storey.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP, L_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "Granary1-lvl2"
LVL2_DX = 2.6   # level 2 stands this far +X of level 1


def basket(b, x, y, fruit='roof_red'):
    b.cylinder(x, y, 0, 0.08, 0.065, 'wood_light', sides=6)
    b.blob(x, y, 0.06, 0.055, 0.06, fruit, sides=5)


def build(lo, hi):
    b = Builder()
    x0, x1, y0, y1 = -0.82, 0.82, -0.28, 0.34
    yc, hs = (y0 + y1) / 2, (y1 - y0) / 2
    zr = L_TOP + hs * d.PITCH

    d.footing(b, x0, x1, y0, y1)
    b.box(x0, x1, y0, y1, FOOTING, L_TOP, 'stone_light', bevel=d.WALL_BEVEL)
    for px, sx in ((x0, -1), (x1, 1)):
        for py, sy in ((y0, -1), (y1, 1)):
            d.quoins(b, px, py, sx, sy, FOOTING, L_TOP)
    b.gable_block('x', x0, x1, y0, y1, L_TOP, zr, 'wood_mid')
    d.roof(b, 0, yc, 'x', x1 + 0.09, hs, L_TOP, **d.RED)
    for nm, plane in (('-x', x0), ('+x', x1)):
        f = Facade(b, nm, plane)
        f.rect(yc - 0.05, yc + 0.05, L_TOP + 0.04, L_TOP + 0.14, 0.012, d.GLASS)

    f = Facade(b, '-y', y0)
    d.plank_door(f, -0.4)
    d.hay_loft(f, -0.4, G_TOP + 0.03, w=0.16, h=0.17)
    for px in (0.0, 0.8):   # the awning over the windows and the baskets
        b.box(px - 0.02, px + 0.02, y0 - 0.17, y0 - 0.13, 0, 0.52, 'wood_dark')
    d.shed_roof(b, 'x', -0.05, 0.85, y0, y0 - 0.17, 0.66, 0.52, **d.RED)
    for s in (0.15, 0.55):
        d.shutter_window(f, s, FOOTING)
    d.plain_window(Facade(b, '+y', y1), 0.0, FOOTING)

    for x, c in ((-0.72, 'roof_red'), (-0.58, 'roof_green'), (0.88 - 0.2, 'plaster_yellow')):
        basket(b, x, -0.4, c)
    d.sack(b, 0.36, -0.42)
    d.barrel(b, 0.85, -0.38, r=0.07, h=0.18)
    return b


kit.run(OBJ, build, source_name="Granary1-lvl1", offset=(LVL2_DX, 0, 0),
        collection="Склады")
