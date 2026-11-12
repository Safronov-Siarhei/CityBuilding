# Citizens and workforce

> **2026-11-02 WORK LOOPS (round 9, the user) -- BUILT 9cd796d3, untested, not seen.** Workers act out
> their trade: woodcutter axe at the tree, quarryman pick at the rock, ploughman hoe on the Farm's plot,
> picker basket in the Orchard, swineherd pail in the Pig Farm (faces the pen centre), fisherman casts a
> rod from a shore cell inside the hut's fishing radius and pulls a fish out (5 s cycle). Tools are
> models in the villager's space (`Tools/blender/tools.py` -> `Models/Citizens/Tool*.fbx`, the moving
> part "<Name>-Swing" pivots at the right shoulder, the rod's fish "-Catch"), wired by
> SetupProjectCityScene into `CitizenVisualsManager.toolModels` -> `CitizenWaddle.ToolModels`.
> `CitizenWaddle.Work(kind, faceTo)/StopWork()` + `TickWork` curves; `CitizenAgent.SetWorking(..., kind,
> spots)` walks spot to spot (`Citizens/WorkSpots.cs`: KindOf by building id, For = plot points / cached
> shore cells; 6 s per spot). A tool is instantiated on first use per citizen and hidden while walking.


> **2026-11-02 AUTO-STAFFING (the user):** every Production and Food building fills itself
> (`ProductionBuilding.AutoStaffed`, `WantedWorkers` = all its places) from the free people, the OLDEST
> building first (`ProductionBuilding.All`, creation order; `CitizenManager` refills once a second,
> only built buildings). The player taking a worker off by hand caps it (`WorkersCapped`, saved as
> `workersCapped`) -- it is no longer refilled until the player fills it to the top again. Not in an
> Invasion run; the Laboratory/healer/fire brigade keep manual staffing. A built workplace short of
> hands shows a mark over it (villager face + red «!», `Citizens/WorkerShortageMarks.cs`,
> `UI/Icons/NoWorker.png`, `Shaders/IconBillboard`). Worker COUNTS of non-food buildings unchanged
> (the user picked «только автоназначение»). **OPEN: keep or drop the workforce window's manual
> distribution.**

> **Status (2026-10-28):** shipped; townsfolk are villager MODELS now (see Looks). Since 2026-10-20 citizens have NAMES and the workforce panel has a
> second tab: one row per citizen.
> **Read with:** [happiness-and-migration](happiness-and-migration.md), [production-chains](production-chains.md),
> [performance-mobile](performance-mobile.md) (walking citizens ARE the CPU budget).

## Looks (2026-10-28, the user: «чуть более детализированные модельки жителей»; pick A)
- Every citizen is one of 12 villager models in ONE piece (no joints, 1 draw each, ~700-840 tris):
  `Tools/blender/villagers.py` -> Models/Citizens/Villager{Peasant,Woman,Craftsman,Elder}{1..3}.fbx
  (straw-hat peasant, headscarf+apron woman, leather-apron craftsman with a hammer, stooped grey elder
  with a stick; 3 colourings each), soldiers' size and face; exported by export_fbx.py (`Villager*`).
- `CitizenVisualsManager.villagerModels` (wired in SetupProjectCityScene) -> random pick -> child
  "Figure" (scale 1/UnitSize, turned 180°, no shadow, NoSnow) + `CitizenWaddle` (hop + sway from the
  distance walked). With no models, the old two cubes.
- `CitizenAgent.Clothes`: the model's main colour, measured at setup (`MainColourOf`: biggest palette
  cell by triangle area, skin excluded) -> `villagerClothes`; the workforce panel's row swatch uses it
  (it read the cube "Body" before). Test `CitizenLookTests` (+ photos citizens-lineup/-town).
- WALK (the user, same day: «лицом вперёд, убери тряску»): the root never turns (CitizenAgent only
  moves it), so `CitizenWaddle` turns the FIGURE to the way walked (slerp 10/s) and only lifts it on
  each step (0.018 m); no sway. Test checks the facing and < 1° tilt.
- Top bar people chip: «total/capacity» on one line (was «18/18 (18)», wrapped); the idle count is a
  green disc on the pill's upper-left rim, only when > 0 (`ResourceHUDController.idleBadge`); the
  icon is the peasant's portrait (UI/Icons/Res_Population.png, render_portrait.py VillagerPeasant1,
  cropped to the head).
- Workforce citizens tab: each row shows the citizen's own face (`CitizenAgent.Portrait` <- 
  `CitizenVisualsManager.villagerPortraits` <- UI/Icons/PortraitVillager{Kind}{n}.png, render_portrait.py
  at head_z 0.45, cropped 46..210 and scaled to 128); the cube figure only for a citizen without one.

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

## Residents bound to houses, work by island (the user, 2026-11-12) -- BUILT, NOT COMPILED YET (Unity was open), not seen
Why: with houses on several islands the town-wide pool would let one island's people outnumber the
others when the main island's houses burn; work on another island had no way to get there.
- A house makes ITS OWN residents up to its places, then stops; they appear beside it (so on its island).
- A burnt or demolished house: its residents LEAVE the town (no re-housing).
- A building takes workers only from residents of ITS island; none free there -> a «no workers» mark
  over it and ONE notice («на этом острове нет жителей»), shown once.
- Barracks (and other hiring buildings) hire only from their island; a soldier KEEPS his place in his
  house (the house does not grow a replacement). A soldier whose house is burnt or demolished LEAVES
  AT ONCE with its residents (the user, 2026-11-12: «уходит сразу»).
- The Town Hall is a house too: a few permanent residents, made silently (never shown as making
  people) -- a hidden guard against an outright loss.
- Moving people between islands = only by building new houses there (no ferrying of citizens).
- Hunger takes people one by one from ANY house on the map (no per-island economy).
- Old saves do not matter (only the user plays).

How it is built: the town-wide headcount pool in `CitizenManager` stays (the economy reads it); beside it
`Citizens/HousePeople.cs` keeps a LEDGER -- per house (and the hall) its residents and its soldiers --
and reconciles it to the pool every frame (pool grew -> a house on an island short of people, then any
house, then the hall, the rest homeless = overcrowded; pool shrank -> homeless first, then the fullest
house, the hall last). Nothing of it is saved: after a load the reconcile re-homes everyone.
- A house grows one person every `person_respawn_sec` while it has room (paused by hunger); the hall
  grows up to `citizens_on_build` with no bar. `HousePeople.IdleOn(island)` = residents - workers there.
- `ProductionBuilding.TryAssignWorker` refuses when `IdleOn(Island)` is 0: the existing
  WorkerShortageMarks icon shows, and `#log_no_people_on_island` is posted ONCE per building (only when
  the town has idle people elsewhere).
- `ArmyManager.TryRecruit` checks `CanHire(position, crew)` (`#army_no_citizens_island`) and calls
  `Enlist` (a citizen of the fullest house there becomes its soldier); `Disband` -> `Discharge` (back to
  the house if it stands). A lost house -> `Abandon`: residents removed from the pool, its soldiers
  `ArmyManager.SendAway` (gone, nobody returned), `#log_house_lost` with the count.
- Figures (`CitizenVisualsManager`): `CitizenAgent.Home`; a new figure spawns at the door of a home
  with fewer figures than residents; an idle figure wanders round its home; a workplace pulls an idle
  figure from its own island (else any, teleported to its door); removal prefers figures of a house
  that has too many; `Rehome()` every node-retry pass moves surplus idle figures to homes short of
  figures (teleport across islands); `KillAgent` -> `HousePeople.Lose(home)`.
- Decor money and gifts now count a house's LIVE residents (`CitizensOf`), not its places.

## Code
`Citizens/CitizenManager.cs`, `Citizens/CitizenAgent.cs` (modes Wandering/Working/ManualMove/Gathering,
own copy of route logic, `Route/RouteVersion`), `Citizens/CitizenVisualsManager.cs` (pool, who walks
where), `Citizens/SettlementTierManager.cs`, `Citizens/CitizenNames.cs`, `UI/WorkforcePanelController.cs` (both tabs), `UI/CitizenSelector.cs`
(`MarkerHeight` 0.75), `UI/SelectionBadgeController.cs`, `Core/UnitSize.cs`.
Built in `Editor/SetupProjectPanels.cs` (`BuildWorkforcePanel`) and `SetupProjectGameplayUI.cs` (`BuildCitizenSelector`).

## Tests
EditMode `CitizenManagerTests`, `SettlementTierManagerTests`, `CitizenNamesTests`; PlayMode
`WorkforcePanelTests`, `CitizensTabTests`, `WalkCostTests`.

## Traps
- **The sea has its own NavMesh (ShipManager, ships' agent type)** -- every citizen query must use the land
  filter (agent type 0; `CitizenAgent.LandFilter`, WorkSpots too). Unfiltered, a wander target snapped onto
  the sea and a path between the two meshes failed («could not determine precisely which agent type»), and
  the straight-line fallback walked a citizen across the water (the user, 2026-11-12, after loading a save).
  Now: a partial path ending > 6 m short of its target, or a straight line that leaves the land NavMesh
  (`LeavesLand`) / crosses water cells, = unreachable: he stands still. A workplace on ANOTHER island is
  therefore never walked to (the building still counts its assigned worker) -- how island work should
  look is an OPEN question put to the user 2026-11-12.
- `Destroy()` is deferred to end of frame — counting `childCount` right after repopulating counts
  old rows; the panel exposes `RowCount`. +/- refreshes in place, the list rebuilds only when a row's
  building is gone.
- `CharacterController` costs ~8 µs per Move; dropping it was offered and the user took only the safe
  half (terrain out of the collision matrix). Roads don't carve the NavMesh; citizens sink ~2 cm.
