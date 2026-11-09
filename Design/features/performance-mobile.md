# Performance on mobile

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
