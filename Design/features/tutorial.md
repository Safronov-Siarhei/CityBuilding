# Tutorial (обучение)

> Status 2026-11-06: game mode BUILT, played twice; the SECOND PLAYTEST's rules (below) BUILT, NOT PLAYED yet. Every tutorial number is on the sheet's own `tutorial` tab (gid 1105001,
> `Balance/tutorial.csv`). Next: the user plays it again.
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

## Rules from the user's first playtest (2026-11-05) -- BUILT 2026-11-05, not played
**HUD**
- While the Town Hall is being placed: NOTHING but the hint panel and «Выберите место для Ратуши».
  Hidden: resource bar, Day, army button, rotate and cancel buttons, people/inventory buttons, minimap.
- After the hall: the top bar shows ONLY the people counter; the menu (exit) button stays. No Day, no
  army button, no people/inventory buttons -- for the whole tutorial.
- No rotate and no cancel button when placing ANY tutorial building.
- No «!» (info) button on any building: buildings can be selected, their info is never shown.
- The Shipyard's info panel is gone; only its ship icons stay.
- No game notifications (e.g. «поселению нужен колодец»).
- The King's portrait in the hint panel must stay inside its round frame (now it sticks out).
**Guidance (highlight = yellow outline + blinking)**
- Every build step: the build button (bottom centre) blinks; once opened, an ARROW points at the card
  to build; the other cards are LOCKED. Same for Лачуга, Казарма, Верфь.
- After the Лачуга: the camera zooms in 50% on a citizen and highlights him until tapped, then moves
  to the tree and highlights it. Same for the boulder.
- Wood and stone are gathered in 5 s.
- After the stone: the camera returns to the normal height over the Town Hall (most of the island seen).
- Shipyard built: the camera moves to it and it blinks until tapped.
- Ship bought: highlight + focus the King, then the soldier, then the ship (highlighted) to tap.
- The King wanders over the whole island (now he walks to the edge and stands there).
**Orcs**
- Their ship stands by the orcs' island from the start; 2 orcs walk out of the portal, board it, it
  carries them to the player's island and sails off into the open sea.
- Orcs are too weak (one Town Hall shot kills one): more HP.
**Bugs seen**
- A unit ordered onto a ship stays selected and leaves its yellow circle behind -> deselect on boarding
  (blocks picking the soldier).
- A school of fish takes the tap meant for the soldier; too many fish schools on the map.
- The Shipyard floats above the ground: it must sit in the ground and the water.
**Sheet:** every tutorial number goes to its OWN sheet tab `tutorial` (like `sandbox`) for the user to edit.
**Answers (2026-11-05):**
- After the hall the screen holds EXACTLY: the King's portrait top right, the main-menu button top left,
  the people counter top centre, the build button bottom centre (+ the hint panel). No minimap.
- «5 s» = the chopping/mining itself; walking stays normal.
- Fish: fewer schools and not tappable -- in the TUTORIAL only.
- Orc HP: a multiplier on the `tutorial` tab, starting at x3.
**My assumptions (not asked):** the Barracks gets the same treatment as the Shipyard (camera + blink until
tapped); the orc ship sails past the map edge and is gone; rotate/cancel/«!»/notifications are
removed in the tutorial only.
**The user (2026-11-05): EVERY change from this playtest is tutorial-only -- other modes are not touched**
EXCEPT two bugs the user said to fix for the whole game: the deselect-on-boarding and the floating Shipyard.

## Second playtest (2026-11-06) -- BUILT, not played
**Rules the user gave** (tutorial only unless said):
- Build button: blinks with a gold GLOW behind it (`TutorialHud.BuildHalo`, the circle sprite), scale +22%.
- Build cards: no price numbers (the `Amount_*` chips hidden), only the resource icons, centred; the card to build
  framed in yellow (`BuildingCategoryPanel.TutorialCard`, an Outline 7 px).
- Game menu: no Save entry; Settings and Home move up one place (`TutorialHud`, by name `SaveEntry`).
- Once the step's building is PLACED: build menu shut, no blinking, no arrow (`ShowTheWay`: `HasAny(Wanted)`).
- King's lines: words between `*stars*` in the sheet are coloured (`TutorialDirector.Emphasise`, #B5400C);
  «третий житель» gone. All 22 `#tutorial_hint_*` rewritten.
- Hiring was impossible: `FocusHireBar` showed only beside the «!», which the tutorial hides ->
  `BuildingFocus.ButtonsShown` (also the lighthouse bar); the arrow points at the hire button on Soldier/Ship.
- A double tap on a building opened its card: `BuildingFocus.OpenDetails` does nothing in the tutorial.
- Orc raid: `tutorial_raid_orcs` 5; the camera follows the barge until it lands them.
- The soldier: walks the island's round and runs at the landing (his squad on «Охранять город» until SoldierPick,
  then «Стоять»); walks at the King's speed (`SoldierUnit.ReadStats`).
- Selection is LOCKED (`TutorialRules.Selectable`, gate in `ArmyManager.SelectGroup`): nobody before KingPick,
  the King at KingPick/KingBoard, the soldier at SoldierPick/SoldierBoard, anyone from Land. Any held group is
  let go at KingPick/SoldierPick. `CancelSelectionButton` hidden.
- Shipyard placing: a gold ring at the map's `shipyard` spot. Sail: a ring at `player_landing`, camera on the ship.
- «Высадить десант» only from Land (`TutorialRules.MayLand`, `ShipActionBar`).
- Landed: the King and the soldier attack by themselves -- orcs, then the tower, then the portal
  (`TutorialDirector.Assault`, `ArmyManager.NearestFoe` now public).
- Portal: health = King's damage/s x `tutorial_portal_sec` 7; it shows a health bar when hit (ALL modes).
- Health bars sharper (ALL modes): `BuildingHealthBar` textures 256x64 / 128 with mipmaps, 1-px edge.
- Ship wake (ALL modes): spawned at the stern, drifting back and out (it lay ahead of the ship).
- Victory: the whole HUD hidden but the King's line and the panel (`TutorialHud.HideAllBut`); the panel shows the
  crystal icon and 25, the menu button centred (`GameOverController.ShowTutorialReward`).
- The held person's mark (ALL modes, the user's pick D of `Design/marker_options.png`): a gold octahedron gem,
  turning, in a soft camera-facing glow (`CitizenSelector.CreateMarker`), instead of the yellow cube.
- Boarding (ALL modes, the user's pick 1): no flag on the beach; while anyone walks to a ship, a gold arrow bobs
  over it and a gold ring pulses round it (`Ships/ShipBoardingMark`, on the ShipManager, from `BoardingShips`).
**Open (proposed to the user, waiting):** other wake looks. **Unverified:** «the King hits the
portal from too far» -- his structure reach is 1.6+1.2 m from the portal's centre; not reproduced.

## Third playtest (2026-11-06) -- BUILT, not played
- King's face in the hint ring: 156 px, 12 px lower (a green strip showed under it).
- Build button lifted `TutorialLift` 46 px in the tutorial (its glow ran off the screen).
- Cards: the yellow Outline gone; the card (or hire button) under the arrow BLINKS (`TutorialHud.BlinkCard`).
- Menu Save entry: `GameMenuController.LayEntries` drops it in the tutorial too (TutorialHud's own move was undone
  by LayEntries -- that is why the Save button stayed).
- `tutorial_birth_sec` 7: the Hovel's person (`HousePeople.Seconds`).
- ALL modes: no «OK!» over a gather order (CitizenSelector; «NO!» stays); people work a tree/boulder from 1.0 m
  (`CitizenAgent.NodeReachDistance`, was 1.6); melee fighters strike a portal / orc building from its wall
  (`SoldierUnit.StructureRange`: half footprint + 0.8 m, never more than the sheet reach).
- Citizen deselected at the Barracks step; the King strolls within 8 m every 6 s.
- Orc raid: boarding give-up 8 s (they hung 30 s); the camera follows EVERY FRAME (walkers -> barge -> the orcs
  ashore and their fight; our ship at Sail) -- `FollowMoving`; FocusOn every 0.3 s looked jerky.
- `tutorial_orc_guards` 3 guards round `orc_guard`, guard radius 9; `WalkGuards` moves their posts round the portal
  (6 m) every 7 s.
- Shipyard placing: every cell it can cover outlined like a fertile patch (`BuildingPlacer.CellsItCanCover`,
  `FertileLand.Flash(..., infinity)`), the ring on the one spot dropped.
- Ship portraits re-rendered (`render_portrait.py` got [distance] [yaw]; ships: distance 20, yaw -65).
- `#tutorial_hint_kingpick` = «Нажмите на меня — я поплыву с вами.»
- MAP: the strait widened -- `tutorial_plan.py` SPREAD 6 (orcs' island 6 cells west, ours 6 east), islet moved;
  islands.py + export_map.py re-run (Isl-tutorial1 on H:, assets in Models/Islands). SailReach 12 -> 6 m.
- No defeat by an empty town in the tutorial (`GameOverManager`); the Barracks hires its one soldier only while
  none is alive (`FocusHireBar`).

## Fourth playtest (2026-11-06) -- BUILT, not played
- Wood/Stone Send: once the person is sent the King's line hides (it covered the chopping bar).
- The boulder shown is one with no tree within 3.5 m (`TreeNear`); only that node takes the tap (also a tap
  within 140 px of it on screen), any other tap is ignored (`TutorialRules.WantedNode`, `CitizenSelector`).
- The camera is locked (`RTSCameraController.GesturesLocked`: no drag, pinch, keys, wheel) while a person and his
  node are shown (Pick steps, Send until sent).
- The blinking card no longer grows (its edge was cut by the cards' scroll view): colour only.
- Hire buttons: no price, the coin alone centred (`FocusHireBar.TutorialPrice`). The Shipyard is let go at KingPick.
- Sail: the camera is free until the ship is sent; a tap must be within 9 m of `player_landing` (land or sea),
  anything else refused (`TutorialRules.SailTarget`, `ShipInput.HandleTap`); once sent the ship is let go, cannot
  be picked (`ShipSelectable`), the camera rides with it. Land: the ship is handed over held, the arrow on the
  landing button (`ShipActionBar.LandButtonShown`), line «Нажмите на значок снизу, чтобы высадить солдат.»
- Victory: the crystal and the number measured and centred as a pair.
- Map: no decor within r+1.4 of a sea rock (`islands.py`; a bush sat on a spire), re-exported.
- ALL modes: landing puts each man a step apart (golden-angle spiral, 1.4 m) and each group holds where it stepped
  ashore (`ShipUnit.Disembark`); the ship is let go after a landing; a ship ordered to take on troops that is not
  by their island's beach SAILS IN to the sea nearest them (`ShipManager.TryMeetingPoint`, `Boarding.Meeting`).
- Destruction (ALL modes, the user's pick 1): `BuildFinishEffect.PlayCollapse` -- a copy of the building's body
  sinks ~its height into the ground in 0.6 s leaning 9°, dust cloud + column + ring, planks and stones thrown,
  `RTSCameraController.Shake` when in view. From `BuildingInstance.TryDamage` (the ruin is laid under it) and
  `OrcCampBuilding.Fall(announce)` (orc shaders: every mesh but the health bar).

## Fifth playtest (2026-11-06) -- BUILT, not played
The user: several round-4 items had NOT worked -- each checked in the code this time:
- Ship price still shown: round 4's `TutorialPrice` had landed in ShowGnomes, not ShowShips (fixed: ShowShips).
- Boarding a ship at sea / at its pier failed (twice reported): now ALWAYS the ship sails to the group
  (`ShipManager.TryMeetingPoint`: the land NavMesh raycast from the group toward the nearest water = a beach they
  can reach; the sea point by it); boarding waits until the ship stopped within 3 m of that sea point
  (`Boarding.Sea`). A ship no longer sails home on its own while people are aboard, someone is boarding it, or in
  the tutorial (`ShipUnit.TickIdle`).
- The person could be picked again and sent elsewhere, skipping the step: `TutorialRules.CitizenSelectable`
  (gate in `CitizenSelector.Select`) only on Pick steps and Send until sent; sent -> deselected.
- Barracks let go at Raid (after the hire). Ship fixed at the orcs' beach from Land (`TutorialRules.ShipFixed`,
  `ShipInput.HandleTap` ignores orders); not pickable while sailing (`ShipSelectable` false once sent).
- Line: «Поставьте *Ратушу* в *жёлтой зоне*».
- Boarding mark: a flat arrow turned to the camera over the hull's middle (the pyramid read as a cube beside it).
- ALL modes: the portal collapses (`OrcPortal.TakeDamage` -> `PlayCollapse`); a portal victory waits 3 s
  (economy `victory_delay_sec`, was 5; `GameOverManager.WinAfterTheFall`: IsGameOver at once, OnGameEnded after); every building that goes collapses
  -- combat, decay, demolish, orc buildings, portal (not roads/fences: `Data.isRoad`).

- Camera (2026-11-06): EVERY moving target (the person, the King/soldier, the ship at boarding, the raid, the ship
  at Sail) is followed per frame in `FollowMoving`; only the still ones (node, Barracks, Shipyard) stay on the
  0.3-s `Follow`. Re-aimed every 0.3 s the camera caught up, stopped, caught up -- the ship looked stepwise.
- Raid camera (2026-11-06): after the soldier's hire the camera stays on the town for `tutorial_raid_camera_sec`
  (3) before it goes over to the orcs boarding (`FollowMoving`, gated on `_stepTime`).

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
4. **BUILT 2026-11-05, not seen.** The orc island is laid when the Town Hall stands
   (`OrcRaidManager.SpawnTutorialIsland`, from `TrySpawnPortalsNearTownHall`): a portal (not main) at the
   `portal` spot, a level-1 `Watchtower` (`SpawnLooseCampBuilding`) at `orc_tower` with its shots
   `SoftenShots(tutorial_orc_damage_mult 0.2)`, one Raider on guard (`SetGuardPost(null, spot, 6)`) at
   `orc_guard`. The raid clock never runs in the tutorial. Step Raid (after Soldier):
   `ShipManager.LaunchTutorialBarge` -- one orc transport from the `orc_barge` sea to the sea by
   `orc_landing` with `tutorial_raid_orcs` 2 orcs; it lands them and is removed (no orc shipyard = its
   home is its landing). Every orc's blows x0.2 (`TutorialDirector.Soften` -> `OrcUnit.Toughen(1, mult)`).
   After Sail: Land (the transport empty) -> Guard (no orc alive) -> Tower (no watchtower) -> Portal ->
   Done. Breaking the portal is the map's win (`GameOverManager`, the only portal); the victory panel
   says `#over_tutorial_reason` with the reward and has no «play on»; the reward
   (`Wallet.Crystals += tutorial_reward_crystals`) is paid ONCE (`PlayerPrefs tutorial.rewarded`), replays
   pay nothing. Test: PlayMode `TutorialTests` (map, no bot/main base/fog, the rules, the orc island:
   1 portal, 1 tower, 1 guard; the script past TownHall).
5. Texts and numbers are in (economy `tutorial_*`, localization `#tutorial_hint_*`, `#mode_tutorial`,
   `#over_tutorial_reason`). Left: whatever the user finds playing it; a skip button; first-launch start.

## How the playtest rules are built (2026-11-05)
- `Tutorial/TutorialHud.cs` (owned by the director): hides HUD elements BY NAME through a CanvasGroup
  (alpha 0, no taps, LayoutElement.ignoreLayout) -- not SetActive, their controllers would switch them
  back. Lists `AlwaysHidden` / `HiddenBeforeHall` (ResourceHUD, Heroes until the hall stands). A renamed
  HUD object silently shows again -- the names are in Editor/SetupProject*.cs. The resource bar shows
  the people chip alone (`ResourceHUDController`, tutorial branch). Also: the blinking build tab
  (`BuildingCategoryPanel.FirstTab`), the arrow over the card (`CardOf`, a procedural triangle), the
  blinking gold ring in the world (LineRenderer).
- `TutorialRules.Wanted` (set per step by the director): the one card not locked (the shade of
  `ShowLimit`, empty text) and the only building `BuildingPlacer.SelectBuilding` accepts.
- Camera: `RTSCameraController.ZoomTo/GoalDistance`; zoom to tutorial_person_zoom at WoodPick, back
  and onto the Town Hall at Barracks. Soldier/Ship steps: camera + ring on the Barracks/Shipyard until
  `BuildingFocus.Instance.Current` is it.
- No «!»: `BuildingFocus.NoDetails`. No notifications: `EventLogManager.Post` drops all in the tutorial.
- Gather: `CitizenAgent.HarvestSeconds` -> tutorial_gather_sec. Fish: `ShipManagerFish` (count,
  `ShoalAt` null). Orc health: `Toughen(tutorial_orc_health_mult, …)` for the guard and every raider.
- King strolls (`Wander`, every 9 s within 14 m of the hall) until KingPick. Portrait: a Mask on its ring.
- Orc barge: `ShipManagerOrcs.MoorTutorialBarge` (shore water by the portal, out of `Steer` while in
  `_moored`) once the portal stands; at Raid the orcs spawn by the portal, `Steered`, walk
  (`SteerTo`) to `TryBargeBeach`, vanish aboard (or after 30 s), `SailTutorialBarge` lands them at
  orc_landing and sends the barge to `_tutorialExit` (open sea to the side), where it is removed.
  Test: TutorialTests checks the empty barge by the orcs' island.

## Open
- Not asked, my calls: the selection's round cancel button (CancelSelectionButton) stays; the King's
  stroll numbers (9 s, 14 m) and the 30-s boarding give-up are code constants.
- The soldier: the user said «гвардеец или что-то такое» -- read as ЛАТНИК (ManAtArms, the armoured
  one) at level 3; confirm when the Barracks step is built.
- Skip button inside the tutorial; first-launch auto-start -- later.

Trap (2026-11-05): `TutorialDirector` decides it is on in **Start**, not Awake -- TutorialMode is set in
MeshMapApplier.Awake and Awake order between scene objects is not fixed (new HUD objects broke it). Its soldier
joins a squad, not the Garrison (army control by squads, [army-control](army-control.md)).
