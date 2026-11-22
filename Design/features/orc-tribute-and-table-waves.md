# Orc raids by the table + the chief's tribute (sandbox archipelago)

> **Status (2026-11-22):** BUILT. setup×2 run. The user has not seen it, and it has no tests.
> Sandbox orc raids by sea now come from the sheet tab **«Орки: волны»** (`Balance/orcwaves.csv`),
> with the tribute scroll (concept C round 2, `Design/concepts/tribute/Tribute_C1..C3`), siege
> machines, and help after a heavy wave.
> **Open:**
> - the tower's climb is a jump over the wall; there is no climbing animation yet;
> - the first table numbers are guesses;
> - not tried on the phone.
> **Read with:** [orc-raids-and-portals](orc-raids-and-portals.md),
> [archipelago-and-ships](archipelago-and-ships.md), [moat](moat.md), [monetization](monetization.md) (the booster badge).

## Rules (the user, 2026-11-22)

**Where it applies**
- Only the sandbox orc raids on `NoRaids` maps (the archipelago), and only while the tab has rows (`OrcRaidManager.TableWaves`).
- Not in Invasion, the tutorial or the campaign. Their guards in `OrcRaidManager.Update` run before it.

**Timing**
- **Wave 1** sails `delay_sec` (420 = 7 min) after the Town Hall is placed. It never comes inside the world's quiet start (`FirstWaveSeconds`).
- **Each later wave** sails `delay_sec` (600) after the death of the last orc of the previous wave, or after the tribute was paid.
- The delay is multiplied by `WorldSettings.RaidIntervalFactor`. Counts are multiplied by `RaidSizeFactor`; any non-zero count stays at least 1.
- **Shipyard down:** the clock waits and no scroll comes up.

**The table**
- The wave number counts every raid in this game and is saved. It grows even when the tribute is paid.
- After the table's last row, the last row repeats. The game reads however many rows the tab has: the user may add waves and edit them.
- A row's columns:
  - the orcs: `raiders`, `throwers`, `brutes`, `chiefs`, and the machines `rams`, `towers`;
  - `level`;
  - the tribute: `res1..5` / `amount1..5`. These are ResourceType names (Coins, Wood, Stone, Food, IronBar…); crystals and Population are not allowed. The amounts are fixed.

**The scroll**
- It comes up `tribute_seconds` (150) before the fleet sails. The game is not paused.
- **Заплатить** takes all of the tribute from the stores or none of it. It stays grey while any resource is short. Paying cancels the wave, counts it, and posts `#log_tribute_paid`.
- **Биться!** closes the scroll and the fleet sails at once.
- **Отложить** (the cross does the same) rolls the scroll into a badge under the resource bar's middle:
  - same size as the booster badge;
  - a gold ring and a timer show the time left;
  - a tap reopens the scroll;
  - at zero the fleet sails.
- **With a booster badge up**, the pair is centred: the scroll on the left, the booster on the right (`BoosterHints.BadgeHome` / `TributeScroll.BadgeHome`).
- There is one chief for all waves: `#tribute_chief`, the name in the genitive in Russian.

**Siege machines**
- A machine is a brute with the campaign's models (`OrcRam.fbx` / `OrcSiegeTower.fbx`). Its stats are set by `raid_ram_*`, `raid_tower_*`, `raid_siege_speed`. It takes `raid_siege_seats` places in a transport.
- **NEVER through the moat** (the user): a target whose NavMesh way crosses moat water (`Moat.Wading`) is skipped. With no target left, the machine waits where it stands (`SiegeErrand`).
- **Ram:** goes for the nearest reachable gate or wall (`WallWalk.Concerns`). Without one it goes for any building.
- **Tower:** rolls up to a reachable wall and docks there. For `raid_tower_stay_sec` it then lifts one wave orc standing near it over the wall every `raid_tower_climb_gap_sec`. When the time is up it is removed. With no wall in reach it rams like a ram.

**Help for the player**
- **Heavy wave (the user's rule):**
  - the island is the one the wave landed on;
  - the buildings are counted at the first landing: roads, moats, fences and decor do not count;
  - if `raid_heavy_share` (0.4) of them are destroyed by the end of the wave, the next wave gets `raid_heavy_mult` (0.5) health and attack;
  - it also comes `raid_heavy_rest_sec` (300) later (option 1);
  - the help lasts one wave.
- **Town Hall below `raid_hall_retreat_share` (0.25)** (option 3): the wave walks back to its beach and is gone there, with a 40 s cap. Notice `#log_orcs_retreat`.
- **Stuck wave:** if for `raid_stuck_sec` (180) no orc of the wave struck, was hurt or moved more than 0.5 m (checked every 5 s), the wave is over and its orcs are removed.

## Code

**`Combat/OrcRaidManagerWaves.cs`**: the phases (Waiting / Out), the queue of kinds aboard, landing, tracking, help, retreat, save. It also holds `SiegeErrand`.
- `OrcRaidManager.Update` calls `TickTableWaves`.
- `SpawnLanding(..., raid)` hands the table wave's transports to `LandTableWave`. Torchbearers come with the first boat and are tracked.

**`Ships/ShipManagerOrcs.cs`**:
- `LaunchOrcLoads(loads, level)`: one transport per load, plus the escorts; returns the raid number. `LaunchOrcFleet` now uses it.
- `OrcRaidOutbound(raid)`.

**`UI/TributeScroll.cs`**: the window and the badge. It opens on the rising edge of `TributeDue`; if another window is already open, it goes straight to the badge. It sets `ModalGate` while open.

**Editor**:
- `Editor/SetupProjectTribute.cs` builds it, called after `BuildBoosterHints`.
- The pictures are in `UI/Tribute` (`UiTextureImport.TributeFolder`), made by `Tools/ui/tribute_art.py` from `Tools/ui/tribute_concepts.py`.
- `SetupProjectOrcs` wires `siegeRamModel` / `siegeTowerModel`.

**Import:** `BalanceImporter.ReadOrcWaves` → `BalanceConfig.OrcWaveRow` / `OrcWave(n)`. Economy keys are on «Орки и набеги».

**Save:** `GameSaveData.wave*` fields, plus `OrcEntry.wave` / `siege`. Wave orcs get their machine dress and their help back on load.

**Cheat:** «Дань сейчас» and the wave line on the Orcs tab.

## Traps
- `TrySpawnPortalsNearTownHall` returns early on the archipelago, before the old `_raidTimer` line. The table's first wait is set right after `_portalsPlaced = true`.
- A ship sunk with orcs aboard: its part of the queue is dropped once no transport of the raid is outbound.
- Ships land in any order and each takes the first `cargo` kinds off the queue. Seat packing only decides how many ships sail.
