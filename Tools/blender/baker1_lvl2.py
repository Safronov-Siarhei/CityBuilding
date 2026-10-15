"""Baker1-lvl2 -- the Bakery (Пекарня) after its first upgrade. Footprint 1x2 (level 1's).
Front is -Y.

Level 1 raised to a storey and a half with a lit loft window in the gable over the door, and the
oven chimney smoking. Level 1, for reference:


A small half-timbered bakehouse with a straw roof, its gable to the street: the door under a
gilded pretzel sign, a warm shop window with loaves on the sill. Built onto the back, the domed
brick bread oven with its glowing mouth and chimney. Flour sacks and a basket of bread by the
door.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP, L_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "Baker1-lvl2"
LVL2_DX = 1.6   # level 2 stands this far +X of level 1
BREAD = 'roof_straw_dark'


def build(lo, hi):
    b = Builder()
    x0, x1, y0, y1 = -0.36, 0.36, -0.55, 0.42
    xc, hs = 0.0, (x1 - x0) / 2
    d.footing(b, x0, x1, y0, y1)
    b.box(x0, x1, y0, y1, FOOTING, L_TOP, 'plaster', bevel=d.WALL_BEVEL)
    zr = L_TOP + hs * d.PITCH
    b.gable_block('y', y0, y1, x0, x1, L_TOP, zr, 'plaster')
    d.roof(b, xc, (y0 + y1) / 2, 'y', (y1 - y0) / 2 + 0.07, hs, L_TOP, **d.STRAW)

    # front: framed gable, door, shop window with loaves, pretzel sign
    f = Facade(b, '-y', y0)
    d.timber(f, x0, x1, FOOTING, G_TOP, (x0 + 0.025, -0.06, x1 - 0.025))
    d.timber(f, x0, x1, G_TOP, L_TOP, (x0 + 0.025, x1 - 0.025))
    d.lit_window(f, 0.0, G_TOP - d.WIN_SILL + 0.02, w=0.16)
    d.gable_frame(f, xc, hs, L_TOP, zr)
    d.plank_door(f, -0.21)
    d.lit_window(f, 0.2, FOOTING, w=0.24)
    for x in (0.12, 0.2, 0.28):
        b.blob(x, y0 - 0.03, FOOTING + d.WIN_SILL, 0.035, 0.04, BREAD, sides=5)
    d.step(b, -0.36, -0.06, y0)
    b.box(-0.04, -0.02, y0 - 0.2, y0, G_TOP - 0.06, G_TOP - 0.04, 'iron')
    sign = Facade(b, '+x', -0.02)
    sign.disc(y0 - 0.13, G_TOP - 0.17, 0.085, 0.02, 'gold', sides=8)
    sign.disc(y0 - 0.15, G_TOP - 0.15, 0.03, 0.025, 'plaster', sides=6)
    sign.disc(y0 - 0.11, G_TOP - 0.15, 0.03, 0.025, 'plaster', sides=6)
    sign.disc(y0 - 0.13, G_TOP - 0.215, 0.025, 0.025, 'plaster', sides=6)
    b.box(-0.03, -0.01, y0 - 0.14, y0 - 0.12, G_TOP - 0.1, G_TOP - 0.06, 'iron')

    # sides: framing and a window each
    for nm, plane in (('-x', x0), ('+x', x1)):
        sf = Facade(b, nm, plane)
        d.timber(sf, y0, y1, FOOTING, G_TOP, (y0 + 0.025, -0.05, y1 - 0.025))
        d.timber(sf, y0, y1, G_TOP, L_TOP, (y0 + 0.025, y1 - 0.025))
        d.plain_window(sf, 0.18, FOOTING)

    # the bread oven at the back
    ox, oy = 0.0, 0.7
    b.box(ox - 0.32, ox + 0.32, oy - 0.28, oy + 0.27, 0, 0.18, 'stone_mid', bevel=0.01)
    b.dome(ox, oy, 0.18, 0.3, 0.38, ['roof_orange_dark', 'plaster_pink'], sides=8)
    of = Facade(b, '+x', ox + 0.27)
    of.arch(oy, 0.18, 0.16, 0.16, 0.03, 'stone_dark')
    of.arch(oy, 0.18, 0.11, 0.11, 0.035, 'fire')
    d.chimney(b, ox - 0.12, oy + 0.08, 0.3, 0.98, hw=0.07, color='roof_orange_dark')
    b.blob(ox - 0.12, oy + 0.08, 1.02, 0.06, 0.1, 'stone_light', sides=5)
    b.blob(ox - 0.08, oy + 0.11, 1.12, 0.045, 0.08, 'white', sides=5)

    # sacks and bread
    for x, y in ((0.3, -0.7), (0.4, -0.8)):
        d.sack(b, x, y, r=0.065)
    d.basket(b, -0.38, -0.78, BREAD)
    return b


kit.run(OBJ, build, source_name="Baker1-lvl1", offset=(LVL2_DX, 0, 0),
        collection="Производство еды")
