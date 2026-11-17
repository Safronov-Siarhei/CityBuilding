# HUD and UI

> **2026-11-10 playtest (the user, phone):** a tapped event icon keeps its line up `event_caption_sec` (4, economy) -- a finger lifting no longer hides it (`EventIconView`); the HUD ignores the SIDE safe-area insets (`SafeAreaFitter`: left column to the screen edge; top/bottom still kept); the Day plaque sits UNDER the windows (sibling before BuildingInfoPanel); one round cancel button for every selection incl. a citizen (old «Выбран житель ✕» badge unused), pinned to the bottom-right corner; the new-squad button draws `Hud_Plus` (was a blank square); the food chip pulses red while hungry, faster while people leave. Built, not seen.

> **2026-11-11:** left column = menu, citizens, sack, then the offer shop's stall icon with its timer (`ShopButtonTop`, sandbox only; see [monetization](monetization.md)); the event feed's `LeftColumnTop` moved down again.
>
> **2026-10-25:** left column = menu, citizens, INVENTORY (sack; `InventoryButtonTop`), then the event
> feed (`LeftColumnTop` moved down); the run's building card and hero portraits are the sandbox's too
> ([sandbox-run-systems](sandbox-run-systems.md)); contentment's round number is off.

> **Status (2026-10-25):** main menu and the run's building card redone after the user's sketches (below). Earlier (2026-10-21): HUD follows the user's sketch (round 2, 2026-10-14) in style D, with four
> switchable HUD looks (Classic/Soft/Glass/Dock) chosen in settings. Windows (info card, research,
> save, settings) are NOT skinned per look — still parchment.
> **2026-10-31 (playtest):** the «Выбран король» badge sits bottom-right above the taxes; «Запасы»
> text in dark ink (`InventoryPanelController.Ink`); a tapped ORC building gets a RED ring like the
> player's own (`BuildingFocus.SelectForeign`) and its «!» opens `UI/ForeignBuildingCard.cs`
> (owner, level, health, damage/reach; keys `#foreign_*`, `#orc_building_<kind>`); the main menu
> backdrop skips maps without a ground prefab (the archipelago, dungeons) -- that was the lost ground.
> **2026-11-01:** an orc building or a dungeon mouth held hides the build menu like one's own
> (`BuildingFocus.Holding`, read by `BuildingPlacerUIVisibility`), so the «!» stands alone.
> **2026-11-10:** the modes screen's banners unroll from the screen's FIRST faded-in frame and stay rolled up
> while it is away (`ModeBannersController`: was alpha > 0.5, so they showed whole, folded and unrolled again).
> **Rule:** the user SKETCHES layouts; implement their sketch, don't redesign or restyle unasked.
> **Rule (the user, 2026-11-11, the whole game):** an amount of CRYSTALS is the NUMBER LEFT, the crystal
> icon RIGHT; other resources keep icon-then-number. Done in: the top bar's crystal chip
> (`ResourceHUDController.PlaceChip(iconRight)`), the research window's counter and prices
> (`SetupProjectPanels` Counter, `ResearchPanelController.Chip(iconRight)`), the tutorial's win panel
> (`GameOverController.ShowTutorialReward`), the Invasion hub's crystals plaque and leaderboard reward
> (`SetupProjectMainMenuScreens` Counter(discRight), Gem), shop/menu-strip goods (`UI/CrystalOrder.Lay`
> per good), the daily bonus window. Already so: the shop counter and buy buttons, the modes banner's
> tutorial reward, the «+N» popups. A new crystal amount anywhere follows it.
> **Read with:** [art-pipeline](art-pipeline.md) (icons), [balance-and-localization](balance-and-localization.md) (all text).

## Layout (user's sketch round 2 + round 3 fixes)
Menu round button top-left (pauses `timeScale`, hides HUD via `HudHider`; save/settings/home);
`HudHider` (menu, «Запасы», workforce) hides the hero portraits, the hero skill bar, the ship bar and the
dungeon potion bar too (the user, 2026-11-05) -- only the menu, dialogs and panels carry `HudOverlay`;
citizens button under it; event feed down the left edge; one resource bar top-centre on a wood plaque,
«День N» under it (sandbox: on the minimap's foot, `Shop/DayOnMinimap`, lifted 52 px so it clears the screen edge -- the user 2026-11-12; field renamed `liftOverFoot` so the scene's old 22 no longer applies); happiness round number top-right (tap → breakdown), migration + army under it;
minimap bottom-left (round); build menu bottom-centre — CLOSED at rest (tabs only), a tab slides the
card row up, re-tap/map tap closes; replaced by ✕ ⟳ ✓ while placing; tax bottom-right «Налог: 10%».
**No text on buttons — pictures only** (the tax plaque is the one allowed word). Settlement tiers hidden.

## Main menu (user's sketches, 2026-10-25; was the 2026-10-20 cards)
Only the CLASSIC look was designed for (the user: «переделывать будем только под него»); the layout is
one for all four looks, each shows it in its own sprites. The Classic menu is drawn in the HUD's OWN
Classic kit (`Tools/ui/make_menu_classic.py`, reusing make_hud_skins.py; the user, 2026-10-25: the
carved menu wood "changed the style" -- ugly next to the game); make_menu_kit.py now draws Dock/Soft/
Glass only; Classic menu inks in `HudSkinApplier.MenuInk` (dark on parchment plaques). Screens (`UI/MainMenuController.cs`, built by
`Editor/SetupProjectMainMenuScreens.cs`; helpers + dialogs in `SetupProjectMainMenu.cs`):
- main: settings round top-centre, «Продолжить» (= the old Play: to the modes), «Магазин» (opens the code-built
  `UI/MenuShopWindow` since 2026-11-15, card monetization.md; the old ShopDialog is gone) (= the
  shop; was «Предложения», renamed 2026-11-15: same parchment plaque, the word moved left, the in-game shop's
  `UI/Shop/Stall.png` in its right end -- `ShopStall()`, the user's pick «1 — внутри» from
  Design/store/ShopButton_Plaque.png; since 2026-11-16 that icon is concept A «Богатая лавка» from
  Design/store/ShopIcon_Concepts.png (Tools/store/shop_icon_concepts.py, also the sandbox shop button); its «!» badge sits top-LEFT, `Badge(.., left: true)`), Discord + YouTube rounds under them. No resume-last-save plaque any more.
- modes (REDONE 2026-11-10, the user: «дёшево и скучно» -> «Зал знамён»): four BANNERS hanging in a
  row -- Обучение (teal), Песочница (green), Нашествие (red, iron perch, torn tail, emblem = horned orc skull over crossed axes since 11-10 round 2), Кампания (grey,
  lock, «Скоро») -- art `UI/MenuArt/Banner_<Mode>.png` drawn by `Tools/ui/make_mode_banners.py` in
  the HUD's Classic kit (same parchment/wood/gold/hard shadow; one art for all four looks). Round 2 (the user, 11-10): no side folds (read as seeing the banner from all sides), book pages joined at a spine, one tail line for hem/cloth/seam (seam parallel to the torn edge), tassels in the banner's colour under a cap of its perch metal. Name on
  the kit's plaque, a motto and a LIVE line on the cloth (tutorial reward or «Пройдено», saves count
  or «Новое королевство», invasion points today, «Скоро»; keys `#banner_*`). The tutorial's reward is
  «Награда:» centred, under it «25» + the upgrade-crystal ICON centred as a pair (`rewardAmount` +
  `rewardIcon`, placed by `ShowReward`; `#banner_reward` is now just «Награда:»; the user 11-10), centred «Пройдено» once rewarded. Tap = pick (bigger x1.14,
  sways more, others dimmed, remembered in PlayerPrefs `menu.mode`); tap the picked one or «Играть»
  (gold, bottom centre) = start; LONG PRESS (0.5 s, `UI/LongPress.cs`) = the mode's «!» words. They
  unroll from the perch on entry (staggered). `UI/ModeBannersController.cs`, built by
  `SetupProjectMainMenuScreens.BuildModeBanners`. The old pills, round «!» and tutorial plaque are
  gone (MainMenuController.tutorialReward left unwired). Round 3 (the user saw it, 11-10): smaller name plaques (196x54, 26 pt); the MOTTO moved off the banners to ONE line bottom centre over «Играть», showing the picked mode's; the live line sits under the plaque, 190 wide and best-fit (14..26 pt) so big numbers stay inside the seam.
  **CAMPAIGN BANNER OPEN (the user, 2026-11-11: «сделай активным баннер кампании… в релиз это пойдёт»):** picking it is remembered like the others, «Играть» is no longer dimmed for it, and it starts `LoadingScreen.IntoGame("CampaignOpening")` (the opening film, then mission 1; the scene is now in the build list, SetupProject). Its live line is `#banner_campaign_line` «Глава 1: Бегство» (was «Скоро»; sheet «Тексты: интерфейс и меню» row 142). Not seen by the user yet.
  **Campaign banner ART open too (the user, 2026-11-12: «он серый и не активный»):** it was still drawn grey with a LOCK in the medallion; now royal blue cloth, gold seam, finials and ring like the wooden banners, emblem `shield_crest()` (2026-11-12, the user chose variant 5 of `Design/campaign_emblem_variants.png` and asked for it drawn richer): a heater shield in a gold rim with rivets, quartered field in two blues, a gold boss with a small crown, crossed swords with fullers, curled guards, wrapped grips (a sword-behind-scroll first version was rejected). The SANDBOX and TUTORIAL emblems drawn as rich too (the user, 2026-11-12: «более дорогими»): `castle_rich()` -- two-tone stone with block lines, terracotta cone roofs, a wooden gate under a gold portcullis, a red pennant; `book_rich()` -- a leather-bound open book with gold corners, shaded pages with text lines, a red ribbon, a jewelled gold crown. Close-ups: `Design/mode_emblems_close.png`. ALL FOUR emblems APPROVED by the user 2026-11-12. `make_mode_banners.py` has no grey/locked look any more (`lock()` kept, unused). Lineup: `Design/campaign_banner_lineup.png`. Not built/seen in the game yet.
- modes screen v2 «ЛЕНТА ПО КРУГУ» -- AGREED with the user 2026-11-16, BUILT the same day (compiled outside Unity,
  NOT run, not seen: the user's Editor was open). Code: `UI/ModeBannersController.cs` (mode constants 0-5 = Tutorial,
  Sandbox, Invasion, Campaign, Online, Workshop; `Order` = the row; slot tables SlotX/Scale/Light/Alpha/Drop by distance
  0..3; drag via IBeginDrag/IDrag/IEndDrag on the modes root + a transparent `SwipeArea` band behind the banners;
  last played in PlayerPrefs `menu.mode.played`, default Tutorial; locked = Online/Workshop: «Играть» greyed);
  built by `SetupProjectMainMenuScreens.BuildModeBanners` (ArrowLeft/ArrowRight: Glow, Twin, Chevron; the right root
  mirrored by scale -1); art `UI/MenuArt/Banner_Online|Workshop.png`, `Chevron.png`, `Glow.png` from
  `Tools/ui/make_mode_banners.py` (LOCKED set draws the gold lock); texts `#mode_online|workshop`,
  `#banner_motto_online|workshop`. Concepts:
  `Design/ui/ModeWheel_3_Row.png` (+ `_Sandbox`), arrows `ModeWheel_Arrows_A_Anim.gif` / `ModeWheel_Arrows_A.png`,
  drawn by `Tools/ui/mode_wheel_concepts.py` (variants 1 carousel, 2 fan and arrows B/C/D were rejected).
  - SIX banners in ONE straight row, the chain CLOSED and endless: the picked one big and bright in the centre (as
    today's picked banner, with a warm glow); one each side ~3/4 its size and darkened; one more each side smaller
    and darker; the sixth off screen and slides in from the edge. Side banners keep their name plaque and live line.
  - Scrolled by the arrows at the screen's edges, by SWIPE and by a TAP on a side banner (it comes to the centre).
  - Arrows = gold CHEVRON «A» (no plate), 70 % of the concept's first size, at the banners' mid height, NOT like the
    round «Назад». Hint: every 2.4 s a 0.9 s step outward ~10 px and back, a faint twin trailing (reads «‹‹»), a soft
    gold glow breathing; after the player's first scroll the hint rests and returns after 10-15 s idle; a press
    squeezes the chevron briefly.
  - The two new modes on the row, both LOCKED «Скоро» (gold lock on the medallion): «Мастерская» (own building
    models, ideas.md; violet) and «Онлайн» (lobby of up to 4 by code; amber). Their emblems REDRAWN 2026-11-16 in
    the other modes' rich style (the user: the flat ink ones did not match): Онлайн «Круглый стол» (oak round table,
    crown on a red cushion, four shields blue/star, green/tower, red/cross, gold/fleur), Мастерская «Чертёж и молот»
    (parchment plan of a house rolled top+bottom, brass compasses, oak-and-leather hammer) -- concepts
    `Design/ui/ModeEmblems_Concepts.png` (round 1) / `ModeEmblems_Round2.png` (chosen), drawn by
    `Tools/ui/mode_emblem_concepts.py`, final code in make_mode_banners.py `online()` / `workshop()`. ORDER (the user 2026-11-16): Обучение, Песочница, Кампания, Нашествие,
    Онлайн, Мастерская. CENTRED ON ENTRY (the user): Обучение the first time, afterwards the LAST PLAYED mode.
    Open: the piggy button top right nearly touches the right outer banner.
- (before 2026-11-10) modes: back TL, settings TR, Discord BL, YouTube BR; «Песочница» / «Нашествие» pills, a round «!»
  under each → info dialog (`#mode_info_*`).
- sandbox (REDONE 2026-11-10, the user picked concept В «Знамя и доска» of three, PIL concepts in
  RunLogs/concept_sandbox_*.png): back TL, «!» + settings TR; the hall's sandbox banner on the left
  (340 wide, name plaque); a board (`Window`, kept the name for MapPickerTests) with the 5 themes as
  PICTURE TABS on its top (glyphs, no words -- the user; `BuildThemeTabs`, same `ThemePicker`); the map
  card on the board's left (one map = no lit frame); a column: «Код карты» label, code field
  (placeholder «Случайная») + round arrows = write a fresh random code in (`RandomCode`), error line,
  «Сохранения» plaque (the corner Saves button is gone), gold «Начать». A theme a map lacks hides the
  map (New Year in the sandbox: no maps, Start off). The user saw it 2026-11-10; then: tapping the banner
  swings it like a struck cloth (`UI/BannerKnock`, damped wobble from the perch, away from the hit side).
  TRAP (fixed 11-10): BannerKnock must stay enabled -- the EventSystem sends no clicks to a disabled
  component; the first version switched itself off at rest and never swung.
- sandbox map = PAINTED CHART IN THE LOADING SCREEN'S LOOK (the user, 2026-11-10: «аналогичную как на
  загрузочном экране»; before that an ink-on-parchment chart): drawn LIVE from the code in the box
  (`UI/InkMapPainter`, 500 px = 1 px per cell, palette = loading_maps.py coloured: grey-blue sea in
  broken horizontal hatching, pale shallow halo + dotted ring, land ochre at the coast -> green inland
  with a rim, ink coastline + thin echo, ink peaks on relief/ridges, compass rose top-left, paper stains,
  worn dark edge). Paint ~96 ms in the editor (generate ~40): smooth noises sampled every 4 cells,
  worn-edge noise only near the edge -- keep it that way, it runs once per code on a phone. PLAIN: no markers, no «С» (the user saw markers + «С» on the board
  and asked for the plain chart, 2026-11-10; they may want to discuss extras later). The screen fills a random code on open (so the chart = the map Start
  makes; empty box still = random, no chart then, the static picture). Redrawn on code change only
  (typing waits 0.4 s). The menu lays the map out from `Resources/ArchipelagoPreview`
  (`ArchipelagoPreviewLibrary`, cells only, written next to ArchipelagoLibrary by CreateArchipelagoLibrary)
  via `Generate(seed, version, preview: true)` -- TRAP: never load ArchipelagoLibrary in the menu, it
  drags every island's meshes in. Test `TheMenuChart_IsTheMapTheGameMakes` (also writes RunLogs/ink_map_42.png + INKMAP ms).
- **SANDBOX MENU REDESIGN (the user, 2026-11-17: «теперь я хочу переделать меню Песочницы»)** with the PAID
  «Свобода песочницы» (~$0.99 once: set the world up before the start and in the game -- raids, orc camps, portals,
  neighbour, fires, plague, storms, start goods, build speed, fog... -- the settings drawn are EXAMPLES, the list is to
  be discussed after the look). Concepts `Tools/ui/sandbox_hub_concepts.py` -> `Design/ui/SandboxHub_A/B/C.png`
  (A «Стол картографа», B «Знамя и доска» with Карта/Настройки мира bookmarks, C «Мастерская мира» with dials); the
  user PICKED A («доработай»); v2 sent: `SandboxHub_A2_Locked/Open/Saves.png` (wood frame with brass corners, theme
  seals + season plate, the chart in a gilt frame with a name tag, quill + inkwell, the settings on a scroll with
  rollers -- dimmed + lock card «Открыть навсегда $0.99» before the purchase, live + «По умолчанию» after; the saves
  window with the per-map autosaves). Background = the real menu village shot `Design/ui/MainMenu_VillageShot.jpg`.
  v3 after the user's notes (2026-11-17) `SandboxHub_A3.png` + `_A3_StartPulse.gif` + `_A3_Banner.png`: the title ribbon
  joined as on the plague/fire banners (`ribbon_joined`: tails behind and lower, the fold triangle, top corners only
  rounded) and RAISED off the theme seals; the brass corners ROUNDED with the frame (`brass_corners_round`); the scroll's
  roller ends turned finials (`finial`); «Начать» PULSES (scale +-5 %, glow with it); the settings are SHOWN OPEN
  (RULE, the user): a try to change one opens the purchase banner «Свобода песочницы», built like the plague/fire
  banners (wood window, a sunny chart field with sparkles, the King and the Queen at the sides, parchment: «Сделай этот
  мир своим!», the settings' glyphs, «Навсегда, для всех карт», gold «Открыть за $0.99», «Закрыть»; green ribbon).
  Round 4 (the user, 2026-11-17): «Начать» WITHOUT glow, a smoother breath (36 frames: scale +-4 %, a +-2 deg turn,
  `start_layer`); the title ribbon lowered onto the frame (band 92..184, overlapping the frame top: no gap in the middle either); the banner hides the menu's
  interface behind it (village only), the Queen faces the centre, the ribbon TEAL (was green).
  The screen (`SandboxHub_A3.png` + `_A3_StartPulse.gif`) and the banner (`_A3_Banner.png`) APPROVED by the user
  («Утверждаю», 2026-11-17). Nothing built yet. Next (the user's order): discuss the feature's list, then build.
- saves dialog rows (2026-11-10, the user): the save's THEME glyph alone (no round under it, like the tabs) (season -> first
  theme of it; old saves summer), name + date, then «День N · Жителей N · Зданий N · Код карты v-seed»
  (`#load_row_stats` / `#load_row_code`; buildings = save entries minus Road/Fence/Moat/Bridge/
  Drawbridge/Dec*, the list written by the builder). `LoadGamePanelController` (also the in-game load).
- invasion: window; theme picture across the top (`Inv_<theme>.png`) with King/Queen leaning in at the
  sides (`Peek_*.png`, Tools/blender/render_peek.py), points plaque (sum of the period's best 5 runs),
  crystals plaque (`Wallet.UpgradeCrystals`, the one currency, the sandbox's icon `Res_UpgradeCrystal`; the user, 2026-11-11) + «!», themes on the picture; «Топ 5 забегов …», the
  player's own lit row (no online board yet), «Забег»; Day/Week/Month tabs under the window
  (`InvasionHubController`).
- Badges: orange «!» on Offers/Discord/YouTube until first opened (PlayerPrefs `menu.seen.*`).
- Links in the localization tab: `#link_youtube` = https://www.youtube.com/@DepigoGames,
  `#link_discord` EMPTY until the user sends a never-expiring invite (button does nothing meanwhile).
- Themes (`Core/MapTheme`, `MapThemes`): spring/summer/autumn/winter/New Year, chosen per mode in
  the menu (`GameSettings.SandboxTheme/InvasionTheme`); the settings' season and invasion-map rows are
  GONE. The game's season: `GameSessionIntent.NewGameSeason` → `SeasonStyle.Session`, kept in the save
  (`GameSaveData.season`, -1 = old save = summer).
- Art: `MenuArtBake` (Explicit, -Graphics) `Bake_TheMapsInEverySeason` (Map_<id>_<theme>.png) and
  `Bake_TheInvasionThemes`; glyphs Info/Discord/YouTube/Theme_*/Heart/Shield/Radius in
  make_hud_glyphs.py (Glyphs import as silhouettes:
  a colour sprite there renders BLACK).
- The campaign card/panel code was removed (shelved; in git history before 2026-10-25).
Live scene behind the menu unchanged (`MainMenuBackground` etc.). Tests: `MainMenuTests`,
`MapPickerTests` (sandbox screen).

### Language picker (the user, 2026-11-14; concept v2 approved, Design/ui/LanguagePicker_Concept.png) -- BUILT, not seen
- MOVED 2026-11-17 (the user picked option A of `Design/ui/lang/`: «А — рядом с ⚙»): no pill any more --
  a round wooden button with the current language's flag, the settings button's size (150) and look,
  right of ⚙ in the top-left corner (gap 20). The pill had stood top centre, then clashed with the
  CityCraft logo of main-menu concept 1. Name and chevron are gone (LanguagePicker skips nulls).
- A tap opens a list under it (left edge under ⚙) (flag + own name per row, the current one gold with a tick), a tap
  outside closes it. Own names, not country names (my proposal in the concept, accepted).
- Base language English (`Localization.BaseLanguage`): no saved choice -> the phone's language if it is
  in the list, else English. A language with no sheet column yet reads the English text (translations
  are the LAST step, the user). Release set + cheap wave: en ru de fr es pt it pl tr.
- Japanese/Korean/Chinese are NOT in the list yet: Rubik has no CJK glyphs; add them with a CJK font
  (Noto Sans CJK, OFL) -- then the list needs a ScrollRect (9 rows fit now).
- Code: `UI/LanguagePicker.cs`, `Editor/SetupProjectLanguagePicker.cs` (called last in BuildMainMenu);
  art + list `Tools/ui/language_flags.py` -> `UI/Icons/Flags/` (Flag_<code>.png, Chevron, Tick, languages.json).
  The in-game settings still have their ru/en buttons (sheet columns).

## Loading screen (REDONE 2026-11-10, the user: a painted island map + a ship on the course)
`UI/LoadingScreen.cs`, prefab `Resources/LoadingScreen.prefab` (built by `SetupProjectLoading.cs`),
DontDestroyOnLoad, put up by `LoadingScreen.IntoGame` (sandbox Start, saves Load, Run, dungeon) and
`OutToMenu` (game menu Home, game over); loads with `LoadSceneAsync`.
- The user's rules: one look for every mode; a full-screen painted map of invented islands (NOT the
  mode's real map), the LIGHTLY COLOURED look (ink-only rejected), no names on the islands; drawn per
  screen shape, never stretched; along the bottom variant A «путь по карте»: anchor -> red cross,
  dashed ahead, red solid behind, a flat ink galleon sailing it -- NO waves behind the ship (removed
  at the user's word); the ship takes `loading_seconds_enter` (10) into a game and
  `loading_seconds_exit` (5) out to the menu, by the clock, eased; arrived early -> it waits still at
  the cross (no rocking) until the scene is up (`MeshMapApplier.CurrentMap`; 15 s cap), then fade.
- Tips: one pool `#tip_1..30` (16 old texts moved into 1-16, 17-30 empty for the user to fill),
  random start, every 4 s the next; an empty one is skipped. Tips keep rotating while it waits.
- Art: `python Tools/loading/loading_maps.py` (numpy/scipy/Pillow) -> `UI/Loading/Resources/
  LoadingMaps/Map_Wide_1..3.png` (2400x1080, 20:9; screens >= 1.55 wide) and `Map_Tall_1..3.png`
  (2048x1536, 4:3), plus `Route_Dashed/Route_Sailed/Route_Anchor/Route_Cross/Ship.png`. The course's
  sine (ROUTE_* in the script) is mirrored in LoadingScreen's CourseBaked* constants -- change both.
  The map is `Resources.Load`ed for the screen alone and unloaded on destroy (mobile memory); the
  builder counts the variants into `mapVariants`. Map image = AspectRatioFitter EnvelopeParent.
- Old art (Ink_<map>.png, Load_<theme>.png, Quill, Runner_A/B, MenuArtBake's loading bake,
  Tools/ui/illustrate.py) is no longer used by the screen; left in place.
Test `LoadingScreenTests` (+ photos loading-sandbox / loading-invasion).

## Building card in a run (user's sketch, 2026-10-25)
`UI/RunBuildingCard.cs` replaces the sandbox card in a run only (sandbox card unchanged «пока»): name,
«Уровень N», heart+shield numbers, − workers/free + (minus LEFT), next level's picture
(`Icons/<id>_L<n>.png`, rendered by render_icons.py from <id>1-lvl<n>) with gain badges (+health,
+defence, +place, +worker, +reach), upgrade cost + button, repair cost + button (damaged only),
demolish/close. No «Производит», no recruiting: hiring is `UI/FocusHireBar.cs` — round faces with
prices beside the bottom «!» in one centred row (`BuildingFocusBar`): Barracks = open soldier tiers
(`Portrait<Type>.png`), castle = the three heroes (was CastleHeroCards on the castle's card, deleted).
Hidden while the card is up (`BuildingFocus.DetailsShown`).

## Top resource bar (the user, 2026-11-02)
- ONLY wood, stone, coins, CRYSTALS (own chip after the coins, always shown outside a run), food,
  people; ores/bars/coal/gold and the food chain only in the inventory window, which ends with an
  «Кристаллы улучшения» row (`#inventory_crystals`). In a dungeon: coins + crystals, no people chip.
- A tap on the minimap jumps the camera to the Town Hall (`UI/MinimapHomeTap`; nothing in a dungeon).
- A resumed save opens with the camera on the Town Hall (`GameSaveController.LookAtTownHallAfterLoad`,
  the user 2026-11-16); back from a dungeon the mouth (`GameSessionIntent.LookAtAfterLoad`) wins.
- Building hp bars sit over the model's top when it is taller than 3.9 (the lighthouse).
- Dungeon card «Можно найти: [crystal]» is a centred HorizontalLayoutGroup.
- The row's centre is y -73.6 = the king's portrait and hp plaque centre (-0.7*HudMargin - RoundButton/2,
  SetupProjectHud); the user 2026-11-16: at -64 the pills stood higher and looked odd (concept
  `Design/ui/HUD_TopBar_Align_Concept.png`). Change HudMargin/RoundButton → change this number too.

## Event feed
Round temporary icons at the left edge whose rim drains; tap = camera glides to the source; hold =
one-line caption. Player's own actions (built, demolished, research started, recruited) are LogOnly
(no icon); upgrade DOES get one. Text history in `EventLogManager.Entries` (8 lines, never cleared between tests).

## Style
Style D: dark wood #5a3218/#7a4a22, parchment #f3e2b8, gold #e0a83a, pills with thick outline + hard
shadow, font Rubik. Colours are BAKED into sprites (Images stay white; `pill_white` is the only tintable).
Looks: `GameSettings.HudSkin` → `Resources/HudSkins/<Skin>/<role>.png`; a missing role hides its image.
Old round-2 look kept as git tag `hud-sketch-v1`.

## Code
`UI/` — `HudSkinApplier`, `UiTheme`, `ResourceHUDController`, `HappinessController`,
`EventFeedController/Model`, `EventIconView`, `EventIconLibrary`, `SettlementStatusController` (tiers, hidden), `GameMenuController`, `BuildingCategoryPanel`,
`BuildingInfoPanelController`, `PlacementHudController`, `SettingsPanelController`, `MainMenuController`,
`MainMenuBackground`, `MapSelectPanelController`, `LoadGamePanelController`,
`GameOverController`, `SafeAreaFitter`, `HudHider`, `HudOverlay`, `LocalizedText`.
Core: `Core/GameEvent.cs` (`EventSeverity`, `EventGlyph`), `Core/EventLogManager.cs`, `Core/GameSettings.cs`.
Built by: `SetupProjectGameplayUI.cs` (`BuildGameplayUI`), `SetupProjectPanels.cs` (windows), `SetupProjectMainMenuScene.cs`, `SetupProjectUiKit.cs` (text/buttons), `SetupProjectIcons.cs`, `SetupProjectHud.cs` (layout consts),
`SetupProjectHudSkins.cs`, `SetupProjectTheme.cs` (`ApplyTheme`), `SetupProjectEventFeed.cs`,
`SetupProjectLayout.cs`. Sprites: `Tools/ui/make_skin.py`, `make_hud_glyphs.py`, `make_hud_skins.py`,
`make_event_glyphs.py`; imported by `Editor/UiTextureImport.cs` (Skin PPU 200, Icons PPU 100).

## Tests
PlayMode `HudPhotoTests` (graphics: no white label on parchment, all text Rubik, fits 4 screen shapes),
`EventFeedTests`, `SettingsPanelTests`; EditMode `EventFeedModelTests`, `EventLogManagerTests`.

## Traps
PlayMode screenshots don't render Screen Space Overlay canvases — `PlaytestCapture.ShootWithUi`
switches them to ScreenSpaceCamera. Layout tests are vacuous under `-nographics`.

## Open
- **OPEN QUESTION, to think over (the user, 2026-11-10): the device check and its camera cut-out.**
  Since 2026-11-10 `SafeAreaFitter` ignores the side insets so the left column sits at the screen
  edge; on a phone whose cut-out (punch hole, notch, waterdrop) lands on a left/right button or the
  event feed, something may be covered. To decide: detect the device / its cut-out (`Screen.cutouts`)
  and shift only what it would cover? a per-device list? a setting «отступ от выреза»? Check on
  every phone of the fleet before release.
Tap on an event icon could also open the building card; persistent problems as markers over
buildings; the old tier-up toast; skinning windows per look; no highlight of the selected card.
Mockup canvases: https://claude.ai/artifact/6NUqZfA52W2gZxMSDUdxUV (style D),
https://claude.ai/artifact/DLLxzFesTdktVVR6uSUwD6 (looks).

- 2026-10-28: the main menu's Discord and YouTube corner buttons wear their brands' colours (Discord #5865F2, YouTube #FF0000) on the kit's wooden ring, white glyph (`SetupProjectMainMenuScreens.BrandCorner`).
- 2026-10-28: a window over the main menu (offers, a mode's «!», the saves) fades the screen behind it out (`MainMenuController.Covered`; test `AWindowOverTheMenu_PutsTheScreenBehindItAway`) -- fixes the two «Песочница» plates queued 10-25. The in-game menu has no save entry in a run (`GameMenuController.LayEntries`, the others move up; test `TheRunsMenu_HasNoSaveEntry`).

**Main menu concept 1 «День в деревне Ардана» (the user, 2026-11-17: the menu must be much «красивее и сочнее»; the empty scene is the fault):** the 3D village of the campaign's opening (OpeningVillage + OpeningLife) behind the main menu, BY DAY (the user), life in it: market trade, kids playing, a house being built (NOT growing between visits -- the user), chimney smoke, now and then Ардан walks through; the menu's buttons stay as they are (the user: the concept is the existing one); the game's name «CityCraft» as a logo on top (gold letters, dark rim, soft shadow; the crest and the light band that greyed the letters' top removed by the user). More scene variants later. Mock-up `Tools/ui/main_menu_village_mockup.py` -> `Design/ui/MainMenu_Village.png` (the game's own village shot, view menu_e: from (2.8, 2.3, -9.5) at (-3.2, 1.4, 1.5), fov 50; the real menu UI over it) + `Design/ui/Logo_CityCraft.png`. STAGE 1 BUILT 2026-11-17 (the user: «Да, этап 1»), not seen: `SetupProjectMainMenuScene.BuildMenuVillage` puts the film's own village + folk + effects + director in the MainMenu scene (`SetupProjectCampaign.CreateOpeningVillage/CreateOpeningFx/CreateOpeningDirector`, shared with the film) with a day sun (48°, 35°), the campaign sky and post; the director plays `Campaign/MenuVillage.json`: cues before `loopFrom` (10 s) once (folk out, the cart rolls up, the peasant then talks to the merchant), then 10..70 s for ever (`OpeningDirector` loop: at `end` the clock goes back to `loopFrom`; a cue `loop: true` restarts its performance at its end with the root's turn eased over `loopTurn`; «life» begins once; `CineFigure.Restart`): the market's trade every ~20 s, Ардан walking across the square once a loop (60 s), the kids' P_Play looping, chimney smoke (fx starts far off, no storm), the camera drifting between menu_e and a nearby view. The logo `UI/MenuArt/Logo_CityCraft.png` top centre (638x170, 110 from the top). The old four random work scenes (MainMenuBackground, MenuWorker, MenuVignetteCamera) DELETED. Script numbers live in the JSON like the film's (not the sheet). TRAP: the film's assets (materials, sky, volume, cast clips) are made ONCE per setup run (`MadeThisRun`, `_castClipsThisRun`): the menu is built first, and making them again for the film deleted the menu's references. STAGE 2 RENDERS SENT 2026-11-17, NOT approved, not in Unity: `Tools/blender/cinematic/builders.py` (CineHouseBuild = a half-timbered house half done, x1.5 the set scale so it matches the village's cottages, frame up, back wall plastered, left half, bare rafters, 3 tile rows on the back slope, scaffold on the front with a waist rail and an open middle bay, a ladder in the left bay leaning on the deck's outer edge (round 2: it stood beside the scaffold touching nothing), pulley + bucket; CinePlankPile / CinePlankStack / CinePlank / CineSawhorse; CineCarpenter = white shirt, leather apron, blue cap, hammer; CineLabourer = ochre smock, shoulder pad, bald, big beard) and `builders_anim.py` (CineCarpenter_Hammer 4.8 s: 5 blows, a look, a nail from the pouch -- round 2 after the user (arm looked broken, hammer held wrong): the arm swings in his front plane, elbow forward at shoulder height, forearm up with the head cocked back, the blow forward-down with the handle level into the wall; the hammer's handle leaves the fist forward-and-down; he stands a hammer's reach (0.21) from the plate; CineLabourer_Carry ~10.6 s: picks a plank from the pile at his right, carries it along x on his right shoulder to the stack, sets it down, turns, walks back; the carried plank pops back to the pile at the loop's restart). Renders to `Design/menu_builders/` (not committed). APPROVED by the user 2026-11-17 and IN THE GAME (setup green, the user not seen in Unity): `export_builders.py` exports the 5 set pieces to Models/Cinematic, the 2 figures (Idle + P_Hammer / P_Carry, looping, posed where they stand) to Models/Cinematic/Cast, merges their clips into OpeningCast.json (export_cast.py now keeps figures it did not make) and writes `Campaign/MenuBuilders.json` (the site's layout in its own frame, the labourer's root and the plank per frame); `Campaign/MenuBuildSite.cs` on the menu village places it at `buildSite` of MenuVillage.json (-6.5, 2.5, yaw -35, left of the church), asks the village to keep `buildSiteKeep` clear (`OpeningVillage.KeepClear`, before it builds) and plays both loops on its own clock (`Step(dt)`). Re-export after a Blender change: `blender --background <Models.blend> --python export_builders.py -- G:/Fork/CityBuilding`. STAGE 3, step 1 -- EDITOR CENSUS 2026-11-17 (a temporary PlayMode probe, deleted; the menu's camera frustum, 3 s in): IN VIEW 1 578 draws, 283 000 tris, 639 shadow draws (< 60 m); 94 figures, 104 particle systems (2 424 particles), 6 lights. By kind: FIGURES 996 renderers / 96 k tris / 498 shadow draws (~10.6 renderers EACH: the cast FBX comes in as one renderer per body part), houses/props 107 / 91 k, trees 330 / 33 k, set pieces 58 / 31 k, decor 22 / 23 k, build site 10 / 6.5 k. CPU: director+folk 0.22 ms, the build site 0.004 ms a frame (desktop). For scale: the sandbox's bare map was ~1 010 draws, the weakest phone's working ceiling ~150 k tris. Levers proposed to the user: one renderer per figure (merge parts into a skinned mesh: ~1 000 -> ~100 draws, also helps the film), fewer folk / no figure shadows on Low, a shorter far plane + fog in the menu. Stage 3 step 2 -- ALL FOUR LEVERS BUILT 2026-11-17 (the user: «Все четыре»), MENU ONLY: `Campaign/MenuVillageQuality.cs` (Awake, before anything spawns; undone in OnDestroy) sets `CineFigure.KeepSkinned` (a figure keeps its one skinned mesh, no Rigidify -- in play mode only; a skinned mesh gets no back-face twins), `CineFigure.HideParts` = CineArdanSack (Ардан's sack hidden in the menu, the user 2026-11-17), on the Low tier `CineFigure.NoShadows` + `OpeningLife.FolkShare` = menu_low_folk_share (0.5 of the walkers and square standers), and the camera's far plane = menu_far_clip (85) with fog menu_fog_start 30 .. menu_fog_end 80 (economy keys, sheet tab «Время и загрузка»). Re-census (desktop High): IN VIEW 459 draws (was 1 578), 158 k tris (was 283 k), 180 shadow draws (was 639); figures 39 draws in view (was 996). The film and the missions keep rigid parts + twins: torches, the sack and MissionMage.Tip hang on the rigid part renderers -- one draw a figure there needs those reworked (optional follow-up). PLAYTEST 2026-11-17 (the user's screenshots) FIXED: roofs showed the sky from below and flickered -- the first cut had switched back-face twins off for the whole menu (`CineTwoSided.Off`, now deleted): the houses need them; haystacks hung on their point (a kit blob is a double cone, widest 0.4 up) -> sunk 0.15 in farm_props.py (film too); the church's kit cone trees removed from church.py, the game's own trees set there by OpeningProps.json "trees" (film too). Re-census: 525 draws, 219 k tris, 165 shadow draws in view. Then «боковые части крыш всё ещё мигают»: the kit's gable verge board ends flush with the slab's end (kit.py _smooth_gable) -- two coplanar faces of different colour z-fight, in every building, seen only from the side. FIXED ON IMPORT by `Editor/BuildingCoplanarFix.cs` (Models/Buildings only): coplanar, same-facing, overlapping faces of different palette brightness -> the darker one (the trim) cloned and moved out 2.5 mm; 254 models touched, the log prints `[CoplanarFix] <file>: N vertices moved out`. Re-exporting the buildings was the alternative (would need saving the models .blend). Stage 3 step 3 = phone check (APK only when the user asks). Trick for mock-ups: the menu UI shot twice (black / white clear, every Renderer off) -> alpha from the difference.
