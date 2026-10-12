"""FortifiedTower1-lvl1 -- the Fortified Tower (Укреплённая башня), stone tier. Footprint 2x2,
stands in the stone wall line.

A broad square keep filling nearly the whole cell block so a wall from any side butts into it:
stone on a footing, string courses at the standard storeys, arrow slits, a door with steps, a
corbelled crenellated top, a blue pyramid roof and a pennant.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP, U_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "FortifiedTower1-lvl1"


def build(lo, hi):
    b = Builder()
    top = hi[2]
    h = 0.86
    body_top = U_TOP + 0.6
    d.footing(b, -h, h, -h, h)
    b.box(-h, h, -h, h, FOOTING, body_top, 'stone_light', bevel=0.025)
    for z in (G_TOP, U_TOP):
        b.box(-h - 0.03, h + 0.03, -h - 0.03, h + 0.03, z - 0.03, z + 0.03, 'stone_mid')
    b.box(-h - 0.08, h + 0.08, -h - 0.08, h + 0.08, body_top - 0.1, body_top + 0.06, 'stone_mid',
          bevel=0.015)
    ct = body_top + 0.06
    e = h + 0.08
    d.merlons(b, 'x', -e, e, -e, -e + 0.14, ct)
    d.merlons(b, 'x', -e, e, e - 0.14, e, ct)
    d.merlons(b, 'y', -e + 0.14, e - 0.14, -e, -e + 0.14, ct)
    d.merlons(b, 'y', -e + 0.14, e - 0.14, e - 0.14, e, ct)
    for nm, plane in (('-y', -h), ('+y', h), ('-x', -h), ('+x', h)):
        f = Facade(b, nm, plane)
        for s in (-0.42, 0.42):
            d.slit(f, s, G_TOP + 0.18, G_TOP + 0.44)
            d.slit(f, s, U_TOP + 0.16, U_TOP + 0.42)
    f = Facade(b, '-y', -h)
    d.arch_door(f, 0)
    d.banner(f, 0, U_TOP - 0.06, 0.42, w=0.16, color='banner_blue')
    b.box(-0.26, 0.26, -h - 0.14, -h, 0, 0.05, 'stone_mid', bevel=0.008)

    apex = top - 0.2
    b.pyramid(0, 0, ct, h - 0.02, h - 0.02, apex, 'roof_blue')
    b.box(-0.015, 0.015, -0.015, 0.015, apex - 0.05, top, 'wood_dark')
    b.prism([(0.015, top - 0.01), (0.015, top - 0.14), (0.24, top - 0.075)], (0, 0.005, 0),
            (1, 0, 0), (0, 0, 1), 0.01, 'banner_blue', back_cap=True)
    return b


kit.run(OBJ, build)
