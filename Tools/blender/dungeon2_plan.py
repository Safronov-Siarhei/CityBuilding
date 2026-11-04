"""The PLAN of dungeon level 2, «Затопленные гномьи чертоги» (the user, 2026-11-03): drowned dwarven
halls, drawn before any 3D, the way level 1 was (dungeon_plan.py). 1.5x level 1: 300 x 300 build
units on a 21 x 21 grid of ~14.3 m cells (level 1's cell size), so the passages read the same.

- a MAZE of passages (seeded depth-first spanning tree + a few loops), nodes off their centres and
  passages bowed, as level 1's plan v3;
- the CAMP at the way in (south; the way out faces the camera, which looks north);
- the CURSED KING's throne hall in the north, entered by ONE passage closed by a LEVER DOOR: it
  opens once the hero has pulled the levers in the two LEVER halls, far from the camp and from
  each other (the user's chosen new mechanic);
- the WATER SERPENT's hall on the main CANAL, one way in;
- CANALS of water across the halls, and FLOODED passages the heroes wade (the user, 2026-11-03:
  «больше воды, пусть по некоторым проходам игрок ходит по воде»): every passage near a canal or
  inside a flooded quarter runs under shallow water; a dry passage crosses a canal on a BRIDGE, a
  flooded one wades through it (a ford);
- halls are LIVING SHAPES, never rectangles (the user, again): each is a union of overlapping blobs
  round its cells, corners left out at random -- the json carries the blobs for the 3D build;
- the great flooded hall and two ruined forges are monster arenas;
- the farthest dead ends hold crystals, the other dead ends are empty, to mislead; junctions hold
  monster halls.

    python dungeon2_plan.py <out.png> [seed] [layout A-D]

Writes the picture and, next to it, <out>.json for the 3D build to follow.
"""
import json
import random
import sys

from PIL import Image, ImageDraw, ImageFont

G = 21                      # cells a side
SIZE = 300                  # build units (x 0.44 -> ~132 game cells)
CELL = SIZE / G             # ~14.3, level 1's
SEED = int(sys.argv[2]) if len(sys.argv) > 2 else 92
LOOPS = 6
CRYSTAL_ROOMS = 10
MONSTER_HALLS = 10

LAYOUT = sys.argv[3].upper() if len(sys.argv) > 3 else "A"
# halls: name -> (x0, y0, w, h) in cells; y = 0 is the south edge, where the way in is. LAYOUTS
# (2026-11-04, the user: «добавить разные вариации карт», as level 1's A-D): the camp always south
# by the way out; the throne, the serpent's pool, the levers, the forges and the canals elsewhere.
# The hall NAMES stay (lever_w is just "the first lever"), the 3D build keys on them.
# A = the approved plan (seed 92), reproduced exactly.
LAYOUTS = {
    "A": dict(
        big={
            "camp": (8, 0, 5, 2),
            "throne": (8, 17, 5, 4),       # the cursed king: one way in, through the lever door
            "serpent": (16, 8, 5, 5),      # the serpent's pool on the main canal: one way in
            "great": (8, 8, 5, 4),         # the great flooded hall, pillars standing in the water
            "lever_w": (0, 16, 3, 3),      # the levers: far from the camp and from each other
            "lever_e": (18, 1, 3, 3),
            "forge_w": (1, 6, 3, 3),       # ruined forges: monster arenas
            "forge_n": (14, 15, 3, 3),
        },
        # the canals: polylines through cell coordinates (centres); the main one runs from the west
        # wall through the great hall into the serpent's pool, a second one falls from the north-west
        canals=[
            [(-0.5, 10.0), (3.0, 10.4), (6.0, 9.6), (10.5, 9.9), (14.0, 10.6), (18.5, 10.5)],
            [(5.0, 20.5), (5.6, 17.0), (4.8, 14.0), (6.0, 11.5), (8.5, 10.0)],
            # out of the serpent's pool northwards past the north forge to the north wall
            [(18.5, 11.5), (19.2, 14.0), (17.2, 16.0), (16.0, 18.5), (16.4, 20.5)],
            # out of the great hall southwards, spreading towards the east lever
            [(10.5, 8.5), (11.4, 6.2), (13.6, 4.6), (16.4, 3.8)],
        ],
        # flooded quarters (cells: x, y, radius): every passage in them runs under shallow water
        floods=[(4.0, 12.5, 3.0), (14.0, 5.0, 2.6), (17.0, 15.0, 2.4), (2.5, 3.0, 2.2)],
    ),
    # B: the king in the WEST, the serpent's pool in the north-east corner, the great hall east of middle
    "B": dict(
        big={
            "camp": (8, 0, 5, 2),
            "throne": (0, 13, 4, 5),
            "serpent": (15, 16, 5, 5),
            "great": (11, 7, 5, 4),
            "lever_w": (6, 18, 3, 3),
            "lever_e": (17, 2, 3, 3),
            "forge_w": (3, 7, 3, 3),
            "forge_n": (15, 11, 3, 3),
        },
        canals=[
            [(20.5, 18.5), (17.5, 18.0), (15.4, 14.6), (13.6, 11.5), (13.2, 8.5), (10.0, 5.6), (5.0, 4.6), (-0.5, 5.2)],
            [(8.5, 20.5), (9.2, 16.0), (11.6, 13.2), (13.6, 11.0)],
            [(20.5, 8.8), (17.6, 8.6), (15.2, 8.8)],
        ],
        floods=[(10.0, 14.0, 3.0), (5.0, 4.5, 2.5), (18.0, 8.0, 2.4), (3.0, 19.0, 2.2)],
    ),
    # C: the king in the north-east corner, the serpent's pool on the WEST wall, the main canal
    # falling from the north wall through the great hall into the pool
    "C": dict(
        big={
            "camp": (8, 0, 5, 2),
            "throne": (16, 16, 5, 5),
            "serpent": (0, 9, 5, 5),
            "great": (9, 10, 5, 4),
            "lever_w": (1, 18, 3, 3),
            "lever_e": (18, 4, 3, 3),
            "forge_w": (4, 3, 3, 3),
            "forge_n": (11, 16, 3, 3),
        },
        canals=[
            [(10.6, 20.5), (10.4, 17.0), (11.4, 12.0), (8.0, 11.2), (4.5, 11.5), (-0.5, 11.0)],
            [(20.5, 10.4), (17.0, 9.6), (13.6, 11.2)],
            [(11.0, 10.0), (13.0, 6.6), (16.0, 5.2), (18.6, 5.6)],
        ],
        floods=[(6.0, 16.0, 2.8), (15.0, 7.0, 2.5), (2.5, 3.0, 2.2), (17.5, 13.0, 2.0)],
    ),
    # D: the king in the north-WEST corner, the serpent's pool on the west wall low down, the great
    # hall east; the levers in the north-east and south-east corners
    "D": dict(
        big={
            "camp": (8, 0, 5, 2),
            "throne": (2, 16, 5, 5),
            "serpent": (0, 5, 5, 5),
            "great": (12, 9, 5, 4),
            "lever_w": (18, 17, 3, 3),
            "lever_e": (17, 2, 3, 3),
            "forge_w": (7, 10, 3, 3),
            "forge_n": (10, 16, 3, 3),
        },
        canals=[
            [(-0.5, 7.4), (2.5, 7.5), (6.0, 8.2), (10.0, 9.6), (14.5, 11.0), (20.5, 12.2)],
            [(14.0, 20.5), (14.6, 16.0), (14.5, 11.5)],
            [(14.0, 9.2), (14.6, 5.6), (12.6, 3.0)],
        ],
        floods=[(8.0, 6.0, 2.5), (17.0, 14.5, 2.4), (5.0, 13.0, 2.2), (19.0, 6.0, 2.0)],
    ),
}
BIG = LAYOUTS[LAYOUT]["big"]
ONE_WAY = {"throne", "serpent"}
DOOR = "throne"                    # its one passage is the lever door
LEVERS = ("lever_w", "lever_e")
ARENAS = {"great", "forge_w", "forge_n"}
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
    blobs = hall_blobs(rnd, links)
    return links, crystal_rooms, monster_halls, dead_ends, dist, pos, bends, door, bridges, fords, flooded, blobs


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


def draw(path, links, crystal_rooms, monster_halls, dead_ends, dist, pos, bends, door, bridges, fords, flooded, blobs):
    px = 4
    pad = 40
    side = round(SIZE * px)
    img = Image.new("RGB", (side + pad * 2 + 380, side + pad * 2), (52, 54, 58))
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
        d.line([(x, pad), (x, pad + side)], fill=(64, 66, 70), width=1)
        d.line([(pad, x), (pad + side, x)], fill=(64, 66, 70), width=1)
    floor = (150, 150, 140)
    water = (60, 130, 200)
    for canal in canal_metres():               # canals ~7 m wide, under everything
        d.line([m(p) for p in canal], fill=water, width=7 * px, joint="curve")
    shallow = (95, 160, 205)
    wet_nodes = set()
    for (a, b), bend, wet in zip(links, bends, flooded):
        d.line([m(p) for p in curve(pos[a], pos[b], bend)], fill=shallow if wet else floor, width=4 * px, joint="curve")
        if wet:
            wet_nodes.update((a, b))
    for c in set(sum(([a, b] for a, b in links), [])):
        if hall_of(c) is None:
            x, y = m(pos[c])
            d.ellipse([x - 2 * px, y - 2 * px, x + 2 * px, y + 2 * px], fill=shallow if c in wet_nodes else floor)
    colours = {"camp": (214, 190, 120), "throne": (170, 110, 220), "serpent": (60, 170, 200), "great": (230, 80, 60),
               "lever_w": (240, 200, 60), "lever_e": (240, 200, 60), "forge_w": (230, 80, 60), "forge_n": (230, 80, 60)}
    labels = {"camp": "ЛАГЕРЬ\nнайм, лавка", "throne": "БОСС 1\nПроклятый\nкороль", "serpent": "БОСС 2\nВодный змей",
              "great": "ЗАТОПЛЕННЫЙ\nЗАЛ", "lever_w": "РЫЧАГ", "lever_e": "РЫЧАГ", "forge_w": "КУЗНЯ", "forge_n": "КУЗНЯ"}
    for name, (x0, y0, w, h) in BIG.items():
        fill = shallow if name in ("serpent", "great") else floor
        for bx_, by_, r in blobs[name]:         # the rim: every blob a little bigger, in the hall's colour
            x, y = m((bx_, by_))
            rr = (r + 1.3) * px
            d.ellipse([x - rr, y - rr, x + rr, y + rr], fill=colours[name])
        for bx_, by_, r in blobs[name]:         # then the floor over them: only the union's rim stays
            x, y = m((bx_, by_))
            d.ellipse([x - r * px, y - r * px, x + r * px, y + r * px], fill=fill)
        cx, cy = centre((x0 + (w - 1) / 2, y0 + (h - 1) / 2))
        d.multiline_text((cx, cy), labels[name], fill=(255, 255, 255), font=font, anchor="mm", align="center")
    for bx_, by_, a, b in fords:               # fords: the flooded passage wades the canal
        x, y = m((bx_, by_))
        d.ellipse([x - 3 * px, y - 3 * px, x + 3 * px, y + 3 * px], outline=(220, 240, 255), width=3)
    for bx_, by_, a, b in bridges:             # bridges: brown planks across the canal
        x, y = m((bx_, by_))
        d.rectangle([x - 5 * px, y - 3 * px, x + 5 * px, y + 3 * px], fill=(150, 100, 60), outline=(90, 60, 30), width=2)
    if door:                                    # the lever door: a red bar with a lock
        x, y = m(((pos[door[0]][0] + pos[door[1]][0]) / 2, (pos[door[0]][1] + pos[door[1]][1]) / 2))
        d.rectangle([x - 6 * px, y - 2 * px, x + 6 * px, y + 2 * px], fill=(200, 40, 40), outline=(255, 220, 120), width=2)
        d.text((x, y - 6 * px), "🔒", fill=(255, 220, 120), font=icons, anchor="mm")
    for c in dead_ends:
        if c not in crystal_rooms:
            x, y = m(pos[c])
            d.line([(x - 8, y - 8), (x + 8, y + 8)], fill=(110, 110, 100), width=3)
            d.line([(x - 8, y + 8), (x + 8, y - 8)], fill=(110, 110, 100), width=3)
    for c in crystal_rooms:
        x, y = m(pos[c])
        d.rounded_rectangle([x - 4 * px, y - 4 * px, x + 4 * px, y + 4 * px], radius=2 * px, fill=floor, outline=(190, 110, 255), width=4)
        d.polygon([(x, y - 14), (x + 9, y), (x, y + 14), (x - 9, y)], fill=(190, 110, 255))
    for c in monster_halls:
        x, y = m(pos[c])
        d.ellipse([x - 4 * px, y - 4 * px, x + 4 * px, y + 4 * px], fill=floor, outline=(230, 80, 60), width=4)
        d.text((x, y), "☠", fill=(230, 80, 60), font=icons, anchor="mm")
    x0, y0, w, h = BIG["camp"]
    ex, ey = centre((x0 + (w - 1) / 2, -0.4))
    d.polygon([(ex - 14, ey - 16), (ex + 14, ey - 16), (ex, ey + 4)], fill=(255, 230, 120))
    lx = pad * 2 + side
    rows = [((190, 110, 255), "комната с кристаллами"), ((110, 110, 100), "пустой тупик (обманка)"),
            ((230, 80, 60), "монстры / кузни / затопленный зал"), ((240, 200, 60), "зал с рычагом (2 шт.)"),
            ((200, 40, 40), "дверь к королю: открывают оба рычага"), (water, "каналы (вода)"),
            ((150, 100, 60), "мост через канал"), (shallow, "затопленный проход: идут по воде"),
            ((220, 240, 255), "брод через канал"), ((214, 190, 120), "лагерь: гномы, лавка"),
            ((170, 110, 220), "логово босса: один вход")]
    for k, (col, text) in enumerate(rows):
        y = pad + 20 + k * 38
        d.rectangle([lx, y, lx + 24, y + 24], fill=col)
        d.text((lx + 34, y + 12), text, fill=(240, 240, 240), font=small, anchor="lm")
    empty = len([c for c in dead_ends if c not in crystal_rooms])
    lever_far = [round(dist.get(l, 0)) for l in LEVERS]
    d.multiline_text((lx, pad + 20 + len(rows) * 38 + 20),
                     f"размер: 300 × 300 (≈132 × 132 клетки)\nкомнат с кристаллами: {len(crystal_rooms)}\n"
                     f"пустых тупиков: {empty}\nзалов с монстрами: {len(monster_halls) + len(ARENAS)}\n"
                     f"мостов: {len(bridges)}, бродов: {len(fords)}\n"
                     f"проходов по воде: {sum(flooded)} из {len(flooded)} ({round(100 * sum(flooded) / len(flooded))}%)\nпуть до рычагов: ~{lever_far[0]} и ~{lever_far[1]} м\n"
                     f"путь до короля: ~{round(dist.get('throne', 0))} м\nпуть до змея: ~{round(dist.get('serpent', 0))} м",
                     fill=(240, 240, 240), font=small, spacing=8)
    img.save(path)


def main():
    out = sys.argv[1]
    links, crystal_rooms, monster_halls, dead_ends, dist, pos, bends, door, bridges, fords, flooded, blobs = build(SEED)
    draw(out, links, crystal_rooms, monster_halls, dead_ends, dist, pos, bends, door, bridges, fords, flooded, blobs)
    with open(out.rsplit(".", 1)[0] + ".json", "w", encoding="utf-8") as f:
        json.dump({"cell": CELL, "grid": G, "size": SIZE, "big": BIG, "links": links, "crystal_rooms": crystal_rooms,
                   "monster_halls": monster_halls, "dead_ends": dead_ends, "door": door, "levers": list(LEVERS),
                   "canals": canal_metres(), "bridges": [[x, y] for x, y, _, _ in bridges],
                   "fords": [[x, y] for x, y, _, _ in fords], "flooded": flooded, "flood_zones": FLOOD_ZONES,
                   "hall_blobs": blobs,
                   "pos": [[c[0], c[1], p[0], p[1]] for c, p in pos.items()], "bends": bends}, f)
    print("PLAN2 layout", LAYOUT, "seed", SEED, "links", len(links), "crystal", len(crystal_rooms), "dead", len(dead_ends),
          "bridges", len(bridges), "fords", len(fords), "wet", sum(flooded), "of", len(flooded), "levers", [round(dist.get(l, 0)) for l in LEVERS],
          "throne", round(dist.get("throne", 0)), "serpent", round(dist.get("serpent", 0)))


main()
