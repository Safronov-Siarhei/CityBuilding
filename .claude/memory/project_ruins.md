---
name: project-ruins
description: "Ruins of the player's destroyed buildings — BUILT 2026-11-05 (look A burnt shell via shader), untested, user not seen"
metadata:
  node_type: memory
  type: project
  originSessionId: 65820551-ddcb-4e52-a0d1-9eafc8080c74
  modified: 2026-11-05T08:16:31.684Z
---

Ruins (card Design/features/ruins.md) BUILT 2026-11-05 after the user approved look A «обгоревший остов».
Sandbox only, from everything but the Town Hall and moat; card Восстановить (price above) / Снести / Закрыть;
restore = full worth incl. coins × ruin_restore_cost_fraction 0.5, as construction × ruin_restore_time_fraction 0.5 (sandbox tab, sheet synced).
Look is a GPU shader (BuildingRuin.shader) clipping the building's own mesh — building meshes are not readable on device.

**Why:** the user wanted destroyed buildings to leave something to restore instead of vanishing.
**How to apply:** tuning (break height, char) waits for the user's playtest; tests go into the next general run. Related: [[project-bot-settlement]].
