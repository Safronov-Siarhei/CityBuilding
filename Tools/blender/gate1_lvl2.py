"""Gate1-lvl2 -- the Gate (Ворота), wooden tier, after its first upgrade. Footprint 1x2 (level
1's); the palisade still comes in from both ends exactly as at level 1.

The log posts become stone piers with blue caps and a lantern on each face. Level 1, for
reference:

Gate1-lvl1 -- the Gate (Ворота), wooden tier. Footprint 1x2: the palisade runs along Y through
it, people pass across it along X.

The palisade comes in from both ends on the shared log grid and stops at two tall log posts;
between them hang two plank leaves with iron bands, under a crossbeam carrying a little blue
roof and a banner.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "Gate1-lvl2"
LVL2_DX = 1.6   # level 2 stands this far +X of level 1


def build(lo, hi):
    b = Builder()
    d.palisade_arm(b, 0, 1.0, 0, -1, 0.0, 0.5)
    d.palisade_arm(b, 0, -1.0, 0, 1, 0.0, 0.5)

    post_h = d.PALISADE_H + 0.42
    for py in (-0.44, 0.44):
        b.box(-0.13, 0.13, py - 0.09, py + 0.09, 0, post_h, 'stone_light', bevel=0.012)
        b.box(-0.15, 0.15, py - 0.11, py + 0.11, post_h - 0.04, post_h + 0.03, 'stone_mid', bevel=0.008)
        b.pyramid(0, py, post_h + 0.03, 0.15, 0.11, post_h + 0.2, 'roof_blue')
        for sx in (-1, 1):
            d.wall_lantern(Facade(b, '-x' if sx < 0 else '+x', sx * 0.13), py, 0.5)
    # the two leaves, closed, with their bands
    for side in (-1, 1):
        f = Facade(b, '-x' if side < 0 else '+x', side * 0.03)
        for s0, s1 in ((-0.37, -0.01), (0.01, 0.37)):
            f.rect(s0, s1, 0.02, d.PALISADE_H - 0.06, 0.03, 'wood_mid')
            for z in (0.18, 0.48):
                f.rect(s0, s1, z, z + 0.035, 0.04, 'iron')
    b.box(-0.03, 0.03, -0.37, 0.37, 0.02, d.PALISADE_H - 0.06, 'wood_mid')
    # crossbeam with a little roof and a banner
    beam_z = post_h - 0.14
    b.box(-0.1, 0.1, -0.52, 0.52, beam_z, beam_z + 0.08, 'wood_dark')
    d.roof(b, 0, 0, 'y', 0.56, 0.16, beam_z + 0.08, overhang=0.04, t=0.03, rib_h=0.012,
           cap_w=0.06, cap_h=0.03, cap_lift=0.015, **d.DEFENCE)
    d.banner(Facade(b, '-x', -0.1), 0.0, beam_z, 0.3, w=0.14, color='banner_blue')
    d.banner(Facade(b, '+x', 0.1), 0.0, beam_z, 0.3, w=0.14, color='banner_blue')
    return b


kit.run(OBJ, build, source_name="Gate1-lvl1", offset=(LVL2_DX, 0, 0),
        collection="Оборонительные здания")
