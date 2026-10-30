"""The dungeon entrance as a MINE in the mountain (the user, 2026-10-30: likes the mine of
cave_entrance.py, "but it still looks rather dull"). Three richer takes on it, same mountain:

- MineWorking: a working mine -- a heavy timber portal with lanterns, a winch on the brow with a
  bucket on its rope, ore veins glinting in the rock, a loaded cart on the rails, crates and
  barrels, a signpost.
- MineAbandoned: an abandoned mine -- the beam broken and sagging, the mouth boarded across with a
  gap torn in it, cobwebs, a skull sign, broken rails, a tipped cart spilling gold, a miner's
  skeleton against the rock, ivy over everything.
- MineHeadframe: a mine with a hoist -- a timber headframe tower with its wheel beside the
  mountain, a string of lanterns over the mouth, rails out to a spoil heap where a cart tips, a
  campfire with a pot, barrels.

    blender --background Buildings-Size1-Models.blend --python mine_entrance.py
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import cave_entrance as cave  # noqa: E402  (sets the rough-rock pattern override too)
from cave_entrance import BONE, DARK, LEAF, MOUTH_Y, STONE, STONE_D, TURF, TURF_D, bush, front, mountain, skull  # noqa: E402
from kit import Builder  # noqa: E402

WOOD, WOOD_D, WOOD_L = 'wood_mid', 'wood_dark', 'wood_light'


def portal(b, broken=False):
    """Timber props at the mouth: two posts, a beam (snapped and sagging when broken), struts."""
    for x in (-0.58, 0.58):
        b.box(x - 0.08, x + 0.08, -1.22, -1.06, 0.0, 1.08, WOOD)
        b.box(x - 0.1, x + 0.1, -1.24, -1.04, 0.0, 0.08, STONE_D)          # a stone under each post
    if broken:
        b.obox((-0.33, -1.13, 1.05), (0.96, 0, -0.28), (0, 1, 0), (0.28, 0, 0.96), 0.4, 0.09, 0.07, WOOD_D)
        b.obox((0.36, -1.13, 0.98), (0.93, 0, 0.37), (0, 1, 0), (-0.37, 0, 0.93), 0.38, 0.09, 0.07, WOOD_D)
    else:
        b.box(-0.76, 0.76, -1.24, -1.02, 1.08, 1.24, WOOD_D)
        for s in (-1, 1):
            b.rod((s * 0.58, -1.14, 0.78), (s * 0.3, -1.14, 1.08), 0.06, WOOD)


def lantern(b, x, y, z, hang=0.12):
    b.rod((x, y, z), (x, y, z - hang), 0.015, 'iron')
    b.box(x - 0.05, x + 0.05, y - 0.05, y + 0.05, z - hang - 0.14, z - hang, 'window_light')
    b.box(x - 0.06, x + 0.06, y - 0.06, y + 0.06, z - hang, z - hang + 0.03, 'iron')
    b.pyramid(x, y, z - hang + 0.03, 0.05, 0.05, z - hang + 0.08, 'iron')


def rails(b, y0, y1, x=0.0, broken=False):
    n = int((y1 - y0) / 0.22)
    for k in range(n):
        y = y0 + k * 0.22
        if broken and k in (1, 4):
            b.zbox(x + 0.05, y + 0.04, 0.0, 0.04, 0.3, 0.04, 20, WOOD_D)    # a sleeper knocked askew
        else:
            b.box(x - 0.3, x + 0.3, y, y + 0.08, 0.0, 0.04, WOOD_D)
    for dx in (-0.2, 0.2):
        if broken and dx > 0:
            b.box(x + dx - 0.02, x + dx + 0.02, y0 + 0.5, y1, 0.04, 0.07, 'iron')
            b.obox((x + dx + 0.08, y0 + 0.2, 0.08), (0.3, 0.95, 0.15), (-0.95, 0.3, 0), (0, 0, 1), 0.25, 0.02, 0.02,
                   'iron')                                                  # a rail torn up
        else:
            b.box(x + dx - 0.02, x + dx + 0.02, y0, y1, 0.04, 0.07, 'iron')


def cart(b, x, y, z=0.07, load='gold'):
    b.box(x - 0.24, x + 0.24, y - 0.3, y + 0.3, z + 0.08, z + 0.36, WOOD, bevel=0.02)
    b.box(x - 0.26, x + 0.26, y - 0.32, y + 0.32, z + 0.32, z + 0.37, 'iron')
    for dy in (-0.18, 0.18):
        for dx in (-0.25, 0.25):
            b.hcylinder('x', (x + dx, y + dy, z + 0.09), 0.025, 0.09, 'iron', sides=8)
    if load:
        b.pyramid(x, y, z + 0.36, 0.2, 0.26, z + 0.52, load, sides=6, rot_deg=0)
        for i, (dx, dy) in enumerate(((-0.08, -0.1), (0.1, 0.08))):
            b.pyramid(x + dx, y + dy, z + 0.4, 0.05, 0.05, z + 0.58, 'water' if i else 'gold', sides=4)


def crate(b, x, y, z=0.0, s=0.3, a=0):
    b.zbox(x, y, z, z + s, s / 2, s / 2, a, WOOD_L, bevel=0.015)
    b.zbox(x, y, z + s * 0.45, z + s * 0.55, s / 2 + 0.01, s / 2 + 0.01, a, WOOD_D)


def barrel(b, x, y, z=0.0, r=0.14, h=0.36):
    b.cylinder(x, y, z, z + h, r, WOOD, sides=8)
    for f in (0.15, 0.8):
        b.cylinder(x, y, z + h * f, z + h * f + 0.03, r + 0.012, 'iron', sides=8)


def gem(b, x, y, z, color='gold', s=0.07):
    """An ore crystal standing proud of the rock face."""
    b.obox((x, y, z), (0.7, 0, 0.7), (0, 1, 0), (-0.7, 0, 0.7), s, s * 0.8, s, color)


def veins(b):
    for x, y, z, c in ((-1.0, -1.2, 0.45, 'gold'), (-0.86, -1.18, 0.75, 'water'), (-1.1, -1.12, 0.95, 'gold'),
                       (0.98, -1.12, 0.4, 'gold'), (1.1, -1.05, 0.7, 'water'), (0.85, -1.1, 0.85, 'gold'),
                       (-1.55, 0.05, 0.6, 'gold'), (1.55, 0.0, 0.5, 'water')):
        gem(b, x, y, z, c)


def mine_working():
    b = Builder()
    mountain(b)
    portal(b)
    lantern(b, -0.58, -1.3, 1.0)
    lantern(b, 0.58, -1.3, 1.0)
    veins(b)
    # the winch on the brow: an A-frame each side, an axle, a wheel, the rope, a bucket
    for x in (-0.5, 0.5):
        b.rod((x, -0.55, 1.45), (x, -0.95, 2.05), 0.05, WOOD)
        b.rod((x, -1.0, 1.45), (x, -0.95, 2.05), 0.05, WOOD)
    b.hcylinder('x', (0.0, -0.95, 2.05), 0.56, 0.035, 'iron', sides=6)
    b.hcylinder('x', (0.22, -0.95, 2.05), 0.04, 0.2, WOOD_D, sides=10)
    b.hcylinder('x', (0.47, -0.95, 2.05), 0.05, 0.03, WOOD_D)
    b.rod((0.62, -0.95, 2.05), (0.62, -0.8, 1.88), 0.03, WOOD_L)               # the crank
    b.rod((0.22, -1.15, 2.05), (0.22, -1.15, 1.42), 0.012, 'burlap')
    b.cylinder(0.22, -1.15, 1.26, 1.42, 0.07, WOOD, sides=7)
    b.cylinder(0.22, -1.15, 1.38, 1.41, 0.075, 'iron', sides=7)
    # rails out with a loaded cart on them
    rails(b, -2.0, -0.95)
    cart(b, 0.0, -1.62)
    # crates and barrels to the left, a signpost to the right
    crate(b, -1.25, -1.55, 0.0, 0.32, 10)
    crate(b, -1.0, -1.3, 0.0, 0.28, -15)
    crate(b, -1.2, -1.5, 0.32, 0.24, 30)
    barrel(b, -1.6, -1.15)
    barrel(b, -1.5, -1.5, 0.0, 0.12, 0.3)
    b.box(1.33, 1.39, -1.63, -1.57, 0.0, 0.85, WOOD)
    b.zbox(1.36, -1.66, 0.6, 0.78, 0.26, 0.02, -8, WOOD_L)
    b.rod((1.2, -1.69, 0.62), (1.52, -1.69, 0.76), 0.025, WOOD_D)              # a pick crossed over it
    b.obox((1.5, -1.7, 0.76), (0.4, 0, 0.92), (0, 1, 0), (-0.92, 0, 0.4), 0.09, 0.02, 0.02, 'iron')
    b.rod((1.6, -1.25, 0.0), (1.85, -1.0, 0.05), 0.04, WOOD_L)                 # a shovel lying
    b.zbox(1.9, -0.95, 0.0, 0.03, 0.08, 0.06, 45, 'iron')
    return b


def mine_abandoned():
    b = Builder()
    mountain(b, hollow=True)
    portal(b, broken=True)
    # inside the tunnel: a second set of props, the rails running on in, a lantern still burning deep in
    for x in (-0.31, 0.31):
        b.box(x - 0.05, x + 0.05, -0.84, -0.76, 0.0, 0.7, WOOD_D)
    b.box(-0.36, 0.36, -0.84, -0.76, 0.66, 0.76, WOOD_D)
    for k in range(3):
        y = -0.95 + k * 0.11
        b.box(-0.24, 0.24, y, y + 0.05, 0.0, 0.03, WOOD_D)
    for dx in (-0.16, 0.16):
        b.box(dx - 0.015, dx + 0.015, -0.98, -0.63, 0.03, 0.05, 'iron')
    lantern(b, 0.1, -0.7, 0.52, 0.06)
    # boards nailed across, one torn away
    b.obox((0.0, -1.27, 0.55), (0.87, 0, 0.5), (0, 1, 0), (-0.5, 0, 0.87), 0.62, 0.03, 0.07, WOOD_L)
    b.obox((-0.25, -1.29, 0.3), (0.98, 0, -0.17), (0, 1, 0), (0.17, 0, 0.98), 0.4, 0.03, 0.07, WOOD)
    b.obox((0.65, -1.45, 0.06), (0.9, -0.43, 0), (0.43, 0.9, 0), (0, 0, 1), 0.38, 0.07, 0.03, WOOD_L)   # the torn board
    # cobwebs in the corners
    front(b, [(-0.5, 1.0), (-0.2, 1.0), (-0.5, 0.72)], MOUTH_Y - 0.12, 0.01, 'plaster_white')
    front(b, [(0.5, 0.95), (0.25, 0.95), (0.5, 0.7)], MOUTH_Y - 0.12, 0.01, 'plaster_white')
    # a warning sign: a board with a skull
    b.box(-1.37, -1.31, -1.63, -1.57, 0.0, 0.8, WOOD_D)
    b.obox((-1.34, -1.66, 0.65), (0.98, 0, 0.17), (0, 1, 0), (-0.17, 0, 0.98), 0.24, 0.02, 0.14, WOOD_L)
    skull(b, -1.34, -1.66, 0.58, 0.14)
    # broken rails, a tipped cart spilling gold
    rails(b, -2.0, -0.95, broken=True)
    b.obox((0.95, -1.65, 0.2), (0.94, 0.34, 0), (-0.24, 0.66, 0.71), (0.24, -0.66, 0.71), 0.28, 0.2, 0.18, WOOD, bevel=0.02)
    for i, (x, y) in enumerate(((0.85, -1.95), (1.05, -2.0), (1.25, -1.9), (0.7, -1.85), (1.15, -2.1))):
        b.cylinder(x, y, 0.0, 0.025, 0.05, 'gold', sides=6)
    # a miner's skeleton sat against the rock, his pick and lantern beside him
    sx, sy = -0.95, -1.28
    skull(b, sx, sy - 0.02, 0.42, 0.12)
    b.box(sx - 0.08, sx + 0.08, sy - 0.04, sy + 0.04, 0.12, 0.4, BONE)
    for z in (0.2, 0.28, 0.36):
        b.box(sx - 0.1, sx + 0.1, sy - 0.07, sy - 0.03, z, z + 0.03, BONE)
    for dx in (-0.06, 0.06):
        b.rod((sx + dx, sy, 0.13), (sx + dx, sy - 0.3, 0.03), 0.03, BONE)
    b.rod((sx + 0.25, sy - 0.25, 0.02), (sx + 0.4, sy + 0.05, 0.02), 0.025, WOOD_D)
    b.obox((sx + 0.25, sy - 0.25, 0.03), (0.9, -0.43, 0), (0.43, 0.9, 0), (0, 0, 1), 0.09, 0.02, 0.02, 'iron')
    b.obox((sx - 0.3, sy - 0.3, 0.06), (1, 0, 0), (0, 0, 1), (0, -1, 0), 0.05, 0.07, 0.05, 'window_light')
    # overgrown: ivy over the props, bushes at the foot
    for x in (-0.66, 0.5):
        b.box(x, x + 0.05, -1.25, -1.21, 0.3, 1.0, LEAF)
    for x, y, r in ((0.55, -1.25, 0.15), (-0.45, -1.35, 0.12), (1.5, -0.95, 0.18)):
        bush(b, x, y, 0.0, r, TURF_D)
    return b


def mine_headframe():
    b = Builder()
    mountain(b)
    portal(b)
    # a string of lanterns over the mouth
    b.rod((-0.7, -1.3, 1.24), (0.7, -1.3, 1.24), 0.012, 'burlap')
    for x in (-0.35, 0.0, 0.35):
        lantern(b, x, -1.3, 1.24, 0.04)
    veins(b)
    # the headframe tower: four legs leaning in, braces, a platform, the wheel on top
    cx, cy, base, top, h = 1.45, -1.3, 0.38, 0.2, 2.3
    legs = [(sx, sy) for sx in (-1, 1) for sy in (-1, 1)]
    for sx, sy in legs:
        b.rod((cx + sx * base, cy + sy * base, 0.0), (cx + sx * top, cy + sy * top, h), 0.07, WOOD)
    for z, r in ((0.7, 0.33), (1.4, 0.27)):
        for (ax, ay), (bx, by) in (((-1, -1), (1, -1)), ((1, -1), (1, 1)), ((1, 1), (-1, 1)), ((-1, 1), (-1, -1))):
            b.rod((cx + ax * r, cy + ay * r, z), (cx + bx * r, cy + by * r, z), 0.05, WOOD_D)
    b.rod((cx - 0.35, cy - 0.35, 0.05), (cx + 0.27, cy - 0.27, 1.4), 0.04, WOOD_D)
    b.box(cx - 0.28, cx + 0.28, cy - 0.28, cy + 0.28, h, h + 0.06, WOOD_L)
    b.hcylinder('y', (cx, cy, h + 0.32), 0.04, 0.28, WOOD_D, sides=10)
    b.hcylinder('y', (cx, cy, h + 0.32), 0.07, 0.05, 'iron', sides=6)
    for k in range(4):                                                       # spokes
        a = k * math.pi / 4
        b.obox((cx, cy, h + 0.32), (math.cos(a), 0, math.sin(a)), (0, 1, 0), (-math.sin(a), 0, math.cos(a)), 0.26, 0.05,
               0.02, WOOD)
    b.rod((cx + 0.28, cy, h + 0.32), (cx + 0.28, cy, 0.4), 0.015, 'burlap')   # the rope down the shaft
    b.box(cx + 0.18, cx + 0.38, cy - 0.1, cy + 0.1, 0.25, 0.4, WOOD, bevel=0.01)   # the cage on it
    # rails out of the mouth to a spoil heap where a cart tips its load
    rails(b, -1.75, -0.95)
    b.crag(-1.1, -1.55, 0.55, 0.4, 0.42, STONE_D, sides=7, taper=0.3, seed=5)
    b.obox((-0.55, -1.6, 0.35), (0.94, -0.34, 0), (0.24, 0.66, 0.71), (-0.24, -0.66, 0.71), 0.24, 0.2, 0.16, WOOD,
           bevel=0.02)
    # a campfire with a pot, barrels by the rock
    fx, fy = 0.5, -1.85
    for a in (0, 60, 120):
        r = math.radians(a)
        b.rod((fx - math.cos(r) * 0.18, fy - math.sin(r) * 0.18, 0.03), (fx + math.cos(r) * 0.18, fy + math.sin(r) * 0.18, 0.03),
              0.05, WOOD_D)
    b.pyramid(fx, fy, 0.04, 0.1, 0.1, 0.3, 'fire', sides=5)
    for s in (-1, 1):
        b.rod((fx + s * 0.22, fy, 0.0), (fx + s * 0.22, fy, 0.45), 0.03, WOOD)
    b.rod((fx - 0.24, fy, 0.45), (fx + 0.24, fy, 0.45), 0.02, 'iron')
    b.cylinder(fx, fy, 0.28, 0.42, 0.09, 'iron', sides=8)
    barrel(b, -1.65, -0.9)
    barrel(b, -1.55, -0.55, 0.0, 0.12, 0.3)
    return b


MODELS = {"MineWorking": mine_working, "MineAbandoned": mine_abandoned, "MineHeadframe": mine_headframe}

cave.main(MODELS, row_y=-68.0)
