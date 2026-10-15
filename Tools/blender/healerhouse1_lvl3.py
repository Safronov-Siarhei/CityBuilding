"""HealerHouse1-lvl3 -- the Healer's House (Дом лекаря) at its last upgrade. Footprint 2x4
(level 1's). Front -Y.

Level 2 (healerhouse1_lvl2.build) crowned: a glazed lantern on the ridge lights the wards,
gold finials stand on both gable ends, the garden path is paved and enters between two stone
piers with gold balls.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import G_TOP  # noqa: E402

OBJ = "HealerHouse1-lvl3"
L2 = kit.borrow("healerhouse1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    x0, x1, y0, y1 = -0.72, 0.72, -0.3, 1.86
    zr = G_TOP + (x1 - x0) / 2 * d.PITCH
    d.cupola(b, 0, 0.78, zr + 0.04, r=0.12, h=0.2, wall='plaster_white')
    for ey in (y0 - 0.09, y1 + 0.09):
        d.finial(b, 0, ey, zr + 0.06)
    gy0 = -1.93
    d.paving(b, -0.14, 0.14, gy0, y0 - 0.14, z=0.016)
    for sx in (-1, 1):
        d.pier(b, sx * 0.15, gy0 - 0.01, h=0.26, w=0.045)
    return b


kit.run(OBJ, build, source_name="HealerHouse1-lvl1", offset=(LVL3_DX, 0, 0), collection="Город")
