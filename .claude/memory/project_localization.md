---
name: project-localization
description: "How CityBuilding's text works: a localization tab in the balance sheet, keys derived from ids, and the traps that bite when adding text or a language."
metadata: 
  node_type: memory
  type: project
  originSessionId: e11f94ca-15a8-4229-b2a6-7681d9d8dbf8
  modified: 2026-08-17T13:06:18.888Z
---

Since 2026-08-17 (`7d37c40`) **no user-visible string is a literal in C#**. Text is content and lives in the spreadsheet with the balance numbers — a fifth tab, `localization`: `key`, then one column per language (`ru`, `en`), then `comment`. 171 keys today. See [[project_balance_pipeline]] for the pipeline it rides on.

- `localization.csv` → `BalanceImporter.RebuildLocalization` → `Assets/_Project/Resources/LocalizationConfig.asset` → `Localization` at runtime.
- **The columns after `key` ARE the language list.** A third language is a column in the sheet and zero code: the settings screen builds its buttons from `LocalizationConfig.Languages`.
- `Localization` is **static, not a MonoBehaviour singleton** — text is requested before any manager wakes, and a missing `Instance` would mean a game of blank labels. Language is remembered in `PlayerPrefs`, defaulting to the system language when the sheet has that column.
- `Localization.Get` returns **the key itself** when missing (an untranslated caption must be visible to whoever is testing). `GetOrDefault` is the exception, used only for names the balance sheet also carries.
- Fixed captions carry a `LocalizedText` component (attached by `SetupProject.Localize`); labels built out of numbers call `Localization.Format` in their controller.

**Key format is `#section_name`, all lowercase** (the user's choice, `78803eb`): `#ui_close`, `#log_no_input`, `#building_fortifiedtower`. The leading `#` makes a raw key on screen unmistakable and greppable.

**Keys are DERIVED from ids, never stored in the other tabs**: `#building_<id>`, `#recipe_<recipe_id>`, `#resource_<type>`, `#tier_<code>`, `#unit_<soldiertype>` — the four call sites append `.ToLowerInvariant()`, which is why lowercase was the right choice: no mapping table to drift. So adding a building needs one localization row and no schema change anywhere. Building and recipe names **fall back to the `display_name` the balance sheet already carries**, so a half-filled localization tab reads as Russian rather than as `#building_smelter`.

## Traps

- **Never put a real line break in a cell.** A published Google Sheet exports it as two CSV rows, and pasting one back splits it. Write `\n` as two characters; `BalanceImporter` turns it into a real break on import (`a30f1bb`). `happiness.breakdown` is the only such string today.
- **Never start a string with `=`, `+`, `-` or `@`.** Google Sheets parses that cell as a formula and the whole string comes back as `#NAME?`, which nothing downstream can tell from a real translation. Hit on 2026-08-17 with `"+ Назначить"` / `"- Снять"` (`a82f73a`); those captions dropped their signs. `LocalizationTests.NoTextStartsWithSomethingTheSheetWouldReadAsAFormula` scans every string in every language for it.
- **Placeholders are the failure that actually happens.** A translation losing its `{0}` shows a hole; one growing a `{2}` throws `FormatException` inside a UI refresh and takes the panel down. `LocalizationTests.TranslationsKeepTheirPlaceholders` compares placeholder sets across languages for a hand-listed set of keys — **add new format keys to that list**.
- Language names in the settings screen are written **in their own language** (`Русский`, `English`) and deliberately not translated — someone opening that screen cannot read the current one.
- Editor-only Russian (cheat inspectors, `BalanceSheetSettings` tooltips, importer dialogs) is deliberately NOT localized; the player never sees it.

**Regenerating the tab:** `Tools/balance/make_localization.py` (untracked, kept out of the scratchpad so it survives a new session) rebuilds `localization.csv`, pulling building and recipe names out of the other CSVs so the Russian column can never drift from them. It fails loudly on an id with no English name rather than emitting a blank. `Tools/balance/compare_sheets.py` diffs the published tabs against the committed CSVs by column name and row id.
