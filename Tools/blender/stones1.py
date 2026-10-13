"""Stone1..Stone10 -- the boulders citizens break up by hand for stone, no quarry needed.

The ten are a SIZE LADDER, smallest first: a few pebbles at Stone1 up to a mossy outcrop filling
its cell at Stone10. The game is meant to make a boulder's stone stock follow its size, so the
ladder has to read at a glance from the game camera and its steps have to be even -- each model's
rough volume is printed (VOLUME ...) for that mapping. All ten use the faceted crags the mines'
hills are built from, so loose rock and mountain read as one material.

Every one fits the user's 1x1x1.5 box. The map scatters ~140 of them, so each stays small in
triangles: a crag is 28.
"""
import math
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
from kit import Builder  # noqa: E402

# (x, y, radius x, radius y, height, colour, crown, seed, rotation, taper) per crag, smallest first.
STONES = {
    "Stone1": [(-0.1, -0.05, 0.09, 0.07, 0.1, 'stone_mid', None, 1, 10, 0.5),
               (0.1, 0.06, 0.07, 0.06, 0.08, 'stone_light', None, 4, 40, 0.5),
               (0.0, 0.14, 0.05, 0.05, 0.06, 'stone_dark', None, 7, 0, 0.5)],
    "Stone2": [(-0.06, 0.0, 0.14, 0.11, 0.17, 'stone_mid', None, 2, 15, 0.55),
               (0.14, -0.08, 0.08, 0.07, 0.1, 'stone_dark', None, 5, 50, 0.5)],
    "Stone3": [(0.0, 0.0, 0.22, 0.17, 0.2, 'stone_light', None, 3, 25, 0.6),
               (-0.2, 0.14, 0.07, 0.06, 0.08, 'stone_mid', None, 8, 0, 0.5)],
    "Stone4": [(-0.04, 0.02, 0.2, 0.18, 0.32, 'stone_mid', None, 6, 5, 0.55),
               (0.2, -0.12, 0.1, 0.09, 0.15, 'stone_dark', None, 9, 35, 0.5)],
    "Stone5": [(0.0, 0.0, 0.27, 0.24, 0.42, 'stone_dark', None, 0, 20, 0.5),
               (-0.22, -0.17, 0.08, 0.07, 0.1, 'stone_light', None, 11, 0, 0.5)],
    "Stone6": [(-0.08, 0.05, 0.25, 0.23, 0.52, 'stone_mid', 'grass_dark', 10, 0, 0.5),
               (0.2, -0.12, 0.15, 0.13, 0.28, 'stone_light', None, 3, 30, 0.55)],
    "Stone7": [(0.0, 0.03, 0.3, 0.28, 0.62, 'stone_light', 'grass', 12, 12, 0.6),
               (-0.24, -0.2, 0.11, 0.1, 0.18, 'stone_mid', None, 5, 0, 0.5),
               (0.25, -0.18, 0.09, 0.08, 0.12, 'stone_dark', None, 1, 45, 0.5)],
    "Stone8": [(-0.1, 0.06, 0.29, 0.27, 0.74, 'stone_mid', 'grass_dark', 4, 8, 0.62),
               (0.2, -0.1, 0.2, 0.18, 0.5, 'stone_dark', None, 7, 32, 0.5)],
    "Stone9": [(0.0, 0.06, 0.36, 0.33, 0.9, 'stone_dark', 'grass', 9, 18, 0.62),
               (-0.26, -0.22, 0.13, 0.12, 0.3, 'stone_light', None, 2, 0, 0.5),
               (0.28, -0.22, 0.12, 0.1, 0.22, 'stone_mid', None, 6, 40, 0.5)],
    "Stone10": [(-0.05, 0.1, 0.36, 0.32, 1.1, 'stone_mid', 'grass', 11, 6, 0.6),
                (0.22, -0.06, 0.22, 0.24, 0.78, 'stone_dark', 'grass_dark', 3, 28, 0.6),
                (-0.22, -0.24, 0.17, 0.15, 0.45, 'stone_light', None, 8, 15, 0.5),
                (0.24, -0.32, 0.1, 0.09, 0.16, 'stone_mid', None, 0, 0, 0.5)],
}


def frustum_volume(rx, ry, h, taper):
    """Rough volume of one crag: a tapered elliptic column (the shoulder ring is ignored)."""
    a0 = math.pi * rx * ry
    a1 = a0 * taper * taper
    return h * (a0 + a1 + math.sqrt(a0 * a1)) / 3


def builder_for(name):
    def build(lo, hi):
        b = Builder()
        for x, y, rx, ry, h, col, top, seed, rot, taper in STONES[name]:
            b.crag(x, y, rx, ry, h, col, top=top, sides=6, rot_deg=rot, taper=taper, seed=seed)
        return b
    return build


def main():
    for name, crags in STONES.items():
        kit.run(name, builder_for(name), save=False)
        vol = sum(frustum_volume(c[2], c[3], c[4], c[9]) for c in crags)
        print(f"VOLUME {name} {vol:.4f} m3")
    bpy.ops.wm.save_mainfile()


main()
