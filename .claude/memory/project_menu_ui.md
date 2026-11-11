---
name: project-menu-ui
description: "The user's own sketch (2026-10-20) of the main menu and mode select for the three modes Кампания/Песочница/Нашествие; discussion stage, nothing built."
metadata:
  node_type: memory
  type: project
  originSessionId: 0f587829-fe18-4bb5-a5b7-742486c4675e
  modified: 2026-10-20T12:21:04.678Z
---

The user drew the menu in three frames (2026-10-20). Discussion only, nothing built yet.

- **Frame 2, main menu.**
  - Background: a live 3D vignette, picked at random on each launch. Citizens are doing something in it (mining stone, chopping trees, ploughing, building a house).
  - Bottom centre: a big Play icon, with Exit to its lower left and Settings to its lower right, in a triangle.
  - No text on the buttons.
- **Frame 3, mode select (after Play).**
  - Back button top left, Settings top right.
  - Three tall cards: Кампания, Песочница, Нашествие, each with a symbolic drawing.
  - Same 3D background as the main menu.
- **Frame 4, one mode chosen.**
  - The chosen card slides to the left, the other two leave.
  - The right side shows the choice between a save and a new game.

Open points I raised:
- There is no "Continue" button.
- Exit is not allowed on iOS.
- Campaign probably wants a level map, not saves.
- Invasion has no saves; the right side is a hub with the daily challenge, record and leaderboards.

User's answers (2026-10-20):
- Continue: a button plus a description of what it continues, at the BOTTOM RIGHT, not centre.
- The Exit button gets replaced by something; I proposed options.
- Campaign right side = a level map.
- Invasion right side = hub (daily challenge, record, day/week/month leaderboards, «В бой»).
- NO resuming a dropped Invasion run: a crash or quit ends it, because it is ranked and a drop is suspicious.

More answers (2026-10-20):
- Exit is replaced by a SHOP (Лавка) of game content, left of Play. Exit becomes a confirm dialog on the system back button / Esc.
- A minimised app pauses the game; the user agreed.
- An Invasion run that is cut off (app killed) TAKES AWAY rating points. RATING MODEL (user, 2026-10-20): CUMULATIVE.
- Each run earns rating points by how long it survived.
- Unlimited runs per period.
- The period total is compared between players, then prizes are given.
- A dropped run subtracts points.
- Agreed refinement (1+2):
  - points grow FASTER than survival (superlinear, e.g. wave 10 = 100, wave 20 = 400, wave 30 = 900);
  - only the BEST 5 runs of each day count;
  - the week and month totals are the sums of those daily best runs.
- Shop contents: decide later.

The sketches are the basis. My earlier text proposal (a Continue button and cards on the main screen) gives way to them. See [[project-invasion-mode]] and [[project-ui-skin]].

**Why:** the user designs the UI themselves from sketches.

**How to apply:** follow the frames. Build only when asked.

Mockup published 2026-10-20: https://claude.ai/artifact/BMoYww31noyVW6E1CM172Z (source in that session scratchpad, menu_mockup.html).

BUILT 2026-10-20 (commits 6536c5c9 + 431f2594):
- SetupProjectMainMenu.cs builds the menu; MainMenuController drives screens (main / modes / mode) with eased motion; Back = Esc/Android back.
- Per-look kits in Resources/MenuSkins/<Look> from Tools/ui/make_menu_kit.py (numpy: height-lit wood/parchment/glossy/glass with nails, rosettes, iron brackets, stitching, filigree); the HudSkinApplier reads them via its resourcesFolder; new HudInk roles OnPanel/OnTitle/OnButton/OnMenuPlaque/OnTab/OnTabSelected.
- Card pictures and map previews are photos from the game: MenuArtBake ([Explicit]): run.ps1 -Steps playmode -Filter MenuArtBake -Graphics, then setup.
- Campaign and Invasion panels are layout-only placeholders ("скоро"); Fight is disabled; the shop is a placeholder dialog.
- LIVE BACKGROUND built 2026-10-20: MainMenuBackground stages one of 4 scenes (Woodcutters/Quarry/Fields/Building) on a random map's flattest dry meadow from stripped building prefabs + map trees/rocks; MenuWorker cube citizens with arm+tool, MenuFallingTree, MenuRisingBuilding (y-scale + scaffold), MenuVignetteCamera (distance 7, pitch 27, breathing drift); the menu scene got a sun; city scene now built BEFORE the menu. Test hook: MainMenuBackground.ForcedVignette/ForcedMapId. User said screens stay as they are for now, to be revised later.
- Localization sheet synced 2026-10-20 (verify_sheet: no differences).
- Photos: playtest_shots/menu-<Look>-<screen>.png from MainMenuTests.Photograph_EveryLook.

**2026-10-24:** the CAMPAIGN card and panel are hidden (`ModeShown` in SetupProjectMainMenu): the user shelved the campaign and nobody is to know of it. Sandbox and Invasion cards stand evenly about the middle.

**2026-10-25 redesign (pushed 5d65b5e9), user's sketches, CLASSIC look only (others share the layout):** main = settings top, «Продолжить»→modes, «Предложения» (shop), Discord/YouTube with «!» badges until opened; modes = 2 pills + «!» info; sandbox screen = map cards per theme, saves icon (only way to saves), «Начать»; invasion hub = theme picture + peeking King/Queen, points = best 5 runs of period, crystals (0), Day/Week/Month tabs. Themes (5) replace the settings' season/invasion-map rows; season saved per game. Run building card + hire buttons beside the bottom «!». Discord link EMPTY: waiting for a never-expiring invite from the user (#link_discord). Sandbox card not redone yet ("test in the run first"). Settings and Offers screens not redone (user's rule).
- 2026-10-25 later: user called the carved-wood Classic menu "уродство" next to the game's Classic HUD → the Classic menu now uses the HUD's own flat kit (Tools/ui/make_menu_classic.py). Rule: menu and game must share one style; don't invent a separate menu look.
- 2026-10-25: drawn LOADING SCREEN built (user picked: quill inks the map chart for sandbox, painted theme scene + running militiaman for invasion, tips, shown both ways). Pipeline: MenuArtBake.Bake_TheLoadingPictures → playtest_shots/loading → Tools/ui/illustrate.py → UI/Loading. User may draw their own Discord/YouTube icons (256x256 PNG, white silhouette or colour — if colour, move out of Glyphs and stop make_hud_glyphs overwriting them).


2026-11-10: MODES SCREEN REDONE as «Зал знамён» (user picked of 3 concepts; asked it to match the HUD Classic style): 4 banners (tutorial/sandbox/invasion/campaign) by Tools/ui/make_mode_banners.py, live lines, tap pick / tap again or Играть to start, long press = info. Card hud-and-ui.md. Not seen by the user.
2026-11-10: banners APPROVED by the user after round 2 + invasion emblem fixes (horned orc skull, axes with real blades turned OUTWARD, 6e2c3de79). Open from the concept: cloth sound on unroll, background camera moving per picked mode -- not built.
