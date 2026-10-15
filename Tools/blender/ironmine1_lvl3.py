"""IronMine1-lvl3 -- the Iron Mine (Железный рудник) at its last upgrade. Footprint 2x2 (level
1's). Front is -Y.

Level 2 (ironmine1_lvl2.build) working at full tilt: a second cart of ore waits by the stamp
mill, the yard along the track is paved, and a guild pennant flies on a tall pole with a gold
ball.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402

OBJ = "IronMine1-lvl3"
L2 = kit.borrow("ironmine1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    d.paving(b, -0.4, 0.05, -0.99, -0.24, z=0.01)
    d.minecart(b, -0.7, -0.84, L2['ORE'])
    b.box(0.26, 0.34, -0.92, -0.84, 0, 0.05, 'stone_mid')
    d.pennant(b, 0.3, -0.88, 0.05, h=1.0, color='banner_red', length=0.2)
    return b


kit.run(OBJ, build, source_name="IronMine1-lvl1", offset=(LVL3_DX, 0, 0),
        collection="Производственные здания")
