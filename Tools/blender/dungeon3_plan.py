"""The PLAN of dungeon level 3, «Огненные глубины» (the user, 2026-11-04): the deepest level, fire
and lava, drawn before any 3D as levels 1 and 2 were. The size of level 2 (the user): 300 x 300 build
units on a 21 x 21 grid of ~14.3 m cells.

- a MAZE of passages (seeded depth-first spanning tree + loops), nodes off their centres, bowed;
- the CAMP at the way in (south), the same gnome hire camp and potion shop;
- the DRAGON's lair: a lava lake with a hoard, one way in; the LAVA TITAN's hall: a magma pool he
  rises from, one way in;
- LAVA RIVERS across the halls; the new mechanic, «меняющийся путь» -- BOTH of the user's forms:
  - LAVA TIDE: passages along a river and inside the tide quarters are basalt floor that lava
    floods now and then (a glow and smoke first); a hero caught in it burns. Such a passage
    crossing a river is a LAVA FORD, open only at low tide;
  - CRUMBLING BRIDGES: a dry passage crosses a river on a basalt bridge; some of them -- only
    where another way back exists (the bridge lies on a loop) -- fall into the lava once crossed;
- the obsidian hall and two craters are monster arenas;
- the farthest dead ends hold crystals, the other dead ends chests.

    python dungeon3_plan.py <out.png> [seed] [layout]

Writes the picture and, next to it, <out>.json for the 3D build to follow.
"""
import json
import random
import sys

from PIL import Image, ImageDraw, ImageFont

G = 21                      # cells a side
SIZE = 300                  # build units (x 0.44 -> ~132 game cells)
CELL = SIZE / G             # ~14.3, level 1's
SEED = int(sys.argv[2]) if len(sys.argv) > 2 else 15
LOOPS = 6
CRYSTAL_ROOMS = 12
MONSTER_HALLS = 10

LAYOUT = sys.argv[3].upper() if len(sys.argv) > 3 else "A"
# A = the approved plan (seed 15); B-D the variants (the user, 2026-11-04: «сделай вариации»)
LAYOUTS = {
    "A": dict(
        big={
            "camp": (8, 0, 5, 2),
            "dragon": (14, 15, 6, 5),      # the dragon's lava lake and hoard: one way in
            "titan": (0, 9, 5, 5),         # the lava titan's magma pool: one way in
            "great": (9, 8, 5, 4),         # the obsidian hall: an arena
            "crater_w": (2, 16, 3, 3),     # craters: monster arenas
            "crater_e": (17, 3, 3, 3),
        },
        # lava rivers: polylines through cell coordinates; the main one from the dragon's lake
        # down through the obsidian hall into the titan's pool
        canals=[
            [(17.0, 17.5), (15.0, 14.0), (13.0, 11.8), (10.5, 10.0), (7.0, 10.6), (3.5, 11.5), (-0.5, 11.2)],
            [(20.5, 9.5), (17.5, 9.8), (15.0, 8.2), (12.5, 9.0)],
            [(7.5, 20.5), (6.8, 17.0), (8.0, 14.0), (10.0, 11.0)],
            [(11.0, 8.6), (11.6, 6.0), (14.5, 4.6), (17.5, 4.5)],
        ],
        # tide quarters (cells: x, y, radius): every passage in them is flooded by the lava tide
        floods=[(5.0, 6.0, 2.6), (12.0, 15.5, 2.4), (18.0, 12.5, 2.0), (3.5, 19.0, 1.8)],
    ),
    # B (the user, 2026-11-04: «сделай вариации»): the dragon in the NORTH-WEST, the titan EAST,
    # the main river running across from the dragon's lake through the obsidian hall into the titan's pool
    "B": dict(
        big={
            "camp": (8, 0, 5, 2),
            "dragon": (0, 14, 6, 5),
            "titan": (16, 9, 5, 5),
            "great": (8, 9, 5, 4),
            "crater_w": (1, 4, 3, 3),
            "crater_e": (15, 16, 3, 3),
        },
        canals=[
            [(3.0, 16.5), (5.0, 13.0), (8.0, 11.5), (12.0, 10.8), (16.0, 11.0), (20.5, 11.5)],
            [(11.0, 20.5), (10.5, 17.0), (10.0, 13.0), (10.5, 11.0)],
            [(13.5, 9.0), (13.0, 6.0), (10.5, 4.5), (6.0, 4.0), (4.5, 5.5)],
            [(20.5, 4.0), (18.0, 5.5), (17.5, 9.5)],
        ],
        floods=[(15.0, 4.0, 2.4), (5.0, 9.0, 2.4), (14.5, 17.5, 2.0), (19.0, 18.5, 1.8)],
    ),
    # C: the dragon NORTH in the middle, the titan EAST, the obsidian hall WEST
    "C": dict(
        big={
            "camp": (8, 0, 5, 2),
            "dragon": (7, 15, 6, 5),
            "titan": (16, 5, 5, 5),
            "great": (1, 8, 5, 4),
            "crater_w": (11, 7, 3, 3),
            "crater_e": (16, 15, 3, 3),
        },
        canals=[
            [(10.0, 17.5), (9.5, 14.0), (6.0, 11.5), (3.5, 10.0), (-0.5, 9.5)],
            [(9.5, 14.0), (12.5, 11.5), (15.0, 9.0), (18.5, 7.5)],
            [(20.5, 13.0), (18.0, 12.5), (17.5, 10.0)],
            [(4.0, 20.5), (4.5, 17.0), (3.5, 13.5)],
        ],
        floods=[(6.0, 5.0, 2.4), (14.0, 13.0, 2.2), (2.5, 17.0, 2.0), (19.0, 2.5, 1.8)],
    ),
    # D: the dragon EAST, the titan NORTH-WEST, the obsidian hall north of the middle
    "D": dict(
        big={
            "camp": (8, 0, 5, 2),
            "dragon": (15, 9, 6, 5),
            "titan": (1, 15, 5, 5),
            "great": (8, 14, 5, 4),
            "crater_w": (2, 6, 3, 3),
            "crater_e": (16, 17, 3, 3),
        },
        canals=[
            [(3.5, 17.5), (6.5, 16.0), (10.5, 15.5), (14.0, 14.0), (18.0, 11.5)],
            [(18.0, 11.5), (19.0, 7.0), (20.5, 4.0)],
            [(10.5, 14.0), (10.0, 10.5), (7.0, 8.5), (3.5, 7.5)],
            [(12.0, 20.5), (12.0, 18.0)],
        ],
        floods=[(13.0, 6.0, 2.6), (5.0, 11.5, 2.4), (17.5, 4.0, 2.0), (19.5, 18.5, 1.8)],
    ),
}
BIG = LAYOUTS[LAYOUT]["big"]
ONE_WAY = {"dragon", "titan"}
DOOR = None                        # no lever door on this level
LEVERS = ()
ARENAS = {"great", "crater_w", "crater_e"}
FORD_SHARE = 0.35                  # of the river crossings, this share is a lava ford (tide), not a bridge
CRUMBLE_SHARE = 0.5               # of the bridges on a loop, this share crumbles behind the heroes
CANALS = LAYOUTS[LAYOUT]["canals"]
FLOOD_ZONES = LAYOUTS[LAYOUT]["floods"]
FLOOD_NEAR = 1.0            # a passage this near a canal (in cells) is flooded too
JITTER = 0.32
BEND = 0.35


def hall_of(c):
    for name, (x0, y0, w, h) in BIG.items():
        if x0 <= c[0] < x0 + w and y0 <= c[1] < y0 + h:
            return name
    return None


def node(c):
    h = hall_of(c)
    return h if h else c


def distances(links):
    adj = {}
    for a, b in links:
        adj.setdefault(node(a), []).append(node(b))
        adj.setdefault(node(b), []).append(node(a))
    dist, todo = {"camp": 0}, ["camp"]
    while todo:
        cur = todo.pop(0)
        for n in adj.get(cur, []):
            if n not in dist:
                dist[n] = dist[cur] + CELL
                todo.append(n)
    return dist


def _cross(p1, p2, p3, p4):
    """Where segment p1-p2 meets p3-p4, or None."""
    d = (p2[0] - p1[0]) * (p4[1] - p3[1]) - (p2[1] - p1[1]) * (p4[0] - p3[0])
    if abs(d) < 1e-9:
        return None
    t = ((p3[0] - p1[0]) * (p4[1] - p3[1]) - (p3[1] - p1[1]) * (p4[0] - p3[0])) / d
    u = ((p3[0] - p1[0]) * (p2[1] - p1[1]) - (p3[1] - p1[1]) * (p2[0] - p1[0])) / d
    if 0 <= t <= 1 and 0 <= u <= 1:
        return (p1[0] + t * (p2[0] - p1[0]), p1[1] + t * (p2[1] - p1[1]))
    return None


def canal_metres():
    return [[((x + 0.5) * CELL, (y + 0.5) * CELL) for x, y in canal] for canal in CANALS]


def build(seed):
    rnd = random.Random(seed)
    cells = [(x, y) for x in range(G) for y in range(G)]
    nbrs = {}
    for x, y in cells:
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            n = (x + dx, y + dy)
            if 0 <= n[0] < G and 0 <= n[1] < G and node(n) != node((x, y)):
                nbrs.setdefault(node((x, y)), set()).add((node(n), (x, y), n))
    seen, links, stack = {"camp"} | set(ONE_WAY), [], ["camp"]
    while stack:
        cur = stack[-1]
        options = [(n, a, b) for n, a, b in nbrs[cur] if n not in seen]
        if not options:
            stack.pop()
            continue
        n, a, b = rnd.choice(sorted(options, key=str))
        seen.add(n)
        links.append((a, b))
        stack.append(n)
    dist = distances(links)
    door = None
    for boss in sorted(ONE_WAY):
        best = max(sorted(nbrs[boss], key=str), key=lambda t: dist.get(t[0], -1))
        links.append((best[2], best[1]))
        if boss == DOOR:
            door = (best[2], best[1])
    extra = [(a, b) for nd, st in nbrs.items() if nd not in ONE_WAY for n, a, b in st if n not in ONE_WAY]
    extra.sort(key=str)
    rnd.shuffle(extra)
    have = {frozenset((node(a), node(b))) for a, b in links}
    added = 0
    for a, b in extra:
        k = frozenset((node(a), node(b)))
        if k in have or added >= LOOPS:
            continue
        have.add(k)
        links.append((a, b))
        added += 1
    degree = {}
    for a, b in links:
        for c in (node(a), node(b)):
            degree[c] = degree.get(c, 0) + 1
    dist = distances(links)
    dead_ends = [c for c in degree if isinstance(c, tuple) and degree[c] == 1]
    dead_ends.sort(key=lambda c: -dist.get(c, 0))
    crystal_rooms = []
    for c in dead_ends:
        if len(crystal_rooms) >= CRYSTAL_ROOMS:
            break
        if all(abs(c[0] - o[0]) + abs(c[1] - o[1]) > 4 for o in crystal_rooms):
            crystal_rooms.append(c)
    junctions = [c for c in degree if isinstance(c, tuple) and degree[c] >= 3 and c not in crystal_rooms]
    junctions.sort(key=str)
    rnd.shuffle(junctions)
    monster_halls = junctions[:MONSTER_HALLS]
    pos = {}
    for x, y in cells:
        j = 0.0 if hall_of((x, y)) else JITTER * CELL
        pos[(x, y)] = ((x + 0.5) * CELL + rnd.uniform(-j, j), (y + 0.5) * CELL + rnd.uniform(-j, j))
    bends = [rnd.uniform(-BEND, BEND) for _ in links]
    canals = canal_metres()
    flooded = [_flooded(pos[a], pos[b], canals) and (a, b) != door for a, b in links]
    # crossings, where a passage (straight between its nodes, near enough for a plan) meets a
    # canal: a dry passage crosses on a bridge, a flooded one wades it (a ford)
    bridges, fords = [], []
    for (a, b), wet in zip(links, flooded):
        if hall_of(a) and hall_of(a) == hall_of(b):
            continue
        for canal in canals:
            for p, q in zip(canal, canal[1:]):
                hit = _cross(pos[a], pos[b], p, q)
                if hit and not hall_of((int(hit[0] // CELL), int(hit[1] // CELL))):
                    (fords if wet else bridges).append((hit[0], hit[1], a, b))
    # a share of the crossings are LAVA FORDS, not bridges: the passage is a tide passage crossing
    # the river, open only at low tide (own random, so the maze stays the seed's)
    frnd = random.Random(seed * 7 + 1)
    for hit in sorted(bridges, key=str):
        if frnd.random() < FORD_SHARE:
            bridges.remove(hit)
            fords.append(hit)
            flooded[links.index((hit[2], hit[3]))] = True
    bridges = [h for h in bridges if not flooded[links.index((h[2], h[3]))]]
    blobs = hall_blobs(rnd, links)
    crumbling = crumbling_bridges(rnd, links, bridges)
    return links, crystal_rooms, monster_halls, dead_ends, dist, pos, bends, door, bridges, fords, flooded, blobs, crumbling


def crumbling_bridges(rnd, links, bridges):
    """Indices of the bridges that fall once crossed: only one on a LOOP (without its passage the
    two ends still meet some other way, so the heroes are never cut off), CRUMBLE_SHARE of those,
    one per passage."""
    out, seen = [], set()
    order = list(range(len(bridges)))
    rnd.shuffle(order)
    for i in order:
        a, b = bridges[i][2], bridges[i][3]
        if (a, b) in seen:
            continue
        k = links.index((a, b))
        adj = {}
        for j, (p, q) in enumerate(links):
            if j != k:
                adj.setdefault(node(p), []).append(node(q))
                adj.setdefault(node(q), []).append(node(p))
        todo, reach = [node(a)], {node(a)}
        while todo:
            for n in adj.get(todo.pop(), []):
                if n not in reach:
                    reach.add(n)
                    todo.append(n)
        if node(b) in reach and rnd.random() < CRUMBLE_SHARE:
            out.append(i)
            seen.add((a, b))
    return sorted(out)


def _seg_dist(p, a, b):
    l2 = (b[0] - a[0]) ** 2 + (b[1] - a[1]) ** 2
    t = 0.0 if l2 == 0 else max(0.0, min(1.0, ((p[0] - a[0]) * (b[0] - a[0]) + (p[1] - a[1]) * (b[1] - a[1])) / l2))
    return ((p[0] - a[0] - t * (b[0] - a[0])) ** 2 + (p[1] - a[1] - t * (b[1] - a[1])) ** 2) ** 0.5


def _flooded(pa, pb, canals):
    mid = ((pa[0] + pb[0]) / 2, (pa[1] + pb[1]) / 2)
    for zx, zy, zr in FLOOD_ZONES:
        if ((mid[0] - (zx + 0.5) * CELL) ** 2 + (mid[1] - (zy + 0.5) * CELL) ** 2) ** 0.5 < zr * CELL:
            return True
    # along a canal, not across it: a passage that crosses one keeps dry on a bridge
    segs = [(p, q) for canal in canals for p, q in zip(canal, canal[1:])]
    if any(_cross(pa, pb, p, q) for p, q in segs):
        return False
    return any(_seg_dist(mid, p, q) < FLOOD_NEAR * CELL for p, q in segs)


def hall_blobs(rnd, links):
    """Each hall as overlapping blobs round its cells: a big one in the middle, one per cell
    jittered and sized at random, CORNER cells left out now and then -- never one a passage comes
    into. (x, y, r) in build units."""
    entries = {c for a, b in links for c in (a, b) if hall_of(c)}
    out = {}
    for name, (x0, y0, w, h) in BIG.items():
        blobs = [((x0 + w / 2 + rnd.uniform(-0.3, 0.3)) * CELL, (y0 + h / 2 + rnd.uniform(-0.3, 0.3)) * CELL,
                  CELL * min(w, h) * rnd.uniform(0.38, 0.48))]
        for x in range(x0, x0 + w):
            for y in range(y0, y0 + h):
                corner = x in (x0, x0 + w - 1) and y in (y0, y0 + h - 1)
                if corner and (x, y) not in entries and rnd.random() < 0.7:
                    continue
                blobs.append(((x + 0.5 + rnd.uniform(-0.25, 0.25)) * CELL, (y + 0.5 + rnd.uniform(-0.25, 0.25)) * CELL,
                              CELL * rnd.uniform(0.58, 0.85)))
        out[name] = blobs
    return out


def curve(pa, pb, bend, steps=6):
    mx, my = (pa[0] + pb[0]) / 2, (pa[1] + pb[1]) / 2
    cx, cy = mx - (pb[1] - pa[1]) * bend, my + (pb[0] - pa[0]) * bend
    out = []
    for k in range(steps + 1):
        t = k / steps
        out.append(((1 - t) ** 2 * pa[0] + 2 * (1 - t) * t * cx + t * t * pb[0],
                    (1 - t) ** 2 * pa[1] + 2 * (1 - t) * t * cy + t * t * pb[1]))
    return out


def draw(path, links, crystal_rooms, monster_halls, dead_ends, dist, pos, bends, door, bridges, fords, flooded, blobs, crumbling):
    px = 4
    pad = 40
    side = round(SIZE * px)
    img = Image.new("RGB", (side + pad * 2 + 420, side + pad * 2), (44, 38, 36))
    d = ImageDraw.Draw(img)
    try:
        font = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 18)
        small = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 17)
        icons = ImageFont.truetype("C:/Windows/Fonts/seguisym.ttf", 22)
    except OSError:
        font = small = icons = ImageFont.load_default()

    def m(p):
        return pad + p[0] * px, pad + (SIZE - p[1]) * px

    def centre(c):
        return m(((c[0] + 0.5) * CELL, (c[1] + 0.5) * CELL))

    for k in range(0, G + 1, 2):
        x = pad + k * CELL * px
        d.line([(x, pad), (x, pad + side)], fill=(56, 50, 48), width=1)
        d.line([(pad, x), (pad + side, x)], fill=(56, 50, 48), width=1)
    floor = (120, 112, 108)                    # basalt
    lava = (240, 110, 30)
    tide = (200, 80, 60)                       # passages the lava tide floods now and then
    for canal in canal_metres():               # lava rivers ~7 m wide, under everything
        d.line([m(p) for p in canal], fill=lava, width=7 * px, joint="curve")
    tide_nodes = set()
    for (a, b), bend, wet in zip(links, bends, flooded):
        d.line([m(p) for p in curve(pos[a], pos[b], bend)], fill=tide if wet else floor, width=4 * px, joint="curve")
        if wet:
            tide_nodes.update((a, b))
    for c in set(sum(([a, b] for a, b in links), [])):
        if hall_of(c) is None:
            x, y = m(pos[c])
            d.ellipse([x - 2 * px, y - 2 * px, x + 2 * px, y + 2 * px], fill=tide if c in tide_nodes else floor)
    colours = {"camp": (214, 190, 120), "dragon": (200, 40, 40), "titan": (255, 150, 40), "great": (150, 90, 200),
               "crater_w": (150, 90, 200), "crater_e": (150, 90, 200)}
    labels = {"camp": "ЛАГЕРЬ\nнайм, лавка", "dragon": "БОСС 1\nДракон\n(лавовое озеро)", "titan": "БОСС 2\nЛавовый\nтитан",
              "great": "ОБСИДИАНОВЫЙ\nЗАЛ", "crater_w": "КРАТЕР", "crater_e": "КРАТЕР"}
    for name, (x0, y0, w, h) in BIG.items():
        for bx_, by_, r in blobs[name]:
            x, y = m((bx_, by_))
            rr = (r + 1.3) * px
            d.ellipse([x - rr, y - rr, x + rr, y + rr], fill=colours[name])
        for bx_, by_, r in blobs[name]:
            x, y = m((bx_, by_))
            d.ellipse([x - r * px, y - r * px, x + r * px, y + r * px], fill=floor)
        if name in ONE_WAY:                     # the boss's pool of lava in the middle of his hall
            cx, cy = m(((x0 + w / 2) * CELL, (y0 + h / 2) * CELL))
            rr = min(w, h) * CELL * px * 0.28
            d.ellipse([cx - rr, cy - rr, cx + rr, cy + rr], fill=lava)
        cx, cy = centre((x0 + (w - 1) / 2, y0 + (h - 1) / 2))
        d.multiline_text((cx, cy), labels[name], fill=(255, 255, 255), font=font, anchor="mm", align="center",
                         stroke_width=2, stroke_fill=(40, 30, 30))
    for bx_, by_, a, b in fords:               # lava fords: open only at low tide
        x, y = m((bx_, by_))
        d.ellipse([x - 3 * px, y - 3 * px, x + 3 * px, y + 3 * px], fill=tide, outline=(255, 230, 120), width=3)
    for k, (bx_, by_, a, b) in enumerate(bridges):   # basalt bridges; the crumbling ones cracked red
        x, y = m((bx_, by_))
        falls = k in crumbling
        d.rectangle([x - 5 * px, y - 3 * px, x + 5 * px, y + 3 * px], fill=(90, 84, 80),
                    outline=(255, 60, 40) if falls else (30, 26, 24), width=3)
        if falls:
            d.line([(x - 3 * px, y - 2 * px), (x, y + px), (x + 2 * px, y - px), (x + 4 * px, y + 2 * px)],
                   fill=(255, 60, 40), width=3)
    for c in dead_ends:
        if c not in crystal_rooms:              # a chest
            x, y = m(pos[c])
            d.rectangle([x - 9, y - 6, x + 9, y + 7], fill=(150, 100, 40), outline=(255, 210, 80), width=2)
    for c in crystal_rooms:
        x, y = m(pos[c])
        d.rounded_rectangle([x - 4 * px, y - 4 * px, x + 4 * px, y + 4 * px], radius=2 * px, fill=floor,
                            outline=(190, 110, 255), width=4)
        d.polygon([(x, y - 14), (x + 9, y), (x, y + 14), (x - 9, y)], fill=(190, 110, 255))
    for c in monster_halls:
        x, y = m(pos[c])
        d.ellipse([x - 4 * px, y - 4 * px, x + 4 * px, y + 4 * px], fill=floor, outline=(230, 80, 60), width=4)
        d.text((x, y), "☠", fill=(230, 80, 60), font=icons, anchor="mm")
    x0, y0, w, h = BIG["camp"]
    ex, ey = centre((x0 + (w - 1) / 2, -0.4))
    d.polygon([(ex - 14, ey - 16), (ex + 14, ey - 16), (ex, ey + 4)], fill=(255, 230, 120))
    lx = pad * 2 + side
    rows = [((190, 110, 255), "комната с кристаллами"), ((150, 100, 40), "тупик с сундуком"),
            ((230, 80, 60), "монстры"), ((150, 90, 200), "арены: обсидиановый зал, кратеры"),
            (lava, "лавовые реки"), (tide, "проход, который заливает прилив лавы"),
            ((255, 230, 120), "лавовый брод: только при отливе"), ((90, 84, 80), "базальтовый мост"),
            ((255, 60, 40), "рушащийся мост (есть обход)"), ((214, 190, 120), "лагерь: гномы, лавка"),
            ((200, 40, 40), "логово босса: один вход")]
    for k, (col, text) in enumerate(rows):
        y = pad + 20 + k * 38
        d.rectangle([lx, y, lx + 24, y + 24], fill=col)
        d.text((lx + 34, y + 12), text, fill=(240, 240, 240), font=small, anchor="lm")
    chests = len([c for c in dead_ends if c not in crystal_rooms])
    d.multiline_text((lx, pad + 20 + len(rows) * 38 + 20),
                     f"размер: 300 × 300 (≈132 × 132 клетки)\nкомнат с кристаллами: {len(crystal_rooms)}\n"
                     f"тупиков с сундуками: {chests}\nзалов с монстрами: {len(monster_halls) + len(ARENAS)}\n"
                     f"мостов: {len(bridges)}, из них рушащихся: {len(crumbling)}\nлавовых бродов: {len(fords)}\n"
                     f"проходов под приливом: {sum(flooded)} из {len(flooded)} ({round(100 * sum(flooded) / len(flooded))}%)\n"
                     f"путь до дракона: ~{round(dist.get('dragon', 0))} м\nпуть до титана: ~{round(dist.get('titan', 0))} м",
                     fill=(240, 240, 240), font=small, spacing=8)
    img.save(path)


def main():
    out = sys.argv[1]
    plan = build(SEED)
    draw(out, *plan)
    links, crystal_rooms, monster_halls, dead_ends, dist, pos, bends, door, bridges, fords, flooded, blobs, crumbling = plan
    with open(out.rsplit(".", 1)[0] + ".json", "w", encoding="utf-8") as f:
        json.dump({"cell": CELL, "grid": G, "size": SIZE, "big": BIG, "links": links, "crystal_rooms": crystal_rooms,
                   "monster_halls": monster_halls, "dead_ends": dead_ends, "lava": canal_metres(),
                   "bridges": [[x, y] for x, y, _, _ in bridges], "crumbling": crumbling,
                   "fords": [[x, y] for x, y, _, _ in fords], "tide": flooded, "tide_zones": FLOOD_ZONES,
                   "hall_blobs": blobs,
                   "pos": [[c[0], c[1], p[0], p[1]] for c, p in pos.items()], "bends": bends}, f)
    print("PLAN3 layout", LAYOUT, "seed", SEED, "links", len(links), "crystal", len(crystal_rooms), "dead", len(dead_ends),
          "bridges", len(bridges), "crumble", len(crumbling), "fords", len(fords), "tide", sum(flooded), "of", len(flooded),
          "dragon", round(dist.get("dragon", 0)), "titan", round(dist.get("titan", 0)))


main()
