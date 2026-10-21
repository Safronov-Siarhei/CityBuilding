---
name: feedback-balance-row-handoff
description: "FALLBACK only: when the browser can't reach the Google Sheet, hand balance changes over as complete tab-separated rows in the sheet's own column order, with exact operations and addresses; then verify by pulling."
metadata:
  node_type: memory
  type: feedback
  originSessionId: 00ec56af-41af-4e20-aa76-c5f36bc84d70
  modified: 2026-10-21T16:40:49.314Z
---

**Normally edit the sheet yourself** with the `balance-sheet` skill (user, 2026-08-31: «добавляй новые
строки сам в таблицу»). This is the fallback when that is impossible.

**Rules the user asked for (2026-08-17/19/21, the second one angrily):**
- Give COMPLETE rows to paste, never a list of cells to edit. One tab-separated line per row in a fenced
  block, or a FILE for many rows ([[feedback-hand-off-as-file]]); paste at column A.
- Spell out OPERATIONS with REAL addresses from the sheet as it stands (download and count): which tab,
  "right-click row 4 → insert 3 rows above", "click `AH1` and paste". Order steps so inserts don't move
  later addresses. Name formula columns and say "drag", not paste. Say when NOT to do the obvious thing.
- Build the block in the SHEET's column order, not the CSV's (`id_check` sits at `P` in units and `AT`
  in buildings — everything after it shifts by one). Take the paste column from the last NAMED header,
  never from the export's width.
- NO EMPTY CELLS in a block (tabs collapse when copied): expand inherited `_N` cells to their values,
  commit that to the CSV and prove `BalanceConfig.asset` is byte-identical.
- Never retype a generated block (a Cyrillic «м» slipped in once) — print it from the file.
- Hand-edited cells in EXISTING rows are invisible to a block paste — list them separately by address.

**Then verify by pulling, not by asking:** download the published tabs with a cache-buster and diff
against the repo CSVs numerically on the columns the code reads (`verify_sheet.py` in the skill).
