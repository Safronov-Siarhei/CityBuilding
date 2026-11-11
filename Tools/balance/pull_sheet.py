"""Pull the published balance sheet into Assets/_Project/Balance/*.csv without Unity.

The same merge as BalanceImporter.PullFromGoogleSheets (sheet_tabs.csv says which tabs make up
which CSV; a tab with the first tab's header loses it, a table with its own header keeps it after
a blank line). For Claude when the Editor cannot run Pull; the Editor's next SetupProject reads
the CSVs as usual.

    python -I Tools/balance/pull_sheet.py               -> every CSV
    python -I Tools/balance/pull_sheet.py economy bundles
"""
import csv
import io
import os
import sys
import time
import urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
BALANCE = os.path.join(ROOT, 'Assets', '_Project', 'Balance')
PUBLISHED = ('https://docs.google.com/spreadsheets/d/e/2PACX-1vSZ1UnjqdmTaPue3aALGzUU9r0UhXNGt-B8qes16Z8FpnxVy3vLdukeqWnAZsbRCG3ayHa8i21wuiCh/pub'
             '?gid={gid}&single=true&output=csv&cb={cb}')


def first_cell(text):
    end = min([i for i in (text.find(','), text.find('\r'), text.find('\n')) if i >= 0] or [len(text)])
    return text[:end].strip().lstrip('﻿')


def merge(tabs):
    if len(tabs) == 1:
        return tabs[0]
    merged = tabs[0].rstrip('\r\n')
    header = first_cell(tabs[0])
    for tab in tabs[1:]:
        tab = tab.rstrip('\r\n')
        if first_cell(tab) == header:
            cut = tab.find('\n')
            if cut < 0:
                continue
            tab = tab[cut + 1:]
        else:
            merged += '\r\n'
        merged += '\r\n' + tab
    return merged


def main():
    targets = {}
    with io.open(os.path.join(BALANCE, 'sheet_tabs.csv'), encoding='utf-8') as f:
        for row in list(csv.reader(f))[1:]:
            if row:
                targets.setdefault(row[0], []).append(row[1])
    wanted = sys.argv[1:] or list(targets)
    for name in wanted:
        tabs = [urllib.request.urlopen(PUBLISHED.format(gid=g, cb=int(time.time() * 1000)), timeout=60).read().decode('utf-8')
                for g in targets[name]]
        with io.open(os.path.join(BALANCE, name + '.csv'), 'w', encoding='utf-8', newline='') as f:
            f.write(merge(tabs))
        print(name, len(tabs), 'tabs')


main()
