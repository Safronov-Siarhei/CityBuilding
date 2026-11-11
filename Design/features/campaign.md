# Campaign («Кампания»)

**Status (2026-11-10):** the opening cutscene is moving into Unity. Parts 1-4 BUILT (village + sky, the cast, the living background, the DIRECTOR): the whole 60 s film plays in Unity; v2 fixed the user's notes 1-3; **v3 (2026-11-10, dadba0246) = the EFFECTS the user chose**, mp4 sent, awaiting notes. **v4 (2026-11-11) = SOUND**; film ACCEPTED by the user 2026-11-11 after v10 + hand-placed props; **v5 (3b06d52d4) = the user's notes on v4** (see below), mp4 1080p sent. v6 = torch flame live (`CineFigure.LightTorch`, particles at the left forearm part's farthest vertex; modelled flame removed in orcs_cast.py) + SPEECH BUBBLES (user's choice): sub cues carry `"speaker"` (NOT `actor` -- a cue with actor is dispatched as an actor cue and the line never shows), bubble sprites drawn by setup (BubbleBody 9-sliced, BubbleTail), follows the head, clamped in frame, tail points at the speaker; numbers in subtitle.bubble*. Film MSAA 4x: 8x at 1080p crashed the GPU driver (and the Claude app with it). Still to do from the user's choice: embers/ash, washing lines, birds/animals and villagers at work (new models -> renders for approval first). Next after that: the handover into mission 1 (not built).
- **Effects (v3):** `Scripts/Campaign/OpeningFx.cs` on the village object, stepped by the director on the
  film clock (`Step(time, dt, camFrom, camAt, lens, dof)`); every number in the `fx` block of
  `Campaign/OpeningScript.json`. Portal: `Shaders/CineVortex.shader` quad in the arch (door given as
  `doorCentre`/`doorSize`, NOT from the swirl mesh; the old swirl disc stays hidden when fx is set),
  opens from its heart (`_Open`), `_Flash` + point light + `CineGroundFx` shock ring (mode 1) + cube
  debris + inward sparks. Runes: `CineRuneGlow_*` FBX (export_cine.py, OpeningSet stones' `glow`) laid
  on each stone, `CineGroundFx` mode 2 (flat additive, Offset -1) lit 0.1 s apart from 20.35. Cracks:
  mode 0 quad ON the platform (`cracksHeight` 0.085 -- at ground level the platform hid it). Arcs
  (LineRenderers) + dust puffs during the assembly. Mood: runtime skybox copy, sun, ambient, fog lerp
  to lilac-crimson from 34.3 over 6 s. Post: `Campaign/OpeningVolume.asset` (Bloom, Vignette,
  ColorAdjustments, DepthOfField Bokeh), camera post on; shots with `"dof": true` focus on |at-from|.
  Chimney smoke from `Campaign/Chimneys.json` (`Tools/blender/cinematic/chimneys.py`, Blender model
  coords mapped (x, z, y), `chimneySigns` if mirrored) until 46 s. Title `#cine_opening_title`
  (sheet row 745) 0.6-4.8 s.
  - Trap: `BuildingDetail.shader` (palette material of buildings, set pieces AND the cast) had no `Cull [_Cull]` -- setting _Cull did nothing until 2026-11-11; verify a look fix in a FRAME, not by the code.
  - Traps: the Editor asmdef needs `Unity.RenderPipelines.Core.Runtime` for VolumeProfile; run
    `run.ps1 -Steps a,b` from the PowerShell tool (from bash the list arrives as ONE unknown step and
    nothing runs, report says only RUN FINISHED); Blender for encode_frames is
    `G:\SteamLibrary\steamapps\common\Blenderlender.exe`.
- **v5 fixes (2026-11-11):** `CineTwoSided` gives houses, set pieces and figures two-sided copies
  of their URP Lit materials (_Cull 0) -- the game models leave out faces the top-down camera never
  sees (head bottoms, roof and stall-cloth undersides); the belfry's dark back wall removed from
  church.py (a black slab in Unity); the amulet light found by bone name (the old path missed, the
  light sat at his feet) + `OpeningFx.AmuletGlow` halo, range 0.35; the 32 s chime cue removed;
  storm: `Shaders/CineStormSky.shader` dome on the camera, cover 0->1 from 33.9 over 7 s; fog
  3-30 after the portal (hides the empty far town); film frames 1920x1080 JPG 95 (encoder takes the
  frame size). Unity may crash on QUIT after the film (finalizer in AssetDatabase) -- frames are
  complete if the log has `[CampaignFilm] 1809`.
- **v8 (2026-11-11, b74a08a44):** speech bubbles REJECTED by the user -> subtitles at the bottom
  middle, white, shadow + soft outline, anchors 0.22-0.78 (no speaker name). Ground: `Shaders/CineGround.shader`
  (world-space, modes grass zones / cobble / dirt lane with ruts along `_Dir` / field furrows), palette
  in sRGB converted to linear, fog PER PIXEL (the grass is one 800 m quad -- per-vertex fog whitened
  it). `OpeningVillage.MoreFields` (~70 strips 20-54 m out) + `Decor` (Map 3's decor kit tufts/flowers/
  pebbles/bushes, 25 m combined chunks, no shadows).
- **v9 (2026-11-11, 9dc8a7bf6):** CineTwoSided REDONE: culling off made flush details (windows,
  doors, roof tiles) z-fight their walls -> flicker; now each renderer gets a twin "Back" drawing
  only back faces (_Cull Front) pushed deeper (`BuildingDetail` `_OffsetFactor/_OffsetUnits`, 0 in
  the game). church.py: nave roof starts at the tower's back wall, belfry `stone_top` U_TOP+0.8
  (ridge was level with the belfry floor) -> bell/priest/rope re-exported, bell shot camera +0.288.
  Bushes along road verges. FARM PROPS `Tools/blender/cinematic/farm_props.py` (CineHaystack,
  CineHayBales, CinePigPen, CineHenYard, CineWashing from approved pieces): renders SENT FOR
  APPROVAL, not exported, not in the film.
- **v10 (2026-11-11):** ground layers each at their own height (OpeningVillage FieldY 0.004 < RingY
  0.008 < LaneY 0.014 (+0.003 on odd segments) < CobbleY 0.021, square +0.003; one seed per street,
  cobbles seed 0 so overlaps match), camera near plane 0.25 -- a lane flickered. Farm props exported
  (`farm_props.py -- export`) and placed by `OpeningVillage.Props` (stacks/bales by roads past 13 m
  and at field edges, pens/hen yards at farms past 26 m, washing at town houses). Then (user: «стогов
  не видно») 16 props SET BY HAND in camera view: `Campaign/OpeningProps.json` (x, z, yaw; placed by
  `OpeningVillage.Featured` right after the streets, before the houses, nudged in rings up to
  `search` m off a road). The user then said the film is fine: «остальное нравится, можно идти дальше».
- **Sound (v4, 2026-11-11):** the user chose free sounds. 18 CC0 clips from OpenGameArt + our own
  synthesised church bell (`Tools/audio/bell.py`; the only bell found was CC-BY-SA) in
  `Assets/_Project/Audio/Campaign/` (sources in its `CREDITS.md`), cut/faded/re-encoded to OGG by
  `Tools/audio/cut.py` (Blender mixdown -- no ffmpeg on this machine). Music: mus_village (Old Tower
  Inn) 0 s, mus_portal (Evil Approach 14-33 s) 20.2 s, mus_attack (War Theme 36-62 s) 36.6 s.
  Cue sheet = `"sounds"` in OpeningScript.json (t, clip, volume, length, fadeIn, fadeOut, loop),
  played by `Scripts/Campaign/OpeningSound.cs` (pooled 2D AudioSources, play mode only, StopAll at
  the end) and laid into the mp4 by `encode_frames.py ... <script.json> <audio folder>` -- one sheet
  for both. Bell strikes follow the clapper hits of the bell track (48.4 + 0.6/1.13/1.63/2.23/2.8/3.37).
  Import (ModelImportDefaults.OnPreprocessAudio): mus_/amb_ streamed, sfx mono + decompress on load.
- **Unity part 1 (2026-11-10):** scene `Scenes/CampaignOpening.unity` (NOT in the build list), made by
  `SetupProjectCampaign.cs`: procedural morning sky `CampaignSky.mat`, low warm sun, trilight ambient,
  linear haze fog 45-140. `Scripts/Campaign/OpeningVillage.cs` lays the village out on Start (seed 11):
  ground, square, the set pieces from `Campaign/OpeningSet.json`, cobble/dirt streets + a ring road r17,
  ~90 of the GAME's buildings (prefabs stripped of behaviours/colliders) scaled by 1.6, 14 fields with
  farmsteads, up to 1100 trees. The set pieces (church without bell, bell, clapper, portal platform,
  swirl, 15 stones with ruin+arch poses, stalls, bolts, fruit, cart) come from
  `Tools/blender/cinematic/export_cine.py` into `Models/Cinematic/`. Photos: `run.ps1 -Steps campaign`
  -> `RunLogs/campaign/*.png` (`CampaignPhotos.cs`). Next: characters with animations, living background,
  the director (cameras, cuts, subtitles), the handover to mission 1.
- **Unity part 2 (2026-11-10): the cast.** `Tools/blender/cinematic/export_cast.py` exports the 11
  figures to `Models/Cinematic/Cast/*.fbx`, each on its rigid rig with every clip a take, baked from the
  approved pose functions: all humans Idle/Walk/Run/Stare/Talk (loops); kids + Cheer/Balance/Crouch;
  Ардан + WalkSack/Amulet/Drop (the sack is its own mesh on the chest bone); woman Scream, peasant
  Startle, merchant Panic; orcs Idle/Walk/Run (each orc his own run); the champion + `Leap` (the only
  clip with root motion, from behind the swirl to the landing). Clips are IN PLACE; the director moves
  the figures. `Campaign/OpeningCast.json`: each figure's clips (length, loop) and walk/run speeds; the
  importer loops clips from it and names them without the `<rig>|` prefix. A figure faces Unity +Z.
  - Traps: the .blend runs at 24 fps -> export sets 30 (else every take stretches); `bakeAxisConversion`
    is OFF for the cast and the export uses -Z forward (the standard Blender->Unity armature setup);
    townsfolk clips need `bell_anim.apply` (aims); in the EDITOR an imported clip poses nothing through
    `SampleAnimation` or a playable graph (a playable graph even T-posed the kids) -- CampaignPhotos reads
    the curves onto the bones itself. In play mode an Animator plays them normally.
- **Unity part 3 (2026-11-10): the living background.** `Scripts/Campaign/OpeningLife.cs` on the village
  object, every number in `Campaign/OpeningLife.json`. `Begin()`: walkers stroll the LANES (the roads out
  of the square, `OpeningVillage.Lanes`, each from its square end out; the ring road is not a lane),
  pause to idle/talk; pairs chat on the square. `Panic(origin)`: each reacts (Scream/Startle/Panic/Stare)
  after a delay growing with distance, then runs OUT along its lane (square standers first to the
  nearest lane mouth) and vanishes past the end. `ReleaseOrcs(portal)`: torchbearers and spearmen in
  turn, one per lane round-robin; torchbearers leave the road for the nearest unclaimed house in reach,
  stand, set the roof alight (flames = stretched additive particles, smoke = alpha particles,
  `CampaignFlame/CampaignSmoke.mat` on a generated `SoftDot.png`); spearmen chase the nearest fleeing
  villager for a few seconds (people are faster). `Step(dt)` runs it (photos step it by hand);
  autoBegin starts it in play until the director takes over.
  - Figures play through `CineFigure` + `CineClips` (no Animator): setup bakes each cast FBX's takes
    into `Campaign/Cast/<Figure>Clips.asset` at 15 fps (bone rotations, positions of bones that move).
- **Unity part 4 (2026-11-10): the director.** `Scripts/Campaign/OpeningDirector.cs` plays
  `Campaign/OpeningScript.json` (the film: shots with camera keys in Unity coords + lens in mm on a
  physical 36x20.25 camera, the actors, the cues on ONE clock -- one world, so a shot's background is
  never empty). A cue: an actor plays a PERFORMANCE (`perf`), runs/walks a `path`, or stands (`at`/
  `face`/`yaw` + `clip`); events `life`, `panic`, `orcs`, `ignite`, `objects` (set-piece tracks; `hold`
  freezes frame 0), `values` (amulet/runes/flash/swirl lights, bell/clapper swing), `rope`, `sack`
  (Ардан's sack baked off the skin and dropped), `sub` (who + key from the localization sheet:
  `#cine_name_elder`, `#cine_opening_stones`, `#cine_opening_run`).
  - PERFORMANCES (`Tools/blender/cinematic/perfs.py`, exported by `export_cast.py`): the approved
    Blender motion of a figure in a shot, baked as an in-place take posed where it stood (aims right)
    plus its root path in `Campaign/OpeningTracks.json` (village frame, Unity axes, yaw = 180 - heading);
    the set pieces that move with them (cart, fruit, toppled stall + bolts, stones' assembly, swirl,
    bell rope points, light curves) sampled there too. Shot frames V/M/T/B/R map onto the village.
  - The user's notes 1-7 as built: kids keep hopping on the stones until 23.5 s (`P_Play`, ping-pong);
    the mother runs in at 20 s, calls, the kids jump to her, all three run off east IN VIEW of Ардан and
    the elder (fright camera beside Ардан); the market raid's orc comes from the square, not out of a
    house; the chaos shot shows the portal, the champion and the heroes behind; the champion roars
    and walks at them in the run shot; the elder shouts «Беги в город, Ардан! Предупреди всех!» and runs
    west; Ардан drops the sack and runs south down the market road; the last shot follows him from
    behind (mission 1's start; the handover itself is not built).
  - Elder/Ардан stand at the talk spots until the run shot, then at the run frame's (a cut hides it).
  - Film to mp4: `run.ps1 -Steps film` (CampaignPhotos.Film, 1809 JPG frames in ~40 s) then
    `blender --background --python Tools/blender/cinematic/encode_frames.py -- RunLogs/campaign/film <out.mp4> 30`.
  - Traps: a shell whose cwd is inside RunLogs/campaign/film locks it and the film step dies on delete.
  - Not yet: nobody has watched it in play mode on a device; the handover to mission 1; sound.
- **The user's notes on the Unity film v1 (2026-11-10)** -- FIXED in v2 (f1bff537d): the sack floated and
  the hand hung (cause: skinned meshes never re-skin in the frame-by-frame editor film, so EVERY figure
  stood in its bind pose -- now `CineFigure.Rigidify` cuts each skinned mesh into rigid parts on their
  bones, like the game's figures; cheaper on phones too); holes in the belfry (the church's faces now
  both ways, `export_cine.double_sided`); a foot poking back out of the dress (women run with
  `perfs.skirt_run`).
- **The user's CHOICE of additions (2026-11-10, after v2):**
  - Portal: living swirl (shader spiral + sparks drawn in + pulsing light), assembly energy (runes truly
    glow, blue arcs between flying stones, dust under rising stones), opening blast (flash, shockwave
    ring, debris, hard shake), glowing cracks from the platform when the shaking starts.
  - Atmosphere: the sky darkens after the portal (sun dims, lilac-crimson, thicker haze), bloom +
    grading + vignette (film only), sparks and ash over fires with flickering firelight.
  - Filling: birds flee at the first shake + animals (hens, dogs, cows/sheep in fields, a beast at the
    cart); villagers at work (fields, wood, buckets, kids at tag); chimney smoke, washing and flags.
  - Cinema: SOUND AND MUSIC, depth of field on close-ups, a title at the start.
  - Not chosen: champion-landing dust, orcs smashing things, letterbox.
  - Traps: Blender `matrix_world` lags a set location until `view_layer.update()`; ground materials need
    smoothness 0 (the sky's brown lower half mirrored in the grass); the editor's first `Render()` drew
    no ground while its shader compiled in the background -> CampaignPhotos renders a warm-up frame.
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
  - The priest APPROVED (2026-11-10).
  - The townsfolk (`townsfolk_anim.py`) are APPROVED (2026-11-10), after the topple fix. **Every animation of the opening cutscene is approved**; next is the video.
    - Market (shot 2): the merchant calls (hand waving high, the other cupped at his mouth), then turns to the woman with an open palm. She reaches for a fruit, looks it over and drops it in her basket. A peasant pulls the cart past: the shafts follow his fists, the body pitches about its axle, the wheels turn by the distance rolled.
    - Panic (shot 11): a torch orc storms in. The woman shrieks with her hands to her face and runs; the merchant ducks behind his counter, arms over his head, trembling. The peasant flings the shafts (the cart drops onto them) and runs. The orc topples the cloth stall backward as he passes.
    - Round 2 of the topple: the user found the first one strange. The whole stall had tipped backward with its baskets and crates floating along and the goods glued to the counter, and the orc never touched it.
      - Now the orc's right hand hooks the counter as he runs past and wrenches it.
      - The FRAME falls forward into the street about its front-bottom edge, slow then ever faster, and lands on its fringe at 80° with a jolt.
      - The bolts ride the counter until it tilts past about 20°, then slide off, tumble and roll to a stop.
      - The baskets and crates stay on the ground. `village.stall(parts=)` and `village.stall_goods(only=)` build the pieces.
    - Built for it, with the approved look unchanged: `village.cart(wheels=False)` + `village.cart_wheel` + `village.CART`; the stalls one by one through `village.stall`.
    - `kids_anim.mix_any` crossfades poses that do not share keys (aims fade by weight).
- **The Blender film, first cut, was sent on 2026-11-10**: 1662 frames, 55 s, 1280x720, no sound.
  - `cutscene.py -- <shot> <dir>` renders one shot (12 shots: wide market square talk fright assembly swirl leap horde chaos bell run) as PNGs.
    - It builds the whole village under CineVillageRoot: square r 4.3, church at (0, 7.4), 8 houses facing the square, trees, a market street with the stalls at (1, -5.2).
    - The village is moved UNDER each shot's own animation frame; the talk shot turns it 180°, so Ардан is always south of the ruins.
    - The file's saved Cine* objects are deleted first, and its game models are hidden.
    - New effects: rune glows lighting one by one, the stones flying ruin->arch, the swirl growing (no spin; a spin tilts its door shape), the purple portal light, the roof fire, camera shake.
    - CUT_SMALL / CUT_SHEET are for checks.
  - `cutscene_edit.py -- <dir> <mp4>` joins the shots in the sequencer, with the subtitles as text strips at the bottom. EDIT_STILL=<frame> renders one frame for a check.
- **The user's notes on the Blender film (2026-11-10).** The decision: «можно переносить в юнити и доделывать сцену там». The film itself is NOT reworked in Blender. The notes are REQUIREMENTS for the Unity scene:
  1. ~22 s, when the shake starts: the children are gone from the stones in the square's middle. They must still be playing there when the runes wake.
  2. ~24 s: the children are standing with their mother there. They must visibly run off with her.
  3. ~26 s: Ардан and the elder look at the centre, but the children and mother are not seen running away. Their flight must be in view.
  4. ~31 s: the bottom pillar stones had sunk into the ground. This is FIXED in portal.py: the standing bases now take their arch pose, on the platform.
  5. ~43 s: an orc runs out of a BUILDING (he started inside it), and the portal in the background stands empty, without the champion, Ардан or the elder. The background must carry on the same moment.
  6. ~51 s: the champion just stands by Ардан and the elder. They must start running, and the elder tells him to run to the town («в город», or similar). The champion must act, not stand.
  7. Missing in general: environment. Unity needs a full, BIG village, a sky, many more people walking about, and orcs with movement logic (not set paths). The background is dull and empty now.
- The user asked: does play begin right after the scene? Yes, as planned: shot 14 hands over to mission 1 (the camera behind Ардан, the joystick hint). That handover exists only in Unity.
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
- **Mission 1 «Бегство» (the user, 2026-11-11):** a CHASE. Play starts where the film's camera stopped, in the game's own third-person view: the main game's camera (pitch 50), NO zoom, a static view (no turning), Ардан always centred and never deselected. Route: village -> tall-wheat field -> forest -> river and bridge -> town gates. Near the bridge an orc latches onto him; after the bridge a mage says «Беги в город, я их задержу»; then he just runs to the gates. **The level map must be approved BEFORE anything is built** (the user, 2026-11-11). Draft v1 sent: `Design/mission1_map_v1.png` (drawn by a PIL script from the film's real street plan; the route runs south down the market street, so the camera looks south and the run goes up the screen): ~262 cells of path (~4.6 min of pure running at 0.95 cells/s, 5-7 min with detours), 10 marks, checkpoints at the village exit, the forest edge and before the bridge; wheat hides Ардан from the orcs on the field's cart track; orcs sweep the forest. **MAP v1 APPROVED by the user (2026-11-11).** The mage scene (the user's answers): a SHORT CUTSCENE of 3-4 s (stick hidden, camera on the mage, then back to Ардан); once Ардан is across, the mage STRIKES THE BRIDGE and it collapses with the chasing orc; the mage stays on the bank facing the orcs over the river. Line «Беги в город, я их задержу».
  - **Stage 1 BUILT (2026-11-11): the handover and the controls**, in the film's village. `MissionOne.cs` takes over on the director's `Finished`: `director.Release("ardan")` (the director stops moving him), `KeepAlive` (the world -- orcs, fire, smoke, folk -- goes on stepping after the end), `CameraReleased`; the camera eases (blend 1.4 s) from the film's physical lens into the game's view: pitch 50, yaw 180 (looks south, the run goes up the screen), FOV 60, distance 6 (9 made Ардан tiny), Ардан always centred, no zoom/turn. `FloatingJoystick.cs`: appears where the finger lands, knob clamped to the ring, below walkBelow 0.5 he walks, WASD/arrows on PC; the hint (ghost ring, gliding dot, line `#m1_hint_move`) shows after 0.8 s and fades once he has run 1.5 s; its circle sprites are drawn in code. Collision: `OpeningVillage.MakeSolid` boxes every house/prop/named set piece (fitted to its meshes in its own frame) and puts an upright capsule post at each tree; Ардан has a CharacterController (y held at 0, the village is flat). Numbers: `Campaign/Mission1.json`. Check: `run.ps1 -Steps mission` (`CampaignPhotos.Mission`: film run unseen, the stick scripted, path logged as `[Mission1]`, frames to RunLogs/campaign/mission/; the overlay hint does not show in them).
  - **Route models sent for approval (2026-11-11), NOT in Unity yet:** `Tools/blender/cinematic/mage.py` (CineMage: a man in his prime, short dark beard, deep-blue robe with gold hem, purple cape with a high collar, broad-brimmed pointed blue hat, forked staff with a violet crystal; on Ардан's rig; in render_cast.py) and `route_props.py` (CineWheat 0.6 tile -- waist-high body, ears to his chest, WHEAT_H 0.26 after the first try hid him whole; CineFallenTree; CineBushClump; CineBridge -- 7 loose deck bays on piles, for the collapse; CineTownGate -- gatehouse with voussoirs, shield, slits, lanterns, corbels, two round blue-coned towers, curtain wall; CineTownWall). `-- render <dir>` / `-- export <project>`. TRAP: install_static keeps ONE object per name -- the render copies one wheat object and moves one Ардан per shot.
  - **The user on the route models (2026-11-11):** mage, wheat, fallen tree, bushes kept; the made-for-the-film bridge and castle REJECTED -> the sandbox's own models: Barbican (FortifiedGate1-lvl3) + level-3 wall (Fence1-lvl3-Straight) + two towers (WallTower-lvl3 chosen -- swap in Mission1.json "models"/"town" if the user meant Tower1-lvl3), the bridge = the sandbox's Bridge (Bridge1-lvl1 chosen, the level was not said), the river exactly 3 bridge pieces wide, the MIDDLE piece the mage destroys. Roads must not be straight (the user: «дороги прямой формы, выглядит не естественно»).
  - **Route BUILT in the scene (2026-11-11)**: `OpeningVillage.Route.cs` (partial) reads Mission1.json "route": its areas are reserved right after the streets (Free() refuses them, so village houses/fields/trees/decor keep out), laid last: the dirt track from the market street's end, the river (CineGround mode 4 water, banks, BoxCollider edges every 1 m except at the bridge), 3 bridge pieces on the river's real centre (`BridgePieces`, `HeightAt` = deck 0.352 with ramps, MissionOne puts Ардан on it), the wheat (one slab mesh in 0.24 cells, CineGround mode 5, the track cut out; CineWheat ear tiles only within earsBand 9 of the track -- a whole field of ears would be millions of tris), the forest (game trees every 2.2 m jittered, bushes, the fallen tree across the track 125 m along, trees cleared 3 m round both its ends), the town (Barbican with two side colliders leaving the passage open, 10 wall pieces each side, a tower at each end, houses behind, a cobbled street). ALL ROADS NOW CURVE: `Street` lays every road along Catmull-Rom through its points (1.2 m steps); dirt ones also meander (`wiggle`, Perlin, fading at the ends and near `_pins` = bridge, gate); the river meanders too (river.wiggle). Check: `run.ps1 -Steps mission` autopilots the route (sidestepping alternately when stuck) -- 256 s to the gate.
  - **Light and wheat fixed (2026-11-11, the user chose «Свет и пшеница»):** `OpeningFx.Calm` (0..1) scales the portal's mood (sun, sky, fog, ambient, the storm dome) back to the morning; MissionOne sets it from Ардан's distance to the square (Mission1.json calm from 30 to 75 m) -- the village stays stormy, the field is in daylight. The wheat slab is now cut by MARCHING SQUARES over min(distance to track - clear, inside the outline), cells 0.48: a smooth edge, side walls only along it (each face its OWN vertices -- shared ones gave cancelled, black normals). Every road and the river are one RIBBON mesh each (`Ribbon`, UV0 = metres along/across): no notches on bends, and CineGround's dirt ruts and water ripples now follow the road's own along/across (before they were measured from the world origin, so ruts barely showed); water lighter along the banks.
  - **Bridge steps (the user, 2026-11-11: the deck's ends hung in the air -- «им нужны какие-то ступеньки»):** `route_props.py` CineBridgeSteps -- 3 timber treads on 2 sloping stringers down from the sandbox Bridge's deck (DECK 0.22), a post at each top corner, a stone footing; origin at the bridge end, running to -y. `-- render_steps <dir>` renders it with 3 Bridge1-lvl1 pieces. APPROVED and IN THE SCENE (2026-11-11): exported, one at each end of the bridge (Mission1.json bridge.steps/stepsYaw 180 -- the north one, the south turned 180 more), HeightAt's ramp 0.66 = the flight, so Ardan climbs the steps instead of popping onto the deck.
  - **Orcs and the chase BUILT (2026-11-11, the user: «Да, орки и погоня»):** `MissionChase.cs` (plain class owned by MissionOne; numbers in Mission1.json "chase"). 8 patrol orcs (film cast CineOrcTorch/CineOrcSpear via `director.Member`, torches lit, own CharacterControllers): 3 in the village streets, 3 to and fro on the field's new CART TRACK (route.field.cartTrack, a dirt lane across the field, cut out of the wheat), 2 sweeping the forest. An orc SEES Ардан within sight 6 m -- in the tall wheat (`OpeningVillage.InWheat`: in the field, off both tracks) only within 1.3 m -- runs at 0.8 m/s (Ардан 1.007), gives up past 8.5 m, walks back to its beat. The AMBUSH orc hides by the bridge (9,-173), bursts out when he comes within 4.5 m of (-4,-172), runs at 0.97 and stops at the bridge's far end (stopZ -185.5) -- the mage's place, scene not built; standing there it still catches. Caught within 0.45 m: 1 s beat, fade to black (0.45 s), back at the last CHECKPOINT (village exit (-6,-44), forest edge (-2,-104), before the bridge (-4,-166)), orcs reset; notice `#m1_checkpoint` «Контрольная точка» (CSV + sheet row 31 of «Тексты: обучение и сюжет»). The film's own life orcs in the village are scenery only. Check: the autopilot reaches the gate uncaught (two field orcs chase it to ~0.7 m), then walks back into the bridge orc: caught once, respawned at (-4,-166).
  - **The mage scene, AGREED with the user (2026-11-11):** when Ардан comes off the bridge (the ambush orc behind him) control and the stick go for 3-4 s; the camera comes down to the mage by the track; he flings his left arm toward the town -- «Беги в город, я их задержу» (subtitle) -- as Ардан runs past; he turns to the bridge, strikes his staff down (the crystal flares) and thrusts his left palm: a VIOLET ORB flies into the middle piece, which breaks and falls into the river with the orc on it -- the CURRENT CARRIES THE ORC AWAY; control returns; the mage HOLDS THE BANK (glowing staff) while orcs gather on the far bank and roar; no more orcs on the way to the gate.
  - Mage animations `mage_anim.py` (CineMage_Idle / _Wave / _Cast / _Hold, each in his own frame facing -y, the director turns his root) -- videos SENT for approval. The staff is part of the right forearm in the approved model, so it is LIFTED (elbow forward, forearm back by as much: stays upright) and STRUCK, never swung overhead; the orb leaves the LEFT palm. Not exported yet (export_cast.py + clips after approval).
  - **Mage scene BUILT (2026-11-11, round 54; the user approved the animations: «Подходят, собирай»):** CineMage exported with clips Idle/Wave/Cast/Hold (export_cast.py). `MissionMage.cs` (plain class owned by MissionOne; numbers in Mission1.json "mage"): the mage stands at (-2.5,-190) facing west; when Ардан passes z -186.4 the stick is LOCKED (`FloatingJoystick.Locked`), the chase stops (`MissionChase.Stop()` hands over the ambush orc, hides the patrols), Ардан runs on by himself to (-4.2,-196); the camera CUTS to a low shot by the mage inside a short dip to black (a blended turn swung the world 180 deg: the shot looks north, the game south), and cuts back the same way at returnAt 7 s. Subtitle via `director.Say` = `#m1_mage_line` «Беги в город, я их задержу!» (CSV + sheet row 32). Wave -> turn north -> Cast: crystal flare (point light + sparks), violet orb (fx.SparkMaterial particles + light) from the left palm into the middle piece, flash, the piece shakes, tilts and sinks, splash (dust particles), `village.BreakBridge` adds a BridgeGap collider; the ambush orc (now stopZ -181.9: it halts on the middle piece) rides it down and drifts downstream flailing until gone. Then Hold with a glowing crystal; 5 orcs (champion roars) run down to the far bank and stand facing him. Check (run.ps1 -Steps mission): route to the gate 0 catches, frames mage_wave/strike/orb/fall/drift/hold; going back north stops at the gap (z -183.1). TRAP: particles in the editor check need `Simulate` by hand. Not seen by the user, not on a phone.
  - **Gate end BUILT (2026-11-11, round 55; the user: «Стража и затемнение», «Итог + в меню»):** `MissionEnd.cs` (numbers and keys in Mission1.json "finish"). Past z -238.6 (the Barbican's passage) the stick is locked for good, Ардан runs on to (-4,-243.5); two town guards (the sandbox's SoldierSpearman-lvl3 at x1.6, FigureAnimator, wired as MissionOne.guardModel by setup -- no new model) step together behind him facing the river; black from 1.6 s; «Миссия выполнена» (#m1_done) at 2.7 s; at 4.2 s the summary on a ScreenSpaceCamera canvas (so the check's frames show it): Время m:ss (#m1_time; the run's time until the gate) and Поймали N (#m1_caught), buttons «Ещё раз» (#m1_retry: `MissionOne.SkipFilm` + reload; MissionOne.Start runs the film through unseen at dt 1/15 under the loading screen; the director's Start no longer replays a finished film) and «В меню» (#m1_menu -> MainMenu). An EventSystem is made if the scene has none. Texts in CSV + sheet rows 33-37. Check: END reached, summary «Время: 6:24 | Поймали: 0» (the time includes the check's walk back to the bridge). Untested by hand: the buttons, the retry's film skip time on a phone. Town houses right by the wall hide the guards' close a little.
  - **TRAP (2026-11-11, the user saw an empty village in Play):** the cast FBX were not Read/Write, and CineFigure cuts every figure into parts at runtime -- in Play mode (and on a phone) every villager, orc, Ардан and the mage came out empty, with «Not allowed to access triangles» in the log. The batchmode checks never saw it: edit mode reads any mesh. Cast FBX are now readable (ModelImportDefaults). LESSON: a batchmode check is not Play mode -- read the user's `Logs/Editor.log` after they play.
  - **FIXED (2026-11-11, the user: «зданий нету»):** since the streets curve (round 47) each street is many 1.2 m pieces, and StreetHouses / the roadside haystacks walked each PIECE from 1.5 m in -- the village kept only 9 of its 91 buildings (the film AND the checks). Now they walk the whole line (`AlongLine`). Regression test `Tests/PlayMode/CampaignPlayTests` (REAL Play mode: >60 buildings, every film figure has parts; frame RunLogs/campaign/play_probe.jpg).
  - **First Unity playtest list (2026-11-11, 26 items) BUILT, round 56** (memory project_pending_2026_11_11_mission1): the film waits for the loading screen (director Start is a coroutine on `LoadingScreen.Busy`); two houses at the square's empty corners (OpeningProps.json "houses", placed facing the square); `MissionChase` rewritten: orcs step round what stops them (detourSeconds), a small DRAWN red eye over an orc that sees him (eyeSize 0.2, procedural sprite; the user rejected a realistic first one), a roar (`Spotted` -> sfx_roar) on first sight, 3 HUNTERS out of the village after him (chase / last seen / search / trail, give up at the forest), 3 more village patrols, the ambush orc closes to 2 m behind him (on his heels at the crossing); `MissionWorld.cs`: dungeon-style DARKNESS off the way (DungeonDark shader over a 1 m mask: village r 30, track +-4.5, the field, the gate) + an invisible wall of boxes along its edge, checkpoint flags (Flag1-lvl1) + rings (gold, green once reached), a pillar of light at the end, a golden guide ARROW when he stands 2 s or strays 4 m, the film's last villagers (merchant, marketWoman, peasant, elder) run off, two orcs run villagers down (they fall), 4 dead villagers lying, refugees before the gate, deer/hares bolt across (Fauna models via "Fauna/..." in Mission1.json models), pigs + a smashed pen (CinePig / CinePigPenBroken in farm_props.py -- RENDERS SENT FOR APPROVAL, not exported: the code skips a missing model); +12 orcs out of the portal after the film (`OpeningLife.Reinforce`); `MissionHud.cs`: Menu top left (pause: Продолжить / Начать заново / В меню), the current objective top right (5 by z), Ардан's portrait top centre (UI/Icons/PortraitArdan.png, rendered by render_portrait.py -- sent with the pig renders) + «Ардан», tap = pause + his story (#m1_story, draft text); music `mus_mission1.ogg` (Stealth in the Woods, CC0, Lisboa / OpenGameArt, the user approved the download) looped at 0.35; joystick reskinned (HUD disc + gold rim, gold knob with a glint); camera 15% closer (5.1), near plane 0.35 (the gate's coat of arms flickered); the mage at the bridge's end (-2.5,-185.9), scene when Ардан steps off the deck (-184.7); bridge -> town 4x shorter (wall z -196); guards stand IN FRONT of the gate, the end is before it; the end screen on an overlay above the black (it was a black screen in Play). Texts in CSV + sheet rows 38-49. Checks: mission to the end 3:37, 0 caught; PlayMode test green.
  - **Mission 1 is complete end to end** (film -> chase -> mage -> gate -> summary); not seen by the user as a whole, not on a phone.
  - Still open: a ~0.7 m bare band of slab next to the track (ears only on whole tiles clear of it); open grass between the river and the town.
  - Not built yet: the route past the village (TRAP: the film's tree ring at r 48-93 covers where the wheat field goes -- the south sector must be cleared), orcs that chase/catch, checkpoints, the bridge/mage scene, the gates. Not seen on a phone. New models (renders first): tall wheat, forest trees/bushes/fallen tree, river + bridge, town wall + gate + houses, the mage.
- The video: the user chose «Сначала в Blender» (2026-11-10). The film is approved in Blender first and then moved into Unity.
- The level structure beyond mission 1.
