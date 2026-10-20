---
name: project-orc-faction
description: "The orc faction (2026-10-14): the user's decisions on its look, model set, in-game wiring and the base design (camp without walls, watchtower/war tent/totem-shield); what is built and what is next."
metadata:
  node_type: memory
  type: project
  originSessionId: 44eb0858-0eb9-49b7-8631-b0eada6c380b
  modified: 2026-10-19T17:43:13.856Z
---

Decided by the user on 2026-10-14. Before that, orcs and portals were cube primitives built in code (`OrcRaidManager.cs`).

## Look and models
- **Reference:** Warcraft orcs. Wood, bone, hide and rough iron, gloomier and cruder than our buildings. The faction's marker colour is **rust-orange**. Anything to do with portals glows in one "alien" colour (violet). **The skin stays green**, even though it blends with the meadow; don't add a palette colour for it.
- **Set:** portal (active/passive/closed), great 4x4 portal for the final fortress, watchtower, war tent, totem, palisade (4 pieces) + gate, a ruin for each building; units grunt, thrower, brute (×1.25), chief (×1.15). No animation for now.
- **Scripts:** in `Tools/blender`: `orcs.py` (the faction standard and shared details) and one script per model (`orc*.py`). The models live in the «Орки» collection of `Buildings-Size1-Models.blend`. Ruins are built with `orcs.run_with_ruin`. Export goes to `Assets/_Project/Models/Orcs`.
- **Trap:** the detail atlas picks its pattern BY COLOUR. Rust (`roof_orange_dark`) on a roof would draw neat town tiles, and the `stone_*` colours draw dressed courses. Fix: `orcs.py` sets `detail.OVERRIDE` so rust, `stone_dark` and `cobble` draw plain on orc models. Use `orcs.ROCK` (slate) for raw rock, and dark `door` for hide roofs.
- **Workflow:** models first, in Blender, for the user's approval; then into the game.

## In the game (`0d7e4bf2`, regenerated assets `c9f54405`)
- `SetupProject.WireOrcModels` hands `OrcRaidManager` the three portal models plus the grunt and the brute.
- **Raiders:** the GRUNT, and from `BruteFromLevel` = 4 the BRUTE. Since 2026-10-15 (`31f5595c`) the THROWER and the CHIEF are `OrcKind`s with their own units rows; wave/garrison composition in `OrcRaidManager.KindInGroup` (see [[project-current-state]]).
- **Portal states:**
  - The ACTIVE portal is the one the NEXT wave comes from (`NextRaidSource`), picked at random. It is a visual cue only: balance is unchanged. It is not saved, so a load re-picks it.
  - A destroyed portal leaves its ruin, saved as `closedPortalCells`.
- **Portal plot:** 2x2 (`PortalFootprint`), facing the Town Hall, snapped to 90°.
- **Fallback:** a missing model falls back to the old cubes.
- **Tests:** `OrcModelTests`, verified by breaking the look switch.

## Orc bases: IN THE GAME (`2dfbb1fe`, regenerated assets `b61cc29f`)
- **Code:** `OrcCamp` (one per portal, keyed by the portal's cell, outlives the portal) and `OrcCampBuilding` (an IDamageTarget structure; on death it leaves its ruin).
- **Layout:** `OrcRaidManager.CampLayout` is in the portal's frame: tower front-left, tent right, totem behind with 2 decorative palisade pieces. `CampSpots` falls back around a ring when the spot is blocked.
- **Guards:** `OrcUnit.SetGuardPost` / `IsGuard`. Guards are NOT saved: the tent re-musters them on load.
- **Shield:** `OrcPortal.IsShielded` is set while the totem stands. SoldierUnit's auto-targeting skips shielded portals.
- **Save:** `orcCampBuildings` (health 0 = ruin). Old saves get fresh camps.
- **Numbers:** 9 economy keys `orc_*`, plus localization `#log_portal_shielded` and `#log_totem_destroyed`.
- **Tests:** `OrcCampTests`, plus checks added to `SaveRoundTripTests`.
- **Sheet updated** (2026-10-14): the 9 `orc_*` economy rows were inserted after `heal_per_healer_per_day` (sheet rows 69–77), and the 2 localization rows were appended at rows 254–255. verify_sheet reports no differences.
- **Not on a phone yet.**

## Orc bases: the user's design (2026-10-14)
- **A camp WITHOUT walls** around each portal: a watchtower, a war tent and a totem. The palisade is decorative only.
- **Watchtower:** shoots the player's soldiers.
- **War tent:** keeps a GARRISON of a few orcs that guard the camp's radius, and refills it while the tent stands.
- **Totem:** the portal's SHIELD. While the totem stands, the portal takes no damage, so the base must be cleared first.
- **Ruins:** destroyed camp buildings leave their ruins.
- **Numbers:** all go to the balance SHEET (the `balance-sheet` skill), not into C#.
- **Still open:** the great-portal final fortress, and what happens after the last portal closes.

## 2026-10-16: the user REVERSED "no walls"
The 2026-10-14 camps are "just stubs". The user wants full BASES of several difficulty levels: the portal in the CENTRE, towers around it, the whole thing enclosed by a PALISADE. Claude proposed: tier by portal distance (5 tiers; the great portal = final citadel), the palisade as a real breakable wall with a gate, layouts as square templates rotated to face the town, the static parts of each base merged into one mesh for mobile.

**The user's answers (2026-10-16):**
1. The palisade is a REAL wall (blocks movement, breakable; gate = weak point).
2. Base difficulty goes by distance from the Town Hall.
3. Clearing a base gives RESOURCES (amount scales with the base's level) + a BREATHER from raids.
4. Bases GROW: 4 of the 5 start weak and level up over time; the MAIN base (great portal) is strong from the start and sends NO raids — raids come only from the 4 secondary ones.
5. **The user will DRAW the base layouts per level** — wait for the sketches, implement them, don't redesign alone (same rule as the HUD sketch). Still to settle: how fast and how far bases grow, and how this combines with "difficulty by distance".

**The user's sketches (2026-10-16), symmetric square plans, gate at the bottom (= towards the town):**
- **Camp level 1:** NO fence. Portal lvl1 in the centre, 4 towers lvl1 at the corners, 4 orcs lvl1 on the diagonals between portal and towers. No tent, no totem.
- **Camp level 2:** fence lvl1 all round with gate lvl1 in the middle of the bottom side. Portal lvl2 in the centre; 4 towers lvl2 at the inner corners; 4 BRUTES lvl1 on the portal's diagonals; 8 orcs lvl2 (one beside each tower, one at mid-left/right above and below); WAREHOUSE (склад, loot store) lvl1 at mid-left; TOTEM lvl1 at mid-right. No tent.
- Implications: orc BUILDINGS have their own levels (tower/portal/fence/gate/totem/warehouse lvl N); the BRUTE is a separate unit kind with its own levels (today it is only the grunt's look from level 4). **User's answers (2026-10-16):**
1. Orc buildings have only HP, attack radius and damage (per level). "у каждого уровня орков меняется модель" — read as: every level has its own MODEL (asked whether this means buildings, units, or both).
2. Portal level adds HP and changes WHICH orc kinds can come out of it (the raid roster).
3. The PORTAL only spawns the ATTACKING force (raids on the player). The base GUARD is refilled by SPAWNER BUILDINGS, one per orc kind (each spawns its own kind on a timer). The sketches for lvl1/2 don't show spawners yet.
4. Guards attack enemies inside the camp's zone (stay home, no gathering at the gate).
5. Sizes agreed: portal 2x2, tower 2x2, warehouse 2x2, totem 1x1; lvl2 camp ~14x14 cells.
6. **Every level changes the MODEL — for buildings AND for warriors** (big Blender job: each orc building and each orc kind × its levels).
7. **Spawner rule:** a spawner is a TARGET (IDamageTarget); while it stands, every 30 s it spawns ONE guard of its kind until that kind's guard count is back to the plan's number; destroyed spawner = no more of that kind. Spawners: war tent → grunts, brute pit → brutes, thrower range → throwers, chief's lodge → chief (main base only).
8. Claude placed spawners on the lvl1/lvl2 plans (shown as an inline diagram 2026-10-16): lvl1 — tent top-centre (behind the portal, away from the gate); lvl2 — tent top-left-centre + brute pit top-right-centre, both behind the portal so the army must cross the camp. Awaiting the user's OK and the plans for lvl3+ and the main base.
9. **Scope (2026-10-16): THREE camp levels for now**; levels 4+ and the main base LATER.
10. **Camp level 3 sketch** — TWO rings, gates at bottom (town side):
   - Outer ring: fence lvl2, gate lvl2 bottom-centre, 4 towers lvl3 at the outer corners; between the rings: ORC spawner (left middle), THROWER spawner (right middle), 7 orcs lvl3 and 14 THROWERS lvl1 spread round the ring.
   - Inner ring: fence lvl2, GATE WITH TOWER lvl1 bottom-centre (new building), 4 towers lvl3 at inner corners, "спавнер громил" top-centre (read as the BRUTE spawner — asked to confirm), 2 warehouses lvl2 on the left, totem lvl2 on the right, portal lvl3 in the centre, 4 brutes lvl2 round the portal, 6 orcs lvl3.
   - Size guess: inner ~14x14, outer ~22x22 cells → portals must sit further from the Town Hall and the area for the MAX level must be reserved from the start.
   - Thrower is level 1 everywhere so far.
   - Asked: raid roster per portal level; how/when a camp grows; confirm "громилы" = brutes.
11. **Answers:** "спавнер громил" = BRUTE spawner (yes). Raid roster per portal level — LATER. A camp levels up WITH THE TOWN'S DEVELOPMENT (PlayerProgression score); on level-up the camp is REBUILT in full (destroyed parts come back).
12. Next: Blender models for the 3 levels (15 new models + ruins), for the user's approval, then code.

**Modelling the base set (started 2026-10-16, `2f7e4e4f`):**
- `orcs.py` got `place`/`place_levels` (a GRID of its own: x=30/34/38 for lvl1/2/3, x=42 the ruin, one row per model; offsets computed from the borrowed box via `source_location`), `iron_spikes`, `skull_chain`.
- PILOT = the watchtower: `orcwatchtower1.py` now builds lvl1-3 + ONE shared ruin on FortifiedTower1's 2x2 box (the 1x1 build grown ×1.75 xy, ×1.3 z → 3.34 m). Level language: lvl1 hide roof; lvl2 + iron parapet plates, spikes, skull chains, RUST STRIPES on the roof; lvl3 + rock plinth, IRON roof with rust strips, eave spikes, great horns, 2nd banner. Roofs carry the level (the game camera sees roofs). Tris 1580/2160/2723.
- The game still uses the OLD 1x1 tower footprint (`OrcRaidManager.FootprintOf`) — nothing exported.
- User approved the pilot; the REST IS MODELLED (2026-10-16, `10dcf1cd`, `fe6f1675`), all in the grid, none exported:
  - `OrcPortal1-lvl2/-lvl3` (+ `-Passive`; `-lvl1-Closed` shared): lvl2 third bands, back menhirs, skull chains, torches; lvl3 + glowing violet rune circle on the platform, iron spike crown, braziers, 2nd banner.
  - `OrcFence1-lvl2-{Straight,Corner,T,Cross}` via `orcs.palisade_arm2` (taller 0.98, iron band, spikes, rocks; 380–866 tris, over the 300 fence budget). `OrcGate1-lvl2` (`orcgate1.gate(b, level)`).
  - `OrcGateTower1-lvl1` (+Ruin), new `orcgatetower1.py`, 1x2, borrows gate lvl2, 2.9 m.
  - `OrcTotem1-lvl2` (iron crown, skull stakes, 4 fire bowls).
  - `OrcStorehouse1-lvl1/2` (+Ruin), 2x2 loot store: loot must stand in the OPEN yard (under the lean-to the camera can't see it).
  - `OrcBrutePit1-lvl1`, `OrcThrowerRange1-lvl1` (+Ruins), 2x2 spawners. The war tent stays the grunt spawner.
  - Units: `OrcGrunt-lvl2/-lvl3`, `OrcBrute-lvl2` (OrcGrunt/OrcBrute = lvl1, names without suffix).
- Ruins: ONE per building type, shared by its levels.
- Review tool used: a scratchpad `render_set.py` (ortho game-angle shots at one scale + stitched sheet) — not in repo.
- User approved the set; exported (26 FBX).

## MAIN BASE — the user's sketch (2026-10-19), under discussion
Backed by IMPASSABLE MOUNTAINS (behind and both sides of the two inner zones). Three zones front to back, every wall STONE, every gate a STONE GATE WITH A TOWER (centre):
- Back zone (against the mountains): the GREAT PORTAL centre-back with a TOTEM either side (two totems), 2 brute spawners, 2 stores at the sides, 2 stone towers, 4 brutes, orcs, throwers.
- Middle zone: CHIEF'S HOUSE (дом вождя) in the centre with the CHIEF in front of it, 2 thrower spawners, 2 stores, 2 stone towers, 2 brutes, orcs, throwers.
- Front zone, WIDER than the mountains (sticks out past them both sides): 2 orc spawners, 6 stone towers, 2 stores, orcs and throwers along it.
Already decided earlier: strong from the start, sends NO raids, does not grow. User added (2026-10-19): ALL buildings at MAX level, the CHIEF is a MINIBOSS, TWO TOTEMS shield the portal.
Claude proposed: portal shielded while ANY totem stands; chief = own sheet row (~400 hp, 25 dmg, bigger), chief's house = its spawner with a long respawn (120 s) or none; the base brings its own mountain models (NavMesh obstacles, no-build) so it can stand anywhere; placed at the furthest spot as a 6th portal; ~34x30 cells. **User's answers (2026-10-19):** (1) a NEW STONE SET — walls, gate-with-tower, stone tower — in an OVERGROWN, GREEN style (moss, ivy); (2) destroying the great portal = INSTANT WIN, plus an optional FREE MODE to keep playing; (3) the chief RESPAWNS after a while as long as the chief's house stands. Next: Blender pilot (stone wall 4 pieces, stone tower, stone gate-with-tower) for approval, then chief's house, mountains, boss chief; then code.
- **Stone pilot modelled** (`Tools/blender/orcstone1.py`): `OrcStoneWall1-{Straight,Corner,T,Cross}` (Fence1 box, 376–1186 tris after slimming — moss as flat slabs, not balls), `OrcStoneTower1` (2x2 octagonal keep, iron roof, 1237 tris), `OrcStoneGate1` (1x2, piers + arch + tower on a bridge, 1503 tris), ruins for tower and gate. Style: stone_mid coursed blocks on a ROCK plinth, dark cap + merlons, moss blanket on top, moss patches down/up the faces, ivy strands, ferns. First pass was too clean/light — the user asked for OVERGROWN; the moss blanket + face patches fixed it. Scratchpad `stone_compose.py` renders a walled yard of them (rotations: Corner N+E=90, N+W=180, S+W=270, S+E=0; Straight E-W=90; gate in an E-W wall=90). Sent to the user for approval. **User approved the stone set (2026-10-19).**
- **Chief's house, mountains, boss modelled** (`orcmainbase1.py`, `orcchiefboss.py`, committed): `OrcChiefHouse1` (+Ruin) 3x3 on BigWarehouse1's box (1830 tris; stone footing + timber hall + hide roof with a bone ridge, huge horned skull over the door); `OrcMountain1-A` (peak, 3.76 m) and `-B` (shoulder) on Laboratory's 4x4 box, ~360 tris each, plain grey crags (stone_dark/cobble — roof_slate drew tile stripes on slopes) with grass crowns and firs; `OrcChief-Boss` = OrcChief ×1.5 with a gold crown RING (a solid disc hid the face), breastplate with a violet rune, bigger banner (1.72 m incl. banner). Sent for approval; nothing exported.
- **The user's rule (2026-10-19): the MAIN BASE has its OWN UNIQUE STYLE** — «все модели максимального уровня» meant: EVERY building on the main base gets its own stone-and-green model that the small camps never use. So beyond wall/tower/gate/chief's house: main portal (+ruin), totem, storehouse, brute pit, thrower range, and the orcs' spawner (a stone barracks instead of the tent). Style: dressed overgrown stone, TURF (grass) roofs, moss, ivy, orc iron and bone. Do not reuse camp models on the main base.
- **Modelled** (`orcmainbase2.py`, committed): `OrcMainPortal1` (+Ruin, 4x4 Laboratory box, 2642 tris: two octagonal stone keeps + stone lintel + horned skull, rune circle on the dais), `OrcMainTotem1` (obelisk with violet runes), `OrcMainStorehouse1`, `OrcMainBarracks1` (orc spawner), `OrcMainBrutePit1`, `OrcMainThrowerRange1` — all with ruins, 1000–1200 tris. Helper `turf_roof(..., cross=)` (bug paid: without `cross` the roof centred on 0, not on the house). Full main-base set now = these + stone wall/tower/gate + chief's house + mountains A/B + boss. Sent for approval; nothing exported.
Next after OK: export, then the main-base CODE (layout from the sketch, mountains as obstacles, 2 totems, boss row + respawn from the house, instant win + free mode).

**Pending fix DONE (2026-10-19, `NavRoute` target snap):** soldiers sent at a totem walked through palisades on the phone — a structure's centre is inside its carve, CalculatePath failed, straight-line fallback. NavRoute now SamplePositions the target (3 m). Test `SoldiersBreakInRatherThanWalkThroughTheWall` fails without it. Not yet rebuilt into an APK / retested on the phone.

## CHIEF'S TREASURY IN THE GAME (2026-10-19, df06daa6; sheet == repo)
- `OrcCampBuildingKind.Treasury`=11, main base only, 2x2 at (-11,0) in the back zone. Model `Tools/blender/orcmaintreasury1.py` (OrcMainTreasury1 +Ruin): round stone vault, moss dome, gold horned skull, two horned guardians, gold behind an iron railing.
- Broken by the player -> `PayOutTreasury`: `orc_treasury_loot_coins` 400, `_ironbar` 30, `_copperbar` 30 (ResourceManager.Add caps at storage; the feed `#log_treasury_plundered` reports what was actually added). Health `orc_main_treasury_health` 500. Restored broken by a save -> pays nothing.
- Other main-base ideas offered and NOT picked yet: shaman hut (heals), war drum (pulls all guards), chief's forge (+damage), portal altar (portal regen), wall catapults, wolf den (new unit), fighters' arena, decor houses.

## PRISONERS' CAGES IN THE GAME (2026-10-19, 60227796; sheet == repo)
- `OrcCampBuildingKind.Cage`=10, 1x1: one at camp levels 1-2, two at 3, three on the main base (layout cells in OrcBaseLayout). Models `Tools/blender/orccage1.py`: OrcCage1-lvl1..3 (+one ruin), OrcMainCage1 (+Ruin); open tops so the prisoners show from the game camera.
- Broken by the player (announce=true) -> `FreedPrisoner` x `orc_cage_prisoners*` (2/3/4, main 5) walk the NavMesh to the Town Hall; free home -> AddCitizens(1); no room -> they WAIT at the hall (user's rule) with one `#log_prisoners_no_room`. Save: `freedPrisoners`. A cage restored broken by a save frees nobody.
- Orcs hunt prisoners only with no soldier to fight (user's rule): guards inside their zone, raiders within aggro (`OrcUnit.HuntPrisoner`). Prisoner 20 hp, 1.2 m/s (orcs 1.3).
- Tests: EditMode `EveryCampHoldsItsCages`; PlayMode `PrisonerCageTests` (verified by breaking). `StorageAndUpgradeTests.Photograph_TheBuildingLevels` failed once on "nowhere free" -- random-placement flake, passes on rerun.

## MAIN BASE IN THE GAME (2026-10-19, pushed; sheet == repo)
- `OrcBaseLayout.Main()` = level `MainLevel` 4: Bounds RectInt(-19,-23,38,32), PortalSize 4, three mountain rects (back strip + two sides), stone walls as Palisade-kind pieces at level 4, three GateTower gates (z -8, -17, -23), 2 totems, chief's house (`OrcCampBuildingKind.ChiefHouse`=9 → spawns `OrcKind.Boss`=4, row `orc_chief_boss` 400 hp/25 dmg), all other buildings at level 4. `CampLayout` gained Bounds/Mountains/PortalSize/IsMain (square layouts still have HalfExtent). `OrcCamp.IsMain`, `InFrontOfGate(m)`, Centre = cell + PortalSize/2.
- Level-4 pieces draw the MAIN models (`OrcRaidManager.MainModelOf`, mainWallModels, mountainModels…); stats from 13 new `orc_main_*`/`main_portal_health`/`orc_chief_*` keys; boss respawn `orc_chief_respawn_seconds`=120 while the house stands (+`#log_chief_returned`).
- Placement: the main base is placed FIRST (before the camps) — `TryFindMainBaseSpot` scans radii 88→30 m step 3, 36 bearings; the camps fit round it. Placing it last failed ~1 in 4 on Map3. No room → warning, old win rule.
- `OrcPortal.IsMain` (4x4, one look, never a raid source — `RaidSources()`), `MainPortalCell`; closing it = instant victory (`GameOverManager.WonByMainPortal`), victory panel has «Продолжить игру» (`ContinuePlaying`, `IsFreeMode`).
- Save: `PortalEntry.main`, `hasMainPortal`/`mainPortalCell` (closed main → main ruin).
- Tests: EditMode `TheMainBaseIsBuildableClosedAndAsDrawn`; PlayMode `MainBaseTests` (Map3: stands whole with chief, both totems must fall + no raids from it, chief respawn, win). Camp tests filter `!IsMain`. EditMode 445, PlayMode 197 green. Photos `playtest_shots/orc-main-base*.png`.
- Not on a phone; FPS of the big base (≈130 wall pieces + ~50 rock blocks) unmeasured.

## Walled bases IN THE GAME (2026-10-16, `5673dc8e` code, `d1c40403` assets; sheet == repo)
- **`OrcBaseLayout`** (pure): plans for levels 1-3 in the camp frame (portal centre = origin on a cell corner, +z = back, gate at -z towards the town), quarter-turn `ToWorld`, `LevelFor(score, rank, l2, l3, perRank, max)`. Half extents 6/7/11 (lvl3 = 22x22 cells). Spawners: WarTent→Raider, BrutePit→Brute, ThrowerRange→Thrower.
- **`OrcCamp`**: reserves the MAX-level square at portal opening (clears boulders/trees via new `RockSpawner/TreesAreaSpawner.ClearCell`; only reserved cells are built on), `Build(level)`, `Teardown`, `GrowTo` (rebuild whole + `#log_camp_grew`), palisade → ONE NavMeshObstacle per straight run (rebuilt when a piece falls), per-kind spawn timers (`orc_garrison_respawn_seconds`=30; a timer once wound runs its full length), guard zone = camp square + 1.5 m, `RaidExit` in front of the outer gate.
- **`OrcCampBuildingKind`** appended: Palisade 3, Gate 4, GateTower 5, Storehouse 6, BrutePit 7, ThrowerRange 8; buildings have Level; tower/gate-tower shoot. **`OrcKind.Brute`=3** (row `orc_brute`). Raiders still drawn as brute from raid level 4.
- **Guards** walk via `NavRoute` with `straightLineFallback:false` (never through walls). **Soldiers** whose route is `Blocked` attack the nearest orc wall within 2.5 m (`SoldierUnit._breach`).
- **Growth**: `OrcRaidManager.GrowCamps(score)` at each raid tick; rank = opening order (nearest first). **Portal closed → `PlunderCamp`**: coins/wood/stone ×level into stores, raid timer += `orc_camp_breather_seconds`, `#log_camp_plundered`; camp `Cleared`, stops growing.
- **Portal search** requires the whole camp square free+solid (fallback max level 3→2→1), `PortalMaxRadiusDeltaMeters` 48, `portal_distance_from_town_hall_meters` 36. Map3 opens all 5; Map1 only ~3 (PortalOpeningTests moved to Map3; SaveRoundTrip TH near the middle).
- **Save**: `orcCamps` (level, maxLevel, rank, quarters, cleared) + only DAMAGED/fallen buildings in `orcCampBuildings`; restore rebuilds from plan then applies health (0 = `FallQuietly`). Restore skips the ground raycast (colliders not ready at load) — water only. Old saves get fresh level-1 camps.
- 27 economy keys + `orc_brute` + 2 localization rows, all in the sheet (verify_sheet: no differences).
- Tests: EditMode 444 (new `OrcBaseLayoutTests`), PlayMode 192 + 2 graphics-only; photos `playtest_shots/orc-camp-level1..3.png`.
- **Not done / watch**: soldier wall-breaching has NO test; ~136 palisade renderers per lvl3 camp (no batching) — watch draws on phones; raid roster per portal level = later (user); main base = later; nothing on a phone.

Related: base-assault design (bases, active vs passive portals) in [[project_game_design_backlog]]; the modelling standard in [[project_blender_modeling]].
