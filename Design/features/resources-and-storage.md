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
  (Valuables). The Big ones are opened in the Lab; the «needs the ordinary one» rule was REMOVED 2026-11-17 (the user; it was
  my own rule of 2026-08-16, the sheet column `requires` cleared). Levels 1-5 each (`storage_capacity.._5`), sandbox prices
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

## Storage rework -- DECIDED 2026-11-17, BUILT IN CODE 2026-11-17 (no setup/test run yet, user not seen)
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

### How it is built (2026-11-17)
- `Resources/StoreLedger.cs` (static): which store holds what. ResourceManager's totals stay the truth; every change
  goes through `ResourceManager.Write` -> `StoreLedger.Sync` (gain: nearest stores with room, else overflow; loss:
  overflow first, then the fullest store). `Active` = sandbox (PiggyTasks.InSandbox, not a dungeon, not
  UnlimitedStorage). Caps from the sheet tab «Склады: лимиты» (`Balance/storage.csv`, gid 2000070:
  building, resources, cap_1..cap_5); `GetCapacity` = sum of store caps (none = 0). A store still building holds
  nothing (`BuildingInstance.StorageReady`); an upgrading one keeps its level (the level changes at the end).
- Gains with a place: `AddProduced(type, n, at)` (workshop, gatherer's yard, tapped tree); `AddOverCapacity(type, n, at)`.
- Burnt store: `StoreLedger.OnStoreLost` (red «-N», `ResourceGainPopups.ShowLoss`, `ResourceManager.DropKept`), half
  rounded up into `BuildingRuin.HeldStock`, given back in `TryRestore`. Hand demolish: `OnStoreDemolished` (before
  the refund); `WouldLose(store)` is ready for the warning.
- Saved: `BuildingEntry.stock`, `RuinEntry.stock`, `GameSaveData.storesSaved/storeOverflow`; an older save lays its
  totals into the stores nearest the Town Hall (`StoreLedger.Restored`).
- Start chain: `BuildingPlacer.TryBeginChain` (replaced the lighthouse-only flow; `ChainIds`, `ChainLink`, `_freeNow`);
  Lighthouse on the archipelago only; Crystal Mine when `Wallet.CrystalMineOwned`; a loaded game gets missing links
  free but no reward. The sandbox starts at 0 (ResourceManager.Start); reward keys `chain_reward_*`; hint keys
  `#chain_hint_<id>` (the lighthouse keeps `#hud_place_lighthouse_hint`); Treasury `starts_unlocked` = 1.
- HUD: `ResourceHUDController.IsFull` + blink; notices `#store_full_<Materials|Food|Valuables>` every
  `store_full_notice_sec` (150) per kind; inventory card shows «n/cap» and blinks. Per-store card
  `UI/StoreInventoryCard.cs` + sack button right of «!» (SetupProjectHud `StoreInventoryButton`,
  SetupProjectPanels `BuildStoreInventoryCard`).
- Grain: not kept by any store, Barn/BigBarn hidden, nothing makes it; the enum value stays (saves store ints).
- Demolish warning APPROVED and BUILT 2026-11-17: the confirm adds a red line `#demolish_store_loss`
  («Пропадёт: 30 дерева — другим складам некуда их взять») from `StoreLedger.WouldLose`; no line when all fits.
- OPEN: first setup + playtest;
  base_capacity_* / start_* still serve the campaign and other modes.

## Playtest 2026-11-17 (built, not seen)
- The top bar and the inventory show «amount/room» ONLY when the store is full (was: from 80 %) --
  `ResourceHUDController.Format`, used by `InventoryPanelController` too; EditMode ResourceHUDFormatTests updated.
- A store card of ONE resource (Кладовая, Сокровищница) puts its icon and number in the card's middle
  (`StoreInventoryCard.Centre`); more resources keep the 3x3 grid.

## Ore icons -- look D IN THE GAME (2026-11-18)
- The user: the ore icons look bad (flat coloured blobs from `render_icons.res_*_ore`). Concepts
  `Design/ui/OreIcons_Concepts.png` (made by `Tools/blender/ore_icon_concepts.py`, the icon set's own angle/light):
  A «Глыба с жилами» (grey crag with ore crystals, like the mines on the map), B «Самородки» (heap of lumps),
  C «Короб руды» (wooden crate full of lumps); iron = rusty red + steel, copper = orange + green patina, gold, coal.
- Round 2 (the user: the lumps looked like flat cakes, «лепёшки»): every lump is a SOLID chunk now
  (`nugget`: jittered icosphere, flat facets, some facets in the second colour); D «Крупный кусок» added.
  Once one is chosen: put its builder into `render_icons.py` (Res_Iron, Res_CopperOre, Res_Gold, Res_Coal) and re-render.
- The user chose D (2026-11-18) but not the grey (iron) and green (copper) facets: four facet colours each in
  `Design/ui/OreIcons_D_Colours.png` (`ore_icon_concepts.py -- <out> D2`); waiting for the pick.
- CHOSEN and IN THE GAME 2026-11-18: look D (`details.ore_chunk`, `render_icons._ore`); facets: iron «4 алый»
  (roof_red_dark + roof_red), copper «1 тёмная медь» (roof_orange + roof_orange_dark), gold + plaster_yellow, coal
  iron + window. Res_Iron/Res_CopperOre/Res_Gold/Res_Coal.png re-rendered. The mines' ore heaps on the map are unchanged
  (`ore_heap`, would need model approval + re-export).
- MAP HEAPS (2026-11-18, the user asked for renders): `details.ore_heap` and the minecart load are solid chunks now
  (`ore_chunk`, subdiv 0 = 20 tris each); iron ORE red (roof_red_dark + roof_red, was blue-grey roof_slate), copper
  PATINA dark copper (was green), gold glint plaster_yellow -- mines lvl1/2 scripts (3-5 borrow them), Smelter, Warehouses.
  Renders `Design/ui/OreHeaps_Review.png` (`render_ore_review.py`); APPROVED 2026-11-18 and RE-EXPORTED: the 7 buildings x 5 levels
  rebuilt in Buildings-Size1-Models.blend and their FBX written (needs a Unity setup to show).
