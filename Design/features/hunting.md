# Hunting: herds on the islands, the Hunter's Hut (sandbox)

> **Status (2026-11-21): BUILT in code (setup x2 run), the user has not seen it, no tests, not on a phone.**
> Models APPROVED («Да, дальше») and in the game: `HunterHut1-lvl1..5` (Models/Buildings), `SoldierHunter`
> (Models/Soldiers, jointed, ArmDraw/ArmBow), `FaunaBoar` (Models/Fauna) -- built into the H: models .blend by
> `Tools/blender/hunting_install.py`, exported by `export_fbx.py`. Icons: `Res_Hides` (`render_icons.py`),
> `HerdHare/HerdDeer/HerdBoar` (`render_hunt_icons.py`), `HunterHut` + `_L2.._L5`.
> Concepts: `Design/concepts/hunting/Hunt_1..3*.png` (`Tools/ui/hunting_concepts.py`), renders `models/`.

## How it is built
- `Scripts/Maps/Hunting.cs` (on the managers object; wired in `SetupProjectCityScene` with the 3 animal models,
  the hunter model, the 3 sign sprites and `Materials/HuntLit.mat`, URP Unlit yellow). Runs only when
  `Infirmary.Active` (the sandbox rules).
- Herds: clock `hunt_first_herd_sec`, then up to `hunt_herds`; the first on the Town Hall's island; others on a random
  island (sampled cells, stride 2, >= 40 cells) that is not the orcs' (`MeshMapApplier.ArchipelagoOrcCell`, new) nor
  the bot's, has no herd and is not the one just left. A spot = Wildlife's `OnLand` + `hunt_building_gap_cells` from
  every building + open land round it. Animals wander inside `hunt_herd_radius_cells`; at the end of the life they
  run outward 4 s and vanish; a retry every 30 s refills the map.
- Sign: a screen-space overlay Button (rim + parchment disc + animal icon) bobbing over the centre; hidden under
  `ModalGate`. Tap = `Light()`: yellow rings under the live animals + the radius ring for 6 s, and the
  `#log_herd_status` notice. Appearance notice `#log_herd_appeared` (picture `Herd<Kind>`, tap flies the camera).
- Hut: `HunterHut` in `buildings.csv`/`sandbox.csv` (Food, batches_per_tick 0 = produces nothing itself; interval 6, not 0 -- a 0 fails BalanceConfigTests,
  max_workers 1-5), shell building in SetupProjectCityScene, `BuildingIds.OnePerIsland` checked by
  `BuildingPlacer.OnePerIslandTaken` (`#place_blocked_one_per_island`), card line `#card_what_hunterhut`.
- Hunters: one `SoldierHunter` figure per `AssignedWorkers` of each operational own hut (synced every 1 s; the
  resident himself is inside the hut). Without a herd on the hut's island: strolls within 6 cells of the hut. With
  one: walks within `hunt_shot_range_cells` of the nearest animal, `FigureAnimator.OnStruck()` every `hunt_shot_sec`,
  an arrow (`CombatProjectiles.FireAt`, new) after 0.35 s, resolved 0.45 s later. Hit -> the animal falls on its side,
  `AddProduced(Food/Hides)` + popups. Miss on a boar with `hunt_boar_attack_pct` -> the boar charges,
  `LayOffWorker()` + `KillCitizens(1, true)` + `Remains.Drop` + `#log_hunter_killed`.
- `ResourceType.Hides` (after Meat): in the inventory panel, NOT in the top bar (its list is explicit).
- Saved: `SaveData.huntClock` + `herds` (`HerdEntry`), in GameSaveController next to the plague. Hunters are not
  saved (re-made from the huts' workers).

Read with: [food-and-hunger](food-and-hunger.md) (food buildings, «+N» popups), [resources-and-storage](resources-and-storage.md)
(hides), [maps-and-terrain](maps-and-terrain.md) (the decorative Wildlife -- deer/hares that never vanish),
[archipelago-and-ships](archipelago-and-ships.md) (islands, residents work on their own island), research/Laboratory card.

## Rules the user decided (2026-11-21) -- do not re-ask
- **Sandbox only.**
- Herds: **hares, deer, boars**. At most **one herd per island**; **3 herds on the map** in all.
- The **first herd** appears **10 min after the game starts on the player's own island** (to show herds exist).
- A herd lives **8 min**, never leaves earlier; then it leaves and a new one appears on ANOTHER island.
- Herds appear on any island **except the orcs' island and the bot's island** (a reason to build on other islands:
  a hunter needs his own hut + houses on that island).
- A herd's **size** is rolled when it appears: small / medium / large, each with its own chance.
- A herd has a **reach** it roams inside after appearing; it may not appear closer than some distance to buildings.
  A built-up island may get no herds -- that is fine («дикие животные не живут, где много людей»).
- Over the herd's centre hangs a **round sign in the air with the animal**. Tap -> every animal of the herd lit
  **yellow**, and a **notice** in the feed: «<herd>: осталось N из M, уйдут через м:сс».
- When a herd appears: a **notice** «На острове появилась стая …»; tapping it flies the camera to the herd.
  **No minimap mark.**
- Each kill gives at once (no carrying): **food and hides 1 / 2 / 3** (hare / deer / boar).
- Hunting is shown: the hunter **shoots with a bow**; every shot hits by the kind's own **chance** (the player cannot
  predict the catch). The herd is finite: enough hunters can wipe it out.
- **Boars may attack and kill the hunter** (a chance).
- **Hunter's Hut**: a new building, **opened in the Laboratory**, **one per island**; levels give **1/2/3/4/5**
  hunters. 1 hunter auto-assigned, more by the usual round-robin; the player may lower the count by hand.
  Hunters **walk round their island** and periodically check it for a herd.
- **Hides**: a plain resource for now (use to be invented later), kept in the ordinary **Warehouse** and shown in
  the **inventory**, **not** in the top bar.
- The decorative deer and hares (Wildlife) stay as they are; a huntable herd is told apart by its sign.

## Numbers proposed by Claude (the user: «поставь значения сам пока что») -- go to the sheet, not C#
Revised 2026-11-21 (the user: «слишком высокие шансы попадания, тогда не будет смысла в прокачке хижины, шкура
должна быть редкой добычей, которую трудно получить»): low hit chances + a slow shot, so ONE hunter takes only part of a
herd and the hut's levels (more hunters) matter. The same day the user dropped the separate hide chance («делает
добычу слишком сложной»): the ONLY chance is the hit; every kill gives its food AND hides.
| herd | animals small/med/large | hit chance | food per kill | hides per kill |
|---|---|---|---|---|
| hares | 4 / 6 / 8 | 25 % | 1 | 1 |
| deer | 3 / 5 / 7 | 15 % | 2 | 2 |
| boars | 2 / 3 / 5 | 10 % | 3 | 3 |
- a hunter shoots once every **30 s** (~16 shots in a herd's 8 min, less the walk to it): per hunter per herd about
  4 hares / 2.4 deer / 1.6 boars = ~4 / 5 / 5 food and as many hides -> a large herd needs 2-3+ hunters.
- size chance small/medium/large 50 / 35 / 15 %; a boar attacks on **2 %** of the misses -> the hunter dies
  (~0.3 deaths per hunter per boar herd; 10 % killed him almost every time).
- Hut research: 40 coins, 60 s. Levels 1-5 cost: 40 wood + 10 stone; 60 + 30; 90 + 60; 130 + 100 + 20 gold; 180 + 150 + 50 gold.
- Hunters check the island every 10 s. Herd reach and the distance to buildings: to set when building (proposed
  reach 6-8 cells, 10-12 cells from any building).

## Open
- The user's playtest; hides are only a resource for now (no use yet).
- Performance: herds of 2-8 animals, off-view animals switched off like Wildlife -- check on the Redmi Pad.
