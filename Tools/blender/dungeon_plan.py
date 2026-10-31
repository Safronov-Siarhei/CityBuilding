"""The PLAN of a dungeon labyrinth, drawn before any 3D (the user, 2026-10-31: «first sketch the
labyrinth's scheme, then build it»; «too simple, make it harder»). 200 x 200 m on a grid of 10 m
cells:

- a MAZE of passages (a seeded depth-first spanning tree, so there are real dead ends and wrong
  turns, plus a few extra links so it is not one long tree);
- halls of several cells: the CAMP at the way in (south), the LAKE hall in the middle, the two
  BOSS lairs in the far corners, each with ONE way in from its farthest side, monster arenas;
- the FARTHEST dead ends are crystal rooms; the other dead ends are empty, to mislead;
- junctions hold MONSTER halls.

    python dungeon_plan.py <out.png> [seed]

Writes the picture and, next to it, <out>.json (cells, links, rooms) for the 3D build to follow.
"""
import json
import random
import sys

from PIL import Image, ImageDraw, ImageFont

G = 20                      # cells a side
CELL = 10                   # metres
SEED = int(sys.argv[2]) if len(sys.argv) > 2 else 7
LOOPS = 6                   # extra links: few, so most wrong turns stay wrong
CRYSTAL_ROOMS = 8           # the farthest dead ends hold crystals; the rest are empty, to mislead

# halls: name -> (x0, y0, w, h) in cells; y = 0 is the south edge, where the way in is
BIG = {
    "camp": (8, 0, 4, 2),
    "lake": (8, 8, 4, 4),
    "golem": (0, 15, 5, 5),
    "spiders": (15, 15, 5, 5),
    "west": (2, 6, 3, 3),
    "east": (15, 5, 3, 3),
    "deep": (9, 16, 2, 2),
}
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
    monster_halls = junctions[:10]
    return links, crystal_rooms, monster_halls, dead_ends, dist


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


def draw(path, links, crystal_rooms, monster_halls, dead_ends, dist):
    px = 6                                   # pixels per metre
    pad = 40
    side = G * CELL * px
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

    floor = (120, 170, 80)
    for k in range(0, G + 1, 2):             # the scale: a line every 20 m
        x = pad + k * CELL * px
        d.line([(x, pad), (x, pad + side)], fill=(72, 66, 60), width=1)
        d.line([(pad, x), (pad + side, x)], fill=(72, 66, 60), width=1)
    for a, b in links:                       # passages, ~4 m wide
        d.line([centre(a), centre(b)], fill=floor, width=4 * px)
    for c in set(sum(([a, b] for a, b in links), [])):
        if hall_of(c) is None:
            x, y = centre(c)
            d.ellipse([x - 2 * px, y - 2 * px, x + 2 * px, y + 2 * px], fill=floor)
    colours = {"camp": (214, 190, 120), "lake": (90, 170, 220), "golem": (150, 120, 200), "spiders": (200, 110, 110),
               "west": (230, 80, 60), "east": (230, 80, 60), "deep": (230, 80, 60)}
    labels = {"camp": "ЛАГЕРЬ\nнайм, лавка", "lake": "ЗАЛ\nС ОЗЕРОМ", "golem": "БОСС 2\nРудный голем",
              "spiders": "БОСС 1\nКоролева\nпауков", "west": "ЗАЛ\nМОНСТРОВ", "east": "ЗАЛ\nМОНСТРОВ", "deep": "☠"}
    for name, (x0, y0, w, h) in BIG.items():
        ax, ay = centre((x0, y0 + h - 1))
        bx, by = centre((x0 + w - 1, y0))
        m = 3.5 * px
        d.rounded_rectangle([ax - m, ay - m, bx + m, by + m], radius=4 * px, fill=floor, outline=colours[name], width=5)
        d.multiline_text(((ax + bx) / 2, (ay + by) / 2), labels[name], fill=(255, 255, 255),
                         font=icons if name == "deep" else font, anchor="mm", align="center")
    for c in dead_ends:                      # empty dead ends: wrong turns
        if c not in crystal_rooms:
            x, y = centre(c)
            d.line([(x - 9, y - 9), (x + 9, y + 9)], fill=(90, 120, 60), width=3)
            d.line([(x - 9, y + 9), (x + 9, y - 9)], fill=(90, 120, 60), width=3)
    for c in crystal_rooms:
        x, y = centre(c)
        d.rounded_rectangle([x - 4 * px, y - 4 * px, x + 4 * px, y + 4 * px], radius=2 * px, fill=floor,
                            outline=(190, 110, 255), width=4)
        d.polygon([(x, y - 16), (x + 10, y), (x, y + 16), (x - 10, y)], fill=(190, 110, 255))
    for c in monster_halls:
        x, y = centre(c)
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
            ((72, 66, 60), "линии сетки: каждые 20 м")]
    for k, (col, text) in enumerate(rows):
        y = pad + 20 + k * 40
        d.rectangle([lx, y, lx + 24, y + 24], fill=col)
        d.text((lx + 34, y + 12), text, fill=(240, 240, 240), font=small, anchor="lm")
    empty = len([c for c in dead_ends if c not in crystal_rooms])
    d.multiline_text((lx, pad + 20 + len(rows) * 40 + 20),
                     f"размер: 200 × 200 м\nкомнат с кристаллами: {len(crystal_rooms)}\n"
                     f"пустых тупиков: {empty}\nзалов с монстрами: {len(monster_halls) + len(ARENAS)}\n"
                     f"путь до босса 1: ~{dist.get('spiders', 0)} м\nпуть до босса 2: ~{dist.get('golem', 0)} м",
                     fill=(240, 240, 240), font=small, spacing=8)
    img.save(path)


def main():
    out = sys.argv[1]
    links, crystal_rooms, monster_halls, dead_ends, dist = build(SEED)
    draw(out, links, crystal_rooms, monster_halls, dead_ends, dist)
    with open(out.rsplit(".", 1)[0] + ".json", "w", encoding="utf-8") as f:
        json.dump({"cell": CELL, "grid": G, "big": BIG, "links": links, "crystal_rooms": crystal_rooms,
                   "monster_halls": monster_halls, "dead_ends": dead_ends}, f)
    print("PLAN seed", SEED, "links", len(links), "crystal rooms", len(crystal_rooms), "dead ends", len(dead_ends),
          "to spiders", dist.get("spiders"), "to golem", dist.get("golem"))


main()
