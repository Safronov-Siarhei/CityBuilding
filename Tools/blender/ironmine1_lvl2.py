"""IronMine1-lvl2 -- the Iron Mine (Железный рудник) after its first upgrade. Footprint 2x2
(level 1's). Front is -Y.

Level 1 with a stamp mill crushing the ore beside the heap -- three iron stamps in a timber
frame on a cam shaft -- and the lantern moved down by the track. Level 1, for reference:


A rock hill with turf on its crown and blue-grey iron ore showing in it; a timber-framed mine mouth
under a slate lean-to; a cart track running out of it with a cart of ore; a heap of ore; a
plank tool shed with picks leaning on it; a lantern on a post.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "IronMine1-lvl2"
LVL2_DX = 2.6   # level 2 stands this far +X of level 1
ORE = 'roof_slate'             # iron ore: blue-grey metal, lighter than coal


def build(lo, hi):
    b = Builder()
    d.rock_hill(b, -0.95, 0.95, -0.3, 0.95, 1.1, ore=ORE)
    yf, s = -0.22, -0.18
    d.rock(b, s, 0.1, 0.48, 0.32, 0.86, 'stone_mid', seed=2)
    d.adit(b, s, yf)

    d.rails(b, s, -0.98, yf)
    d.minecart(b, s, -0.62, ORE)
    d.ore_heap(b, -0.68, -0.62, r=0.17, color=ORE, second='stone_dark')

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
    # the stamp mill
    mx0, mx1, my = -0.9, -0.46, -0.28
    b.box(mx0, mx1, my - 0.09, my + 0.09, 0, 0.06, 'wood_dark')
    for px in (mx0 + 0.03, mx1 - 0.03):
        b.box(px - 0.025, px + 0.025, my - 0.025, my + 0.025, 0.06, 0.62, 'wood_dark')
    b.box(mx0, mx1, my - 0.03, my + 0.03, 0.58, 0.64, 'wood_mid')
    b.hcylinder('x', (( mx0 + mx1) / 2, my + 0.06, 0.44), (mx1 - mx0) / 2 - 0.03, 0.025, 'iron')
    for k, px in enumerate((-0.79, -0.68, -0.57)):
        z = 0.08 + 0.03 * k
        b.box(px - 0.012, px + 0.012, my - 0.012, my + 0.012, z + 0.2, 0.62, 'wood_light')
        b.box(px - 0.035, px + 0.035, my - 0.035, my + 0.035, z, z + 0.2, 'iron', bevel=0.006)
    d.lamp(b, -0.38, -0.86, h=0.5)
    return b


kit.run(OBJ, build, source_name="IronMine1-lvl1", offset=(LVL2_DX, 0, 0),
        collection="Производственные здания")
