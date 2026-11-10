# Orc camps (levels 1–3) and the main base

> **2026-11-10 (the user):** crows over the main base REMOVED (CrowFlock no longer added). Camps, their buildings and orcs not drawn under the fog; patrols walk only on view (FogCulling, see performance-mobile).

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
> - ORC PALETTE (approved 2026-11-06, "цвета орков темнее"): `Tools/palette/orc_palette.py` -> `OrcPalette.png/.mat`
>   (copy of the buildings' material, `_SnowMask` 0, made in BuildSeasonLooks); `ModelImportDefaults.UsesOrcPalette`
>   remaps every Models/Orcs FBX to it at import (units, buildings, portals, ruins) EXCEPT the island set
>   (`OrcIslandModel`: skull gate, ribs, vertebra, dead trees, stakes, wreck, crow); `ReimportOrcModels` re-imports
>   stale ones. Portal purple/white, fire, lit windows kept. This settles the "green base" question.
> - LIVING BASE, variant A «Логово в рёбрах» (scheme `Design/orc_base_living_a.png`, APPROVED 2026-11-06,
>   IN THE GAME same day, built, not seen): `OrcBaseLayout.Main()` = the scheme shifted 7 cells back (portal
>   4x4 on the origin), Bounds x -19..18, z -30..7, NO mountains. Yard (z -30..-19: 2 war tents, 2 stores,
>   gate x -1) -> chief's quarter (gate LEFT at x -10, front bulging round the drum; chief house, 2 ranges,
>   2 pits, treasury, 2 cages) -> sanctum = TWO walls all round (outer gate RIGHT x 5, inner LEFT x -6),
>   2 totems, 2 stores, cage. 17 Watchtowers on the bends take the wall cells under them.
>   PATROLS: `GuardSlot.Route` (camp frame loop); `OrcCamp.WalkPatrols` every 0.5 s sends a member within 2 m
>   of its point on to the next (`MovePost`; a fight pulls it off, it returns to the same point); a refilled
>   member makes for its patrol's current point. 5 patrols: yard 3 grunts, chief's quarter brute+2, sanctum
>   corridor 2 brutes, round the portal 2 brutes+grunt, skull 2 grunts out to the stakes. Static: 5 grunts at
>   the fires, 2 brutes at the sanctum gate, 13 throwers by towers, the chief by the throne (0.5,-12.5).
>   `CampLayout.Apron` (main: x -5..5, z -44..-30) = the skull way in counts as the camp's zone for its guards.
>   Skeleton moved outside the walls: 5 ribs per side in the strips x ±15..18 beside the quarter and sanctum,
>   spine at z 10 behind the back wall (`SpawnMountains` now also runs for IsMain with no rects, for the dress).
>   islands.py orc MAIN_PAD (-21,20,-47,14); all 6 Isl-orc re-exported (each found a spot).
>   Main layout is SHARED by every map (Map3-5 too): their bases are 7 cells deeper now -- not checked there.
> - DECOR for A APPROVED and IN THE GAME 2026-11-06 (not seen): `Tools/blender/orcdecor1.py` -> Models/Orcs
>   (OrcPalette), fields on OrcRaidManager, wired in SetupProjectOrcs; `OrcRaidManagerCamps.DressMainBase` (from
>   SpawnMountains for IsMain, skeleton or not) places at native scale: yard campfire (4.5,-26.5) with 2 seated
>   orcs, 5 dummies z -27.5, cauldron, meat rack (11,-23), 2 hide racks; drum in the bulge (-0.5,-19.3), bone
>   throne (0.5,-12.6) facing the sanctum with the chief before it (0.5,-11.5), 2nd fire (6,-12.5), skull stakes;
>   8 rune stones on an ellipse 3 x 2.6 round the portal, runes outwards. No obstacles. Patrol 5's back leg moved
>   to z 3.4 to clear the stones; fire guards stand ~1.6 off the fires.
> - VARIANT B «Гать» APPROVED and IN THE GAME 2026-11-06 (built, not seen): `OrcBaseLayout.MainGat()`; the variant
>   is `OrcBaseLayout.MainVariant` (static, one main base a map), set by OrcRaidManager (placement AND restore)
>   from `MeshMapDefinition.MainBaseVariant` <- `ArchipelagoGenerator` map.MainBaseVariant <- `VariantOfIsland`
>   (Isl-orc2/4/6 = «Гать», the rest A; other maps A). Geometry shared C#/Python: `GatDry` (octagon x -15..14,
>   z -16..11, cut 4; its edge = the stone wall), `GatCauseway`, `GatPileTowers`, `GatBog` (bounds x -26..25,
>   z -28..19, minus dry+1 strip, causeways, piles). Bog = the layout's Mountains (row runs merged into rects,
>   carving obstacles, `BogMountains` -> no rocks). Side gates: GateTower 1x2; SpawnCampBuilding turns a gate's
>   outside away from the middle (side: by x sign; back: +90). Portal mound: lvl-2 wooden palisade ring
>   x -7..6, z -7..6, Gate lvl 2 at the BACK. 16 towers (6 on piles). Patrols: front causeway (ping-pong),
>   left, right, wall ring, mound; ambush grunts at the bend; chief at (-3.5,8.2) by the throne (-3.5,9.3).
>   Skeleton for B: ribs out of the bog behind (z 18, turned 90), vertebrae + dead trees in the bog.
>   islands.py `GAT` (even orc variants): pad (-29,28,-50,22), `gat_sd` carves the bog (min into land_sd);
>   new rolls orc2=45, orc4=46, orc6=60 (the old shapes had no room). Cells checked: patch, causeways, pile islets.
> - B's own DECOR APPROVED and IN THE GAME 2026-11-06 (not seen): `Tools/blender/orcdecor2.py` -> Models/Orcs
>   (OrcCauseway1, OrcStiltHut1, OrcRaft1, OrcBogLantern1, OrcReeds1), wired in SetupProjectOrcs; DressMainBase
>   (variant 1) lays a causeway piece a cell along every GatCauseway leg (model runs along x, legs along z turned
>   90), 9 bog lanterns beside them, 5 stilt huts + 2 rafts in the bog, 14 reed clumps (ambush bend, edges).
>   Bog decor stands at the bog's ground height: check it does not sink under the water in play.
> - VARIANT C «Хребет» APPROVED and IN THE GAME 2026-11-06 (built, not seen): `OrcBaseLayout.MainSpine()`, variant 2,
>   on Isl-orc3 (since «Кольцо»: 3 only). Bounds x -15..14, z -48..8; spine =
>   `SpineCell` (x -1..0, z -41..-8, minus cross walls -30/-18 and gaps -33/-32, -21/-20, -10/-9) as the layout's
>   Mountains (no rocks); cross gates alternate (-42 L, -30 R, -18 L, -7 R); sanctum double-walled (outer x -15/14,
>   z 8; corridor entered by a 1x2 GateTower in the inner wall at (-12,-2)); 25 towers. Patrols: plaza, runner down
>   the whole snake and back, middle gap, sanctum, corridor, skull (Apron x -5..5, z -62..-48); 2 brutes per gap.
>   Dress: vertebrae x1.7 on the spine, 4 rib pairs from the spine arching out over the side walls, tail behind the
>   back wall, dead trees in the side strips; A's decor zone by zone (throne turned -90 to face the spine).
>   islands.py `SPINE` (orc 3/5): pad (-18,17,-64,14), rolls orc3=10, orc5=11.
> - All three variants share A's decor models; only «Гать» has its own (orcdecor2.py).
> - VARIANT D «Кольцо» APPROVED and IN THE GAME 2026-11-06 (built, not seen): `OrcBaseLayout.MainRing()`, variant 3.
>   Rings `RingCell` (r 20.5/13.5/7.5); gates found where a ring crosses an axis (outer front+back 2x1, middle
>   left+right 1x2, inner front); 26 towers placed by bearing on r-0.5; Bounds x -24..23, z -21..26; Apron the
>   skull way. Patrols: outer street r 18.8 both ways (3+2 grunts), middle r 11.5 (brute+2), portal r 5.6 (2
>   brutes), skull 2; brutes at middle and inner gates; 12 throwers; chief (-2.7,8.5) before the throne.
>   Dress: spine arc r 23 from -60 to 240 deg, 6 ribs at r 21.8 arching in (yaw 180-bearing), dead trees.
>   ISLANDS NOW (VariantOfIsland): 1 = «Кольцо» (RING pad (-27,27,-38,27), roll 16), 2/4/6 = «Гать» (45/46/60),
>   3 = «Хребет» (roll 10), 5 = A (its own roll again). Island 5 dropped from «Хребет» for the ring.
> - NEXT: the user playtests all four
(«Гать», «Хребет», «Кольцо»).
> - Not seen in game; phone cost unmeasured.
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
EditMode `OrcBaseLayoutTests` (incl. `TheMainBaseIsBuildableClosedAndAsDrawn` -- written for the OLD plan with
mountains, expect it to need rewriting for plan A; not run 2026-11-06, the user tests themselves; `EveryCampHoldsItsCages`);
PlayMode `OrcCampTests` (filter `!IsMain`), `MainBaseTests`, `PrisonerCageTests`, `SaveRoundTripTests`.

## Traps
- (2026-11-06, the user: town hall + lighthouse placed, NO orc base) A designed map's main spot used
  `CampSquareFits` = the WHOLE Bounds dry -> «Гать»'s bog (and any pool in a corner) placed no main base, only a
  warning. Now `OpenAtMapSpots` asks only: portal 4x4 free-or-scenery and dry, Bounds on the map
  (`FreeOrScenery`, `MainSquareOnMap`); Reserve skips water cells. Not yet seen working.
- Place the MAIN base FIRST, then camps round it (last failed ~1 in 4 on Map 3).
- Restore skips the ground raycast (colliders not ready at load) — water check only.
- Guards are not saved; spawners re-muster them. Only damaged/fallen buildings are saved.
- ~136 palisade renderers per L3 camp, ~130 wall pieces + ~50 rock blocks on the main base — watch draws. Plan A: ~330 wall cells + 17 towers.
