"""Warehouse1-lvl2 -- the Warehouse (Склад) after its first upgrade. Footprint 2x2 (level 1's).
Front -Y.

Level 1's plank store grown a full storey on a stone ground floor: quoined stone below with the
same braced double doors, the planked upper floor with a loft hatch and a hoist beam -- a crate
hangs from it on its rope -- under the same red roof. The yard is busier: the logs now lie under
a low plank lean-to, crates are stacked, barrels and sacks wait beside the cut stone and the ore.
Stands next to level 1 (LVL2_DX) on level 1's placeholder box.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP, U_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "Warehouse1-lvl2"
LVL2_DX = 2.6


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

    # front: the doors in stone, the loft hatch and hoist above, an upper window
    f = Facade(b, '-y', y0)
    d.barn_door(f, -0.15, color='door')
    f.rect(-0.35, 0.05, FOOTING + d.DOOR_H * 0.2, FOOTING + d.DOOR_H * 0.2 + 0.025, 0.045, 'iron')
    d.hay_loft(f, 0.42, G_TOP + 0.05, w=0.22, h=0.26)
    d.plain_window(f, -0.42, G_TOP)
    hook_y = y0 - 0.14
    beam_z = G_TOP + 0.05 + 0.26 + 0.04
    b.rod((0.42, hook_y, beam_z), (0.42, hook_y, 0.58), 0.008, 'burlap')
    b.zbox(0.42, hook_y, 0.46, 0.58, 0.065, 0.065, 12, 'wood_light', bevel=0.01)
    d.plain_window(Facade(b, '+x', x1), yc, G_TOP)
    d.plain_window(Facade(b, '-x', x0), yc, G_TOP)
    d.barred_window(Facade(b, '+x', x1), yc, FOOTING)
    d.barred_window(Facade(b, '-x', x0), yc, FOOTING)

    # yard: path, logs under a lean-to, cut stone, ore, crates, barrels, sacks
    b.box(-0.4, 0.1, -0.98, y0, 0, 0.012, 'sand')
    for px in (-0.95, -0.5):
        for wy, h in ((-0.32, 0.4), (-0.84, 0.33)):
            b.box(px - 0.02, px + 0.02, wy - 0.02, wy + 0.02, 0, h, 'wood_dark')
    d.shed_roof(b, 'x', -0.98, -0.47, -0.3, -0.86, 0.41, 0.33, color='wood_mid', dark='wood_dark')
    d.log_pile(b, -0.72, -0.58, length=0.44, axis='y')
    d.stone_pile(b, 0.32, -0.42)
    d.ore_heap(b, 0.62, -0.74)
    d.crate(b, 0.66, -0.24, 0.16, 15)
    d.crate(b, 0.66, -0.24, 0.13, 40, z0=0.16)
    d.crate(b, 0.86, -0.38, 0.13, -10)
    d.barrel(b, 0.22, -0.8, r=0.07, h=0.18)
    d.barrel(b, 0.3, -0.93, r=0.06, h=0.16)
    d.sack(b, 0.68, -0.47)
    d.sack(b, 0.82, -0.52, r=0.06)
    return b


kit.run(OBJ, build, source_name="Warehouse1-lvl1", offset=(LVL2_DX, 0, 0), collection="Склады")
