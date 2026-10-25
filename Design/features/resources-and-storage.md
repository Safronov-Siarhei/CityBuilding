# Resources, storage, coins and taxes

> **2026-10-25:** the bar along the top shows Food but no longer Grain/Flour/Bread; EVERY resource is in
> the INVENTORY -- a round sack button under the citizens button (`UI/InventoryPanelController`, built
> by `SetupProject.BuildInventoryPanel`; glyph `Hud_Bag` from make_hud_glyphs.py): picture, name,
> amount, dimmed at 0; no per-row room (a storehouse's room is shared by its group). New `Meat`
> (Food group, icon `Res_Meat`). Tests PlayMode `FoodAndInventoryTests`.

> **Status (2026-10-21):** shipped and stable. Coins come from tax and from minting
> ([production-chains](production-chains.md)). Balance of coin income vs research/army still unplayed.
> **Read with:** [production-chains](production-chains.md), [food-and-hunger](food-and-hunger.md).

## What the player sees
A resource bar at the top showing only resources the settlement has ever met (a chip stays once
seen, even at 0). Storage ceilings per family; storehouses raise them. A tax control bottom-right
(«Налог: 10%», − / +) — coins arrive once per game day.

## Rules (the user's decisions — don't re-ask)
- `ResourceType`: Wood, Stone, Food, Gold, Population, Iron, Coal, Coins, Grain, Flour, Bread,
  CopperOre, IronBar, CopperBar, GoldBar. **`Iron`/`Gold` are ORE** (names kept because the sheet
  spells them so); `ResourceNames` says «железная руда». Bars are separate types.
- **Coins ≠ Gold.** Coins are spending money (recruit, upkeep, research); gold is raw metal.
- Capacity is per FAMILY, not per resource: `Materials` / `Food` / `Valuables` (+ `Grain` has its own
  family for the Амбар). `ResourceStorage.GroupOf` maps type → family. Ore lives in the Склад
  (Materials); bars and coins in the Сокровищница (Valuables). The Treasury is research-locked, so
  the Town Hall gives `base_capacity_valuables` to bridge the gap.
- `ResourceManager.Add` RETURNS how much fitted; callers that care report waste.
- `ResourceManager.AddProduced` (vs `Add`) also counts toward `LifetimeProduced` — the raid
  progression term. Only real production calls it (workshop output, gatherer home, manual tap);
  refunds, taxes and cheats do not. Saved.
- **Easy start (2026-10-20):** opening stock from the sheet `start_*` (80 wood, 30 stone, 50 food,
  `start_gold`); first levels of the basic buildings cost 30–50 % less (road 1 stone), upgrades keep
  their prices; no raid and no sickness while the town settles in (`settling_in_seconds`, ~7 min).
- Tax: 0–100 %, `coins_per_citizen_per_day_at_max_tax` × population × rate, per day. Lower tax =
  happier (`TaxScore = 100 − rate`).

## Code
- `Scripts/Resources/`: `ResourceManager` (store, Add/TrySpend/AddProduced, OnChanged),
  `ResourceStorage` (families, capacity), `ResourceType`, `ResourceNames`, `ResourceDiet`,
  `ResourceAmount`, `ResourceIconLibrary`, `TaxManager`.
- UI: `UI/ResourceHUDController.cs` (chips, reads its real width), `UI/TaxRateController.cs`,
  `UI/CostRowBuilder.cs`.
- Day clock: `Core/GameCalendar.cs` (`day_length_seconds` = 120 s real).

## Data
economy: `start_*`, `base_capacity_{materials,food,valuables,grain}`, `coins_per_citizen_per_day_at_max_tax`,
`day_length_seconds`. buildings: `storage_group`, `storage_capacity(_2.._5)`.

## Tests
EditMode `ResourceManagerTests`, `ResourceStorageTests`, `TaxManagerTests`, `TaxManagerRateTests`,
`ResourceHUDFormatTests`, `GameCalendarTests`; PlayMode `StorageAndUpgradeTests`, `EasyStartTests`.

## Traps
- `ResourceIconLibrary` arrays must be sized off the enum — a fixed `new Sprite[8]` once silently
  stopped covering new resources.
- Audit (2026-08-23): coins were the only currency of progress with tax as the only tap; minting
  (2026-08-31) fixed the drought. Research tree still ~8 605 coins total — re-price only after a playtest.

## Open
- Stone is finite (~2 800 on a map) vs ~4 700 to build everything at level 3 — never addressed.
- Resource wishlist not built: sand, clay, water, hides; fruit, meat, vegetables, fish, wine.
