# Production chains and recipes

> **Status (2026-10-21):** grain→flour→bread, ore+coal→bars, gold+coal→coins shipped. Meat/fruit
> chains await the user's design (asked 2026-08-17, unanswered).
> **Read with:** [resources-and-storage](resources-and-storage.md), [citizens-and-workforce](citizens-and-workforce.md),
> [building-levels-and-decay](building-levels-and-decay.md).

## What the player sees
Workshops staffed by workers turn inputs into outputs every tick. The Плавильня's card has a metal
picker (iron / copper / gold bar / coins). A starved converter logs once; a full output store stops it.

## Rules (decided)
- **Grain chain (user, 2026-08-16):** Ферма → пшеница (Амбар) → Ветряк → мука (Кладовая) →
  Пекарня → хлеб (Кладовая). Stays **1:1:1 at every level**.
- **Metals (user, 2026-08-17):** ore stays raw (Склад); 2 ore + 1 coal → 1 bar (Сокровищница). The
  player picks the recipe in the Smelter card. One recipe at a time.
- **Minting (user picked, 2026-08-31):** Smelter recipe `coins`: 1 GoldBar + 1 Coal → 4 Coins.
  ~120 coins/day from one level-1 gold chain.
- **Пристань and Водяная мельница produce NOTHING** («это бред») until the Water category is designed.
- Sleeping buildings (no resource yet): Коптильня, Свиноферма, Фруктовый сад.
- Upgrade rule (user, 2026-08-21): **sprawl cheaper in RESOURCES, upgrading cheaper in PEOPLE.**
  Workshops: batches ×1.5 at L2, ×2 at L3. Can't express ×1.5 of 1 batch → mines/Smelter tick
  doubled (6→12 s) with batch count doubled. Reuse that trick rather than fractional batches.

## How it works
- `recipes` tab owns ALL production: one row per thing a building can make (`building, recipe_id,
  in1/in1_amount, in2.., out, out_amount`). No row = produces nothing.
- buildings tab `batches_per_tick(_N)` = batches per worker per tick (per level).
- **Gatherers (Sawmill, Quarry) never read `batches_per_tick`** — `ProductionBuilding.Update` returns
  early for wood/stone; their income is the walk ([gathering](gathering.md)).
- An empty `in1` marks "made out of nothing". `ProductionBuilding.AffordableBatches` runs as many
  batches as the scarcest input allows (half-supplied = half speed). Converter refuses the tick when output is full; a plain producer wastes it.
- Production penalty from decay > 70 % ([building-levels-and-decay](building-levels-and-decay.md)).

## Code
`Citizens/ProductionBuilding.cs` (tick, recipes, AffordableBatches, NothingLeftInReach,
OnDestroy hands workers back), `Buildings/BuildingRecipe.cs`, `Buildings/BuildingData.cs`;
UI recipe picker in `UI/BuildingInfoPanelController.cs`. Import: `Editor/BalanceImporter.cs`
(`MaxRecipeInputs`), `SetupProject.CopyRecipes`.

## Tests
EditMode `ProcessingChainTests`, `FoodBalanceTests`, `UpgradeBalanceTests`,
`ProductionBuildingDecayMultiplierTests`; PlayMode `MintingTests`.

## Traps
- `per_day` / `payback_days` sheet columns LIE for gatherers (computed from batches they never use).
- Рыбацкая хижина is ×6 by L3 — cheapest food in the game; left alone until meat/fruit exist.
- `ProductionBuilding.SetAssignedWorkers` is the SAVE's door and asymmetric — tests use
  `TryAssignWorker/TryUnassignWorker`.

## Open
Meat/fruit/smoked chains; whether Fisher Hut switches to fish and generic `Food` retires; what bars
buy beyond the 3 buildings that cost them; equipment buildings (Bowyer, Armoury) deferred — the
Barracks takes bars directly.
