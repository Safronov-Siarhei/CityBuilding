"""The bot's town, template B «Приморский город» (the user, 2026-11-05): the town climbs from the
harbour to the citadel at the island's far end, so the player crosses the whole town to reach the
Town Hall. Strips from the shore: port + posad (lvl 1-2, open), the lower town behind a lvl-3 wall,
the upper town behind a lvl-4 wall, the citadel (lvl 5) with its moat at the back."""
from bot_town_common import Town

t = Town(21)

# the main street from the harbour up to the citadel, a cross street in each town
t.road(0, -11, 0, 27, 3)
t.road(-26, 7, 26, 7, 2)
t.road(-16, -6, 16, -6, 3)
t.citadel(0, -19, bridges=("S", "W", "E"))
t.wall_rect(-17, -27, 17, 0, 4, [("S", 0), ("W", -14), ("E", -14)], tower_every=5)
t.wall_rect(-27, -27, 27, 14, 3, [("S", 0), ("S", -14), ("S", 14)], tower_every=6,
            skip=lambda x, y: y == -27 and abs(x) <= 17)


def region(x, y):
    if -26 <= y <= -1 and -16 <= x <= 16:                  # the upper town, round the citadel
        if abs(x) <= 9 and y <= -10:
            return None
        if y <= -7:
            return ("upper", "W" if x < 0 else "E")
        return ("upper", "SW" if x < -5 else "SE" if x > 5 else "S")
    if -26 <= y <= 13 and -26 <= x <= 26:                  # the lower town
        if x <= -18:
            return ("middle", "W")
        if x >= 18:
            return ("middle", "E")
        if y >= 8:
            return ("middle", "S" if abs(x) <= 6 else "SW" if x < 0 else "SE")
        return ("middle", "C")
    if y >= 15:                                            # the shore
        if abs(x) <= 9:
            return ("posad", "port")
        return ("posad", "W" if x < 0 else "E")
    return None


PLAN = {
    ("upper", "W"): ("noble", [("DiplomacyHall", 1, (5, 5)), ("Church", 1, (4, 4)), ("Manor", 1, (4, 4))]),
    ("upper", "E"): ("armoury", [("Barracks", 2, (4, 4)), ("FortifiedTower", 1, (4, 4)), ("Smelter", 1, (4, 4))]),
    ("upper", "SW"): ("noble", [("Theater", 1, (3, 4)), ("Manor", 1, (3, 4))]),
    ("upper", "S"): ("treasury", [("BigTreasury", 1, (4, 4)), ("TaxHouse", 1, (4, 4))]),
    ("upper", "SE"): ("healer", [("HealerHouse", 2, (3, 4)), ("FireBrigade", 1, (3, 3))]),
    ("middle", "W"): ("craft", [("Sawmill", 2, (2, 3)), ("Quarry", 2, (2, 3)), ("IronMine", 1, (2, 3)), ("CoalMine", 1, (2, 3)), ("BigWarehouse", 1, (3, 3))]),
    ("middle", "E"): ("food", [("BigGranary", 1, (3, 3)), ("Granary", 2, (2, 2)), ("Smokehouse", 2, (2, 3)), ("Baker", 2, (2, 3))]),
    ("middle", "C"): ("armoury", [("Barracks", 1, (3, 3)), ("Smelter", 1, (2, 3)), ("CopperMine", 1, (2, 2)), ("GoldMine", 1, (2, 2))]),
    ("middle", "SW"): ("living", [("Cottage", 4, (2, 3)), ("Well", 1, (2, 2))]),
    ("middle", "S"): ("market", [("Colosseum", 1, (3, 3)), ("TownSquare", 2, (3, 3)), ("Tavern", 2, (2, 3))]),
    ("middle", "SE"): ("living", [("Cottage", 4, (2, 3)), ("Manor", 1, (3, 3))]),
    ("posad", "port"): ("port", [("Laboratory", 1, (3, 3)), ("Shipyard", 1, (3, 3)), ("Lighthouse", 1, (3, 3)), ("Warehouse", 1, (2, 2)), ("FisherHut", 4, (1, 2))]),
    ("posad", "W"): ("posad", [("PigFarm", 1, (1, 2)), ("Orchard", 1, (1, 2)), ("Well", 1, (1, 1))]),
    ("posad", "E"): ("fields", [("Orchard", 1, (1, 2)), ("Hovel", 3, (1, 2))]),
}
t.districts(region, PLAN)
t.finish("Город бота B «Приморский город»", "bot_town_b",
         ("Пояса силы (от моря к цитадели):", "Берег: порт и посад, ур. 1-2, без стены", "Нижний город: ур. 2-3, стена ур. 3",
          "Верхний город: ур. 3-4, стена ур. 4", "Цитадель: ур. 5, ров с трёх сторон", "",
          "Ратуша в дальнем конце: чтобы дойти,", "надо пройти весь город"))
