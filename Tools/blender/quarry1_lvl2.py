"""Quarry1-lvl2 -- the Quarry (Каменоломня) after its first upgrade. Footprint 2x2 (level 1's).
Front is -Y.

Level 1 with a treadwheel driving the crane's winch, a second stack of cut blocks and a lamp.
Level 1, for reference:


A rock hill cut back in clean steps: pale freshly cut terraces in front of the weathered crags,
with a block half sawn out on the lower step. A timber crane on the right lifts a block on its
rope; cut blocks wait in a stack and on a sledge; a tool lean-to with a slate roof.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "Quarry1-lvl2"
LVL2_DX = 2.6   # level 2 stands this far +X of level 1


def build(lo, hi):
    b = Builder()
    d.rock_hill(b, -0.95, 0.95, 0.05, 0.95, 1.15)

    # cut terraces: the fresh stone is light, each step lower and further forward, scored where
    # the next blocks will come out; rough crags hold the cut on the left
    for x0, x1, y0, y1, h in ((-0.7, 0.3, 0.1, 0.5, 0.72), (-0.62, 0.2, -0.22, 0.1, 0.42),
                              (-0.55, 0.05, -0.5, -0.22, 0.16)):
        b.box(x0, x1, y0, y1, 0, h, 'stone_light', bevel=0.012)
        a = x0 + 0.2
        while a < x1 - 0.1:
            b.box(a - 0.008, a + 0.008, y0 + 0.02, y1 - 0.02, h, h + 0.004, 'stone_mid')
            a += 0.22
        Facade(b, '-y', y0).rect(x0 + 0.02, x1 - 0.02, h * 0.5 - 0.006, h * 0.5 + 0.006, 0.004,
                                  'stone_mid')
    d.rock(b, -0.8, -0.2, 0.15, 0.3, 0.55, 'stone_dark', top='grass_dark', seed=2, rot=10)
    d.rock(b, -0.75, -0.66, 0.13, 0.14, 0.25, 'stone_mid', seed=8)
    b.box(-0.4, -0.16, -0.18, 0.04, 0.42, 0.56, 'stone_light', bevel=0.01)   # block being cut
    b.box(-0.15, -0.13, -0.18, 0.04, 0.42, 0.54, 'stone_dark')

    # crane: a mast with a raked stay, a jib, a rope and a block hanging from it
    mx, my = 0.55, 0.0
    b.box(mx - 0.035, mx + 0.035, my - 0.035, my + 0.035, 0, 1.25, 'wood_mid')
    b.rod((mx, my + 0.4, 0.01), (mx, my + 0.02, 1.0), 0.04, 'wood_dark')
    b.rod((mx + 0.03, my, 1.15), (0.22, -0.52, 1.02), 0.045, 'wood_mid')
    b.rod((mx, my, 1.25), (0.24, -0.51, 1.05), 0.012, 'burlap')
    b.box(0.215, 0.225, -0.525, -0.515, 0.42, 1.02, 'burlap')
    b.zbox(0.22, -0.52, 0.26, 0.42, 0.11, 0.09, 12, 'stone_light', bevel=0.012)
    b.hcylinder('y', (mx, my - 0.07, 0.35), 0.05, 0.07, 'wood_light', sides=6)   # winch drum
    b.box(mx - 0.09, mx + 0.09, my - 0.13, my - 0.11, 0.18, 0.46, 'wood_dark')
    # the treadwheel that turns the winch
    tw = Facade(b, '-y', my - 0.17)
    tw.disc(mx, 0.32, 0.29, 0.015, 'wood_dark', sides=12)
    tw.disc(mx, 0.32, 0.25, 0.02, 'wood_light', sides=12)
    tw.rect(mx - 0.24, mx + 0.24, 0.31, 0.33, 0.026, 'wood_dark')
    tw.rect(mx - 0.01, mx + 0.01, 0.08, 0.56, 0.026, 'wood_dark')
    tw.beam(mx - 0.17, 0.15, mx + 0.17, 0.49, 0.02, 0.026, 'wood_dark')
    tw.beam(mx - 0.17, 0.49, mx + 0.17, 0.15, 0.02, 0.026, 'wood_dark')
    tw.disc(mx, 0.32, 0.05, 0.034, 'iron', sides=6)

    # cut stone: a stack, a sledge with blocks, a few loose ones
    d.stone_pile(b, 0.6, -0.62, size=0.14)
    b.box(-0.05, 0.35, -0.95, -0.75, 0.03, 0.06, 'wood_mid')
    for x in (-0.05, 0.35):
        b.box(x - 0.015, x + 0.015, -0.97, -0.73, 0, 0.05, 'wood_dark')
    for x, a in ((0.06, 4), (0.24, -7)):
        b.zbox(x, -0.85, 0.06, 0.2, 0.08, 0.08, a, 'stone_light', bevel=0.01)
    d.pickaxe(b, 0.12, -0.3, lean_to_y=-0.2)
    d.stone_pile(b, -0.33, -0.76, size=0.12)
    d.lamp(b, -0.12, -0.9, h=0.5)

    # tool lean-to against the hill on the far right
    x0, x1, y0, y1 = 0.7, 0.92, -0.32, 0.25
    for py in (y0, y1):
        b.box(x0 - 0.03, x0 + 0.03, py - 0.03, py + 0.03, 0, 0.42, 'wood_dark')
    b.box(x1 - 0.04, x1, y0, y1, 0, 0.56, 'wood_light')
    d.shed_roof(b, 'y', y0 - 0.04, y1 + 0.04, x1, x0 - 0.06, 0.62, 0.44, **d.SLATE)
    d.barrel(b, 0.82, 0.08, r=0.07, h=0.18)
    return b


kit.run(OBJ, build, source_name="Quarry1-lvl1", offset=(LVL2_DX, 0, 0),
        collection="Производственные здания")
