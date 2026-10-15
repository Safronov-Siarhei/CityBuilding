"""Flag1-lvl2 -- the Flag (Флаг) after its first upgrade. Footprint 1x1 (level 1's).

Level 1 with the pole raised by 0.3, a longer banner and a gilded cap on the plinth.
Level 1, for reference:


A stepped stone plinth ringed with flowers, a tall pole with a gold finial and a big violet
banner with a gold emblem, caught in the wind in two folds.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from kit import Builder  # noqa: E402

OBJ = "Flag1-lvl2"
LVL2_DX = 1.6   # level 2 stands this far +X of level 1


def build(lo, hi):
    b = Builder()
    top = hi[2] + 0.3   # a taller pole than at level 1
    b.box(-0.42, 0.42, -0.42, 0.42, 0, 0.06, 'stone_mid', bevel=0.008)
    b.box(-0.3, 0.3, -0.3, 0.3, 0.06, 0.14, 'stone_light', bevel=0.008)
    b.box(-0.16, 0.16, -0.16, 0.16, 0.14, 0.28, 'stone_mid', bevel=0.008)
    b.box(-0.18, 0.18, -0.18, 0.18, 0.26, 0.3, 'gold', bevel=0.006)
    for x, y in ((-0.36, -0.36), (0.36, -0.36), (-0.36, 0.36), (0.36, 0.36)):
        d.flowers(b, x, y, 0.06, r=0.04)

    b.cylinder(0, 0, 0.28, top - 0.08, 0.025, 'wood_dark', sides=6)
    b.cylinder(0, 0, top - 0.08, top - 0.02, 0.045, 'gold', sides=6)
    b.blob(0, 0, top - 0.04, 0.035, 0.04, 'gold', sides=4)

    # the banner in two folds, hanging from the top of the pole towards +x
    y = 0.0
    z1, z0 = top - 0.14, top - 0.78
    folds = [(0.02, 0.0), (0.24, 0.06), (0.46, -0.02)]
    for (xa, ya), (xb, yb) in zip(folds, folds[1:]):
        b.prism([(xa, z0 + 0.04 * (xa > 0.3)), (xb, z0 + 0.04 * (xb > 0.3)), (xb, z1), (xa, z1)],
                (0, y + (ya + yb) / 2, 0), (1, 0, 0), (0, 0, 1), 0.012, 'roof_purple', back_cap=True)
    for xa, ya, xb, yb in ((0.02, 0.0, 0.24, 0.06),):
        pass
    b.prism([(0.16, z0 + 0.14), (0.3, z0 + 0.14), (0.3, z0 + 0.34), (0.16, z0 + 0.34)],
            (0, 0.02, 0), (1, 0, 0), (0, 0, 1), 0.03, 'gold', back_cap=True)
    b.box(0.0, 0.48, -0.012, 0.012, z1, z1 + 0.025, 'wood_dark')
    return b


kit.run(OBJ, build, source_name="Flag1-lvl1", offset=(LVL2_DX, 0, 0),
        collection="Развлечения")
