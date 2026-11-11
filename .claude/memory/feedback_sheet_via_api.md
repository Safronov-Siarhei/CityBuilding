---
name: feedback-sheet-via-api
description: "Edit the Google balance sheet only through the API / a Google MCP connector, never through the browser; if no connector is in the session, ask the user to connect one."
metadata:
  node_type: memory
  type: feedback
  originSessionId: da5d3a69-4ad8-4054-87db-e49cf7a02848
  modified: 2026-10-25T17:59:59.658Z
---

The user, 2026-10-25: «когда будешь работать с конфигов в гугл, то работай через апи или мсп гугл».
All edits of `CityBuilding_Balance` (new tabs, rows, header notes) go through the Google Sheets API or a
Google MCP connector -- not Claude in Chrome / the built-in browser.

**Why:** the browser path is slow and fails silently (Cyrillic typed into the canvas vanishes); the API
reads back exactly what was written.

**How to apply:** check `session_connectors_status` first; when no Sheets connector is there, finish the
repo side (CSV, code, tests) and ask the user to connect one rather than falling back to the browser.
Supersedes the browser fallback in [[feedback-balance-row-handoff]]; the skill is `balance-sheet`.
