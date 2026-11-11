# Balance and localization pipeline

> **2026-11-11 (the user: «чтобы я понимал без вопросов, где и что менять»):** the sheet is 36 Russian-named tabs with an «Оглавление» first (links, what is where; green = numbers, orange = modes, blue = texts). The 9 repo CSVs and all parsing are unchanged: `Balance/sheet_tabs.csv` (`target,gid,tab`) says which tabs make up which CSV, Pull merges them (`BalanceImporter.MergeTabs`; a tab with the first tab's header loses it, a table with its own header -- `building`/`unit`/`key` -- keeps it after a blank line). A CSV is written only if ALL its tabs came down. `BalanceSheetSettings.publishedUrl` is the one document link (whole document published, so a new tab downloads with no Publish press). Old tabs `economy`, `localization`, `invasion`, `sandbox` are hidden as «СТАРЫЙ … (не править)»: NOT read by anything. Tab order in sheet_tabs.csv decides row order in the CSV, nothing else; a key may live on any tab of its target. New tab = addSheet + one line in sheet_tabs.csv + a row in «Оглавление». «Сводка» formulas point at the new tabs; 4 of its rows show #N/A since long before the split (their keys `coins_per_citizen_per_day_at_max_tax`, `raid_days_per_extra_raider` no longer exist).
>
> **2026-10-25:** new tab `sandbox` (gid 291742027, `Balance/sandbox.csv`, `BalanceSheetSettings.sandboxCsvUrl`) laid over buildings/units/economy for the SANDBOX config, same shape as `invasion`; made through the Sheets API (connector), published and pulled 2026-10-25 (0 differences) -- [sandbox-run-systems](sandbox-run-systems.md). Sheet edits go through the API/MCP only (the user).

> **Status (2026-10-21):** sheet == repo (verify_sheet: no differences, 2026-10-20). Claude edits the
> Google Sheet DIRECTLY via the `balance-sheet` project skill (`.claude/skills/balance-sheet/`) —
> the user said to stop handing over paste blocks.
> **2026-10-23:** new tab `invasion` (gid 1941808734) -- the Invasion mode's own balance, laid over the
> other tabs for a run (rules in [invasion-mode](invasion-mode.md) «Balance»); its run-only keys moved
> out of economy. The Sheets connector was not in that session; the browser fallback did it.
> **Read with:** whichever feature's numbers/text you are changing.

## Rules
- **No gameplay number and no user-visible string is a literal in C#.** Numbers live in the sheet;
  `BalanceConfig.cs` field initialisers are a fallback only (never tune there).
- **Uploading (the user's question, 2026-10-23):** Unity → CityBuilder → Balance → Pull From Google
  Sheets downloads every published tab into the CSVs and rebuilds both configs; the generated assets
  (BuildingData) need a SetupProject run for the SANDBOX's building numbers, the run's are applied live.
  Since 2026-11-10 it downloads all tabs AT ONCE with UnityWebRequest, 30 s timeout each, behind a
  cancelable progress bar (`BalanceImporter.DownloadAll`). TRAP: the old one-by-one WebClient had no
  timeout and froze the Editor for 4+ minutes (Mono WebClient stalls; the links themselves answer in 1-2 s).
- Chain: Google Sheet `CityBuilding_Balance` (doc `1eroe1LMQCrpFtP4se7Oae4pGv-aOrHiqYAMdWKxxook`) →
  committed `Assets/_Project/Balance/{units,economy,buildings,recipes,localization}.csv` →
  `Editor/BalanceImporter.cs` (run first thing by `SetupProject.Run`) → `Resources/BalanceConfig.asset`
  + `Resources/LocalizationConfig.asset`. Build and tests never touch the network.
- Nothing reads the config per frame: units copy values at Awake/Initialize.
- Columns are matched BY NAME; derived sheet columns (`dps`, `ttk_*`, `per_day`…) and quoted decimal
  commas are expected — don't "clean them up". Notes under a table must sit in ONE cell of column A
  (`IsNoteRow`).
- A new per-level stat = sheet column + `BalanceImporter.ReadLevels` + `SetupProject.CopyLevels`.
- **Localization:** tab `localization` = `key`, one column per language (`ru`, `en` — adding a column
  adds a language, zero code), `comment`. Keys `#section_name`, lowercase. Keys derived from ids, never
  stored: `#building_<id>`, `#recipe_<id>`, `#resource_<type>`, `#unit_<type>`, `#tier_<code>`,
  `#map_name_<id>`. `Localization` is static; `Get` returns the key itself when missing.
  Fixed captions carry `LocalizedText`; computed labels call `Localization.Format`. Editor-only
  Russian is deliberately not localized.

## Code / tools
`Core/BalanceConfig.cs`, `Core/Localization.cs`, `Core/LocalizationConfig.cs`, `UI/LocalizedText.cs`,
`Editor/BalanceImporter.cs`, `Editor/BalanceSheetSettings.cs` (+ `Balance/BalanceSheetSettings.asset`
with the document link), `Balance/sheet_tabs.csv` (tab → CSV). `Tools/balance/`: `make_localization.py`, `compare_sheets.py`, `extend_levels_4_5.py`.
`Design/README.md` describes the workbook; `Design/build_balance_xlsx.py` OVERWRITES it (rarely wanted).

## Tests
EditMode `BalanceConfigTests` (sheet-shaped breakage), `LocalizationTests` (placeholders kept across
languages — sweeps every row; no string starts with `= + - @`).

## Traps
- Never a real line break in a cell — write `\n` as two characters.
- A string starting with `=`, `+`, `-`, `@` becomes a formula → `#NAME?`.
- A comma inside an unquoted CSV cell splits the row (English column became half the Russian sentence).
- The sheet's row ORDER is not the CSV's — append, don't match positions. Never paste whole rows over
  formula columns (`total_cost`, `per_day`, `payback_days`, `up*`).
- Google caches published CSVs — cache-bust when diffing. Cyrillic can't be typed via the extension
  (fails silently); Chrome screenshots time out on this machine — read Name Box / formula bar via JS.
