"""DecTree1-lvl2 -- the Decorative Tree (Декоративное дерево) after its first upgrade.
Footprint 1x1 (level 1's).

Level 1 in full bloom -- twice the blossom, pink and white -- with two lanterns hung from the
lower boughs. Level 1, for reference:


An ornamental tree in an octagonal stone planter: a trunk that forks, a crown of three
faceted tiers with a few pink blossoms, flowers round its foot.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from kit import Builder  # noqa: E402

OBJ = "DecTree1-lvl2"
LVL2_DX = 1.6   # level 2 stands this far +X of level 1


def build(lo, hi):
    b = Builder()
    top = hi[2]
    b.ring(0, 0, 0, 0.16, 0.34, 0.42, ['stone_light', 'stone_mid'])
    b.ring(0, 0, 0.16, 0.19, 0.32, 0.44, ['stone_light'], bevel=0.006)
    b.cylinder(0, 0, 0, 0.15, 0.35, 'grass_dark')

    b.cylinder(0, 0, 0.15, 0.75, 0.06, 'wood_mid', sides=6)
    b.rod((0, 0, 0.6), (0.14, 0.05, 0.95), 0.06, 'wood_mid')
    b.rod((0, 0, 0.62), (-0.12, -0.06, 0.92), 0.055, 'wood_mid')
    for z, r, h, c in ((0.7, 0.42, 0.62, 'leaf'), (1.05, 0.36, 0.55, 'grass_dark'),
                       (top - 0.6, 0.24, 0.6, 'leaf')):
        b.blob(0.02, 0, z, r, h, c, sides=7)
    for x, y, z in ((0.3, -0.15, 1.05), (-0.25, -0.25, 1.15), (0.05, -0.35, 0.95),
                    (-0.1, 0.3, 1.3), (0.22, 0.2, 1.45)):
        b.blob(x, y, z, 0.05, 0.07, 'plaster_pink', sides=4)
    for k, (x, y, z) in enumerate(((0.38, 0.1, 0.95), (-0.36, 0.1, 1.0), (0.15, 0.33, 1.1),
                                   (-0.28, -0.12, 1.25), (0.25, -0.12, 1.3), (0.0, 0.25, 1.55),
                                   (-0.12, -0.18, 1.6), (0.32, 0.0, 1.15))):
        b.blob(x, y, z, 0.05, 0.07, 'white' if k % 2 else 'plaster_pink', sides=4)
    for x, y in ((0.34, -0.06), (-0.32, 0.06)):   # lanterns hung from the lower boughs
        b.box(x - 0.005, x + 0.005, y - 0.005, y + 0.005, 0.68, 0.8, 'iron')
        b.box(x - 0.03, x + 0.03, y - 0.03, y + 0.03, 0.6, 0.68, 'window_light')
        b.pyramid(x, y, 0.68, 0.035, 0.035, 0.71, 'iron')
    for x, y in ((0.2, -0.18), (-0.2, 0.15), (0.18, 0.2), (-0.2, -0.2)):
        d.flowers(b, x, y, 0.15, r=0.035)
    return b


kit.run(OBJ, build, source_name="DecTree1-lvl1", offset=(LVL2_DX, 0, 0),
        collection="Развлечения")
