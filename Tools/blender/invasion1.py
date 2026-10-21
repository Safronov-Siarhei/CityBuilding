"""Invasion map 1 -- the first map of the Invasion mode (the user's brief, 2026-10-21).

    blender --background --factory-startup --python invasion1.py -- <out.blend> [preview]

Built on Map 3's machinery (the faceted surface, the colour zones, the water bands, the decor) with
a layout like a Dota map: the player's base on an island in the middle, three lanes over three
causeways to three round orc camps, and between the lanes three jungles for the hero -- paths, monster
camps, a ridge with a gorge, a stream with a ford. See the layout section.

`preview` also stands the real trees, boulders and a small town on it (a separate collection the
game never reads) for the review pictures.
"""
import math
import os
import random
import sys

import bmesh
import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
import terrain  # noqa: E402
from terrain import TerrainBuilder  # noqa: E402

argv = sys.argv[sys.argv.index("--") + 1:]
OUT = argv[0]
PREVIEW = "preview" in argv[1:]
MODELS = os.path.join(os.path.dirname(OUT), "Buildings-Size1-Models.blend")
PREFIX = "Inv-1"

N = 200
HALF = N // 2
WATER_Z = -0.5
CLIP_Z = -0.47          # the ground stops here; below it a cell has no ground and is water
BEACH = 2.6             # metres of beach slope above the waterline
BEACH_DROP = 0.40       # how far the beach falls to meet the water
RELIEF_MIN = 0.06       # relief lower than this is flattened into the plain

# -- noise -------------------------------------------------------------------------------------


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


# -- the layout --------------------------------------------------------------------------------
#
# North (+y) away from the camera. A round island with the sea all round. The player's base is a
# small island in the middle behind a ring of river; three earth causeways lead off it, and from each a
# lane runs out to a round orc camp near the coast (north, south-west, south-east) -- the waves
# come down the lanes. Between the lanes lie three jungles of thick forest with paths through them
# for the hero, monster camps on the paths, a healing shrine in each. A mountain ridge crosses
# every jungle and splits it into a near half (easy monsters) and a far half (hard ones), with a
# gorge through the ridge's middle; a stream runs from a lake under each ridge to the sea, crossed
# by a ford. The lanes reach their camps between low cliffs. A boss lair in the south.

def _polar(ang, r):
    return (math.cos(ang) * r, math.sin(ang) * r)


RAD = math.radians
COAST_R = 94.0                                 # the island's radius
RING_R, RING_HALF = 34.0, 4.0                  # the river round the base: centre line, half width
BASE_R = RING_R - RING_HALF                    # the base island's radius
LANE_ANGLES = (90.0, 210.0, 330.0)             # bearings of the three lanes (and causeways)
JUNGLE_ANGLES = (30.0, 150.0, 270.0)           # bearings of the three jungles between them
CAMP_R = 70.0                                  # how far out the orc camps stand
CAMP_PAD = 17.0                                # a camp's round flat ground (the palisade is at ~15)
LANE_CLEAR = 7.5                               # half width of a lane kept free of forest
RIDGE_R, RIDGE_GAP, RIDGE_END = 59.0, 0.12, 0.62   # the jungle ridge: radius, half gap, half span (rad)
RIDGE_HALF = 6.0                               # the ridge's half width at its foot
START = (0.0, 0.0)                             # the player's base: kept flat, open and dry

CAMPS = [(*_polar(RAD(a), CAMP_R), 3) for a in LANE_ANGLES]
CAMP_HALF = {1: 6, 2: 7, 3: 11}
START_ZONE = [(-8, -8), (8, -8), (8, 8), (-8, 8)]
BRIDGES = [_polar(RAD(a), RING_R) for a in LANE_ANGLES]   # where each lane crosses the river (a causeway; spots kind "bridge")

# the hero's paths through each jungle: the near loop, the gorge, the far loop
PATHS = []
MONSTERS = []                                  # (x, y, level): a monster camp's clearing
SHRINES = []
PONDS = []
STREAM_COURSES = []
FORDS = []
for _a in JUNGLE_ANGLES:
    a = RAD(_a)
    PATHS.append([_polar(a + t, 45 + 2.5 * math.cos(t * 9)) for t in [k * 0.1 - 0.8 for k in range(17)]])
    PATHS.append([_polar(a, 45), _polar(a + 0.03, 52), _polar(a - 0.02, 59), _polar(a + 0.02, 67)])
    PATHS.append([_polar(a + t, 74 + 3.0 * math.sin(t * 7 + _a)) for t in [k * 0.1 - 0.75 for k in range(16)]])
    MONSTERS += [(*_polar(a - 0.32, 45.5), 1), (*_polar(a + 0.32, 45.5), 1),
                 (*_polar(a - 0.66, 47.5), 2), (*_polar(a + 0.66, 47.5), 2),
                 (*_polar(a + 0.42, 72.5), 2), (*_polar(a - 0.5, 75.5), 3), (*_polar(a + 0.2, 79.0), 3)]
    SHRINES.append(_polar(a + 0.03, 68.5))
    pond = _polar(a - 0.27, 65.5)
    PONDS.append((*pond, 4.5, 3.5))
    ford = _polar(a - 0.27, 75.0)
    FORDS.append(ford)
    STREAM_COURSES.append([pond, _polar(a - 0.31, 70), ford, _polar(a - 0.33, 84), _polar(a - 0.3, 99)])
BOSS = _polar(RAD(270.0), 84.0)
MONSTER_CLEAR = {1: 3.5, 2: 4.0, 3: 5.0}
FORD_R = 4.0
LAKE = (900.0, 900.0, 1.0, 1.0)                # (Map 4's lake; none here -- the ponds are the still water)
RUIN = BOSS
BEACHES = []
HILLS = [(*_polar(RAD(a + 50), 14), 3.5, 1.4) for a in JUNGLE_ANGLES]
GROVES = [(*_polar(RAD(a), 20), 6.0) for a in JUNGLE_ANGLES]
OUTCROPS = [(*_polar(RAD(a), 18), 4) for a in (0.0, 120.0, 240.0)] + \
           [(*_polar(RAD(a + 20), 53), 3) for a in JUNGLE_ANGLES] + \
           [(*_polar(RAD(a - 18), 82), 4) for a in JUNGLE_ANGLES]


def _lengths(pts):
    out = [0.0]
    for i in range(1, len(pts)):
        out.append(out[-1] + math.dist(pts[i - 1], pts[i]))
    return out


def _point_at(pts, lens, s):
    for i in range(len(pts) - 1):
        if s <= lens[i + 1] or i == len(pts) - 2:
            seg = lens[i + 1] - lens[i]
            t = 0.0 if seg == 0 else max(0.0, min(1.0, (s - lens[i]) / seg))
            (ax, ay), (bx, by) = pts[i], pts[i + 1]
            return (ax + (bx - ax) * t, ay + (by - ay) * t), ((bx - ax) / seg, (by - ay) / seg)


def _meander(pts, amp, wave, seed):
    lens = _lengths(pts)
    total = lens[-1]
    out = []
    s = 0.0
    while s < total + 1.0:
        s = min(s, total)
        (x, y), (dx, dy) = _point_at(pts, lens, s)
        taper = smoothstep(0.0, 8.0, s) * smoothstep(total, total - 10.0, s)
        off = amp * taper * (math.sin(2 * math.pi * s / wave + seed) +
                             0.45 * math.sin(2 * math.pi * s / (wave * 0.41) + seed * 2.3))
        out.append((x - dy * off, y + dx * off))
        if s >= total:
            break
        s += 2.0
    return out


RIVERS = tuple(_meander(c, 1.2, 18.0, k * 1.7) for k, c in enumerate(STREAM_COURSES))
_RIVER_LENS = [_lengths(r) for r in RIVERS]
FLOWER_SEED, DRY_SEED = 501, 533


def _seg(px, py, ax, ay, bx, by):
    dx, dy = bx - ax, by - ay
    t = max(0.0, min(1.0, ((px - ax) * dx + (py - ay) * dy) / (dx * dx + dy * dy)))
    return math.hypot(px - ax - t * dx, py - ay - t * dy), t


def _poly_dist(px, py, pts):
    return min(_seg(px, py, *pts[i], *pts[i + 1])[0] for i in range(len(pts) - 1))


RIDGES = []
for _a in JUNGLE_ANGLES:
    a = RAD(_a)
    for lo, hi in ((-RIDGE_END, -RIDGE_GAP), (RIDGE_GAP, RIDGE_END)):
        RIDGES.append([_polar(a + lo + (hi - lo) * k / 10, RIDGE_R + 1.5 * math.sin(k * 1.3 + _a)) for k in range(11)])
LANES = [[_polar(RAD(a), BASE_R - 2), _polar(RAD(a), CAMP_R)] for a in LANE_ANGLES]


def lane_dist(x, y):
    return min(_seg(x, y, *l[0], *l[1])[0] for l in LANES)


def path_dist(x, y):
    return min(_poly_dist(x, y, p) for p in PATHS)


def camp_pad(x, y):
    """1 on a camp's round ground and the monster clearings, fading to 0 a few metres out: no relief, no wood."""
    best = 0.0
    for cx, cy, _ in CAMPS:
        best = max(best, smoothstep(CAMP_PAD + 3.0, CAMP_PAD, math.hypot(x - cx, y - cy)))
    for mx, my, lv in MONSTERS:
        best = max(best, smoothstep(MONSTER_CLEAR[lv] + 1.5, MONSTER_CLEAR[lv], math.hypot(x - mx, y - my)))
    best = max(best, smoothstep(11.0, 8.0, math.hypot(x - BOSS[0], y - BOSS[1])))
    for sx, sy in SHRINES:
        best = max(best, smoothstep(5.0, 3.5, math.hypot(x - sx, y - sy)))
    return best


def river(px, py):
    """(distance to the nearest stream's line, how far down it: 0 at its pond, 1 at the sea)."""
    best = (1e9, 0.0)
    for pts, lens in zip(RIVERS, _RIVER_LENS):
        for i in range(len(pts) - 1):
            d, t = _seg(px, py, *pts[i], *pts[i + 1])
            if d < best[0]:
                best = (d, (lens[i] + t * (lens[i + 1] - lens[i])) / lens[-1])
    return best


def _ellipse_sd(x, y, cx, cy, rx, ry, wobble, seed):
    ang = math.atan2((y - cy) / ry, (x - cx) / rx)
    k = 1.0 + wobble * (fbm(math.cos(ang) * 1.3 + seed, math.sin(ang) * 1.3 - seed, seed) - 0.5)
    r = math.hypot((x - cx) / rx, (y - cy) / ry)
    return (r - k) * min(rx, ry)


def coast_sd(x, y):
    """Signed distance from the sea, positive on the island: a circle, its line wobbling."""
    ang = math.atan2(y, x)
    wob = 4.0 * (fbm(math.cos(ang) * 2.2 + 3, math.sin(ang) * 2.2 + 5, 61) - 0.5) + \
        2.0 * (fbm(x / 9, y / 9, 63) - 0.5)
    return COAST_R - math.hypot(x, y) + wob


def beach_gate(x, y):
    return 0.0


def river_width(x, y, t):
    return 1.5 + 1.0 * t + 0.4 * (fbm(x / 9, y / 9, 11) - 0.5)


def ford_at(x, y):
    return max(smoothstep(FORD_R, FORD_R - 1.2, math.hypot(x - fx, y - fy)) for fx, fy in FORDS)


CAUSEWAY_HALF = 3.5                            # half width of the earth causeway across the river


def ring_sd(x, y):
    """Signed distance from the river round the base, positive on land. Where each lane meets the
    river an earth causeway runs across it (the user, 2026-10-21: «просто проход из земли»
    rather than the plank bridges): ground, so everyone walks it and the waves come over it."""
    r = math.hypot(x, y)
    half = RING_HALF + 0.8 * (fbm(x / 8, y / 8, 13) - 0.5)
    sd = abs(r - RING_R) - half
    for bx, by in BRIDGES:
        ux, uy = bx / RING_R, by / RING_R
        across = abs(-x * uy + y * ux)              # off the lane's line
        along = abs(x * ux + y * uy - RING_R)        # off the river's middle
        width = CAUSEWAY_HALF + 0.6 * (fbm(x / 5, y / 5, 17) - 0.5)
        if along < RING_HALF + 3.0:
            sd = max(sd, width - across)
    return sd


def land_sd(x, y):
    sd = min(coast_sd(x, y), ring_sd(x, y))
    d, t = river(x, y)
    rsd = d - river_width(x, y, t)
    f = ford_at(x, y)
    if f > 0:
        rsd = rsd + (0.4 - rsd) * f if rsd < 0.4 else rsd
    sd = min(sd, rsd)
    for pond in PONDS:
        sd = min(sd, _ellipse_sd(x, y, *pond, 0.35, 5))
    return sd


def mountains(x, y):
    """The jungle ridges (a gorge through each one's middle) and the cliffs walling the lanes' ends."""
    m = 0.0
    for k, ridge in enumerate(RIDGES):
        d = _poly_dist(x, y, ridge) + 1.6 * (fbm(x / 6, y / 6, 71 + k) - 0.5)
        if d < RIDGE_HALF:
            peak = 7.0 + 7.0 * fbm(x / 14, y / 14, 77)
            crag = 1.0 - abs(2.0 * fbm(x / 4.5, y / 4.5, 80) - 1.0)
            u = 1.0 - d / RIDGE_HALF
            m = max(m, peak * (u ** 0.8) + 1.4 * crag * smoothstep(0.0, 0.5, u))
    for a in LANE_ANGLES:                      # the lanes run out to their camps between cliffs
        ang = RAD(a)
        ux, uy = math.cos(ang), math.sin(ang)
        along = x * ux + y * uy
        across = abs(-x * uy + y * ux)
        if 42.0 < along < 58.0:
            d = abs(across - (LANE_CLEAR + 2.5))
            if d < 2.5:
                h = (3.5 + 2.5 * fbm(x / 5, y / 5, 85)) * (1.0 - d / 2.5) ** 0.7
                m = max(m, h * smoothstep(42.0, 46.0, along) * smoothstep(58.0, 54.0, along))
    for px, py, _, _ in PONDS:                 # the ponds' hollows
        m *= smoothstep(4.0, 9.0, math.hypot(x - px, y - py))
    return max(0.0, m)


def cliffs(x, y):
    """A low rocky rim along the coast, broken only at the streams' mouths."""
    c = coast_sd(x, y)
    if c < BEACH or c > BEACH + 8:
        return 0.0
    rim = smoothstep(BEACH, BEACH + 2.5, c) * smoothstep(BEACH + 8.0, BEACH + 4.5, c)
    h = (2.2 + 1.8 * fbm(x / 7, y / 7, 87)) * rim
    h *= smoothstep(6.0, 12.0, river(x, y)[0])
    return h


def relief_at(x, y):
    pad = camp_pad(x, y)
    if pad >= 1.0:
        return 0.0
    h = max(mountains(x, y), cliffs(x, y)) * (1.0 - pad)
    for hx, hy, r, height in HILLS:
        dd = math.hypot(x - hx, y - hy) / r
        if dd < 1.0:
            h = max(h, height * (0.5 + 0.5 * math.cos(math.pi * dd)) ** 0.9)
    return max(0.0, h - RELIEF_MIN)


def height_at(x, y):
    sd = land_sd(x, y)
    if sd >= BEACH:
        base = 0.0
    elif sd >= 0:
        base = -BEACH_DROP * (1 - sd / BEACH) ** 1.6
    else:
        base = -BEACH_DROP + sd * 0.6
    rel = relief_at(x, y) * smoothstep(BEACH + 0.5, BEACH + 5.0, sd)
    return base + rel, sd


def forest_field(x, y):
    """Negative inside a forest, positive outside, roughly in metres: the jungles fill everything
    between the lanes but the paths and the clearings; the base island has three groves."""
    r = math.hypot(x, y)
    clear = max(LANE_CLEAR - lane_dist(x, y),             # the lanes
                2.4 - path_dist(x, y),                    # the hero's paths
                RING_R + RING_HALF + 3.0 - r,             # the base island and the river's bank
                r - (COAST_R - 3.0))
    pad = camp_pad(x, y)
    if pad > 0.3:
        clear = max(clear, 6.0 * pad - 1.5)
    jungle = clear + 1.2 * (fbm(x / 6, y / 6, 97) - 0.5)
    grove = min(math.hypot(x - gx, y - gy) - gr * (0.75 + 0.5 * fbm(x / 5, y / 5, 98)) for gx, gy, gr in GROVES)
    return min(jungle, grove)


def in_forest(x, y):
    return forest_field(x, y) < 0


def outcrop(x, y):
    for cx, cy, r in OUTCROPS:
        if math.hypot(x - cx, y - cy) < r * (0.7 + 0.6 * fbm(x / 6, y / 6, 99)):
            return True
    return False


# -- the surface ---------------------------------------------------------------------------------
#
# Colour zones are scalar fields, negative inside the zone, sampled at every grid corner. A
# triangle that a field crosses is cut along the field's zero line (interpolated between its
# corners), so every boundary -- sand to grass, meadow to forest floor, shallow to deep water --
# is a smooth contour, and each piece takes one flat colour. No per-triangle noise: the ground is
# calm, like the buildings, and the life comes from the zones and the props on them.

FIELD_NAMES = ("wet", "sand", "forest", "forest_core", "stony", "lush", "dry", "light", "mid", "alt")


def sand_width(x, y):
    """How far up the bank the sand reaches: a wide strand on a landing beach, a strip under the
    cliffs, a little on the rivers and the lake, none round the ponds. A ford is gravel."""
    for pond in PONDS:
        if _ellipse_sd(x, y, *pond, 0.35, 5) < 6:
            return 0.0
    if ford_at(x, y) > 0:
        return 2.2
    if coast_sd(x, y) < 12:
        return 0.6 + 7.0 * beach_gate(x, y)
    if _ellipse_sd(x, y, *LAKE, 0.45, 7) < 8:
        return 2.0
    return 0.4 + 1.4 * smoothstep(0.4, 0.7, fbm(x / 16, y / 16, 117))


def outcrop_field(x, y):
    best = 1e9
    for cx, cy, r in OUTCROPS:
        best = min(best, math.hypot(x - cx, y - cy) - r * (0.7 + 0.6 * fbm(x / 6, y / 6, 99)))
    return best


def fields_at(x, y, sd):
    town = math.hypot(x - START[0], y - START[1])
    forest = forest_field(x, y)
    meadow = fbm(x / 34, y / 34, 103)
    dry = fbm(x / 28, y / 28, DRY_SEED) + 0.25 * smoothstep(0, 90, x - y)
    flowers = fbm(x / 14, y / 14, FLOWER_SEED)
    sand = sand_width(x, y)
    return (
        sd - min(0.7, sand * 0.3) if sand > 0 else 1.0,              # wet sand at the waterline
        sd - (sand + 0.8 * (fbm(x / 5, y / 5, 101) - 0.5)) if sand > 0 else 1.0,   # the beach
        forest,
        forest + 3.0 + 2.0 * (fbm(x / 7, y / 7, 109) - 0.5),         # the darker heart of a wood
        outcrop_field(x, y),                                         # stony ground: stone to gather
        sd - (BEACH + 4.5 + 4.0 * (fbm(x / 8, y / 8, 111) - 0.5)),   # lush grass along the water
        min(0.64 - dry + (0.2 if town < 18 else 0.0), lane_dist(x, y) - 3.5),   # dry meadow; the lanes
        min(0.62 - flowers, path_dist(x, y) - 1.8),                  # flower meadow, light grass; the paths
        meadow - 0.40,                                               # darker, longer grass
        0.6 - fbm(x / 3.2, y / 3.2, 119),                            # small patches of a second shade
    )


def zone_colour(f):
    """The colour of a piece of plain from its field values (all of one sign within a piece)."""
    wet, sand, forest, core, stony, lush, dry, light, mid, alt = f
    if wet < 0:
        return "sand_wet"
    if sand < 0:
        return "sand"
    b = "_b" if alt < 0 else ""
    if core < 0:
        return "forest_floor" + b
    if forest < 0:
        return "grass_dark" + b
    if stony < 0:                    # gravel broken up by turf, so it reads as a clearing
        return "stony" if alt < 0 else "pebble" if mid < 0 else "grass_mid_b"
    if lush < 0:
        return "grass_lush" + b
    if dry < 0:
        return "grass_dry" + b
    if light < 0:
        return "grass_light" + b
    if mid < 0:
        return "grass_mid" + b
    return "grass" + b


def build_vertices():
    """(N+1)^2 grid corners, nudged off the grid (never along the map's border): position,
    height, distance from water and every colour field."""
    verts = {}
    for i in range(N + 1):
        for j in range(N + 1):
            x, y = i - HALF, j - HALF
            jx = (_hash(i, j, 5) - 0.5) * 0.6 if 0 < i < N else 0.0
            jy = (_hash(i, j, 7) - 0.5) * 0.6 if 0 < j < N else 0.0
            px, py = x + jx, y + jy
            z, sd = height_at(px, py)
            verts[i, j] = (px, py, z, sd) + fields_at(px, py, sd)
    return verts


def _cut(poly, k, value=0.0):
    """Splits a polygon of vertex tuples along component k == value: (below, above)."""
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
    """Cuts a polygon along every field in `ks` that it straddles; returns the pieces."""
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


def _centre(poly):
    n = len(poly)
    return tuple(sum(p[q] for p in poly) / n for q in range(len(poly[0])))


def relief_colour(z, nz, x, y, f):
    """A relief piece: gentle slopes keep the plain's zone colours so a hill grows out of its
    meadow without a seam; steeper ones turn to moss, then to banded rock."""
    if nz > 0.9 and z < 3.0:
        c = zone_colour(f)
        return "grass_mid" if c in ("sand", "sand_wet", "stony", "pebble") else c
    if nz > 0.78:
        return "grass_mid" if z < 6 else "moss"
    if nz > 0.62:
        return "moss" if fbm(x / 6, y / 6, 113) > 0.5 else "rock"
    band = int((z + 2.0 * fbm(x / 7, y / 7, 109)) / 1.4) % 3
    return ["rock_mid", "rock_dark", "rock"][band] if z < 12 else "rock_light"


CHUNKS = 4                                      # ground and relief come in CHUNKS^2 pieces


def _chunk_of(x, y):
    size = N / CHUNKS
    return (min(CHUNKS - 1, max(0, int((x + HALF) // size))),
            min(CHUNKS - 1, max(0, int((y + HALF) // size))))


def build_surface(verts):
    """{(cx, cy): builder} for the plain and for the relief, and the buildable-flat mask."""
    ground, relief = {}, {}
    flat = [[True] * N for _ in range(N)]         # every corner at exactly 0: buildable plain
    for i in range(N):
        for j in range(N):
            corners = [verts[i, j], verts[i + 1, j], verts[i + 1, j + 1], verts[i, j + 1]]
            if any(abs(c[2]) > 1e-6 for c in corners):
                flat[i][j] = False
            if _hash(i, j, 3) < 0.5:
                tris = [(corners[0], corners[1], corners[2]), (corners[0], corners[2], corners[3])]
            else:
                tris = [(corners[0], corners[1], corners[3]), (corners[1], corners[2], corners[3])]
            for tri in tris:
                _, above = _cut(list(tri), 2, CLIP_Z)        # the part above the water's cut
                if not above:
                    continue
                is_relief = max(p[2] for p in above) > 0.02
                for piece in _split(above, FIELD_KS):
                    c = _centre(piece)
                    f = c[4:]
                    key = _chunk_of(c[0], c[1])
                    if is_relief:
                        a, b, d = (Vector(p[:3]) for p in piece[:3])
                        n = (b - a).cross(d - a)
                        nz = abs(n.z) / max(n.length, 1e-9)
                        relief.setdefault(key, TerrainBuilder()).poly(
                            [p[:3] for p in piece], relief_colour(c[2], nz, c[0], c[1], f))
                    else:
                        ground.setdefault(key, TerrainBuilder()).poly([p[:3] for p in piece], zone_colour(f))
    return ground, relief, flat


def build_water(verts):
    """The water plane, only where there is water (and a lip under the shore), on the same nudged
    grid as the land, cut into depth bands along smooth contours: a pale rim at the waterline,
    turquoise shallows, blue, deep blue."""
    b = TerrainBuilder()
    for i in range(N):
        for j in range(N):
            corners = [verts[i, j], verts[i + 1, j], verts[i + 1, j + 1], verts[i, j + 1]]
            if min(c[3] for c in corners) > 0.6:
                continue
            # (x, y, z, depth, rim, shallow, deep) -- negative = inside the band
            pts = []
            for c in corners:
                x, y, sd = c[0], c[1], c[3]
                depth = -sd + 1.8 * (fbm(x / 6, y / 6, 113) - 0.5)
                pts.append((x, y, WATER_Z, depth, -sd - 0.28, depth - 1.8, depth - 6.5))
            for tri in ((pts[0], pts[1], pts[2]), (pts[0], pts[2], pts[3])):
                for pc in _split(list(tri), (4, 5, 6)):
                    c = _centre(pc)
                    if c[4] < 0:
                        col = "foam"
                    elif c[5] < 0:
                        col = "water_shallow"
                    elif c[6] < 0:
                        col = "water"
                    else:
                        col = "water_deep"
                    b.poly([p[:3] for p in pc], col)
    return b


def merge_flat(obj):
    """Dissolves coplanar neighbours of one colour into larger faces, then re-triangulates --
    a large single-colour stretch of plain costs a handful of triangles instead of thousands."""
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


def build_mask(cells, z, color):
    b = TerrainBuilder()
    for (i, j) in cells:
        x, y = i - HALF, j - HALF
        b.poly([(x, y, z), (x + 1, y, z), (x + 1, y + 1, z), (x, y + 1, z)], color)
    return b


# -- decoration ----------------------------------------------------------------------------------


def scatter_decor(flat, forest_cells, rnd):
    """(kind, x, y, rotation, scale) for every prop. Densities are per square metre."""
    items = []

    def add(kind, x, y, s=1.0):
        items.append((kind, x, y, rnd.uniform(0, 6.283), s))

    def h_at(x, y):
        return height_at(x, y)[0]

    for i in range(N):
        for j in range(N):
            x0, y0 = i - HALF, j - HALF
            for k in range(2):                       # two tries per cell
                x, y = x0 + rnd.random(), y0 + rnd.random()
                z, sd = height_at(x, y)
                r = rnd.random()
                if sd < -0.9:                         # open water: lilies on the still ones
                    still = (_ellipse_sd(x, y, *LAKE, 0.45, 7) < 0 or
                             any(_ellipse_sd(x, y, *p, 0.35, 5) < 0 for p in PONDS))
                    if still and fbm(x / 7, y / 7, 131) > 0.6 and r < 0.18 and sd > -9:
                        add("lily", x, y, rnd.uniform(0.8, 1.2))
                    continue
                if ford_at(x, y) > 0.5 and sd < BEACH:  # a ford's gravel bar
                    if r < 0.12:
                        add("pebbles", x, y, rnd.uniform(0.9, 1.5))
                    continue
                if -0.9 <= sd < 0.6:                  # the waterline: reeds where the water is calm
                    calm = (river(x, y)[1] > 0.5 and coast_sd(x, y) > 10 and ford_at(x, y) == 0) or \
                        _ellipse_sd(x, y, *LAKE, 0.45, 7) < 1.5
                    if calm and fbm(x / 9, y / 9, 137) > 0.45 and r < 0.35 and \
                            math.hypot(x - START[0], y - START[1]) > 20:
                        add("reeds", x, y, rnd.uniform(0.8, 1.25))
                    continue
                if z > 0.05:                          # hills and mountains
                    # crags only up on the slope, never at the foot where a player could take
                    # one for a boulder to gather
                    if r < 0.03 and z > 0.8:
                        add("crag", x, y, rnd.uniform(0.6, 1.3))
                    elif r < 0.09 and z < 9:
                        add("bush", x, y, rnd.uniform(0.8, 1.3))
                    elif r < 0.3 and z < 10:
                        add("tuft", x, y, rnd.uniform(0.9, 1.3))
                    continue
                if sd < BEACH:                        # the beach itself stays clean but for pebbles
                    if r < 0.03:
                        add("pebbles", x, y)
                    continue
                if (i, j) in forest_cells:            # forest floor
                    if r < 0.12:
                        add("fern", x, y, rnd.uniform(0.8, 1.4))
                    elif r < 0.15:
                        add("mushrooms", x, y)
                    elif r < 0.20:
                        add("bush", x, y, rnd.uniform(0.8, 1.2))
                    elif r < 0.205:
                        add("log", x, y)
                    elif r < 0.212:
                        add("stump", x, y)
                    continue
                if outcrop(x, y):
                    if r < 0.15:
                        add("pebbles", x, y, rnd.uniform(0.9, 1.5))
                    continue
                town = math.hypot(x - START[0], y - START[1]) < 12
                flowers = fbm(x / 14, y / 14, FLOWER_SEED)
                clump = fbm(x / 3, y / 3, 139)          # tufts grow in clumps, not evenly
                if flowers > 0.64 and not town and r < 0.45:
                    add("flowers", x, y, rnd.uniform(0.9, 1.3))
                elif r < (0.04 if town else 0.16) * clump * 1.6:
                    add("tuft", x, y, rnd.uniform(0.8, 1.4))
                elif r < 0.03 * clump and not town:
                    add("bush", x, y, rnd.uniform(0.7, 1.1))
                elif r < 0.10 and flowers > 0.52 and not town:
                    add("flowers", x, y)
    return items


# -- scene ---------------------------------------------------------------------------------------


def make_object(name, builder, material, collection):
    me = builder.to_mesh(name)
    me.materials.append(material)
    obj = bpy.data.objects.new(name, me)
    collection.objects.link(obj)
    return obj


def decor_variant(kind, x, y, count):
    return int(_hash(int(x * 7), int(y * 7), 151) * count) % count


def decor_z(kind, x, y):
    if kind == "lily":
        return WATER_Z + 0.005
    return max(height_at(x, y)[0], WATER_Z)


def place_decor(items, protos, coll, rnd):
    """Linked duplicates of the prototype meshes -- for the pictures; the game takes the list."""
    for kind, x, y, rot, s in items:
        variants = protos[kind]
        me = variants[decor_variant(kind, x, y, len(variants))]
        o = bpy.data.objects.new(f"d_{kind}", me)
        o.location = (x, y, decor_z(kind, x, y))
        o.rotation_euler = (0, 0, rot)
        o.scale = (s, s, s)
        coll.objects.link(o)


def preview_population(flat, forest_cells, rocks_cells, coll_name="Preview (not part of the map)"):
    """The real trees, boulders and a small town, placed the way the game would."""
    with bpy.data.libraries.load(MODELS, link=False) as (src, dst):
        wanted = [n for n in src.objects if n.startswith(("Tree", "Stone")) or n in (
            "Castle1-lvl1", "Hovel1-lvl1", "Cottage1-lvl1", "Farm1-lvl1", "Windmill1-lvl1",
            "Sawmill1-lvl1", "Well1-lvl1", "Barn1-lvl1", "Orchard1-lvl1", "Road1-lvl1",
            "Quarry1-lvl1", "FisherHut1-lvl1", "Tavern1-lvl1")]
        dst.objects = wanted
    coll = bpy.data.collections.new(coll_name)
    bpy.context.scene.collection.children.link(coll)
    protos = {o.name: o for o in dst.objects if o is not None}
    trees = [protos[f"Tree{i}"] for i in range(1, 11) if f"Tree{i}" in protos]
    stones = [protos[f"Stone{i}"] for i in range(1, 11) if f"Stone{i}" in protos]
    rnd = random.Random(7)

    def place(proto, x, y, rot=None):
        o = proto.copy()
        o.location = (x, y, 0)
        o.rotation_euler = (0, 0, rot if rot is not None else rnd.uniform(0, 6.283))
        coll.objects.link(o)

    taken = set()
    cells = list(forest_cells)
    for _ in range(6000 * 20):
        if len(taken) >= 6000 or not cells:
            break
        i, j = rnd.choice(cells)
        if (i, j) in taken:                     # the jungle is thick: a tree in every cell
            continue
        taken.add((i, j))
        place(rnd.choice(trees), i - HALF + 0.5, j - HALF + 0.5)
    rc = list(rocks_cells)
    rnd.shuffle(rc)
    weights = [10 - k for k in range(len(stones))]
    used = set()
    n = 0
    for i, j in rc:
        if n >= 140:
            break
        if any((i + a, j + c) in used for a in (-2, -1, 0, 1, 2) for c in (-2, -1, 0, 1, 2)):
            continue
        used.add((i, j))
        n += 1
        place(rnd.choices(stones, weights)[0], i - HALF + 0.5, j - HALF + 0.5)

    sx, sy = START
    town = [("Castle1-lvl1", 0, 0), ("Hovel1-lvl1", -7, -7), ("Cottage1-lvl1", -3, -8),
            ("Hovel1-lvl1", 6, -7), ("Well1-lvl1", 1, -7), ("Farm1-lvl1", -9, 2),
            ("Farm1-lvl1", -9, 3), ("Farm1-lvl1", -10, 2), ("Windmill1-lvl1", -9, 7),
            ("Sawmill1-lvl1", 9, 4), ("Barn1-lvl1", 7, -3), ("Orchard1-lvl1", 10, -11),
            ("Tavern1-lvl1", -8, -12)]
    for name, dx, dy in town:
        if name in protos:
            place(protos[name], sx + dx, sy + dy, 0.0)
    if "Road1-lvl1" in protos:
        for dy in range(-14, -3):
            place(protos["Road1-lvl1"], sx + 0.5, sy + dy + 0.5, 0.0)
        for dx in range(-6, 7):
            place(protos["Road1-lvl1"], sx + dx + 0.5, sy - 9.5, 0.0)
    for o in protos.values():
        o.hide_render = True
    preview_camps(coll)


def preview_camps(coll):
    """Stand-ins for the round orc camps (their real models come later): an outer palisade and an
    inner wall of posts, a gap for the gate facing the lane, a purple ring for the portal."""
    def material(name, rgb):
        m = bpy.data.materials.new(name)
        m.diffuse_color = (*rgb, 1.0)
        m.use_nodes = True
        m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*rgb, 1.0)
        return m
    wood, stone, magic = material("Palisade", (0.35, 0.2, 0.1)), material("CampWall", (0.4, 0.38, 0.35)), material("Portal", (0.6, 0.1, 0.8))
    for (cx, cy, _), lane in zip(CAMPS, LANE_ANGLES):
        gate = RAD(lane + 180.0)
        for radius, count, post_r, post_h, mat in ((15.0, 70, 0.32, 2.4, wood), (8.5, 40, 0.38, 2.0, stone)):
            for k in range(count):
                t = gate + 2 * math.pi * k / count
                off = (t - gate + math.pi) % (2 * math.pi) - math.pi
                if abs(off) < 2.6 / radius:      # the gate
                    continue
                bpy.ops.mesh.primitive_cylinder_add(vertices=6, radius=post_r, depth=post_h,
                                                    location=(cx + math.cos(t) * radius, cy + math.sin(t) * radius, post_h / 2))
                o = bpy.context.active_object
                o.data.materials.append(mat)
                for c in o.users_collection:
                    c.objects.unlink(o)
                coll.objects.link(o)
        bpy.ops.mesh.primitive_torus_add(major_radius=2.2, minor_radius=0.35, location=(cx, cy, 2.4), rotation=(math.pi / 2, 0, gate))
        o = bpy.context.active_object
        o.data.materials.append(magic)
        for c in o.users_collection:
            c.objects.unlink(o)
        coll.objects.link(o)


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.wm.save_as_mainfile(filepath=OUT)
    img = terrain.ensure_palette(os.path.join(os.path.dirname(OUT), "Terrain-Palette.png"))
    mat = terrain.ensure_material(img)

    verts = build_vertices()
    ground_parts, relief_parts, flat = build_surface(verts)

    coll = bpy.data.collections.new("Map")
    bpy.context.scene.collection.children.link(coll)
    raw_ground = sum(b.triangles() for b in ground_parts.values())
    g_tris = r_tris = 0
    for (cx, cy), b in sorted(ground_parts.items()):
        g_tris += merge_flat(make_object(f"{PREFIX}-Ground-c{cx}{cy}", b, mat, coll))
    for (cx, cy), b in sorted(relief_parts.items()):
        r_tris += merge_flat(make_object(f"{PREFIX}-Relief-c{cx}{cy}", b, mat, coll))
    water = make_object(f"{PREFIX}-Water", build_water(verts), mat, coll)
    w_tris = merge_flat(water)

    # zones, by cell: flat ground only, clear of the beach
    cell_sd = {(i, j): height_at(i - HALF + 0.5, j - HALF + 0.5)[1] for i in range(N) for j in range(N)}
    forest_cells = {(i, j) for i in range(N) for j in range(N)
                    if flat[i][j] and cell_sd[i, j] > BEACH + 1
                    and math.hypot(i - HALF + 0.5 - START[0], j - HALF + 0.5 - START[1]) > 8
                    and in_forest(i - HALF + 0.5, j - HALF + 0.5)}
    rocks_cells = {(i, j) for i in range(N) for j in range(N)
                   if flat[i][j] and (i, j) not in forest_cells and cell_sd[i, j] > BEACH + 1
                   and outcrop(i - HALF + 0.5, j - HALF + 0.5)}
    # the water a bridge or a dock may stand on -- to the very edge of the bank, where a cell
    # whose centre is just dry can still read as water in the game
    shore_cells = {(i, j) for i in range(N) for j in range(N) if -3.0 < cell_sd[i, j] < 0.6}
    # where nothing may be built: any cell the relief touches (a corner above the plain)
    no_build = {(i, j) for i in range(N) for j in range(N)
                if cell_sd[i, j] > 0 and any(verts[c][2] > 0.02 for c in ((i, j), (i + 1, j), (i + 1, j + 1), (i, j + 1)))}
    # a ford is ground everyone walks: a building on it would shut the crossing
    no_build |= {(i, j) for i in range(N) for j in range(N)
                 if ford_at(i - HALF + 0.5, j - HALF + 0.5) > 0 and cell_sd[i, j] < BEACH + 1}
    zones = []
    for name, cells, z, col in (("TreesArea", forest_cells, -0.02, "leaf"),
                                ("RocksArea", rocks_cells, -0.03, "rock_dark"),
                                ("NoBuildArea", no_build, -0.04, "rock_deep"),
                                ("PlaceForWaterObjects", shore_cells, -0.6, "water_deep")):
        o = make_object(f"{PREFIX}-{name}", build_mask(cells, z, col), mat, coll)
        merge_flat(o)
        o.hide_render = True
        o.hide_set(True)
        zones.append(o)

    land = sum(1 for i in range(N) for j in range(N) if flat[i][j] and cell_sd[i, j] > 0)
    water_cells = sum(1 for v in cell_sd.values() if v < 0)
    print("INV1 fords", [(round(x, 1), round(y, 1)) for x, y in FORDS],
          "bridges", [(round(x, 1), round(y, 1)) for x, y in BRIDGES])
    print(f"INV1 ground tris={g_tris} (from {raw_ground}), relief tris={r_tris}, water tris={w_tris}")
    print(f"INV1 buildable flat cells={land} ({100 * land / N / N:.0f}%), water cells={water_cells} "
          f"({100 * water_cells / N / N:.0f}%), forest cells={len(forest_cells)}, rock cells={len(rocks_cells)}, "
          f"shore cells={len(shore_cells)}, no-build cells={len(no_build)}")

    rnd = random.Random(31)
    items = scatter_decor(flat, forest_cells, rnd)
    counts = {}
    for it in items:
        counts[it[0]] = counts.get(it[0], 0) + 1
    print("INV1 decor", len(items), counts)
    kcoll = bpy.data.collections.new("DecorKit")
    bpy.context.scene.collection.children.link(kcoll)
    protos = terrain.build_prototypes(mat, kcoll)
    for kind, meshes in protos.items():
        for v, me in enumerate(meshes):
            o = bpy.data.objects.new(f"{PREFIX}-DecorKit-{kind}_{v}", me)
            kcoll.objects.link(o)
            o.hide_render = True
    total = 0
    with open(os.path.splitext(OUT)[0] + "-decor.csv", "w", encoding="utf-8") as f:
        f.write("kind,variant,x,y,z,rot,scale\n")
        for kind, x, y, rot, sc in items:
            v = decor_variant(kind, x, y, len(protos[kind]))
            total += sum(len(p.vertices) - 2 for p in protos[kind][v].polygons)
            f.write(f"{kind},{v},{x:.3f},{y:.3f},{decor_z(kind, x, y):.3f},{rot:.3f},{sc:.3f}\n")
    print(f"INV1 decor triangles {total}")
    with open(os.path.splitext(OUT)[0] + "-spots.csv", "w", encoding="utf-8") as f:
        f.write("kind,x,y\n")
        for x, y, level in CAMPS:
            f.write(f"camp,{x:.1f},{y:.1f},{level}\n")
        for x, y in START_ZONE:
            f.write(f"zone,{x:.1f},{y:.1f}\n")
        for x, y, level in MONSTERS:
            f.write(f"monsters,{x:.1f},{y:.1f},{level}\n")
        f.write(f"boss,{BOSS[0]:.1f},{BOSS[1]:.1f}\n")
        for x, y in SHRINES:
            f.write(f"shrine,{x:.1f},{y:.1f}\n")
        for x, y in BRIDGES:
            f.write(f"bridge,{x:.1f},{y:.1f}\n")

    if PREVIEW:
        dcoll = bpy.data.collections.new("Decor")
        bpy.context.scene.collection.children.link(dcoll)
        place_decor(items, protos, dcoll, rnd)
        preview_population(flat, forest_cells, rocks_cells)
    kit.file_display_settings()
    bpy.ops.wm.save_mainfile()
    print("SAVED", OUT)


main()
