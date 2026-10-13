"""Road1-lvl1 and Road1-lvl2 -- the Road (Дорога). Footprint 1x1, laid cell by cell.

Level 1 is a sand track: a packed sand tile 2 cm high with a few pebbles pressed into it
(worn patches were tried and dropped: repeated cell by cell they read as a stamp). Level 2 is the paved street: a cobbled tile 3 cm high, earth with stones set in
staggered rows.

Roads do not autotile (every cell is this one model), so neither level has kerbs or ruts --
they would leave seams at every turn -- and both are built to continue across every edge: the
sand's details stay well inside the cell, and of the cobbles the halves cut by the left and
right edges meet their neighbour's halves.

It is deliberately not an obstacle: the game does not carve roads out of the NavMesh and pins
citizens to the ground height, so they walk on it with their feet sunk a couple of centimetres.
Hundreds are on screen at once, so it stays near a hundred triangles.
"""
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
from kit import Builder  # noqa: E402

SAND_H = 0.02
BASE, TOP = 0.015, 0.03
STONES = ('cobble', 'stone_mid', 'cobble', 'stone_dark')   # muted: a street, not a chessboard


def sand(lo, hi):
    b = Builder()
    b.box(-0.5, 0.5, -0.5, 0.5, 0, SAND_H, 'sand')
    for x, y, r, a, c in ((-0.3, -0.28, 0.035, 10, 'stone_mid'), (0.05, -0.05, 0.028, 40, 'cobble'),
                          (0.3, 0.25, 0.032, -20, 'stone_mid'), (-0.08, 0.34, 0.024, 65, 'cobble'),
                          (0.36, -0.36, 0.022, 0, 'cobble')):
        b.zbox(x, y, SAND_H - 0.005, SAND_H + 0.012, r * 1.3, r, a, c)   # pebbles
    return b


def cobble(lo, hi):
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


def main():
    kit.run("Road1-lvl1", sand, save=False)
    kit.run("Road1-lvl2", cobble, source_name="Road1-lvl1", offset=(1.3, 0, 0),
            collection="Город", save=False)
    bpy.ops.wm.save_mainfile()


main()
