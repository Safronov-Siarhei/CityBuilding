"""BigBarn1-lvl2 -- the Big Barn (Большой амбар) after its first upgrade. Footprint 2x2 (level
1's). Front is -Y.

Level 1 rebuilt in stone up to the first floor, quoined, with a weathervane on the ridge, lanterns
either side of the doors and hay bales stacked in the yard. Level 1, for reference:


The Barn grown up: two full storeys of planks with the gable to the street, the same red
X-braced doors, a loft hatch up in the gable with its hoist beam, a lean-to shed on the side,
a haystack and a cart of sacks. Same doors, hatch and roof as the Barn, only more of it.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP, U_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "BigBarn1-lvl2"
LVL2_DX = 2.6   # level 2 stands this far +X of level 1


def build(lo, hi):
    b = Builder()
    x0, x1, y0, y1 = -0.62, 0.42, -0.5, 0.84
    xc, hs = (x0 + x1) / 2, (x1 - x0) / 2
    zr = U_TOP + hs * d.PITCH

    d.footing(b, x0, x1, y0, y1)
    b.box(x0, x1, y0, y1, FOOTING, G_TOP, 'stone_light', bevel=d.WALL_BEVEL)
    for px, sx in ((x0, -1), (x1, 1)):
        for py, sy in ((y0, -1), (y1, 1)):
            d.quoins(b, px, py, sx, sy, FOOTING, G_TOP)
    b.box(x0, x1, y0, y1, G_TOP, U_TOP, 'wood_light', bevel=d.WALL_BEVEL)
    for px in (x0, x1):
        for py in (y0, y1):
            b.box(px - 0.04, px + 0.04, py - 0.04, py + 0.04, G_TOP, U_TOP, 'wood_dark')
    b.gable_block('y', y0, y1, x0, x1, U_TOP, zr, 'wood_mid')
    d.roof(b, xc, (y0 + y1) / 2, 'y', (y1 - y0) / 2 + 0.09, hs, U_TOP, **d.RED)
    d.weathervane(b, xc, y0 + 0.15, zr + 0.07)

    f = Facade(b, '-y', y0)
    f.rect(x0, x1, G_TOP - 0.02, G_TOP + 0.02, d.BEAM_D, 'wood_dark')
    d.barn_door(f, xc)
    for s in (xc - 0.36, xc + 0.36):
        d.wall_lantern(f, s, FOOTING + 0.36)
    d.hay_loft(f, xc, G_TOP + 0.12)
    d.hay_loft(f, xc, U_TOP + 0.08, w=0.16, h=0.18)
    for nm, plane in (('-x', x0), ('+x', x1)):
        f = Facade(b, nm, plane)
        f.rect(y0, y1, G_TOP - 0.02, G_TOP + 0.02, d.BEAM_D, 'wood_dark')
        for s in (-0.1, 0.45):
            f.rect(s - 0.07, s + 0.07, G_TOP + 0.2, G_TOP + 0.34, 0.01, d.GLASS)

    # lean-to shed against the +x wall, open to the front, hay inside
    sx1 = 0.88
    b.box(x1, sx1, -0.1, 0.84, 0, 0.03, 'wood_mid')
    for py in (-0.06, 0.8):
        b.box(sx1 - 0.06, sx1 - 0.02, py - 0.02, py + 0.02, 0, 0.62, 'wood_dark')
    b.box(sx1 - 0.06, sx1 - 0.02, -0.08, 0.82, 0.58, 0.62, 'wood_dark')
    d.shed_roof(b, 'y', -0.16, 0.9, x1, sx1 + 0.02, 0.9, 0.6)
    for y in (0.1, 0.4, 0.66):
        b.box(x1 + 0.04, sx1 - 0.1, y - 0.12, y + 0.12, 0.03, 0.2, 'hay', bevel=0.015)
    b.box(x1 + 0.04, sx1 - 0.1, 0.28, 0.52, 0.2, 0.37, 'hay', bevel=0.015)

    # yard: haystack, a cart of sacks
    b.blob(0.68, -0.62, 0, 0.26, 0.5, 'hay', sides=7)
    d.cart(b, -0.5, -0.78)
    for dx in (-0.1, 0.06):
        d.sack(b, -0.5 + dx, -0.78, 0.14)
    d.hay_bale(b, 0.2, -0.88, 8)
    d.hay_bale(b, 0.22, -0.64, -6)
    d.hay_bale(b, 0.21, -0.76, 80, z0=0.15)
    return b


kit.run(OBJ, build, source_name="BigBarn1-lvl1", offset=(LVL2_DX, 0, 0),
        collection="Склады")
