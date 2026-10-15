"""TownSquare1-lvl2 -- the Town Square (Городская площадь) after its first upgrade. Footprint
2x2 (level 1's).

Level 1 with a second, smaller bowl on the fountain crowned in gold, and a ring of pink and
white paving round the basin. Level 1, for reference:


A paved square around an octagonal fountain: a stone basin of water, a column with a bowl
spilling into it, four benches, four lamp posts on the corners, flowers in stone tubs.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from kit import Builder  # noqa: E402

OBJ = "TownSquare1-lvl2"
LVL2_DX = 2.6   # level 2 stands this far +X of level 1


def build(lo, hi):
    b = Builder()
    b.box(-0.97, 0.97, -0.97, 0.97, 0, 0.03, 'cobble')
    b.box(-0.9, 0.9, -0.9, 0.9, 0.03, 0.035, 'stone_mid')
    b.ring(0, 0, 0.035, 0.04, 0.52, 0.64, ['plaster_pink', 'stone_light'], sides=16)

    # fountain
    b.ring(0, 0, 0.035, 0.22, 0.4, 0.5, ['stone_light', 'stone_mid'])
    b.ring(0, 0, 0.22, 0.26, 0.38, 0.52, ['stone_light'], bevel=0.006)
    b.cylinder(0, 0, 0.035, 0.18, 0.41, 'water')
    b.cylinder(0, 0, 0.18, 0.62, 0.07, 'stone_light', sides=8)
    b.cylinder(0, 0, 0.6, 0.66, 0.22, 'stone_light', sides=8, bevel=0.006)
    b.cylinder(0, 0, 0.66, 0.67, 0.19, 'water', sides=8)
    b.cylinder(0, 0, 0.67, 0.86, 0.035, 'stone_light', sides=6)
    b.cylinder(0, 0, 0.86, 0.9, 0.12, 'stone_light', sides=8, bevel=0.005)   # a second bowl
    b.cylinder(0, 0, 0.9, 0.905, 0.1, 'water', sides=8)
    b.cylinder(0, 0, 0.9, 1.02, 0.025, 'stone_light', sides=6)
    b.blob(0, 0, 1.0, 0.045, 0.13, 'gold', sides=5)
    for x, y in ((0.078, 0.078), (-0.078, 0.078), (-0.078, -0.078), (0.078, -0.078)):
        b.box(x - 0.012, x + 0.012, y - 0.012, y + 0.012, 0.67, 0.86, 'water')
    for k in range(4):  # water falling from the bowl
        import math
        a = math.pi / 4 + k * math.pi / 2
        x, y = 0.2 * math.cos(a), 0.2 * math.sin(a)
        b.box(x - 0.02, x + 0.02, y - 0.02, y + 0.02, 0.2, 0.6, 'water')

    # benches facing the fountain, lamps, flower tubs
    d.bench(b, -0.25, 0.25, -0.82, -0.7)
    d.bench(b, -0.25, 0.25, 0.7, 0.82)
    d.bench(b, -0.82, -0.7, -0.25, 0.25)
    d.bench(b, 0.7, 0.82, -0.25, 0.25)
    for sx in (-1, 1):
        for sy in (-1, 1):
            d.lamp(b, sx * 0.8, sy * 0.8)
            b.cylinder(sx * 0.58, sy * 0.58, 0.035, 0.14, 0.09, 'stone_mid', sides=6)
            d.flowers(b, sx * 0.58, sy * 0.58, 0.14)
    return b


kit.run(OBJ, build, source_name="TownSquare1-lvl1", offset=(LVL2_DX, 0, 0),
        collection="Развлечения")
