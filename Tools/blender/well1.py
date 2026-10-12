"""Well1-lvl1 -- the Well (Колодец). Footprint 1x1, placeholder height 0.62, roof allowed above it (0.86).

An octagonal stone shaft on a flagstone apron, two posts carrying a little ribbed terracotta
roof, a windlass with a rope and a bucket, a crank on the side, a spare bucket on the ground.

    blender --background Buildings-Size1-Models.blend --python well1.py
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
import details as d  # noqa: E402
from kit import Builder  # noqa: E402

OBJ = "Well1-lvl1"


def bucket(b, x, y, z0):
    b.cylinder(x, y, z0, z0 + 0.075, 0.05, 'wood_light')
    b.cylinder(x, y, z0 + 0.03, z0 + 0.045, 0.054, 'iron')


def build(lo, hi):
    top = 0.86  # the user allowed the roof above the placeholder's 0.62
    b = Builder()

    # apron of flagstones, then the shaft in alternating stone blocks, then the coping
    b.cylinder(0, 0, 0, 0.03, 0.43, 'stone_mid', bevel=0.006)
    b.ring(0, 0, 0.03, 0.14, 0.21, 0.29, ['stone_light', 'stone_mid'])
    b.ring(0, 0, 0.14, 0.25, 0.21, 0.29, ['stone_mid', 'stone_light'])
    b.ring(0, 0, 0.25, 0.29, 0.19, 0.31, ['stone_light'], rot_deg=0, bevel=0.006)
    b.cylinder(0, 0, 0.03, 0.17, 0.215, 'water')

    # posts on the x axis, outside the coping; the roof ridge runs between them
    ridge = top - 0.06
    for sx in (-1, 1):
        b.box(sx * 0.36 - 0.03, sx * 0.36 + 0.03, -0.03, 0.03, 0.03, ridge, 'wood_mid',
              bevel=0.006)
        b.box(sx * 0.36 - 0.045, sx * 0.36 + 0.045, -0.045, 0.045, 0.03, 0.07, 'stone_dark')
    d.roof(b, 0, 0, 'x', 0.43, 0.16, ridge - 0.16, overhang=0.04, t=0.03)

    # windlass: axle with a coil of rope, the rope, the bucket at the top of the shaft
    b.hcylinder('x', (0, 0, 0.47), 0.33, 0.022, 'wood_dark')
    b.hcylinder('x', (0, 0, 0.47), 0.07, 0.04, 'sand')
    b.box(-0.006, 0.006, -0.006, 0.006, 0.31, 0.44, 'sand')
    bucket(b, 0, 0, 0.24)

    # crank on the +x post
    b.box(0.39, 0.41, -0.012, 0.012, 0.4, 0.48, 'iron')
    b.box(0.39, 0.45, -0.012, 0.012, 0.39, 0.415, 'wood_dark')

    # a spare bucket on the apron
    bucket(b, -0.27, -0.29, 0.03)
    return b


kit.run(OBJ, build)
