"""FortifiedGate1-lvl2 -- the Fortified Gate (Укреплённые ворота), stone tier, after its first
upgrade. Footprint 1x3 (level 1's); the curtain wall comes in exactly as at level 1.

Level 1 with the corner turrets raised and flying blue pennants, and lanterns either side of
the passage on both faces. Level 1, for reference:

FortifiedGate1-lvl1 -- the Fortified Gate (Укреплённые ворота), stone tier. Footprint 1x3
(the user, 2026-10-21): the stone wall runs along Y through it, people pass across it along X,
and only the middle cell is the way through.

The curtain wall fills the two outer cells on the shared merlon grid and meets, in the middle
cell, a gatehouse: a
taller crenellated block with an arched passage, a raised portcullis, a blue-roofed turret on
each front corner and blue banners.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, WALL_H  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "FortifiedGate1-lvl2"
LVL2_DX = 2.6   # level 2 stands this far +X of level 1
GX, GY = 0.38, 0.5   # the gatehouse's half-width and half-length: exactly the middle cell along Y
END = 1.5            # the plot's ends, where the curtain wall meets the fence beyond
BANNER_S = 0.37      # the banners' place along a gatehouse face


def build(lo, hi):
    b = Builder()
    gx, gy = GX, GY
    d.stone_wall_arm(b, 0, END, 0, -1, 0.0, END - gy)
    d.stone_wall_arm(b, 0, -END, 0, 1, 0.0, END - gy)

    g_top = WALL_H + 0.36
    d.footing(b, -gx, gx, -gy, gy)
    b.box(-gx, gx, -gy, gy, FOOTING, g_top, 'stone_light', bevel=0.02)
    b.box(-gx - 0.05, gx + 0.05, -gy - 0.05, gy + 0.05, g_top - 0.08, g_top + 0.04, 'stone_mid',
          bevel=0.012)
    d.merlons(b, 'y', -gy - 0.05, gy + 0.05, -gx - 0.05, -gx + 0.09, g_top + 0.04)
    d.merlons(b, 'y', -gy - 0.05, gy + 0.05, gx - 0.09, gx + 0.05, g_top + 0.04)
    for side in (-1, 1):
        f = Facade(b, '-x' if side < 0 else '+x', side * gx)
        d.gate(f, 0, 0.0, w=0.42, h=0.58)
        f.rect(-0.21, 0.21, 0.5, 0.56, 0.05, 'iron')  # the raised portcullis
        for s in (-0.12, 0.0, 0.12):
            f.rect(s - 0.01, s + 0.01, 0.42, 0.56, 0.055, 'iron')
        d.banner(f, -BANNER_S, g_top - 0.1, 0.36, color='banner_blue')
        d.banner(f, BANNER_S, g_top - 0.1, 0.36, color='banner_blue')
        for s in (-0.27, 0.27):
            d.wall_lantern(f, s, 0.36)

    # turrets on the four corners of the gatehouse
    t_top = g_top + 0.4   # level 1: + 0.22
    for sx in (-1, 1):
        for sy in (-1, 1):
            cx, cy = sx * (gx - 0.1), sy * (gy - 0.1)
            b.box(cx - 0.16, cx + 0.16, cy - 0.16, cy + 0.16, g_top, t_top, 'stone_light', bevel=0.015)
            b.pyramid(cx, cy, t_top, 0.2, 0.2, t_top + 0.3, 'roof_blue')
            b.box(cx - 0.01, cx + 0.01, cy - 0.01, cy + 0.01, t_top + 0.26, t_top + 0.52, 'wood_dark')
            b.prism([(cx + 0.01, t_top + 0.51), (cx + 0.01, t_top + 0.39), (cx + 0.2, t_top + 0.45)],
                    (0, cy + 0.005, 0), (1, 0, 0), (0, 0, 1), 0.01, 'banner_blue', back_cap=True)
    return b


kit.run(OBJ, build, source_name="FortifiedGate1-lvl1", offset=(LVL2_DX, 0, 0),
        collection="Оборонительные здания")
