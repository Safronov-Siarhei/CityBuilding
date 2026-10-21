# HUD and UI

> **Status (2026-10-21):** HUD follows the user's sketch (round 2, 2026-10-14) in style D, with four
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

## Main menu (user's sketch, 2026-10-20)
Play brings up three MODE cards — campaign, sandbox, invasion; a card slides left and brings in its
panel (campaign level map, sandbox new game + saves + map picker, invasion hub with «В бой»).
Continue bottom-right, shop and settings beside Play; Back walks the screens and asks before quitting.
Each HUD look has its own menu kit (`Tools/ui/make_menu_kit.py`); cards/map list show pictures baked
from the game (`MenuArtBake`). Behind the menu a LIVE scene: a random map meadow with one of four
staged scenes (woodcutters, quarry, ploughmen, a house in scaffolding), the game's sun, a breathing
camera. Code: `UI/MainMenuController.cs`, `MainMenuBackground`, `MenuWorker`,
`MenuFallingTree`, `MenuRisingBuilding`, `MenuVignetteCamera`, `MenuRowLook`; built by
`Editor/SetupProjectMainMenu.cs`. Tests PlayMode `MainMenuTests`, `MapPickerTests`. The campaign is a
placeholder (its design is not decided).

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
Built by: `SetupProject.BuildGameplayUI/BuildMainMenuScene/...`, `SetupProjectHud.cs` (layout consts),
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
