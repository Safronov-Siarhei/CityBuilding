"""DecGarden1-lvl1 -- the Decorative Garden (Декоративный сад). Footprint 3x1, kept low.

A long formal flower bed: a clipped hedge border, a gravel path down the middle with a little
stone birdbath, rows of flowers in the four colours of the palette either side.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from kit import Builder  # noqa: E402

OBJ = "DecGarden1-lvl1"


def build(lo, hi):
    b = Builder()
    b.box(-1.45, 1.45, -0.45, 0.45, 0, 0.03, 'grass_dark')
    for x0, x1, y0, y1 in ((-1.45, 1.45, -0.45, -0.37), (-1.45, 1.45, 0.37, 0.45),
                           (-1.45, -1.37, -0.37, 0.37), (1.37, 1.45, -0.37, 0.37)):
        b.box(x0, x1, y0, y1, 0.03, 0.18, 'leaf', bevel=0.015)
    b.box(-1.37, 1.37, -0.08, 0.08, 0.03, 0.035, 'sand')

    colours = ['roof_red', 'plaster_yellow', 'roof_purple', 'white']
    for row, y in enumerate((-0.24, 0.24)):
        for i in range(8):
            x = -1.18 + i * 0.29 + (0.145 if row else 0)
            if abs(x) < 0.2:
                continue
            c = colours[(i + row) % 4]
            d.flowers(b, x, y, 0.03, colors=(c, c, c), r=0.05)
    b.cylinder(0, 0, 0.03, 0.2, 0.05, 'stone_light', sides=6)
    b.cylinder(0, 0, 0.2, 0.25, 0.13, 'stone_light', sides=8, bevel=0.005)
    b.cylinder(0, 0, 0.25, 0.255, 0.11, 'water', sides=8)
    return b


kit.run(OBJ, build)
