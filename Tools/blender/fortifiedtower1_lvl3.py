"""FortifiedTower1-lvl3 -- the Fortified Tower (Укреплённая башня), stone tier, at its last
upgrade. Footprint 2x2 (level 1's), still filling the block so the wall butts into it.

Level 2 (fortifiedtower1_lvl2.build) with gold finials on the four bartizans and on the roof's
pole, and blue banners hung on the other three faces as on the front.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import U_TOP  # noqa: E402
from kit import Facade  # noqa: E402

OBJ = "FortifiedTower1-lvl3"
L2 = kit.borrow("fortifiedtower1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)
    h = 0.86
    body_top = U_TOP + 0.6 + d.STOREY
    ct = body_top + 0.06
    for sx in (-1, 1):
        for sy in (-1, 1):
            d.finial(b, sx * (h - 0.03), sy * (h - 0.03), ct + 0.47, h=0.12)
    d.finial(b, 0, 0, hi[2] + d.STOREY, h=0.14)
    for nm, plane in (('+y', h), ('-x', -h), ('+x', h)):
        d.banner(Facade(b, nm, plane), 0, U_TOP - 0.06, 0.42, w=0.16, color='banner_blue')
    return b


kit.run(OBJ, build, source_name="FortifiedTower1-lvl1", offset=(LVL3_DX, 0, 0),
        collection="Оборонительные здания")
