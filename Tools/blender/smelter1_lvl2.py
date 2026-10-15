"""Smelter1-lvl2 -- the Smelter (Плавильня) after its first upgrade. Footprint 2x3 (level 1's).
Front is -Y.

Level 1 with the furnace stack raised by STACK_UP and banded in iron, quoins on the hall and a
grindstone in the yard. Level 1, for reference:


A stone foundry hall with a slate roof and, built into its front corner, the tall tapered
furnace stack with a glowing arched mouth and a brazier of heat above it. In the open yard in
front: the bellows, an anvil on its block, a quench butt, heaps of coal and of the three ores
the smelter takes, and the bars it makes stacked on a pallet -- iron, copper, gold.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "Smelter1-lvl2"
STACK_UP = 0.4   # how much taller the furnace stack stands than at level 1
LVL2_DX = 2.6   # level 2 stands this far +X of level 1


def ingots(b, x, y, color, n=3):
    for i in range(n):
        b.zbox(x + (i - (n - 1) / 2) * 0.07, y, 0.04, 0.075, 0.03, 0.06, 0, color, bevel=0.006)
    b.zbox(x - 0.035, y, 0.075, 0.11, 0.03, 0.06, 90, color, bevel=0.006)


def build(lo, hi):
    b = Builder()
    b.box(-0.95, 0.95, -1.45, -0.1, 0, 0.008, 'cobble')

    # the hall: stone walls, slate roof along y, door and windows
    x0, x1, y0, y1 = -0.85, 0.85, -0.05, 1.38
    xc, hs = (x0 + x1) / 2, (x1 - x0) / 2
    d.footing(b, x0, x1, y0, y1)
    b.box(x0, x1, y0, y1, FOOTING, G_TOP, 'stone_light', bevel=d.WALL_BEVEL)
    for px, sx in ((x0, -1), (x1, 1)):
        for py, sy in ((y0, -1), (y1, 1)):
            d.quoins(b, px, py, sx, sy, FOOTING, G_TOP)
    zr = G_TOP + hs * d.PITCH
    b.gable_block('y', y0, y1, x0, x1, G_TOP, zr, 'stone_light')
    d.roof(b, xc, (y0 + y1) / 2, 'y', (y1 - y0) / 2 + 0.06, hs, G_TOP, **d.SLATE)
    f = Facade(b, '-y', y0)
    d.arch_door(f, 0.45)
    d.arch_window(f, 0.18, G_TOP + 0.02, h=0.26)
    for nm, plane in (('-x', x0), ('+x', x1)):
        sf = Facade(b, nm, plane)
        for s in (0.35, 0.95):
            d.barred_window(sf, s, FOOTING)
    d.chimney(b, 0.45, 1.1, 1.0, zr + 0.15)

    # the furnace stack: square plinth, tapered octagonal stack, glowing mouth, a hood of heat
    fx, fy = -0.42, -0.14
    b.box(fx - 0.36, fx + 0.36, fy - 0.36, fy + 0.36, 0, 0.5, 'stone_mid', bevel=0.02)
    st = 1.9 + STACK_UP
    b.frustum(fx, fy, 0.5, st, 0.33, 0.2, 'stone_mid', sides=8)
    b.cylinder(fx, fy, st, st + 0.08, 0.24, 'stone_dark', sides=8)
    b.cylinder(fx, fy, st + 0.08, st + 0.1, 0.16, 'fire', sides=8)
    b.blob(fx, fy, st + 0.1, 0.12, 0.16, 'stone_light', sides=5)
    for z in (0.95, 1.4, 1.85):
        r = 0.33 - (0.33 - 0.2) * (z - 0.5) / (st - 0.5)
        b.cylinder(fx, fy, z, z + 0.05, r + 0.02, 'iron', sides=8)
    mouth = Facade(b, '-y', fy - 0.36)
    mouth.arch(fx, 0.06, 0.32, 0.38, 0.015, 'stone_dark')
    mouth.arch(fx, 0.06, 0.24, 0.3, 0.025, 'fire')
    mouth.arch(fx, 0.06, 0.14, 0.18, 0.03, 'window_light')
    b.box(fx - 0.2, fx + 0.2, fy - 0.5, fy - 0.36, 0, 0.06, 'stone_dark')   # hearth sill

    # bellows against the stack's side
    bx, by = -0.86, -0.4
    b.prism([(0.0, 0.0), (0.34, 0.04), (0.34, 0.12), (0.0, 0.17)], (bx - 0.06, by - 0.12, 0.1),
            (0, 1, 0), (0, 0, 1), 0.12, 'burlap', back_cap=True)
    b.box(bx - 0.065, bx + 0.065, by - 0.12, by + 0.22, 0.27, 0.29, 'wood_mid')
    b.box(bx - 0.065, bx + 0.065, by - 0.12, by - 0.08, 0.1, 0.3, 'wood_mid')
    b.box(bx - 0.02, bx + 0.02, by - 0.3, by - 0.12, 0.2, 0.24, 'wood_dark')

    # anvil and quench butt
    ax, ay = 0.05, -0.8
    b.cylinder(ax, ay, 0, 0.16, 0.09, 'wood_mid')
    b.box(ax - 0.08, ax + 0.08, ay - 0.04, ay + 0.04, 0.16, 0.2, 'iron')
    b.box(ax - 0.12, ax + 0.1, ay - 0.05, ay + 0.05, 0.2, 0.26, 'iron')
    b.pyramid(ax - 0.12, ay, 0.205, 0.0, 0.05, 0.26, 'iron', sides=4)
    d.barrel(b, 0.4, -0.55, r=0.1, h=0.24, top='water')

    # coal, ores and bars
    d.ore_heap(b, -0.62, -0.9, r=0.18, color='window', second='iron')
    d.ore_heap(b, -0.1, -1.18, r=0.12, color='roof_slate', second='stone_dark')
    d.ore_heap(b, 0.22, -1.25, r=0.1, color='roof_orange', second='roof_green')
    d.ore_heap(b, -0.33, -1.3, r=0.09, color='gold', second='stone_mid')
    px, py = 0.6, -0.95
    b.box(px - 0.24, px + 0.24, py - 0.18, py + 0.18, 0, 0.04, 'wood_mid')
    ingots(b, px, py - 0.08, 'iron')
    ingots(b, px - 0.12, py + 0.09, 'roof_orange', n=2)
    ingots(b, px + 0.13, py + 0.09, 'gold', n=2)
    d.crate(b, 0.74, -0.58, 0.16, 10)
    # a grindstone on its frame
    gx, gy = 0.12, -0.4
    for k in (-1, 1):
        b.box(gx + k * 0.08 - 0.015, gx + k * 0.08 + 0.015, gy - 0.02, gy + 0.02, 0, 0.26, 'wood_dark')
    b.hcylinder('x', (gx, gy, 0.24), 0.03, 0.14, 'stone_light', sides=10)
    b.hcylinder('x', (gx, gy, 0.24), 0.11, 0.015, 'iron', sides=6)
    return b


kit.run(OBJ, build, source_name="Smelter1-lvl1", offset=(LVL2_DX, 0, 0),
        collection="Производственные здания")
