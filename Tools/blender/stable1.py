"""Stable1-lvl1 -- the Stable (Конюшня), where the knights are hired (the user, 2026-11-05: cavalry from
a new Stable; Design/features/army-and-soldiers.md). Footprint 3x3 like the Barracks and the
Workshop (no placeholder of its own yet: borrows the Barracks' box and stands beside it). Front
is -Y. Defence, so the castle's blue roof.

A long stable along the back -- stone below, timber hayloft above under the blue roof -- its
blue double doors in the middle, and along its front four open loose boxes under a blue lean-to --
plank partitions, half doors, straw -- a horse standing in each, its head out over the door (the
user: horses out of the wall did not read as stalls), a hay door in the loft with a pulley beam
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
from mathutils import Vector  # noqa: E402

OBJ = "Stable1-lvl1"
COATS = ('white', 'wood_light', 'wood_dark', 'stone_mid')     # white, chestnut, bay, grey


def horse(b, x, y_door, coat, turn=0.0):
    """A horse standing in its stall, facing the half door: its back above the door, the neck and
    head out over it, turned a little; ears, a dark mane, a blue halter. Every box closed (the
    user: open boxes read as faces missing)."""
    mane = 'wood_dark' if coat != 'wood_dark' else 'iron'
    # the body inside the stall, its back showing over the door
    b.obox((x, y_door + 0.25, 0.3), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.085, 0.17, 0.09, coat, bevel=0.03, skip=())
    t = math.radians(turn)
    fwd = Vector((math.sin(t), -math.cos(t), 0.0))
    side = Vector((math.cos(t), math.sin(t), 0.0))
    root = Vector((x, y_door + 0.12, 0.34))
    top = Vector((x, y_door, 0.5)) + fwd * 0.04
    b.rod(root, top, 0.11, coat)                                                                        # neck
    b.rod(root + Vector((0, 0.03, 0.07)), top + Vector((0, 0.03, 0.06)), 0.035, mane)                   # mane
    down = (fwd * 0.8 + Vector((0, 0, -0.6))).normalized()
    up = side.cross(down)
    c = top + fwd * 0.08 + Vector((0, 0, -0.01))
    b.obox(c, side, down, up, 0.05, 0.1, 0.055, coat, bevel=0.02, skip=())
    muzzle = c + down * 0.085
    b.obox(muzzle, side, down, up, 0.044, 0.032, 0.05, 'stone_light' if coat != 'white' else 'stone_mid', bevel=0.012, skip=())
    b.obox(c - down * 0.01, side, down, up, 0.053, 0.014, 0.058, 'banner_blue', skip=())                 # halter
    for k in (-1, 1):
        ear = top + side * (k * 0.028) + Vector((0, 0, 0.04))
        b.pyramid(ear.x, ear.y, ear.z, 0.013, 0.013, ear.z + 0.06, coat, sides=4)
        eye = c + side * (k * 0.052) - down * 0.02 + up * 0.015
        b.obox(eye, side, down, up, 0.004, 0.009, 0.009, 'window', skip=())


def stall_row(b, xa, xb, n, y_front, y_back, coats):
    """Open-fronted loose boxes along the stable's front wall: plank partitions, half doors with
    white X braces, straw on the floor, a blue lean-to roof from the wall -- a horse in each."""
    w = (xb - xa) / n
    for k in range(n + 1):
        px = xa + k * w
        b.box(px - 0.02, px + 0.02, y_front, y_back, 0, 0.44, 'wood_mid', bevel=0.004)                # partition
        b.box(px - 0.025, px + 0.025, y_front - 0.01, y_back, 0.44, 0.47, 'wood_dark')
        b.box(px - 0.03, px + 0.03, y_front - 0.03, y_front + 0.03, 0, 0.66, 'wood_dark', bevel=0.006)  # post
    b.box(xa - 0.03, xb + 0.03, y_front - 0.035, y_front + 0.035, 0.62, 0.67, 'wood_dark')              # head beam
    f = Facade(b, '-y', y_front)
    for k in range(n):
        a, c = xa + k * w + 0.025, xa + (k + 1) * w - 0.025
        b.box(a, c, y_front + 0.01, y_back, 0, 0.022, 'hay')                                            # straw
        f.rect(a, c, 0.02, 0.3, 0.03, 'wood_mid')                                                       # half door
        f.rect(a, c, 0.28, 0.31, 0.045, 'wood_dark')
        f.beam(a + 0.02, 0.04, c - 0.02, 0.27, 0.022, 0.04, 'plaster_white')
        f.beam(a + 0.02, 0.27, c - 0.02, 0.04, 0.022, 0.04, 'plaster_white')
        coat, turn = coats[k]
        horse(b, (a + c) / 2, y_front, coat, turn)
    d.shed_roof(b, 'x', xa - 0.04, xb + 0.04, y_back, y_front - 0.06, G_TOP + 0.08, 0.66, **d.DEFENCE)


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
    # the loose boxes along the front, two each side of the doors
    stall_row(b, x0 + 0.02, -0.32, 2, -0.16, y0, ((COATS[1], 15), (COATS[0], -10)))
    stall_row(b, 0.32, x1 - 0.02, 2, -0.16, y0, ((COATS[2], 10), (COATS[3], -15)))
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
    quintain(b, -0.75, -0.85)
    lance_rack(b, 0.2, -0.75)
    d.trough(b, 0.62, 1.17, -0.62, w=0.14, h=0.12)
    d.hay_bale(b, 1.15, -1.2, angle=10)
    d.hay_bale(b, 0.92, -1.25, angle=-5)
    d.hay_bale(b, 1.05, -1.18, angle=0, z0=0.15)
    d.rail_fence(b, [(-1.42, -1.42), (-0.3, -1.42)], h=0.24)
    d.rail_fence(b, [(0.3, -1.42), (1.42, -1.42)], h=0.24)
    d.bucket(b, 0.5, -0.9, 0)
    # a saddle and a blue saddle cloth over the fence rail
    b.box(-1.0, -0.78, -1.47, -1.37, 0.2, 0.3, 'banner_blue', bevel=0.01)
    b.box(-0.96, -0.82, -1.46, -1.38, 0.28, 0.33, 'wood_dark', bevel=0.012)
    return b


kit.run(OBJ, build, source_name="Barracks1-lvl1", offset=(0, -12.0, 0))
