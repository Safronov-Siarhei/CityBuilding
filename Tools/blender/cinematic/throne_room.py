"""The throne room for mission 2's scene 2 «Гонец у короля» (the user, 2026-11-15): «величественный замок». v2 after
the user's look at v1: «выглядит маленьким и не величественным + тусклым, нужно чтобы он был яркий и большой,
внутри были стража и люди».

Built at the cutscene figures' own size (scale 1; the King stands ~0.73): the hall 6.4 wide (x), 15 long (y: the
great doors at y -7.5, the throne's wall at +7.5), its walls 4.6 high; open above (gilded beams across) so the
day pours in. Bright and warm:
  * a floor of white and gold tiles in a red-bordered field; a broad red carpet with a gold border to the dais;
  * cream walls with white pilasters, a stone plinth, a gold-and-white cornice;
  * two colonnades of seven tall white columns with gold bands and capitals, a white entablature on them with a
    gold frieze; purple banners with a gold crown hanging from it between the columns;
  * tall STAINED-GLASS windows in the side walls (blue, red, gold panes in lead lines), round rose windows above;
  * three gold chandeliers on chains, ringed with candles, down the hall;
  * the dais: four broad steps, the throne on top under a purple CANOPY with gold fringes and a crown on its
    crest; the throne itself tall and gilded (purple back with a crown, three finials, red cushion); a purple
    tapestry with the great gold crown behind; gold candelabra either side;
  * the great double door at the far end, gilded studs, ajar for the rider.

    blender --background <Buildings-Size1-Models.blend> --python throne_room.py -- render <out_dir>
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))
import figure as fg  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "CineThroneRoom"
HX, Y0, Y1, H = 3.2, -7.5, 7.5, 4.6          # half width, door wall, throne wall, wall height
WALL_T = 0.2
CARPET_W = 0.8
STEP_H, STEP_D, STEPS = 0.07, 0.2, 4
DAIS_Y = 5.4                                   # the first step's front
COLS_Y = [-5.6, -3.9, -2.2, -0.5, 1.2, 2.9, 4.6]
COL_X = 2.0
COL_H = 3.4
DOOR_W, DOOR_H = 1.7, 2.6
PURPLE, PURPLE_DARK = 'roof_purple', 'roof_purple_dark'
GOLD = 'gold'
RED, RED_DARK = 'banner_red', 'roof_red_dark'
CREAM, WHITE = 'plaster', 'plaster_white'
STONE, STONE_MID = 'stone_light', 'stone_mid'
TILE_A, TILE_B = 'plaster_white', 'sand'
GLASS = ('roof_blue', 'roof_red', 'plaster_yellow', 'water')
WOOD, WOOD_DARK = 'wood_mid', 'wood_dark'
IRON = 'iron'
FIRE = 'fire'


def crown(f, s, z, w, depth):
    """A gold crown emblem on a Facade: a band and three points."""
    f.rect(s - w / 2, s + w / 2, z, z + w * 0.28, depth, GOLD)
    for k in (-1, 0, 1):
        x = s + k * w * 0.36
        f.poly([(x - w * 0.12, z + w * 0.28), (x + w * 0.12, z + w * 0.28), (x, z + w * (0.62 if k == 0 else 0.52))],
               depth, GOLD)


def floor(b):
    tile = 0.5
    nx = int(2 * HX / tile) + 1
    ny = int((Y1 - Y0) / tile) + 1
    for i in range(nx):
        for j in range(ny):
            x = -HX + i * tile
            y = Y0 + j * tile
            b.box(x, min(x + tile, HX), y, min(y + tile, Y1), -0.02, 0.0, TILE_A if (i + j) % 2 == 0 else TILE_B)
    # a red border round the field between the colonnades
    for k in (-1, 1):
        x = k * (COL_X - 0.35)
        b.box(x - 0.05, x + 0.05, Y0 + WALL_T, DAIS_Y, 0.0, 0.004, RED)
    # the carpet with its gold border, door to dais
    b.box(-CARPET_W / 2 - 0.05, CARPET_W / 2 + 0.05, Y0 + WALL_T, DAIS_Y, 0.0, 0.008, GOLD)
    b.box(-CARPET_W / 2, CARPET_W / 2, Y0 + WALL_T, DAIS_Y, 0.0, 0.01, RED)
    for y in [Y0 + 1.0 + i * 1.2 for i in range(int((DAIS_Y - Y0) / 1.2))]:
        b.box(-0.08, 0.08, y - 0.08, y + 0.08, 0.0, 0.011, GOLD)                         # gold lozenges on it


def dais(b):
    w = 1.7
    for i in range(STEPS):
        y = DAIS_Y + i * STEP_D
        grow = 0.15 * (STEPS - 1 - i)
        b.box(-w - grow, w + grow, y, Y1 - WALL_T, 0.0, STEP_H * (i + 1), WHITE, bevel=0.006)
        b.box(-w - grow - 0.01, w + grow + 0.01, y - 0.006, y + 0.012, STEP_H * i + 0.01, STEP_H * (i + 1), GOLD)
        b.box(-CARPET_W / 2, CARPET_W / 2, y - 0.008, y + STEP_D, STEP_H * (i + 1), STEP_H * (i + 1) + 0.008, RED)


def throne(b):
    z = STEP_H * STEPS
    y = Y1 - WALL_T - 0.75
    s = 1.3
    for kx in (-1, 1):
        for ky in (-1, 1):
            b.box(kx * 0.2 * s - 0.035, kx * 0.2 * s + 0.035, y + ky * 0.14 * s - 0.035, y + ky * 0.14 * s + 0.035,
                  z, z + 0.2 * s, GOLD, bevel=0.008)
    b.box(-0.25 * s, 0.25 * s, y - 0.19 * s, y + 0.19 * s, z + 0.18 * s, z + 0.25 * s, GOLD, bevel=0.012)
    b.box(-0.21 * s, 0.21 * s, y - 0.17 * s, y + 0.16 * s, z + 0.25 * s, z + 0.3 * s, RED, bevel=0.015)
    for k in (-1, 1):
        b.box(k * 0.25 * s - 0.04, k * 0.25 * s + 0.04, y - 0.18 * s, y + 0.17 * s, z + 0.25 * s, z + 0.42 * s, PURPLE, bevel=0.01)
        b.box(k * 0.25 * s - 0.05, k * 0.25 * s + 0.05, y - 0.2 * s, y + 0.18 * s, z + 0.42 * s, z + 0.46 * s, GOLD, bevel=0.008)
        b.ball(k * 0.25 * s, y - 0.2 * s, z + 0.46 * s, 0.04, 0.05, GOLD, sides=6)
    yb = y + 0.18 * s
    b.box(-0.28 * s, 0.28 * s, yb, yb + 0.07, z + 0.25 * s, z + 1.2 * s, GOLD, bevel=0.012)
    b.box(-0.23 * s, 0.23 * s, yb - 0.012, yb, z + 0.3 * s, z + 1.13 * s, PURPLE, bevel=0.006)
    for k in (-1, 0, 1):
        b.cylinder(k * 0.24 * s, yb + 0.035, z + 1.2 * s, z + 1.25 * s, 0.035, GOLD, sides=8)
        b.pyramid(k * 0.24 * s, yb + 0.035, z + 1.25 * s, 0.028, 0.028, z + (1.42 if k == 0 else 1.34) * s, GOLD)
    f = Facade(b, '-y', yb - 0.012)
    crown(f, 0.0, z + 0.92 * s, 0.2 * s, 0.014)
    # the canopy over it: a purple tester on gold posts, gold fringe, a crown on its crest
    cy = y + 0.05
    top = z + 2.3
    for kx in (-1, 1):
        b.cylinder(kx * 0.7, cy - 0.55, z, top, 0.045, GOLD, sides=8)
    b.box(-0.82, 0.82, cy - 0.68, Y1 - WALL_T, top, top + 0.14, PURPLE, bevel=0.01)
    b.box(-0.84, 0.84, cy - 0.7, Y1 - WALL_T, top - 0.04, top, GOLD)
    for i in range(18):
        x = -0.8 + i * 0.094
        b.box(x - 0.022, x + 0.022, cy - 0.705, cy - 0.69, top - 0.12, top - 0.04, GOLD)
    for k in (-1, 1):                                                                   # the drapes at its sides
        b.prism([(cy - 0.6, top), (Y1 - WALL_T - 0.02, top), (Y1 - WALL_T - 0.02, z + 0.4), (cy - 0.2, z + 0.4)],
                (k * 0.82, 0, 0), (0, 1, 0), (0, 0, 1), 0.02, PURPLE_DARK, back_cap=True)
    f = Facade(b, '-y', cy - 0.7)
    crown(f, 0.0, top + 0.14, 0.4, 0.04)


def walls(b):
    b.box(-HX - WALL_T, -HX, Y0, Y1, -0.02, H, CREAM)
    b.box(HX, HX + WALL_T, Y0, Y1, -0.02, H, CREAM)
    b.box(-HX - WALL_T, HX + WALL_T, Y1 - WALL_T, Y1, -0.02, H, CREAM)
    b.box(-HX - WALL_T, -DOOR_W / 2 - 0.2, Y0, Y0 + WALL_T, -0.02, H, CREAM)
    b.box(DOOR_W / 2 + 0.2, HX + WALL_T, Y0, Y0 + WALL_T, -0.02, H, CREAM)
    b.box(-DOOR_W / 2 - 0.2, DOOR_W / 2 + 0.2, Y0, Y0 + WALL_T, DOOR_H + 0.2, H, CREAM)
    for k in (-1, 1):
        x = k * HX
        inner = -k
        b.box(min(x, x + inner * 0.05), max(x, x + inner * 0.05), Y0 + WALL_T, Y1 - WALL_T, 0.0, 0.18, STONE_MID)   # plinth
        b.box(min(x, x + inner * 0.09), max(x, x + inner * 0.09), Y0 + WALL_T, Y1 - WALL_T, H - 0.22, H - 0.1, WHITE)
        b.box(min(x, x + inner * 0.1), max(x, x + inner * 0.1), Y0 + WALL_T, Y1 - WALL_T, H - 0.26, H - 0.22, GOLD)
        for y in COLS_Y:                                                                 # pilasters behind the columns
            b.box(min(x, x + inner * 0.07), max(x, x + inner * 0.07), y - 0.16, y + 0.16, 0.0, H - 0.22, WHITE)
    for zz, c in ((H - 0.22, WHITE), (H - 0.26, GOLD)):
        b.box(-HX, HX, Y1 - WALL_T - 0.09, Y1 - WALL_T, zz, zz + (0.12 if c == WHITE else 0.04), c)
    # gilded beams across the open top
    for y in [Y0 + 0.4 + i * 1.6 for i in range(10)]:
        b.box(-HX - WALL_T, HX + WALL_T, y - 0.08, y + 0.08, H - 0.02, H + 0.14, WOOD)
        b.box(-HX - WALL_T, HX + WALL_T, y - 0.085, y + 0.085, H - 0.04, H - 0.02, GOLD)


def door(b):
    """The great door: a white arch with a gold keystone, two leaves with gold studs standing a little open."""
    f = Facade(b, '+y', Y0 + WALL_T)
    f.arch(0.0, 0.0, DOOR_W + 0.4, DOOR_H + 0.2 + 0.2, 0.06, WHITE, segments=12)
    f.rect(-0.1, 0.1, DOOR_H + 0.3, DOOR_H + 0.55, 0.08, GOLD)
    crown(f, 0.0, DOOR_H + 0.7, 0.6, 0.03)
    for k in (-1, 1):
        hinge = k * DOOR_W / 2
        a = math.radians(22) * (-k)
        w = DOOR_W / 2
        cx = hinge - k * math.cos(a) * w / 2
        cy = Y0 + WALL_T + 0.05 + math.sin(abs(a)) * w / 2
        ax, ay = (math.cos(a), math.sin(a), 0), (-math.sin(a), math.cos(a), 0)
        b.obox((cx, cy, DOOR_H * 0.5), ax, ay, (0, 0, 1), w / 2, 0.04, DOOR_H * 0.5, WOOD, bevel=0.008)
        for zz in (0.4, 1.3, 2.2):
            b.obox((cx + ay[0] * 0.042, cy + ay[1] * 0.042, zz), ax, ay, (0, 0, 1), w / 2 - 0.03, 0.005, 0.04, GOLD)
        for i in range(4):
            for zz in (0.85, 1.75):
                t = -w / 2 + 0.12 + i * (w - 0.24) / 3
                b.ball(cx + ax[0] * t + ay[0] * 0.045, cy + ax[1] * t + ay[1] * 0.045, zz - 0.02, 0.018, 0.04, GOLD, sides=5)


def stained(f, s, z0, w, h):
    """A tall stained-glass window: a white frame, coloured panes in lead lines, a gold border."""
    f.arch(s, z0 - 0.06, w + 0.18, h + 0.12, 0.03, WHITE, segments=10)
    f.arch(s, z0 - 0.02, w + 0.06, h + 0.04, 0.034, GOLD, segments=10)
    f.arch(s, z0, w, h, 0.04, 'window_light', segments=10)
    rows = 6
    pane_h = (h - w / 2) / rows
    for r in range(rows):
        for c in range(3):
            x0 = s - w / 2 + c * w / 3
            col = GLASS[(r + c) % len(GLASS)]
            f.rect(x0 + 0.012, x0 + w / 3 - 0.012, z0 + r * pane_h + 0.012, z0 + (r + 1) * pane_h - 0.012, 0.046, col)
    f.disc(s, z0 + h - w / 2, w * 0.3, 0.046, 'roof_red', sides=10)
    f.disc(s, z0 + h - w / 2, w * 0.14, 0.05, 'plaster_yellow', sides=8)
    f.rect(s - w / 2 - 0.12, s + w / 2 + 0.12, z0 - 0.12, z0 - 0.04, 0.14, WHITE)          # the sill


def windows_and_banners(b):
    for k in (-1, 1):
        f = Facade(b, '+x' if k < 0 else '-x', k * HX)
        for i in range(len(COLS_Y) - 1):
            s = (COLS_Y[i] + COLS_Y[i + 1]) / 2
            stained(f, s, 0.9, 0.8, 2.3)
            f.disc(s, 3.85, 0.26, 0.04, WHITE, sides=12)                                  # the rose window above
            f.disc(s, 3.85, 0.21, 0.046, 'roof_blue', sides=12)
            f.disc(s, 3.85, 0.1, 0.05, 'plaster_yellow', sides=8)
    # the great tapestry behind the throne (the banners hang on the columns: see columns())
    f = Facade(b, '-y', Y1 - WALL_T)
    f.rect(-1.3, 1.3, 0.6, 3.9, 0.014, PURPLE)
    f.rect(-1.36, 1.36, 3.9, 3.98, 0.03, GOLD)
    for i in range(24):
        s = -1.26 + i * 0.11
        f.rect(s - 0.025, s + 0.025, 0.52, 0.6, 0.016, GOLD)
    crown(f, 0.0, 2.75, 0.9, 0.025)
    f.rect(-1.2, 1.2, 0.72, 0.76, 0.018, GOLD)
    f.rect(-1.2, 1.2, 3.74, 3.78, 0.018, GOLD)


def columns(b):
    for k in (-1, 1):
        x = k * COL_X
        for y in COLS_Y:
            b.box(x - 0.24, x + 0.24, y - 0.24, y + 0.24, 0.0, 0.14, STONE_MID, bevel=0.012)
            b.cylinder(x, y, 0.14, 0.24, 0.21, WHITE, sides=12)
            b.cylinder(x, y, 0.24, COL_H - 0.3, 0.17, WHITE, sides=12)
            for zz in (0.24, 1.3, COL_H - 0.34):
                b.cylinder(x, y, zz, zz + 0.06, 0.18, GOLD, sides=12)
            b.frustum(x, y, COL_H - 0.3, COL_H - 0.12, 0.17, 0.25, GOLD, sides=12)
            b.box(x - 0.27, x + 0.27, y - 0.27, y + 0.27, COL_H - 0.12, COL_H, WHITE, bevel=0.01)
            # a gold sconce on the column's carpet side, a burning torch
            tx = x - k * 0.2
            b.box(tx - 0.03, tx + 0.03, y - 0.03, y + 0.03, 1.75, 1.83, GOLD, bevel=0.006)
            b.rod((tx, y, 1.78), (tx - k * 0.04, y, 2.02), 0.036, WOOD_DARK)
            b.ball(tx - k * 0.045, y, 1.99, 0.045, 0.11, FIRE, sides=6)
            # the banner on the column's carpet face, under the capital
            f = Facade(b, '+x' if k < 0 else '-x', x - k * 0.17)
            f.rect(y - 0.22, y + 0.22, 2.25, COL_H - 0.4, 0.014, PURPLE)
            f.rect(y - 0.25, y + 0.25, COL_H - 0.4, COL_H - 0.34, 0.03, GOLD)
            f.poly([(y - 0.22, 2.25), (y + 0.22, 2.25), (y, 2.08)], 0.014, PURPLE)
            crown(f, y, 2.6, 0.26, 0.022)
        # the entablature along the colonnade: white, a gold frieze
        b.box(x - 0.28, x + 0.28, COLS_Y[0] - 0.28, COLS_Y[-1] + 0.28, COL_H, COL_H + 0.3, WHITE, bevel=0.01)
        b.box(x - 0.29, x + 0.29, COLS_Y[0] - 0.29, COLS_Y[-1] + 0.29, COL_H + 0.1, COL_H + 0.18, GOLD)


def chandeliers(b):
    for y in (-4.5, -1.3, 1.9):
        z = 2.9
        b.rod((0, y, z + 0.1), (0, y, H + 0.1), 0.018, IRON)                              # the chain
        b.ring(0, y, z, z + 0.06, 0.5, 0.56, [GOLD, GOLD], sides=16)
        b.ring(0, y, z + 0.22, z + 0.26, 0.26, 0.3, [GOLD, GOLD], sides=12)
        b.cylinder(0, y, z - 0.08, z + 0.3, 0.06, GOLD, sides=8)
        for i in range(12):
            a = i * 2 * math.pi / 12
            cx, cy = math.cos(a) * 0.53, y + math.sin(a) * 0.53
            b.cylinder(cx, cy, z + 0.06, z + 0.16, 0.022, WHITE, sides=6)
            b.ball(cx, cy, z + 0.16, 0.016, 0.045, FIRE, sides=5)
        for i in range(6):
            a = i * 2 * math.pi / 6 + 0.3
            b.rod((math.cos(a) * 0.28, y + math.sin(a) * 0.28, z + 0.24), (math.cos(a) * 0.53, y + math.sin(a) * 0.53, z + 0.04), 0.014, GOLD)
    # gold candelabra either side of the dais
    for k in (-1, 1):
        x, y = k * 1.25, Y1 - WALL_T - 0.6
        z = STEP_H * STEPS
        b.cylinder(x, y, z, z + 0.05, 0.12, GOLD, sides=8)
        b.cylinder(x, y, z + 0.05, z + 1.3, 0.028, GOLD, sides=6)
        b.cylinder(x, y, z + 1.3, z + 1.35, 0.14, GOLD, sides=8)
        for a in range(5):
            ang = a * 2 * math.pi / 5
            cx, cy = x + math.cos(ang) * 0.09, y + math.sin(ang) * 0.09
            b.cylinder(cx, cy, z + 1.35, z + 1.45, 0.018, WHITE, sides=6)
            b.ball(cx, cy, z + 1.45, 0.014, 0.035, FIRE, sides=5)


def build():
    b = Builder()
    floor(b)
    dais(b)
    throne(b)
    walls(b)
    door(b)
    windows_and_banners(b)
    columns(b)
    chandeliers(b)
    return b


def make(location=(0, 0, 0)):
    return fg.install_static(OBJ, build(), scale=1.0, location=location)


# ---------------------------------------------------------------- the review: the hall full of people

def _copy(src, at, turn_deg, name):
    import bpy
    o = src.copy()
    o.name = name
    o.location = at
    o.rotation_euler = (0, 0, math.radians(turn_deg))
    fg.ensure_collection().objects.link(o)
    return o


def populate():
    """The King by his throne, a line of guards either side of the carpet and two at the throne and the door,
    the court in groups between the columns and petitioners waiting."""
    import king
    import courtiers
    import lookout
    import townsfolk
    figs = [king.make((0.0, Y1 - WALL_T - 1.3, STEP_H * STEPS))]
    guard = lookout.make((50, 0, 0))
    for i, y in enumerate([-6.3, -4.75, -3.05, -1.35, 0.35, 2.05, 3.75]):
        for k in (-1, 1):
            figs.append(_copy(guard, (k * 0.95, y, 0), 90 * -k + 0, f"Guard {i} {k}"))
    for k in (-1, 1):
        figs.append(_copy(guard, (k * 1.1, DAIS_Y + 0.05, STEP_H), 180, f"Guard throne {k}"))
        figs.append(_copy(guard, (k * 1.2, Y0 + 0.6, 0), 180, f"Guard door {k}"))
    court = {n: courtiers.make(n, (60 + i, 0, 0)) for i, n in enumerate(courtiers.FIGURES)}
    groups = [("CineAdvisor", (0.0, DAIS_Y - 0.6, 0), 180),
              ("CineNoble", (-1.45, 3.6, 0), 120), ("CineLady", (-1.5, 4.1, 0), 150), ("CineNoble", (-1.2, 2.4, 0), 70),
              ("CineLady", (1.45, 3.7, 0), 220), ("CineNoble", (1.55, 4.2, 0), 200), ("CineLady", (1.3, 2.3, 0), 260),
              ("CineNoble", (-1.4, -0.2, 0), 100), ("CineLady", (1.4, 0.0, 0), 250),
              ("CineAdvisor", (1.5, 1.2, 0), 210)]
    for i, (n, at, turn) in enumerate(groups):
        figs.append(_copy(court[n], at, turn, f"{n} {i}"))
    for i, (n, at, turn) in enumerate((("CineMerchant", (-1.4, -2.4, 0), 120), ("CinePeasant", (1.45, -2.0, 0), 220),
                                       ("CinePeasantWoman", (1.6, -2.6, 0), 200))):
        figs.append(townsfolk.make(n, at))
        figs[-1].rotation_euler = (0, 0, math.radians(turn))
    for o in (guard, *court.values()):
        o.hide_render = True
    return figs


def render(out):
    import bpy
    from mathutils import Vector
    os.makedirs(out, exist_ok=True)
    room = make()
    figs = populate()
    sc = bpy.context.scene
    keep = {room.name} | {f.name for f in figs}
    for o in sc.objects:
        if o.type in ('MESH', 'CURVE', 'EMPTY', 'ARMATURE'):
            o.hide_render = o.name not in keep
    sc.render.engine = 'BLENDER_EEVEE'
    sc.render.resolution_x, sc.render.resolution_y = 1600, 900
    sc.view_settings.view_transform = 'Standard'
    sc.view_settings.look = 'Medium High Contrast' if 'Medium High Contrast' in [i.identifier for i in sc.view_settings.bl_rna.properties['look'].enum_items] else 'None'
    for name, energy, rx, rz in (("SetSun", 3.0, 35, -35), ("SetFill", 0.9, 55, 150), ("SetBack", 0.5, 60, 60)):
        light = bpy.data.objects.new(name, bpy.data.lights.new(name, 'SUN'))
        light.data.energy = energy
        sc.collection.objects.link(light)
        light.rotation_euler = (math.radians(rx), 0, math.radians(rz))
    w = sc.world or bpy.data.worlds.new("W")
    sc.world = w
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.85, 0.88, 1.0, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 0.55
    cam = bpy.data.objects.new("SetCam", bpy.data.cameras.new("SetCam"))
    sc.collection.objects.link(cam)
    sc.camera = cam
    for name, at, frm, lens in (("hall_from_door", (0, 3.0, 1.2), (0, Y0 + 0.5, 1.8), 22),
                                ("hall_overview", (0, 0.5, 0.5), (4.2, -9.5, 8.5), 24),
                                ("hall_from_throne", (0, -3.0, 0.8), (0.3, Y1 - 0.9, 2.2), 22),
                                ("hall_dais", (0, Y1 - 1.2, 1.2), (2.2, DAIS_Y - 2.4, 1.2), 26)):
        at, frm = Vector(at), Vector(frm)
        cam.location = frm
        cam.data.lens = lens
        cam.rotation_euler = (at - frm).to_track_quat('-Z', 'Y').to_euler()
        sc.render.filepath = os.path.join(out, name + ".png")
        bpy.ops.render.render(write_still=True)
        print("RENDERED", sc.render.filepath)


if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if argv and argv[0] == "render":
        render(argv[1])
    else:
        make()
