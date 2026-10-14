"""OrcBrute -- the orcs' heavy: slow, thick, armoured. Facing -y, standing on the origin.

Built as a grunt's frame with a belly and an iron breastplate, a horned iron helm, chained
bracers and great spiked pauldrons, a spiked log maul resting on the right shoulder -- then the
whole figure grown by SCALE, so it stands a head over the grunt and is twice as wide.

Borrows the Flag's box and stands in the orc column. No animation.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402

OBJ = "OrcBrute"
SCALE = 1.25          # 0.58 * 1.25 = 0.725 m


def build(lo, hi):
    b = Builder()
    since = o.mark(b)
    o.legs_and_hips(b, wide=1.15)

    # belly, chest, breastplate
    b.obox((0, -0.02, 0.27), (1, 0, 0), (0, 0.97, 0.24), (0, -0.24, 0.97), 0.13, 0.09, 0.07,
           o.SKIN, bevel=0.03)
    b.obox((0, -0.015, 0.37), (1, 0, 0), (0, 0.97, 0.24), (0, -0.24, 0.97), 0.155, 0.085, 0.055,
           o.SKIN, bevel=0.025)
    b.obox((0, -0.07, 0.34), (1, 0, 0), (0, 0.97, 0.24), (0, -0.24, 0.97), 0.11, 0.03, 0.08,
           o.METAL, bevel=0.015)
    b.obox((0, -0.1, 0.34), (1, 0, 0), (0, 0.97, 0.24), (0, -0.24, 0.97), 0.03, 0.006, 0.07,
           o.RUST)

    # great pauldrons, three spikes each
    for k in (-1, 1):
        x = k * 0.17
        b.ball(x, -0.01, 0.32, 0.09, 0.12, o.METAL, sides=6)
        b.cylinder(x, -0.01, 0.33, 0.35, 0.095, o.RUST, sides=6)
        for dx, dy in ((k, 0), (k * 0.5, -0.8), (k * 0.5, 0.8)):
            o.horn(b, (x + dx * 0.03, -0.01 + dy * 0.03, 0.42), (dx, dy, 0), 0.07, 0.017, up=1.3)

    # head under a horned iron helm with a nasal bar
    hy = -0.07
    o.orc_head(b, hy, z=-0.005)
    b.box(-0.07, 0.07, hy - 0.065, hy + 0.065, 0.47, 0.54, o.METAL, bevel=0.02)
    b.box(-0.012, 0.012, hy - 0.078, hy - 0.06, 0.43, 0.52, o.METAL)
    b.box(-0.075, 0.075, hy - 0.07, hy + 0.07, 0.465, 0.485, o.RUST)
    for k in (-1, 1):
        o.horn(b, (k * 0.065, hy, 0.51), (k, 0.2, 0), 0.11, 0.022, up=1.3, segments=4)

    # thick arms with chained bracers
    for k in (-1, 1):
        x = k * 0.21
        b.obox((x, -0.02, 0.27), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.05, 0.05, 0.07, o.SKIN,
               bevel=0.018)
        b.box(x - 0.048, x + 0.048, -0.075, 0.03, 0.14, 0.22, o.METAL, bevel=0.012)
        b.cylinder(x, -0.022, 0.17, 0.185, 0.058, o.STONE_LIGHT, sides=6)
        b.ball(x, -0.03, 0.095, 0.048, 0.07, o.SKIN, sides=6)

    # the maul: a haft from the right fist up over the shoulder, a banded log head with spikes
    b.rod((0.21, -0.07, 0.1), (0.17, 0.16, 0.56), 0.03, o.TIMBER)
    head = (0.17, 0.17, 0.58)
    b.hcylinder('x', head, 0.09, 0.07, o.TIMBER_LIGHT, sides=7)
    for dx in (-0.065, 0.065):
        b.hcylinder('x', (head[0] + dx, head[1], head[2]), 0.012, 0.075, o.METAL, sides=7)
    for dy, dz in ((0, 1), (0.8, 0.6), (-0.8, 0.6), (1, 0)):
        for dx in (-0.03, 0.03):
            p = (head[0] + dx, head[1] + dy * 0.065, head[2] + dz * 0.065)
            o.tube(b, [p, (p[0], p[1] + dy * 0.045, p[2] + dz * 0.045)], [0.013, 0.0], o.METAL,
                   sides=4)
    o.scale_since(b, since, SCALE)
    return b


o.ensure_collection()
kit.run(OBJ, build, source_name="Flag1-lvl1", offset=(o.COLUMN_X + 2.4, 0, 0),
        collection=o.COLLECTION)
