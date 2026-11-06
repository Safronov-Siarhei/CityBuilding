# Orc camps (levels 1–3) and the main base

> **Status (2026-10-21):** walled camps L1–3, the main base (stone set, mountains, boss chief),
> prisoners' cages and the chief's treasury are IN THE GAME. Not on a phone; big-base FPS unmeasured.
> **Next the user named:** raid roster per portal level; main-base extras offered but not picked
> (shaman hut, war drum, chief's forge, portal altar, catapults, wolf den, arena).
> **Gloomy orc island -- IN THE GAME 2026-11-06 (built, NOT played, no tests -- the user checks):**
> the user's concept "C heart + B swamp" (renders `Design/concepts/orcisland_*.png`, models approved).
> - All 6 orc library islands are SWAMPS (`islands.py`: `SWAMP = KIND == 'orc'`, relief 'swamp',
>   `swamp_sd` pools as holes in land_sd, no flowers; MAIN_PAD front -40 for the skull), re-exported.
> - Own palette: `terrain.ORC_PALETTE` -> `orc_palette.py` -> `Models/Palette/OrcIslandPalette.png/.mat`
>   (name must NOT start with TerrainPalette); `IslandAsset.Palette` set for Kind.Orc in
>   `CreateArchipelagoLibrary`; `MeshMapApplier.PlaceLibraryIslands` puts it on the island's ground,
>   relief and coast; its props are a 2nd MapDecorRenderer (`IslandPropsOwnPalette`). Seasons do not touch it.
>   Deep water keeps the sea's blue (no seam), shallows murky.
> - Dead forest: `TreesAreaSpawner.PrefabsAt` (cells inside a palette island's frame -> OrcDeadTree1-A/B/C, no DressTree).
> - `DressSwampIsland`: OrcWreck1 on every landing beach, 12 SwampHaze patches (`Shaders/SwampHaze.shader`,
>   flat alpha quads, wisps by sines) on land >= 30 m from the main base.
> - EVERY map's main base: `SpawnMountains` keeps the blocks' NavMesh obstacles (base as closed as before)
>   but stands the SKELETON instead of OrcMountain rocks (`DressSkeleton`): 4 ribs per side block (one
>   snapped), vertebrae + dead trees between, the spine across the back block; OrcSkullGate1 in front of the
>   outer gate when its 10x13 cells are free land (`SkullGroundFree`, then occupied), two box obstacles,
>   the corridor between the jaws open (raid exit at Bounds.yMin-1.5 lies in it); 12 skull stakes along the
>   way in; `CrowFlock` (7 over the skull, 9 over the base, one Update). Mountains stay as the fallback.
> - Models: `Tools/blender/orcisland1.py` (Models file) -> Models/Orcs. Concept: `orcisland_concept.py`.
> - Open: the main base's green turf style (the user: decide later); not seen in game; phone cost unmeasured.
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
