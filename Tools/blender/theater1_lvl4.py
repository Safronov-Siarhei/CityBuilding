"""Theater1-lvl4 -- the Theatre (Театр) at level 4.

Footprint 4x4 (level 1's). Level 3 (theater1_lvl3.build) with the stage's roof and the outer
slope of the ring roof re-laid in glazed tiles, and gilded crests along the stage's ridge and
all round the ring's ridge. Level 4's mark across the set is the glazed roof: a lattice of the
category's dark shade and gold diamonds over the tiles. Models only -- the game stops at level 3
for now.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402,F401
import kit  # noqa: E402
from details import G_TOP, L_TOP, STOREY, U_TOP, WALL_H  # noqa: E402,F401

OBJ = "Theater1-lvl4"
L3 = kit.borrow("theater1_lvl3.py")
LVL4_DX = L3['LVL3_DX'] * 1.5


def build(lo, hi):
    b = L3['build'](lo, hi)
    d.glazed(b, 0, 0.725, 'x', 0.62, 0.375, U_TOP + 0.1, 'roof_purple_dark')
    oc, T_TOP = L3['L2']['octagon'], L3['L2']['T_TOP']
    r_mid = (1.78 + 1.12) / 2
    d.glazed_ring(b, oc(0.0, 0.05, 1.78 + 0.12), oc(0.0, 0.05, r_mid), T_TOP, T_TOP + 0.36,
                  'roof_purple_dark')
    d.ring_crest(b, oc(0.0, 0.05, r_mid), T_TOP + 0.39)
    return b


kit.run(OBJ, build, source_name="Theater1-lvl1", offset=(LVL4_DX, 0, 0),
        collection="Развлечения")
