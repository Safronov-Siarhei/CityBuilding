"""Smelter1-lvl3 -- the Smelter (Плавильня) at its last upgrade. Footprint 2x3 (level 1's).
Front is -Y.

Level 2 (smelter1_lvl2.build) with the furnace stack raised again (STACK_UP 0.4 -> 0.7), gold
finials on the hall's ridge ends, a second pallet of bars -- all gold -- and a guild pennant
on a tall pole.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import G_TOP  # noqa: E402

OBJ = "Smelter1-lvl3"
L2 = kit.borrow("smelter1_lvl2.py")
L2['STACK_UP'] = 0.7      # level 2's build reads it as a global
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    x0, x1, y0, y1 = -0.85, 0.85, -0.05, 1.38
    zr = G_TOP + (x1 - x0) / 2 * d.PITCH
    for ey in (y0 - 0.04, y1 + 0.04):
        d.finial(b, 0, ey, zr + 0.06)
    px, py = 0.62, -1.3
    b.box(px - 0.2, px + 0.2, py - 0.12, py + 0.12, 0, 0.04, 'wood_mid')
    L2['ingots'](b, px - 0.07, py, 'gold')
    L2['ingots'](b, px + 0.1, py, 'gold', n=2)
    b.box(-0.9, -0.82, -1.4, -1.32, 0, 0.05, 'stone_mid')
    d.pennant(b, -0.86, -1.36, 0.05, h=1.1, color='banner_red', length=0.2)
    return b


kit.run(OBJ, build, source_name="Smelter1-lvl1", offset=(LVL3_DX, 0, 0),
        collection="Производственные здания")
