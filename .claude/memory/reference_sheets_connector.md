---
name: reference-sheets-connector
description: The balance sheet is edited through the Google Sheets claude.ai connector (API) since 2026-10-22, not the browser.
metadata:
  type: reference
---

The user connected the **Google Sheets** connector on 2026-10-22 (asked: work with the sheet via API, not screenshots). Tools `mcp__<id>__get_values / update_values / insert_dimension / append_values` (ToolSearch to load). Spreadsheet id `1eroe1LMQCrpFtP4se7Oae4pGv-aOrHiqYAMdWKxxook`; insert_dimension's sheetId = the tab gid (economy 257341966, localization 304072291, buildings 766628053, units 538856846, recipes 208452028).

Writes are USER_ENTERED: `+{0}` becomes a formula -> write `'+{0}`. Decimals as text with a comma. Read back after writing. The workflow lives in the `balance-sheet` skill; Chrome is the fallback. See [[feedback_balance_row_handoff]], [[project_balance_pipeline]].

**UPDATE 2026-11-04 (the user):** «если тебе понадобится вносить изменения в таблицу в гугл, то я подключил расширение клода, правь через него» -- the Sheets connector was not in the session; edit through Claude in Chrome now (one connected browser, "Browser 1"). Working path: open the sheet in a new tab, find the last row with an in-page `fetch('/spreadsheets/d/<id>/export?format=csv&gid=<gid>')`, put the TSV on the clipboard via javascript_tool, Name Box -> first empty A-cell -> ctrl+v; then verify_sheet.py.

**2026-11-05:** the user (desktop app account «Kostya») connected the Google Sheets connector again (Settings → Customize → Connectors → Discover). It works in session: the read of economy!A598 succeeded. Use the API first again; the Chrome extension is the fallback (it was offline once on 2026-11-05). Also: botwaves gid is 1680852949.
