"""Dungeon 1, the first test labyrinth (the user, 2026-10-31): 100 x 100 m, an old mine, made in
Blender ahead of time (no generator in the game). Built from a 1 m grid carved here by hand:

- south: the CAMP at the entrance -- the gnome prospectors' hiring tent, the potion stall, a
  campfire, the place hirelings wait; the daylight of the way out behind it;
- west: the GALLERY with rails, side pockets with crystals;
- centre: the LAKE HALL -- an underground lake with a plank bridge, glowing mushrooms;
- east: the workings in cobwebs and the SPIDER QUEEN's lair (boss 1);
- north: the deep face and the ORE GOLEM's hall (boss 2), great ore crystals, a chest;
- 12 upgrade crystals in dead ends and on the far sides of the halls.

Rock walls are faceted crags round every floor edge; the solid rock beyond is a dark roof, so from
the game camera the passages read as cut into darkness. Light points (lanterns, fires, crystals)
are Empties named DungeonLight_<kind>_<n> for the preview and, later, the game's lights.
Scale: the hero stands ~0.5 m; corridors 3-4 m, halls 25-36 m (bosses are much bigger than a hero).

    blender --background Buildings-Size1-Models.blend --python dungeon1.py
"""
import math
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import cave_entrance as cave  # noqa: E402  (the rough-rock pattern override)
import detail  # noqa: E402
import kit  # noqa: E402
from kit import Builder  # noqa: E402
from mathutils import Vector  # noqa: E402

NAME = "Dungeon1"
N = 100                     # metres a side, one cell a metre
ORIGIN = Vector((400.0, 400.0, 0.0))   # far from the building models in the file
ROOF = 1.15                 # the dark rock top over solid ground
DARK, STONE, STONE_D, STONE_L = 'window', 'stone_mid', 'stone_dark', 'stone_light'
WOOD, WOOD_D, WOOD_L = 'wood_mid', 'wood_dark', 'wood_light'
CRYSTAL = 'roof_purple'
SOLID, FLOOR, WATER = 0, 1, 2

rng = random.Random(31)
grid = [[SOLID] * N for _ in range(N)]
lights = []                 # (kind, x, y, z)


# -- carving ---------------------------------------------------------------------------------

def rect(x0, y0, x1, y1, kind=FLOOR):
    for x in range(max(0, x0), min(N, x1)):
        for y in range(max(0, y0), min(N, y1)):
            grid[x][y] = kind


def ellipse(cx, cy, rx, ry, kind=FLOOR):
    for x in range(max(0, int(cx - rx) - 1), min(N, int(cx + rx) + 2)):
        for y in range(max(0, int(cy - ry) - 1), min(N, int(cy + ry) + 2)):
            if ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1.0:
                grid[x][y] = kind


def is_floor(x, y):
    return 0 <= x < N and 0 <= y < N and grid[x][y] == FLOOR


def roughen(passes=2, chance=0.28):
    """Bites out of the walls, so a carved rectangle reads as a cave, not a room."""
    for _ in range(passes):
        bite = []
        for x in range(1, N - 1):
            for y in range(1, N - 1):
                if grid[x][y] != SOLID:
                    continue
                if any(grid[x + dx][y + dy] == FLOOR for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))) and rng.random() < chance:
                    bite.append((x, y))
        for x, y in bite:
            grid[x][y] = FLOOR


def carve():
    rect(39, 4, 61, 18)                        # the camp
    rect(47, 0, 53, 5)                         # the way out
    rect(48, 18, 52, 40)                       # north corridor (rails)
    ellipse(50, 50, 22, 13)                    # the lake hall
    rect(10, 10, 39, 14)                       # west: from the camp
    rect(10, 10, 14, 77)                       # west: the gallery north
    rect(14, 24, 25, 33)                       #   a room off it (crystal)
    rect(3, 52, 11, 59)                        #   a pocket west (crystal)
    rect(13, 47, 30, 51)                       #   joins the lake hall
    rect(71, 47, 85, 51)                       # east: out of the lake hall
    rect(81, 47, 85, 68)                       # east: north to the spiders
    ellipse(82, 80, 14, 13)                    # the spider queen's lair
    rect(81, 22, 85, 48)                       # east: south branch
    rect(84, 19, 95, 31)                       #   its pocket (crystal)
    rect(48, 61, 52, 74)                       # north: the deep corridor
    ellipse(44, 84, 18, 12)                    # the ore golem's hall
    rect(22, 87, 28, 91)                       #   west out of it
    rect(13, 85, 24, 95)                       #   its pocket (crystal)
    roughen()
    # the lake, after the roughening so its shore stays a shore
    ellipse(50, 50, 13, 7, WATER)
    for x in range(N):                         # a closed rim: nothing walks off the map (the way out aside)
        if not 47 <= x < 53:
            grid[x][0] = SOLID
        grid[x][N - 1] = SOLID
        grid[0][x] = grid[N - 1][x] = SOLID


def nearest_floor(x, y):
    for r in range(0, 12):
        for dx in range(-r, r + 1):
            for dy in range(-r, r + 1):
                if is_floor(x + dx, y + dy):
                    return x + dx, y + dy
    return x, y


def w(x, y, z=0.0):
    """Grid metres (0..100) -> the object's local space, centred."""
    return (x - N / 2, y - N / 2, z)


# -- the shell: floor, walls, roof, water ----------------------------------------------------

def floor_colour(x, y):
    return STONE           # one tone; the variety comes from patches and scatter (build_scatter)


def build_shell(b):
    # floor and roof, each a run of cells along x merged into one quad
    for y in range(N):
        x = 0
        while x < N:
            kind = grid[x][y]
            colour = floor_colour(x, y) if kind == FLOOR else None
            x1 = x
            while x1 < N and grid[x1][y] == kind and (kind != FLOOR or floor_colour(x1, y) == colour):
                x1 += 1
            x0w, y0w, _ = w(x, y)
            x1w, y1w, _ = w(x1, y + 1)
            if kind == FLOOR:
                b.box(x0w, x1w, y0w, y1w, -0.05, 0.0, colour)
            elif kind == SOLID:
                b.box(x0w, x1w, y0w, y1w, ROOF - 0.05, ROOF, DARK)
            x = x1
    # the lake
    for y in range(N):
        x = 0
        while x < N:
            if grid[x][y] != WATER:
                x += 1
                continue
            x1 = x
            while x1 < N and grid[x1][y] == WATER:
                x1 += 1
            b.box(*w(x, y)[:1], *w(x1, y)[:1], w(0, y)[1], w(0, y + 1)[1], -0.35, -0.3, 'water')
            x = x1
    # the walls: a crag on every rock cell next to the way, every other one to save triangles
    for x in range(N):
        for y in range(N):
            if grid[x][y] != SOLID:
                continue
            edge = any(0 <= x + dx < N and 0 <= y + dy < N and grid[x + dx][y + dy] != SOLID
                       for dx in (-1, 0, 1) for dy in (-1, 0, 1))
            if not edge or (x + y) % 2:
                continue
            cx, cy, _ = w(x + 0.5, y + 0.5)
            h = ROOF + 0.25 + rng.random() * 0.9
            b.crag(cx, cy, 0.95, 0.95, h, STONE_D if rng.random() < 0.55 else STONE, top=DARK, sides=5,
                   rot_deg=rng.random() * 72, seed=rng.randrange(40))


# -- props -----------------------------------------------------------------------------------

def light(kind, x, y, z):
    lights.append((kind, x, y, z))


def support(b, x, y, axis):
    """Timber props across a passage at cell (x, y): posts at both walls, a beam, a lantern."""
    dx, dy = (1, 0) if axis == 'x' else (0, 1)
    a, c = (x, y), (x, y)
    while is_floor(a[0] - dx, a[1] - dy):
        a = (a[0] - dx, a[1] - dy)
    while is_floor(c[0] + dx, c[1] + dy):
        c = (c[0] + dx, c[1] + dy)
    p0 = Vector(w(a[0] + 0.5 - dx * 0.35, a[1] + 0.5 - dy * 0.35))
    p1 = Vector(w(c[0] + 0.5 + dx * 0.35, c[1] + 0.5 + dy * 0.35))
    for p in (p0, p1):
        b.box(p.x - 0.09, p.x + 0.09, p.y - 0.09, p.y + 0.09, 0.0, 1.05, WOOD)
    b.rod((p0.x, p0.y, 1.05), (p1.x, p1.y, 1.05), 0.16, WOOD_D)
    m = (p0 + p1) / 2
    b.rod((m.x, m.y, 1.0), (m.x, m.y, 0.85), 0.015, 'iron')
    b.box(m.x - 0.06, m.x + 0.06, m.y - 0.06, m.y + 0.06, 0.68, 0.85, 'window_light')
    light('lantern', m.x, m.y, 0.75)


def rails(b, x0, y0, x1, y1):
    """Rails down the middle of a straight passage from cell (x0, y0) to (x1, y1)."""
    p0, p1 = Vector(w(x0, y0)), Vector(w(x1, y1))
    d = (p1 - p0)
    length = d.length
    d.normalize()
    side = Vector((-d.y, d.x, 0.0))
    for k in range(int(length / 0.55)):
        c = p0 + d * (k * 0.55 + 0.2)
        a, e = c - side * 0.38, c + side * 0.38
        b.rod((a.x, a.y, 0.03), (e.x, e.y, 0.03), 0.12, WOOD_D)
    for s in (-0.25, 0.25):
        a, e = p0 + side * s, p1 + side * s
        b.rod((a.x, a.y, 0.07), (e.x, e.y, 0.07), 0.05, 'iron')


def crystal(b, x, y, big=1.0, colour=CRYSTAL):
    """An upgrade crystal cluster (a pickup in the game): three points of different height."""
    cx, cy, _ = w(x + 0.5, y + 0.5)
    for k, (ox, oy, h, r) in enumerate(((0, 0, 0.75, 0.14), (0.18, 0.1, 0.5, 0.1), (-0.15, 0.12, 0.42, 0.09))):
        h, r = h * big, r * big
        b.frustum(cx + ox, cy + oy, 0.0, h * 0.7, r, r * 0.8, colour, sides=6, cap=False)
        b.pyramid(cx + ox, cy + oy, h * 0.7, r * 0.8, r * 0.8, h, colour, sides=6)
    light('crystal', cx, cy, 0.5 * big)


def mushrooms(b, x, y, n=4):
    cx, cy, _ = w(x, y)
    for k in range(n):
        a = k * 2.4
        px, py, s = cx + math.cos(a) * 0.5, cy + math.sin(a) * 0.4, 0.7 + 0.3 * (k % 2)
        b.cylinder(px, py, 0.0, 0.16 * s, 0.035 * s, 'plaster_white', sides=5)
        b.frustum(px, py, 0.16 * s, 0.24 * s, 0.13 * s, 0.04 * s, 'water', sides=7)
    light('mushroom', cx, cy, 0.3)


def web(b, x, y, z, s=1.0, a=0.0):
    """A cobweb hung between rocks: a flat white fan."""
    cx, cy, _ = w(x, y)
    u = (math.cos(a), math.sin(a), 0.0)
    b.prism([(-0.6 * s, 0.0), (0.6 * s, 0.0), (0.0, 0.9 * s)], (cx, cy, z), u, (0, 0, 1), 0.01, 'plaster_white',
            back_cap=True)


def eggs(b, x, y):
    cx, cy, _ = w(x, y)
    for k in range(5):
        a = k * 1.3
        b.ball(cx + math.cos(a) * 0.35, cy + math.sin(a) * 0.3, 0.0, 0.22, 0.4, 'plaster_white', sides=6)


def bones(b, x, y):
    cx, cy, _ = w(x, y)
    for k in range(3):
        a = k * 2.1 + x
        p0 = (cx + math.cos(a) * 0.2, cy + math.sin(a) * 0.2, 0.03)
        b.rod(p0, (p0[0] + math.cos(a + 1.2) * 0.3, p0[1] + math.sin(a + 1.2) * 0.3, 0.03), 0.05, 'plaster')
    cave.skull(b, cx, cy, 0.0, 0.16)


def tent(b, x, y, w_, d, colour='burlap'):
    """A ridge tent along y: two sloping cloth sides on a pole frame."""
    cx, cy, _ = w(x, y)
    b.prism([(-w_ / 2, 0.0), (w_ / 2, 0.0), (0.0, w_ * 0.8)], (cx, cy + d / 2, 0.0), (1, 0, 0), (0, 0, 1), d, colour,
            back_cap=True)
    for s in (-1, 1):
        b.rod((cx, cy + s * (d / 2 + 0.05), 0.0), (cx, cy + s * (d / 2 + 0.05), w_ * 0.85), 0.06, WOOD_D)


def build_camp(b):
    # the gnome prospectors' camp: a stone-footed timber hut under a red roof, a sign, barrels
    hx, hy, _ = w(42.5, 13.0)
    b.box(hx - 1.6, hx + 1.6, hy - 1.3, hy + 1.3, 0.0, 0.25, STONE_L, bevel=0.03)
    b.box(hx - 1.45, hx + 1.45, hy - 1.15, hy + 1.15, 0.25, 1.35, 'plaster', bevel=0.02)
    for px in (-1.45, 1.45):
        for py in (-1.15, 1.15):
            b.box(hx + px - 0.08, hx + px + 0.08, hy + py - 0.08, hy + py + 0.08, 0.25, 1.35, WOOD_D)
    b.box(hx - 1.5, hx + 1.5, hy - 1.2, hy - 1.12, 0.8, 0.9, WOOD_D)
    b.gable_roof(hx, hy, 'x', 1.75, 1.45, 1.3, 2.25, 'roof_red', 'roof_red_dark')
    b.box(hx + 0.3, hx + 0.9, hy - 1.2, hy - 1.15, 0.25, 1.05, 'door')
    b.box(hx - 1.0, hx - 0.45, hy - 1.2, hy - 1.15, 0.55, 1.0, 'window_light')
    b.box(hx - 0.25, hx - 0.05, hy + 0.5, hy + 0.75, 1.6, 2.5, STONE_D)      # the chimney
    light('lantern', hx - 0.7, hy - 1.6, 0.8)
    cx, cy, _ = w(45.0, 10.2)
    b.box(cx - 0.05, cx + 0.05, cy - 0.05, cy + 0.05, 0.0, 1.5, WOOD_D)
    b.box(cx - 0.55, cx + 0.55, cy - 0.06, cy + 0.0, 1.05, 1.45, WOOD_L)
    for i, (px, py) in enumerate(((40.0, 9.5), (40.6, 8.8), (39.6, 16.0))):
        bx, by, _ = w(px, py)
        b.cylinder(bx, by, 0.0, 0.5, 0.2, WOOD, sides=8)
        b.cylinder(bx, by, 0.36, 0.4, 0.21, 'iron', sides=8)
    # pick and shovel racks
    for k in range(3):
        rx, ry, _ = w(39.2 + k * 0.4, 12.0)
        b.rod((rx, ry, 0.0), (rx + 0.1, ry + 0.25, 0.9), 0.04, WOOD_L)
        b.obox((rx + 0.1, ry + 0.25, 0.92), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.16, 0.03, 0.03, 'iron')
    # the potion stall: a counter under an awning, bottles in colours
    sx, sy, _ = w(57.0, 12.0)
    b.box(sx - 1.4, sx + 1.4, sy - 0.35, sy + 0.35, 0.0, 0.6, WOOD, bevel=0.02)
    for s in (-1, 1):
        b.box(sx + s * 1.35 - 0.05, sx + s * 1.35 + 0.05, sy + 0.25, sy + 0.35, 0.0, 1.6, WOOD_D)
        b.box(sx + s * 1.35 - 0.05, sx + s * 1.35 + 0.05, sy - 0.75, sy - 0.65, 0.0, 1.35, WOOD_D)
    for k in range(6):
        b.obox((sx - 1.25 + k * 0.5, sy - 0.2, 1.5), (1, 0, 0), (0, 0.9, -0.44), (0, 0.44, 0.9), 0.25, 0.55, 0.02,
               'banner_blue' if k % 2 else 'plaster_white')
    for k, colour in enumerate(('banner_red', 'fire', 'water', 'grass', 'banner_red', 'water')):
        px = sx - 1.1 + k * 0.44
        b.cylinder(px, sy, 0.6, 0.78, 0.07, colour, sides=6)
        b.cylinder(px, sy, 0.78, 0.85, 0.03, 'wood_light', sides=5)
    light('lantern', sx, sy, 1.3)
    # the campfire, logs to sit on
    fx, fy, _ = w(50.0, 9.0)
    for a in (0, 60, 120):
        r = math.radians(a)
        b.rod((fx - math.cos(r) * 0.35, fy - math.sin(r) * 0.35, 0.05), (fx + math.cos(r) * 0.35, fy + math.sin(r) * 0.35, 0.05),
              0.1, WOOD_D)
    for k in range(7):
        a = k * 2 * math.pi / 7
        b.crag(fx + math.cos(a) * 0.55, fy + math.sin(a) * 0.55, 0.12, 0.1, 0.1, STONE_L, sides=5, seed=k)
    b.pyramid(fx, fy, 0.06, 0.22, 0.22, 0.7, 'fire', sides=5)
    light('fire', fx, fy, 0.5)
    for lx, ly, a in ((48.0, 7.2, 0), (52.2, 8.0, 70)):
        px, py, _ = w(lx, ly)
        b.hcylinder('x' if a == 0 else 'y', (px, py, 0.13), 0.6, 0.13, WOOD, sides=7)
    # where the hirelings wait: a little banner on a pole
    px, py, _ = w(54.0, 16.0)
    b.rod((px, py, 0.0), (px, py, 1.8), 0.05, WOOD_D)
    b.box(px + 0.03, px + 0.6, py - 0.02, py + 0.02, 1.2, 1.75, 'banner_red')
    # the way out: daylight at the end of the tunnel
    ox, oy, _ = w(50.0, 0.0)
    b.box(ox - 3.0, ox + 3.0, oy - 0.02, oy, 0.0, 1.6, 'window_light', skip=())
    light('day', ox, oy + 1.0, 0.8)


def build_lake(b):
    # the plank bridge across the lake, north to south
    for y in range(41, 60):
        bx, by, _ = w(50.0, y + 0.5)
        b.box(bx - 0.8, bx + 0.8, by - 0.42, by + 0.42, -0.05, 0.05, WOOD if y % 2 else WOOD_L)
    for y in (41, 46, 51, 56, 60):
        for s in (-1, 1):
            px, py, _ = w(50.0 + s * 0.85, y)
            b.box(px - 0.06, px + 0.06, py - 0.06, py + 0.06, -0.35, 0.5, WOOD_D)
    b.rod(w(49.15, 41, 0.45), w(49.15, 60, 0.45), 0.05, WOOD)
    b.rod(w(50.85, 41, 0.45), w(50.85, 60, 0.45), 0.05, WOOD)
    for x, y in ((31, 42), (68, 57), (35, 59), (64, 40), (30, 53)):
        mushrooms(b, x, y)
    # rocks standing in the water
    for x, y in ((33, 47), (67, 44), (40, 60), (62, 60), (36, 40)):        # pillars left by the miners
        cx, cy, _ = w(x, y)
        b.crag(cx, cy, 1.0, 0.9, ROOF + 0.8, STONE_D, top=DARK, sides=6, seed=x + y)
    for x, y, r in ((42, 47, 0.9), (58, 53, 0.7), (45, 54, 0.5)):
        cx, cy, _ = w(x, y)
        b.crag(cx, cy, r, r * 0.8, 0.5, STONE, sides=6, seed=x)


def build_spiders(b):
    for x, y, z, s, a in ((70, 78, 0.4, 1.2, 1.4), (93, 82, 0.3, 1.4, 1.7), (80, 92, 0.4, 1.0, 0.1), (75, 70, 0.3, 0.9, 0.5),
                          (90, 72, 0.4, 1.1, 2.4), (83, 66, 0.2, 0.8, 0.0), (84, 52, 0.2, 0.7, 1.57)):
        web(b, x, y, z, s, a)
    for x, y in ((88, 88), (74, 86), (92, 76), (79, 82), (86, 72)):
        eggs(b, x, y)
    for x, y in ((76, 78), (88, 80), (82, 88)):                         # pillars draped in web
        cx, cy, _ = w(x, y)
        b.crag(cx, cy, 0.8, 0.8, ROOF + 0.6, STONE_D, top=DARK, sides=6, seed=x)
        web(b, x - 0.8, y - 0.9, 0.2, 1.0, 0.3)
    for x, y in ((80, 74), (86, 84), (78, 88), (83, 60)):
        bones(b, x, y)
    # cocooned victims leaning on the rock
    for x, y in ((71, 83), (94, 79)):
        cx, cy, _ = w(x, y)
        b.ball(cx, cy, 0.0, 0.18, 0.75, 'plaster_white', sides=6)


def build_golem(b):
    # great blue ore crystals out of the hall's walls, pillars of rock left standing, a broken cart
    for x, y, s in ((29, 82, 2.2), (60, 88, 2.6), (40, 95, 2.0), (55, 76, 1.6), (32, 90, 1.7)):
        crystal(b, x, y, s, 'water')
    for x, y in ((38, 80), (50, 86), (44, 90), (32, 86), (56, 80)):
        cx, cy, _ = w(x, y)
        b.crag(cx, cy, 1.1, 1.0, ROOF + 0.9, STONE_D, top=DARK, sides=6, seed=x)
    rails(b, 50, 62, 50, 78)
    cx, cy, _ = w(47.0, 77.0)
    b.obox((cx, cy, 0.25), (0.94, 0.34, 0), (-0.24, 0.66, 0.71), (0.24, -0.66, 0.71), 0.45, 0.32, 0.28, WOOD, bevel=0.02)
    # the chest the golem guards
    cx, cy, _ = w(44.0, 93.0)
    b.box(cx - 0.45, cx + 0.45, cy - 0.3, cy + 0.3, 0.0, 0.45, WOOD, bevel=0.02)
    b.box(cx - 0.47, cx + 0.47, cy - 0.32, cy + 0.32, 0.45, 0.55, 'gold')
    b.box(cx - 0.06, cx + 0.06, cy - 0.34, cy - 0.3, 0.25, 0.45, 'iron')
    light('fire', cx, cy - 1.0, 0.6)
    for x, y in ((36, 76), (52, 92)):
        bones(b, x, y)


def build_scatter(b):
    """What makes a passage look old and walked: patches on the floor, pebbles, rubble, stalagmites
    by the walls, boards and broken barrels. Seeded, so the map is the same every build."""
    r = random.Random(7)
    for x in range(2, N - 2):
        for y in range(2, N - 2):
            if grid[x][y] != FLOOR or (38 <= x < 62 and 2 <= y < 19):
                continue                                   # the camp keeps its own clean ground
            walls = sum(grid[x + dx][y + dy] == SOLID for dx in (-1, 0, 1) for dy in (-1, 0, 1))
            cx, cy, _ = w(x + r.random(), y + r.random())
            roll = r.random()
            if roll < 0.035:                               # a patch: damp dark stone or trodden earth
                rad = 0.8 + r.random() * 1.6
                b.crag(cx, cy, rad, rad * (0.6 + r.random() * 0.4), 0.015, STONE_D if r.random() < 0.7 else 'wood_dark',
                       sides=7, taper=0.95, rot_deg=r.random() * 60, seed=r.randrange(40))
            elif roll < 0.11:                              # a pebble or two
                for _ in range(1 + (r.random() < 0.4)):
                    s = 0.08 + r.random() * 0.14
                    b.crag(cx + r.random() * 0.4, cy + r.random() * 0.4, s, s * 0.8, s * 0.9, STONE_L if r.random() < 0.4 else STONE,
                           sides=5, rot_deg=r.random() * 72, seed=r.randrange(40))
            elif walls >= 2 and roll < 0.2:                # stalagmites grow along the walls
                for k in range(1 + r.randrange(3)):
                    rad = 0.12 + r.random() * 0.22
                    h = 0.35 + r.random() * 1.0
                    px, py = cx + (r.random() - 0.5) * 0.7, cy + (r.random() - 0.5) * 0.7
                    b.frustum(px, py, 0.0, h * 0.45, rad, rad * 0.6, STONE_D if k % 2 else STONE, sides=6, cap=False)
                    b.pyramid(px, py, h * 0.45, rad * 0.6, rad * 0.6, h, STONE_D if k % 2 else STONE, sides=6)
            elif walls >= 1 and roll < 0.215:              # a heap of rubble fallen from the wall
                for k in range(4):
                    s = 0.15 + r.random() * 0.2
                    b.crag(cx + (r.random() - 0.5) * 0.8, cy + (r.random() - 0.5) * 0.8, s, s, s * 0.8, STONE_D,
                           sides=5, seed=r.randrange(40))
            elif roll < 0.222:                             # an old board or a broken barrel
                if r.random() < 0.5:
                    a = r.random() * math.pi
                    b.obox((cx, cy, 0.03), (math.cos(a), math.sin(a), 0), (-math.sin(a), math.cos(a), 0), (0, 0, 1),
                           0.6, 0.1, 0.025, WOOD_L if r.random() < 0.5 else WOOD)
                else:
                    b.cylinder(cx, cy, 0.0, 0.22, 0.2, WOOD, sides=8)
                    b.cylinder(cx, cy, 0.18, 0.22, 0.21, 'iron', sides=8)
            elif roll < 0.226:
                bones(b, x + 0.5, y + 0.5)


def build_props(b):
    build_camp(b)
    build_scatter(b)
    rails(b, 50, 18, 50, 39)
    rails(b, 12, 12, 37, 12)
    rails(b, 12, 12, 12, 76)
    for x, y, axis in ((50, 24, 'x'), (50, 32, 'x'), (20, 12, 'y'), (30, 12, 'y'), (12, 22, 'x'), (12, 38, 'x'),
                       (12, 62, 'x'), (12, 72, 'x'), (78, 49, 'y'), (83, 56, 'x'), (83, 30, 'x'), (50, 66, 'x'),
                       (20, 49, 'y')):
        support(b, x, y, axis)
    build_lake(b)
    build_spiders(b)
    build_golem(b)
    for x, y in ((19, 28), (6, 55), (11, 74), (89, 25), (18, 90), (92, 86), (56, 92), (32, 78), (66, 52), (30, 46),
                 (83, 62), (12, 32)):
        fx, fy = nearest_floor(x, y)
        crystal(b, fx, fy)
    # ore veins glinting along the walls of the gallery and the corridors
    for _ in range(70):
        x, y = rng.randrange(2, N - 2), rng.randrange(2, N - 2)
        if grid[x][y] != FLOOR:
            continue
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            if grid[x + dx][y + dy] == SOLID:
                cx, cy, _ = w(x + 0.5 + dx * 0.45, y + 0.5 + dy * 0.45)
                z = 0.4 + rng.random() * 0.5
                b.obox((cx, cy, z), (0.7, 0.7, 0), (-0.7, 0.7, 0), (0, 0, 1), 0.08, 0.08, 0.1, 'gold')
                break


def main():
    carve()
    b = Builder()
    build_shell(b)
    build_props(b)

    coll = bpy.data.collections.get(cave.COLLECTION)
    if coll is None:
        coll = bpy.data.collections.new(cave.COLLECTION)
        bpy.context.scene.collection.children.link(coll)
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    me = b.to_mesh(NAME)
    me.materials.append(mat)
    detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
    obj = bpy.data.objects.get(NAME)
    if obj is None:
        obj = bpy.data.objects.new(NAME, me)
        coll.objects.link(obj)
    else:
        old = obj.data
        obj.data = me
        if old.users == 0:
            bpy.data.meshes.remove(old)
    obj.location = ORIGIN
    for o in list(obj.children):
        bpy.data.objects.remove(o, do_unlink=True)
    for i, (kind, x, y, z) in enumerate(lights):
        e = bpy.data.objects.new(f"DungeonLight_{kind}_{i}", None)
        e.empty_display_size = 0.3
        coll.objects.link(e)
        e.parent = obj
        e.location = (x, y, z)
    print(f"MODEL {NAME}: tris={sum(len(p.vertices) - 2 for p in obj.data.polygons)} lights={len(lights)} "
          f"floor={sum(row.count(FLOOR) for row in grid)}")
    kit.file_display_settings()
    bpy.ops.wm.save_mainfile()


main()
