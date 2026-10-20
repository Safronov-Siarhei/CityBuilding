---
name: project-mobile-device-fleet
description: "The six Android devices the user owns for testing, what each one is FOR, and the proposed (not yet approved) three-layer quality-tier scheme with the rules about what a tier may and may not change."
metadata:
  node_type: memory
  type: project
---

Established 2026-08-29, in the session that audited the rendering side (numbers live in
[[project_performance_baseline]]). **Nothing here is built yet and nothing has been approved** --
this is the fleet and the proposal, recorded so it does not have to be re-derived.

## The devices, and what each is FOR

| Device | GPU | RAM | Role |
|---|---|---|---|
| Pixel 6 Pro | Mali-G78 MP20 | 12 GB | High-tier ceiling; Tensor throttles |
| POCO X3 Pro | Adreno 640 (Snapdragon **860**) | 8 GB | 2019-flagship GPU; **throttles hard** -- the right device to validate adaptive downshifting |
| Galaxy A71 | Adreno 618 (SD 730G) | 6/8 GB | Medium; the CPU-vs-GPU slope experiment |
| Redmi Note 14 | Mali-G57 MC2 (Helio G99 Ultra, **variant unconfirmed**) | 6 GB | Medium, modern mainstream |
| Redmi Pad 10.6 (2022) | Mali-G57 MC2 (Helio G99) | 4 GB | **Worst case for fill rate: 2000x1200**; also the only tablet UI check |
| Redmi 12 | Mali-G52 MC2 (Helio G88) | 4 GB | The floor: does it run at all, and the memory ceiling |

- The user said the POCO is a Snapdragon 835; it is an **860 / Adreno 640**. Resolve every device's
  real GPU by printing `SystemInfo.graphicsDeviceName` from a build rather than trusting spec lists.
- Note 14 has four variants (4G Helio G99 Ultra / 5G Dimensity 7025 / Pro 7300 / Pro+ SD 7s Gen 3)
  spanning ~2x in GPU. Which one this is has NOT been confirmed.
- **Redmi Note 14 (2024) is barely faster than the A71 (2020)**: Mali-G57 MC2 sits about where
  Adreno 618 does, and the CPU cores are near-identical (2xA76 + 6xA55 either way). Mid-range
  Android GPUs have hardly moved in five years. **Therefore a tier must never be chosen from
  release year or Android version** -- only from GPU family + RAM. The fleet spans ~6x in GPU,
  which is enough to define three tiers by measurement. Do not suggest buying more devices.

## The proposed tier scheme (three layers)

1. **A coarse guess at boot from `SystemInfo`** -- RAM, core count, GPU family, screen resolution.
   Never a device-model whitelist: thousands of Android models, new ones monthly, pure tech debt.
2. **Runtime adaptation, and this is the layer that matters.** A rolling window of real frame time
   steps the tier down (or back up) -- it catches a wrong guess AND thermal throttling, which a
   static tier can never see. `renderScale` should be the first knob because it is the only
   continuous one; discrete steps (shadows on/off) need hysteresis or they flicker.
   `com.unity.modules.adaptiveperformance` 1.0.0 is in the manifest and unused; a homegrown
   frame-time loop is more portable (the full thermal API is Samsung-only).
3. **A manual override that wins**: Auto / Low / Medium / High in `SettingsPanelController`
   (which today has exactly one setting, the language). Choosing anything but Auto stops the
   adaptation from touching it again.

**The rule that must not be broken: a tier may change only how the world is DRAWN, never what it
IS.** renderScale, shadow distance/casting, draw distance, shader tier, MSAA, HDR, target FPS are
all fair game. Tree and rock COUNT is not -- trees are a resource, so cutting them would hand two
players different games and break a save moved between devices. Same for citizen counts, radii,
raid ranges.

Honest caveat: the measured ceiling is **CPU** (walking citizens), and no graphics tier lowers it.
If a weak device dies there, the legitimate lever is "distant citizens move on a slower cadence,
visually interpolated" -- a change to the picture, not the simulation.

## The order agreed as sensible (not started)

1. Profile a Development build on the phones -- we have never measured rendering at all.
2. Build the knobs themselves (instancing, shadows off on props, Simple Lit, HDR off). Several of
   these just make everything faster everywhere, no tiers involved.
3. Only then put tiers + adaptation on top, when it is known what they turn and by how many ms.

**The slope experiment, the cheapest thing in the list:** run one identical town on the Pixel and
on the A71. ~4x slower => we are GPU-bound and the render fixes are the right work. Only ~1.5-2x
slower => we are CPU-bound (Kryo 470 vs Tensor is about 2x) and graphics tiers will save nothing.
One measurement settles a question that is otherwise pure guesswork.
