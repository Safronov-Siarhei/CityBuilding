"""OrcMainTreasury1 (+Ruin) -- the chief's treasury on the main base (the user's pick,
2026-10-19): a building that does nothing in a fight and pays out big when broken, a reason to
go deep. In the main base's own stone-and-green style. Footprint 2x2.

A round vault of dressed stone under a moss-grown dome, a golden horned skull on top; its
gold-bound door between two horned stone guardians; out front, behind a low iron railing, the
gold the raids brought home. The ruin: the dome fallen in, the guardians toppled, the yard
emptied but for a few coins.

Borrows BigBarn1's 2x2 box.
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
STORE = kit.borrow("orcstorehouse1.py")
STONE, CAP, MOSS, MOSS_DARK, IVY = ST['STONE'], ST['CAP'], ST['MOSS'], ST['MOSS_DARK'], ST['IVY']
ivy, ferns = ST['ivy'], ST['foot_greenery']

SOURCE = "BigBarn1-lvl1"
VX, VY, VR = 0.0, 0.36, 0.5          # the vault: centre and radius
WALL = 0.78                           # top of its wall


def plinth(b):
    b.crag(0.0, 0.03, 0.84, 0.84, 0.1, o.ROCK, top='cobble', sides=10, taper=0.97, seed=5)


def guardian(b, x, y, standing=True):
    """A horned stone guardian on a block: a pillar for a body, a squared head, bone horns."""
    if not standing:
        b.obox((x, y - 0.18, 0.08), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.08, 0.26, 0.08, STONE,
               bevel=0.01, skip=())
        b.zbox(x + 0.05, y - 0.5, 0.0, 0.14, 0.09, 0.08, 30, STONE, bevel=0.01)
        return
    b.box(x - 0.12, x + 0.12, y - 0.12, y + 0.12, 0.1, 0.2, CAP, bevel=0.01)
    b.box(x - 0.08, x + 0.08, y - 0.08, y + 0.08, 0.2, 0.6, STONE, bevel=0.012)
    b.box(x - 0.1, x + 0.1, y - 0.1, y + 0.08, 0.6, 0.78, STONE, bevel=0.015)
    for k in (-1, 1):
        b.box(x + k * 0.045 - 0.02, x + k * 0.045 + 0.02, y - 0.105, y - 0.095, 0.69, 0.72, o.FIRE)
        o.horn(b, (x + k * 0.09, y, 0.74), (k, -0.2, 0), 0.13, 0.026, up=1.4)
    b.box(x - 0.05, x + 0.05, y - 0.11, y - 0.09, 0.62, 0.65, 'gold')
    ivy(b, x + 0.081, y, 0.58, 0.25, 1, 0, seed=int(x * 10) + 3)


def railing(b, y, x0, x1):
    """A low iron railing with spear points, a gap in the middle for the way in."""
    b.box(x0, -0.2, y - 0.012, y + 0.012, 0.16, 0.185, o.METAL)
    b.box(0.2, x1, y - 0.012, y + 0.012, 0.16, 0.185, o.METAL)
    n = 7
    for side in (-1, 1):
        for i in range(n):
            x = side * (0.22 + (abs(x0) - 0.22) * i / (n - 1))
            o.tube(b, [(x, y, 0.0), (x, y, 0.26), (x, y, 0.31)], [0.012, 0.012, 0.0], o.METAL, sides=4)


def build(lo, hi):
    b = Builder()
    plinth(b)
    # the vault
    b.cylinder(VX, VY, 0.1, WALL, VR, STONE, sides=12, bevel=0.01, rot_deg=15)
    b.cylinder(VX, VY, WALL, WALL + 0.06, VR + 0.04, CAP, sides=12, rot_deg=15)
    b.cylinder(VX, VY, 0.44, 0.5, VR + 0.012, 'gold', sides=12, rot_deg=15)     # a gold band
    b.dome(VX, VY, WALL + 0.06, VR + 0.02, 0.5, [MOSS_DARK, MOSS], sides=12, rot_deg=15)
    for i in range(6):
        a = math.radians(200 + 30 * i)
        b.blob(VX + (VR - 0.05) * math.cos(a), VY + (VR - 0.05) * math.sin(a), WALL + 0.05,
               0.07, 0.08, IVY if i % 2 else MOSS, sides=4)
    # the golden horned skull on top
    top = WALL + 0.06 + 0.5
    b.ball(VX, VY, top - 0.06, 0.09, 0.15, 'gold', sides=7)
    for k in (-1, 1):
        o.horn(b, (VX + k * 0.07, VY, top + 0.04), (k, 0, 0), 0.16, 0.03, color='gold', up=1.3)
    for k in (-1, 1):
        b.box(VX + k * 0.035 - 0.018, VX + k * 0.035 + 0.018, VY - 0.088, VY - 0.07, top, top + 0.03, o.HAIR)
    # gold-bound door facing -y
    dy = VY - VR
    b.box(-0.17, 0.17, dy - 0.03, dy + 0.06, 0.1, 0.6, o.HAIR)
    for k in (-1, 1):
        b.box(k * 0.17 - 0.035, k * 0.17 + 0.035, dy - 0.06, dy + 0.06, 0.1, 0.66, STONE, bevel=0.01)
    b.box(-0.24, 0.24, dy - 0.07, dy + 0.06, 0.6, 0.7, CAP, bevel=0.01)
    for z in (0.2, 0.42):
        b.box(-0.15, 0.15, dy - 0.045, dy - 0.03, z, z + 0.035, 'gold')
    b.box(0.08, 0.13, dy - 0.06, dy - 0.04, 0.3, 0.36, 'gold')
    for i, a in enumerate((120, 60, 20, 160)):
        r = math.radians(a)
        ivy(b, VX + VR * math.cos(r), VY + VR * math.sin(r), WALL, 0.42, math.cos(r), math.sin(r), seed=i + 2)
    # the guardians
    guardian(b, -0.36, -0.22)
    guardian(b, 0.36, -0.22)
    # the yard: gold out front behind the railing, where the camera sees it
    STORE['gold_heap'](b, -0.44, -0.55, 0.27)
    STORE['gold_heap'](b, 0.42, -0.56, 0.25)
    STORE['gold_heap'](b, 0.0, -0.4, 0.16)
    STORE['chest'](b, -0.7, -0.12, iron=True)
    STORE['chest'](b, 0.7, -0.1, iron=True)
    STORE['chest'](b, 0.12, -0.7, iron=True)
    railing(b, -0.84, -0.8, 0.8)
    for x in (-0.8, 0.8):
        b.cylinder(x, -0.84, 0.0, 0.38, 0.04, STONE, sides=6)
        b.blob(x, -0.84, 0.38, 0.05, 0.07, 'gold', sides=5)
    for i, (x, y) in enumerate(((-0.72, 0.45), (0.7, 0.5), (-0.5, 0.75), (0.48, 0.76))):
        ferns(b, x, y, seed=i)
    return b


def build_ruin(lo, hi):
    b = Builder()
    plinth(b)
    o.scorch(b, 0.0, 0.0, 0.7, 0.6, seed=4)
    # the vault's broken wall: segments of uneven height, the dome fallen in
    for i in range(12):
        if i in (8, 9):
            continue                                   # the door side, smashed through
        a0, a1 = math.radians(15 + 30 * i), math.radians(45 + 30 * i)
        h = 0.18 + 0.12 * ((i * 5) % 4)
        pts = [(VX + VR * math.cos(a0), VY + VR * math.sin(a0)), (VX + VR * math.cos(a1), VY + VR * math.sin(a1))]
        mx, my = (pts[0][0] + pts[1][0]) / 2, (pts[0][1] + pts[1][1]) / 2
        ang = math.degrees(math.atan2(pts[1][1] - pts[0][1], pts[1][0] - pts[0][0]))
        b.zbox(mx, my, 0.1, 0.1 + h, 0.145, 0.05, ang, STONE, bevel=0.008)
        if i % 3 == 0:
            b.zbox(mx, my, 0.1 + h, 0.12 + h, 0.12, 0.045, ang, MOSS_DARK)
    for i, (x, y, r) in enumerate(((0.05, 0.3, 0.16), (-0.2, 0.15, 0.12), (0.25, 0.1, 0.1))):
        o.rubble(b, x, y, r=r, seed=i + 3, color=STONE)
    b.obox((0.1, 0.4, 0.2), (0.9, 0.4, 0), (-0.3, 0.7, 0.6), (0.2, -0.6, 0.77), 0.24, 0.16, 0.035,
           MOSS_DARK, skip=())
    b.ball(-0.6, -0.55, 0.0, 0.08, 0.12, 'gold', sides=6)        # the skull, rolled off
    guardian(b, -0.36, -0.22)
    guardian(b, 0.36, -0.1, standing=False)
    for dx, dy in ((-0.3, -0.6), (0.1, -0.7), (0.4, -0.5), (-0.1, -0.45)):
        b.cylinder(dx, dy, 0.0, 0.015, 0.03, 'gold', sides=6)
    b.box(0.56, 0.8, -0.28, -0.12, 0.0, 0.1, o.TIMBER, bevel=0.01)
    b.obox((0.68, 0.0, 0.14), (1, 0, 0), (0, 0.3, 0.95), (0, -0.95, 0.3), 0.13, 0.08, 0.02, o.RUST)
    for i in range(4):
        b.zbox(-0.55 + 0.35 * i, -0.8, 0.0, 0.02, 0.14, 0.012, 8 * i - 10, o.METAL)
    for i, (x, y) in enumerate(((-0.72, 0.45), (0.7, 0.5), (0.3, -0.3))):
        ferns(b, x, y, seed=i + 7)
    return b


def main():
    o.ensure_collection()
    y = -144.0
    o.place("OrcMainTreasury1", build, SOURCE, o.GRID_X[1], y)
    o.place("OrcMainTreasury1-Ruin", build_ruin, SOURCE, o.GRID_X['ruin'], y)
    bpy.ops.wm.save_mainfile()


main()
