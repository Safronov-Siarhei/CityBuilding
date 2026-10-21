# Building placement, roads, fences, demolition

> **Status (2026-10-21):** shipped. NEW: four alternative placement looks built 2026-10-20
> (`bff312ec`) and photographed (`playtest_shots/placement-look-*`) — **waiting on the user's pick**;
> `PlacementPreview.Look` is still `Classic` (the user called Classic "cheap").
> **Read with:** [camera-and-input](camera-and-input.md) (touch flow, `Design/touch_controls.md`),
> [buildings-catalog](buildings-catalog.md), [maps-and-terrain](maps-and-terrain.md).

## What the player sees
Pick a card in the build menu → the HUD's build menu is replaced by ✕ ⟳ ✓. On touch the ghost is
pinned to a FIXED screen point (centre X, 58 % up) and the player drags the WORLD under it; confirm
with ✓ or a tap on a cell the ghost covers. Mouse keeps hover-and-click (`UsesTouchAiming`). Roads and
fences are DRAWN as lines. Demolish lives in the building card.

## Rules (decided)
- Aim point is the building's CENTRE, not its corner (`OriginForCentredFootprint`).
- Fence/road lines snap to an axis (`SnapStraightEnd`); ✓ in draw mode = finish.
- Fences autotile: Straight / Corner / T / Cross (`FenceShape.Resolve`; T opens WEST at step 0);
  `ResolveWithoutJunctions` is the old 2-piece rule when T/Cross models are missing. Gates/towers
  join the line via `BuildingData.connectsToFences`. The fence is a real NavMesh obstacle.
- Roads don't autotile (one model per cell) and don't carve the NavMesh.
- Refused cells: occupied, water, map no-build area (relief, fords) → «Занято»; Town Hall only inside
  the map's start zone and ≥ `town_hall_min_distance_to_orcs` (45 m) from every orc spot
  (`#place_blocked_start_zone`); the zone is drawn until the hall stands.
- Footprint rotates with the building (90° steps). Footprint stays level 1's on upgrade.
- Demolition refunds `demolish_refund_fraction` (0.5, NOT balanced — beats losing it to a raid).
- Placement looks (`PlacementLook`): Classic (green/red tint), OwnColours (gold plate), Drawing (ink),
  OwnColoursAndCells, DrawingAndCells — all but Classic hop cell to cell and puff dust on landing.
  Shader `Shaders/PlacementGhost.shader`, always included in builds (`RuntimeShaders`).

## Code
`Buildings/BuildingPlacer.cs` (selection, aim, confirm, draw mode, refusal reasons),
`Buildings/PlacementPreview.cs` (looks, ground plate, rebuilt only when cell/verdict changes),
`Buildings/FenceShape.cs`, `FenceNetwork.cs`, `FenceAppearance.cs`, `Buildings/RoadNetwork.cs`,
`Grid/GridManager.cs` (cells, occupancy, `GroundHeight`, `OnAreaOccupancyChanged`),
`Maps/StartZone.cs`, `Maps/StartZoneOverlay.cs`, `UI/PlacementHudController.cs`,
`UI/BuildingCategoryPanel.cs`, `UI/BuildingSelector.cs`, `UI/CategoryButtonHandler.cs`,
`UI/HotbarButtonHandler.cs`, `UI/BuildingPlacerUIVisibility.cs`.

## Tests
EditMode `PlacementAimTests`, `FenceShapeTests`, `FenceLineSnapTests`, `GridManagerTests`,
`RoadNetworkTests`; PlayMode `FenceAutotilingTests`, `RotatedFootprintTests`, `PlacementLookPhotoTests` (graphics).

## Traps
- Nothing about gesture recognition is covered by tests; the 9-point checklist in
  `Tools/research/build_notes.txt` is the only verification on a phone.
- `BuildingInstance.OnDestroy` must decrement its count FIRST (an exception on scene unload once left
  the Town Hall counted into the next game).

## Open
User's pick of placement look; fence's three levels (upgrade vs separate buildings) undecided;
the Bridge as a closed early-game wall (user kept it closed).
