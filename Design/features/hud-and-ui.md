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
- main: settings round top-centre, «Продолжить» (= the old Play: to the modes), «Предложения» (= the
  shop), Discord + YouTube rounds under them. No resume-last-save plaque any more.
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
- Building hp bars sit over the model's top when it is taller than 3.9 (the lighthouse).
- Dungeon card «Можно найти: [crystal]» is a centred HorizontalLayoutGroup.

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
