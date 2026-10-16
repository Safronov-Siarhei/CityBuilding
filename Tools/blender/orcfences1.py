"""The orc palisade, four pieces like the settlement's fences: Straight, Corner, T and Cross.

    OrcFence1-lvl1-*    rough log palisade -- pairs with OrcGate1-lvl1
    OrcFence1-lvl2-*    taller, iron-banded, spiked, on rocks -- pairs with OrcGate1-lvl2 and
                        OrcGateTower1

Every piece fills its 1x1 cell edge to edge and keeps the game's FenceShape convention, read in
Blender axes (N = +Y, E = +X): Straight N-S, Corner S + E, T N-S + E, Cross all four. A joint
gets a thick post crowned with a skull or horns.

All four borrow Fence1's placeholder box and stand side by side in the orc column.
"""
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402

DIRS = {'N': (0, 1), 'E': (1, 0), 'S': (0, -1), 'W': (-1, 0)}
SHAPES = {'Straight': 'NS', 'Corner': 'SE', 'T': 'NSE', 'Cross': 'NESW'}


def palisade(shape, level=1):
    def build(lo, hi):
        b = Builder()
        sides = SHAPES[shape]
        joint = shape != 'Straight'
        arm = o.palisade_arm if level == 1 else o.palisade_arm2
        for i, k in enumerate(sides):
            dx, dy = DIRS[k]
            arm(b, 0, 0, dx, dy, t0=0.09 if joint else 0.0, seed=i * 5)
        if joint:
            top = (o.PALISADE_H if level == 1 else o.PALISADE2_H) + 0.16
            if level == 2:
                b.cylinder(0, 0, top - 0.02, top + 0.03, 0.1, o.METAL, sides=7)
                o.iron_spikes(b, 0, 0, 0.09, 6, top - 0.2, length=0.09)
            o.tube(b, [(0, 0, 0.01), (0, 0, top)], [0.085, 0.075], o.TIMBER, sides=7, tip=False)
            b.cylinder(0, 0, 0.3, 0.36, 0.095, o.RUST, sides=7)
            if shape in ('Corner', 'Cross'):
                o.skull(b, 0, -0.01, top, s=0.13)
            else:
                for k in (-1, 1):
                    o.horn(b, (0, 0, top - 0.02), (k, 0, 0), 0.13, 0.03, up=1.3)
        return b
    return build


def main():
    o.ensure_collection()
    for i, shape in enumerate(SHAPES):
        kit.run(f"OrcFence1-lvl1-{shape}", palisade(shape), source_name="Fence1-lvl1",
                offset=(o.COLUMN_X + 4.0 + 1.3 * i, 0, 0), collection=o.COLLECTION, save=False)
        o.place(f"OrcFence1-lvl2-{shape}", palisade(shape, 2), "Fence1-lvl1",
                o.GRID_X[2] + 1.3 * i, -81.0)
    bpy.ops.wm.save_mainfile()


main()
