"""OreVein<Kind> -- the ore deposits on the maps (the user, 2026-10-21): "на земле есть куски, где
светится соответствующая руда, на эти куски можно ставить здания добычи". Four of them, one per
ore, each a flat patch on a 2x2 plot -- the mine's own -- so the mine covers its vein.

A patch of broken ground, not a rock: bare dark earth with flat, irregular stones lying in it,
and the ore breaking through in low crystal clusters. The glow is the game's (a pulsing halo
under the patch, OreDepositSpawner); the crystals are what it shines from. The colours are the
mines' own (ironmine1.py etc.), so a vein and its mine read as the same ore. Earth and stones
are drawn plain (detail.OVERRIDE) -- the town's coursed-stone pattern made the first try look
like paving.

No placeholders of their own: each borrows the Iron Mine's 2x2 box and stands beside it.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import detail  # noqa: E402
import details as d  # noqa: E402
import kit  # noqa: E402
from kit import Builder  # noqa: E402

for plain in ('wood_dark', 'stone_dark', 'stone_mid', 'cobble', 'iron'):
    detail.OVERRIDE[plain] = detail.PLAIN

VEINS = (  # name, ore colour, offset along X from the box
    ("OreVeinIron", 'roof_slate', 3.0),
    ("OreVeinCoal", 'window', 5.5),
    ("OreVeinCopper", 'roof_orange', 8.0),
    ("OreVeinGold", 'gold', 10.5),
)

STONES = [  # x, y, radius x, radius y, height, colour, seed, turn
    (0.05, 0.02, 0.36, 0.3, 0.06, 'stone_dark', 1, 10),
    (-0.5, 0.25, 0.22, 0.18, 0.05, 'cobble', 2, 40),
    (0.5, 0.3, 0.2, 0.24, 0.05, 'stone_mid', 3, -20),
    (0.15, -0.52, 0.26, 0.17, 0.045, 'iron', 4, 15),
    (-0.42, -0.4, 0.17, 0.15, 0.04, 'stone_mid', 5, 60),
    (0.6, -0.32, 0.14, 0.13, 0.04, 'stone_dark', 6, 0),
    (-0.1, 0.6, 0.2, 0.14, 0.04, 'cobble', 7, -35),
]

CLUSTERS = [  # centre, number of crystals, size -- between and on the stones
    (0.0, 0.05, 5, 0.07), (-0.3, 0.05, 2, 0.045), (0.32, 0.12, 3, 0.05),
    (0.12, -0.32, 3, 0.05), (-0.36, -0.2, 2, 0.04), (0.42, -0.5, 2, 0.04),
    (-0.22, 0.48, 3, 0.045), (0.62, 0.05, 2, 0.04),
]


def make(ore):
    def build(lo, hi):
        b = Builder()
        # the bare earth, ragged at the edge so it sits in the grass rather than on it
        b.crag(0.0, 0.0, 0.86, 0.8, 0.012, 'wood_dark', sides=11, taper=0.98, seed=21)
        for x, y, rx, ry, h, col, seed, rot in STONES:
            b.crag(x, y, rx, ry, h, col, sides=6, taper=0.8, rot_deg=rot, seed=seed)
        for cx, cy, n, r in CLUSTERS:
            for k in range(n):
                a = k * 2.4 + cx * 7
                rr = 0.0 if k == 0 else r * 1.3
                size = r * (1.0 if k == 0 else 0.7)
                d.ore_vein(b, cx + math.cos(a) * rr, cy + math.sin(a) * rr, 0.03 + size * 1.5, ore, r=size)
        return b
    return build


for name, ore, dx in VEINS:
    kit.run(name, make(ore), source_name="IronMine1-lvl1", offset=(dx, 0, 0),
            collection="Месторождения")
