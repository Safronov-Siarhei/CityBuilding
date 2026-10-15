"""DecBush1-lvl3 -- the Decorative Bush (Декоративный куст) at its last upgrade. Footprint 1x1
(level 1's).

Level 2 (decbush1_lvl2.build) with a gold finial on the topiary's crown and gilded balls on the
back corners of the clipped hedge.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402

OBJ = "DecBush1-lvl3"
L2 = kit.borrow("decbush1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    d.finial(b, 0, 0.02, hi[2] + 0.18, h=0.14)
    for x in (-0.35, 0.35):
        b.ball(x, 0.35, 0.25, 0.05, 0.09, 'gold', sides=6)
    return b


kit.run(OBJ, build, source_name="DecBush1-lvl1", offset=(LVL3_DX, 0, 0), collection="Развлечения")
