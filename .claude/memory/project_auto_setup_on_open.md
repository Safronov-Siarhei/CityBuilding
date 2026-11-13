---
name: project-auto-setup-on-open
description: Opening the Unity project auto-runs Pull From Google Sheets then Setup Project (2026-11-13) — local CSV edits not in the sheet are overwritten on next open
metadata:
  type: project
---

Since 2026-11-13 (`Scripts/Editor/AutoSetupOnOpen.cs`): the user's Editor runs Pull (quiet) then Setup Project once per session; batchmode skipped; toggle «CityBuilder/Автозапуск при открытии (Pull + Setup)».

**Why:** the user asked to automate both on project open.

**How to apply:** ALWAYS sync the Google Sheet before the user opens Unity — any CSV change not in the sheet is overwritten by the pull. Card: Design/features/build-and-test.md. Related: [[project-balance-pipeline]].
