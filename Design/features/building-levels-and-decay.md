# Building levels (1–5), upgrades, decay and repair

> **2026-10-25:** decay OFF (`BuildingInstance.DecayEnabled` = false); upgrades and new buildings take `build_sec(_N)` time in the sandbox too; repair = damage-priced `RepairCostOf`; sandbox prices now on the `sandbox` tab -- see [sandbox-run-systems](sandbox-run-systems.md). The decay rules below are history.

> **Status (2026-10-21):** levels 1–5 in the game with models for all 44 buildings (`f80abe62`).
> Numbers for levels 4–5 are EXTRAPOLATED and nobody has played them.
> **Read with:** [research-laboratory](research-laboratory.md), [art-pipeline](art-pipeline.md),
> [hazards-fire-sickness](hazards-fire-sickness.md).

## Rules (decided)
- `BuildingInstance.MaxLevel = 5` (soldiers stay at `UnitBalance.MaxLevel = 3`). Researching level N
  needs Laboratory level N, so 4–5 are late game by construction. L2 always before L3.
- Upgrade cost `BuildingData.UpgradeCostTo(level)` from `up2_..up5_` columns.
- Per-level stats: `<column>` = L1, `<column>_2.._5`; an empty higher cell repeats the level below.
  L4/L5 columns made by `Tools/balance/extend_levels_4_5.py` (×1.44 then ×1.39 for HP/defence/
  happiness/storage; workers/batches/housing +the 2→3 step; radii ×1.2; up4/up5 = up3×1.75/×1.75²;
  research coins ×1.7, 180 s / 240 s).
- Upgrade rule: sprawl cheaper in resources, upgrade cheaper in people ([production-chains](production-chains.md)).
- **Models:** `<Id>1-lvl2..5` picked up BY NAME by `SetupProject.AddLevelModels`; every placed building
  carries 5 model children (4 inactive). HEIGHT follows the level (click box via
  `BuildingLevelAppearance.ResizeClickBox`); FOOTPRINT stays level 1's. A silver/gold disc shows only
  over buildings with no model for their level (fence, water buildings).
- **Decay:** accrues with age, `decay_per_day_at_level1` ÷ level. Above `decay_penalty_threshold`
  (70 %) production falls linearly to `min_decay_production_multiplier` (50 %) at 100 %; at 100 % the
  building is destroyed. Town Hall, roads, bridges exempt. Repair = `repair_cost_fraction` (40 %) of
  base cost, resets to 0. Orange marker over a building in the penalty band. Decay = fire chance.

## Code
`Buildings/BuildingInstance.cs` (`TryUpgrade`, `ComputeNextDecay`, `TryRepair`, `MaxLevel`),
`Buildings/BuildingLevelAppearance.cs`, `Buildings/BuildingHealthBar.cs`, `Buildings/WorldIndicator.cs`
(marks world-space HUD so look tests skip it), `UI/BuildingInfoPanelController.cs` (Upgrade/Repair buttons).

## Tests
EditMode `BuildingLevelAppearanceTests`, `BuildingInstanceDecayTests`, `UpgradeBalanceTests`;
PlayMode `StorageAndUpgradeTests` (`Photograph_TheBuildingLevels` — occasional "nowhere free" flake).

## Traps
- Adding a per-level stat = THREE places: sheet column, `BalanceImporter.ReadLevels`,
  `SetupProject.CopyLevels`. Miss the last and config tests pass while placed buildings are worth zero.
- `Destroy` is deferred: markers disable their collider before destroying, or the first frame eats
  the tap meant for the building.
- `batches_per_tick_2/_3` were empty on all rows until 2026-08-21 — print raw cells before claiming
  a cell is empty (a display script once blanked authored `service_radius` columns).

## Open
The card never says what an upgrade BUYS («Рабочих 3 → 4»); L4/L5 balance unplayed; ~18 MB of level
FBX referenced by prefabs (APK size).
