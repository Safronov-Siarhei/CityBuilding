# Maps and terrain

> **Status (2026-10-21):** New Game opens a MAP PICKER (`MapSelector.PlayerMapIds` = Map5, Map4, Map3,
> newest first = default). Map 5 «Кальдера» and Map 4 «Долина двух рек» are designed island maps with
> planned orc spots. Maps 1–2 stay only for old saves. None of Maps 4–5 seen on a phone.
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

## Maps
- **Map 3 «Летняя долина»** — Claude's first own map: 6 forests + groves, 6 stony clearings, relief.
- **Map 4 «Долина двух рек»** — two rivers from northern mountains split the island; fords; main base
  in a cirque (0,67); 4 camps all L3; beaches (0,−90), (−90,−4), (90,−8).
- **Map 5 «Кальдера»** — town on an old volcano floor round a crater lake; mountain ring cut by four
  gorges; a creek out the east gorge with two fords and a bridge; main base in a valley between two
  spurs; four camps (L3, L3, L2, L2) outside the ring; three Viking beaches.
- Variants kept for later: Перешеек, Фьорды, Великое ущелье.

## Pipeline
Blender generator `Tools/blender/mapN.py -- <out.blend> [preview]` (Map3 machinery; Map4/5 add a
layout section: coast, cliffs, mountains, rivers with `FORDS_AT/BRIDGES_AT`, CAMPS=(x,y,level),
MAIN_BASE, START_ZONE, BEACHES) → `render_mapN.py` review shots → regenerate WITHOUT `preview` →
`export_map.py -- D:/Fork/CityBuilding MapN Map-N` (Ground/Relief chunks, Water, TreesArea, RocksArea,
NoBuildArea, DecorKit, TerrainPalette, Decor.csv, Spots.csv) → SetupProject `CreateMeshMapDefinition`,
`ReadOrcSpots`, `CreateMapDecorData` → `Resources/MeshMaps/MapN.asset`.
Probe a layout without Blender: `probe_map.py` (counts water/relief cells per camp square — 0 = fits).
The .blend files live OUTSIDE git: `D:/blender projects/Buildings3D/Map-*.blend`.

## Code
`Maps/MeshMapApplier.cs` (applies a map; relief out of NavMesh; `IsNoBuildCell`, `RocksZoneCells`,
`CurrentMap`; terrain onto layer 6), `Maps/MeshMapDefinition.cs` (`HasMainBaseSpot`, `CampSpots`,
`CampLevels`, `TownHallZone`, decor), `Maps/MeshMapCatalog.cs`, `Maps/MapSelector.cs`,
`Maps/MapDecorRenderer.cs` (25 m chunks of combined meshes, rebuilt on occupancy change),
`Maps/MapDecorData.cs`, `Maps/TerrainPhysicsLayer.cs`, `Maps/StartZone*.cs`, `Maps/MapHistoryStore.cs`;
legacy PNG maps `MapCatalog/MapDefinition/MapTerrainGenerator/TerrainType` (+ `Editor/MapImporter.cs`). UI `UI/MapSelectPanelController.cs`
(`#map_name_<id>`, `#map_desc_<id>`).

## Tests
PlayMode `Map2Tests`, `Map3Tests`, `Map4Tests`, `Map5Tests`, `MapPickerTests`, `TerrainPhysicsTests`.

## Traps
- Water = NO ground mesh under the cell → no NavMesh → everyone paths round. Fords are ground + no-build.
- BRIDGE: the deck must sit 0.2 m below the building origin and overlap the shore (1.6, 0.3 m past each
  edge) — before the fix no bridge on Maps 3/4 ever opened a path (0.5 m step > agentClimb).
- Decor first came out 2× too big: trees are 1–2 m, a citizen 0.45 m. Decor crags only on relief
  > 0.8 m (nothing walkable may look like gatherable stone).
- `CreateWaterMaterial` deletes+recreates — make it ONCE per run.

## Open / ideas
Vikings by sea (free mode); dungeons in ruins (squad walks a labyrinth for treasure); islands of
resources across a river; Map 3 decor ~12.8k props / ~240k tris is the first thing to watch on a phone.
