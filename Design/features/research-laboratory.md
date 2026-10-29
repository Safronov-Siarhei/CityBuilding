# Research and the Laboratory

> **Status (2026-10-29):** window v2 after the user's final mockup: rows of level pictures, tabs
> Здания/Армия, coins + crystals top centre, PARALLEL research (one per scientist), bubbles, own
> upgrade/repair windows; levels 3-5 cost an UPGRADE CRYSTAL; the Barracks opens soldier tiers.
> **Read with:** [building-levels-and-decay](building-levels-and-decay.md), [army-and-soldiers](army-and-soldiers.md).

## Rules (the user's spec — do not re-ask)
- Tapping the Laboratory opens the RESEARCH WINDOW itself (not the building card). User's sketch
  (2026-10-28): plaque «Лаборатория: уровень N» between tabs «Здания» / «Армия» (chosen tab x1.12 + lit);
  one ROW per building in build-menu order (category, then menu order) / per soldier tier: name plaque,
  level pictures 1..5 (1..3 soldiers) joined by a line, price (coins + crystal) and a button under each.
  Under the title ONLY the coins and upgrade crystals (no status line). States: had = tick; under way =
  its own timer bar + «Отмена»; waits on previous = grey «Прокачать»; lab too small = grey «Лаб. ур. N»;
  next step = green (grey but tappable when blocked). Tap a picture = a BUBBLE over it with what the
  level gives (DescribeGain); a refused start shakes the button and flashes the coins/crystals counter
  red, or says why in the bubble. Foot: scientists -/+, «Улучшить лабораторию» and «Отремонтировать»
  (each opens its own small window: picture, what it gives / health, price, confirm), «Закрыть», «!».
- A building with no research topic has no row: Flag and decor (DecTree/DecBush/DecGarden) had their
  research zeroed (user: not in the lab), so their levels are free; the Laboratory itself has none.
- UPGRADE CRYSTALS (user, 2026-10-28): levels >= `research_crystals_from_level` (3) cost
  `research_crystals` (1) on top of the coins (`ResearchTopic.Crystals`). Account purse
  `Wallet.UpgradeCrystals` (PlayerPrefs, kept between games, sandbox only -- the lab is sandbox only);
  cancel / lost lab refund it in full; saved as `currentResearchPaidCrystals`; infinite-resources cheat
  makes them free; cheat GameCheats/Research «+10 кристаллов улучшения». Separate from the blue shop
  crystal `Wallet.Crystals`. Icon `UI/Icons/Res_UpgradeCrystal.png` (render_icons.py, violet shards on gold).
- Cost in coins (+ crystal from lvl 3), taken at start; time = the topic's own real seconds (floor
  `research_min_seconds`). Cancel refunds 70 % of the coins and the whole crystal.
- PARALLEL (user, 2026-10-28, answer Б): each scientist runs ONE research -- as many at once as
  scientists (`FreeSlots`; blocker `#research_blocked_slots`). Fewer scientists than researches = the
  newest stand still (`IsPaused`), not lost. Scientists no longer speed research up (economy
  `research_seconds_per_worker`/`research_free_workers` are now unused). User's principle: no wall on
  upgrades -- the game is about growing the settlement.
- Lab level gates: L1 opens buildings and soldier types; level N permits level N of anything. Lab's
  own levels are NOT gated.
- Lose the Lab → nothing researched is lost; an interrupted research refunds in full and resets.
- Two kinds of building gate: built first + levels researched later, or researched before building.
- 18 buildings open from minute one: Ратуша, Колодец, Лачуга, Дорога, Лаборатория, Склад, Кладовая,
  Городская площадь, Декор. дерево, Декор. куст, Флаг, Забор, Ворота, Казарма, Лесопилка, Каменоломня,
  Фруктовый сад, Рыбацкая хижина. Мост and Сокровищница are closed.
- Soldiers: the BARRACKS' level opens each tier (units `barracks_level`), the lab only researches
  levels 2/3 (user, 2026-10-28: no double key -- every tier starts_unlocked=1, no unlock research).
  A researched level upgrades soldiers already recruited. Heroes are NOT in the lab (XP levels).

## Code
`Research/ResearchManager.cs`, `Research/ResearchCatalog.cs` (category order, skips orc rows),
`Research/ResearchTopic.cs` (`RequiredLabLevel`, `ResearchKind`, `Crystals`), `UI/ResearchPanelController.cs`, `Core/Wallet.cs`,
`Cheats/ResearchCheat.cs`. Built in `Editor/SetupProjectPanels.cs` (`BuildResearchPanel`).

## Data
buildings/units: `research_coins(_N)`, `research_sec(_N)`, `starts_unlocked`; economy
`research_seconds_per_worker`, `research_free_workers`, `research_cancel_refund_percent`, `research_min_seconds`,
`research_crystals`, `research_crystals_from_level`. Pictures: `UI/Icons/<id>.png`, `<id>_L2..5.png`, `Portrait<SoldierType>.png`.

## Tests
EditMode `ResearchTests` (crystal price, DescribeGain); PlayMode `ResearchGateTests` (`EachScientist_RunsOneResearch_SideBySide`, `ALevelThree_TakesAnUpgradeCrystal_AndCancellingGivesItBack`, `Photograph_TheLaboratoryWindow` -> lab-window-buildings/army/hint/upgrade/bubble).
Save: `GameSaveData.runningResearch` (list); the old single `currentResearch*` fields are read for old saves only.

## Traps
Keys `#research_start/_cost/_cancel/_lab_state/_empty_lab_level` are no longer shown (kept: LocalizationTests
list them). Ships go into the Армия tab later (3 levels, see archipelago memory).
