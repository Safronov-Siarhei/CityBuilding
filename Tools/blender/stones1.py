"""Stone1..Stone10 -- the boulders citizens break up by hand for stone, no quarry needed.

The ten are a SIZE LADDER, smallest first, up to a mossy outcrop filling its cell at Stone10.
The first three are one size (about 20 cm tall, half a metre across) in three shapes: the user's
call, after pebbles any smaller turned out nearly invisible from the game camera. The game is meant to make a boulder's stone stock follow its size, so the
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

from stone_shapes import STONES  # noqa: E402


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
