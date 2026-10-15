"""DecGarden1-lvl3 -- the Decorative Garden (Декоративный сад) at its last upgrade. Footprint 3x1
(level 1's).

Level 2 (decgarden1_lvl2.build) with a small gilded statue rising out of the fountain, gold
finials on the two rose arches and gold balls topping the clipped cones.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402

OBJ = "DecGarden1-lvl3"
L2 = kit.borrow("decgarden1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    d.statue(b, 0, 0, 0.3, scale=0.5)
    for ax in (-0.72, 0.72):
        d.finial(b, ax, 0, 0.53, h=0.12)
    for x in (-1.25, 1.25):
        b.ball(x, 0, 0.41, 0.04, 0.08, 'gold', sides=6)
    return b


kit.run(OBJ, build, source_name="DecGarden1-lvl1", offset=(LVL3_DX, 0, 0),
        collection="Развлечения")
