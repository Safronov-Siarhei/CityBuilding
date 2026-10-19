"""The rest of the main base, in ITS OWN style (the user's rule, 2026-10-19): every building of
the great portal's base has a stone-and-green model of its own that the small camps never use.
Dressed stone gone green -- moss blankets, ivy, ferns -- under TURF roofs, with the orcs' iron
and bone on it.

    OrcMainPortal1 (+Ruin)        4x4  the great portal: two stone keeps and a stone lintel round
                                       the whirlpool, a glowing rune circle on the dais
    OrcMainTotem1 (+Ruin)         1x1  a stone obelisk with the portal's runes and a horned skull
    OrcMainStorehouse1 (+Ruin)    2x2  a stone storehouse under a turf roof, the loot heaped out front
    OrcMainBarracks1 (+Ruin)      2x2  the grunts' spawner: a stone longhouse with a turf roof
    OrcMainBrutePit1 (+Ruin)      2x2  a ring of dressed stone round the pit, a gong on stone posts
    OrcMainThrowerRange1 (+Ruin)  2x2  a stone back wall with a turf shelter, targets on stone posts
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
RANGE = kit.borrow("orcthrowerrange1.py")
STONE, CAP, MOSS, MOSS_DARK, IVY = ST['STONE'], ST['CAP'], ST['MOSS'], ST['MOSS_DARK'], ST['IVY']
moss_cap, ivy, ferns = ST['moss_cap'], ST['ivy'], ST['foot_greenery']


def turf_roof(b, axis, a0, a1, half_span, ze, zr, over=0.12, cross=0.0):
    """A gable of turf: two thick green slabs, a ridge of tufts, grass hanging over the eaves."""
    slope = math.atan2(zr - ze, half_span + over)
    span = math.hypot(half_span + over, zr - ze)
    half_len = (a1 - a0) / 2 + over
    mid = (a0 + a1) / 2
    for side in (-1, 1):
        if axis == 'y':
            d = (-side * math.cos(slope), 0, math.sin(slope))
            n = (side * math.sin(slope), 0, math.cos(slope))
            c = (cross + side * (half_span + over) / 2, mid, (ze + zr) / 2)
            along = (0, 1, 0)
        else:
            d = (0, -side * math.cos(slope), math.sin(slope))
            n = (0, side * math.sin(slope), math.cos(slope))
            c = (mid, cross + side * (half_span + over) / 2, (ze + zr) / 2)
            along = (1, 0, 0)
        b.obox((c[0] + n[0] * 0.05, c[1] + n[1] * 0.05, c[2] + n[2] * 0.05), d, along, n,
               span / 2, half_len, 0.05, MOSS_DARK, skip=())
        # a lighter top skin, a little short of the edges so the dark shows as the sod's side
        b.obox((c[0] + n[0] * 0.105, c[1] + n[1] * 0.105, c[2] + n[2] * 0.105), d, along, n,
               span / 2 - 0.04, half_len - 0.04, 0.006, MOSS, skip=())
    for i in range(5):
        t = a0 + (a1 - a0) * (i + 0.5) / 5
        x, y = (cross, t) if axis == 'y' else (t, cross)
        b.blob(x, y, zr + 0.06, 0.07, 0.14, IVY if i % 2 else MOSS, sides=4)


# -- the main portal (4x4) ---------------------------------------------------------------------

DAIS = 0.25
KEEP_X = 1.2
LINTEL = 2.8
OPEN_W, OPEN_TOP = 0.78, 2.62


def arch_outline(half_w, z0, top, segments=10):
    spring = top - half_w
    pts = [(-half_w, z0), (half_w, z0)]
    for i in range(segments + 1):
        t = math.pi * i / segments
        pts.append((half_w * math.cos(t), spring + half_w * math.sin(t)))
    return pts


def keep(b, x, h, r=0.46):
    """One of the two stone keeps that hold the lintel: octagonal, banded, crenellated, green."""
    b.frustum(x, 0, DAIS, h, r, r * 0.88, STONE, sides=8)
    for z in (1.0, 2.0):
        if z < h - 0.1:
            rr = r * (1 - 0.12 * (z - DAIS) / (h - DAIS))
            b.frustum(x, 0, z, z + 0.07, rr + 0.02, rr + 0.02, o.RUST, sides=8)
    if h > 2.0:
        b.frustum(x, 0, h, h + 0.08, r * 0.88 + 0.06, r * 0.88 + 0.06, CAP, sides=8)
        for i in range(8):
            a = math.radians(22.5 + 45 * i)
            b.zbox(x + r * 0.85 * math.cos(a), r * 0.85 * math.sin(a), h + 0.08, h + 0.26, 0.08, 0.05,
                   math.degrees(a) + 90, CAP)
        b.zbox(x, 0, h + 0.08, h + 0.12, r * 0.7, r * 0.7, 22.5, MOSS_DARK)
    for i, a in enumerate((-70, 30, 150, 230)):
        rr = r * 0.97
        top = min(h, 2.4) - 0.05
        length = min(0.7 + 0.5 * ST['_rand'](i, int(x * 10) + 7), top - DAIS - 0.05)
        ivy(b, x + rr * math.cos(math.radians(a)), rr * math.sin(math.radians(a)), top, length,
            math.cos(math.radians(a)), math.sin(math.radians(a)), seed=i)


def whirlpool(b, colours):
    b.prism(arch_outline(OPEN_W, DAIS + 0.02, OPEN_TOP), (0, 0.03, 0), (1, 0, 0), (0, 0, 1), 0.06,
            o.PORTAL_DARK, back_cap=True)
    cz = DAIS + (OPEN_TOP - DAIS) * 0.5
    for (rx, rz, depth), color in zip(((0.64, 1.05, 0.08), (0.47, 0.78, 0.1), (0.31, 0.52, 0.12), (0.12, 0.2, 0.14)),
                                      colours):
        pts = [(rx * math.cos(2 * math.pi * i / 12), cz + rz * math.sin(2 * math.pi * i / 12)) for i in range(12)]
        b.prism(pts, (0, depth / 2, 0), (1, 0, 0), (0, 0, 1), depth, color, back_cap=True)


def dais(b, runes=True, lit=True):
    b.crag(0, 0, 1.66, 1.62, DAIS, o.ROCK, top='cobble', sides=12, taper=0.98, seed=5)
    for i, z in enumerate((0.08, 0.16)):
        b.box(-0.8 + 0.1 * i, 0.8 - 0.1 * i, -1.85 + 0.15 * i, -1.35, 0.0, z + 0.08, CAP)
    if runes:
        for i in range(20):
            t = 2 * math.pi * (i + 0.5) / 20
            x, y = 1.5 * math.cos(t), 1.42 * math.sin(t)
            color = (o.PORTAL if i % 2 else o.PORTAL_DARK) if lit else o.PORTAL_DARK
            b.zbox(x, y, DAIS - 0.01, DAIS + 0.025, 0.11, 0.05, math.degrees(t) + 90, color)
    for i in range(10):
        a = math.radians(18 + 36 * i)
        ferns(b, 1.66 * math.cos(a), 1.62 * math.sin(a), seed=i)


def build_main_portal(lo, hi):
    b = Builder()
    dais(b)
    for k in (-1, 1):
        keep(b, k * KEEP_X, LINTEL + 0.5)
    # the lintel: a span of dressed stone between the keeps, a moss blanket and merlons on top
    b.box(-KEEP_X, KEEP_X, -0.32, 0.32, LINTEL, LINTEL + 0.42, STONE, bevel=0.02)
    b.box(-KEEP_X, KEEP_X, -0.35, 0.35, LINTEL + 0.42, LINTEL + 0.48, CAP)
    b.box(-KEEP_X + 0.05, KEEP_X - 0.05, -0.33, 0.33, LINTEL + 0.48, LINTEL + 0.51, MOSS_DARK)
    for i in range(7):
        x = -0.9 + 0.3 * i
        b.box(x - 0.08, x + 0.08, -0.33, 0.33, LINTEL + 0.48, LINTEL + 0.66, CAP)
        if i % 2 == 0:
            b.box(x - 0.085, x + 0.085, -0.335, 0.335, LINTEL + 0.66, LINTEL + 0.7, MOSS)
    b.box(-KEEP_X + 0.4, KEEP_X - 0.4, -0.36, -0.33, LINTEL + 0.08, LINTEL + 0.14, o.RUST)
    # the stone jambs inside the keeps, the whirlpool between
    for k in (-1, 1):
        b.box(k * (OPEN_W + 0.02), k * (KEEP_X - 0.3), -0.25, 0.25, DAIS, LINTEL, STONE, bevel=0.015)
    whirlpool(b, (o.PORTAL, o.PORTAL_DARK, o.PORTAL, o.PORTAL_CORE))
    # the great horned skull over the arch, iron spikes along the lintel's face
    o.skull(b, 0, -0.38, LINTEL + 0.02, s=0.42)
    for k in (-1, 1):
        o.horn(b, (k * 0.2, -0.36, LINTEL + 0.32), (k, -0.2, 0), 0.6, 0.09, up=1.5, segments=4)
    for i in range(8):
        x = -1.05 + 0.3 * i
        if abs(x) < 0.3:
            continue
        o.tube(b, [(x, -0.33, LINTEL + 0.3), (x, -0.48, LINTEL + 0.36)], [0.025, 0.0], o.METAL, sides=4)
    ivy(b, -0.6, -0.33, LINTEL + 0.42, 0.45, 0, -1, seed=2)
    ivy(b, 0.75, -0.33, LINTEL + 0.42, 0.35, 0, -1, seed=5)
    # braziers on stone posts by the steps, banners on the keeps
    for k in (-1, 1):
        x = k * 0.95
        b.box(x - 0.1, x + 0.1, -1.45, -1.25, 0.0, 0.5, STONE, bevel=0.01)
        b.cylinder(x, -1.35, 0.5, 0.6, 0.1, o.METAL, sides=6)
        b.blob(x, -1.35, 0.56, 0.09, 0.22, o.FIRE, sides=4)
        o.war_banner(b, k * KEEP_X, 0.0, LINTEL + 1.35, w=0.22, drop=0.45)
    return b


def build_main_portal_ruin(lo, hi):
    """Closed: one keep broken off at half height, the other a stump, the lintel down across
    the dais, no whirlpool -- only a scorch and the runes gone dark under the moss."""
    b = Builder()
    dais(b, lit=False)
    keep(b, -KEEP_X, 1.5)
    keep(b, KEEP_X, 0.7)
    o.scorch(b, 0, 0, 0.9, 0.6, seed=6)
    ax = (0.95, -0.28, 0.12)
    b.obox((0.1, -0.7, 0.62), ax, (0.28, 0.96, 0), (-0.12, 0.03, 0.99), 1.1, 0.3, 0.2, STONE, bevel=0.02)
    for i, (x, y) in enumerate(((0.9, 0.7), (-0.5, 0.8), (1.3, -0.9), (-1.3, -0.7))):
        b.crag(x, y, 0.2, 0.17, 0.2, STONE, sides=5, seed=i)
    o.skull(b, -0.6, -1.0, DAIS, s=0.35, facing=(0.3, -1))
    for x, y in ((0.4, 0.4), (-0.3, -0.2), (0.9, -0.3)):
        b.zbox(x, y, DAIS, DAIS + 0.05, 0.14, 0.1, 30, MOSS)
    return b


# -- the main totem (1x1) ----------------------------------------------------------------------

def build_main_totem(lo, hi):
    b = Builder()
    b.box(-0.4, 0.4, -0.4, 0.4, 0.0, 0.2, o.ROCK, bevel=0.02)
    b.box(-0.42, 0.42, -0.42, 0.42, 0.2, 0.25, CAP)
    b.zbox(0, 0, 0.25, 0.28, 0.36, 0.36, 0, MOSS_DARK)
    b.frustum(0, 0, 0.25, 1.75, 0.25, 0.15, STONE, sides=4, rot_deg=45)
    for z0, z1 in ((0.55, 0.85), (1.05, 1.3)):
        b.box(-0.05, 0.05, -0.245 + 0.02 * (z0 - 0.25), -0.2, z0, z1, o.PORTAL)
    b.box(-0.15, 0.15, -0.23, -0.2, 1.4, 1.46, o.RUST)
    o.skull(b, 0, -0.2, 1.47, s=0.13)
    b.cylinder(0, 0, 1.75, 1.82, 0.17, o.METAL, sides=6)
    o.iron_spikes(b, 0, 0, 0.17, 6, 1.79, length=0.1, r=0.02)
    o.skull(b, 0, -0.02, 1.84, s=0.24)
    for k in (-1, 1):
        o.horn(b, (k * 0.1, 0, 1.98), (k, 0.1, 0), 0.26, 0.05, up=1.5, segments=4)
    b.zbox(0, 0, 1.82, 1.85, 0.12, 0.12, 45, MOSS)
    for i, (nx, ny) in enumerate(((1, 0), (-1, 0), (0, 1))):
        ivy(b, nx * 0.2, ny * 0.2, 1.5, 0.9, nx, ny, seed=i)
    for k, (x, y) in enumerate(((-0.33, -0.33), (0.33, -0.33), (0.33, 0.33), (-0.33, 0.33))):
        b.box(x - 0.05, x + 0.05, y - 0.05, y + 0.05, 0.25, 0.42, STONE)
        b.cylinder(x, y, 0.42, 0.47, 0.06, o.METAL, sides=6)
        b.blob(x, y, 0.45, 0.045, 0.13, o.FIRE, sides=4)
    return b


def build_main_totem_ruin(lo, hi):
    b = Builder()
    b.box(-0.4, 0.4, -0.4, 0.4, 0.0, 0.2, o.ROCK, bevel=0.02)
    b.frustum(0, 0, 0.2, 0.55, 0.25, 0.22, STONE, sides=4, rot_deg=45)
    b.obox((0.08, 0.12, 0.33), (0.3, 0.95, 0.0), (-0.95, 0.3, 0), (0, 0, 1), 0.3, 0.17, 0.13, STONE)
    o.skull(b, -0.25, -0.25, 0.2, s=0.2, facing=(0.3, -1))
    b.zbox(-0.2, 0.25, 0.2, 0.25, 0.12, 0.09, 20, MOSS)
    o.scorch(b, 0, 0, 0.36, 0.34, seed=1)
    return b


# -- stone houses under turf: the storehouse and the barracks (2x2) ----------------------------

def stone_house(b, x0, x1, y0, y1, h, roof_axis, ridge, door_face='-y', door_w=0.32):
    b.crag((x0 + x1) / 2, (y0 + y1) / 2, (x1 - x0) / 2 + 0.04, (y1 - y0) / 2 + 0.04, 0.12, o.ROCK,
           top='cobble', sides=8, taper=0.97, seed=3)
    b.box(x0, x1, y0, y1, 0.1, h, STONE, bevel=0.02)
    b.box(x0 - 0.02, x1 + 0.02, y0 - 0.02, y1 + 0.02, h, h + 0.05, CAP)
    if roof_axis == 'y':
        b.gable_block('y', y0, y1, x0, x1, h + 0.05, ridge, STONE)
        turf_roof(b, 'y', y0, y1, (x1 - x0) / 2, h + 0.05, ridge, cross=(x0 + x1) / 2)
    else:
        b.gable_block('x', x0, x1, y0, y1, h + 0.05, ridge, STONE)
        turf_roof(b, 'x', x0, x1, (y1 - y0) / 2, h + 0.05, ridge, cross=(y0 + y1) / 2)
    cx = (x0 + x1) / 2
    b.box(cx - door_w, cx + door_w, y0 - 0.02, y0 + 0.02, 0.1, 0.62, o.HAIR)
    for k in (-1, 1):
        b.box(cx + k * door_w - 0.04, cx + k * door_w + 0.04, y0 - 0.05, y0 + 0.02, 0.1, 0.68, o.TIMBER)
    b.box(cx - door_w - 0.06, cx + door_w + 0.06, y0 - 0.05, y0 + 0.02, 0.64, 0.72, o.RUST)
    for i, (x, y, nx, ny) in enumerate(((x0 + 0.15, y0, 0, -1), (x1 - 0.2, y0, 0, -1), (x0, (y0 + y1) / 2, -1, 0),
                                        (x1, (y0 + y1) / 2 + 0.2, 1, 0))):
        ivy(b, x + nx * 0.005, y + ny * 0.005, h, 0.45, nx, ny, seed=i)


def build_main_storehouse(lo, hi):
    b = Builder()
    stone_house(b, -0.85, 0.85, 0.05, 0.88, 0.62, 'x', 1.18, door_w=0.36)
    o.skull(b, 0, 0.0, 0.78, s=0.12)
    # the loot heaped out front, where the camera sees it
    STORE['gold_heap'](b, -0.35, -0.45, 0.24)
    STORE['gold_heap'](b, 0.15, -0.62, 0.15)
    STORE['chest'](b, 0.5, -0.4, iron=True)
    STORE['chest'](b, -0.65, -0.62)
    STORE['crate'](b, 0.62, -0.05, 0.0, 0.09, iron=True)
    STORE['barrel'](b, -0.72, -0.1)
    for x, y in ((-0.1, -0.15), (0.2, -0.2)):
        STORE['sack'](b, x, y, s=0.07)
    for i in range(6):
        a = math.radians(200 + 28 * i)
        ferns(b, 0.92 * math.cos(a), 0.1 + 0.88 * math.sin(a), seed=i)
    return b


def build_main_barracks(lo, hi):
    b = Builder()
    stone_house(b, -0.6, 0.6, -0.55, 0.9, 0.7, 'y', 1.45)
    o.skull(b, 0, -0.6, 0.95, s=0.18)
    for k in (-1, 1):
        o.horn(b, (k * 0.09, -0.58, 1.05), (k, -0.2, 0), 0.22, 0.04, up=1.4)
    # a fire pit and a weapon rack in the yard, a banner
    fx, fy = -0.45, -0.85
    for i in range(6):
        a = 2 * math.pi * i / 6
        o.rubble(b, fx + 0.11 * math.cos(a), fy + 0.11 * math.sin(a), r=0.04, seed=i, color=CAP)
    b.blob(fx, fy, 0.0, 0.07, 0.16, o.FIRE, sides=5)
    rx, ry = 0.45, -0.82
    for k in (-1, 1):
        b.box(rx + k * 0.16 - 0.03, rx + k * 0.16 + 0.03, ry - 0.03, ry + 0.03, 0.0, 0.34, STONE)
    b.rod((rx - 0.2, ry, 0.3), (rx + 0.2, ry, 0.3), 0.02, o.TIMBER_LIGHT)
    for dx in (-0.08, 0.0, 0.08):
        b.rod((rx + dx - 0.03, ry + 0.05, 0.0), (rx + dx, ry - 0.02, 0.44), 0.012, o.TIMBER_LIGHT)
        o.tube(b, [(rx + dx, ry - 0.02, 0.44), (rx + dx, ry - 0.03, 0.5)], [0.014, 0.0], o.METAL, sides=4)
    o.war_banner(b, 0.8, 0.85, 1.7, w=0.16, drop=0.32)
    for i in range(6):
        a = math.radians(200 + 28 * i)
        ferns(b, 0.9 * math.cos(a), 0.15 + 0.85 * math.sin(a), seed=i + 3)
    return b


def house_ruin(x0, x1, y0, y1, extra=None):
    def build(lo, hi):
        b = Builder()
        b.crag((x0 + x1) / 2, (y0 + y1) / 2, (x1 - x0) / 2 + 0.04, (y1 - y0) / 2 + 0.04, 0.12, o.ROCK,
               top='cobble', sides=8, taper=0.97, seed=3)
        o.scorch(b, (x0 + x1) / 2, (y0 + y1) / 2, 0.6, 0.5, seed=2)
        for i, (a0, a1, c0, c1, h) in enumerate(((x0, x0 + 0.5, y0, y0 + 0.16, 0.45), (x1 - 0.4, x1, y0, y0 + 0.16, 0.3),
                                                 (x0, x0 + 0.16, y0, y1, 0.38), (x1 - 0.16, x1, y0, y1, 0.5),
                                                 (x0, x1, y1 - 0.16, y1, 0.28))):
            b.box(a0, a1, c0, c1, 0.1, h, STONE, bevel=0.02)
        b.obox(((x0 + x1) / 2, (y0 + y1) / 2 + 0.05, 0.22), (1, 0, 0), (0, 0.95, 0.3), (0, -0.3, 0.95),
               (x1 - x0) / 2 - 0.1, (y1 - y0) / 2 - 0.15, 0.04, MOSS_DARK, skip=())
        for i, (x, y) in enumerate(((x0 + 0.3, y0 - 0.25), (x1 - 0.2, y0 - 0.35))):
            b.crag(x, y, 0.13, 0.11, 0.12, STONE, sides=5, seed=i)
        if extra:
            extra(b)
        return b
    return build


# -- the brute pit (2x2) -----------------------------------------------------------------------

def build_main_brute_pit(lo, hi):
    b = Builder()
    b.crag(0, 0.02, 0.62, 0.6, 0.02, o.HAIR, sides=10, taper=0.98, seed=2)
    n, gap = 16, 2
    for i in range(n):
        a0 = 2 * math.pi * i / n - math.pi / 2
        a1 = 2 * math.pi * (i + 1) / n - math.pi / 2
        if i < gap // 2 or i >= n - gap // 2:
            continue
        pts_out = [(0.86 * math.cos(a0), 0.02 + 0.86 * math.sin(a0)), (0.86 * math.cos(a1), 0.02 + 0.86 * math.sin(a1))]
        pts_in = [(0.66 * math.cos(a1), 0.02 + 0.66 * math.sin(a1)), (0.66 * math.cos(a0), 0.02 + 0.66 * math.sin(a0))]
        b.prism(pts_out + pts_in, (0, 0, 0), (1, 0, 0), (0, 1, 0), 0.42, STONE if i % 2 else 'stone_light')
        am = (a0 + a1) / 2
        b.zbox(0.76 * math.cos(am), 0.02 + 0.76 * math.sin(am), 0.42, 0.46, 0.1, 0.09, math.degrees(am),
               MOSS if i % 3 else MOSS_DARK)
        if i % 4 == 1:
            ivy(b, 0.87 * math.cos(am), 0.02 + 0.87 * math.sin(am), 0.42, 0.3, math.cos(am), math.sin(am), seed=i)
    # the iron cage gate between two stone posts in the gap (front, -y)
    for k in (-1, 1):
        x = k * 0.36
        b.box(x - 0.08, x + 0.08, -0.86, -0.7, 0.0, 0.8, STONE, bevel=0.01)
        b.box(x - 0.09, x + 0.09, -0.87, -0.69, 0.8, 0.85, CAP)
        b.zbox(x, -0.78, 0.85, 0.88, 0.07, 0.07, 0, MOSS)
    b.box(-0.3, 0.3, -0.8, -0.76, 0.6, 0.66, o.METAL)
    for i in range(7):
        b.cylinder(-0.27 + 0.09 * i, -0.78, 0.0, 0.6, 0.012, o.METAL, sides=4)
    o.skull(b, 0, -0.85, 0.66, s=0.12)
    # the gong between two stone posts at the back
    gy = 0.78
    for k in (-1, 1):
        b.box(k * 0.32 - 0.07, k * 0.32 + 0.07, gy - 0.07, gy + 0.07, 0.0, 1.05, STONE)
    b.box(-0.42, 0.42, gy - 0.06, gy + 0.06, 1.05, 1.13, CAP)
    b.box(-0.4, 0.4, gy - 0.05, gy + 0.05, 1.13, 1.16, MOSS_DARK)
    pts = [(0.22 * math.cos(2 * math.pi * i / 10), 0.66 + 0.22 * math.sin(2 * math.pi * i / 10)) for i in range(10)]
    b.prism(pts, (0, gy - 0.03, 0), (1, 0, 0), (0, 0, 1), 0.03, 'gold', back_cap=True)
    for k in (-1, 1):
        o.horn(b, (k * 0.42, gy, 1.12), (k, 0, 0), 0.14, 0.035, up=1.4)
    b.cylinder(0, 0.1, 0.0, 0.55, 0.06, STONE, sides=6)
    b.cylinder(0, 0.1, 0.48, 0.55, 0.07, o.METAL, sides=6)
    o.skull(b, -0.55, -0.35, 0.02, s=0.08, facing=(0.4, -1))
    return b


def build_main_brute_pit_ruin(lo, hi):
    b = Builder()
    b.crag(0, 0.02, 0.62, 0.6, 0.02, o.HAIR, sides=10, taper=0.98, seed=2)
    for i in range(1, 15):
        if i % 3 == 0:
            continue
        a = 2 * math.pi * (i + 0.5) / 16 - math.pi / 2
        b.crag(0.76 * math.cos(a), 0.02 + 0.76 * math.sin(a), 0.12, 0.1, 0.12 + 0.12 * (i % 2), STONE, sides=5, seed=i)
    b.box(-0.44, -0.28, -0.86, -0.7, 0.0, 0.35, STONE)
    b.box(-0.3, 0.32, -0.66, -0.3, 0.02, 0.05, o.METAL)
    b.rod((0.32, 0.78, 0.03), (-0.2, 0.62, 0.25), 0.07, STONE)
    o.scorch(b, 0.1, 0.2, 0.5, 0.4, seed=6)
    return b


# -- the throwers' range (2x2) -----------------------------------------------------------------

def build_main_thrower_range(lo, hi):
    b = Builder()
    b.crag(0.0, 0.05, 0.78, 0.76, 0.03, o.STONE, top='cobble', sides=8, taper=0.97, seed=7)
    # a stone back wall with a turf shelter roof leaning off it on stone posts
    b.box(-0.82, 0.82, 0.7, 0.86, 0.0, 0.95, STONE, bevel=0.015)
    b.box(-0.84, 0.84, 0.68, 0.88, 0.95, 1.0, CAP)
    for x in (-0.78, 0.0, 0.78):
        b.box(x - 0.06, x + 0.06, 0.32, 0.44, 0.0, 0.66, STONE)
    d = (0, 0.5, 0.3)
    ln = math.hypot(0.5, 0.3)
    dy, dz = 0.5 / ln, 0.3 / ln
    b.obox((0, 0.6, 0.86), (1, 0, 0), (0, dy, dz), (0, -dz, dy), 0.9, ln / 2 + 0.12, 0.05, MOSS_DARK, skip=())
    b.obox((0, 0.6, 0.915), (1, 0, 0), (0, dy, dz), (0, -dz, dy), 0.86, ln / 2 + 0.08, 0.006, MOSS, skip=())
    for i in range(4):
        b.blob(-0.6 + 0.4 * i, 0.38, 0.62, 0.06, 0.12, IVY, sides=4)
    for i in range(3):
        ivy(b, -0.5 + 0.5 * i, 0.69, 0.95, 0.5, 0, -1, seed=i)
    # racks of javelins under the shelter
    for rx in (-0.4, 0.4):
        b.rod((rx - 0.2, 0.62, 0.3), (rx + 0.2, 0.62, 0.3), 0.02, o.TIMBER_LIGHT)
        for i in range(4):
            x = rx - 0.12 + 0.08 * i
            b.rod((x - 0.02, 0.68, 0.0), (x, 0.6, 0.6), 0.012, o.TIMBER_LIGHT)
            o.tube(b, [(x, 0.6, 0.6), (x, 0.59, 0.67)], [0.014, 0.0], o.METAL, sides=4)
    # targets on stone posts, a skull target, a pile of stones at the throwing line
    for x, y in ((-0.42, -0.05), (0.38, 0.0)):
        b.box(x - 0.05, x + 0.05, y + 0.03, y + 0.11, 0.0, 0.3, STONE)
        RANGE['target'](b, x, y, 0.45)
    b.box(-0.04, 0.04, 0.16, 0.24, 0.0, 0.55, STONE)
    o.skull(b, 0.0, 0.15, 0.55, s=0.09)
    for i, (x, y) in enumerate(((-0.62, -0.62), (-0.52, -0.66), (-0.58, -0.54), (-0.47, -0.58))):
        o.rubble(b, x, y, r=0.055, seed=i, color=CAP if i % 2 else STONE)
    o.war_banner(b, 0.75, -0.7, 1.5, w=0.16, drop=0.3)
    for i in range(4):
        ferns(b, -0.75 + 0.5 * i, -0.72, seed=i)
    return b


def build_main_thrower_range_ruin(lo, hi):
    b = Builder()
    b.crag(0.0, 0.05, 0.78, 0.76, 0.03, o.STONE, top='cobble', sides=8, taper=0.97, seed=7)
    b.box(-0.82, -0.1, 0.7, 0.86, 0.0, 0.6, STONE, bevel=0.015)
    b.box(0.2, 0.82, 0.7, 0.86, 0.0, 0.35, STONE, bevel=0.015)
    b.obox((0, 0.45, 0.15), (1, 0, 0), (0, 0.9, 0.44), (0, -0.44, 0.9), 0.85, 0.25, 0.04, MOSS_DARK, skip=())
    for x, y in ((-0.4, -0.05), (0.38, 0.0)):
        b.box(x - 0.05, x + 0.05, y + 0.03, y + 0.11, 0.0, 0.15, STONE)
        b.zbox(x + 0.1, y - 0.2, 0.0, 0.05, 0.17, 0.12, 25, 'hay')
    o.scorch(b, 0.0, 0.3, 0.6, 0.4, seed=8)
    return b


def main():
    o.ensure_collection()
    y = -112.0
    o.place("OrcMainPortal1", build_main_portal, "Laboratory1-lvl1", o.GRID_X[1], y)
    o.place("OrcMainPortal1-Ruin", build_main_portal_ruin, "Laboratory1-lvl1", o.GRID_X['ruin'] + 2, y)
    rows = (
        ("OrcMainTotem1", build_main_totem, build_main_totem_ruin, "DecTree1-lvl1"),
        ("OrcMainStorehouse1", build_main_storehouse, house_ruin(-0.85, 0.85, 0.05, 0.88), "BigBarn1-lvl1"),
        ("OrcMainBarracks1", build_main_barracks, house_ruin(-0.6, 0.6, -0.55, 0.9), "BigBarn1-lvl1"),
        ("OrcMainBrutePit1", build_main_brute_pit, build_main_brute_pit_ruin, "BigBarn1-lvl1"),
        ("OrcMainThrowerRange1", build_main_thrower_range, build_main_thrower_range_ruin, "BigBarn1-lvl1"),
    )
    for i, (name, build, ruin, source) in enumerate(rows):
        yy = y - 5.0 - 3.0 * i
        o.place(name, build, source, o.GRID_X[1], yy)
        o.place(name + "-Ruin", ruin, source, o.GRID_X['ruin'], yy)
    bpy.ops.wm.save_mainfile()


main()
