"""Cottage1-lvl4 -- the Cottage (Коттедж) at level 4. Footprint 2x2 (level 1's). Front -Y.

Level 3 (cottage1_lvl3.build) with its roof re-laid in glazed tiles -- a lattice of dark and
gold diamonds over the terracotta -- a gilded crest along the ridge, and a second turret on the
back corner answering the first.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, JETTY, U_TOP  # noqa: E402

OBJ = "Cottage1-lvl4"
L3 = kit.borrow("cottage1_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5


def build(lo, hi):
    b = L3['build'](lo, hi)
    x0, x1, y0, y1 = -0.76, 0.76, -0.48, 0.72
    uy0, uy1 = y0 - JETTY, y1 + JETTY
    yc, hs = (y0 + y1) / 2, (uy1 - uy0) / 2
    d.glazed(b, 0, yc, 'x', x1 + JETTY + 0.09, hs, U_TOP, 'roof_orange_dark')
    d.turret(b, -0.74, 0.7, FOOTING, U_TOP + 0.3, 0.18, face='-x')
    return b


kit.run(OBJ, build, source_name="Cottage1-lvl1", offset=(LVL4_DX, 0, 0), collection="Город")
