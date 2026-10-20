---
name: project-maps-roadmap
description: "The user's plan for new maps (2026-10-20): designed terrain on an island, one buildable plain, the sea all round for Viking raids in free mode; one map at a time starting with «Долина двух рек»; pathing rules; the dungeon idea for later."
metadata:
  node_type: memory
  type: project
  originSessionId: 979576c4-7a0c-49a0-b236-1e57adabdab9
  modified: 2026-10-20T04:58:20.291Z
---

The user's brief for new maps (2026-10-20):
- Not one flat plane: designed terrain with impassable mountains, gorges, rivers. The player still builds on ONE plain.
- The map edge is SEA all round: after the main portal falls, Viking ships come from the sea and attack, so free mode is not boring. Not built yet.
- Size stays 200x200 m for now.
- EVERY character (citizens, soldiers, orcs, later Vikings) must path AROUND mountains and rivers, never through them. Crossings are fords and bridges.
- One map at a time. The first is variant 1, «Долина двух рек»: two rivers from northern mountains split the island in three; the town in the middle; fords and bridges; the main base in a mountain cirque. Other variants shown to the user (kept for later): Кальдера, Перешеек, Фьорды, Великое ущелье.

**Special places, for LATER (user's note):** ruins on the map hold DUNGEON entrances, like the Warcraft 3 campaign. The player sends a squad down; the dungeon is a labyrinth with monsters; the squad walks it, kills monsters, and finds treasure and resources. Islands with resources across a river were also floated. Leave room on maps for such spots.

**Why:** the user wants maps that are rich and interesting, plus an endgame (Vikings) and exploration (dungeons) beyond the orc war.

**How to apply:** design each new map with fixed landing beaches, crossings and a reserved ruin spot. Check that the orc camps' automatic placement (PortalPlacement) still finds room on designed terrain. See [[project_map3]] for the map pipeline and [[project_orc_faction]] for the bases.

**Map 4 «Долина двух рек» in Blender (2026-10-20, fc88f3ba, NOT exported, NOT in the game):**
`Tools/blender/map4.py -- "D:/blender projects/Buildings3D/Map-4-TwoRivers.blend" preview` (copy of map3.py's machinery with its own layout section: coast_sd superellipse island, cliffs() rim broken at BEACHES and river mouths, mountains() with CIRQUE (0,60,23,15) for the main base, rivers = `_meander`ed courses, FORDS_AT/BRIDGES_AT as fractions along each river, LAKE on the west river). Fords lift the river bed to ground at about -0.33 m (walkable sand bar). `render_map4.py` = 11 review shots, copies in playtest_shots/map4. 40% buildable, 33% water.
Still to do for the game: export (export_map.py with prefix Map-4); fixed spots for the camps and the main base (auto placement may put a camp across a river); fords must be walkable and not buildable; check that a built Bridge opens a path for everyone; Viking landing beaches are the 3 BEACHES.

**Map 4 IN THE GAME (2026-10-20, baddc7b1) -- every new game starts on it** (`MapSelector.OnlyMapId = "Map4"`; Maps 1-3 stay for saves).
- Export: `export_map.py -- D:/Fork/CityBuilding Map4 Map-4` (now also copies `<blend>-spots.csv` as `Map-4-Spots.csv`). Regenerate with map4.py WITHOUT `preview` before exporting.
- Orc spots: map4.py MAIN_BASE (0,67) and CAMPS -> Spots.csv -> SetupProject.ReadOrcSpots -> MeshMapDefinition.HasMainBaseSpot/MainBaseSpot/CampSpots -> OrcRaidManager.OpenAtMapSpots (main faces south, quarters 0; camps nearest the Town Hall first, biggest level that fits within 8 m). Maps without spots still search round the Town Hall. MeshMapApplier.CurrentMap added.
- Fords are ground (walkable) and in NoBuildArea. Rivers/sea/mountains have no ground mesh -> no NavMesh -> everyone paths round.
- BRIDGE FIX: the walkable deck now sits 0.2 m below the building origin and 15% longer; before, it met the faceted shore (-0.4 m) in a 0.5 m step > agentClimb, so NO bridge on Map 3/4 ever opened a path.
- Tests: Map4Tests (7). Full: EditMode 446, PlayMode 208 (+4 graphics-only). Photos playtest_shots/map4-*.png.
- OPEN: the two southern camps (-76,-30) and (77,-38) sit on/near the west and east Viking BEACHES -- ask the user whether to move camps or beaches. Not on a phone yet (Map4 models 3.2 MB).

**Map 4 rework (2026-10-20, 2474c240) -- the user's rules for EVERY map:**
- Each map plans its own camps: count, place, and the LEVEL each grows to. Map 4 = 4 camps, all level 3: (-66,28), (-70,-38), (66,26), (68,-40); the camp before the cirque was dropped ("лишний"). map4.py CAMPS=(x,y,level) -> camp_pad() keeps flat, tree-free ground for the level's square (CAMP_HALF 6/7/11 + PAD_MARGIN 1); Spots.csv `camp,x,y,level`; MeshMapDefinition.CampLevels; OpenAtMapSpots starts from the designed level. Probe a layout without Blender: scratchpad probe_camps.py (exec map4.py with stub bpy) counts water/relief cells per camp square -- 0 means it fits.
- Viking BEACHES moved between the camps: (0,-90), (-90,-4), (90,-8).
- START ZONE: map4.py START_ZONE polygon -> Spots.csv `zone,x,y` -> MeshMapDefinition.TownHallZone; `StartZone.Allows` (inside the zone AND >= town_hall_min_distance_to_orcs = 45 m from every camp/main spot) gates the Town Hall in BuildingPlacer (`#place_blocked_start_zone`); `StartZoneOverlay` draws it (gold fill + rim, 2 submeshes, URP Unlit ignores vertex colours) until the Town Hall stands.
- Bugs fixed: BuildingInstance.OnDestroy now decrements its count FIRST (a HUD exception on scene unload left the Town Hall counted into the next game); ResourceHUDController.Refresh guards `this == null`; bridge deck overlap 1.6 (0.3 m past each edge).
- Sheet synced 2026-10-20 (economy A135 town_hall_min_distance_to_orcs, localization A266 #place_blocked_start_zone); verify_sheet: no differences. Trap: Chrome screenshots timed out on this machine -- read the Name Box (#t-name-box) and formula bar (#t-formula-bar-input) via javascript_tool instead, and switch sheet tabs by clicking the tab found via .docs-sheet-tab (ctrl+PageDown did nothing).
- Tests: EditMode 446, PlayMode 210 (+4 graphics). Map4Tests 10.
