"""GoldMine1-lvl2 -- the Gold Mine (Золотой рудник) after its first upgrade. Footprint 2x2
(level 1's). Front is -Y.

Level 1 with red-and-gold banners either side of the portal, a gilded keystone, a second
strongbox of nuggets and a pennant on the sentry box. Level 1, for reference:


The richest mine wears it: the mouth is a dressed stone arch with a keystone, lanterns on both
sides and gold showing in the rock all round. A cart of gold on the track, an iron-bound chest
with its lid heaped with nuggets, a guard's sentry box with a slate roof and a spear.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import ADIT_H, ADIT_W  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "GoldMine1-lvl2"
LVL2_DX = 2.6   # level 2 stands this far +X of level 1
ORE = 'gold'


def build(lo, hi):
    b = Builder()
    d.rock_hill(b, -0.95, 0.95, -0.3, 0.95, 1.1, ore=ORE)
    yf, s = -0.24, -0.05
    d.rock(b, s, 0.1, 0.5, 0.32, 0.86, 'stone_mid', seed=6)

    # dressed stone portal: a wall face, an arch ring with a keystone, the dark mouth
    b.box(s - 0.36, s + 0.36, yf, yf + 0.3, 0, 0.82, 'stone_light', bevel=0.02)
    f = Facade(b, '-y', yf)
    f.arch(s, 0, ADIT_W + 0.12, ADIT_H + 0.08, 0.02, 'stone_mid')
    f.arch(s, 0, ADIT_W, ADIT_H, 0.025, 'window')
    f.poly([(s - 0.05, ADIT_H + 0.1), (s + 0.05, ADIT_H + 0.1), (s + 0.04, ADIT_H - 0.0),
            (s - 0.04, ADIT_H - 0.0)], 0.04, 'stone_light')
    f.rect(s - 0.36, s + 0.36, 0.76, 0.82, 0.03, 'stone_mid')
    f.poly([(s - 0.035, ADIT_H + 0.09), (s + 0.035, ADIT_H + 0.09), (s + 0.028, ADIT_H + 0.01),
            (s - 0.028, ADIT_H + 0.01)], 0.05, 'gold')
    for k in (-1, 1):
        d.banner(f, s + k * 0.3, 0.76, 0.26, w=0.1, color='banner_red')
    for k in (-1, 1):
        d.lamp(b, s + k * 0.3, yf - 0.1, h=0.55)

    d.rails(b, s, -0.98, yf)
    d.minecart(b, s, -0.64, ORE)

    # strongbox heaped with nuggets
    cx, cy = -0.6, -0.62
    b.zbox(cx, cy, 0, 0.16, 0.15, 0.1, 8, 'wood_mid', bevel=0.01)
    for t in (-0.09, 0.09):
        b.zbox(cx + t * 0.99, cy + t * 0.14, 0, 0.165, 0.018, 0.105, 8, 'iron')
    b.blob(cx, cy, 0.15, 0.12, 0.09, ORE, sides=5)
    d.ore_heap(b, -0.75, -0.3, r=0.11, color=ORE, second='stone_mid')
    cx, cy = -0.32, -0.86   # a second strongbox
    b.zbox(cx, cy, 0, 0.13, 0.12, 0.08, -10, 'wood_mid', bevel=0.01)
    b.zbox(cx, cy, 0.0, 0.135, 0.014, 0.085, -10, 'iron')
    b.blob(cx, cy, 0.12, 0.1, 0.07, ORE, sides=5)

    # sentry box
    x0, x1, y0, y1 = 0.5, 0.86, -0.62, -0.26
    b.box(x0, x1, y0 + 0.08, y1, 0, 0.62, 'wood_light', bevel=d.WALL_BEVEL)
    for px in (x0, x1):
        b.box(px - 0.03, px + 0.03, y0 + 0.05, y0 + 0.11, 0, 0.66, 'wood_dark')
    sf = Facade(b, '-y', y0 + 0.08)
    sf.rect(x0 + 0.08, x1 - 0.08, 0.0, 0.48, 0.01, 'window')
    sf.rect(x0 + 0.05, x1 - 0.05, 0.48, 0.52, 0.02, 'wood_dark')
    d.shed_roof(b, 'x', x0 - 0.04, x1 + 0.04, y1 + 0.03, y0 - 0.02, 0.82, 0.66, **d.SLATE)
    b.rod((x0 - 0.06, y0 + 0.04, 0.01), (x0 - 0.04, y0 + 0.08, 0.7), 0.02, 'wood_light')
    b.pyramid(x0 - 0.04, y0 + 0.08, 0.7, 0.02, 0.02, 0.78, 'iron')
    d.sack(b, 0.75, -0.82)
    b.box(x1 - 0.01, x1 + 0.01, y1 - 0.01, y1 + 0.01, 0.8, 1.08, 'wood_dark')
    b.prism([(x1 - 0.01, 1.07), (x1 - 0.01, 0.96), (x1 - 0.18, 1.015)], (0, y1 + 0.005, 0),
            (1, 0, 0), (0, 0, 1), 0.01, 'banner_red', back_cap=True)
    return b


kit.run(OBJ, build, source_name="GoldMine1-lvl1", offset=(LVL2_DX, 0, 0),
        collection="Производственные здания")
