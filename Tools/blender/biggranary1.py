"""BigGranary1-lvl1 -- the Big Larder (Большая кладовая), food store. Footprint 3x2. Front -Y.

The Larder grown up: a longer stone storehouse with two doors onto a timber loading platform
under a lean-to awning, timber gables with vents, and the food waiting outside -- crates of
vegetables, baskets of apples, sacks, barrels and a cart.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP, L_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "BigGranary1-lvl1"


def basket(b, x, y, z0, fruit):
    b.cylinder(x, y, z0, z0 + 0.08, 0.065, 'wood_light', sides=6)
    b.blob(x, y, z0 + 0.06, 0.055, 0.06, fruit, sides=5)


def veg_crate(b, x, y, z0, veg):
    b.box(x - 0.1, x + 0.1, y - 0.08, y + 0.08, z0, z0 + 0.1, 'wood_light', bevel=0.008)
    b.blob(x, y, z0 + 0.08, 0.08, 0.06, veg, sides=5)


def build(lo, hi):
    b = Builder()
    x0, x1, y0, y1 = -1.3, 1.3, -0.12, 0.84
    yc, hs = (y0 + y1) / 2, (y1 - y0) / 2
    zr = L_TOP + hs * d.PITCH

    d.footing(b, x0, x1, y0, y1)
    b.box(x0, x1, y0, y1, FOOTING, L_TOP, 'stone_light', bevel=d.WALL_BEVEL)
    b.gable_block('x', x0, x1, y0, y1, L_TOP, zr, 'wood_mid')
    d.roof(b, 0, yc, 'x', x1 + 0.09, hs, L_TOP, **d.RED)
    for nm, plane in (('-x', x0), ('+x', x1)):
        f = Facade(b, nm, plane)
        f.rect(yc - 0.06, yc + 0.06, L_TOP + 0.08, L_TOP + 0.2, 0.012, d.GLASS)
        d.plain_window(f, yc, FOOTING)

    # loading platform and its awning along the front
    py0 = -0.5
    b.box(x0 + 0.05, x1 - 0.05, py0, y0, 0, FOOTING, 'wood_mid')
    for px in (x0 + 0.1, 0.0, x1 - 0.1):
        b.box(px - 0.025, px + 0.025, py0 + 0.02, py0 + 0.07, FOOTING, G_TOP + 0.02, 'wood_dark')
    d.shed_roof(b, 'x', x0, x1, y0, py0 - 0.04, L_TOP - 0.06, G_TOP)
    f = Facade(b, '-y', y0)
    for s in (-0.65, 0.65):
        d.plank_door(f, s)
    for s in (-1.05, 0.0, 1.05):
        d.shutter_window(f, s, FOOTING)
    d.plain_window(Facade(b, '+y', y1), -0.6, FOOTING)
    d.plain_window(Facade(b, '+y', y1), 0.6, FOOTING)

    # the food: on the platform and in the yard
    for x, veg in ((-1.0, 'roof_green'), (-0.8, 'roof_orange')):
        veg_crate(b, x, py0 + 0.13, FOOTING, veg)
    for x, fruit in ((0.3, 'roof_red'), (0.45, 'plaster_yellow')):
        basket(b, x, py0 + 0.12, FOOTING, fruit)
    for x in (1.0, 1.15):
        d.sack(b, x, py0 + 0.14, FOOTING)
    d.barrel(b, -1.2, -0.75)
    d.barrel(b, -1.02, -0.82)
    d.cart(b, 0.35, -0.78)
    veg_crate(b, 0.3, -0.78, 0.14, 'roof_green')
    d.sack(b, 0.45, -0.75, 0.14)
    b.box(-0.8, -0.5, -0.98, py0, 0, 0.012, 'sand')
    return b


kit.run(OBJ, build)
