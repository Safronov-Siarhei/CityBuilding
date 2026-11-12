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
# the orcs' roads, ford -> outer gate (the plan's ways), then on to the inner gate
OUTER_GATES = {"W": (28, 40), "N": (50, 34), "E": (72, 40)}
INNER_GATES = {"W": (41, 56), "N": (50, 51), "E": (59, 56)}

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
    t.wall_rect(il, it, ir, ib, INNER_LEVEL, [("W", INNER_GATES["W"][1]), ("N", INNER_GATES["N"][0]), ("E", INNER_GATES["E"][1])],
                tower_every=6, district="inner")
    t.wall_rect(ol, ot, or_, ob, OUTER_LEVEL, [("W", OUTER_GATES["W"][1]), ("N", OUTER_GATES["N"][0]), ("E", OUTER_GATES["E"][1])],
                tower_every=6, district="wall")

    # streets: a ring inside each wall, a street from each outer gate to its inner gate, a cross street
    def road(x0, y0, x1, y1, level=2):
        for x in range(min(x0, x1), max(x0, x1) + 1):
            for y in range(min(y0, y1), max(y0, y1) + 1):
                put(t, "Road", x, y, level, "street")

    road(ol + 2, ot + 2, ol + 2, ob - 1)
    road(ol + 2, ot + 2, or_ - 2, ot + 2)
    road(or_ - 2, ot + 2, or_ - 2, ob - 1)
    road(ol + 1, OUTER_GATES["W"][1], il - 2, OUTER_GATES["W"][1])
    road(il - 2, OUTER_GATES["W"][1], il - 2, INNER_GATES["W"][1])
    road(il - 2, INNER_GATES["W"][1], il - 1, INNER_GATES["W"][1])
    road(or_ - 1, OUTER_GATES["E"][1], ir + 2, OUTER_GATES["E"][1])
    road(ir + 2, OUTER_GATES["E"][1], ir + 2, INNER_GATES["E"][1])
    road(ir + 1, INNER_GATES["E"][1], ir + 2, INNER_GATES["E"][1])
    road(50, ot + 1, 50, it - 1)
    road(ol + 2, 47, or_ - 2, 47)
    # the centre: its ring and the way from each inner gate to the square
    road(il + 2, it + 2, il + 2, ib - 1, 4)
    road(il + 2, it + 2, ir - 2, it + 2, 4)
    road(ir - 2, it + 2, ir - 2, ib - 1, 4)
    road(il + 1, INNER_GATES["W"][1], ir - 1, INNER_GATES["W"][1], 4)
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
    return t


def spots():
    s = []
    for i, (fx, fy) in enumerate(FORDS):
        s.append({"kind": "spawn", "x": fx, "y": fy - 6.0, "lane": i})     # beyond the river, in the far forest
        s.append({"kind": "ford", "x": fx, "y": fy, "lane": i})
    for i, side in enumerate(("W", "N", "E")):
        x, y = OUTER_GATES[side]
        s.append({"kind": "outer_gate", "x": x + 0.5, "y": y + 0.5, "lane": i})
        x, y = INNER_GATES[side]
        s.append({"kind": "inner_gate", "x": x + 0.5, "y": y + 0.5, "lane": i})
    # the plan's ways from each ford to its outer gate (the lane's corners), plan cells
    ways = [
        [FORDS[0], (14, 22), (16, 32), (20, 40), (OUTER_GATES["W"][0] - 2.4, 40.5)],
        [FORDS[1], (50.5, 20), (50.5, OUTER_GATES["N"][1] - 2.4)],
        [FORDS[2], (86, 22), (84, 32), (80, 40), (OUTER_GATES["E"][0] + 3.4, 40.5)],
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
    inside = [
        [(28.5, 40.5), (39.5, 40.5), (39.5, 56.5), (41.5, 56.5), (44.5, 56.5), (50.5, 59.5)],
        [(50.5, 34.5), (50.5, 51.5), (50.5, 57.5)],
        [(72.5, 40.5), (61.5, 40.5), (61.5, 56.5), (59.5, 56.5), (56.5, 56.5), (50.5, 59.5)],
    ]
    lanes = []
    for i, way in enumerate(ways):
        fx, fy = FORDS[i]
        pts = [(fx, fy - 6.0)] + [tuple(p) for p in way] + inside[i]
        lanes.append([[round(x, 2), round(y, 2)] for x, y in pts])
    blueprint = {"format": "citybuilder-base/1", "name": "Mission 2 town", "width": W, "height": H,
                 "origin": [-50, 46],
                 "lanes": [{"points": [v for xy in l for v in xy]} for l in lanes],
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
