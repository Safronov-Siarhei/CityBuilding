"""Gate1-lvl1 -- the Gate (Ворота), wooden tier. Footprint 1x1 (the user, 2026-10-21: one cell, to
stand on a road): the palisade line runs along Y through it, people pass across it along X.

The fence pieces either side reach the cell's edge; the gate fills the cell with two tall log posts;
between them hang two plank leaves with iron bands, under a crossbeam carrying a little blue
roof and a banner.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "Gate1-lvl1"
POST_Y = 0.38    # the posts' centres; their outer faces stay inside the cell
LEAF = 0.30      # each leaf's outer edge
BEAM = 0.47      # the crossbeam's half-length
ROOF_LEN = 0.44  # the little roof's half-length (plus its 0.04 overhang)


def build(lo, hi):
    b = Builder()
    post_h = d.PALISADE_H + 0.42
    for py in (-POST_Y, POST_Y):
        b.box(-0.09, 0.09, py - 0.07, py + 0.07, 0, post_h, 'wood_dark', bevel=0.01)
        b.pyramid(0, py, post_h, 0.1, 0.08, post_h + 0.1, 'wood_dark')
    # the two leaves, closed, with their bands
    for side in (-1, 1):
        f = Facade(b, '-x' if side < 0 else '+x', side * 0.03)
        for s0, s1 in ((-LEAF, -0.01), (0.01, LEAF)):
            f.rect(s0, s1, 0.02, d.PALISADE_H - 0.06, 0.03, 'wood_mid')
            for z in (0.18, 0.48):
                f.rect(s0, s1, z, z + 0.035, 0.04, 'iron')
    b.box(-0.03, 0.03, -LEAF, LEAF, 0.02, d.PALISADE_H - 0.06, 'wood_mid')
    # crossbeam with a little roof and a banner
    beam_z = post_h - 0.14
    b.box(-0.1, 0.1, -BEAM, BEAM, beam_z, beam_z + 0.08, 'wood_dark')
    d.roof(b, 0, 0, 'y', ROOF_LEN, 0.16, beam_z + 0.08, overhang=0.04, t=0.03, rib_h=0.012,
           cap_w=0.06, cap_h=0.03, cap_lift=0.015, **d.DEFENCE)
    d.banner(Facade(b, '-x', -0.1), 0.0, beam_z, 0.3, w=0.14, color='banner_blue')
    d.banner(Facade(b, '+x', 0.1), 0.0, beam_z, 0.3, w=0.14, color='banner_blue')
    return b


kit.run(OBJ, build)
