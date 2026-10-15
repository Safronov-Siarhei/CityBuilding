"""Cottage1-lvl2 -- the Cottage (Коттедж) after its first upgrade. Footprint 2x2 (level 1's).
Front is -Y.

Level 1's half-timbered house rebuilt on a dressed stone ground floor: quoined corners, an arched
stone doorway between two lanterns, an arched window. The jettied timber upper floor, its flower
boxes and the terracotta roof stay, and the roof gains a dormer over the door. A white picket
fence closes the front garden, with a gate on the path and a lamp at the corner.
Stands next to level 1 (LVL2_DX) on level 1's placeholder box.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP, JETTY, U_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "Cottage1-lvl2"
LVL2_DX = 2.6


def picket_fence(b, x0, x1, y, h=0.22, step=0.07):
    """White pickets along x at y with a rail behind them."""
    x = x0 + step / 2
    while x < x1 - 1e-6:
        b.box(x - 0.018, x + 0.018, y - 0.01, y + 0.01, 0, h - 0.04, 'plaster_white', skip=('-z', '+z'))
        b.pyramid(x, y, h - 0.04, 0.018, 0.01, h, 'plaster_white')
        x += step
    for z in (0.06, h - 0.08):
        b.box(x0, x1, y + 0.01, y + 0.03, z, z + 0.025, 'plaster_white', skip=())


def build(lo, hi):
    b = Builder()
    x0, x1, y0, y1 = -0.76, 0.76, -0.48, 0.72
    ux0, ux1, uy0, uy1 = x0 - JETTY, x1 + JETTY, y0 - JETTY, y1 + JETTY
    yc, hs = (y0 + y1) / 2, (uy1 - uy0) / 2
    zr = U_TOP + hs * d.PITCH

    d.footing(b, x0, x1, y0, y1, color='stone_dark')
    b.box(x0, x1, y0, y1, FOOTING, G_TOP, 'stone_light', bevel=d.WALL_BEVEL)
    b.box(ux0, ux1, uy0, uy1, G_TOP, U_TOP, 'plaster', bevel=d.WALL_BEVEL)
    b.gable_block('x', ux0, ux1, uy0, uy1, U_TOP, zr, 'plaster')
    d.roof(b, 0, yc, 'x', ux1 + 0.09, hs, U_TOP)
    d.chimney(b, -0.42, 0.45, 1.3, zr + 0.2, hw=0.1)

    # stone ground floor: quoins, arched door between lanterns, arched window, side windows
    for px, sx in ((x0, -1), (x1, 1)):
        for py, sy in ((y0, -1), (y1, 1)):
            d.quoins(b, px, py, sx, sy, FOOTING, G_TOP)
    front = Facade(b, '-y', y0)
    d.arch_door(front, -0.3)
    for s in (-0.52, -0.08):
        d.wall_lantern(front, s, FOOTING + 0.36)
    d.arch_window(front, 0.36, FOOTING)
    d.step(b, -0.47, -0.13, y0)
    for nm, plane, s in (('+y', y1, 0.0), ('-x', x0, yc), ('+x', x1, yc)):
        d.arch_window(Facade(b, nm, plane), s, FOOTING)

    # upper floor: as level 1
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

    # a dormer in the front roof slope, right over the door's axis between the upper windows
    dw, dd = 0.2, 0.5
    yd = uy0 + 0.05
    zt = U_TOP + 0.44
    b.box(-dw, dw, yd, yd + dd, U_TOP - 0.02, zt, 'plaster')
    b.gable_block('y', yd, yd + dd, -dw, dw, zt, zt + dw * d.PITCH, 'plaster')
    d.roof(b, 0, yd + dd / 2 - 0.05, 'y', dd / 2 + 0.05, dw, zt, overhang=0.06)
    df = Facade(b, '-y', yd)
    df.rect(-dw, dw, U_TOP - 0.02, U_TOP + 0.02, d.BEAM_D, 'wood_dark')
    df.rect(-dw, dw, zt - 0.04, zt, d.BEAM_D, 'wood_dark')
    for s in (-dw + 0.025, dw - 0.025):
        df.rect(s - d.BEAM_W / 2, s + d.BEAM_W / 2, U_TOP + 0.02, zt - 0.04, d.BEAM_D, 'wood_dark')
    d.cross_window(df, 0, U_TOP + 0.13 - d.WIN_SILL, w=0.16)
    d.gable_frame(df, 0, dw, zt, zt + dw * d.PITCH)

    # a climbing rose up the front corner
    d.climber(b, x0 + 0.03, y0 - 0.07, 0.62)

    # garden: path through a gate in the picket fence, bench, flower beds, a lamp
    for px, py in ((-0.3, -0.66), (-0.27, -0.79), (-0.32, -0.92)):
        b.cylinder(px, py, 0, 0.016, 0.055, 'stone_light', sides=7)
    fy = -0.93
    picket_fence(b, -0.98, -0.47, fy)
    picket_fence(b, -0.13, 0.98, fy)
    for gx in (-0.47, -0.13):
        b.box(gx - 0.025, gx + 0.025, fy - 0.025, fy + 0.025, 0, 0.27, 'plaster_white')
        b.pyramid(gx, fy, 0.27, 0.03, 0.03, 0.31, 'plaster_white')
    d.bench(b, 0.12, 0.62, -0.68, -0.56)
    b.box(-0.95, -0.55, -0.88, -0.62, 0, 0.05, 'wood_mid')
    for x, c in ((-0.86, 'roof_red'), (-0.75, 'plaster_yellow'), (-0.64, 'roof_red')):
        b.blob(x, -0.755, 0.05, 0.065, 0.12, c, sides=4)
    d.flowers(b, 0.82, -0.62, colors=('plaster_pink', 'white', 'roof_purple'))
    d.lamp(b, 0.86, -0.82, h=0.5)
    return b


kit.run(OBJ, build, source_name="Cottage1-lvl1", offset=(LVL2_DX, 0, 0), collection="Город")
