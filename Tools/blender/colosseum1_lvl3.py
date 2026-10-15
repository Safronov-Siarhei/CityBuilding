"""Colosseum1-lvl3 -- the Colosseum (Коллизей) at its last upgrade. Footprint 4x6 (level 1's).
Front is -Y.

Level 2 (colosseum1_lvl2.build) on a games day: bunting runs from mast to mast all round the
rim, and a second pair of gilded statues stands on the attic at the back end.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402

OBJ = "Colosseum1-lvl3"
L2 = kit.borrow("colosseum1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    top = hi[2]
    rx, ry, wall, N = 1.9, 2.88, 0.3, L2['N']
    attic = L2['T_TOP'] + 0.2
    masts = []
    for i in range(0, N, 2):
        a = 2 * math.pi * i / N
        masts.append(((rx - wall / 2) * math.cos(a), (ry - wall / 2) * math.sin(a), top - 0.3))
    for p, q in zip(masts, masts[1:] + masts[:1]):
        d.bunting(b, p, q, sag=0.1, step=0.13)
    for sx in (-0.38, 0.38):   # the back pair, as level 2's front pair
        sy = (ry - wall * 0.5) * math.sqrt(max(0.0, 1 - (sx / rx) ** 2))
        b.cylinder(sx, sy, attic, attic + 0.08, 0.07, 'stone_mid', sides=6)
        b.blob(sx, sy, attic + 0.08, 0.06, 0.22, 'gold', sides=6)
        b.blob(sx, sy, attic + 0.28, 0.035, 0.07, 'gold', sides=5)
    return b


kit.run(OBJ, build, source_name="Colosseum1-lvl1", offset=(LVL3_DX, 0, 0),
        collection="Развлечения")
