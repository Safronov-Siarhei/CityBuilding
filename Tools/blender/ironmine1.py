"""IronMine1-lvl1 -- the Iron Mine (Железный рудник). Footprint 2x2. Front is -Y.

A rock hill with turf on its crown and rust-red ore showing in it; a timber-framed mine mouth
under a slate lean-to; a cart track running out of it with a cart of ore; a heap of ore; a
plank tool shed with picks leaning on it; a lantern on a post.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "IronMine1-lvl1"
ORE = 'roof_red_dark'          # iron ore: rust


def build(lo, hi):
    b = Builder()
    d.rock_hill(b, -0.95, 0.95, -0.3, 0.95, 1.1, ore=ORE)
    yf, s = -0.22, -0.18
    d.rock(b, s, 0.1, 0.48, 0.32, 0.86, 'stone_mid', seed=2)
    d.adit(b, s, yf)

    d.rails(b, s, -0.98, yf)
    d.minecart(b, s, -0.62, ORE)
    d.ore_heap(b, -0.68, -0.62, r=0.17, color=ORE, second='iron')

    # tool shed against the hill on the right
    x0, x1, y0, y1 = 0.42, 0.92, -0.62, -0.18
    b.box(x0, x1, y0, y1, 0, 0.5, 'wood_light', bevel=d.WALL_BEVEL)
    f = Facade(b, '-y', y0)
    for a in (0.54, 0.66, 0.78):
        f.rect(a - 0.008, a + 0.008, 0, 0.5, 0.006, 'wood_mid')
    d.plank_door(f, 0.62, base=0.0, h=0.42)
    for px in (x0, x1):
        b.box(px - 0.035, px + 0.035, y0 - 0.035, y0 + 0.035, 0, 0.52, 'wood_dark')
    d.shed_roof(b, 'x', x0 - 0.06, x1 + 0.04, y1 + 0.04, y0 - 0.1, 0.66, 0.48, **d.SLATE)
    for x in (0.1, 0.19):
        d.pickaxe(b, x, -0.36, lean_to_y=-0.26)
    d.crate(b, 0.8, -0.82, 0.16, 12)
    d.lamp(b, -0.62, -0.3)
    return b


kit.run(OBJ, build)
