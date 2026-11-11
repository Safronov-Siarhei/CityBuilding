---
name: feedback-balance-row-handoff
description: "How to hand CityBuilding balance changes to the user: exact mechanical operations with real cell addresses, in the order they must be done, plus complete tab-separated rows to paste."
metadata: 
  node_type: memory
  type: feedback
  originSessionId: e11f94ca-15a8-4229-b2a6-7681d9d8dbf8
  modified: 2026-08-18T05:30:20.000Z
---

**SUPERSEDED IN PART, 2026-08-31: edit the sheet yourself.** The user connected the Claude in
Chrome extension and said to add new rows directly rather than handing them over
("добавляй новые строки сам в таблицу"). Do that first; the handoff format below is the fallback
for when the browser is unavailable, and everything it says about verifying afterwards still
applies in full. How to drive the sheet -- including that `computer:type` silently drops Cyrillic
and the clipboard is the way in -- is in [[project_current_state]].

When a balance change touches the Google Sheet, **give the user COMPLETE rows to paste, never a list of cells to edit**. Asked for explicitly on 2026-08-17 after several rounds of cell-hunting: *"я трачу много времени на поиск ячеек для изменений, сделай правило по которому ты будешь мне кидать уже готовую строку и я буду её вставлять в нужное место"*.

**SPELL OUT THE OPERATIONS, NOT THE INTENT (asked for angrily on 2026-08-19).** The words used were: *"блять, формулируй чётче что мне нужно делать если надо (например 'вставь в таблицу units столько-то строк после такой-то строки и вставь столько-то столбцов после такого-то столбцов')"*. What earned it: a handoff that said "add three columns at the far right" and "insert after `#log_research_lost`" instead of naming cells. Half of it silently went undone, and neither of us noticed until the diff.

Every manual step gets a numbered instruction with a REAL address, taken from the downloaded tab rather than guessed:

1. **Which tab**, then **the exact operation**: "right-click row 4 -> insert 3 rows above", "click `AH1` and paste", "replace the contents of `C210`".
2. **Row numbers from the SHEET as it stands now** -- download it and count. Never "after the row that says X": they then have to find it, and a tab of 218 rows is exactly where that goes wrong.
3. **Order the steps so earlier ones do not move later addresses.** An insert shifts every row below it, so a single-cell fix must come BEFORE the insert that would renumber it -- or be given at its post-insert number. Say which.
4. **Say when NOT to do the obvious thing.** "No need to insert columns, `AH:AK` are already empty" saves a step and a misunderstanding.
5. **Name the formula columns and say drag, not paste** -- "select `L2:P2` and drag down to row 6" -- or a paste of blanks kills them.

This is not sheet-specific. Any manual work handed to the user gets the same treatment: the operation, the exact target, and the order.

**BUILD THE PASTE BLOCK IN THE SHEET'S OWN COLUMN ORDER, NOT THE CSV'S.** The `units` and `buildings` tabs carry an `id_check` column that the CSV does not -- at `P` in units, at `AT` in buildings -- so every letter after it is off by one and a block generated straight from the CSV lands one column short from that point on. Generate the block by walking the DOWNLOADED header and pulling each named column out of the repo row, leaving a blank for `id_check` and for the formula columns. Then the block is positional-paste-safe by construction.

**NEVER RETYPE A GENERATED BLOCK INTO THE MESSAGE.** On 2026-08-19 the generated line held `Service radius: {0} m` and the message held `Service radius: {0} м` -- a Cyrillic м, typed while copying the block over by hand. The user pasted exactly what they were given, and it took a cell-by-cell diff to find. Print the block from the file and copy the printed output; if a block is worth generating, it is worth not touching afterwards.

**A HANDOFF BLOCK MUST HAVE NO EMPTY CELLS (2026-08-21).** The user could not paste a block that
had them: *"я не могу скопировать правильно твой файл аа2 так как там есть пустые ячейки и они не
копируются"*. Runs of tabs collapse when the block is copied out of a file, and everything after the
gap lands a column to the left. The buildings tab's rule that an empty `<column>_2`/`_3` repeats the
level below is what produces those gaps, so **expand every inherited cell to the value it already
resolves to** before generating the block (Sawmill batches 2/2/2, a Hovel's workers 0/0/0), commit
that expansion to the CSV so the repo and the sheet still match, and PROVE it changed nothing --
rebuild and check `BalanceConfig.asset` comes out byte-identical.

**The format:**

1. Say **how many rows** change and **which tab**, before anything else.
2. Give each row as **one tab-separated line inside a fenced code block** — never a markdown table, never a bullet list of "column K -> 2". A markdown table makes them hunt for cells again, and consecutive empty cells collapse when copied out of one (that is exactly how the `recipes` tab landed shifted by a column on 2026-08-17).
3. Tell them to click the **first cell of that row (column A)** and paste: Sheets splits a tab-separated line across the columns and overwrites the whole row.
4. One row per line, so several rows can be pasted in one go when they are adjacent.
5. When most of a tab changes, hand over the **entire tab** the same way (header included, paste at A1) rather than a dozen rows.

**Before telling them to paste a whole row, verify the sheet's column ORDER matches the repo CSV's** — download the published tab and compare headers position by position, not just by name. The comparison script in the scratchpad matches columns by name, so a reordered sheet would pass it and still be destroyed by a positional paste.

**Never take a paste column from the export's WIDTH** (2026-08-18). A published tab exports padded out to the widest cell anywhere on the sheet, so the buildings tab came back 63 columns wide when its named columns ended at `BA` — the extra ten were stray cells left in two note rows by an earlier paste. I told the user to paste at `BL`; they asked why, because they could see the last content at `BB`, and they were right. Find the last NAMED header in row 1 and use the column after it, and check what is sitting in the rows below before trusting a letter.

**Why:** the sheet is the authoring surface and the CSV is generated from it, so every change means the user retypes what I already have in a file. A row they paste is one action; a table of cells is a dozen lookups, and each lookup is a chance to put a number in the wrong column — which happened repeatedly.

**How to apply:** generate the rows straight out of the committed CSV (`Assets/_Project/Balance/*.csv`) after the batchmode rebuild has proved the numbers, so what they paste is exactly what the game was tested with. For a whole tab, write a tab-separated copy and send it as a FILE (SendUserFile) — 170 rows do not belong in chat. Then re-download the published tabs **with a cache-buster** and diff them against the repo to confirm the paste landed — see [[project_balance_pipeline]]; without it Google served a pre-paste copy twice and made a good paste look truncated.

**Verify the paste by pulling, not by asking (learned 2026-08-18).** After the user says they pasted, download the published tabs and diff them against the committed CSVs cell by cell on the columns the code actually reads — a text diff drowns in formula churn, and `''` vs `0` vs `1` vs `1,0` all read identically, so compare numerically. That check caught two hand-edited cells in one row that had never made it into any of the four blocks handed over: the whole system would have broken on the user's next Pull with nothing to indicate why.

**The corollary: a handoff is not only the blocks.** Any cell changed by hand in an EXISTING row (a cost, a worker count) is invisible to a block-of-new-columns paste. List those separately, by exact cell reference, in the same message as the blocks.
