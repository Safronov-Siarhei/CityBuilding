"""Castle1-lvl3 -- the Town Hall (Ратуша) at its last upgrade. Footprint 5x5 (level 1's).
Front is -Y.

Level 2 (castle1_lvl2.build) crowned: two round towers under tall blue cones flank the
gatehouse, red-and-gold banners hang on the outer wall, gold finials stand on the hall's ridge
ends, the courtyard in front of the hall is paved and a gilded statue faces the fountain across
it. Stands next to level 2 on level 1's placeholder box.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import U_TOP, WALL_H  # noqa: E402
from kit import Facade  # noqa: E402

OBJ = "Castle1-lvl3"
L2 = kit.borrow("castle1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)

    # the paved forecourt between the gate and the hall, and a gilded statue on it
    d.paving(b, -0.75, 0.75, -2.0, -0.62, z=0.034)
    d.statue(b, -1.1, -1.0, 0.02, scale=1.5)

    # two round towers flanking the gatehouse
    g_top = WALL_H + 0.3
    for sx in (-1, 1):
        apex = d.turret(b, sx * 0.8, -2.2, 0, g_top + 0.36, 0.19, roof='roof_blue')
        d.pennant(b, sx * 0.8, -2.2, apex + 0.08, h=0.2, color='banner_blue', length=0.16)

    # banners on the outer face of the curtain wall
    f = Facade(b, '-y', -2.4)
    for s in (-1.35, 1.35):
        d.banner(f, s, WALL_H - 0.04, 0.34)

    # gold finials on the hall's ridge ends
    J = d.JETTY
    uy0, uy1 = 0.15 - J, 1.85 + J
    zr = U_TOP + (uy1 - uy0) / 2 * d.PITCH
    for ex in (-1.75 - J - 0.1, 1.75 + J + 0.1):
        d.finial(b, ex, (uy0 + uy1) / 2, zr + 0.06, h=0.2)
    return b


kit.run(OBJ, build, source_name="Castle1-lvl1", offset=(LVL3_DX, 0, 0), collection="Город")
