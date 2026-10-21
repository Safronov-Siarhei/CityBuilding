---
name: project-map3
description: "Map 3 \"summer valley\" (2026-10-13) — the user's brief, the Blender generator and its rules, what is still needed to put it in the game. Read before touching Map 3 or the map pipeline."
metadata:
  node_type: memory
  type: project
  originSessionId: e2f9aeb6-fb8c-4e4d-a103-d1e062cd15cf
  modified: 2026-10-13T17:41:15.645Z
---

The user disliked the maps (flat green, cubic shores) and asked (2026-10-13) for a map of my own that matches the buildings and trees and is more "alive". Their answers: varied ground + relief + small decor (NOT motion); smooth shores/slopes; a NEW Map 3 (Maps 1-2 stay); gameplay layout may change (several forests, stone in outcrops); relief is DECOR ONLY (build on the plain); "сочное лето"; show Blender renders before the game.

**Generator:** `Tools/blender/map3.py -- <out.blend> [preview]` → `D:\Blender projects\Buildings3D\Map-3-Claude.blend` (+ `-decor.csv`). `Tools/blender/terrain.py` = the map's OWN palette (`TerrainPalette`, 8x6, material "Terrain"; the buildings' palette is full) + decor models. `render_map3.py -- <out_dir> [only=…]` renders 8 review shots.
- Ground = 1 m grid with corners nudged ±0.3 m, cut at CLIP_Z −0.47 (below = no ground = water cell). Colour zones are scalar FIELDS cut along their zero contour (`_cut`/`_split`) → smooth boundaries, flat colours, then `dissolve_limit` by UV. Per-triangle random shades looked like broken glass — the user-facing look is calm zones + a barely different `_b` shade in small patches.
- Objects: Map-3-Ground (plain + beach, ≤0.4 m dip), Map-3-Relief (hills/mountains, separate so the game can keep it out of the NavMesh), Map-3-Water (faceted, depth bands, rim), TreesArea (6 forests + 12 small groves, ~2700 cells for 600 trees), RocksArea (NEW, 6 stony clearings), PlaceForWaterObjects.
- Scale lesson: in-game trees are 1–2 m and a citizen 0.45 m — decor first came out twice too big. Decor crags only on relief above 0.8 m so nothing on walkable ground looks like gatherable stone.
- Numbers (last run): ground 58k tris, relief 22k, water 5k; decor ~17.5k props ≈ 260k tris total (needs chunking + density tier); 62% buildable, 11% water.

**IN THE GAME since 2026-10-13 (`620dceba`), and the ONLY map for new games** (`MapSelector.OnlyMapId = "Map3"`; Maps 1-2 stay in the catalog for old saves and their tests). Export: `export_map.py -- D:/Fork/CityBuilding Map3 Map-3` (parts may be many objects: Ground/Relief are 4x4 chunks; also RocksArea, NoBuildArea, DecorKit, TerrainPalette.png, Map-3-Decor.csv). Game side: MeshMapDefinition extras (relief, rocksArea, noBuildArea, decorData, decorMaterial, keepWaterRotation); MeshMapApplier keeps relief out of the NavMesh, IsNoBuildCell, RocksZoneCells; RockSpawner packs boulders into clearings (spacing 1); BuildingPlacer refuses no-build cells ("Занято"); MapDecorRenderer = 25 m chunks of combined meshes rebuilt on GridManager.OnAreaOccupancyChanged; TerrainPalette.mat via ModelImportDefaults remap "Terrain". Map3Tests (8) green; photos playtest_shots/map3-*.png. Not yet seen on a phone; decor ~12.8k props / ~240k tris is the first thing to watch for frame time.
