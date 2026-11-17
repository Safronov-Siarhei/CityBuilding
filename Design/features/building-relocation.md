# Moving a building (Перенести здание)

> **Status (2026-11-17): BUILT (setup green), not seen by the user. SANDBOX ONLY.**

## Rules (the user, 2026-11-17)
- Every building held gets one more round button RIGHT of the «!» (which opens the info card): four arrows pointing
  every way = «перенести здание».
- A tap opens placement again for that building: ✕ and ✓ as when placing (`building-placement`).
  - ✕: the building stays where it was, nothing rebuilt, nothing paid.
  - ✓ shows its price, 1 crystal: the building is rebuilt on the new spot; the rebuild takes as long as building it.

## Details (agreed 2026-11-17)
- SANDBOX ONLY.
- Every building EXCEPT the Town Hall and the piece-laid ones: roads, fences, walls, bridges, the moat.
- The rebuild takes the CURRENT level's build time.
- While it is rebuilt it does not work (no production, no service); a store keeps its contents; residents of a
  house and soldiers of a barracks stay bound to it and wait.
- Only onto the SAME island (residents are bound to their island).
- Short of the crystal -> the shop, like «Построить сразу» (InstantBuildBar). Not in the tutorial.
- The button row: [Построить сразу] [Дипломатия] [!] [Перенести] (each only where it applies).

## How it works (code)
- `UI/MoveBuildingBar.cs` -- shows the button while `BuildingFocus` holds a building that `BuildingPlacer.CanBeMoved`
  allows; the tap clears the focus and calls `BuildingPlacer.BeginMove`.
- `BuildingPlacer`: `CanBeMoved` (sandbox, not tutorial, own, finished, alive; never Town Hall, road, fence/gate,
  moat, crossing, walkable surface, water category, Shipyard, wall pieces, ore mines); `BeginMove` selects the
  building's data, copies its rotation and FREES its old cells (it may land overlapping itself); `CostOf` is free
  for it, the research gate and OnlyOne are skipped, the same-island check is added; `TryPlace` -> `FinishMove`
  (crystals or the shop, `BuildingInstance.MoveTo`, new cells taken, `ConstructionSite.Begin(..., move: true)`,
  log `#log_moved`). `ClearSelection` (✕, or any clear) takes the old cells back if the move never finished.
- `ConstructionSite.IsMove`: no piggy-bank task, no upgrade fanfare on its finish.
- `BuildingInstance.MoveTo`: transform, footing rebuilt (`BuildingFooting.Add`), fog opened.
- `PlacementHudController.movePrice` -- the crystal + number on the ✓ (built in `SetupProjectGameplayUI`);
  the status line says `#move_status`.
- Button built by `SetupProjectHud.BuildMoveBuildingButton` (glyph `Hud_Move`, `Tools/ui/make_hud_glyphs.py`).
- Numbers/texts: `move_building_crystals` (economy.csv, sheet «Здания и постройки»), `#move_status`
  («Тексты: здания и ресурсы»), `#log_moved` («Тексты: журнал событий»).

## Traps
- Workers walking to the old spot and NavMesh carving are not re-done explicitly: check in a playtest whether
  workers/residents find the new spot.
- A save taken mid-move (between the tap and ✓) has the building on its old cells -- the cells are freed only in
  memory, so nothing is lost.

## Open
- The user has not seen it. No test (UI + a placement path; a test only if a bug turns up).

See also [building-placement](building-placement.md) (the placing look), [resources-and-storage](resources-and-storage.md).
