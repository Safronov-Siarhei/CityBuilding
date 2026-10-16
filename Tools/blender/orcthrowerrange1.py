"""OrcThrowerRange1-lvl1 -- the throwers' range, the spawner of a base's throwers (the user's
sketches, 2026-10-16: one spawner per orc kind). Footprint 2x2.

A practice yard: a hide lean-to at the back with racks of javelins and throwing axes under it,
two straw-and-hide targets on stakes with axes stuck in them, a skull target on a post, a heap
of throwing stones, a chopping block and a banner.

Borrows BigBarn1's 2x2 box and stands in the base grid; its ruin is in the ruin column.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402

STORE = kit.borrow("orcstorehouse1.py")


def target(b, x, y, z, r=0.17, axes=((0.05, 0.04), (-0.06, -0.05))):
    """A round straw target faced in hide on two stakes, facing -y, axes stuck in it."""
    for k in (-1, 1):
        b.rod((x + k * 0.1, y + 0.05, 0.0), (x + k * 0.08, y + 0.02, z + 0.06), 0.025, o.TIMBER)
    pts = [(r * math.cos(2 * math.pi * i / 10), z + r * math.sin(2 * math.pi * i / 10))
           for i in range(10)]
    b.prism(pts, (x, y, 0), (1, 0, 0), (0, 0, 1), 0.06, 'hay', back_cap=True)
    pts = [(r * 0.65 * math.cos(2 * math.pi * i / 8), z + r * 0.65 * math.sin(2 * math.pi * i / 8))
           for i in range(8)]
    b.prism(pts, (x, y - 0.004, 0), (1, 0, 0), (0, 0, 1), 0.005, o.HIDE, back_cap=True)
    pts = [(r * 0.25 * math.cos(2 * math.pi * i / 6), z + r * 0.25 * math.sin(2 * math.pi * i / 6))
           for i in range(6)]
    b.prism(pts, (x, y - 0.008, 0), (1, 0, 0), (0, 0, 1), 0.005, o.RUST, back_cap=True)
    for dx, dz in axes:
        b.rod((x + dx, y - 0.01, z + dz), (x + dx + 0.04, y - 0.16, z + dz - 0.04), 0.014, o.TIMBER)
        b.prism([(0.0, 0.0), (0.05, 0.0), (0.06, 0.05), (0.0, 0.035)],
                (x + dx - 0.005, y - 0.02, z + dz - 0.02), (1, 0, 0), (0, 0, 1), -0.012, o.METAL,
                back_cap=True)


def build(lo, hi):
    b = Builder()
    b.crag(0.0, 0.05, 0.78, 0.8, 0.03, o.STONE, top="cobble", sides=8, taper=0.97, seed=7)
    STORE['lean_to'](b, -0.82, 0.82, 0.38, 0.88, 0.82, 0.55)
    # racks of javelins and throwing axes under the lean-to
    for rx in (-0.5, 0.1):
        b.rod((rx - 0.2, 0.62, 0.3), (rx + 0.2, 0.62, 0.3), 0.02, o.TIMBER_LIGHT)
        for i in range(5):
            x = rx - 0.16 + 0.08 * i
            b.rod((x - 0.02, 0.7, 0.0), (x, 0.6, 0.62), 0.012, o.TIMBER_LIGHT)
            o.tube(b, [(x, 0.6, 0.62), (x + 0.003, 0.59, 0.69)], [0.014, 0.0], o.METAL, sides=4)
    for i in range(3):
        x = 0.52 + 0.1 * i
        b.rod((x, 0.66, 0.0), (x, 0.62, 0.32), 0.014, o.TIMBER)
        b.prism([(0.0, 0.26), (0.06, 0.26), (0.07, 0.33), (0.0, 0.31)], (x - 0.005, 0.62, 0),
                (1, 0, 0), (0, 0, 1), 0.012, o.METAL, back_cap=True)
    # targets out in the yard, facing the throwing line at the front
    target(b, -0.42, -0.02, 0.42)
    target(b, 0.36, 0.08, 0.46, axes=((0.0, 0.07), (0.07, -0.04), (-0.07, -0.02)))
    # a skull target on a post
    o.stake(b, 0.0, 0.18, 0.0, 0.62, r=0.025)
    o.skull(b, 0.0, 0.15, 0.5, s=0.09)
    b.rod((0.02, 0.14, 0.55), (0.06, -0.02, 0.52), 0.012, o.TIMBER)
    # a heap of throwing stones and a chopping block on the throwing line
    for i, (x, y) in enumerate(((-0.62, -0.62), (-0.52, -0.66), (-0.58, -0.54), (-0.47, -0.56),
                                (-0.55, -0.6))):
        o.rubble(b, x, y, r=0.055, seed=i, color=o.ROCK_LIGHT if i % 2 else o.ROCK)
    b.cylinder(0.4, -0.6, 0.0, 0.16, 0.1, o.TIMBER_LIGHT, sides=7)
    b.rod((0.36, -0.6, 0.16), (0.5, -0.52, 0.3), 0.014, o.TIMBER)
    b.prism([(0.0, 0.0), (0.06, 0.0), (0.07, 0.05), (0.0, 0.04)], (0.33, -0.606, 0.12),
            (1, 0, 0), (0, 0, 1), 0.012, o.METAL, back_cap=True)
    # a line of stakes marks the throwing line
    for x in (-0.25, -0.05, 0.15):
        o.stake(b, x, -0.74, 0.0, 0.26, r=0.02, color=o.TIMBER_LIGHT)
    o.war_banner(b, 0.78, -0.76, 1.5, w=0.16, drop=0.3)
    return b


def build_ruin(lo, hi):
    """Wrecked: the lean-to down over its racks, the targets knocked flat, spears scattered."""
    b = Builder()
    b.crag(0.0, 0.05, 0.78, 0.8, 0.03, o.STONE, top="cobble", sides=8, taper=0.97, seed=7)
    o.scorch(b, 0.0, 0.4, 0.6, 0.36, seed=8)
    o.broken_log(b, -0.78, 0.42, 0.0, 0.46, r=0.03)
    o.broken_log(b, 0.78, 0.84, 0.0, 0.3, r=0.03)
    b.obox((0.0, 0.62, 0.12), (1, 0, 0), (0, 0.95, 0.3), (0, -0.3, 0.95), 0.78, 0.24, 0.015, 'door',
           skip=())
    for x, y, a in ((-0.4, 0.05, 30), (0.36, 0.1, -20)):
        b.zbox(x, y, 0.0, 0.05, 0.17, 0.12, a, 'hay')
    for i, (x0, y0, x1, y1) in enumerate(((-0.6, -0.2, -0.2, -0.4), (0.1, -0.3, 0.5, -0.1),
                                          (-0.2, 0.3, 0.2, 0.2))):
        b.rod((x0, y0, 0.015), (x1, y1, 0.015), 0.014, o.TIMBER_LIGHT)
    for i, (x, y) in enumerate(((-0.62, -0.62), (-0.52, -0.66), (-0.58, -0.54))):
        o.rubble(b, x, y, r=0.055, seed=i, color=o.ROCK)
    o.skull(b, 0.1, -0.1, 0.02, s=0.08, facing=(0.3, -1))
    return b


o.place("OrcThrowerRange1-lvl1", build, "BigBarn1-lvl1", o.GRID_X[1], -42.0)
o.place("OrcThrowerRange1-lvl1-Ruin", build_ruin, "BigBarn1-lvl1", o.GRID_X['ruin'], -42.0)
import bpy  # noqa: E402
bpy.ops.wm.save_mainfile()
