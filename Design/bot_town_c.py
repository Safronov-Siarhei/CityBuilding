"""The bot's town, template C «Город кварталов» (the user, 2026-11-05): nine 17x17 quarters on a
3x3 grid with streets between them, each quarter walled on its own at its own strength -- the
citadel in the middle (lvl 5), the noble and armoury quarters on the far side (walls lvl 4), the
craft, food and living quarters (walls lvl 3), the market (wall lvl 2), the open posad and the port
on the shore. Each quarter is a target of its own."""
from bot_town_common import Town

t = Town(31)
BLOCKS = {-1: (-27, -11), 0: (-8, 8), 1: (11, 27)}

# the streets between the quarters, and the harbour street
# the lanes between quarters stay grass (walkable, cheap); paved only from the harbour to the citadel
t.road(0, 9, 0, 27, 2)
t.citadel(0, 0)

# (bx, by) -> wall level (None = open), gates [(side, at)]
QUARTERS = {
    (-1, -1): (4, [("S", -19), ("E", -19)]), (0, -1): (4, [("S", 0), ("W", -19), ("E", -19)]), (1, -1): (3, [("S", 19), ("W", -19)]),
    (-1, 0): (3, [("E", 0), ("N", -19), ("S", -19)]), (1, 0): (3, [("W", 0), ("N", 19), ("S", 19)]),
    (-1, 1): (None, []), (0, 1): (2, [("N", 0)]), (1, 1): (2, [("N", 19), ("W", 19)]),
}
for (bx, by), (level, gates) in QUARTERS.items():
    if level is None:
        continue
    x0, x1 = BLOCKS[bx]
    y0, y1 = BLOCKS[by]
    # the port quarter stays open to the sea
    skip = (lambda x, y, y1=y1: y == y1) if (bx, by) == (0, 1) else None
    t.wall_rect(x0, y0, x1, y1, level, gates, tower_every=4, skip=skip)


def block(v):
    for k, (a, b) in BLOCKS.items():
        if a <= v <= b:
            return k
    return None


def region(x, y):
    bx, by = block(x), block(y)
    if bx is None or by is None or (bx, by) == (0, 0):
        return None
    return (bx, by)


PLAN = {
    (-1, -1): ("noble", [("DiplomacyHall", 1, (5, 5)), ("Church", 1, (4, 4)), ("Theater", 1, (4, 4)), ("Manor", 3, (3, 4))]),
    (0, -1): ("armoury", [("Barracks", 3, (4, 4)), ("FortifiedTower", 2, (4, 4)), ("Smelter", 2, (3, 4)), ("BigWarehouse", 1, (4, 4))]),
    (1, -1): ("craft", [("IronMine", 1, (3, 3)), ("CoalMine", 1, (3, 3)), ("GoldMine", 1, (3, 3)), ("Smelter", 2, (3, 3)), ("Sawmill", 2, (2, 3)), ("Quarry", 2, (2, 3))]),
    (-1, 0): ("living", [("Manor", 2, (3, 3)), ("Cottage", 6, (2, 3)), ("HealerHouse", 1, (3, 3)), ("FireBrigade", 1, (3, 3)), ("Well", 2, (2, 3))]),
    (1, 0): ("food", [("BigGranary", 1, (3, 3)), ("Granary", 2, (2, 3)), ("Smokehouse", 2, (2, 3)), ("Baker", 2, (2, 3)), ("Orchard", 1, (2, 3))]),
    (-1, 1): ("fields", [("PigFarm", 1, (1, 2)), ("Orchard", 2, (1, 2)), ("Hovel", 4, (1, 2)), ("Well", 1, (1, 1))]),
    (0, 1): ("port", [("Laboratory", 1, (3, 3)), ("Shipyard", 1, (3, 3)), ("Lighthouse", 1, (3, 3)), ("Warehouse", 1, (2, 2)), ("FisherHut", 4, (1, 2))]),
    (1, 1): ("market", [("Colosseum", 1, (2, 3)), ("BigTreasury", 1, (3, 3)), ("TaxHouse", 1, (3, 3)), ("TownSquare", 2, (2, 3)), ("Tavern", 3, (2, 2))]),
}
t.districts(region, PLAN, farm_blocks={"fields": 6}, fill_cap={"fields": 16})
t.finish("Город бота C «Город кварталов»", "bot_town_c",
         ("Кварталы (у каждого своя стена):", "Знать, оружейный: стены ур. 4", "Ремесленный, жилой, продуктовый: ур. 3",
          "Рынок и казна: стена ур. 2", "Поля и порт: открыты", "Цитадель: ур. 5, ров", "",
          "Каждый квартал - отдельная цель:", "можно взять, не трогая соседей"))
