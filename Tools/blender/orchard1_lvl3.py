"""Orchard1-lvl3 -- the Orchard (Фруктовый сад) at its last upgrade. Footprint 3x3 (level 1's).
Front is -Y.

Level 2 (orchard1_lvl2.build) with stone gate piers carrying gold balls in the gap of the fence,
a paved path in from the gate, gold finials on the picker's hut and a weathervane on its ridge.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import G_TOP  # noqa: E402

OBJ = "Orchard1-lvl3"
L2 = kit.borrow("orchard1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    for sx in (-1, 1):
        d.pier(b, sx * 0.3, -1.43, h=0.3, w=0.05)
    d.paving(b, -0.25, 0.25, -1.42, -0.95, z=0.012)
    x0, x1, y0, y1 = -1.3, -0.62, -1.2, -0.62
    yc, hs = (y0 + y1) / 2, (y1 - y0) / 2
    zr = G_TOP + hs * d.PITCH
    for ex in (x0 - 0.07, x1 + 0.07):
        d.finial(b, ex, yc, zr + 0.06, h=0.12)
    d.weathervane(b, (x0 + x1) / 2, yc, zr + 0.07, h=0.2)
    return b


kit.run(OBJ, build, source_name="Orchard1-lvl1", offset=(LVL3_DX, 0, 0),
        collection="Производство еды")
