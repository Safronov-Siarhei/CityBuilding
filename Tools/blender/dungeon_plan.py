"""The PLAN of a dungeon labyrinth, drawn before any 3D (the user, 2026-10-31: «first sketch the
labyrinth's scheme, then build it»; «too simple, make it harder»). 200 x 200 m on a grid of 10 m
cells:

- a MAZE of passages (a seeded depth-first spanning tree, so there are real dead ends and wrong
  turns, plus a few extra links so it is not one long tree);
- halls of several cells: the CAMP at the way in (south), the LAKE hall in the middle, the two
  BOSS lairs in the far corners, each with ONE way in from its farthest side, monster arenas;
- the FARTHEST dead ends are crystal rooms; the other dead ends are empty, to mislead;
- junctions hold MONSTER halls.

Plan v3 (the user, 2026-10-31: «the right angles and cubic rooms -- give it a living shape; fewer
passages but more detail»): a coarser 14 x 14 grid (~14 m cells), every cell's node nudged off
its centre and every passage bent, so nothing runs on a ruler.

    python dungeon_plan.py <out.png> [seed]

Writes the picture and, next to it, <out>.json (cells, links, rooms) for the 3D build to follow.
"""
import json
import random
import sys

from PIL import Image, ImageDraw, ImageFont

G = 14                      # cells a side
CELL = 200 / G              # metres (~14.3)
SEED = int(sys.argv[2]) if len(sys.argv) > 2 else 7
LOOPS = 4                   # extra links: few, so most wrong turns stay wrong
CRYSTAL_ROOMS = 8           # the farthest dead ends hold crystals; the rest are empty, to mislead

# halls: name -> (x0, y0, w, h) in cells; y = 0 is the south edge, where the way in is
BIG = {
    "camp": (5, 0, 4, 2),
    "lake": (5, 5, 4, 4),
    "golem": (0, 10, 4, 4),
    "spiders": (10, 10, 4, 4),
    "west": (1, 4, 2, 2),
    "east": (11, 4, 2, 2),
    "deep": (6, 11, 2, 2),
}
JITTER = 0.32               # how far a passage's node strays from its cell's centre (of a cell)
BEND = 0.35                 # how far a passage bows sideways (of its length)
ONE_WAY = {"golem", "spiders"}     # a boss lair is entered by one passage only
ARENAS = {"west", "east", "deep"}  # monster halls of their own


def hall_of(c):
    for name, (x0, y0, w, h) in BIG.items():
        if x0 <= c[0] < x0 + w and y0 <= c[1] < y0 + h:
            return name
    return None


def node(c):
    """A big hall is one node of the maze; every other cell is its own."""
    h = hall_of(c)
    return h if h else c


def build(seed):
    rnd = random.Random(seed)
    cells = [(x, y) for x in range(G) for y in range(G)]
    nbrs = {}
    for x, y in cells:
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            n = (x + dx, y + dy)
            if 0 <= n[0] < G and 0 <= n[1] < G and node(n) != node((x, y)):
                nbrs.setdefault(node((x, y)), set()).add((node(n), (x, y), n))
    # depth-first spanning tree from the camp: long winding corridors, many dead ends
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
    # each boss lair hangs on ONE link, from the cell beside it farthest from the camp
    dist = distances(links)
    for boss in sorted(ONE_WAY):
        best = max(sorted(nbrs[boss], key=str), key=lambda t: dist.get(t[0], -1))
        links.append((best[2], best[1]))
    # a few loops, never into a boss lair
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
    for c in dead_ends:                       # the farthest ones, spread out
        if len(crystal_rooms) >= CRYSTAL_ROOMS:
            break
        if all(abs(c[0] - o[0]) + abs(c[1] - o[1]) > 4 for o in crystal_rooms):
            crystal_rooms.append(c)
    junctions = [c for c in degree if isinstance(c, tuple) and degree[c] >= 3 and c not in crystal_rooms]
    junctions.sort(key=str)
    rnd.shuffle(junctions)
    monster_halls = junctions[:7]
    # the living shape: nodes off their centres, passages bowed
    pos = {}
    for x, y in cells:
        j = 0.0 if hall_of((x, y)) else JITTER * CELL
        pos[(x, y)] = ((x + 0.5) * CELL + rnd.uniform(-j, j), (y + 0.5) * CELL + rnd.uniform(-j, j))
    bends = [rnd.uniform(-BEND, BEND) for _ in links]
    return links, crystal_rooms, monster_halls, dead_ends, dist, pos, bends


def distances(links):
    """Metres walked from the camp to every node, along the links."""
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


def curve(pa, pb, bend, steps=6):
    """A passage from pa to pb, bowed sideways by `bend` of its length (a quadratic bezier)."""
    mx, my = (pa[0] + pb[0]) / 2, (pa[1] + pb[1]) / 2
    cx, cy = mx - (pb[1] - pa[1]) * bend, my + (pb[0] - pa[0]) * bend
    out = []
    for k in range(steps + 1):
        t = k / steps
        out.append(((1 - t) ** 2 * pa[0] + 2 * (1 - t) * t * cx + t * t * pb[0],
                    (1 - t) ** 2 * pa[1] + 2 * (1 - t) * t * cy + t * t * pb[1]))
    return out


def draw(path, links, crystal_rooms, monster_halls, dead_ends, dist, pos, bends):
    px = 6                                   # pixels per metre
    pad = 40
    side = round(G * CELL * px)
    img = Image.new("RGB", (side + pad * 2 + 360, side + pad * 2), (58, 52, 48))
    d = ImageDraw.Draw(img)
    try:
        font = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 20)
        small = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 17)
        icons = ImageFont.truetype("C:/Windows/Fonts/seguisym.ttf", 22)
    except OSError:
        font = small = icons = ImageFont.load_default()

    def centre(c):
        return pad + (c[0] + 0.5) * CELL * px, pad + (G - 1 - c[1] + 0.5) * CELL * px

    def at(c):
        return pad + pos[c][0] * px, pad + (G * CELL - pos[c][1]) * px

    floor = (120, 170, 80)
    for k in range(0, G + 1, 2):             # the scale: a line every 20 m
        x = pad + k * CELL * px
        d.line([(x, pad), (x, pad + side)], fill=(72, 66, 60), width=1)
        d.line([(pad, x), (pad + side, x)], fill=(72, 66, 60), width=1)
    for (a, b), bend in zip(links, bends):   # passages, ~4 m wide
        d.line(curve(at(a), at(b), bend), fill=floor, width=4 * px, joint="curve")
    for c in set(sum(([a, b] for a, b in links), [])):
        if hall_of(c) is None:
            x, y = at(c)
            d.ellipse([x - 2 * px, y - 2 * px, x + 2 * px, y + 2 * px], fill=floor)
    colours = {"camp": (214, 190, 120), "lake": (90, 170, 220), "golem": (150, 120, 200), "spiders": (200, 110, 110),
               "west": (230, 80, 60), "east": (230, 80, 60), "deep": (230, 80, 60)}
    labels = {"camp": "ЛАГЕРЬ\nнайм, лавка", "lake": "ЗАЛ\nС ОЗЕРОМ", "golem": "БОСС 2\nРудный голем",
              "spiders": "БОСС 1\nКоролева\nпауков", "west": "ЗАЛ\nМОНСТРОВ", "east": "ЗАЛ\nМОНСТРОВ", "deep": "☠"}
    for name, (x0, y0, w, h) in BIG.items():
        ax, ay = centre((x0, y0 + h - 1))
        bx, by = centre((x0 + w - 1, y0))
        m = 3.5 * px
        d.rounded_rectangle([ax - m, ay - m, bx + m, by + m], radius=9 * px, fill=floor, outline=colours[name], width=5)
        d.multiline_text(((ax + bx) / 2, (ay + by) / 2), labels[name], fill=(255, 255, 255),
                         font=icons if name == "deep" else font, anchor="mm", align="center")
    for c in dead_ends:                      # empty dead ends: wrong turns
        if c not in crystal_rooms:
            x, y = at(c)
            d.line([(x - 9, y - 9), (x + 9, y + 9)], fill=(90, 120, 60), width=3)
            d.line([(x - 9, y + 9), (x + 9, y - 9)], fill=(90, 120, 60), width=3)
    for c in crystal_rooms:
        x, y = at(c)
        d.rounded_rectangle([x - 4 * px, y - 4 * px, x + 4 * px, y + 4 * px], radius=2 * px, fill=floor,
                            outline=(190, 110, 255), width=4)
        d.polygon([(x, y - 16), (x + 10, y), (x, y + 16), (x - 10, y)], fill=(190, 110, 255))
    for c in monster_halls:
        x, y = at(c)
        d.ellipse([x - 4 * px, y - 4 * px, x + 4 * px, y + 4 * px], fill=floor, outline=(230, 80, 60), width=4)
        d.text((x, y), "☠", fill=(230, 80, 60), font=icons, anchor="mm")
    x0, y0, w, h = BIG["camp"]
    ex, ey = centre((x0 + (w - 1) / 2, -0.4))  # the way in
    d.polygon([(ex - 14, ey - 16), (ex + 14, ey - 16), (ex, ey + 4)], fill=(255, 230, 120))
    # legend
    lx = pad * 2 + side
    rows = [((190, 110, 255), "комната с кристаллами"), ((90, 120, 60), "пустой тупик (обманка)"),
            ((230, 80, 60), "монстры"), ((214, 190, 120), "лагерь: найм гномов, лавка"),
            ((150, 120, 200), "логово босса: один вход"), (floor, "проходы ~4 м"),
            ((72, 66, 60), "линии сетки: каждые ~28 м")]
    for k, (col, text) in enumerate(rows):
        y = pad + 20 + k * 40
        d.rectangle([lx, y, lx + 24, y + 24], fill=col)
        d.text((lx + 34, y + 12), text, fill=(240, 240, 240), font=small, anchor="lm")
    empty = len([c for c in dead_ends if c not in crystal_rooms])
    d.multiline_text((lx, pad + 20 + len(rows) * 40 + 20),
                     f"размер: 200 × 200 м\nкомнат с кристаллами: {len(crystal_rooms)}\n"
                     f"пустых тупиков: {empty}\nзалов с монстрами: {len(monster_halls) + len(ARENAS)}\n"
                     f"путь до босса 1: ~{round(dist.get("spiders", 0))} м\nпуть до босса 2: ~{round(dist.get("golem", 0))} м",
                     fill=(240, 240, 240), font=small, spacing=8)
    img.save(path)


def main():
    out = sys.argv[1]
    links, crystal_rooms, monster_halls, dead_ends, dist, pos, bends = build(SEED)
    draw(out, links, crystal_rooms, monster_halls, dead_ends, dist, pos, bends)
    with open(out.rsplit(".", 1)[0] + ".json", "w", encoding="utf-8") as f:
        json.dump({"cell": CELL, "grid": G, "big": BIG, "links": links, "crystal_rooms": crystal_rooms,
                   "monster_halls": monster_halls, "dead_ends": dead_ends,
                   "pos": [[c[0], c[1], p[0], p[1]] for c, p in pos.items()], "bends": bends}, f)
    print("PLAN seed", SEED, "links", len(links), "crystal rooms", len(crystal_rooms), "dead ends", len(dead_ends),
          "to spiders", dist.get("spiders"), "to golem", dist.get("golem"))


main()
