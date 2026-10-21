# Citizens and workforce

> **Status (2026-10-21):** shipped. Since 2026-10-20 citizens have NAMES and the workforce panel has a
> second tab: one row per citizen.
> **Read with:** [happiness-and-migration](happiness-and-migration.md), [production-chains](production-chains.md),
> [performance-mobile](performance-mobile.md) (walking citizens ARE the CPU budget).

## What the player sees
Little people walking between home, work and resources. A citizens button (left, under the menu)
opens the workforce panel: tab 1 = one row per production building, +/- to assign idle people;
tab 2 = one row per citizen (name, clothes colour, what he is doing: free, sick, going to work,
gathering N %, carrying a load home, at work, on an order) with a Find button that flies the camera to him. Tapping a
citizen selects it; a tap on the world orders it to walk/gather.

## Rules (decided)
- Jobs are still a headcount: `CitizenManager` counts (`_totalPopulation`, `_assignedPopulation`,
  `SickPopulation`); each `ProductionBuilding` keeps an `AssignedWorkers` int. The walking figures are
  visuals that now carry NAMES («хочу чтобы они стали более одушевлёнными», 2026-10-20) — handed out in
  spawn order (`CitizenNames`, coprime stride), so a reload names them the same without saving a word.
- `IdlePopulation` counts the HEALTHY. Illness lays workers off (`LayOffWorkersBeyondWorkforce`);
  only the LAST worker lost logs (`#log_workplace_emptied`). Recovered people are not put back.
- A destroyed building hands its workers back (`ProductionBuilding.OnDestroy`).
- Recruiting takes an idle citizen; disbanding returns one; a soldier killed is a permanent loss.
- Settlement tiers (Хутор…) still computed by `SettlementTierManager` but **hidden from the player**
  (user, 2026-10-14). A campaign with tiers as milestones was floated, not decided.
- Units drawn at `UnitSize.Scale` = 0.625 (a citizen is 0.45 m, fits 0.5 m doors).

## Code
`Citizens/CitizenManager.cs`, `Citizens/CitizenAgent.cs` (modes Wandering/Working/ManualMove/Gathering,
own copy of route logic, `Route/RouteVersion`), `Citizens/CitizenVisualsManager.cs` (pool, who walks
where), `Citizens/SettlementTierManager.cs`, `Citizens/CitizenNames.cs`, `UI/WorkforcePanelController.cs` (both tabs), `UI/CitizenSelector.cs`
(`MarkerHeight` 0.75), `UI/SelectionBadgeController.cs`, `Core/UnitSize.cs`.
SetupProject: `BuildWorkforcePanel`, `BuildCitizenSelector`.

## Tests
EditMode `CitizenManagerTests`, `SettlementTierManagerTests`, `CitizenNamesTests`; PlayMode
`WorkforcePanelTests`, `CitizensTabTests`, `WalkCostTests`.

## Traps
- `Destroy()` is deferred to end of frame — counting `childCount` right after repopulating counts
  old rows; the panel exposes `RowCount`. +/- refreshes in place, the list rebuilds only when a row's
  building is gone.
- `CharacterController` costs ~8 µs per Move; dropping it was offered and the user took only the safe
  half (terrain out of the collision matrix). Roads don't carve the NavMesh; citizens sink ~2 cm.
