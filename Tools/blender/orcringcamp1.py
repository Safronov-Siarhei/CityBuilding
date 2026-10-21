"""The Invasion mode's round orc camps (the user, 2026-10-21): the two rings of wall are built
from one straight section laid round a circle, with an open gate in each ring the waves march out
through. Everything else in the camp is the walled camps' own buildings at their top level.

    OrcRingWall       one section of the ring palisade, RING_SECTION metres along X, centred on
                      the origin, the outside towards -Y. The level-2 palisade: tall iron-banded
                      logs, iron spikes; a horned post at the +X end, where the next section meets it.
    OrcRingWall-Ruin  the section broken: stumps, fallen logs, scorched ground.
    OrcRingGate       an open gateway as wide as GATE_W: two massive horned posts, a crossbeam
                      with skulls and a rust banner, torches -- no leaves, the orcs walk through
                      along Y.

The game lays 24 sections round the outer ring (15 m) and 14 round the inner (8.5 m): a 15 m ring
cut into 24 is 3.92 m a chord, an 8.5 m one cut into 14 is 3.8 -- one section of 3.9 serves both.
Borrows Fence1's and Gate1's boxes and stands in the orc column.
"""
import math
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402

RING_SECTION = 3.9
GATE_W = 2.4


def end_post(b, x):
    top = o.PALISADE2_H + 0.2
    o.tube(b, [(x, 0, 0.01), (x, 0, top)], [0.1, 0.09], o.TIMBER, sides=6, tip=False, cap=False)
    b.cylinder(x, 0, top - 0.02, top + 0.03, 0.11, o.METAL, sides=6)
    for k in (-1, 1):
        o.horn(b, (x, 0, top - 0.02), (0, k, 0), 0.12, 0.028, up=1.3, segments=2)


LOG_STEP = 0.2                 # the ring is long: fewer, thicker logs than a cell's palisade
LOG_R = 0.075
_JAG = (0.0, 0.07, -0.04, 0.1, 0.02, -0.06, 0.08, -0.02)


def wall(lo, hi):
    """The section, lean: ~25 metres of it go round a camp and three camps stand on the map, so a
    log is a five-sided pointed post, the bands are boxes, and a few rocks and spikes stand in for
    the cell palisade's many."""
    b = Builder()
    half = RING_SECTION / 2
    t = -half + LOG_STEP / 2
    i = 0
    while t < half - 1e-6:
        j = (i * 3 + 1) % len(_JAG)
        h = o.PALISADE2_H + _JAG[j]
        o.tube(b, [(t, 0, 0.01), (t, 0, h - 0.1), (t, 0, h + 0.05)], [LOG_R, LOG_R * 0.92, 0.0],
               o.TIMBER_LIGHT if i % 2 else o.TIMBER, sides=5, cap=False)
        t += LOG_STEP
        i += 1
    for z, col, w in ((0.22, o.RUST, 0.04), (0.58, o.RUST, 0.04), (0.76, o.METAL, 0.04)):
        b.box(-half, half, -LOG_R - 0.01, LOG_R + 0.01, z, z + w, col, skip=('-z', '+z'))
    for x in (-1.2, 0.0, 1.2):
        for side in (-1, 1):
            o.tube(b, [(x, side * (LOG_R + 0.005), 0.46), (x, side * (LOG_R + 0.11), 0.5)], [0.016, 0.0], o.METAL, sides=3)
    # one horned post, at the +X end: round the ring the next section's -X end stands on it
    end_post(b, half - 0.02)
    return b


def wall_ruin(lo, hi):
    b = Builder()
    half = RING_SECTION / 2
    t = -half + LOG_STEP / 2
    i = 0
    while t < half:
        h = 0.18 + 0.14 * ((i * 7) % 5) / 4
        if i % 3 != 1:
            o.tube(b, [(t, 0, 0.01), (t, 0.02 * ((i % 3) - 1), h)], [LOG_R, LOG_R * 0.9], o.TIMBER, sides=5, tip=False, cap=False)
        t += LOG_STEP
        i += 1
    for x0, y0, x1, y1 in ((-1.2, -0.15, -0.4, -0.75), (0.3, 0.2, 1.1, 0.7), (1.3, -0.2, 1.7, -0.85)):
        o.fallen_log(b, (x0, y0, LOG_R), (x1, y1, LOG_R), r=LOG_R)
    o.scorch(b, -0.6, -0.3, 0.7, 0.35, seed=2)
    o.scorch(b, 0.9, 0.25, 0.6, 0.3, seed=5)
    for x in (-half + 0.04, half - 0.04):
        o.broken_log(b, x, 0.0, 0.01, 0.42, r=0.1)
    return b


def gate(lo, hi):
    b = Builder()
    half = GATE_W / 2
    top = o.PALISADE2_H + 0.7
    for x in (-half, half):
        b.crag(x, 0, 0.2, 0.2, 0.14, o.ROCK, top=o.ROCK_LIGHT, sides=6, seed=int(x * 10) + 7)
        o.tube(b, [(x, 0, 0.01), (x, 0, top)], [0.13, 0.11], o.TIMBER, sides=8, tip=False)
        b.cylinder(x, 0, top - 0.03, top + 0.04, 0.14, o.METAL, sides=8)
        for z in (0.35, 0.8):
            b.cylinder(x, 0, z, z + 0.06, 0.14, o.RUST, sides=8)
        for k in (-1, 1):
            o.horn(b, (x, 0, top), (0, k, 0), 0.17, 0.04, up=1.4)
        o.torch(b, x + (0.25 if x > 0 else -0.25), -0.22, h=0.5)
    # the crossbeam with its skulls, a banner hanging on each face
    beam = top - 0.18
    o.tube(b, [(-half - 0.12, 0, beam), (half + 0.12, 0, beam)], [0.075, 0.075], o.TIMBER, sides=6, tip=False)
    b.box(-half + 0.1, half - 0.1, -0.08, 0.08, beam - 0.12, beam - 0.08, o.RUST, skip=())
    for i in range(5):
        x = -0.6 + i * 0.3
        o.skull(b, x, -0.1, beam + 0.04, s=0.09, facing=(0, -1))
    # a rust cloth hanging from the beam on each face, high enough to walk under
    for side in (-1, 1):
        y0 = side * 0.085
        b.box(-0.17, 0.17, min(y0, y0 + side * 0.012), max(y0, y0 + side * 0.012), beam - 0.42, beam - 0.08, o.RUST, skip=())
        b.box(-0.05, 0.05, min(y0, y0 + side * 0.02), max(y0, y0 + side * 0.02), beam - 0.3, beam - 0.2, o.BONE, skip=())
    return b


def main():
    o.ensure_collection()
    x0 = o.COLUMN_X + 4.0
    kit.run("OrcRingWall", wall, source_name="Fence1-lvl1", offset=(x0, -12.0, 0), collection=o.COLLECTION, save=False)
    kit.run("OrcRingWall-Ruin", wall_ruin, source_name="Fence1-lvl1", offset=(x0 + o.RUIN_DX, -12.0, 0),
            collection=o.COLLECTION, save=False)
    kit.run("OrcRingGate", gate, source_name="Gate1-lvl1", offset=(x0 + 5.0, -12.0, 0), collection=o.COLLECTION, save=False)
    bpy.ops.wm.save_mainfile()


main()
