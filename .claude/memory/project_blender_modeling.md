---
name: project-blender-modeling
description: "Claude models the buildings in Blender (started 2026-10-12): art direction, the placeholder-file workflow, the set's standard, what is done (all 44 of the roster), what is left, the open questions and the traps. Read this before touching Tools/blender or the .blend."
metadata:
  node_type: memory
  type: project
  originSessionId: 641cb095-4738-4cd0-bdba-017e1713d143
  modified: 2026-10-15T18:35:05.199Z
---

Snapshot as of **2026-10-13**. The last commit on this work is `ba599d9e`; everything is pushed. ALL 44 roster buildings in level 1 are now modelled (only Road1 and Tree1–5 placeholders remain).

## The brief, and how the user works

**Art direction, in the user's words:** do NOT copy Kingdoms and Castles. Take its SIMPLICITY
and its JUICY, saturated colour. Mobile comes first ([[feedback_mobile_first_optimization]]):
keep the triangle count modest but the detail good. The budget is in
[[project_performance_baseline]]: housing 500–1000, services 1000–1500, landmarks 3000–4000,
fence pieces ≤300. The reference look is chunky blocks with small chamfers, ribbed roofs made
of geometry, dark timber on cream walls, and flat colour.

**Loop:**
1. The user names the next batch and says they rebuilt the scene. Re-read the source file every
   time: sizes change between batches.
2. Claude models, renders and sends the PNGs.
3. Claude commits the scripts and pushes ([[feedback_auto_push]]).

The user reviews in Blender. Since 2026-10-13 the set IS in the game (see «In Unity» below): after changing a model, re-export it (`export_fbx.py -- D:/Fork/CityBuilding [Name ...]`) and rebuild. Work in level 1 only, and only on
the buildings the user names.

**Files:**
- The user's placeholders are in `D:\blender projects\Buildings3D\Buildings-Size1.blend`. That
  file is READ-ONLY for Claude: every run loads the size boxes from it, so a scene update is
  picked up automatically.
- Claude writes only into the copy `Buildings-Size1-Models.blend` in the same folder. Each model
  replaces the mesh of the object with the same name. The box is kept as a wireframe
  `<Name>-size` object in the «Габариты» collection.

**Code:** `Tools/blender/` in the repo.
- `kit.py` holds the palette (40 cells, one material, UVs point at cell centres) and the
  primitives: box/obox, prism, cylinder, ring, segment_ring, pyramid, dome, blob, rod,
  hcylinder, gable_roof, ring_roof, gable_block.
- `kit.py` also has `Facade` for axis-aligned walls, `FaceAt` + `polygon_faces` for walls facing
  any angle, and `kit.run(obj, build, source_name=, offset=, collection=, save=)`.
- `details.py` holds THE STANDARD plus the shared details.
- There is one script per building, named `<id>1.py`. `fences1.py` builds all 8 fence pieces.
- The renderers never save: `render_preview.py <Obj> <dir>` makes 3 shots,
  `render_lineup.py <out> Obj...` renders a row with game-scale citizens, `render_fence_test.py`
  and `render_defence_line.py` check fence joins.
- Blender 5.2.2 is at `D:\SteamLibrary\steamapps\common\Blender\blender.exe`. Run
  `--background <Models.blend> --python <script>`. Each script prints `MODEL … tris=` and
  `FITS` or `DOES NOT FIT`.

## The standard (the user's KEY requirement: everything reads as one set)

**Rule:** every size a player compares between buildings lives in `details.py`. Building
scripts pass a floor's base and a position, never their own sizes.

| Constant | Value |
|---|---|
| FOOTING | 0.10 |
| STOREY | 0.56 (G_TOP 0.66, U_TOP 1.22, L_TOP 0.94 for stores) |
| Door | 0.26×0.50 |
| Gate | 0.40×0.56 |
| Window | 0.20×0.24, sill 0.2 above the floor's base |
| Arch window | 0.18×0.32 |
| Roof pitch | 45° |
| Roof rib | 0.08 wide, every 0.2 |
| Curtain wall | 0.62 |
| Merlon | 0.14 long, step 0.28 (0.25 on fence pieces) |
| Palisade | 0.72 |
| Stone fence wall | 0.30 thick |

**Scale:** 1 unit = 1 m = 1 grid cell. The citizen is 0.45 m: the user chose to shrink the
people rather than raise the buildings, via `UnitSize.Scale` = 0.625 on citizens, soldiers and
orcs, commit `966b54e7`, tests green.

**Limits:** height may exceed a placeholder (user permission); the footprint may not.

**Roof colour by category:**

| Category | Colour |
|---|---|
| City | terracotta |
| Town Hall / power | blue |
| Defence | blue |
| Storage | red |
| Entertainment | violet |
| Production | slate `d.SLATE` (user's choice 2026-10-13) |
| Food | straw/thatch `d.STRAW` (user's choice 2026-10-13) |

## Done: 30 buildings, plus 8 fence pieces

- **City:** Castle1 (Town Hall, 3941 tris, tower 2.64 high), Well1, Hovel1, Cottage1, Manor1,
  FireBrigade1, HealerHouse1, Laboratory1.
- **Storage:** Barn, BigBarn, Granary, BigGranary, Warehouse, BigWarehouse, Treasury,
  BigTreasury. Each big one is the small one grown up.
- **Entertainment:** TownSquare, Flag, DecTree, DecBush, DecGarden, Tavern, Church, Theater
  (round "Globe"), Colosseum.
- **Defence:** Barracks, Gate, Tower, FortifiedGate, FortifiedTower.
- **Fence pieces:** `Fence1-lvl1-Straight/-Corner/-T/-Cross` (palisade) and the same four for
  `FortifiedFence1-lvl1` (stone). The user decided 4 pieces per fence. Wooden tier pairs with
  Gate1 and Tower1; stone tier with FortifiedGate1 and FortifiedTower1. A centre post or block,
  with arms to the cell edge on a shared grid. FortifiedFence is models ONLY; it is not a
  building in the game.

- **Production (2026-10-13, `88b24aad`):** Sawmill, Quarry, Iron/Copper/Gold/CoalMine, Smelter. Mines share `d.rock_hill` built from `kit.crag` (faceted tapered rocks, turf crowns); `rock(ore=, veins=)` auto-places ore crystals on the most front-facing faces. Ore colours: iron `roof_slate` (rust red read as rubies, `iron` grey read as coal), copper `roof_orange`+`roof_green` patina, gold `gold`, coal `window`. Each mine has its own head (adit / headframe / sluice / stone arch).
- **Food (2026-10-13, `ba599d9e`):** Farm (1x1 field of wheat tufts + scarecrow), Orchard, FisherHut, PigFarm, Smokehouse, Windmill (`kit.frustum` tower, dome cap, X sails), Baker.
- Palette is now FULL (40/40 cells; slate + straw took the last four). A new colour means growing ROWS, which moves every UV: then re-run every script.

## Left in the roster

- **Trees (2026-10-13):** `trees1.py` builds Tree1–10 (young 1.0, fir 2.0, oak 1.75, birch 1.25, spruce 1.5, pine 1.9, poplar 1.8, twin 1.1, sapling 0.8, maple 1.4); Tree6–10 had no object in the Models file, so `kit.run` creates them in «Collection». The user's placeholders are all ONE 1x1x2 box, so heights live in `HEIGHT` in the script. Budget ~60–200 tris (≈250 on screen). Leafy crowns use `kit.ball` (blob reads as a gem).
- **Road (2026-10-13):** `road1.py` builds Road1-lvl1 = SAND track 2 cm, 60 tris (user: level 1 is sand) and Road1-lvl2 = 3 cm cobbles, 110 tris (lvl3 not made). Repeating details (worn patches) read as a stamp — keep tile details tiny. No kerbs: roads do NOT autotile in the game (one model per cell), so the pattern tiles across every edge; edge half-stones share one colour. Not physical by design (user): roads don't carve NavMesh, citizens are pinned to ground height and sink ~2 cm. `scratchpad/road_test.py -- <out> <Obj>` renders a road network (not in repo).
- **Stones (2026-10-13):** `stones1.py` builds Stone1–10 (user's 1x1x1.5 boxes) as a SIZE LADDER, Stone1 smallest; volumes printed (0.002–0.37 m³). Purpose: hand-gathered stone, the user wants the stock to depend on size. IN THE GAME since 2026-10-13: MeshMapDefinition.RockPrefabs → RockSpawner picks a model per cell by cell hash (weights n−i, small common), so saves need no change. Every rock still holds `StonePerRock` — yield-by-size postponed by the user ("разберёмся позже"). Stone1–3 were nearly invisible in-game, so the user had them made ONE size (Stone3's, ~20 cm × 0.5 m), different shapes; Stone4–10 untouched.
- **Every placeholder in the source file is now modelled.**
- **Map proposal (2026-10-13, `e566b34e`, awaiting the user's verdict):** `Tools/blender/map1_claude.py -- <out.blend> [preview]` generates `D:lender projects\Buildings3D\Map-1-Claude.blend` (user's Map-1.blend untouched): one mainland, N/W mountain ridges, SE sea, river with a stone ford, lake, hills. Same 4 mesh names as Map 1. Game rules it respects: water = NO ground mesh under the cell; walkable/buildable = flat at 0 (±0.5); trees zone flat only, ~2 500–3 000 cells for the spawner's 600 trees (min spacing 1); palette-coloured, greedy-merged (17k tris vs 35.7k). Lessons: large darker meadow patches read as treeless forest — only forest gets the dark floor. IN THE GAME as **Map2** (`af109865`): `export_map.py -- D:/Fork/CityBuilding Map2 Map-2` writes Models/Map2/Map-2-*.fbx; SetupProject.CreateMeshMapDefinition(mapId, prefix, sharedWater) — water material created ONCE (CreateWaterMaterial deletes+recreates). Palette remap now applies to all Models/. Map1 untouched; MapSelector prefers unseen maps (fresh install = random). Map2Tests (5) guard orientation/water/NavMesh/forest.
- Water buildings were deliberately left alone in the old generator. Check before touching them.
- Manor1 pokes 0.01 past its box in x (found 2026-10-13 against the updated source file); not fixed yet.

## Open questions put to the user and never answered

- Manor at 2214 tris: trim it to ~1300 by dropping the garden, or keep it?
- The Laboratory dome is violet, the old "science" accent, which now clashes with
  Entertainment's violet. Recolour it blue or gold?
- Treasuries have red roofs (Storage). Should they be blue (power) instead?
- Hovel: raise the walls? Use a straw thatch as an exception to the colour rule?
- Town Hall: warmer stone? Is the courtyard too empty?
- Placeholders that float slightly above z=0 (Well, Manor, FireBrigade) are modelled from the
  ground; `kit.run` prints a NOTE when it does this.

## In Unity (2026-10-13, `1d7533b3`)

- `Tools/blender/export_fbx.py` exports every model to `Assets/_Project/Models/Buildings/<Name>.fbx` (trees to `Models/Terrain/Tree<n>.fbx`) plus `Models/Palette/BuildingsPalette.png`. Origin = plot centre; axis_forward='Z', axis_up='Y', bake_space_transform → Blender (x,y,z) = Unity (x,z,y). Verified in-game: fronts face the camera, fence ring and corners join.
- The FBX material `Buildings` is remapped at import (ModelImportDefaults) onto `Models/Palette/BuildingsPalette.mat` (Simple Lit, point filter, no mips, uncompressed). SetupProject.EnsurePaletteMaterial updates it IN PLACE — never delete it (remaps are by GUID).
- Authored buildings now use the WRITTEN footprint (SetupProject list = the user's boxes, checked all 47); only height is measured. FitModelToGround only fixes the base, trusts the horizontal origin.
- Buildings/Terrain FBX: isReadable off, no animation. TreeModelCount = 10. Fence uses Fence1-lvl1-Straight/-Corner FBX; T/Cross exported but unused.
- Road1-lvl2 is wired as the road's upgrade model automatically.
- Tests after integration: EditMode 403, PlayMode 114 green; photo tests (`-testFilter "Photograph|FenceAutotiling"`, WITH graphics) write `playtest_shots/`.
- Not yet seen on a phone.

## Pending

- DONE 2026-10-15 (`31f5595c`): the fence uses its T and Cross pieces in the game.

## Traps already paid for

- **The palette must be RECREATED every run.** Repainting a packed image keeps the old packed
  bytes, so new colours rendered black. Fixed in `ensure_palette`. Palette cells are
  append-only: moving one breaks every face pointing at it.
- **Never use a bare `cat > file` without a heredoc.** It waits on stdin and hangs the shell.
  Write scripts with the Write tool.
- **Python one-liners that patch files:** assert the old text exists first. Files are LF. Unity
  `.cs` files may be CRLF.
- **A rotated square rod resting at z=0 dips ~1 mm below the ground.** Start rods at z ≥ 0.01.
- **First-pass blocky rock reads as a concrete bunker; a roof over a workbench hides it from the 50° camera** (sawmill). Put the defining prop in the open yard.
- **Check the footprint after every build.** It is usually a roof overhang, a sign or a step
  that pokes out.

## Levels 2–5: where things stand and how to pick up (2026-10-15, end of session, master `f4ca458d`)

- **Models exist for levels 1–5 of all 44 buildings** in `Buildings-Size1-Models.blend`, scripts committed. Each level N stands (N−1)×LVL2_DX to +X of level 1 (the step is footprint width + 0.6), all on level 1's placeholder box.
- **UPDATE 2026-10-15 late night: all of 2–5 are exported and in the game (MaxLevel 5, `f80abe62`).** After editing a level model, re-export it by name (`export_fbx.py -- D:/Fork/CityBuilding X1-lvl4 ...`) and rebuild. Trap paid: `Orchard*` starts with `Orc`, so export_fbx sent it to Models/Orcs (fixed in the script). The lines below describe the state before that.
- **The game supports only 1–3.** Nothing of 2–5 is exported to Unity yet. Open, all waiting on the user:
  1. export + rebuild + phone check of lvl2/lvl3;
  2. code for lvl4/5 «потом»: `BuildingInstance.MaxLevel`/`UnitBalance.MaxLevel`, `_4/_5` sheet columns for 49 buildings (via the balance-sheet skill), research gating, `SetupProject.AddLevelModels` (loops to MaxLevel).
- **Export trap:** `export_fbx.py` with no names exports EVERY model, so lvl4/lvl5 would also land in Models/Buildings (88 unused FBX) until the code supports them — pass explicit names (`... D:/Fork/CityBuilding Hovel1-lvl2 Hovel1-lvl3 ...`) or accept the extra files.
- **Chain of dependence:** lvl3 borrows lvl2 (except the 4 pilot copies), lvl4 borrows lvl3, lvl5 borrows lvl4. Editing a lower level changes the higher ones on their next run — rerun the higher scripts after a lower one changes. Generated: lvl4 (40 of 44) by `gen_lvl4.py`, lvl5 (all 44) by `gen_lvl5.py`; edit the tables, not the outputs.
- **Review loop that worked:** a per-category lineup (`render_lineup.py` with pairs `X-lvlN-1 X-lvlN`) for the set, plus `render_preview.py` game-angle shots for anything small; the small helper shell scripts for this lived in the session scratchpad, not the repo.
- Every level was done as pilot-of-four (Hovel, Cottage, Warehouse, Sawmill) → user approves → the rest. Keep that rhythm for any level 6+.
- Blender drops a `Tools/blender/__pycache__/` — untracked junk, delete, never commit.

## Level 2 (2026-10-15) — all 44 buildings modelled, committed, not exported

- **Concept (user's choice):** «развитие того же здания» — recognisably the same building, richer: stone ground floor/course with quoins, dormers, an extra storey or taller tower where it fits, flags/weathervanes/lanterns, busier yard. Roof colour and silhouette stay. lvl3 later = grander still (towers, gold). The user approved after a 4-building pilot (Hovel, Cottage, Warehouse, Sawmill) and said «делай остальные».
- **How it's built:** `Tools/blender/<id>1_lvl2.py` = a COPY of the lvl1 script with additions (copy, then exact-string patches), `kit.run("<Id>1-lvl2", build, source_name="<Id>1-lvl1", offset=(LVL2_DX,0,0), collection=<category>)` — uses lvl1's placeholder box, stands footprint-width+0.6 to +X of lvl1 (no lvl2 placeholders exist; if the user adds `*-lvl2` boxes, switch source_name). New shared helpers in `details.py`: `quoins`, `dormer(face, eave, s, ze, h=, wall=, **roof)`, `fountain`, `weathervane`.
- **Rules kept:** footprint = lvl1's (all inside their plots; Granary awning 2.5 cm and Manor 1.3 cm over, as lvl1); height grows freely (Tavern 3 storeys 2.5 m, FortifiedTower +1 storey 3.56 m, Church spire +0.3). Defence pieces keep the palisade/wall joins exactly (Gate/Tower/FortifiedGate arms untouched).
- **Skipped:** fences/FortifiedFence (game shows only lvl1 fence pieces), Bridge/WaterMill/Dock (no Blender models).
- **Traps:** lvl1 scripts have no `__main__` guard — importing one rebuilds lvl1 (copy helpers instead, e.g. sawmill planks); don't roof over a building's identifying prop (sawmill blade); `water` is the only light-blue PLAIN colour (used for the glasshouse roof — `roof_*` colours get tile pattern on slopes); a scratchpad file named `inspect.py` shadows the stdlib — run helper Python with `-I`.
- Scripts committed 2026-10-15 (`710f9c81`). Export still pending — see «Level 3».

## Level 3 (2026-10-15) — all 44 modelled, committed+pushed (b076bf79..fd527fbe), NOT exported

- **Concept (user approved after the 4-building pilot):** level 2 crowned — gold finials on ridge ends, weathervanes, a turret/cupola/louvre/tower where the plot allows, paved yards, low stone walls with gold-ball piers, gilded statues; entertainment gets bunting. Roof colours kept.
- **How it's built:** the pilot four (`hovel/cottage/warehouse/sawmill1_lvl3.py`) are full copies; the other 40 use `kit.borrow("<id>1_lvl2.py")` — loads the lvl2 script's namespace with kit.run stubbed — then `b = L2['build'](lo, hi)` and ADD. So lvl3 = lvl2 + extras and follows lvl2 edits. Tricks: set `L2['RAISE']`/`['STACK_UP']` before calling build (FireBrigade, BigTreasury, Smelter); pass a raised `hi` (Flag, Church). Offset `LVL3_DX = 2 * L2['LVL2_DX']`.
- **New helpers in details.py:** finial, turret, paving, garden_wall, pier, pennant, cupola, statue, armillary, louvre, bunting.
- **Tris:** sum lvl1 56.8k → lvl2 81.8k → lvl3 100.5k (avg ×1.23 over lvl2; Sawmill ×1.7, TownSquare ×1.66 are the biggest jumps). Housing is well over the 500–1000 budget at lvl2/3 (Hovel3 2488, Cottage3 3006).
- **Traps:** a gold ball finial at a ridge end overshoots the plot by its radius — keep it ≥0.04 inside the edge; a turret's cone (r+0.07) is what overflows, not its shaft; a turret next to a dormer or below the neighbouring ridge gets swallowed (Manor: moved to the side wing's corner, shaft raised above that wing's ridge).
- **Next (waiting on the user):** `export_fbx.py -- D:/Fork/CityBuilding` (lvl2+lvl3 FBX auto-wired by SetupProject.AddLevelModels), rebuild, check on a phone.

## Level 4 (2026-10-15) — models only, all 44, committed (9e5103ea..fadaee5d)

- **The game has NO level 4** (`BuildingInstance.MaxLevel = 3`, sheet has `_2/_3` only, SetupProject wires up to `-lvl3`). The user asked for models now, code later ("в коде поправим потом"): MaxLevel, `_4` sheet columns for 49 buildings, research gate, AddLevelModels.
- **Concept (user approved after the pilot):** level 3 + a GLAZED ROOF — a lattice of flat diamonds in the category's dark shade and gold over the tiles — plus a gilded crest along the ridge. Read from the game camera because roofs are what it sees. Thatch (Food) uses red accent diamonds: gold vanishes on straw.
- **Helpers in details.py:** glazed (=glazed_roof+ridge_crest, args mirror d.roof), glazed_pyramid, glazed_shed (args mirror d.shed_roof), glazed_ring (ring_roof), glazed_dome, ring_crest. Diamonds are single quads (2 tris), so lvl4 is only +10% tris over lvl3 (100.5k → 110.7k for all 44).
- **Scripts:** pilot four hand-written (`kit.borrow("<id>1_lvl3.py")`); the other 40 are GENERATED by `Tools/blender/gen_lvl4.py` (one table row per building: roof params + extras) — edit the table and rerun `python gen_lvl4.py .`, don't hand-edit those 40. `LVL4_DX = L3['LVL3_DX'] * 1.5`.
- Round roofs had to get their own glazing (theatre ring, windmill cap, observatory dome) — without it those looked identical to level 3.

## Level 5 (2026-10-15) — models only, all 44, committed (706d9f18..f4ca458d)

- **Concept (user approved):** «золотая кровля» — level 4 with every roof slope recoloured to gold (`d.gild_roofs`: roof base colours only, sloped faces 0.2<nz<0.995, area ≥0.006 so apples/pumpkins/flowers survive), gold drawn as TILES via `detail.OVERRIDE['gold'] = detail.TILES` set in that model's script (per-process, never leaks). Gold diamonds of lvl4 vanish into the gold; the dark lattice stays. No new triangles.
- **Food = red-orange** (`gild_roofs(color='roof_orange')`, user's call): gold ≈ straw (F5B73B vs EDB63E) so gold was invisible on thatch. Same trap for the farm's wheat (hay/straw ≈ gold) — its level 5 also paints the field fence roof_orange/roof_red.
- **Roofless ones** use `d.recolor(b, {src: dst}, where=lambda c: ..., min_area=)` (face centre filter; swaps work): golden wheat+fence (Farm), golden fountain (TownSquare, DecGarden + its cones), cloth-of-gold banner (Flag, purple↔gold swap), gold-leaf crown (DecTree), gold topiary (DecBush).
- **All 44 scripts generated** by `Tools/blender/gen_lvl5.py` (SPECIAL dict for Food/roofless). `LVL5_DX = L4['LVL4_DX'] * 4/3`.
- **Trap paid:** `glob("*_lvl4.py")` also matches `gen_lvl4.py` — the generator overwrote ITSELF. Glob `*1_lvl4.py`.

## Soft look (2026-10-14) — the user's choice after rejecting a desaturated palette

The user found the buildings "грубые" next to Map 3 and wanted SOFTER DETAIL, explicitly NOT less bright colour (a desaturated-palette try was rejected). Picked "variant 3" = soft geometry details + a painted detail texture:
- `Tools/blender/detail.py`: grey pattern atlas `Buildings-Detail.png` (8 seamless patterns in a 4x4 grid of 256 px: plain, scalloped tiles, plaster, planks, coursed stone, leaves, slate, thatch), picked per face FROM ITS PALETTE COLOUR (roof colour on an upright face → plain, so `roof_slate` works as glass); 2nd UV `Detail` (metres on the face plane / pattern size), 3rd UV `Pattern` (cell index in u), vertex colour `Shade` (walls darken to 0.84 at the ground). kit.run applies it to every model; export_fbx exports colours LINEAR and copies the atlas to Models/Palette/BuildingsDetail.png.
- Game: `Shaders/BuildingDetail.shader` = palette × atlas×2 × shade, Blinn-Phong via URP's UniversalFragmentBlinnPhong (same light as the old Simple Lit). Palette material switched in place (GUID kept). Models without uv2 fall back to flat.
- Roofs are now smooth slabs (kit.gable_roof smooth=True, details.shed_roof) with a round ridge roll; tiles come from the atlas. Verge boards sit INSIDE half_len (outside them pushed Barracks/FisherHut out of their plots).
- details.py: windows = light frame + blue-grey glass (`d.GLASS`='roof_slate') + white curtain triangles; flower boxes = green mound + blossoms (`Facade.blossom`); `footing` adds grass tufts clipped to `kit.PLOT`; `climber`, `wall_lantern` helpers (used on Cottage, Hovel).
