---
name: project-pending-2026-11-19c
description: "Windows-build playtest 2026-11-19 (11 items): which were Windows-only, what is done, the broken generated-prefabs trap, open items"
metadata:
  node_type: memory
  type: project
  originSessionId: 65820551-ddcb-4e52-a0d1-9eafc8080c74
  modified: 2026-11-19T19:21:46.557Z
---

The user's notes from the Windows build (2026-11-19), done in code, no build/test (user said none):
- Menu/film houses missing = Windows PC quality level (Forward+, D3D12); Standalone now uses the Mobile level.
- Invasion towers silent = a Unity run at 17:21 regenerated building prefabs WITHOUT the sheet (no DefensiveBuilding, empty requiredBuilding); the Windows build shipped them, the APK did not. RESTORED from HEAD 2026-11-19 on the user's yes (Tower/Castle/FortifiedTower carry DefensiveBuilding again). See [[feedback-build-only-on-request]].
- Subtitle double shadow -> one outline; red dots on round buttons' rings, shop dot on the right; loading ship smaller; Skip: EventSystem guard + Esc/Space/Enter + `[FilmSkip] skip` log line (cause unproven).
- Ратуша -> Замок in all 15 text keys (ru+en), sheet + CSV.
- OPEN: «выбор здания в нашествии затемняет не весь интерфейс»: needs a screenshot.

**Why:** the user asked whether these were Windows-only: houses yes (graphics level), towers yes by accident (broken files), the rest no (would show on phones too).
**How to apply:** before any build, `git status` for modified Prefabs/ScriptableObjects; next setup must be `setup,setup`.
