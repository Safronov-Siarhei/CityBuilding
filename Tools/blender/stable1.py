"""Stable1-lvl1 -- the Stable (Конюшня), where the knights are hired (the user, 2026-11-05: cavalry from
a new Stable; Design/features/army-and-soldiers.md). Footprint 3x3 like the Barracks and the
Workshop (no placeholder of its own yet: borrows the Barracks' box and stands beside it). Front
is -Y. Defence, so the castle's blue roof.

A long stable along the back -- stone below, timber hayloft above under the blue roof -- its
blue double doors in the middle, two stalls each side with horses looking out over the half
doors (no dark holes: the user does not want them), a hay door in the loft with a pulley beam
and a bale on the rope, a gold horseshoe on a blue sign. The yard in front: a quintain to tilt
at with a lance, a rack of lances, a water trough, hay bales, a rail fence.

    blender --background Buildings-Size1-Models.blend --python stable1.py
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP, L_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "Stable1-lvl1"
COATS = ('white', 'wood_light', 'wood_dark', 'stone_mid')     # white, chestnut, bay, grey


def horse_head(b, x, y_wall, z, coat, turn=0.0):
    """A horse looking out over a stall's half door: its neck out of the opening, the head
    turned a little, ears, a dark mane, a blue halter with a gold buckle."""
    mane = 'wood_dark' if coat != 'wood_dark' else 'iron'
    t = math.radians(turn)
    fwd = (math.sin(t), -math.cos(t), 0.0)
    nx, ny = x + fwd[0] * 0.13, y_wall + fwd[1] * 0.13
    b.rod((x, y_wall + 0.03, z - 0.04), (nx, ny, z + 0.08), 0.12, coat)                                # neck
    b.rod((x, y_wall + 0.05, z + 0.04), (nx, ny + 0.03, z + 0.15), 0.04, mane)                         # mane
    hx, hy = nx + fwd[0] * 0.09, ny + fwd[1] * 0.09
    down = (fwd[0] * 0.8, fwd[1] * 0.8, -0.6)
    n = math.sqrt(sum(c * c for c in down))
    down = tuple(c / n for c in down)
    side = (math.cos(t), math.sin(t), 0.0)
    up = (side[1] * down[2] - side[2] * down[1], side[2] * down[0] - side[0] * down[2], side[0] * down[1] - side[1] * down[0])
    c = (hx, hy, z + 0.06)
    b.obox(c, side, down, up, 0.055, 0.11, 0.06, coat, bevel=0.022)
    muzzle = (c[0] + down[0] * 0.09, c[1] + down[1] * 0.09, c[2] + down[2] * 0.09)
    b.obox(muzzle, side, down, up, 0.048, 0.035, 0.054, 'stone_light' if coat != 'white' else 'stone_mid', bevel=0.014)
    b.obox(c, side, down, up, 0.058, 0.016, 0.064, 'banner_blue')                                     # halter
    for k in (-1, 1):
        ex, ey = hx - fwd[0] * 0.05 + side[0] * k * 0.03, hy - fwd[1] * 0.05 + side[1] * k * 0.03
        b.pyramid(ex, ey, z + 0.13, 0.014, 0.014, z + 0.19, coat, sides=4)                             # ears
        b.box(c[0] + side[0] * k * 0.056 - 0.005, c[0] + side[0] * k * 0.056 + 0.005,
              c[1] - 0.008, c[1] + 0.008, c[2] + 0.015, c[2] + 0.03, 'window')                        # eyes


def stall(f, b, s, coat, turn):
    """A stall's half door: the frame, the lower door with a white X brace, the opening above it
    filled by a horse."""
    w = 0.3
    f.rect(s - w / 2 - 0.03, s + w / 2 + 0.03, FOOTING, G_TOP - 0.06, 0.012, 'wood_dark')
    f.rect(s - w / 2, s + w / 2, FOOTING + 0.3, G_TOP - 0.09, 0.006, 'wood_dark')                      # the stall inside, brown
    f.rect(s - w / 2, s + w / 2, FOOTING, FOOTING + 0.3, 0.03, 'wood_mid')
    f.beam(s - w / 2 + 0.02, FOOTING + 0.02, s + w / 2 - 0.02, FOOTING + 0.28, 0.022, 0.04, 'plaster_white')
    f.beam(s - w / 2 + 0.02, FOOTING + 0.28, s + w / 2 - 0.02, FOOTING + 0.02, 0.022, 0.04, 'plaster_white')
    f.rect(s - w / 2 - 0.02, s + w / 2 + 0.02, FOOTING + 0.3, FOOTING + 0.33, 0.045, 'wood_dark')        # the door's top rail
    horse_head(b, s, f.origin.y, FOOTING + 0.36, coat, turn)


def quintain(b, x, y):
    """The tilting post: a pole, a turning arm with a blue shield at one end and a sandbag at the
    other -- what a knight rides at with his lance."""
    b.box(x - 0.03, x + 0.03, y - 0.03, y + 0.03, 0, 0.62, 'wood_dark', bevel=0.006)
    b.box(x - 0.07, x + 0.07, y - 0.07, y + 0.07, 0, 0.05, 'stone_mid', bevel=0.01)
    b.rod((x - 0.32, y, 0.6), (x + 0.32, y, 0.6), 0.035, 'wood_mid')
    b.cylinder(x, y, 0.58, 0.63, 0.04, 'iron', sides=8)
    f = Facade(b, '-y', y - 0.02)
    f.poly([(x - 0.4, 0.66), (x - 0.24, 0.66), (x - 0.24, 0.52), (x - 0.32, 0.44), (x - 0.4, 0.52)], 0.02, 'banner_blue')
    f.rect(x - 0.33, x - 0.31, 0.47, 0.64, 0.025, 'gold')
    b.rod((x + 0.3, y, 0.6), (x + 0.3, y, 0.48), 0.008, 'burlap')
    b.blob(x + 0.3, y, 0.36, 0.055, 0.13, 'burlap', sides=6)


def lance_rack(b, x, y):
    b.box(x - 0.3, x + 0.3, y - 0.03, y + 0.03, 0.42, 0.45, 'wood_dark')
    for px in (x - 0.28, x + 0.28):
        b.box(px - 0.02, px + 0.02, y - 0.02, y + 0.02, 0, 0.47, 'wood_dark')
    for i in range(4):
        sx = x - 0.2 + i * 0.13
        b.rod((sx, y - 0.1, 0.01), (sx, y + 0.02, 0.95), 0.025, 'wood_light')
        b.pyramid(sx, y + 0.025, 0.95, 0.018, 0.018, 1.03, 'stone_light')
        b.box(sx - 0.005, sx + 0.03, y + 0.005, y + 0.02, 0.8, 0.9, 'banner_blue' if i % 2 else 'gold')   # pennants


def build(lo, hi):
    b = Builder()
    x0, x1, y0, y1 = -1.36, 1.36, 0.3, 1.32
    yc, hs = (y0 + y1) / 2, (y1 - y0) / 2
    zr = L_TOP + hs * d.PITCH

    d.footing(b, x0, x1, y0, y1)
    b.box(x0, x1, y0, y1, FOOTING, G_TOP, 'stone_light', bevel=d.WALL_BEVEL)
    b.box(x0 + 0.02, x1 - 0.02, y0 + 0.02, y1 - 0.02, G_TOP, L_TOP, 'plaster', bevel=d.WALL_BEVEL)
    b.gable_block('x', x0 + 0.02, x1 - 0.02, y0 + 0.02, y1 - 0.02, L_TOP, zr, 'plaster')
    d.roof(b, 0, yc, 'x', x1 + 0.04, hs, L_TOP, **d.DEFENCE)

    # the front: the blue double doors in the middle, two stalls each side, the loft above
    f = Facade(b, '-y', y0)
    d.barn_door(f, 0, base=FOOTING, w=0.46, h=0.5, color='roof_blue')
    f.rect(-0.27, 0.27, FOOTING + 0.5, FOOTING + 0.55, 0.04, 'wood_dark')
    for s, coat, turn in ((-1.05, COATS[1], 20), (-0.6, COATS[0], -10), (0.6, COATS[2], 15), (1.05, COATS[3], -20)):
        stall(f, b, s, coat, turn)
    d.timber(f, x0, x1, G_TOP, L_TOP, (x0 + 0.025, -0.8, -0.3, 0.3, 0.8, x1 - 0.025))
    # the hay door in the loft, hay in it, the pulley beam, a bale on the rope
    f.rect(-0.16, 0.16, G_TOP + 0.02, L_TOP - 0.04, 0.012, 'wood_dark')
    f.rect(-0.13, 0.13, G_TOP + 0.04, L_TOP - 0.06, 0.02, 'hay')
    b.box(-0.02, 0.02, y0 - 0.22, y0 + 0.02, L_TOP - 0.03, L_TOP, 'wood_dark')                         # pulley beam
    b.box(-0.012, 0.012, y0 - 0.21, y0 - 0.19, L_TOP - 0.06, L_TOP - 0.03, 'iron')
    # the sign: a gold horseshoe on a blue board, over the doors
    f.rect(-0.12, 0.12, FOOTING + 0.56, G_TOP + 0.02, 0.025, 'banner_blue')
    for k in range(7):
        a0, a1 = math.radians(200 + k * 20), math.radians(220 + k * 20)
        f.beam(math.cos(a0) * 0.045, FOOTING + 0.6 + math.sin(a0) * 0.045,
               math.cos(a1) * 0.045, FOOTING + 0.6 + math.sin(a1) * 0.045, 0.024, 0.035, 'gold')
    # the back and ends
    f = Facade(b, '+y', y1)
    d.timber(f, x0, x1, G_TOP, L_TOP, ())
    for s in (-0.8, 0.0, 0.8):
        d.plain_window(f, s, FOOTING)
    for nm, plane in (('-x', x0), ('+x', x1)):
        f = Facade(b, nm, plane)
        d.timber(f, y0, y1, G_TOP, L_TOP, (y0 + 0.025, yc, y1 - 0.025))
        d.gable_frame(f, yc, hs, L_TOP, zr)
        # a loft hatch heaped with hay (no dark hole, no beam out of the plot)
        f.rect(yc - 0.135, yc + 0.135, L_TOP + 0.05, L_TOP + 0.3, 0.012, 'wood_dark')
        f.rect(yc - 0.11, yc + 0.11, L_TOP + 0.05, L_TOP + 0.27, 0.025, 'hay')
        f.rect(yc - 0.09, yc + 0.09, L_TOP + 0.05, L_TOP + 0.12, 0.045, 'roof_straw_dark')

    # the yard
    b.box(-1.44, 1.44, -1.44, y0 - 0.1, 0, 0.012, 'sand')
    quintain(b, -0.75, -0.75)
    lance_rack(b, 0.95, -0.5)
    d.trough(b, 0.55, 1.1, -0.85, w=0.14, h=0.12)
    d.hay_bale(b, 1.15, -1.2, angle=10)
    d.hay_bale(b, 0.92, -1.25, angle=-5)
    d.hay_bale(b, 1.05, -1.18, angle=0, z0=0.15)
    d.rail_fence(b, [(-1.42, -1.42), (-0.3, -1.42)], h=0.24)
    d.rail_fence(b, [(0.3, -1.42), (1.42, -1.42)], h=0.24)
    d.bucket(b, 0.45, -0.75, 0)
    # a saddle and a blue saddle cloth over the fence rail
    b.box(-1.0, -0.78, -1.47, -1.37, 0.2, 0.3, 'banner_blue', bevel=0.01)
    b.box(-0.96, -0.82, -1.46, -1.38, 0.28, 0.33, 'wood_dark', bevel=0.012)
    return b


kit.run(OBJ, build, source_name="Barracks1-lvl1", offset=(0, -12.0, 0))
