# Maps and terrain

> **Status (2026-10-24):** every map has four SEASON looks (see «Seasons»). New Game opens a MAP PICKER (`MapSelector.PlayerMapIds` = Map5, Map4, Map3,
> newest first = default). Maps 3, 4, 5 are designed island maps with planned orc spots (Map 3 redrawn
> 2026-10-21). **Maps 1–2 are DELETED** (user, 2026-10-20); a save on one shows greyed «map removed».
> `Inv1` is the Invasion map (not in the picker). None of these seen on a phone.
> **Next the user named:** more designed maps one at a time (Перешеек, Фьорды, Великое ущелье);
> Vikings from the sea in free mode; dungeons in ruins (later).
> **Read with:** [gathering](gathering.md), [orc-raids-and-portals](orc-raids-and-portals.md),
> [fog-of-war](fog-of-war.md), [art-pipeline](art-pipeline.md).

## The user's rules for every map (2026-10-20)
- Designed terrain: impassable mountains, gorges, rivers; the player builds on ONE plain. Relief is decor only.
- Edge = SEA all round (Viking landings later at fixed BEACHES). Size 200×200 m for now.
- EVERY character paths AROUND mountains and rivers; crossings are fords and bridges only.
- Each map plans its OWN camps: count, place, and the level each grows to; a main-base spot; a Town
  Hall start zone (≥ 45 m from every orc spot); a reserved ruin spot for future dungeons.
- Every new map joins the player's picker («каждая новая карта должна быть доступна в выборе»).
- Look: «сочное лето», smooth shores, calm colour zones (per-triangle shades read as broken glass).

## General map rules (the user, 2026-10-29 — every map and island, add new ones here)
1. No trees on a beach: the forest (TreesArea) and its decor start where the sand ends, the wide
   landing beaches included (`islands.py`: forest cells need `sd > sand_width + 1.5`; beach decor = pebbles only).

## Maps
- **Map 3 «Летняя долина»** — redrawn 2026-10-21 to the designed-map rules: an island with a lake under
  the northern hills and a river to the sea; start zone in the south-west meadow; four camps + the main
  base's valley fixed in `Map-3-Spots.csv`.
- **Map 4 «Долина двух рек»** — two rivers from northern mountains split the island; fords; main base
  in a cirque (0,67); 4 camps all L3; beaches (0,−90), (−90,−4), (90,−8).
- **Map 5 «Кальдера»** — town on an old volcano floor round a crater lake; mountain ring cut by four
  gorges; a creek out the east gorge with two fords and a bridge; main base in a valley between two
  spurs; four camps (L3, L3, L2, L2) outside the ring; three Viking beaches.
- **Inv1** — the Invasion map (`Tools/blender/invasion1.py`): see [invasion-mode](invasion-mode.md).
  `OUTCROPS` = stony patches (3 on the island, 12 beyond the river since 2026-10-23); each is a
  clearing in the forest (`forest_field`), since forest cells take no boulders. Regenerate:
  `blender --background --factory-startup --python invasion1.py -- "D:/blender projects/Buildings3D/Inv-1.blend"`,
  then `export_map.py -- D:/Fork/CityBuilding Inv1 Inv-1`. Blender is `D:/SteamLibrary/steamapps/common/Blender/blender.exe`.
- Variants kept for later: Перешеек, Фьорды, Великое ущелье.

## Pipeline
Blender generator `Tools/blender/mapN.py -- <out.blend> [preview]` (Map3 machinery; Map4/5 add a
layout section: coast, cliffs, mountains, rivers with `FORDS_AT/BRIDGES_AT`, CAMPS=(x,y,level),
MAIN_BASE, START_ZONE, BEACHES) → `render_mapN.py` review shots → regenerate WITHOUT `preview` →
`export_map.py -- D:/Fork/CityBuilding MapN Map-N` (Ground/Relief chunks, Water, TreesArea, RocksArea,
NoBuildArea, DecorKit, TerrainPalette, Decor.csv, Spots.csv) → SetupProject `CreateMeshMapDefinition`,
`ReadOrcSpots`, `CreateMapDecorData` → `Resources/MeshMaps/MapN.asset`.
Decor STONES (2026-11-01, the user: «старые камни-коробки заменить»): every land map's `pebbles`
and `crag` props are drawn from ONE kit, `Models/Decor/DecorRocks.fbx` (`Tools/blender/decor_rocks.py
-- <project>`, shapes = the gatherable Stone1..10 layouts in `stone_shapes.py`: pebbles = Stone1-3 x0.8,
crags = Stone7-10 x1.5), swapped in by `CreateMapDecorData` (variant dealt by a stable hash of the spot);
a map's own DecorKit pebbles/crags are ignored, dungeons keep theirs. No map needs regenerating for
a new rock shape -- only decor_rocks.py + setup. Cost: ~56-112 tris per stone (was 26-54).
Probe a layout without Blender: `probe_map.py` (counts water/relief cells per camp square — 0 = fits).
The .blend files live OUTSIDE git: `D:/blender projects/Buildings3D/Map-*.blend`.

## Code
`Maps/MeshMapApplier.cs` (applies a map; relief out of NavMesh; `IsNoBuildCell`, `RocksZoneCells`,
`CurrentMap`; terrain onto layer 6), `Maps/MeshMapDefinition.cs` (`HasMainBaseSpot`, `CampSpots`,
`CampLevels`, `TownHallZone`, decor), `Maps/MeshMapCatalog.cs`, `Maps/MapSelector.cs`,
`Maps/MapDecorRenderer.cs` (25 m chunks of combined meshes, rebuilt on occupancy change),
`Maps/MapDecorData.cs`, `Maps/TerrainPhysicsLayer.cs`, `Maps/StartZone*.cs`, `Maps/MapHistoryStore.cs`,
`Maps/NavAreas.cs` (Gate area), `Maps/OreDepositSpawner.cs`; spots of any kind via
`MeshMapDefinition.SpotsOf(kind)` (`MapSpot`); legacy PNG maps `MapCatalog/MapDefinition/MapTerrainGenerator/TerrainType` (+ `Editor/MapImporter.cs`). UI `UI/MapSelectPanelController.cs`
(`#map_name_<id>`, `#map_desc_<id>`).

## Tests
PlayMode `Map3Tests`, `Map4Tests`, `Map5Tests`, `MapPickerTests`, `TerrainPhysicsTests`, `CameraTerrainTests`.

## Traps
- Water = NO ground mesh under the cell → no NavMesh → everyone paths round. Fords are ground + no-build.
- BRIDGE: the deck must sit 0.2 m below the building origin and overlap the shore (1.6, 0.3 m past each
  edge) — before the fix no bridge on Maps 3/4 ever opened a path (0.5 m step > agentClimb).
- Decor first came out 2× too big: trees are 1–2 m, a citizen 0.45 m. Decor crags only on relief
  > 0.8 m (nothing walkable may look like gatherable stone).

## Seasons (2026-10-24)
The user: «сделай стиль на каждый сезон, потом разберёмся как давать игроку их» (delivery undecided:
NOT by the real calendar -- the user rejected that; random per run doubted; my proposal = a collection
unlocked by play). Every mesh map, both modes, in Summer (today's look), Autumn, Winter, Spring.
- No new models: `Tools/blender/seasons.py -- G:/Fork/CityBuilding` writes `Models/Palette/Seasons/`
  `Terrain-<S>.png` (the terrain palette recoloured: grasses, decor leaves, flowers, water; winter
  also light rock) and `Trees-<S>-Conifer.png` / `Trees-<S>-<n>.png` (the buildings' palette with the
  4 crown cells leaf/grass_dark/roof_green/grass recoloured; autumn gold/orange/red, winter frosted,
  spring pink/white blossom + fresh green). Edit the dicts there, rerun, rebuild.
- `SetupProjectSeasons.BuildSeasonLooks` makes a material copy per palette (`Seasons/*.mat`, GUIDs
  kept) and `Resources/SeasonLooks.asset` (`Maps/SeasonLooks.cs`: per season terrain, conifer,
  broadleaf[], snow, sunTint; conifers = Tree2/5/6/9).
- `Maps/SeasonStyle.cs` at map load (`MeshMapApplier.Apply`): `ApplyToScene` (global `_SeasonSnow`,
  sun tint via `SeasonSunTint`), `DressTerrain` on ground/relief, `TerrainMaterial` for decor,
  `DressTree` in `TreesAreaSpawner` (broadleaf palette picked by cell hash). Winter snow =
  `BuildingDetail.shader`: faces with normal.y > 0.35 go white × `_SnowMask`; figures (soldiers,
  heroes, orcs: `ArmyManager.AddFigure`, `OrcRaidManager`) wear `Seasons/Figures.mat` (_SnowMask 0).
- Picked as a THEME in the menu (2026-10-25: sandbox and invasion screens' round buttons,
  `Core/MapTheme`; the settings row is gone): `GameSessionIntent.NewGameSeason` -> `SeasonStyle.Session`
  (set in `MeshMapApplier.Start`), saved per game (`GameSaveData.season`; -1 in old saves = summer).
  `Current = Override ?? MapForced ?? Session`. Tests can pin `SeasonStyle.Override`.
- Test `SeasonTests.EverySeason_DressesTheMap` (+ photos season-<s>-base/-wide).
- Not done: building greens (castle lawn, gardens) stay summer-green in autumn; water is its own
  material (unchanged); the main menu's live background is always summer; not seen on a phone.

## Liveliness: wind and clouds (2026-11-02, user: «карта безжизненная»)
- **Wind** (approved): `BuildingDetail.shader` keyword `_WIND` (+ `_WindStrength`, `_WindHeight`,
  `_WindByUV3`) sways vertices in all three passes (`WindOffset`: one direction 0.94/0.34, rolling
  gusts + sway + flutter). Trees: every season's tree material made `Windy()` in
  `SetupProjectSeasons` (summer now has its own `Trees-Summer.mat`, not the buildings' palette);
  weight = (world height over the pivot / `_WindHeight`)². Map props: `MapDecorRenderer` writes
  uv3.x (tuft/flowers/bush/fern/reeds, (y/top)²; stones 0) and `SeasonStyle.DecorWind` gives them a
  wind copy on the terrain palette. Dungeons stay still. Buildings never take the keyword.
- **Clouds** (user rule: a cloud shadow only with the cloud itself visible): `Maps/CloudLayer.cs`
  + `Shaders/CloudField.hlsl` (one global drifting field from CloudNoise.png) — every BuildingDetail
  surface is darkened under it (forward pass, along the sun ray); the sheet itself
  (`CloudLayer.shader`, `Materials/CloudLayer.mat`) at 10 m fades in only when the camera is
  15.5–18.5 m over the ground (max zoom). Not in a dungeon; globals zeroed in OnDestroy.
  Fix 2026-11-02: the fog blanket (Transparent+50) covered the clouds in a rectangle -> clouds now
  Transparent+80 and the sheet fades out round its rim; shadow strength 0.28 -> 0.12 («как дым»).
- The terrain palette material is now `CityBuilder/BuildingDetail` (was Simple Lit) so the land
  takes the cloud shadows.
- **Not seen in game / on a phone; no tests run** (user: tests later, one general run).
- **Marked, NOT to build:** sun glints on the sea; chickens/cows at farms.
- **Wildlife BUILT 2026-11-02** (models approved by the user): `Tools/blender/fauna.py` ->
  `Models/Fauna/Fauna{Gull,Bird,Deer,Rabbit,Fish,Dolphin,Whale}.fbx` (export_fbx «Fauna*», jointed:
  wings/legs/head via `FigureAnimator`, Tail turned by code). `Maps/Wildlife.cs`, started by
  MeshMapApplier (not dungeon, not Invasion), models in `wildlifeModels` (Wildlife.Kind order, wired
  in SetupProjectCityScene). **User rule (same day): animals have FIXED places, never vanish and
  reappear elsewhere as the camera moves.** `LayOut()` once, 2 s after start: walks the grid on a
  3-cell stride into land / coast / open / deep water, seeds from the layout, spreads homes apart:
  deer pairs and hares walk a loop of up to 5 waypoints round their home (graze, walk, bolt 3.5 m from
  citizens/soldiers/orcs, then back to the round); gulls circle fixed coast spots (+1 per ship in view,
  kept with it); fish leap / dolphin pods arc / whale surfaces at fixed spots along a fixed line,
  alternating direction, under water between showings. Off-view (> 1.5 x reach, reach = cam height
  x1.3, 10..28 m) or unrevealed animals are SetActive(false) and resume where they were; models made
  on first sight. Songbird flocks still rise from a random tree in view (pooled). Shoal fish
  untouched (boxy by the user's wish). Visual-only constants in Wildlife.cs. LayOut's raycast
  cost on a big map not measured on a phone. Not seen in game; no tests.
  Points of interest (wrecks, ruins, landmarks) liked but models need approval first. Island
  biomes and neutral ships: «пока что нет».

## Open / ideas
Vikings by sea (free mode); dungeons in ruins (squad walks a labyrinth for treasure); islands of
resources across a river; Map 3 decor ~12.8k props / ~240k tris is the first thing to watch on a phone.
