# Gathering: trees, rocks and the harvest walk

> **Status (2026-10-21):** shipped. Rated by the audit as one of the genuinely good systems
> (the only one where geography matters). Gathering income never retuned since the walk became income.
> **Read with:** [maps-and-terrain](maps-and-terrain.md), [production-chains](production-chains.md).

## What the player sees
Sawmill/Quarry workers walk to trees/rocks inside the building's harvest radius, chop with a progress
bar, and carry the load home. A green radius carpet shows while placing/selected. A citizen can be
sent by hand to chop one tree (breaks the no-wood bootstrap deadlock). Felled trees regrow.

## Rules
- Trees exist only inside the map's authored `TreesArea`; rocks go into `RocksArea` clearings
  (Map 3+) or over the whole map (older maps). So "no wood is gathered" is usually geography.
- A gatherer with nothing in reach says so **on its card and workforce row for as long as it is true**
  (`ProductionBuilding.NothingLeftInReach`, asked fresh each redraw). `ResourceNode.AnyHarvestableWithin`
  is deliberately softer than a worker's search: a claimed boulder and a sapling both count.
- Rock models: Stone1–10 size ladder picked by cell hash (no save change). Every rock still holds
  `stone_per_rock`; yield-by-size postponed by the user («разберёмся позже»).
- Gatherer upgrades add workers (Sawmill/Quarry 3/4/5) or reach, not batches.

## Code
`Maps/ResourceNode.cs` (registry `All`, yields), `Maps/TreesAreaSpawner.cs`, `Maps/RockSpawner.cs`
(`ClearCell` used by orc camps), `Maps/TreeGrowth.cs`, `Maps/HarvestProgressBar.cs`,
`Grid/HarvestRadiusOverlay.cs`, `Citizens/ManualGathering.cs`,
`Citizens/CitizenVisualsManager.cs` (worker node search, `ReportRadiusEmpty` latch),
`Citizens/CitizenAgent.cs` (Gathering mode).

## Data
economy: `wood_per_tree`, `stone_per_rock`, `wood_per_harvest`, `stone_per_harvest`, `harvest_seconds`.
buildings: harvest radius / workers per level.

## Tests
PlayMode `GatheringTests`, `GatherRadiusReachTests`, `ForestClickTargetingTests` (**flaky**: unseeded
forest, ~2/100), EditMode `ResourceNodeYieldTests`, `HarvestRadiusOverlayTests`.

## Traps
- **EditMode never calls `OnEnable`**, so self-registering registries (`ResourceNode.All`) are EMPTY
  there — reach tests live in PlayMode and probe a point 5 km off the map.
- `CitizenVisualsManager`'s empty-radius latch only moves while somebody is employed — don't mirror it.

## Open
Real output (~46–78 wood, 18–31 stone/day per 3 workers) vs the sheet's claimed 120; no in-world
radius feedback while a gatherer works (only placing/selected).
