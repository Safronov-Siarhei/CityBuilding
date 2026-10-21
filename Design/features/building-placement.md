# Building placement, roads, fences, gates, demolition

> **Status (2026-10-21):** the PEN look is the only placement look (user's pick 2026-10-20, after six
> were photographed): an ink drawing hovering over a pen sketch of its plot. Roads and fences are laid
> ONE PIECE AT A TIME (the drag-to-draw line is gone). Gates are 1×1 and stand on roads.
> **Read with:** [camera-and-input](camera-and-input.md) (touch flow, `Design/touch_controls.md` —
> its Draw-mode section predates piece-by-piece roads), [buildings-catalog](buildings-catalog.md),
> [maps-and-terrain](maps-and-terrain.md), [gathering](gathering.md) (mines on ore veins).

## What the player sees
Pick a card → the build menu is replaced by ✕ ⟳ ✓. On touch the ghost is pinned to a FIXED screen
point (centre X, 58 % up) and the player drags the WORLD under it; confirm with ✓ or a tap on a cell
the ghost covers. Mouse keeps hover-and-click (`UsesTouchAiming`). When picked up, the pen sketches the
plot stroke by stroke and the drawing rises behind a line of fresh ink; each new cell → a hop and a
quicker re-sketch; dust on landing; a soft shadow. Red ink only over the cells in the way (hatched),
or all of it when the reason is elsewhere (money, start zone). Cost chips go red while stock can't cover them.

## Rules (decided)
- Aim point is the building's CENTRE (`OriginForCentredFootprint`). Footprint rotates in 90° steps.
- **Roads/fences (user, 2026-10-20):** placed like any building, piece by piece on ✓, and stay in hand;
  the next piece is offered right after the last, along the way the last two ran (first time: towards
  the screen's right). Touch: the camera glides one piece along. ✕ puts the road away.
- Fences autotile Straight / Corner / T / Cross (`FenceShape.Resolve`). Gates/towers join the line via
  `BuildingData.connectsToFences`. A fence is a real NavMesh obstacle.
- **Gate (user, 2026-10-21) is 1×1**, may stand over a road (road stays under it) and turns across it.
  It does NOT carve the NavMesh: it marks its cell as the Gate NavMesh area — citizens and soldiers
  path through, orcs path round (`NavAreas.WithoutGates`), so raiders stop and break it; its collider
  is a trigger. **Fortified gate is 1×3** (wall, passage, wall): only the middle cell is the Gate area
  and may sit on a road.
- **A mine fits only exactly over a free ore vein of its ore** (ghost snaps to the nearest within 4
  cells; `BuildingPlacer.IsFreeFor`).
- Refused: occupied, water, map no-build area (relief, fords) → «Занято»; Town Hall only inside the
  start zone and ≥ `town_hall_min_distance_to_orcs` from every orc spot. A new game opens with the
  view on the start zone; the Town Hall hint sits bottom-centre.
- Demolition refunds `demolish_refund_fraction` (0.5, NOT balanced). Roads don't carve the NavMesh.

## Code
`Buildings/BuildingPlacer.cs` (selection, aim, confirm, next-piece offer, refusal reasons, `IsFreeFor`),
`Buildings/PlacementPreview.cs` + `Buildings/SketchMarks.cs` + `Buildings/Ink.cs` +
`Shaders/PlacementGhost.shader` (always included via `RuntimeShaders`), `Buildings/FenceShape.cs`,
`FenceNetwork.cs`, `FenceAppearance.cs`, `Buildings/RoadNetwork.cs`, `Maps/NavAreas.cs`
(`MeshMapApplier.RegisterGate`), `Grid/GridManager.cs` (cells, occupancy, `GroundHeight`,
`OnAreaOccupancyChanged`), `Maps/StartZone.cs`, `Maps/StartZoneOverlay.cs`,
`UI/PlacementHudController.cs`, `UI/BuildingCategoryPanel.cs`, `UI/BuildingSelector.cs`,
`UI/CategoryButtonHandler.cs`, `UI/HotbarButtonHandler.cs`, `UI/BuildingPlacerUIVisibility.cs`,
`UI/CostRowBuilder.cs`.

## Tests
EditMode `PlacementAimTests`, `FenceShapeTests`, `GridManagerTests`, `RoadNetworkTests`; PlayMode
`PlacementPreviewTests` (road with a turn, each offer; Caldera meadow/lake shore; photo with graphics),
`FenceAutotilingTests`, `RotatedFootprintTests`, `GateTests`, `CostRowTests`, `StartPlacementTests`.

## Traps
- Gesture recognition has no automated coverage; the checklist in `Tools/research/build_notes.txt` is
  the phone verification.
- `BuildingInstance.OnDestroy` must decrement its count FIRST (an exception on scene unload once left
  the Town Hall counted into the next game).
- The sketch reuses its buffers — keep the per-frame rebuild garbage-free.

## Open
Fence's three levels (upgrade vs separate buildings) undecided; the Bridge stays research-locked.
