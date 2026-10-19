"""The main base's own pieces (the user's sketch, 2026-10-19): the chief's house and the
impassable mountains the base is backed into.

    OrcChiefHouse1        3x3. The chief's hall: an overgrown stone footing and lower walls,
                          a great timber hall over them under a hide roof with a bone ridge,
                          a huge horned skull over the door, braziers and banners. The chief's
                          spawner: while it stands, a slain chief comes back.
    OrcChiefHouse1-Ruin   burnt out: stone stumps, the roof fallen in.
    OrcMountain1-A, -B    4x4 blocks of rock to build the mountains from: A a tall peak, B a
                          lower shoulder. Craggy, slate-grey, green on every ledge, a few firs.
                          They tile into a ridge and are turned for variety.

Uses the stone set's moss and ivy (orcstone1) so the base reads as one place.
"""
import math
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402

ST = kit.borrow("orcstone1.py")
STONE, CAP, MOSS, MOSS_DARK, IVY = ST['STONE'], ST['CAP'], ST['MOSS'], ST['MOSS_DARK'], ST['IVY']
HIDE = 'door'

# the hall's frame: ridge along Y, front gable facing -y (the camera)
HX, Y0, Y1 = 1.05, -0.95, 1.15
STONE_TOP = 0.7
EAVE = 1.25
RIDGE = 2.15


def build_house(lo, hi):
    b = Builder()
    # a stone platform, steps up to the door
    b.crag(0, 0.05, 1.3, 1.2, 0.2, o.ROCK, top='cobble', sides=10, taper=0.97, seed=8)
    for i, z in enumerate((0.06, 0.13)):
        b.box(-0.36 + 0.04 * i, 0.36 - 0.04 * i, Y0 - 0.42 + 0.12 * i, Y0 - 0.05, 0.0, z + 0.06, CAP)
    # stone lower walls, timber above, a hide roof with a bone ridge
    b.box(-HX, HX, Y0, Y1, 0.18, STONE_TOP, STONE, bevel=0.02)
    b.box(-HX - 0.03, HX + 0.03, Y0 - 0.03, Y1 + 0.03, STONE_TOP, STONE_TOP + 0.06, CAP)
    b.box(-HX + 0.05, HX - 0.05, Y0 + 0.05, Y1 - 0.05, STONE_TOP + 0.06, EAVE, o.TIMBER_LIGHT, bevel=0.01)
    for x in (-HX + 0.06, -0.35, 0.35, HX - 0.06):
        for y in (Y0 + 0.04, Y1 - 0.04):
            b.box(x - 0.05, x + 0.05, y - 0.05, y + 0.05, STONE_TOP, EAVE + 0.02, o.TIMBER)
    b.gable_block('y', Y0 + 0.05, Y1 - 0.05, -HX + 0.05, HX - 0.05, EAVE, RIDGE, o.TIMBER_LIGHT)
    slope = math.atan2(RIDGE - EAVE, HX + 0.2)
    span = math.hypot(HX + 0.2, RIDGE - EAVE)
    for side in (-1, 1):
        d = (-side * math.cos(slope), 0, math.sin(slope))
        n = (side * math.sin(slope), 0, math.cos(slope))
        c = (side * (HX + 0.2) / 2, (Y0 + Y1) / 2, (EAVE + RIDGE) / 2 - 0.02)
        b.obox((c[0] + n[0] * 0.03, c[1], c[2] + n[2] * 0.03), d, (0, 1, 0), n, span / 2,
               (Y1 - Y0) / 2 + 0.16, 0.03, HIDE, skip=())
        for y in (Y0 + 0.35, (Y0 + Y1) / 2, Y1 - 0.35):
            b.obox((c[0] + n[0] * 0.065, y, c[2] + n[2] * 0.065), d, (0, 1, 0), n, span / 2 - 0.02,
                   0.05, 0.008, o.RUST, skip=())
    # the bone ridge: a row of tusks along the top
    for i in range(7):
        y = Y0 + 0.1 + (Y1 - Y0 - 0.2) * i / 6
        for k in (-1, 1):
            o.horn(b, (0, y, RIDGE + 0.02), (k, 0, 0), 0.16, 0.03, up=1.6)
    # the door and the great horned skull on the front gable
    b.box(-0.24, 0.24, Y0 - 0.04, Y0 + 0.02, 0.18, 0.95, o.HAIR)
    b.box(-0.3, -0.24, Y0 - 0.07, Y0 + 0.02, 0.18, 1.02, o.TIMBER)
    b.box(0.24, 0.3, Y0 - 0.07, Y0 + 0.02, 0.18, 1.02, o.TIMBER)
    b.box(-0.32, 0.32, Y0 - 0.07, Y0 + 0.02, 0.96, 1.04, o.RUST)
    o.skull(b, 0, Y0 - 0.06, EAVE + 0.12, s=0.36)
    for k in (-1, 1):
        o.horn(b, (k * 0.17, Y0 - 0.02, EAVE + 0.38), (k, -0.15, 0), 0.5, 0.08, up=1.4, segments=4)
    # braziers by the door, banners at the front corners, a rack of trophies
    for k in (-1, 1):
        x = k * 0.62
        b.cylinder(x, Y0 - 0.28, 0.18, 0.42, 0.03, o.TIMBER, sides=5)
        b.cylinder(x, Y0 - 0.28, 0.42, 0.5, 0.09, o.METAL, sides=6)
        b.blob(x, Y0 - 0.28, 0.47, 0.08, 0.2, o.FIRE, sides=4)
        o.war_banner(b, k * 1.22, Y0 + 0.05, 2.3, w=0.2, drop=0.4)
    # overgrown: moss on the stone ledge, ivy down the stone, ferns round the platform
    ST['moss_cap'](b, -HX, HX, Y0 - 0.02, Y0 - 0.02, STONE_TOP + 0.06, seed=3)
    ST['moss_cap'](b, -HX - 0.02, -HX - 0.02, Y0, Y1, STONE_TOP + 0.06, seed=5)
    ST['moss_cap'](b, HX + 0.02, HX + 0.02, Y0, Y1, STONE_TOP + 0.06, seed=9)
    for i, (x, y, nx, ny) in enumerate(((-0.7, Y0, 0, -1), (0.8, Y0, 0, -1), (-HX, 0.2, -1, 0),
                                        (HX, -0.3, 1, 0), (HX, 0.7, 1, 0))):
        ST['ivy'](b, x + nx * 0.005, y + ny * 0.005, STONE_TOP + 0.04, 0.4, nx, ny, seed=i)
    for i in range(9):
        a = math.radians(20 + 40 * i)
        ST['foot_greenery'](b, 1.22 * math.cos(a), 0.05 + 1.15 * math.sin(a), seed=i)
    return b


def build_house_ruin(lo, hi):
    b = Builder()
    b.crag(0, 0.05, 1.3, 1.2, 0.2, o.ROCK, top='cobble', sides=10, taper=0.97, seed=8)
    o.scorch(b, 0, 0.1, 1.0, 1.0, seed=4)
    for i, (x0, x1, y0, y1, h) in enumerate(((-HX, -0.3, Y0, Y0 + 0.22, 0.55), (0.3, HX, Y0, Y0 + 0.22, 0.4),
                                             (-HX, -HX + 0.22, Y0, Y1, 0.45), (HX - 0.22, HX, Y0, 0.3, 0.6),
                                             (-HX, HX, Y1 - 0.22, Y1, 0.35))):
        b.box(x0, x1, y0, y1, 0.18, h, STONE, bevel=0.02)
    b.obox((0.1, 0.2, 0.5), (1, 0, 0), (0, 0.9, 0.44), (0, -0.44, 0.9), 0.9, 0.7, 0.03, HIDE, skip=())
    for x0, y0, x1, y1 in ((-0.8, -0.4, 0.7, 0.6), (0.6, -0.7, -0.4, 0.9)):
        o.fallen_log(b, (x0, y0, 0.3), (x1, y1, 0.42), r=0.06)
    o.skull(b, 0.2, Y0 - 0.25, 0.18, s=0.3, facing=(0.3, -1))
    o.horn(b, (-0.3, Y0 - 0.3, 0.22), (-1, -0.2, 0), 0.4, 0.07, up=0.4)
    for i in range(6):
        a = math.radians(40 + 55 * i)
        b.zbox(0.9 * math.cos(a), 0.1 + 0.85 * math.sin(a), 0.2, 0.26, 0.14, 0.1, 30 * i, MOSS)
    return b


# -- mountains ---------------------------------------------------------------------------------

def fir(b, x, y, z0, h):
    """A dark fir on a ledge: a trunk and three stacked cones."""
    b.cylinder(x, y, z0, z0 + h * 0.2, h * 0.05, o.TIMBER, sides=5)
    for i, (f, r) in enumerate(((0.15, 0.32), (0.42, 0.25), (0.66, 0.17))):
        b.pyramid(x, y, z0 + h * f, h * r, h * r, z0 + h * (f + 0.42), 'leaf' if i % 2 else 'grass_dark',
                  sides=6, rot_deg=15 * i)


def mountain(blocks, firs, ledges):
    """A mass of crags (cx, cy, rx, ry, h, seed), grass crowns on every one, firs and grassy
    ledges -- the green the user wanted on the base carried onto its rock."""
    def build(lo, hi):
        b = Builder()
        for cx, cy, rx, ry, h, seed in blocks:
            b.crag(cx, cy, rx, ry, h, o.STONE if seed % 2 else 'cobble', top=MOSS_DARK if seed % 3 else MOSS,
                   sides=7, taper=0.42, seed=seed)
        for x, y, z, r in ledges:
            b.crag(x, y, r, r * 0.8, 0.12, MOSS_DARK, top=MOSS, sides=6, taper=0.8, seed=int(abs(x * 10)))
        for x, y, z, h in firs:
            fir(b, x, y, z, h)
        return b
    return build


MOUNTAIN_A = mountain(
    blocks=[(0.0, 0.1, 1.55, 1.5, 1.2, 1), (0.1, 0.3, 1.25, 1.2, 2.6, 2), (-0.2, 0.35, 0.8, 0.75, 3.7, 3),
            (1.1, -0.9, 0.7, 0.6, 1.5, 4), (-1.2, -0.8, 0.6, 0.7, 1.9, 5), (1.0, 1.0, 0.55, 0.55, 2.0, 7)],
    firs=[(-1.5, -1.4, 0.0, 0.9), (1.55, -1.5, 0.0, 0.75), (-1.55, 1.3, 0.3, 0.8), (0.8, -0.2, 1.4, 0.7)],
    ledges=[(-0.9, 0.9, 1.6, 0.35), (0.9, 0.0, 1.9, 0.3)])

MOUNTAIN_B = mountain(
    blocks=[(0.0, 0.0, 1.8, 1.7, 0.9, 2), (-0.4, 0.3, 1.2, 1.1, 1.8, 3), (0.7, 0.4, 0.9, 0.8, 2.3, 5),
            (-1.1, -1.0, 0.6, 0.55, 1.2, 6), (1.2, -1.1, 0.55, 0.5, 1.0, 1)],
    firs=[(-1.5, 1.4, 0.0, 0.9), (0.2, -1.4, 0.0, 0.8), (1.5, 1.4, 0.0, 0.7), (-0.6, -0.4, 1.0, 0.65),
          (1.4, -0.3, 0.6, 0.6)],
    ledges=[(0.2, -0.5, 1.2, 0.4)])


def main():
    o.ensure_collection()
    o.place("OrcChiefHouse1", build_house, "BigWarehouse1-lvl1", o.GRID_X[1], -100.0)
    o.place("OrcChiefHouse1-Ruin", build_house_ruin, "BigWarehouse1-lvl1", o.GRID_X['ruin'], -100.0)
    o.place("OrcMountain1-A", MOUNTAIN_A, "Laboratory1-lvl1", o.GRID_X[1], -106.0)
    o.place("OrcMountain1-B", MOUNTAIN_B, "Laboratory1-lvl1", o.GRID_X[2] + 1.0, -106.0)
    bpy.ops.wm.save_mainfile()


main()
