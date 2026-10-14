"""OrcWarTent1-lvl1 -- the orcs' war tent, where a base's garrison lives. Footprint 2x2.

A long ridge tent of dark hides, the ridge running away from the camera so the front gable,
with its dark entrance under a rust awning, faces it. Rust stripes run down the hides, poles
cross above both ends with horns lashed on, guy ropes run to pegs. In front: a fire pit with a
spit, a rack of axes and spears, a war drum, and a stake with a skull.

Borrows BigBarn1's 2x2 box and stands in the orc column.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402

OBJ = "OrcWarTent1-lvl1"

Y0, Y1 = -0.42, 0.88        # the tent runs from just behind the yard to the back of the plot
HALF = 0.62                 # half its width at the ground
RIDGE = 1.08
HIDE = 'door'               # dark hide -- reads as leather, takes the plank pattern softly


def build(lo, hi):
    b = Builder()
    b.crag(0.0, 0.08, 0.8, 0.76, 0.03, o.STONE, top='cobble', sides=8, taper=0.97, seed=6)

    # the hide body and its two slopes' rust stripes
    b.gable_block('y', Y0, Y1, -HALF, HALF, 0.03, RIDGE, HIDE)
    slope = math.atan2(RIDGE - 0.03, HALF)
    for side in (-1, 1):
        d = (-side * math.cos(slope), 0, math.sin(slope))
        n = (side * math.sin(slope), 0, math.cos(slope))
        for y in (Y0 + 0.28, Y1 - 0.28):
            c = (side * HALF / 2, y, (RIDGE + 0.03) / 2)
            b.obox((c[0] + n[0] * 0.006, c[1], c[2] + n[2] * 0.006), d, (0, 1, 0), n,
                   math.hypot(HALF, RIDGE) / 2 - 0.02, 0.06, 0.006, o.RUST, skip=())
        # a patch sewn on
        c = (side * HALF * 0.45, (Y0 + Y1) / 2, RIDGE * 0.52)
        b.obox((c[0] + n[0] * 0.006, c[1], c[2] + n[2] * 0.006), d, (0, 1, 0), n,
               0.12, 0.1, 0.006, o.HIDE, skip=())

    # front gable: a dark entrance under a rust awning, a skull over it
    b.prism([(-0.22, 0.03), (0.22, 0.03), (0.0, 0.62)], (0, Y0 + 0.004, 0), (1, 0, 0), (0, 0, 1),
            0.01, o.HAIR, back_cap=True)
    for k in (-1, 1):
        b.prism([(k * 0.02, 0.03), (k * 0.25, 0.03), (k * 0.02, 0.6)], (0, Y0 + 0.002, 0),
                (1, 0, 0), (0, 0, 1), 0.004, HIDE, back_cap=True)
    b.obox((0, Y0 - 0.12, 0.66), (1, 0, 0), (0, 0.9, 0.44), (0, -0.44, 0.9), 0.24, 0.14, 0.01,
           o.RUST, skip=())
    for k in (-1, 1):
        b.rod((k * 0.22, Y0 - 0.24, 0.0), (k * 0.22, Y0 - 0.24, 0.61), 0.022, o.TIMBER)
    o.skull(b, 0, Y0 - 0.02, 0.74, s=0.1)

    # crossed poles above both ends, horns lashed where they cross
    for y in (Y0 - 0.01, Y1 + 0.01):
        for k in (-1, 1):
            b.rod((k * HALF, y, 0.0), (-k * 0.16, y, RIDGE + 0.2), 0.03, o.TIMBER)
        b.cylinder(0, y, RIDGE - 0.02, RIDGE + 0.04, 0.04, o.RUST, sides=5)
    o.horn(b, (0, Y0 - 0.03, RIDGE + 0.04), (1, -0.3, 0), 0.14, 0.03, up=1.3)
    o.horn(b, (0, Y0 - 0.03, RIDGE + 0.04), (-1, -0.3, 0), 0.14, 0.03, up=1.3)
    b.hcylinder('y', (0, (Y0 + Y1) / 2, RIDGE + 0.015), (Y1 - Y0) / 2 + 0.04, 0.03, o.TIMBER)

    # guy ropes to pegs along both sides
    for k in (-1, 1):
        for y in (Y0 + 0.2, (Y0 + Y1) / 2, Y1 - 0.2):
            px = k * (HALF + 0.24)
            b.rod((k * HALF * 0.45, y, RIDGE * 0.55), (px, y, 0.05), 0.008, o.HIDE)
            b.rod((px, y, 0.0), (px, y, 0.08), 0.02, o.TIMBER)

    # the yard: fire pit with a spit, weapon rack, war drum, skull stake
    fx, fy = -0.42, -0.72
    for i in range(7):
        a = 2 * math.pi * i / 7
        o.rubble(b, fx + 0.13 * math.cos(a), fy + 0.13 * math.sin(a), r=0.045, seed=i)
    b.blob(fx, fy, 0.0, 0.08, 0.18, o.FIRE, sides=5)
    for k in (-1, 1):
        b.rod((fx + k * 0.17, fy, 0.0), (fx + k * 0.17, fy, 0.24), 0.016, o.TIMBER)
    b.rod((fx - 0.2, fy, 0.23), (fx + 0.2, fy, 0.23), 0.012, o.METAL)
    b.blob(fx, fy, 0.18, 0.05, 0.09, 'wood_light', sides=5)

    rx, ry = 0.5, -0.66
    for k in (-1, 1):
        b.rod((rx + k * 0.17, ry, 0.0), (rx + k * 0.17, ry, 0.32), 0.022, o.TIMBER)
    b.rod((rx - 0.2, ry, 0.28), (rx + 0.2, ry, 0.28), 0.018, o.TIMBER_LIGHT)
    for i, dx in enumerate((-0.1, 0.0, 0.1)):
        b.rod((rx + dx - 0.04, ry + 0.06, 0.0), (rx + dx, ry - 0.02, 0.42), 0.012, o.TIMBER_LIGHT)
        if i == 1:
            o.tube(b, [(rx + dx, ry - 0.02, 0.42), (rx + dx + 0.003, ry - 0.03, 0.48)],
                   [0.014, 0.0], o.METAL, sides=4)
        else:
            b.prism([(0.0, 0.3), (0.0, 0.38), (-0.07, 0.4), (-0.08, 0.31)],
                    (rx + dx - 0.006, 0, 0), (0, 1, 0), (0, 0, 1), 0.012, o.METAL, back_cap=True)

    b.cylinder(0.15, -0.82, 0.0, 0.16, 0.09, o.RUST, sides=8)
    b.cylinder(0.15, -0.82, 0.16, 0.17, 0.095, o.HIDE, sides=8)
    b.cylinder(0.15, -0.82, 0.04, 0.06, 0.095, o.TIMBER, sides=8)
    b.cylinder(0.15, -0.82, 0.11, 0.13, 0.095, o.TIMBER, sides=8)

    o.stake(b, 0.82, -0.2, 0.0, 0.62, r=0.025)
    o.skull(b, 0.82, -0.23, 0.48, s=0.08)
    o.war_banner(b, -0.8, 0.82, 1.5, w=0.18, drop=0.34)
    return b


o.ensure_collection()
kit.run(OBJ, build, source_name="BigBarn1-lvl1", offset=(o.COLUMN_X + 2.0, 0, 0),
        collection=o.COLLECTION)
