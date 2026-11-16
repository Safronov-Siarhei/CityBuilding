# Piggy bank («Копилка»)

**Status (2026-11-16): BUILT in the game** (the user has not seen it; setup green; the general test run is in the
log of that day). Everything approved went in: the button, the window v2, the «!» popup, the break animation,
the prices, 40 tasks reset every 8 h. Earlier history: rules DISCUSSED 2026-11-15, concepts in `Design/store/Piggy_*`.

## How it is built (2026-11-16)
- `Core/PiggyBank.cs` -- level (0-based) + crystals in PlayerPrefs (`piggy.level`, `piggy.stored`), Add (capped),
  GainPercent (the live badge), OnBroken (Store calls it). Numbers always from `BalanceConfig.Sandbox`.
- `Core/Store.cs` -- products `piggy_1..5` (one per level): CrystalsOf = what is in it, UsdOf = piggy_usd_N, CanBuy =
  the current level's product and CanBreak; Grant pays out then calls PiggyBank.OnBroken.
- `Core/PiggyTasks.cs` -- the 3 tasks of the 8-hour slot (slot = hours since 2000-01-01 local / 8), 1 easy + 1 medium
  + 1 any, different kinds, seeded by the slot; JSON in `piggy.tasks`; Report(kind,n) / ReportMax(kind,v);
  sandbox/archipelago tasks count only in the sandbox (not a run, campaign, tutorial). Dungeon tasks drop after the
  first dungeon entry (`piggy.seen.dungeon`), archipelago after a minute on an archipelago map, campaign_record once
  `m1_best` exists. Islands visited per map in `piggy.isl.<mapId>` (the shipyard's island counts as visited).
- `Core/PiggyRunner.cs` -- made at start, DontDestroyOnLoad: 1 crystal per piggy_minute_seconds of ACTIVE play (scene
  CityBuilder, timeScale>0, no ModalGate, input within piggy_idle_seconds); the sandbox-minute task; orc deaths
  (orc_kill = not a monster; dungeon_monster/boss inside a dungeon; king_kill per run; raid = the last raiding orc fell).
- Where each kind is reported: BuildingPlacer (road; building with no timer), ConstructionSite.Finish (build /
  upgrade / building_level, not the bot's), ResourceManager.Add (coins) / AddProduced (wood, stone, food, bars),
  ArmyManager.TryRecruit (hire / machine / squad) + MoveSoldier (squad), HousePeople + MigrationManager (citizen),
  BuildingInstance.TryRepair, ResearchManager.Complete, Hero.AddExperience (hero_level), OrcCampBuilding.Fall (orc
  building, not walls; cage), OrcRaidManagerCamps.PlunderCamp (orc_camp), ShipManager.TryBuild / TryLand (ship,
  island), CityDefenceMode.Win + MissionOne end (campaign_win, campaign_record), DungeonTravel.Enter (seen, deeper),
  DungeonCrystals.Take, DungeonChests.OpenChest, DungeonShopCard.Buy, InvasionScore.Finish (run), InvasionWaves (wave).
- UI: `UI/PiggyUi.cs` (helpers), `UI/PiggyButton.cs` (the square button: Create(parent, anchor, offset, size)),
  `UI/PiggyWindow.cs` (window, «!», shatter coroutine; Show, OfferInMenu, BackCurrent). Pictures:
  `Tools/store/piggy_art.py <renders>` -> `Resources/Shop/Piggy/` (shards.json is FLAT: 6 numbers a shard).
- Buttons: main menu top right (settings moved top LEFT, language picker top CENTRE), modes screen under the settings,
  campaign map under the settings, main-menu shop top right (its balance plaque moved top left), the sandbox's game
  menu top right, the Лавка top right of the screen beside its frame (OfferShopWindow.Awake; the user 2026-11-16 showed
  the free corner on a game screenshot -- the frame is smaller in the game than in the mock-up).
- Sheet: tab «Копилка: задания» (gid 2000068, `piggytasks.csv`), keys piggy_* in «Наборы: настройки», texts #piggy_*
  in «Тексты: лавка»; task names become `#piggy_task_<id>`. Cheats: Экономика tab (level, fill, window, redraw, finish; row «Добавить кристаллы в копилку» +10/+50/+100/+step/+typed, through PiggyBank.Add so the cap holds).
- Test: `Tests/EditMode/PiggyBankTests.cs` (capacity, break, gain ~+50 % full / none below 2/3, tasks credit).
- Traps: BalanceConfig has a CLASS PiggyTask, so the lookup is `FindPiggyTask` (a same-named method broke compile).
  The stand-in Store grants at once; real billing must answer through Store.Grant so Broken plays the shatter.
  Campaign Win kills every orc -- those count toward orc_kill (accepted). Not checked on a phone.

## Rules (the user, 2026-11-15)
- A PIGGY (pink pig: the shape everyone knows). Corner icon with a shine, the crystals saved on a plaque under it.
  A tap opens its window: banner with the pig and crystals, «Разбить за $X», a line that crystals are saved
  while playing and doing tasks, the daily tasks.
- Filled by: 1 crystal per minute of play (NO daily limit -- long play is rewarded; only active play: not
  paused, not menus, input in the last ~2 min -- proposed) + DAILY TASKS (e.g. play an Invasion battle,
  destroy an orc building in the sandbox, kill 100 orcs), crystals by difficulty; tasks reset every 8 h (was midnight);
  one free/ad swap a day proposed.
- Breakable from 150 crystals. NO reset/expiry (the 7-day timer was dropped).
- 5 levels, each PURCHASE levels it up (and it costs more); level 5 stays forever.
  Capacity 300 / 500 / 1000 / 1500 / 3000.
- A FULL piggy is +50 % crystals per dollar over the nearest regular pack (so it pays to fill it first).
  PRICES APPROVED (the user 2026-11-16): $0.99 / $1.69 / $2.79 / $4.19 / $6.49 = +50 / +46 / +49 / +49 / +54 %
  against the regular pack of the nearest SIZE (300, 500 -> 200 for $0.99; 1000, 1500 -> 1200 for $4.99; 3000 -> 3000
  for $9.99). The pack prices are still stubs: if they change, recompute the piggy prices to keep ~+50 %. The window's
  live badge uses the same reference pack (in code, from the sheet).
  5 Google Play products. All numbers in the sheet.
- Full: no more crystals in; «Полна!» on the icon; a «Разбей копилку» banner on entering the main menu at most
  every 4 h and whenever a task's crystals are lost.
- Break animation: shake + cracks, bursts into shards with a flash and rays, a fountain of crystals flying into
  the crystal counter (it rolls up), «Уровень N», the next-level pig assembles from shards. The pig = a 3D
  model (renders for approval first); the corner icon = its render. Colour per level proposed (pink..gold).
- Not in «Подожди!» (its value is the savings, not a discount).

## Where the icon stands (v2 concept after the user's notes 2026-11-16, waiting for approval)
Top-right corner everywhere, never a card:
1. Main menu: settings MOVE to the top LEFT, the language picker to the top CENTRE, the piggy top right.
2. Modes screen: under settings. 3. Menu shop: top right; the player's crystal balance in the top LEFT corner as a plain
LIGHT number + crystal icon (the beige plaque behind it goes; the user 2026-11-16). 4. Sandbox Лавка: screen top right next to the window.
5. Sandbox menu window: top right (not on the bare map). 6. Campaign map: under settings.

## The icon (v2)
A RENDER of the 3D piggy (`Tools/blender/store/piggy_bank.py <out> [pink|silver|gold]`, glossy ceramic, coin
slot, 5 crystals FLOATING around -- none stick out of it; render `Design/store/shop_art/Piggy_render.png`) on a soft
golden shine with sparkles; the number AT THE PIG'S FEET (light, dark outline; the user 2026-11-16) -- no plaque (the user rejected the
flat drawing, crystals in the slot and the plaque). Full: «Полна!» tag + brighter shine.
Three models were shown (А glossy, Б ROYAL, В blocky; `Design/store/Piggy_Models_Concept.png`): the user CHOSE Б ROYAL
(2026-11-16) -- gold crown with rubies, gold slot rim and hooves.
Concept `Tools/store/piggy_placement_concept.py <photos> <render A> [B] [C]`.

## The button (the user, 2026-11-16; concept `Design/store/Piggy_Button_Concept.webp`; animation APPROVED 2026-11-16)
- Its area is a SQUARE (~200 px in the 1080 reference), the piggy centred, as wide as the settings button (~130).
- Crystals float round it SMOOTHLY and a little CHAOTICALLY (the user 2026-11-16: two waves of unrelated periods on
  up-down, sideways and tilt per crystal) -- in the game they are separate sprites
  (`piggy_bank.py ... gem` renders one crystal), the pig a render without crystals (`*_alone.png`).
- The number at the pig's feet. FULL: «Полна!» tag centred on the button's TOP edge (the user 2026-11-16; 4 crystals -- a 5th by the number was tried and rejected; the top centre left free), the number GOLD and glowing (pulse), the
  pig WOBBLES SOFTLY (0.9 s eased in/out every 1.8 s, ~3 Hz, ±2 px / ±3°; the user: smoother), the shine brighter.
- Script `Tools/store/piggy_button_concept.py <renders> <modes photo>`.

## The window (APPROVED by the user 2026-11-16; `Design/store/Piggy_Window_Concept.png` saving,
`Piggy_Window_Full_Concept.png` full, `Piggy_Window_Info_Concept.png` the «!» popup; `Tools/store/piggy_window_concept.py <renders>`)
RULE (the user 2026-11-16): v1 had too much text and looked off-putting -- the window keeps only what the player acts
on; every explanation goes behind the «!» button.
- Frame of the support banner, ribbon «Копилка», «!» top LEFT corner, ✕ top right.
- Left: the big pig (its material = the level; no level text/pips), floating crystals, the fill bar «245 / 300 💎»
  (gold + «Полна!» tag when full), green «Разбить за $0.99» with a small red badge; full: the button glows.
- The badge = the REAL gain right now (the user 2026-11-16): price per crystal vs the regular pack, i.e. with a full
  piggy at +50 %: count/cap*1.5-1 (245/300 -> +23 %); at or below break-even (2/3 full) NO badge. In the game compute
  it from the real pack price, not the formula. `Piggy_Window_Low_Concept.png` = 170/300, no badge.
- No big front crystals in the field (they fought the pig; `frame(near=False)`).
- ALIVE (the user 2026-11-16; animation `Design/store/Piggy_Window_Concept.webp`): the 4 crystals by the pig bob like the
  button's (2x amplitude) on a pulsing violet-white halo so they stand out; the pig is never still -- saving: a small
  wiggle (0.8 s every 2.6 s, ±2 px/±2°), full: the button's wobble bigger (0.9 s every 1.8 s, ±4 px/±3°).
- Right: «Задания» + clock «07:42»; 3 short task cards (name, bar, +N 💎 / a tick when done -- credited by itself);
  full: the rewards go GREY (no warning text). Task names are EXAMPLES, the list is to be discussed.
- «!» popup (parchment): «Как работает копилка», «Уровень 1 из 5», five icon rows: +1 a minute of play; day tasks add
  more; break from 150; a FULL one is 50 % better than a pack; a purchase opens a bigger piggy; «Понятно».

## Break animation (APPROVED by the user 2026-11-16; `Design/store/Piggy_Break_Concept.webp`,
`Tools/store/piggy_break_concept.py <renders>`)
~6.6 s over the window: 0-0.6 the window dims, the full pig flies to the centre and grows x1.35; 0.6-1.9 it shakes
harder and harder, cracks spread in 3 steps (a tick of light each); 1.9 white flash + golden rays; the pig bursts into
~22 Voronoi shards (spin + gravity) and crystals fountain up; 2.4-4.0 they stream into the crystal counter (top left),
which rolls up (+300); 3.8 «Уровень 2» pops in, bronze shards fly in and assemble the next pig, a flash; 5.1 the dim
lifts, the bronze pig returns to its place: 0 / 500, grey «Разбить от 150» (below 150 the button is grey).
In the game: shards = the render cut on the fly or a pre-cut sprite sheet; the counter = the screen's real balance.

## Levels (APPROVED by the user 2026-11-16, `Design/store/Piggy_Levels_Concept.png`)
Same royal pig, the material changes: 1 pink ceramic (300), 2 bronze (500), 3 silver (1000), 4 gold (1500),
5 amethyst -- glowing purple crystal (3000). `piggy_bank.py <out> <pink|bronze|silver|gold|amethyst> royal`.

## Traps
- GIF bands the pig's smooth shading into lines (256 colours) and a palette per frame flickers: concept animations go as
  full-colour animated WebP. The model itself is smooth -- the lines were never in the render.
- The menu shop does not render in a batchmode capture (its blur/ScreenCapture) -- the concept used its mockup.
