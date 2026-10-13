"""Road1-lvl1 -- the Road (Дорога). Footprint 1x1, laid cell by cell.

A cobbled tile 3 cm high, edge to edge: packed earth with stones set in staggered rows. Roads
do not autotile (every cell is this one model), so there are no kerbs -- they would leave seams
at every turn -- and the pattern is built to continue across every edge: whole stones stay
inside the cell, and the stones cut by the left and right edges are halves that meet their
neighbour's halves.

It is deliberately not an obstacle: the game does not carve roads out of the NavMesh and pins
citizens to the ground height, so they walk on it with their feet sunk a couple of centimetres.
Hundreds are on screen at once, so it stays near a hundred triangles.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
from kit import Builder  # noqa: E402

OBJ = "Road1-lvl1"
BASE, TOP = 0.015, 0.03
STONES = ('cobble', 'stone_mid', 'cobble', 'stone_dark')   # muted: a street, not a chessboard


def build(lo, hi):
    b = Builder()
    b.box(-0.5, 0.5, -0.5, 0.5, 0, BASE, 'burlap')
    rows = 3
    step, gap = 1.0 / rows, 0.035
    for r in range(rows):
        y0 = -0.5 + r * step + gap / 2
        y1 = y0 + step - gap
        if r % 2 == 0:      # three whole stones
            xs = [(-0.5 + i * step + gap / 2, -0.5 + (i + 1) * step - gap / 2) for i in range(3)]
        else:               # two whole stones between two halves cut by the cell edges
            h = step / 2
            xs = [(-0.5, -0.5 + h - gap / 2)]
            xs += [(-0.5 + h + i * step + gap / 2, -0.5 + h + (i + 1) * step - gap / 2)
                   for i in range(2)]
            xs += [(0.5 - h + gap / 2, 0.5)]
        for i, (x0, x1) in enumerate(xs):
            edge = x0 <= -0.5 or x1 >= 0.5    # a half: same colour on both edges, no seam
            b.box(x0, x1, y0, y1, BASE, TOP, 'cobble' if edge else STONES[(r * 3 + i) % 4],
                  skip=('-z',))
    return b


kit.run(OBJ, build)
