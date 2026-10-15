"""Quarry1-lvl3 -- the Quarry (Каменоломня) at its last upgrade. Footprint 2x2 (level 1's).
Front is -Y.

Level 2 (quarry1_lvl2.build) showing off its craft: a finished obelisk with a gilded tip stands
in the front corner, the crane's mast wears a gold finial, a guild pennant flies on a tall pole,
and the ground where the sledge is loaded is paved with the quarry's own stone.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402

OBJ = "Quarry1-lvl3"
L2 = kit.borrow("quarry1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def obelisk(b, x, y, h=0.78):
    b.box(x - 0.1, x + 0.1, y - 0.1, y + 0.1, 0, 0.06, 'stone_mid', bevel=0.006)
    b.frustum(x, y, 0.06, h, 0.065, 0.04, 'stone_light', sides=4, rot_deg=45)
    b.pyramid(x, y, h, 0.04, 0.04, h + 0.08, 'gold')


def build(lo, hi):
    b = L2['build'](lo, hi)
    d.paving(b, -0.12, 0.45, -0.99, -0.68, z=0.012)
    obelisk(b, -0.84, -0.88)
    d.finial(b, 0.55, 0.0, 1.25, h=0.14)
    b.box(0.7, 0.78, -0.94, -0.86, 0, 0.05, 'stone_mid')
    d.pennant(b, 0.74, -0.9, 0.05, h=1.0, color='banner_red', length=0.2)
    return b


kit.run(OBJ, build, source_name="Quarry1-lvl1", offset=(LVL3_DX, 0, 0),
        collection="Производственные здания")
