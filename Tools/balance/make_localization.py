"""Build Assets/_Project/Balance/localization.csv.

Names come straight out of the other tabs so the Russian column can never drift from what the
sheet already says; everything else is written here. An id with no English name is a hard error
rather than a blank cell -- a silently untranslated building is exactly what this file exists to
prevent.
"""
import csv
import io

BUILDINGS_EN = {
    "Hovel": "Hovel", "Cottage": "Cottage", "Manor": "Manor", "Castle": "Town Hall",
    "Road": "Road", "Well": "Well", "FireBrigade": "Fire Brigade",
    "HealerHouse": "Healer's House", "Laboratory": "Laboratory",
    "Warehouse": "Warehouse", "Granary": "Pantry", "Treasury": "Treasury", "Barn": "Barn",
    "BigWarehouse": "Great Warehouse", "BigGranary": "Great Pantry",
    "BigTreasury": "Great Treasury", "BigBarn": "Great Barn",
    "TownSquare": "Town Square", "Tavern": "Tavern", "Theater": "Theatre", "Church": "Church",
    "Colosseum": "Colosseum", "DecGarden": "Ornamental Garden", "DecTree": "Ornamental Tree",
    "DecBush": "Ornamental Bush", "Flag": "Flag",
    "Fence": "Fence", "Gate": "Gate", "Tower": "Tower", "Barracks": "Barracks",
    "FortifiedFence": "Fortified Fence", "FortifiedGate": "Fortified Gate",
    "FortifiedTower": "Fortified Tower",
    "Sawmill": "Sawmill", "Quarry": "Quarry", "IronMine": "Iron Mine", "GoldMine": "Gold Mine",
    "CopperMine": "Copper Mine", "CoalMine": "Coal Mine", "Smelter": "Smelter",
    "Orchard": "Orchard", "Farm": "Farm", "FisherHut": "Fisherman's Hut", "PigFarm": "Pig Farm",
    "Smokehouse": "Smokehouse", "Windmill": "Windmill", "Baker": "Bakery",
    "Bridge": "Bridge", "Dock": "Dock", "WaterMill": "Water Mill",
}

RECIPES_EN = {
    "wood": "Timber", "stone": "Stone", "iron_ore": "Iron Ore", "coal": "Coal",
    "copper_ore": "Copper Ore", "gold_ore": "Gold Ore", "fish": "Food", "grain": "Wheat",
    "flour": "Flour", "bread": "Bread",
    "iron_bar": "Iron Bar", "copper_bar": "Copper Bar", "gold_bar": "Gold Bar",
}

RESOURCES = [
    ("Wood", "дерево", "timber"), ("Stone", "камень", "stone"), ("Food", "еда", "food"),
    ("Gold", "золотая руда", "gold ore"), ("Population", "жители", "citizens"),
    ("Iron", "железная руда", "iron ore"), ("Coal", "уголь", "coal"),
    ("Coins", "монеты", "coins"), ("Grain", "пшеница", "wheat"), ("Flour", "мука", "flour"),
    ("Bread", "хлеб", "bread"), ("CopperOre", "медная руда", "copper ore"),
    ("IronBar", "железный слиток", "iron bar"), ("CopperBar", "медный слиток", "copper bar"),
    ("GoldBar", "золотой слиток", "gold bar"),
]

CATEGORIES = [
    ("City", "Город", "City"), ("Storage", "Склады", "Storage"),
    ("Entertainment", "Развлечения", "Leisure"), ("Defence", "Оборонительные", "Defence"),
    ("Production", "Производственные", "Industry"), ("Food", "Производство еды", "Food"),
    ("Water", "Водные", "Water"),
]

TIERS = [
    ("hamlet", "Хутор", "Hamlet"), ("village", "Деревня", "Village"),
    ("town", "Городок", "Town"), ("city", "Город", "City"),
    ("kingdom", "Королевство", "Kingdom"),
]

UI = [
    # Main menu
    ("#menu_title", "СТРОИТЕЛЬ ГОРОДОВ", "CITY BUILDER", ""),
    ("#menu_subtitle", "мобильный градостроитель", "a mobile city builder", ""),
    ("#menu_new_game", "Новая игра", "New Game", ""),
    ("#menu_load_game", "Загрузить игру", "Load Game", ""),
    ("#menu_settings", "Настройки", "Settings", ""),
    ("#menu_quit", "Выход", "Quit", ""),

    # Settings
    ("#settings_title", "Настройки", "Settings", ""),
    ("#settings_language", "Язык", "Language", ""),
    ("#settings_back", "Назад", "Back", ""),

    # Shared
    ("#ui_close", "Закрыть", "Close", ""),
    ("#ui_cancel", "Отмена", "Cancel", ""),
    ("#ui_back", "Назад", "Back", ""),
    ("#ui_free", "Бесплатно", "Free", ""),

    # In-game top row
    ("#hud_menu", "Меню", "Menu", ""),
    ("#hud_save", "Сохранить", "Save", ""),
    ("#hud_workforce", "Жители", "Citizens", ""),
    ("#hud_status", "{0} · День {1}", "{0} · Day {1}", "{0} — ступень поселения, {1} — номер дня"),
    ("#hud_place_hint", "Выберите место для Ратуши ({0}x{1})", "Choose a spot for the Town Hall ({0}x{1})", ""),

    # Exit dialog
    ("#exit_title", "Выйти в главное меню?", "Return to the main menu?", ""),
    ("#exit_warning", "Несохранённые изменения будут потеряны.", "Unsaved progress will be lost.", ""),
    ("#exit_confirm", "Выйти", "Quit", ""),

    # Save / load
    ("#save_title", "Сохранить игру", "Save Game", ""),
    ("#save_confirm", "Сохранить", "Save", ""),
    ("#load_title", "Загрузить игру", "Load Game", ""),
    ("#load_empty", "Нет сохранений", "No saved games", ""),
    ("#load_confirm", "Загрузить", "Load", ""),
    ("#load_selected", "Выбрано: {0}", "Selected: {0}", ""),

    # Building card
    ("#building_level", "Уровень: {0} / {1}", "Level: {0} / {1}", ""),
    ("#building_condition", "Прочность: {0}/{1}   Защита: {2}   Ветхость: {3}",
     "Health: {0}/{1}   Defence: {2}   Wear: {3}", ""),
    ("#building_no_decay", "не изнашивается", "never wears out", ""),
    ("#building_workers", "Рабочие: {0} / {1}", "Workers: {0} / {1}", ""),
    ("#building_idle_citizens", "Свободных жителей: {0}", "Idle citizens: {0}", ""),
    ("#building_produces", "Производит: {0} / {1} с на работника",
     "Produces: {0} / {1}s per worker", "{0} — рецепт, {1} — интервал в секундах"),
    # No leading +, -, = or @ anywhere in this file: Google Sheets parses a cell starting with
    # one as a formula, and the whole string comes back as #NAME?. These two used to read
    # "+ Назначить" / "- Снять" and did exactly that.
    ("#building_assign", "Назначить", "Hire", ""),
    ("#building_unassign", "Снять", "Dismiss", ""),
    ("#building_upgrade", "Улучшить", "Upgrade", ""),
    ("#building_repair", "Отремонтировать", "Repair", ""),
    ("#building_recruit", "Нанять ополченца", "Recruit militia", ""),

    # Recipes in words
    ("#recipe_conversion", "{0} -> {1}", "{0} -> {1}", "{0} — что уходит, {1} — что получается"),
    ("#recipe_batches", " x{0}", " x{0}", "Множитель партий, если их больше одной"),

    # Workforce panel
    ("#workforce_title", "Жители и работы", "Citizens and Work", ""),
    ("#workforce_empty", "Пока негде работать — постройте производство",
     "Nowhere to work yet — build something that produces", ""),
    ("#workforce_summary", "Свободных жителей: {0}   ·   Всего: {1}",
     "Idle: {0}   ·   Total: {1}", ""),
    ("#workforce_gives", "даёт {0}", "makes {0}", ""),
    ("#workforce_idle_building", "ничего не производит", "produces nothing", ""),

    # Happiness / tax
    ("#happiness_title", "Довольство: {0}%", "Contentment: {0}%", ""),
    # "\n" as two characters, not a real line break: a cell holding a newline comes back from a
    # published CSV as two rows. BalanceImporter turns it into a real break on import.
    ("#happiness_breakdown", "Налог {0} · Ветхость {1} · Оборона {2}\\nЕда {3} · Потери {4}",
     "Tax {0} · Wear {1} · Defence {2}\\nFood {3} · Losses {4}", ""),
    ("#tax_rate", "Налог: {0}%", "Tax: {0}%", ""),

    # Army
    ("#army_summary", "Армия: {0}/{1}   Содержание: {2} мон./день",
     "Army: {0}/{1}   Upkeep: {2} coins/day", ""),
    ("#army_target_units", "Цель: враги", "Target: enemies", ""),
    ("#army_target_buildings", "Цель: строения", "Target: buildings", ""),
    ("#army_full", "Армия полна ({0})", "The army is full ({0})", ""),
    ("#army_no_citizens", "Нет свободных жителей", "No idle citizens", ""),
    ("#army_no_coins", "Не хватает монет", "Not enough coins", ""),
    ("#unit_militia", "Ополчение", "Militia", ""),
    ("#unit_militia_flavour", "жители с вилами", "townsfolk with pitchforks", ""),

    # Game over
    ("#over_victory", "Победа", "Victory", ""),
    ("#over_defeat", "Поражение", "Defeat", ""),
    ("#over_victory_reason", "Все порталы орков закрыты.", "Every orc portal is closed.", ""),
    ("#over_defeat_reason", "Ратуша разрушена.", "The Town Hall is destroyed.", ""),
    ("#over_to_menu", "В главное меню", "Main menu", ""),

    # Event log
    ("#log_day", "День {0}: {1}", "Day {0}: {1}", ""),
    ("#log_built", "Построено: {0}", "Built: {0}", ""),
    ("#log_destroyed_decay", "Разрушено от ветхости: {0}", "Fell apart from neglect: {0}", ""),
    ("#log_destroyed_combat", "Разрушено в бою: {0}", "Destroyed in battle: {0}", ""),
    ("#log_no_input", "{0}: не хватает сырья ({1})", "{0}: out of raw material ({1})", ""),
    ("#log_no_storage", "Некуда складывать: {0} работает впустую, нужно хранилище",
     "Nowhere to put it: {0} is working for nothing, build a storehouse", ""),
    ("#log_fed", "Поселение снова сыто.", "The settlement is fed again.", ""),
    ("#log_hungry", "Голод: не хватает еды ({0}). Люди начнут умирать, если не накормить.",
     "Hunger: {0} short. People will start dying unless they are fed.", ""),
    ("#log_starved", "Голод: умерло жителей — {0}", "Starvation: citizens lost — {0}", ""),
    ("#log_recruited", "{0}: нанят боец ({1}/{2})", "{0}: soldier recruited ({1}/{2})", ""),
    ("#log_soldier_died", "Боец погиб в бою.", "A soldier fell in battle.", ""),
    ("#log_disbanded", "Нечем платить содержание: распущено бойцов — {0}",
     "No coins for upkeep: soldiers disbanded — {0}", ""),
    ("#log_portal_opened", "На карте открылся портал орков.", "An orc portal has opened.", ""),
    ("#log_portal_destroyed", "Портал орков разрушен!", "The orc portal is destroyed!", ""),
    ("#log_raid", "Орки начали набег ({0})", "The orcs are raiding ({0})", ""),
    ("#log_happiness_low", "Довольство упало до критического уровня ({0}%)",
     "Contentment has fallen to a critical level ({0}%)", ""),
    ("#log_happiness_recovered", "Довольство восстановилось ({0}%)", "Contentment has recovered ({0}%)", ""),
    ("#log_defeat_townhall", "Поражение: Ратуша разрушена.", "Defeat: the Town Hall is destroyed.", ""),
    ("#log_defeat_empty", "Поражение: в поселении не осталось жителей.",
     "Defeat: no citizens remain in the settlement.", ""),
    ("#log_victory", "Победа: все порталы орков закрыты!", "Victory: every orc portal is closed!", ""),
    ("#log_tier_up", "Поселение выросло. Теперь это: {0}.", "The settlement has grown. It is now a {0}.", ""),
]


def column(path, name):
    with io.open(path, encoding="utf-8-sig", newline="") as f:
        rows = list(csv.reader(f))
    header = [c.strip() for c in rows[0]]
    out = []
    for row in rows[1:]:
        if not row or not row[0].strip() or " " in row[0].strip():
            continue
        out.append((row[0].strip(), row[header.index(name)].strip(), row))
    return out, header


def key_of(section, name):
    """`#section_name`, all lowercase -- the format the user picked on 2026-08-17.

    Derived keys lowercase the id, so `FortifiedTower` becomes `#building_fortifiedtower`; the
    code builds the same string with ToLowerInvariant and nothing has to keep a mapping table.
    """
    return f"#{section}_{name}".lower()


rows = [["key", "ru", "en", "comment"]]

for key, ru, en, comment in UI:
    rows.append([key, ru, en, comment])

for code, ru, en in TIERS:
    rows.append([key_of("tier", code), ru, en, ""])

for name, ru, en in CATEGORIES:
    rows.append([key_of("category", name), ru, en, ""])

for name, ru, en in RESOURCES:
    rows.append([key_of("resource", name), ru, en, ""])

buildings, _ = column("Assets/_Project/Balance/buildings.csv", "display_name")
for building_id, ru, _row in buildings:
    if building_id not in BUILDINGS_EN:
        raise SystemExit(f"no English name for building '{building_id}'")
    rows.append([key_of("building", building_id), ru, BUILDINGS_EN[building_id], ""])

with io.open("Assets/_Project/Balance/recipes.csv", encoding="utf-8-sig", newline="") as f:
    recipe_rows = list(csv.reader(f))
recipe_header = [c.strip() for c in recipe_rows[0]]
for row in recipe_rows[1:]:
    if not row or not row[0].strip() or " " in row[0].strip():
        continue
    recipe_id = row[recipe_header.index("recipe_id")].strip()
    if recipe_id not in RECIPES_EN:
        raise SystemExit(f"no English name for recipe '{recipe_id}'")
    rows.append([key_of("recipe", recipe_id), row[recipe_header.index("display_name")].strip(),
                 RECIPES_EN[recipe_id], ""])

rows.append(["Ключ читает код: переименование значит потерянную надпись (в игре останется сам ключ).", "", "", ""])
rows.append(["Текст не должен начинаться с плюса, минуса, равно или собаки: таблица примет его за формулу и покажет ошибку.", "", "", ""])
rows.append(["Пустая ячейка перевода = показывается русский. Фигурные скобки {0} обязаны остаться на месте.", "", "", ""])

with io.open("Assets/_Project/Balance/localization.csv", "w", encoding="utf-8-sig", newline="") as f:
    csv.writer(f, lineterminator="\n").writerows(rows)

print("keys:", len(rows) - 3)
