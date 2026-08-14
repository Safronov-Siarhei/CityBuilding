# Design

Deliberately **outside `Assets/`**, so Unity never imports any of it — this is authoring material for humans, not game content.

## `CityBuilding_Balance.xlsx`

The starting point for the balance spreadsheet. Two tabs the game actually reads, and one that only thinks:

| Tab | What it is |
|---|---|
| `units` | One row per fighting unit. Yellow cells are authored; grey ones are formulas (dps, time-to-kill each way, coins per HP). |
| `economy` | `key` / `value` / `comment`. The keys are read by name in code — renaming one loses that setting (the import says so out loud). |
| `buildings` | One row per building, numbers only — cost per resource, population, production, health, defence, reveal radius, prerequisite, and the full upgrade costs. The `up2_`/`up3_` columns are formulas over the base cost, except where a building deviates on purpose (the Town Hall is free to place, and iron/coal gate the mines and the defence line). Grey `total_cost`/`per_day`/`payback_days` answer whether a producer pays for itself. |
| `сводка` | Read-only. Turns the raw numbers into the questions worth asking: who wins a duel, whether taxes cover the army, how long a portal survives a group of five, how many real minutes a building takes to fall apart. |

A building's **shape** is deliberately absent from the workbook: footprint, height, colours, which procedural generator or FBX builds it, and flags like "is a road" live in `SetupProject.cs`. Only numbers are balance.

**To put it to work:** upload to Google Drive → open as a Google Sheet → for `units`, `economy` and `buildings` do Файл → Поделиться → Опубликовать в интернете → pick the tab → format **CSV** → paste the three links into `Assets/_Project/Balance/BalanceSheetSettings.asset`. Then *CityBuilder → Balance → Pull From Google Sheets* refreshes the committed CSVs and rebuilds the config.

Notes typed under a table are exported by Google as ordinary rows. Keep each one in a **single cell of column A** — that is exactly how the importer tells prose from data (`BalanceImporter.IsNoteRow`); a note spread across columns gets read as a row of balance.

Don't publish `сводка` — nothing reads it.

The workbook is a convenience, not a source of truth: the committed CSVs in `Assets/_Project/Balance/` are what the build reads. If the two ever disagree, the CSVs win.

## `build_balance_xlsx.py`

Regenerates the workbook above from scratch, in place next to itself (`pip install openpyxl`, then run it). Kept because the derived columns and the summary tab are the actual work — the raw numbers are already in the CSVs.

Careful: running it **overwrites** the workbook with the numbers hardcoded in the script. Once the sheet is live in Google, it is the sheet — not this script — that is being edited, so regenerate only when you mean to rebuild the structure, and re-apply the numbers from the CSVs first.

Formulas here were checked by hand and by recomputing every derived value independently in Python; they were **not** machine-recalculated, because LibreOffice isn't available in this environment. Google Sheets recalculates everything on import, so an error would show up immediately in the cell.
