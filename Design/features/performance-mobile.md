# Performance on mobile

> **Low tier = no post effects** (2026-11-21, the user): `OpeningFx.NoPostOnLow`; Redmi Pad mission 1 40 -> 60 fps,
> menu ~40 -> 55. The resolution stays native (0.85 / 0.75 scale gave only +1 / +4 fps with post on).
>
> **2026-11-21 Pixel 6 Pro (High):** 55-60 fps everywhere cold, 120 v 120 48; heats to thermal 2 in ~1 min of big
> fights (-25 %).
>
> **AUTO TIER v2 (the user, 2026-11-21), BUILT, not run on a phone yet:**
> - boot: `GraphicsQuality.Detect` -> RAM < 4.5 GB Low, else `GpuTier(graphicsDeviceName)`: Adreno <630 / 710 /
>   Mali-G31..G72, G52, G57, G68, G310 / Mali-T / PowerVR -> Low; Adreno 630-690, 720-725 / Mali-G76, G77, G610 /
>   Xclipse / unknown -> Medium; Adreno 730+ / Mali-G78, G710+ / Immortalis -> High, but only with 7 GB + 8 cores.
>   A71 (Adreno 618) now Low; Redmi Pad Low; Pixel High.
> - run (`Core/RuntimeQuality`, Auto only): two 10 s windows under **40 fps** (the user: not 50) or thermal status >= 2
>   -> one step: scale x0.85, x0.7, post off (`GraphicsQuality.PostAllowed`), shadows off, MSAA off (no-op steps
>   skipped); 30 s at 55+ fps and not hot -> one step up; an up undone within a minute blocks ups 5 min (doubling).
>   PERF line shows `rq <step> th <thermal>`; probe `governor off` freezes it for measuring.
> - Player army cap = **80** (`army_max_size`, sheet «Армия и отряды» + «Нашествие: настройки»; the user 2026-11-21).
>
> **Every measurement is logged in `Design/PERFORMANCE_TESTS.md`** (method, devices, raw tables) -- add new ones there.
>
> **2026-11-21, Redmi Pad (the floor we design for):** battle limit 120 v 120 measured; player army cap 80 (the user); Invasion with a developed
> town + late waves never under 44 fps; mission 1 play was 27-28 fps GPU-bound: CineGround picked its pattern with
> `?:`, which in HLSL runs BOTH sides -- every pixel ran all six patterns. `[branch] if` now: menu 34 -> 39, film
> 31 -> 39, mission 1 28 -> 36-37. RULE: never pick an expensive shader function with `?:`. Darkness: fixed masks pre-blurred (`DarkMaskBlur`) -> mission 1 40 fps; left ~4.3 ms (the blend), 
> post ~7 ms (mission 1).

> **2026-11-18 evening, "дожимай" -- MEASURED on three devices (dev APK + PERF lines, PerfProbe toggles):**
> - **The big one: the film/menu/mission GROUND shader** (`CineGround.shader`) computed four-octave value noise per pixel
>   (up to five Fbm calls, ~80 sine hashes a pixel). Redmi Pad: GPU 60 ms -> ~28 ms in the main menu (16 -> 31-36 fps);
>   Pixel 6 Pro mission 1: 21-27 -> **57-60 fps** standing and running. Now `CineGroundNoise` bakes a tiling 512² R8
>   fbm texture once (global `_CineFbm`, period 64 cells, octaves at 2x not 2.07x) and `Fbm()` is one sample. Same look.
>   RULE: no per-pixel procedural noise in a shader that covers the screen -- bake it.
> - Mission 1 darkness (`DungeonDark`, `_NoDepth 1`): finds each pixel's ground point by its view ray on the flat village,
>   so the camera needs no depth texture (the depth copy with MSAA is a tile-flush on Mali). Mission 2 / dungeon still use depth.
> - Film on a phone: no depth of field, no bloom, shadow reach capped to 30 m while the film runs
>   (`GraphicsQuality.ShadowDistanceCap`, released by `LightenForPlay`/OnDestroy), and only the 3 biggest parts of each
>   figure cast shadows (`CineFigure.ShadowFloor`). Pixel film: ~50 -> ~53 fps before the bloom change (bloom-off build NOT measured).
>   Shadows OFF entirely gave 57-60 in the film -- the remaining lever, a look decision for the user.
> - Whole skinned figures (KeepSkinned for all) were TRIED and REVERTED: mission 1 got worse (GPU skinning on Vulkan).
> - Auto on an **Adreno** GPU: no MSAA (A71 menu 25 -> 34 fps with 2x off; Mali pays nothing for 2x).
> - **Samsung A71 drew the game squeezed into the left ~45 % of the screen** (rest black): Vulkan pre-transform.
>   `PlayerSettings.vulkanEnablePreTransform = false` (BuildAndroid + ProjectSettings) fixed it. GameActivity was NOT the cause.
> - Measuring on a device: `Core/PerfProbe.cs` (in every build; a file `perf.txt` pushed to the app's files dir switches parts
>   off: `post|dark|particles|skinned|figures|shadows|ui|sky off`, `shader <name> off`, `scale x`, `msaa n`, `census` -> perf_out.txt).
>   The dev build's `Cheats/PerfProbe` logs `PERF` lines (cpu/render/gpu ms) to logcat. A 120 Hz Pixel quantises: a frame just
>   over 16.6 ms shows as 40 fps, so 40/52 readings mean "slightly over budget", not "twice too slow".

> **2026-11-18 (the user: menu 13 fps, film 25, mission 1 10-15 on a strong phone) -- levers BUILT, NOT MEASURED:** Auto High on a PHONE = 1080 short side + MSAA 2x (desktop keeps 1440/4x); no bloom in the main menu on phones; Gaussian DOF in the film on phones; bloom+DOF off once mission 1 play starts; mission 1 camera far 260 -> 45 (also clips URP's shadow distance). MEASURED on the Pixel 6 Pro the same day (release APK, Wi-Fi adb, SurfaceFlinger timestats): **main menu 60.4 fps** (was 13), 0 frames over 34 ms; **mission 1 in the village 40 fps standing, 38.5 running** (was 10-15). The village is the film's whole village with figures cut into ~10 parts each (CineFigure.Rigidify) -- the next lever if 60 is wanted there (KeepSkinned like the menu needs the torches/sack/mage staff moved off the parts). Threads while running: render thread ~45 %, main ~38 % of a core, GPU at 510 of 848 MHz -- no single wall, measure with a development build before pulling the next lever.

> **2026-11-10 GRAPHICS SETTINGS: Auto / Manual (the user).** Settings → Graphics: **Авто | Ручная**.
> Auto = the device's tier (line «Сейчас: Низкий»). Manual opens, each row with a «!» info window
> (texts `#settings_gfx_*_info` in the sheet): **Уровень графики** Низкий/Средний/Высокий/Свой (a level
> sets all the rows; any changed row shows «Свой») · **Плавность** 30/60 · **Ровные края**
> Выкл/Немного/Сильно (MSAA 1/2/4) · **Чёткость** 50/75/90/100 % · **Тени** Выкл/Простые/Чёткие (off = reach 0; sharp =
> 2048 map; soft shadows cannot be switched at runtime) · **Дальность теней** Близко/Средне/Далеко
> (35/50/60 m) · **Счётчик плавности** (UI/FpsCounter, bottom left right of the minimap, every scene).
> Texts rule (the user, 2026-11-10): a child with no tech knowledge must understand them — no
> «FPS», «кадры», «разрешение», «сглаживание», «MSAA» in labels or «!» texts.
> Model: `GraphicsQuality` (Chosen Auto = Auto mode; `Detail`, `Preset`, `Current` in PlayerPrefs
> `graphics_detail.*`, `MatchingLevel`, `SetManual`/`SetLevel`, `FrameRate`, `ShowFps`); presets
> Low 60/2x/100/simple/near, Medium 60/4x/90/simple/mid, High 60/4x/100/sharp/far. The settings card's
> rows now scroll (SetupProjectMainMenuScene: Scroll/Content + GraphicsDetails built at play time by
> SettingsPanelController). Left open by the user: Water (simple/live) and Effects (simple/full) rows.
> Not seen on a device yet.

> **2026-11-10 SAMSUNG A71 (SM-A715F, Snapdragon 730, Adreno 618, 5.5 GB -> Auto gives Medium,
> 2400x1080, serial RZ8NA0ZYGSX; GPU load readable at /sys/class/kgsl/kgsl-3d0/gpu_busy_percentage).**
> Sandbox start (Archipelago, ~210k tris), GPU-bound (busy 98 %): scale 0.9 MSAA 4x (Medium) **27 fps**
> GPU 36 ms · 0.9/2x **39** (19 ms) · 0.9/off **54** (12 ms) · 1.0/2x 38 (20 ms) · 1.0/4x 26 (36 ms).
> Main menu on Medium: 24 fps. Same story as the Mali tablet: **MSAA is the cost, resolution ~free**,
> so the Adreno hope (cheap MSAA) is wrong. CPU ~16-20 ms (BehaviourUpdate ~11 ms with 14 orcs and
> 49 soldiers in the sandbox) -- a second ceiling near 55-60 fps. Edge photo: RunLogs/a71_edges_off_2x_4x.png.
> **REDMI NOTE 14 4G (24117RN76G, Helio G99 MT6789, Mali-G57 MC2, 5.6 GB -> Medium, 2400x1080,
> serial iznbqkljnbtsl7vc)**, same sandbox start: Medium (0.9/4x) **21-26 fps** GPU 38-48 ms ·
> 0.9/2x **54** · 0.9/off **54** (GPU 14 ms). Main menu: 4x 26, everything else 60. So on Mali 2x is
> ~free and 4x is the whole cost; on Adreno 2x still costs (39 vs 54). Both phones top out at ~54 in
> the sandbox with the GPU idle (12-14 ms) -- an unexplained ceiling below 60 to look into.
> HyperOS trap: "USB debugging (security settings)" ON (persist.security.adbinput=1) still gave
> INJECT_EVENTS denied (needs a phone reboot?). Workaround without taps: the dev build is debuggable,
> so `run-as com.depigogames.citycraft` reads/writes shared_prefs/<pkg>.v2.playerprefs.xml
> (graphics_tier=2 + graphics_detail.*), force-stop + relaunch = a new setting in the main menu.
> Main-menu bug seen on device: MainMenuBackground's CreatePrimitive logs "Can't add component
> because class 'CapsuleCollider' doesn't exist" (stripped) on every menu open.
> **DONE (the user approved, 2026-11-10): Medium = scale 1.0 / MSAA 2x, Low = 1.0 / no MSAA**
> (GraphicsQuality Tiers + Preset; High unchanged; Auto bands unchanged). Not yet on a device.
> CapsuleCollider/SphereCollider kept from stripping by `Assets/_Project/link.xml` (CreatePrimitive
> cylinders/spheres in the menu, dungeons, jungle, respawn flash, building markers all logged it);
> MainMenuBackground builds its pieces from the built-in meshes without colliders.
> The ~54 fps ceiling: config is right (vSyncCount 0, targetFrameRate 60, Swappy frame pacing on).
> Main thread 12-16 ms with the GPU idle means some frames miss the 16.7 ms vsync (and the governor
> keeps clocks just high enough), so it is a CPU-time item, not a settings bug -- see the A71's
> BehaviourUpdate 11 ms with units. Next: profile the CPU in the sandbox/battles.

> **2026-11-10 REDMI PAD (Helio G99, Mali-G57 MC2, 4 GB, 1200x2000) -- the first weak device measured.**
> Same sandbox save as the Pixel. **The GPU is the limit and the tier decides everything:**
> - Auto picks **Medium** here (3.9 GB) → **21 fps** in the game (GPU ~46 ms, even over plain fog),
>   menu 28 fps. **Low → 54 fps** (GPU 15 ms, town 54 fps, CPU main 12–13 ms). Medium = scale 0.9 +
>   MSAA 4x (1800x1080), Low = 0.6 + MSAA 2x (1200x720): 2.25× the pixels + 2× MSAA ≈ 3× the GPU time.
> - **FIXED (same day): a tier switched in Settings took effect only after a restart** (editing the
>   live URP copy's renderScale/MSAA did nothing on the device). `GraphicsQuality.Apply` now builds a
>   FRESH copy of the project's asset each call, assigns it to `QualitySettings.renderPipeline` and
>   destroys the old one. Verified on the tablet in-session: Low 54 → Medium 21 → Low 54 fps. No
>   editor test: the bug never showed in the editor.
> - **Auto thresholds changed (the user: «чини оба»):** under **4 500 MB** → Low (was 3 500; a "4 GB"
>   device reports ~3 800), ≥ 7 000 MB + 8 cores → High, the rest Medium. The tablet now gets Low.
> - GPU-bound frames inflate BehaviourUpdate (14–16 ms at Medium vs 6 ms at Low): a script that
>   draws (MinimapCameraUpdater calls `Camera.Render()` from Update every 0.15 s in the sandbox) waits
>   for the GPU there. Scripts at Low: ~6 ms (Pixel 3.4), physics 1.0, NavMesh 0.4.
> - **Low retuned (the user: Low «рябит»)** with the cheat «Рендер» (CheatsPanel → Служебное: render
>   scale ±0.05, MSAA 1/2/4x, upscaler Linear/Point/FSR over the tier, not saved). Tablet, town:
>   0.6/2x 54 fps · 1.0/2x **54** · 0.6/4x 29 · 0.9/4x 31 · 1.0/4x 31 · FSR changes nothing.
>   **Resolution is ~free on Mali-G57, MSAA 4x halves the frame rate.** Low is now scale 1.0, cap
>   1200 px, MSAA 2x (was 0.75/720 = 0.6 here). Medium (0.9, 4x) is just as slow on this GPU -- left
>   until an Adreno device (A71) is measured. Comparison picture: RunLogs/compare (not in git).
> - Per-script markers (`X.Update() [Invoke]`) do NOT exist in a non-deep development player --
>   a ProfilerRecorder split by script finds nothing (tried, reverted).
> - The cheats panel's «К лагерю» did not react to adb taps on the tablet (worked on the Pixel).

> **2026-11-10 MEASURED ON THE PIXEL 6 PRO** (development APK + `Cheats/PerfProbe.cs`; the user's sandbox
> day 22: 52 orcs, 20 people, 3120x1440). Holds **60 fps everywhere** (town, fog, an L4 orc camp, menu).
> - **The phone is loafing:** Mali GPU at **251–351 MHz of ~850**, CPU cores at ~40 % clock. So the
>   FrameTimingManager times (CPU main 12 ms, GPU 14–15 ms) are work STRETCHED by DVFS to fill the vsync
>   frame, NOT headroom used up. Read GPU ms only together with `cur_freq`
>   (`/sys/devices/platform/1c500000.mali/cur_freq`, readable over adb; max_freq/utilization are not).
> - Main-thread split, town: scripts **BehaviourUpdate 3.4 ms** (largest), DrawOpaque 2.2, Physics
>   Simulate+Sync 0.8, Culling 0.5, LateUpdate 0.43, NavMesh 0.25, UI 0.25–0.36. setpass 33, 276k tris,
>   23 shadow casters. Fog/sea only: 215k tris, 0 casters, scripts 3.2.
> - **Orc camp (L4) is the heaviest view:** 412k tris, **150 shadow casters**, MainLightShadow ×2,
>   GPU +3 ms at the same clock, ~+30 setpass.
> - Tiers (same view, same low clock): High GPU 14.8 → Medium 12.9 → Low 11.0 ms -- resolution/MSAA
>   are not where the GPU time goes (Low is ¼ of the pixels).
> - GC: **3.3 KB/frame in the game, 5.2 KB/frame in the main menu** with nothing running -- steady
>   allocation, source not found yet. The cheats panel open = 26.7 KB/frame + ~85 setpass (ignore it
>   when measuring; closing it moves the camera back to the town).
> - Menu was 30 fps only in the first seconds after launch; settled at 60.
> - Next: the same probe on a WEAK phone of the fleet (the flagship can't show a bottleneck);
>   find the per-frame allocations; camp shadow casters (palisade/decor out of the shadow pass);
>   split BehaviourUpdate by script (deep profile or markers).
> - Probe: in a development build only (`build_android.ps1 -Development`; BuildAndroid sets
>   `enableFrameTimingStats` for it); a line `PERF …` every 3 s in `adb logcat -s Unity`. The
>   "Draw Calls/Batches Count" recorders read 0 in a player build; setpass/tris/casters work.

> **2026-11-10 FOG CULLING (the user: «под туманом ничего не должно быть»):** `Maps/FogCulling.cs` + `FogCulled.cs` -- an object's renderers are off while none of its sample cells (middle + 4 at RadiusCells) is revealed; 40 objects checked a frame. On: orc camp roots (decor/rocks/skeleton, radius HalfExtent), every orc camp building, every OrcUnit, every bot building (InitializeAsBot), bot garrison soldiers. Simulation only on view: bot castle raids + rebuild (BotKingdom, `SeenRadiusCells` 14), bot garrison (BotGarrison), orc camp patrol walking (OrcCamp), fish shoals hidden + not animated. Wildlife already spawned only where revealed. Crows over the main base removed. No fog (dungeon, cheat, no map fog) = nothing hidden (`FogOfWarManager.IsActive`). Not measured on a phone.

> **2026-11-09:** quality tiers + transparent-variant fix + dims past the SafeArea (see "Picture quality"), not seen on a phone.
> **Status (2026-10-21):** CPU and draw calls measured in the editor (2026-08-19/31); almost nothing
> measured ON A DEVICE. Since then much was added unmeasured (5-level models, Map 3–5 decor, orc bases,
> projectiles). **Waiting on the user since 2026-09-01:** whether to take the GPU-instancing lever for
> trees/rocks. Deep numbers: `archive/project_performance_baseline.md`.
> **Rule:** mobile first — weigh frame time/GC in every design decision, not as a later pass.

## What the frame is made of (measured)
- **2026-11-20, citizens without the capsule (the user: «переводи жителей сначала в песочнице, потом замер»):**
  `CitizenAgent.SimpleWalk` (controller off, transform steps) vs the CharacterController, same town,
  `PerformanceProfileTests` stages 6-9 (editor, uncapped): 150 people 0,539 ms capsule / 0,534 simple;
  300 people 0,611 / 0,611 ms BehaviourUpdate. **No gain** -- the August cost (Move ~8 us) is gone since the
  terrain left the collision matrix. Switch left OFF everywhere (default false). 300 citizens add only ~0,07 ms
  over 150; the costly thing per unit is the army: +11 soldiers +8 orcs = +0,31 ms (stage 5) -- look there next.
  Caveat: only 12 workplaces in the test town, most people idle-wander.
- **2026-11-20 evening, MEASURED ON THE REDMI PAD (the user: «живые тесты на планшете Редми»)** -- dev APK, archipelago
  sandbox, PerfProbe `town 100 220` (Core/PerfTown: 100 buildings round the hall, people, staffed) and `capsule on|off`,
  SurfaceFlinger timestats + PERF lines, alternated on/off/on/off:
  | town | capsule ON | capsule OFF |
  |---|---|---|
  | 5 people (start) | 50 fps, scripts 13 ms | -- |
  | 220 people | 41 fps, scripts 18.3 ms, physics 1.1 | **50 fps**, scripts 13.4 ms, physics 0.7 |
  | 300 people | 24-34 fps, scripts 33 ms, physics 2.0 | **50 fps**, scripts 13 ms, physics 0.7 |
  (the first 220 reading right after the spawn: 20 fps, scripts 40 ms -- 220 capsules pushing apart.) Without the capsule
  the citizens cost ~nothing; the desktop editor test showed NO difference -- the phone is the judge. -> `CitizenAgent.SimpleWalk`
  is ON in every mode (capsule disabled from Awake). **The next lever: the 13 ms of scripts with an EMPTY town** (cpu ~20 ms,
  gpu ~15 ms at 2000x1200 Low): 52 orcs + 50 bot soldiers on the archipelago, all with capsules -- measure next.
- **2026-11-21, FIELD BATTLE LIMIT on the Redmi Pad** (the user: how big an army, against how many, before fps < 30?).
  PerfProbe `battle N M` (PerfTown.Battle: N soldiers -- 2/3 men-at-arms, 1/3 archers, squads of 20 -- against M orcs a
  few cells apart on screen, both x30 health so the fight lasts), empty archipelago sandbox, default camera, after the
  fixes above (+~40 camp orcs idle elsewhere):
  | per side | fps | cpu / gpu ms | scripts ms | frames > 34 ms |
  |---|---|---|---|---|
  | 20 v 20 | 48-55 | 14 / 15-19 | 5.5 | 0 |
  | 40 v 40 | 54-55 | 15 / 15 | 6.2 | 0 |
  | 60 v 60 | 45 | 17 / 19 | 8.3 | 0 |
  | 80 v 80 | 41-42 | 19 / 22 | 10.9 | 1 % |
  | 100 v 100 | 40-42 | 20 / 22 | 12.9 | 4 % |
  | 120 v 120 | 35-37 | 23 / 25 | 15.6 | 8 % |
  | 150 v 150 | 30 | 31 / 26 | 21.8 | 22 %, 6 % > 50 ms (stutter) |
  Reading: **~100 v 100 (200 fighters on screen) keeps 40 fps; the 30-fps floor is ~120-130 a side; 150 a side
  stutters.** Both halves grow: scripts ~0.1 ms per fighter, GPU ~0.04 ms per fighter (figures + hp bars). The Redmi
  Pad is the weak GPU of the fleet; the Pixel should hold more. Not measured: night/rain, the camera zoomed in.
  **The same battle INSIDE A BIG TOWN** (`town 100 220`, ~210 citizens walking capsule-free, the fight just south of
  the hall among buildings, orcs burning them -- ruins on screen):
  | per side | fps | cpu / gpu ms | scripts ms | frames > 34 ms |
  |---|---|---|---|---|
  | town alone | 46 | 16 / 19 | 6.5 | 2 % |
  | 20 v 20 | 43 | 18 / 20 | 7.8 | 0.4 % |
  | 40 v 40 | 41 | 19 / 21 | 9.5 | 1 % |
  | 60 v 60 | 39 | 20 / 22 | 11.3 | 4 % |
  | 80 v 80 | 37 | 23 / 24 | 13.6 | 11 % |
  | 100 v 100 | 36 | 25 / 24 | 16.0 | 13 % |
  | 120 v 120 | 27 | 36 / 27 | 24.9 | 36 %, 6 % > 50 ms |
  | 150 v 150 | 20 | 51 / 31 | 38.1 | 70 %, 35 % > 50 ms |
  Reading: **in a big town the ceiling is ~100 a side (36 fps); 120 a side already falls under 30.** Past 100 the
  scripts climb faster than per fighter (16 -> 25 -> 38 ms). **Found (marks at 120 v 120):** CharacterController.Move
  in the CROWD -- Orc.FightSoldier 8.8 ms (its Chase), Orc.GuardWalk 3.8, Soldier capsule Move 2.1 ms; a Move costs
  ~60-100 us when packed among 240 capsules (a few us in the open), so the cost grows with density. Fix 2026-11-21:
  `Core/CrowdPause` -- a walker that got under a quarter of its step for 0.4 s stops pushing for ~0.4 s (orc Chase,
  GuardWalk, soldier WalkTowards). After (same town save, 188 people): 100 v 100 38 fps (scripts 16 -> 8.9 ms),
  120 v 120 **30 fps** (was 27), 150 v 150 **29 fps** (was 20; scripts 38 -> 24 ms, no frames > 50 ms).
  Still left: soldiers WALKING in a crowd -- 107 capsule Moves a frame, 58 us each = 6.2 ms. The real lever: soldiers and
  orcs without the capsule (transform steps like citizens, ~0.5 us) plus a cheap separation push -- a LOOK decision
  (units may overlap a little), asked the user.
- **2026-11-20 night, the EMPTY town's 11 ms on the Redmi Pad** (PerfProbe `kinds`, `off <Type>`/`on <Type>`, `marks <s>`):
  switching kinds off one by one: the bot's 50 SoldierUnits ~7 ms, everything else ~1 ms or less. Marks inside: **Soldier.Wall
  6.8 ms** -- the bot's archers POSTED on their walls ran `NearestEnemyInReach` over ~200 enemies (orcs + 153 camp buildings)
  EVERY frame (WallFight, the same no-target-every-frame bug). Fixed (timer + jitter, distance before BodyRadius);
  OrcCampBuilding towers also wait 0.4-0.6 s when nobody is in reach. Same town after: Soldier.Wall 0.44 ms, scripts
  11.2 -> 6.2 ms, cpu 17.5 -> 13.9 ms. Left: Soldier.Update ~2 ms (1.5 of it outside the marks), gpu ~14 ms at 2000x1200 Low.
  Then: the 50 were a NEW-game bug -- the bot garrison manned before the fog was on (a load correctly keeps it unmanned
  until the player sees the bot castle); fixed in BotGarrison. Empty new town on the Redmi Pad: scripts **4.0 ms**, cpu 11.7 ms.
- **2026-11-20, the army (the user: «разбери армию»):** `PerformanceProfileTests` now (a) switches each script
  kind off in turn against a fresh baseline and (b) reads `Core/PerfMarks` -- Stopwatch marks inside the army's
  Update, on only while the test reads them (Unity's ProfilerRecorder read 0 for custom markers inside Update in
  batchmode). Found and fixed: **an orc or soldier with NO target searched for one EVERY frame** (the 0.4 s timer
  only ran while he had one): 50 idle camp guards ~5 us each, an idle soldier through all 326 enemies (orcs +
  portals + 271 camp buildings) ~9 us. Now the search waits for the timer (jittered 0.75-1.25x), except a fallen
  target or a new player attack order -> at once. Camp guards also SETTLE at their post (reached, or no closer for
  1.5 s, or the route ends) and stand till a soldier/prisoner/knock/new post moves them. Result, stage 5 (11
  soldiers + 8 orcs): +0.33 -> +0.20 ms; soldier 17-24 -> 3.5-6.5 us/frame, orc 4.4-5.3 -> 1.9-2.3 us; GC
  3 982 -> 1 982 B/frame. Left: a WALKING unit's step 6-10 us (CharacterController.Move 4-9 us -- removing it
  lets units overlap, a look decision); 271 camp buildings' Update 0.07 ms (0.27 us each, most never shoot).
- CPU: **walking citizens are the whole budget** (17 walkers +0.72 ms BehaviourUpdate; a town of 150
  ≈ 1.7 ms desktop → ×3–8 on a phone). GC is clean — go after CPU, not allocations.
- `CharacterController.Move` ≈ 8 µs vs 0.45 µs for a transform write; the terrain is off the collision
  matrix (layer 6, `TerrainPhysicsLayer`) — that halved Move.
- Draws (`RenderCostTests`, 2026-08-31): bare map 1 010, +30 buildings 1 146, +60 citizens 1 266;
  trees 600, rocks 404. Buildings baked to ONE renderer (`SetupProject.CombineParts`); fence variants
  baked separately. Scenery out of the shadow pass (`SceneryShadows.DisableCasting`).
- Instancing NOT taken: the minimap renders by hand (instanced draws would miss it), instanced draws
  skip frustum culling (trades draws for ~139k tris), nothing profiled on hardware.

## Budgets (per model, at its highest level)
Props/fences/roads ≤ 300 tris; housing 500–1 000; production/service 1 000–1 500; landmarks 3 000–4 000;
units 300–500; trees ≤ 200; hard ceiling 5 000. One mesh per FBX, 1–2 materials — those matter more
than triangles. Working ceiling ~150k tris/frame incl. shadows on the weakest phones.

## Settings
Vulkan first/GLES3, IL2CPP ARM64, minSdk 26, Linear; `androidUseSwappy` = 1 (90/120 Hz phones);
60 fps cap (`Core/FrameRateController.cs`); mesh compression Low on Buildings/Terrain/Orcs (APK size).

## Picture quality on the phone (2026-11-09, the user: "effects pretty on PC look bad on the phone")
Built, **not yet seen on a phone**:
- **Transparent variants kept in builds**: every see-through effect (dust, zone carpets, ink drawing,
  net, shield, order marker) is `new Material(Lit/Unlit)` switched to transparent by the
  `_SURFACE_TYPE_TRANSPARENT` keyword at runtime. No material on disk used that variant, so a build
  could drop it and draw them OPAQUE (the hard beige squares on the user's screenshots).
  `SetupProject.KeepTransparentVariantsInBuilds` writes `Resources/ShaderVariants/Transparent*.mat`.
  A NEW runtime-only keyword combination needs its own material there.
- **Tiers** `Core/GraphicsQuality.cs`: Auto/Low/Medium/High in the main-menu settings (row «Графика»,
  PlayerPrefs `graphics_tier`). Applied to a runtime COPY of the active URP asset (never writes
  Assets/Settings). Auto: desktop High; phone <3.5 GB Low; ≥7 GB and 8 cores High; else Medium.
  Low 0.75 scale/MSAA 2/shadow 1024/35 m; Medium 0.9/4/1024/50; High 1.0/4/2048/60; render scale
  also capped so the short screen side ≤ 720/1080/1440 px. HDR off everywhere (no post effects).
  Rule: a tier changes how the world is drawn, never what is in it. Numbers in C# (render technique,
  like the camera), not the sheet.
- Mobile_RPAsset itself: HDR off, MSAA 4x (the defaults before a tier applies).
- **Screen dims bleed past the SafeArea** (`StretchBleed`, ±600): the victory screen left a light strip
  by the phone's cut-out.
- Open: soft shadows can't be switched at runtime (setter internal); clouds/fog look on Low unchanged.

## Tools
PlayMode `PerformanceProfileTests` (stages), `WalkCostTests`, `RenderCostTests` (table in `RunLogs/test_run_playmode.log`),
`BuildingDrawCountTests` (rule), `SceneryShadowTests`, `TerrainPhysicsTests`. Profiling traps: the 60 fps
cap makes wall-clock timing measure SLEEP (set `targetFrameRate = -1`, `vSyncCount = 0`); only stage
deltas mean anything; ±10 % run noise. On device: Pixel 6 Pro over adb (FPS via SurfaceFlinger
timestats) — a flagship, the wrong phone to conclude "fast enough" from.

## Watch list
Map 3 decor ~240k tris; L3 orc camps ~136 palisade renderers; main base ~180 pieces; 5 model
children per building; detail shader fragment cost; fog overdraw; autosave hitch.

## APK size (measured 2026-11-10, dev APK of that day)
Real content ~198 MB (the 365 MB file has ~167 MB of orphan zip data from Gradle's in-place update --
not in the entries, Play's AAB won't carry it). `data.unity3d` 167 MB LZ4HC, 308 MB raw, of which
**meshes 251 MB**: dungeons 153 MB (12 variants, 7.2 M verts, 2701 meshes), islands 48 MB, Map/Inv
13 MB, buildings small. Textures ~0. Inside meshes: positions 90, **UV 83 (3 UV sets per vertex,
the palette needs 1)**, triangles 33, normals 30, vertex colours 12 MB. Exact duplicate meshes only
6.7 MB. Native libs ~24 MB packed. Estimated Play download ≈ 185-190 MB -- at Google Play's 200 MB
base-module cap. Analysis: UnityPy on the APK (pip `UnityPy`).
**DECISION (the user, 2026-11-10): Google Play Asset Delivery, NOT mesh slimming.** Dropping the
extra UV sets / stronger map compression was rejected as too risky to the look (a palette UV moved
into the next cell recolours a piece; seams between ground chunks). Plan: before release, move the
dungeons (and likely islands) to Addressables in Play Asset Delivery packs (fast-follow: downloaded
in the background right after install while the player plays the start), base ~50-70 MB. Own CDN
only if the game goes to other stores (RuStore etc.) or needs content updates without a release.
Needs: maps loaded via Addressables instead of being baked into SetupProject's scenes; a release
AAB build path (build_android.ps1 makes APKs today). Not started.
