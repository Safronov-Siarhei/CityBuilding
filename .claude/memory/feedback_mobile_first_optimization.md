---
name: feedback-mobile-first-optimization
description: "CityBuilding targets mobile first, so performance cost is a first-class factor in every design and implementation decision, not a later cleanup pass"
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 3e842454-0d11-425f-9bc0-2e216cee14ae
  modified: 2026-08-13T08:21:46.777Z
---

The user stated 2026-08-13 that CityBuilding is aimed primarily at mobile devices, and asked that performance be weighed in **every** decision going forward — not treated as an optimization pass to run afterwards.

**Why:** mobile is the constraining platform for this project, and the cost of an architectural choice (per-frame allocations, extra full-screen passes, hundreds of dynamic obstacles) is far cheaper to avoid at design time than to unpick later. The user raised this unprompted right after a session where several such costs had to be found and removed retroactively.

**How to apply:** when choosing between implementations, treat frame time and GC pressure as a design constraint of equal weight to correctness/simplicity, and say so explicitly in the reasoning rather than picking the cheap-to-write option and noting perf as a caveat. Concretely, for this project:

- **Avoid steady allocations in `Update()`/timed ticks.** GC pauses read as stutter on mobile far more than on desktop. Reuse buffers (ping-pong arrays, cached lists, pre-allocated single-element arrays) instead of allocating per tick. Watch for the non-obvious ones: `Renderer.materials`, `NavMeshPath.corners`, and any `new` inside a loop over agents.
- **Throttle anything that renders or scans the whole world.** Secondary cameras must not run at the main camera's cadence (the minimap camera renders the entire scene into a RenderTexture — it is manually driven on an interval, not left enabled). Same for full-grid scans and `FindObjectsByType` sweeps.
- **Be sparing with `NavMeshObstacle` carving.** Each carving obstacle costs continuous navmesh work; ~600 trees already carve. New scattered props should default to *not* carving unless they genuinely need to block pathing (boulders deliberately don't). Prefer debounced + async (`UpdateNavMeshDataAsync`) over synchronous full re-bakes.
- **Question full-screen post-processing.** The flat-shaded low-poly art style gains little from effects that cost a full pass plus a depth/opaque texture. The Mobile render-pipeline tier is the one that actually ships — keep it lean (it already runs at renderScale 0.8, no SSAO, single shadow cascade).
- **Design-level counts matter too**, not just code: how many units/props/buildings a mechanic implies on screen at once is part of whether that mechanic is a good idea. The user has already made at least one design decision (the fixed 20-soldier army cap) partly on device-performance grounds — see [[game-design-backlog]].

Verification runs through the usual batchmode loop ([[feedback-batchmode-workflow]]); note that neither compilation nor the EditMode suite catches a performance regression, so reason about cost up front rather than expecting the tests to reveal it.
