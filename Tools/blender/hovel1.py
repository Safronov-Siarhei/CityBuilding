"""Hovel1-lvl1 -- the Hovel (Лачуга), the poorest house. Footprint 2x2. Front is -Y.

A one-room log house on a low stone footing, steep ribbed terracotta roof with plank gables, a
stone chimney, a plank door and shuttered windows; a woodpile, a chopping block and a barrel by
the door. One standard storey (details.STOREY).
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "Hovel1-lvl1"


def build(lo, hi):
    b = Builder()
    x0, x1, y0, y1 = -0.8, 0.8, -0.5, 0.75
    yc, hs = (y0 + y1) / 2, (y1 - y0) / 2

    # footing, log walls with their seams, corner posts
    d.footing(b, x0, x1, y0, y1)
    b.box(x0, x1, y0, y1, FOOTING, G_TOP, 'wood_light', bevel=d.WALL_BEVEL)
    for z in (0.24, 0.38, 0.52):
        for nm, plane, s0, s1 in (('-y', y0, x0, x1), ('-x', x0, y0, y1), ('+x', x1, y0, y1)):
            Facade(b, nm, plane).rect(s0, s1, z - 0.012, z + 0.012, 0.006, 'wood_mid')
    for px in (x0, x1):
        for py in (y0, y1):
            b.box(px - 0.045, px + 0.045, py - 0.045, py + 0.045, FOOTING, G_TOP + 0.02, 'wood_dark')

    # plank gables, the roof, the chimney
    zr = G_TOP + hs * d.PITCH
    b.gable_block('x', x0, x1, y0, y1, G_TOP, zr, 'wood_mid')
    for nm, plane in (('-x', x0), ('+x', x1)):
        f = Facade(b, nm, plane)
        for s in (yc - 0.22, yc + 0.22):
            top = zr - (zr - G_TOP) * abs(s - yc) / hs - 0.03
            f.rect(s - 0.008, s + 0.008, G_TOP, top, 0.008, 'wood_dark')
        f.rect(yc - 0.07, yc + 0.07, G_TOP + 0.2, G_TOP + 0.34, 0.012, 'wood_dark')
        f.rect(yc - 0.05, yc + 0.05, G_TOP + 0.22, G_TOP + 0.32, 0.02, d.GLASS)
    d.roof(b, 0, yc, 'x', x1 + 0.1, hs, G_TOP)
    d.chimney(b, 0.52, 0.42, 0.9, zr + 0.12)

    # front: door and a shuttered window; one window on the side and one at the back
    front = Facade(b, '-y', y0)
    d.plank_door(front, -0.32)
    d.shutter_window(front, 0.33, FOOTING)
    d.step(b, -0.5, -0.14, y0)
    d.climber(b, x1 - 0.05, y0 - 0.07, 0.5, flowers=('plaster_yellow', 'white'))
    d.shutter_window(Facade(b, '+x', x1), yc, FOOTING)
    d.shutter_window(Facade(b, '+y', y1), -0.3, FOOTING)

    # yard: path, woodpile against the -x wall, chopping block, barrel
    b.box(-0.47, -0.17, -0.98, y0 - 0.14, 0, 0.012, 'sand')
    for i, (z, y) in enumerate(((0.05, -0.64), (0.05, -0.76), (0.05, -0.88), (0.135, -0.7),
                                (0.135, -0.82))):
        b.hcylinder('x', (-0.72, y, z), 0.2, 0.05, 'wood_light' if i % 2 else 'wood_mid', sides=5)
    b.cylinder(0.05, -0.8, 0, 0.12, 0.08, 'wood_mid')
    d.barrel(b, 0.62, -0.72, top='water')
    return b


kit.run(OBJ, build)
