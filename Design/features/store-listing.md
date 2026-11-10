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

## Rate-us window (concept, 2026-11-10; NOT in the game yet)
- The user's rules: a POPUP that can be shown at any moment over whatever the game shows (not a screen
  of its own); the King and the orc chief stand either side and ask to support the project with a
  rating; the King is the GAME'S OWN HeroKing model (the user rejected the icon's retouched king:
  "a model we use nowhere"); the orc chief is BIG, a head taller than the king.
- Mock-up v2: `Design/store/RateWindow_Mockup.png` by `Tools/store/rate_window_mockup.py <folder>`:
  the game dimmed behind (Load_summer stands in for the game), a wood-framed window whose picture is a
  blurred town, the heroes standing on the window floor with heads over the frame, a parchment column
  in the middle (ribbon «Поддержите нас!», text, 5 stars, green «Оценить», «Позже», close X).
  Heroes from `render_icons_3d.py -- figure_king|figure_orc <abs out> 1200` (whole figure,
  transparent, ortho camera fitted to bounds; figure_king = plain HeroKing, no retouch).
  In the game the heroes would be pre-rendered sprites (cheap on mobile), not live 3D.
- Google Play rules that shape it: our stars are decoration only (the real rating is Google's In-App
  Review sheet or the store page); NO «do you like it?» gating, NO reward for rating; the In-App Review
  sheet has a quota and may silently not show, so a button should open the store page (or our window
  calls the review flow by itself, not from a button).
- Proposed timing (numbers on the sheet): only after a happy moment (raid won, portal closed, tutorial
  done, Town Hall up), never after a defeat, not before N sessions; «Позже» = again in N days, gone after
  two refusals or after «Оценить».

## Open
- Rate window: the user's corrections to the mock-up, then the build (UI, trigger points, sheet keys, texts).
- The user's corrections to both icons.
- The king is not a character in the game yet (if he wins, he may need to appear in the menu/tutorial).
- Feature graphic 1024x500 and screenshots.
