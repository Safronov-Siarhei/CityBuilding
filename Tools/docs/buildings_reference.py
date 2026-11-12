"""Writes Design/Buildings.md: the ACTIVE building roster with every level's icon, its numbers per
level and a short note on what it does (the user, 2026-11-12: «файл, где будут расписаны здания,
их иконки для каждого уровня ... постоянное обновление, чтобы поддерживать активный состав»).

Everything comes from the project, so the file follows the game by itself:
  - the roster, names, categories and per-level numbers: Assets/_Project/Balance/buildings.csv
    (an empty level cell repeats the level below, as the sheet's rule says);
  - food a day and the bakery/smokehouse bonus: economy.csv (food_day_<id>_N, food_boost_<id>_N);
  - which buildings are put away: BuildingIds.Hidden in Scripts/Buildings/BuildingIds.cs;
  - the icons: Assets/_Project/UI/Icons/<Id>.png, <Id>_L2..L5.png (render_icons.py);
  - the notes: Design/buildings/descriptions.tsv (the one hand-written part; a building without a
    note is flagged in the file and on the console); its optional third column = how many levels the
    building has (empty = 5), so a one-level building is not flagged for missing level icons.

Run by Tools/research/run.ps1 after every `setup` step, and by hand:  python Tools/docs/buildings_reference.py
"""
import csv
import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
BALANCE = os.path.join(ROOT, "Assets", "_Project", "Balance")
ICONS = os.path.join(ROOT, "Assets", "_Project", "UI", "Icons")
IDS = os.path.join(ROOT, "Assets", "_Project", "Scripts", "Buildings", "BuildingIds.cs")
NOTES = os.path.join(ROOT, "Design", "buildings", "descriptions.tsv")
OUT = os.path.join(ROOT, "Design", "Buildings.md")
LEVELS = 5
ICON_PX = 72

CATEGORIES = [
    ("City", "Город"), ("Storage", "Склады"), ("Production", "Производство"), ("Food", "Еда"),
    ("Defence", "Оборона"), ("Water", "Водные"), ("Entertainment", "Развлечения и украшения"),
]

# per-level numbers worth showing: (column, label); a building shows only those it has
STATS = [
    ("max_health", "Прочность"), ("defense", "Защита"), ("housing_capacity", "Жильё"),
    ("storage_capacity", "Склад"), ("max_workers", "Рабочие"), ("service_radius", "Радиус"),
    ("harvest_radius", "Радиус сбора"),
]


def read_csv(name):
    with io.open(os.path.join(BALANCE, name), encoding="utf-8-sig", newline="") as f:
        return list(csv.reader(f))


def num(text):
    text = (text or "").strip().replace(",", ".")
    try:
        value = float(text)
    except ValueError:
        return None
    return int(value) if value == int(value) else value


def fmt(value):
    return str(value).replace(".", ",")


def hidden_ids():
    src = io.open(IDS, encoding="utf-8").read()
    m = re.search(r"HashSet<string>\s+Hidden\s*=\s*new[^{]*\{([^}]*)\}", src)
    return set(re.findall(r'"(\w+)"', m.group(1))) if m else set()


def economy():
    out = {}
    for row in read_csv("economy.csv")[1:]:
        if len(row) >= 2 and row[0]:
            out[row[0].strip()] = num(row[1])
    return out


def sandbox():
    """The sandbox tab's building table (sandbox.csv, first table: «building» header): build price
    (level 1), upgrade prices up2..up5 and build seconds per level -- what the player pays in the sandbox."""
    table = read_csv("sandbox.csv")
    header, out = table[0], {}
    for r in table[1:]:
        if not r or not r[0].strip():
            continue
        if r[0].strip() in ("unit", "key"):
            break
        out[r[0].strip()] = dict(zip(header, r))
    return out


RES = (("wood", "дерево"), ("stone", "камень"), ("iron", "железо"), ("coal", "уголь"), ("gold", "золото"), ("coins", "монеты"))


def price(row, lv):
    parts = []
    for res, label in RES:
        col = f"cost_{res}" if lv == 1 else f"up{lv}_{res}"
        v = num(row.get(col, ""))
        if v:
            parts.append(f"{label} {fmt(v)}")
    return ", ".join(parts) if parts else "—"


def per_level(row, column):
    """The column's value at levels 1..5: column, column_2.. column_5; an empty cell repeats the one below."""
    values, last = [], None
    for lv in range(1, LEVELS + 1):
        key = column if lv == 1 else f"{column}_{lv}"
        v = num(row.get(key, ""))
        if v is None:
            v = last
        values.append(v)
        last = v
    return values


def icon(bid, lv):
    name = f"{bid}.png" if lv == 1 else f"{bid}_L{lv}.png"
    return name if os.path.exists(os.path.join(ICONS, name)) else None


def main():
    table = read_csv("buildings.csv")
    header = table[0]
    known = {cat for cat, _ in CATEGORIES}
    rows = [dict(zip(header, r)) for r in table[1:] if len(r) > 2 and r[0].strip() and r[2].strip() in known]
    hidden = hidden_ids()
    eco = economy()
    sand = sandbox()
    notes, levels = {}, {}
    with io.open(NOTES, encoding="utf-8-sig", newline="") as f:
        for r in csv.reader(f, delimiter="\t"):
            if len(r) >= 2 and r[0] != "id":
                notes[r[0].strip()] = r[1].strip()
                if len(r) >= 3 and r[2].strip().isdigit():
                    levels[r[0].strip()] = int(r[2])

    active = [r for r in rows if r["id"] not in hidden]
    missing_notes = [r["id"] for r in active if r["id"] not in notes]
    missing_icons = [f'{r["id"]} ур.{lv}' for r in active for lv in range(1, levels.get(r["id"], LEVELS) + 1) if icon(r["id"], lv) is None]

    out = []
    w = out.append
    w("# Здания: активный состав")
    w("")
    w("> Файл СОБИРАЕТСЯ САМ (`Tools/docs/buildings_reference.py`, после каждого `setup` в `run.ps1`): не правь его руками.")
    w("> Состав и числа — лист «Здания» (`buildings.csv`), еда — `economy.csv`, скрытые — `BuildingIds.Hidden`,")
    w("> иконки — `UI/Icons`, описания — `Design/buildings/descriptions.tsv` (новое здание: добавь туда строку).")
    w("> Цены и время стройки — песочницы (лист «Песочница: здания», `sandbox.csv`; ур.1 = постройка, дальше — улучшение);")
    w("> остальные числа — базовые; Нашествие переопределяет своё своим листом.")
    w("")
    w(f"Активных зданий: **{len(active)}** (скрыто: {len(hidden & {r['id'] for r in rows})}).")
    w("")
    if missing_notes:
        w(f"⚠ Без описания: {', '.join(missing_notes)}.")
        w("")
    if missing_icons:
        w(f"⚠ Нет иконки: {', '.join(missing_icons)}.")
        w("")
    w("**Разделы:** " + " · ".join(f"[{ru}](#{ru.lower().replace(' ', '-')})" for cat, ru in CATEGORIES if any(r["category"] == cat for r in active)))
    w("")

    for cat, ru in CATEGORIES:
        group = [r for r in active if r["category"] == cat]
        if not group:
            continue
        w(f"## {ru}")
        w("")
        for r in group:
            bid = r["id"]
            w(f"### {r['display_name']} `{bid}`")
            w("")
            w(notes.get(bid, "⚠ *описание не написано — добавь строку в `Design/buildings/descriptions.tsv`*"))
            w("")
            heads = "| | " + " | ".join(f"ур. {lv}" for lv in range(1, LEVELS + 1)) + " |"
            w(heads)
            w("|---|" + "---|" * LEVELS)
            cells = []
            top = levels.get(bid, LEVELS)
            for lv in range(1, LEVELS + 1):
                if lv > top:
                    cells.append("нет")
                    continue
                name = icon(bid, lv)
                cells.append(f'<img src="../Assets/_Project/UI/Icons/{name}" width="{ICON_PX}">' if name else "—")
            w("| | " + " | ".join(cells) + " |")
            for column, label in STATS:
                vals = per_level(r, column)
                if all(v in (None, 0) for v in vals):
                    continue
                w(f"| {label} | " + " | ".join(fmt(v) if v is not None else "—" for v in vals) + " |")
            low = bid.lower()
            for key, label in ((f"food_day_{low}", "Еда в день"), (f"food_boost_{low}", "Бонус еды соседям")):
                vals = [eco.get(f"{key}_{lv}") for lv in range(1, LEVELS + 1)]
                if any(v is not None for v in vals):
                    w(f"| {label} | " + " | ".join(fmt(v) if v is not None else "—" for v in vals) + " |")
            sb = sand.get(bid)
            if sb:
                w("| Цена (песочница) | " + " | ".join(price(sb, lv) if lv <= top else "нет" for lv in range(1, LEVELS + 1)) + " |")
                secs = per_level(sb, "build_sec")
                if any(secs):
                    w("| Стройка, с (песочница) | " + " | ".join(fmt(v) if v is not None and lv <= top else ("нет" if lv > top else "—")
                                                       for lv, v in zip(range(1, LEVELS + 1), secs)) + " |")
            else:
                w("| Цена (базовая) | " + price(r, 1) + " | | | | |")
            w("")

    if any(r["id"] in hidden for r in rows):
        w("## Скрыты (данные и модели есть, в игре не строятся)")
        w("")
        for r in sorted((r for r in rows if r["id"] in hidden), key=lambda r: r["id"]):
            w(f"- {r['display_name']} `{r['id']}` — {notes.get(r['id'], 'скрыто')}")
        w("")

    text = "\n".join(out)
    old = io.open(OUT, encoding="utf-8").read() if os.path.exists(OUT) else None
    if old != text:
        with io.open(OUT, "w", encoding="utf-8", newline="\n") as f:
            f.write(text)
    print(f"buildings reference: {len(active)} active, {'updated' if old != text else 'unchanged'}"
          + (f"; NO NOTE: {', '.join(missing_notes)}" if missing_notes else "")
          + (f"; NO ICON: {len(missing_icons)}" if missing_icons else ""))
    return 0


if __name__ == "__main__":
    sys.exit(main())
