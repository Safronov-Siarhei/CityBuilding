# The sandbox on the run's systems (people, heroes, timed building, repair, UI)

> **Status (2026-10-25):** in code, tests green (`PlayMode/SandboxRulesTests`). The sheet's `sandbox` tab
> (gid 291742027) was created and filled THROUGH THE SHEETS API (Google Sheets connector; the user:
> sheet work only via API/MCP, never the browser), with header notes; `repair_cost_fraction` deleted
> from the sheet's economy tab. The tab is NOT PUBLISHED yet (the API cannot publish): until the user
> adds it under File -> Share -> Publish to web, Pull skips it and `verify_sheet.py sandbox` gets HTML.
> Not played on a phone.
> **Read with:** [invasion-mode](invasion-mode.md) (where every system came from),
> [balance-and-localization](balance-and-localization.md), [hud-and-ui](hud-and-ui.md),
> [building-levels-and-decay](building-levels-and-decay.md), [save-load](save-load.md).

## What the user decided (2026-10-25)
- The sandbox takes over from a run: people made by houses, heroes, timed build + upgrade, repair by
  damage, the run's building card and hire bar, the run's minimap (close, following the camera).
- People: every house (Лачуга, Коттедж, Особняк) makes people up to its level's housing_capacity, one
  every `person_respawn_sec`; **not the Town Hall** (its founding party just lives there; its
  housing_capacity is 0 on the sandbox tab). Migration moves nobody (`MigrationManager.MovesPeople`
  = false; the settling-in grace still runs -- sickness waits on it).
- Decay is gone (`BuildingInstance.DecayEnabled` = false); repair = what the building is worth
  (price + paid upgrades, every material but coins) × `repair_cost_fraction` × missing health share.
- **All three heroes are bought for coins** in the Town Hall, the King too (`king_price`,
  `<id>_castle_level` on the sandbox tab). A run still gives its King free.
- Contentment is off (`HappinessManager.InUse` = false: never computed, its number off the HUD);
  the hero portraits stand where it was. Taxes and food stay.
- A separate sheet tab `sandbox` with comments for all of it (prices, upgrades, build times,
  housing, hero/skill/people/repair/minimap keys).

## How it works
- `Balance/sandbox.csv` = the `invasion` tab's shape: notes in column A, a `building` table
  (cost_*, upN_*, build_sec(_N), housing_capacity(_N), comment), a `key` table. `BalanceImporter`
  (`OverlayTab`) lays it over buildings/units/economy for `BalanceConfig.asset` WITHOUT wholeRow (a
  column the tab lacks keeps the buildings tab's cell); its keys win over economy's and over the
  invasion keys the sandbox config also carries. The run's config is untouched by it.
  `repair_cost_fraction` moved OUT of economy (sandbox tab / invasion tab each have their own).
  Build times were set by formula (L1 = 4 + 0.25×price, upgrade = 6 + 0.15×price; Road 1 s) --
  `Tools/balance/make_sandbox_tab.py` made the first CSV from buildings.csv + invasion.csv (it OVERWRITES
  sandbox.csv -- once the sheet has the tab, the sheet is the source).
- New keys read by both configs: `person_respawn_sec` (else `inv_person_respawn_sec`),
  `minimap_radius` (else `inv_minimap_radius`) -> `BalanceConfig.PersonRespawnSeconds/MinimapRadius`.
- `BuildingPlacer.PlaceAt` -> `BuildingInstance.BuildOnTimer` (all but the mandatory Town Hall);
  `TryUpgrade` -> `ConstructionSite.Begin(Data.BuildSeconds(level+1))` in both modes. A building going
  up holds its storage room back until done (`_storageHeld`); `ServiceRadius`/`Happiness` are 0
  while a site stands.
- `Citizens/HousePeople` (was `Invasion/InvasionPeople`) on GameManagers in every game: houses =
  `BuildingInstance.Houses` (housing at L1 or L5 > 0, not the Town Hall); a house first going up has
  0 places; the town's capacity is forced to the houses' places. A run calls `FillNow()`.
  `CitizenManager.RefillWantedJobs` now in both modes.
- `Combat/HeroRoster` on GameManagers: Heroes, HeroOf, CanBuy/Buy (`Offer`), Spawn (free), Restore
  (save). Castle = `BuildingInstance.TownHall`. Heroes are out of upkeep and out of the army panel.
  `InvasionMode.King/Heroes/HeroOf/CanBuy/BuyHero` delegate to it.
- UI: `BuildingInfoPanelController` always shows `RunBuildingCard`; in the sandbox a band opens under
  the workers (what it makes / bare reach in amber / a Плавильня's recipe row) and the card grows by
  110 (`ShowBand`, halves in `Top`/`Bottom` holders). `FocusHireBar` (Barracks soldiers, Town Hall
  heroes), `HeroPortraitController` and `MinimapCameraUpdater` follow in both modes. The army panel
  sits under the shown portraits (`HeroPortraitController.ShownRows/StackBottom`).
  Level pictures `UI/Icons/<id>_L2..5.png` now exist for all 44 buildings with level models
  (render_icons.py with `<Id>1-lvlN` names, renamed).
- Save: `BuildingEntry.siteRemaining/siteTotal/siteUpgrade` (resumed by BuildOnTimer/ResumeUpgrade),
  `GameSaveData.heroes` (`HeroEntry`); hero and summoned groups are no longer written to armyGroups.

## Tests
PlayMode `SandboxRulesTests`: timed build/upgrade via the placer, storehouse room only when built,
repair by damage + no decay, houses make people (not the Town Hall), heroes for coins in the Town
Hall, the card's band, heroes and half-built buildings through a save. Existing tests upgrade through
`PlaytestWorld.UpgradeNow` (TryUpgrade + Finish).

## Traps
- The sandbox's `repair_cost_fraction` and every hero key live ONLY on the sandbox tab now; an
  EditMode test reading `BalanceConfig.Sandbox` sees the tab's values.
- The Town Hall makes no people TWICE over: its housing_capacity is 0 on the sandbox tab AND
  `BuildingInstance.IsHouse` excludes it -- breaking only the code (2026-10-25) left the test green.
- `HappinessManager.InUse` / `MigrationManager.MovesPeople` / `BuildingInstance.DecayEnabled` are the
  three switches -- the code behind them is kept, not deleted.

## Open
The user publishes the `sandbox` tab; then `verify_sheet.py sandbox` and a Pull (the committed CSV
becomes Google's export format); playtest the build times and hero prices; the migration/decay/happiness keys in economy are unused.
