"""Farm1-lvl2 -- the Farm (Ферма) after its first upgrade. Footprint 1x1 (level 1's). Front -Y.

Level 1 fenced along its back and left edges, with a second sheaf and pumpkins ripening at the
front. Level 1, for reference:


One field: a bed of tilled earth with ripe wheat standing in four rows, a scarecrow in a straw
hat guarding the back corner, a sheaf tied up at the front.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from kit import Builder  # noqa: E402

OBJ = "Farm1-lvl2"
LVL2_DX = 1.6   # level 2 stands this far +X of level 1


def build(lo, hi):
    b = Builder()
    b.box(-0.47, 0.47, -0.47, 0.47, 0, 0.035, 'wood_mid', bevel=0.01)
    for i, y in enumerate((-0.33, -0.11, 0.11, 0.33)):
        b.box(-0.44, 0.44, y - 0.05, y + 0.05, 0.035, 0.05, 'wood_dark')    # the furrow ridge
        x, k = -0.38, 0
        while x < (0.14 if i == 3 else 0.4):
            dy = 0.025 if k % 2 else -0.025
            b.blob(x, y + dy, 0.05, 0.05, 0.22 + 0.03 * (k % 3 == 1), 'hay' if k % 3 else 'roof_straw',
                   sides=4)
            x += 0.085
            k += 1

    # scarecrow
    sx, sy = 0.3, 0.3
    b.box(sx - 0.018, sx + 0.018, sy - 0.018, sy + 0.018, 0.035, 0.52, 'wood_dark')
    b.box(sx - 0.15, sx + 0.15, sy - 0.015, sy + 0.015, 0.38, 0.41, 'wood_dark')
    b.zbox(sx, sy, 0.22, 0.42, 0.075, 0.05, 0, 'roof_red', bevel=0.01)
    b.zbox(sx, sy, 0.22, 0.27, 0.08, 0.055, 0, 'roof_blue')
    for k in (-1, 1):
        b.blob(sx + k * 0.15, sy, 0.35, 0.03, 0.07, 'hay', sides=4)
    b.blob(sx, sy, 0.41, 0.065, 0.13, 'burlap', sides=6)
    b.cylinder(sx, sy, 0.5, 0.515, 0.1, 'roof_straw_dark', sides=8)
    b.pyramid(sx, sy, 0.515, 0.055, 0.055, 0.6, 'roof_straw', sides=8)

    # a sheaf at the front edge
    b.cylinder(0.3, -0.43, 0.035, 0.2, 0.045, 'hay', sides=6)
    b.cylinder(0.3, -0.43, 0.1, 0.125, 0.05, 'roof_straw_dark', sides=6)
    b.pyramid(0.3, -0.43, 0.2, 0.06, 0.06, 0.27, 'roof_straw', sides=6)
    b.cylinder(0.14, -0.44, 0.035, 0.19, 0.04, 'hay', sides=6)   # a second sheaf
    b.cylinder(0.14, -0.44, 0.09, 0.115, 0.045, 'roof_straw_dark', sides=6)
    b.pyramid(0.14, -0.44, 0.19, 0.055, 0.055, 0.25, 'roof_straw', sides=6)
    for x in (-0.36, -0.2):   # pumpkins
        b.ball(x, -0.43, 0.035, 0.045, 0.07, 'roof_orange', sides=6)
        b.box(x - 0.006, x + 0.006, -0.436, -0.424, 0.1, 0.125, 'leaf')
    d.rail_fence(b, [(-0.47, -0.47), (-0.47, 0.47), (0.47, 0.47)], h=0.2, post_step=0.3)
    return b


kit.run(OBJ, build, source_name="Farm1-lvl1", offset=(LVL2_DX, 0, 0),
        collection="Производство еды")
