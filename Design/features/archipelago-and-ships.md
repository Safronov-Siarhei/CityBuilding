# Archipelago sandbox map and ships

> **2026-11-02 (correction):** the user meant «too little STONE», not land: the start island is back to
> 130 (round 9's 190 reverted, same assets), and `island_start_rocks` (40) extra boulders are laid on its
> own land away from the Town Hall spot (`MeshMapApplier.StartIslandRockCells` ->
> `RockSpawner.Initialize(..., extraCells, extraCount)`, fresh games only). Not seen.


> **2026-11-02 round 9:** the START island is bigger: 130 -> 190 (`islands.py` _SCALES/KINDS 'start',
> `ArchipelagoGenerator` `(Kind.Start, 190, 0f)`); Isl-start1/2 regenerated and exported (≈10-11k
> buildable cells). Reverses the earlier «start ~25% smaller» pick for the start island only. Not seen.


> **Status (2026-10-29):** the sandbox plays ONLY on the archipelago (Map3/4/5 removed from the
> picker; Map3 stays as the test fixture). A seed builds a 500×500 sea with 12–20 islands taken
> from a Blender LIBRARY; the map code («2-482915») is in the pause menu and the sandbox screen.
> **Ships, phase 3:** the Shipyard builds a scout (lvl 1) and a transport (lvl 2), they sail where
> tapped and are saved. BOARDING and LANDING built 2026-10-29 (see «Boarding and landing»).
> **Phase 4a (2026-10-29):** the war ship (lvl 3) and the battleship (lvl 4, carries 8), cannons,
> health bars, sinking that drowns everyone aboard, repair at a Shipyard for wood (see «Sea war»).
> **Phase 4b (2026-10-29):** the orcs' shipyard, fleets that land the raids, towers vs ships
> (see «The orcs at sea»). **Phase 5 (2026-10-30):** fertile land, fish shoals, the lighthouse
> (see «Land and sea riches»).
> **Playtest fixes (2026-10-31):** fertile land drawn into the ground, fish as boxy swimming fish on
> the coast only, a real storm edge, light water = the building zone (see «Playtest fixes»).
> The user has NOT seen ships in game.
> **Read with:** [maps-and-terrain](maps-and-terrain.md), [fog-of-war](fog-of-war.md),
> [army-and-soldiers](army-and-soldiers.md), [research-laboratory](research-laboratory.md).

## The user's rules (do not re-ask)
- Sandbox only; random by seed; the code carries the generator version, an old-version code is
  refused («старая версия»), not silently turned into another map. Empty code = random map.
- World 500×500 (variant А), start and orc islands ~25% smaller (Б). 12–20 islands, unique coasts.
- Mandatory islands: the start (yellow Town Hall zone), the orc MAIN base (≥ 55% of the world away),
  the bot settlement. Islands keep the season themes. No island per ore.
- Orcs: NO raids until orc ships exist; NO small camps on other islands, only the main island.
  The orc base will be redone later — leave it.
- Islands are made in BLENDER at old-map quality, 2 per kind; the seed picks and turns them.
- Map rule 1: no trees on beaches (every map, see maps-and-terrain).
- Edge = «Бурное море» band ships stop at.
- Ships: the Shipyard stands in shore water and must be RESEARCHED; its LEVEL opens ship types
  (1 scout, 2 transport; proposed 3 war, 4 battleship/линкор, 5 trade). A bought ship appears on
  the water by the Shipyard. No crew. Count unlimited, but a per-type `max_count` column (0 = no
  limit). Repair only for resources. Control like the army. Boarding: select a unit, tap the ship.
  Landing ONLY at beaches (button). A sunk ship kills all aboard; the hero respawns at the Town Hall.
  The scout reveals the shore it passes FOREVER (the user, 2026-10-29: «так проще» -- no
  temporary reveal, no return to a sketch). Ship levels are raised in the Laboratory.
- Orcs get ONE shipyard at the main base; destroyed, it is rebuilt after a sheet-set time.

## Placing the Shipyard (fixed 2026-10-29)
- The user could not put it down at all: a water building had to have EVERY cell in the shore-water
  zone (6 cells off land), and a 3x4 slipway in a band that narrow nearly always touched the beach.
  Now (`BuildingPlacer.CanPlaceSelectedBuilding`, every Water building): cells may be shore water OR
  plain land, at least HALF in the water; relief/cliff cells never. Test
  `TheShipyard_GoesDownOnShoreWater_AndAcrossTheShoreline` (water / across the shoreline / mostly land refused).
- THE REAL CAUSE (found second): the placer aims at `MeshMapApplier.TryRaycastGround`, and on the
  archipelago the islands are the only ground -- over open water the ray hit nothing and the ghost
  stayed stuck on the beach. A Water building now aims at the sea's surface (`ShipManager.WaterLevel`
  plane) when the ray hits nothing or the seabed (`BuildingPlacer.TryGetGroundCell`). The test taps
  the water through `HandleWorldTap`; it FAILS with the aim fix switched off (checked).
- Trap: pinning `PinnedOriginForTests` skips the aiming -- the first fix passed a pinned test and
  the bug stayed. Test what the finger does.

## Boarding and landing (built 2026-10-29)
- With an army group (or a hero) selected, a tap on a ship = board order (`ArmyOrderInput`):
  the group walks to the BEACH the ship stands at; each member there (≤ 4 m of the landing or
  ≤ 1 m of its ring slot) goes aboard while seats last (`ShipManager.Update`). Another order to
  the group, or the ship sailing off, calls it off. Refused (red ripple + event-feed line):
  `#ship_cannot_carry` (scout, capacity 0), `#ship_full`, `#ship_no_beach`.
- A BEACH = walkable land (the land NavMesh, agent type 0) within `ShipManager.LandingReach` = 7 m
  of the hull. Cliffs and the rocky rim are not walkable, so they never count. (The island cells
  have no beach flag: 16 is shore WATER.)
- Aboard = the unit's GameObject inactive, still in its group (army count, upkeep, save); out of
  `SoldierUnit.All`, so orcs do not see it.
- A selected transport replaces the build menu with ONE button (`UI/ShipActionBar`, people glyph,
  «aboard/seats»); a tap lands everyone on the beach in their group's rings and the groups hold
  there; dimmed with nobody aboard or no beach (`#ship_empty`, `#ship_no_beach`).
- Saved: `SoldierEntry.aboardShip` / `HeroEntry.aboardShip` (1-based ship index, 0 = land).
- The scout's reveal is its fog walker (radius = `engage_radius` 14 vs the transport's 8); what it
  opens stays open, like everything the fog opens -- final rule, nothing waits on a 3-state fog.

## Sea war (phase 4a, built 2026-10-29)
- Ships are `IDamageTarget`s with a health bar once hurt. An armed ship (units tab `attack_damage`
  > 0: `ship_war`, `ship_battleship`) fires a CANNONBALL (`ProjectileKind.Cannonball`) every
  `attack_interval_sec` at the nearest orc within `attack_range_units` m, else at an orc camp
  building (+2.5 m reach to its walls). The scout and the transport carry no guns.
- At 0 health `ShipManager.Sink`: everyone aboard drowns (`ArmyManager.NotifyDrowned`: a soldier is
  gone, no corpse; a hero `Fall(drowned: true)` -- no Captain's Бессмертие at sea -- and rises at
  the Town Hall as usual); the ship leaves `Ships` (not saved), heels over and goes down in 4 s;
  event line `#ship_sunk` / `#ship_sunk_with`.
- Repair: a holed ship's `ShipActionBar` shows a hammer button with the wood price; works only
  within `ship_repair_reach_meters` (14) of a working Shipyard; price `ship_repair_wood_per_hp`
  (0.5) x lost health, rounded up; mends to full at once. Blockers `#ship_whole`,
  `#ship_repair_far`, `#ship_repair_cost`. The bar shows for a ship that carries people OR is holed.
- My defaults the user has NOT confirmed: the numbers (war 220 HP / 12 dmg / 2.5 s / 11 m / 160
  coins; battleship 520 / 18 / 3 s / 13 m / 340 coins / 8 seats), repair in WOOD, instant repair.

## The orcs at sea (phase 4b, built 2026-10-29)
- The orc SHIPYARD (`OrcCampBuilding`, kind `Shipyard` = 12, no camp, model `OrcShipyard1`) rises on
  the sea nearest the main base's centre once the main base exists (`ShipManager.RaiseOrcShipyard`,
  `Ships/ShipManagerOrcs.cs`). It is a normal army target (it is in `OrcCampBuilding.All`), fires
  cannonballs like a tower (`orc_shipyard_damage` / `_range_meters`) at soldiers, else the player's
  ships. Broken: `#log_orc_shipyard_destroyed`, raised again after `orc_shipyard_rebuild_seconds`
  (`#log_orc_shipyard_rebuilt`); never while the main base is cleared.
- RAIDS BY SEA: on a NoRaids map the raids' clock (OrcRaidManager, same interval/size/level as the
  land raids) calls `ShipManager.LaunchOrcFleet(size, level)`: transports (`ship_orc_transport`,
  `capacity` orcs each) + `orc_fleet_escorts` war ships (`ship_orc_war`) sail from the shipyard to
  the sea by the beach nearest the Town Hall (`TryFindOrcLanding`). A transport there puts its
  orcs ashore (`OrcRaidManager.SpawnLanding`, the land raid's wave mix) and sails home; at home it
  is removed. War ships hunt the player's ships within 25 m, wait off the beach, go home with the
  transports. No shipyard standing = no raid that turn (the clock runs on).
- Orc ships: not selectable or boardable (`ShipInput.ShipAt` skips them), open no fog, are not a
  soldier's tap target (`ArmyOrderInput`). The player's war ships fire at them; the orcs' war ships
  fire at the player's ships, then soldiers, then shore buildings. Player towers
  (`DefensiveBuilding`) and every shooting orc building fire at ships of the other side in reach.
- Saved: `ShipEntry.orcRaid/orcCargo/orcLevel/destX/destZ/homeward`; `GameSaveData.orcShipyardSaved/
  orcShipyardHealth/orcShipyardRebuildIn`.
- My defaults (unconfirmed): shipyard 900 HP / 10 dmg / 12 m / 300 s rebuild; barge 160 HP, 8 orcs;
  longship 200 HP / 10 dmg / 2.5 s / 11 m; 1 escort per fleet. Orc ships are seen through the fog.

## Land and sea riches (phase 5, built 2026-10-30)
- FERTILE LAND (`Maps/Archipelago/FertileLand.cs`): `fertile_patches_per_island` ragged blobs of
  `fertile_patch_radius` cells on plain ground (no forest/stones/beach/cliff/relief), none within 60
  cells of the orc base, one always 12-30 cells from the start. Drawn as dark soil quads with darker
  furrow rows (one mesh, 2 materials, no shadows). From its own seeded RNG: the generator and map
  codes are untouched, nothing saved. `MeshMapApplier.IsFertileCell`. A FARM ONLY yields
  `1 + fertile_farm_bonus x share of its plot that is fertile` (`ProductionBuilding.SiteMultiplier`).
- FISH SHOALS (`Ships/ShipManagerFish.cs`): `fish_shoal_count` shoals on sea within reach of a beach,
  2 by the start island; seen as 7 dark fish backs + glints spinning slowly (1 mesh, 2 materials).
  A Fisher's Hut with a live shoal within `fish_hut_reach_meters` catches `1 + fish_shoal_bonus`;
  the extra comes out of the shoal's `fish_shoal_stock`; fished out = gone (`#log_shoal_gone`), back
  after `fish_shoal_respawn_seconds` at a random shore. Saved (`GameSaveData.shoals`).
- LIGHTHOUSE (`Ships/Lighthouse.cs` LighthouseAim, `UI/LighthouseBar.cs`, model `Lighthouse1-lvl1`
  in ships.py, buildings row `Lighthouse` category Water but a LAND building): only within
  `lighthouse_coast_cells` of water (`#place_blocked_coast`). In focus, a button with its coin
  price; tap -> the next map tap (WorldInputDispatcher, before everything but placement) lights a
  spot within `lighthouse_range_meters` (range ring shown): `lighthouse_reveal_coins`, fog opened
  for good over `lighthouse_reveal_cells`, then `lighthouse_cooldown_seconds` (not saved).
- All numbers mine, unconfirmed.

## Playtest fixes (2026-10-31)
- FERTILE LAND is no longer squares on the map: `FertileLand.Build` traces smooth outlines round the
  cells (marching squares over a softened field) in the TERRAIN material and palette (dirt_dark
  inside, dirt rim) -- one draw, dressed by the season. `KeepGrass` drops every cell whose ground
  is not a grass colour (ray + `hit.textureCoord`): library islands carry NO beach flags
  (`IslandShape.FromAsset` leaves `Beach` empty), so the sand is only known by its colour.
- FISH: 9 boxy fish per shoal (body, dark back, back fin, tail; 2 materials) swimming their own
  circles round the shoal, one leaping every ~2.4 s; all moved from `TickShoals`, no per-fish
  scripts. Shoals spawn ONLY on `IsWaterPlacementZone` water (the light coastal strip).
- COAST: `ArchipelagoMeshes.LightenCoast` repaints the island water's blue band as shallow
  turquoise, so the light water = the 6 m water-building zone; deep sea refuses the player's
  water buildings (`BuildingPlacer`, already so) and now the orc shipyard too
  (`TryNearestSea(shoreOnly)`, any sea only if no shore water is reachable).
- EDGE: `Maps/Archipelago/StormEdge.cs`, REDONE the same evening after the user said the
  whitecaps were "white diamonds that hardly look like a storm". It is now built from:
  - a broken white surf line, Unlit, its width wandering 0 to 2 m;
  - storm water in three darkening bands, from levels 0/6/13 of the field outwards, plain Lit colours;
  - rows of long tapered wave crests bowed towards the islands, one object per side, swinging
    0.9 m in and out over 3.2 s (`StormSwell.cs`);
  - massive 7-sided sea stacks with foam rings at their feet;
  - dark cloud puffs at 5-8 m over the far storm.
  - Cost: about 10 draws, no shadows.
- FERTILE TAP (2026-10-31): a tap on fertile soil with no building over it lights the whole
  patch's edge for 4 s and posts «Обнаружена плодородная земля» (`#log_fertile_found`).
  - The patch's edge is a pulsing gold ribbon on the same softened outline the soil uses
    (`FertileLand.PatchAt`/`Flash`, `FertileFlash.cs`).
  - The tap is handled by `WorldInputDispatcher.TryShowFertileLand`; the patch comes from
    `MeshMapApplier.FertilePatchAt`.

## Playtest fixes (2026-11-01, pushed 39fc7a56; user has not seen them yet)
- Fertile land: no patch cell is closer to water than `fertile_min_coast_cells` (8), measured as a
  4-neighbour BFS from the water (`FertileLand.CoastDistance`). It looked bad on the beach.
- The storm edge has NO clouds any more. Its low-poly puffs read as grey "pyramids" hanging in the
  air (`StormEdge.cs`).
- Fish shoals spawn within `fish_shoal_shore_meters` (4 m) of land, in `TryShoreSea`, so that the
  Fisher's Hut makes sense.
- Tapping a shoal rings it with a pulsing gold ring (FertileFlash) and posts
  `#log_shoal_found` «Обнаружен косяк рыб».
  - Code: `ShipManager.ShoalAt`/`FlashShoal`, `WorldInputDispatcher.TryShowShoal`.
  - The tap is checked AFTER fertile land.
- The orc shipyard now needs its WHOLE 3×4 m slipway on the light water (`TryNearestSea`
  shoreOnly). Before, only the middle was checked, so the far end stood in deep sea.
- OPEN: «у горы кусок висит в воздухе» (screenshot 4). Not found yet; the user was asked where.
- Round 2 (2026-11-01): shoals stay at least `fish_shoal_min_spacing` (14 m) apart, live or
  waiting to return (`TooNearAShoal` in `TryShoreSea`).

## Forest, boulders and the dungeons (2026-10-31)
- Denser forest: one tree per `archipelago_forest_cells_per_tree` (6) forest cells, clamped
  `archipelago_trees_min/max` (600/3000). Boulders: one per `archipelago_stony_cells_per_rock` (5)
  stony cells, clamped 250/900, and `archipelago_rocks_scattered_share` (0.3) of them over open
  land (`RockSpawner.Initialize(prefabs, count, scattered)`).
- `Maps/SceneryStreamer.cs` draws trees and boulders only in squares of
  `scenery_stream_square_cells` (24) near the camera focus. The reach is the same as
  `DungeonChunkStreamer`'s. Trees and rocks register as they are made; only renderers are
  switched, never colliders or the grid.
- Three dungeon entrances: see [dungeon](dungeon.md) «Entrances on the map».

## Playtest round 5 (2026-11-02)
- LIVING COASTS (the user's options 1 + 2): `Maps/Archipelago/ShoreSurf.cs` lays a band along every
  coast (signed distance from the land cells, chamfer + marching squares, -0.6..2.4 m, 64-cell tiles)
  drawn by `Shaders/ShoreSurf.shader`: the water's edge runs up the beach and back (white crest, wet
  sheet), each stretch in its own phase, a broken foam line breathes ~1.6 m out. Material on
  OrcRaidManager `shoreSurfMaterial` (Materials/ShoreSurf.mat). GPU only, no per-frame CPU.
  TRAP: it drew MAGENTA at first -- a variable named `line` (reserved in HLSL) broke the compile.
- Orcs ashore on the Town Hall's island: blinking red «!» on the minimap, one per band of orcs
  (10 m clusters) -- `MinimapAttackAlerts.PlaceLandings` (sandbox; Invasion keeps its lanes), island
  land from `MeshMapApplier.OnStartIsland` (kept by RevealStartIsland).
- The start island's fog now lifts on EVERY Town Hall appearance, a load included
  (`BuildingInstance` calls `RevealStartIsland`) -- it came back fogged after a dungeon.
- Boulders restored from a save before the map handed the models over were grey cubes:
  `RockSpawner.RestoreFromSave` now waits for `Initialize` (`_pendingRestore`).
- Fertile land rarer: economy `fertile_patches_per_island` 2 -> 1, `fertile_patch_radius` 5 -> 3.

## Code
- `Maps/Archipelago/ArchipelagoGenerator.cs` (layout, `Version = 2`, `FromLibrary`), `IslandShape.cs`,
  `IslandAsset.cs`, `ArchipelagoLibrary.cs`, `ArchipelagoMeshes.cs`, `ArchipelagoMapId.cs`
  (`Archipelago-v{V}-{seed}`, `Code`, `TryParseCode`).
- `Maps/MeshMapApplier.cs` `ApplyGenerated` / `PlaceLibraryIslands` / `DecorateLibraryIslands`;
  water placement zone = 6 m from any shore in library mode; ends with `ShipManager.BuildSea`.
- `Grid/GridManager` resizes to 500 in Awake (`ArchipelagoMapId.Pending()`).
- Ships: `Ships/ShipUnit.cs` (`ShipKind`, `ShipStats` from units rows `ship_*`), `Ships/ShipManager.cs`
  (sea NavMesh, `DescribeBuildBlocker`, `TryBuild`, `Spawn`, `Sink`, `TryRepair`, `RepairCost`), `Ships/ShipInput.cs` (via
  `Input/WorldInputDispatcher`), `UI/FocusHireBar.cs` `Kind.Shipyard`.
- Map code UI: `UI/GameMenuController.cs` (plaque, copy), `UI/SandboxScreenController.cs` (input).
- Save: `SaveData.ShipEntry`, `GameSaveData.ships`, `GameSaveController`.
- Editor: `SetupProjectMaps.CreateArchipelagoDefinition/CreateArchipelagoLibrary`,
  `SetupProjectCityScene` (Shipyard building, ShipManager with models), `SetupProjectHud` (third hire bar).
- Projectile: `Combat/CombatProjectiles.cs` `Cannonball` (pooled ball mesh). UI: `UI/ShipActionBar.cs`
  (land + repair), built by `SetupProjectHud.BuildShipActionBar`.
- Blender: `Tools/blender/islands.py` (`-- <out.blend> <kind> <variant> [preview]`, writes
  `-cells.bytes`), `export_map.py` (`Isl-` prefix), `render_island.py`, `ships.py`.
- Prototypes: `Tools/archipelago/islands_preview.py`, `archipelago_preview.py`.

## Data
- units tab: `ship_scout`, `ship_transport`, `ship_war`, `ship_battleship` rows; columns `capacity`, `shipyard_level`, `max_count`
  (`engage_radius` = scout reveal cells). Ships are skipped by the soldier balance test.
- buildings tab: `Shipyard` (3×4, Water), health 160/256/400/576/800.
- localization: `#unit_ship_*`, `#building_shipyard`, `#ship_*`, `#map_*_archipelago`, `#sandbox_code_*`,
  `#menu_map_code*`.

## Tests
- `Tests/EditMode/ArchipelagoGeneratorTests.cs`.
- `Tests/PlayMode/ArchipelagoTests.cs`: map build + Town Hall in the zone; save round trip;
  `AShipyard_BuildsTheShipsItsLevelOpens_AndTheySail`, `AGroup_BoardsATransportAtABeach_AndLandsAgain`,
  `AWarShip_ShellsAnOrc_AndASunkShipDrownsEveryoneAboard`, `AnOrcFleet_SailsFromTheirShipyard_AndLandsItsRaid`,
  `TheShipyard_GoesDownOnShoreWater_AndAcrossTheShoreline`, `FertileLand_FishShoals_AndTheLighthouse`
  (all 8 green 2026-10-30).
- `MapPickerTests.AMapCode_StartsThatVeryMap`.

## Traps already paid for
- `kit.hull()`'s rail is a full-width slab: its colour IS the deck's (a gold rail = a gold deck).
- Orcs landed at the very water's edge spread into the sea: a fleet lands them 4 m up the beach.
- `OrcUnit`/`OrcCampBuilding` implement `IsAlive` explicitly: test `CurrentHealth > 0` on them.
- Island cells: 1 land, 2 no-build, 4 forest, 8 stony, 16 shore. A quarter turn maps (x,z)→(z,−x).
- Island water meshes carry only coastal bands; the game draws the deep sea at −0.54.
- `ArchipelagoLibrary` must live in its own file (Unity serialization).
- The ships' NavMesh agent type is made at run time and its id can be NEGATIVE — test `_sea.valid`,
  not `agentType >= 0`.
- A NavMesh path to a guessed sea point may not exist (islands); tests search a ring of points.
- MapPicker tests must wait for `LoadingScreen.Busy` or the next test inherits the loading screen.

## Open
- Citizens aboard (the user said capacity counts citizens too) -- only soldiers and heroes board now.
- Orc ships hidden in the fog; soldiers may not reach the orc shipyard in the water (archers and
  ships can). Ship levels in the Laboratory. Lighthouse cooldowns are not saved;
  no hint on the Farm/Fisher's Hut card of the bonus it gets. Fog with 3 states for 500 m on phones (no longer needed by
  the scout; only if wanted for its own sake). Mirrored islands.
- Bot settlement, monsters/dungeons on islands, the orc base redo.

## Rules of round 8 (the user, 2026-11-02) -- BUILT, not seen, untested
- An island with NONE of the player's buildings takes a building only while a living hero stands on
  it (`BuildingPlacer.IslandOpenToBuild`, rechecked twice a second while placing; the Town Hall and
  its lighthouse exempt; `#place_blocked_no_hero`). Other maps unaffected.
- A move order (or a walk to a dungeon mouth) to ANOTHER island is refused with
  `#order_other_island` (`MeshMapApplier.SameIsland` / `IslandAt`, ArmyOrderInput.OnFoot).
- A player ship not held and not sailing for `ship_idle_return_sec` (30) sails home: the sea by the
  Town Hall or by a working Shipyard, whichever is nearer, unless within `ship_home_reach_cells` (12)
  (`ShipUnit.TickIdle` -> `ShipManager.SendHome`).
- The ship's move marker is a BUOY (the user's pick of 4, 2026-11-02): `Models/Buildings/OrderBuoy.fbx`
  (`Tools/blender/orderbuoy.py`, white/red float, lamp on legs), set where the voyage ends
  (`ShipUnit.Destination`), dropped in with a bounce, bobbing; it sinks when the ship arrives, sinks,
  or is sent elsewhere (`Ships/ShipManagerBuoy.cs`, one buoy for the latest order; only player taps).

## Start of an archipelago game (playtest round 7, 2026-11-02, the user) -- BUILT, not seen, untested
- Town Hall down -> the KING steps out before it for free (`BuildingPlacer.GiveStartingParty`,
  HeroRoster.Spawn; not in an Invasion run) and a RESEARCH SHIP (Scout) appears on the sea nearest
  the hall (`ShipManager.GiveStartScout`). The orcs ignore it (`ShipUnit.GraceCells`, skipped in
  `NearestShip`) until it has sailed `start_scout_grace_cells` (50) cells; saved as `ShipEntry.graceCells`.
- The mandatory LIGHTHOUSE goes anywhere inside the same yellow start zone (StartZone.Allows), NOT
  only at the shore (the coast rule is gone for every lighthouse); the zone overlay stays while it is
  in hand. The start island is NO LONGER revealed whole (reverses 2026-11-01): only the hall's and
  the lighthouse's own circles clear the fog.
- Dungeon mouths and caches: a spot must lie on the island's OWN land (its frame overlaps the
  neighbours' -- two mouths once stood on the start island).
- Veins: `island_vein_shore_gap` (4) cells of the island's own land round each vein (none at the coast).
- Cache tap -> «Найден сундук с сокровищами» (`#log_tide_cache_found`; looted: `#log_tide_cache_empty`).
  With a boss the serpent stands 4 cells behind the chest, the rest in a half-ring in front (they
  overlapped). The gold heap in the serpent's coil is part of its model (its hoard).

## Island riches and the mandatory lighthouse (2026-11-02, the user)
- **Riches** (`Maps/Archipelago/IslandRiches.cs`, assigned at generation, deterministic): Start =
  coal x`island_start_coal` + copper x`island_start_copper` + its one fertile patch by the Town Hall;
  the nearest ordinary island IRON (`island_iron_veins` + `island_iron_coal`), the next COPPER
  (`island_copper_veins`), the farthest GOLD (`island_gold_veins`); the rest by shape: Rocky/Atoll and
  islets STONE (`island_stone_coal`), Hilly IRON, Long COPPER, Flat/Crescent FERTILE
  (`island_fertile_patches`; ordinary islands `fertile_patches_per_island` = 0); orc and bot islands none.
  `MeshMapApplier.IslandLabels/IslandRichesOf/RichesAt(cell)`; `OreDepositSpawner.PlaceByIsland`
  (the old distance Plan is for designed maps only); `FertileLand.Compute(world, labels, riches)`.
  Not shown to the player yet except by the veins' glow.
- **Lighthouse = 2nd mandatory building** (archipelago only): `starts_unlocked` 1 (Laboratory only
  raises its levels); right after the Town Hall `BuildingPlacer.TryBeginLighthouse` puts it in hand,
  free (`CostOf` -> empty via `_freeNow`), no build timer, no cancel, hint `#hud_place_lighthouse_hint`
  (BuildingPlacerUIVisibility swaps the baked Town Hall hint). `IsPlacingTownHall` (fog activation,
  save flag) vs `IsPlacingMandatoryBuilding` (both). A town with no lighthouse is handed one a second
  after load (not saved; a lost lighthouse is handed back on the next load).
- **Light**: `BuildingInstance.RevealRadius` -- a lighthouse clears `lighthouse_light_cells_<level>`
  (45/52/60/70/80 cells: a medium island at 1, a large one at 5) instead of fog_reveal_radius, again
  on every upgrade and on load. The old paid spot-lighting (LighthouseAim) is kept as it was.
- Not seen in game; no tests (user: one general run later).

## Guarded caches and Стражи Глубин (2026-11-02, BUILT, not seen in game, no tests)
- Models APPROVED and exported to `Models/Tide/` (export_fbx «Tide*»/«Cache*»): CacheChest (+Lid),
  CacheRuin (stone), TideCrab, TideShark (hammerhead), TideOctopus (octopus-headed shaman), TidePuffer,
  TideSerpent (boss). Eel/priestess/hermit-in-hull and the anglerfish were REJECTED.
  `Tools/blender/tide.py`, `treasures.py`. Floating parts were checked by a BVH script (scratchpad).
- Combat: `OrcKind.TideCrab..TideSerpent` (20-24), `OrcUnit.IsTide`, IsMonster/IsBossKind (serpent),
  ranged = puffer + shaman (Cannonball shot); models on `OrcRaidManager.tide*Model` (SetupProjectOrcs).
  Stats `mon_tide_{crab,shark,shaman,puffer,serpent}_*` (economy rows 313-337).
- `Maps/Archipelago/TideCaches.cs`, placed by MeshMapApplier right after the dungeons: one cache per
  ordinary island (not start/orc/bot/islet), spot via `DungeonEntrances.TryFindSpot(freeOnGrid)`,
  5x5 occupied + no-build. Index = order nearest-first. Tier: nearest third 1, middle 2, far 3, gold
  island 4. Guards `tide_t{tier}_{crab,shark,puffer,shaman,serpent}`, held at their posts
  (`tide_guard_radius`), ASLEEP until a SoldierUnit is within `tide_wake_radius`; a shaman heals the most
  hurt guard (`tide_shaman_heal*`). Last guard down -> lid swings open (sign tried at build), rewards
  `tide_reward_{wood,stone,gold,crystals}_{tier}` + popups + `#log_tide_cache_looted`; `TideCaches.Looted`
  saved as `tideCachesLooted`; guards are not saved (IsGuard) and are mustered again 1.5 s after load.
- Rule: they ONLY guard caches (no raids, no attacks on ships). All numbers mine, unconfirmed.
- Ore rule (asked 2026-11-02): one mine per vein -- already so (`BuildingPlacer`: footprint == vein,
  `vein.Mine == null`).
- The Water build tab's icon is the Lighthouse picture (`CategoryIconBuilding`).
