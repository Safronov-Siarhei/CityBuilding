---
name: project-performance-baseline
description: "CityBuilding's frame budget — the measured CPU side (2026-08-19: walking citizens are the whole cost), the MEASURED draw census (2026-08-31: buildings were 16 draws each, trees are one renderer not two), the statically audited rest, and the traps that make batchmode numbers lie."
metadata: 
  node_type: memory
  type: project
  originSessionId: c0c63e70-df66-43d5-81bc-e112016a9859
  modified: 2026-08-19T12:08:20.377Z
---

First real performance measurement of the game, 2026-08-19 (commit `32396a68`). Everything below was measured, not reasoned about. Read with [[project_current_state]] and [[feedback_mobile_first_optimization]].

## The tools now live in the repo

Three PlayMode fixtures, all in `Assets/_Project/Tests/PlayMode/`:

- **`PerformanceProfileTests`** — grows a town in stages (bare map → 30 buildings → 60 citizens → staffed → fight → 150 population) and reports `BehaviourUpdate` ms and GC bytes per frame for each. The DELTA between two stages is the price of what was added.
- **`WalkCostTests`** — microseconds per operation (`CharacterController.Move`, `transform.position +=`, `NavMesh.CalculatePath`), plus an isolation pass that disables collider groups one at a time to attribute the sweep's cost.
- **`TerrainPhysicsTests`** — guards that the map stays out of the collision matrix and in every raycast.

Run one with `-testFilter <ClassName>`; the whole suite takes ~2 min per pass.

## What the frame is made of

**Walking citizens are the entire budget. Nothing else registers.** Bare map 0,010 ms of Update; 30 buildings add 0,003; 60 STANDING citizens add 0,012; 17 citizens that start WALKING add **0,72**. A fight (12 soldiers, 8 orcs) adds nothing measurable.

- A town of 150 costs ~1,7 ms of `BehaviourUpdate` per frame on a desktop editor. Multiply by 3–8 for a phone: that is most of a 16,6 ms budget.
- **GC is clean and was never the problem.** 338 bytes/frame is the editor's own noise and does not move as the town grows; 150 citizens add ~128–512 B/frame. Do not go hunting for allocations — go after CPU.
- `NavMesh.CalculatePath` is 2,6 us and is NOT per-frame. It is not the problem either.

## Two traps that make batchmode numbers lie

- **`FrameRateController` caps the game at 60**, so wall-clock timing between two `yield return null` measures SLEEP. The first profile run showed a flat 16,67 ms in every single stage. Set `Application.targetFrameRate = -1` and `QualitySettings.vSyncCount = 0` after the scene loads, and prefer the `BehaviourUpdate` marker over the wall clock regardless.
- **The editor's own GC noise swamps the game's.** Only stage-to-stage deltas mean anything for allocation numbers in a `-nographics` editor run.
- Run-to-run variance on these numbers is roughly ±10%. A 20% change is real; a 5% one is not.

## What shipped from it

`TerrainPhysicsLayer` — the map's ground, water and authored zones moved to layer 6 (`Terrain`), whose entire row is cleared from the collision matrix at runtime in `MeshMapApplier.Apply`.

- **Half of every `Move` was the capsule sweeping the ground mesh**: 15,0 → 8,9 us. The proof it worked is that disabling the ground colliders by hand now changes nothing, where before it halved the cost.
- **Nothing was standing on that collision.** There is no Rigidbody anywhere in this project, nothing calls `SimpleMove`, and every agent pins its own Y to `GridManager.GroundHeight`. Gravity has never touched them.
- **Raycasts were never at risk**: the collision matrix governs contacts and sweeps, not queries. Every mask in the project is `~0`, `TryRaycastGround` asks its colliders directly, and `BuildNavMesh` reads collider meshes rather than collecting by layer.
- The 740 trigger boxes on trees and boulders cost **0,8 us for all of them together**. The spawners were right to make them triggers; leave them alone.

## The next lever, if walking needs to get cheaper again

The remaining ~8 us is `CharacterController` itself, in an empty physics scene, against 0,45 us for a plain `transform` write — a 20x gap. Dropping the controller was offered on 2026-08-19 and **the user chose the safe half only**. If it is ever revisited: the controller is there so a citizen cannot walk through a placed building (`CitizenAgent.cs:293`), NavMesh already routes around buildings, and the straight-line fallback is the only case that would regress — where `GridManager`'s O(1) occupied-cell lookup is both cheaper and more accurate than a capsule.

## The draw census, MEASURED 2026-08-31 -- and it corrected the audit below

`RenderCostTests` (PlayMode) counts the live scene's SUBMESH DRAWS rather than GPU time, so the
number is the same headless as on a phone and `-nographics` cannot invalidate it. Run it and read
the table out of `test_run_playmode.log`. It asserts nothing; `BuildingDrawCountTests` holds the
one rule that came out of it.

**Two things in the 2026-08-29 audit below were simply wrong, and only measuring found it:**

- **A building was 16 draws, not "4-5 parts".** Thirty placed buildings added 488 forward draws
  AND 488 shadow draws. Buildings, not trees, were the largest single bill on the map.
- **A tree is ONE renderer, not two.** The five tree meshes came to exactly 600 renderers for 600
  trees. The "600 x 2 meshes = 1 200" figure was a guess from reading the spawner.

**Fixed the same day (`095572f3`): SetupProject.CombineParts** bakes a procedurally assembled
building's cube parts into one mesh with one submesh per material, saved as its own asset under
`Models/Generated` (a prefab does not embed mesh data -- point it at an in-memory mesh and the
building comes back invisible). The fence's two variants are baked individually, since
FenceAppearance has to keep switching between them; authored FBX level models are left alone.

| stage | renderers | draws | shadow | before |
|---|---|---|---|---|
| bare map (600 trees, ~140 rock clusters, fog) | 1 007 | 1 010 | 4 | 1 056 / 1 056 / 33 |
| + 30 buildings | 1 040 | 1 146 | 140 | 1 544 / 1 544 / 521 |
| + 60 citizens | 1 160 | 1 266 | 140 | 1 664 / 1 664 / 521 |

A building went from 16,3 draws to 4,5 and from 16,3 renderers to 1. `BuildingDrawCountTests` was
verified by failing -- it caught the fence at 10 renderers per segment, which the first pass had
excluded wholesale.

**Where the remaining 1 266 draws are:** trees 600, rocks 404, citizens ~120, buildings 136, map
~6. The FLOOR -- what would be left if everything sharing a mesh and a material were one call --
is **153**.

## The instancing decision, NOT taken (2026-08-31)

Getting trees and rocks from 1 004 draws to about 7 means `Graphics.RenderMeshInstanced`, because
**in URP the SRP Batcher takes priority over GPU instancing**: flipping `enableInstancing` on the
195 materials would change nothing at all while the SRP Batcher is on. That was the plan until
three things came out against doing it blind:

- **The minimap camera is DISABLED and rendered by hand** every 0,4 s (`MinimapCameraUpdater`
  calls `Camera.Render()`), so a `RenderMeshInstanced` submitted in LateUpdate misses it. Trees
  and rocks would vanish from the minimap -- the two things it is read for. Fixable (submit
  per-camera before that Render call), but every future camera then has to opt in or silently
  lose the scenery.
- **Instanced draws bypass per-object frustum culling.** Without culling, all 600 trees are drawn
  always: the census's ~139 000 triangles becomes what the GPU actually processes, against a
  working ceiling of ~150 000 on the weakest phone in the fleet. It trades draw calls for
  triangles, and which side wins has never been measured on a device.
- Nothing here has been profiled on hardware. Trading a known-good state for an unknown one is
  the wrong move while a Development build on the Pixel/A71 is still the cheapest missing fact.

## The rendering side, audited statically 2026-08-29 (NOT measured)

`-nographics` measures none of this, so every number here was counted out of the assets and the
code, not profiled. It stands until a Development build is profiled on the phone.

**The frame is almost certainly spent on draw calls, not on polygons.** Counted renderers in a
mature town: ~600 trees x 2 meshes (trunk + canopy) = 1 200, ~140 rocks x 2-5 boulders = ~420,
each citizen is 2 cubes (`CitizenVisualsManager.cs:407`) so 150 people = 300, plus 4-5 parts per
generated building. **2 000-2 500 renderers**, and:

- **195 materials now (was 57), GPU instancing off on every single one** (`m_EnableInstancingVariants: 0`).
- Dynamic batching off, static batching impossible (trees/rocks are spawned at runtime).
- SRP Batcher IS on -- it cuts the CPU cost per call, it does NOT cut the number of calls.
- 600 trees of one mesh and one material are the textbook instancing case; nothing does it.

**FIXED 2026-08-30 (`65b69697`), shadows.** The ground, water, trees, rocks, citizens and the
fog of war are out of the shadow pass (`SceneryShadows.DisableCasting`, called from
MeshMapApplier / TreesAreaSpawner / RockSpawner / CitizenVisualsManager / FogOfWarManager);
buildings still cast; everything still receives. **The fog was the surprise**: 100 opaque chunks
lying over hidden ground, all of them casting -- found by the guard test, not by reading the code.
`SceneryShadowTests` (5 tests) holds the rule and WAS verified by breaking it. Two of its own
checks were over-broad at first, both because the map, the fog and the spawners share one
GameObject: the ground sweep caught trees (scoped by `TerrainPhysicsLayer.Layer` now) and the fog
sweep caught the authored zones (scoped by the `FogChunk` name now).

**FIXED 2026-08-30, the camera** (`65b69697`, then `2006a7a9` when the user retuned it).
`maxDistance` 220 -> 35 -> **25**, `minDistance` 8 -> 15 -> **5**, and the OPENING distance
25 -> **15**. That opening value lives in `SetupProject` (the camera's local z), not on the
controller, and has to stay inside min/max or the first zoom input snaps the view.

At 50 deg pitch and a 60 deg lens the ground strip in frame is about **1,97 x distance**: 5 m ~=
10 m, 15 m ~= 30 m, 25 m ~= 49 m, 35 m ~= 69 m. The old 220 framed the entire 200x200 map -- the
frame nothing could be culled out of, with the grey void past the map edge visible in it.

**OPEN, and the user has not answered: the 5 m floor is visually broken.** Photographed it -- at
5 m the camera sits 3,8 m up, BELOW the rooflines, and the shot is a wall filling the frame with
a stranger's roof behind it. Recommended raising the floor to 8 (camera at 6,1 m, about level with
an ordinary roof ridge); the user chose 5 and has not responded to the picture. One number plus a
scene rebuild if they say yes.

The minimap's shadows are off and its interval is 0,4 s (was 0,15); its culling mask was
deliberately left alone, since dropping trees and rocks would cost the player the two things a
minimap is read for.

Still open from the audit below: **instancing (the big one), Simple Lit, HDR off, the FBX import
flags, and `androidUseSwappy`.**

**Shadows multiply all of the above.** One directional light, shadows on, `m_ShadowDistance: 50`,
one cascade -- every renderer inside 50 m is drawn a second time into the shadowmap. Turning
`shadowCastingMode` off on trees/rocks/citizens and dropping the distance to 25-30 costs nothing
visually at this camera angle.

**All 195 materials are URP/Lit with NO textures at all** (`_BaseMap: none`) -- full PBR fragment
work for a flat colour. Simple Lit or Unlit would look identical in this art style. Same asset
still has `m_SupportsHDR: 1`: an FP16 target for post-processing we do not have
(`m_RendererFeatures: []` is empty).

**Measured triangle counts** (parsed straight out of the binary FBX): the whole building set is
6 102 tris because only 8 models are real -- Castle 2 832, Cottage 896, Hovel 840, Tower 604,
Road 232, Fence 110/90, Well 98 -- and the other 40 are 10-triangle stub cubes. The map is the
heavy geometry: `Map-1-Ground` **35 728 tris** (and it carries the MeshCollider and feeds the
NavMesh), `TreesArea` 18 908, `PlaceForWaterObjects` 12 446. Tree models are 98-164 tris each.
**Polygons have enormous headroom; what matters per building is the MATERIAL COUNT inside the
FBX**, since each one is another draw call per instance.

**Two import mistakes on all 57 FBX** (still open, and both are RAM rather than frame time -- weighed and deprioritised 2026-08-31): `isReadable: 1` (a second copy of every mesh in RAM, needed
only by the map, which builds MeshColliders and the NavMesh at runtime) and `animationType: 2`
(Generic) with `importAnimation: 1` on models that have no animation, stub cubes included.

The tooling for this lives in the scratchpad, not the repo: a ~60-line Python binary-FBX parser
that reports tris/verts/meshes per file without opening Unity. Rewrite it if needed, it is quick.

**Quality tiers exist but nothing picks between them at runtime.** Two quality levels, `Mobile`
(index 0) and `PC` (index 1); `m_PerPlatformDefaultQuality` maps Android -> 0, and the Mobile
level does point at `Mobile_RPAsset` through `customRenderPipeline` (the field is NOT called
`renderPipeline` -- grepping for that name finds nothing and looks alarming). Nothing calls
`QualitySettings.SetQualityLevel`, `SettingsPanelController` has exactly one setting (language),
and `com.unity.modules.adaptiveperformance` 1.0.0 is in the manifest but unused.

## The FBX budget handed to the user 2026-08-29 (estimated, not measured)

Derived from the scene composition above against the weakest devices in
[[project_mobile_device_fleet]] (Redmi 12 / Redmi Pad): a working ceiling of **~150 000 triangles
per frame including the shadow pass**. Today the ground mesh (35 728, one mesh, so frustum culling
never trims it) plus ~250 visible trees, ~40 rock clusters and ~80 citizens already come to ~83 000
BEFORE any building -- and shadows on all of it takes that to ~150 000. Turning `shadowCastingMode`
off on trees/rocks/citizens is therefore worth roughly half the frame's geometry.

Budget given, per model, to be spent on the thing's LEVEL-3 look (a mature town is all level 3;
rule of thumb lvl2 <= 1.5x lvl1, lvl3 <= 2x lvl1):

| Class | On screen at once | Budget |
|---|---|---|
| Props repeated in dozens (fences, roads, decor) | 30-100 | <= 300 tris |
| Housing | 20-40 | 500-1 000 |
| Production / service buildings | 10-25 | 1 000-1 500 |
| Unique landmarks (Town Hall, Cathedral, Colosseum) | 1-3 | 3 000-4 000 |
| Units (citizens, soldiers, orcs) | 100-150 | 300-500 |
| Trees | 150-300 | <= 200 (today 98-164, right) |
| Absolute ceiling for any single model | -- | 5 000 |

Existing authored models are comfortably inside it (Castle 2 832, Cottage 896, Hovel 840,
Tower 604, Well 98) -- the user's modelling instincts need no correction.

**Three things told to the user as mattering MORE than the triangle count**, in order:
1. **Separate objects inside the FBX** -- each becomes its own renderer and its own draw call, per
   instance. Every building FBX today is a single mesh; that must stay true.
2. **Material count** -- one, two at most. A 400-tri building with 6 materials costs more than a
   2 000-tri building with one.
3. **Flat shading splits vertices.** The FBX stores control points (Castle: 2 832 tris / 1 562
   points), but Unity splits by normal/UV, so an all-hard-edge model approaches 3 verts per
   triangle. Budget in triangles, but know the real vertex load is 2-3x the FBX figure.

## Android player settings, checked 2026-08-29

Correct already, leave alone: graphics APIs are **Vulkan first, GLES3 fallback**
(`m_APIs: 150000000b000000`, `m_Automatic: 0` -- good on Adreno 6xx), multithreaded rendering on,
IL2CPP + ARM64, minSdk 26, Linear colour space (fine on every device in the fleet).

**FIXED 2026-08-31: `androidUseSwappy` is now 1**, set from SetupProject.ConfigureMobileLandscape via `PlayerSettings.Android.optimizedFramePacing`. It was 0 -- -- Optimized Frame Pacing is OFF while
`FrameRateController` caps the game at 60 and four of the six test devices have 90/120 Hz screens.
Without Swappy the frames are handed to a 90/120 Hz display unevenly, which reads as judder at a
perfectly good frame time -- exactly the kind of defect that gets blamed on a slow phone and
hunted in the wrong place for months. See [[project_mobile_device_fleet]] for the devices.

## Android builds work now (2026-08-19)

`Tools/research/build_android.ps1` (add `-Development` for the profiler-carrying one) drives `CityBuilder.EditorTools.BuildAndroid`. First build: **32,5 MB, IL2CPP, ARM64, minSdk 26, 15 minutes**; later ones are faster. Android Build Support is installed for 6000.5.4f1.

- The identifier is now `com.depigogames.citybuilding`, set in SetupProject. It was the URP template's own until this build.
- **`summary.totalSize` lies on Android** -- it reports the uncompressed payload, 692 MB against a 32,5 MB APK. The script reads the file itself.
- The test phone is a **Pixel 6 Pro**: a 2021 flagship, so it is the WRONG device to conclude "fast enough" from -- expect roughly x2-3 over desktop, not the x3-8 a budget phone would show. It is the right device for thermals, since Tensor throttles.
