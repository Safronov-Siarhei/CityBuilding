# Citizens and workforce

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
- `Destroy()` is deferred to end of frame — counting `childCount` right after repopulating counts
  old rows; the panel exposes `RowCount`. +/- refreshes in place, the list rebuilds only when a row's
  building is gone.
- `CharacterController` costs ~8 µs per Move; dropping it was offered and the user took only the safe
  half (terrain out of the collision matrix). Roads don't carve the NavMesh; citizens sink ~2 cm.
