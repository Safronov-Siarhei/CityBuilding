# Tutorial (обучение)

> Status 2026-11-05: SCENARIO AGREED, map APPROVED and exported (Isl-tutorial1); game mode STAGES 1-3 BUILT (menu button,
> the map, systems off; rules; step script + King's hints + camera focus), not seen, no tests; stages 4-5 not started; with the user, NOTHING BUILT. Option «Г» of five proposed
> (a separate tutorial scenario on its own two islands). Next: the islands in Blender, then the mode.
> Read with: archipelago-and-ships (islands, ships, shipyard), sandbox-run-systems (Town Hall start,
> houses make people), army-and-soldiers / army-control (hiring; the Garrison is OFF here),
> orc-raids-and-portals (portal, orc landing), hud-and-ui (main-menu mode cards).

## What the player sees
A short guided scenario (aim: 10-15 min) on a tiny map of TWO small islands side by side: the
player's and the orcs'. No fog -- the whole map is visible from the start. Hints come from the
KING (his portrait + a line). It teaches the core loop: build -> gather -> hire -> defend -> ship ->
attack -> destroy the portal -> victory.

## Rules the user decided (2026-11-05)
- **Enemy: orcs** (a portal, a level-1 orc tower, one orc fighter on their island; a landing ship
  brings the first wave of 2 orcs).
- **Hints from the King.**
- **Main menu:** the tutorial card is ALWAYS shown and can be replayed (for testing now; later
  probably first-launch + replay).
- **Build menu: ONE category** with only Ратуша, Лачуга, Казарма, Верфь. Each shows «1/1» once built
  and cannot be built again.
- **Town Hall, Barracks, Shipyard show nothing extra** -- only what the tutorial needs.
- **Buildings take ~5 s.** **Resources are infinite** -- the gathering is shown, never waited for.
- **Town Hall -> the King + 3 people at once** (2 for work, 1 for the soldier). The Лачуга then
  starts filling its birth bar (the mechanic is shown, nobody waits on it).
- **No squads / Garrison:** the hired soldier is selected by hand like the hero.
- **Barracks: only a level-3 soldier** is offered.
- **Reward: +25 crystals.** The main-menu tutorial card shows a «кристаллы +25» badge.
- **Damage:** orcs and the orc tower deal minimal damage -- visible, never critical. The King never
  dies: at low health he heals himself. The soldier CAN die (shows the player that units die).
- **Shipyard:** the player builds it and hires the ship himself.
- **Camera focus steps** (gathering, boarding): the camera centres on the unit; the player follows
  it until he taps it; once picked, the camera centres on the target.

## The script
1. Start: two islands, no fog. King: «Поставьте Ратушу».
2. Town Hall placed -> the King and 3 people appear. King: «Постройте Лачугу».
3. Лачуга (5 s) -> its birth bar starts filling. Hint: houses make people.
4. «Отправьте жителя за деревом»: camera on a person (centre) until tapped -> camera on a tree ->
   tap it -> he walks, chops, carries.
5. Same for stone with the second person.
6. «Постройте Казарму» (5 s).
7. «Наймите солдата»: only the level-3 soldier; the third person goes.
8. An orc landing ship brings 2 orcs. Just a notice: «Отбейте набег» (no selection asked for).
   They hit softly; the King and the soldier kill them.
9. «Постройте Верфь» (5 s), «Наймите корабль».
10. «Посадите на корабль Короля и солдата»: camera focus as in step 4, on the King, then the
    soldier, each onto the ship.
11. «Отправьте корабль к острову орков» (the buoy).
12. Landing: portal, level-1 tower, 1 orc. «Уничтожьте защитников», then the tower. If the soldier
    falls: «Солдаты гибнут -- нанимайте новых». The King cannot die.
13. «Уничтожьте портал» -> victory screen -> on to the sandbox.

## The map (scheme for approval, 2026-11-05)
`Tools/blender/tutorial_plan.py` -> `tutorial-plan.json` (shapes + spots, for the Blender build) and
`tutorial-plan.png`. 64 x 44 cells: TURNED 180° at the user's word: the player's island EAST (~26 x 26, Town Hall zone 9x9 in the
middle, forest east, stones north-east, the Shipyard on the west shore facing the strait, the orc
landing beach north-west), the orcs' island WEST (~16 x 16: landing beach east, tower, guard,
portal). Strait ~10 cells. APPROVED by the user 2026-11-05 (after turning it 180°).

**Blender (2026-11-05, previews sent, not reviewed):** `islands.py` kind `tutorial` (`-- <Islands/Isl-tutorial1.blend> tutorial 1 [preview]`):
both islands in one 76x76 frame drawn from `tutorial-plan.json` (`SHAPE 'plan'`: coast = the plan's blob distance),
flat, no rim, beach slope 1.2 (not 2.6: the islands are small), landing beaches at the plan's orc_landing /
player_landing / shipyard / orc_barge, forest + stones from the plan's points (33 forest, 18 stony cells), flat pads
round the Town Hall zone (r 4.5) and the portal/tower/guard; `-spots.csv` carries every plan spot. ~504 land cells.
**More sea (the user, 2026-11-05):** the plan got a 10-cell margin (map 84 x 64, frame 90), 10 small
grassy islets (`islets`) and 12 bare sea rocks (`sea_rocks`: stone spires up to ~2.5 m, all stony
ground) on the open water, clear of the ship routes. The sea is SOLID here (deep water drawn as `water_deep`;
library islands leave it to the game's Sea layer) -- the user saw holes otherwise. The sea is ROUND (the user): a disc r = HALF-0.5 = 44.5
(`SEA_R`), the frame's corners empty; every islet and rock lies inside it. Now ~508 buildable cells, 89% water.
**Exported 2026-11-05** (`export_map.py -- D:/Fork/CityBuilding Islands Isl-tutorial1`) to
`Assets/_Project/Models/Islands/Isl-tutorial1-*` (Ground, Relief, Water, areas, DecorKit, Cells.bytes, Spots.csv,
Decor.csv). TRAP: `SetupProjectMaps.CreateArchipelagoLibrary` reads every `Isl-*` there; 'tutorial' is no
`ArchipelagoGenerator.Kind`, so it is skipped (one warning) and never enters a random archipelago -- the tutorial
mode must load it itself. .meta files come with the next Unity setup. Next: the game's tutorial mode.

## To build (plan)
- Two islands drawn in Blender, with spots: Town Hall area, trees, rocks, shipyard shore, orc
  landing, portal, tower, orc guard.
- A tutorial mode with its own rules (no fog, no Garrison, 5-s builds, infinite resources, King
  immortal, one build category with 1/1 caps, trimmed building cards).
- Step script: completion condition, camera focus / highlight, the King's line.
- Numbers and texts in the sheet (a `tutorial` tab; localization keys `#tutorial_*`).
- Main-menu card «Обучение».

## Game mode -- stages
1. **BUILT 2026-11-05, not seen, untested.** Modes screen: an «Обучение» plaque (`#mode_tutorial`) under the
   two mode pills with a crystal tag «+25» over it (`economy tutorial_reward_crystals`; nothing pays it yet)
   -> `MainMenuController.StartTutorial` -> map id `Archipelago-tutorial` (`ArchipelagoMapId.TutorialId`),
   summer. `Tutorial/TutorialMode.IsActive` is decided in `MeshMapApplier.Awake` from the pending map id.
   `ArchipelagoGenerator.Tutorial()` lays the library's `Kind.Tutorial` frame (Isl-tutorial1) unturned in
   the middle of the 500 m sea; start zone = its `zone` spots; camera pan limited to the frame
   (±37 m). Off in the tutorial: fog (`FogOfWarManager.DisabledCheat`), bot (no `ArchipelagoBotCell`),
   orc main base and portals (`OrcRaidManager.TrySpawnPortalsNearTownHall` returns), lighthouse, start
   scout ship, dungeon mouths, tide caches, fertile land, ore veins, riches, autosave. The King still
   comes with the Town Hall (GiveStartingParty).
   `IslandAsset` now keeps every other -Spots.csv row by name (`TryGetSpot`): the tutorial's hovel,
   barracks, shipyard, orc_landing, portal... for the later stages.
2. **BUILT 2026-11-05, not seen, untested.** `Tutorial/TutorialRules.cs`: build menu = ONE tab (the first
   category's tab; the others hidden, `BuildingCategoryPanel.ShowRunTabsOnly`) holding Castle/Hovel/Barracks/
   Shipyard whatever their categories, each greyed «1/1» once built (`ShowLimit`, `BuildingPlacer`
   `HasRequiredBuilding`/`SelectBuilding`); every build `tutorial_build_sec` 5 s (`BuildingData.BuildSeconds`);
   infinite resources (`ResourceManager.Start` -> `SetInfiniteResources`); the hall brings
   `tutorial_start_people` 3 (`CitizenManager.HandleBuildingPlaced`); Barracks: the Латник only, at level 1
   (`SoldierStats.IsUnlocked`/`BarracksLevel`); Shipyard: the transport only, at level 1
   (`ShipStats.ShipyardLevel`, `FocusHireBar.ShowShips`); the Town Hall's hero bar hidden; building cards
   bare (name only, no demolish: `RunBuildingCard`); the King heals to full at `tutorial_king_heal_below`
   0.25 of his health (`SoldierUnit.TakeDamage`). Keys on the economy tab (sheet == repo).
   Still shown, for stage 3: the army icon / squad row (a tap on the soldier selects his Garrison squad,
   which is him alone), the resource bar.
3. **BUILT 2026-11-05, not seen, untested.** `Tutorial/TutorialDirector.cs` (on «TutorialDirector» under the HUD
   root, made by `SetupProjectHud.BuildTutorialDirector`, sprites only; idle outside the tutorial): steps
   TownHall -> Hovel (built) -> People (6 s) -> WoodPick/WoodSend -> StonePick/StoneSend -> Barracks (built) ->
   Soldier (a Латник exists) -> Shipyard (built) -> Ship (the transport) -> KingPick/KingBoard ->
   SoldierPick/SoldierBoard (in `ShipUnit.Aboard`; a fallen soldier skips it) -> Sail (the ship within 12 m
   of the frame's `player_landing` spot) -> Done. Panel at the top: the King's portrait + `#tutorial_hint_<step>`
   (17 keys, sheet == repo). Camera (`RTSCameraController.FocusOn` every 0.3 s): on a free person until
   one is selected (`CitizenSelector.Selected`), then on the nearest tree / boulder until he takes the
   order (`CitizenAgent.GatherTarget`); the step ends when he is idle again. King/soldier: on the unit
   until his group is selected (`ArmyManager.SelectedGroup`), then on the ship until he is aboard.
   `MeshMapApplier.TryTutorialSpot(name)` gives the frame's spots in the world.
   Not yet: the step order skips the orc landing (stage 4 adds Raid after Soldier); no arrow/highlight on
   the build menu; the camera fights a player who pans during a focus step (by design: it re-centres).
4. Orcs: the landing barge with 2 orcs (soft hits), their island: portal, level-1 tower, 1 orc; win
   on the portal; victory screen pays the crystals (`Wallet.Crystals`).
5. Texts (`#tutorial_*`), the numbers (a `tutorial` sheet tab or economy keys), tests.

## Open
- The soldier: the user said «гвардеец или что-то такое» -- read as ЛАТНИК (ManAtArms, the armoured
  one) at level 3; confirm when the Barracks step is built.
- Skip button inside the tutorial; first-launch auto-start -- later.
