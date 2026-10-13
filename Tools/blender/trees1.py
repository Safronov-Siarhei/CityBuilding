"""Tree1..Tree5 -- the forest the map is covered with. The user's placeholders are all the same
1x1x2 box; the species set their own heights inside it (HEIGHT) so a forest has a skyline.

Five species so a forest never looks stamped: a young round tree, a tall fir, a broad oak, a
birch and a slim spruce. A map draws some 250 of them at once, so each stays in the budget of
the old tree models (~100-160 triangles): faceted crowns, six-sided trunks, no hidden faces.
"""
import math
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
from kit import Builder  # noqa: E402


HEIGHT = {"Tree1": 1.0, "Tree2": 2.0, "Tree3": 1.75, "Tree4": 1.25, "Tree5": 1.5}


def trunk(b, x, y, z1, r, color='wood_mid', z0=0.0):
    b.cylinder(x, y, z0, z1, r, color, sides=6, rot_deg=0)


def cone(b, x, y, z0, r, apex, color, sides=7, rot=0.0):
    """A tier of a conifer: a cone with its underside closed (seen from a low camera)."""
    ring = [b.bm.verts.new((x + r * math.cos(math.radians(rot + 360 * i / sides)),
                            y + r * math.sin(math.radians(rot + 360 * i / sides)), z0))
            for i in range(sides)]
    top = b.bm.verts.new((x, y, apex))
    for i in range(sides):
        b._face([ring[i], ring[(i + 1) % sides], top], color)
    b._face(list(reversed(ring)), color)


def young(lo, hi, h=HEIGHT["Tree1"]):          # Tree1, 1.0: a young round tree
    b = Builder()
    trunk(b, 0, 0, 0.36, 0.045)
    b.ball(0, 0, 0.3, 0.3, h - 0.3, 'leaf', sides=7, top='grass_dark')
    return b


def fir(lo, hi, h=HEIGHT["Tree2"]):            # Tree2, 2.0: a tall fir in four tiers
    b = Builder()
    trunk(b, 0, 0, 0.32, 0.06, 'wood_dark')
    tiers = ((0.22, 0.36, 0.62), (0.52, 0.3, 0.86), (0.84, 0.23, 0.9), (1.14, 0.16, 1.0))
    for i, (z, r, k) in enumerate(tiers):
        apex = z + (h - z) * k if i == 3 else z + (h - 0.22) * 0.3 + 0.08
        cone(b, 0, 0, z, r, min(apex, h), 'leaf' if i % 2 == 0 else 'grass_dark', rot=i * 25)
    return b


def oak(lo, hi, h=HEIGHT["Tree3"]):            # Tree3, 1.75: a broad oak, one wide crown with two side lobes
    b = Builder()
    trunk(b, 0, 0, 0.62, 0.075)
    b.rod((0, 0, 0.45), (0.17, 0.05, 0.8), 0.06, 'wood_mid')
    b.rod((0, 0, 0.5), (-0.15, -0.06, 0.82), 0.055, 'wood_mid')
    b.ball(0.0, 0.03, 0.6, 0.4, h - 0.6, 'leaf', sides=8, top='grass_dark')
    b.ball(0.22, -0.14, 0.7, 0.2, 0.46, 'leaf', sides=6, top='grass_dark')
    b.ball(-0.24, -0.06, 0.78, 0.17, 0.4, 'leaf', sides=6, top='grass_dark')
    return b


def birch(lo, hi, h=HEIGHT["Tree4"]):          # Tree4, 1.25: a birch, white trunk with dark marks, light crown
    b = Builder()
    trunk(b, 0, 0, 0.78, 0.035, 'plaster_white')
    for z, a in ((0.22, 0), (0.45, 60), (0.66, 20)):
        b.zbox(0.0, 0.0, z, z + 0.03, 0.037, 0.02, a, 'iron')
    b.rod((0, 0, 0.55), (0.12, 0.03, 0.78), 0.025, 'plaster_white')
    b.ball(0.0, 0.0, 0.5, 0.24, h - 0.5, 'roof_green', sides=6, top='grass')
    return b


def spruce(lo, hi, h=HEIGHT["Tree5"]):         # Tree5, 1.5: a slim spruce, a stack of narrow tiers
    b = Builder()
    trunk(b, 0, 0, 0.26, 0.05, 'wood_dark')
    tiers = ((0.18, 0.27), (0.48, 0.22), (0.78, 0.16), (1.06, 0.11))
    for i, (z, r) in enumerate(tiers):
        apex = h if i == 3 else z + 0.5
        cone(b, 0, 0, z, r, apex, 'grass_dark' if i % 2 == 0 else 'leaf', sides=6, rot=i * 30)
    return b


def main():
    for name, build in (("Tree1", young), ("Tree2", fir), ("Tree3", oak), ("Tree4", birch),
                        ("Tree5", spruce)):
        kit.run(name, build, save=False)
    bpy.ops.wm.save_mainfile()


main()
