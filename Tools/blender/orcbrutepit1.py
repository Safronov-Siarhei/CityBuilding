"""OrcBrutePit1-lvl1 -- the brute pit, the spawner of a base's brutes (the user's sketches,
2026-10-16: one spawner per orc kind; while it stands it sends out a new guard every 30 s).
Footprint 2x2.

A fighting pit: a round wall of raw rock and earth, dark trodden ground inside, a ring of
leaning stakes round its lip with a gap in front closed by an iron-barred cage gate, chains
from a post in the middle, a war gong on a frame at the back, a rack of mauls and skulls.

Borrows BigBarn1's 2x2 box and stands in the base grid; its ruin is in the ruin column.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402

R_OUT, R_IN = 0.84, 0.6
WALL = 0.24
GAP = 26          # half-angle of the gate gap in front, degrees


def pit_wall(b, broken=False):
    """The rock wall in segments round the pit, the trodden floor inside, the gap in front."""
    b.crag(0, 0.02, R_IN + 0.02, R_IN, 0.02, o.HAIR, sides=10, taper=0.98, seed=2)
    n = 14
    for i in range(n):
        t = 2 * math.pi * (i + 0.5) / n
        deg = (math.degrees(t) + 90) % 360
        if min(deg, 360 - deg) < GAP:
            continue
        r = (R_OUT + R_IN) / 2
        h = WALL * (0.5 if broken and i % 3 == 0 else 1.0)
        b.crag(r * math.cos(t), 0.02 + r * math.sin(t), 0.15, 0.12, h, o.ROCK,
               top='cobble' if i % 2 else o.ROCK_LIGHT, sides=6, rot_deg=math.degrees(t),
               taper=0.8, seed=i)


def build(lo, hi):
    b = Builder()
    pit_wall(b)
    # leaning stakes round the lip
    for i in range(20):
        t = 2 * math.pi * (i + 0.5) / 20
        deg = (math.degrees(t) + 90) % 360
        if min(deg, 360 - deg) < GAP:
            continue
        x, y = 0.86 * math.cos(t), 0.02 + 0.84 * math.sin(t)
        o.stake(b, x, y, WALL - 0.04, WALL + 0.36 + 0.06 * (i % 2), r=0.026,
                color=o.TIMBER_LIGHT if i % 2 else o.TIMBER,
                lean=(-0.07 * math.cos(t), -0.07 * math.sin(t)))
    # the cage gate in the gap: two posts, iron bars, a skull over it
    for k in (-1, 1):
        x = k * 0.36
        b.cylinder(x, -0.78, 0.0, 0.72, 0.05, o.TIMBER, sides=6)
        b.cylinder(x, -0.78, 0.5, 0.56, 0.058, o.RUST, sides=6)
        o.horn(b, (x, -0.78, 0.7), (k, -0.3, 0), 0.12, 0.028, up=1.3)
    b.box(-0.36, 0.36, -0.8, -0.76, 0.6, 0.66, o.TIMBER, bevel=0.008)
    for i in range(7):
        x = -0.27 + 0.09 * i
        b.cylinder(x, -0.78, 0.0, 0.6, 0.012, o.METAL, sides=4)
    b.box(-0.32, 0.32, -0.795, -0.765, 0.28, 0.31, o.METAL)
    o.skull(b, 0, -0.82, 0.62, s=0.1)
    # the chain post in the middle and its chains
    b.cylinder(0.0, 0.1, 0.0, 0.5, 0.045, o.TIMBER, sides=6)
    b.cylinder(0.0, 0.1, 0.44, 0.5, 0.055, o.METAL, sides=6)
    for k in (-1, 1):
        for j in range(4):
            x = k * (0.06 + 0.07 * j)
            b.box(x - 0.015, x + 0.015, 0.09, 0.11, 0.42 - 0.1 * j - 0.02, 0.42 - 0.1 * j + 0.02,
                  o.METAL if j % 2 else o.STONE_LIGHT)
    # the war gong on its frame at the back
    gx, gy = 0.0, 0.72
    for k in (-1, 1):
        b.rod((gx + k * 0.3, gy, 0.0), (gx + k * 0.24, gy, 1.0), 0.05, o.TIMBER)
    b.box(gx - 0.36, gx + 0.36, gy - 0.03, gy + 0.03, 0.96, 1.03, o.TIMBER, bevel=0.008)
    for k in (-1, 1):
        o.horn(b, (gx + k * 0.36, gy, 1.0), (k, 0, 0), 0.12, 0.03, up=1.5)
    for k in (-1, 1):
        b.rod((gx + k * 0.08, gy, 0.96), (gx + k * 0.06, gy, 0.88), 0.01, o.METAL)
    pts = [(0.2 * math.cos(2 * math.pi * i / 10), 0.62 + 0.2 * math.sin(2 * math.pi * i / 10))
           for i in range(10)]
    b.prism(pts, (gx, gy - 0.02, 0), (1, 0, 0), (0, 0, 1), 0.03, 'gold', back_cap=True)
    pts = [(0.07 * math.cos(2 * math.pi * i / 8), 0.62 + 0.07 * math.sin(2 * math.pi * i / 8))
           for i in range(8)]
    b.prism(pts, (gx, gy - 0.026, 0), (1, 0, 0), (0, 0, 1), 0.006, o.RUST, back_cap=True)
    # a rack of mauls at the right, a pile of skulls at the left
    rx = 0.62
    for k in (-1, 1):
        b.rod((rx, -0.2 + k * 0.16, 0.0), (rx, -0.2 + k * 0.16, 0.42), 0.022, o.TIMBER)
    b.rod((rx, -0.4, 0.38), (rx, 0.0, 0.38), 0.02, o.TIMBER_LIGHT)
    for dy in (-0.1, 0.06):
        b.rod((rx - 0.05, -0.2 + dy, 0.0), (rx + 0.02, -0.2 + dy, 0.5), 0.018, o.TIMBER_LIGHT)
        b.hcylinder('x', (rx + 0.02, -0.2 + dy, 0.52), 0.06, 0.045, o.METAL, sides=6)
    for i, (x, y) in enumerate(((-0.6, -0.3), (-0.66, -0.18), (-0.56, -0.2))):
        o.skull(b, x, y, 0.02 + (0.05 if i == 2 else 0), s=0.07, facing=(0.4, -1))
    o.war_banner(b, -0.78, 0.6, 1.5, w=0.16, drop=0.3)
    return b


def build_ruin(lo, hi):
    """Broken: the wall tumbled in places, the stakes down, the cage gate flat, the gong
    fallen face-down, a scorch."""
    b = Builder()
    pit_wall(b, broken=True)
    o.scorch(b, 0.1, 0.2, 0.5, 0.4, seed=6)
    for i, a in enumerate((0.4, 1.3, 2.2, 2.9, 3.6, 5.6)):
        x, y = 0.84 * math.cos(a), 0.02 + 0.82 * math.sin(a)
        if i % 2:
            o.broken_log(b, x, y, WALL - 0.04, WALL + 0.12, r=0.026)
        else:
            b.rod((x, y, WALL), (x * 0.7, y * 0.7, 0.06), 0.045, o.TIMBER_LIGHT)
    o.broken_log(b, -0.36, -0.78, 0.0, 0.4, r=0.05)
    b.box(-0.3, 0.32, -0.66, -0.3, 0.02, 0.05, o.METAL)
    b.box(-0.28, 0.3, -0.5, -0.46, 0.05, 0.08, o.TIMBER)
    b.rod((0.3, 0.72, 0.03), (-0.2, 0.62, 0.3), 0.05, o.TIMBER)
    b.cylinder(0.12, 0.5, 0.0, 0.03, 0.2, 'gold', sides=10)
    o.skull(b, 0.4, -0.3, 0.02, s=0.08, facing=(0.3, -1))
    return b


o.place("OrcBrutePit1-lvl1", build, "BigBarn1-lvl1", o.GRID_X[1], -46.0)
o.place("OrcBrutePit1-lvl1-Ruin", build_ruin, "BigBarn1-lvl1", o.GRID_X['ruin'], -46.0)
import bpy  # noqa: E402
bpy.ops.wm.save_mainfile()
