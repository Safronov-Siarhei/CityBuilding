---
name: project-account-switcher
description: Editor account switcher (2026-11-13) — toolbar «Аккаунт» must be ticked via right-click on the toolbar; fallback menu CityBuilder → Аккаунт…
metadata:
  type: project
---

Editor-only account switcher built 2026-11-13 (`Scripts/Editor/AccountToolbar.cs`): an account = all PlayerPrefs + persistentDataPath, kept in `Library/CB_Accounts/<name>/`. Card: Design/features/build-and-test.md.

**Why:** the user could not restart campaign level 2 after playing it half-way.

**How to apply:** if the user says the button is missing, tell them Unity hides new main-toolbar elements: right-click the toolbar near Play and tick «CityBuilder/Аккаунт», or use menu CityBuilder → Аккаунт…. Not yet confirmed working by the user. Also fixed the same day: ~800 "economy tab has no key" errors per load (run config lacked sandbox-only keys; stale bot monster entries).
