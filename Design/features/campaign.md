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
- The church (`church.py`, CineChurch, ~6 500 tris) is APPROVED.
  - It is the game's Church1 with more detail:
    - stained lancets, a spoked rose window, quoins, stepped buttresses;
    - an OPEN belfry the priest can stand in, with the bell, its yoke and its rope;
    - steps, a strapped door, lanterns, a notice board and a bench.
  - Set pieces are drawn to the game's building standard and then grown by `figure.SET_SCALE` (1.6), so a cutscene figure fits the doors.
  - Render: `church.py -- render <out>`.
- The village (`village.py`) is APPROVED.
  - Fixed on the way: the manor without the half-timber diagonals across its windows, and real tied sacks.
  - The GAME's manor was fixed too (`manor1.py`/`manor1_lvl2.py`, `manor1_lvl3.py`): no braces over the gable windows; the lvl3-5 turret was moved clear of the eaves. Re-exported; Unity not rebuilt yet.
  - The houses are the GAME's hovel, cottage and manor, loaded with `kit.borrow` so their scripts are not run or saved. Life is added round them: washing on a line, a broom, hens, a shop sign, sacks, a rain barrel, flower pots, a cat, barrels and crates.
  - New pieces: two market stalls (striped awnings, fruit and cloth, counter at a figure's waist) and a farm cart (spoked wheels, hay and sacks).
  - Render: `village.py -- render <out>`.
- The square's stones and the portal (`portal.py`) are APPROVED (2026-11-10). With them every model of the opening scene is approved; next is the shot list.
  - It is ONE set of 15 stones (`CinePortalStone_*`) in dark stone with blue runes, the amulet's stone. There are 8 pillar blocks and 7 arch blocks, the middle one the keystone.
  - Each object stands in its RUIN pose: a ring round the cracked round platform `CinePortalSquare`. The bottom pillar blocks still stand; the rest lean, lie on a side or lie on their backs, runes outward.
  - The ARCH pose is the custom property `arch`, a flattened 4x4 world matrix; `set_arch(stones, swirl, on)` switches poses. The cutscene flies each stone between the two poses; how they fly is for the storyboard.
  - `CinePortalSwirl`, hidden in the ruin pose, fills the opening: a door shape, purple ovals, a white heart and swirling dashes.
  - Render: `portal.py -- render <out>`.
  - Round 2: the user found the stones «слишком ровные» and disliked the green squares.
    - Every stone is now lofted from a worn front outline to a differently worn, smaller back one: corners knocked off unevenly, sides leaning, no seam.
    - All moss (squares) is gone; small rubble chips lie on the platform.
    - Pillar blocks sway slightly, and ruin stones are placed so none sits inside another.
  - Render the cast: `blender --background <Models.blend> --python Tools/blender/cinematic/render_cast.py -- <out> CineElder CinePriest`. Nothing is in the game yet; the menu banner says «Скоро».

## What the user decided
- **The opening cutscene's shot list was AGREED on 2026-11-10.** It is about 60 s long, set in the morning.
  1. Wide (5 s): the camera glides over roofs to the square; chimney smoke, birds, the church.
  2. Market (6 s): the merchant calls, the peasant woman picks fruit, the cart rolls by.
  3. Square (6 s): the children jump about the ruin stones, the elder stands by. Ардан carries a sack across the square.
  4. Talk (6 s): the elder stops Ардан, subtitle «Эти камни старше деревни…»; Ардан glances at his amulet.
  5. Sign (4 s): the amulet's rune flashes faintly blue, then the stones' runes light one by one; a rumble, dust. The user KEEPS the amulet flash.
  6. Fright (3 s): the children jump off and run to the woman, and people turn.
  7. Assembly (6 s): the stones rise, turn and fly into place. First the pillars grow from the two standing bases, then the arch, the keystone last, with a flash.
  8. Swirl (3 s): it opens from a point; its light falls on faces.
  9. A beat of silence, then the champion leaps out (4 s): he lands, the platform cracks, he roars.
  10. Flood (3 s): torch and spear orcs pour out.
  11. Chaos (5 s): a straw roof is set alight, a stall knocked over, people flee.
  12. Bell (3 s): the priest pulls the rope in the belfry.
  13. «Беги!» (3 s): the elder shouts it and pushes Ардан away.
  14. Into play (3 s): Ардан drops the sack and runs; the camera swings behind him and the joystick hint appears.
- **Animations come next, group by group, each sent for approval** (the user, 2026-11-10: «Сначала анимации»).
  - Ардан round 2 (`ardan_anim2.py`, it reuses the rig, sack and stage of `ardan_anim.py`) was sent on 2026-11-10:
    - CineArdan_Amulet, 4.6 s: he turns his head to the elder, looks down and brings his left fist to the amulet; the rune flickers, then flashes (an emissive gem plus a point light on the chest bone); he looks up, startled.
    - CineArdan_DropRun, 3.6 s: a flinch; the right hand lets go and the sack falls off his shoulder (baked world keys: it tumbles, thumps and settles); he eases into the run, the cycle driven by distance so the feet don't skate.
    - CineArdan_Run: a 0.72 s cycle at 0.95*S units a second.
  - The left-fist-on-amulet angles were FITTED numerically (upper arm -40/-10/-30, forearm -50/0/-45), because eyeballed angles put the fist in his face or inside his chest.
  - Ардан round 2 APPROVED (2026-11-10).
  - The children (`kids_anim.py`) were sent on 2026-11-10. They play on the REAL ruin stones (portal.py); `stepping_stones` picks a chain of flat tops below 0.36 with hops of 0.25-0.7.
    - KidsPlay (shot 3): the boy hops stone to stone, arms out for balance, then jumps down by the girl; she bounces and waves, the doll in her other hand.
    - KidsFlee (shot 6): both freeze and stare at the centre; the boy jumps down, and they run to the peasant woman.
    - `rig_figure(mesh, scale)` is Ардан's rig at any figure.py figure's scale, and `Track` is a timeline of pose / jump / run moves that bakes the root and heading with crossfades. Reuse both for the other groups.
  - The children APPROVED (2026-11-10).
  - The elder (`elder_anim.py`) was sent on 2026-11-10. He is played against Ардан's approved clips so the timing is judged together.
    - ElderTalk (shot 4): a raised hand stops Ардан; he talks with small nods and sweeps the hand at the stones; when the runes wake he turns to them and draws back.
    - ElderShout (shot 13): the open portal is behind Ардан. The elder starts, draws his arm back, shoves Ардан's left shoulder and flings the arm out the way to run, then watches him go. The shove lands at Ардан's flinch: his clip starts 1 s in.
    - The right hand keeps the staff planted.
    - The left arm is AIMED in world space (`AIM` key: direction + weight, turned about the shoulder after the angles), not guessed in angles.
    - `rig_figure(..., bend=stoop)` moves the joints with the stoop, so his arms pivot at his real shoulders.
  - The elder APPROVED (2026-11-10).
  - The orcs (`orc_anim.py`) were sent on 2026-11-10. They have their OWN rig (`BONES` from orcs_cast's wider, lower joints), passed to `rig_figure(..., bones=)`.
    - ChampionLeap (shot 9): he gathers hidden behind the swirl, leaps out (an arc 0.32 high) and lands in a deep crouch, a fist down. Cracks key in under him and grit chips fly. He rises slowly, roars with arms flung wide and a shake, then turns his head to the village.
    - HordeRun (shot 10): the torchbearer (torch high, axe swinging) and the spearman (shield up, spear forward) run out after him, past the roaring champion, and split left and right. Each path crosses the arch's line 0.2 off its middle, clear of the pillars.
    - Track gained a 'fn' move (hand-made root and pose) and a per-figure `run_pose`.
  - The orcs APPROVED (2026-11-10).
  - The priest (`bell_anim.py`) was sent on 2026-11-10.
    - The church can be built WITHOUT its bell, clapper and rope: `church.build(with_bell=False)`, with the pieces in `church.bell`/`church.clapper` and the belfry's measures in `church.BELFRY`. The approved look is unchanged.
    - CineBell swings about its yoke (x axis), 24° once going, on a 1.15 s period. A timber lever to the front carries the rope; CineClapper follows 0.1 s late.
    - CineBellRope is a unit rod keyed every frame from the lever to between his fists.
    - The priest faces the bell; both arms are AIMED at the grip, which rises and falls with the swing, and his knees and back give on each haul.
  - Groups still to make: the townsfolk (fright, flight, the merchant calling).
- The elder's fate is NOT shown. Later he may turn up as a captive in an orc camp (a future mission hook).
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
- How the cutscene is produced: a Blender animatic first, and the animations the shots need (children jumping, the orc leap and roar, the bell ringing, the shout and push, Ардан's run).
- The level structure beyond mission 1.
