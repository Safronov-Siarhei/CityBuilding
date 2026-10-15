"""DecGarden1-lvl2 -- the Decorative Garden (Декоративный сад) after its first upgrade.
Footprint 3x1 (level 1's).

Level 1 with the birdbath grown into a small fountain, two rose arches over the path and
clipped topiary cones at its ends. Level 1, for reference:


A long formal flower bed: a clipped hedge border, a gravel path down the middle with a little
stone birdbath, rows of flowers in the four colours of the palette either side.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from kit import Builder  # noqa: E402

OBJ = "DecGarden1-lvl2"
LVL2_DX = 3.6   # level 2 stands this far +X of level 1


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
    d.fountain(b, 0, 0, 0.03, r=0.17)
    for ax in (-0.72, 0.72):   # rose arches over the path
        for ay in (-0.13, 0.13):
            b.box(ax - 0.02, ax + 0.02, ay - 0.02, ay + 0.02, 0.03, 0.5, 'wood_dark')
            b.ball(ax, ay, 0.05, 0.05, 0.42, 'leaf', sides=5, top='grass')
        b.box(ax - 0.025, ax + 0.025, -0.15, 0.15, 0.48, 0.53, 'wood_dark')
        for ay in (-0.1, 0.0, 0.1):
            b.blob(ax, ay, 0.49, 0.06, 0.09, 'leaf', sides=5)
        for ay, c in ((-0.12, 'roof_red'), (0.04, 'plaster_pink'), (0.13, 'roof_red')):
            b.blob(ax + 0.03, ay, 0.53, 0.025, 0.04, c, sides=4)
    for x in (-1.25, 1.25):   # topiary cones at the path's ends
        b.cylinder(x, 0, 0.03, 0.08, 0.05, 'stone_light', sides=6)
        b.pyramid(x, 0, 0.08, 0.07, 0.07, 0.42, 'leaf', sides=6, rot_deg=0)
    return b


kit.run(OBJ, build, source_name="DecGarden1-lvl1", offset=(LVL2_DX, 0, 0),
        collection="Развлечения")
