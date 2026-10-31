"""The dungeon camp's two buildings, made apart for the user to approve before they go into the
map (the user, 2026-10-31: «make the hirelings' camp and the shop separately, I want to approve
them, then you'll add them»). Metres, the front faces -y (the game camera).

- DungeonHireCamp: the gnome prospectors' camp, where up to five hirelings are hired. A squat
  gnome hut under a steep red roof with a round door, and one corner for each of the three
  hirelings so the player reads what is on offer: a pick rack and an ore cart (the MINER), a
  straw target stuck with bolts and a crossbow rack (the CROSSBOWMAN), a stone guardian on a
  plinth (the ORE GUARDIAN). A blue banner with a gold hammer, a hiring board, barrels, logs.
- DungeonPotionShop: the potion stall. A wagon-shop under a striped awning; the four potions of
  the shop -- healing (red), rage (orange), speed (green), defence (blue) -- as big bottles on the
  counter and rows on the shelves behind; a cauldron, a hanging bottle sign, crates of bottles.

No flames and no glow: fire is a Unity effect (the user, 2026-10-31).

    blender --background Buildings-Size1-Models.blend --python dungeon_camp.py
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import cave_entrance as cave  # noqa: E402  (the rough-rock override, the «Подземелье» collection)
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402

WOOD, WOOD_D, WOOD_L = 'wood_mid', 'wood_dark', 'wood_light'
STONE, STONE_D, STONE_L = 'stone_mid', 'stone_dark', 'stone_light'
POTIONS = (('banner_red', 'healing'), ('fire', 'rage'), ('grass', 'speed'), ('banner_blue', 'defence'))


def barrel(b, x, y, z=0.0, r=0.3, h=0.7):
    b.cylinder(x, y, z, z + h, r, WOOD, sides=8)
    for zz in (0.15, 0.85):
        b.cylinder(x, y, z + h * zz - 0.03, z + h * zz + 0.03, r + 0.02, 'iron', sides=8)


def crate(b, x, y, z=0.0, s=0.55, a=0.0):
    b.zbox(x, y, z, z + s, s / 2, s / 2, a, WOOD_L)
    b.zbox(x, y, z + s * 0.42, z + s * 0.58, s / 2 + 0.02, s / 2 + 0.02, a, WOOD_D)


def log_seat(b, x, y, axis='x', half=0.7):
    b.hcylinder(axis, (x, y, 0.16), half, 0.16, WOOD, sides=7)


def bottle(b, x, y, z, colour, s=1.0):
    """A round-bellied potion: the coloured body, a pale neck, a cork."""
    b.ball(x, y, z, 0.11 * s, 0.2 * s, colour, sides=7, top='white')
    b.cylinder(x, y, z + 0.18 * s, z + 0.28 * s, 0.04 * s, 'plaster_white', sides=6)
    b.cylinder(x, y, z + 0.28 * s, z + 0.33 * s, 0.05 * s, WOOD_L, sides=6)


# -- the hirelings' camp ---------------------------------------------------------------------

def gnome_hut(b, x, y):
    """Squat and round-doored, under a steep roof: a gnome builds low and roofs high."""
    b.box(x - 1.5, x + 1.5, y - 1.2, y + 1.2, 0.0, 0.35, STONE_L, bevel=0.04)
    b.box(x - 1.35, x + 1.35, y - 1.05, y + 1.05, 0.35, 1.45, 'plaster', bevel=0.02)
    for px in (-1.35, 1.35):
        for py in (-1.05, 1.05):
            b.box(x + px - 0.09, x + px + 0.09, y + py - 0.09, y + py + 0.09, 0.35, 1.45, WOOD_D)
    b.box(x - 1.4, x + 1.4, y - 1.12, y - 1.02, 0.9, 1.0, WOOD_D)
    b.gable_roof(x, y, 'x', 1.7, 1.4, 1.4, 2.9, 'roof_red', 'roof_red_dark')
    b.hcylinder('y', (x + 0.45, y - 1.08, 0.75), 0.05, 0.38, 'door', sides=10)    # the round door
    b.box(x + 0.07, x + 0.83, y - 1.13, y - 1.03, 0.37, 0.75, 'door')
    b.cylinder(x + 0.72, y - 1.16, 0.6, 0.66, 0.04, 'gold', sides=6)              # its knob
    b.box(x - 0.95, x - 0.35, y - 1.1, y - 1.04, 0.7, 1.15, 'window_light')
    b.box(x - 1.0, x - 0.3, y - 1.14, y - 1.06, 0.64, 0.7, WOOD_D)
    b.box(x - 0.85, x - 0.45, y + 0.35, y + 0.75, 1.9, 3.1, STONE_D)              # the chimney
    b.box(x - 0.9, x - 0.4, y + 0.3, y + 0.8, 3.1, 3.2, STONE)


def pick_rack(b, x, y):
    """The MINER's corner: picks and shovels in a rack, a little ore cart beside it."""
    b.box(x - 0.8, x + 0.8, y - 0.06, y + 0.06, 0.0, 0.1, WOOD_D)
    for px in (-0.8, 0.8):
        b.box(x + px - 0.06, x + px + 0.06, y - 0.06, y + 0.06, 0.0, 1.2, WOOD_D)
    b.box(x - 0.85, x + 0.85, y - 0.07, y + 0.07, 1.0, 1.1, WOOD)
    for k in range(4):
        px = x - 0.55 + k * 0.37
        b.rod((px, y - 0.1, 0.1), (px, y - 0.1, 1.15), 0.05, WOOD_L)               # the haft
        if k % 2 == 0:                                                             # a pick head
            b.rod((px - 0.25, y - 0.12, 1.05), (px + 0.25, y - 0.12, 1.15), 0.05, 'iron')
        else:                                                                      # a shovel blade
            b.box(px - 0.1, px + 0.1, y - 0.13, y - 0.09, 0.05, 0.35, 'iron')
    cx, cy = x + 1.5, y - 0.5                                                      # the ore cart
    b.box(cx - 0.45, cx + 0.45, cy - 0.32, cy + 0.32, 0.2, 0.68, WOOD, bevel=0.03)
    b.box(cx - 0.47, cx + 0.47, cy - 0.34, cy + 0.34, 0.64, 0.7, 'iron')
    for sx in (-0.3, 0.3):
        for sy in (-0.34, 0.34):
            b.hcylinder('y', (cx + sx, cy + sy, 0.15), 0.03, 0.15, 'iron', sides=8)
    b.ball(cx, cy, 0.55, 0.35, 0.3, STONE_D, sides=6, top=STONE)
    for k in range(3):
        b.crag(cx - 0.2 + k * 0.2, cy + 0.05 * k, 0.07, 0.06, 0.12, 'gold', sides=5, seed=k, rot_deg=k * 40)
    b.rod((x + 0.9, y - 0.9, 0.02), (x + 2.6, y - 0.9, 0.02), 0.05, 'iron')        # a stub of rail
    b.rod((x + 0.9, y - 0.1, 0.02), (x + 2.6, y - 0.1, 0.02), 0.05, 'iron')


def target_range(b, x, y):
    """The CROSSBOWMAN's corner: a straw target stuck with bolts, a rack of crossbows."""
    for px in (-0.45, 0.45):
        b.rod((x + px, y + 0.15, 0.0), (x + px * 0.6, y, 1.4), 0.07, WOOD_D)
    b.rod((x, y + 0.5, 0.0), (x, y + 0.05, 1.2), 0.06, WOOD_D)
    for r, colour, dy in ((0.55, 'hay', 0.0), (0.42, 'banner_red', -0.04), (0.28, 'white', -0.07), (0.13, 'banner_red', -0.1)):
        b.hcylinder('y', (x, y - 0.02 + dy, 1.0), 0.04, r, colour, sides=12)
    for k, (bx, bz) in enumerate(((0.12, 1.1), (-0.2, 0.92), (0.25, 0.8))):        # bolts in it
        b.rod((x + bx, y - 0.12, bz), (x + bx + 0.03, y - 0.5, bz + 0.05), 0.025, WOOD_L)
        b.box(x + bx - 0.04, x + bx + 0.07, y - 0.52, y - 0.46, bz + 0.0, bz + 0.1, 'white')
    rx, ry = x + 1.4, y + 0.1                                                      # the crossbow rack
    b.box(rx - 0.6, rx + 0.6, ry - 0.08, ry + 0.08, 0.0, 0.08, WOOD_D)
    for px in (-0.6, 0.6):
        b.box(rx + px - 0.05, rx + px + 0.05, ry - 0.05, ry + 0.05, 0.0, 1.0, WOOD_D)
    b.box(rx - 0.65, rx + 0.65, ry - 0.06, ry + 0.06, 0.92, 1.0, WOOD)
    for k in range(3):
        px = rx - 0.35 + k * 0.35
        b.rod((px, ry - 0.1, 0.12), (px, ry - 0.1, 0.9), 0.05, WOOD)               # the stock
        b.rod((px - 0.22, ry - 0.14, 0.8), (px + 0.22, ry - 0.14, 0.8), 0.04, 'iron')   # the bow
        b.rod((px - 0.22, ry - 0.14, 0.8), (px, ry - 0.14, 0.62), 0.012, 'white')
        b.rod((px + 0.22, ry - 0.14, 0.8), (px, ry - 0.14, 0.62), 0.012, 'white')
    b.box(rx + 0.75, rx + 1.05, ry - 0.2, ry + 0.1, 0.0, 0.55, WOOD, bevel=0.02)   # a quiver box
    for k in range(4):
        b.rod((rx + 0.8 + k * 0.06, ry - 0.08, 0.5), (rx + 0.8 + k * 0.06, ry - 0.08, 0.75), 0.02, WOOD_L)


def guardian(b, x, y):
    """The ORE GUARDIAN's corner: a squat stone golem waiting on a plinth, ore in his fists."""
    b.box(x - 0.8, x + 0.8, y - 0.7, y + 0.7, 0.0, 0.3, STONE_D, bevel=0.05)
    b.box(x - 0.7, x + 0.7, y - 0.6, y + 0.6, 0.3, 0.42, STONE, bevel=0.03)
    for s in (-1, 1):                                                              # legs
        b.ball(x + s * 0.28, y, 0.42, 0.22, 0.5, STONE_L, sides=6)
    b.ball(x, y, 0.85, 0.55, 0.9, STONE_L, sides=7, top=STONE)                     # the body
    b.ball(x, y - 0.12, 1.6, 0.3, 0.35, STONE_L, sides=6)                          # the head
    for s in (-1, 1):
        b.box(x + s * 0.12 - 0.05, x + s * 0.12 + 0.05, y - 0.43, y - 0.38, 1.72, 1.8, 'water')   # glowing eyes
        b.ball(x + s * 0.7, y - 0.05, 1.05, 0.25, 0.55, STONE_L, sides=6)          # shoulders
        b.ball(x + s * 0.78, y - 0.1, 0.55, 0.24, 0.45, STONE, sides=6)            # fists
        b.crag(x + s * 0.78, y - 0.35, 0.09, 0.07, 0.16, 'gold', sides=5, seed=7 + s, rot_deg=30)
    for k in range(4):                                                             # moss on him
        a = k * 1.7
        b.box(x + math.cos(a) * 0.4 - 0.12, x + math.cos(a) * 0.4 + 0.12, y + math.sin(a) * 0.3 - 0.1,
              y + math.sin(a) * 0.3 + 0.1, 1.45, 1.55, 'grass')


def hire_board(b, x, y):
    for px in (-0.55, 0.55):
        b.box(x + px - 0.06, x + px + 0.06, y - 0.06, y + 0.06, 0.0, 1.6, WOOD_D)
    b.box(x - 0.65, x + 0.65, y - 0.05, y + 0.05, 0.7, 1.45, WOOD)
    b.gable_roof(x, y, 'x', 0.78, 0.22, 1.55, 1.8, 'roof_red', 'roof_red_dark')
    for k, (px, pz) in enumerate(((-0.35, 1.2), (0.05, 1.25), (0.35, 1.05), (-0.25, 0.9), (0.2, 0.85))):
        b.box(x + px - 0.13, x + px + 0.13, y - 0.08, y - 0.05, pz - 0.15, pz + 0.12, 'plaster_white' if k % 2 else 'burlap')


def banner(b, x, y):
    b.rod((x, y, 0.0), (x, y, 2.8), 0.08, WOOD_D)
    b.rod((x - 0.05, y, 2.65), (x + 0.75, y, 2.65), 0.05, WOOD_D)
    b.box(x + 0.05, x + 0.7, y - 0.02, y + 0.02, 1.6, 2.6, 'banner_blue')
    b.prism([(0.05, 1.6), (0.7, 1.6), (0.375, 1.38)], (x, y, 0), (1, 0, 0), (0, 0, 1), 0.04, 'banner_blue')
    b.box(x + 0.32, x + 0.42, y - 0.04, y - 0.02, 1.85, 2.3, 'gold')               # a gold hammer
    b.box(x + 0.2, x + 0.55, y - 0.04, y - 0.02, 2.22, 2.38, 'gold')
    b.ball(x, y, 2.8, 0.08, 0.12, 'gold', sides=6)


def hire_camp():
    b = Builder()
    b.cylinder(0.0, 0.0, -0.06, 0.02, 4.6, 'cobble', sides=12)                  # trodden ground
    gnome_hut(b, 0.0, 1.6)
    pick_rack(b, -3.3, 1.2)
    target_range(b, 2.6, 1.3)
    guardian(b, 0.0, -2.3)
    hire_board(b, -1.6, -1.2)
    banner(b, 1.7, -0.9)
    for x, y in ((-3.6, -1.1), (-3.0, -1.6)):
        barrel(b, x, y)
    crate(b, 3.4, -1.2, a=20)
    crate(b, 3.9, -1.5, s=0.45, a=-10)
    crate(b, 3.55, -1.3, z=0.55, s=0.4, a=35)
    log_seat(b, -1.5, -2.6)
    log_seat(b, 1.6, -2.7, 'y', 0.55)
    for k in range(7):                                                             # a cold fire ring
        a = k * 2 * math.pi / 7
        b.crag(-0.2 + math.cos(a) * 0.4, -3.6 + math.sin(a) * 0.4, 0.1, 0.08, 0.1, STONE_L, sides=5, seed=k)
    for a in (0, 60, 120):
        r = math.radians(a)
        b.rod((-0.2 - math.cos(r) * 0.3, -3.6 - math.sin(r) * 0.3, 0.05), (-0.2 + math.cos(r) * 0.3, -3.6 + math.sin(r) * 0.3, 0.05), 0.08, WOOD_D)
    return b


# -- the potion shop -------------------------------------------------------------------------

def potion_shop():
    b = Builder()
    b.cylinder(0.0, 0.0, -0.06, 0.02, 3.4, 'cobble', sides=12)
    # the wagon: a bed on wheels, the shop built on it
    b.box(-1.7, 1.7, -0.2, 1.3, 0.45, 0.6, WOOD_D)
    for sx in (-1.2, 1.2):
        for sy in (-0.25, 1.35):
            b.hcylinder('y', (sx, sy, 0.45), 0.06, 0.45, WOOD, sides=10)
            b.hcylinder('y', (sx, sy, 0.45), 0.07, 0.1, 'iron', sides=6)
    b.rod((1.7, 0.55, 0.5), (2.6, 0.55, 0.15), 0.07, WOOD)                       # the shaft
    # the back wall with its shelves of potions
    b.box(-1.6, 1.6, 1.0, 1.2, 0.6, 2.4, WOOD)
    for z in (1.05, 1.55, 2.0):
        b.box(-1.55, 1.55, 0.75, 1.0, z - 0.05, z, WOOD_L)
        for k in range(10):
            colour = POTIONS[(k + int(z * 3)) % 4][0]
            bottle(b, -1.35 + k * 0.3, 0.88, z, colour, 0.85)
    for px in (-1.6, 1.6):
        b.box(px - 0.08, px + 0.08, -0.15, 0.05, 0.6, 2.6, WOOD_D)                # front posts
        b.box(px - 0.08, px + 0.08, 1.05, 1.25, 0.6, 2.6, WOOD_D)
    # the striped awning, sloping to the front
    for k in range(8):
        x0 = -1.8 + k * 0.45
        colour = 'plaster_white' if k % 2 else 'roof_green'
        b.obox((x0 + 0.225, 0.3, 2.65), (1, 0, 0), (0, 0.94, 0.34), (0, -0.34, 0.94), 0.225, 0.95, 0.03, colour)
    for k in range(8):                                                             # its scalloped edge
        b.prism([(-1.8 + k * 0.45, 0.0), (-1.35 + k * 0.45, 0.0), (-1.575 + k * 0.45, -0.18)],
                (0, -0.6, 2.32), (1, 0, 0), (0, 0, 1), 0.02, 'roof_green' if k % 2 else 'plaster_white')
    # the counter with the four big potions of the shop on it
    b.box(-1.5, 1.5, -0.2, 0.25, 0.6, 1.25, WOOD, bevel=0.02)
    b.box(-1.6, 1.6, -0.28, 0.3, 1.25, 1.33, WOOD_L, bevel=0.02)
    for k, (colour, _what) in enumerate(POTIONS):
        x = -1.1 + k * 0.73
        bottle(b, x, 0.0, 1.33, colour, 1.9)
        b.box(x - 0.2, x + 0.2, -0.29, -0.26, 0.85, 1.1, colour)                  # its colour tag below
    # the hanging sign: a bottle
    b.rod((1.6, -0.1, 2.3), (2.3, -0.1, 2.3), 0.06, WOOD_D)
    for px in (1.95, 2.2):
        b.rod((px, -0.1, 2.3), (px, -0.1, 2.05), 0.015, 'iron')
    b.box(1.8, 2.35, -0.13, -0.07, 1.45, 2.05, WOOD_L, bevel=0.02)
    bottle(b, 2.07, -0.16, 1.5, 'banner_red', 1.6)
    # the cauldron at the side, green brew in it
    cx, cy = -2.4, 0.2
    for a in (0, 120, 240):
        r = math.radians(a)
        b.rod((cx + math.cos(r) * 0.55, cy + math.sin(r) * 0.55, 0.0), (cx + math.cos(r) * 0.3, cy + math.sin(r) * 0.3, 0.5), 0.06, 'iron')
    b.ball(cx, cy, 0.25, 0.48, 0.6, 'iron', sides=9)
    b.cylinder(cx, cy, 0.78, 0.82, 0.4, 'grass', sides=9)
    for k in range(3):
        b.ball(cx - 0.15 + k * 0.15, cy + 0.08 * (k - 1), 0.8, 0.07, 0.08, 'leaf', sides=5)   # bubbles
    b.rod((cx + 0.2, cy - 0.1, 0.8), (cx + 0.5, cy - 0.3, 1.4), 0.04, WOOD_L)                 # the ladle
    # crates of bottles, a barrel
    for (x, y, a) in ((2.4, 0.9, 15), (2.6, 1.6, -10)):
        crate(b, x, y, a=a)
        for k in range(3):
            bottle(b, x - 0.15 + k * 0.15, y, 0.55, POTIONS[k][0], 0.6)
    barrel(b, -2.3, 1.4)
    return b


SCALE = 0.44     # built for a 1.8 m person; the game's hero is 0.6-0.8 cells tall (as dungeon1.SCALE)


def scaled(build):
    def run():
        b = build()
        o.scale_since(b, 0, SCALE)
        return b
    return run


MODELS = {"DungeonHireCamp": scaled(hire_camp), "DungeonPotionShop": scaled(potion_shop)}

if __name__ == "__main__":     # dungeon1.py imports the builders to set them in the map
    cave.main(MODELS, row_y=-76.0)
