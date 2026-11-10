# Food, eating and starvation

> **2026-11-10 (the user):** past the hungry grace (`hungry_days_before_deaths`) with NO food in store, people leave ONE AT A TIME every `starve_leave_interval_sec` (20, economy) instead of a batch at the meal (`FoodConsumptionManager.Update`, `Starving`/`Hungry`); the day's leavers go into the death register at the next meal; the HUD food chip warns. Placing a Farm/Fisher/PigFarm inside a working Bakery's/Smokehouse's reach shows «+N» over it, a Bakery/Smokehouse «+N ×M» (`PlacementHudController.UpdateBonus`, `FoodOutput.BoostAt/ReachedFrom`, keys `#place_food_*`). The fisherman casts only into cells whose ground is under the sea (`WorkSpots.Wet`), standing on land or the beach's wet margin. Old hunger tests (deaths at the meal) are now outdated. Built, not seen.

> **2026-11-02 round 9:** every citizen eats 1 food a day (`food_per_mouth_per_day` = 1, confirmed to the
> user). Fisher's hut fishing radius = `fish_hut_reach_meters` 8 (was 16), shown as the hut's radius
> ring (`HarvestRadiusOverlay.RadiusOf`); the fisherman fishes from shore cells inside it, and a shoal
> inside it doubles the hut.


> **2026-11-02: FOOD REWORK (the user, «проще, для мобилок») -- BUILT, untested, not seen.** Supersedes
> the chains below. One Food (new icon: apple + loaf + ham, `render_icons.py res_food`). Each food
> building has ONE worker and makes a flat amount a day, delivered as «+1» popups evenly over the day
> (`ProductionBuilding.TickFood`, `Citizens/FoodOutput.cs`):
> - Рыбацкая хижина: `food_day_fisherhut_1..5` = 3..7; on land within `fisher_shore_cells` (2) of
>   water; a shoal in reach doubles it (every other fish comes out of the shoal).
> - Ферма: 3..7 (+`food_fertile_bonus` 1 with most of its plot fertile); starts unlocked.
> - Фруктовый сад / Свиноферма: 5..25 (+5 a level), dearer (sandbox costs) and RESEARCHED (Orchard
>   25 coins/60 s, PigFarm 30/60). Farm/Orchard/PigFarm not within `food_inland_cells` (2) of water.
> - Пекарня: every working Farm within its service_radius (6) +`food_boost_baker_N` (= its level);
>   Коптильня: the same for Fisher's Huts and Pig Farms. Bonuses STACK (an L1 + an L2 bakery = +3).
>   Baker requires Farm, Smokehouse requires FisherHut.
> - Windmill, Barn, BigBarn (and WaterMill + Dock, 2026-11-03, «не нужны») HIDDEN (`BuildingIds.Hidden`: off the build menu and the Laboratory);
>   all food in the Pantries. Grain/Flour/Meat no longer made (off the inventory card).
> - Card + workforce row: «Еда: +N в день» / the booster's «+N … в радиусе R клеток».
> Numbers: economy rows 383-415; buildings max_workers 1, service_radius 6; recipes all -> Food.

> **2026-10-25: ONE FOOD COUNTER** (the user): `ResourceDiet.Edible` = Food only. Bread is no longer made
> or eaten -- the Пекарня makes Food (`ResourceType.Bread` kept only so an old save's bread becomes Food
> at `GameSaveController.BreadToFood` = 1.5). Food buildings differ in how much Food they give: the
> longer the chain, the more per worker (L1: fish/fruit 1.0, PigFarm->Smokehouse 1.5, Farm->Windmill->
> Baker 2.0; EditMode `ALongerFoodChain_FeedsMorePerWorker`). `food_variety_target` = 1 (contentment is
> off anyway). Middle links (Grain, Flour, Meat) are not eaten. The houses replace the starved, so
> hunger only slows growth now ([sandbox-run-systems](sandbox-run-systems.md)).

> **Status (2026-10-21):** shipped 2026-08-17. Only two edible types exist (Bread, Food), so the
> variety factor is a three-state switch. Grace period and harshness are Claude's guesses, never playtested.
> **Read with:** [production-chains](production-chains.md), [happiness-and-migration](happiness-and-migration.md).

## Rules
- **Everyone eats, citizens and soldiers**: `food_per_mouth_per_day`. Food consumption does NOT rise
  with soldier level.
- Served **round-robin across every edible store** (not largest first) — variety reads what was eaten.
- `ResourceDiet.Edible` is the diet, deliberately NOT the storage group: flour lives in the Кладовая
  but is not food (eating it would drain the bakery). New foods go in that one list.
- First hungry day = warning only (`hungry_days_before_deaths`); then deaths proportional to missing rations.
- Population 0 = defeat (guarded by `_hadCitizens`; population is 0 before the Town Hall exists).
- `FoodConsumptionManager` is the settlement's death register (`RecordDeaths`) — plague dead go
  there too. Its save field keeps the name `recentStarvationDeaths` ON PURPOSE (renaming drops old saves).

## Code
`Resources/FoodConsumptionManager.cs`, `Resources/ResourceDiet.cs`, `Citizens/CitizenManager.cs`
(`KillCitizens`), `Core/GameOverManager.cs`.

## Data
economy: `food_per_mouth_per_day`, `hungry_days_before_deaths`, `food_variety_target` (2),
`deaths_memory_days`, `happiness_penalty_per_death`.

## Tests
EditMode `FoodConsumptionTests`, `FoodBalanceTests` (asserts `food_variety_target` ≤ edible count —
nothing forces it UP as foods are added; raise it by hand).

## Open
The farm feeds nobody directly (grain isn't edible) — the opening food is the Fisher Hut. Meat/fruit
design owed by the user.
