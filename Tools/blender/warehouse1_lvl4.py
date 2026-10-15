"""Warehouse1-lvl4 -- the Warehouse (Склад) at level 4. Footprint 2x2 (level 1's). Front -Y.

Level 3 (warehouse1_lvl3.build) with its roof re-laid in glazed tiles -- a lattice of dark red
and gold diamonds over the red -- a gilded crest along the ridge, and a long red pennant flying
from the cupola's finial.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import U_TOP  # noqa: E402

OBJ = "Warehouse1-lvl4"
L3 = kit.borrow("warehouse1_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5


def build(lo, hi):
    b = L3['build'](lo, hi)
    x1, y0, y1 = 0.78, -0.08, 0.8
    yc, hs = (y0 + y1) / 2, (y1 - y0) / 2
    zr = U_TOP + hs * d.PITCH
    d.glazed(b, 0, yc, 'x', x1 + 0.09, hs, U_TOP, 'roof_red_dark')
    d.pennant(b, 0, yc, zr + 0.26 + 0.2 + 0.14, h=0.3, color='banner_red', length=0.24)
    return b


kit.run(OBJ, build, source_name="Warehouse1-lvl1", offset=(LVL4_DX, 0, 0), collection="Склады")
