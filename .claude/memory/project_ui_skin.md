---
name: project-ui-skin
description: "The interface reskin to style D (2026-10-13, commit 66683dd4): how the skin, icons, font and theme pass are built, what was verified, and what is still left. Read before touching UI or SetupProject's UI code."
metadata:
  node_type: memory
  type: project
  originSessionId: 43bf2a83-fac9-4bcc-a587-078df23a283e
  modified: 2026-10-13T13:29:37.278Z
---

**Style D** = the user's pick from the mockup canvas https://claude.ai/artifact/6NUqZfA52W2gZxMSDUdxUV (5 variants А–Д; Д = variant В's shapes — pills, thick outline, hard drop shadow — in variant А's colours: dark wood #5a3218/#7a4a22, parchment #f3e2b8, gold #e0a83a; round minimap from Б; font Rubik). Implemented in commit `66683dd4`, pushed. Tests EditMode 403, PlayMode 124 green.

## How it is built
- `Tools/ui/make_skin.py <project>` → `Assets/_Project/UI/Skin/*.png` (+ `borders.json` 9-slice table). Sprites drawn at 2x, imported at PPU 200 by `Scripts/Editor/UiTextureImport.cs` (also handles `UI/Icons`, PPU 100). Colours are BAKED in sprites → Images stay white; `pill_white` is the only tintable one (confirm button).
- `Tools/blender/render_icons.py -- <out> 192` renders every `*1-lvl1` building (+ fence corner) and `Res_<ResourceType>` props into `Assets/_Project/UI/Icons`. `CreateBuildingIcon/CreateResourceIcon/CreateCategoryIcon/CreatePopulationIcon` use the PNG if present, else the old procedural one.
- Rubik Medium/Bold/ExtraBold TTF + OFL licence in `Assets/_Project/Resources/Fonts` (downloaded with the user's explicit OK).
- `Scripts/UI/UiTheme.cs` (runtime): Ink/InkSoft/OnWood/Gold/Danger/Warning/Good, row/tab tints, fonts, and `OnParchment(color)` which maps old light-on-dark colours to dark counterparts (already-dark colours pass through).
- `Scripts/Editor/SetupProjectTheme.cs` (partial SetupProject): `SkinSprite`, `IconSprite`, `ApplyTheme(canvas)` — run at the end of BuildGameplayUI and BuildMainMenuScene: panel-sprite images → parchment panel (buttons → pill), labels → ink on parchment / OnWood on wood / unchanged when floating over the world. Also `CreateHotbarCard`, `CreateSkinButton`, `CreateSkinImage`.
- HUD layout: top row y488 (Меню gold −865, Сохранить −690, Жители −500, status wood pill centre); resources row y405 packed pills (`ResourceHUDController.chipBackgrounds`); happiness (740,296); tax (−760,318); hotbar = cards 124×164 on `panel_banded`; tabs 96 circles with sprite swap (`BuildingCategoryPanel.selectedSprite/unselectedSprite`); round minimap (−834,−414) with Mask + ring.
- `PlaytestCapture.ShootWithUi` switches overlay canvases to ScreenSpaceCamera for a shot. `HudPhotoTests` (Map2): no white label on parchment, all text Rubik, photos `hud`, `hud-exit-dialog`, `hud-workforce`.

## Screen adaptation (2026-10-13, commit 22e318be)
- Phone screenshot showed the HUD laid out from the centre for 16:9 only. Fix: CanvasScaler `Expand` (`ConfigureScaler`) + `SetupProjectLayout.PinHudToEdges` re-anchors each standing piece to its corner by NAME (direct children of SafeArea) without moving it on 16:9. A new HUD piece must be added there, or it stays centre-anchored.
- ResourceHUD stretches across the top; ResourceHUDController reads its real width. Event log fits its text. Selection badge moved to y -180 from top.
- `HudPhotoTests.TheHudFitsEveryScreenShape` lays out at 2400x1080/2000x1200/1920x1080/1024x768 (`PlaytestCapture.ShootWithUiAt`), asserts inside-screen + no overlap of `HudPieces`; needs a run WITHOUT -nographics (vacuous otherwise). Proven to fail on a real overlap.
- KNOWN, left for the user's layout: the Migration panel overlaps the Rotate placement button (all sizes); a 12-building hotbar (1736 wide) reaches the minimap on 16:9.
- The user said (2026-10-13) they will SKETCH where every button goes on each scene; implement their sketch rather than redesigning on my own.

## Left (as of 2026-10-13)
1. Seen on a phone (2026-10-13) and on photo tests at four sizes; layout redo pending the user's sketch.
2. Tune windows the theme pass converted automatically: building info card, research, load panel, main menu.
3. No highlight of the selected building on the hotbar.
4. Not seen on a phone; APK only when the user asks ([[feedback_build_only_on_request]]).

Related: [[project_blender_modeling]] (models, icons source), [[feedback_mobile_first_optimization]].

## Event feed replaced the text log (2026-10-13, commit 08d4ea19)
- User disliked the text log panel ("too much text"); asked for FF-style temporary round icons whose rim drains, tap = camera to the source. Done: `EventFeedController`/`EventFeedModel`/`EventIconView`, `GameEvent` (severity/glyph/picture/place), built in `SetupProjectEventFeed.cs`, column at left edge where the log was (pinned TopLeft as "EventFeed"), 4 slots, own nested canvas.
- Glyphs: `Tools/ui/make_event_glyphs.py` -> `UI/Glyphs` (file name = EventGlyph value); disc/rim/badge in make_skin.py.
- Player's own actions (built, demolished, research started, recruited) are LogOnly — no icon. Upgrade DOES get an icon (user's own example). Text history kept in EventLogManager.Entries for tests; hold/hover shows one-line caption.
- Not done / open: tap only moves the camera (does not open the building card); persistent problems (no input, no well) could become markers above buildings; tier-up still also shows its old text toast.

**User's HUD sketches, round 1 (2026-10-14, bf232a5a):** the build menu is CLOSED at rest — only the category tabs on the bottom edge, no tab lit; tapping a tab slides the row up (closedBarY→openBarY) and shows that category's cards beneath it; re-tapping the open tab or tapping the map closes it (not while BuildingPlacer.IsSelecting). The user did NOT want it to close on picking a building. Logic in BuildingCategoryPanel (TapCategory/Open/Close/HandleMapTap via TouchInputRouter.Tapped); HudPhotoTests checks both states on 4 screen shapes. More HUD changes from the user's sketches are expected.

**User's HUD sketch, round 2 (2026-10-14, 98e41ac8) — the HUD now follows it:** menu round top-left (GameMenuController: save/settings/home, PAUSES timeScale, hides HUD via HudHider; home = autosave + leave, no confirm, ExitToMenuController deleted); one resource bar top-centre on a wood plaque sized to chips; "День N" (#hud_day) under it; citizens round button under that; happiness = round number top-right, tap → breakdown; migration + army under it; event feed left; minimap bottom-left; build menu bottom-centre, REPLACED by ✕ ⟳ ✓ (PlacementControls) while IsSelecting (✓ in draw mode = finish); cards row max 1040 wide, scrolls with a scrollbar; tax bottom-right (− coin% +). Interface rule from the user: NO TEXT on buttons, pictures only (Tools/ui/make_hud_glyphs.py → UI/Glyphs/Hud_*.png). Settlement tiers are hidden from the player entirely (toast + status removed). Builders in SetupProjectHud.cs; corner pinning no longer used except the minimap. HudOverlay marks panels that stay visible when the HUD hides.

**Round 3 (2026-10-14 evening, c5b380a8):** the user called the round-2 look "cheap and ugly", said the previous style (D) was nicer. Kept round 2 as git tag `hud-sketch-v1` (pushed). Done in game: citizens button moved to the left under the menu (SetupProjectHud consts MenuButtonTop/WorkforceButtonTop/LeftColumnTop; event feed starts at LeftColumnTop); tax plaque says "Налог: 10%" via #tax_rate — the ONE HUD control allowed a word. Restyle variants mocked on canvas https://claude.ai/artifact/DLLxzFesTdktVVR6uSUwD6 (А style D faithfully, Б scroll + brass medallions, В soft mobile, Г dark glass, Д bottom-dock concept) — waiting for the user's pick; implement the pick, don't restyle unasked.

**Round 4 (2026-10-14 late, e57c356c): four switchable HUD looks.** The user picked А, В, Г, Д from the canvas and wants one base look plus the others as PAID cosmetic content later (IAP not built yet — next layer: Unity IAP non-consumables, a lock on unowned looks in settings, "Restore purchases"; asked but unanswered: Android only or iOS too). Built: `GameSettings.HudSkin` (Classic default = А, Soft = В, Glass = Г, Dock = Д), PlayerPrefs `settings.hud_skin`; `Tools/ui/make_hud_skins.py` → `Assets/_Project/Resources/HudSkins/<Skin>/<role>.png` + borders.json (roles: round_main/round/round_step/round_alt/round_value/round_tint, chip, bar, plaque, panel, card(_selected), tab(_selected), tray, scroll_*, minimap_ring, event_disc, dock_top/bottom; a missing role hides its image). Runtime `UI/HudSkinApplier.cs` (on SafeArea; lists written by `SetupProjectHudSkins.cs` via HudImage/SkinRole/SkinInk/DockAt; HudInk roles; HudSkins.Status lifts status colours on Glass). Dock layout moves ResourceHUD + day onto a top rail (84) and keeps tabs in a bottom dock (142), cards open above. Settings card has an "Оформление" row (#settings_hud* keys, in the sheet). Windows (info card, research, save, settings) are NOT skinned — still parchment in every look. Rubik in all looks (Fredoka/Cinzel would need a font download OK). Tabs used to stand 48 above their CategoryBar rect — fixed (tab y = 0).
