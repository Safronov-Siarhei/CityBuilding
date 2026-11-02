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
