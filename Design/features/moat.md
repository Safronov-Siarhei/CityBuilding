# Moat (Ров)

> **BUILT c4e31bb4 (2026-11-02), compiled, untested, not seen in game.**

## Rules (the user, 2026-11-02)
- Option B: the moat SLOWS, never blocks. Everyone in its water walks at `moat_speed_multiplier` (0.4):
  orcs, soldiers, heroes, citizens.
- Routes keep off it while dry land or a road will do: NavMesh area `NavAreas.Moat` (4), cost `moat_path_cost` (6),
  set with `NavMesh.SetAreaCost` in `MeshMapApplier.RegisterMoat`.
- An orc in the water fights no soldier or hero (`OrcUnit.CanReach` -> `Moat.Wading`), but it STILL breaks a
  wall/building it stands against (variant A: a moat dug under a wall must not make the wall untouchable).
- Laid cell by cell like a fence (`keepSelectedAfterPlacement`); banks show on each side with no moat beside.
- Opened in the Laboratory for `moat_research_crystals` (10) upgrade crystals (`ResearchTopic.Crystals`),
  research_sec 60; ONE level. A cell costs 25 COINS: sandbox tab column `cost_coins` (new; `BalanceImporter.CostResources`
  now reads Coins).
- Its card: only the name «Ров» and Demolish (`RunBuildingCard` bare mode, `statParts`).

## Files
- `Buildings/Moat.cs` (cells, Wading, SpeedAt, bank refresh), `Buildings/MoatAppearance.cs`.
- `BuildingData.isMoat`, `IsGroundWork` (road or moat: no orc target, no decay, no level marker, no scaffolding);
  `FireManager` skips it; `BuildingInstance` registers/unregisters (trigger collider, no carve).
- Speed: `CitizenAgent.CurrentWalkSpeed`, `OrcUnit.Step`, `SoldierUnit.WalkTowards`.
- Editor: `SetupProjectBuildings.AddMoatBanks`, moat data in `SetupProjectCityScene` (Defence tab).
- Models: `Tools/blender/moat.py` -> Moat1-lvl1.fbx (water), Moat-Bank.fbx (south bank); icon `bld_moat` in render_icons.py.
- Balance: buildings row 23 (Moat), sandbox row 56 + column AN cost_coins, economy rows 430-432, localization row 556.

## Traps / open
- Not in Invasion runs (the invasion tab has no Moat row).
- The water uses the palette blue, not the map's living-water material.
- A saved moat restores through BuildingInstance.Initialize like any building (not checked).

## Crossings: Bridge and Drawbridge (BUILT e1d1d617, 2026-11-02, untested, not seen)
- The user: a bridge laid over the moat and on shallow water, long legs into the water (ref: old stone bridge);
  a DRAWBRIDGE 2x1, each cell a raising leaf, allies pass, enemies don't. Levels 1-2 timber, 3-5 stone.
- `BuildingData.isCrossing` (Bridge, Drawbridge): every cell over a moat (`Moat.Has`, not yet spanned) or
  shallow water (`IsWaterPlacementZone`); a land cell is allowed but at least one must be over moat/water
  (a Drawbridge over a 1-wide moat rests a leaf on the bank). Otherwise `#place_blocked_crossing`.
  Placed over a moat it is turned across the moat's line (`BuildingPlacer.TurnACrossingOverTheMoat`).
- Over a moat: `Moat.Span` marks the cells -- no slowing (`Wading` false), the moat's Moat area dropped
  (`MeshMapApplier.RegisterMoat` checks `Moat.IsSpanned`); `SharesCellWithAnother` keeps both occupying the cell.
- Bridge: isRoad (road speed, nobody's target), walkable surface, collider made a trigger. Sandbox up3-5 cost stone.
- Drawbridge: `isGate` with the whole footprint as passage (`RegisterGate` sizes it, no `CarveTheGateWalls`),
  walkable surface too -- `_walkableSurfaces` now keyed by Object (instance + instance.transform).
  `DrawbridgeLook`: per level two pivots at the hinges (+-0.78, deck 0.22), leaves rise to
  `drawbridge_raise_degrees` in `drawbridge_raise_sec` while an orc is within `drawbridge_raise_radius_cells`.
  Locked: research 30 coins/60 s; levels researched like the Gate. Rows: buildings 24, sandbox 57, economy 433-435,
  localization 557-558.
- Models `Tools/blender/crossings.py` (Bridge1-lvlN, Drawbridge1-lvlN, Drawbridge-Leaf-lvlN); SetupProject does NOT
  stand crossings on their lowest point (`FitModelToGround`), the legs go below the ground line.
- Traps: raised leaves are only a look -- allies still walk the Gate area; the leaf hinge numbers are duplicated
  in `SetupProjectBuildings.AddDrawbridgeLeaves` (keep in step with crossings.py).

## 2026-11-02 (923e2c5f): living water, two draws a cell
- Water: `Shaders/MoatWater.shader` (Materials/MoatWater.mat) on a quad over each cell, world-space ripples +
  glints so neighbouring cells read as one stream; the model's own bed/still water is not drawn.
- Banks: `MoatAppearance` combines the shown banks into ONE mesh per side-mask (16 cached, shared); Moat-Bank.fbx
  is imported Read/Write (`ModelImportDefaults`) for that. A cell = 2 draws (banks + water).
  `BuildingDrawCountTests` allows the MoatWater renderer by name.

## 2026-11-03: sea-like water, wading to the knee, orders onto the moat
- The living ripples were rejected («как море»). `Shaders/MoatWater.shader` now draws:
  - dark still water;
  - along each BANKED side only: a white foam line at the stone lip, then the shallows' turquoise fading into the deep (TerrainPalette sea colours).
- The banked sides reach the shader through UV1.x of a per-side-mask quad. `MoatAppearance.Water(mask)` holds 16 cached meshes, laid in world axes like the banks. The quad is a plain MeshFilter (`MoatWater`) wired by `MoatAppearance.Wire(..., water)`.
- Wading: every figure (soldier, hero, orc, citizen) is pinned `moat_wade_depth` (economy, 0.25) below the ground while `Moat.Wading` (`Moat.WadeDepthAt`, used in each `PinToGroundHeight`).
- An army order tapped on a moat, road or crossing sets the destination; it no longer selects the building (`ArmyOrderInput.HandleWorldTap` skips `IsGroundWork || isCrossing`).
- Not seen in game yet.
