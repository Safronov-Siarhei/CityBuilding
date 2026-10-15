"""FortifiedGate1-lvl3 -- the Fortified Gate (Укреплённые ворота), stone tier, at its last
upgrade. Footprint 2x2 (level 1's); the curtain wall comes in exactly as at level 1.

Level 2 (fortifiedgate1_lvl2.build) with a stone watch room risen between the four turrets under
a tall blue roof and a gold finial, and gold finials on the turrets' pennant poles.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import WALL_H  # noqa: E402
from kit import Facade  # noqa: E402

OBJ = "FortifiedGate1-lvl3"
L2 = kit.borrow("fortifiedgate1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    gx, gy = 0.42, 0.62
    g_top = WALL_H + 0.36
    t_top = g_top + 0.4
    z0, z1 = g_top + 0.04, g_top + 0.42
    b.box(-0.17, 0.17, -0.26, 0.26, z0, z1, 'stone_light', bevel=0.012)
    b.box(-0.2, 0.2, -0.29, 0.29, z1 - 0.04, z1, 'stone_mid')
    for side in (-1, 1):
        f = Facade(b, '-x' if side < 0 else '+x', side * 0.17)
        d.slit(f, 0, z0 + 0.1, z1 - 0.1)
    apex = z1 + 0.42
    b.pyramid(0, 0, z1, 0.22, 0.31, apex, 'roof_blue')
    d.finial(b, 0, 0, apex - 0.02, h=0.16)
    for sx in (-1, 1):
        for sy in (-1, 1):
            d.finial(b, sx * (gx - 0.1), sy * (gy - 0.1), t_top + 0.52, h=0.08)
    return b


kit.run(OBJ, build, source_name="FortifiedGate1-lvl1", offset=(LVL3_DX, 0, 0),
        collection="Оборонительные здания")
