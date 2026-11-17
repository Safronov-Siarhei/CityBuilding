---
name: project-building-relocation
description: «Перенести здание» (sandbox) BUILT 2026-11-17, not seen -- four-arrow button right of «!», ✕/✓ with 1 crystal on the ✓, rebuilt in current level's time, same island
metadata:
  type: project
---
The user asked for it 2026-11-17; I forgot it once while doing the tutorial banner and the user caught it -- finish
every task named in a day before closing it ([[feedback-do-every-item]]).
Built 2026-11-17: MoveBuildingBar + BuildingPlacer.BeginMove/FinishMove/CanBeMoved, ConstructionSite.IsMove,
BuildingInstance.MoveTo, price badge on the ✓; key move_building_crystals, texts #move_status/#log_moved.
Card Design/features/building-relocation.md. Not seen; open: do workers find the new spot.
**Why:** players misplace buildings and want to fix the layout without demolishing.
**How to apply:** any change to placement (BuildingPlacer) must keep the _movingNow branches working.
