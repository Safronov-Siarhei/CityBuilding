# Emits the exact tab-separated blocks to paste into the Google Sheet, straight from the
# committed CSVs, so the sheet and the game cannot drift apart.
import csv, io, os

BAL = r"D:\Fork\CityBuilding\Assets\_Project\Balance"
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "sheet_blocks.txt")

BUILD_NEW = ["starts_unlocked", "research_coins", "research_sec",
             "research_coins_2", "research_sec_2", "research_coins_3", "research_sec_3"]

UNIT_NEW = ["starts_unlocked", "max_health_2", "max_health_3", "attack_damage_2", "attack_damage_3",
            "attack_interval_sec_2", "attack_interval_sec_3", "walk_speed_2", "walk_speed_3",
            "recruit_coins_2", "recruit_coins_3", "upkeep_coins_per_day_2", "upkeep_coins_per_day_3",
            "research_coins_2", "research_sec_2", "research_coins_3", "research_sec_3"]

def rows(name):
    with io.open("{0}\\{1}".format(BAL, name), encoding="utf-8-sig", newline="") as f:
        return list(csv.reader(f))

def is_data(row):
    first = (row[0] if row else "").strip()
    if not first:
        return False
    if " " in first:
        return False
    return any((c or "").strip() for c in row[1:])

def column_letter(index):  # 1-based
    letters = ""
    while index:
        index, rem = divmod(index - 1, 26)
        letters = chr(65 + rem) + letters
    return letters

def block(name, new_columns):
    data = rows(name)
    header = [h.strip() for h in data[0]]
    first_new = header.index(new_columns[0]) + 1
    picked = [header.index(c) for c in new_columns]

    # id_check rides along as the first column so the paste can be eyeballed against the sheet's own
    # id column; the importer ignores any column it does not know, and it can be deleted afterwards.
    lines = ["\t".join(["id_check"] + new_columns)]
    for row in data[1:]:
        if not is_data(row):
            continue
        row = row + [""] * (len(header) - len(row))
        lines.append("\t".join([row[0]] + [row[i] for i in picked]))
    return column_letter(first_new), lines

out = []

col, lines = block("buildings.csv", BUILD_NEW)
out.append("### buildings: 7 new columns, paste into cell {0}1 ({1} rows incl. header)".format(col, len(lines)))
out += lines
out.append("")

col, lines = block("units.csv", UNIT_NEW)
out.append("### units: 17 new columns, paste into cell {0}1 ({1} rows incl. header)".format(col, len(lines)))
out += lines
out.append("")

out.append("### economy: 4 new rows, paste under the last key row")
for row in rows("economy.csv")[1:]:
    if is_data(row) and row[0].strip().startswith("research_"):
        out.append("\t".join(row))
out.append("")

out.append("### localization: new rows, paste under the last key row")
for row in rows("localization.csv")[1:]:
    key = (row[0] or "").strip()
    if key.startswith("#research_") or key.startswith("#log_research_") \
       or key in ("#building_upgrade_locked", "#workforce_research"):
        out.append("\t".join(row))

with io.open(OUT, "w", encoding="utf-8", newline="") as f:
    f.write("\n".join(out) + "\n")

print("wrote {0} lines".format(len(out)))
