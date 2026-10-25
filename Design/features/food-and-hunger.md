# Food, eating and starvation

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
