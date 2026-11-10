# Campaign («Кампания»)

**Status (2026-11-10):** design stage.
- Ардан's cutscene MODEL is APPROVED by the user (`Tools/blender/cinematic/ardan.py`).
- His rig and two animations (walk, carry the sack) are APPROVED (`ardan_anim.py`).
- The old man (`elder.py`, CineElder) and the priest (`priest.py`, CinePriest) are built on Ардан's rig
  through the shared `figure.py`. Both are APPROVED. The priest was fixed first: the stole now lies on the skirt under the sash, and the medallion hangs between the bands on a chain round the neck.
- The townsfolk (`townsfolk.py`) are APPROVED:
  - CineMerchant, CinePeasant (with a sickle), CinePeasantWoman (with a basket), CineBoy and CineGirl.
  - The children are scaled to about 0.67 with heads 1.18x bigger.
  - None of them may look like Ардан.
- The orcs (`orcs_cast.py`) are APPROVED. The torchbearer's axe was fixed first: it is now gripped in the fist with the haft up.
  - CineOrcChampion leaps out of the portal: scaled 1.3, a bare painted chest, one great spiked pauldron, a scar, a topknot and a cleaver.
  - CineOrcTorch carries a torch and an axe.
  - CineOrcSpear carries a spear and a shield with claw slashes.
  - They use the game's orc look and the OrcPalette, on the orcs' own wider joints.
  - Trap: `figure.material()` is made ONCE a session. `ensure_palette`/`ensure_atlas` remake their
    images, so an earlier orc copy pointed at removed images and rendered black.
- The church (`church.py`, CineChurch, ~6 500 tris) is built and its renders went to the user; awaiting approval.
  - It is the game's Church1 with more detail:
    - stained lancets, a spoked rose window, quoins, stepped buttresses;
    - an OPEN belfry the priest can stand in, with the bell, its yoke and its rope;
    - steps, a strapped door, lanterns, a notice board and a bench.
  - Set pieces are drawn to the game's building standard and then grown by `figure.SET_SCALE` (1.6), so a cutscene figure fits the doors.
  - Render: `church.py -- render <out>`.
  - Render the cast: `blender --background <Models.blend> --python Tools/blender/cinematic/render_cast.py -- <out> CineElder CinePriest`. Nothing is in the game yet; the menu banner says «Скоро».

## What the user decided
- The campaign has **cutscenes, a story and levels**. The story frame: a big human kingdom, and an orc
  portal opens on its outskirts.
- **Mission 1:** a portal opens in a village and the horde burns it. The player is **Ардан** (a named
  hero, a brave lad who will become someone strong and important), fleeing to warn the nearest town.
  - It must be **quick and easy**, about 5–8 minutes. Claude recommended a plain chase (orcs slower than
    him, scripted scares, no stealth); the user has not confirmed this yet.
  - **Controls:** a floating joystick that appears where the finger touches. No dash for now.
    A **non-intrusive hint** shows that the joystick exists. No minimap in this mode.
  - **Death:** checkpoints per zone.
- **Opening cutscene (the user's version):**
  1. A beautiful village: market lanes, houses, a main square, a church, and old stone ruins in the
     middle of the square. Peaceful life: trade, field work, a church service.
  2. The ruins assemble into a glowing portal, an orc leaps out, and chaos and the invasion begin.
  3. **The old man** who spoke to Ардан on the square («эти камни старше деревни…») shouts «Беги!».
     **The priest** rings the alarm bell.
  4. All speech is **subtitles at the bottom** (not the tutorial's dialog plaque).
  5. Cutscenes run in-engine. **Every cutscene's actions are discussed in detail before it is built.**
- **Cutscene models:** a SEPARATE, more detailed set from the gameplay models, but in the **same blocky,
  flat-palette style as the King and the townsfolk**. The player must see that it is one game.
  (A smooth, realistic first try was rejected on 2026-11-10.)
- **The order of work (the user):**
  1. The model is approved.
  2. Its animations are approved.
  3. Then the other cutscene models are made.
  4. Only after that is the cutscene video made.
- Model renders go to the user for approval before anything enters the game.

## Ардан (the model)
`CineArdan`, about 1 750 tris, in the «Кино» collection of `Buildings-Size1-Models.blend`.
It uses the shared palette material, and only palette colours (the palette is full).

What marks him out:
- ginger hair with a forelock swept over his right brow;
- a red neckerchief: a bandana triangle on the chest, knotted at the back (the first version, tails knotted at the side, looked like a flag stuck on the vest);
- a green tunic under an open, laced leather vest;
- a dark stone amulet with a blue rune: the stone of the portal ruins, a story hook;
- a bracer on his left arm, a knife behind his back, a pouch, a knee patch;
- big eyes with glints.

Commands:
- Render: `blender --background <Models.blend> --python Tools/blender/cinematic/ardan.py -- render <out dir>`.
  This never saves.
- Store in the file: `-- save`.

## Open
- Ардан's rig, approved:
  - The rig is rigid, like the game's jointed figures: each part of the model rides one bone, through the vertex groups of `ardan.PARTS`.
  - Actions: `CineArdan_Walk` (30 frames) and `CineArdan_CarrySack` (34 frames).
  - Demo clips: `blender --background <Models.blend> --python Tools/blender/cinematic/ardan_anim.py -- render <out> [walk|carry|all]`.
  - `ARDAN_SHEET=1,5,9` renders those frames as PNGs instead of the clips.
- Chase versus stealth for mission 1.
- The cutscene shot list, detailed action by action.
- The rest of the cinematic set: houses, the market stalls, the cart, the
  square's stones and the portal.
- The level structure beyond mission 1.
