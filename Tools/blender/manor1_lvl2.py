"""Manor1-lvl2 -- the Manor (Особняк) after its first upgrade. Footprint 3x3 (level 1's).
Front is -Y.

Level 1 with quoined stone corners, two dormers in the main roof and a banner over the door;
the garden gets a fountain in place of its second tree and stone gate piers with lamps where
the path comes in. Level 1, for reference:


An L-shaped two-storey house: the main wing along the back, a side wing coming forward on +x,
both with a stone ground floor and a yellow half-timbered upper floor under terracotta roofs
that meet in a valley. The front corner is a small walled garden with a path to a porch.
Two standard storeys.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP, JETTY as J, U_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "Manor1-lvl2"
LVL2_DX = 3.6   # level 2 stands this far +X of level 1


def storeys(b, x0, x1, y0, y1):
    d.footing(b, x0, x1, y0, y1)
    b.box(x0, x1, y0, y1, FOOTING, G_TOP, 'stone_light', bevel=d.WALL_BEVEL)
    for px, sx in ((x0, -1), (x1, 1)):
        for py, sy in ((y0, -1), (y1, 1)):
            d.quoins(b, px, py, sx, sy, FOOTING, G_TOP)
    b.box(x0 - J, x1 + J, y0 - J, y1 + J, G_TOP, U_TOP, 'plaster_yellow', bevel=d.WALL_BEVEL)


def build(lo, hi):
    b = Builder()

    # main wing along x at the back
    mx0, mx1, my0, my1 = -1.34, 1.34, 0.15, 1.3
    storeys(b, mx0, mx1, my0, my1)
    myc, mhs = (my0 + my1) / 2, (my1 - my0) / 2 + J
    mzr = U_TOP + mhs * d.PITCH
    b.gable_block('x', mx0 - J, mx1 + J, my0 - J, my1 + J, U_TOP, mzr, 'plaster_yellow')
    d.roof(b, 0, myc, 'x', mx1 + J + 0.09, mhs, U_TOP)

    # side wing along y on +x, coming forward to the front edge; its roof runs into the main one
    wx0, wx1, wy0, wy1 = 0.48, 1.34, -1.3, my0
    storeys(b, wx0, wx1, wy0, wy1)
    wxc, whs = (wx0 + wx1) / 2, (wx1 - wx0) / 2 + J
    wzr = U_TOP + whs * d.PITCH
    b.gable_block('y', wy0 - J, myc, wx0 - J, wx1 + J, U_TOP, wzr, 'plaster_yellow')
    w_start, w_end = wy0 - J - 0.09, myc
    d.roof(b, wxc, (w_start + w_end) / 2, 'y', (w_end - w_start) / 2, whs, U_TOP)

    d.chimney(b, -0.85, 1.0, 1.3, mzr + 0.12)
    d.chimney(b, 0.2, 1.0, 1.3, mzr + 0.12)
    for s in (-1.0, -0.3):
        d.dormer(b, '-y', my0 - J, s, U_TOP, h=0.42, wall='plaster_yellow')

    # -- main wing faces ------------------------------------------------------------------------
    f = Facade(b, '-y', my0)  # into the garden
    for s in (-1.05, 0.15):
        d.arch_window(f, s, FOOTING)
    d.arch_door(f, -0.45)
    f = Facade(b, '-y', my0 - J)
    d.timber(f, mx0 - J, wx0 - J, G_TOP, U_TOP, (mx0 - J + 0.025, -0.75, -0.15, wx0 - J - 0.025),
             ((-0.75, -0.15), (-0.15, -0.75)))
    for s in (-1.05, 0.17):
        d.cross_window(f, s, G_TOP)
        d.flower_box(f, s, G_TOP)
    d.banner(f, -0.45, U_TOP - 0.05, 0.3, w=0.13)
    f = Facade(b, '+y', my1 + J)
    d.timber(f, mx0 - J, mx1 + J, G_TOP, U_TOP, ())
    for s in (-0.9, 0.0, 0.9):
        d.plain_window(f, s, G_TOP)
    f = Facade(b, '-x', mx0 - J)
    d.timber(f, my0 - J, my1 + J, G_TOP, U_TOP, ())
    d.plain_window(f, myc - 0.3, G_TOP)
    d.gable_frame(f, myc, mhs, U_TOP, mzr)
    d.arch_window(Facade(b, '-x', mx0), myc, FOOTING)
    d.gable_frame(Facade(b, '+x', mx1 + J), myc, mhs, U_TOP, mzr)

    # -- side wing faces ------------------------------------------------------------------------
    f = Facade(b, '-y', wy0 - J)  # gable end on the street
    d.timber(f, wx0 - J, wx1 + J, G_TOP, U_TOP, (wx0 - J + 0.025, wxc, wx1 + J - 0.025),
             ((wx0, wxc), (wx1, wxc)))
    d.cross_window(f, wxc - 0.22, G_TOP, w=0.18)
    d.cross_window(f, wxc + 0.22, G_TOP, w=0.18)
    d.gable_frame(f, wxc, whs, U_TOP, wzr)
    d.arch_window(Facade(b, '-y', wy0), wxc, FOOTING)
    f = Facade(b, '-x', wx0 - J)  # into the garden
    d.timber(f, wy0 - J, wy1, G_TOP, U_TOP, (wy0 - J + 0.025, -0.75, -0.25), ((-0.75, -0.25),))
    for s in (-1.0, -0.05):
        d.cross_window(f, s, G_TOP)
        d.flower_box(f, s, G_TOP)
    f = Facade(b, '-x', wx0)
    for s in (-1.0, -0.5):
        d.arch_window(f, s, FOOTING)
    f = Facade(b, '+x', wx1 + J)
    d.timber(f, wy0 - J, my1 + J, G_TOP, U_TOP, ())
    for s in (-0.95, -0.17, 0.75):
        d.plain_window(f, s, G_TOP)

    # -- porch over the door --------------------------------------------------------------------
    for px in (-0.63, -0.27):
        b.box(px - 0.025, px + 0.025, -0.13, -0.08, 0, G_TOP + 0.02, 'wood_dark')
    d.roof(b, -0.45, 0.0, 'y', 0.17, 0.24, G_TOP, overhang=0.05, t=0.03, rib_h=0.012,
           cap_w=0.06, cap_h=0.03, cap_lift=0.015)
    b.box(-0.68, -0.22, -0.2, my0, 0, 0.06, 'stone_mid', bevel=0.008)

    # -- garden ---------------------------------------------------------------------------------
    b.box(-1.42, wx0 - 0.02, -1.42, my0 - 0.02, 0, 0.015, 'grass')
    b.box(-0.6, -0.3, -1.42, -0.2, 0.015, 0.025, 'sand')
    for x0, x1, y0, y1 in ((-1.42, -0.62, -1.42, -1.3), (-0.28, wx0 - 0.04, -1.42, -1.3),
                           (-1.42, -1.3, -1.3, my0 - 0.04)):
        b.box(x0, x1, y0, y1, 0.015, 0.16, 'leaf')
    d.tree(b, -1.1, -0.85, 0.015, r=0.15)
    d.fountain(b, 0.05, -0.8, 0.015)
    for x, y, c in ((-1.12, -0.25, 'roof_red'), (-0.9, -0.25, 'plaster_yellow'),
                    (0.1, -0.25, 'roof_purple'), (0.3, -0.25, 'roof_red')):
        b.blob(x, y, 0.015, 0.07, 0.13, c, sides=4)
    d.bench(b, -1.25, -0.85, -0.6, -0.48)
    for px in (-0.66, -0.24):   # gate piers with lamps where the path comes in
        b.box(px - 0.045, px + 0.045, -1.42, -1.33, 0.015, 0.3, 'stone_light', bevel=0.008)
        b.pyramid(px, -1.375, 0.3, 0.05, 0.05, 0.34, 'stone_mid')
        b.box(px - 0.03, px + 0.03, -1.405, -1.345, 0.34, 0.42, 'window_light')
        b.pyramid(px, -1.375, 0.42, 0.04, 0.04, 0.46, 'iron')
    return b


kit.run(OBJ, build, source_name="Manor1-lvl1", offset=(LVL2_DX, 0, 0),
        collection="Город")
