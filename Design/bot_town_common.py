"""Shared kit for the bot's town templates B, C, D (the user, 2026-11-05; A is bot_town_a.py, which
came first): a 55x55 town (cells -27..27, x right, y down = south = the harbour), walls, the
citadel, districts packed in rows with lanes, decor, then the base-editor JSON and the scheme PNG.
A template script makes a Town, lays its streets/walls/citadel, gives a region function and a
PLAN, and calls town.finish(name, file stem)."""
import json
import math
import os
import random

from PIL import Image, ImageDraw, ImageFont

R = 27
SEA_FROM = R + 3
SEA_TO = R + 9
OFF = R + 1
SIZE = {}
for _line in """Castle 5 5|DiplomacyHall 4 4|Barracks 3 3|Shipyard 3 4|Lighthouse 2 2|HealerHouse 2 4|Church 3 3
Manor 3 3|Square 2 2|Road 1 1|Moat 1 1|Drawbridge 1 2|Fence 1 1|Tower 1 1|FortifiedTower 2 2|FortifiedGate 1 3
DecTree 1 1|DecBush 1 1|Flag 1 1|Well 1 1|WaterTower 1 1|Baker 1 2|BigGranary 3 2|BigTreasury 4 3
BigWarehouse 3 3|CoalMine 2 2|Colosseum 4 6|CopperMine 2 2|Cottage 2 2|DecGarden 3 1|Farm 1 1|FireBrigade 2 2
FisherHut 1 1|GoldMine 2 2|Granary 2 1|Hovel 2 2|IronMine 2 2|Laboratory 4 4|Orchard 3 3|PigFarm 3 4|Quarry 2 2
Sawmill 2 2|Smelter 2 3|Smokehouse 1 2|Tavern 2 1|TaxHouse 2 2|Theater 4 4|TownSquare 2 2|Treasury 3 2
Warehouse 2 2|Gate 1 1""".replace("\n", "|").split("|"):
    _n, _a, _b = _line.split()
    SIZE[_n] = (int(_a), int(_b))

LANE_OK = ("Road", "DecTree", "DecBush", "Flag", "Well", "Moat", "Fence", "Tower", "FortifiedGate", "Gate", "Drawbridge")

FILL = {   # what a district builds on in its leftover room: (building, level range), tried in turn
    "noble": [("Manor", (3, 4)), ("DecGarden", (3, 3))], "armoury": [("Warehouse", (3, 3)), ("Square", (3, 3)), ("Smelter", (3, 3))],
    "treasury": [("Treasury", (3, 3)), ("DecGarden", (3, 3))], "market": [("Tavern", (2, 3)), ("Square", (2, 2))],
    "healer": [("DecGarden", (3, 3))], "craft": [("Sawmill", (2, 3)), ("Cottage", (2, 2)), ("Quarry", (2, 3)), ("DecGarden", (2, 2)), ("Warehouse", (2, 2))],
    "food": [("Smokehouse", (2, 3)), ("Cottage", (2, 2)), ("Granary", (2, 2)), ("Orchard", (2, 2)), ("Baker", (2, 3))], "living": [("Cottage", (2, 3))],
    "fields": [("FarmBlock", (1, 2)), ("FarmBlock", (1, 2)), ("Hovel", (1, 1))], "posad": [("Hovel", (1, 2)), ("Hovel", (1, 2)), ("FarmBlock", (1, 1))],
    "port": [("Hovel", (1, 2)), ("Warehouse", (2, 2)), ("Hovel", (1, 2)), ("Tavern", (2, 2))],
}

DISTRICT = {
    "citadel": ((150, 150, 160), "Цитадель (ур. 5)"), "wall": ((120, 120, 120), "Стены и башни"),
    "street": ((246, 214, 175), "Улицы"), "noble": ((180, 140, 230), "Знать: поместья, церковь, театр, Зал дипломатии"),
    "armoury": ((215, 90, 90), "Оружейный: казармы, плавильни, склады"), "treasury": ((240, 200, 60), "Казна: сокровищницы, налоговая"),
    "market": ((255, 170, 70), "Рынок: площади, таверны, пекарни, Колизей"), "healer": ((255, 180, 205), "Лекари и пожарные"),
    "craft": ((150, 110, 75), "Ремесленный: лесопилки, каменоломни, рудники"), "food": ((120, 190, 90), "Продуктовый: амбары, коптильни, пекарни"),
    "living": ((110, 170, 230), "Жилой: коттеджи"), "fields": ((200, 225, 120), "Поля: фермы, сады, свинарник"),
    "posad": ((170, 200, 225), "Посад: лачуги, колодцы, рыбаки"), "port": ((90, 120, 170), "Порт: верфь, лаборатория, маяк, рыбаки, водяные башни"),
    "decor": ((95, 170, 95), "Деревья и кусты"),
}
SHORT = {"Castle": "Ратуша", "DiplomacyHall": "Зал\nдипл.", "Barracks": "Казар-\nма", "Shipyard": "Верфь", "Lighthouse": "Маяк",
         "HealerHouse": "Лек.", "Church": "Церковь", "Manor": "Поме-\nстье", "Square": "пл.", "FortifiedTower": "Б",
         "Baker": "Пек", "BigGranary": "Амбар", "BigTreasury": "Казна", "BigWarehouse": "Склад", "CoalMine": "Уголь",
         "Colosseum": "Коли-\nзей", "CopperMine": "Медь", "Cottage": "Кот", "DecGarden": "сад", "FireBrigade": "Пож",
         "FisherHut": "р", "GoldMine": "Зол", "Granary": "Амб", "Hovel": "Лач", "IronMine": "Жел", "Laboratory": "Лабор.",
         "Orchard": "Сад", "PigFarm": "Свин.", "Quarry": "Кам", "Sawmill": "Лес", "Smelter": "Плав", "Smokehouse": "Копт",
         "Tavern": "Тав", "TaxHouse": "Нал", "Theater": "Театр", "TownSquare": "Пл", "Treasury": "Казна", "Warehouse": "Скл",
         "WaterTower": "ВБ"}


class Town:
    def __init__(self, seed):
        self.rng = random.Random(seed)
        self.taken = {}
        self.items = []

    # ------------------------------------------------------------ pieces
    def free(self, cells, inside=True):
        return all(c not in self.taken and (not inside or max(abs(c[0]), abs(c[1])) <= R) for c in cells)

    def put(self, bid, x, y, level, district, turned=False, margin=0, inside=True):
        w, h = SIZE[bid]
        if turned:
            w, h = h, w
        cells = [(x + i, y + j) for i in range(w) for j in range(h)]
        if not self.free(cells, inside):
            return False
        if margin:
            for i in range(-1, w + 1):
                for j in range(-1, h + 1):
                    c = (x + i, y + j)
                    if c in self.taken and self.items[self.taken[c]]["id"] not in LANE_OK:
                        return False
        for c in cells:
            self.taken[c] = len(self.items)
        self.items.append({"id": bid, "x": x, "y": y, "w": w, "h": h, "rot": 1 if turned else 0, "level": level, "district": district})
        return True

    def road(self, x0, y0, x1, y1, level):
        for x in range(min(x0, x1), max(x0, x1) + 1):
            for y in range(min(y0, y1), max(y0, y1) + 1):
                self.put("Road", x, y, level, "street")

    # ------------------------------------------------------------ walls
    def wall_rect(self, x0, y0, x1, y1, level, gates, tower_every=5, skip=None, district="wall"):
        """A wall round the box x0..x1, y0..y1. gates: [("N"|"S"|"W"|"E", along)] -- a FortifiedGate
        centred on that cell of that side. skip(x, y) -> True leaves a wall cell out (an open side)."""
        for side, at in gates:
            if side in ("N", "S"):
                self.put("FortifiedGate", at - 1, y0 if side == "N" else y1, level, district, turned=True)
            else:
                self.put("FortifiedGate", x0 if side == "W" else x1, at - 1, level, district)
        k = 0
        for x, y in self.perimeter(x0, y0, x1, y1):
            if skip and skip(x, y):
                continue
            corner = x in (x0, x1) and y in (y0, y1)
            self.put("Tower" if corner or k % tower_every == 0 else "Fence", x, y, level, district)
            k += 1

    @staticmethod
    def perimeter(x0, y0, x1, y1):
        out = [(x, y0) for x in range(x0, x1 + 1)]
        out += [(x1, y) for y in range(y0 + 1, y1 + 1)]
        out += [(x, y1) for x in range(x1 - 1, x0 - 1, -1)]
        out += [(x0, y) for y in range(y1 - 1, y0, -1)]
        return out

    def citadel(self, cx, cy, bridges=("N", "S", "W", "E")):
        """The Town Hall at (cx, cy) in a 13x13 lvl-5 fort with 4 fortified towers and a 2-cell moat."""
        put = self.put
        put("Castle", cx - 2, cy - 2, 5, "citadel")
        for sx in (-5, 3):
            for sy in (-5, 3):
                put("FortifiedTower", cx + sx, cy + sy, 5, "citadel")
        gates = {"N": (cx - 1, cy - 6, True), "S": (cx - 1, cy + 6, True), "W": (cx - 6, cy - 1, False), "E": (cx + 6, cy - 1, False)}
        for side in bridges:
            gx, gy, t = gates[side]
            put("FortifiedGate", gx, gy, 5, "citadel", turned=t)
        for x, y in self.perimeter(cx - 6, cy - 6, cx + 6, cy + 6):
            put("Fence", x, y, 5, "citadel")
        for x, y in self.perimeter(cx - 3, cy - 3, cx + 3, cy + 3):
            put("Road", x, y, 5, "citadel")
        for side in bridges:
            dx, dy = {"N": (0, -1), "S": (0, 1), "W": (-1, 0), "E": (1, 0)}[side]
            for i in (4, 5):
                put("Road", cx + dx * i, cy + dy * i, 5, "citadel")
        for k, (x, y) in enumerate(((-2, -5), (2, -5), (-2, 5), (2, 5), (-5, -2), (-5, 2), (5, -2), (5, 2))):
            put("Flag", cx + x, cy + y, 5, "citadel")
        bridge = {"N": (cx, cy - 8, False), "S": (cx, cy + 7, False), "W": (cx - 8, cy, True), "E": (cx + 7, cy, True)}
        for side in bridges:
            bx, by, t = bridge[side]
            put("Drawbridge", bx, by, 5, "citadel", turned=t)
        for d in (7, 8):
            for x, y in self.perimeter(cx - d, cy - d, cx + d, cy + d):
                put("Moat", x, y, 5, "citadel")

    # ------------------------------------------------------------ districts
    def districts(self, region, plan, farm_blocks=None, fill_cap=None):
        """region(x, y) -> plan key or None; plan[key] = (district, [(building, n, (lo, hi))]).
        Each district gets its listed buildings, biggest first, then its FILL until nothing fits."""
        farm_blocks = farm_blocks or {}
        fill_cap = {"fields": 12, "posad": 14, "living": 12, **(fill_cap or {})}
        cells_of = {}
        for y in range(-R, R + 1):
            for x in range(-R, R + 1):
                if (x, y) in self.taken:
                    continue
                key = region(x, y)
                if key is not None:
                    cells_of.setdefault(key, []).append((x, y))
        self.short = {}
        for key, (district, wants) in plan.items():
            cells = cells_of.get(key, [])
            cellset = set(cells)
            if district == "port":
                cells = sorted(cells, key=lambda c: (-c[1], abs(c[0])))
            else:
                cells = sorted(cells, key=lambda c: (c[1] // 3, c[0], c[1]))
            for bid, n, (lo, hi) in sorted(wants, key=lambda t: -SIZE[t[0]][0] * SIZE[t[0]][1] if t[0] in SIZE else -4):
                for _ in range(n):
                    if not self._try(bid, cells, cellset, lo, hi, district):
                        self.short[bid] = self.short.get(bid, 0) + 1
            fills = FILL.get(district, [])
            cap = farm_blocks.get(district, 5 if district == "fields" else 2)
            misses, k, blocks, added = 0, 0, 0, 0
            limit = fill_cap.get(district, 8)
            spread = self.rng.sample(cells, len(cells))
            while fills and misses < len(fills) and added < limit:
                bid, (lo, hi) = fills[k % len(fills)]
                k += 1
                if bid == "FarmBlock":
                    if blocks >= cap:
                        misses += 1
                        continue
                    blocks += 1
                if self._try(bid, spread, cellset, lo, hi, district):
                    misses, added = 0, added + 1
                else:
                    misses += 1
        # a living town: a few trees and bushes in the gaps
        for key, cells in cells_of.items():
            for (x, y) in cells:
                if (x, y) not in self.taken and self.rng.random() < 0.07:
                    self.put(self.rng.choice(("DecTree", "DecTree", "DecBush")), x, y, 1, "decor")

    def _try(self, bid, cells, cellset, lo, hi, district):
        if bid == "FarmBlock":
            for (x, y) in cells:
                quad = [(x, y), (x + 1, y), (x, y + 1), (x + 1, y + 1)]
                if all(q in cellset for q in quad) and self.free(quad):
                    lvl = self.rng.randint(lo, hi)
                    for (qx, qy) in quad:
                        self.put("Farm", qx, qy, lvl, district)
                    return True
            return False
        for (x, y) in cells:
            turned = SIZE[bid][0] != SIZE[bid][1] and self.rng.random() < 0.5
            margin = 0 if bid in ("Farm", "Tower") else 1
            if self.put(bid, x, y, self.rng.randint(lo, hi), district, turned, margin):
                return True
        return False

    # ------------------------------------------------------------ output
    def finish(self, name, stem, legend_extra=()):
        self.put("WaterTower", -6, R + 4, 3, "port", inside=False)
        self.put("WaterTower", 6, R + 4, 3, "port", inside=False)
        here = os.path.dirname(os.path.abspath(__file__))
        terrain = {}
        for x in range(-OFF, OFF + 1):
            for y in range(R + 1, SEA_TO + 1):
                terrain[f"{x + OFF},{y + OFF}"] = "sand" if y < SEA_FROM else "shallow" if y < SEA_FROM + 3 else "sea"
        blueprint = {"format": "citybuilder-base/1", "name": name, "width": 2 * OFF + 1, "height": OFF + SEA_TO + 1,
                     "buildings": [{k: (v + OFF if k in ("x", "y") else v) for k, v in it.items() if k != "district"} for it in self.items],
                     "terrain": terrain}
        with open(os.path.join(here, stem + ".json"), "w", encoding="utf-8") as f:
            json.dump(blueprint, f, ensure_ascii=False, separators=(",", ":"))
        self._draw(name, os.path.join(here, stem + ".png"), legend_extra)
        counts = {}
        for it in self.items:
            counts[it["id"]] = counts.get(it["id"], 0) + 1
        print(stem, "pieces", len(self.items), "short", self.short)
        print(sorted(counts.items(), key=lambda kv: -kv[1])[:12])

    def _draw(self, name, path, legend_extra):
        C, PAD, LEG = 18, 30, 360
        W = (2 * OFF + 1) * C + 2 * PAD + LEG
        H = (OFF + SEA_TO + 1) * C + 2 * PAD + 30
        OX, OY = PAD + OFF * C, PAD + 30 + OFF * C
        img = Image.new("RGB", (W, H), (250, 250, 247))
        d = ImageDraw.Draw(img)
        FS = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 8)
        FB = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 15)
        FL = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 12)

        def box(x, y, w, h):
            return [OX + x * C, OY + y * C, OX + (x + w) * C - 1, OY + (y + h) * C - 1]

        d.rectangle(box(-OFF, -OFF, 2 * OFF + 1, 2 * OFF + 1), fill=(218, 236, 200))
        for y in range(R + 1, SEA_TO + 1):
            col = (240, 222, 160) if y < SEA_FROM else (150, 205, 240) if y < SEA_FROM + 3 else (98, 160, 235)
            d.rectangle(box(-OFF, y, 2 * OFF + 1, 1), fill=col)

        def shade(col, level):
            k = 0.45 + 0.55 * (level - 1) / 4
            return tuple(int(255 - (255 - c) * k) for c in col)

        for it in self.items:
            bid = it["id"]
            col = DISTRICT[it["district"]][0]
            plain = False
            if bid == "Moat":
                col, plain = (100, 155, 240), True
            elif bid == "Road":
                col, plain = (246, 214, 175), True
            elif it["district"] == "wall":
                col = (110, 110, 110) if bid != "Fence" else (175, 175, 175)
            b = box(it["x"], it["y"], it["w"], it["h"])
            d.rectangle(b, fill=col if plain else shade(col, it["level"]), outline=(110, 110, 110) if it["w"] * it["h"] > 1 else None)
            label = SHORT.get(bid, "")
            if it["w"] * it["h"] > 1 and label:
                txt = label + ("" if bid in ("Square", "DecGarden") else f"\n{it['level']}") if it["h"] >= 2 or it["w"] >= 3 else label
                d.multiline_text(((b[0] + b[2]) / 2, (b[1] + b[3]) / 2), txt, fill=(25, 25, 25), font=FS, anchor="mm", align="center")
            elif bid == "Farm":
                d.text(((b[0] + b[2]) / 2, (b[1] + b[3]) / 2), "ф", fill=(70, 90, 30), font=FS, anchor="mm")
            elif bid in ("Fence", "Tower") and it["district"] == "wall":
                d.text(((b[0] + b[2]) / 2, (b[1] + b[3]) / 2), str(it["level"]), fill=(40, 40, 40), font=FS, anchor="mm")
        d.text((PAD, 8), name + " — 55x55 клеток; цифра = уровень, бледнее = слабее; цифра на стене = её уровень", fill=(20, 20, 20), font=FB)
        lx, ly = W - LEG + 10, PAD + 40
        d.text((lx, ly), "Районы", fill=(20, 20, 20), font=FB)
        ly += 26
        used = {it["district"] for it in self.items}
        for key, (col, label) in DISTRICT.items():
            if key not in used:
                continue
            d.rectangle([lx, ly, lx + 16, ly + 14], fill=col, outline=(110, 110, 110))
            d.text((lx + 24, ly), label, fill=(20, 20, 20), font=FL)
            ly += 22
        ly += 14
        for line in legend_extra:
            d.text((lx, ly), line, fill=(20, 20, 20), font=FB if line.endswith(":") else FL)
            ly += 20
        img.save(path)
