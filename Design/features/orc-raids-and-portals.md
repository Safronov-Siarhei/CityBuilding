# Orc raids, portals, victory and defeat

> **Status (2026-10-21):** raids scale with the player's progression; portals open at map spots
> (Maps 4–5) or round the Town Hall; destroying the MAIN portal = instant win + optional free mode.
> **Open, the user's call:** what closing a portal BUYS (today: a quieter direction + plunder + a
> breather); the raid roster per portal level («позже»).
> **Read with:** [orc-camps-and-main-base](orc-camps-and-main-base.md), [combat-core](combat-core.md),
> [maps-and-terrain](maps-and-terrain.md).

## Design (the user's, 2026-08-12 onward)
- Goal of a map: destroy the orc portals. Warcraft-style: each portal sits in a base to clear first.
- Defeat: Town Hall destroyed OR population 0.
- Raids grow with **progression**, not the calendar: `PlayerProgression.Score()` = building levels +
  population + soldiers × level + defence stat + LIFETIME produced (can't be gamed by spending down
  or idling workers). Raids grow in three directions: more raiders, stronger (`raid_max_orc_level`),
  and more often (interval falls to `raid_min_interval_seconds`).
- **One raid clock**, a wave from a random standing secondary portal (five clocks would land five
  squads on day one). The main base sends NO raids.
- Wave composition `OrcRaidManager.KindInGroup`: from `raid_thrower_from_level` every
  `raid_thrower_every`-th orc throws; from `raid_chief_from_level` the first orc of each wave is a chief.
  Raiders are drawn as the brute from raid level 4.
- Portal states: ACTIVE (next wave's source, picked at random, visual only, not saved) burns bright;
  passive smoulders; closed leaves a ruin (`closedPortalCells`). Portal plot 2×2 facing the Town Hall.
- Closing a portal → `PlunderCamp` (coins/wood/stone × level) + `orc_camp_breather_seconds` on the raid timer.
- Victory: main portal closed (`GameOverManager.WonByMainPortal`) → panel with «Продолжить игру»
  (`ContinuePlaying`, `IsFreeMode`). Maps without a main base: last portal wins.

## Code
`OrcRaidManager` is a partial class in four files (split 2026-10-21; fields stay in the main one):
`Combat/OrcRaidManager.cs` (fields, lifecycle, the raid clock, waves, kinds `KindInGroup`, raid
size/level/interval formulas, `SpawnOrc`), `OrcRaidManagerPlacement.cs` (portal search round the Town
Hall, `OpenAtMapSpots`, `TryFindMainBaseSpot`, ground tests, `SpawnPortal`), `OrcRaidManagerCamps.cs`
(models by kind/level, camps build/grow, guards, `PlunderCamp`, ruins), `OrcRaidManagerSave.cs`
(restore states, `RestoreFromSave`, `RestoreCamps`); models wired by `SetupProjectOrcs.WireOrcModels`; `Combat/OrcUnit.cs` (`OrcKind`
Raider/Thrower/Chief/Brute/Boss, `HuntPrisoner`, guards), `Combat/OrcPortal.cs` (`IsShielded`, `IsMain`),
`Combat/PortalPlacement.cs` (spiral of (distance, bearing) slots; bearing is the outer loop),
`Combat/PlayerProgression.cs`, `Core/GameOverManager.cs`, `UI/GameOverController.cs`,
`Cheats/OrcSpawnCheat.cs`.

## Data
economy `raid_*`, `portal_*`, `progress_per_*`; units rows `orc`, `orc_thrower`, `orc_chief`, `orc_brute`,
`orc_chief_boss`. Trap: the sheet's `ttk_*` formulas count WHOLE hits (ceil(hp/dmg)×interval).

## Tests
EditMode `OrcRaidManagerTests`, `PortalPlacementTests`, `PlayerProgressionTests`; PlayMode
`PortalOpeningTests` (Map 3), `ProgressionScoreTests`, `OrcModelTests`.

## Traps
- `PlaytestWorld.FindFreeArea` scans from cell (1,1) = the map CORNER; tests about surroundings use
  `FindFreeAreaNearTheMiddle`.
- Since 2026-10-20 (easy start) no raid comes while the town settles in (`settling_in_seconds`).
  Opening hints for a new player are still not built.
