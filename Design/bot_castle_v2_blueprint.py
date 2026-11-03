"""The bot's castle v2 (Design/bot_castle_v2.png) as a base-editor blueprint: JSON with every
building's id, cell (x right, y down, north up), turned footprint and level, plus the terrain.
Written to Design/bot_castle_v2.json; the base editor loads it as its example."""
import json
import os

H = 17                     # outer wall at |x|,|y| = 17 (scheme coordinates, centre 0)
OFF = H + 1                # scheme -> editor cells
W, HGT = 2 * OFF + 1, OFF + 26
SIZE = {}                  # id -> (w, h) unturned, from the game
for line in """Castle 5 5|DiplomacyHall 4 4|Barracks 3 3|Shipyard 3 4|Lighthouse 2 2|HealerHouse 2 4|Church 3 3|Manor 3 3
Square 2 2|Road 1 1|Moat 1 1|Drawbridge 1 2|Fence 1 1|Tower 1 1|FortifiedTower 2 2|FortifiedGate 1 3|DecTree 1 1
DecBush 1 1|Flag 1 1|Well 1 1|WaterTower 1 1""".replace("\n", "|").split("|"):
    n, a, b = line.split()
    SIZE[n] = (int(a), int(b))

taken = {}
items = []


def put(bid, x, y, level=5, turned=False):
    w, h = SIZE[bid]
    if turned:
        w, h = h, w
    cells = [(x + i, y + j) for i in range(w) for j in range(h)]
    if any(c in taken for c in cells):
        return False
    for c in cells:
        taken[c] = bid
    items.append({"id": bid, "x": x + OFF, "y": y + OFF, "w": w, "h": h, "rot": 1 if turned else 0, "level": level})
    return True


# inner fort
put("Castle", -2, -2)
for sx in (-5, 4):
    for sy in (-5, 4):
        put("FortifiedTower", sx, sy)
put("FortifiedGate", -1, -6, turned=True)
put("FortifiedGate", -1, 6, turned=True)
put("FortifiedGate", -6, -1)
put("FortifiedGate", 6, -1)
for i in range(-6, 7):
    for (x, y) in ((i, -6), (i, 6), (-6, i), (6, i)):
        put("Fence", x, y)
for i in range(-3, 4):
    for (x, y) in ((i, -3), (i, 3), (-3, i), (3, i)):
        put("Road", x, y)
for i in (4, 5):
    for (x, y) in ((0, -i), (0, i), (-i, 0), (i, 0)):
        put("Road", x, y)
for (x, y) in ((-2, -5), (2, -5), (-2, 5), (2, 5), (-5, -2), (-5, 2), (5, -2), (5, 2)):
    put("Flag", x, y)
k = 0
for (x, y) in ((-1, -5), (1, -5), (-1, 5), (1, 5), (-5, -1), (-5, 1), (5, -1), (5, 1),
               (-2, -4), (2, -4), (-2, 4), (2, 4), (-4, -2), (-4, 2), (4, -2), (4, 2)):
    put("DecTree" if k % 2 == 0 else "DecBush", x, y)
    k += 1

# drawbridges, then the moat round them
put("Drawbridge", 0, -8)
put("Drawbridge", 0, 7)
put("Drawbridge", -8, 0, turned=True)
put("Drawbridge", 7, 0, turned=True)
for x in range(-8, 9):
    for y in range(-8, 9):
        if max(abs(x), abs(y)) in (7, 8):
            put("Moat", x, y)

# the square ring, its axis roads, the corner decor
for i in (-8, -6, -4, -2, 1, 3, 5, 7):
    for (x, y) in ((i, -10), (i, 9), (-10, i), (9, i)):
        put("Square", x, y)
for (x, y) in ((0, -10), (0, -9), (0, 9), (0, 10), (-10, 0), (-9, 0), (9, 0), (10, 0)):
    put("Road", x, y)
for (x, y) in ((-10, -10), (9, -10), (-10, 9), (9, 9)):
    put("DecTree", x, y)
    put("Flag", x + 1, y)
    put("Well", x, y + 1)
    put("DecBush", x + 1, y + 1)

# avenues out to the gates, and the ring's streets on to the wall
for kk in range(11, 17):
    for (x, y) in ((0, -kk), (0, kk), (-kk, 0), (kk, 0)):
        put("Road", x, y)
for kk in range(11, 17, 2):
    for side in (-2, 1):
        put("Square", side, -kk - 1)
        put("Square", side, kk)
        put("Square", -kk - 1, side)
        put("Square", kk, side)
    for (x, y) in ((-10, -kk - 1), (9, -kk - 1), (-10, kk), (9, kk), (-kk - 1, -10), (-kk - 1, 9), (kk, -10), (kk, 9)):
        put("Square", x, y)

# blocks of 3x3: manors, barracks, the church
special = {(3, 14): "Barracks", (-8, 14): "Barracks", (11, 3): "Barracks", (-16, 3): "Barracks", (-5, -16): "Church"}
for sx in (-8, 3):
    for sy in (-16, 11):
        for i in range(2):
            for j in range(2):
                put(special.get((sx + 3 * i, sy + 3 * j), "Manor"), sx + 3 * i, sy + 3 * j)
for sx in (-16, 11):
    for sy in (-8, 3):
        for i in range(2):
            for j in range(2):
                put(special.get((sx + 3 * i, sy + 3 * j), "Manor"), sx + 3 * i, sy + 3 * j)

# corner zones
put("DiplomacyHall", -15, -15)
for (x, y) in ((-16, -16), (-11, -16), (-16, -11), (-11, -11)):
    put("Flag", x, y)
for (x, y) in ((-14, -16), (-13, -16), (-16, -14), (-16, -13), (-14, -11), (-13, -11), (-11, -14), (-11, -13)):
    put("DecTree", x, y)
put("HealerHouse", 11, -16)
put("HealerHouse", 14, -16)
for x in range(11, 17):
    put("DecBush", x, -11)
for (zx, zy) in ((-16, 11), (11, 11)):
    put("Barracks", zx + 1, zy + 1)
    put("Well", zx + 4, zy + 1)
    for kk in range(6):
        put("DecBush", zx + kk, zy + 5)

# the outer wall lvl4: barbicans, towers, then the wall between
put("FortifiedGate", -1, -H, 4, turned=True)
put("FortifiedGate", -1, H, 4, turned=True)
put("FortifiedGate", -H, -1, 4)
put("FortifiedGate", H, -1, 4)
for kk in (-H, -11, -6, 6, 11, H):
    for (x, y) in ((kk, -H), (kk, H), (-H, kk), (H, kk)):
        put("Tower", x, y, 4)
for i in range(-H, H + 1):
    for (x, y) in ((i, -H), (i, H), (-H, i), (H, i)):
        put("Fence", x, y, 4)

# the harbour
put("Road", 0, H + 1)
put("Shipyard", -1, H + 2)
put("Lighthouse", H - 3, H + 1)
put("WaterTower", -7, H + 4)
put("WaterTower", 7, H + 4)

terrain = {}
for x in range(-OFF, OFF + 1):
    for y in range(H + 1, H + 3):
        terrain[f"{x + OFF},{y + OFF}"] = "sand"
    for y in range(H + 3, H + 6):
        terrain[f"{x + OFF},{y + OFF}"] = "shallow"
    for y in range(H + 6, HGT - OFF):
        terrain[f"{x + OFF},{y + OFF}"] = "sea"

blueprint = {"format": "citybuilder-base/1", "name": "Замок бота v2", "width": W, "height": HGT,
             "buildings": items, "terrain": terrain}
out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "bot_castle_v2.json")
with open(out, "w", encoding="utf-8") as f:
    json.dump(blueprint, f, ensure_ascii=False, separators=(",", ":"))
print("buildings", len(items), "size", W, HGT)
