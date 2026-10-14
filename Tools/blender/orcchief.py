"""OrcChief -- the warchief who holds the final fortress. Facing -y, standing on the origin.

A grunt's frame grown by SCALE, wearing what the others do not: a great fur mantle over the
shoulders with a rust cloak falling behind, gold-trimmed iron pauldrons, a crown of bone horns
over a war-painted brow, a gold-ringed topknot, and a double-bladed greataxe planted upright
beside him. A trophy pole with a small rust banner rises from his back, so he can be picked out
of a melee at a glance.

Borrows the Flag's box and stands in the orc column. No animation.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402

OBJ = "OrcChief"
SCALE = 1.15          # 0.58 * 1.15 = 0.67 m to the crown of the head
GOLD = 'gold'


def build(lo, hi):
    b = Builder()
    since = o.mark(b)
    o.legs_and_hips(b, wide=1.1)

    # torso, a gold-buckled war harness
    b.obox((0, -0.005, 0.3), (1, 0, 0), (0, 0.97, 0.24), (0, -0.24, 0.97), 0.125, 0.078, 0.08,
           o.SKIN, bevel=0.025)
    b.obox((0, -0.02, 0.37), (1, 0, 0), (0, 0.97, 0.24), (0, -0.24, 0.97), 0.145, 0.082, 0.05,
           o.SKIN, bevel=0.025)
    for k in (-1, 1):
        b.rod((k * 0.12, -0.1, 0.41), (-k * 0.08, -0.088, 0.24), 0.026, o.TIMBER)
    b.box(-0.03, 0.03, -0.12, -0.1, 0.3, 0.36, GOLD)

    # fur mantle over the shoulders, rust cloak behind
    b.obox((0, 0.01, 0.415), (1, 0, 0), (0, 0.97, 0.24), (0, -0.24, 0.97), 0.17, 0.1, 0.035,
           o.HIDE, bevel=0.025)
    b.prism([(-0.15, 0.4), (0.15, 0.4), (0.18, 0.05), (0.05, 0.1), (-0.05, 0.06), (-0.18, 0.08)],
            (0, 0.09, 0), (1, 0, 0), (0, 0, 1), 0.016, o.RUST, back_cap=True)
    for k in (-1, 1):
        x = k * 0.165
        b.ball(x, -0.01, 0.34, 0.075, 0.1, o.METAL, sides=6)
        b.cylinder(x, -0.01, 0.35, 0.365, 0.08, GOLD, sides=6)
        for dx, dy in ((k, 0), (k * 0.4, -0.9)):
            o.horn(b, (x + dx * 0.03, -0.01 + dy * 0.03, 0.43), (dx, dy, 0), 0.07, 0.016, up=1.4)

    # head: war paint on the brow, a crown of bone horns, gold-ringed topknot
    hy = -0.06
    o.orc_head(b, hy, z=0.0)
    b.box(-0.064, 0.064, hy - 0.063, hy - 0.04, 0.495, 0.51, o.RUST)
    b.cylinder(0, hy, 0.505, 0.53, 0.075, o.TIMBER, sides=8)
    for i, (dx, dy, ln) in enumerate(((-1, 0.0, 0.12), (1, 0.0, 0.12), (-0.5, -0.8, 0.07),
                                      (0.5, -0.8, 0.07), (0.0, -1.0, 0.08))):
        o.horn(b, (dx * 0.06, hy + dy * 0.06, 0.52), (dx or 0.01, dy, 0), ln, 0.018,
               up=1.8 if i < 2 else 2.6)
    o.tube(b, [(0, hy + 0.04, 0.53), (0, hy + 0.08, 0.6), (0, hy + 0.13, 0.62)],
           [0.026, 0.02, 0], o.HAIR, sides=5)
    b.cylinder(0, hy + 0.06, 0.56, 0.58, 0.026, GOLD, sides=6)

    # arms: left fist on the hip, right hand on the planted greataxe
    b.obox((-0.2, -0.02, 0.29), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.042, 0.044, 0.06, o.SKIN,
           bevel=0.015)
    b.box(-0.24, -0.16, -0.07, 0.0, 0.17, 0.235, o.METAL, bevel=0.01)
    b.ball(-0.19, -0.04, 0.15, 0.04, 0.06, o.SKIN, sides=6)
    b.obox((0.2, -0.04, 0.3), (1, 0, 0), (0, 0.8, -0.6), (0, 0.6, 0.8), 0.042, 0.044, 0.065,
           o.SKIN, bevel=0.015)
    b.box(0.165, 0.245, -0.14, -0.07, 0.24, 0.3, o.METAL, bevel=0.01)
    b.ball(0.21, -0.13, 0.27, 0.04, 0.06, o.SKIN, sides=6)

    # the greataxe, planted head-up beside the right hand: haft, two crescent blades, a spike
    ax, ay = 0.24, -0.14
    b.cylinder(ax, ay, 0.0, 0.62, 0.016, o.TIMBER, sides=6)
    b.cylinder(ax, ay, 0.3, 0.34, 0.02, o.RUST, sides=6)
    for k in (-1, 1):
        b.prism([(0.01, 0.46), (0.01, 0.56), (0.11, 0.62), (0.13, 0.51), (0.1, 0.4)],
                (ax, ay - 0.006, 0), (k, 0, 0), (0, 0, 1), 0.012, o.METAL, back_cap=True)
        b.prism([(0.12, 0.62), (0.145, 0.615), (0.145, 0.395), (0.12, 0.4)],
                (ax, ay - 0.005, 0), (k, 0, 0), (0, 0, 1), 0.01, 'stone_light', back_cap=True)
    b.box(ax - 0.022, ax + 0.022, ay - 0.022, ay + 0.022, 0.48, 0.54, GOLD)
    o.tube(b, [(ax, ay, 0.62), (ax, ay, 0.7)], [0.016, 0.0], o.METAL, sides=4)

    # trophy pole on the back with a small banner and a skull
    b.cylinder(0.06, 0.1, 0.25, 0.86, 0.012, o.TIMBER, sides=5)
    b.prism([(0.0, 0.84), (0.13, 0.84), (0.13, 0.7), (0.065, 0.73), (0.0, 0.7)],
            (0.066, 0.1, 0), (1, 0, 0), (0, 0, 1), 0.008, o.RUST, back_cap=True)
    o.skull(b, 0.06, 0.09, 0.62, s=0.05)
    o.scale_since(b, since, SCALE)
    return b


o.ensure_collection()
kit.run(OBJ, build, source_name="Flag1-lvl1", offset=(o.COLUMN_X + 3.6, 0, 0),
        collection=o.COLLECTION)
