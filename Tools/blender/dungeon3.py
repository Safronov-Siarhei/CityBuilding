"""Dungeon 3, «Огненные глубины» (the user, 2026-11-04): the fire depths, the deepest level, the
size of level 2 (300 x 300 build units -> ~132 x 132 cells), built as Dungeons 1 and 2 (dungeon2.py
reworked: the passages a signed-distance field, the floor cut into colour zones, the rock rising as
relief, the map's decor kit, props from the building palette) on the APPROVED plan
(dungeon3_plan.py, seed 15, dungeon3-plan.json):

- LAVA RIVERS: one lava surface at WATER_Z over beds that fall under CLIP_Z (no ground: a "water"
  cell in the game, nobody walks); the boss halls hold a lava LAKE (the dragon) and a magma POOL
  (the titan);
- the LAVA TIDE (the user's «меняющийся путь»): the tide passages and the lava fords are basalt
  floor at SHALLOW_Z (walkable, dark crust with ember seams); over them lies the TIDE skin, its own
  object (<prefix>-Tide), which the game raises to flood them now and then;
- BRIDGES over the rivers ALONG the passage (never across the river into the rock -- level 2's
  bridges «вели в никуда» because they were laid square to the canal): the deck is the passage's
  own ground; the bridges on a loop of the maze CRUMBLE once crossed -- each such bridge is its own
  object (<prefix>-Bridges-f<i>) and a `bridge_fall` point, for the game to drop;
- halls are LIVING SHAPES; basalt columns, obsidian spires, the dragon's hoard, the titan's pool.

    blender --background --factory-startup --python dungeon3.py -- <out.blend> [preview] [variant=b]

The terrain palette is the FIRE palette (Terrain-Palette-Fire.png next to the blend: the shared
palette's slots recoloured -- water -> lava, grass -> ash, rock -> basalt); the game has to draw
this map with it.
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
# `variant=b|c|d`: other layouts of the plan (dungeon3<v>-plan.json), none yet
VARIANT = next((a.split("=", 1)[1].lower() for a in argv[1:] if a.startswith("variant=")), "")
PREFIX = "Dungeon-3" + VARIANT.upper()
POPULATED = True
# Built in "metres" for a 1.8 m person, then shrunk to the GAME's units as Dungeon 1: the 300 x 300
# build comes out 132 x 132 cells.
SCALE = 0.44

N = 300
HALF = N // 2
WATER_Z = -0.45             # the one LAVA surface, over the deep beds of the rivers, the lake and the pool
SHALLOW_Z = -0.30           # the TIDE floor (basalt crust), dry at low tide, above the lava
TIDE_Z = -0.08              # the tide's lava skin over it (the game raises it at high tide)
CLIP_Z = -0.70              # below this there is no ground: lava, a "water" cell in the game
DEEP_DROP = 1.6             # how far the deep beds fall under CLIP_Z
CANAL_W = 3.6               # a lava river's half width
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

PLAN = json.load(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), f"dungeon3{VARIANT}-plan.json"), encoding="utf-8"))
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
NICHE_KINDS = ("ruin", "vent", "spires", "rubble", "bones", "ruin", "vent", "spires")
NICHES = []                 # (x, y, r, kind, (tx, ty) the passage's direction there)
for i, ((pl, near_hall), wet) in enumerate(zip(CURVES, PLAN["tide"])):
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

# the lava: the tide passages (tide floor), the rivers (lava between the halls), the lava fords
# and bridges where a passage meets a river, the bosses' lake and pool
WET_SEGMENTS = [(pl[k], pl[k + 1]) for (pl, _h), wet in zip(CURVES, PLAN["tide"]) if wet for k in range(len(pl) - 1)]
WET_BUCKET = {}
for k, (pa, pb) in enumerate(WET_SEGMENTS):
    for p in (pa, pb, ((pa[0] + pb[0]) / 2, (pa[1] + pb[1]) / 2)):
        WET_BUCKET.setdefault((int(p[0] // CELL), int(p[1] // CELL)), set()).add(k)
CANAL_SEGMENTS = [(c[k], c[k + 1]) for c in PLAN["lava"] for k in range(len(c) - 1)]
# the passages between halls (a hall's own links run across its open floor): deep water never
# lies across one -- where no bridge spans it, the river is crossed by a lava ford (level 2: a
# corner of the map could not be reached)
EXT_SEGMENTS = [(pl[k], pl[k + 1]) for (pl, _h), (ca, cb) in zip(CURVES, PLAN["links"])
                if not (hall_of(ca) and hall_of(ca) == hall_of(cb)) for k in range(len(pl) - 1)]
EXT_BUCKET = {}
for k, (pa, pb) in enumerate(EXT_SEGMENTS):
    for p in (pa, pb, ((pa[0] + pb[0]) / 2, (pa[1] + pb[1]) / 2)):
        EXT_BUCKET.setdefault((int(p[0] // CELL), int(p[1] // CELL)), set()).add(k)
PASS_CLEAR = 4.2            # no deep water this near a passage's centre line, but under a bridge
FORDS = []                  # (x, y): the lava fords, where a tide passage crosses a river -- set by plan_bridges()
BRIDGES = []                # (x, y, ux, uy, half): the span's middle, its axis (ALONG the passage), half its length
BRIDGE_HW = 1.7
BRIDGE_RAMP = 0.7           # the deck slopes down to the floor over the outer 30% of each half
BRIDGE_DRY = 1.0            # a bridge's ends land this far past the lava's edge (the passage bends hard at a river)
BRIDGE_APART = 10.0         # no two bridges nearer than this (they run along their passages, so they need less room than level 2's)
WET_HALLS = ()              # no hall floor under the tide


def _hall_mid(name):
    x0, y0, w_, h_ = BIG[name]
    return (x0 + w_ / 2) * CELL, (y0 + h_ / 2) * CELL


# the bosses' lava: the dragon's LAKE (a big one, the hoard on its south shore) and the titan's
# magma POOL he rises from -- (x, y, rx, ry), deep (no ground)
DRAGON_LAKE = (*_hall_mid("dragon"), 16.0, 11.0)
TITAN_POOL = (*_hall_mid("titan"), 8.0, 7.0)
CRUMBLE_SHARE = 0.5         # of the bridges on a loop of the maze, this share crumbles once crossed
CRUMBLING = []              # indices into BRIDGES -- set by plan_bridges()
BRIDGE_LINKS = []           # the plan link each bridge belongs to
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


def pass_sd(x, y):
    """Signed distance to the walkable ground: negative inside, positive in the rock."""
    return _open_sd(x, y)


def _open_sd(x, y):
    """The passages, their bulges and niches, the halls' living shapes."""
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
    """Negative where the LAVA is (no ground: nobody walks): the rivers between the halls -- never
    in a hall, nor across a passage but under its bridge -- the dragon's lake and the titan's pool."""
    d = canal_sd(x, y)
    if d < 6:
        for name in BIG:
            d = max(d, 2.5 - _rect_sd(x, y, *hall_rect(name, 0.0)))
        if not any(max(_span(x, y, b, 0.5, 5.0)) < 0 for b in BRIDGES):   # the lava runs open beside a bridge
            d = max(d, PASS_CLEAR - ext_route_dist(x, y))
    lake = _ellipse_sd(x, y, *DRAGON_LAKE, 0.35, 9)
    pool = _ellipse_sd(x, y, *TITAN_POOL, 0.3, 13)
    return min(d, lake, pool)


def wet_sd(x, y):
    """Negative where the floor is TIDE floor: the tide passages and the lava fords (the rivers'
    banks too, but never a bridge's dry passage)."""
    ix, iy = int(x // CELL), int(y // CELL)
    best = 1e9
    for dx in (-1, 0, 1):
        for dy in (-1, 0, 1):
            for k in WET_BUCKET.get((ix + dx, iy + dy), ()):
                (ax, ay), (bx, by) = WET_SEGMENTS[k]
                best = min(best, _seg(x, y, ax, ay, bx, by))
    best -= 4.0 + 1.5 * fbm(x / 6, y / 6, 73)
    best = min(best, canal_sd(x, y) - 1.5)
    for fx, fy in FORDS:
        best = min(best, math.hypot(x - fx, y - fy) - 6.0)
    for b in BRIDGES:                           # a dry passage keeps dry up to its bridge
        best = max(best, -max(_span(x, y, b, 3.0, 2.5)))
    return best


def deck_height(along, half):
    """A bridge's deck: humped over the river, sloping down to the dry floor (z 0) at both ends --
    a deck that ended a step above the bank stopped the hero on level 2."""
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




def _on_loop(k, without):
    """True if the plan's passage k (and every passage in `without`) can go and the camp still
    reaches every hall and node: a crumbled bridge never cuts the heroes off."""
    adj = {}
    for j, (ca, cb) in enumerate(PLAN["links"]):
        if j == k or j in without:
            continue
        a, b = hall_of(ca) or tuple(ca), hall_of(cb) or tuple(cb)
        adj.setdefault(a, []).append(b)
        adj.setdefault(b, []).append(a)
    seen, todo = {"camp"}, ["camp"]
    while todo:
        for n in adj.get(todo.pop(), []):
            if n not in seen:
                seen.add(n)
                todo.append(n)
    every = {hall_of(c) or tuple(c) for l in PLAN["links"] for c in l}
    return every <= seen


def plan_bridges():
    """Where every passage between halls meets a lava river: a dry passage gets a BRIDGE laid ALONG
    the passage (its axis the passage's own direction there, so both ends land on the passage's
    floor -- level 2's square-to-the-canal bridges led «в никуда»), when both ends are clear floor
    and no other bridge is near; a tide passage, or a crossing no bridge fits, is a LAVA FORD (tide
    floor across the river: deep_sd keeps the lava off every passage but under a bridge). Then the
    bridges on a loop crumble, CRUMBLE_SHARE of them, while the rest still join everything."""
    crossings = []
    for li, ((pl, _h), (ca, cb), wet) in enumerate(zip(CURVES, PLAN["links"], PLAN["tide"])):
        if hall_of(ca) and hall_of(ca) == hall_of(cb):
            continue
        for k in range(len(pl) - 1):
            for p, q in CANAL_SEGMENTS:
                hit = _cross(pl[k], pl[k + 1], p, q)
                if hit and not hall_of((int(hit[0] // CELL), int(hit[1] // CELL))):
                    crossings.append((hit, wet, pl, k, li))
    for (x, y), wet, pl, k, li in crossings:
        if wet or any(math.hypot(x - o[0], y - o[1]) < BRIDGE_APART for o in BRIDGES):
            if "bridges_only" in argv and not wet:
                print("NEAR another bridge", round(x), round(y))
            FORDS.append((x, y))
            continue
        a, b = pl[max(0, k - 1)], pl[min(len(pl) - 1, k + 2)]       # the passage's direction here
        ux, uy = b[0] - a[0], b[1] - a[1]
        ul = math.hypot(ux, uy) or 1.0
        ux, uy = ux / ul, uy / ul
        ends = []
        for side in (1, -1):
            t = 1.0
            while t < 16.0:
                ex, ey = x + ux * side * t, y + uy * side * t
                if canal_sd(ex, ey) > BRIDGE_DRY:
                    # the end lands on the passage's floor -- or a little further on, where a bend
                    # brings the floor under the deck's line
                    for extra in range(0, 13):
                        fx, fy = ex + ux * side * extra * 0.25, ey + uy * side * extra * 0.25
                        if pass_sd(fx, fy) < -0.6:
                            ends.append(t + extra * 0.25)
                            break
                    break
                t += 0.25
        if len(ends) < 2:
            if "bridges_only" in argv:
                print("NO BRIDGE at", round(x), round(y), "ends", ends, "dir", round(ux, 2), round(uy, 2),
                      [(sd, round(canal_sd(x + ux * sd * t, y + uy * sd * t), 1), round(pass_sd(x + ux * sd * t, y + uy * sd * t), 1))
                       for sd in (1, -1) for t in (4, 8, 12, 15)])
            FORDS.append((x, y))
            continue
        # each end as long as it needs: the middle moves to where the two halves balance
        shift = (ends[0] - ends[1]) / 2
        BRIDGES.append((x + ux * shift, y + uy * shift, ux, uy, (ends[0] + ends[1]) / 2 + 0.8))
        BRIDGE_LINKS.append(li)
    rnd = random.Random(17)
    gone = set()
    for i in sorted(range(len(BRIDGES)), key=lambda i: rnd.random()):
        li = BRIDGE_LINKS[i]
        if li in gone or len(CRUMBLING) >= math.ceil(len(BRIDGES) * CRUMBLE_SHARE):
            continue
        if _on_loop(li, gone):
            gone.add(li)
            CRUMBLING.append(i)
    CRUMBLING.sort()
    if "bridges_only" in argv:
        print("ON A LOOP ONE BY ONE:", sum(1 for li in BRIDGE_LINKS if _on_loop(li, set())), "of", len(BRIDGES))
    print(f"BRIDGES {len(BRIDGES)} (crumbling {len(CRUMBLING)}) LAVA FORDS {len(FORDS)} (of {len(crossings)} crossings)")


MAX_STEP = 0.55            # metres up per metre across that the game's NavMesh still walks (agentSlope 40, agentClimb 0.3 cells)


def reachability(verts):
    """Every passage node and hall reached on foot from the camp: walks the built floor (ground
    between the lava and the rock, decks and tide floor included) on the 1 m vertex grid."""
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
    # the bosses' halls are lava in the middle: their goal is where the boss stands
    at = {"dragon": HOARD_AT, "titan": TITAN_AT}
    goals = [(c, cell_centre(c)) for c in NODES] + [(name, at.get(name, hall_centre(name))) for name in BIG]
    lost = []
    for name, (x, y) in goals:
        if not any((int(x) + di, int(y) + dj) in seen for di in range(-2, 3) for dj in range(-2, 3)):
            lost.append(name)
    print(f"REACH {len(goals) - len(lost)}/{len(goals)} unreachable: {lost}")
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
    if dp < 1.0 and BRIDGES:
        # a GORGE beside every bridge: the river runs open under it for a few metres either side,
        # so a bridge reads as one (the rock closed over the river right at the deck's edge)
        gorge = min(max(_span(x, y, b, -1.0, 4.0)) for b in BRIDGES)
        rock *= smoothstep(-1.5, 1.0, max(gorge, dp))
    rock += smoothstep(WALL_RISE, WALL_RISE + 5, sd) * 2.2 * fbm(x / 3.5, y / 3.5, 57)   # a broken, craggy top
    if x < 1 or y < 1 or x > N - 1 or y > N - 1:
        rock = max(rock, WALL_H)
    return base + rock, dp


# -- colour zones (as map5: fields negative inside, cut along their zero lines) --------------

FIELD_NAMES = ("wet", "hall", "moss", "path", "lush", "stony", "dark", "alt")


def fields_at(x, y, lk, z):
    """The fire depths underfoot: dark crust with ember seams on the tide floor, a glowing rim at
    the lava, obsidian in the halls, ash and basalt down the passages."""
    sd = pass_sd(x, y)
    d, hw = route_dist(x, y)
    hall = hall_sd(x, y)
    return (
        z - (SHALLOW_Z + 0.08),                                       # the tide floor and below
        hall + 1.0 - 2.0 * fbm(x / 6, y / 6, 101),                    # the halls' obsidian
        -sd - (0.5 + 0.7 * fbm(x / 4, y / 4, 103)),                   # dark rock at the walls' foot
        d - (0.9 + 0.5 * fbm(x / 5, y / 5, 105)),                     # the trodden way down a passage
        0.68 - fbm(x / 9, y / 9, 107),                                # ash drifts
        0.62 - fbm(x / 7, y / 7, 109),                                # rougher basalt
        0.6 - fbm(x / 12, y / 12, 111),                               # scorched earth
        0.55 - fbm(x / 3.2, y / 3.2, 119),                            # a second shade; ember seams
    )


def zone_colour(f):
    wet, hall, moss, path, lush, stony, dark, alt = f
    if wet < 0:
        return "mud" if alt > 0.28 else "clay"                        # crust, a seam glowing through
    if moss < 0:
        return "rock_dark"
    if hall < 0:
        return "rock_deep" if alt > 0 else "rock_dark"
    if path < 0:
        return "pebble" if alt > 0 else "stony"
    if lush < 0:
        return "dirt" if alt < 0 else "dirt_dark"
    if dark < 0:
        return "dirt_dark"
    if stony < 0:
        return "rock" if alt < 0 else "rock_light"
    return "pebble" if alt < 0 else "stony"


def relief_colour(z, nz, x, y):
    """Basalt banded up the walls, darker at the foot, ash on the tops with ember cracks."""
    if z < 0.6:
        return "rock_dark" if nz > 0.6 else "rock_mid"
    if z > WALL_H + 0.3 and nz > 0.8:
        if fbm(x / 6, y / 6, 123) > 0.68:
            return "mud"
        return "dirt" if fbm(x / 9, y / 9, 125) > 0.45 else "rock_mid"
    band = int((z + 1.5 * fbm(x / 7, y / 7, 117)) / 0.8) % 3
    return ["rock_mid", "rock_dark", "rock"][band]


# the FIRE palette: the shared terrain palette's slots recoloured, so the layout (and every UV) stays
FIRE_PALETTE = {
    "water_shallow": "FFB43C", "water": "FF7A1E", "water_deep": "E2491A", "foam": "FFE27A",   # lava
    "clay": "4E3530", "mud": "E4612C",                                    # tide crust, ember seams
    "rock_light": "A89D96", "rock": "8E827C", "rock_mid": "766B67", "rock_dark": "5D5452",
    "rock_deep": "463E52",                                                # obsidian
    "pebble": "B3A49A", "stony": "9C8D84", "dirt": "6E5A50", "dirt_dark": "56463F",
    "grass": "8E5E48", "grass_mid": "7A4E3E", "grass_dark": "6A4236", "grass_light": "A0705A",   # ash and scorched turf
    "grass_b": "915F4A", "grass_mid_b": "7D503F", "grass_dark_b": "6C4438", "grass_light_b": "A3725B",
    "moss": "8A4A34", "leaf": "6A4236", "leaf_light": "8E5E48",
    "mushroom_cap": "FF8A2A", "mushroom_stem": "F2D9B0",                 # fire caps
}


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


FALL_DECKS = {}             # bridge index -> the crumbling bridge's deck (terrain), its own object


def falls_at(x, y):
    """The index of the crumbling bridge whose span (x, y) is on, else None."""
    for i in CRUMBLING:
        bx, by, ux, uy, half = BRIDGES[i]
        if abs((x - bx) * ux + (y - by) * uy) < half and abs(-(x - bx) * uy + (y - by) * ux) < BRIDGE_HW:
            return i
    return None


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
                    elif falls_at(c[0] + HALF, c[1] + HALF) is not None:
                        FALL_DECKS.setdefault(falls_at(c[0] + HALF, c[1] + HALF), TerrainBuilder()).poly(
                            [p[:3] for p in piece], zone_colour(c[4:]))
                    else:
                        ground.setdefault(key, TerrainBuilder()).poly([p[:3] for p in piece], zone_colour(c[4:]))
    return ground, relief


def build_water(verts):
    """One LAVA surface at WATER_Z wherever the floor is under it: a bright rim, orange, deep red
    over the beds (the palette's water slots, recoloured by FIRE_PALETTE)."""
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


def build_tide(verts):
    """The tide's lava skin at TIDE_Z over the tide floor (the tide passages and the fords): its
    own object, which the game raises over the floor at high tide."""
    b = TerrainBuilder()
    for i in range(N):
        for j in range(N):
            corners = [verts[i, j], verts[i + 1, j], verts[i + 1, j + 1], verts[i, j + 1]]
            zs = [c[2] for c in corners]
            if max(zs) >= TIDE_Z or min(zs) < WATER_Z:
                continue
            if wet_sd(corners[0][0] + HALF, corners[0][1] + HALF) > 0.5:
                continue
            pts = [(c[0], c[1], TIDE_Z) for c in corners]
            col = "water_shallow" if fbm(corners[0][0] / 5, corners[0][1] / 5, 161) > 0.5 else "water"
            b.poly([pts[0], pts[1], pts[2]], col)
            b.poly([pts[0], pts[2], pts[3]], col)
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
def clear_spot(want, radius, away, gap):
    """The dry floor nearest `want` where a building of `radius` stands clear of the rock (its
    whole round inside the passages by a margin) and `gap` from every point in `away`. Level 1's
    "east of the campfire" put the shop INTO the rock here: this camp hall is narrower on the east
    (the user's screenshot, 2026-11-04: «почему в подземелье 2-го уровня нет лавки?»)."""
    best, best_d = want, 1e9
    for dx in range(-24, 25):
        for dy in range(-24, 25):
            x, y = want[0] + dx, want[1] + dy
            d = math.hypot(dx, dy)
            if d >= best_d or any(math.hypot(x - ax, y - ay) < gap for ax, ay in away):
                continue
            ok = pass_sd(x, y) < -1.0 and wet_sd(x, y) > 1.5
            for k in range(12):
                if not ok:
                    break
                a = k * math.pi / 6
                px, py = x + math.cos(a) * radius, y + math.sin(a) * radius
                ok = pass_sd(px, py) < -0.8 and wet_sd(px, py) > 1.5
            if ok:
                best, best_d = (float(x), float(y)), d
    return best


SHOP_AT = clear_spot((CAMP[0] + 14.0, CAMP[1] + 3.0), 4.5, [HIRE_AT, CAMP[:2]], 11.0)
print("SHOP_AT", SHOP_AT, "camp", CAMP[:2], "hire", HIRE_AT)
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
                if bridge_at(x, y) is not None or z < WATER_Z + 0.05:
                    continue                                   # lava: nothing grows
                if z < SHALLOW_Z + 0.05:                       # the tide floor: bare crust
                    continue
                if z > 0.05:                                   # the rock: crags, a scorched tuft
                    if z < 1.2:
                        if r < 0.08:
                            add("pebbles", x, y, rnd.uniform(0.9, 1.5))
                        elif r < 0.16:
                            add("tuft", x, y, rnd.uniform(0.9, 1.3))
                    elif r < 0.05:
                        add("crag", x, y, rnd.uniform(0.6, 1.3))
                    continue
                if math.hypot(x - CAMP[0], y - CAMP[1]) < CAMP[2] or \
                        any(math.hypot(x - cx, y - cy) < r for cx, cy, r in CAMP_SPOTS):
                    continue
                d, hw = route_dist(x, y)
                if d < 0.8:                                    # the path itself stays clear
                    if r < 0.02:
                        add("pebbles", x, y)
                    continue
                if sd > -1.8 and r < 0.16:                     # along the foot of the rock
                    add("pebbles" if r < 0.08 else "crag" if r < 0.1 else "mushrooms", x, y, rnd.uniform(0.6, 1.2))
                elif r < 0.08 * fbm(x / 3, y / 3, 139) * 1.6:
                    add("tuft", x, y, rnd.uniform(0.8, 1.2))
                elif r < 0.1:
                    add("pebbles", x, y, rnd.uniform(0.9, 1.4))
    return items


def decor_variant(kind, x, y, count):
    return int(_hash(int(x * 7), int(y * 7), 151) * count) % count


# a stone's half-width at scale 1, metres (terrain.py crag: the Stone7-10 layouts x1.5)
CRAG_FOOT_M = {"crag": 0.8, "pebbles": 0.25}


def decor_z(kind, x, y, sc=1.0):
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


def crystal(b, x, y, big=1.0, colour='roof_purple', glow='crystal'):
    cx, cy, _ = w(x, y)
    for ox, oy, h, r, lean in ((0, 0, 0.8, 0.15, 0.0), (0.2, 0.08, 0.55, 0.11, 0.12), (-0.17, 0.12, 0.45, 0.1, -0.1),
                               (0.05, -0.18, 0.35, 0.08, 0.05)):
        h, r = h * big, r * big
        b.frustum(cx + ox * big, cy + oy * big, 0.0, h * 0.7, r, r * 0.8, colour, sides=6, cap=False)
        b.pyramid(cx + ox * big + lean * big, cy + oy * big, h * 0.7, r * 0.8, r * 0.8, h, colour, sides=6)
    b.crag(cx, cy, 0.35 * big, 0.3 * big, 0.12 * big, STONE_D, sides=6, seed=int(x))
    light(glow, cx, cy, 0.6 * big)


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


rewards = []


# -- the halls' furniture (v5, the user: «the rooms are too empty») -------------------------------

def column(b, x, y, h, broken=False):
    """A basalt column: a square base, a hexagonal shaft, a capital -- or a jagged stump."""
    cx, cy, _ = w(x, y)
    b.box(cx - 0.55, cx + 0.55, cy - 0.55, cy + 0.55, -0.1, 0.35, STONE_D, bevel=0.04)
    b.cylinder(cx, cy, 0.35, h, 0.38, STONE, sides=6)
    if broken:
        b.pyramid(cx, cy, h - 0.02, 0.36, 0.36, h + 0.45, STONE_L, sides=5, rot_deg=x * 17)
    else:
        b.box(cx - 0.5, cx + 0.5, cy - 0.5, cy + 0.5, h, h + 0.3, STONE_D, bevel=0.04)


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


def crate(b, x, y, size=0.6, z=0.0):
    cx, cy, _ = w(x, y)
    b.zbox(cx, cy, z, z + size, size / 2, size / 2, x * 37, WOOD_L)
    b.zbox(cx, cy, z + size * 0.42, z + size * 0.58, size / 2 + 0.02, size / 2 + 0.02, x * 37, WOOD_D)


def pedestal(b, x, y):
    """Where a floating reward hangs: stepped stones, a glow to draw the eye."""
    cx, cy, _ = w(x, y)
    b.cylinder(cx, cy, -0.1, 0.2, 1.1, STONE_D, sides=8)
    b.cylinder(cx, cy, 0.2, 0.5, 0.7, STONE_L, sides=8)
    for k in range(4):
        a = k * math.pi / 2 + 0.4
        b.crag(cx + math.cos(a) * 1.6, cy + math.sin(a) * 1.6, 0.25, 0.2, 0.9, STONE_L, sides=5, seed=k)
    light('crystal', cx, cy, 0.9)


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

def brazier(b, x, y):
    cx, cy, _ = w(x, y)
    b.frustum(cx, cy, 0.0, 0.9, 0.22, 0.14, 'iron', sides=6)
    b.frustum(cx, cy, 0.9, 1.15, 0.25, 0.45, 'iron', sides=8)
    fire_spot(cx, cy, 1.25)








# -- the fire depths' own pieces --------------------------------------------------------------------

OBSIDIAN, OBSIDIAN_L = 'roof_purple_dark', 'roof_purple'
falling = {}                # bridge index -> Builder: a crumbling bridge's stones, its own object


def basalt_bridge(b, x, y, ux, uy, half):
    """A basalt bridge over a lava river, laid ALONG its passage: a humped deck of dark slabs (the
    walkable deck is the ground's, height_at -> bridge_at; this sits a hair above it), low
    parapets, piers standing in the lava, a brazier at each end."""
    deg = math.degrees(math.atan2(uy, ux))
    n = int(2 * half / 0.8)
    for k in range(n + 1):
        t = -half + k * 2 * half / n
        px, py = x + ux * t, y + uy * t
        z = deck_height(t, half)
        cx, cy, _ = w(px, py)
        b.zbox(cx, cy, z - 0.35, z + 0.04, 0.46, BRIDGE_HW + 0.05, deg, STONE_D if k % 2 else STONE)
        for side in (-1, 1):
            qx, qy, _ = w(px - uy * side * (BRIDGE_HW + 0.12), py + ux * side * (BRIDGE_HW + 0.12))
            b.zbox(qx, qy, z, z + 0.5, 0.46, 0.14, deg, STONE_D if k % 2 else 'window')
    t = -half
    while t < half:                                          # piers wherever the deck is over lava
        px, py = x + ux * t, y + uy * t
        if canal_sd(px, py) < -0.5:
            cx, cy, _ = w(px, py)
            b.zbox(cx, cy, -2.4, deck_height(t, half) - 0.3, 0.45, BRIDGE_HW + 0.2, deg, STONE_D)
            t += 3.0
        t += 0.5
    for side in (-1, 1):
        px, py = x + ux * side * half, y + uy * side * half
        qx, qy, _ = w(px - uy * (BRIDGE_HW + 0.12), py + ux * (BRIDGE_HW + 0.12))
        b.frustum(qx, qy, 0.0, 0.9, 0.2, 0.13, 'iron', sides=6)
        b.frustum(qx, qy, 0.9, 1.1, 0.22, 0.38, 'iron', sides=8)
        fire_spot(qx, qy, 1.2)


def crack_marks(b, x, y, ux, uy, half):
    """The crumbling bridge's warning: red-glowing cracks across its middle slabs."""
    for t in (-half * 0.3, 0.0, half * 0.25):
        px, py = x + ux * t, y + uy * t
        z = deck_height(t, half)
        cx, cy, _ = w(px, py)
        b.zbox(cx, cy, z + 0.04, z + 0.06, 0.07, BRIDGE_HW * 0.8, math.degrees(math.atan2(uy, ux)) + 12, 'fire')


def lava_ford(b, x, y):
    """Where a tide passage crosses a river: crust slabs over the crossing and two warning posts
    with a red rag -- the way is open only at low tide."""
    r = random.Random(int(x * 13 + y))
    for k in range(6):
        a = r.uniform(0, 6.28)
        cx, cy, _ = w(x + math.cos(a) * r.uniform(0.8, 3.2), y + math.sin(a) * r.uniform(0.8, 3.2))
        b.cylinder(cx, cy, SHALLOW_Z - 0.1, SHALLOW_Z + 0.06, r.uniform(0.4, 0.7), STONE_D, sides=6, rot_deg=r.uniform(0, 60))
    for side in (-1, 1):
        cx, cy, _ = w(x + side * 4.2, y + side * 1.4)
        b.rod((cx, cy, SHALLOW_Z), (cx + 0.1, cy, 1.5), 0.12, STONE_D)
        b.obox((cx + 0.25, cy, 1.2), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.22, 0.02, 0.3, 'banner_red')


def spire(b, x, y, h):
    """An obsidian spire: a black glassy shard with a purple sheen, a few smaller ones at its foot."""
    cx, cy, _ = w(x, y)
    r = h * 0.22
    b.frustum(cx, cy, -0.4, h * 0.45, r, r * 0.6, OBSIDIAN, sides=5, rot_deg=x * 23)
    b.pyramid(cx, cy, h * 0.45, r * 0.6, r * 0.6, h, OBSIDIAN_L, sides=5, rot_deg=x * 23)
    for k in range(3):
        a = k * 2.1 + y
        sx, sy = cx + math.cos(a) * r * 1.6, cy + math.sin(a) * r * 1.6
        b.pyramid(sx, sy, -0.2, r * 0.35, r * 0.35, h * 0.35, OBSIDIAN, sides=4, rot_deg=a * 40)


def spires(b, x, y, rnd):
    for k in range(3 + rnd.randrange(3)):
        a = rnd.random() * 6.283
        d = 0.4 + rnd.random() * 1.6
        spire(b, x + math.cos(a) * d, y + math.sin(a) * d, rnd.uniform(1.2, 3.2))


def vent(b, x, y, big=1.0):
    """A fire vent: a ring of scorched rock round a glowing mouth, a flame over it."""
    cx, cy, _ = w(x, y)
    for k in range(7):
        a = k * 2 * math.pi / 7
        b.crag(cx + math.cos(a) * 0.8 * big, cy + math.sin(a) * 0.8 * big, 0.35 * big, 0.3 * big, 0.45 * big,
               STONE_D, top='window', sides=5, seed=k + int(x))
    b.cylinder(cx, cy, -0.05, 0.12, 0.6 * big, 'fire', sides=8)
    fire_spot(cx, cy, 0.5 * big)
    light('fire', cx, cy, 0.8 * big)


def gold_pile(b, x, y, s=1.0):
    cx, cy, _ = w(x, y)
    b.ball(cx, cy, -0.2, 0.9 * s, 0.75 * s, 'gold', sides=8, top='gold')
    for k in range(5):
        a = k * 1.3 + x
        b.ball(cx + math.cos(a) * 0.9 * s, cy + math.sin(a) * 0.9 * s, -0.1, 0.35 * s, 0.35 * s, 'gold', sides=6)


def hoard(b, x, y, rnd):
    """The dragon's bed on the lake's shore: heaps of gold, a few chests, a crown, bones."""
    gold_pile(b, x, y, 1.8)
    for k in range(5):
        a = k * 1.25 + 0.4
        gold_pile(b, x + math.cos(a) * 3.0, y + math.sin(a) * 2.2, rnd.uniform(0.6, 1.1))
    for k in range(3):
        a = k * 2.0 + 1.0
        crate(b, x + math.cos(a) * 4.2, y + math.sin(a) * 3.0, 0.7)
    cx, cy, _ = w(x + 0.6, y - 0.4)
    b.cylinder(cx, cy, 1.25, 1.45, 0.3, 'gold', sides=8)
    for k in range(4):
        bones(b, x + rnd.uniform(-5, 5), y + rnd.uniform(-4, 3))
    light('fire', *w(x, y)[:2], 1.5)


def place_on_shore(cx, cy, ry, dy=-1):
    """The floor just off a lava lake's shore, straight north/south of its middle: clear floor."""
    y = cy + dy * (ry + 2.0)
    for _ in range(80):
        if pass_sd(cx, y) < -2.5 and deep_sd(cx, y) > 2.5:
            return cx, y
        y += dy * 0.5
    return cx, cy + dy * (ry + 4.0)


HOARD_AT = place_on_shore(DRAGON_LAKE[0], DRAGON_LAKE[1], DRAGON_LAKE[3])
TITAN_AT = place_on_shore(TITAN_POOL[0], TITAN_POOL[1], TITAN_POOL[3])
print("HOARD_AT", HOARD_AT, "TITAN_AT", TITAN_AT)


def build_halls(b, r):
    # the obsidian hall: spires in clusters, two rows of basalt columns, some snapped
    x0, y0, x1, y1 = hall_rect("great", 5.0)
    for row in (0.25, 0.75):
        for k in range(5):
            x, y = x0 + (x1 - x0) * (k + 0.5) / 5 + r.uniform(-1, 1), y0 + (y1 - y0) * row
            if pass_sd(x, y) < -1.0 and deep_sd(x, y) > 2.0 and route_dist(x, y)[0] > 1.5:
                with on_floor(b, x, y):
                    column(b, x, y, r.uniform(3.5, 5.0), r.random() < 0.35)
    box = hall_rect("great", 3.0)
    scatter(r, box, 5, lambda x, y: spires(b, x, y, r), -2.0, b)
    scatter(r, box, 2, lambda x, y: fallen_column(b, x, y, r.uniform(0, 3.1), r.uniform(3.0, 4.5)), -2.0, b)
    # the dragon's hall: spires round the lake, the hoard on its south shore
    lx, ly, rx, ry = DRAGON_LAKE
    for k in range(12):
        a = k * 2 * math.pi / 12 + 0.2
        x, y = lx + math.cos(a) * (rx + 3.0), ly + math.sin(a) * (ry + 3.0)
        if pass_sd(x, y) < -1.0 and deep_sd(x, y) > 1.5 and math.dist((x, y), HOARD_AT) > 7:
            with on_floor(b, x, y):
                spire(b, x, y, r.uniform(2.0, 4.5))
    hoard(b, *HOARD_AT, r)
    scatter(r, hall_rect("dragon", 3.0), 8, lambda x, y: bones(b, x, y), -0.8, b)
    # the titan's hall: broken basalt columns round the magma pool, vents
    px, py, rx, ry = TITAN_POOL
    for k in range(8):
        a = k * 2 * math.pi / 8 + 0.4
        x, y = px + math.cos(a) * (rx + 3.0), py + math.sin(a) * (ry + 3.0)
        if pass_sd(x, y) < -1.0 and deep_sd(x, y) > 1.5 and math.dist((x, y), TITAN_AT) > 4:
            with on_floor(b, x, y):
                column(b, x, y, r.uniform(2.0, 4.0), k % 2 == 0)
    scatter(r, hall_rect("titan", 3.0), 2, lambda x, y: vent(b, x, y, 1.2), -2.0, b)
    scatter(r, hall_rect("titan", 3.0), 5, lambda x, y: bones(b, x, y), -0.8, b)
    # the craters: a big vent in the middle, rock round it, the monsters' totems
    for name in ("crater_w", "crater_e"):
        cx, cy = hall_centre(name)
        if pass_sd(cx, cy) > -4:
            cx, cy = free_spot(r, *hall_rect(name, 4.0), clear=-4.0) or (cx, cy)
        with on_floor(b, cx, cy):
            vent(b, cx, cy, 2.0)
        box = hall_rect(name, 3.0)
        scatter(r, box, 4, lambda x, y: boulder(b, x, y, r.uniform(0.5, 1.0)), -1.2, b)
        scatter(r, box, 2, lambda x, y: totem(b, x, y), -1.5, b)
        scatter(r, box, 3, lambda x, y: bones(b, x, y), -0.8, b)
    # the monster junctions: a totem each; the crystal rooms: a pedestal, red crystals round it
    for c in PLAN["monster_halls"]:
        cx, cy = cell_centre(c)
        with on_floor(b, cx + 2.0, cy + 1.5):
            totem(b, cx + 2.0, cy + 1.5)
    for i, c in enumerate(PLAN["crystal_rooms"]):
        cx, cy = cell_centre(c)
        with on_floor(b, cx, cy):
            pedestal(b, cx, cy)
            crystal(b, cx + 2.6, cy - 1.4, 1.2, 'roof_red', 'fire')
            crystal(b, cx - 2.4, cy + 1.6, 0.9, 'roof_orange', 'fire')


def build_props(b):
    build_camp(b)
    rnd = random.Random(5)
    # the niches: each has something in it, so no two stretches of passage look alike
    for i, (x, y, r, kind, (tx, ty)) in enumerate(NICHES):
        with on_floor(b, x, y):
            if kind == "spires":
                spires(b, x, y, rnd)
            elif kind == "ruin":
                column(b, x, y, rnd.uniform(2.5, 4.0), rnd.random() < 0.6)
                fallen_column(b, x + tx * 1.8, y + ty * 1.8, math.atan2(ty, tx) + 0.6, 2.4)
            elif kind == "vent":
                vent(b, x, y, rnd.uniform(0.8, 1.2))
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
    # the rivers: bridges for the dry passages (the crumbling ones their own objects), slabs at the fords
    for i, (x, y, ux, uy, half) in enumerate(BRIDGES):
        if i in CRUMBLING:
            falling[i] = Builder()
            basalt_bridge(falling[i], x, y, ux, uy, half)
            crack_marks(falling[i], x, y, ux, uy, half)
        else:
            basalt_bridge(b, x, y, ux, uy, half)
    for x, y in FORDS:
        lava_ford(b, x, y)
    # the rivers' glow: a fire light every ~25 m along them
    for canal in PLAN["lava"]:
        acc = 12.0
        for (ax, ay), (bx, by) in zip(canal, canal[1:]):
            ln = math.hypot(bx - ax, by - ay)
            t = 0.0
            while t < ln:
                if acc >= 25.0:
                    x, y = ax + (bx - ax) * t / ln, ay + (by - ay) * t / ln
                    if 0 < x < N and 0 < y < N:
                        light('fire', *w(x, y)[:2], 0.6)
                    acc = 0.0
                step = min(1.0, ln - t)
                t += step
                acc += step
    # spires and stalagmites in the halls
    for name in BIG:
        if name == "camp":
            continue
        box = hall_rect(name, 3.0)
        for _ in range(2):
            spot = free_spot(rnd, *box)
            if spot:
                with on_floor(b, *spot):
                    spires(b, spot[0], spot[1], rnd)
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
    # the crystal rooms: nothing on the ground -- the reward floats in the air
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
    junction, the obsidian hall and the craters; the dragon on his hoard, the titan at his pool;
    lone crystals in the farthest empty dead ends, chests in the rest; patrols along the passages."""
    import dungeon_lairs

    def game(x, y):
        return (x - HALF) * SCALE, (y - HALF) * SCALE
    out = [("lair",) + game(*cell_centre(c)) for c in PLAN["monster_halls"]]
    out += [("lair",) + game(*hall_centre(name)) for name in ("great", "crater_w", "crater_e")]
    out.append(("boss_dragon",) + game(*HOARD_AT))
    out.append(("boss_titan",) + game(*TITAN_AT))
    camp = game(*hall_centre("camp"))
    # the user, 2026-11-04: «слишком мало противников ... больше мест с 1 кристаллом» and chests
    # in the dead ends: the farthest empty dead ends take a lone crystal each, the rest a chest;
    # more lone crystals out in the passages (each guarded, so the corridors are not empty), and
    # twice the patrols
    rooms = {tuple(c) for c in PLAN["crystal_rooms"]}
    empty = sorted((tuple(c) for c in PLAN["dead_ends"] if tuple(c) not in rooms),
                   key=lambda c: -math.dist(game(*POS[c]), camp))
    for c in empty[:LONE_CRYSTALS]:
        out.append(("crystal",) + game(*POS[c]))
    for c in empty[LONE_CRYSTALS:]:
        out.append(("chest",) + game(*POS[c]))
    for c in passage_crystals(game, camp, [(x, y) for _, x, y in out]):
        out.append(("crystal",) + game(*POS[c]))
    lairs = [(x, y) for k, x, y in out if k == "lair" or k.startswith("boss_")]
    dungeon_lairs.PATROLS = PATROLS
    for k, route in enumerate(dungeon_lairs.patrols(PLAN, POS, game, camp, lairs)):
        out += [(f"patrol_{k}", x, y) for x, y in route]
    print("POINTS", {k: sum(1 for o in out if o[0] == k) for k in ("lair", "crystal", "chest")},
          "patrols", len({o[0] for o in out if o[0].startswith("patrol_")}))
    return out


LONE_CRYSTALS = 9         # in the farthest empty dead ends; the other dead ends hold chests
PASSAGE_CRYSTALS = 11     # out in the passages, spread apart
PATROLS = 10              # level 1 has 5


def passage_crystals(game, camp, taken):
    """Passage nodes (two links, not in a hall, not a lair or a room), each as far as can be from
    the camp, every spot already placed and the others chosen -- so they spread over the map."""
    adj = {}
    for a, b in PLAN["links"]:
        adj.setdefault(tuple(a), set()).add(tuple(b))
        adj.setdefault(tuple(b), set()).add(tuple(a))
    special = {tuple(c) for c in PLAN["monster_halls"] + PLAN["crystal_rooms"] + PLAN["dead_ends"]}

    def in_hall(c):
        return any(x0 <= c[0] < x0 + w and y0 <= c[1] < y0 + h for x0, y0, w, h in PLAN["big"].values())
    nodes = [c for c, n in adj.items() if len(n) == 2 and c not in special and not in_hall(c)]
    points = [camp] + list(taken)
    chosen = []
    for _ in range(PASSAGE_CRYSTALS):
        best, best_d = None, 0.0
        for c in nodes:
            if c in chosen:
                continue
            d = min(math.dist(game(*POS[c]), p) for p in points)
            if d > best_d:
                best, best_d = c, d
        if best is None:
            break
        chosen.append(best)
        points.append(game(*POS[best]))
    return chosen


def main():
    folder = os.path.dirname(OUT)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.wm.save_as_mainfile(filepath=OUT)
    for k, (name, hx) in enumerate(terrain.TERRAIN_PALETTE):       # the FIRE palette, slot for slot
        terrain.TERRAIN_PALETTE[k] = (name, FIRE_PALETTE.get(name, hx))
    timg = terrain.ensure_palette(os.path.join(folder, "Terrain-Palette-Fire.png"))
    tmat = terrain.ensure_material(timg)
    bimg = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    bmat = kit.ensure_material(bimg)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(bmat, bimg, atlas)

    find_exit()
    plan_bridges()
    if "bridges_only" in argv:
        return
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
    wtr = merge_flat(make_object(f"{PREFIX}-Water", build_water(verts), tmat, coll))     # the lava
    tide = merge_flat(make_object(f"{PREFIX}-Tide", build_tide(verts), tmat, coll))     # the game raises it

    pb = Builder()
    build_props(pb)
    ptris = pb.triangles()
    props = make_object(f"{PREFIX}-Props", pb, bmat, coll)
    detail.add_layers(props.data, kit.NAMES, kit.COLS, kit.ROWS)
    rough_exit_rock(props.data)
    pchunks = split_by_chunk(props, coll)
    make_object(f"{PREFIX}-ExitGlow", exit_glow, bmat, coll)   # the way out's daylight, drawn unlit
    for i, deck in sorted(FALL_DECKS.items()):                # the crumbling decks, with their stones
        merge_flat(make_object(f"{PREFIX}-Bridges-f{i}-deck", deck, tmat, coll))
    for i, fb in sorted(falling.items()):                     # the crumbling bridges, each its own object
        ob = make_object(f"{PREFIX}-Bridges-f{i}", fb, bmat, coll)
        detail.add_layers(ob.data, kit.NAMES, kit.COLS, kit.ROWS)
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
    print(f"DUNGEON ground tris={g} relief tris={r} lava tris={wtr} tide tris={tide} props tris={ptris} "
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
        for i in CRUMBLING:                                    # bridge_fall: the middle of each crumbling bridge
            bx, by, _ux, _uy, _half = BRIDGES[i]
            px, py, _ = w(bx, by)
            f.write(f"bridge_fall,{px * SCALE:.3f},{py * SCALE:.3f},{deck_height(0.0, _half) * SCALE:.3f}\n")
        for lk, lx, ly in (lair_rows() if POPULATED else ()):
            f.write(f"{lk},{lx:.3f},{ly:.3f},0.000\n")
    kit.file_display_settings()
    bpy.ops.wm.save_mainfile()
    print("SAVED", OUT)


main()
