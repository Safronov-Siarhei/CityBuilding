"""OrcGate1-lvl1 -- the gate in an orc palisade. Footprint 1x2 like the settlement's Gate: the
palisade runs along Y through it, raiders pass across it along X.

The palisade comes in from both ends and stops at two massive posts crowned with horns. Between
them hang two leaves of lashed logs, sharpened along the top, studded with iron spikes and
barred across. Over them a crossbeam carries a row of skulls and a rust banner on each face.

Borrows Gate1's box and stands in the orc column.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402

OBJ = "OrcGate1-lvl1"


def gate(b, level=1):
    """The gate itself; level 2 stands in the level-2 palisade: its arms, taller posts on rock
    footings with iron caps and torches, the leaves plated in iron."""
    arm = o.palisade_arm if level == 1 else o.palisade_arm2
    arm(b, 0, 1.0, 0, -1, 0.0, 0.46, seed=1)
    arm(b, 0, -1.0, 0, 1, 0.0, 0.46, seed=4)

    post_h = (o.PALISADE_H if level == 1 else o.PALISADE2_H) + 0.5
    for py in (-0.44, 0.44):
        o.tube(b, [(0, py, 0.01), (0, py, post_h)], [0.1, 0.085], o.TIMBER, sides=7, tip=False)
        for z in (0.25, 0.75):
            b.cylinder(0, py, z, z + 0.06, 0.11, o.RUST, sides=7)
        for k in (-1, 1):
            o.horn(b, (0, py, post_h - 0.02), (0.3 * k, py, 0), 0.16, 0.035, up=1.4)

    # the leaves: lashed logs, sharpened tops, a bar across and iron studs on the outside
    n = 6
    for i in range(n):
        y = -0.34 + 0.68 * i / (n - 1)
        o.stake(b, 0, y, 0.02, o.PALISADE_H - 0.04 + (0.04 if i % 2 else 0.0), r=0.055,
                color=o.TIMBER_LIGHT if i % 2 else o.TIMBER, point=0.1)
    for z in (0.2, 0.52):
        b.box(-0.075, 0.075, -0.37, 0.37, z, z + 0.05, o.RUST, skip=())
    for side in (-1, 1):
        b.rod((side * 0.07, -0.34, 0.1), (side * 0.07, 0.34, 0.66), 0.035, o.TIMBER)
        for y, z in ((-0.2, 0.36), (0.0, 0.42), (0.2, 0.36), (-0.1, 0.12), (0.1, 0.12)):
            o.tube(b, [(side * 0.06, y, z), (side * 0.12, y, z + 0.01)], [0.016, 0.0], o.METAL,
                   sides=4)

    # crossbeam with skulls and banners
    beam_z = post_h - 0.18
    b.box(-0.09, 0.09, -0.56, 0.56, beam_z, beam_z + 0.1, o.TIMBER, bevel=0.012)
    for y in (-0.22, 0.0, 0.22):
        o.skull(b, -0.08, y, beam_z + 0.1, s=0.1, facing=(-1, 0))
    # a ragged rust cloth hanging from the beam on each face, two tongues and a notch
    top, drop, w = beam_z + 0.02, 0.3, 0.13
    for side in (-1, 1):
        x0 = side * 0.095 + (0.0 if side > 0 else -0.012)
        for pts in ([(-w, top), (0, top), (0, top - drop + 0.06), (-w, top - drop)],
                    [(0, top), (w, top), (w, top - drop), (0, top - drop + 0.06)]):
            b.prism(pts, (x0, 0, 0), (0, 1, 0), (0, 0, 1), 0.012, o.RUST, back_cap=True)
        b.rod((side * 0.112, -0.05, top - drop * 0.5 - 0.05),
              (side * 0.112, 0.05, top - drop * 0.5 + 0.05), 0.014, o.BONE)
        b.rod((side * 0.112, 0.05, top - drop * 0.5 - 0.05),
              (side * 0.112, -0.05, top - drop * 0.5 + 0.05), 0.014, o.BONE)
    if level >= 2:
        for py in (-0.44, 0.44):
            b.crag(0, py, 0.17, 0.14, 0.16, o.ROCK, top=o.ROCK_LIGHT, sides=6, seed=int(py * 10) + 5)
            b.cylinder(0, py, post_h - 0.04, post_h + 0.02, 0.1, o.METAL, sides=7)
            o.iron_spikes(b, 0, py, 0.09, 6, post_h - 0.16, length=0.08)
            for side in (-1, 1):
                o.torch(b, side * 0.2, py * 1.25, h=0.62)
        for side in (-1, 1):
            for y in (-0.18, 0.18):
                b.box(side * 0.06 - 0.008, side * 0.06 + 0.008, y - 0.15, y + 0.15, 0.08, 0.66,
                      o.METAL, skip=())
            b.box(side * 0.07 - 0.006, side * 0.07 + 0.006, -0.36, 0.36, 0.3, 0.36, o.RUST, skip=())
        o.skull(b, -0.1, 0, beam_z + 0.12, s=0.14, facing=(-1, 0))
    return b


def build(lo, hi):
    return gate(Builder())


def build2(lo, hi):
    return gate(Builder(), 2)


def build_ruin(lo, hi):
    """Broken in: the palisade either side snapped to stumps of uneven height, one post
    standing with its horns, the other a stump; the leaves thrown flat outward, the crossbeam
    down across them, skulls rolled away."""
    b = Builder()
    o.scorch(b, 0.0, 0.0, 0.34, 0.62, seed=1)
    for end in (-1, 1):
        for i in range(4):
            y = end * (1.0 - o.PALISADE_STEP / 2 - i * o.PALISADE_STEP)
            h = (0.62, 0.3, 0.46, 0.18)[(i + (end > 0)) % 4]
            o.broken_log(b, 0, y, 0.01, h, r=o.PALISADE_R,
                         color=o.TIMBER_LIGHT if i % 2 else o.TIMBER)
        b.box(-0.058, 0.058, end * 0.54 - 0.06, end * 0.54 + 0.06, 0.18, 0.21, o.RUST, skip=())
    post_h = o.PALISADE_H + 0.5
    o.tube(b, [(0, -0.44, 0.01), (0, -0.44, post_h)], [0.1, 0.085], o.TIMBER, sides=7, tip=False)
    b.cylinder(0, -0.44, 0.25, 0.31, 0.11, o.RUST, sides=7)
    o.horn(b, (0, -0.44, post_h - 0.02), (-0.3, -0.44, 0), 0.16, 0.035, up=1.4)
    o.broken_log(b, 0, 0.44, 0.01, 0.4, r=0.1)
    # the leaves, flat in the gateway, logs side by side across it, bars along
    for i in range(6):
        y = -0.32 + 0.64 * i / 5
        o.fallen_log(b, (0.42, y, 0.055), (-0.42, y + 0.03, 0.055), r=0.05,
                     color=o.TIMBER_LIGHT if i % 2 else o.TIMBER)
    for x in (-0.22, 0.2):
        b.box(x - 0.03, x + 0.03, -0.36, 0.38, 0.1, 0.13, o.RUST, skip=())
    # the crossbeam fallen across, skulls rolled away
    o.fallen_log(b, (0.3, -0.74, 0.07), (-0.3, 0.7, 0.17), r=0.055)
    for x, y, f in ((0.36, 0.62, (1, -0.3)), (-0.36, -0.62, (-0.4, -1)), (0.28, -0.5, (0.6, -1))):
        o.skull(b, x, y, 0.02, s=0.09, facing=f)
    return b


o.run_with_ruin(OBJ, build, build_ruin, "Gate1-lvl1", (o.COLUMN_X, 0, 0))
o.place("OrcGate1-lvl2", build2, "Gate1-lvl1", o.GRID_X[2], -66.0)
import bpy  # noqa: E402
bpy.ops.wm.save_mainfile()
