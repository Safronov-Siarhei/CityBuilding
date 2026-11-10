# Store listing (Google Play)

**Status (2026-11-10):** two icon candidates, each as a flat drawing (king corrected once by the
user) and as a 3D render from the game's models; waiting for the user's corrections and pick. Not yet uploaded anywhere; no store screenshots yet.

## Icon
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
  weapon) and box-built arms + a hammer, Cottage1-lvl3 / FortifiedTower1-lvl2 / Hovel1-lvl2 behind in
  depth of field. Output -> `Design/store/Icon3D_<Orc|King>_512.png` + `Icons3D_Sheet.png`.
  Trap: give Blender an ABSOLUTE output path (a relative one lands next to the .blend).
  Run per scene: `blender --background <Models.blend> --python Tools/store/render_icons_3d.py -- orc D:/Fork/CityBuilding/Design/store/Icon3D_Orc_1024.png 1024`,
  then downscale to 512 and build the sheet with `make_icons.masked`.
- Rules: no text and no badges (Google forbids "#1", "free"...); keep the subject in the middle ~80%
  (the mask rounds the corners); judge it at 48 px.

## Open
- The user's corrections to both icons.
- The king is not a character in the game yet (if he wins, he may need to appear in the menu/tutorial).
- Feature graphic 1024x500 and screenshots.
