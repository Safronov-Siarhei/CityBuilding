"""Cottage1-lvl1 -- the Cottage (Коттедж). Footprint 2x2. Front is -Y.

A two-storey half-timbered house: plaster ground floor with corner posts, a jettied upper floor
framed in dark beams, a steep ribbed terracotta roof, flower boxes under the upper windows, a
bench by the door and a flower bed. Two standard storeys.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP, JETTY, U_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "Cottage1-lvl1"


def build(lo, hi):
    b = Builder()
    x0, x1, y0, y1 = -0.76, 0.76, -0.48, 0.72
    ux0, ux1, uy0, uy1 = x0 - JETTY, x1 + JETTY, y0 - JETTY, y1 + JETTY
    yc, hs = (y0 + y1) / 2, (uy1 - uy0) / 2
    zr = U_TOP + hs * d.PITCH

    d.footing(b, x0, x1, y0, y1)
    b.box(x0, x1, y0, y1, FOOTING, G_TOP, 'plaster', bevel=d.WALL_BEVEL)
    b.box(ux0, ux1, uy0, uy1, G_TOP, U_TOP, 'plaster', bevel=d.WALL_BEVEL)
    b.gable_block('x', ux0, ux1, uy0, uy1, U_TOP, zr, 'plaster')
    d.roof(b, 0, yc, 'x', ux1 + 0.09, hs, U_TOP)
    d.chimney(b, -0.42, 0.45, 1.3, zr + 0.12)

    # ground floor: corner posts, door and a window in front, one window on each other side
    for px in (x0, x1):
        for py in (y0, y1):
            b.box(px - 0.04, px + 0.04, py - 0.04, py + 0.04, FOOTING, G_TOP, 'wood_dark')
    front = Facade(b, '-y', y0)
    d.plank_door(front, -0.3)
    d.shutter_window(front, 0.36, FOOTING)
    d.step(b, -0.47, -0.13, y0)
    for nm, plane, s in (('+y', y1, 0.0), ('-x', x0, yc), ('+x', x1, yc)):
        d.plain_window(Facade(b, nm, plane), s, FOOTING, sill='wood_dark')

    # upper floor: framing, windows with flower boxes in front
    f = Facade(b, '-y', uy0)
    d.timber(f, ux0, ux1, G_TOP, U_TOP, (ux0 + 0.025, -0.27, 0.27, ux1 - 0.025),
             ((-0.27, 0.27), (0.27, -0.27)))
    for s in (-0.545, 0.545):
        d.cross_window(f, s, G_TOP)
        d.flower_box(f, s, G_TOP)
    f = Facade(b, '+y', uy1)
    d.timber(f, ux0, ux1, G_TOP, U_TOP, (ux0 + 0.025, 0.0, ux1 - 0.025))
    for s in (-0.42, 0.42):
        d.plain_window(f, s, G_TOP)
    for nm, plane in (('-x', ux0), ('+x', ux1)):
        f = Facade(b, nm, plane)
        d.timber(f, uy0, uy1, G_TOP, U_TOP, (uy0 + 0.025, yc, uy1 - 0.025))
        d.plain_window(f, (yc + uy1) / 2, G_TOP)
        d.gable_frame(f, yc, hs, U_TOP, zr)

    # yard: path, bench, flower bed
    b.box(-0.45, -0.15, -0.98, y0 - 0.14, 0, 0.012, 'sand')
    d.bench(b, 0.12, 0.62, -0.68, -0.56)
    b.box(-0.95, -0.55, -0.95, -0.62, 0, 0.05, 'wood_mid')
    for x, c in ((-0.86, 'roof_red'), (-0.75, 'plaster_yellow'), (-0.64, 'roof_red')):
        b.blob(x, -0.785, 0.05, 0.065, 0.12, c, sides=4)
    return b


kit.run(OBJ, build)
