# Orc camps (levels 1–3) and the main base

> **Status (2026-10-21):** walled camps L1–3, the main base (stone set, mountains, boss chief),
> prisoners' cages and the chief's treasury are IN THE GAME. Not on a phone; big-base FPS unmeasured.
> **Next the user named:** raid roster per portal level; main-base extras offered but not picked
> (shaman hut, war drum, chief's forge, portal altar, catapults, wolf den, arena).
> **Gloomy orc island -- CONCEPT 2026-11-06, waiting for the user's verdict (nothing in the game):**
> the user wants the orcs' island to look savage and gloomy and the main base less same-y; picked
> "C as the heart + B round it": a giant beast's SKELETON (skull lying in front of the main gate, the
> way in through its jaws; ribs arching over the side zones -- they replace the side/back mountains;
> spine running off behind) + a SWAMP round it (hummocks, black pools, dead trees, stakes with
> skulls, wrecks, crows circling the skeleton, a thin low swamp haze; the storm cloud was REJECTED). Renders: `Design/concepts/orcisland_*.png`.
> Made by `ISL_SWAMP=1` islands.py (orc kind: relief 'swamp', `swamp_sd` pools as land_sd holes)
> + `Tools/blender/orcisland_concept.py` (gloomy palette on the same terrain cells = a 2nd
> Terrain material for this island only; the base from its real plan and models; skeleton,
> dead trees, stakes, wrecks, crows, haze -- placeholders, not game models). Open: the main base's green
> turf/moss style clashes with the gloom (needs a reskin?); later: per-seed base VARIANTS
> (layouts + special buildings: shaman hut, war drum, catapults, wolf den, forge, arena) and
> liveliness (smoke, fires, patrols, alarm horn) -- discussed 2026-11-06, not decided.
> **2026-11-06 models for approval** (`Tools/blender/orcisland1.py`, in the Models file, NOT exported):
> OrcSkullGate1 (10x12, corridor 2 wide / 3.4 high), OrcRib1-A/-B (foot 2x2, arc 10 up / 11.5 in),
> OrcVertebra1, DeadTree1-A/B/C (slate grey: wood colours get the planks detail), OrcSkullStake1,
> Wreck1, Crow1. Bone = `burlap`/`stone_mid`/`stone_dark` (the camps' plaster read as cream).
> Sheet `render_orcisland_models.py`; the concept stands them when all are in the Models file.
> **Read with:** [orc-raids-and-portals](orc-raids-and-portals.md), [art-pipeline](art-pipeline.md)
> (orc models), `archive/project_orc_faction.md` for the full sketch-by-sketch history.

## Rules (the user's — implement their sketches, don't redesign alone)
- Look: Warcraft orcs; wood/bone/hide/rough iron; marker colour rust-orange; anything portal glows
  violet; skin stays green. Every LEVEL changes the MODEL, for buildings AND warriors.
- Camp = portal in the CENTRE, towers, PALISADE as a REAL wall (blocks, breakable; the gate is the
  weak point), gate facing the town. Plans per level are the user's sketches (L1 no fence; L2 fence +
  totem + storehouse; L3 two rings with a gate-tower) — coded in `OrcBaseLayout`.
- Totem = portal SHIELD while it stands (main base: shielded while ANY of its two totems stands).
- Guards are refilled by SPAWNER buildings, one per orc kind (war tent → grunts, brute pit → brutes,
  thrower range → throwers, chief's house → boss): every `orc_garrison_respawn_seconds` one guard until
  the plan count; destroyed spawner = no more of that kind. Guards fight only inside the camp zone.
- Camps GROW with the town's progression score; a level-up REBUILDS the camp whole. Per-map designs
  may fix each camp's level (Map 4: four camps, all L3).
- **Main base:** strong from the start, all buildings max level in its OWN overgrown-stone style
  (never reuse camp models), backed by impassable mountains, three walled zones, stone gates with
  towers, the great 4×4 portal, two totems, chief's house; the BOSS chief respawns
  (`orc_chief_respawn_seconds`) while the house stands. Sends no raids, doesn't grow.
- **Cages:** 1 at L1–2, 2 at L3, 3 on the main base. Broken → prisoners walk to the Town Hall and
  become citizens; no room → they WAIT at the hall. Orcs hunt prisoners only with no soldier to fight.
- **Treasury** (main base): broken → pays `orc_treasury_loot_*` (capped by storage; the feed reports
  what fitted). Restored-broken by a save pays nothing; same for cages.

## Code
`Combat/OrcBaseLayout.cs` (pure plans, `Main()`, `ToWorld`, `LevelFor`), `Combat/OrcCamp.cs`
(reserve max square, `Build/Teardown/GrowTo`, one NavMeshObstacle per straight palisade run, spawners,
`RaidExit`, `InFrontOfGate`), `Combat/OrcCampBuilding.cs` (`OrcCampBuildingKind` 0–11 — append only),
`Combat/FreedPrisoner.cs`, `Cheats/DebugPanel.cs` (fly to camp, rebuild at L1–3, send a squad).

## Data
economy `orc_*`, `main_portal_health`, `orc_main_*`, `orc_chief_*`, `orc_cage_*`, `prisoner_*`,
`orc_treasury_*`, `orc_camp_level{2,3}_score`, `orc_camp_loot_*`.

## Tests
EditMode `OrcBaseLayoutTests` (incl. `TheMainBaseIsBuildableClosedAndAsDrawn`, `EveryCampHoldsItsCages`);
PlayMode `OrcCampTests` (filter `!IsMain`), `MainBaseTests`, `PrisonerCageTests`, `SaveRoundTripTests`.

## Traps
- Place the MAIN base FIRST, then camps round it (last failed ~1 in 4 on Map 3).
- Restore skips the ground raycast (colliders not ready at load) — water check only.
- Guards are not saved; spawners re-muster them. Only damaged/fallen buildings are saved.
- ~136 palisade renderers per L3 camp, ~130 wall pieces + ~50 rock blocks on the main base — watch draws.
