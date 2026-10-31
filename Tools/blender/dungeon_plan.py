"""The PLAN of a dungeon labyrinth, drawn before any 3D (the user, 2026-10-31: «first sketch the
labyrinth's scheme, then build it»). 200 x 200 m on a coarse grid of 20 m cells:

- a MAZE of passages (a seeded depth-first spanning tree, so there are real dead ends and wrong
  turns, plus a few extra links so it is not one long tree);
- special halls that span 2 x 2 cells: the CAMP at the way in (south), the LAKE hall in the
  middle, the two BOSS lairs in the far corners, each with ONE way in;
- every dead end is a small ROOM with upgrade crystals;
- some cells hold MONSTER halls.

    python dungeon_plan.py <out.png> [seed]

Writes the picture and, next to it, <out>.json (cells, links, rooms) for the 3D build to follow.
"""
import json
import random
import sys

from PIL import Image, ImageDraw, ImageFont

G = 10                      # cells a side
CELL = 20                   # metres
SEED = int(sys.argv[2]) if len(sys.argv) > 2 else 7

# 2 x 2 halls: name -> (x0, y0); y = 0 is the south edge, where the way in is
BIG = {
    "camp": (4, 0),
    "lake": (4, 4),
    "golem": (0, 8),
    "spiders": (8, 8),
}
ONE_WAY = {"golem", "spiders"}     # a boss lair is entered by one passage only


def hall_of(c):
    for name, (x0, y0) in BIG.items():
        if x0 <= c[0] < x0 + 2 and y0 <= c[1] < y0 + 2:
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
    # depth-first spanning tree from the camp
    seen, links, stack = {"camp"}, [], ["camp"]
    while stack:
        cur = stack[-1]
        options = [(n, a, b) for n, a, b in nbrs[cur] if n not in seen and not (cur in ONE_WAY)]
        if cur in ONE_WAY and cur != "camp":
            options = []
        if not options:
            stack.pop()
            continue
        n, a, b = rnd.choice(sorted(options, key=str))
        seen.add(n)
        links.append((a, b))
        stack.append(n)
    # the boss lairs: the tree never went through them, so each hangs on one link
    for boss in ONE_WAY:
        if boss not in seen:
            n, a, b = rnd.choice(sorted(nbrs[boss], key=str))
            links.append((b, a))
    # a few loops, never into a boss lair, so the maze has more than one way round
    extra = [(a, b) for nd, s in nbrs.items() if nd not in ONE_WAY for n, a, b in s
             if n not in ONE_WAY and node(a) != node(b)]
    rnd.shuffle(extra)
    have = {frozenset((node(a), node(b))) for a, b in links}
    added = 0
    for a, b in extra:
        k = frozenset((node(a), node(b)))
        if k in have or added >= 9:
            continue
        have.add(k)
        links.append((a, b))
        added += 1
    # degrees: dead ends become crystal rooms
    degree = {}
    for a, b in links:
        for c in (node(a), node(b)):
            degree[c] = degree.get(c, 0) + 1
    crystal_rooms = [c for c in degree if isinstance(c, tuple) and degree[c] == 1]
    plain = [c for c in degree if isinstance(c, tuple) and degree[c] >= 2]
    rnd.shuffle(plain)
    monster_halls = plain[:10]
    return links, crystal_rooms, monster_halls


def draw(path, links, crystal_rooms, monster_halls):
    px = 6                                   # pixels per metre
    pad = 40
    size = G * CELL * px + pad * 2 + 330
    img = Image.new("RGB", (size, G * CELL * px + pad * 2), (58, 52, 48))
    d = ImageDraw.Draw(img)
    try:
        font = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 22)
        small = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 17)
        icons = ImageFont.truetype("C:/Windows/Fonts/seguisym.ttf", 30)
    except OSError:
        font = small = icons = ImageFont.load_default()

    def centre(c):
        return pad + (c[0] + 0.5) * CELL * px, pad + (G - 1 - c[1] + 0.5) * CELL * px

    floor = (120, 170, 80)
    # the grid scale
    for k in range(G + 1):
        x = pad + k * CELL * px
        d.line([(x, pad), (x, pad + G * CELL * px)], fill=(80, 74, 68), width=1)
        d.line([(pad, x), (pad + G * CELL * px, x)], fill=(80, 74, 68), width=1)
    for a, b in links:                       # passages
        d.line([centre(a), centre(b)], fill=floor, width=5 * px)
    for c in set(sum(([a, b] for a, b in links), [])):
        if hall_of(c) is None:
            x, y = centre(c)
            d.ellipse([x - 3 * px, y - 3 * px, x + 3 * px, y + 3 * px], fill=floor)
    colours = {"camp": (214, 190, 120), "lake": (90, 170, 220), "golem": (150, 120, 200), "spiders": (200, 110, 110)}
    labels = {"camp": "ЛАГЕРЬ\nнайм, лавка\nвход ↓", "lake": "ЗАЛ С ОЗЕРОМ", "golem": "БОСС 2\nРудный голем",
              "spiders": "БОСС 1\nКоролева пауков"}
    for name, (x0, y0) in BIG.items():
        ax, ay = centre((x0, y0 + 1))
        bx, by = centre((x0 + 1, y0))
        m = 7 * px
        d.rounded_rectangle([ax - m, ay - m, bx + m, by + m], radius=6 * px, fill=floor, outline=colours[name], width=6)
        d.multiline_text(((ax + bx) / 2, (ay + by) / 2), labels[name], fill=(255, 255, 255), font=font, anchor="mm",
                         align="center")
    for c in crystal_rooms:                  # crystal rooms
        x, y = centre(c)
        d.rounded_rectangle([x - 6 * px, y - 6 * px, x + 6 * px, y + 6 * px], radius=2 * px, fill=floor,
                            outline=(190, 110, 255), width=5)
        d.polygon([(x, y - 22), (x + 14, y), (x, y + 22), (x - 14, y)], fill=(190, 110, 255))
    for c in monster_halls:                  # monster halls
        x, y = centre(c)
        d.ellipse([x - 7 * px, y - 7 * px, x + 7 * px, y + 7 * px], fill=floor, outline=(230, 80, 60), width=5)
        d.text((x, y), "☠", fill=(230, 80, 60), font=icons, anchor="mm")
    ex, ey = centre((4.5, -0.5))             # the way in
    d.polygon([(ex - 16, ey - 20), (ex + 16, ey - 20), (ex, ey + 4)], fill=(255, 230, 120))
    # legend
    lx = pad * 2 + G * CELL * px
    rows = [((190, 110, 255), "◆ комната с кристаллами (тупик)"), ((230, 80, 60), "☠ зал с монстрами"),
            ((214, 190, 120), "лагерь: найм гномов, лавка зелий"), ((150, 120, 200), "боссы: один вход"),
            (floor, "проходы ~5 м"), ((200, 200, 200), "клетка сетки = 20 м")]
    for k, (col, text) in enumerate(rows):
        y = pad + 20 + k * 44
        d.rectangle([lx, y, lx + 26, y + 26], fill=col)
        d.text((lx + 36, y + 13), text[2:] if text[1] == " " else text, fill=(240, 240, 240), font=small, anchor="lm")
    d.text((lx, pad + 20 + len(rows) * 44 + 20),
           f"кристальных комнат: {len(crystal_rooms)}\nзалов с монстрами: {len(monster_halls)}\nразмер: 200 × 200 м",
           fill=(240, 240, 240), font=small)
    img.save(path)


def main():
    out = sys.argv[1]
    links, crystal_rooms, monster_halls = build(SEED)
    draw(out, links, crystal_rooms, monster_halls)
    with open(out.rsplit(".", 1)[0] + ".json", "w", encoding="utf-8") as f:
        json.dump({"cell": CELL, "grid": G, "big": BIG, "links": links, "crystal_rooms": crystal_rooms,
                   "monster_halls": monster_halls}, f)
    print("PLAN links", len(links), "crystal rooms", len(crystal_rooms), "monster halls", len(monster_halls))


main()
