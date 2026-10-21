# Performance on mobile

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
60 fps cap (`Core/FrameRateController.cs`); quality levels Mobile/PC exist but nothing switches them;
mesh compression Low on Buildings/Terrain/Orcs (APK size).

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
