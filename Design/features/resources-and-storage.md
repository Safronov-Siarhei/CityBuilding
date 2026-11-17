# Resources, storage, coins and taxes

> **2026-11-02 TAX REWORK (the user) -- BUILT bdacd8a6, untested, not seen.** The tax slider is GONE from the
> HUD (with contentment off in the sandbox it only cost money). Every citizen pays `coins_per_citizen_per_day`
> (1) once a day, + `tax_house_bonus_per_level` (0.2) per level of the best finished **Tax House**
> (`BuildingIds.TaxHouse`, City tab, 2x2, `Tools/blender/taxhouse.py` -> TaxHouse1-lvl1..5).
> **Rules (the user, 2026-11-02, 04427d62):** ONE per town (`BuildingIds.OnlyOne`, `BuildingPlacer.OnlyOneAlreadyStands`,
> `#place_blocked_only_one`); expensive (150 wood, 200 stone); NOT open at start -- opened and levelled in the
> Laboratory for upgrade crystals ONLY: 3 / 5 / 7 / 9 / 11 (`tax_house_research_crystals_1..5`,
> `ResearchTopic.Crystals` override; research_coins 0, research_sec 60/90/120/180/240). The upgrade at the
> building itself costs nothing but build time. `TaxManager` keeps TaxRatePercent (save, contentment's
> TaxScore) but it no longer changes income; fractions carry to the next day. `TaxRateController` deleted.


> **2026-10-25:** the bar along the top shows Food but no longer Grain/Flour/Bread; EVERY resource is in
> the INVENTORY -- a round sack button under the citizens button (`UI/InventoryPanelController`, built
> by `SetupProject.BuildInventoryPanel`; glyph `Hud_Bag` from make_hud_glyphs.py): picture, name,
> amount, dimmed at 0; no per-row room (a storehouse's room is shared by its group). New `Meat`
> (Food group, icon `Res_Meat`). Tests PlayMode `FoodAndInventoryTests`.

> **Status (2026-10-21):** shipped and stable. Coins come from tax (minting removed 2026-11-12)
> ([production-chains](production-chains.md)). Balance of coin income vs research/army still unplayed.
> **Read with:** [production-chains](production-chains.md), [food-and-hunger](food-and-hunger.md).

## What the player sees
A resource bar at the top showing only resources the settlement has ever met (a chip stays once
seen, even at 0). Storage ceilings per family; storehouses raise them. Coins arrive once per game day
(flat tax, no slider since 2026-11-02 -- see the top note).

## Storage buildings (checked 2026-11-17)
- In the build menu: Склад `Warehouse` + Большой склад `BigWarehouse` (Materials), Кладовая `Granary` +
  Большая кладовая `BigGranary` (Food), Сокровищница `Treasury` + Большая сокровищница `BigTreasury`
  (Valuables). The Big one needs the ordinary one. Levels 1-5 each (`storage_capacity.._5`), sandbox prices
  in the sheet tab «Песочница: Здания (Склады)», level icons in Design/Reference.md.
- HIDDEN since the food rework (2026-11-02, `BuildingIds.Hidden`): Амбар `Barn`, `BigBarn` -- all food in the
  Pantries. The `Grain` family stays in the code.

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
- Tax: flat `coins_per_citizen_per_day` + Tax House bonus (top note); the old 0–100 % rate no longer changes income.

## Code
- `Scripts/Resources/`: `ResourceManager` (store, Add/TrySpend/AddProduced, OnChanged),
  `ResourceStorage` (families, capacity), `ResourceType`, `ResourceNames`, `ResourceDiet`,
  `ResourceAmount`, `ResourceIconLibrary`, `TaxManager`.
- UI: `UI/ResourceHUDController.cs` (chips, reads its real width), `UI/InventoryPanelController.cs`,
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

## Storage rework -- DECIDED by the user 2026-11-17, NOT BUILT (talk only so far)
SANDBOX ONLY. Replaces the family pools with a cap PER RESOURCE.
- Start chain (shown to the player at map start): Town Hall -> Lighthouse -> Склад -> Кладовая -> Сокровищница
  (open from the start, no Lab) -> Crystal Mine (if the map has one). After the chain the stores receive
  wood 40, stone 40, food 20, coins 100 (replaces start_* and the Town Hall's base capacity).
- Caps by level 1..5 (each building of the kind ADDS its cap: two lvl-1 Склады = 100 wood):
  | | Склад | Большой склад |
  |---|---|---|
  | wood, stone | 50/60/70/80/90 | 70/90/110/130/150 |
  | iron, gold, copper ore; coal | 20/25/30/35/40 | 40/50/60/70/80 |
  | iron, copper, gold bars | 10/12/14/16/18 | 20/25/30/35/40 |
  Кладовая food 25/30/35/40/45, Большая кладовая 50/60/70/80/90.
  Сокровищница coins 200/250/300/350/400, Большая сокровищница 400/500/600/700/800.
  Crystals: no cap. The `Grain` family and resource go away.
- Full: the bar shows «10/10» blinking red (top bar + inventory); a notice «Постройте больше складов или улучшите
  старый» at most once per 2-3 min PER BUILDING KIND (склад / кладовая / сокровищница).
- Purchases, gifts and rewards may overfill a store; production stops until the stock drops below the cap.
- A storage building destroyed: its resources are lost, with a visible «minus» effect on the inventory; restoring
  the ruin gives back half of them.
- Islands: one shared store for the whole archipelago.
- The chain buildings are FREE (the user, 2026-11-17).
- Food stays 25 at lvl 1 on purpose: the player starts with 5 residents; growth must be hard (the user).
- Each storage building holds ITS OWN amounts. Tapping a store (or any building holding resources) shows, next to
  the «!», a round INVENTORY button: what this very building holds. Destroyed with 50 wood inside -> the player
  loses 50 wood; restoring its ruin gives back 25; fractions round UP (in the player's favour).
- Overflow (purchases, gifts, rewards past the caps) lies «in the Town Hall»: never lost with a store, spent first.
- Demolishing a store by hand: its contents move to the other stores as far as they fit, the rest is lost; a
  WARNING first («пропадёт 30 дерева») -- concept to show the user before building it.
- A store under upgrade keeps its OLD level's caps until the upgrade ends (no stock thrown away).
- The inventory button by the «!» sits on the six storage buildings only.
- The start chain's buildings are given INTO THE HAND one after another (like the Town Hall and the Lighthouse now),
  not looked up in the build tabs; each, when placed, shows a short line on what it is for («Маяк открывает
  местность», «Склад хранит ваши ресурсы»...) -- texts in the sheet.
- PRE-RELEASE CHECK: prices above the caps (Tax House 150/200, Colosseum 90/80, Big Treasury 110 stone, Big
  Warehouse 80 / Big Granary 70 wood, Manor 55, Moat 25 of a 20-cap resource, research up to 800 coins) -- the user
  re-balances after the review of every building's logic.
- Fill / spend order (agreed 2026-11-17): a new resource goes into the stores nearest to where it was made, one by
  one; a price is paid from the FULLEST store first (stock stays even, losing one store hurts less).
