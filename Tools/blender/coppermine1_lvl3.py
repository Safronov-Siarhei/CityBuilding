"""CopperMine1-lvl3 -- the Copper Mine (Медный рудник) at its last upgrade. Footprint 2x2 (level
1's). Front is -Y.

Level 2 (coppermine1_lvl2.build) working at full tilt: a second cart of ore stands at the end
of the track, the track's yard is paved, and a guild pennant flies on a tall pole with a gold
ball.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402

OBJ = "CopperMine1-lvl3"
L2 = kit.borrow("coppermine1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    s = 0.25
    d.paving(b, s - 0.22, s + 0.22, -0.99, -0.24, z=0.01)
    d.minecart(b, s, -0.84, L2['PATINA'])
    b.box(0.74, 0.82, -0.94, -0.86, 0, 0.05, 'stone_mid')
    d.pennant(b, 0.78, -0.9, 0.05, h=1.0, color='banner_red', length=0.18)
    return b


kit.run(OBJ, build, source_name="CopperMine1-lvl1", offset=(LVL3_DX, 0, 0),
        collection="Производственные здания")
