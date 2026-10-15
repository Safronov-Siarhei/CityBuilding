"""Windmill1-lvl3 -- the Windmill (Ветряк) at its last upgrade. Footprint 2x2 (level 1's).
Front is -Y.

Level 2 (windmill1_lvl2.build) with a gold finial and a long pennant flying from the top of the
thatched cap, and the yard before the porch paved where the sacks wait.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402

OBJ = "Windmill1-lvl3"
L2 = kit.borrow("windmill1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    cx, cy, z1 = 0.0, 0.22, 1.85
    d.pennant(b, cx, cy, z1 + 0.72, h=0.36, color='banner_red', length=0.26)
    d.paving(b, -0.9, 0.9, -0.98, cy - 0.78, z=0.01)
    return b


kit.run(OBJ, build, source_name="Windmill1-lvl1", offset=(LVL3_DX, 0, 0),
        collection="Производство еды")
