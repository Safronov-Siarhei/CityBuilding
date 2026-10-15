"""DecTree1-lvl3 -- the Decorative Tree (Декоративное дерево) at its last upgrade. Footprint 1x1
(level 1's).

Level 2 (dectree1_lvl2.build) turned into a wishing tree: gilded leaves catch the light through
the crown, a gold band runs round the planter's rim, and two more lanterns hang from the
boughs on the other side.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402

OBJ = "DecTree1-lvl3"
L2 = kit.borrow("dectree1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    b.ring(0, 0, 0.19, 0.21, 0.4, 0.45, ['gold'], sides=8)
    for x, y, z in ((0.34, -0.2, 0.98), (-0.3, -0.18, 1.12), (0.12, -0.3, 1.38),
                    (-0.18, 0.22, 1.48), (0.28, 0.12, 1.62), (-0.05, -0.12, 1.75)):
        b.blob(x, y, z, 0.055, 0.08, 'gold', sides=4)
    for x, y in ((0.06, -0.36), (-0.06, 0.36)):
        b.box(x - 0.005, x + 0.005, y - 0.005, y + 0.005, 0.72, 0.84, 'iron')
        b.box(x - 0.03, x + 0.03, y - 0.03, y + 0.03, 0.64, 0.72, 'window_light')
        b.pyramid(x, y, 0.72, 0.035, 0.035, 0.75, 'iron')
    return b


kit.run(OBJ, build, source_name="DecTree1-lvl1", offset=(LVL3_DX, 0, 0), collection="Развлечения")
