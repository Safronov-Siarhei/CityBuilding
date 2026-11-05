"""The bot's town, template D «Цитадель и посад» (the user, 2026-11-05): a small, very strong keep
-- the citadel (lvl 5) and a ring of lvl 4-5 buildings behind a lvl-5 wall -- in a wide OPEN posad of
lvl 1-2 (some workshops lvl 3): easy pickings outside, a hard nut in the middle."""
import math

from bot_town_common import Town

t = Town(41)
K = 14                                  # the keep's wall
t.citadel(0, 0)
t.wall_rect(-K, -K, K, K, 5, [("N", 0), ("S", 0), ("W", 0), ("E", 0)], tower_every=4)
for x, y in ((0, -9), (0, 9), (-9, 0), (9, 0)):
    pass
t.road(0, -13, 0, -9, 5)
t.road(0, 9, 0, 13, 5)
t.road(-13, 0, -9, 0, 5)
t.road(9, 0, 13, 0, 5)
# the posad's streets: the four roads out of the gates, and a few lanes
t.road(0, -27, 0, -15, 2)
t.road(0, 15, 0, 27, 2)
t.road(-27, 0, -15, 0, 2)
t.road(15, 0, 27, 0, 2)
t.road(-26, -20, -15, -20, 1)
t.road(15, 20, 26, 20, 1)
t.road(20, -26, 20, -15, 1)
t.road(-20, 15, -20, 26, 1)

SECTORS = ["N", "NE", "E", "SE", "S", "SW", "W", "NW"]


def region(x, y):
    d = max(abs(x), abs(y))
    if 9 <= d <= 13:
        return ("keep", "N" if y < -4 and abs(x) <= 13 and y <= -9 else "S" if y >= 9 else "W" if x < 0 else "E")
    if d >= 15:
        a = math.degrees(math.atan2(x, -y)) % 360
        return ("posad", SECTORS[int(((a + 22.5) % 360) // 45)])
    return None


PLAN = {
    ("keep", "N"): ("armoury", [("Barracks", 2, (5, 5)), ("FortifiedTower", 2, (5, 5))]),
    ("keep", "W"): ("noble", [("DiplomacyHall", 1, (5, 5)), ("Church", 1, (4, 4))]),
    ("keep", "E"): ("treasury", [("BigTreasury", 1, (5, 5)), ("TaxHouse", 1, (4, 4)), ("Treasury", 1, (4, 4))]),
    ("keep", "S"): ("healer", [("HealerHouse", 2, (4, 4)), ("Manor", 1, (4, 4))]),
    ("posad", "N"): ("fields", [("PigFarm", 1, (1, 2)), ("Orchard", 2, (1, 2))]),
    ("posad", "NE"): ("craft", [("IronMine", 1, (2, 3)), ("CoalMine", 1, (2, 3)), ("Smelter", 2, (2, 3)), ("Sawmill", 2, (1, 2)), ("Quarry", 2, (1, 2))]),
    ("posad", "E"): ("living", [("Cottage", 4, (1, 2)), ("Hovel", 4, (1, 1)), ("Well", 2, (1, 1))]),
    ("posad", "SE"): ("market", [("Colosseum", 1, (2, 2)), ("TownSquare", 2, (1, 2)), ("Tavern", 3, (1, 2)), ("Theater", 1, (2, 2))]),
    ("posad", "S"): ("port", [("Laboratory", 1, (3, 3)), ("Shipyard", 1, (3, 3)), ("Lighthouse", 1, (3, 3)), ("Warehouse", 1, (2, 2)), ("FisherHut", 4, (1, 2))]),
    ("posad", "SW"): ("food", [("BigGranary", 1, (2, 3)), ("Granary", 2, (1, 2)), ("Smokehouse", 2, (1, 2)), ("Baker", 2, (1, 2))]),
    ("posad", "W"): ("posad", [("Hovel", 4, (1, 2)), ("Well", 2, (1, 1)), ("FireBrigade", 1, (2, 2))]),
    ("posad", "NW"): ("fields", [("Orchard", 1, (1, 2)), ("CopperMine", 1, (2, 2)), ("GoldMine", 1, (2, 2))]),
}
t.districts(region, PLAN, farm_blocks={"fields": 6}, fill_cap={"fields": 14, "posad": 16, "living": 14, "craft": 10, "food": 10, "market": 8})
t.finish("Город бота D «Цитадель и посад»", "bot_town_d",
         ("Пояса силы:", "Посад: ур. 1-2 (мастерские до 3), без стен", "Крепость: ур. 4-5, стена ур. 5", "Цитадель: ур. 5, ров", "",
          "Лёгкая добыча снаружи,", "крепкий орешек в центре"))
