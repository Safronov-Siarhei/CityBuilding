# Production chains and recipes

> **2026-11-02: the food chains are replaced** by flat per-day food buildings and bakery/smokehouse
> bonuses -- see [food-and-hunger](food-and-hunger.md). Windmill hidden; the grain/flour/meat links
> are gone. Ore/bars/coins unchanged.

> **2026-10-25:** the user's food design: ONE Food counter, chains pay more per worker the longer they are.
> New: Orchard `fruit` -> Food 1; PigFarm `meat` -> Meat 1 (new `ResourceType.Meat`, kept in the
> Кладовая like flour); Smokehouse `smoked` Meat 2 -> Food 3 (`requires` PigFarm); Baker `bread` Flour 1
> -> Food 3 (was Bread 2). Workers 2..6 and batches per level for the three new ones are on the buildings
> tab (Smokehouse batches 1..5 -- `UpgradeBalanceTests` wants more per worker every level). See
> [food-and-hunger](food-and-hunger.md).

> **Status (2026-10-25):** grain→flour→Food, pigs→meat→Food, fruit/fish→Food, ore+coal→bars, gold+coal→coins.
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
- Mines stand only on a free ORE VEIN of their ore; veins never run dry ([gathering](gathering.md)).
- Sleeping buildings (no resource yet): Коптильня, Свиноферма, Фруктовый сад.
- Upgrade rule (user, 2026-08-21): **sprawl cheaper in RESOURCES, upgrading cheaper in PEOPLE.**
  Workshops: batches ×1.5 at L2, ×2 at L3. Can't express ×1.5 of 1 batch → mines/Smelter tick
  doubled (6→12 s) with batch count doubled. Reuse that trick rather than fractional batches.

## Smelter controls (the user, 2026-11-12, concept A approved; built, setup green, not seen)
- A tapped Плавильня shows, like the Barracks, round buttons beside the «!»: copper, iron, gold bar
  (no price). A tap picks what is smelted; the metal smelting now sits on the gold button with a green
  tick. `UI/FocusHireBar` Kind.Smelter (`SmeltedBars`, `Smelt`, `ShowMetals`, `picked` objects),
  built in `SetupProjectHud.BuildBuildingFocus`.
- The card's band shows the recipe in PICTURES: «2 [ore] + 1 [coal] ▶ 1 [bar]»
  (`BuildingInfoPanelController.RefreshRecipeFormula`; the arrow is the `Hud_Play` glyph -- Rubik has
  no «→»). The old text picker on the card is gone for a converter.
- A bar over the building fills towards the next smelt, the bar's icon on it (as a sawmill's over its
  tree): `ProductionBuilding.ShowSmeltBar` (HarvestProgressBar; hidden with no workers, while built,
  or when the last tick was short of an input).
- MINTING (gold bar -> coins) has NO button now: the recipe stays in the data but cannot be picked.
  Open: what gold bars are for (the user is deciding; proposals: level-5 upgrades, diplomacy, keep minting).
- Concepts: `Tools/ui/smelter_bar_concepts.py` (A/B/C sheet, `final` -> `Design/ui/Smelter_Final_Concept.png`).

## Planned (agreed in talk 2026-11-12, NOT built)
- Оружейня: iron bar + coal -> melee weapons; melee soldiers hired for melee weapons + coins.
- Мастерская стрелка: ranged weapons (the user doubts copper fits; proposed wood + iron bar) -> ranged
  soldiers for ranged weapons + coins. Copper's role open (proposed: ship hulls, siege machines).

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
