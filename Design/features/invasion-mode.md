# Invasion mode («Нашествие»)

> **Status (2026-10-22):** in progress. Done: the Invasion map `Inv1` (base island halved to ~16 m),
> «В бой» starts a run, the King hero (model, jointed animation, XP/levels/respawn), the run's economy
> (6 houses × 3 people, 9 buildings, wood/stone prices, no food/tax/happiness), the King's portrait
> (top-right) + tap select + guard-the-base with no order, orc WAVES down the three lanes (countdown,
> breather, lanes open one by one, gold for kills and waves), the JUNGLE (wolf/bear/troll camps at the
> "monsters" spots, the giant boss, respawn stronger with the waves, healing shrines, the boss's
> resources + blessing), the ROUND ORC CAMPS (two rings of wall with open gates, towers, totems,
> spawners, storehouses, an unclosable portal, a refilled garrison; waves march out of the portal),
> the SCORE + LOCAL records (waves² + giant + camp buildings; day = best 5 runs, week/month = sums
> of days; written only when the castle falls; end panel + the menu hub show them).
> The user's first playtest (2026-10-22) fixed: tougher wave orcs (×inv_orc_health_mult) with health
> bars, flag order marker, more island trees, a 40 m minimap following the view with blinking «!» at
> the attack, «+N» over yards, one sawmill + one quarry, the King reveals fog and guards whenever he
> is not selected (no guard button). Playtest 2: fog round the King closes behind him, every orc /
> beast figure turned 180° and orcs face where they walk, smooth sliding minimap, King's return
> (camera + light + log), orc body animation (OrcBodyAnimator) and fall-and-sink death (OrcCorpse),
> no Laboratory gate on picking a run building, mouse places the offered fence piece, plain gates
> don't shoot (sheet), run walls sturdier (×4, now written straight into the invasion tab) and only broken when in the way, orcs kill
> citizens, harder waves. Playtest 3: TIMED BUILD + UPGRADE (ConstructionSite; nothing works meanwhile;
> hammer labels), limited cards greyed «1/1», flat harvest distance + gatherable-node rings, «Где» selects,
> heart/shield health bars, coin popups, more island wood / less stone; building selection (BuildingFocus)
> is a shared feature now. Playtest 4: «!» hides under the card, orc chases (soldier/citizen/prisoner)
> keep to the NavMesh (`OrcUnit.Chase`: straight while NavMesh.Raycast is clear, else a route), health
> bar layers in a fixed render queue, Inv1 stone where the user drew it, wave orcs ×0.7 damage
> (`inv_orc_damage_mult`) and health 4.2, castle reach ×`castle_attack_range_mult`.
> Playtest 5: yard workers drop their node claim while the yard rests (they stood idle after an
> upgrade), killed workers are replaced (`RefillShortBuildings`), one quarry again (since 2026-10-23: two of each, `max_count`),
> run boulders ×`inv_stone_reserve_mult` (`ResourceNode.ScaleStock` in BeginRun), `inv_per_harvest` (both yards, 2).
> Heroes (2026-10-23): the King, let go of after an order, stands `hero_return_wait_sec` then guards
> (`KingHero.ReturnIn`); his 4 SKILLS (`Combat/HeroSkills.cs`, rows `skill_<hero>_<skill>_*` read by
> `BalanceConfig.HeroSkills`; `SoldierUnit.Shield/Enrage`, `HeroBlessing` checked in BuildingInstance.TryDamage,
> SoldierUnit.TakeDamage, OrcUnit.HuntCitizen) and the bottom-centre `UI/HeroSkillBar.cs` + `UI/HoldTooltip.cs`
> (built by `SetupProjectHud.BuildHeroSkillBar`; icons `UI/Glyphs/Skill_<skill id>.png`, drawn by
> `Tools/ui/make_skill_icons.py`). QUEEN + CAPTAIN: `Combat/Hero.cs` (was KingHero; any hero, `HeroBalance`
> rows `<id>_*`, Queen's flee via `SoldierUnit.Retreat`), `SoldierType.Queen/Captain` (`SoldierStats.IsHero`),
> `ArmyManager.SpawnHero/SpawnSummons/Dismiss` + `SummonedLifetime`, `HeroAuras` (Сила), `CitizenMilitia`
> (Боевой дух), `BuildingInstance.Shield` (Спасение), `HeroSkills.TryCheatDeath` (Бессмертие);
> `InvasionMode.Heroes/HeroOf/CanBuy/BuyHero`; `UI/CastleHeroCards.cs` (built by
> `SetupProjectPanels.BuildCastleHeroCards`), `UI/HeroPortraitController.cs` (was KingPortraitController,
> one per hero, stacked). Models `Tools/blender/heroes2.py` -> Models/Heroes; portraits
> `render_portrait.py ... [size] [head_z]`. Playtest 6 (2026-10-23): the WHOLE MODE'S BALANCE on the
> sheet's `invasion` tab (see «Balance» below), 2 sawmills + 2 quarries (`max_count`), Barracks soldiers
> hired for gold with no Laboratory and guarding the base on their own (`Invasion/InvasionGarrison.cs`,
> `inv_soldier_guard_radius`), soldier health bars, heroes and soldiers fall as figures (`OrcCorpse.LeaveCopy`),
> towers ×1.5 range (run `defence_attack_range_meters` 9), orcs never path through a gate when chasing
> and break the blocker (`OrcUnit.Chase`/`BreakThrough`, `NavRoute.AreaMask`), gates turn into a fence
> line (`BuildingPlacer.GateTurnInAFence`), fence level 2+ is stone, no army panel in a run, the quarry
> man no longer sticks on a house corner. Next: online leaderboard (service not chosen), more playtests.
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

## Balance (the sheet's `invasion` tab, 2026-10-23)
Three stacked tables, each opened by a header row: `building` (buildings-tab columns + `max_count`),
`unit` (units-tab columns), `key` (key/value/comment). A building/unit row REPLACES, for a run, the
cells of the columns its table has (empty = empty: a level repeats the one below, a price is 0); a price
prefix the table has (`cost_`, `upN_`, `recruit_`) makes the table the whole price. Keys replace
economy's for the run or add run-only ones; ALL run-only keys (`inv_*`, `king_*`, `queen_*`, `captain_*`,
`hero_*`, `mon_*`, `skill_*`) live here, not in economy -- the sandbox config gets them too (so EditMode
tests read them), the overrides of sandbox keys stay the run's.
`Editor/BalanceImporter.InvasionTab` → `Resources/BalanceConfigInvasion.asset`; `BalanceConfig.Instance`
is it while `GameSessionIntent.NewGameIsInvasion || InvasionMode.IsActive` (`BalanceConfig.Run`/`.Sandbox`
for both). `InvasionMode.Awake` swaps the placer's catalogue for COPIES (`ForRun`: Instantiate +
`BuildingData.ApplyBalance(run row)`), so building numbers need no SetupProject run. Sheet gid 1941808734;
`BalanceSheetSettings.invasionCsvUrl` (published as CSV 2026-10-23, same publication id as the other tabs; Pull checked: 11 buildings, 9 units, 228 keys).

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
`InvasionScore.cs` (the run's points, listens to `OrcUnit.Died` for the Giant and
`OrcCampBuilding.OnDestroyed` except Palisade/Gate; `Finish()` idempotent, called on
`GameOverManager.OnGameEnded` and by the end panel), `InvasionRecords.cs` (device file
`invasion_records.json` in persistentDataPath, each day's best runs, pruned after 62 days; pure statics
`DayPoints`/`WeekPoints`/`MonthPoints`; tests set `FilePathOverride`), `UI/GameOverController.ShowRunEnd`
(«Замок пал» + waves/points/today), `UI/InvasionHubController.cs` (menu hub numbers + day/week/month tabs,
built in `SetupProjectMainMenu.BuildInvasionPanel`);
`UI/MinimapAttackAlerts.cs` (the blinking «!», from `InvasionWaves.TryLaneFront`),
`UI/ResourceGainPopups.cs` («+N», on `CitizenVisualsManager.Delivered`; every mode),
`UI/MoveOrderMarker.ShowFlagFor` (army orders plant the citizens' flag), `OrcUnit.Toughen`/health bar
(`BuildingHealthBar.CreateFor(unit, height, width)`), `InvasionRules.AtLimit`/`LimitOf` (`BuildingData.maxCount` from the invasion tab: two sawmills, two quarries; the card greys with «2/2»),
`Buildings/ConstructionSite.cs` + `UI/ConstructionLabels.cs` (timed build/upgrade, `InvasionRules.BuildSeconds`);
`Combat/KingHero.cs` (XP = health of any enemy falling within `king_xp_radius`, levels, respawn at the
castle, `Guarding`: attacks orcs within `king_guard_radius` of the castle until a player's order —
`ArmyGroup.PlayerOrdered`), `UI/KingPortraitController.cs` (portrait, tap-select; built
in `SetupProjectHud.BuildKingPortrait`, face `UI/Icons/PortraitKing.png` from `render_portrait.py`), `Combat/FigureAnimator.cs` (stand/run/strike without an Animator), `SoldierType.King` (own
group, outside the army cap); entry `MainMenuController.StartInvasion` →
`GameSessionIntent.NewGameIsInvasion`. Map spots by kind: `MeshMapDefinition.SpotsOf(kind)`
(`monsters`, `boss`, `shrine`, `bridge`); the orc camps are NOT among them — they are `CampSpots`.

## Data
economy `inv_score_wave_sq`, `inv_score_boss`, `inv_score_camp_building`, `inv_runs_per_day`;
localization `#invasion_you`, `#over_inv_*`; economy `king_*`, `inv_*` (waves: `inv_first_wave_sec` … `inv_orc_building_radius`); localization
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
Playtest fixes: `TheKing_LetGo_GoesBackToGuardTheBase`, `WaveOrcs_AreTougher_AndShowTheirHealth`,
`ARun_HasTwoSawmillsAndTwoQuarries`, `QuarryMen_GetToTheirRocks_PastTheHouses`, `TheRun_PlaysOnTheInvasionTab`, `BarracksSoldiers_AreForGold_AndGuardTheBaseOnTheirOwn`, `AFallenHero_GoesDown_AsAFigure`, `AnUpgradedFence_IsStone`, `AnOrc_DoesNotWalkInAtAGate_ItBreaksIn`; EditMode `GateTurnTests`. Score: PlayMode `TheCastleFalling_EndsTheRun_AndWritesItsPoints`; EditMode `InvasionRecordsTests`
(waves², best 5 a day, week from Monday, month).

## Traps
- A run's BuildingData are runtime COPIES (`InvasionMode.ForRun`), not the assets: compare by
  `buildingName`, never by reference to an asset. A component attached at generation (DefensiveBuilding
  only where a sandbox level has `defense > 0`, ProductionBuilding only with workers) is not added by a
  run row -- giving defence in the invasion tab to a building that has none in the sandbox does nothing.
- `BalanceConfig.Instance` is the run's config from the menu's «В бой» on: a PlayMode test that sets
  `NewGameIsInvasion` and never loads the scene leaves every later Instance read on the run's numbers.
- Photos with UI (`PlaytestCapture.ShootWithUi*`) turn the HUD canvas camera-space: the parchment fog draws
  OVER the HUD in them (top-right King portrait hidden). Not so in the game (overlay canvas).
- A run building placed through `BuildingPlacer.PlaceAt` gets a `ConstructionSite`; one put down by
  `InvasionMode.PlaceFinished` or `PlaytestWorld.Place` does not. `IsOperational` gates DefensiveBuilding,
  ProductionBuilding.Update, the yard's node claims (CitizenVisualsManager) and the Barracks' recruiting.
  An upgrade in a run pays at once and raises the level only at `ConstructionSite.Finish`.
- Every unit model (King, orcs, beasts) is modelled facing Blender -y and must be turned 180° about Y
  when put under its root, or it walks backwards.
- In a run `OrcUnit.AcquireTarget` skips `InvasionRules.IsBarrier`; walls are only hit through
  `AttackWhateverIsBlockingTheWay` / `BreakThrough`. A test expecting an orc to target a fence will fail by design.
- An orc fights a soldier or a hero before any building: a test of orcs vs buildings or walls must
  put the King away (`King.gameObject.SetActive(false)`), or he comes out and the orc fights him
  (checked 2026-10-23: with him away, orcs hit every run building within inv_orc_building_radius).
- A killed citizen goes through `CitizenVisualsManager.KillAgent` (that agent + one population), not
  `KillCitizens`, which would remove some other agent.
- The King obeys orders ONLY while selected (user, 2026-10-22): `KingHero.OnSelectionChanged` calls
  `Guard()` on deselect, which walks him home. A test that orders him must select him first.
- Wave orcs are `Toughen`ed AFTER `SpawnOrcOfKind`; anything comparing an orc's health to the
  units sheet must multiply by `inv_orc_health_mult`.
- The minimap in a run follows the camera rig (`MinimapCameraUpdater`, `inv_minimap_radius`);
  `MinimapCameraIndicator.ToMap` places things relative to the minimap camera, in both modes.
- A run that is left or crashes is never written down (the user's rule): there is no warning on
  leaving yet -- an open item.
- PlayMode tests of a run must keep `InvasionRecords.FilePathOverride` set (InvasionTests does), or a
  test castle falling writes into the editor's real records.
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
