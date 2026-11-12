---
name: project_island_residents
description: AGREED 2026-11-12, not built: residents bound to their house, workers/soldiers only from the building's island, Town Hall hidden residents; all points settled
metadata:
  type: project
---
The user's design (2026-11-12) after a citizen walked the sea to a sawmill on another island: each house
makes and keeps its own residents (spawned beside it, stops at capacity); burnt house -> its residents
leave; buildings and the Barracks take people only from their own island, a «no workers» mark + ONE
notice when none; soldiers keep their house place; the Town Hall silently holds a few permanent residents
(hidden anti-loss); people move between islands only by building houses there; hunger removes from any
house; old saves irrelevant. Full rules: Design/features/citizens-and-workforce.md «AGREED, NOT BUILT».
A soldier whose house is destroyed leaves AT ONCE with its residents (the user's answer).

**Why:** a town-wide pool would let islands overpopulate when the main island's houses burn.
**How to apply:** build only when the user says go; every rule is settled.


**Built 2026-11-12:** HousePeople ledger beside the CitizenManager pool (reconciled every frame, not saved), island checks in ProductionBuilding.TryAssignWorker + ArmyManager.TryRecruit, figures bound to homes in CitizenVisualsManager; texts #log_house_lost, #log_no_people_on_island, #army_no_citizens_island on the sheet. Compiled, setup green 2026-11-12; next: the user's playtest. Details in Design/features/citizens-and-workforce.md.
