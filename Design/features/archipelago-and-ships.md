# Archipelago sandbox map and ships

> **Status (2026-10-29):** the sandbox plays ONLY on the archipelago (Map3/4/5 removed from the
> picker; Map3 stays as the test fixture). A seed builds a 500×500 sea with 12–20 islands taken
> from a Blender LIBRARY; the map code («2-482915») is in the pause menu and the sandbox screen.
> **Ships, phase 3, half done:** the Shipyard builds a scout (lvl 1) and a transport (lvl 2), they
> sail where tapped and are saved. Boarding/landing not built. The user has NOT seen ships in game.
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
  The scout temporarily reveals some shore. Ship levels are raised in the Laboratory.
- Orcs get ONE shipyard at the main base; destroyed, it is rebuilt after a sheet-set time.

## Code
- `Maps/Archipelago/ArchipelagoGenerator.cs` (layout, `Version = 2`, `FromLibrary`), `IslandShape.cs`,
  `IslandAsset.cs`, `ArchipelagoLibrary.cs`, `ArchipelagoMeshes.cs`, `ArchipelagoMapId.cs`
  (`Archipelago-v{V}-{seed}`, `Code`, `TryParseCode`).
- `Maps/MeshMapApplier.cs` `ApplyGenerated` / `PlaceLibraryIslands` / `DecorateLibraryIslands`;
  water placement zone = 6 m from any shore in library mode; ends with `ShipManager.BuildSea`.
- `Grid/GridManager` resizes to 500 in Awake (`ArchipelagoMapId.Pending()`).
- Ships: `Ships/ShipUnit.cs` (`ShipKind`, `ShipStats` from units rows `ship_*`), `Ships/ShipManager.cs`
  (sea NavMesh, `DescribeBuildBlocker`, `TryBuild`, `Spawn`), `Ships/ShipInput.cs` (via
  `Input/WorldInputDispatcher`), `UI/FocusHireBar.cs` `Kind.Shipyard`.
- Map code UI: `UI/GameMenuController.cs` (plaque, copy), `UI/SandboxScreenController.cs` (input).
- Save: `SaveData.ShipEntry`, `GameSaveData.ships`, `GameSaveController`.
- Editor: `SetupProjectMaps.CreateArchipelagoDefinition/CreateArchipelagoLibrary`,
  `SetupProjectCityScene` (Shipyard building, ShipManager with models), `SetupProjectHud` (third hire bar).
- Blender: `Tools/blender/islands.py` (`-- <out.blend> <kind> <variant> [preview]`, writes
  `-cells.bytes`), `export_map.py` (`Isl-` prefix), `render_island.py`, `ships.py`.
- Prototypes: `Tools/archipelago/islands_preview.py`, `archipelago_preview.py`.

## Data
- units tab: `ship_scout`, `ship_transport` rows; columns `capacity`, `shipyard_level`, `max_count`
  (`engage_radius` = scout reveal cells). Ships are skipped by the soldier balance test.
- buildings tab: `Shipyard` (3×4, Water), health 160/256/400/576/800.
- localization: `#unit_ship_*`, `#building_shipyard`, `#ship_*`, `#map_*_archipelago`, `#sandbox_code_*`,
  `#menu_map_code*`.

## Tests
- `Tests/EditMode/ArchipelagoGeneratorTests.cs`.
- `Tests/PlayMode/ArchipelagoTests.cs`: map build + Town Hall in the zone; save round trip;
  `AShipyard_BuildsTheShipsItsLevelOpens_AndTheySail` (green 2026-10-29).
- `MapPickerTests.AMapCode_StartsThatVeryMap`.

## Traps already paid for
- Island cells: 1 land, 2 no-build, 4 forest, 8 stony, 16 shore. A quarter turn maps (x,z)→(z,−x).
- Island water meshes carry only coastal bands; the game draws the deep sea at −0.54.
- `ArchipelagoLibrary` must live in its own file (Unity serialization).
- The ships' NavMesh agent type is made at run time and its id can be NEGATIVE — test `_sea.valid`,
  not `agentType >= 0`.
- A NavMesh path to a guessed sea point may not exist (islands); tests search a ring of points.
- MapPicker tests must wait for `LoadingScreen.Busy` or the next test inherits the loading screen.

## Open
- Step 3b: boarding (unit/group taps a ship, walks to it), landing button at beaches only, scout
  shore reveal, transport capacity in use.
- Phase 4: war ships, battleship, sea combat, orc shipyard + landings. Phase 5: lighthouse (coins),
  fish shoals, fertile land (farms only). Fog with 3 states for 500 m on phones. Mirrored islands.
- Bot settlement, monsters/dungeons on islands, the orc base redo.
