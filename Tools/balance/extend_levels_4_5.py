"""Appends the level-4 and level-5 columns to buildings.csv by continuing each row's 2 -> 3 progression.

    python Tools/balance/extend_levels_4_5.py

A one-off used on 2026-10-15 when the game went from three building levels to five. The numbers
are a starting point for the sheet, not a balance pass: every rule continues what the row already
does between levels 2 and 3, so a stat the sheet leaves flat stays flat and a blank cell stays
blank (and is inherited from the level below by BalanceImporter).

- max_health, defense, happiness, storage_capacity grow x1.44 then x1.39 -- the ladder
  1 / 1.6 / 2.5 / 3.6 / 5.0 times level 1 that health already follows up to level 3;
- housing_capacity, max_workers, batches_per_tick keep adding the level-2 -> 3 step, so the grain
  chain's ratios (farm : windmill : baker) hold at every level;
- harvest_radius, service_radius grow x1.2 per level -- slower than their own 2 -> 3 step, which
  would have a quarry reaching across half the map by level 5;
- up4_* / up5_* are up3_* x1.75 and x1.75^2 -- the same factor the sheet uses between up2 and up3;
- research_coins_4/_5 are research_coins_3 x1.7 and x1.7^2, rounded to 5; research_sec keeps
  adding its 2 -> 3 step (60 s): 180 s, 240 s. A row without level research (the Laboratory)
  stays without it.

Refuses to run twice: it stops if the header already has up4_wood.
"""
import csv
import io
import os

PATH = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Project", "Balance", "buildings.csv")
RESOURCES = ["wood", "stone", "iron", "coal", "gold"]
GROW = ["max_health", "defense", "happiness", "storage_capacity"]
STEP = ["housing_capacity", "max_workers", "batches_per_tick"]
REACH = ["harvest_radius", "service_radius"]
GROW_FACTORS = {4: 1.44, 5: 5.0 / 3.6}


def num(text):
    return float(text.replace(",", ".")) if text.strip() else None


def fmt(value):
    return str(int(round(value)))


def round_to(value, step):
    return int(round(value / step) * step)


def extend_stat(column, l2, l3):
    """Values for levels 4 and 5, or blanks when the row leaves this stat alone above level 1."""
    if l2 is None and l3 is None:
        return ["", ""]
    l2 = l2 or 0
    l3 = l3 if l3 is not None else l2
    if column in STEP:
        step = l3 - l2
        return [fmt(l3 + step), fmt(l3 + 2 * step)]
    if column in REACH:
        l4 = max(l3 + 1, round(l3 * 1.2))
        l5 = max(l4 + 1, round(l4 * 1.2))
        return [fmt(l4), fmt(l5)]
    # GROW
    if l3 == 0:
        return ["0", "0"]
    snap = 50 if column == "storage_capacity" else 1
    l4 = max(l3 + snap, round_to(l3 * GROW_FACTORS[4], snap))
    l5 = max(l4 + snap, round_to(l4 * GROW_FACTORS[5], snap))
    return [fmt(l4), fmt(l5)]


def main():
    with open(PATH, encoding="utf-8", newline="") as f:
        rows = list(csv.reader(f))
    header = rows[0]
    if "up4_wood" in header:
        raise SystemExit("buildings.csv already has the level-4 columns")

    added = []
    for column in GROW + STEP + REACH:
        added += [f"{column}_4", f"{column}_5"]
    for level in (4, 5):
        added += [f"up{level}_{r}" for r in RESOURCES]
    added += ["research_coins_4", "research_sec_4", "research_coins_5", "research_sec_5"]

    def cell(row, name):
        i = header.index(name)
        return row[i] if i < len(row) else ""

    out = [header + added]
    for row in rows[1:]:
        is_building = len(row) == len(header) and row[0].strip() and row[2].strip()
        if not is_building:
            out.append(row)
            continue
        values = []
        for column in GROW + STEP + REACH:
            values += extend_stat(column, num(cell(row, f"{column}_2")), num(cell(row, f"{column}_3")))
        for level in (4, 5):
            for r in RESOURCES:
                up3 = num(cell(row, f"up3_{r}"))
                values.append("" if up3 is None else fmt(up3 * 1.75 ** (level - 3)))
        c2, c3 = num(cell(row, "research_coins_2")) or 0, num(cell(row, "research_coins_3")) or 0
        s2, s3 = num(cell(row, "research_sec_2")) or 0, num(cell(row, "research_sec_3")) or 0
        for level in (4, 5):
            if c3 <= 0:
                values += ["0", "0"]
            else:
                values += [str(round_to(c3 * 1.7 ** (level - 3), 5)), fmt(s3 + (s3 - s2) * (level - 3))]
        out.append(row + values)

    buffer = io.StringIO()
    csv.writer(buffer, lineterminator="\n").writerows(out)
    with open(PATH, "w", encoding="utf-8", newline="") as f:
        f.write(buffer.getvalue())
    print("added", len(added), "columns")


main()
