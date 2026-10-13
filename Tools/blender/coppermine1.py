"""CopperMine1-lvl1 -- the Copper Mine (Медный рудник). Footprint 2x2. Front is -Y.

A rock hill with copper ore and green patina showing in it, the timber-framed mine mouth under a
slate lean-to, a cart track with a cart of ore, and beside it the washing sluice that rinses the
ore: a plank trough on trestles fed from a water butt, a heap of washed copper ore and crates.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from kit import Builder  # noqa: E402

OBJ = "CopperMine1-lvl1"
ORE = 'roof_orange'            # copper
PATINA = 'roof_green'


def build(lo, hi):
    b = Builder()
    d.rock_hill(b, -0.95, 0.95, -0.3, 0.95, 1.05, ore=ORE)
    d.ore_vein(b, -0.72, -0.1, 0.42, PATINA)
    d.ore_vein(b, 0.7, 0.0, 0.5, PATINA)
    yf, s = -0.22, 0.25
    d.rock(b, s, 0.1, 0.48, 0.32, 0.84, 'stone_mid', seed=4)
    d.adit(b, s, yf)
    d.rails(b, s, -0.98, yf)
    d.minecart(b, s, -0.6, ORE)

    # washing sluice: a trough falling towards the front on two trestles, a butt at its head
    x = -0.58
    for y, h in ((-0.32, 0.36), (-0.8, 0.24)):
        for k in (-1, 1):
            b.rod((x + k * 0.1, y, 0.01), (x + k * 0.06, y, h), 0.03, 'wood_dark')
        b.box(x - 0.12, x + 0.12, y - 0.02, y + 0.02, h - 0.03, h, 'wood_dark')
    b.rod((x, -0.24, 0.42), (x, -0.92, 0.27), 0.14, 'wood_light')
    b.rod((x, -0.24, 0.455), (x, -0.92, 0.305), 0.09, 'water')
    d.barrel(b, x - 0.04, -0.1, z0=0.0, r=0.11, h=0.36, top='water')
    d.ore_heap(b, -0.25, -0.84, r=0.12, color=ORE, second=PATINA)

    d.crate(b, 0.75, -0.45, 0.17, 10)
    d.crate(b, 0.74, -0.45, 0.13, 35, z0=0.17)
    d.sack(b, 0.72, -0.75)
    d.lamp(b, -0.1, -0.3)
    return b


kit.run(OBJ, build)
