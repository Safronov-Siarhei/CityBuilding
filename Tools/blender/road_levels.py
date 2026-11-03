"""Levels 3-5 of the Road and the Square (the user, 2026-11-03: "think up levels 3-5"):

    3  Плиты       big dressed slabs in staggered rows, dark joints
    4  Кирпич      red and orange brick in basket weave, light joints
    5  Королевская white marble slabs on GOLD joints (the golden roofs of every level 5)

The Square wears the same surface round its middle piece: a stone ring (3), a compass rose in a
brick ring (4), a gold star in a gold ring (5).

Like road1.py, every pattern continues across every edge of a cell (the grid of half-cells lines
up with the cell edges, halves cut by an edge meet their neighbour's halves in the same colour),
so a street or a square laid next to a street shows no seam; and the road stays near a hundred
triangles, hundreds being on screen.

    blender --background Buildings-Size1-Models.blend --python road_levels.py
    then export_fbx.py -- G:/Fork/CityBuilding Road1-lvl3 Road1-lvl4 Road1-lvl5 Square1-lvl3 Square1-lvl4 Square1-lvl5
"""
import math
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
from kit import Builder  # noqa: E402

BASE, TOP = 0.015, 0.03
STEP = 0.5            # the half-cell grid every pattern keeps to


def _inside(x0, x1, y0, y1, skip_r):
    return skip_r > 0 and math.hypot((x0 + x1) / 2, (y0 + y1) / 2) < skip_r


def slabs(b, half, skip_r=0.0):
    b.box(-half, half, -half, half, 0, BASE, 'stone_dark')
    gap = 0.03
    rows = round(2 * half / STEP)
    for r in range(rows):
        y0 = -half + r * STEP + gap / 2
        y1 = y0 + STEP - gap
        if r % 2 == 0:
            xs = [(-half + i * STEP + gap / 2, -half + (i + 1) * STEP - gap / 2) for i in range(rows)]
        else:
            h = STEP / 2
            xs = [(-half, -half + h - gap / 2)]
            xs += [(-half + h + i * STEP + gap / 2, -half + h + (i + 1) * STEP - gap / 2) for i in range(rows - 1)]
            xs += [(half - h + gap / 2, half)]
        for i, (x0, x1) in enumerate(xs):
            if _inside(x0, x1, y0, y1, skip_r):
                continue
            edge = x0 <= -half or x1 >= half
            colour = 'stone_mid' if edge else ('stone_light', 'stone_mid', 'cobble')[(r * 5 + i * 3) % 3]
            b.box(x0, x1, y0, y1, BASE, TOP, colour, skip=('-z',))


def bricks(b, half, skip_r=0.0):
    b.box(-half, half, -half, half, 0, BASE, 'stone_mid')
    gap = 0.025
    n = round(2 * half / STEP)
    reds = ('roof_red_dark', 'roof_orange_dark', 'roof_red_dark', 'plaster_pink')
    for i in range(n):
        for j in range(n):
            bx, by = -half + i * STEP, -half + j * STEP
            if _inside(bx, bx + STEP, by, by + STEP, skip_r):
                continue
            for k in range(2):                     # two bricks a block, across or along by turns
                if (i + j) % 2 == 0:
                    x0, x1 = bx + gap / 2, bx + STEP - gap / 2
                    y0, y1 = by + k * STEP / 2 + gap / 2, by + (k + 1) * STEP / 2 - gap / 2
                else:
                    x0, x1 = bx + k * STEP / 2 + gap / 2, bx + (k + 1) * STEP / 2 - gap / 2
                    y0, y1 = by + gap / 2, by + STEP - gap / 2
                b.box(x0, x1, y0, y1, BASE, TOP, reds[(i * 3 + j * 5 + k) % 4], skip=('-z',))


def marble(b, half, skip_r=0.0):
    b.box(-half, half, -half, half, 0, BASE + 0.005, 'gold')    # the joints are gold
    gap = 0.024
    n = round(2 * half / STEP)
    for i in range(n):
        for j in range(n):
            x0, y0 = -half + i * STEP + gap / 2, -half + j * STEP + gap / 2
            x1, y1 = x0 + STEP - gap, y0 + STEP - gap
            if _inside(x0, x1, y0, y1, skip_r):
                continue
            b.box(x0, x1, y0, y1, BASE, TOP, 'plaster_white' if (i + j) % 2 == 0 else 'stone_light', skip=('-z',))
    # a gold lozenge where four slabs meet in the middle of each cell
    cells = round(half * 2)
    for i in range(cells):
        for j in range(cells):
            cx, cy = -half + i + 0.5, -half + j + 0.5
            if skip_r > 0 and math.hypot(cx, cy) < skip_r + 0.1:
                continue
            b.zbox(cx, cy, TOP - 0.002, TOP + 0.006, 0.06, 0.06, 45, 'gold')


SURFACE = {3: slabs, 4: bricks, 5: marble}
RING = 0.46


def road(level):
    def build(lo, hi):
        b = Builder()
        SURFACE[level](b, 0.5)
        return b
    return build


def square(level):
    def build(lo, hi):
        b = Builder()
        SURFACE[level](b, 1.0, skip_r=RING + 0.06)
        if level == 3:
            b.cylinder(0, 0, BASE, TOP + 0.004, RING, 'stone_light', sides=16)
            b.cylinder(0, 0, TOP + 0.004, TOP + 0.008, RING - 0.1, 'stone_mid', sides=16)
            b.cylinder(0, 0, TOP + 0.008, TOP + 0.03, 0.12, 'stone_light', sides=10)    # a round plinth
        elif level == 4:
            b.cylinder(0, 0, BASE, TOP + 0.004, RING, 'roof_red_dark', sides=16)
            b.cylinder(0, 0, TOP + 0.004, TOP + 0.008, RING - 0.07, 'stone_light', sides=16)
            for k in range(4):                     # the compass rose: four long points, four short
                a = 90 * k
                r = math.radians(a)
                b.zbox(math.cos(r) * 0.17, math.sin(r) * 0.17, TOP + 0.008, TOP + 0.014, 0.17, 0.035, a, 'stone_dark')
                r2 = math.radians(a + 45)
                b.zbox(math.cos(r2) * 0.1, math.sin(r2) * 0.1, TOP + 0.008, TOP + 0.013, 0.1, 0.025, a + 45, 'roof_orange_dark')
            b.cylinder(0, 0, TOP + 0.008, TOP + 0.016, 0.045, 'gold', sides=8)
        else:
            b.cylinder(0, 0, BASE, TOP + 0.004, RING, 'gold', sides=20)
            b.cylinder(0, 0, TOP + 0.004, TOP + 0.008, RING - 0.035, 'plaster_white', sides=20)
            for a in (0, 45):                      # an eight-point star of two turned squares
                b.zbox(0, 0, TOP + 0.008, TOP + 0.013, 0.2, 0.2, a, 'gold')
            b.cylinder(0, 0, TOP + 0.013, TOP + 0.018, 0.1, 'plaster_white', sides=12)
            b.cylinder(0, 0, TOP + 0.018, TOP + 0.024, 0.05, 'gold', sides=8)
        return b
    return build


def main():
    for level in (3, 4, 5):
        kit.run(f"Road1-lvl{level}", road(level), source_name="Road1-lvl1", offset=(1.3 * level, 0, 0),
                collection="Город", save=False)
        kit.run(f"Square1-lvl{level}", square(level), source_name="Road1-lvl1", offset=(2.5 * level, 3.0, 0),
                collection="Город", save=False)
    bpy.ops.wm.save_mainfile()


main()
