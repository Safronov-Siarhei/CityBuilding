# Design

Deliberately **outside `Assets/`**, so Unity never imports any of it — this is authoring material for humans, not game content.

## `CityBuilding_Balance.xlsx`

The starting point for the balance spreadsheet. Two tabs the game actually reads, and one that only thinks:

| Tab | What it is |
|---|---|
| `units` | One row per fighting unit. Yellow cells are authored; grey ones are formulas (dps, time-to-kill each way, coins per HP). |
| `economy` | `key` / `value` / `comment`. The keys are read by name in code — renaming one loses that setting (the import says so out loud). |
| `сводка` | Read-only. Turns the raw numbers into the questions worth asking: who wins a duel, whether taxes cover the army, how long a portal survives a group of five, how many real minutes a building takes to fall apart. |

**To put it to work:** upload to Google Drive → open as a Google Sheet → for `units` and `economy` do Файл → Поделиться → Опубликовать в интернете → pick the tab → format **CSV** → paste the two links into `Assets/_Project/Balance/BalanceSheetSettings.asset`. Then *CityBuilder → Balance → Pull From Google Sheets* refreshes the committed CSVs and rebuilds the config.

Don't publish `сводка` — nothing reads it.

The workbook is a convenience, not a source of truth: the committed CSVs in `Assets/_Project/Balance/` are what the build reads. If the two ever disagree, the CSVs win.

## `build_balance_xlsx.py`

Regenerates the workbook above from scratch (`pip install openpyxl`, then run it). Kept because the derived columns and the summary tab are the actual work — the raw numbers are already in the CSVs.

Formulas here were checked by hand and by recomputing every derived value independently in Python; they were **not** machine-recalculated, because LibreOffice isn't available in this environment. Google Sheets recalculates everything on import, so an error would show up immediately in the cell.
