"""The archipelago's islands, one per run (the user, 2026-10-29: a library of islands made here,
at the designed maps' quality, which the game's seed picks, turns and lays out).

    blender --background --factory-startup --python islands.py -- <out.blend> <kind> <variant> [preview]

kind: start, orc, bot, flat, hilly, rocky, long, crescent, atoll, islet; variant: 1, 2 ...

Map 5's generator (the faceted surface, the colour zones, the water bands, the decor, the zones the
game reads) with the island's own layout: see the island section. The frame is square, 40 % wider
than the island, so no cape runs into its edge.

`preview` also stands the real trees, boulders (and on the start island a small town) on it.
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
KIND = argv[1]
VARIANT = int(argv[2])
PREVIEW = "preview" in argv[3:]
MODELS = os.path.join(os.path.dirname(OUT), "Buildings-Size1-Models.blend")
if not os.path.exists(MODELS):     # the islands live in a folder of their own next to the models
    MODELS = os.path.join(os.path.dirname(os.path.dirname(OUT)), "Buildings-Size1-Models.blend")
PREFIX = f"Isl-{KIND}{VARIANT}"

_SCALES = {'start': 130, 'orc': 125, 'bot': 120, 'flat': 150, 'hilly': 150, 'rocky': 150, 'long': 170,
           'crescent': 150, 'atoll': 150, 'islet': 70}
N = int(round(_SCALES[KIND] * 1.4 / 2)) * 2
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


def _ellipse_sd(x, y, cx, cy, rx, ry, wobble, seed):
    """Approximate signed distance to a noisy ellipse, positive outside."""
    ang = math.atan2((y - cy) / ry, (x - cx) / rx)
    k = 1.0 + wobble * (fbm(math.cos(ang) * 1.3 + seed, math.sin(ang) * 1.3 - seed, seed) - 0.5)
    r = math.hypot((x - cx) / rx, (y - cy) / ry)
    return (r - k) * min(rx, ry)


# -- the island ----------------------------------------------------------------------------------
#
# North (+y) away from the camera. One island of the archipelago in its own square frame (the user,
# 2026-10-29: a library of islands made here, at the designed maps' quality; the game's seed picks,
# turns and lays them out). Its coast is a base shape for its KIND -- round, long and bent, a
# crescent, an atoll with channels, an islet -- bent by three octaves of noise (the shapes the user
# approved in Tools/archipelago/islands_preview.py). Relief follows the kind: a hill, rocky ridges,
# a spine along a long island, a few low hills on a meadow island. A low rocky rim runs along the
# coast, broken at the BEACHES -- where ships land, at least one on every island. The start island
# has the Town Hall's zone, the orcs' island the main base's flat ground near the coast (their
# shipyard), the bot's island its town's ground.

KINDS = {
    #          scale, base shape, relief, hills, beaches, rim height
    'start':    (130, 'round', 'flat', 2, (2, 3), 1.4),
    'orc':      (125, 'round', 'rocky', 0, (2, 2), 2.6),
    'bot':      (120, 'round', 'flat', 2, (2, 3), 1.4),
    'flat':     (150, 'round', 'flat', 3, (2, 3), 1.4),
    'hilly':    (150, 'round', 'hill', 1, (2, 3), 1.8),
    'rocky':    (150, 'round', 'rocky', 0, (1, 1), 2.8),
    'long':     (170, 'long', 'spine', 1, (2, 3), 2.0),
    'crescent': (150, 'crescent', 'low', 0, (2, 2), 1.4),
    'atoll':    (150, 'atoll', 'flat', 0, (2, 2), 0.8),
    'islet':    (70, 'islet', 'flat', 0, (1, 1), 0.8),
}
_spec = KINDS[KIND]
SCALE = _spec[0] if VARIANT == 1 or KIND in ('start', 'orc', 'bot', 'islet') else int(_spec[0] * 0.86)
SHAPE, RELIEF_KIND, HILL_COUNT, BEACH_COUNT, RIM = _spec[1], _spec[2], _spec[3], _spec[4], _spec[5]
SEED = {'start': 11, 'orc': 23, 'bot': 37, 'flat': 41, 'hilly': 53, 'rocky': 67, 'long': 71,
        'crescent': 83, 'atoll': 97, 'islet': 101}[KIND] * 10 + VARIANT
_rnd = random.Random(SEED)
ANGLE = _rnd.uniform(0, 2 * math.pi)
SQUASH = 1.0 + _rnd.random() * 0.35
RADIUS = {'start': 0.33, 'orc': 0.38, 'bot': 0.33, 'flat': 0.33, 'hilly': 0.34, 'rocky': 0.32}.get(KIND, 0.33) * SCALE
CHANNELS = [_rnd.uniform(0, 2 * math.pi) for _ in range(_rnd.randint(1, 2))] if SHAPE == 'atoll' else []
S1, S2, S3, S4 = SEED * 7 + 1, SEED * 7 + 2, SEED * 7 + 3, SEED * 7 + 4

PONDS = []
LAKE = (1e5, 1e5, 1.0, 1.0)        # none: far off the frame
FORDS, BRIDGES = [], []
RUIN = (1e5, 1e5)


def _uv(x, y):
    ca, sa = math.cos(ANGLE), math.sin(ANGLE)
    return x * ca + y * sa, -x * sa + y * ca


def shore(x, y):
    """< 1 on land: the kind's base shape plus the bent coast."""
    u, v = _uv(x, y)
    if SHAPE == 'long':
        bend = (u / SCALE) ** 2 * SCALE * 0.5
        d = math.hypot(u / (0.46 * SCALE), (v - bend) / (0.14 * SCALE))
    elif SHAPE == 'crescent':
        ring = abs(math.hypot(u, v) - 0.32 * SCALE) / (0.11 * SCALE)
        d = ring + max(0.0, u / (0.4 * SCALE) + 0.35) * 1.4
    elif SHAPE == 'atoll':
        thick = 0.06 + 0.06 * fbm(x / (SCALE / 4), y / (SCALE / 4), S4)
        d = abs(math.hypot(u, v) - 0.34 * SCALE) / (thick * SCALE)
        ang = math.atan2(v, u)
        for ch in CHANNELS:
            off = (ang - ch + math.pi) % (2 * math.pi) - math.pi
            d += math.exp(-(off / 0.12) ** 2) * 2.0
    elif SHAPE == 'islet':
        d = math.hypot(u, v) / (0.16 * SCALE)
    else:
        d = math.hypot(u / SQUASH, v * SQUASH * 0.9) / RADIUS
    warp = (0.75 * (fbm(x / (SCALE / 3), y / (SCALE / 3), S1) - 0.5)
            + 0.45 * (fbm(x / (SCALE / 7), y / (SCALE / 7), S2) - 0.5)
            + 0.30 * (fbm(x / max(4.0, SCALE / 18), y / max(4.0, SCALE / 18), S3) - 0.5))
    return d + warp


# metres per unit of `shore` near the coast: the island's half width for its shape
WIDTH = {'long': 0.14, 'crescent': 0.11, 'atoll': 0.09, 'islet': 0.16}.get(SHAPE, RADIUS / SCALE) * SCALE


def coast_sd(x, y):
    """Signed distance from the sea in metres (roughly), positive on the island."""
    return (1.0 - shore(x, y)) * WIDTH


def inland(x, y):
    return max(0.0, min(1.0, 1.0 - shore(x, y)))


def river(px, py):
    return (1e9, 0.0)


def ford_at(x, y):
    return 0.0


def raw_mountains(x, y):
    """The kind's relief above the plain, before any zone is flattened into it."""
    i = inland(x, y)
    if i <= 0:
        return 0.0
    ridge = 1.0 - abs(2.0 * fbm(x / 12, y / 12, SEED + 79) - 1.0)
    crag = 1.0 - abs(2.0 * fbm(x / 4.5, y / 4.5, SEED + 80) - 1.0)
    if RELIEF_KIND == 'hill':
        rise = smoothstep(0.35, 0.95, i)
        return (6.0 + 5.0 * fbm(x / 20, y / 20, SEED + 77)) * rise ** 1.2 * (0.7 + 0.3 * ridge) + 0.8 * crag * rise
    if RELIEF_KIND == 'rocky':
        rise = smoothstep(0.28, 0.85, i)
        return (9.0 + 9.0 * fbm(x / 22, y / 22, SEED + 77)) * rise * (0.5 + 0.5 * ridge) + 1.5 * crag * rise
    if RELIEF_KIND == 'spine':
        u, v = _uv(x, y)
        bend = (u / SCALE) ** 2 * SCALE * 0.5
        spine = math.exp(-((v - bend) / (0.055 * SCALE)) ** 2)
        rise = smoothstep(0.15, 0.6, i)
        return (7.0 + 6.0 * fbm(x / 16, y / 16, SEED + 77)) * spine * rise * (0.7 + 0.3 * ridge) + 1.0 * crag * spine * rise
    if RELIEF_KIND == 'low':
        rise = smoothstep(0.5, 0.95, i)
        return 4.0 * rise * (0.6 + 0.4 * ridge)
    return 0.0


# -- the zones the island needs: found on the shape before the relief is laid ------------------

def _search(score, step=4.0):
    best, best_s = None, -1e18
    lim = HALF - 4
    y = -lim
    while y <= lim:
        x = -lim
        while x <= lim:
            s = score(x, y)
            if s is not None and s > best_s:
                best, best_s = (x, y), s
            x += step
        y += step
    return best


def _clear(x, y, r, margin):
    """The worst of `margin` minus the coast distance and the summed relief over a disc: None if the disc is not all land with `margin` to spare."""
    total = 0.0
    for k in range(12):
        a = k * math.pi / 6
        for rr in (r * 0.5, r):
            px, py = x + math.cos(a) * rr, y + math.sin(a) * rr
            if coast_sd(px, py) < margin:
                return None
            total += raw_mountains(px, py)
    return total


START = (1e5, 1e5)
START_ZONE = []
MAIN_BASE = (1e5, 1e5)
MAIN_PAD = (-21.0, 20.0, -26.0, 8.0)           # the main base's flat ground round its spot (map 5's)
BOT = None
CAMPS = []
CAMP_HALF = {1: 6, 2: 7, 3: 11}
PAD_MARGIN = 1.0
ZONE_R = 18.0

if KIND == 'start':
    def _start_score(x, y):
        m = _clear(x, y, ZONE_R + 6, 8.0)
        return None if m is None else -m * 5 - math.hypot(x, y) * 0.02
    START = _search(_start_score)
    START_ZONE = [(START[0] + math.cos(math.pi / 8 + k * math.pi / 4) * ZONE_R,
                   START[1] + math.sin(math.pi / 8 + k * math.pi / 4) * ZONE_R) for k in range(8)]
if KIND == 'orc':
    def _orc_score(x, y):
        x0, x1, y0, y1 = MAIN_PAD
        worst, total = 1e9, 0.0
        for px in (x + x0, x + (x0 + x1) / 2, x + x1):
            for py in (y + y0, y + (y0 + y1) / 2, y + y1):
                sd = coast_sd(px, py)
                if sd < 5.0:
                    return None
                worst = min(worst, sd)
                total += raw_mountains(px, py)
        # near the coast: the orcs' shipyard stands by their base
        return -worst - total * 0.3
    MAIN_BASE = _search(_orc_score)
if KIND == 'bot':
    def _bot_score(x, y):
        m = _clear(x, y, 16.0, 6.0)
        return None if m is None else -m * 5 - math.hypot(x, y) * 0.02
    BOT = _search(_bot_score)


def camp_pad(x, y):
    """1 on a zone's ground and its margin, fading out a few metres further: no relief, no wood."""
    best = 0.0
    if KIND == 'start':
        best = smoothstep(ZONE_R + 10.0, ZONE_R + 4.0, math.hypot(x - START[0], y - START[1]))
    if KIND == 'bot' and BOT:
        best = smoothstep(22.0, 16.0, math.hypot(x - BOT[0], y - BOT[1]))
    if KIND == 'orc':
        x0, x1, y0, y1 = MAIN_PAD
        mx, my = MAIN_BASE
        d = max(x0 + mx - x, x - x1 - mx, y0 + my - y, y - y1 - my)
        best = max(best, smoothstep(3.0, 0.0, d))
    return best


# -- the beaches: arcs of coast where the rim breaks and the sand is wide --------------------------

def _coast_point(angle):
    """Where a ray from the frame's middle last leaves land going out at this bearing."""
    last = None
    r = 0.0
    while r < HALF:
        x, y = math.cos(angle) * r, math.sin(angle) * r
        if coast_sd(x, y) > 0:
            last = (x, y)
        r += 1.0
    return last


def _beaches():
    out = []
    want = _rnd.randint(*BEACH_COUNT)
    if KIND == 'orc':
        # one by the main base: where their ships put out
        a = math.atan2(MAIN_BASE[1], MAIN_BASE[0])
        p = _coast_point(a)
        if p:
            out.append(p)
    tries = 0
    while len(out) < want and tries < 200:
        tries += 1
        p = _coast_point(_rnd.uniform(0, 2 * math.pi))
        if p is None:
            continue
        if any(math.dist(p, q) < 45 for q in out):
            continue
        if raw_mountains(p[0] * 0.85, p[1] * 0.85) > 3.0 and tries < 150:
            continue
        out.append(p)
    return out or [_coast_point(0.0)]


BEACHES = _beaches()
GROVES, FORESTS, OUTCROPS, HILLS = [], [], [], []


def beach_gate(x, y):
    """1 on a landing beach, 0 away from them."""
    return max(smoothstep(24.0, 14.0, math.hypot(x - bx, y - by)) for bx, by in BEACHES)


def land_sd(x, y):
    return coast_sd(x, y)


def mountains(x, y):
    return raw_mountains(x, y)


def cliffs(x, y):
    """A low rocky rim along the coast, broken at the beaches; on a meadow island only in stretches."""
    c = coast_sd(x, y)
    if c < BEACH or c > BEACH + 9:
        return 0.0
    rim = smoothstep(BEACH, BEACH + 2.5, c) * smoothstep(BEACH + 9.0, BEACH + 5.0, c)
    h = (RIM + RIM * 0.75 * fbm(x / 7, y / 7, SEED + 87)) * rim
    if RIM < 2.0:
        h *= smoothstep(0.42, 0.6, fbm(x / 26, y / 26, SEED + 88))
    h *= 1.0 - beach_gate(x, y)
    return h


def _place_features():
    """Hills, forests, groves and stony clearings, inside the island and off its zones."""
    area = sum(1 for i in range(0, N, 4) for j in range(0, N, 4) if coast_sd(i - HALF, j - HALF) > 0) * 16

    def spot(margin, avoid=()):
        for _ in range(300):
            x, y = _rnd.uniform(-HALF, HALF), _rnd.uniform(-HALF, HALF)
            if coast_sd(x, y) < margin or camp_pad(x, y) > 0 or raw_mountains(x, y) > 1.0:
                continue
            if any(math.dist((x, y), q[:2]) < q[2] for q in avoid):
                continue
            return x, y
        return None

    for _ in range(HILL_COUNT):
        p = spot(14.0)
        if p:
            HILLS.append((p[0], p[1], _rnd.uniform(4.5, 6.5), _rnd.uniform(1.5, 2.2)))
    for _ in range(max(1, int(area / 1000))):
        p = spot(8.0, [(h[0], h[1], h[2] + 4) for h in HILLS])
        if p:
            FORESTS.append((p[0], p[1], _rnd.uniform(7, 14), _rnd.uniform(6, 11)))
    for _ in range(max(1, int(area / 800))):
        p = spot(6.0)
        if p:
            GROVES.append((p[0], p[1], _rnd.uniform(3.0, 5.0)))
    for _ in range(max(1, int(area / 2500))):
        p = spot(8.0, [(f[0], f[1], max(f[2], f[3]) + 3) for f in FORESTS])
        if p:
            OUTCROPS.append((p[0], p[1], _rnd.uniform(4.0, 6.0)))
    FORESTS.extend((x, y, r, r) for x, y, r in GROVES)


_place_features()
FLOWER_SEED, DRY_SEED = SEED + 401, SEED + 433


def relief_at(x, y):
    """Height above the plain: the ring, the spurs, the coastal rim, a few low hills -- none on
    the orcs' ground."""
    pad = camp_pad(x, y)
    if pad >= 1.0:
        return 0.0
    h = max(mountains(x, y), cliffs(x, y)) * (1.0 - pad)
    for hx, hy, r, height in HILLS:
        ang = math.atan2(y - hy, x - hx)
        wob = 0.75 + 0.5 * fbm(math.cos(ang) * 1.5 + hx * 0.1, math.sin(ang) * 1.5 + hy * 0.1, 89)
        dd = math.hypot(x - hx, y - hy) / (r * wob)
        if dd < 1.0:
            bump = (0.5 + 0.5 * math.cos(math.pi * dd)) ** 0.9
            h = max(h, height * bump * (0.85 + 0.3 * fbm(x / 5, y / 5, 91)))
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
    """Negative inside a forest, positive outside, roughly in metres."""
    best = 1e9
    for cx, cy, rx, ry in FORESTS:
        e = math.sqrt(((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2)
        k = 0.82 + 0.4 * (fbm(x / 11, y / 11, 97) - 0.5)
        best = min(best, (e - k) * min(rx, ry))
    if math.hypot(x - RUIN[0], y - RUIN[1]) < 8 or camp_pad(x, y) > 0:
        best = max(best, 1.0)
    return best


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
    if _ellipse_sd(x, y, *LAKE, 0.35, 7) < 8:
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
        0.64 - dry + (0.2 if town < 18 else 0.0),                    # dry, yellowish meadow
        0.62 - flowers,                                              # flower meadow, light grass
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
                    still = (_ellipse_sd(x, y, *LAKE, 0.35, 7) < 0 or
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
                        _ellipse_sd(x, y, *LAKE, 0.35, 7) < 1.5
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
    if not os.path.exists(MODELS):
        print("MAP5 no", MODELS, "-- the preview has no trees, boulders or town")
        return
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
    for _ in range(600 * 40):
        if len(taken) >= 600 or not cells:
            break
        i, j = rnd.choice(cells)
        if any((i + a, j + c) in taken for a in (-1, 0, 1) for c in (-1, 0, 1)):
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

    if KIND != 'start':
        for o in protos.values():
            o.hide_render = True
        return
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
                    and math.hypot(i - HALF + 0.5 - START[0], j - HALF + 0.5 - START[1]) > 24
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
    print("MAP5 fords", [(round(x, 1), round(y, 1)) for x, y in FORDS],
          "bridges", [(round(x, 1), round(y, 1)) for x, y in BRIDGES])
    print(f"MAP5 ground tris={g_tris} (from {raw_ground}), relief tris={r_tris}, water tris={w_tris}")
    print(f"MAP5 buildable flat cells={land} ({100 * land / N / N:.0f}%), water cells={water_cells} "
          f"({100 * water_cells / N / N:.0f}%), forest cells={len(forest_cells)}, rock cells={len(rocks_cells)}, "
          f"shore cells={len(shore_cells)}, no-build cells={len(no_build)}")

    rnd = random.Random(31)
    items = scatter_decor(flat, forest_cells, rnd)
    counts = {}
    for it in items:
        counts[it[0]] = counts.get(it[0], 0) + 1
    print("MAP5 decor", len(items), counts)
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
    print(f"MAP5 decor triangles {total}")
    with open(os.path.splitext(OUT)[0] + "-spots.csv", "w", encoding="utf-8") as f:
        f.write("kind,x,y\n")
        if KIND == 'orc':
            f.write(f"main,{MAIN_BASE[0]:.1f},{MAIN_BASE[1]:.1f}\n")
        for x, y in START_ZONE:
            f.write(f"zone,{x:.1f},{y:.1f}\n")
        if BOT:
            f.write(f"bot,{BOT[0]:.1f},{BOT[1]:.1f}\n")
        for x, y in BEACHES:
            f.write(f"beach,{x:.1f},{y:.1f}\n")
    print("ISLAND", KIND, VARIANT, "frame", N, "start", START, "main", MAIN_BASE, "bot", BOT, "beaches", BEACHES)

    if PREVIEW:
        dcoll = bpy.data.collections.new("Decor")
        bpy.context.scene.collection.children.link(dcoll)
        place_decor(items, protos, dcoll, rnd)
        preview_population(flat, forest_cells, rocks_cells)
    kit.file_display_settings()
    bpy.ops.wm.save_mainfile()
    print("SAVED", OUT)


main()
