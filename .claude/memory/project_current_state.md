---
name: project-current-state
description: "Where CityBuilding stands NOW (2026-10-21): project split into feature cards in Design/features/; work happens on TWO devices; Invasion mode in progress; what waits on the user. Short on purpose — per-feature state lives in the cards."
metadata:
  node_type: memory
  type: project
  originSessionId: 00ec56af-41af-4e20-aa76-c5f36bc84d70
  modified: 2026-11-10T13:18:26.178Z
---

**Since 2026-10-21 the project's knowledge lives in feature cards: `Design/features/README.md`
(index) → one card per feature** (status, the user's decisions, code/data/tests, traps, open items).
See [[feedback-feature-cards]]. The long chronological notes that used to be here are frozen in
`Design/features/archive/` (in git).

**Two devices.** The user also works from another machine (paths like `G:/Fork/CityBuilding`); on
2026-10-21 origin had 55 commits this machine had never seen. `git fetch` at the start of a task and
read new commit messages; the cards were brought up to `c6627577`.

**Latest (2026-10-20/21, mostly from the other device):** Invasion mode («Нашествие») started — its
own log `Design/Invasion.md`; pen-sketch placement look is the only one; roads/fences piece by piece;
1×1 gate on roads; ore veins; node stocks; citizen names + citizens tab; Maps 1–2 deleted, Map 3
redrawn; new main menu with mode cards and a live scene; easy start.

**Invasion mode is the current focus (user, 2026-10-21).** Start from the invasion-mode card and
`Design/Invasion.md` («Дальше по плану»). Steps 3 (orc WAVES, `541336bf`) and 4 (the
JUNGLE: wolf/bear/troll camps, giant boss, shrines, `09a8a667`) and 5 (ROUND ORC CAMPS by a scheme
the user approved -- their drawing is not in the repo -- `9722bf8a`) done 2026-10-21; the numbers
were picked by eye and nobody has played them yet. Next planned step: score and leaderboards. `git fetch` first — the other device works on it too.

**2026-10-23 playtest 6:** the Invasion mode's balance moved onto its own sheet tab `invasion`
(gid 1941808734; run config `BalanceConfigInvasion`, rules in the invasion-mode card «Balance»).
Every run-only key now lives there, NOT in economy -- add new mode keys to that tab. Published as CSV
and wired into `BalanceSheetSettings.invasionCsvUrl`; Pull fetches it with the other tabs.

**2026-10-25 (`dede8a36`):** the SANDBOX now runs on the run's systems -- houses make people, heroes
for coins in the Town Hall, timed build/upgrade, repair by damage, no decay/happiness/migration, the
run's card + minimap; card `sandbox-run-systems`. Its balance tab `sandbox` (gid 291742027) was made
through the Google Sheets connector ([[feedback-sheet-via-api]]), published by the user and pulled
(`4550c54b`); verify_sheet: 0 differences on all 7 tabs. Next: playtest build times and hero prices.
Then (`5682ab3f`) ONE FOOD COUNTER (bread -> Food, new Meat chain, Orchard) and the inventory (round sack
button): cards food-and-hunger, production-chains, resources-and-storage. Photos sent, not reviewed by the user yet. The user's old local regenerated assets sit in `git stash` (pre-pull).

**2026-11-03:** pulled 190 fork commits (archipelago, ships, dungeon, moat, bot kingdom, food/tax reworks).
Then 6 library variants per island kind + orc island never turned (generator v3, `108a94e8`/`4aac7129`;
card archipelago-and-ships). The fork's unpulled buildings tab was pulled (`c235fc09`): verify_sheet 0 diffs, EditMode 461/461. Four level-1 dungeons (`f83b0de2`, card dungeon): each mouth of a v3 map leads to its own; looks not reviewed by the user. Design/GDD.md is behind the fork's changes.

**2026-11-04:** Dungeon level 2 «Затопленные гномьи чертоги» (card dungeon): plan dungeon2_plan.py
(more water, wading passages, blob halls) approved; Blender map dungeon2.py; in the game as an EMPTY
walk-through (`59f0caff`/`d3fff818`): level-2 mouths -> Dungeon2, all three mouths on the start
island, no monsters/crystals (POPULATED=False), lever door not wired. User is to walk it in the game;
next: monster/boss models for approval. Possible game title: Kingdom Craft 3D (GDD).
Then the user's 11-screenshot playtest of level 2 was fixed (card dungeon, «Playtest round 1 of
level 2»): bridges from built geometry with ramps, knee-deep slower wading + rings, buoy marker,
levers + door (throne walled off), Maps.DungeonWaypoints for long routes (CalculatePath goes
PARTIAL on long detours in big dungeons), fog cheat; town time stays paused (user's choice).
User has not seen it yet.

**2026-11-05 pull (74 commits from the other device, `d7cd017a`):** dungeon level 2 got monsters/bosses,
playtest rounds 2-3 (NO bridges now -- every crossing a ford), variants Dungeon2B-D; dungeon level 3
«Огненные глубины» complete (+3B-D); mouths of levels 2-3 back on far islands; bot war + bot town
templates; ruins (sandbox); army roster widened (crossbowman, knight/Stable, monk, Workshop + 4 siege
machines), army control by squads stages 1-2 (Garrison, 5 squads, squad window). CLAUDE.md: few
builds/tests (see its rule). Local churn stashed as stash@{0} before the pull.

**Waiting on the user (each also sits in its card):** what closing a portal should buy and the raid
roster per portal level (orc-raids-and-portals); GPU instancing (performance-mobile); zoom floor
5 → 8 m (camera-and-input); meat/fruit chains (production-chains); Invasion next steps (invasion-mode).

**2026-10-21 (`0e2ae107`):** SetupProject.cs and OrcRaidManager.cs were split into partial files by
feature (map in the build-and-test card). If the other device had uncommitted edits in those two
files, its next pull conflicts: re-apply the edit to the method in its new file.

Nothing since the main orc base has been on a phone. Blender `.blend` files are outside git.


2026-11-05 (later): TUTORIAL. Map Isl-tutorial1 approved (round sea, islets, sea rocks) and exported; game mode stage 1 of 5 built and pushed (811e631c): menu «Обучение» +25 tag, Archipelago-tutorial map, systems off. Not seen by the user, no tests run. Stage 2 (rules) built and pushed 7d80b4f1. Stage 3 (step script, King's hints, camera focus) built 26032e04. Stage 4 (orcs, portal win, +25 once) built 7f02915c; full test run green (EditMode 461, PlayMode 367 passed / 18 skipped) incl. new TutorialTests. The user played it once (2026-11-05) and gave a long list; all of it is BUILT (cut HUD, blinking build button/arrow/locked cards, gold rings, zoom, orc barge from their island, orc HP x3, 5-s gathering, King strolls) -- NOT played yet. EVERY tutorial number now on the sheet's own `tutorial` tab (gid 1105001, Balance/tutorial.csv); the user's rule: playtest changes are tutorial-only (the floating Shipyard and the deselect-on-boarding were the two whole-game fixes they asked for). Details in Design/features/tutorial.md.

**2026-11-10:** on-device performance survey done on the Pixel 6 Pro (60 fps everywhere, phone at low
clocks; scripts 3.4 ms the largest CPU item; L4 orc camp the heaviest view, 150 shadow casters; steady
3–5 KB/frame GC). Next asked of the user: a weak phone to measure on. See the performance-mobile card.
Redmi Pad (Helio G99, 4 GB) measured the same day: Auto=Medium gives 21 fps, Low 54 fps (GPU-bound);
the tier-applies-only-after-restart bug FIXED and Auto now gives Low under 4 500 MB (verified on
the tablet). Later that day: graphics settings Auto/Manual with kid-plain texts; A71 and Redmi
Note 14 measured -> presets changed (Medium native+MSAA 2x, Low native, no MSAA) + link.xml for
the stripped CapsuleCollider -- NOT yet built or seen on a phone (next setup also makes
Assets/_Project/link.xml.meta: commit it). APK download ~190 MB: the user chose Google Play Asset
Delivery before release, no mesh slimming. Next: CPU profiling (54 fps ceiling, battles).
Same day, sandbox screen redone as «Знамя и доска» (season picture tabs, saves rows with theme glyph
/ day / people / buildings / code, tutorial reward crystal icon, banner sways when tapped), then its map
became an INK CHART drawn live from the map code (InkMapPainter + cells-only ArchipelagoPreview library;
see hud-and-ui card). None of it seen by the user in the game yet -- only the PNG RunLogs/ink_map_42.png.
Later 2026-11-10: the sandbox chart is plain (no markers); sheet Pull no longer freezes the Editor;
BannerKnock fixed (must stay enabled); tutorial reward on two lines; LOADING SCREEN REDONE: painted
island map (colour, per screen shape) + ship on a course, 10 s in / 5 s out (economy keys), 30 tips
#tip_1..30 (17-30 empty for the user). Art: Tools/loading/loading_maps.py. Not yet seen by the user in game.
Then the sandbox chart was repainted in the loading map's coloured look (still live from the code,
InkMapPainter, ~96 ms paint); also not yet seen by the user in game.
Store icons (card store-listing): flat (make_icons.py) + 3D (render_icons_3d.py) orc and king; the
game's icon is the 3D orc for now (AppIcon.png), orc vs king left open by the user.

- 2026-11-10: rate-us window BUILT (store-listing card): RateUsWindow.Show() from anywhere; TEMP hook shows it on opening settings for the user to test; the real show rule waits on the user; remove the hook then.

- 2026-11-11: balance sheet split into 36 Russian-named tabs + «Оглавление» (card balance-and-localization; Balance/sheet_tabs.csv maps tabs to the 9 CSVs). Old economy/localization/invasion/sandbox tabs hidden as «СТАРЫЙ …», read by nothing.
- 2026-11-11: monetization direction decided (card monetization). BEFORE RELEASE: crystals must survive a reinstall (cloud save) -- the user asked to remember. Sandbox offer bundles (3 rotating, ads or crystals) concept in the card, being discussed.
- 2026-11-11: sandbox OFFER SHOP BUILT and pushed (card monetization, «Offer shop: how it is built»): sheet tabs Наборы / Наборы: уровни / Наборы: настройки / Тексты: лавка, Scripts/Shop/, SetupProjectOfferShop, art by Tools/store/offer_shop_art.py. Not seen on a phone yet; numbers are guesses for the user to tune; ads are a 2-s stand-in.
- 2026-11-11: offer shop round 2 pushed (close seal, crystal counter + red shake, no-ad -> crystals only, red timer, pop on change). Built (setup x2 ok). Received effect = goods fly IN the shop window to the bar chip / sack / crystal counter (OfferRewardFlight). Not seen in play yet.
- 2026-11-11: offer shop round 4 pushed: FOUR offers (goods, goods, crystals for an ad, crystals for money = stand-in), 1 crystal = $0.01, ad = 10 crystals, auto bonus % badge; «Наборы» rebuilt one family per row x 5 level groups. Built, OfferRotationTests green, not seen in play. Goods' crystal prices untouched until the user tests.
- 2026-11-11: «Не хватает ресурсов» built and pushed: picking an unaffordable building in the sandbox opens a one-offer shop window with exactly the shortfall (priced by «Цены ресурсов» -> shop_price10_*), goods past the storage cap. «Наборы» got formula columns lN_worth/lN_bonus/lN_ads; worth > 2 ads = crystals only. Lighthouse level disc waits on the user.

2026-11-11 (later): playtest fixes to «Не хватает ресурсов» (wider ribbon, offer on failed Upgrade, none for a building waiting on another) pushed 5468d1cb5; HINTS built and pushed dd71223b1 (card Design/features/hints.md): padlocked build cards + arrow/blink leading to the needed building / research / «Улучшить». Neither seen in play yet.
2026-11-11 (later): decided, not built -- rate-window show rules (store-listing card), daily login bonus 10-10-10-10-50-10-10-10-10-99 with ×2 for an ad (monetization card); «online only» idea POSTPONED indefinitely (monetization card). Crystal icon = variant 4.
2026-11-11 (latest): rate-window show rules BUILT (UI/RateUsDirector.cs, rateus_* on «Наборы: настройки»; temp settings hook removed); DAILY LOGIN BONUS BUILT (UI/DailyBonus.cs + DailyBonusWindow, art Tools/store/daily_bonus_art.py, concept approved; shown in the main menu at the day's first calm moment; cheat «Ежедневный бонус: снова»). The user's WHOLE-GAME RULE: crystal amounts = number LEFT, icon RIGHT (hud-and-ui card lists where). None seen on a phone yet.
2026-11-12: BOOSTERS being designed with the user (monetization card, rounds 1-5): 12 without tiers, 4 rotating shop slots, 15-min recharge; Рудокоп OPEN (mines' mechanics unsettled) -- not to be built yet. Nothing built.
- 2026-11-12 (later): BOOSTERS design done in concept (card monetization.md): shop tab, «Запасы» 6-a-row, HUD row under the resource bar with «День» moved onto the minimap (sandbox only), VIOLET wind stream on boosted buildings (approved), hints + triggers agreed, sheet tab «Песочница (Лавка): Бустеры» (gid 2000065) made, icons v1 sent for approval. OPEN: housing overflow does not send people away in the sandbox (asked); nothing implemented.
- 2026-11-12 (end): boosters icons v2 (sign + violet glow, Новосёлы from villager FBX) + inventory APPROVED; sandbox housing shortage sends people away (crowded_leave_interval_sec) BUILT, not seen. Boosters design complete; implementation not started.
