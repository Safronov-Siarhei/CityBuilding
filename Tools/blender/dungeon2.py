"""Dungeon 2, «Затопленные гномьи чертоги» (the user, 2026-11-03): the drowned dwarven halls,
300 x 300 m (1.5x level 1), built like Dungeon 1 (dungeon1.py: the passages a signed-distance
field, the floor cut into colour zones, the rock rising as relief, the map's decor kit, props from
the building palette) on the APPROVED level-2 plan (dungeon2_plan.py, seed 92, dungeon2-plan.json):

- WATER everywhere (the user: «больше воды, пусть по некоторым проходам игрок ходит по воде»):
  one water surface at WATER_Z; under it the floor is either SHALLOW (still ground, above CLIP_Z:
  the heroes wade, no slow-down -- the user, 2026-11-03) or DEEP (no ground: the game reads a water
  cell, nobody walks). The canals are deep between the halls; in a hall, at a ford and along the
  flooded passages and quarters it is shallow; the serpent's pool is deep in its hall's middle;
- a dry passage crosses a canal on a stone BRIDGE whose deck is part of the ground (walkable);
- halls are LIVING SHAPES (the plan's `hall_blobs`), dressed stone underfoot, dwarven columns;
- the lever door before the cursed king's throne, a lever in each lever hall, the forges.

    blender --background --factory-startup --python dungeon2.py -- <out.blend> [preview]

Light points are Empties DungeonLight_<kind>_<n>, as in Dungeon 1 (dungeon_preview.py renders it).
"""
import math
import os
import random
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import detail  # noqa: E402
import kit  # noqa: E402
import terrain  # noqa: E402
from kit import Builder  # noqa: E402
from terrain import TerrainBuilder  # noqa: E402

argv = sys.argv[sys.argv.index("--") + 1:]
OUT = argv[0]
PREVIEW = "preview" in argv[1:]
PREFIX = "Dungeon-2"
# no monsters, crystals or patrols yet (the user, 2026-11-03: «пока что не добавляй монстров и
# ресурсы, я хочу просто пройтись и осмотреть его в игре») -- True writes them into the points
POPULATED = True
# Built in "metres" for a 1.8 m person, then shrunk to the GAME's units as Dungeon 1: the 300 x 300
# build comes out 132 x 132 cells.
SCALE = 0.44

N = 300
HALF = N // 2
WATER_Z = -0.12             # the one water surface: over the shallow floor and the deep canals
# the wading floor, still ground (> CLIP_Z): knee-deep on a hero (the user, 2026-11-04: «погружался
# в неё где-то по колено») -- 0.48 m of water is 0.21 cells, a 0.7-cell hero's knee
SHALLOW_Z = -0.60
CLIP_Z = -0.70              # below this there is no ground: deep water, a water cell in the game
DEEP_DROP = 1.6             # how far the deep beds fall under CLIP_Z
CANAL_W = 3.6               # a canal's half width
WALL_RISE = 1.8             # metres from the passage's edge over which the rock climbs
WALL_H = 3.0                # the rock's height above the floor (plus noise)


# -- noise (as map5) -------------------------------------------------------------------------

def _hash(ix, iy, seed):
    h = (ix * 374761393 + iy * 668265263 + seed * 2147483647) & 0xFFFFFFFF
    h = ((h ^ (h >> 13)) * 1274126177) & 0xFFFFFFFF
    return ((h ^ (h >> 16)) & 0xFFFF) / 65535.0


def _vnoise(x, y, seed):
    ix, iy = math.floor(x), math.floor(y)
    fx, fy = x - ix, y - iy
    sx, sy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
    a, b = _hash(ix, iy, seed), _hash(ix + 1, iy, seed)
    c, d = _hash(ix, iy + 1, seed), _hash(ix + 1, iy + 1, seed)
    return (a + (b - a) * sx) + ((c + (d - c) * sx) - (a + (b - a) * sx)) * sy


def fbm(x, y, seed, octaves=4):
    v, amp, tot = 0.0, 1.0, 0.0
    for o in range(octaves):
        v += _vnoise(x * (2 ** o), y * (2 ** o), seed + o * 17) * amp
        tot += amp
        amp *= 0.5
    return v / tot


def smoothstep(e0, e1, x):
    t = max(0.0, min(1.0, (x - e0) / (e1 - e0)))
    return t * t * (3 - 2 * t)


# -- the layout: the plan (grid metres 0..200, north = +y away from the camera) ---------------

import json  # noqa: E402

PLAN = json.load(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "dungeon2-plan.json"), encoding="utf-8"))
CELL = PLAN["cell"]
BIG = {k: tuple(v) for k, v in PLAN["big"].items()}
POS = {(c[0], c[1]): (c[2], c[3]) for c in PLAN["pos"]}
CRYSTAL_R, MONSTER_R = 4.6, 5.6
SMOOTH = 3.0                # how softly passages, niches and halls flow into each other


def cell_centre(c):
    return POS[tuple(c)]


def hall_rect(name, inset=1.5):
    x0, y0, w, h = BIG[name]
    return x0 * CELL + inset, y0 * CELL + inset, (x0 + w) * CELL - inset, (y0 + h) * CELL - inset


def hall_centre(name):
    x0, y0, x1, y1 = hall_rect(name)
    return (x0 + x1) / 2, (y0 + y1) / 2


def hall_of(c):
    for name, (x0, y0, w, h) in BIG.items():
        if x0 <= c[0] < x0 + w and y0 <= c[1] < y0 + h:
            return name
    return None


def curve(pa, pb, bend, steps=6):
    """A passage from pa to pb, bowed sideways by `bend` of its length (as dungeon_plan.curve)."""
    mx, my = (pa[0] + pb[0]) / 2, (pa[1] + pb[1]) / 2
    cx, cy = mx - (pb[1] - pa[1]) * bend, my + (pb[0] - pa[0]) * bend
    return [((1 - t) ** 2 * pa[0] + 2 * (1 - t) * t * cx + t * t * pb[0],
             (1 - t) ** 2 * pa[1] + 2 * (1 - t) * t * cy + t * t * pb[1]) for t in (k / steps for k in range(steps + 1))]


def smin(a, b, k=SMOOTH):
    h = max(k - abs(a - b), 0.0) / k
    return min(a, b) - h * h * k * 0.25


_lr = random.Random(23)
CURVES = []                 # (polyline, touches a hall)
for (ca, cb), bend in zip(PLAN["links"], PLAN["bends"]):
    CURVES.append((curve(cell_centre(ca), cell_centre(cb), bend), bool(hall_of(ca) or hall_of(cb))))
SEGMENTS = [(pl[k], pl[k + 1]) for pl, _ in CURVES for k in range(len(pl) - 1)]
BUCKET = {}                 # ~14 m cell -> sub-segments touching it, so a lookup checks a handful
for k, (pa, pb) in enumerate(SEGMENTS):
    for p in (pa, pb, ((pa[0] + pb[0]) / 2, (pa[1] + pb[1]) / 2)):
        BUCKET.setdefault((int(p[0] // CELL), int(p[1] // CELL)), set()).add(k)
NODES = sorted({tuple(c) for l in PLAN["links"] for c in l if not hall_of(c)})
BLOBS = [(cell_centre(c), _lr.uniform(2.3, 3.8)) for c in NODES]          # soft bulges at the turns
BLOBS += [(cell_centre(c), CRYSTAL_R) for c in PLAN["crystal_rooms"]]
BLOBS += [(cell_centre(c), MONSTER_R) for c in PLAN["monster_halls"]]
NICHE_KINDS = ("ruin", "grove", "mine", "rubble", "bones", "ruin", "grove", "rubble")
NICHES = []                 # (x, y, r, kind, (tx, ty) the passage's direction there)
for i, ((pl, near_hall), wet) in enumerate(zip(CURVES, PLAN["flooded"])):
    if near_hall or wet or _lr.random() > 0.5:
        continue
    k = len(pl) // 2
    (ax, ay), (bx, by) = pl[k - 1], pl[k + 1]
    ln = math.hypot(bx - ax, by - ay) or 1.0
    tx, ty = (bx - ax) / ln, (by - ay) / ln
    side = _lr.choice((-1, 1))
    r = _lr.uniform(2.4, 3.6)
    off = r + 1.2
    NICHES.append((pl[k][0] - ty * side * off, pl[k][1] + tx * side * off, r, NICHE_KINDS[i % len(NICHE_KINDS)], (tx, ty)))
BLOBS += [((x, y), r) for x, y, r, _k, _t in NICHES]
BLOB_BUCKET = {}
for k, ((x, y), r) in enumerate(BLOBS):
    BLOB_BUCKET.setdefault((int(x // CELL), int(y // CELL)), []).append(k)
HALL_BLOBS = {}             # a hall = the plan's blobs (the living shape the user approved) + one at every way in
for name in BIG:
    # smaller than drawn and wobbly: the plan's blobs at full size merged back into the rectangle
    lobes = [(x, y, r * 0.66 * _lr.uniform(0.85, 1.15), r * 0.66 * _lr.uniform(0.85, 1.15)) for x, y, r in PLAN["hall_blobs"][name]]
    for ca, cb in PLAN["links"]:
        for c in (ca, cb):
            if hall_of(c) == name:
                ex, ey = cell_centre(c)
                lobes.append((ex, ey, 5.0, 5.0))
    HALL_BLOBS[name] = lobes

# the water: the flooded passages (shallow), the canals (deep between the halls), the fords and
# bridges where a passage meets a canal, the serpent's deep pool
WET_SEGMENTS = [(pl[k], pl[k + 1]) for (pl, _h), wet in zip(CURVES, PLAN["flooded"]) if wet for k in range(len(pl) - 1)]
WET_BUCKET = {}
for k, (pa, pb) in enumerate(WET_SEGMENTS):
    for p in (pa, pb, ((pa[0] + pb[0]) / 2, (pa[1] + pb[1]) / 2)):
        WET_BUCKET.setdefault((int(p[0] // CELL), int(p[1] // CELL)), set()).add(k)
CANAL_SEGMENTS = [(c[k], c[k + 1]) for c in PLAN["canals"] for k in range(len(c) - 1)]
# the passages between halls (a hall's own links run across its open floor): deep water never
# lies across one -- where no bridge spans it, the canal is waded there (the user, 2026-11-04: a
# corner of the map could not be reached)
EXT_SEGMENTS = [(pl[k], pl[k + 1]) for (pl, _h), (ca, cb) in zip(CURVES, PLAN["links"])
                if not (hall_of(ca) and hall_of(ca) == hall_of(cb)) for k in range(len(pl) - 1)]
EXT_BUCKET = {}
for k, (pa, pb) in enumerate(EXT_SEGMENTS):
    for p in (pa, pb, ((pa[0] + pb[0]) / 2, (pa[1] + pb[1]) / 2)):
        EXT_BUCKET.setdefault((int(p[0] // CELL), int(p[1] // CELL)), set()).add(k)
PASS_CLEAR = 4.2            # no deep water this near a passage's centre line, but under a bridge
FORDS = []                  # (x, y): where a passage wades a canal -- set by plan_bridges()
BRIDGES = []                # (x, y, ux, uy, half): the span's middle, its axis (across the canal), half its length
BRIDGE_HW = 1.7
BRIDGE_RAMP = 0.7           # the deck slopes down to the floor over the outer 30% of each half
BRIDGE_DRY = 2.4            # a bridge's ends land this far from the canal: past its shallow bank (ankle-deep at most)
BRIDGE_APART = 18.0         # no two bridges nearer than this (two crossed each other, the user's screenshots)
WET_HALLS = ("great", "serpent")    # floors under shallow water from wall to wall
SERPENT_POOL = (*((lambda r: ((r[0] + r[2]) / 2, (r[1] + r[3]) / 2))((BIG["serpent"][0] * CELL, BIG["serpent"][1] * CELL,
                (BIG["serpent"][0] + BIG["serpent"][2]) * CELL, (BIG["serpent"][1] + BIG["serpent"][3]) * CELL))), 13.0, 10.0)
PILLARS = []


def _seg(px, py, ax, ay, bx, by):
    dx, dy = bx - ax, by - ay
    t = max(0.0, min(1.0, ((px - ax) * dx + (py - ay) * dy) / max(1e-9, dx * dx + dy * dy)))
    return math.hypot(px - ax - dx * t, py - ay - dy * t)


def route_dist(x, y):
    """Distance to the nearest passage's centre line, and the passage's half width there."""
    ix, iy = int(x // CELL), int(y // CELL)
    best = 1e9
    for dx in (-1, 0, 1):
        for dy in (-1, 0, 1):
            for k in BUCKET.get((ix + dx, iy + dy), ()):
                (ax, ay), (bx, by) = SEGMENTS[k]
                best = min(best, _seg(x, y, ax, ay, bx, by))
    return best, 1.5 + 1.7 * fbm(x / 14, y / 14, 47)


def _ellipse_sd(x, y, cx, cy, rx, ry, wobble, seed):
    ang = math.atan2((y - cy) / ry, (x - cx) / rx)
    k = 1.0 + wobble * (fbm(math.cos(ang) * 1.3 + seed, math.sin(ang) * 1.3 - seed, seed) - 0.5)
    r = math.hypot((x - cx) / rx, (y - cy) / ry)
    return (r - k) * min(rx, ry)


# the way out: a cave in a big rock standing against the camp's NORTH wall, so it faces the game's
# camera (which looks north) -- set by find_exit() at the start of main(); (x, y of the cave's face)
EXIT_AT = None


# the king's hall is shut off but for its lever door (the user, 2026-11-04: the hero must pull the
# levers first): the hall and its door passage are one compartment, and every other passage keeps
# THRONE_WALL of rock from it -- the plan's passages run along the hall's very edge, and the hall's
# living shape flowed into them, a second way in past the door
THRONE_WALL = 3.0
_DOOR = tuple(tuple(c) for c in PLAN["door"])
_DOOR_LINK = next(k for k, (ca, cb) in enumerate(PLAN["links"]) if {tuple(ca), tuple(cb)} == set(_DOOR))
_DOOR_PL = CURVES[_DOOR_LINK][0]
_THRONE_I = list(BIG).index("throne")
_TX0, _TY0, _TX1, _TY1 = hall_rect("throne")


def door_gate():
    """(x, y, ux, uy): the lever door's bars, across its passage just before the king's hall."""
    (da, db) = _DOOR
    pa, pb = cell_centre(da), cell_centre(db)
    if hall_of(da):
        pa, pb = pb, pa
    t = 0.0                                      # from outside towards the hall, to where the hall begins
    ux, uy = (pb[0] - pa[0]), (pb[1] - pa[1])
    ln = math.hypot(ux, uy)
    ux, uy = ux / ln, uy / ln
    hall = HALL_BLOBS["throne"]
    while t < ln and min(_ellipse_sd(pa[0] + ux * t, pa[1] + uy * t, cx, cy, rx, ry, 0.0, 0) for cx, cy, rx, ry in hall) > 2.0:
        t += 0.25
    return pa[0] + ux * max(0.0, t - 1.5), pa[1] + uy * max(0.0, t - 1.5), ux, uy


def throne_sd(x, y):
    """Signed distance to the king's compartment: his hall and the door passage into it."""
    best = 1e9
    for k in range(len(_DOOR_PL) - 1):
        (ax, ay), (bx, by) = _DOOR_PL[k], _DOOR_PL[k + 1]
        best = min(best, _seg(x, y, ax, ay, bx, by))
    best -= 1.5 + 1.7 * fbm(x / 14, y / 14, 47)
    for j, (cx, cy, rx, ry) in enumerate(HALL_BLOBS["throne"]):
        best = smin(best, _ellipse_sd(x, y, cx, cy, rx, ry, 0.6, 11 + _THRONE_I * 7 + j), 5.0)
    return best


def pass_sd(x, y):
    """Signed distance to the walkable ground: negative inside, positive in the rock."""
    o = _open_sd(x, y)
    if not (_TX0 - 25 < x < _TX1 + 25 and _TY0 - 25 < y < _TY1 + 25):
        return o
    # the rock wall keeps every other passage off the HALL; the door passage itself is left alone
    o = max(o, THRONE_WALL - hall_sd(x, y, names=("throne",)))
    return min(o, throne_sd(x, y))


def _open_sd(x, y):
    """pass_sd for everything but the king's hall."""
    d, hw = route_dist(x, y)
    best = d - hw * (0.85 + 0.3 * fbm(x / 5, y / 5, 41))
    ix, iy = int(x // CELL), int(y // CELL)
    for dx in (-1, 0, 1):
        for dy in (-1, 0, 1):
            for k in BLOB_BUCKET.get((ix + dx, iy + dy), ()):
                (cx, cy), r = BLOBS[k]
                if abs(x - cx) < r + 6 and abs(y - cy) < r + 6:
                    best = smin(best, math.hypot(x - cx, y - cy) - r * (0.8 + 0.4 * fbm(x / 3, y / 3, 43 + k % 7)))
    for i, name in enumerate(BIG):
        if name == "throne":
            continue
        x0, y0, x1, y1 = hall_rect(name)
        if x0 - 10 < x < x1 + 10 and y0 - 10 < y < y1 + 10:
            for j, (cx, cy, rx, ry) in enumerate(HALL_BLOBS[name]):
                best = smin(best, _ellipse_sd(x, y, cx, cy, rx, ry, 0.6, 11 + i * 7 + j), 5.0)
    return best


def hall_sd(x, y, names=None):
    """Signed distance to the halls' living shapes (their blobs, as pass_sd draws them)."""
    best = 1e9
    for i, name in enumerate(BIG):
        if names and name not in names:
            continue
        x0, y0, x1, y1 = hall_rect(name)
        if x0 - 10 < x < x1 + 10 and y0 - 10 < y < y1 + 10:
            for j, (cx, cy, rx, ry) in enumerate(HALL_BLOBS[name]):
                best = smin(best, _ellipse_sd(x, y, cx, cy, rx, ry, 0.6, 11 + i * 7 + j), 5.0)
    return best


def _rect_sd(x, y, x0, y0, x1, y1):
    dx, dy = max(x0 - x, 0.0, x - x1), max(y0 - y, 0.0, y - y1)
    if dx or dy:
        return math.hypot(dx, dy)
    return -min(x - x0, x1 - x, y - y0, y1 - y)


def canal_sd(x, y):
    best = 1e9
    for (ax, ay), (bx, by) in CANAL_SEGMENTS:
        if min(ax, bx) - 12 < x < max(ax, bx) + 12 and min(ay, by) - 12 < y < max(ay, by) + 12:
            best = min(best, _seg(x, y, ax, ay, bx, by))
    return best - CANAL_W * (0.85 + 0.3 * fbm(x / 6, y / 6, 71))


def ext_route_dist(x, y):
    ix, iy = int(x // CELL), int(y // CELL)
    best = 1e9
    for dx in (-1, 0, 1):
        for dy in (-1, 0, 1):
            for k in EXT_BUCKET.get((ix + dx, iy + dy), ()):
                (ax, ay), (bx, by) = EXT_SEGMENTS[k]
                best = min(best, _seg(x, y, ax, ay, bx, by))
    return best


def _span(x, y, b, grow=0.0, grow_across=0.0):
    """(along, across) of (x, y) from bridge b's middle, less its half length and half width."""
    bx, by, ux, uy, half = b
    along = abs((x - bx) * ux + (y - by) * uy) - half - grow
    across = abs(-(x - bx) * uy + (y - by) * ux) - BRIDGE_HW - grow_across
    return along, across


def deep_sd(x, y):
    """Negative where the water is DEEP (no ground: nobody walks): the canals between the halls --
    never in a hall, nor across a passage but under its bridge -- and the serpent's pool."""
    d = canal_sd(x, y)
    if d < 6:
        for name in BIG:
            d = max(d, 2.5 - _rect_sd(x, y, *hall_rect(name, 0.0)))
        if not any(max(_span(x, y, b, 0.5, 1.0)) < 0 for b in BRIDGES):
            d = max(d, PASS_CLEAR - ext_route_dist(x, y))
    pool = _ellipse_sd(x, y, *SERPENT_POOL, 0.35, 9)
    return min(d, pool)


def wet_sd(x, y):
    """Negative where the floor is under SHALLOW water: the flooded passages, the wet halls, the
    canals' banks and fords."""
    ix, iy = int(x // CELL), int(y // CELL)
    best = 1e9
    for dx in (-1, 0, 1):
        for dy in (-1, 0, 1):
            for k in WET_BUCKET.get((ix + dx, iy + dy), ()):
                (ax, ay), (bx, by) = WET_SEGMENTS[k]
                best = min(best, _seg(x, y, ax, ay, bx, by))
    best -= 4.0 + 1.5 * fbm(x / 6, y / 6, 73)
    best = min(best, canal_sd(x, y) - 2.5)
    best = min(best, hall_sd(x, y, WET_HALLS) + 1.0)
    for fx, fy in FORDS:
        best = min(best, math.hypot(x - fx, y - fy) - 6.0)
    for b in BRIDGES:                           # a dry passage keeps dry up to its bridge
        best = max(best, -max(_span(x, y, b, 3.0, 2.5)))
    return best


def deck_height(along, half):
    """A bridge's deck: humped over the canal, sloping down to the dry floor (z 0) at both ends --
    a deck that ended a step above the bank stopped the hero (the user, 2026-11-04, screenshot 6)."""
    u = min(1.0, abs(along) / half)
    return (0.18 + 0.25 * (1 - u * u)) * smoothstep(1.0, BRIDGE_RAMP, u)


def bridge_at(x, y):
    """The deck's height if (x, y) is on a bridge's span, else None."""
    for bx, by, ux, uy, half in BRIDGES:
        along = (x - bx) * ux + (y - by) * uy
        across = -(x - bx) * uy + (y - by) * ux
        if abs(along) < half and abs(across) < BRIDGE_HW:
            return deck_height(along, half)
    return None


def _cross(p1, p2, p3, p4):
    d = (p2[0] - p1[0]) * (p4[1] - p3[1]) - (p2[1] - p1[1]) * (p4[0] - p3[0])
    if abs(d) < 1e-9:
        return None
    t = ((p3[0] - p1[0]) * (p4[1] - p3[1]) - (p3[1] - p1[1]) * (p4[0] - p3[0])) / d
    u = ((p3[0] - p1[0]) * (p2[1] - p1[1]) - (p3[1] - p1[1]) * (p2[0] - p1[0])) / d
    return (p1[0] + t * (p2[0] - p1[0]), p1[1] + t * (p2[1] - p1[1])) if 0 <= t <= 1 and 0 <= u <= 1 else None


def plan_bridges():
    """Where every passage between halls meets a canal: a dry one gets a BRIDGE straight across the
    canal when both its ends land on the passage's dry floor (not rock), it crosses squarely enough
    and no other bridge is near; every other crossing is a FORD (deep_sd keeps the water shallow
    across a passage, so a ford needs nothing more than its stepping stones)."""
    crossings = []
    for (pl, _h), (ca, cb), wet in zip(CURVES, PLAN["links"], PLAN["flooded"]):
        if hall_of(ca) and hall_of(ca) == hall_of(cb):
            continue
        for k in range(len(pl) - 1):
            for p, q in CANAL_SEGMENTS:
                hit = _cross(pl[k], pl[k + 1], p, q)
                if hit and not hall_of((int(hit[0] // CELL), int(hit[1] // CELL))):
                    crossings.append((hit, wet, pl[k], pl[k + 1], p, q))
    for (x, y), wet, a, b, p, q in crossings:
        if wet or any(math.hypot(x - o[0], y - o[1]) < BRIDGE_APART for o in BRIDGES):
            FORDS.append((x, y))
            continue
        cl = math.hypot(q[0] - p[0], q[1] - p[1]) or 1.0
        ax, ay = -(q[1] - p[1]) / cl, (q[0] - p[0]) / cl              # across the canal
        tx, ty = b[0] - a[0], b[1] - a[1]
        tl = math.hypot(tx, ty) or 1.0
        square = (ax * tx + ay * ty) / tl
        if square < 0:
            ax, ay, square = -ax, -ay, -square
        ends = []
        for side in (1, -1):
            t = 2.0
            while t < 14.0:
                ex, ey = x + ax * side * t, y + ay * side * t
                if pass_sd(ex, ey) > -0.3:
                    break                                               # rock before the far bank
                if canal_sd(ex, ey) > BRIDGE_DRY:
                    ends.append(t)
                    break
                t += 0.25
        if square < 0.8 or len(ends) < 2:
            FORDS.append((x, y))
            continue
        BRIDGES.append((x, y, ax, ay, max(ends) + 0.8))
    print(f"BRIDGES {len(BRIDGES)} FORDS {len(FORDS)} (of {len(crossings)} crossings)")


MAX_STEP = 0.55            # metres up per metre across that the game's NavMesh still walks (agentSlope 40, agentClimb 0.3 cells)


def reachability(verts):
    """Every passage node and hall reached on foot from the camp: walks the built floor (ground
    between the deep beds and the rock, decks included) on the 1 m vertex grid."""
    walk = {k for k, v in verts.items() if CLIP_Z < v[2] < 0.5}
    # a hero is ~1.5 m wide: a neck one vertex wide is no way through (the NavMesh closes it)
    walk = {k for k in walk if all((k[0] + di, k[1] + dj) in walk for di, dj in ((1, 0), (-1, 0), (0, 1), (0, -1)))}
    sx, sy = int(CAMP[0]), int(CAMP[1] + 1)
    start = min(walk, key=lambda k: (k[0] - sx) ** 2 + (k[1] - sy) ** 2)
    seen, stack = {start}, [start]
    while stack:
        i, j = stack.pop()
        for di in (-1, 0, 1):
            for dj in (-1, 0, 1):
                n = (i + di, j + dj)
                if n in walk and n not in seen and abs(verts[n][2] - verts[(i, j)][2]) < MAX_STEP * math.hypot(di, dj):
                    seen.add(n)
                    stack.append(n)
    goals = [(c, cell_centre(c)) for c in NODES] + [(name, hall_centre(name)) for name in BIG]
    lost = []
    for name, (x, y) in goals:
        if not any((int(x) + di, int(y) + dj) in seen for di in range(-2, 3) for dj in range(-2, 3)):
            lost.append(name)
    print(f"REACH {len(goals) - len(lost)}/{len(goals)} unreachable: {lost}")
    # the door shut: its passage beyond the outer end is barred -- the throne must be cut off
    gx, gy, ux, uy = door_gate()
    shut = {k for k in walk if abs((k[0] - gx) * ux + (k[1] - gy) * uy) < 1.0 and abs(-(k[0] - gx) * uy + (k[1] - gy) * ux) < 6.0}
    seen, stack = {start}, [start]
    while stack:
        i, j = stack.pop()
        for di in (-1, 0, 1):
            for dj in (-1, 0, 1):
                n = (i + di, j + dj)
                if n in walk and n not in shut and n not in seen and abs(verts[n][2] - verts[(i, j)][2]) < MAX_STEP * math.hypot(di, dj):
                    seen.add(n)
                    stack.append(n)
    tx, ty = hall_centre("throne")
    leak = any((int(tx) + di, int(ty) + dj) in seen for di in range(-2, 3) for dj in range(-2, 3))
    print(f"DOOR SHUT: barred {len(shut)} vertices, throne {'REACHED -- A WAY ROUND THE DOOR' if leak else 'cut off'}")
    cut = [c for c, (x, y) in goals if c != "throne" and (isinstance(c, str) or hall_of(c) != "throne")
           and not any((int(x) + di, int(y) + dj) in seen for di in range(-2, 3) for dj in range(-2, 3))]
    print(f"DOOR SHUT: cut off besides the throne: {cut}")
    return lost


def pillar(x, y):
    return 0.0


def height_at(x, y):
    """(z, depth field): the dry floor at 0, the wading floor at SHALLOW_Z, the deep beds under
    CLIP_Z, the rock above; on a bridge, its deck."""
    sd = pass_sd(x, y)
    dp = deep_sd(x, y)
    wet = wet_sd(x, y)
    base = SHALLOW_Z * smoothstep(1.5, -1.0, wet)
    if dp < 1.0:
        base = min(base, SHALLOW_Z * smoothstep(3.0, 1.0, dp) - DEEP_DROP * smoothstep(1.0, -2.0, dp))
    deck = bridge_at(x, y)
    if deck is not None and sd < 0.6:           # a deck only over the passage, never a tunnel in the rock
        return deck, dp
    rock = smoothstep(-0.2, WALL_RISE, sd) * (WALL_H + 1.4 * fbm(x / 7, y / 7, 53))
    rock += smoothstep(WALL_RISE, WALL_RISE + 5, sd) * 2.2 * fbm(x / 3.5, y / 3.5, 57)   # a broken, craggy top
    if x < 1 or y < 1 or x > N - 1 or y > N - 1:
        rock = max(rock, WALL_H)
    return base + rock, dp


# -- colour zones (as map5: fields negative inside, cut along their zero lines) --------------

FIELD_NAMES = ("wet", "hall", "moss", "path", "lush", "stony", "dark", "alt")


def fields_at(x, y, lk, z):
    """The dwarves' halls underfoot: wet stone at the water, dressed flagstones in the halls, worn
    stone down the passages, moss in the cracks -- as light as Dungeon 1's grass (the user asked
    for no duller dungeons)."""
    sd = pass_sd(x, y)
    d, hw = route_dist(x, y)
    hall = hall_sd(x, y)
    return (
        z - (WATER_Z + 0.1),                                          # wet stone at and under the water
        hall + 1.0 - 2.0 * fbm(x / 6, y / 6, 101),                    # the halls' flagstones
        -sd - (0.5 + 0.7 * fbm(x / 4, y / 4, 103)),                   # a thin hem of moss under the walls
        d - (0.9 + 0.5 * fbm(x / 5, y / 5, 105)),                     # the worn way down a passage
        0.68 - fbm(x / 9, y / 9, 107),                                # moss and grass in the cracks
        0.62 - fbm(x / 7, y / 7, 109),                                # rougher stone
        0.6 - fbm(x / 12, y / 12, 111),                               # earth where the floor broke
        0.55 - fbm(x / 3.2, y / 3.2, 119),                            # a second shade
    )


def zone_colour(f):
    wet, hall, moss, path, lush, stony, dark, alt = f
    if wet < 0:
        return "rock_mid" if alt < 0 else "rock"
    if moss < 0:
        return "moss"
    if hall < 0:
        return "rock_light" if alt > 0 else "pebble"
    if path < 0:
        return "pebble" if alt > 0 else "stony"
    if lush < 0:
        return "grass_mid" if alt < 0 else "moss"
    if dark < 0:
        return "dirt"
    if stony < 0:
        return "rock" if alt < 0 else "rock_light"
    return "pebble" if alt < 0 else "stony"


def relief_colour(z, nz, x, y):
    """Green at the rock's foot; banded rock up the walls; a light rocky top with grass on it. As
    bright as the surface maps (the user, 2026-10-31, «вариант 1»): the dark tops and the wide moss
    made the dungeon ~20% duller than Map 3 (mean luminance 0.52 vs 0.65)."""
    if z < 0.6:
        if nz > 0.6:
            return "moss" if fbm(x / 4, y / 4, 113) > 0.6 else "grass_mid"
        return "rock"
    if z < 1.3:
        return "moss" if fbm(x / 5, y / 5, 115) > 0.75 else "rock" if nz < 0.8 else "grass"
    if z > WALL_H + 0.3 and nz > 0.8:
        if fbm(x / 6, y / 6, 123) > 0.6:
            return "grass_mid"
        return "rock_light" if fbm(x / 9, y / 9, 125) > 0.45 else "rock"
    band = int((z + 1.5 * fbm(x / 7, y / 7, 117)) / 0.8) % 3
    return ["rock", "rock_mid", "rock_light"][band]


# -- the surface (as map5) ---------------------------------------------------------------------

def build_vertices():
    verts = {}
    for i in range(N + 1):
        for j in range(N + 1):
            jx = (_hash(i, j, 5) - 0.5) * 0.6 if 0 < i < N else 0.0
            jy = (_hash(i, j, 7) - 0.5) * 0.6 if 0 < j < N else 0.0
            px, py = i + jx, j + jy
            z, lk = height_at(px, py)
            verts[i, j] = (px - HALF, py - HALF, z, lk) + fields_at(px, py, lk, z)
    return verts


def _cut(poly, k, value=0.0):
    lo, hi = [], []
    for m in range(len(poly)):
        a, b = poly[m], poly[(m + 1) % len(poly)]
        (lo if a[k] < value else hi).append(a)
        if (a[k] < value) != (b[k] < value):
            t = (value - a[k]) / (b[k] - a[k])
            p = tuple(a[q] + (b[q] - a[q]) * t for q in range(len(a)))
            lo.append(p)
            hi.append(p)
    return (lo if len(lo) >= 3 else None), (hi if len(hi) >= 3 else None)


def _split(poly, ks):
    pieces = [poly]
    for k in ks:
        out = []
        for pc in pieces:
            vals = [p[k] for p in pc]
            if min(vals) < 0 < max(vals):
                lo, hi = _cut(pc, k)
                out.extend(q for q in (lo, hi) if q)
            else:
                out.append(pc)
        pieces = out
    return pieces


FIELD_KS = tuple(range(4, 4 + len(FIELD_NAMES)))
CHUNKS = 8


def _centre(poly):
    n = len(poly)
    return tuple(sum(p[q] for p in poly) / n for q in range(len(poly[0])))


def _chunk_of(x, y):
    size = N / CHUNKS
    return (min(CHUNKS - 1, max(0, int((x + HALF) // size))), min(CHUNKS - 1, max(0, int((y + HALF) // size))))


def build_surface(verts):
    ground, relief = {}, {}
    for i in range(N):
        for j in range(N):
            corners = [verts[i, j], verts[i + 1, j], verts[i + 1, j + 1], verts[i, j + 1]]
            if _hash(i, j, 3) < 0.5:
                tris = [(corners[0], corners[1], corners[2]), (corners[0], corners[2], corners[3])]
            else:
                tris = [(corners[0], corners[1], corners[3]), (corners[1], corners[2], corners[3])]
            for tri in tris:
                _, above = _cut(list(tri), 2, CLIP_Z)
                if not above:
                    continue
                c0 = _centre(above)
                is_relief = max(p[2] for p in above) > 0.02 and bridge_at(c0[0] + HALF, c0[1] + HALF) is None
                for piece in _split(above, FIELD_KS):
                    c = _centre(piece)
                    key = _chunk_of(c[0], c[1])
                    if is_relief:
                        a, b, d = (Vector(p[:3]) for p in piece[:3])
                        n = (b - a).cross(d - a)
                        nz = abs(n.z) / max(n.length, 1e-9)
                        relief.setdefault(key, TerrainBuilder()).poly([p[:3] for p in piece],
                                                                      relief_colour(c[2], nz, c[0], c[1]))
                    else:
                        ground.setdefault(key, TerrainBuilder()).poly([p[:3] for p in piece], zone_colour(c[4:]))
    return ground, relief


def build_water(verts):
    """One surface at WATER_Z wherever the floor is under it: foam at the rim, clear shallows over
    the wading floor, blue and deep blue over the canals' and the pool's beds."""
    b = TerrainBuilder()
    for i in range(N):
        for j in range(N):
            corners = [verts[i, j], verts[i + 1, j], verts[i + 1, j + 1], verts[i, j + 1]]
            if min(c[2] for c in corners) >= WATER_Z:
                continue
            pts = []
            for c in corners:
                x, y = c[0], c[1]
                depth = WATER_Z - c[2] + 0.25 * (fbm(x / 6, y / 6, 121) - 0.5)
                pts.append((x, y, WATER_Z, c[2] - WATER_Z, 0.07 - depth, 0.45 - depth, 1.2 - depth))
            for tri in ((pts[0], pts[1], pts[2]), (pts[0], pts[2], pts[3])):
                below, _ = _cut(list(tri), 3)
                if not below:
                    continue
                for pc in _split(below, (4, 5, 6)):
                    c = _centre(pc)
                    col = "foam" if c[4] > 0 else "water_shallow" if c[5] > 0 else "water" if c[6] > 0 else "water_deep"
                    b.poly([p[:3] for p in pc], col)
    return b


def merge_flat(obj):
    me = obj.data
    bm = bmesh.new()
    bm.from_mesh(me)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-4)
    bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(0.5), use_dissolve_boundaries=False,
                             verts=bm.verts, edges=bm.edges, delimit={'UV'})
    bmesh.ops.triangulate(bm, faces=bm.faces, quad_method='BEAUTY', ngon_method='BEAUTY')
    bm.to_mesh(me)
    bm.free()
    return sum(len(p.vertices) - 2 for p in me.polygons)


# -- the map's own decor -----------------------------------------------------------------------

CAMP = (hall_centre("camp")[0], hall_centre("camp")[1] - 2.0, 6)   # kept tidy: (x, y, radius)


HIRE_AT = (CAMP[0] - 14.0, CAMP[1] + 4.0)      # the approved camp buildings (dungeon_camp.py), west
SHOP_AT = (CAMP[0] + 14.0, CAMP[1] + 3.0)      # and east of the campfire, facing the way in
CAMP_SPOTS = [(*HIRE_AT, 5.0), (*SHOP_AT, 3.6)]   # + the way out's forecourt (find_exit)


def find_exit():
    """The way out (the user, 2026-10-31: «развёрнут к игроку», then «в полноценной скале»): a big
    rock standing on the camp's floor against its north wall, straight up from the campfire, the
    cave in its south face."""
    global EXIT_AT
    x, y = CAMP[0], CAMP[1]
    while pass_sd(x, y + 0.25) < 0:
        y += 0.25
    EXIT_AT = (x, y - 2.0)
    CAMP_SPOTS.append((x, y - 3.5, 5.0))
    print(f"EXIT cave face at {x:.1f},{y - 2.0:.1f} (wall at {y:.1f})")


def scatter_decor(rnd):
    items = []

    def add(kind, x, y, s=1.0):
        items.append((kind, x, y, rnd.uniform(0, 6.283), s))

    for i in range(N):
        for j in range(N):
            for _ in range(2):
                x, y = i + rnd.random(), j + rnd.random()
                z, lk = height_at(x, y)
                sd = pass_sd(x, y)
                r = rnd.random()
                if bridge_at(x, y) is not None:
                    continue
                if z < CLIP_Z:                                 # deep water: lilies here and there
                    if fbm(x / 6, y / 6, 131) > 0.55 and r < 0.12:
                        add("lily", x, y, rnd.uniform(0.8, 1.2))
                    continue
                if z < WATER_Z - 0.05:                         # the wading floor: reeds by the walls only
                    if sd > -1.5 and r < 0.25:
                        add("reeds", x, y, rnd.uniform(0.8, 1.25))
                    continue
                if z > 0.05:                                   # the rock: green at its foot, crags above
                    if z < 1.2:
                        if r < 0.14:
                            add("bush", x, y, rnd.uniform(0.9, 1.5))
                        elif r < 0.4:
                            add("tuft" if r < 0.3 else "fern", x, y, rnd.uniform(1.0, 1.5))
                    elif r < 0.05:
                        add("crag", x, y, rnd.uniform(0.6, 1.3))
                    continue
                if math.hypot(x - CAMP[0], y - CAMP[1]) < CAMP[2] or                         any(math.hypot(x - cx, y - cy) < r for cx, cy, r in CAMP_SPOTS):
                    if r < 0.03:
                        add("tuft", x, y)
                    continue
                d, hw = route_dist(x, y)
                if d < 0.8:                                    # the path itself stays clear
                    if r < 0.02:
                        add("pebbles", x, y)
                    continue
                clump = fbm(x / 3, y / 3, 139)
                if sd > -1.8 and r < 0.22:                     # thick along the foot of the rock
                    add("fern" if r < 0.12 else "mushrooms" if r < 0.17 else "bush", x, y, rnd.uniform(0.9, 1.5))
                elif r < 0.2 * clump * 1.6:
                    add("tuft", x, y, rnd.uniform(0.9, 1.4))
                elif r < 0.25 and fbm(x / 10, y / 10, 141) > 0.6:
                    add("flowers", x, y, rnd.uniform(0.9, 1.3))
                elif r < 0.27:
                    add("mushrooms", x, y)
                elif r < 0.285:
                    add("pebbles", x, y, rnd.uniform(0.9, 1.4))
                elif r < 0.288:
                    add("log" if r < 0.2865 else "stump", x, y)
    return items


def decor_variant(kind, x, y, count):
    return int(_hash(int(x * 7), int(y * 7), 151) * count) % count


# a stone's half-width at scale 1, metres (terrain.py crag: the Stone7-10 layouts x1.5)
CRAG_FOOT_M = {"crag": 0.8, "pebbles": 0.25}


def decor_z(kind, x, y, sc=1.0):
    if kind == "lily":
        return WATER_Z + 0.005
    z = height_at(x, y)[0]
    foot = CRAG_FOOT_M.get(kind)
    if foot:
        # a stone sits on the LOWEST ground under its foot, not its middle's: on a slope the
        # downhill side hung in the air by more than half its size (the user, 2026-11-02)
        r = foot * sc   # the prop is scaled by sc * SCALE, as the map by SCALE: x/y units
        for k in range(8):
            a = k * math.pi / 4
            z = min(z, height_at(x + r * math.cos(a), y + r * math.sin(a))[0])
    return max(z, WATER_Z)


# -- the props (building palette) ----------------------------------------------------------------

DARK, STONE, STONE_D, STONE_L = 'window', 'stone_mid', 'stone_dark', 'stone_light'
WOOD, WOOD_D, WOOD_L = 'wood_mid', 'wood_dark', 'wood_light'
lights = []


def w(x, y, z=0.0):
    return (x - HALF, y - HALF, z)


def light(kind, x, y, z):
    lights.append((kind, x, y, z))


fires = []


def fire_spot(x, y, z):
    """Where a bonfire's or a torch's fire goes: no flame model and no light here -- the fire and
    its glow are a Unity effect at the DungeonFire_<n> Empty (the user, 2026-10-31)."""
    fires.append((x, y, z))


def edge_point(x, y, dx, dy, inset=0.35):
    """Walks from (x, y) along (dx, dy) to just short of where the rock begins."""
    t = 0.0
    while t < 12 and pass_sd(x + dx * (t + 0.1), y + dy * (t + 0.1)) < -inset:
        t += 0.1
    return x + dx * t, y + dy * t


def support(b, x, y, dx, dy):
    """Timber props across a passage at (x, y), the beam along (dx, dy): posts where the rock begins."""
    p0, p1 = Vector(w(*edge_point(x, y, -dx, -dy))), Vector(w(*edge_point(x, y, dx, dy)))
    m = (p0 + p1) / 2
    for p in (p0, p1):
        b.box(p.x - 0.1, p.x + 0.1, p.y - 0.1, p.y + 0.1, 0.0, 1.25, WOOD)
        q = p * 0.75 + m * 0.25
        b.rod((p.x, p.y, 0.95), (q.x, q.y, 1.25), 0.07, WOOD)
    b.rod((p0.x, p0.y, 1.25), (p1.x, p1.y, 1.25), 0.18, WOOD_D)
    b.rod((m.x, m.y, 1.18), (m.x, m.y, 1.02), 0.015, 'iron')
    b.box(m.x - 0.07, m.x + 0.07, m.y - 0.07, m.y + 0.07, 0.82, 1.02, 'window_light')
    b.box(m.x - 0.08, m.x + 0.08, m.y - 0.08, m.y + 0.08, 1.02, 1.05, 'iron')
    light('lantern', m.x, m.y, 0.9)


def rails(b, pts):
    for (ax, ay), (bx, by) in zip(pts, pts[1:]):
        p0, p1 = Vector(w(ax, ay)), Vector(w(bx, by))
        d = p1 - p0
        length = d.length
        d.normalize()
        side = Vector((-d.y, d.x, 0.0))
        for k in range(int(length / 0.55)):
            c = p0 + d * (k * 0.55 + 0.2)
            a, e = c - side * 0.38, c + side * 0.38
            b.rod((a.x, a.y, 0.03), (e.x, e.y, 0.03), 0.12, WOOD_D)
        for s in (-0.25, 0.25):
            a, e = p0 + side * s, p1 + side * s
            b.rod((a.x, a.y, 0.08), (e.x, e.y, 0.08), 0.05, 'iron')


def crystal(b, x, y, big=1.0, colour='roof_purple', glow='crystal'):
    cx, cy, _ = w(x, y)
    for ox, oy, h, r, lean in ((0, 0, 0.8, 0.15, 0.0), (0.2, 0.08, 0.55, 0.11, 0.12), (-0.17, 0.12, 0.45, 0.1, -0.1),
                               (0.05, -0.18, 0.35, 0.08, 0.05)):
        h, r = h * big, r * big
        b.frustum(cx + ox * big, cy + oy * big, 0.0, h * 0.7, r, r * 0.8, colour, sides=6, cap=False)
        b.pyramid(cx + ox * big + lean * big, cy + oy * big, h * 0.7, r * 0.8, r * 0.8, h, colour, sides=6)
    b.crag(cx, cy, 0.35 * big, 0.3 * big, 0.12 * big, STONE_D, sides=6, seed=int(x))
    light(glow, cx, cy, 0.6 * big)


def giant_mushroom(b, x, y, h, cap, r=None):
    """A Warcraft-style glowing toadstool: a pale stem, a broad cap in a strong colour with spots."""
    cx, cy, _ = w(x, y)
    r = r or h * 0.55
    b.frustum(cx, cy, 0.0, h, h * 0.13, h * 0.09, 'plaster_white', sides=7)
    b.frustum(cx, cy, h - 0.02, h + r * 0.35, r, r * 0.55, cap, sides=9)
    b.pyramid(cx, cy, h + r * 0.35, r * 0.55, r * 0.55, h + r * 0.6, cap, sides=9)
    for k in range(4):
        a = k * 1.6 + x
        sx, sy = cx + math.cos(a) * r * 0.6, cy + math.sin(a) * r * 0.6
        b.box(sx - 0.04, sx + 0.04, sy - 0.04, sy + 0.04, h + r * 0.28, h + r * 0.36, 'plaster_white')
    kind = 'mushroom' if cap == 'water' else 'crystal' if cap == 'roof_purple' else 'fire'
    light(kind, cx, cy, h * 0.7)


def mushroom_grove(b, x, y, seed, caps):
    r = random.Random(seed)
    for k in range(3 + r.randrange(3)):
        a = r.random() * 6.283
        d = 0.4 + r.random() * 1.4
        giant_mushroom(b, x + math.cos(a) * d, y + math.sin(a) * d, 1.0 + r.random() * 1.2, caps[k % len(caps)])


def web(b, x, y, z, s=1.0, a=0.0):
    """An orb web strung upright between two rock spurs: spokes and rings of thread, its lowest
    point just off the ground (v5's flat triangles read as shards hanging in the air -- the user).
    `z` is kept for the callers but the web always stands on its spurs."""
    cx, cy, _ = w(x, y)
    ux, uy = math.cos(a), math.sin(a)
    hub = s + 0.15

    def at(ang, r):
        return (cx + ux * math.cos(ang) * r, cy + uy * math.cos(ang) * r, hub + math.sin(ang) * r)

    for side in (-1, 1):                                       # the spurs it hangs between
        px, py = cx + ux * side * (s + 0.25), cy + uy * side * (s + 0.25)
        b.crag(px, py, 0.22, 0.2, hub + 0.4, STONE_D, top=STONE, sides=5, seed=int(x * 5) + side)
        b.rod(at(0 if side > 0 else math.pi, s), (px, py, hub + 0.1), 0.03, 'plaster_white')
    spokes = 8
    for k in range(spokes):
        b.rod(at(0, 0), at(k * 2 * math.pi / spokes, s), 0.03, 'plaster_white')
    for ring in (0.35, 0.65, 0.95):
        for k in range(spokes):
            b.rod(at(k * 2 * math.pi / spokes, s * ring), at((k + 1) * 2 * math.pi / spokes, s * ring), 0.025, 'plaster_white')


def skull(b, x, y, z, s=0.16):
    b.box(x - s * 0.5, x + s * 0.5, y - s * 0.45, y + s * 0.45, z, z + s * 0.8, 'plaster', bevel=s * 0.12)
    for k in (-1, 1):
        b.box(x + k * s * 0.2 - s * 0.1, x + k * s * 0.2 + s * 0.1, y - s * 0.47, y - s * 0.4, z + s * 0.3, z + s * 0.52, DARK)


def bones(b, x, y):
    cx, cy, _ = w(x, y)
    for k in range(3):
        a = k * 2.1 + x
        p0 = (cx + math.cos(a) * 0.2, cy + math.sin(a) * 0.2, 0.03)
        b.rod(p0, (p0[0] + math.cos(a + 1.2) * 0.3, p0[1] + math.sin(a + 1.2) * 0.3, 0.03), 0.05, 'plaster')
    skull(b, cx, cy, 0.0)


def build_camp(b):
    ox, oy = hall_centre("camp")[0] - 50.0, hall_centre("camp")[1] - 11.0

    def cw(x, y, z=0.0):
        return w(x + ox, y + oy, z)

    # no hut, barrels or potion stall at the way in (the user, 2026-10-31): the hirelings' camp
    # and the shop are not placed yet
    fx, fy, _ = cw(50.0, 9.0)                                            # the campfire
    for a in (0, 60, 120):
        r = math.radians(a)
        b.rod((fx - math.cos(r) * 0.35, fy - math.sin(r) * 0.35, 0.05), (fx + math.cos(r) * 0.35, fy + math.sin(r) * 0.35, 0.05),
              0.1, WOOD_D)
    for k in range(7):
        a = k * 2 * math.pi / 7
        b.crag(fx + math.cos(a) * 0.55, fy + math.sin(a) * 0.55, 0.12, 0.1, 0.1, STONE_L, sides=5, seed=k)
    fire_spot(fx, fy, 0.5)
    for lx, ly, ax in ((48.2, 7.6, 'x'), (52.3, 8.4, 'y')):
        px, py, _ = cw(lx, ly)
        b.hcylinder(ax, (px, py, 0.13), 0.6, 0.13, WOOD, sides=7)
    mine_exit(b)


def placed(b, origin, k):
    """Context for building in a model's own units (the surface entrance's, ~1 = a cell's worth)
    and moving what was built to `origin`, scaled by `k`."""
    class _Placed:
        def __enter__(self):
            b.bm.verts.ensure_lookup_table()
            self.since = len(b.bm.verts)

        def __exit__(self, *_):
            b.bm.verts.ensure_lookup_table()
            for v in list(b.bm.verts)[self.since:]:
                v.co = Vector(origin) + v.co * k
    return _Placed()


# the way out's stair: steps, each one's rise and run in the rock's own units
EXIT_STEPS, EXIT_STAIR_RISE, EXIT_STAIR_RUN = 4, 0.12, 0.12
EXIT_MOUTH = [(-0.45, 0.0), (0.45, 0.0), (0.52, 0.55), (0.32, 0.93), (0.0, 1.05), (-0.34, 0.9), (-0.52, 0.5)]


exit_glow = Builder()


def mine_exit(b):
    """The way out (the user, 2026-10-31: «сделай выход в полноценной скале ... без ненужной
    геометрии»): the surface entrance's mountain, whole and solid, 2.6x as big -- one big rock, a
    peak behind, two shoulders, turf on top -- with a dark cave in its face. Nothing else."""
    x, y = EXIT_AT
    k = 2.6
    cx, cy, _ = w(x, y)
    with placed(b, (cx, cy + 1.0 * k, 0.0), k):                          # local y -1.0 = the face
        b.crag(0.0, 0.55, 1.75, 1.3, 1.9, STONE, top='grass', sides=8, taper=0.5, seed=1)
        b.crag(0.35, 1.0, 0.95, 0.8, 2.7, STONE, top='grass', sides=6, taper=0.4, seed=4)
        b.crag(-1.25, 0.85, 0.75, 0.9, 1.25, STONE_D, top='grass', sides=6, seed=7)
        b.crag(1.3, 0.65, 0.7, 0.85, 1.05, STONE, top='grass', sides=6, seed=9)
        b.crag(-0.85, -0.8, 0.42, 0.38, 1.25, STONE_D, top='grass', sides=6, seed=12)     # the cave's cheeks
        b.crag(0.9, -0.75, 0.4, 0.36, 1.05, STONE, top='grass', sides=6, seed=15)
        b.crag(0.0, -0.55, 0.85, 0.42, 1.55, STONE_D, top='grass', sides=7, taper=0.6, seed=18)  # the brow, rock not a slab
        # the way up (the user, 2026-11-02, option 2 «каменная лестница»): dressed steps climb
        # to an opening high in the face, full of daylight, a torch either side
        lift = EXIT_STAIR_RISE * EXIT_STEPS
        b.prism([(px * 1.18, pz * 1.18 + lift) for px, pz in EXIT_MOUTH], (0.0, -1.0 - 0.01 + 0.4, 0.0),
                (1, 0, 0), (0, 0, 1), 0.4, STONE_D, back_cap=True)                  # the rim
        for i in range(EXIT_STEPS):
            y0 = -1.0 - EXIT_STAIR_RUN * (EXIT_STEPS - i)
            b.box(-0.42, 0.42, y0, -0.95, 0.0, EXIT_STAIR_RISE * (i + 1), STONE_L, bevel=0.015)
    # the day beyond: its own object (ExitGlow), drawn unlit and bright by the game -- a lit
    # yellow face read as a wall (the user, 2026-11-02)
    with placed(exit_glow, (cx, cy + 1.0 * k, 0.0), k):
        exit_glow.prism([(px * 1.0, pz * 1.0 + EXIT_STAIR_RISE * EXIT_STEPS) for px, pz in EXIT_MOUTH],
                        (0.0, -1.0 - 0.03 + 0.4, 0.0), (1, 0, 0), (0, 0, 1), 0.4, 'window_light', back_cap=True)
    with placed(b, (cx, cy + 1.0 * k, 0.0), k):
        for side in (-1, 1):                                                       # the stair's low walls
            b.box(side * 0.42, side * 0.52, -1.0 - EXIT_STAIR_RUN * EXIT_STEPS, -0.95, 0.0, lift + 0.08, STONE_D,
                  bevel=0.015)
    for side in (-1, 1):                                                           # the torches
        tx, ty = cx + side * 0.72 * k, cy + 1.0 * k - 1.08 * k
        b.rod((tx, ty, 0.0), (tx, ty, 1.0 * k * 0.55), 0.07, WOOD_D)
        b.box(tx - 0.09, tx + 0.09, ty - 0.09, ty + 0.09, 1.0 * k * 0.55, 1.0 * k * 0.55 + 0.14, 'iron')
        fire_spot(tx, ty, 1.0 * k * 0.55 + 0.2)
    light('day', cx, cy + 0.6, (lift + 0.5) * k)                                    # daylight pouring down the steps


def rough_exit_rock(me):
    """The way out's rock is natural rock: its greys draw rough (plaster's grain) instead of the
    dressed courses every other stone prop of the dungeon draws (the user, 2026-10-31). Patterns go
    by colour over the whole props mesh (detail.add_layers), so the rock's faces are rewritten after."""
    x, y = EXIT_AT
    cx, cy, _ = w(x, y)
    pat, det = me.uv_layers["Pattern"], me.uv_layers["Detail"]
    to = detail.METRES[detail.STONE] / detail.METRES[detail.PLASTER]
    n = 0
    for poly in me.polygons:
        c = poly.center
        if not (abs(c.x - cx) < 7.0 and cy - 0.5 < c.y < cy + 9.0):
            continue
        if int(pat.data[poly.loop_indices[0]].uv[0]) != detail.STONE:
            continue
        for li in poly.loop_indices:
            pat.data[li].uv = (detail.PLASTER + 0.5, 0.5)
            u, v = det.data[li].uv
            det.data[li].uv = (u * to, v * to)
        n += 1
    print(f"EXIT rock: {n} faces drawn rough")


def eggs(b, x, y, n=6):
    """A spider's clutch: pale, glossy, egg-shaped sacs under a sheet of web."""
    cx, cy, _ = w(x, y)
    for k in range(n):
        a = k * 2.4
        d = 0.15 + 0.35 * (k % 3) / 2
        b.ball(cx + math.cos(a) * d, cy + math.sin(a) * d, 0.0, 0.2, 0.55, 'plaster_white', sides=7, top='plaster')
    web(b, x, y, 0.7, 1.4, x)                                            # the web strung over it


rewards = []


# -- the halls' furniture (v5, the user: «the rooms are too empty») -------------------------------

def column(b, x, y, h, broken=False):
    """An old dwarven column: a square base, a round shaft, a capital -- or a jagged stump."""
    cx, cy, _ = w(x, y)
    b.box(cx - 0.55, cx + 0.55, cy - 0.55, cy + 0.55, -0.1, 0.35, STONE_D, bevel=0.04)
    b.cylinder(cx, cy, 0.35, h, 0.38, STONE_L, sides=8)
    if broken:
        b.pyramid(cx, cy, h - 0.02, 0.36, 0.36, h + 0.45, STONE_L, sides=5, rot_deg=x * 17)
    else:
        b.box(cx - 0.5, cx + 0.5, cy - 0.5, cy + 0.5, h, h + 0.3, STONE_D, bevel=0.04)
    for k in range(3):                                              # ivy creeping up
        a = k * 2.1 + y
        b.box(cx + math.cos(a) * 0.36 - 0.1, cx + math.cos(a) * 0.36 + 0.1, cy + math.sin(a) * 0.36 - 0.1,
              cy + math.sin(a) * 0.36 + 0.1, 0.3, 0.3 + h * (0.3 + 0.2 * k), 'leaf' if k % 2 else 'grass')


def fallen_column(b, x, y, a, length=3.0):
    cx, cy, _ = w(x, y)
    dx, dy = math.cos(a) * length / 2, math.sin(a) * length / 2
    b.rod((cx - dx, cy - dy, 0.3), (cx + dx, cy + dy, 0.3), 0.6, STONE_L)
    b.box(cx + dx - 0.5, cx + dx + 0.5, cy + dy - 0.5, cy + dy + 0.5, -0.1, 0.5, STONE_D, bevel=0.04)


def ruin_arch(b, x, y, a, span=3.2, h=2.6):
    """Two columns and a lintel, half fallen in: a gateway of the old halls."""
    dx, dy = math.cos(a) * span / 2, math.sin(a) * span / 2
    column(b, x - dx, y - dy, h)
    column(b, x + dx, y + dy, h * 0.75, broken=True)
    p, q = w(x - dx, y - dy, h + 0.45), w(x + dx * 0.4, y + dy * 0.4, h + 0.2)
    b.rod(p, q, 0.45, STONE_D)


def bonfire(b, x, y, big=1.0):
    fx, fy, _ = w(x, y)
    for a in (0, 60, 120):
        r = math.radians(a)
        b.rod((fx - math.cos(r) * 0.45 * big, fy - math.sin(r) * 0.45 * big, 0.06),
              (fx + math.cos(r) * 0.45 * big, fy + math.sin(r) * 0.45 * big, 0.06), 0.12 * big, WOOD_D)
    for k in range(8):
        a = k * 2 * math.pi / 8
        b.crag(fx + math.cos(a) * 0.7 * big, fy + math.sin(a) * 0.7 * big, 0.15, 0.12, 0.14, STONE_D, sides=5, seed=k)
    # no flame: the fire will be an effect at the DungeonLight_fire Empty (the user, 2026-10-31)
    fire_spot(fx, fy, 0.6)


def totem(b, x, y):
    """The monsters' marker: a crooked pole, skulls up it, a red rag."""
    cx, cy, _ = w(x, y)
    b.rod((cx, cy, 0.0), (cx + 0.08, cy, 2.4), 0.14, WOOD_D)
    b.rod((cx - 0.5, cy, 1.8), (cx + 0.55, cy, 1.95), 0.08, WOOD_D)
    for z in (1.1, 1.55, 2.2):
        skull(b, cx + 0.04, cy - 0.08, z, 0.26)
    b.obox((cx + 0.35, cy - 0.02, 1.6), (1, 0, 0), (0, 1, 0), (0.15, 0, 1), 0.2, 0.02, 0.32, 'banner_red')
    for k in range(4):
        a = k * 1.57 + 0.5
        bones(b, x + math.cos(a) * 0.8, y + math.sin(a) * 0.8) if k % 2 else None


def spikes(b, x, y, a, n=5):
    """A row of sharpened stakes leaning outwards."""
    ux, uy = math.cos(a), math.sin(a)
    for k in range(n):
        t = (k - (n - 1) / 2) * 0.55
        px, py, _ = w(x + ux * t, y + uy * t)
        b.rod((px, py, 0.0), (px - uy * 0.5, py + ux * 0.5, 1.3 + 0.2 * (k % 2)), 0.1, WOOD_L if k % 2 else WOOD)
    p0, p1 = w(x - ux * n * 0.3, y - uy * n * 0.3, 0.45), w(x + ux * n * 0.3, y + uy * n * 0.3, 0.45)
    b.rod(p0, p1, 0.08, WOOD_D)


def crate(b, x, y, size=0.6, z=0.0):
    cx, cy, _ = w(x, y)
    b.zbox(cx, cy, z, z + size, size / 2, size / 2, x * 37, WOOD_L)
    b.zbox(cx, cy, z + size * 0.42, z + size * 0.58, size / 2 + 0.02, size / 2 + 0.02, x * 37, WOOD_D)


def cart(b, x, y, a, ore=True):
    cx, cy, _ = w(x, y)
    deg = math.degrees(a)
    b.zbox(cx, cy, 0.25, 0.85, 0.75, 0.5, deg, WOOD, bevel=0.03)
    b.zbox(cx, cy, 0.8, 0.88, 0.78, 0.53, deg, 'iron')
    for sx in (-0.5, 0.5):
        for sy in (-0.52, 0.52):
            px = cx + math.cos(a) * sx - math.sin(a) * sy
            py = cy + math.sin(a) * sx + math.cos(a) * sy
            b.zbox(px, py, 0.0, 0.42, 0.21, 0.05, deg, 'iron')
    if ore:
        b.ball(cx, cy, 0.75, 0.55, 0.45, STONE_D, sides=6, top=STONE)


def cage(b, x, y):
    cx, cy, _ = w(x, y)
    b.box(cx - 0.8, cx + 0.8, cy - 0.8, cy + 0.8, 0.0, 0.15, WOOD_D)
    b.box(cx - 0.85, cx + 0.85, cy - 0.85, cy + 0.85, 1.8, 1.95, WOOD_D)
    for k in range(12):
        t = -0.75 + (k % 6) * 0.3
        px, py = (cx + t, cy - 0.75) if k < 6 else (cx - 0.75, cy + t)
        b.rod((px, py, 0.15), (px, py, 1.8), 0.05, 'iron')
        b.rod((2 * cx - px, 2 * cy - py, 0.15), (2 * cx - px, 2 * cy - py, 1.8), 0.05, 'iron')
    skull(b, cx + 0.1, cy, 0.15, 0.22)


def rune_circle(b, x, y, r=5.0):
    """Where the golem stands: a ring of standing stones round flagstones."""
    cx, cy, _ = w(x, y)
    for k in range(9):
        a = k * 2 * math.pi / 9
        b.crag(cx + math.cos(a) * r, cy + math.sin(a) * r, 0.45, 0.35, 1.6 + 0.6 * (k % 3), STONE_L, top=STONE_D,
               sides=5, seed=k)
        b.box(cx + math.cos(a) * r * 0.55 - 0.6, cx + math.cos(a) * r * 0.55 + 0.6,
              cy + math.sin(a) * r * 0.55 - 0.6, cy + math.sin(a) * r * 0.55 + 0.6, -0.1, 0.06, STONE_D)
    b.cylinder(cx, cy, -0.1, 0.08, r * 0.35, STONE_L, sides=9)
    light('crystal', cx, cy, 0.5)


def pedestal(b, x, y):
    """Where a floating reward hangs: stepped stones, a glow to draw the eye."""
    cx, cy, _ = w(x, y)
    b.cylinder(cx, cy, -0.1, 0.2, 1.1, STONE_D, sides=8)
    b.cylinder(cx, cy, 0.2, 0.5, 0.7, STONE_L, sides=8)
    for k in range(4):
        a = k * math.pi / 2 + 0.4
        b.crag(cx + math.cos(a) * 1.6, cy + math.sin(a) * 1.6, 0.25, 0.2, 0.9, STONE_L, sides=5, seed=k)
    light('crystal', cx, cy, 0.9)


def tent(b, x, y, a):
    """A ridge tent of the abandoned camp (the user, 2026-11-01: the old four-sided cone read as a
    «strange pyramid»): two canvas slopes on a ridge pole, a dark doorway in the front gable,
    poles at both ends, guy ropes to pegs, a bedroll before the door."""
    cx, cy, _ = w(x, y)
    across = (math.cos(a), math.sin(a), 0.0)
    along = (-math.sin(a), math.cos(a), 0.0)
    length, half, height = 2.2, 0.95, 1.2

    def at(s, t, z):
        return (cx + along[0] * s + across[0] * t, cy + along[1] * s + across[1] * t, z)

    # across x up points to -along: start at the front gable and run back
    b.prism([(-half, 0.0), (half, 0.0), (0.0, height)], at(length / 2, 0, 0.0), across, (0, 0, 1), length,
            'plaster', back_cap=True)
    b.prism([(-0.36, 0.0), (0.36, 0.0), (0.0, 0.78)], at(length / 2 + 0.02, 0, 0.0), across, (0, 0, 1), 0.03,
            WOOD_D, back_cap=True)
    b.rod(at(length / 2 + 0.18, 0, height + 0.04), at(-length / 2 - 0.18, 0, height + 0.04), 0.035, WOOD_D)
    for s in (length / 2 + 0.06, -length / 2 - 0.06):
        b.rod(at(s, 0, 0.0), at(s, 0, height + 0.18), 0.035, WOOD_D)
        b.rod(at(s, 0, height + 0.02), at(s + math.copysign(0.75, s), 0, 0.0), 0.012, WOOD_L)
        b.box(*(lambda p: (p[0] - 0.04, p[0] + 0.04, p[1] - 0.04, p[1] + 0.04))(at(s + math.copysign(0.75, s), 0, 0)), 0.0, 0.12, WOOD_D)
    b.zbox(*at(length / 2 + 0.75, 0.2, 0.0)[:2], 0.0, 0.1, 0.35, 0.8, math.degrees(a), 'banner_blue')


def pier(b, x0, y0, x1, y1):
    dx, dy = x1 - x0, y1 - y0
    ln = math.hypot(dx, dy)
    ux, uy = dx / ln, dy / ln
    for k in range(int(ln / 0.5)):
        px, py, _ = w(x0 + ux * (k * 0.5 + 0.25), y0 + uy * (k * 0.5 + 0.25))
        b.zbox(px, py, -0.05, 0.05, 0.23, 0.7, math.degrees(math.atan2(uy, ux)), WOOD if k % 2 else WOOD_L)
    for k in range(0, int(ln) + 1, 2):
        for sd in (-0.65, 0.65):
            px, py, _ = w(x0 + ux * k - uy * sd, y0 + uy * k + ux * sd)
            b.box(px - 0.06, px + 0.06, py - 0.06, py + 0.06, -0.7, 0.3, WOOD_D)


def boat(b, x, y, a):
    cx, cy, _ = w(x, y)
    deg = math.degrees(a)
    b.zbox(cx, cy, -0.25, 0.15, 1.1, 0.42, deg, WOOD, bevel=0.06)
    b.zbox(cx, cy, 0.1, 0.18, 0.9, 0.3, deg, WOOD_D)
    b.rod((cx, cy, 0.2), (cx + math.cos(a + 0.4) * 1.3, cy + math.sin(a + 0.4) * 1.3, 0.25), 0.05, WOOD_L)


class on_floor:
    """Builds a prop where the floor is: what is built inside drops with the wading floor (props
    are made for the dry floor at z = 0)."""
    def __init__(self, b, x, y):
        self.b, self.dz = b, min(0.0, height_at(x, y)[0])

    def __enter__(self):
        self.b.bm.verts.ensure_lookup_table()
        self.since = len(self.b.bm.verts)

    def __exit__(self, *_):
        if self.dz:
            self.b.bm.verts.ensure_lookup_table()
            for v in list(self.b.bm.verts)[self.since:]:
                v.co.z += self.dz


def scatter(r, box, n, fn, clear=-1.5, b=None):
    for _ in range(n):
        spot = free_spot(r, *box, clear=clear)
        if spot:
            if b is None:
                fn(*spot)
            else:
                with on_floor(b, *spot):
                    fn(*spot)


statues = []


def stalagmite(b, x, y, h):
    cx, cy, _ = w(x, y)
    r = h * 0.28
    b.frustum(cx, cy, -0.5, h * 0.55, r, r * 0.55, STONE, sides=6)
    b.pyramid(cx, cy, h * 0.55, r * 0.55, r * 0.55, h, STONE_L, sides=6)


def boulder(b, x, y, r):
    cx, cy, _ = w(x, y)
    b.ball(cx, cy, -0.5, r, r * 1.3 + 0.35, STONE if int(x * 7) % 2 else STONE_D, sides=6, top=STONE_L)


def free_spot(r, x0, y0, x1, y1, clear=-1.2, tries=30):
    """A random spot inside the box that is floor (dry or wading), with room round it, off the
    trodden path, away from deep water and the bridges."""
    for _ in range(tries):
        x, y = r.uniform(x0, x1), r.uniform(y0, y1)
        if pass_sd(x, y) < clear and route_dist(x, y)[0] > 2.5 and deep_sd(x, y) > 2.5 and bridge_at(x, y) is None:
            return x, y
    return None


# -- the drowned halls' own pieces ---------------------------------------------------------------

def stone_bridge(b, x, y, ux, uy, half):
    """A dwarven stone bridge over a canal: a humped deck of slabs (the walkable deck is the
    ground's, height_at -> bridge_at; this sits a hair above it), low parapets, piers in the water."""
    deg = math.degrees(math.atan2(uy, ux))

    def deck(t):
        return deck_height(t, half)
    n = int(2 * half / 0.8)
    for k in range(n + 1):
        t = -half + k * 2 * half / n
        px, py = x + ux * t, y + uy * t
        z = deck(t)
        cx, cy, _ = w(px, py)
        b.zbox(cx, cy, z - 0.35, z + 0.04, 0.46, BRIDGE_HW + 0.05, deg, STONE_L if k % 2 else STONE)
        for side in (-1, 1):
            qx, qy, _ = w(px - uy * side * (BRIDGE_HW + 0.12), py + ux * side * (BRIDGE_HW + 0.12))
            b.zbox(qx, qy, z, z + 0.55, 0.46, 0.14, deg, STONE_D if k % 2 else STONE)
    for t in (-min(CANAL_W * 0.9, half - 1.2), 0.0, min(CANAL_W * 0.9, half - 1.2)):   # the piers, down into the water
        px, py = x + ux * t, y + uy * t
        cx, cy, _ = w(px, py)
        b.zbox(cx, cy, -2.4, deck(t) - 0.3, 0.45, BRIDGE_HW + 0.2, deg, STONE_D)
    for side in (-1, 1):                                     # a lantern post at each end
        px, py = x + ux * side * half, y + uy * side * half
        qx, qy, _ = w(px - uy * (BRIDGE_HW + 0.12), py + ux * (BRIDGE_HW + 0.12))
        b.box(qx - 0.12, qx + 0.12, qy - 0.12, qy + 0.12, 0.0, 1.3, STONE_D)
        b.box(qx - 0.1, qx + 0.1, qy - 0.1, qy + 0.1, 1.3, 1.5, 'window_light')
        light('lantern', qx, qy, 1.4)


def ford_marks(b, x, y):
    """Where a flooded passage wades a canal: a row of worn stepping stones and two posts."""
    r = random.Random(int(x * 13 + y))
    for k in range(5):
        a = r.uniform(0, 6.28)
        cx, cy, _ = w(x + math.cos(a) * r.uniform(1.0, 3.5), y + math.sin(a) * r.uniform(1.0, 3.5))
        b.cylinder(cx, cy, SHALLOW_Z - 0.1, WATER_Z + 0.06, r.uniform(0.35, 0.55), STONE, sides=6, rot_deg=r.uniform(0, 60))
    for side in (-1, 1):
        cx, cy, _ = w(x + side * 3.8, y + side * 1.2)
        b.rod((cx, cy, SHALLOW_Z), (cx + 0.1, cy, 1.4), 0.12, WOOD_D)


def dwarf_column(b, x, y, h, broken=False):
    """A dressed dwarven column: a square plinth, a fluted octagonal shaft, a square capital with a
    gold band -- or the shaft snapped off."""
    cx, cy, _ = w(x, y)
    b.box(cx - 0.7, cx + 0.7, cy - 0.7, cy + 0.7, -0.5, 0.4, STONE_D, bevel=0.04)
    b.box(cx - 0.55, cx + 0.55, cy - 0.55, cy + 0.55, 0.4, 0.6, STONE, bevel=0.03)
    b.cylinder(cx, cy, 0.6, h, 0.42, STONE_L, sides=8)
    if broken:
        b.pyramid(cx, cy, h - 0.02, 0.4, 0.4, h + 0.5, STONE_L, sides=5, rot_deg=x * 17)
        return
    b.box(cx - 0.5, cx + 0.5, cy - 0.5, cy + 0.5, h, h + 0.12, 'gold')
    b.box(cx - 0.65, cx + 0.65, cy - 0.65, cy + 0.65, h + 0.12, h + 0.45, STONE_D, bevel=0.04)


def brazier(b, x, y):
    cx, cy, _ = w(x, y)
    b.frustum(cx, cy, 0.0, 0.9, 0.22, 0.14, 'iron', sides=6)
    b.frustum(cx, cy, 0.9, 1.15, 0.25, 0.45, 'iron', sides=8)
    fire_spot(cx, cy, 1.25)


def banner(b, x, y, a, colour='banner_red'):
    cx, cy, _ = w(x, y)
    b.rod((cx, cy, 0.0), (cx, cy, 2.8), 0.1, WOOD_D)
    b.obox((cx + math.cos(a) * 0.05, cy + math.sin(a) * 0.05, 2.0), (math.cos(a), math.sin(a), 0), (-math.sin(a), math.cos(a), 0),
           (0, 0, 1), 0.02, 0.45, 0.75, colour)


def throne(b, x, y):
    """The cursed king's throne at the hall's far (north) end: a stepped round dais, the high
    stone seat facing the way in, gold on its back, braziers and banners round it."""
    cx, cy, _ = w(x, y)
    b.cylinder(cx, cy, -0.1, 0.3, 4.2, STONE_D, sides=10)
    b.cylinder(cx, cy, 0.3, 0.6, 3.2, STONE, sides=10)
    b.cylinder(cx, cy, 0.6, 0.9, 2.2, STONE_L, sides=10)
    b.box(cx - 0.9, cx + 0.9, cy - 0.7, cy + 0.5, 0.9, 1.5, STONE_D, bevel=0.05)       # the seat
    b.box(cx - 0.9, cx + 0.9, cy + 0.5, cy + 0.9, 0.9, 3.6, STONE_D, bevel=0.05)       # the back
    b.box(cx - 0.75, cx + 0.75, cy + 0.45, cy + 0.52, 1.6, 3.3, 'gold')
    b.pyramid(cx, cy + 0.7, 3.6, 0.9, 0.2, 4.4, STONE_D, sides=4)
    for side in (-1, 1):
        b.box(cx + side * 0.9 - 0.15, cx + side * 0.9 + 0.15, cy - 0.7, cy + 0.9, 0.9, 2.0, STONE, bevel=0.03)
        b.ball(cx + side * 0.9, cy - 0.6, 2.0, 0.22, 0.3, 'gold', sides=6)
        brazier(b, x + side * 3.6, y - 1.5)
        banner(b, x + side * 2.2, y + 2.2, -math.pi / 2, 'banner_red')
    light('crystal', cx, cy - 0.5, 2.5)
    thrones.append((cx, cy - 2.6, 0.0))           # where the king stands, before his seat


LEVER_PIVOT = 1.05           # the arm's axle over the floor (build units)


def lever(b, x, y):
    """A dwarven lever (the user, 2026-11-04: the first one's shape read as strange): a dressed
    stone block with a slot, two cheeks carrying the axle, an upright cog wheel behind it, a chain
    from the wheel into the floor. The ARM is its own object (lever_arm), so the game can pull it."""
    cx, cy, _ = w(x, y)
    b.box(cx - 0.85, cx + 0.85, cy - 0.65, cy + 0.65, -0.6, 0.55, STONE_D, bevel=0.05)
    b.box(cx - 0.7, cx + 0.7, cy - 0.5, cy + 0.5, 0.55, 0.7, STONE, bevel=0.03)
    b.box(cx - 0.12, cx + 0.12, cy - 0.42, cy + 0.42, 0.7, 0.71, DARK)            # the slot the arm swings in
    for side in (-1, 1):                                                          # the cheeks and the axle's caps
        b.box(cx + side * 0.2 - 0.09, cx + side * 0.2 + 0.09, cy - 0.22, cy + 0.22, 0.7, LEVER_PIVOT + 0.22, STONE_L, bevel=0.02)
        b.hcylinder('x', (cx + side * 0.3, cy, LEVER_PIVOT), 0.03, 0.12, 'gold', sides=8)
    gx, gy, gz = cx, cy + 0.95, 1.25                                              # the cog wheel, upright, facing the camera
    b.box(gx - 0.12, gx + 0.12, gy - 0.12, gy + 0.12, -0.6, gz, STONE_D)
    b.hcylinder('y', (gx, gy, gz), 0.12, 0.8, STONE, sides=14)
    b.hcylinder('y', (gx, gy, gz), 0.16, 0.25, 'gold', sides=8)
    for k in range(12):
        a = k * 2 * math.pi / 12
        tx, tz = gx + math.cos(a) * 0.88, gz + math.sin(a) * 0.88
        b.obox((tx, gy, tz), (math.cos(a), 0, math.sin(a)), (0, 1, 0), (-math.sin(a), 0, math.cos(a)), 0.1, 0.13, 0.09, 'gold')
    for k in range(5):                                                            # the chain down into the floor
        b.box(gx + 0.75 - 0.05, gx + 0.75 + 0.05, gy - 0.05, gy + 0.05, gz - 0.25 * k - 0.2, gz - 0.25 * k - 0.05, 'iron')
    light('lantern', cx, cy - 0.7, 1.7)
    levers.append((cx, cy, LEVER_PIVOT + min(0.0, height_at(x, y)[0])))


lever_arm = Builder()


def build_lever_arm(b):
    """The lever's arm round its axle at the origin, standing UP and leaning to the camera (-y);
    the game turns it about x to pull it over."""
    b.ball(0.0, 0.0, -0.18, 0.18, 0.36, 'gold', sides=8)
    b.rod((0.0, 0.0, 0.0), (0.0, -0.45, 1.35), 0.13, WOOD_D)
    for t in (0.25, 0.65):
        b.rod((0.0, -0.45 * t + 0.02, 1.35 * t - 0.06), (0.0, -0.45 * t - 0.02, 1.35 * t + 0.06), 0.16, 'iron')
    b.rod((-0.28, -0.47, 1.4), (0.28, -0.47, 1.4), 0.1, WOOD_L)                 # the grip across the top
    for side in (-1, 1):
        b.ball(side * 0.3, -0.47, 1.33, 0.08, 0.14, 'gold', sides=6)


def gate(b, bars, x, y, ux, uy):
    """The lever door before the king: two massive posts where the rock begins, a carved lintel,
    and the iron bars (their own object, so the game can raise them when both levers are pulled)."""
    nx, ny = -uy, ux
    p0, p1 = Vector(w(*edge_point(x, y, -nx, -ny, 0.2))), Vector(w(*edge_point(x, y, nx, ny, 0.2)))
    deg = math.degrees(math.atan2(ny, nx))
    for p in (p0, p1):
        b.zbox(p.x, p.y, -0.5, 3.6, 0.7, 0.8, deg, STONE_D, bevel=0.05)
        b.zbox(p.x, p.y, 3.6, 3.9, 0.85, 0.95, deg, STONE, bevel=0.04)
    m = (p0 + p1) / 2
    span = (p1 - p0).length
    b.zbox(m.x, m.y, 3.3, 4.1, span / 2 + 0.6, 0.7, deg, STONE, bevel=0.05)
    b.zbox(m.x, m.y, 3.6, 3.8, span / 4, 0.75, deg, 'gold')
    d = (p1 - p0).normalized()
    n = max(3, int(span / 0.45))
    for k in range(1, n):
        q = p0 + d * (span * k / n)
        bars.rod((q.x, q.y, -0.4), (q.x, q.y, 3.35), 0.09, 'iron')
    for z in (0.6, 1.8, 3.0):
        bars.rod((p0.x, p0.y, z), (p1.x, p1.y, z), 0.12, 'iron')
    light('lantern', m.x - uy * 1.2, m.y + ux * 1.2, 3.0)
    doors.append((m.x, m.y, 0.0))


def forge(b, x, y):
    """A dwarven forge: the hearth's stone hood over a fire, an anvil, a quench trough, bars of
    metal, an ore cart."""
    cx, cy, _ = w(x, y)
    b.box(cx - 1.5, cx + 1.5, cy + 0.6, cy + 2.2, -0.1, 1.0, STONE_D, bevel=0.05)
    b.frustum(cx, cy + 1.4, 1.0, 3.2, 1.6, 0.6, STONE, sides=4, rot_deg=45)
    b.box(cx - 0.8, cx + 0.8, cy + 0.55, cy + 0.65, 0.3, 0.9, 'fire')
    fire_spot(cx, cy + 1.2, 1.1)
    b.box(cx - 0.35, cx + 0.35, cy - 1.6, cy - 1.1, 0.0, 0.55, STONE_D)               # the anvil
    b.box(cx - 0.55, cx + 0.55, cy - 1.55, cy - 1.15, 0.55, 0.8, 'iron')
    b.box(cx + 1.6, cx + 2.8, cy - 1.0, cy - 0.3, 0.0, 0.5, STONE)                    # the trough
    b.box(cx + 1.7, cx + 2.7, cy - 0.9, cy - 0.4, 0.45, 0.48, 'water')
    for k in range(3):
        b.box(cx - 2.4, cx - 1.7, cy - 0.6 + k * 0.3, cy - 0.4 + k * 0.3, 0.0, 0.18 + 0.04 * k, 'gold' if k == 1 else 'iron')


def build_halls(b, r):
    # the great drowned hall: two rows of columns standing in the water, some broken or fallen
    x0, y0, x1, y1 = hall_rect("great", 5.0)
    for row in (0.3, 0.7):
        for k in range(6):
            x, y = x0 + (x1 - x0) * (k + 0.5) / 6 + r.uniform(-1, 1), y0 + (y1 - y0) * row
            if pass_sd(x, y) < -1.0 and deep_sd(x, y) > 2.0 and route_dist(x, y)[0] > 1.5:
                with on_floor(b, x, y):
                    dwarf_column(b, x, y, r.uniform(4.0, 5.0), r.random() < 0.3)
    box = hall_rect("great", 3.0)
    scatter(r, box, 3, lambda x, y: fallen_column(b, x, y, r.uniform(0, 3.1), r.uniform(3.0, 4.5)), -2.0, b)
    scatter(r, box, 2, lambda x, y: ruin_arch(b, x, y, r.uniform(0, 3.1)), -2.5, b)
    # the serpent's hall: broken columns round its deep pool, bones on the rim
    sx, sy, rx, ry = SERPENT_POOL
    for k in range(9):
        a = k * 2 * math.pi / 9 + 0.3
        x, y = sx + math.cos(a) * (rx + 3.5), sy + math.sin(a) * (ry + 3.5)
        if pass_sd(x, y) < -1.0:
            with on_floor(b, x, y):
                dwarf_column(b, x, y, r.uniform(2.0, 4.5), k % 2 == 0)
    scatter(r, hall_rect("serpent", 3.0), 6, lambda x, y: bones(b, x, y), -0.8, b)
    # the throne hall: the throne at the north, columns down both sides, braziers
    tx0, ty0, tx1, ty1 = hall_rect("throne", 4.0)
    tcx = (tx0 + tx1) / 2
    ty = ty1 - 6.0
    while pass_sd(tcx, ty) > -4.5 and ty > ty0:
        ty -= 1.0
    throne(b, tcx, ty)
    for side in (-1, 1):
        for k in range(4):
            x, y = tcx + side * (tx1 - tx0) * 0.33, ty0 + (ty1 - ty0) * (k + 0.5) / 4.5
            if pass_sd(x, y) < -1.5:
                dwarf_column(b, x, y, 5.0, False)
    scatter(r, hall_rect("throne", 3.0), 4, lambda x, y: brazier(b, x, y), -1.5, b)
    scatter(r, hall_rect("throne", 3.0), 6, lambda x, y: skull(b, *w(x, y)[:2], 0.0, 0.3), -0.8, b)
    # the lever halls: the lever in the middle, broken columns, a guard's bones
    for name in ("lever_w", "lever_e"):
        lx, ly = hall_centre(name)
        if pass_sd(lx, ly) > -4:
            lx, ly = free_spot(r, *hall_rect(name, 4.0), clear=-4.0) or (lx, ly)
        with on_floor(b, lx, ly):
            lever(b, lx, ly)
        box = hall_rect(name, 3.0)
        scatter(r, box, 4, lambda x, y: dwarf_column(b, x, y, r.uniform(2.0, 4.0), r.random() < 0.6), -1.2, b)
        scatter(r, box, 3, lambda x, y: bones(b, x, y), -0.8, b)
    # the forges: the hearth, carts, rails, crates, a bonfire for the monsters living there
    for name in ("forge_w", "forge_n"):
        fx, fy = hall_centre(name)
        if pass_sd(fx, fy) > -5:
            fx, fy = free_spot(r, *hall_rect(name, 5.0), clear=-5.0) or (fx, fy)
        with on_floor(b, fx, fy):
            forge(b, fx, fy)
        box = hall_rect(name, 3.0)
        scatter(r, box, 2, lambda x, y: cart(b, x, y, r.uniform(0, 3.1)), -2.0, b)
        scatter(r, box, 2, lambda x, y: rails(b, [(x - 3, y), (x + 3, y + r.uniform(-1, 1))]), -2.5, b)
        scatter(r, box, 5, lambda x, y: crate(b, x, y, r.uniform(0.45, 0.7)), -1.5, b)
        scatter(r, box, 2, lambda x, y: totem(b, x, y), -1.5, b)
        scatter(r, box, 3, lambda x, y: dwarf_column(b, x, y, r.uniform(2.0, 4.0), True), -1.2, b)
    # the monster junctions: a totem each; the crystal rooms: a pedestal, mushrooms round it
    for c in PLAN["monster_halls"]:
        cx, cy = cell_centre(c)
        with on_floor(b, cx + 2.0, cy + 1.5):
            totem(b, cx + 2.0, cy + 1.5)
    for i, c in enumerate(PLAN["crystal_rooms"]):
        cx, cy = cell_centre(c)
        with on_floor(b, cx, cy):
            pedestal(b, cx, cy)
            giant_mushroom(b, cx + 2.6, cy - 1.4, 1.3, 'water')
            giant_mushroom(b, cx - 2.4, cy + 1.6, 1.0, 'roof_purple')


levers, doors, thrones = [], [], []
door_bars = Builder()


def build_props(b):
    build_camp(b)
    rnd = random.Random(5)
    caps = (('water', 'roof_purple'), ('roof_purple', 'banner_red'), ('water',), ('banner_red', 'water'))
    # the niches: each has something in it, so no two stretches of passage look alike
    for i, (x, y, r, kind, (tx, ty)) in enumerate(NICHES):
        with on_floor(b, x, y):
            if kind == "grove":
                mushroom_grove(b, x, y, i, caps[i % 4])
            elif kind == "ruin":
                dwarf_column(b, x, y, rnd.uniform(2.5, 4.0), rnd.random() < 0.6)
                fallen_column(b, x + tx * 1.8, y + ty * 1.8, math.atan2(ty, tx) + 0.6, 2.4)
            elif kind == "mine":
                support(b, x - tx * 1.2, y - ty * 1.2, tx, ty)
                rails(b, [(x - tx * 2.5, y - ty * 2.5), (x + tx * 2.5, y + ty * 2.5)])
                cx, cy, _ = w(x + ty * 1.2, y - tx * 1.2)
                b.frustum(cx, cy, 0.0, 0.7, 0.32, 0.36, WOOD, sides=8)               # a barrel
            elif kind == "rubble":
                for k in range(5):
                    a = k * 1.3 + i
                    d = rnd.uniform(0.5, r - 0.4)
                    if k % 2:
                        boulder(b, x + math.cos(a) * d, y + math.sin(a) * d, rnd.uniform(0.35, 0.8))
                    else:
                        stalagmite(b, x + math.cos(a) * d, y + math.sin(a) * d, rnd.uniform(1.2, 2.6))
            elif kind == "bones":
                bones(b, x, y)
                bones(b, x + 1.0, y - 0.8)
                cx, cy, _ = w(x - 0.8, y + 0.6)
                b.rod((cx, cy, 0.0), (cx, cy, 1.0), 0.06, WOOD_D)                    # a torch
                fire_spot(cx, cy, 1.1)
    # stalagmites along the passages' sides, here and there
    for k, ((ax, ay), (bx, by)) in enumerate(SEGMENTS):
        if rnd.random() < 0.12:
            mx, my = (ax + bx) / 2, (ay + by) / 2
            ln = math.hypot(bx - ax, by - ay) or 1.0
            nx, ny = -(by - ay) / ln, (bx - ax) / ln
            if rnd.random() < 0.5:
                nx, ny = -nx, -ny
            ex, ey = edge_point(mx, my, nx, ny, 0.6)
            with on_floor(b, ex, ey):
                stalagmite(b, ex, ey, rnd.uniform(0.9, 2.2))
    # the canals: bridges for the dry passages, stepping stones at the fords
    for x, y, ux, uy, half in BRIDGES:
        stone_bridge(b, x, y, ux, uy, half)
    for x, y in FORDS:
        ford_marks(b, x, y)
    # the lever door before the king's hall
    gate(b, door_bars, *door_gate())
    # mushroom groves and stalagmites in the halls
    k = 100
    for name in BIG:
        if name == "camp":
            continue
        box = hall_rect(name, 3.0)
        for _ in range(3):
            spot = free_spot(rnd, *box)
            if spot:
                with on_floor(b, *spot):
                    mushroom_grove(b, spot[0], spot[1], k, caps[k % 4])
                k += 1
        for _ in range(5):
            spot = free_spot(rnd, *box, clear=-0.3)
            if spot and pass_sd(*spot) > -1.5:
                with on_floor(b, *spot):
                    stalagmite(b, spot[0], spot[1], rnd.uniform(1.0, 2.8))
    for c in PLAN["monster_halls"]:
        cx, cy = cell_centre(c)
        with on_floor(b, cx, cy):
            bones(b, cx - 1.5, cy + 0.8)
            bones(b, cx + 1.2, cy - 1.4)
    build_halls(b, random.Random(9))
    # the crystal rooms: nothing on the ground -- the reward will float in the air (the user)
    for i, c in enumerate(PLAN["crystal_rooms"]):
        x, y = cell_centre(c)
        cx, cy, _ = w(x, y)
        if POPULATED:
            rewards.append((cx, cy, 1.6 + min(0.0, height_at(x, y)[0])))


# -- scene -----------------------------------------------------------------------------------

def split_by_chunk(obj, coll):
    """Cuts a props mesh into the ground's chunks: each loose part goes whole to the chunk its middle
    is in, so a prop never pops in half. Returns the number of chunk objects."""
    import bmesh
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    parent = list(range(len(bm.verts)))

    def find(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i

    for e in bm.edges:
        a, c = find(e.verts[0].index), find(e.verts[1].index)
        if a != c:
            parent[a] = c
    sums = {}
    for v in bm.verts:
        t = sums.setdefault(find(v.index), [0.0, 0.0, 0])
        t[0] += v.co.x
        t[1] += v.co.y
        t[2] += 1
    of_root = {k: _chunk_of(t[0] / t[2], t[1] / t[2]) for k, t in sums.items()}
    face_chunk = [of_root[find(f.verts[0].index)] for f in bm.faces]
    for ch in sorted(set(face_chunk)):
        part = bm.copy()
        part.faces.ensure_lookup_table()
        bmesh.ops.delete(part, geom=[f for f, c in zip(part.faces, face_chunk) if c != ch], context='FACES')
        me = bpy.data.meshes.new(f"{obj.name}-c{ch[0]}{ch[1]}")
        part.to_mesh(me)
        part.free()
        for m in obj.data.materials:
            me.materials.append(m)
        coll.objects.link(bpy.data.objects.new(me.name, me))
    bm.free()
    n = len(set(face_chunk))
    bpy.data.objects.remove(obj)
    return n


def make_object(name, builder, material, collection):
    me = builder.to_mesh(name)
    me.materials.append(material)
    obj = bpy.data.objects.new(name, me)
    collection.objects.link(obj)
    return obj


def lair_rows():
    """Where the monsters live (as dungeon_lairs.rows for Dungeon 1): a lair at every monster
    junction, the great hall and the forges; the cursed king in his hall, the serpent in its pool;
    lone crystals in the farthest empty dead ends; patrols along the long passages."""
    import dungeon_lairs

    def game(x, y):
        return (x - HALF) * SCALE, (y - HALF) * SCALE
    out = [("lair",) + game(*cell_centre(c)) for c in PLAN["monster_halls"]]
    out += [("lair",) + game(*hall_centre(name)) for name in ("great", "forge_w", "forge_n")]
    out.append(("boss_king",) + game(*hall_centre("throne")))
    out.append(("boss_serpent",) + game(SERPENT_POOL[0], SERPENT_POOL[1]))
    camp = game(*hall_centre("camp"))
    for c in dungeon_lairs.lone_crystals(PLAN, POS, game, camp):
        out.append(("crystal",) + game(*POS[c]))
    lairs = [(x, y) for k, x, y in out if k == "lair" or k.startswith("boss_")]
    for k, route in enumerate(dungeon_lairs.patrols(PLAN, POS, game, camp, lairs)):
        out += [(f"patrol_{k}", x, y) for x, y in route]
    return out


def main():
    folder = os.path.dirname(OUT)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.wm.save_as_mainfile(filepath=OUT)
    timg = terrain.ensure_palette(os.path.join(folder, "Terrain-Palette.png"))
    tmat = terrain.ensure_material(timg)
    bimg = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    bmat = kit.ensure_material(bimg)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(bmat, bimg, atlas)

    find_exit()
    plan_bridges()
    verts = build_vertices()
    reachability(verts)
    ground, relief = build_surface(verts)
    coll = bpy.data.collections.new("Dungeon")
    bpy.context.scene.collection.children.link(coll)
    g = r = 0
    for (cx, cy), b in sorted(ground.items()):
        g += merge_flat(make_object(f"{PREFIX}-Ground-c{cx}{cy}", b, tmat, coll))
    for (cx, cy), b in sorted(relief.items()):
        r += merge_flat(make_object(f"{PREFIX}-Relief-c{cx}{cy}", b, tmat, coll))
    wtr = merge_flat(make_object(f"{PREFIX}-Water", build_water(verts), tmat, coll))

    pb = Builder()
    build_props(pb)
    ptris = pb.triangles()
    props = make_object(f"{PREFIX}-Props", pb, bmat, coll)
    detail.add_layers(props.data, kit.NAMES, kit.COLS, kit.ROWS)
    rough_exit_rock(props.data)
    pchunks = split_by_chunk(props, coll)
    make_object(f"{PREFIX}-ExitGlow", exit_glow, bmat, coll)   # the way out's daylight, drawn unlit
    build_lever_arm(lever_arm)                                # the lever's arm at the origin: the game places one per lever
    arm = make_object(f"{PREFIX}-LeverArm", lever_arm, bmat, coll)
    detail.add_layers(arm.data, kit.NAMES, kit.COLS, kit.ROWS)
    bars = make_object(f"{PREFIX}-Door", door_bars, bmat, coll)  # the lever door's bars: the game raises them
    detail.add_layers(bars.data, kit.NAMES, kit.COLS, kit.ROWS)
    import dungeon_camp                                       # the approved camp buildings, whole
    for name, build, (x, y) in (("HireCamp", dungeon_camp.hire_camp, HIRE_AT),
                                ("PotionShop", dungeon_camp.potion_shop, SHOP_AT)):
        ob = make_object(f"{PREFIX}-{name}", build(), bmat, coll)
        detail.add_layers(ob.data, kit.NAMES, kit.COLS, kit.ROWS)
        ob.location = w(x, y)
    for i, (x, y, z) in enumerate(fires):
        e = bpy.data.objects.new(f"DungeonFire_{i}", None)
        e.empty_display_size = 0.3
        coll.objects.link(e)
        e.location = (x, y, z)
    for i, (x, y, z) in enumerate(rewards):
        e = bpy.data.objects.new(f"DungeonReward_{i}", None)
        e.empty_display_type = 'SPHERE'
        e.empty_display_size = 0.5
        coll.objects.link(e)
        e.location = (x, y, z)
    for i, (x, y, z) in enumerate(statues):
        e = bpy.data.objects.new(f"statue_{i}", None)
        e.empty_display_size = 0.5
        coll.objects.link(e)
        e.location = (x, y, z)
    for i, (kind, x, y, z) in enumerate(lights):
        e = bpy.data.objects.new(f"DungeonLight_{kind}_{i}", None)
        e.empty_display_size = 0.3
        coll.objects.link(e)
        e.location = (x, y, z)

    rnd = random.Random(31)
    items = scatter_decor(rnd)
    kcoll = bpy.data.collections.new("DecorKit")
    bpy.context.scene.collection.children.link(kcoll)
    protos = terrain.build_prototypes(tmat, kcoll)
    for kind, meshes in protos.items():                       # the kit export_map.py writes out
        for v, me in enumerate(meshes):
            o = bpy.data.objects.new(f"{PREFIX}-DecorKit-{kind}_{v}", me)
            kcoll.objects.link(o)
            o.hide_render = True
    decor_tris = 0
    with open(os.path.splitext(OUT)[0] + "-decor.csv", "w", encoding="utf-8") as f:
        f.write("kind,variant,x,y,z,rot,scale\n")
        for kind, x, y, rot, sc in items:
            v = decor_variant(kind, x, y, len(protos[kind]))
            decor_tris += sum(len(p.vertices) - 2 for p in protos[kind][v].polygons)
            f.write(f"{kind},{v},{(x - HALF) * SCALE:.3f},{(y - HALF) * SCALE:.3f},{decor_z(kind, x, y, sc) * SCALE:.3f},"
                    f"{rot:.3f},{sc * SCALE:.3f}\n")
    if PREVIEW:
        dcoll = bpy.data.collections.new("Decor")
        bpy.context.scene.collection.children.link(dcoll)
        for kind, x, y, rot, sc in items:
            o = bpy.data.objects.new(f"d_{kind}", protos[kind][decor_variant(kind, x, y, len(protos[kind]))])
            o.location = ((x - HALF) * SCALE, (y - HALF) * SCALE, decor_z(kind, x, y, sc) * SCALE)
            o.rotation_euler = (0, 0, rot)
            o.scale = (sc * SCALE,) * 3
            dcoll.objects.link(o)
    counts = {}
    for it in items:
        counts[it[0]] = counts.get(it[0], 0) + 1
    print(f"DUNGEON ground tris={g} relief tris={r} water tris={wtr} props tris={ptris} "
          f"in {pchunks} chunks decor={len(items)} ({decor_tris} tris) lights={len(lights)} {counts}")
    for ob in coll.objects:                                   # into the game's units
        if ob.type == 'MESH':
            ob.data.transform(Matrix.Scale(SCALE, 4))
        ob.location = ob.location * SCALE
    bpy.context.scene["dungeon_half"] = HALF * SCALE
    # the Empties as a list for the game (Blender axes, the game's units): kind,x,y,z -- kind is
    # light_<colour> / fire / reward / hirecamp / potionshop / exit
    with open(os.path.splitext(OUT)[0] + "-points.csv", "w", encoding="utf-8") as f:
        f.write("kind,x,y,z\n")
        for ob in coll.objects:
            p = ob.location
            if ob.type == 'EMPTY':
                kind = ob.name.split("_")[0]
                kind = {"DungeonLight": "light_" + ob.name.split("_")[1], "DungeonFire": "fire",
                        "DungeonReward": "reward"}.get(kind, kind)
            elif ob.name.endswith(("-HireCamp", "-PotionShop")):
                kind = ob.name.rsplit("-", 1)[1].lower()
            else:
                continue
            f.write(f"{kind},{p.x:.3f},{p.y:.3f},{p.z:.3f}\n")
        ex, ey, _ = w(EXIT_AT[0], EXIT_AT[1] - 1.0)                # at the foot of the cave's mouth
        f.write(f"exit,{ex * SCALE:.3f},{ey * SCALE:.3f},0.000\n")
        f.write(f"edge,0.000,{-HALF * SCALE:.3f},0.000\n")          # the south edge: the map's half size
        sx, sy, _ = w(CAMP[0], CAMP[1] + 1.0)
        f.write(f"start,{sx * SCALE:.3f},{sy * SCALE:.3f},0.000\n")
        for kind, pts in (("lever", levers), ("door", doors), ("throne", thrones)):
            for x, y, z in pts:
                f.write(f"{kind},{x * SCALE:.3f},{y * SCALE:.3f},{z * SCALE:.3f}\n")
        for lk, lx, ly in (lair_rows() if POPULATED else ()):
            f.write(f"{lk},{lx:.3f},{ly:.3f},0.000\n")
    kit.file_display_settings()
    bpy.ops.wm.save_mainfile()
    print("SAVED", OUT)


main()
