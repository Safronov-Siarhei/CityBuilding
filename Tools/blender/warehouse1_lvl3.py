"""Warehouse1-lvl3 -- the Warehouse (Склад) at its last upgrade. Footprint 2x2 (level 1's).
Front -Y.

Level 2's store -- quoined stone ground floor, planked upper floor, loft hatch with its hoist and
hanging crate, red roof -- crowned: a louvred cupola with a gold finial rides the ridge, gilded
finials stand on the ridge ends, a red banner with a gold mark hangs by the loft. The doors open
onto a raised plank loading dock under its own red canopy; the yard is paved and the log
lean-to is tiled now.
Stands next to level 2 (LVL3_DX) on level 1's placeholder box.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP, U_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "Warehouse1-lvl3"
LVL3_DX = 5.2   # level 2 stands at 2.6


def build(lo, hi):
    b = Builder()
    x0, x1, y0, y1 = -0.78, 0.78, -0.08, 0.8
    yc, hs = (y0 + y1) / 2, (y1 - y0) / 2
    zr = U_TOP + hs * d.PITCH

    # stone ground floor, planked upper floor, the red roof
    d.footing(b, x0, x1, y0, y1, color='stone_dark')
    b.box(x0, x1, y0, y1, FOOTING, G_TOP, 'stone_light', bevel=d.WALL_BEVEL)
    for px, sx in ((x0, -1), (x1, 1)):
        for py, sy in ((y0, -1), (y1, 1)):
            d.quoins(b, px, py, sx, sy, FOOTING, G_TOP)
    b.box(x0, x1, y0, y1, G_TOP, U_TOP, 'wood_light', bevel=d.WALL_BEVEL)
    for nm, plane, s0, s1 in (('-y', y0, x0, x1), ('-x', x0, y0, y1), ('+x', x1, y0, y1),
                              ('+y', y1, x0, x1)):
        f = Facade(b, nm, plane)
        f.rect(s0, s1, G_TOP - 0.02, G_TOP + 0.03, 0.02, 'wood_dark')
        a = s0 + 0.12
        while a < s1 - 0.08:
            f.rect(a - 0.008, a + 0.008, G_TOP + 0.03, U_TOP, 0.006, 'wood_mid')
            a += 0.24
    for px in (x0, x1):
        for py in (y0, y1):
            b.box(px - 0.04, px + 0.04, py - 0.04, py + 0.04, G_TOP, U_TOP, 'wood_dark')
    b.gable_block('x', x0, x1, y0, y1, U_TOP, zr, 'wood_mid')
    d.roof(b, 0, yc, 'x', x1 + 0.09, hs, U_TOP, **d.RED)
    d.chimney(b, -0.5, 0.55, 1.4, zr + 0.08)
    for nm, plane in (('-x', x0), ('+x', x1)):
        d.gable_frame(Facade(b, nm, plane), yc, hs, U_TOP, zr)

    # on the ridge: a louvred cupola under a red pyramid and a gold finial; finials at the ends
    cz0, cz1 = zr - 0.04, zr + 0.26
    b.box(-0.11, 0.11, yc - 0.11, yc + 0.11, cz0, cz1, 'wood_light', bevel=0.008)
    for nm, plane in (('-y', yc - 0.11), ('+y', yc + 0.11), ('-x', -0.11), ('+x', 0.11)):
        f = Facade(b, nm, plane)
        f.rect(-0.07, 0.07, cz0 + 0.1, cz1 - 0.03, 0.006, 'window')
        for k in range(4):
            z = cz0 + 0.115 + k * 0.032
            f.rect(-0.07, 0.07, z, z + 0.014, 0.016, 'wood_dark')
    b.box(-0.13, 0.13, yc - 0.13, yc + 0.13, cz1, cz1 + 0.025, 'wood_dark')
    b.pyramid(0, yc, cz1 + 0.025, 0.15, 0.15, cz1 + 0.2, 'roof_red')
    d.finial(b, 0, yc, cz1 + 0.19)
    for ex in (x0 - 0.09, x1 + 0.09):
        d.finial(b, ex, yc, zr + 0.06)

    # front: the doors in stone, the loft hatch and hoist above, an upper window, a banner
    f = Facade(b, '-y', y0)
    d.barn_door(f, -0.15, color='door')
    f.rect(-0.35, 0.05, FOOTING + d.DOOR_H * 0.2, FOOTING + d.DOOR_H * 0.2 + 0.025, 0.045, 'iron')
    d.hay_loft(f, 0.42, G_TOP + 0.05, w=0.22, h=0.26)
    d.plain_window(f, -0.42, G_TOP)
    d.banner(f, 0.12, U_TOP - 0.04, 0.34, w=0.12)
    hook_y = y0 - 0.14
    beam_z = G_TOP + 0.05 + 0.26 + 0.04
    b.rod((0.42, hook_y, beam_z), (0.42, hook_y, 0.58), 0.008, 'burlap')
    b.zbox(0.42, hook_y, 0.46, 0.58, 0.065, 0.065, 12, 'wood_light', bevel=0.01)
    d.plain_window(Facade(b, '+x', x1), yc, G_TOP)
    d.plain_window(Facade(b, '-x', x0), yc, G_TOP)
    d.barred_window(Facade(b, '+x', x1), yc, FOOTING)
    d.barred_window(Facade(b, '-x', x0), yc, FOOTING)

    # the loading dock: a plank deck at door-sill height under a red canopy on two posts
    dx0, dx1, dy0 = -0.44, 0.14, y0 - 0.3
    b.box(dx0, dx1, dy0, y0, 0, FOOTING, 'wood_mid', bevel=0.006)
    for x in (dx0 + 0.03, dx1 - 0.03):
        b.box(x - 0.025, x + 0.025, dy0 + 0.005, dy0 + 0.055, FOOTING, G_TOP - 0.02, 'wood_dark')
    d.shed_roof(b, 'x', dx0 - 0.04, dx1 + 0.04, y0, dy0 - 0.05, G_TOP + 0.02, G_TOP - 0.12, **d.RED)
    d.step(b, -0.32, 0.02, dy0)
    d.crate(b, dx0 + 0.1, dy0 + 0.1, 0.13, 10, z0=FOOTING)
    d.sack(b, dx1 - 0.08, dy0 + 0.09, r=0.055)

    # yard: paving, logs under a tiled lean-to, cut stone, ore, crates, barrels, sacks
    d.paving(b, -0.4, 0.1, -0.98, dy0 - 0.14)
    for px in (-0.95, -0.5):
        for wy, h in ((-0.32, 0.4), (-0.84, 0.33)):
            b.box(px - 0.02, px + 0.02, wy - 0.02, wy + 0.02, 0, h, 'wood_dark')
    d.shed_roof(b, 'x', -0.98, -0.47, -0.3, -0.86, 0.41, 0.33, **d.RED)
    d.log_pile(b, -0.72, -0.58, length=0.44, axis='y')
    d.stone_pile(b, 0.34, -0.5)
    d.ore_heap(b, 0.62, -0.76)
    d.crate(b, 0.66, -0.24, 0.16, 15)
    d.crate(b, 0.66, -0.24, 0.13, 40, z0=0.16)
    d.crate(b, 0.86, -0.38, 0.13, -10)
    d.barrel(b, 0.24, -0.84, r=0.07, h=0.18)
    d.barrel(b, 0.3, -0.95, r=0.045, h=0.13)
    d.sack(b, 0.68, -0.5)
    d.sack(b, 0.84, -0.54, r=0.06)
    return b


kit.run(OBJ, build, source_name="Warehouse1-lvl1", offset=(LVL3_DX, 0, 0), collection="Склады")
