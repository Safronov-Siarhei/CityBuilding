"""Compare the committed balance CSVs against the tabs freshly published from Google Sheets.

Compares by column NAME and row id, not byte-for-byte: the sheet re-exports derived columns and
quotes decimals in its own way, and none of that is a difference the game would ever notice.
"""
import csv
import io
import sys


def read(path, key=None):
    with io.open(path, encoding="utf-8-sig", newline="") as f:
        rows = list(csv.reader(f))
    header = [c.strip() for c in rows[0]]
    key_index = header.index(key) if key else 0
    out = {}
    order = []
    for row in rows[1:]:
        if not row or not row[0].strip():
            continue
        # The prose under the table: an id never contains a space (BalanceImporter.IsNoteRow).
        if " " in row[0].strip():
            continue
        rid = row[key_index].strip() if key_index < len(row) else ""
        if not rid:
            continue
        out[rid] = {header[i]: (row[i].strip() if i < len(row) else "") for i in range(len(header))}
        order.append(rid)
    return header, out, order


def norm(value):
    """A cell as the importer would read it: decimal commas are dots, and blank == 0 for numbers."""
    value = (value or "").strip().replace(",", ".")
    try:
        return "%g" % float(value)
    except ValueError:
        return value


def compare(name, repo_path, sheet_path, key=None):
    repo_header, repo_rows, repo_order = read(repo_path, key)
    sheet_header, sheet_rows, sheet_order = read(sheet_path, key)

    problems = []

    missing_columns = [c for c in repo_header if c and c not in sheet_header]
    extra_columns = [c for c in sheet_header if c and c not in repo_header]
    if missing_columns:
        problems.append("  columns in repo but NOT in sheet: %s" % ", ".join(missing_columns))
    if extra_columns:
        problems.append("  columns in sheet but not in repo: %s" % ", ".join(extra_columns))

    missing_rows = [r for r in repo_order if r not in sheet_rows]
    extra_rows = [r for r in sheet_order if r not in repo_rows]
    if missing_rows:
        problems.append("  rows in repo but NOT in sheet: %s" % ", ".join(missing_rows))
    if extra_rows:
        problems.append("  rows in sheet but not in repo: %s" % ", ".join(extra_rows))

    shared_columns = [c for c in repo_header if c and c in sheet_header]
    for rid in repo_order:
        if rid not in sheet_rows:
            continue
        for column in shared_columns:
            a = norm(repo_rows[rid].get(column, ""))
            b = norm(sheet_rows[rid].get(column, ""))
            if a != b:
                problems.append("  %s.%s: repo=%r sheet=%r" % (rid, column, a, b))

    print("%s: %d rows, %d columns compared" % (name, len(repo_order), len(shared_columns)))
    if problems:
        print("\n".join(problems))
    else:
        print("  identical")
    print()
    return not problems


ok = True
ok &= compare("buildings", "Assets/_Project/Balance/buildings.csv", "buildings.sheet.csv")
ok &= compare("economy", "Assets/_Project/Balance/economy.csv", "economy.sheet.csv")
ok &= compare("units", "Assets/_Project/Balance/units.csv", "units.sheet.csv")
# Recipes are keyed by recipe_id, not by the first column: three rows share the building "Smelter".
ok &= compare("recipes", "Assets/_Project/Balance/recipes.csv", "recipes.sheet.csv", key="recipe_id")
sys.exit(0 if ok else 1)
