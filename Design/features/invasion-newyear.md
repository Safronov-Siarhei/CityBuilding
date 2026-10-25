# Invasion: the New Year map «Новогодняя»

> **Status (2026-10-25):** built, in the game, chosen as the New Year THEME (the tree round button) in
> the menu's invasion screen (was a settings row, removed the same day). When it is open to players (holidays? always?) is NOT decided — the user: «пока что
> сделай её доступной в настройках, потом будем разбираться». Not seen on a phone.
> **Read with:** [invasion-mode](invasion-mode.md), [maps-and-terrain](maps-and-terrain.md) («Seasons»).

## The user's choices (2026-10-25)
- Mechanics picked from my list: **2** gifts + the Thief, **3** the blizzard, **5** the midnight
  chimes, **6** the Ice Shaman. **No ice** on the rivers (the ice crossing was rejected).
- My amendments, accepted: no blizzard on the midnight wave; the Shaman comes WITH midnight; the Thief
  is easy to see (gold «!» on the minimap, the gift on his back).

## The map
- `Tools/blender/invasion2.py` = invasion1.py with other noise seeds (woods' edges, meadows, beaches,
  flowers, decor scatter) and each jungle stone patch on the mirror bearing; same structure (island,
  river ring, 3 causeways/lanes/camps, jungles, shrines) — the run's code leans on it. Blend:
  `H:/…/Buildings3D/Inv-2.blend`; export `export_map.py -- G:/Fork/CityBuilding Inv2 Inv-2`.
- `SetupProjectCityScene`: `CreateMeshMapDefinition("Inv2", "Inv-2", null, MapSeason.Winter,
  conifers)` → `MeshMapDefinition.ForcedSeason` (SeasonStyle.MapForced) + fir forest only.
- `InvasionMode.RunMapId` (`GameSettings.InvasionTheme == MapTheme.NewYear`), `IsRunMap`, `IsNewYear`;
  `InvasionMode.MapId` stays "Inv1" (tests).

## The rules (`Invasion/InvasionNewYear.cs`, numbers: invasion tab `inv_ny_*`, `inv_gold_thief`, `inv_gold_ice_shaman`)
- Look: `NYTree` on 2x2 before the castle (grid cells taken, NavMesh carve, no snow on it), a
  `NYSnowman` by each house, `SnowFall` on the camera (3 layers of tetrahedra, Unlit; blizzard = all 3,
  faster, slanting) + `BlizzardOverlay` white veil.
- Gifts (`NewYearGift`): `inv_ny_gifts_per_wave` after each wave (`InvasionWaves.WaveEnded`) between
  min/max radius of the castle, ≤ `inv_ny_gifts_max`; taken by the King/soldier/islander within
  `inv_ny_gift_pickup_meters` → wood/stone/gold (`inv_ny_gift_*`).
- Thief (`OrcKind.Thief`, units row `orc_thief`, model OrcThief, `GiftThief`): from
  `inv_ny_thief_from_wave`, `inv_ny_thieves_per_wave` with each wave out of the portal nearest the
  gifts; `OrcUnit.Steered` + `SteerTo`; carries the gift (not parented — the corpse would take it);
  home = gift lost; killed = gift drops. Gold «!» `MinimapAttackAlerts.thiefMark`.
- Blizzard: chance `inv_ny_blizzard_chance` per non-midnight wave, announced `…_warning_sec` before,
  lasts `…_sec`: `Core/Weather` (tower range × `…_range_mult` in `DefensiveBuilding.RangeOf`, walking
  × `…_speed_mult` in Orc/Soldier/Citizen steps).
- Midnight every `inv_ny_chimes_every` waves: `InvasionWaves.WaveSizeMultiplier` × `inv_ny_chimes_orcs_mult`;
  the Ice Shaman (`OrcKind.IceShaman`, row `orc_ice_shaman`, `IceShamanPower`: every
  `…_freeze_every_sec` the nearest tower within `…_freeze_radius` → `DefensiveBuilding.Freeze`
  (`FrozenFor`, ice block)) joins via `InvasionWaves.AddToWave`; `inv_ny_salute_delay_sec` later
  `Salute()` = `Fireworks` + every run orc (not monsters, not the Shaman) loses `…_salute_damage_share`.
- Run-only unit rows: `BalanceImporter.LayOver` now APPENDS ids the sandbox lacks (was: error, skipped).
- Trap paid: `OrcUnit.IsMonster` was `kind >= Wolf` — the new kinds (9, 10) would have read as
  jungle beasts; now `Wolf..Giant`.

## Models
`Tools/blender/newyear.py`: NYTree, NYGift1-3, NYSnowman (Models/Buildings), OrcThief, OrcIceShaman
(Models/Orcs). Lineup: `playtest_shots/newyear-models.png`.

## Tests
PlayMode `NewYearTests` (6): winter fir map with tree and snow (+ photos newyear-base/-wide/-blizzard),
gift taken by the King, Thief carries and drops, midnight multiplier + salute spares the Shaman
(+ photo newyear-salute), Shaman freezes a tower (+ photo), blizzard shortens and slows then ends.

## Open
When the map is available; a dedicated New Year leaderboard; garlands/lights on buildings (not done);
the gifts' kinds are only wood/stone/gold (buffs were an idea).
