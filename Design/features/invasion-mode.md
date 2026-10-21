# Invasion mode («Нашествие»)

> **Status (2026-10-21):** in progress. Done: the Invasion map `Inv1` (base island halved to ~16 m),
> «В бой» starts a run, the King hero (model, jointed animation, XP/levels/respawn), the run's economy
> (6 houses × 3 people, 9 buildings, wood/stone prices, no food/tax/happiness), the King's portrait
> (top-right) + tap select + guard-the-base with no order, orc WAVES down the three lanes (countdown,
> breather, lanes open one by one, gold for kills and waves), the JUNGLE (wolf/bear/troll camps at the
> "monsters" spots, the giant boss, respawn stronger with the waves, healing shrines, the boss's
> resources + blessing), the ROUND ORC CAMPS (two rings of wall with open gates, towers, totems,
> spawners, storehouses, an unclosable portal, a refilled garrison; waves march out of the portal).
> Numbers NOT yet checked by playing. Next (plan in the log): score and leaderboards.
> **The authoritative record is `Design/Invasion.md`** (Russian): the user's answers in four rounds,
> how it works now, the plan and a changelog. Read it before touching the mode and append to its log.
> **Read with:** [army-and-soldiers](army-and-soldiers.md), [maps-and-terrain](maps-and-terrain.md),
> [orc-raids-and-portals](orc-raids-and-portals.md).

## In one paragraph
Endless defence of a base island against ever-stronger orc waves, played as the King-hero. Dota-like
map: the island behind a river ring, three earth causeways (not plank bridges) and lanes out to three
round orc camps, jungles with monster camps between them, a boss lair. Simplified economy: gold
(kills, waves), wood, stone, people. Score grows faster than the wave number (10→100, 20→400, 30→900);
daily/weekly/monthly ladders; an interrupted run ends and costs points. Lose = the castle falls.
No pause, no speed-up, no autosave, no ore veins, no sandbox raids/illness/fires in a run.

## Code
`Scripts/Invasion/InvasionMode.cs` (turns sandbox systems off, places castle, towers at the
causeways, the King), `InvasionRules.cs` (the 9-building menu, wood/stone prices, upgrades also take
gold, repairs, nothing waits on the Laboratory or decays), `InvasionPeople.cs` (houses respawn a person
every 15 s while people < places; a burnt house loses its places for good);
`InvasionWaves.cs` (the wave clock: first wave, breather, overtime; lanes out of the map's `CampSpots`; the ramp as pure statics `LanesAt`/`OrcsPerLane`/`OrcLevel`/`KindAt`/`IsBossWave`; gold for
every `OrcUnit.Died` in a run and per wave; spawns through `OrcRaidManager.SpawnOrcOfKind`, which works
with the raid manager disabled), run orcs target the castle (`OrcUnit.AcquireTarget`: a building only
within `inv_orc_building_radius`; `OrcUnit.Target` exposed for tests), the wave line under the resource
bar (`UI/SettlementStatusController` shows it instead of the day in a run);
`InvasionCampLayout.cs` (the round camp's plan, pure: rings, gates at angle 0, buildings by angle
and radius, `ToWorld`), `InvasionOrcCamp.cs` (builds a camp at a `CampSpots` point facing the castle
through `OrcRaidManager.SpawnLooseCampBuilding`/`SpawnRingGate`/`SpawnPortalLook`/`SpawnRingGuard`
(in `OrcRaidManagerCamps.cs`); garrison refill by surviving spawners; `WaveExit`; `Tick(dt)` public),
`InvasionWaves.Begin(exits)` takes the camps' exits;
`InvasionJungle.cs` (camps at `SpotsOf("monsters")` by spot level, the lair at `boss`, shrines at
`shrine`; monsters are `OrcUnit` kinds `Wolf/Bear/Troll/Giant` (`OrcUnit.IsMonster`) put on guard with
`SetGuardPost(null, post, radius)`, stats from `BalanceConfig.MonsterUnit` (economy `mon_<id>_*`);
respawn + level from `InvasionWaves.WavesSurvived`; boss reward + `KingHero.Empower`; `Tick(dt)` public
for tests), monster models wired into `OrcRaidManager` (`wolfModel`… by `SetupProjectOrcs`), the shrine
model into `InvasionMode.shrineModel`;
`Combat/KingHero.cs` (XP = health of any enemy falling within `king_xp_radius`, levels, respawn at the
castle, `Guarding`: attacks orcs within `king_guard_radius` of the castle until a player's order —
`ArmyGroup.PlayerOrdered`), `UI/KingPortraitController.cs` (portrait, tap-select, guard button; built
in `SetupProjectHud.BuildKingPortrait`, face `UI/Icons/PortraitKing.png` from `render_portrait.py`), `Combat/FigureAnimator.cs` (stand/run/strike without an Animator), `SoldierType.King` (own
group, outside the army cap); entry `MainMenuController.StartInvasion` →
`GameSessionIntent.NewGameIsInvasion`. Map spots by kind: `MeshMapDefinition.SpotsOf(kind)`
(`monsters`, `boss`, `shrine`, `bridge`); the orc camps are NOT among them — they are `CampSpots`.

## Data
economy `king_*`, `inv_*` (waves: `inv_first_wave_sec` … `inv_orc_building_radius`); localization
`#unit_king`, `#hud_inv_wave`, `#hud_inv_wave_in`, `#log_inv_wave`, `#log_inv_wave_survived`,
`#log_inv_boss_slain`, `#log_inv_boss_back`; economy `mon_*` and the jungle's `inv_*` (camps, respawn,
guard radii, monster gold, shrine, boss reward and blessing), `inv_camp_*` of the round orc camps.
Blender `wild.py` → `Models/Wild`; `orcringcamp1.py` → `Models/Orcs` (OrcRingWall, -Ruin, OrcRingGate). Blender: `Tools/blender/invasion1.py`,
`render_invasion1.py`, `heroking.py` (jointed; export routes `Hero*` to `Models/Heroes`).

## Tests
PlayMode `InvasionTests` (waves: countdown + one lane + HUD line, every lane reaches the castle,
run orcs go for the castle, a wave beaten off pays and starts the breather); EditMode
`InvasionWavesTests` (the ramp), `InvasionJungleTests` (camp kinds, growth, the bite); PlayMode jungle:
a camp per spot with its beasts, monsters hold their ground, a wiped camp returns stronger, the
shrine heals, the boss pays and blesses; round camps: buildings + garrison + no portal, walls block
and the waves get out, a fallen spawner sends no more of its kind; EditMode `InvasionCampLayoutTests`.

## Traps
- The round camps' walls carve the NavMesh a frame or more after the run starts: a path test that
  runs at once sees no walls and passes for nothing (wait first, as `TheWalls_Block…` does).
- A ring wall section is 401 triangles and there are 108 of them on the map -- mind the budget
  before making it richer.
- Jungle monsters are `OrcUnit`s, so they are in `OrcUnit.All` and fire `OrcUnit.Died`: a test or
  system meaning "the wave's orcs" must use `InvasionWaves.WaveOrcs`, not `OrcUnit.All`.
- `FigureAnimator` skips a monster figure that no camera sees (`Renderer.isVisible`), but never the
  King and never without a graphics device (batchmode tests would see nothing move).
- The wave clock runs in every PlayMode test of a run: a test that lets more than
  `inv_first_wave_sec` of game time pass gets a wave's orcs in its scene.
- A PNG for the HUD must sit in `UI/Icons` (UiTextureImport makes it a single sprite); elsewhere it
  imports as a multi-sprite sheet and `LoadAssetAtPath<Sprite>` returns null.
- `PlaytestCapture.ShootWithUi(At)` does not show the HUD's corner widgets correctly (the menu button
  and the King's portrait come out cut or missing) -- check corners on a device.
