"""The bot's town, template A «Кольцевой город» (the user, 2026-11-05): a living town in rings of
strength -- an open posad (lvl 1-2) outside, a middle town (lvl 2-3) behind a lvl-3 wall, an upper
town (lvl 3-4) behind a lvl-4 wall, the citadel (lvl 5) behind the moat -- each ring cut into
districts (residential, food, craft, armoury, noble, market, port) so the player can raze it stage
by stage. Writes Design/bot_town_a.json (base-editor format, the game's BotKingdom reads it) and
Design/bot_town_a.png (the scheme), both from the same list of pieces.
Cells: centre (0,0) = the Town Hall's middle, x right, y down (south = the harbour)."""
import json
import math
import os
import random

from PIL import Image, ImageDraw, ImageFont

SEED = 11
R = 27                      # the town reaches |x|,|y| <= 27: 55 x 55 cells
SEA_FROM = R + 3            # sand R+1..R+2, shallows R+3..R+5, sea beyond
SEA_TO = R + 9
OFF = R + 1                 # scheme -> editor cells
SIZE = {}
for line in """Castle 5 5|DiplomacyHall 4 4|Barracks 3 3|Shipyard 3 4|Lighthouse 2 2|HealerHouse 2 4|Church 3 3
Manor 3 3|Square 2 2|Road 1 1|Moat 1 1|Drawbridge 1 2|Fence 1 1|Tower 1 1|FortifiedTower 2 2|FortifiedGate 1 3
DecTree 1 1|DecBush 1 1|Flag 1 1|Well 1 1|WaterTower 1 1|Baker 1 2|BigGranary 3 2|BigTreasury 4 3
BigWarehouse 3 3|CoalMine 2 2|Colosseum 4 6|CopperMine 2 2|Cottage 2 2|DecGarden 3 1|Farm 1 1|FireBrigade 2 2
FisherHut 1 1|GoldMine 2 2|Granary 2 1|Hovel 2 2|IronMine 2 2|Laboratory 4 4|Orchard 3 3|PigFarm 3 4|Quarry 2 2
Sawmill 2 2|Smelter 2 3|Smokehouse 1 2|Tavern 2 1|TaxHouse 2 2|Theater 4 4|TownSquare 2 2|Treasury 3 2
Warehouse 2 2""".replace("\n", "|").split("|"):
    n, a, b = line.split()
    SIZE[n] = (int(a), int(b))

rng = random.Random(SEED)
taken = {}                  # cell -> piece index
items = []                  # {"id","x","y","w","h","rot","level","district"}


def free(cells, inside=True):
    return all(c not in taken and (not inside or max(abs(c[0]), abs(c[1])) <= R) for c in cells)


def put(bid, x, y, level, district, turned=False, margin=0, inside=True):
    w, h = SIZE[bid]
    if turned:
        w, h = h, w
    cells = [(x + i, y + j) for i in range(w) for j in range(h)]
    if not free(cells, inside):
        return False
    if margin:
        # a lane round every house: nothing but roads and decor may touch it
        for i in range(-1, w + 1):
            for j in range(-1, h + 1):
                c = (x + i, y + j)
                if c in taken and items[taken[c]]["id"] not in ("Road", "DecTree", "DecBush", "Flag", "Well", "Moat", "Fence", "Tower", "FortifiedGate"):
                    return False
    for c in cells:
        taken[c] = len(items)
    items.append({"id": bid, "x": x, "y": y, "w": w, "h": h, "rot": 1 if turned else 0, "level": level, "district": district})
    return True


def ring(d):
    """The cells at Chebyshev distance d from the centre, clockwise from the north-west corner."""
    out = [(x, -d) for x in range(-d, d + 1)]
    out += [(d, y) for y in range(-d + 1, d + 1)]
    out += [(x, d) for x in range(d - 1, -d - 1, -1)]
    out += [(-d, y) for y in range(d - 1, -d, -1)]
    return out


def wall(d, level, district, tower_every, gate_level):
    """A wall ring at distance d: fortified gates on the 4 axes, towers every few cells, fence between."""
    put("FortifiedGate", -1, -d, gate_level, district, turned=True)
    put("FortifiedGate", -1, d, gate_level, district, turned=True)
    put("FortifiedGate", -d, -1, gate_level, district)
    put("FortifiedGate", d, -1, gate_level, district)
    for k, (x, y) in enumerate(ring(d)):
        corner = abs(x) == d and abs(y) == d
        put("Tower" if corner or k % tower_every == 0 else "Fence", x, y, level, district)


# ---------------------------------------------------------------- the citadel (lvl 5)
put("Castle", -2, -2, 5, "citadel")
for sx in (-5, 3):
    for sy in (-5, 3):
        put("FortifiedTower", sx, sy, 5, "citadel")
for x, y in ring(6):
    pass
put("FortifiedGate", -1, -6, 5, "citadel", turned=True)
put("FortifiedGate", -1, 6, 5, "citadel", turned=True)
put("FortifiedGate", -6, -1, 5, "citadel")
put("FortifiedGate", 6, -1, 5, "citadel")
for x, y in ring(6):
    put("Fence", x, y, 5, "citadel")
for x, y in ring(3):
    put("Road", x, y, 5, "citadel")
for i in (4, 5):
    for x, y in ((0, -i), (0, i), (-i, 0), (i, 0)):
        put("Road", x, y, 5, "citadel")
for x, y in ((-2, -5), (2, -5), (-2, 5), (2, 5), (-5, -2), (-5, 2), (5, -2), (5, 2)):
    put("Flag", x, y, 5, "citadel")
for k, (x, y) in enumerate(((-1, -5), (1, -5), (-1, 5), (1, 5), (-5, -1), (-5, 1), (5, -1), (5, 1))):
    put("DecTree" if k % 2 else "DecBush", x, y, 5, "citadel")

# the moat and its drawbridges
put("Drawbridge", 0, -8, 5, "citadel")
put("Drawbridge", 0, 7, 5, "citadel")
put("Drawbridge", -8, 0, 5, "citadel", turned=True)
put("Drawbridge", 7, 0, 5, "citadel", turned=True)
for d in (7, 8):
    for x, y in ring(d):
        put("Moat", x, y, 5, "citadel")

# ---------------------------------------------------------------- the walls and the main streets
wall(14, 4, "wall", 5, 4)
wall(20, 3, "wall", 6, 3)
for i in range(9, R + 1):
    lvl = 4 if i < 14 else 3 if i < 20 else 2
    for x, y in ((0, -i), (-i, 0), (i, 0), (0, i)):
        put("Road", x, y, lvl, "street")

# ---------------------------------------------------------------- the districts
SECTORS = ["N", "NE", "E", "SE", "S", "SW", "W", "NW"]


def sector(x, y):
    a = math.degrees(math.atan2(x, -y)) % 360      # 0 = north, clockwise
    return SECTORS[int(((a + 22.5) % 360) // 45)]


def band(x, y):
    d = max(abs(x), abs(y))
    return "upper" if 9 <= d <= 13 else "middle" if 15 <= d <= 19 else "posad" if d >= 21 else None


# (band, sector) -> (district, [(building, how many, (lowest, highest level))])
PLAN = {
    ("upper", "N"): ("noble", [("Church", 1, (4, 4)), ("Manor", 3, (3, 4)), ("DecGarden", 2, (3, 3))]),
    ("upper", "NE"): ("armoury", [("Barracks", 2, (4, 4)), ("Smelter", 1, (3, 4)), ("FortifiedTower", 1, (4, 4))]),
    ("upper", "E"): ("armoury", [("Barracks", 1, (4, 4)), ("Warehouse", 2, (3, 3)), ("Smelter", 1, (3, 3))]),
    ("upper", "SE"): ("treasury", [("BigTreasury", 1, (4, 4)), ("TaxHouse", 1, (3, 4)), ("Treasury", 1, (3, 3))]),
    ("upper", "S"): ("market", [("TownSquare", 2, (3, 4)), ("Tavern", 2, (3, 3)), ("Baker", 2, (3, 3))]),
    ("upper", "SW"): ("healer", [("HealerHouse", 2, (3, 4)), ("FireBrigade", 1, (3, 3)), ("Well", 2, (3, 3))]),
    ("upper", "W"): ("noble", [("Theater", 1, (3, 4)), ("Manor", 2, (3, 4))]),
    ("upper", "NW"): ("noble", [("DiplomacyHall", 1, (5, 5)), ("Laboratory", 1, (3, 3))]),
    ("middle", "N"): ("craft", [("Sawmill", 2, (2, 3)), ("Quarry", 2, (2, 3)), ("Warehouse", 1, (2, 2))]),
    ("middle", "NE"): ("craft", [("IronMine", 1, (2, 3)), ("CoalMine", 1, (2, 3)), ("Smelter", 2, (2, 3)), ("BigWarehouse", 1, (3, 3))]),
    ("middle", "E"): ("food", [("BigGranary", 1, (3, 3)), ("Granary", 2, (2, 2)), ("Smokehouse", 2, (2, 3)), ("Baker", 2, (2, 3))]),
    ("middle", "SE"): ("living", [("Cottage", 6, (2, 3)), ("Well", 1, (2, 2)), ("Square", 1, (2, 2))]),
    ("middle", "S"): ("market", [("Colosseum", 1, (3, 3)), ("Tavern", 2, (2, 2)), ("Square", 2, (2, 2))]),
    ("middle", "SW"): ("living", [("Cottage", 5, (2, 3)), ("Manor", 1, (3, 3)), ("Well", 1, (2, 2))]),
    ("middle", "W"): ("living", [("Cottage", 6, (2, 3)), ("FireBrigade", 1, (2, 2))]),
    ("middle", "NW"): ("armoury", [("Barracks", 1, (2, 3)), ("CopperMine", 1, (2, 2)), ("GoldMine", 1, (2, 2)), ("Warehouse", 1, (2, 2))]),
    ("posad", "N"): ("fields", [("PigFarm", 1, (1, 2)), ("Farm", 14, (1, 2)), ("Orchard", 1, (1, 2))]),
    ("posad", "NE"): ("fields", [("Orchard", 2, (1, 2)), ("Farm", 10, (1, 1)), ("Hovel", 2, (1, 1))]),
    ("posad", "E"): ("posad", [("Hovel", 7, (1, 2)), ("Well", 2, (1, 1)), ("Farm", 6, (1, 1))]),
    ("posad", "SE"): ("posad", [("Hovel", 5, (1, 2)), ("FisherHut", 3, (1, 2))]),
    ("posad", "S"): ("port", [("Shipyard", 1, (3, 3)), ("Lighthouse", 1, (3, 3)), ("Warehouse", 1, (2, 2)), ("FisherHut", 3, (1, 2))]),
    ("posad", "SW"): ("posad", [("Hovel", 5, (1, 2)), ("FisherHut", 2, (1, 1)), ("Well", 1, (1, 1))]),
    ("posad", "W"): ("posad", [("Hovel", 7, (1, 2)), ("PigFarm", 1, (1, 1)), ("Farm", 4, (1, 1))]),
    ("posad", "NW"): ("fields", [("Farm", 14, (1, 2)), ("Orchard", 1, (1, 2)), ("Hovel", 2, (1, 1))]),
}

region = {}
for x in range(-R, R + 1):
    for y in range(-R, R + 1):
        b = band(x, y)
        if b and (x, y) not in taken:
            region.setdefault((b, sector(x, y)), []).append((x, y))

FILL = {   # what a district builds on in its leftover room: (building, level range), tried in turn
    "noble": [("Manor", (3, 4)), ("DecGarden", (3, 3))], "armoury": [("Warehouse", (3, 3)), ("Smelter", (3, 3))],
    "treasury": [("Treasury", (3, 3)), ("DecGarden", (3, 3))], "market": [("Tavern", (2, 3)), ("Square", (2, 2))],
    "healer": [("DecGarden", (3, 3))], "craft": [("Sawmill", (2, 3)), ("Quarry", (2, 3)), ("Warehouse", (2, 2))],
    "food": [("Smokehouse", (2, 3)), ("Granary", (2, 2)), ("Baker", (2, 3))], "living": [("Cottage", (2, 3))],
    "fields": [("FarmBlock", (1, 2)), ("FarmBlock", (1, 2)), ("Hovel", (1, 1))], "posad": [("Hovel", (1, 2)), ("Hovel", (1, 2)), ("FarmBlock", (1, 1))],
    
}


def order_for(cells, key):
    """Pack from the band's inner edge outwards, round the sector: houses come in rows with lanes."""
    inner = {"upper": 9, "middle": 15, "posad": 21}[key[0]]
    def k(c):
        dd = max(abs(c[0]), abs(c[1])) - inner
        return (dd // 3, math.atan2(c[0], -c[1]), dd)
    return sorted(cells, key=k)


def try_place(bid, cells, lo, hi, district):
    if bid == "FarmBlock":
        # a field of 2x2 farm plots, all four or none
        for (x, y) in cells:
            quad = [(x, y), (x + 1, y), (x, y + 1), (x + 1, y + 1)]
            if all(q in cellset for q in quad) and free(quad):
                lvl = rng.randint(lo, hi)
                for (qx, qy) in quad:
                    put("Farm", qx, qy, lvl, district)
                return True
        return False
    for (x, y) in cells:
        turned = SIZE[bid][0] != SIZE[bid][1] and rng.random() < 0.5
        margin = 0 if bid in ("Farm", "Tower") else 1
        if put(bid, x, y, rng.randint(lo, hi), district, turned, margin):
            return True
    return False


placed_count = {}
for key, (district, wants) in PLAN.items():
    cells = region.get(key, [])
    cellset = set(cells)
    if district == "port":
        cells = sorted(cells, key=lambda c: -c[1])
    else:
        cells = order_for(cells, key)
    for bid, n, (lo, hi) in sorted(wants, key=lambda t: -SIZE[t[0]][0] * SIZE[t[0]][1]):
        for _ in range(n):
            if try_place(bid, cells, lo, hi, district):
                placed_count[bid] = placed_count.get(bid, 0) + 1
    fills = FILL.get(district, [])
    misses = 0
    k = 0
    blocks = 0
    while fills and misses < len(fills):
        bid, (lo, hi) = fills[k % len(fills)]
        k += 1
        if bid == "FarmBlock":
            if blocks >= (5 if district == "fields" else 2):
                misses += 1
                continue
            blocks += 1
        if try_place(bid, cells, lo, hi, district):
            misses = 0
        else:
            misses += 1

# a living town: trees and bushes in some of the gaps, gardens in the noble quarters
for key, cells in region.items():
    for (x, y) in cells:
        if (x, y) in taken:
            continue
        p = 0.05
        if rng.random() < p:
            put(rng.choice(("DecTree", "DecTree", "DecBush")), x, y, 1, "decor")

# the harbour beyond the posad: water towers in the shallows
put("WaterTower", -6, R + 4, 3, "port", inside=False)
put("WaterTower", 6, R + 4, 3, "port", inside=False)

# ---------------------------------------------------------------- the blueprint
terrain = {}
for x in range(-OFF, OFF + 1):
    for y in range(R + 1, SEA_TO + 1):
        terrain[f"{x + OFF},{y + OFF}"] = "sand" if y < SEA_FROM else "shallow" if y < SEA_FROM + 3 else "sea"
here = os.path.dirname(os.path.abspath(__file__))
blueprint = {"format": "citybuilder-base/1", "name": "Город бота A «Кольцевой город»", "width": 2 * OFF + 1,
             "height": OFF + SEA_TO + 1,
             "buildings": [{k: (v + OFF if k in ("x", "y") else v) for k, v in it.items() if k != "district"} for it in items],
             "terrain": terrain}
with open(os.path.join(here, "bot_town_a.json"), "w", encoding="utf-8") as f:
    json.dump(blueprint, f, ensure_ascii=False, separators=(",", ":"))

# ---------------------------------------------------------------- the scheme
C = 18
PAD = 30
LEG = 360
W = (2 * OFF + 1) * C + 2 * PAD + LEG
H = (OFF + SEA_TO + 1) * C + 2 * PAD + 30
OX, OY = PAD + OFF * C, PAD + 30 + OFF * C
img = Image.new("RGB", (W, H), (250, 250, 247))
d = ImageDraw.Draw(img)
F = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 10)
FS = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 8)
FB = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 15)
FL = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 12)

DISTRICT = {
    "citadel": ((150, 150, 160), "Цитадель (ур. 5)"), "wall": ((120, 120, 120), "Стены и башни (ур. 4 / 3)"),
    "street": ((246, 214, 175), "Улицы"), "noble": ((180, 140, 230), "Знать: поместья, церковь, театр, Зал дипломатии"),
    "armoury": ((215, 90, 90), "Оружейный: казармы, плавильни, склады"), "treasury": ((240, 200, 60), "Казна: сокровищницы, налоговая"),
    "market": ((255, 170, 70), "Рынок: площади, таверны, пекарни, Колизей"), "healer": ((255, 180, 205), "Лекари и пожарные"),
    "craft": ((150, 110, 75), "Ремесленный: лесопилки, каменоломни, рудники"), "food": ((120, 190, 90), "Продуктовый: амбары, коптильни, пекарни"),
    "living": ((110, 170, 230), "Жилой: коттеджи"), "fields": ((200, 225, 120), "Поля: фермы, сады, свинарник"),
    "posad": ((170, 200, 225), "Посад: лачуги, колодцы, рыбаки"), "port": ((90, 120, 170), "Порт: верфь, маяк, рыбаки, водяные башни"),
    "decor": ((95, 170, 95), "Деревья и кусты"),
}
SHORT = {"Castle": "Ратуша", "DiplomacyHall": "Зал\nдипл.", "Barracks": "Казар-\nма", "Shipyard": "Верфь", "Lighthouse": "Маяк",
         "HealerHouse": "Лек.", "Church": "Церковь", "Manor": "Поме-\nстье", "Square": "пл.", "Drawbridge": "", "FortifiedTower": "Б",
         "FortifiedGate": "", "Baker": "Пек", "BigGranary": "Амбар", "BigTreasury": "Казна", "BigWarehouse": "Склад", "CoalMine": "Уголь",
         "Colosseum": "Коли-\nзей", "CopperMine": "Медь", "Cottage": "Кот", "DecGarden": "сад", "Farm": "", "FireBrigade": "Пож",
         "FisherHut": "р", "GoldMine": "Зол", "Granary": "Амб", "Hovel": "Лач", "IronMine": "Жел", "Laboratory": "Лабор.",
         "Orchard": "Сад", "PigFarm": "Свин.", "Quarry": "Кам", "Sawmill": "Лес", "Smelter": "Плав", "Smokehouse": "Копт",
         "Tavern": "Тав", "TaxHouse": "Нал", "Theater": "Театр", "TownSquare": "Пл", "Treasury": "Казна", "Warehouse": "Скл",
         "WaterTower": "ВБ", "Well": "", "Road": "", "Moat": "", "Fence": "", "Tower": "", "Flag": "", "DecTree": "", "DecBush": ""}


def cell_box(x, y, w, h):
    return [OX + x * C, OY + y * C, OX + (x + w) * C - 1, OY + (y + h) * C - 1]


# ground, sand, water
d.rectangle(cell_box(-OFF, -OFF, 2 * OFF + 1, 2 * OFF + 1), fill=(218, 236, 200))
for y in range(R + 1, SEA_TO + 1):
    col = (240, 222, 160) if y < SEA_FROM else (150, 205, 240) if y < SEA_FROM + 3 else (98, 160, 235)
    d.rectangle(cell_box(-OFF, y, 2 * OFF + 1, 1), fill=col)


def shade(col, level):
    """Higher level = deeper colour: a lvl 1 house is pale, a lvl 5 one full."""
    k = 0.45 + 0.55 * (level - 1) / 4
    return tuple(int(255 - (255 - c) * k) for c in col)


for it in items:
    bid = it["id"]
    col = DISTRICT[it["district"]][0]
    if bid == "Moat":
        col = (100, 155, 240)
    elif bid in ("Fence", "Tower", "FortifiedGate") and it["district"] == "wall":
        col = (110, 110, 110) if bid != "Fence" else (175, 175, 175)
    elif bid == "Road":
        col = (246, 214, 175)
    box = cell_box(it["x"], it["y"], it["w"], it["h"])
    fill = col if bid in ("Moat", "Road") else shade(col, it["level"])
    d.rectangle(box, fill=fill, outline=(110, 110, 110) if it["w"] * it["h"] > 1 else None)
    label = SHORT.get(bid, bid)
    if it["w"] * it["h"] > 1 and label:
        cx, cy = (box[0] + box[2]) / 2, (box[1] + box[3]) / 2
        txt = label + ("" if bid in ("Square", "DecGarden") else f"\n{it['level']}") if it["h"] >= 2 or it["w"] >= 3 else label
        d.multiline_text((cx, cy), txt, fill=(25, 25, 25), font=FS, anchor="mm", align="center")
    elif bid == "Farm":
        d.text(((box[0] + box[2]) / 2, (box[1] + box[3]) / 2), "ф", fill=(70, 90, 30), font=FS, anchor="mm")

d.text((PAD, 8), "Город бота, шаблон A «Кольцевой город» — 55x55 клеток, 1 квадрат = 1 клетка; цифра = уровень здания, бледнее = слабее",
       fill=(20, 20, 20), font=FB)

# the legend
lx, ly = W - LEG + 10, PAD + 40
d.text((lx, ly), "Районы", fill=(20, 20, 20), font=FB)
ly += 26
for key, (col, name) in DISTRICT.items():
    d.rectangle([lx, ly, lx + 16, ly + 14], fill=col, outline=(110, 110, 110))
    d.multiline_text((lx + 24, ly), name, fill=(20, 20, 20), font=FL)
    ly += 22
ly += 14
d.text((lx, ly), "Пояса силы (снаружи внутрь)", fill=(20, 20, 20), font=FB)
ly += 24
for line in ("Посад: ур. 1-2, без стены", "Средний город: ур. 2-3, стена ур. 3", "Верхний город: ур. 3-4, стена ур. 4",
             "Цитадель: ур. 5, ров и барбаканы", "", "ф = поле фермы, р = рыбак, Б = укрепл. башня,", "ВБ = водяная башня, Лач = лачуга, Кот = коттедж"):
    d.text((lx, ly), line, fill=(20, 20, 20), font=FL)
    ly += 19

img.save(os.path.join(here, "bot_town_a.png"))
counts = {}
for it in items:
    counts[it["id"]] = counts.get(it["id"], 0) + 1
print("pieces", len(items))
print(sorted(counts.items(), key=lambda kv: -kv[1]))
missing = [(bid, n) for (_, (_, wants)) in PLAN.items() for bid, n, _ in wants]
want = {}
for bid, n in missing:
    want[bid] = want.get(bid, 0) + n
print("short:", {k: (placed_count.get(k, 0), v) for k, v in want.items() if placed_count.get(k, 0) < v})
