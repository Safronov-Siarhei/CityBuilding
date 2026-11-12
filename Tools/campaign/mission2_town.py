"""Mission 2 «Оборона города»: the town and the land of the APPROVED plan v2 (Design/mission2_map_v2.png,
Tools/campaign/mission2_map.py, the user 2026-11-12) as data the game and Blender read.

Writes
  Assets/_Project/Campaign/Mission2Town.json -- every building of the town in the bot templates' format
      ("citybuilder-base/1": id, x, y, w, h, rot, level; x right, y down = south, in PLAN cells 0..W, 0..H),
      placed by Campaign.CityDefenceTown;
  Tools/blender/mission2-plan.json -- the land for Tools/blender/mission2.py: the river and its three fords,
      the forests, the fields under the walls (they burn in part 2), the town's flat pad, the cliff and the
      lake, the capital road, and the spots the mission reads (orc spawns, gates, the square, the castle).

The walls are the game's own square pieces (Fence / Tower / FortifiedGate = the Barbican): the outer ring
level 3 (walked by the archers), the inner level 4.

Run: python -I Tools/campaign/mission2_town.py
"""
import json
import math
import os
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
sys.path.insert(0, os.path.join(ROOT, 'Design'))
from bot_town_common import Town  # noqa: E402

W, H = 100, 92
OUTER = (28, 34, 72, 66)
INNER = (41, 51, 59, 66)
CLIFF_Y = 66
SQUARE = (50, 59)
OUTER_LEVEL, INNER_LEVEL = 3, 4


def river_y(x):
    return 9 + 1.4 * math.sin(x / 9.0)


FORDS = [(14, river_y(14)), (50, river_y(50)), (86, river_y(86))]
# ONE gate in each ring, north (the user's third playtest, 2026-11-12: «замок с одними воротами» -- Ардан can
# help where it is decided); the three roads from the fords all come to it
OUTER_GATES = {"N": (50, 34)}
INNER_GATES = {"N": (50, 51)}

FORESTS = [  # boxes (l, t, r, b), plan cells
    (0, 0, W, 5), (0, 13, W, 19), (0, 19, 12, CLIFF_Y - 1), (88, 19, W, CLIFF_Y - 1),
    (36, 21, 44, 28), (56, 21, 64, 28), (14, 48, 22, 58), (78, 48, 86, 58),
]
FIELDS = [
    (20, 21, 34, 31), (45, 21, 48.5, 31), (51.5, 21, 55, 31), (66, 21, 80, 31),
    (12, 34, 18, 46), (82, 34, 88, 46), (12, 58, 26, 65), (74, 58, 88, 65),
]
PASTURES = [(21, 42, 26, 56), (74, 42, 79, 56)]


def put(town, bid, x, y, level, district, turned=False):
    return town.put(bid, x, y, level, district, turned=turned, inside=False)


def build_town():
    t = Town(2)
    # the bot's kit keeps pieces inside its 55x55 square; this town has its own plan frame
    t.free = lambda cells, inside=True: all(c not in t.taken and 0 <= c[0] < W and 0 <= c[1] < H for c in cells)
    ol, ot, or_, ob = OUTER
    il, it, ir, ib = INNER

    # the castle on the cliff, the square before it
    put(t, "Castle", 48, 61, 5, "citadel")
    put(t, "Square", 49, 58, 4, "street")

    # the walls (gates first, so the ring of pieces goes round them)
    t.wall_rect(il, it, ir, ib, INNER_LEVEL, [("N", INNER_GATES["N"][0])], tower_every=6, district="inner")
    t.wall_rect(ol, ot, or_, ob, OUTER_LEVEL, [("N", OUTER_GATES["N"][0])], tower_every=6, district="wall")

    # streets: a ring inside each wall, a street from each outer gate to its inner gate, a cross street
    def road(x0, y0, x1, y1, level=2):
        for x in range(min(x0, x1), max(x0, x1) + 1):
            for y in range(min(y0, y1), max(y0, y1) + 1):
                put(t, "Road", x, y, level, "street")

    road(ol + 2, ot + 2, ol + 2, ob - 1)
    road(ol + 2, ot + 2, or_ - 2, ot + 2)
    road(or_ - 2, ot + 2, or_ - 2, ob - 1)
    road(50, ot + 1, 50, it - 1)
    road(ol + 2, 47, or_ - 2, 47)
    # the centre: its ring and the way from each inner gate to the square
    road(il + 2, it + 2, il + 2, ib - 1, 4)
    road(il + 2, it + 2, ir - 2, it + 2, 4)
    road(ir - 2, it + 2, ir - 2, ib - 1, 4)
    road(il + 1, 56, ir - 1, 56, 4)
    road(50, it + 1, 50, 57, 4)

    # the centre's buildings
    put(t, "Church", 44, 58, 4, "inner")
    put(t, "Barracks", 53, 58, 4, "inner")
    for x in (44, 47, 51, 54):
        put(t, "Cottage", x, 54, 4, "inner")
    put(t, "HealerHouse", 44, 61, 3, "inner")
    put(t, "Cottage", 54, 61, 4, "inner")
    put(t, "Cottage", 54, 63, 4, "inner")

    # the lower town: houses on a 3-cell grid, yards left here and there
    rnd = t.rng
    for gx in range(ol + 4, or_ - 2, 3):
        for gy in range(ot + 4, ob - 1, 3):
            if il - 3 <= gx + 1 <= ir + 3 and gy + 1 >= it - 3:
                continue
            if rnd.random() < 0.12:
                continue
            bid = "Cottage" if rnd.random() < 0.55 else "Hovel"
            put(t, bid, gx, gy, 2 if bid == "Cottage" else 1, "living")
    # wells and trees in the yards
    for gx in range(ol + 3, or_ - 2, 5):
        for gy in range(ot + 3, ob - 1, 5):
            put(t, "Well" if rnd.random() < 0.3 else "DecTree", gx, gy, 1, "decor")

    # barns by the fields (props; the fields themselves are the ground's colour)
    for x, y in ((29, 25), (70, 25), (14, 61), (85, 61)):
        put(t, "Granary", x, y, 2, "fields")

    # no plain towers on the walls: only the gates' own (the user's second playtest, 2026-11-12)
    for it in t.items:
        if it["id"] == "Tower":
            it["id"] = "Fence"

    # the farmsteads outside the walls the orcs went through (the user: «пожары, руины -- видно, что
    # нашествие»): every one a ruin, some still burning
    for i, (x, y) in enumerate(FARMSTEADS):
        bid = "Cottage" if i % 3 == 0 else "Hovel"
        if put(t, bid, x, y, 1, "farmstead"):
            t.items[-1]["state"] = "ruin"
    return t


# farmsteads under the walls (plan cells, top-left), all ruined
FARMSTEADS = [(22, 32), (31, 22), (41, 29), (58, 29), (68, 22), (77, 32), (9, 40), (89, 40),
              (24, 44), (75, 44), (20, 60), (78, 60), (35, 24), (63, 25)]
# fields that burnt (index into FIELDS): furrows of ash, smoke over them
BURNT_FIELDS = (0, 3, 5, 6)
# where it still burns: the ruined farmsteads' middles and spots in the burnt fields
FIRES = [(23, 33), (42, 30), (69, 23), (90, 41), (25, 45), (79, 61), (24, 26), (72, 27), (84, 39), (16, 62)]
DECOR_KINDS = ("CineHaystack", "CineHayBales", "CineHaystack", "CineCartBody")


def decor(town):
    """Props in the town's free yards and by the fields (the user: «в самом городе не хватает декораций, добавь
    стоги сена и другое»): haystacks, hay bales, a cart -- looks only, nothing walks round them."""
    rnd = town.rng
    out = []
    ol, ot, or_, ob = OUTER
    il, it, ir, ib = INNER
    near_road = set()
    for item in town.items:
        if item["id"] == "Road":
            near_road.add((item["x"], item["y"]))
    for y in range(ot + 2, ob - 1):
        for x in range(ol + 2, or_ - 1):
            if (x, y) in town.taken or (il - 1 <= x <= ir + 1 and y >= it - 1):
                continue
            # a free cell with free cells round it (a yard), not on a street
            if any((x + dx, y + dy) in town.taken for dx in (-1, 0, 1) for dy in (-1, 0, 1)):
                continue
            if rnd.random() < 0.45:
                out.append({"id": rnd.choice(DECOR_KINDS), "x": x + 0.5, "y": y + 0.5, "rot": rnd.randrange(360)})
    # by the fields: haystacks
    for (l, tp, r, b) in FIELDS:
        for _ in range(2):
            out.append({"id": "CineHaystack", "x": round(rnd.uniform(l, r), 2), "y": round(b + 0.8, 2), "rot": rnd.randrange(360)})
    return out


def spots():
    s = []
    for i, (fx, fy) in enumerate(FORDS):
        s.append({"kind": "spawn", "x": fx, "y": fy - 6.0, "lane": i})     # beyond the river, in the far forest
        s.append({"kind": "ford", "x": fx, "y": fy, "lane": i})
    x, y = OUTER_GATES["N"]
    s.append({"kind": "outer_gate", "x": x + 0.5, "y": y + 0.5})
    x, y = INNER_GATES["N"]
    s.append({"kind": "inner_gate", "x": x + 0.5, "y": y + 0.5})
    # the ways from each ford to the one gate (the lane's corners), plan cells: the side roads skirt the
    # fields and meet the middle road before the gate
    gate_front = (50.5, OUTER_GATES["N"][1] - 2.4)
    ways = [
        [FORDS[0], (16, 22), (24, 32.5), (44, 32.5), gate_front],
        [FORDS[1], (50.5, 20), gate_front],
        [FORDS[2], (84, 22), (76, 32.5), (57, 32.5), gate_front],
    ]
    s.append({"kind": "square", "x": SQUARE[0] + 0.5, "y": SQUARE[1] + 0.5})
    s.append({"kind": "castle", "x": 50.5, "y": 63.5})
    s.append({"kind": "capital", "x": 50.5, "y": H - 2.0})
    return s, ways


def main():
    town = build_town()
    s, ways = spots()
    # the orcs' three lanes (W, N, E), plan points: out of the far forest, over the ford, the plan's way
    # to the outer gate, along the streets to the inner gate, on to the square
    inside = [[(50.5, 34.5), (50.5, 51.5), (50.5, 57.5)]] * 3
    lanes = []
    for i, way in enumerate(ways):
        fx, fy = FORDS[i]
        pts = [(fx, fy - 6.0)] + [tuple(p) for p in way] + inside[i]
        lanes.append([[round(x, 2), round(y, 2)] for x, y in pts])
    blueprint = {"format": "citybuilder-base/1", "name": "Mission 2 town", "width": W, "height": H,
                 "origin": [-50, 46],
                 "lanes": [{"points": [v for xy in l for v in xy]} for l in lanes],
                 "fields": [{"box": list(f), "burnt": i in BURNT_FIELDS} for i, f in enumerate(FIELDS)],
                 "fires": [{"x": x + 0.5, "y": y + 0.5} for x, y in FIRES],
                 "decor": decor(town),
                 "buildings": [{k: v for k, v in it.items() if k != "district"} for it in town.items]}
    out = os.path.join(ROOT, 'Assets', '_Project', 'Campaign', 'Mission2Town.json')
    with open(out, 'w', encoding='utf-8') as f:
        json.dump(blueprint, f, ensure_ascii=False, separators=(',', ':'))
    plan = {"size": [W, H], "outer": OUTER, "inner": INNER, "cliff_y": CLIFF_Y,
            "river": [[x, river_y(x)] for x in range(-10, W + 11, 2)], "fords": FORDS,
            "forests": FORESTS, "fields": FIELDS, "pastures": PASTURES,
            "road_south": [50, CLIFF_Y, 50, H], "spots": s, "ways": ways}
    with open(os.path.join(ROOT, 'Tools', 'blender', 'mission2-plan.json'), 'w', encoding='utf-8') as f:
        json.dump(plan, f, ensure_ascii=False, indent=1)
    counts = {}
    for it in town.items:
        counts[it["id"]] = counts.get(it["id"], 0) + 1
    print("pieces", len(town.items), sorted(counts.items(), key=lambda kv: -kv[1]))


if __name__ == '__main__':
    main()
