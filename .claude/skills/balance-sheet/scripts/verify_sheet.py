#!/usr/bin/env python3
"""Diff the published CityBuilding balance sheet against the repo CSVs.

Run this after every sheet edit. Asking the user whether the paste landed is not
verification -- a row can go in with one cell silently empty (Cyrillic typed into the
Sheets canvas does that) or shifted a column (a block built from the repo header rather
than the sheet's), and both look fine on screen.

    python .claude/skills/balance-sheet/scripts/verify_sheet.py
    python .claude/skills/balance-sheet/scripts/verify_sheet.py recipes localization

Compares numerically where it can: '', '0', '1' and '1,0' all read differently as strings
while meaning the same number, so a plain text diff drowns in false positives and the real
difference hides among them.

Exit code 0 when the only differences are the known permanent ones, 1 otherwise.
"""

import csv
import io
import os
import sys
import time
import urllib.request

# The Windows console is not UTF-8 by default, and every label in this sheet is Russian --
# without this the differences print as mojibake and are unreadable exactly when they matter.
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")

REPO = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..", "..")
BALANCE = os.path.normpath(os.path.join(REPO, "Assets", "_Project", "Balance"))

PUBLISHED = (
    "https://docs.google.com/spreadsheets/d/e/2PACX-1vSZ1UnjqdmTaPue3aALGzUU9r0Uh"
    "XNGt-B8qes16Z8FpnxVy3vLdukeqWnAZsbRCG3ayHa8i21wuiCh/pub"
    "?gid={gid}&single=true&output=csv&cb={cb}"
)

# The column whose value identifies a row, per tab. economy and localization are key/value
# tabs; buildings, units and recipes are keyed on an id (recipes needs two columns, since
# the Smelter owns four rows).
TABS = {
    "units": {"gid": "538856846", "key": ["id"]},
    "economy": {"gid": "257341966", "key": ["key"]},
    "buildings": {"gid": "766628053", "key": ["id"]},
    "recipes": {"gid": "208452028", "key": ["building", "recipe_id"]},
    "localization": {"gid": "304072291", "key": ["key"]},
}

# Differences that are correct and permanent. Reported as a note, not as a failure.
IGNORED_COLUMNS = {
    "id_check",   # a sheet-side formula check; the CSV has no such column
    "comment",    # recipes: the repo CSV carries one, the published tab does not
}
IGNORED_CELLS = {
    # (tab, column): the repo writes 0 where the sheet leaves the cell empty
    ("buildings", "cost_ironbar"),
}


def fetch(gid):
    url = PUBLISHED.format(gid=gid, cb=int(time.time()))
    return urllib.request.urlopen(url, timeout=60).read().decode("utf-8-sig")


def rows(text):
    return list(csv.reader(io.StringIO(text)))


def read_repo(name):
    path = os.path.join(BALANCE, name + ".csv")
    with io.open(path, encoding="utf-8-sig", newline="") as handle:
        return list(csv.reader(handle))


def same(a, b):
    """True when two cells mean the same thing.

    Numbers first: the sheet writes decimals with a comma, the repo may write a dot, and
    an empty cell and a zero are the same quantity to the importer. Falls back to an exact
    string comparison, which is what matters for labels -- including their leading spaces,
    since those are load-bearing in suffix strings like ' x{0}'.
    """
    a = "" if a is None else a
    b = "" if b is None else b
    if a == b:
        return True
    try:
        fa = float((a or "0").replace(",", ".").strip())
        fb = float((b or "0").replace(",", ".").strip())
        return abs(fa - fb) < 1e-9
    except ValueError:
        return False


def is_note_row(row):
    """The footnotes under the data, by the same rule the game uses.

    Mirrors BalanceImporter.IsNoteRow: an id never contains a space and a note always does.
    The comma inside a Russian sentence makes Google split the note across two cells, so
    "everything after the first cell is empty" alone would let it through as a row whose id
    is a paragraph -- which is how these first showed up here as three missing recipes.
    """
    if row and row[0] and " " in row[0].strip():
        return True
    return not any(cell.strip() for cell in row[1:])


def index(table, header, key_columns):
    out = {}
    for row in table:
        if not row or is_note_row(row):
            continue
        record = dict(zip(header, row))
        key = tuple(record.get(c, "") for c in key_columns)
        if not any(key):
            continue
        out[key] = record
    return out


def compare(name, spec):
    sheet = rows(fetch(spec["gid"]))
    repo = read_repo(name)
    if not sheet or not repo:
        print("%-13s COULD NOT READ" % name)
        return 1

    sheet_head, repo_head = sheet[0], repo[0]
    key = spec["key"]
    if any(c not in sheet_head for c in key):
        print("%-13s the sheet has no %s column -- was the tab reordered?" % (name, key))
        return 1

    sheet_rows = index(sheet[1:], sheet_head, key)
    repo_rows = index(repo[1:], repo_head, key)

    shared = [c for c in sheet_head
              if c in repo_head and c not in IGNORED_COLUMNS
              and (name, c) not in IGNORED_CELLS]

    problems = []
    for k in sorted(set(sheet_rows) | set(repo_rows)):
        label = "/".join(x for x in k if x)
        if k not in sheet_rows:
            problems.append("  MISSING IN SHEET: %s  <- the next Pull would drop it" % label)
            continue
        if k not in repo_rows:
            problems.append("  MISSING IN REPO:  %s  <- the next Pull would add it" % label)
            continue
        for column in shared:
            a, b = sheet_rows[k].get(column, ""), repo_rows[k].get(column, "")
            if not same(a, b):
                problems.append("  DIFF %s [%s]: sheet=%r repo=%r" % (label, column, a, b))

    skipped = [c for c in sheet_head if c in IGNORED_COLUMNS or (name, c) in IGNORED_CELLS]
    note = (" (ignored: %s)" % ", ".join(skipped)) if skipped else ""
    print("%-13s %d rows, %d columns compared, %d differences%s"
          % (name, len(set(sheet_rows) | set(repo_rows)), len(shared), len(problems), note))
    for line in problems:
        print(line)
    return len(problems)


def main():
    wanted = sys.argv[1:] or list(TABS)
    unknown = [t for t in wanted if t not in TABS]
    if unknown:
        print("unknown tab(s): %s; known: %s" % (", ".join(unknown), ", ".join(TABS)))
        return 2

    total = 0
    for name in wanted:
        total += compare(name, TABS[name])

    print()
    print("no differences -- the sheet and the repo agree" if total == 0
          else "%d difference(s): fix them before committing, or the next Pull decides for you" % total)
    return 0 if total == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
