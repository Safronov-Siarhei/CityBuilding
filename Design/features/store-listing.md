# Store listing (Google Play)

**Status (2026-11-10):** two icon candidates, each as a flat drawing (king corrected once by the
user) and as a 3D render from the game's models; waiting for the user's corrections and pick. Not yet uploaded anywhere; no store screenshots yet. Rate-us window: concept + mock-up only.

## Icon
- **The game's icon NOW = the 3D orc** (the user, 2026-11-10: «поставь пока что орка, оставим вопрос
  открытым»): `Assets/_Project/UI/AppIcon.png` (1024, rendered straight there by
  `render_icons_3d.py -- orc <abs path>/AppIcon.png 1024`); SetupProject sets it as the default icon.
  Replacing the file keeps its GUID, so no setup is needed to switch. Orc vs king is still open.
- Concepts proposed: island diorama, orc over the wall, builder king, banner crest, galleon. The user
  picked **2 (orc)** and **3 (king)**; both polished, to be A/B tested later with Play Console store
  listing experiments.
- `Tools/store/make_icons.py` (PIL, no inputs) draws both and writes `Design/store/`:
  `Icon_Orc_512.png`, `Icon_King_512.png` (the store needs 512x512 PNG, no alpha) and
  `Icons_Sheet.png` (Google Play's rounded mask, at 512/192/96/48 px: 48 is the size in search).
- Style: flat cartoon, thick dark outline, cel shading via `clipped()` (shade polygons masked to a
  shape), `soft()` glows, vignette. Orc: sunset, smoke, embers, mossy wall, iron axe. King: town,
  scaffolding, red cloak with ermine, crown, hammer, sparkles.
- **3D versions** (the user asked, 2026-11-10): `Tools/store/render_icons_3d.py` renders the same two
  ideas in Blender from the game's own models (opens Buildings-Size1-Models.blend, never saves):
  `orc` = OrcChief behind a stone wall built from boxes, sunset backdrop plane, fire lights, embers;
  `king` = HeroKing with BOTH arm meshes hidden (ArmSword/ArmShield are single meshes with their
  weapon) and box-built arms + a hammer -- the parts are CHILDREN of HeroKing placed in his own frame
  (`part()`/`arm()`: a straight block from the shoulder at an angle from hanging down; the user
  rejected skewed world-space beams as crooked), the handle upright through the fist, the head
  straight across. The user disliked the hammer version: now a BIG PORTRAIT, head and shoulders,
  no hammer, both arms hanging (lens 172), on a plain sky gradient like the orc (the user: stands out
  better; the town and the depth of field are gone),
  `retouch_king()` (icon only, the file is never saved) answers the user's «корона кривая, треугольники,
  глаза чёрные не живые, чёрные штуки на плечах»: deletes the head's crown faces by palette cell
  (gold 0.69/0.5, gems 0.06/0.9 + 0.81/0.9, purple cap 0.56/0.9 + 0.69/0.9), builds a block crown
  (band, velvet cap, five tall points with pearls, ruby + sapphires); eyes = white + brown iris turned
  to the camera + pupil + glint over the black blocks (local x +-0.025, z 0.092, front y -0.06); the
  collar's black ermine tails (cell 0.81/0.5, the SAME cell as the eyes) recoloured cream; rosy cheeks.
  Gold must stay low-metal (0.25) or it renders olive. Cottage1-lvl3 / FortifiedTower1-lvl2 / Hovel1-lvl2 behind in
  depth of field. Output -> `Design/store/Icon3D_<Orc|King>_512.png` + `Icons3D_Sheet.png`.
  Trap: give Blender an ABSOLUTE output path (a relative one lands next to the .blend).
  Run per scene: `blender --background <Models.blend> --python Tools/store/render_icons_3d.py -- orc D:/Fork/CityBuilding/Design/store/Icon3D_Orc_1024.png 1024`,
  then downscale to 512 and build the sheet with `make_icons.masked`.
- Rules: no text and no badges (Google forbids "#1", "free"...); keep the subject in the middle ~80%
  (the mask rounds the corners); judge it at 48 px.

## Rate-us window (BUILT 2026-11-10, mock-up v6 approved; the user to try it; WHEN it shows: the user will say)
- **TEMPORARY test hook:** `SettingsPanelController.OpenPanel` calls `RateUsWindow.Show()` -- the window
  comes up every time the settings open (the user, 2026-11-10: «для теста ... при открытии настроек»).
  REMOVE it when the real show rule goes in.
- The user's rules: a POPUP that can be shown at any moment over whatever the game shows (not a screen
  of its own); the King and the orc chief stand either side and ask to support the project with a
  rating; the King is the GAME'S OWN HeroKing model (the user rejected the icon's retouched king:
  "a model we use nowhere"); the orc chief a head taller, measured FEET TO HEAD (never by his flag or
  axe); the window ~1.5x the first mock-up (1800x820 of the 1920x1080 reference); the blurred summer
  town as the window's picture (background A of A-D); the ribbon reaches down to the parchment, its
  dark folds go along the band's bottom edge from each end inward down to the tail's inner corner;
  NO close X -- «Позже», or a tap OUTSIDE the frame, closes it.
- Art: `Tools/store/rate_window_art.py <folder with figure_king.png/figure_orc.png>` draws every piece on
  its own layer and writes `Assets/_Project/UI/RateUs/` (Window = shadow+frame+town+parchment+heroes'
  ground shadows, one per theme under Resources/RateUs, Tails, King, Orc, Band = folds+band, Stars, Button) + `layout.json` (piece/text/frame
  rects in reference px, y down) + the mock-up `Design/store/RateWindow_Mockup.png` with Russian words.
  Heroes from `render_icons_3d.py -- figure_king|figure_orc <abs out> 1200` (whole figure, transparent,
  ortho camera fitted to bounds; plain HeroKing). Scale from measured pixels (king feet 1170 / head top
  under the crown 270, orc feet 1090 / head top under the spikes 490, in the 900x1200 renders); the
  orc's banner above his skull staff is erased (`no_flag`). Draw order: tails, heroes, band.
- Game: `UI/RateUsWindow.cs` -- `RateUsWindow.Show()` from anywhere (prefab `Resources/RateUsWindow`
  built by `Editor/SetupProjectRateUs.cs` from layout.json; canvas order 900, under the loading screen);
  pops in (unscaled time); «Оценить» opens `market://details?id=com.depigogames.citybuilding` (the Android identifier SetupProject sets; the game's own page once it is published) (https Play link off
  Android) and sets `Rated`; «Позже»/shade tap count `LaterCount` + `LastLaterUtc` -- all PlayerPrefs
  (device, not the save) for the future show rule. Art imported as UI art (`UiTextureImport.RateUsFolder`,
  ASTC on Android), unloaded after the window closes. Words: `#rateus_title/_body/_rate/_later`
  (sheet + csv). Cheat: Сервис -> «Окно оценки игры».
- Trap (2026-11-10, white boxes in the editor): PNGs imported BEFORE `UiTextureImport.RateUsFolder` existed stay
  plain textures (LoadAssetAtPath<Sprite> = null); `BuildRateUsWindow` re-imports any piece that is not a Sprite.
  Seasons (the user, 2026-11-10): the framed town follows the map's theme -- `Resources/RateUs/Window_<spring|summer|
  autumn|winter|newyear>.png` (same rect; crop of each `Load_<theme>.png`), loaded by `RateUsWindow` as it comes up
  (the prefab holds none): New Year Invasion map -> newyear, else `SeasonStyle.Current`; in the menu, the last
  game's season (summer at start). Preview of all five: `Design/store/RateWindow_Seasons.png`.
  Behind it: EVERY other screen-space root canvas under its order is switched off while it is up and back
  on as it closes (the user, 2026-11-10: «скрывать весь задний интерфейс везде»); world-space canvases and
  the loading screen (order 1000) stay.
  Size: the window shows at HALF the art's reference size (the user, 2026-11-10; `RateUsSize` 0.5 ->
  `RateUsWindow.size`, multiplied into the pop scale); its art is capped at 1024 on Android (UiTextureImport).
  Body text: 3 lines of 46 did not fit Rubik's line box (third line lost) -> 42, vertical overflow allowed.
- Google Play rules that shape it: our stars are decoration only (the real rating is Google's In-App
  Review sheet or the store page); NO «do you like it?» gating, NO reward for rating; the In-App Review
  sheet has a quota and may silently not show, so a button should open the store page (or our window
  calls the review flow by itself, not from a button).
- Proposed timing (numbers on the sheet): only after a happy moment (raid won, portal closed, tutorial
  done, Town Hall up), never after a defeat, not before N sessions; «Позже» = again in N days, gone after
  two refusals or after «Оценить».

## Open
- Rate window: WHEN it shows (the user will say; proposed below), then see it on a phone.
- The user's corrections to both icons.
- The king is not a character in the game yet (if he wins, he may need to appear in the menu/tutorial).
- Feature graphic 1024x500 and screenshots.
