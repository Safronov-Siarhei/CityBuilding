"""DecBush1-lvl2 -- the Decorative Bush (Декоративный куст) after its first upgrade. Footprint
1x1 (level 1's).

Level 1 with the topiary clipped into a second, smaller ball on top, and stone urns of flowers
on the front corners. Level 1, for reference:


A square box-hedge planter with a clipped topiary ball on a short stem in the middle, two
smaller bushes in the corners and flowers along the front.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from kit import Builder  # noqa: E402

OBJ = "DecBush1-lvl2"
LVL2_DX = 1.6   # level 2 stands this far +X of level 1


def build(lo, hi):
    b = Builder()
    top = hi[2]
    b.box(-0.44, 0.44, -0.44, 0.44, 0, 0.1, 'stone_mid', bevel=0.01)
    b.box(-0.4, 0.4, -0.4, 0.4, 0.1, 0.11, 'grass_dark')
    for x0, x1, y0, y1 in ((-0.4, 0.4, 0.3, 0.4), (-0.4, -0.3, -0.4, 0.3), (0.3, 0.4, -0.4, 0.3)):
        b.box(x0, x1, y0, y1, 0.1, 0.26, 'leaf', bevel=0.02)

    b.cylinder(0, 0.02, 0.11, 0.3, 0.035, 'wood_mid', sides=6)
    b.blob(0, 0.02, 0.24, 0.24, top - 0.3, 'leaf', sides=8)
    b.blob(0, 0.02, 0.33, 0.2, top - 0.42, 'grass_dark', sides=8)
    for x, y in ((-0.25, -0.24), (0.25, -0.24)):
        b.blob(x, y, 0.11, 0.1, 0.22, 'leaf', sides=6)
    for x in (-0.12, 0.0, 0.12):
        d.flowers(b, x, -0.3, 0.11, r=0.03)
    b.cylinder(0, 0.02, top - 0.12, top + 0.02, 0.025, 'wood_mid', sides=6)
    b.ball(0, 0.02, top - 0.02, 0.12, 0.22, 'leaf', sides=7, top='grass')
    for x in (-0.37, 0.37):   # urns on the front corners
        b.cylinder(x, -0.37, 0.1, 0.14, 0.035, 'stone_mid', sides=6)
        b.cylinder(x, -0.37, 0.14, 0.24, 0.055, 'stone_light', sides=8)
        d.flowers(b, x, -0.37, 0.24, r=0.035, colors=('roof_red', 'white', 'plaster_pink'))
    return b


kit.run(OBJ, build, source_name="DecBush1-lvl1", offset=(LVL2_DX, 0, 0),
        collection="Развлечения")
