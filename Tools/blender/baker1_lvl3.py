"""Baker1-lvl3 -- the Bakery (Пекарня) at its last upgrade. Footprint 1x2 (level 1's).
Front is -Y.

Level 2 (baker1_lvl2.build) selling in the street: a bread stall -- a trestle table of loaves
under a straw awning -- stands before the shop, a gold finial tops the front gable, and the
doorstep is paved.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import L_TOP  # noqa: E402

OBJ = "Baker1-lvl3"
L2 = kit.borrow("baker1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    x0, x1, y0 = -0.36, 0.36, -0.55
    zr = L_TOP + (x1 - x0) / 2 * d.PITCH
    d.finial(b, 0, y0 - 0.06, zr + 0.06, h=0.13)
    d.paving(b, -0.46, 0.46, -0.98, y0, z=0.01)

    # the bread stall
    sx0, sx1, sy0, sy1 = -0.24, 0.16, -0.86, -0.72
    b.box(sx0, sx1, sy0, sy1, 0.2, 0.23, 'wood_light')
    for px in (sx0 + 0.03, sx1 - 0.03):
        for py in (sy0 + 0.02, sy1 - 0.02):
            b.box(px - 0.015, px + 0.015, py - 0.015, py + 0.015, 0, 0.2, 'wood_dark')
    for k, x in enumerate((-0.19, -0.11, -0.03, 0.05, 0.12)):
        b.blob(x, (sy0 + sy1) / 2 + (0.02 if k % 2 else -0.02), 0.23, 0.035, 0.045,
               L2['BREAD'], sides=5)
    for px in (sx0, sx1):
        b.box(px - 0.012, px + 0.012, sy0 - 0.012, sy0 + 0.012, 0, 0.42, 'wood_dark')
    d.shed_roof(b, 'x', sx0 - 0.03, sx1 + 0.03, sy1 + 0.02, sy0 - 0.03, 0.48, 0.42, **d.STRAW)
    return b


kit.run(OBJ, build, source_name="Baker1-lvl1", offset=(LVL3_DX, 0, 0),
        collection="Производство еды")
