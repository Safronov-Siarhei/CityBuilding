"""The orcs' STONE set, for the great portal's main base (the user's sketch, 2026-10-19): walls
of rough stone, a stone tower and a stone gate under a tower -- overgrown, the way the user asked
for it: moss on every top, ivy down the faces, grass and ferns at the foot.

    OrcStoneWall1-{Straight,Corner,T,Cross}   one cell each, the FenceShape convention (N = +Y,
                                              E = +X): Straight N-S, Corner S + E, T N-S + E
    OrcStoneTower1                            2x2, a squat stone keep with a timber fighting top
    OrcStoneGate1                             1x2 like the palisade gate: the wall runs along Y
                                              through it, the way in along X, a tower over it
    OrcStoneGate1-Ruin, OrcStoneTower1-Ruin   what is left of them

Stone is the dressed-course colour (stone_mid draws coursed blocks from the detail atlas) on a
plinth of raw rock, capped in plain dark stone; the orcs' marks are the same as everywhere --
rust iron, bone, skulls, horns.
"""
import math
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402

WALL_H = 1.2
WALL_T = 0.22          # half thickness
PLINTH_T = 0.27
STONE = 'stone_mid'    # dressed courses from the atlas
CAP = o.STONE          # plain dark stone
MOSS, MOSS_DARK, IVY = 'grass', 'grass_dark', 'leaf'

DIRS = {'N': (0, 1), 'E': (1, 0), 'S': (0, -1), 'W': (-1, 0)}
SHAPES = {'Straight': 'NS', 'Corner': 'SE', 'T': 'NSE', 'Cross': 'NESW'}


def _rand(seed, k):
    """A cheap deterministic jitter in [0, 1)."""
    return ((seed * 7919 + k * 104729) % 1000) / 1000.0


def moss_cap(b, x0, x1, y0, y1, z, seed=0):
    """Moss lying in uneven cushions along a top."""
    n = max(1, int(max(abs(x1 - x0), abs(y1 - y0)) / 0.22))
    for i in range(n):
        t = (i + 0.5) / n
        x, y = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
        r = 0.07 + 0.04 * _rand(seed, i)
        b.zbox(x, y, z, z + 0.04 + 0.03 * _rand(seed, i + 9), r, r * 0.8, 30 * i + 15,
               MOSS if i % 2 else MOSS_DARK)


def ivy(b, x, y, z_top, length, nx, ny, seed=0):
    """A strand of ivy hanging down a face from z_top, leaves alternating side to side."""
    ang = math.degrees(math.atan2(ny, nx)) + 90
    b.zbox(x + nx * 0.008, y + ny * 0.008, z_top - length, z_top, 0.016, 0.006, ang, MOSS_DARK)
    k = 0
    z = z_top - 0.05
    while z > z_top - length:
        side = 1 if k % 2 else -1
        tx, ty = -ny * side * 0.045, nx * side * 0.045
        b.zbox(x + tx + nx * 0.014, y + ty + ny * 0.014, z - 0.05, z + 0.03, 0.045, 0.006, ang,
               IVY if (k + seed) % 3 else MOSS)
        z -= 0.1
        k += 1


def foot_greenery(b, x, y, seed=0):
    """A fern and a tuft or two at the foot of the wall."""
    b.blob(x, y, 0.0, 0.06 + 0.02 * _rand(seed, 1), 0.13, IVY, sides=4)


def wall_arm(b, dx, dy, t0, t1, seed=0):
    """A run of stone wall from t0 to t1 out of the cell centre along (dx, dy): raw-rock plinth,
    dressed courses, a dark cap with merlons, moss on top, ivy down both faces."""
    nx, ny = -dy, dx

    def span(a, c, pad):
        """Axis-aligned box from a to c along the arm, `pad` either side across it."""
        xa, ya, xc, yc = dx * a, dy * a, dx * c, dy * c
        return (min(xa, xc) - abs(nx) * pad, max(xa, xc) + abs(nx) * pad,
                min(ya, yc) - abs(ny) * pad, max(ya, yc) + abs(ny) * pad)

    x0, x1, y0, y1 = span(t0, t1, PLINTH_T)
    b.box(x0, x1, y0, y1, 0.0, 0.18, o.ROCK, bevel=0.02)
    x0, x1, y0, y1 = span(t0, t1, WALL_T)
    b.box(x0, x1, y0, y1, 0.18, WALL_H, STONE, bevel=0.015)
    x0, x1, y0, y1 = span(t0, t1, WALL_T + 0.03)
    b.box(x0, x1, y0, y1, WALL_H, WALL_H + 0.06, CAP, bevel=0.01)
    # merlons on the top, three a cell
    t = 0.083
    while t < t1 - 0.04:
        if t > t0 + 0.04:
            mx, my = dx * t, dy * t
            hx = 0.06 if dx else WALL_T
            hy = 0.06 if dy else WALL_T
            b.box(mx - hx, mx + hx, my - hy, my + hy, WALL_H + 0.06, WALL_H + 0.2, CAP, bevel=0.01)
        t += 0.167
    # a blanket of moss over the whole top, and caps of it on every other merlon
    x0, x1, y0, y1 = span(t0 + 0.02, t1 - 0.02, WALL_T + 0.01)
    b.box(x0, x1, y0, y1, WALL_H + 0.06, WALL_H + 0.09, MOSS_DARK)
    t = 0.083
    k = 0
    while t < t1 - 0.04:
        if t > t0 + 0.04 and k % 2 == 0:
            mx, my = dx * t, dy * t
            hx = 0.065 if dx else WALL_T + 0.01
            hy = 0.065 if dy else WALL_T + 0.01
            b.box(mx - hx, mx + hx, my - hy, my + hy, WALL_H + 0.2, WALL_H + 0.24, MOSS)
        t += 0.167
        k += 1
    # moss creeping down the faces from the top and up them from the ground
    for side in (-1, 1):
        ang = math.degrees(math.atan2(ny * side, nx * side)) + 90
        for k, (tt, z0, z1, w) in enumerate(((0.12, WALL_H - 0.28, WALL_H + 0.06, 0.12),
                                             (0.36, WALL_H - 0.16, WALL_H + 0.06, 0.1),
                                             (0.25, 0.18, 0.42, 0.14))):
            if not (t0 + 0.05 < tt < t1 - 0.05):
                continue
            jt = tt + 0.06 * (_rand(seed + side, k) - 0.5)
            px = dx * jt + nx * side * (WALL_T + 0.006)
            py = dy * jt + ny * side * (WALL_T + 0.006)
            b.zbox(px, py, z0 + 0.05 * _rand(seed, k + 3), z1, w, 0.006, ang, MOSS if k % 2 else MOSS_DARK)
    # moss along the top, ivy down both faces, greenery at the foot
    moss_cap(b, dx * (t0 + 0.06), dx * (t1 - 0.06), dy * (t0 + 0.06), dy * (t1 - 0.06), WALL_H + 0.06,
             seed=seed)
    for side in (-1, 1):
        for k, tt in enumerate((0.18 if side < 0 else 0.4,)):
            if not (t0 < tt < t1):
                continue
            px, py = dx * tt + nx * side * (WALL_T + 0.005), dy * tt + ny * side * (WALL_T + 0.005)
            length = 0.35 + 0.45 * _rand(seed + side, k)
            ivy(b, px, py, WALL_H + 0.04, length, nx * side, ny * side, seed=seed + k)
        tt = 0.3
        if t0 < tt < t1:
            foot_greenery(b, dx * tt + nx * side * (PLINTH_T + 0.03), dy * tt + ny * side * (PLINTH_T + 0.03),
                          seed=seed + side)


def stone_post(b, h=WALL_H + 0.32, half=0.3):
    """The square pillar at a joint: banded in rust iron, a skull on the face, moss on top."""
    b.box(-half - 0.04, half + 0.04, -half - 0.04, half + 0.04, 0.0, 0.2, o.ROCK, bevel=0.02)
    b.box(-half, half, -half, half, 0.2, h, STONE, bevel=0.02)
    b.box(-half - 0.02, half + 0.02, -half - 0.02, half + 0.02, h * 0.55, h * 0.55 + 0.06, o.RUST)
    b.box(-half - 0.04, half + 0.04, -half - 0.04, half + 0.04, h, h + 0.07, CAP, bevel=0.01)
    b.zbox(0, 0, h + 0.07, h + 0.13, half * 0.8, half * 0.7, 20, MOSS)


def wall_piece(shape):
    def build(lo, hi):
        b = Builder()
        sides = SHAPES[shape]
        joint = shape != 'Straight'
        for i, k in enumerate(sides):
            dx, dy = DIRS[k]
            wall_arm(b, dx, dy, 0.3 if joint else 0.0, 0.5, seed=i * 5 + len(sides))
        if joint:
            stone_post(b)
            o.skull(b, 0, -0.31, WALL_H * 0.7, s=0.11)
            if shape in ('Cross', 'T'):
                for k in (-1, 1):
                    o.horn(b, (0, 0, WALL_H + 0.42), (k, 0, 0), 0.12, 0.03, up=1.3)
        return b
    return build


def build_straight_full(lo, hi):
    """A straight piece is one arm through the cell (both halves in one box, no seam)."""
    b = Builder()
    wall_arm(b, 0, 1, -0.5, 0.5, seed=3)
    return b


# -- the stone tower (2x2) ---------------------------------------------------------------------

TOWER_R = 0.82
TOWER_H = 2.0


def octagon(r, rot=22.5):
    return [(r * math.cos(math.radians(rot + 45 * i)), r * math.sin(math.radians(rot + 45 * i)))
            for i in range(8)]


def build_tower(lo, hi):
    """A squat octagonal keep of dressed stone on a rock footing, a timber fighting top with a
    stake parapet and an iron roof, moss and ivy over all of it."""
    b = Builder()
    b.crag(0, 0, 0.86, 0.84, 0.22, o.ROCK, top='cobble', sides=8, taper=0.94, seed=4)
    b.frustum(0, 0, 0.18, TOWER_H, TOWER_R, TOWER_R * 0.86, STONE, sides=8)
    for z in (0.7, 1.45):
        b.frustum(0, 0, z, z + 0.07, TOWER_R * (1.0 - 0.14 * z / TOWER_H) + 0.02,
                  TOWER_R * (1.0 - 0.14 * (z + 0.07) / TOWER_H) + 0.02, o.RUST, sides=8)
    # a door at the back, slit windows, a skull over the front
    b.box(-0.14, 0.14, 0.72, 0.82, 0.18, 0.62, o.HAIR)
    for a in (-90, 0, 180):
        r = TOWER_R * 0.9
        x, y = r * math.cos(math.radians(a)), r * math.sin(math.radians(a))
        b.zbox(x, y, 1.1, 1.36, 0.035, 0.06, a, o.HAIR)
    o.skull(b, 0, -TOWER_R * 0.88, 1.6, s=0.14)
    # the fighting top: a stone parapet ring with merlons, then a timber hut with an iron roof
    top = TOWER_H
    b.frustum(0, 0, top, top + 0.08, TOWER_R * 0.86 + 0.08, TOWER_R * 0.86 + 0.08, CAP, sides=8)
    for i, (x, y) in enumerate(octagon(TOWER_R * 0.86)):
        b.zbox(x, y, top + 0.08, top + 0.28, 0.09, 0.06, math.degrees(math.atan2(y, x)) + 90, CAP)
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.rod((sx * 0.42, sy * 0.42, top + 0.08), (sx * 0.4, sy * 0.4, top + 0.82), 0.05, o.TIMBER)
    b.pyramid(0, 0, top + 0.76, 0.62, 0.62, top + 1.3, o.METAL)
    for nx, ny in ((0, -1), (1, 0), (0, 1), (-1, 0)):
        for off in (-0.2, 0.2):
            b.rod((nx * 0.6 + (-ny) * off, ny * 0.6 + nx * off, top + 0.78),
                  (nx * 0.12 + (-ny) * off * 0.2, ny * 0.12 + nx * off * 0.2, top + 1.2), 0.04, o.RUST)
    for k in (-1, 1):
        o.horn(b, (k * 0.05, 0, top + 1.25), (k, 0, 0), 0.26, 0.05, up=1.6, segments=4)
    b.cylinder(0.3, 0.2, top + 0.08, top + 0.18, 0.07, o.METAL, sides=6)
    b.blob(0.3, 0.2, top + 0.15, 0.06, 0.15, o.FIRE, sides=4)
    o.war_banner(b, -0.62, 0.62, top + 1.5, w=0.16, drop=0.3)
    # overgrown: moss on the parapet, ivy down the faces, ferns round the foot
    for i, (x, y) in enumerate(octagon(TOWER_R * 0.86)):
        if i % 2 == 0:
            b.zbox(x, y, top + 0.08, top + 0.13, 0.11, 0.08, 45 * i, MOSS if i % 4 else MOSS_DARK)
    for i, a in enumerate((-60, 20, 120, 200, 290)):
        r = TOWER_R * (0.98 - 0.14 * 1.9 / TOWER_H)
        x, y = r * math.cos(math.radians(a)), r * math.sin(math.radians(a))
        ivy(b, x, y, top - 0.02, 0.6 + 0.6 * _rand(i, 2), math.cos(math.radians(a)), math.sin(math.radians(a)), seed=i)
    for i in range(7):
        a = math.radians(30 + 51 * i)
        foot_greenery(b, 0.86 * math.cos(a), 0.86 * math.sin(a), seed=i)
    return b


def build_tower_ruin(lo, hi):
    """The keep broken off at a third of its height, its top tumbled round it, the iron roof
    crumpled at the foot -- and the green already taking it back."""
    b = Builder()
    b.crag(0, 0, 0.86, 0.84, 0.22, o.ROCK, top='cobble', sides=8, taper=0.94, seed=4)
    b.frustum(0, 0, 0.18, 0.75, TOWER_R, TOWER_R * 0.95, STONE, sides=8, cap=False)
    for i, (x, y) in enumerate(octagon(TOWER_R * 0.78)):
        h = 0.75 + 0.18 * _rand(i, 4)
        b.zbox(x, y, 0.7, h, 0.16, 0.08, math.degrees(math.atan2(y, x)) + 90, STONE)
    b.cylinder(0, 0, 0.5, 0.56, TOWER_R * 0.7, o.HAIR, sides=8)
    for i, (x, y) in enumerate(((0.75, 0.3), (-0.55, 0.62), (0.4, -0.72), (-0.72, -0.4))):
        b.crag(x, y, 0.16, 0.13, 0.15, STONE, sides=5, seed=i)
    b.pyramid(-0.5, -0.55, 0.02, 0.32, 0.3, 0.3, o.METAL)
    o.scorch(b, 0.1, 0.1, 0.6, 0.5, seed=3)
    for i in range(5):
        a = math.radians(40 + 70 * i)
        b.zbox(0.7 * math.cos(a), 0.7 * math.sin(a), 0.7, 0.76, 0.11, 0.08, 40 * i, MOSS)
    o.skull(b, 0.6, -0.6, 0.02, s=0.1, facing=(0.3, -1))
    return b


# -- the stone gate under a tower (1x2) --------------------------------------------------------

GATE_H = 1.15          # the arch's crown
BRIDGE = 1.55          # the fighting floor over it


def build_gate(lo, hi):
    """Two stone piers on the wall's line (Y), an arch between them with iron-bound leaves, a
    stone bridge over it and a small timber tower with an iron roof on top."""
    b = Builder()
    wall_arm(b, 0, 1, 0.72, 1.0, seed=1)          # the wall comes in from both ends
    wall_arm(b, 0, -1, 0.72, 1.0, seed=4)
    for py in (-0.58, 0.58):
        sy = 1 if py > 0 else -1
        b.box(-0.4, 0.4, py - 0.16, py + 0.16, 0.0, 0.2, o.ROCK, bevel=0.02)
        b.box(-0.34, 0.34, py - 0.13, py + 0.13, 0.2, BRIDGE, STONE, bevel=0.02)
        b.box(-0.36, 0.36, py - 0.15, py + 0.15, 0.95, 1.0, o.RUST)
        foot_greenery(b, -0.38, py, seed=2 + sy)
        foot_greenery(b, 0.38, py + 0.1 * sy, seed=5 + sy)
    # the arch: a dark opening filled by two iron-bound leaves of planks
    b.box(-0.3, 0.3, -0.45, 0.45, GATE_H, BRIDGE, STONE, bevel=0.015)
    for i in range(6):
        y = -0.38 + 0.152 * i
        b.box(-0.05, 0.05, y - 0.07, y + 0.07, 0.02, GATE_H - 0.02, o.TIMBER_LIGHT if i % 2 else o.TIMBER, skip=())
    for z in (0.25, 0.75):
        b.box(-0.07, 0.07, -0.45, 0.45, z, z + 0.06, o.METAL, skip=())
    for side in (-1, 1):
        for y, z in ((-0.25, 0.5), (0.25, 0.5), (0.0, 0.95)):
            o.tube(b, [(side * 0.06, y, z), (side * 0.13, y, z)], [0.018, 0.0], o.METAL, sides=4)
    # the bridge floor, parapet merlons on both faces, moss
    b.box(-0.4, 0.4, -0.75, 0.75, BRIDGE, BRIDGE + 0.08, CAP, bevel=0.01)
    for side in (-1, 1):
        for i in range(5):
            y = -0.6 + 0.3 * i
            b.box(side * 0.36 - 0.04, side * 0.36 + 0.04, y - 0.07, y + 0.07, BRIDGE + 0.08, BRIDGE + 0.26, CAP)
    moss_cap(b, -0.38, -0.38, -0.7, 0.7, BRIDGE + 0.08, seed=7)
    moss_cap(b, 0.38, 0.38, -0.7, 0.7, BRIDGE + 0.08, seed=11)
    # the tower on the bridge: timber posts, iron roof, horns, a banner, a skull on the face
    top = BRIDGE + 0.08
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.rod((sx * 0.3, sy * 0.4, top), (sx * 0.28, sy * 0.38, top + 0.65), 0.045, o.TIMBER)
    b.pyramid(0, 0, top + 0.6, 0.42, 0.55, top + 1.05, o.METAL)
    for sy in (-1, 1):
        b.rod((0.0, sy * 0.55, top + 0.62), (0.0, sy * 0.1, top + 0.98), 0.04, o.RUST)
    for k in (-1, 1):
        o.horn(b, (0, k * 0.05, top + 1.0), (0, k, 0), 0.2, 0.04, up=1.5, segments=4)
    o.skull(b, -0.33, 0, GATE_H + 0.15, s=0.13, facing=(-1, 0))
    o.war_banner(b, 0.32, -0.6, top + 1.2, w=0.14, drop=0.26, facing='-x')
    for side in (-1, 1):
        ivy(b, side * 0.345, -0.3, BRIDGE, 0.5, side, 0, seed=3)
        ivy(b, side * 0.345, 0.32, BRIDGE, 0.7, side, 0, seed=6)
    return b


def build_gate_ruin(lo, hi):
    """Broken in: one pier standing to half its height, the other a stump, the arch and the
    tower down across the way, the leaves flat, moss over the rubble."""
    b = Builder()
    wall_arm(b, 0, 1, 0.72, 1.0, seed=1)
    wall_arm(b, 0, -1, 0.72, 1.0, seed=4)
    for py, h in ((-0.58, 0.8), (0.58, 0.35)):
        b.box(-0.4, 0.4, py - 0.16, py + 0.16, 0.0, 0.2, o.ROCK, bevel=0.02)
        b.box(-0.34, 0.34, py - 0.13, py + 0.13, 0.2, h, STONE, bevel=0.02)
    for i, (x, y) in enumerate(((-0.25, -0.1), (0.2, 0.15), (0.35, -0.3), (-0.3, 0.3), (0.0, 0.0))):
        b.crag(x, y, 0.14, 0.12, 0.14, STONE, sides=5, seed=i)
    for i in range(5):
        y = -0.32 + 0.16 * i
        o.fallen_log(b, (0.42, y, 0.04), (-0.42, y + 0.03, 0.04), r=0.045,
                     color=o.TIMBER_LIGHT if i % 2 else o.TIMBER)
    b.pyramid(0.25, 0.4, 0.02, 0.28, 0.3, 0.28, o.METAL)
    o.scorch(b, 0, 0, 0.36, 0.6, seed=2)
    for x, y in ((-0.2, 0.5), (0.3, -0.5), (0.0, 0.2)):
        b.zbox(x, y, 0.1, 0.16, 0.1, 0.08, 25, MOSS)
    o.skull(b, 0.3, -0.2, 0.02, s=0.1, facing=(0.4, -1))
    return b


def main():
    o.ensure_collection()
    for i, shape in enumerate(SHAPES):
        build = build_straight_full if shape == 'Straight' else wall_piece(shape)
        o.place(f"OrcStoneWall1-{shape}", build, "Fence1-lvl1", o.GRID_X[1] + 1.4 * i, -90.0)
    o.place("OrcStoneTower1", build_tower, "FortifiedTower1-lvl1", o.GRID_X[1], -94.0)
    o.place("OrcStoneTower1-Ruin", build_tower_ruin, "FortifiedTower1-lvl1", o.GRID_X['ruin'], -94.0)
    o.place("OrcStoneGate1", build_gate, "Gate1-lvl1", o.GRID_X[2] + 2.0, -94.0)
    o.place("OrcStoneGate1-Ruin", build_gate_ruin, "Gate1-lvl1", o.GRID_X['ruin'] + 3.0, -94.0)
    bpy.ops.wm.save_mainfile()


main()
