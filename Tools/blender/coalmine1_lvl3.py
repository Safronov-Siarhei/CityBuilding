"""CoalMine1-lvl3 -- the Coal Mine (Угольная шахта) at its last upgrade. Footprint 2x2 (level
1's). Front is -Y.

Level 2 (coalmine1_lvl2.build) working at full tilt: a gold finial over the headframe's
pennant, a second cart of coal on the track, the track's yard paved, and the winding shed's
roof crowned with a louvred vent.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402

OBJ = "CoalMine1-lvl3"
L2 = kit.borrow("coalmine1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    sx, sy, top = -0.38, -0.12, 1.32
    d.finial(b, sx, sy, top + 0.42, h=0.12)
    d.paving(b, sx - 0.2, sx + 0.2, -0.99, sy - 0.26, z=0.01)
    d.minecart(b, sx, -0.84, L2['ORE'])
    d.louvre(b, 0.4, -0.22, 0.6, half=0.08, h=0.2, roof='roof_slate')
    return b


kit.run(OBJ, build, source_name="CoalMine1-lvl1", offset=(LVL3_DX, 0, 0),
        collection="Производственные здания")
