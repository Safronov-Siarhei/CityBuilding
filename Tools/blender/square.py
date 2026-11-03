"""Square1-lvl1 and Square1-lvl2 -- the Square (Площадь, the user 2026-11-03: "a 2x2 road" in the
Decorations). Footprint 2x2, the Road's two looks (road1.py) on four cells:

Level 1 is packed sand with pebbles pressed in; level 2 is cobbles in staggered rows round a ring
of light stone in the middle -- what makes it a square and not four road tiles. Levels 3-5 show
level 2, as the Road's do.

Both stay flat and continue across the edges like the road's, so a square sits in a street with
no seam. Not an obstacle, like the road.

    blender --background Buildings-Size1-Models.blend --python square.py
    then export_fbx.py -- G:/Fork/CityBuilding Square1-lvl1 Square1-lvl2
"""
import math
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
from kit import Builder  # noqa: E402

SAND_H = 0.02
BASE, TOP = 0.015, 0.03
STONES = ('cobble', 'stone_mid', 'cobble', 'stone_dark')
HALF = 1.0


def sand(lo, hi):
    b = Builder()
    b.box(-HALF, HALF, -HALF, HALF, 0, SAND_H, 'sand')
    for x, y, r, a, c in ((-0.62, -0.55, 0.035, 10, 'stone_mid'), (0.1, -0.12, 0.028, 40, 'cobble'),
                          (0.6, 0.5, 0.032, -20, 'stone_mid'), (-0.18, 0.7, 0.024, 65, 'cobble'),
                          (0.72, -0.7, 0.022, 0, 'cobble'), (-0.7, 0.3, 0.03, 25, 'stone_mid'),
                          (0.35, 0.82, 0.026, 50, 'cobble'), (-0.3, -0.8, 0.025, -35, 'stone_mid')):
        b.zbox(x, y, SAND_H - 0.005, SAND_H + 0.012, r * 1.3, r, a, c)
    return b


def cobble(lo, hi):
    b = Builder()
    b.box(-HALF, HALF, -HALF, HALF, 0, BASE, 'burlap')
    ring_out, ring_in = 0.42, 0.3
    rows, step, gap = 6, 2 * HALF / 6, 0.035
    for r in range(rows):
        y0 = -HALF + r * step + gap / 2
        y1 = y0 + step - gap
        if r % 2 == 0:
            xs = [(-HALF + i * step + gap / 2, -HALF + (i + 1) * step - gap / 2) for i in range(6)]
        else:
            h = step / 2
            xs = [(-HALF, -HALF + h - gap / 2)]
            xs += [(-HALF + h + i * step + gap / 2, -HALF + h + (i + 1) * step - gap / 2) for i in range(5)]
            xs += [(HALF - h + gap / 2, HALF)]
        for i, (x0, x1) in enumerate(xs):
            cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
            if math.hypot(cx, cy) < ring_out + 0.04:
                continue                       # the middle is the ring's
            edge = x0 <= -HALF or x1 >= HALF
            b.box(x0, x1, y0, y1, BASE, TOP, 'cobble' if edge else STONES[(r * 6 + i) % 4], skip=('-z',))
    # the ring of light stone, and a rosette of four stones inside it
    b.cylinder(0, 0, BASE, TOP + 0.004, ring_out, 'stone_light', sides=16)
    b.cylinder(0, 0, TOP + 0.004, TOP + 0.008, ring_in, 'cobble', sides=16)
    for k in range(4):
        a = math.radians(45 + 90 * k)
        b.zbox(math.cos(a) * 0.15, math.sin(a) * 0.15, TOP + 0.008, TOP + 0.016, 0.07, 0.045, 45 + 90 * k, 'stone_light')
    return b


def main():
    kit.run("Square1-lvl1", sand, source_name="Road1-lvl1", offset=(0, 3.0, 0), collection="Город", save=False)
    kit.run("Square1-lvl2", cobble, source_name="Road1-lvl1", offset=(2.5, 3.0, 0), collection="Город", save=False)
    bpy.ops.wm.save_mainfile()


main()
