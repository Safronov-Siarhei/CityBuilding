# Army control: squads (concept, 2026-11-05)

> **2026-11-10:** the squad row's «+» draws `Hud_Plus` (Tools/ui/make_hud_glyphs.py `plus`). The squad-creation window needs rework -- TO DISCUSS with the user (not started).

> **Status:** CONCEPT AGREED 2026-11-05. STAGE 1 (squads, Garrison, army icon, squad row, machine row, round
> cancel, tap-selects-squad) BUILT 592efc49 2026-11-05 (army/save/dungeon/sandbox tests green; Invasion/Archipelago tests
> not run yet; user not seen). STAGE 2 (squad window) BUILT 2026-11-05, user not seen. STAGE 3 (behaviours + formations) BUILT 2026-11-05,
> user not seen. General run after stage 3: EditMode 461/461, PlayMode 365/384 (18 skipped, 1 flaky
> dungeon test, green alone). STAGE 4 (ships) BUILT 2026-11-05, user not seen. STAGE 5 crests APPROVED and IN THE GAME 2026-11-05.
> ALL FIVE STAGES DONE; the user has not played any of it yet.
> Replaces the right-side army panel (one group per soldier type) described in
> [army-and-soldiers](army-and-soldiers.md). **Read with:** [army-and-soldiers](army-and-soldiers.md),
> [archipelago-and-ships](archipelago-and-ships.md), [hud-and-ui](hud-and-ui.md).

## HUD
- **Army icon** under the hero portraits (right edge); with fewer than 3 heroes it moves up under the last one.
- Tap it: drops any hero/building selection, opens the **squad row** bottom-centre: Garrison crest, the
  player's squads (max **5**), «＋» (opens the squad window on the «Создание отрядов» tab). With a squad
  selected a «!» appears right of «＋» (opens «Управление отрядами» for it).
- **Machines** have their own smaller icons in a row ABOVE the squad icons; each is selected and ordered
  alone; no «!» for a machine.
- A squad icon = its **crest** (8-10 to choose from, changed in «Управление»; art to be approved) with the soldier count under it.
- With a squad selected: 3 **formation** buttons on the right under the army icon, and a **cancel
  selection** button below them. The same cancel button replaces the «Выбран король» plaque for heroes.
- One squad (or one machine) selected at a time; no multi-select, no «all».
- Tap on a soldier in the world selects HIS squad; his card moves to a long press.

## Orders (variant B)
- Selected squad: tap ground = go, tap enemy = attack, tap wall = go up, tap own ship = board.
- **Deselecting changes nothing**: the squad finishes the order, then follows its behaviour where it is.
  To stop it: tap the ground beside it.
- **Fight rule**: fight while an enemy is within the squad's reach; none left -> back to its point.
  Shot from beyond reach -> goes at the shooter: a short way on «Стоять», without limit on «Наступать».

## Garrison
- Every new recruit joins it. No size limit. Always at home on the main island, defends it, soldiers act
  one by one. No behaviour choice, no formation, cannot board / go to another island: trying shows a hint.

## Squad window (pauses the game, like the menu)
- Tab «Создание отрядов»: two squads side by side, each picked from a drop-down (the drop-down also
  makes a new squad); pencil = rename, then delete (soldiers go back to the Garrison); tap a soldier's
  card = he moves to the other column; scrolling list with a visible scroll bar; «!» = short help.
  Card: portrait, type, level, health.
- Tab «Управление отрядами»: one squad from the drop-down; crest; behaviour (3-4 buttons); formation (3).
- **Auto-refill** switch: a squad takes soldiers of the types it lost from the Garrison; works only while
  the squad is on the main island (hint when it is not). Not auto-hiring.

## Behaviours and formations (proposed by Claude, the user's examples were placeholders)
- Behaviour: Стоять (hold, short chase) / Охранять город (patrol near the hall and walls, chase in a zone)
  / Наступать (seek enemies on the island) / За героем (follow a chosen hero) -- all 4 agreed 2026-11-05.
- Formation (the user, 2026-11-05): **Квадрат / Треугольник / Врассыпную**. Slots computed once per order, not per frame.

## Ships
- Capacity grows with the ship's level (the user, 2026-11-05): **transport 8-10-12-14-16, battleship
  (линкор) 12-14-16-18-20**; ship levels are raised in the **Laboratory** (not built yet -- part of this work). A squad bigger than the ship does NOT board
  and NOT split: notice «Уменьшите отряд или улучшите корабль».
- Squad selected + tap the ship = it boards when it fits; the ship then has a «Высадить десант» icon by a shore.
- A machine takes **4 places**; boards the same way.
- No «bring the squads home» button on a raid (the user: return them by hand or grow the Garrison).

## Open
- Crest art (8-10) -- renders to the user first. Heroes stay apart from squads.
- Hiring stays as is (the user, 2026-11-05): recruit -> Garrison -> moved into a squad in the window.
- Squads in save data; the bot and Invasion keep their own automatic armies.

## Code (stage 1)
`Combat/ArmyGroup.cs` (`ArmyGroupKind` Type/Garrison/Squad/Machine, Name, Crest, garrison posts),
`Combat/ArmyManager.cs` (`UsesSquads` = not Invasion, `Garrison`, `Squads`, `Machines`, `CreateSquad`,
`DeleteSquad`, `MoveSoldier`, `RecruitGroup`, `SquadName`; CountOf/SoldierCount per member),
`Combat/SoldierUnit.cs` (`Regroup`, `GarrisonTarget`: fighters within `garrison_defend_cells` of the hall
or near his post, never structures), `UI/ArmyBar.cs` (+ `TrySelectSoldierAt`), `UI/SelectionBadgeController.cs`
(round cancel for a group, plaque for a citizen), `Input/WorldInputDispatcher.cs` (long press = fighter card),
save `ArmyGroupEntry.kind/squadName/crest`, `SoldierEntry.type/post` (older saves: soldiers -> Garrison,
machines -> own groups). Built by `SetupProjectHud.BuildArmyBar`. Old `ArmyPanelController` DELETED.
Sheet: economy `army_max_squads` 5, `garrison_defend_cells` 30; localization `#army_garrison`,
`#army_squad_name`, `#army_garrison_hint`, `#army_squads_full`. Test `ArmyCombatTests.ARecruitJoinsTheGarrison_...`.
Until stage 2, «+» makes an empty squad (no window yet to fill it); the cheat «отряд» fills a new squad.

## Code (stage 2, the squad window)
`UI/SquadWindow.cs` -- built AT RUNTIME on first open (SetupProjectHud.BuildSquadWindow only passes sprites,
portraits by SoldierType, the HUD root and HudHider); opened by `ArmyBar.WindowRequested` («＋» = create tab,
«!» = manage tab); pauses (timeScale 0, ModalGate, HudHider). Create tab: two columns (drop-down = Garrison /
squads / «Новый отряд»; same group picked on both sides swaps), card tap = `ArmyManager.PlayerMove` (sets the
squad's wanted makeup), «Aa» rename (InputField, 24 chars), «X» break up. Manage tab: squad drop-down (the
Garrison listed only to say it takes no settings), crest tap = `NextCrest`, members, «Пополнять из Гарнизона»
(`AutoRefill`, `ArmyManager.TickRefill` every 2 s, only while `AtHome`). `TransferBlocker`: aboard a ship,
or a squad off the main island (`MeshMapApplier.SameIsland` with the hall). Saved: `autoRefill`, `wanted` pairs.
Pencil/bin are text «Aa»/«X» (no glyphs drawn for them). Behaviour/formation buttons: stage 3.
Texts: `#squads_*` (13 keys), `#army_move_aboard`, `#army_move_not_home`.

## Code (stage 3, behaviours and formations)
`ArmyGroup`: `SquadBehaviour` Hold/Guard/Advance/FollowHero, `SquadFormation` Square/Triangle/Scatter (pinned ints,
saved), `Facing` (set by orders: the front rank stands on the side it was sent), `FormationOffset(formation, i, n)`
(pure; slots sorted by rank: melee/riders front, shooters, monk last -- stable), `AttackOnItsOwn` / `DropOwnAttack`
(a behaviour's target, apart from the player's: `HasPlayerAttack`). `ArmyManager.TickBehaviours` every 0.5 s:
Guard = 6-point round of the hall (`squad_guard_patrol_cells` 10), chases fighters inside `squad_guard_zone_cells` 22,
only at home; Advance = nearest enemy on its island (fighters first unless priority Structures), any distance;
FollowHero = rally point `squad_follow_cells` 2 behind the chosen hero. Advance answers a shooter at any distance
(SoldierUnit.AcquireTarget). UI: 3 formation buttons under the army icon (pictures drawn in SetupProjectHud.FormationIcons),
manage-tab rows Поведение (shield/home/swords/King portrait; «За героем» asks which hero) and Построение.
Spacing `squad_formation_spacing` 0.9 m, `squad_scatter_spacing` 1.8 m. Open: a mixed squad marches at each
member's own speed (no common pace yet).

## Code (stage 4, ships)
units `capacity_2..capacity_5` (transport 10-16, battleship 14-20; battleship `capacity` 8 -> 12) ->
`UnitBalance.CapacityAt(level)` / `HasShipLevels`; `ResearchKind.ShipLevel` (`level_ship:<id>:<n>`, levels 2-5,
economy `ship_level_coins_N` / `ship_level_seconds_N`, crystals by the usual research rule from level 3, Lab level
>= ship level), `ResearchManager.ShipLevel(id)`, rows on the Lab's army tab after the soldiers. `ShipStats.CapacityNow`
(the player's at its level; orc/bot at level 1), `PlacesOf` (machine `ship_machine_places` 4, anyone else 1),
`ShipUnit.UsedPlaces`/`FreeSeats`. `ShipManager.DescribeBoardBlocker`: a squad or machine boards whole --
`#ship_squad_too_big` «Уменьшите отряд или улучшите корабль». «Высадить десант» = the existing land button,
its plaque now places used/capacity. Test: ArchipelagoTests boarding (a squad bigger than the transport refused).

## Crests (stage 5)
`Tools/ui/make_crests.py <out> [--sheet preview.png]` draws ten heater shields (gold rim, coloured field, one
charge: crown, tower, tree, swords, star, waves, moon, cross, chevron+star, sun) -> `UI/Glyphs/Crest_0..9.png`;
approved by the user 2026-11-05. `ArmyBar.crestSprites` (index = `ArmyGroup.Crest`), `CrestSprite(i)`;
the stand-in colours stay as the fallback. The squad window's crest shows the picture.

Tutorial (2026-11-05, met the other device's tutorial work): in `TutorialMode` a recruit joins the first squad
(made if none), not the Garrison -- the tutorial taps its soldier and sends him aboard. `TutorialDirector` now
checks the mode in Start, not Awake (MeshMapApplier decides it in its Awake; the HUD objects added for the squads
changed the Awake order and turned the tutorial off).
