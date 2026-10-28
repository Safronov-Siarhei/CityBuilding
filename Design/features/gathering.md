# Gathering: trees, rocks, ore veins and the harvest walk

> **Status (2026-10-21):** shipped. Since 2026-10-21 nodes hold a STOCK and give one per visit; mines
> stand only on ORE VEINS. Two delivery bugs fixed 2026-10-20 (no sawmill on any map ever delivered).
> **Read with:** [maps-and-terrain](maps-and-terrain.md), [production-chains](production-chains.md),
> [building-placement](building-placement.md).

## What the player sees
Sawmill/Quarry workers walk to trees/rocks inside the building's harvest radius, chop under a
harvest bar (parchment medallion + gold pill), and carry the load home. Tapping a tree or boulder
shows a round badge with what is left, counting down while worked. It goes after 4 s, at the next finger down ANYWHERE (world or HUD; `TouchInputRouter.TouchedDown`), or under a dialog (ModalGate) -- the user 2026-10-28, it used to stay up over a building card. A selected citizen sent to a tree
or boulder just goes (no badge) — manual gathering breaks the no-wood bootstrap deadlock.
Ore veins are glowing patches of broken ground; a mine is built ON one.

## Rules (decided)
- **Stocks (user, 2026-10-21):** a tree holds `wood_per_tree` (5) and gives ONE per visit; the worker
  keeps it until it is gone, then it falls and regrows elsewhere. A boulder's stock follows its size
  (`stone_rock_size_1..10`: 12…50), one per visit; an emptied boulder never comes back.
- **Ore veins (user, 2026-10-21, variant 1):** 2×2 patches; a mine fits only exactly over a free vein
  of its ore; the vein hides under it and returns when it goes; veins NEVER run dry. Dealt by a RNG
  seeded with the map id (no save data), before forest and boulders: coal and copper near the start,
  iron further, gold far out towards the orcs; never in the forest, start zone or orc ground. Not in
  Invasion.
- Trees exist only inside the map's `TreesArea`; rocks in `RocksArea` clearings. "No wood gathered"
  is usually geography.
- A gatherer with nothing in reach says so on its card and workforce row for as long as it is true
  (`ProductionBuilding.NothingLeftInReach`; `ResourceNode.AnyHarvestableWithin` is deliberately softer
  than a worker's search).
- Gatherer upgrades add workers (Sawmill/Quarry 3/4/5) or reach, not batches.

## Code
`Maps/ResourceNode.cs` (registry `All`, stock), `Maps/TreesAreaSpawner.cs`, `Maps/RockSpawner.cs`
(`ClearCell`), `Maps/TreeGrowth.cs`, `Maps/OreDeposit.cs`, `Maps/OreDepositSpawner.cs`,
`Shaders/OreGlow.shader`, `Maps/HarvestProgressBar.cs` + `Maps/HarvestBarSkin.cs` +
`Shaders/HarvestBar.shader` (one shared material, per-bar property block), `UI/NodeYieldBubble.cs`
(one badge for the whole game), `Grid/HarvestRadiusOverlay.cs`, `Citizens/ManualGathering.cs`,
`Citizens/CitizenVisualsManager.cs` (node search, idle-gatherer sweep), `Citizens/CitizenAgent.cs`.
Blender: `Tools/blender/orevein1.py`, `stones1.py`, `trees1.py`.

## Data
economy `wood_per_tree`, `stone_rock_size_1..10`, `wood_per_harvest`, `stone_per_harvest`, `harvest_seconds`.

## Tests
PlayMode `GatheringTests`, `GatherRadiusReachTests`, `NodeYieldTests`, `OreDepositTests`,
`SawmillDeliveryTests`, `CalderaGatheringTests`, `ForestClickTargetingTests` (**flaky**: unseeded forest);
EditMode `ResourceNodeYieldTests`, `HarvestRadiusOverlayTests`.

## Traps (paid for)
- A felled tree dropped its claim when the woodcutter shouldered the wood and the 5 s idle-gatherer
  sweep turned him round — loaded walkers are now left alone and loads add up.
- Walking home aimed at the building's middle (inside its walls) → partial path → retried forever; a
  long step (sped-up game) overshot 5 cm route corners. Home now counts from anywhere against the walls,
  a tree from arm's reach, and no step passes the point walked to.
- A worker pinned against a building's corner a hand short of a route corner (a house's collider
  reaches past its NavMesh carve) retried the same leg for ever while the bar over his rock filled
  and emptied (Invasion, 2026-10-23). Now stuck within `CornerGiveUpMeters` of a corner that is not the
  last he walks on to the next; the bar is not drawn during a stuck breather. Second cause, same day:
  the way HOME aimed at the yard's middle, inside its carve -- `CalculatePath` failed and the worker
  walked a straight line into a house for good. `CitizenAgent.BuildRoute` now snaps the target onto
  the NavMesh (3 m) first, as NavRoute does. PlayMode `QuarryMen_GetToTheirRocks_PastTheHouses`
  (counts only trips after the first 50 s: the stuck man had made one or two before).
- EditMode never calls `OnEnable` — `ResourceNode.All` is empty there; reach tests live in PlayMode.

## Open
No in-world radius feedback while a gatherer works; gathering income never retuned against the sheet's
`per_day`.
