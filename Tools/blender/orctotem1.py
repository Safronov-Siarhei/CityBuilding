"""OrcTotem1-lvl1 -- a war totem standing in an orc base. Footprint 1x1.

A thick carved pole on a cairn of raw rock: three stacked faces carved into it, each with a
rust-painted brow and bone teeth, bone wings spread from the top, and over it all a great
horned skull. Two fire bowls burn at its foot, bones lie scattered around.

Borrows DecTree1's 1x1 box and stands in the orc column.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402

OBJ = "OrcTotem1-lvl1"
POLE_R = 0.13
TOP = 1.32


def carved_face(b, z, h, paint):
    """One face carved into the pole, facing -y: a jutting brow, two eyes, a jaw with teeth."""
    y = -POLE_R
    b.box(-0.12, 0.12, y - 0.05, y + 0.02, z + h * 0.62, z + h * 0.78, paint, bevel=0.012)
    for k in (-1, 1):
        b.box(k * 0.055 - 0.025, k * 0.055 + 0.025, y - 0.02, y + 0.01, z + h * 0.44,
              z + h * 0.6, o.HAIR)
    b.box(-0.1, 0.1, y - 0.045, y + 0.02, z + h * 0.06, z + h * 0.3, o.TIMBER_LIGHT, bevel=0.01)
    for k in (-1, 0, 1):
        b.box(k * 0.05 - 0.014, k * 0.05 + 0.014, y - 0.052, y - 0.03, z + h * 0.26,
              z + h * 0.38, o.BONE)
    for k in (-1, 1):
        o.horn(b, (k * 0.08, y - 0.03, z + h * 0.24), (k * 0.3, -1, 0), 0.05, 0.016, up=1.6)


def build(lo, hi):
    b = Builder()
    # the cairn
    for i, (x, y, r, h) in enumerate(((0, 0, 0.32, 0.16), (-0.24, -0.18, 0.12, 0.1),
                                      (0.26, -0.12, 0.1, 0.08), (0.2, 0.24, 0.12, 0.1),
                                      (-0.22, 0.22, 0.1, 0.09))):
        b.crag(x, y, r, r * 0.9, h, o.ROCK, top=o.ROCK_LIGHT, sides=6, taper=0.7, seed=i)

    # the pole with its three faces, bound in rust between them
    b.cylinder(0, 0, 0.1, TOP, POLE_R, o.TIMBER, sides=8)
    seg = (TOP - 0.16) / 3
    for i in range(3):
        z = 0.16 + i * seg
        carved_face(b, z, seg - 0.04, o.RUST if i % 2 == 0 else o.TIMBER_LIGHT)
        b.cylinder(0, 0, z + seg - 0.04, z + seg, POLE_R + 0.015, o.RUST, sides=8)

    # bone wings spread from the top
    for k in (-1, 1):
        for j, (up, ln) in enumerate(((0.9, 0.34), (0.5, 0.3), (0.15, 0.24))):
            o.horn(b, (k * POLE_R, 0.02, TOP - 0.1 - j * 0.06), (k, 0.25, 0), ln, 0.028, up=up)
        b.prism([(0.0, TOP - 0.1), (0.3, TOP + 0.12), (0.26, TOP - 0.14)],
                (k * POLE_R, 0.025, 0) if k > 0 else (-POLE_R, 0.025, 0),
                (k, 0, 0), (0, 0, 1), 0.008, o.HIDE, back_cap=True)

    # the great horned skull on top
    b.cylinder(0, 0, TOP, TOP + 0.03, POLE_R + 0.02, o.TIMBER_LIGHT, sides=8)
    o.skull(b, 0, -0.02, TOP + 0.07, s=0.24)
    for k in (-1, 1):
        o.horn(b, (k * 0.1, 0.0, TOP + 0.2), (k, 0.1, 0), 0.24, 0.045, up=1.5, segments=4)

    # fire bowls and scattered bones
    for k in (-1, 1):
        x, y = k * 0.3, -0.34
        b.cylinder(x, y, 0.0, 0.12, 0.02, o.TIMBER, sides=5)
        b.cylinder(x, y, 0.12, 0.17, 0.06, o.METAL, sides=6)
        b.blob(x, y, 0.15, 0.045, 0.12, o.FIRE, sides=4)
    for i, (x, y, a) in enumerate(((-0.08, -0.38, 20), (0.12, -0.4, -40), (0.36, 0.0, 70))):
        r = math.radians(a)
        b.rod((x - 0.05 * math.cos(r), y - 0.05 * math.sin(r), 0.012),
              (x + 0.05 * math.cos(r), y + 0.05 * math.sin(r), 0.012), 0.016, o.BONE)
    o.skull(b, -0.36, 0.0, 0.02, s=0.07, facing=(0.4, -1))
    return b


def build_ruin(lo, hi):
    """Toppled: the pole snapped above the first face, the rest lying across the cairn with
    the great skull split off beside it, the wings scattered as bones, the fire bowls over."""
    b = Builder()
    for i, (x, y, r, h) in enumerate(((0, 0, 0.32, 0.16), (-0.24, -0.18, 0.12, 0.1),
                                      (0.26, -0.12, 0.1, 0.08), (0.2, 0.24, 0.12, 0.1),
                                      (-0.22, 0.22, 0.1, 0.09))):
        b.crag(x, y, r, r * 0.9, h, o.ROCK, top=o.ROCK_LIGHT, sides=6, taper=0.7, seed=i)
    o.scorch(b, 0.02, 0.0, 0.4, 0.36, seed=5)
    # the stump with its one surviving face
    b.cylinder(0, 0, 0.1, 0.5, POLE_R, o.TIMBER, sides=8)
    seg = (TOP - 0.16) / 3
    carved_face(b, 0.16, seg - 0.04, o.RUST)
    o.broken_log(b, 0, 0, 0.48, 0.56, r=POLE_R)
    # the fallen top, lying across the cairn towards the back right
    o.fallen_log(b, (0.06, 0.1, 0.2), (0.4, 0.4, 0.1), r=POLE_R * 0.95)
    # the great skull split off, one horn snapped
    o.skull(b, -0.28, 0.3, 0.06, s=0.2, facing=(-0.3, -1))
    o.horn(b, (-0.36, 0.32, 0.16), (-1, 0.2, 0), 0.12, 0.04, up=1.2)
    o.tube(b, [(0.18, -0.3, 0.03), (0.36, -0.22, 0.04), (0.44, -0.08, 0.03)], [0.04, 0.03, 0.0],
           o.BONE, sides=5)
    # the wings, scattered bones
    for x, y, a in ((-0.36, -0.08, 30), (0.38, 0.06, -60), (-0.1, -0.4, 80), (0.04, 0.42, 10)):
        r = math.radians(a)
        b.rod((x - 0.08 * math.cos(r), y - 0.08 * math.sin(r), 0.015),
              (x + 0.08 * math.cos(r), y + 0.08 * math.sin(r), 0.015), 0.018, o.BONE)
    # fire bowls knocked over
    for k in (-1, 1):
        x, y = k * 0.3, -0.36
        b.rod((x, y, 0.02), (x + k * 0.1, y - 0.06, 0.02), 0.02, o.TIMBER)
        b.cylinder(x + k * 0.04, y + 0.06, 0.0, 0.04, 0.06, o.METAL, sides=6)
    return b


o.run_with_ruin(OBJ, build, build_ruin, "DecTree1-lvl1", (o.COLUMN_X, 0, 0))
