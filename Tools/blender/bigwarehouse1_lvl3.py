"""BigWarehouse1-lvl3 -- the Big Warehouse (Большой склад) at its last upgrade. Footprint 3x3
(level 1's). Front is -Y.

Level 2 (bigwarehouse1_lvl2.build) with a louvred vent turret on the ridge, gold finials on the
ridge ends, the yard paved, and a timber jib crane lifting a crate in the yard.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import JETTY as J, U_TOP  # noqa: E402

OBJ = "BigWarehouse1-lvl3"
L2 = kit.borrow("bigwarehouse1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def crane(b, x, y, h=1.05, reach=0.5):
    """A timber jib crane: a mast on a cross foot, a jib slanting out along +x on a brace, a rope
    from its tip and a crate on the hook."""
    for dx, dy in ((0.14, 0), (0, 0.14)):
        b.box(x - dx - 0.03, x + dx + 0.03, y - dy - 0.03, y + dy + 0.03, 0, 0.05, 'wood_dark')
    b.box(x - 0.035, x + 0.035, y - 0.035, y + 0.035, 0, h, 'wood_mid')
    tip = (x + reach, y, h + 0.12)
    b.rod((x, y, h - 0.05), tip, 0.04, 'wood_mid')
    b.rod((x, y, h - 0.35), (x + reach * 0.6, y, h + 0.02), 0.03, 'wood_dark')
    b.box(tip[0] - 0.005, tip[0] + 0.005, y - 0.005, y + 0.005, 0.4, tip[2], 'burlap')
    d.crate(b, tip[0], y, 0.14, 15, z0=0.26)
    b.hcylinder('y', (x + 0.07, y, 0.45), 0.06, 0.05, 'wood_light', sides=6)   # winch


def build(lo, hi):
    b = L2['build'](lo, hi)
    x0, x1, y0, y1 = -1.3, 1.3, 0.05, 1.3
    ux1, uy0, uy1 = x1 + J, y0 - J, y1 + J
    yc, hs = (uy0 + uy1) / 2, (uy1 - uy0) / 2
    zr = U_TOP + hs * d.PITCH
    d.louvre(b, -0.15, yc, zr - 0.04)
    for ex in (-ux1 - 0.09, ux1 + 0.09):
        d.finial(b, ex, yc, zr + 0.06)
    d.paving(b, -0.95, 0.95, -1.45, y0)
    crane(b, -0.62, -0.62)
    return b


kit.run(OBJ, build, source_name="BigWarehouse1-lvl1", offset=(LVL3_DX, 0, 0),
        collection="Склады")
