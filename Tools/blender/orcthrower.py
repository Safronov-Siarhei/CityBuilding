"""OrcThrower -- the orcs' ranged raider: a javelin thrower, facing -y, standing on the origin.

A leaner grunt in a hide hood and mantle, one iron pauldron, a quiver of javelins on the back,
the right arm cocked over the shoulder with a javelin ready to throw and the left arm reaching
forward to aim. Slightly shorter than the grunt so the two read apart in a squad.

Borrows the Flag's box and stands in the orc column. No animation.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402

OBJ = "OrcThrower"


def javelin(b, p0, p1):
    o.tube(b, [p0, p1], [0.009, 0.009], o.TIMBER_LIGHT, sides=4, tip=False)
    from mathutils import Vector
    p0, p1 = Vector(p0), Vector(p1)
    d = (p1 - p0).normalized()
    o.tube(b, [p1, p1 + d * 0.05], [0.016, 0.0], o.METAL, sides=4)
    o.tube(b, [p0 - d * 0.0, p0 + d * 0.04], [0.015, 0.0], o.RUST, sides=3, tip=False)


def build(lo, hi):
    b = Builder()
    o.legs_and_hips(b, wide=0.92)

    # a leaner torso under a hide mantle
    b.obox((0, -0.005, 0.3), (1, 0, 0), (0, 0.97, 0.24), (0, -0.24, 0.97), 0.1, 0.068, 0.08,
           o.SKIN, bevel=0.022)
    b.obox((0, 0.0, 0.385), (1, 0, 0), (0, 0.97, 0.24), (0, -0.24, 0.97), 0.125, 0.085, 0.035,
           o.HIDE, bevel=0.02)
    b.rod((0.1, -0.09, 0.4), (-0.09, -0.075, 0.24), 0.024, o.TIMBER)
    # one iron pauldron, on the aiming arm
    b.ball(-0.14, -0.02, 0.33, 0.065, 0.09, o.METAL, sides=6)
    o.horn(b, (-0.14, -0.02, 0.41), (-1, 0, 0), 0.06, 0.014, up=1.4)

    # head under a hood, the hood's point hanging back
    hy = -0.055
    o.orc_head(b, hy, z=-0.02)
    b.box(-0.072, 0.072, hy - 0.045, hy + 0.07, 0.47, 0.525, o.HIDE, bevel=0.02)
    for k in (-1, 1):
        b.box(k * 0.068 - 0.012, k * 0.068 + 0.012, hy - 0.04, hy + 0.07, 0.4, 0.5, o.HIDE)
    o.tube(b, [(0, hy + 0.06, 0.5), (0, hy + 0.12, 0.47), (0, hy + 0.15, 0.4)],
           [0.04, 0.025, 0], o.HIDE, sides=5)

    # quiver on the back, javelins standing out of it
    b.obox((-0.04, 0.085, 0.31), (0.94, 0, 0.34), (0, 1, 0), (-0.34, 0, 0.94), 0.035, 0.03, 0.11,
           o.RUST, bevel=0.01)
    for dx, dz in ((-0.07, 0.48), (-0.05, 0.5), (-0.1, 0.46)):
        javelin(b, (dx + 0.08, 0.09, 0.3), (dx, 0.09, dz))

    # left arm reaching forward to aim, right arm cocked back over the shoulder
    b.obox((-0.17, -0.07, 0.31), (1, 0, 0), (0, 0.6, 0.8), (0, -0.8, 0.6), 0.036, 0.075, 0.038,
           o.SKIN, bevel=0.014)
    b.box(-0.205, -0.135, -0.18, -0.12, 0.3, 0.36, o.METAL, bevel=0.01)
    b.ball(-0.17, -0.2, 0.31, 0.036, 0.055, o.SKIN, sides=6)
    b.obox((0.155, 0.01, 0.42), (1, 0, 0), (0, 0.94, -0.34), (0, 0.34, 0.94), 0.036, 0.038, 0.07,
           o.SKIN, bevel=0.014)
    b.ball(0.16, 0.04, 0.48, 0.036, 0.055, o.SKIN, sides=6)
    javelin(b, (0.16, 0.2, 0.47), (0.16, -0.24, 0.56))
    return b


o.ensure_collection()
kit.run(OBJ, build, source_name="Flag1-lvl1", offset=(o.COLUMN_X + 1.2, 0, 0),
        collection=o.COLLECTION)
