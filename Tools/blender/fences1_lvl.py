"""The fence's levels 3-5 (the user, 2026-10-28: every level its own model). Level 1 is the
wooden palisade (Fence1-lvl1-*), level 2 the stone wall (FortifiedFence1-lvl1-*); these grow the
stone wall the way the set grows everything else:

    Fence1-lvl3-*   blue banners hang on both faces of a straight run; a junction rises into a
                    squat pier with a blue cap and a gold finial (level 3's gold)
    Fence1-lvl4-*   level 3 with every merlon under a blue cap -- level 4's mark is the colour
                    roof with gold on it: the pier's cap glazed
    Fence1-lvl5-*   level 4 gilded: the merlon caps and the pier's cap turn gold

Same four shapes and orientation as fences1.py (Straight N-S, Corner S+E, T N-S+E, Cross all
four); every piece still meets its neighbour on the cell border with the merlon rhythm unbroken.
Pieces stand in a row beside FortifiedFence1-lvl1, one row per level.
"""
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from kit import Builder  # noqa: E402

FENCES = kit.borrow("fences1.py")
SHAPES = FENCES['SHAPES']
DIRS = FENCES['DIRS']
COLLECTION = "Оборонительные здания"
PIER_H = d.WALL_H + 0.24


def merlon_caps(b, shape, colour):
    """A four-sided cap on every merlon the piece carries, on the shared merlon grid (the same
    walk as details.stone_wall_arm, so each cap sits on its merlon)."""
    hw = d.STONE_WALL_T / 2
    top = d.WALL_H + d.MERLON_H
    if shape == 'Straight':
        arms = [(0.0, -0.5, 0, 1, 0.0, 1.0)]
    else:
        arms = [(0.0, 0.0, DIRS[k][0], DIRS[k][1], hw, 0.5) for k in SHAPES[shape]]
    for ox, oy, dx, dy, t0, t1 in arms:
        t = d.FENCE_MERLON_STEP / 2
        while t < t1 - 1e-6:
            if t >= t0 - 1e-6:
                x, y = ox + dx * t, oy + dy * t
                along, across = d.MERLON_L / 2 + 0.012, hw + 0.012
                hx, hy = (across, along) if dx == 0 else (along, across)
                b.pyramid(x, y, top, hx, hy, top + 0.07, colour)
            t += d.FENCE_MERLON_STEP


def banners(b):
    """A blue banner with a gold edge hanging down each face of a straight run, mid-cell."""
    hw = d.STONE_WALL_T / 2
    for side in (-1, 1):
        x = side * (hw + 0.006)
        b.box(x - 0.005, x + 0.005, -0.075, 0.075, d.WALL_H - 0.05, d.WALL_H - 0.02, 'gold', skip=())
        b.prism([(-0.065, d.WALL_H - 0.05), (0.065, d.WALL_H - 0.05), (0.065, 0.24), (0.0, 0.18),
                 (-0.065, 0.24)], (x, 0, 0), (0, 1, 0), (0, 0, 1), 0.008, 'banner_blue',
                back_cap=True)


def pier(b, cap, finial):
    """The junction raised into a squat pier: the wall's core grows to PIER_H under a cap."""
    hw = d.STONE_WALL_T / 2 + 0.04
    b.box(-hw, hw, -hw, hw, d.WALL_H, PIER_H, 'stone_light', bevel=0.006)
    b.box(-hw - 0.02, hw + 0.02, -hw - 0.02, hw + 0.02, PIER_H, PIER_H + 0.03, 'stone_mid')
    apex = PIER_H + 0.03 + 0.2
    b.pyramid(0, 0, PIER_H + 0.03, hw + 0.03, hw + 0.03, apex, 'roof_blue' if cap == 'glazed' else cap)
    if cap == 'glazed':
        d.glazed_pyramid(b, 0, 0, PIER_H + 0.03, hw + 0.03, hw + 0.03, apex, 'roof_blue_dark',
                         crest=False)
    if finial:
        d.finial(b, 0, 0, apex - 0.02, h=0.12)


def level(n):
    def make(shape):
        def build(lo, hi):
            b = FENCES['stone'](shape)(lo, hi)
            if shape == 'Straight':
                banners(b)
            else:
                pier(b, {3: 'roof_blue', 4: 'glazed', 5: 'gold'}[n], finial=True)
            if n >= 4:
                merlon_caps(b, shape, 'roof_blue' if n == 4 else 'gold')
            return b
        return build
    return make


def main():
    for n in (3, 4, 5):
        for i, shape in enumerate(SHAPES):
            kit.run(f"Fence1-lvl{n}-{shape}", level(n)(shape), source_name="FortifiedFence1-lvl1",
                    offset=(1.3 * i, 1.6 * (n - 2), 0), collection=COLLECTION, save=False)
    bpy.ops.wm.save_mainfile()


main()
