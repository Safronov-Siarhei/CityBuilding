"""Cottage1-lvl3 -- the Cottage (Коттедж) at its last upgrade. Footprint 2x2 (level 1's).
Front is -Y.

Level 2's house -- stone ground floor, jettied timber upper floor, dormer, terracotta roof --
with a round stone turret on its front corner under a tall cone and a gold finial, and gilded
finials on both ridge ends. The picket fence became a low stone wall with two gate piers carrying
gold balls; the path is paved.
Stands next to level 2 (LVL3_DX) on level 1's placeholder box.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP, JETTY, U_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "Cottage1-lvl3"
LVL3_DX = 5.2                    # level 2 stands at 2.6
TX, TY, TR = 0.74, -0.5, 0.2     # the turret on the front-right corner


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
            if (px, py) != (x1, y0):    # the turret stands on that corner
                d.quoins(b, px, py, sx, sy, FOOTING, G_TOP)
    front = Facade(b, '-y', y0)
    d.arch_door(front, -0.3)
    for s in (-0.52, -0.08):
        d.wall_lantern(front, s, FOOTING + 0.36)
    d.arch_window(front, 0.3, FOOTING)
    d.step(b, -0.47, -0.13, y0)
    for nm, plane, s in (('+y', y1, 0.0), ('-x', x0, yc), ('+x', x1, yc)):
        d.arch_window(Facade(b, nm, plane), s, FOOTING)

    # upper floor: as level 1, but the turret takes the front-right window
    f = Facade(b, '-y', uy0)
    d.timber(f, ux0, ux1, G_TOP, U_TOP, (ux0 + 0.025, -0.27, 0.27, ux1 - 0.025),
             ((-0.27, 0.27), (0.27, -0.27)))
    d.cross_window(f, -0.545, G_TOP)
    d.flower_box(f, -0.545, G_TOP)
    f = Facade(b, '+y', uy1)
    d.timber(f, ux0, ux1, G_TOP, U_TOP, (ux0 + 0.025, 0.0, ux1 - 0.025))
    for s in (-0.42, 0.42):
        d.plain_window(f, s, G_TOP)
    for nm, plane in (('-x', ux0), ('+x', ux1)):
        f = Facade(b, nm, plane)
        d.timber(f, uy0, uy1, G_TOP, U_TOP, (uy0 + 0.025, yc, uy1 - 0.025))
        d.plain_window(f, (yc + uy1) / 2, G_TOP)
        d.gable_frame(f, yc, hs, U_TOP, zr)

    # the dormer over the door, as level 2
    d.dormer(b, '-y', uy0, 0, U_TOP)

    # the turret on the front-right corner, gold finials on the ridge ends
    d.turret(b, TX, TY, FOOTING, U_TOP + 0.3, TR, wall='stone_light')
    tf = Facade(b, '-y', TY - TR * 0.924)
    tf.rect(TX - 0.075, TX + 0.075, G_TOP - 0.03, G_TOP + 0.01, 0.01, 'stone_mid')
    d.arch_window(tf, TX, FOOTING, w=0.1, h=0.22)
    for ex in (ux0 - 0.09, ux1 + 0.09):
        d.finial(b, ex, yc, zr + 0.06)

    # a climbing rose up the front corner
    d.climber(b, x0 + 0.03, y0 - 0.07, 0.62)

    # garden: a paved path between gate piers in a low stone wall, bench, flower beds, a lamp
    d.paving(b, -0.42, -0.18, -0.93, y0 - 0.14)
    fy = -0.93
    d.garden_wall(b, [(-0.94, fy), (-0.5, fy)])
    d.garden_wall(b, [(-0.1, fy), (0.94, fy)])
    for gx in (-0.47, -0.13):
        d.pier(b, gx, fy, h=0.3, w=0.05)
    d.bench(b, 0.02, 0.42, -0.68, -0.56)
    b.box(-0.92, -0.55, -0.88, -0.62, 0, 0.05, 'wood_mid')
    for x, c in ((-0.84, 'roof_red'), (-0.735, 'plaster_yellow'), (-0.63, 'roof_red')):
        b.blob(x, -0.755, 0.05, 0.065, 0.12, c, sides=4)
    d.flowers(b, 0.84, -0.84, colors=('plaster_pink', 'white', 'roof_purple'))
    d.lamp(b, 0.54, -0.84, h=0.5)
    return b


kit.run(OBJ, build, source_name="Cottage1-lvl1", offset=(LVL3_DX, 0, 0), collection="Город")
