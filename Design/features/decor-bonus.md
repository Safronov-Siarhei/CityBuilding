# Decor's money and gift bubbles

> **Status (2026-11-12): BUILT, setup green, untested in play, the user has not seen it.**
> **Read with:** [resources-and-storage](resources-and-storage.md) (the tax it rides on),
> [sandbox-run-systems](sandbox-run-systems.md) (houses make people), [balance-and-localization](balance-and-localization.md).

## What the player sees
- A decoration (tree, bush, garden, flag, town square, church, tavern, theatre, colosseum) gives every
  person living in a house inside its radius extra coins a day. Its card: «Жителям домов в радиусе N
  клеток: +X монеты в день каждому»; a house's card: «Уют: +X монеты в день каждому жителю».
- While placing: a decoration shows «+X монеты × N дом.» (houses it would reach), a house shows
  «+X монеты жителю» from the decor round that spot. The radius disc shows as for any service building.
- Now and then a round bubble with a coin (rarely a crystal) and «+N» bobs over a decorated house;
  a tap takes it.

## Rules (the user's decisions, 2026-11-12 -- don't re-ask)
- Different decor = different radius and different money: a flag/tree little, a theatre a lot and wider.
- Several decorations reaching one house ADD UP (cap `decor_coins_cap_per_resident` per resident).
- Gifts: coins, rarely a crystal; the bigger the house's bonus, the more often; a house without decor
  gives none; one waiting gift per house.
- Which buildings: the user ticked town square, tavern, theatre, colosseum, church; the small decor
  (tree, bush, garden, flag) has small numbers too because the user said «флаг или дерево — мало».
  Square (the 2x2 road) gives nothing. Any of them can be zeroed on the sheet.
- Every number on the sheet.

## Data
- Sheet tab «Песочница: Здания (Декор)» (gid 2000077): `service_radius(_2.._5)` (cells) and
  `resident_coins(_2.._5)` (coins a day per resident) beside the prices. Sandbox only (the Invasion
  config has none, so decor gives nothing in a run).
- «Песочница: настройки»: `decor_coins_cap_per_resident` 1, `decor_gift_seconds` 240 at
  `decor_gift_bonus_ref` 0,2 (interval = seconds x ref / bonus, min `decor_gift_min_seconds` 60),
  `decor_gift_coins_base` 3 + bonus x residents x `decor_gift_coins_per_bonus` 5,
  `decor_gift_crystal_chance` 0,03 -> `decor_gift_crystals` 1.
- Texts «Тексты: здания и ресурсы»: `#decor_card_gives`, `#decor_card_house`, `#place_decor_gives`, `#place_decor_house`.
- Residents of a house = its live residents (`HousePeople.CitizensOf`, since 2026-11-12 residents are bound to houses); places only when the ledger is absent.

## Code
- `Citizens/DecorBonus.cs` (pure statics: PerResidentAt, DailyCoins, HousesReachedFrom, Coins format).
- `Resources/TaxManager.CollectTaxes` adds `DecorBonus.DailyCoins()` (x the Казна booster) to the day's tax.
- `UI/DecorGifts.cs` on GameManagers (SetupProjectCityScene wires the coin/crystal icons): one overlay
  canvas (order 4), pooled buttons over houses, hidden while a modal is up. Gifts are NOT saved.
- `BuildingLevelStats.residentCoins` (BuildingData), imported optional by `BalanceImporter` (`resident_coins`).
- Card: `BuildingInfoPanelController.DecorLine` (the sandbox band). Placing: `PlacementHudController.UpdateDecorBonus`.

## Open
- Not seen in play: bubble size/height over each house model, tap reliability over world taps.
- Tavern/theatre/colosseum still carry the old `happiness` column (unused while contentment is off).
