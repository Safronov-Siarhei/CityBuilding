# HUD and UI

> **Status (2026-10-25):** main menu and the run's building card redone after the user's sketches (below). Earlier (2026-10-21): HUD follows the user's sketch (round 2, 2026-10-14) in style D, with four
> switchable HUD looks (Classic/Soft/Glass/Dock) chosen in settings. Windows (info card, research,
> save, settings) are NOT skinned per look — still parchment.
> **Rule:** the user SKETCHES layouts; implement their sketch, don't redesign or restyle unasked.
> **Read with:** [art-pipeline](art-pipeline.md) (icons), [balance-and-localization](balance-and-localization.md) (all text).

## Layout (user's sketch round 2 + round 3 fixes)
Menu round button top-left (pauses `timeScale`, hides HUD via `HudHider`; save/settings/home);
citizens button under it; event feed down the left edge; one resource bar top-centre on a wood plaque,
«День N» under it; happiness round number top-right (tap → breakdown), migration + army under it;
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
- modes: back TL, settings TR, Discord BL, YouTube BR; «Песочница» / «Нашествие» pills, a round «!»
  under each → info dialog (`#mode_info_*`).
- sandbox: window, back + SAVES (the only way into saves now) TL, «!» + settings TR; map cards 3 per
  row with the theme's picture (`SandboxScreenController`); 5 theme rounds (`ThemePicker`); «Начать».
  A theme a map lacks hides the map (New Year in the sandbox: no maps, Start off).
- invasion: window; theme picture across the top (`Inv_<theme>.png`) with King/Queen leaning in at the
  sides (`Peek_*.png`, Tools/blender/render_peek.py), points plaque (sum of the period's best 5 runs),
  crystals plaque (`Core/Wallet`, 0 for now) + «!», themes on the picture; «Топ 5 забегов …», the
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
  make_hud_glyphs.py; the colour crystal goes to UI/MenuArt/Crystal.png (Glyphs import as silhouettes:
  a colour sprite there renders BLACK).
- The campaign card/panel code was removed (shelved; in git history before 2026-10-25).
Live scene behind the menu unchanged (`MainMenuBackground` etc.). Tests: `MainMenuTests`,
`MapPickerTests` (sandbox screen).

## Loading screen (user's pick, 2026-10-25: 1 for the sandbox, 2 for the Invasion, tips, both ways)
`UI/LoadingScreen.cs`, prefab `Resources/LoadingScreen.prefab` (built by `SetupProjectLoading.cs`),
DontDestroyOnLoad, put up by `LoadingScreen.IntoGame` (menu: sandbox Start, saves Load, Run) and
`OutToMenu` (game menu Home, game over); loads with `LoadSceneAsync`, shown ≥ 2.4 s, gone once the new
scene's map is laid out (`MeshMapApplier.CurrentMap`).
- sandbox: parchment page, the map's own ink chart (`UI/Loading/Ink_<map>.png`) drawn in left→right
  (Image Filled) over a faint copy, a quill (`Quill.png`) riding the edge; the map's name at the end.
- invasion: the theme's painted scene (`Load_<theme>.png`), a plank filling at the foot with a running
  militiaman (`Runner_A/B.png`, Tools/blender/render_runner.py).
- tips: rotating every 4 s from `#tip_sandbox_N` / `#tip_invasion_N` (the builder reads the CSV keys).
- Pictures: `MenuArtBake.Bake_TheLoadingPictures` (-Graphics) writes raw shots to the IGNORED
  `playtest_shots/loading/` (the chart = `FogOfWarManager.ConcealAll()` + top-down shot of the
  parchment fog; the scene = the run with the King in front), then `python Tools/ui/illustrate.py .`
  paints them (flatten, ink edges, paper, vignette), crops the charts, draws the quill.
- Trap: a Filled Image without a sprite draws WHOLE (the plank fill uses the HUD card sprite).
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
Tap on an event icon could also open the building card; persistent problems as markers over
buildings; the old tier-up toast; skinning windows per look; no highlight of the selected card.
Mockup canvases: https://claude.ai/artifact/6NUqZfA52W2gZxMSDUdxUV (style D),
https://claude.ai/artifact/DLLxzFesTdktVVR6uSUwD6 (looks).
