# Performance on mobile

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
children per building; detail shader fragment cost; fog overdraw; autosave hitch; APK 49.7 MB vs
Telegram's 50 MB cap.
