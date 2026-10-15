"""Hovel1-lvl4 -- the Hovel (Лачуга) at level 4. Footprint 2x2 (level 1's). Front -Y.

Level 3 (hovel1_lvl3.build) with its roof re-laid in glazed tiles -- a lattice of dark and gold
diamonds over the terracotta -- a gilded crest along the ridge, and a small glazed lantern on
the ridge between the weathervane and the chimney.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import G_TOP  # noqa: E402

OBJ = "Hovel1-lvl4"
L3 = kit.borrow("hovel1_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5


def build(lo, hi):
    b = L3['build'](lo, hi)
    x1, y0, y1 = 0.8, -0.5, 0.75
    yc, hs = (y0 + y1) / 2, (y1 - y0) / 2
    d.glazed(b, 0, yc, 'x', x1 + 0.1, hs, G_TOP, 'roof_orange_dark')
    d.cupola(b, 0.05, yc, G_TOP + hs * d.PITCH + 0.04, r=0.08, h=0.15, wall='wood_light')
    return b


kit.run(OBJ, build, source_name="Hovel1-lvl1", offset=(LVL4_DX, 0, 0), collection="Город")
