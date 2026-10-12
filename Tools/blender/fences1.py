"""The two fences, four pieces each (agreed 2026-10-12): Straight, Corner, T and Cross.

    Fence1-lvl1-*           wooden palisade  -- pairs with Gate1 and Tower1
    FortifiedFence1-lvl1-*  stone wall       -- pairs with FortifiedGate1 and FortifiedTower1

Every piece fills its 1x1 cell edge to edge; neighbours meet on the cell border, where the log
and merlon rhythm continues unbroken. Orientation is the game's FenceShape convention, read in
Blender axes (N = +Y, E = +X; to be checked on the first FBX export):

    Straight  N-S           Corner  S + E
    T         N-S + E       Cross   N + E + S + W

T and Cross have no placeholders of their own in the user's file: they borrow the Straight's
box and stand beside it.
"""
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from kit import Builder  # noqa: E402

DIRS = {'N': (0, 1), 'E': (1, 0), 'S': (0, -1), 'W': (-1, 0)}
SHAPES = {'Straight': 'NS', 'Corner': 'SE', 'T': 'NSE', 'Cross': 'NESW'}
COLLECTION = "Оборонительные здания"


def palisade(shape):
    def build(lo, hi):
        b = Builder()
        sides = SHAPES[shape]
        for k in sides:
            dx, dy = DIRS[k]
            d.palisade_arm(b, 0, 0, dx, dy)
        if shape != 'Straight':
            b.box(-0.06, 0.06, -0.06, 0.06, 0, d.PALISADE_H - 0.02, 'wood_dark', skip=('-z', '+z'))
            b.pyramid(0, 0, d.PALISADE_H - 0.02, 0.06, 0.06, d.PALISADE_H + 0.08, 'wood_dark')
        return b
    return build


def stone(shape):
    def build(lo, hi):
        b = Builder()
        sides = SHAPES[shape]
        hw = d.STONE_WALL_T / 2
        if shape == 'Straight':
            d.stone_wall_arm(b, 0, -0.5, 0, 1, 0.0, 1.0)
            return b
        b.box(-hw - 0.03, hw + 0.03, -hw - 0.03, hw + 0.03, 0, d.FOOTING, 'stone_mid')
        b.box(-hw, hw, -hw, hw, d.FOOTING, d.WALL_H, 'stone_light')
        b.box(-d.MERLON_L / 2, d.MERLON_L / 2, -d.MERLON_L / 2, d.MERLON_L / 2, d.WALL_H,
              d.WALL_H + d.MERLON_H, 'stone_light')
        for k in sides:
            dx, dy = DIRS[k]
            d.stone_wall_arm(b, 0, 0, dx, dy, hw, 0.5)
        return b
    return build


def main():
    pieces = list(SHAPES)
    for i, shape in enumerate(pieces):
        kit.run(f"Fence1-lvl1-{shape}", palisade(shape),
                source_name="Fence1-lvl1" if shape != 'Corner' else "Fence1-lvl1.001",
                offset=(0, 0, 0) if shape in ('Straight', 'Corner') else (1.3 * (i - 1), 0, 0),
                collection=COLLECTION, save=False)
        kit.run(f"FortifiedFence1-lvl1-{shape}", stone(shape), source_name="FortifiedFence1-lvl1",
                offset=(1.3 * i, 0, 0), collection=COLLECTION, save=False)
    bpy.ops.wm.save_mainfile()


main()
