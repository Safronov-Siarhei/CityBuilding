---
name: balance-sheet
description: Edit the CityBuilding balance Google Sheet (CityBuilding_Balance) directly through the Google Sheets connector (API; the Claude in Chrome extension is the fallback), then verify the edit landed. Use this whenever a change touches the game's numbers or text — a new recipe, building, unit, economy key or localization string; retuning a cost, a rate or a research price; renaming a label; or reconciling the repo CSVs with the sheet. Reach for it as soon as you are about to edit anything under Assets/_Project/Balance/, and also when the user says the sheet and the repo have drifted, asks what differs between them, or asks you to add/paste/update rows. The user has said explicitly not to hand them paste blocks — add the rows yourself.
---

# Editing the CityBuilding balance sheet

The sheet is the authoring surface for every number and every string in the game;
`Assets/_Project/Balance/*.csv` is generated from it and `BalanceConfig.asset` from those.
So a change is only finished when **both** sides agree — leave the sheet behind and the user's
next Pull silently reverts your work with nothing to indicate why.

The user asked for this to be automatic: *"добавляй новые строки сам в таблицу"*. Handing over a
block for them to paste is the fallback for when the browser is unavailable, not the default.

## Order of work

Change the repo CSV first, rebuild, let the tests prove the numbers, and only then put the proven
values into the sheet. Doing it the other way round means typing numbers into the authoring
surface that nothing has run yet.

1. Edit `Assets/_Project/Balance/<tab>.csv`.
2. Rebuild and test (`Tools/research/rebuild_twice_and_test.ps1` — twice, because a CSV change is
   read by the importer in the Editor assembly).
3. Put the same rows into the sheet through the Sheets connector (API, below; the browser is the fallback).
4. Verify with `scripts/verify_sheet.py` (below). Not by asking.
5. Commit the CSV and the regenerated assets.

## The document

| | |
|---|---|
| Document id | `1eroe1LMQCrpFtP4se7Oae4pGv-aOrHiqYAMdWKxxook` |
| Edit URL | `https://docs.google.com/spreadsheets/d/<id>/edit#gid=<gid>` |
| Browser | the user has two Chrome browsers connected; theirs is named **DepigoGames** |

Tab gids — the same ones in `Assets/_Project/Balance/BalanceSheetSettings.asset`:

| tab | gid |
|---|---|
| units | 538856846 |
| economy | 257341966 |
| buildings | 766628053 |
| recipes | 208452028 |
| localization | 304072291 |
| invasion | 1941808734 |

`invasion` (2026-10-23) is three tables stacked in one tab (`building`, `unit`, `key` header rows): the
Invasion mode's numbers laid over the other tabs for a run. Row order matters only within a table;
`verify_sheet.py` compares it by first cell. A key added for the mode goes HERE, not into economy.

**If the document id ever stops working, find it through Drive → Недавние, not by guessing.**
The `2PACX-…` string in the published CSV links is a publication id, not a document id, and no
amount of rearranging turns one into the other.

## Driving the sheet through the API (the default since 2026-10-22)

The user connected the **Google Sheets** connector (claude.ai connector; its tools are
`mcp__<id>__get_values`, `update_values`, `insert_dimension`, `append_values`, `get_spreadsheet`,
loaded with ToolSearch "select:..."). Use it instead of the browser: no screenshots, no Name Box,
no clipboard. If its tools are missing, check `mcp__ccd_connectors__session_connectors_status`;
fall back to the browser section below only when the connector is unavailable.

- **Read** a range: `get_values(spreadsheetId, "economy!A160:C170")` -- find a row's number by
  reading column A of the tab, never by guessing from the repo CSV (the orders differ).
- **Insert rows**: `insert_dimension(sheetId=<tab gid>, dimension="ROWS", startIndex=<row-1>,
  endIndex=<row-1+n>, inheritFromBefore=true)`; the gid is the tab's `sheetId` (table below).
  An insert renumbers everything below it: do the lower edits first, or re-read.
- **Write**: `update_values(spreadsheetId, "economy!A184:C185", [[...], [...]])`. Values are
  entered AS IF TYPED (USER_ENTERED): a text starting with `+`, `-` or `=` becomes a formula and
  reads back `0` -- write it with a leading apostrophe (`"'+{0}"`), which is not stored. Write
  decimals the way the sheet shows them, as text with a comma (`"0,5"`), like the CSV.
- **Read back** what was written (`get_values`) before moving on -- it is one cheap call, and it
  is what caught the `+{0}` formula.
- Then run `verify_sheet.py` as before. The published CSV it downloads can lag the sheet by a
  minute or more: a difference in a cell just written usually means "not published yet" -- re-read
  the cell through the API, and re-run the script after a wait (in the background) rather than
  re-editing.

## Driving the sheet through the browser (fallback)

Select the browser first — `switch_browser` broadcasts a Connect prompt to every connected Chrome,
or `select_browser` with a known deviceId. Without it every other call fails.

### Cyrillic cannot be typed, and fails silently

`computer:type` drops non-ASCII into the Sheets canvas and **leaves the cell empty with no error
of any kind**. A row typed cell-by-cell went in with a blank `display_name` and nothing said so;
it was only caught on a screenshot. This is the single most dangerous thing about this task,
because the failure looks exactly like success.

The clipboard is the way in, and it handles the whole row at once:

```js
// javascript_tool on the sheet's tab
await navigator.clipboard.writeText('#recipe_coins\tМонеты\tCoins\tподпись кнопки')
```

then click the row's first cell and press `ctrl+v`. Sheets splits a tab-separated string across
the columns, so one paste fills the row — faster than typing, and it cannot lose a cell to the
encoding problem or to autocomplete. **Use this for every row, Latin ones included**; a single
uniform path is worth more than saving one call on the rows that happen to be ASCII.

### Getting to a cell

Click the Name Box (the cell-reference box just under the toolbar, around `x=50, y=126`), type the
address, press Return. It scrolls the row into view and selects it, which beats hunting for a row
by scrolling.

### Switching tabs

Click the tab you want in the strip along the bottom. **Do not navigate to a `#gid=` URL**: if
Sheets still has an unsaved change, the navigation is refused by a "Leave site?" dialog, and
forcing past it discards the edit.

### Inserting a row

With the target cell selected: menu **Вставка → Строки → Вставить строку выше**. The row that was
there moves down and takes the number after it.

Keep rows in their group — `#recipe_*` keys together, a Smelter recipe next to the other Smelter
recipes. Nothing in the code depends on the order, but the sheet is read by a person.

**An insert renumbers every row below it.** When several edits are queued, do the ones with fixed
addresses before the insert that would move them, or state the addresses as they will be
afterwards. Appending below the last row needs no insert at all.

### Screenshots

Each one costs on the order of 1 400 tokens, and the end-of-job CSV diff checks more than a
screenshot can. Take one after the first paste into an unfamiliar tab (that is where a column
offset would show), and otherwise rely on the verification step. Reading a cell back through the
Name Box and the formula bar is cheaper than a full screenshot when only one value is in doubt.

## The column-order trap

**Build a paste row from the SHEET's header, never from the repo CSV's.** The two differ, on
purpose, and a row generated from the CSV lands one column short from the first mismatch onwards:

- `recipes` in the sheet has **9** columns and no `comment`; the repo CSV has 10.
- `buildings` and `units` carry an `id_check` column the CSV does not (`AT` in buildings, `P` in
  units), so every letter after it is off by one.

Download the tab, read its header row, and pull each named column out of the repo row by name,
leaving a blank for `id_check` and for any formula column. Then the row is positional-paste-safe
by construction.

Two more rules that come from the same place:

- **Never take a paste column from the export's width.** A published tab is padded out to the
  widest cell anywhere on the sheet, so the export can be ten columns wider than the last named
  header. Find the last NAMED header and use the column after it.
- **Formula columns are dragged, not pasted.** Pasting blanks over one kills it.

## Verifying

Run the bundled script. It re-downloads every published tab with a cache-buster and compares it to
the repo CSVs on the columns the code actually reads:

```bash
python .claude/skills/balance-sheet/scripts/verify_sheet.py
```

The cache-buster matters: Google will serve a pre-paste copy twice in a row and make a good edit
look truncated.

A text diff is useless here — formula columns churn, and `''`, `0`, `1` and `1,0` all read
differently as strings while meaning the same number. The script compares numerically where it
can, which is what makes it able to say "no differences" and mean it.

Known permanent differences it reports and you can ignore: the `id_check` column, `cost_ironbar`
reading `0` in the repo and empty in the sheet, and the `comment` column on `recipes`.

**Read what it prints even when the row you added is fine.** Running it after the minting change
turned up `#recipe_batches` differing by a leading space (`" x{0}"` in the repo, `"x{0}"` in the
sheet) — a suffix that would have rendered "Мукаx2" after the next Pull. That defect predated the
change and nothing else would have found it.

The fix for that one is worth generalising: **do not put load-bearing whitespace in a sheet cell.**
The space had to be there because the summary concatenates the description and the multiplier
directly, which made a single invisible character at the front of a cell decide the layout — a
character nobody editing the sheet can see, review, or protect, and which Sheets had already
dropped once. It moved into `BuildingInfoPanelController.ProductionSummary`, where it is visible
code, and the cell now says `x{0}`, which is what it means. When a string's spacing matters, the
spacing belongs to the code and the words belong to the sheet.

## Header notes

Every header cell on every tab carries a NOTE (hover tooltip) saying in Russian what the column
means — the user asked for it (2026-10-24) so they know what they are changing. A new column gets
its note in the same edit: `update_spreadsheet` with `updateCells` and `fields: "note"` on the
header cell. Notes are not data — the CSV export ignores them. Per-level columns say which level
and that an empty cell repeats the level below; formula columns say not to type in them.

## Localization rows

Keys are derived from ids: a recipe's label is `#recipe_<recipe_id>`, a building's
`#building_<id>`. `BuildingRecipe.LocalizedName` falls back to the sheet's own `display_name`
column when the key is missing, so a forgotten localization row shows plausible text and hides
itself — add the key.

Typing `0	0` into a cell does NOT move to the next column -- the Tab lands in the cell's text.
Set each non-adjacent or adjacent cell through the Name Box one at a time (or paste).

A cell starting with `+`, `-` or `=` (e.g. `+{0}`) is pasted as a FORMULA and shows `0`: type it
with a leading apostrophe (`'+{0}`) instead of pasting it.

Both `ru` and `en` are required. Watch for `\n` and `{0}`-style placeholders surviving the paste
intact, and for leading or trailing spaces being significant — see `#recipe_batches` above.
