"""Writes the game's reference, Design/Reference.md and Design/Reference.html (pictures inside, opens
anywhere): the ACTIVE building roster with every level's icon, its numbers per level and a short note on
what it does, then the ARMY -- soldiers, siege machines and ships with a picture per level, their numbers
per level and a note (the user, 2026-11-12: «переделай файл под общий ... видны уровни солдат») (the user, 2026-11-12: «файл, где будут расписаны здания,
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

Army: units.csv (numbers; levels 2-3 as column_2/_3), Design/reference/units.tsv (notes), pictures
Design/reference/units/<Model>[-lvlN].png (Tools/blender/render_unit_cards.py; ships use their HUD
portraits from UI/Icons).

Run by Tools/research/run.ps1 after every `setup` step, and by hand:  python Tools/docs/reference.py
"""
import base64
import csv
import html
import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
BALANCE = os.path.join(ROOT, "Assets", "_Project", "Balance")
ICONS = os.path.join(ROOT, "Assets", "_Project", "UI", "Icons")
IDS = os.path.join(ROOT, "Assets", "_Project", "Scripts", "Buildings", "BuildingIds.cs")
NOTES = os.path.join(ROOT, "Design", "buildings", "descriptions.tsv")
OUT = os.path.join(ROOT, "Design", "Reference.md")
OUT_HTML = os.path.join(ROOT, "Design", "Reference.html")
UNIT_NOTES = os.path.join(ROOT, "Design", "reference", "units.tsv")
UNIT_PICS = os.path.join(ROOT, "Design", "reference", "units")
UNIT_LEVELS = 3
# the player's army in the reference: (group, [(units.csv id, picture base, or None = a ship's HUD portrait)])
ARMY = [
    ("Солдаты", [("militia", "SoldierMilitia"), ("spearman", "SoldierSpearman"), ("archer", "SoldierArcher"),
                 ("manatarms", "SoldierManAtArms"), ("crossbowman", "SoldierCrossbow"), ("knight", "SoldierKnight"),
                 ("monk", "SoldierMonk")]),
    ("Осадные машины", [("ram", "SiegeRam"), ("ballista", "SiegeBallista"), ("catapult", "SiegeCatapult"),
                        ("siege_tower", "SiegeTower")]),
    ("Корабли", [("ship_scout", None), ("ship_transport", None), ("ship_war", None), ("ship_battleship", None)]),
]
UNIT_STATS = [
    ("max_health", "Здоровье"), ("attack_damage", "Урон"), ("attack_interval_sec", "Удар раз в, с"),
    ("attack_range_units", "Дальность"), ("walk_speed", "Скорость"), ("capacity", "Вместимость"),
]
LEVELS = 5
THUMB = 80

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
        if r[0].strip() == "building":   # the next tab of buildings (one per kind since 2026-11-12), its own columns
            header = r
            continue
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


def thumb(name):
    """The picture shrunk to THUMB px as a base64 PNG, for the self-contained HTML (`name`: an icon in
    UI/Icons, or an absolute path)."""
    from PIL import Image
    im = Image.open(name if os.path.isabs(name) else os.path.join(ICONS, name)).convert("RGBA")
    im.thumbnail((THUMB, THUMB), Image.LANCZOS)
    buf = io.BytesIO()
    im.save(buf, "PNG", optimize=True)
    return "data:image/png;base64," + base64.b64encode(buf.getvalue()).decode("ascii")


def collect():
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
    data = {
        "active": len(active),
        "hidden_count": len(hidden & {r["id"] for r in rows}),
        "missing_notes": [r["id"] for r in active if r["id"] not in notes],
        "missing_icons": [f'{r["id"]} ур.{lv}' for r in active for lv in range(1, levels.get(r["id"], LEVELS) + 1) if icon(r["id"], lv) is None],
        "groups": [],
        "hidden": [(r["display_name"], r["id"], notes.get(r["id"], "скрыто")) for r in sorted((r for r in rows if r["id"] in hidden), key=lambda r: r["id"])],
    }
    data["army"], data["missing_unit_pics"] = collect_units({r["id"]: r["display_name"] for r in rows})
    for cat, ru in CATEGORIES:
        group = [r for r in active if r["category"] == cat]
        if not group:
            continue
        items = []
        for r in group:
            bid = r["id"]
            top = levels.get(bid, LEVELS)
            icons = [("нет" if lv > top else icon(bid, lv)) for lv in range(1, LEVELS + 1)]
            table_rows = []
            for column, label in STATS:
                vals = per_level(r, column)
                if all(v in (None, 0) for v in vals):
                    continue
                table_rows.append((label, [fmt(v) if v is not None else "—" for v in vals]))
            low = bid.lower()
            for key, label in ((f"food_day_{low}", "Еда в день"), (f"food_boost_{low}", "Бонус еды соседям")):
                vals = [eco.get(f"{key}_{lv}") for lv in range(1, LEVELS + 1)]
                if any(v is not None for v in vals):
                    table_rows.append((label, [fmt(v) if v is not None else "—" for v in vals]))
            sb = sand.get(bid)
            if sb and any(per_level(sb, "resident_coins")):   # decor's money (2026-11-12): the sandbox's decor tab
                table_rows.append(("Радиус бонуса, клеток", [fmt(v) if v is not None else "—" for v in per_level(sb, "service_radius")]))
                table_rows.append(("Монет в день жителю", [fmt(v) if v is not None else "—" for v in per_level(sb, "resident_coins")]))
            if sb:
                table_rows.append(("Цена (песочница)", [price(sb, lv) if lv <= top else "нет" for lv in range(1, LEVELS + 1)]))
                secs = per_level(sb, "build_sec")
                if any(secs):
                    table_rows.append(("Стройка, с (песочница)", [("нет" if lv > top else fmt(v) if v is not None else "—")
                                                                  for lv, v in zip(range(1, LEVELS + 1), secs)]))
            else:
                table_rows.append(("Цена (базовая)", [price(r, 1)] + [""] * (LEVELS - 1)))
            items.append({"name": r["display_name"], "id": bid, "note": notes.get(bid), "icons": icons, "rows": table_rows})
        data["groups"].append((ru, items))
    return data


def unit_picture(model, uid, lv):
    """The level's picture: a soldier/machine card (L1 = <Model>.png, then <Model>-lvlN.png; a missing
    level shows the one below, as the game dresses it), or a ship's HUD portrait (one look)."""
    if model is None:
        path = os.path.join(ICONS, "Portrait" + "".join(w.capitalize() for w in uid.split("_")) + ".png")
        return path if os.path.exists(path) else None
    for k in range(lv, 0, -1):
        path = os.path.join(UNIT_PICS, f"{model}.png" if k == 1 else f"{model}-lvl{k}.png")
        if os.path.exists(path):
            return path
    return None


def unit_levels(row, column):
    values, last = [], None
    for lv in range(1, UNIT_LEVELS + 1):
        v = num(row.get(column if lv == 1 else f"{column}_{lv}", ""))
        values.append(last if v is None else v)
        last = values[-1]
    return values


def collect_units(names):
    table = read_csv("units.csv")
    header = table[0]
    rows = {r[0]: dict(zip(header, r)) for r in table[1:] if r and r[0]}
    notes = {}
    with io.open(UNIT_NOTES, encoding="utf-8-sig", newline="") as f:
        for r in csv.reader(f, delimiter="\t"):
            if len(r) >= 2 and r[0] != "id":
                notes[r[0].strip()] = r[1].strip()
    groups, missing = [], []
    for title, units in ARMY:
        items = []
        for uid, model in units:
            r = rows.get(uid)
            if r is None:
                continue
            ship = model is None
            top = 1 if ship else UNIT_LEVELS
            pad = ["нет"] * (UNIT_LEVELS - top)
            pics = [unit_picture(model, uid, lv) if lv <= top else "нет" for lv in range(1, UNIT_LEVELS + 1)]
            missing += [f"{uid} ур.{lv}" for lv, pic in enumerate(pics, 1) if pic is None]
            table_rows = []
            for column, label in UNIT_STATS:
                vals = unit_levels(r, column)[:top]
                if column == "attack_interval_sec" and not any(unit_levels(r, "attack_damage")[:top]):
                    continue                                   # no attack (a monk, a transport): no rhythm either
                if all(v in (None, 0) for v in vals):
                    continue
                table_rows.append((label, [fmt(v) if v is not None else "—" for v in vals] + pad))
            extras = "".join(f", {label} {fmt(num(r[res]))}" for res, label in (
                ("recruit_wood", "дерево"), ("recruit_ironbar", "жел. слитки"), ("recruit_copperbar", "медн. слитки"))
                if num(r.get(res, "")))
            hire = [(f"монеты {fmt(c)}" if c else "—") + extras for c in unit_levels(r, "recruit_coins")[:top]]
            table_rows.append(("Найм", hire + pad))
            if not ship:
                research = ["—"]
                for lv in range(2, UNIT_LEVELS + 1):
                    coins, secs = num(r.get(f"research_coins_{lv}", "")), num(r.get(f"research_sec_{lv}", ""))
                    research.append((f"монеты {fmt(coins)}" if coins else "—") + (f", {fmt(secs)} с" if secs else ""))
                table_rows.append(("Исследование", research))
                at = (r.get("hired_at") or "Barracks").strip()
                where = f"{names.get(at, at)} ур. {fmt(num(r.get('barracks_level')) or 1)}"
            else:
                where = f"{names.get('Shipyard', 'Верфь')} ур. {fmt(num(r.get('shipyard_level')) or 1)}"
            items.append({"name": r["display_name"], "id": uid, "note": notes.get(uid), "where": where,
                          "icons": pics, "rows": table_rows})
        groups.append((title, items))
    return groups, missing


HEAD_LINES = [
    "Файл СОБИРАЕТСЯ САМ (Tools/docs/reference.py, после каждого setup в run.ps1): не правь его руками.",
    "Состав и числа — лист «Здания» (buildings.csv), еда — economy.csv, скрытые — BuildingIds.Hidden, иконки — UI/Icons,",
    "описания — Design/buildings/descriptions.tsv (новое здание: добавь туда строку).",
    "Армия: числа — лист «units» (units.csv), описания — Design/reference/units.tsv, картинки уровней —",
    "Design/reference/units (Tools/blender/render_unit_cards.py; у кораблей — портреты из UI/Icons).",
    "Цены и время стройки — песочницы (sandbox.csv; ур.1 = постройка, дальше — улучшение); остальные числа — базовые.",
]
UNIT_NO_NOTE = "⚠ описание не написано — добавь строку в Design/reference/units.tsv."
NO_NOTE = "⚠ описание не написано — добавь строку в Design/buildings/descriptions.tsv"


def render_md(d):
    out = ["# Справочник: здания и армия", ""] + ["> " + line for line in HEAD_LINES] + [
        "", "> Картинки удобнее смотреть в Design/Reference.html (картинки внутри файла).", "",
        f"Активных зданий: **{d['active']}** (скрыто: {d['hidden_count']}).", ""]
    if d["missing_notes"]:
        out += [f"⚠ Без описания: {', '.join(d['missing_notes'])}.", ""]
    if d["missing_icons"]:
        out += [f"⚠ Нет иконки: {', '.join(d['missing_icons'])}.", ""]
    for ru, items in d["groups"]:
        out += [f"## {ru}", ""]
        for b in items:
            out += [f"### {b['name']} `{b['id']}`", "", b["note"] or NO_NOTE, ""]
            out.append("| | " + " | ".join(f"ур. {lv}" for lv in range(1, LEVELS + 1)) + " |")
            out.append("|---|" + "---|" * LEVELS)
            cells = []
            for lv, c in enumerate(b["icons"], 1):
                if c == "нет":
                    cells.append("нет")
                elif c is None:
                    cells.append("нет иконки")
                else:
                    cells.append(f"![{b['id']} {lv}](../Assets/_Project/UI/Icons/{c})")
            out.append("| | " + " | ".join(cells) + " |")
            for label, vals in b["rows"]:
                out.append(f"| {label} | " + " | ".join(vals) + " |")
            out.append("")
    if d["hidden"]:
        out += ["## Скрыты (данные и модели есть, в игре не строятся)", ""]
        out += [f"- {n} `{i}` — {note}" for n, i, note in d["hidden"]] + [""]
    out += ["# Армия", ""]
    if d["missing_unit_pics"]:
        out += [f"⚠ Нет картинки: {', '.join(d['missing_unit_pics'])}.", ""]
    for title, items in d["army"]:
        out += [f"## {title}", ""]
        for u in items:
            out += [f"### {u['name']} `{u['id']}`", "", (u["note"] or UNIT_NO_NOTE) + f" Где: {u['where']}.", ""]
            out.append("| | " + " | ".join(f"ур. {lv}" for lv in range(1, UNIT_LEVELS + 1)) + " |")
            out.append("|---|" + "---|" * UNIT_LEVELS)
            cells = []
            for lv, c in enumerate(u["icons"], 1):
                if c == "нет" or c is None:
                    cells.append(c or "нет картинки")
                else:
                    cells.append(f"![{u['id']} {lv}]({os.path.relpath(c, os.path.dirname(OUT)).replace(os.sep, '/')})")
            out.append("| | " + " | ".join(cells) + " |")
            for label, vals in u["rows"]:
                out.append(f"| {label} | " + " | ".join(vals) + " |")
            out.append("")
    return "\n".join(out)


CSS = """body{font-family:Segoe UI,Arial,sans-serif;background:#f6efe0;color:#3a2a1a;margin:24px;max-width:1100px}
h1{margin:0 0 8px}h2{margin:32px 0 8px;border-bottom:2px solid #c9a96b;padding-bottom:4px}
.head{background:#efe2c4;border-left:4px solid #c9a96b;padding:8px 12px;font-size:13px}
.warn{color:#a33;font-size:14px}.toc a{margin-right:12px}
.b{background:#fffaf0;border:1px solid #e0cfa8;border-radius:10px;padding:12px 14px;margin:12px 0}
.b h3{margin:0 0 4px}.id{font:12px Consolas,monospace;color:#8a7350}.note{margin:4px 0 10px}
table{border-collapse:collapse;font-size:13px}td,th{padding:4px 10px;text-align:center;border-bottom:1px solid #eadbb8}
td:first-child,th:first-child{text-align:left;white-space:nowrap;color:#6b5434}
img{width:THUMBpx;height:THUMBpx;display:block;margin:auto}.none{color:#b9a27a}
img.u{width:110px;height:110px}.where{color:#8a7350;font-size:13px}h1.part{margin-top:40px}"""


def render_html(d):
    esc = html.escape
    p = ['<!DOCTYPE html><html lang="ru"><head><meta charset="utf-8"><title>Справочник: здания и армия</title>',
         "<style>" + CSS.replace("THUMB", str(THUMB)) + "</style></head><body>",
         "<h1>Справочник: здания и армия</h1>",
         '<div class="head">' + "<br>".join(esc(line) for line in HEAD_LINES) + "</div>",
         f"<p>Активных зданий: <b>{d['active']}</b> (скрыто: {d['hidden_count']}).</p>"]
    if d["missing_notes"]:
        p.append('<p class="warn">⚠ Без описания: ' + esc(", ".join(d["missing_notes"])) + ".</p>")
    if d["missing_icons"]:
        p.append('<p class="warn">⚠ Нет иконки: ' + esc(", ".join(d["missing_icons"])) + ".</p>")
    p.append('<p class="toc"><b>Здания:</b> ' + "".join(f'<a href="#g{i}">{esc(ru)}</a>' for i, (ru, _) in enumerate(d["groups"]))
             + '<br><b>Армия:</b> ' + "".join(f'<a href="#a{i}">{esc(t)}</a>' for i, (t, _) in enumerate(d["army"])) + "</p>")
    p.append('<h1 class="part">Здания</h1>')
    for i, (ru, items) in enumerate(d["groups"]):
        p.append(f'<h2 id="g{i}">{esc(ru)}</h2>')
        for b in items:
            p.append(f'<div class="b"><h3>{esc(b["name"])} <span class="id">{b["id"]}</span></h3>')
            p.append('<div class="note">' + esc(b["note"] or NO_NOTE) + "</div><table>")
            p.append("<tr><th></th>" + "".join(f"<th>ур. {lv}</th>" for lv in range(1, LEVELS + 1)) + "</tr>")
            cells = []
            for c in b["icons"]:
                if c == "нет":
                    cells.append('<td class="none">нет</td>')
                elif c is None:
                    cells.append('<td class="none">нет иконки</td>')
                else:
                    cells.append(f'<td><img src="{thumb(c)}" alt=""></td>')
            p.append("<tr><td></td>" + "".join(cells) + "</tr>")
            for label, vals in b["rows"]:
                p.append("<tr><td>" + esc(label) + "</td>" + "".join("<td>" + esc(v) + "</td>" for v in vals) + "</tr>")
            p.append("</table></div>")
    if d["hidden"]:
        p.append("<h2>Скрыты (данные и модели есть, в игре не строятся)</h2><ul>")
        for n, i, note in d["hidden"]:
            p.append("<li>" + esc(n) + f' <span class="id">{i}</span> — ' + esc(note) + "</li>")
        p.append("</ul>")
    p.append('<h1 class="part">Армия</h1>')
    if d["missing_unit_pics"]:
        p.append('<p class="warn">⚠ Нет картинки: ' + esc(", ".join(d["missing_unit_pics"])) + ".</p>")
    for i, (title, items) in enumerate(d["army"]):
        p.append(f'<h2 id="a{i}">{esc(title)}</h2>')
        for u in items:
            p.append(f'<div class="b"><h3>{esc(u["name"])} <span class="id">{u["id"]}</span></h3>')
            p.append('<div class="note">' + esc(u["note"] or UNIT_NO_NOTE) + ' <span class="where">Где: '
                     + esc(u["where"]) + "</span></div><table>")
            p.append("<tr><th></th>" + "".join(f"<th>ур. {lv}</th>" for lv in range(1, UNIT_LEVELS + 1)) + "</tr>")
            cells = []
            for c in u["icons"]:
                if c == "нет":
                    cells.append('<td class="none">нет</td>')
                elif c is None:
                    cells.append('<td class="none">нет картинки</td>')
                else:
                    cells.append(f'<td><img class="u" src="{thumb(c)}" alt=""></td>')
            p.append("<tr><td></td>" + "".join(cells) + "</tr>")
            for label, vals in u["rows"]:
                p.append("<tr><td>" + esc(label) + "</td>" + "".join("<td>" + esc(v) + "</td>" for v in vals) + "</tr>")
            p.append("</table></div>")
    p.append("</body></html>")
    return "\n".join(p)


def write(path, text):
    old = io.open(path, encoding="utf-8").read() if os.path.exists(path) else None
    if old != text:
        with io.open(path, "w", encoding="utf-8", newline="\n") as f:
            f.write(text)
    return old != text


def main():
    d = collect()
    changed = write(OUT, render_md(d))
    changed = write(OUT_HTML, render_html(d)) or changed
    print(f"reference: {d['active']} buildings, {sum(len(i) for _, i in d['army'])} army units, {'updated' if changed else 'unchanged'}"
          + (f"; NO NOTE: {', '.join(d['missing_notes'])}" if d["missing_notes"] else "")
          + (f"; NO ICON: {len(d['missing_icons'])}" if d["missing_icons"] else ""))
    return 0


if __name__ == "__main__":
    sys.exit(main())
