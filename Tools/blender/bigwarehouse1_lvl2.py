"""BigWarehouse1-lvl2 -- the Big Warehouse (Большой склад) after its first upgrade. Footprint
3x3 (level 1's). Front is -Y.

Level 1 with quoined corners, two plank dormers in the roof, a weathervane, lanterns at the
doors and crates and barrels added to the yard. Level 1, for reference:

The Warehouse grown up: a stone ground floor with two wide doors, a jettied plank upper floor
with a loading hatch and a hoist beam holding a crate on a rope, a red roof, and a yard twice
the size -- two log piles, cut stone, ore, a cart loaded with logs.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP, JETTY as J, U_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "BigWarehouse1-lvl2"
LVL2_DX = 3.6   # level 2 stands this far +X of level 1


def build(lo, hi):
    b = Builder()
    x0, x1, y0, y1 = -1.3, 1.3, 0.05, 1.3
    ux0, ux1, uy0, uy1 = x0 - J, x1 + J, y0 - J, y1 + J
    yc, hs = (uy0 + uy1) / 2, (uy1 - uy0) / 2
    zr = U_TOP + hs * d.PITCH

    d.footing(b, x0, x1, y0, y1)
    b.box(x0, x1, y0, y1, FOOTING, G_TOP, 'stone_light', bevel=d.WALL_BEVEL)
    for px, sx in ((x0, -1), (x1, 1)):
        for py, sy in ((y0, -1), (y1, 1)):
            d.quoins(b, px, py, sx, sy, FOOTING, G_TOP)
    b.box(ux0, ux1, uy0, uy1, G_TOP, U_TOP, 'wood_light', bevel=d.WALL_BEVEL)
    for nm, plane, s0, s1 in (('-y', uy0, ux0, ux1), ('-x', ux0, uy0, uy1), ('+x', ux1, uy0, uy1)):
        f = Facade(b, nm, plane)
        a = s0 + 0.12
        while a < s1 - 0.08:
            f.rect(a - 0.008, a + 0.008, G_TOP, U_TOP, 0.006, 'wood_mid')
            a += 0.24
        f.rect(s0, s1, G_TOP, G_TOP + 0.04, d.BEAM_D, 'wood_dark')
    b.gable_block('x', ux0, ux1, uy0, uy1, U_TOP, zr, 'wood_mid')
    d.roof(b, 0, yc, 'x', ux1 + 0.09, hs, U_TOP, **d.RED)
    d.chimney(b, -0.9, 1.0, 1.5, zr + 0.1)
    for s in (-0.75, 0.75):
        d.dormer(b, '-y', uy0, s, U_TOP, wall='wood_light', **d.RED)
    d.weathervane(b, 0.6, yc, zr + 0.07)

    f = Facade(b, '-y', y0)
    for s in (-0.7, 0.7):
        d.barn_door(f, s, color='door')
        d.wall_lantern(f, s + 0.3 * (1 if s > 0 else -1), FOOTING + 0.36)
    d.plain_window(f, 0.0, FOOTING, sill='stone_mid')
    f = Facade(b, '-y', uy0)
    d.hay_loft(f, 0, G_TOP + 0.04, w=0.26, h=0.32)
    for s in (-0.8, 0.8):
        d.plain_window(f, s, G_TOP)
    # the hoist: beam out of the gable, rope, a crate on its way up
    b.box(-0.025, 0.025, uy0 - 0.3, uy0 + 0.02, G_TOP + 0.42, G_TOP + 0.47, 'wood_dark')
    b.box(-0.006, 0.006, uy0 - 0.28, uy0 - 0.27, G_TOP + 0.15, G_TOP + 0.42, 'burlap')
    d.crate(b, 0, uy0 - 0.275, 0.14, 0, G_TOP + 0.02)
    for nm, plane in (('-x', ux0), ('+x', ux1)):
        f = Facade(b, nm, plane)
        d.plain_window(f, yc, G_TOP)
        d.gable_frame(f, yc, hs, U_TOP, zr)
    for nm, plane in (('-x', x0), ('+x', x1)):
        d.plain_window(Facade(b, nm, plane), yc, FOOTING, sill='stone_mid')

    # the yard
    b.box(-0.95, 0.95, -1.45, y0, 0, 0.012, 'sand')
    d.log_pile(b, -1.15, -0.55, length=0.5, rows=(4, 3, 2, 1), axis='y')
    d.log_pile(b, -0.55, -1.2, length=0.5, rows=(3, 2, 1))
    d.stone_pile(b, 0.65, -0.35)
    d.stone_pile(b, 1.0, -0.6, size=0.12)
    d.ore_heap(b, 1.05, -1.15, r=0.22)
    d.crate(b, 0.62, -0.75, 0.16, 10)
    d.crate(b, 0.62, -0.75, 0.13, -25, z0=0.16)
    d.barrel(b, -0.2, -0.45, r=0.07, h=0.18)
    d.barrel(b, -0.05, -0.5, r=0.07, h=0.18)
    d.cart(b, 0.2, -1.05)
    for dy in (-0.06, 0.06):
        b.hcylinder('x', (0.2, -1.05 + dy, 0.2), 0.22, 0.05, 'wood_light', sides=5)
    return b


kit.run(OBJ, build, source_name="BigWarehouse1-lvl1", offset=(LVL2_DX, 0, 0),
        collection="Склады")
