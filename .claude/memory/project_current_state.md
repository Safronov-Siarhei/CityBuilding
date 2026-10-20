---
name: project-current-state
description: "Where CityBuilding stands (latest: 2026-10-20 handoff -- Map 4, orc bases; older: 2026-10-14 late — orc faction models, portal states, orc camps; day — move-order flag, soft building look, autosave, HUD after the user's sketch; morning: event feed, fog; older: 2026-10-13, 2026-09-01) — what shipped, what is waiting on the user, the threads to pick up next. Read this first after a break."
metadata: 
  node_type: memory
  type: project
  originSessionId: e11f94ca-15a8-4229-b2a6-7681d9d8dbf8
  modified: 2026-10-16T17:20:26.906Z
---

**2026-10-20 (latest) -- HANDOFF, the user moves to another device.** master `4a8216e4`, pushed;
working tree clean but for untracked local tools (Tools/research/*.ps1 build/test scripts,
send_build_to_telegram.py, Tools/balance/compare_sheets.py, make_localization.py) that are NOT in
git -- the user was told; on a new machine they are missing unless copied. The Blender .blend files
(D:/blender projects/Buildings3D/*.blend, incl. Map-4-TwoRivers.blend) are also outside git.
Tests: EditMode 446, PlayMode 210 (+4 graphics-only). Sheet == repo (verify_sheet: no differences).
What is in the game now:
- NEW GAMES START ON MAP 4 «Долина двух рек» ([[project_maps_roadmap]]): island, 2 rivers, fords,
  bridges that really open paths, main base in the cirque, 4 camps planned with levels (all 3),
  Town Hall only in the gold start zone and >= 45 m from the orcs.
- Orcs ([[project_orc_faction]]): walled camps L1-3, the main base (stone set, mountains, boss chief,
  instant win + free mode), prisoners' cages (freed townsfolk walk home), the chief's treasury.
NOT on a phone: no APK since the main base (user: «APK пока не надо»); last APK 49.7 MB vs
Telegram's 50 MB cap, and maps/models were added since.
Next threads the user named: more designed maps one at a time (variants Кальдера, Перешеек,
Фьорды, Великое ущелье); Vikings by sea in free mode; dungeons in ruins (later); main-base extras
offered but not picked (shaman, war drum, forge, portal altar...); raid roster per portal level.

Updated **2026-09-01**. `master` is `70ec9ff3`; tests **EditMode 403, PlayMode 114**, all green;
the working tree is clean and the Google Sheet is provably identical to the repo CSVs.

The sections below are newest-first — each one says what shipped that day and links to the file
carrying the numbers. Older entries are kept because they explain WHY a system looks the way it
does, which the code cannot say.

**Waiting on the user, all three genuinely open:**

- **Whether to raise the camera's zoom floor from 5 m to 8** (asked 2026-08-30; a photograph of
  the 5 m view — a wall filling the frame — was sent and not answered).
- **Whether to take the GPU instancing lever for trees and rocks**, which is 1 004 of the
  remaining 1 266 draws (raised 2026-09-01; the trade-off is written out in
  [[project_performance_baseline]]).
- **What closing a portal should BUY the player**, now that there are five (raised 2026-09-01).
  Today it buys one fewer direction a wave can come from and nothing else: the raid clock is one
  clock, and it does not slow down as portals fall. Farthest Frontier pays twelve months of peace
  for clearing a raider camp, which is what gives its army a job between raids
  ([[reference_competitor_analyses]]). Deliberately NOT taken unilaterally — it retunes pacing.

**The sheet needs nothing.** `python .claude/skills/balance-sheet/scripts/verify_sheet.py` reported
**no differences on any of the five tabs** on 2026-09-01 — the first time that has been true and
provable. Every older note in this file about pending pastes or drifted rows is settled: the two
localization keys are in, and the `#recipe_batches` space was fixed on the code side.

Read alongside [[project_balance_audit]] (what the measured numbers say is broken, and the proposed order of repair), [[project_balance_pipeline]] (how numbers get into the game), [[project_localization]] (how text does), [[project_game_design_backlog]] (the design decisions and the authoritative building roster), and [[feedback_batchmode_workflow]] (how to build and test).

## 2026-10-16: walled orc bases, 3 levels, IN THE GAME — `master` = `d1c40403` (pushed)

Models for every building/warrior level (Blender, approved), then the code: camps that reserve their max square, grow with the town and get rebuilt, real palisade walls, per-kind spawners, plunder + breather on closing a portal. Tests EditMode 444, PlayMode 192 green; sheet == repo. Everything is in [[project_orc_faction]] → «Walled bases IN THE GAME». Nothing on a phone. Next from the user: raid roster per portal level, the main (great-portal) base, maybe levels 4-5.
- **APK sent to Telegram 2026-10-16 20:18** (`61535219` + assets): bases + new icon. **SIZE TRAP: 49,7 MB against the bot's 50 MB cap** — the first build was 51,1 MB and was NOT sent; fixed with `ModelImporterMeshCompression.Low` on Buildings/Terrain/Orcs (meshes were 62% of user assets, 63,7 MB raw). Any new models will push it over again: next levers are dropping the unused maps (Map1/Map2) or Android texture compression; ARM64-only already.
- **App icon** (placeholder, user's pick «в4»): the village with the windmill, `Assets/_Project/UI/AppIcon.png`, set by SetupProject `ConfigureMobileLandscape` via `PlayerSettings.SetIcons(NamedBuildTarget.Unknown, …)`. Rendered by a scratchpad script (`render_icons_app.py`, 5 concepts: castle, castle vs portal, portal at dusk, village, brute portrait) — not in the repo. A final icon is still open (no adaptive layers).

## 2026-10-15 (night, 2nd task): orc thrower + chief, fence T/Cross, icons — `master` = `31f5595c` (pushed)

- **Orcs:** `OrcKind` {Raider, Thrower, Chief} (saved as `OrcEntry.kind`, old saves = Raider). Rows `orc_thrower` (14 hp, 4 dmg/1.6 s, range 6) and `orc_chief` (60 hp, 8 dmg/1.3 s). Composition by `OrcRaidManager.KindInGroup`: from `raid_thrower_from_level`=2 every `raid_thrower_every`=3rd orc of a WAVE or GARRISON throws; from `raid_chief_from_level`=3 the first orc of each WAVE is the chief (garrisons have none). `SpawnOrcs` (cheat, tests) still spawns plain raiders; only `SpawnWave` (real raids) and `SpawnGuard` mix kinds. No projectile visual (the archer has none either). Numbers are first guesses.
- **Fence:** `FenceShape.Resolve` returns T (model open to the WEST at step 0) and Cross; `ResolveWithoutJunctions` is the old 2-piece rule, used when a T/Cross model is missing. Photo `playtest_shots/fence-junction.png` looks right.
- **Icons:** `render_icons.py` used to rebuild the material WITHOUT the detail atlas (flat icons); now it applies `detail.build_material_nodes` + `add_layers` for resource props. All 61 icons re-rendered into `Assets/_Project/UI/Icons` (it also renders 6 Orc*.png that the game does not use — not copied).
- Tests: EditMode 429, PlayMode 188 + 2 graphics-only, all green. Sheet: units rows 7–8, economy rows 78–80; verify_sheet: no differences. Trap: the sheet's ttk_* formulas count WHOLE hits (ceil(hp/dmg)×interval), not hp/dps.
- Still open from item 4 of the menu: balancing the camp numbers. Nothing on a phone.

## 2026-10-15 (late night): levels 2–5 IN THE GAME — `master` = `f80abe62` (pushed)

- 176 level FBX exported (`<Id>1-lvl2..5`), wired by `SetupProject.AddLevelModels`. `BuildingInstance.MaxLevel = 5` (units stay at `UnitBalance.MaxLevel = 3`). `BuildingData.UpgradeCostTo(level)` + `upgradeToLevel4Cost/5Cost`.
- The user chose "playable 4–5 with extrapolated numbers": 32 new columns in buildings.csv AND in the sheet (BL..CQ, after `service_radius_3`), made by `Tools/balance/extend_levels_4_5.py` (rules in its docstring: HP/defense/happiness/storage ×1.44 then ×1.39; workers/batches/housing +the 2→3 step; radii ×1.2; up4/up5 = up3×1.75/×1.75²; research coins ×1.7, 180 s / 240 s). verify_sheet: no differences. Nobody has playtested this balance — the user may want to retune.
- Researching level N needs Laboratory level N (`ResearchTopic.RequiredLabLevel`), so 4–5 are late-game by construction.
- The silver/gold level disc now shows ONLY over buildings with no model for their level (fence, water buildings).
- Tests: EditMode 420 green; PlayMode 188 = 185 + the fixed level test (fixture re-run 15/15) + 2 graphics-only. Photo `playtest_shots/building-levels.png` (Hovel/Cottage/Sawmill/Farm × 5 levels) looks right.
- Not on a phone. Watch: every placed building now carries 5 model children (4 inactive), and ~18 MB of level FBX are referenced by prefabs, so they all load.

## 2026-10-15 (night): level-5 MODELS too (golden roofs; Food red-orange) — `master` = `f4ca458d`

Levels 2–5 exist as models for all 44 buildings; the game supports 1–3. Code for 4–5 is the user's "later". Details in [[project-blender-modeling]] → «Level 5».

## 2026-10-15 (evening): level-4 MODELS for every building (the game still stops at level 3)

`master` was `fadaee5d`. `<Id>1-lvl4` exists for all 44 in the Models.blend: glazed diamond roofs + gilded crests over level 3. The user wants the code side (MaxLevel 4, `_4` sheet columns, research, wiring) LATER — not started. Nothing exported. Details in [[project-blender-modeling]] → «Level 4».

## 2026-10-15 (later): level-3 models for every building (committed+pushed, NOT exported)

`master` = `fd527fbe`. All 44 buildings have `<Id>1-lvl3` in the Models.blend; lvl2 scripts committed too (`710f9c81`). Nothing exported to Unity, no rebuild, no tests run (Blender-only work). Next on the user's word: `export_fbx.py`, rebuild, phone check. Details in [[project-blender-modeling]] → «Level 3».

## 2026-10-15: level-2 models for every building (Blender only, NOT exported)

All 44 buildings got a `<Id>1-lvl2` model in `Buildings-Size1-Models.blend` (45 with Road1-lvl2). Scripts are 44 new untracked `Tools/blender/<id>1_lvl2.py` + new helpers in `details.py` (quoins, dormer, fountain, weathervane) — the user asked for "models only": nothing exported to Unity, nothing committed yet, so [[feedback_auto_push]] has not run. Next steps waiting on the user's word: commit+push the scripts, then `export_fbx.py` (the `-lvl2` FBXes are picked up by SetupProject.AddLevelModels automatically), rebuild, check on a phone. Details, the concept and the skipped items in [[project-blender-modeling]] → «Level 2».

## Late evening (2026-10-14): the orc faction, modelled and in the game

`master` = `b61cc29f` (pushed). Tests: EditMode 420; PlayMode 184 passed + 2 graphics-only photo tests (they pass when run with graphics). Sheet == repo: verify_sheet reports no differences. Nothing of this is on a phone; no APK was built. All of the orc work is in [[project_orc_faction]].
- **Models** (Blender, `Tools/blender/orcs.py` + `orc*.py`):
  - 3 units: grunt, thrower, brute; plus a chief.
  - Portals: one model per state (active, passive, closed), and the great 4x4 portal.
  - Buildings: watchtower, war tent, totem, a 4-piece palisade and a gate, each with its ruin.
- **In the game:**
  - The grunt is the raider, and from level 4 the brute.
  - The portal the next wave comes from burns bright; the others smoulder.
  - A closed portal leaves its ruin.
  - Every portal has a camp: a watchtower that shoots soldiers, a war tent whose garrison guards the camp, and a totem that shields the portal until it falls.
- **Next candidates:**
  - Unit types for the thrower and the chief.
  - The great-portal final fortress, and what happens after the last portal closes. Still open: plain victory vs continue vs next map.
  - Balancing the camp numbers, which are first guesses.
- **Discussed, not built:**
  - The user feels the sandbox lacks goals. A campaign of scenario levels with stars was proposed, with the tiers brought back as milestones; nothing is decided yet.
  - Monetization ideas: [[project_monetization_ideas]].
  - The user asked whether a trailer is possible. Answer: yes, as Blender camera flythroughs with whole-object animation; there are no rigs, so no walk cycles. Not started.

## Evening (2026-10-14): four HUD looks

`master` = `e57c356c`; EditMode 420, PlayMode 178, HudPhotoTests 26 green; sheet == repo. Citizens under menu + tax in words (c5b380a8); round-2 HUD tagged `hud-sketch-v1`; four looks (Classic/Soft/Glass/Dock) switchable in settings (e57c356c). Next: paid looks via IAP (user's idea), skinning the windows. Handoff 2026-10-14: user may continue from ANOTHER Claude account — mockup canvas https://claude.ai/artifact/DLLxzFesTdktVVR6uSUwD6 belongs to this account (needs Share); IAP open question: Android only or iOS too; real purchase tests need a Google Play merchant account. See [[project_ui_skin]] rounds 3–4. Nothing of this is on a phone.

## Latest (2026-10-14, day session)

`master` = `98e41ac8` (pushed); tests EditMode 420, PlayMode 172 (+ HudPhotoTests 20 with graphics), all green. Sheet == repo (rows `#save_autosave`, `#hud_day` added and verified). One APK built mid-session (`D:\Builds CB\CityBuilding.apk`, sent to Telegram) — it predates the soft look, autosave and the new HUD; nothing since is on a phone.

Shipped today, newest last:
- **Move-order marker** (`5bdf144b`): ripple + Blender gold flag (`Tools/blender/orderflag.py`) + dot trail along the agent's own NavMesh route (`MoveOrderMarker`, `CitizenAgent.Route/RouteVersion`); gather = ring, refused = red ripple. **Parchment fog is the default** (key bumped to `settings.fog_style.v2`).
- **Tapped event icon stays until its timer** (`62135046`).
- **Soft building look** (`34f10e3f`): detail atlas over the palette + smooth roofs + glass/curtain windows + grass tufts; user chose "variant 3" after REJECTING a desaturated palette. Details in [[project_blender_modeling]].
- **Build menu closed at rest**, tabs on the bottom edge (`bf232a5a`).
- **Autosave** (`686cc7ee`): slot `_autosave`, every 240 s real time + OnApplicationPause/Quit; ~10 ms per save in editor for an 18 KB town. Rules in the GameSaveController doc comment.
- **HUD after the user's sketch** (`98e41ac8`): see [[project_ui_skin]] round 2. Pictures only on buttons; tiers hidden; menu pauses; placement swaps the build menu for ✕⟳✓.

Open / next:
- The user said more HUD sketches are coming; also "пока не меняем вкладку жителей" (the citizens panel content stays as is for now).
- Not on a phone yet: soft look (watch fragment cost of the detail shader), new HUD, autosave hitch (if visible, move the file write off the main thread).
- Building icons in menus were rendered from the OLD models — rerender with `render_icons.py` when convenient.
- Cottage is 1617 tris (housing budget 500–1000), Laboratory +850, Castle +730 after the soft details; Manor still pokes ~1.3 cm out of its plot.
- Save dialog and settings card still carry text (only HUD buttons became pictures).
- `Tools/research/build_android.ps1` now copies the APK into `D:\Builds CB` (removing only old APKs) and waits with `WaitForExit`.

## Earlier (2026-10-14, morning session)

`master` = `3dc0f9db` (pushed); tests EditMode 416, PlayMode 146 (+13 with graphics: FogTests, SettingsPanelTests, HudPhotoTests), all green. Sheet == repo (verify_sheet: no differences). Nothing built or sent to a phone this session — the user has not asked for a build.
- **Event log panel replaced by an event feed** (`08d4ea19`): round temporary icons at the left edge, rim drains, tap = camera glides to the source (`RTSCameraController.FocusOn`). Player's own actions are log-only. Details in [[project_ui_skin]].
- **Fog of war redrawn** — clouds (`4c829b0a`) and a parchment map with an ink chart of the land (`3dc0f9db`), switched in the MAIN MENU settings (`GameSettings.FogStyle`). Exploration rules deliberately unchanged per the user. Details in [[project_fog_clouds]].
- Not yet seen on a phone: event feed, clouds, parchment. Watch transparent overdraw over large fog when zoomed out.
- Open small questions put to the user, unanswered: should tapping an event icon also open the building card; is the settings highlight (selected = light) read the right way round; the old tier-up text toast still shows alongside its icon.
- Unity batchmode once crashed at startup (CtrlRoutine during mono init, exit -1073741819) — environmental, a rerun was fine.

## Latest (2026-10-13, evening session)

`master` = `8dfa81b5`; tests EditMode 403, PlayMode 133 (+ Map3Tests/HudPhotoTests with graphics 14), all green.
- **Builds sent**: APK (16:45, release, Telegram) and Windows builds; the latest Windows build (Map 3) is in `D:\Builds CB` and Telegram. New rule: builds go to `D:\Builds CB`, previous one deleted first ([[feedback_build_folder]]). Command-line Windows build added (`BuildWindows.cs`, `Tools/research/build_windows.ps1`).
- **HUD adapted to screen shapes** (`22e318be`): CanvasScaler Expand + HUD pinned to corners; resource numbers dark; tax +/- visible; event log fits content. Known overlaps left for the user's sketch: Migration panel over the Rotate button; a 12-card hotbar reaches the minimap on 16:9. [[project_ui_skin]]
- **The user will SKETCH the HUD layout per scene** ("позже кину") — implement their sketch when it arrives, don't redesign alone.
- **Map 3 «Летняя долина»** is in the game and the ONLY map for new games (`620dceba`); the user is testing it now. [[project_map3]]
- Open question to the user: should the APK also go to `D:\Builds CB` (one folder, each build wipes all) or per-platform subfolders?

## Since then (2026-10-12/13): the Blender building set, and smaller people

The work moved to modelling the buildings in Blender. Everything about it is in
[[project_blender_modeling]]. One change reached the GAME itself:
- `966b54e7`: citizens, soldiers and orcs are drawn at `UnitSize.Scale` = 0.625, so a citizen is
  0.45 m and fits the new 0.5 m doors.
- `CitizenSelector.MarkerHeight` went from 1.15 to 0.75.
- Tests passed after the change: EditMode 403, PlayMode 114.
- Nobody has seen the change on a phone yet.

**2026-10-13 (later): Map2 (Claude's Blender map) added, stones in the game, and the UI reskinned to style D** — see [[project_ui_skin]]; master `66683dd4`, EditMode 403 / PlayMode 124 green.

**2026-10-13: the whole Blender set is in the game** (`1d7533b3`): every building, tree, fence piece and road is an authored FBX on one palette material; tests green. Details in [[project_blender_modeling]].

`master` was `3a114a3c` before that. The working tree carried the many modified Materials and Prefabs
that were already there before 2026-10-12. They are NOT Claude's and were never committed by it.

## Five portals, from a template (2026-09-01, `70ec9ff3`)

The user chose portals — item 1 of the audit's order of repair — and asked for **a template** with
**all five at a distance from the Town Hall**, the same anchoring rule the single portal had.

`PortalPlacement` is that template: (distance, bearing) slots generated as a spiral — each portal
on its own evenly-spread bearing, each one `portal_distance_step_meters` further out than the last.
`portal_count` (5), `portal_distance_from_town_hall_meters` (20) and the step (8) are economy keys,
so spreading them over the whole map is a number, not a patch. Hand-authored per-map positions,
when they exist, supply their own slots and nothing downstream changes.

**Raids were deliberately NOT multiplied.** One wave per interval, out of a portal picked at random
from those still standing. Five independent clocks would land five squads on day one, when the
player has no coins and no soldiers (see the audit's first-raid finding). So five portals multiply
the work of finishing the map, not the pressure on the way there.

**Two defects only the PlayMode tests could find** (both are why `PortalOpeningTests` exists):
the ground search swept every bearing before nudging the radius, so it preferred moving a portal
180° around the settlement to moving it four metres — two portals opened 4° apart. Bearing is now
the outer loop, capped at a third of each portal's share of the circle (which is what makes
neighbours provably unable to meet), and the DISTANCE gives way instead, walking ±32 m. Separately,
the inward walk ran to the absolute floor and put a portal 8 m from the Town Hall, inside the walls.

**A test-fixture trap worth remembering:** `PlaytestWorld.FindFreeArea` scans from cell (1,1), so
every test that used it built in the map's CORNER — where half the compass is off the map and no
rule can spread five portals. That cost a run to diagnose. `FindFreeAreaNearTheMiddle` is the
answer, and is what any test about what happens AROUND a building should use.

Saving turned three single-portal fields into a list keyed by cell, and an attack order records
WHICH portal it pointed at; old saves still load through the legacy fields. Both restore paths were
verified by breaking them ([[feedback_verify_green_by_breaking]]).

## Claude edits the Google Sheet directly now (2026-09-01)

The user connected the Claude in Chrome extension and said to stop handing over paste blocks:
*"добавляй новые строки сам в таблицу"*. **The whole workflow now lives in the repo as a project
skill, `.claude/skills/balance-sheet/`** -- document id, tab gids, the traps (Cyrillic cannot be
typed and fails silently, the column-order mismatch, the unsaved-changes dialog) and
`scripts/verify_sheet.py`, which diffs every published tab against the repo CSVs. Read the skill
rather than rediscovering any of it.

Two things that belong here rather than in the skill: the user has **two Chrome browsers**
connected and theirs is named **DepigoGames**, and browser work is token-expensive (a screenshot
is ~1 400 tokens), so the skill's "verify by CSV diff, not by screenshot" rule is about cost as
well as correctness.

## Shipped 2026-08-31 (late): minting

The user picked minting out of the [[project_balance_audit]] list, over the portals the audit rated
first. The Плавильня knows a fourth recipe -- 1 GoldBar + 1 Coal -> 4 Coins -- which closes the
gold dead end and the coin drought in one row. Full reasoning, the arithmetic behind the rate and
what was deliberately left undone are in [[project_balance_audit]]. `master` is `151da70e`, tests
**EditMode 382, PlayMode 109**.

## Shipped 2026-08-31: the draw census, and buildings baked into one mesh

`master` HEAD is `2704bdb9`. Tests **EditMode 380, PlayMode 107**, green. Numbers and the full
reasoning are in [[project_performance_baseline]].

- **The GPU side is measured now, not guessed.** `RenderCostTests` counts the live scene's submesh
  draws, which is a number `-nographics` cannot invalidate. It corrected two things the 2026-08-29
  audit had wrong: **a building was 16 draws, not "4-5 parts"**, and **a tree is one renderer, not
  two**. Buildings, not trees, were the biggest single bill on the map.
- **`SetupProject.CombineParts`** bakes a procedurally assembled building's cube parts into one
  mesh, one submesh per material, saved as its own asset under `Models/Generated`. A building went
  from 16,3 draws to 4,5 and from 16,3 renderers to 1; the whole scene at 60 citizens went 1 664 ->
  1 266 draws and the shadow pass 521 -> 140.
- **`BuildingDrawCountTests`** holds the rule (a placed building draws through at most one
  renderer) and was verified by FAILING: it caught the fence, ten cube posts per segment, which the
  first pass had excluded wholesale because FenceAppearance switches its two variants at runtime.
  Each variant is baked on its own now.
- **`androidUseSwappy` is 1**, set from SetupProject. Four of the six test phones are 90/120 Hz and
  the game is capped at 60; without frame pacing that reads as judder at a healthy frame time.
- **The working tree is CLEAN**, for the first time since 2026-08-19. The four "foreign" files the
  older notes told you to ask about were nothing of the kind -- `ProjectSettings.asset` was
  SetupProject's own output (company name, orientation, identifier, batching), the
  `PerformanceTestRun*.json` are the Performance Testing package's run output, and the other two
  were Unity re-serialisation. All committed (`a94d26ba`, `2704bdb9`).

**The next lever is GPU instancing for trees and rocks (1 004 of the remaining 1 266 draws), and
it was deliberately NOT taken** -- the minimap camera is rendered by hand so instanced draws would
miss it, instanced draws bypass frustum culling, and nothing has been profiled on a device yet.
The trade-off is written out in [[project_performance_baseline]] and is the user's to call.

## Shipped 2026-08-30: the first rendering work (`65b69697`, `2006a7a9`)

`master` HEAD is `4d30273f`. Tests **EditMode 380, PlayMode 105**, green. Full detail and the
numbers are in [[project_performance_baseline]]; the device fleet and the tier proposal this came
out of are in [[project_mobile_device_fleet]].

- **The scenery is out of the shadow pass** -- ground, water, trees, rocks, citizens and the fog
  of war. Buildings still cast; everything still receives. `SceneryShadows.DisableCasting` is the
  single place the rule is written.
- **The fog of war was casting shadows** -- 100 opaque chunks over ground the player cannot see.
  Found by the guard test, not by reading the code. Nobody knew that system was there.
- **Camera zoom is 5 / 15 / 25** (min / opening / max), down from 8 / 25 / 220.
- **The minimap** lost its shadows and refreshes at 0,4 s instead of 0,15.
- `SceneryShadowTests` (5 tests) was verified by breaking it, and the break exposed TWO
  over-broad checks of my own -- the map, the fog and the spawners all hang off one GameObject,
  so an unfiltered `GetComponentsInChildren` sweep catches each other's objects. Scope every such
  sweep by layer or by name.
- **The `Unity.RenderPipelines.Universal.Runtime` reference was added to the Editor asmdef** so
  SetupProject can reach `GetUniversalAdditionalCameraData()`. The runtime asmdef still does not
  reference URP.

**Waiting on the user (2026-08-30):** whether to raise the zoom floor from 5 m to 8. At 5 the
camera is under the rooflines and the view is a wall -- photographed and sent, no answer yet.

**Nothing has been measured on a phone yet.** Every figure behind this work is a static count.
The profiling run on the Pixel/A71 is still the most valuable next step, and the big remaining
lever is GPU instancing for the ~1 200 tree renderers.

## The game today, in one paragraph

49 placeable buildings across seven categories, 22 drawn from the user's own FBX. A settlement with storage ceilings, decay, upgrades, taxes, coins, a six-factor happiness stat that decides who moves in and who walks out, fires that start where the player let a district rot, illness that starts where they let it go hungry or dry, orc raids that scale with how far the player has actually got, an army of four soldier tiers gated behind the Laboratory and equipped in smelted metal, a tech tree gating almost everything, and a real economy: buildings convert resources into other resources, the settlement eats, food is genuinely scarce, and starvation can lose the map. Saving covers all of it — town, army, hunger, raid, orders. All text and all balance live in a five-tab Google Sheet.

## Shipped 2026-08-19

### The save stopped cancelling the player's orders (`1a8a0ae5`)

The last three gaps, all of them silent: the group the player had SELECTED (selection is not cosmetic — while a group is selected a tap on the world is an order to it and the other selectors stand down, so a load handed the player back a different input mode), the in-flight ATTACK order, and `RaidsSuspended`.

- An attack order is saved as a KIND (`ArmyAttackTargetKind`: None/Portal/Orc) plus, for an orc, its INDEX in the save's own orc list. A position would not do — the orc is walking, and both are written at the same instant.
- That forces an ordering: `RestoreArmy` and `RestoreRaids` now each return what they built, and a third pass (`RestoreArmyOrders`) pairs them up. The order cannot go back before the portal and the orcs exist to point at.
- Selection is an INDEX, not the group's `Id`: `ArmyGroup` hands ids out of a static counter that a scene reload resets.
- **`ArmyGroup.RestoreAttackOrder` exists instead of replaying `OrderAttack`** because `OrderAttack` moves the rally point to wherever the target stands NOW, which would quietly overwrite the rally point the save had just restored.

### Raids measure the player, not the calendar (`a786c525`)

`PlayerProgression.Score()` — the backlog's composite, all five terms: building levels summed (so upgrading counts, not just sprawling), population, soldiers times their researched level, the defence STAT summed (not a count of walls), and everything the settlement has ever produced.

- **The production term is a LIFETIME total** (`ResourceManager.LifetimeProduced`), and that is the whole point. A stockpile rewards spending down to nothing before a raid; a production RATE has the same hole one level up, since a player watching the clock could pull every worker off their job for a minute. Only a monotonic total cannot be gamed.
- `ResourceManager.AddProduced` sits beside `Add`. The three paths that genuinely make something call it (a workshop's output, a gatherer coming home, the player's own tap on a tree); refunds, taxes and the cheats deliberately do not. It counts what FITTED, not what was offered.
- It is in the save, or a reload would have been the cheapest defence in the game.
- **Raids grow in three directions**, because a size cap alone means a player who out-builds it is never threatened again: more raiders (`raid_progress_per_extra_raider`), then stronger ones (`raid_progress_per_orc_level`, capped by `raid_max_orc_level`), and the interval falls from `raid_interval_seconds` to `raid_min_interval_seconds` by `raid_progress_at_min_interval`.
- `raid_days_per_extra_raider` is GONE. `OrcRaidManager` no longer has a `gameCalendar` reference at all, and SetupProject no longer wires one.
- Rough scale at today's weights: a fresh town scores ~20 (2 orcs, level 1, every 88 s); a developed one ~800 (8 orcs, level 5, every 45 s).

### Fires and illness (`3fdf38bc`, `06d84c6d`)

The Колодец, the Дом лекаря and the Пожарная бригада stopped being decoration. One new PER-LEVEL `service_radius` column carries all three, and it had to reach the usual three places -- sheet, `BalanceImporter.ReadLevels`, `SetupProject.CopyLevels`.

- **Fire chance IS decay** (the user's call). A maintained town never catches fire at all, which turns repair from an errand into fire insurance. Spread ignores the neighbour's decay, so gaps between districts are worth leaving.
- An unattended fire burns `fire_burn_seconds` and stops, having eaten a chunk of the building -- a town with no brigade loses part of a building, not every building that ever catches. Each firefighter in reach subtracts fixed seconds, floored: the research/scientists shape, recomputed from the CURRENT crew, so manning the station mid-fire shortens it.
- Fire damage goes through `BuildingInstance.TryDamage`, the orcs' own door, so a Town Hall lost to fire is a defeat.
- **Illness is a headcount on `CitizenManager`** (`SickPopulation`), and `IdlePopulation` now measures the HEALTHY -- taking to bed empties a workplace exactly the way dying does. That stall is the whole cost of an epidemic, days before anybody is buried.
- The untreated streak counts days the BEDS WERE NOT CLEARED, not days without a healer standing somewhere: one Дом лекаря must not make a town of forty immune.
- Plague dead go into the same window starvation's dead do, so `FoodConsumptionManager` has become the settlement's death register (`RecordDeaths`). Its save field keeps the name `recentStarvationDeaths` on purpose -- renaming it would drop the value out of every save already written.
- **Fires are diced for and illness is worked out, deliberately.** A fire is an event, and one a player could predict to the day is not one they would hurry to prevent; an epidemic is a condition, and one whose cause they can read is one they can fix.
- Saved: sick count, untreated streak, and every fire with how far into its burn it is. ELAPSED rather than remaining -- how long a fire has left depends on the crew that can reach it after the load, not before the save.

### Three soldier tiers (`04dee57b`)

Spearman (reach 2 m), Archer (shoots from 8 m), Man-at-arms (45 health, slow). Rows in the units tab plus an entry in `SoldierType` and `SheetIdOf` — stats, levels, research, cost and upkeep all come from the sheet.

- **Where the kit comes from was the user's call (2026-08-19), against the design's earlier "one workshop per equipment type"**: the roster of 49 has no armoury in it, and the Плавильня already made bars nothing much wanted. So a recruit is equipped straight at the Barracks, in `recruit_ironbar` / `recruit_copperbar`. Militia stays coins-only, which is exactly why it is the tier a settlement can raise before it has an industry.
- **The kit is FLAT across levels**: a level is training, and the level tax is paid in coins, where `recruit_coins` already rises.
- **`OrcUnit.NotifyAttackedBy` is what stops the archer being a free win.** An archer shoots from 8 m and an orc looks 6, so without it a line of archers takes a raid apart untouched. Leashed at 2.5x the orc's aggro radius because `FightSoldier` closes in a straight line rather than over the NavMesh.
- The Barracks card grew four tier buttons in the band the Плавильня's metal buttons use (y 160 — the recipe block is hidden for a Barracks, so nothing overlaps). **A locked tier stays tappable on purpose**: a greyed-out row that says nothing is how a player never finds out what the Laboratory is for.
- `ResearchGateTests` lost the test asserting the soldiers tab is empty on a level-1 Laboratory. That state can no longer occur — three unlocks are level-1 topics and the list shows completed topics too — so the test asserted an impossible thing. **`#research_empty_lab_level` is now effectively unreachable**; the code is kept as a safety net and says so.

## The first playtest (2026-08-19, one screenshot from the user)

Day 7 of a real game, sent because **no wood was being gathered at all**. Standing: Ратуша, Лачуга, Лесопилка, Каменоломня. Wood 0, stone 75, food 100, coins 2, population 6/12, contentment 68%.

**The diagnosis, and it was not a bug: there were no trees within reach.** Rocks are scattered over the WHOLE map (140 of them, ~17 m apart); trees exist only inside the map's authored `TreesArea` zone (600 of them, all in one place). The town was built on open rocky ground, so the Quarry's 20 m radius found a boulder and the Sawmill's 12 m radius found nothing. The arithmetic confirms it exactly: 50 starting wood - 20 Sawmill - 20 Quarry - 10 Лачуга = 0, so the Sawmill had produced NOTHING in seven days.

**What the game did wrong, and it is worth fixing before anything else is added:**

- `#log_nothing_in_radius` fires ONCE per building and latches (`CitizenVisualsManager.ReportRadiusEmpty`). It would have appeared on day 1 and scrolled out of an eight-line log days ago. After that a starved gatherer looks identical to a working one -- the workers still stand on the plot. **The building's own card says nothing about it at all.**
- **Illness empties workplaces silently.** The log showed "Заболели: 1" twice and "Умерли от болезни: 3", and behind that `CitizenManager.LayOffWorkersBeyondWorkforce` had quietly unassigned those people from their jobs. Nothing said so, and recovered or replaced citizens are deliberately never put back -- so even a Sawmill standing in a forest can go quiet for a reason the player cannot see.

A fix for both was offered and not yet answered: show "nothing in reach" on the building card for as long as it is true, and log it when illness costs a workplace its last worker.

**Also confirmed working by eye, for the first time:** the settlement tier name (Хутор), the day counter, the tax +/- panel, the contentment breakdown (Налог 90, Ветхость 95, Оборона 100, Еда 50, Потери 70, Развлечения 0), the migration countdown, the event log, and **the whole of this session's hazard work end to end** -- fires started, a Лачуга burnt and was destroyed, people fell ill from having no well, and three died untreated. The illness numbers are clearly harsh: two people fell ill on consecutive days and by day 7 three were dead.

## Traps this session added to the pile

- **A comma inside an unquoted CSV cell splits the row.** A localization row read `#log_raid_levelled,Орки начали набег ({0}, уровень {1}),...` and the English column silently became the back half of the Russian sentence. The placeholder test ran off a HAND-WRITTEN key list the new row was not on, so it saw nothing. `LocalizationTests.NoTranslationAnywhereLosesOrGainsAPlaceholder` now sweeps every row in the sheet; `LocalizationConfig.Entries` was added for it.
- **A batchmode run that fails to compile takes TEN MINUTES to exit**, not the usual one. If a run seems hung, read `setup_run.log` / `test_run.log` for `error CS` before assuming a lock.
- **Half this repo is CRLF and half is LF**, and a patch script matching on `\n` silently finds nothing in a CRLF file. Detect the file's own line endings before matching. `OrcUnit.cs` and `SoldierUnit.cs` are CRLF; `SaveData.cs`, `GameSaveController.cs`, `ArmyGroup.cs` are LF.
- **`Tools/research/retest_fresh.ps1` is new**: three launches (throwaway compile pass, then EditMode, then PlayMode). `retest.ps1` reads the FIRST EditMode pass, which reports on code that no longer exists — fine when only PlayMode changed, wrong whenever an EditMode test was edited.
- **`ForestClickTargetingTests` is FLAKY.** Two of its tests failed once and passed on an identical re-run: the forest is scattered by an unseeded `Random`, so the geometry under test differs run to run. A background task was filed for it. The observed failure was an angled ray missing a ground mesh that a vertical ray at the same x/z hits.

## Touch controls rebuilt (2026-08-20)

The user called the phone controls "very bad and unplayable" and was right: four systems each
polled the pointer and each acted on `press.wasPressedThisFrame`, the frame the finger LANDS.
The agreed schema and every gesture/outcome pair now lives in **`Design/touch_controls.md`** --
read that rather than re-deriving it, and keep it updated when the behaviour changes.

Three commits, in order: `1fefb460` (the rebuild), `613fdfe2` (the aim point stops moving),
`27fcaa7e` (the aim point is the building's CENTRE, not its corner -- found by the user from a
screenshot: a 5x5 Town Hall grew up and to the right of the crosshair). Tests **EditMode 370,
PlayMode 85**, green. Each set of new tests was verified by breaking the code under it.

**The placement flow as it now stands, since it is easy to get wrong again:** the ghost is pinned
to a FIXED screen point (centre X, 58% up) and never moves -- the player drags the WORLD under it.
Confirm is the button or a tap on any cell the ghost covers; a tap anywhere else does nothing. On
a mouse the old hover-and-click flow is untouched (`UsesTouchAiming` picks between them), because
a mouse has a hover position before it clicks and a finger does not.

- **Nothing about the new input is covered by tests except the fence-line axis snap** (6 EditMode
  tests, verified by breaking `BuildingPlacer.SnapStraightEnd`). Gesture recognition, the
  world-anchored pan and the centroid zoom have never been touched by a real finger -- batchmode
  cannot reach them at all. The nine-point checklist in `Tools/research/build_notes.txt` is the
  only verification that exists.
- **Demolition now exists** (there was none at all before). `demolish_refund_fraction` = 0,5 is a
  first guess and is NOT balanced: at that share, demolishing a building before a raid beats
  losing it. Either lower it or block demolition during a raid.
- **The sheet IS in sync** -- the user pasted both blocks on 2026-08-20 and a cache-busted
  download confirmed it: localization 236 rows on both sides with all 14 new keys matching on ru
  and en and no placeholder damage, economy 64 rows with `demolish_refund_fraction` = 0,5, and no
  neighbouring row shifted by either insert.
- `ShowWhileSelectingBuilding` was deleted; `PlacementHudController` replaced it.

## Levels 2 and 3 became visible (2026-08-21, `d4812570`)

Everything about levels 2/3 already worked EXCEPT the picture: all 49 buildings carry per-level
health, workers, storage, defence, happiness and radius in the sheet, `TryUpgrade` applies them,
research gates them -- and `BuildingLevelAppearance` was written, called from three places, and
attached to nothing, because SetupProject only ever looked for `<id>1-lvl1.fbx`. **Do not repeat
the earlier note that upgrading "only bumps a number": the stats were never the gap.**

- `SetupProject.AddLevelModels` now picks up `<id>1-lvl2.fbx` / `-lvl3.fbx` by name. **The
  footprint stays level 1's** (a wider upgrade would have to fit into ground already built over --
  an upgrade failing on geometry the moment it is paid for); the HEIGHT follows the level and
  carries the click box with it via `BuildingLevelAppearance.ResizeClickBox`.
- **A silver/gold disc floats over every upgraded building**, nothing at level 1, none on roads.
  98 level models do not exist and probably will not soon, so this is the whole feedback loop for
  an upgrade. Its height comes from the click box, so it sits over the roof of a hovel and a
  castle alike -- unlike the decay marker (fixed 3 m) and the health bar (3.9 m), which sink into
  anything taller. Photographed: `playtest_shots/building-levels.png`.
- **`Destroy` is deferred to the end of the frame**, so a marker built from a primitive spends its
  first frame catching taps meant for the building underneath -- and that frame is the one where
  the player's finger is on the building they just upgraded. Both markers now disable the collider
  before destroying it. A PlayMode test caught this; the decay marker had shipped with it.
- `WorldIndicator` marks the disc, the decay diamond and the health bar as world-space HUD so
  tests measuring what a building LOOKS like stop measuring them.
- Still not done from the same list: the card never says what an upgrade BUYS ("Рабочих 3 -> 4"),
  the fence's three levels are still an undecided design question, and no upgrade anywhere has
  been balanced -- `per_day`/`payback_days` in the sheet describe level 1 only.

## The upgrade economy, retuned (2026-08-21, `6be8f9c1`)

**`batches_per_tick_2/_3` was empty on all 49 rows**, so every producer made exactly as much PER
WORKER at level 3 as at level 1 and an upgrade was only ever extra worker slots -- a dearer way to
hire somebody. A second Farm: 20 timber for +60 grain/day. Upgrading one: 32 timber, 15 coins and a
minute of Laboratory time for +20. Sprawl won five to one across all eleven producers, and the four
mines gained NOTHING (flat workers, flat batches, no harvest radius).

- **The agreed rule (the user chose it 2026-08-21): sprawl stays cheaper in RESOURCES, upgrading is
  cheaper in PEOPLE.** People are the scarce thing -- migration is gated on contentment, not timber.
- Workshops: batches x1,5 at level 2, x2 at level 3. **The grain chain stays 1:1:1 at every level.**
- **A base of 1 batch cannot express x1,5 in whole numbers.** The mines and the Smelter therefore
  have their TICK doubled (6 -> 12 s) and their batch count with it: level 1 output is unchanged and
  the counter gains room. Reach for this trick again rather than inventing fractional batches.
- **`batches_per_tick` is never read for a gatherer** -- `ProductionBuilding.Update` returns early
  for wood and stone, their income is the walk. Sawmill and Quarry got workers 3/4/5 instead.
- Nothing at level 1 moved, so the opening of a game is untouched.
- `UpgradeBalanceTests` pins the rule (per-worker growth, gatherers gain hands or reach, service
  radius grows, and the farm stays dearer than sprawl in resources / cheaper in people). Verified by
  running against the old numbers: two of the four fail there, two were already satisfied.
- **The Рыбацкая хижина is the one steep row** (x6 by level 3, since a base of 1 batch and growing
  workers multiply): a level-3 hut feeds 60 mouths with four fishermen, the cheapest food in the
  game. Left alone deliberately until the meat/fruit chains are designed.
- **The sheet was AHEAD of the repo on nothing, and I nearly broke that.** `service_radius_2/_3`
  IS authored (Well 14/18/24, Healer 16/20/26, Brigade 18/24/30) -- a display script of mine blanked
  those columns and I read the blanks as empty cells, then "fixed" them with invented numbers.
  Reverted before committing. **Print the raw cell when claiming a cell is empty.**
- Handed to the user as two paste blocks: `K2:M50` and `AA2:AD50` on the buildings tab. **Not whole
  rows** -- `AQ:AT` (total_cost, per_day, payback_days, id_check) and the up2/up3 columns are the
  sheet's own formulas and a full-row paste would flatten them.

## Every building is generated now (2026-08-21, `59661bbb`)

**The user's own FBX were stand-ins** -- 45 of 48 were rough blockouts -- and they asked for all of
them to be replaced by procedural geometry, from scratch, not built on top of theirs: *"все здания с
нуля, не надо добавлять к моим!"*, water buildings excluded.

- **The FBX moved to `Assets/_Project/Models/Buildings/Stubs/`, they were NOT deleted.** The build
  only scans the folder root, so moving one file back is all it takes to override the generator
  again. Keep that contract intact -- it is the reason the whole thing was acceptable to do.
- **Footprints were MEASURED from the stubs before the move and are now written down** in
  `CreateBuildingData`'s call sites. The user's one hard requirement was that sizes not move
  ("сейчас здание 2х2, таким и должно остаться"). Do not "tidy" those numbers.
- A fourth shell, `BuildingStyle.Plot`, for what has no walls (well, flag, garden, gate, arena).
  The old flat shell was a fixed 1x1 road tile and drew a 1x1 slab for a 3x3 square.
- **`SetupProjectMotifs.cs`** holds 30 motifs -- sails, pit-head frame, spire, dome, silo bins, barn
  doors, strongbox, hose tower, portico, arena, well head, fountain, drying racks, pig pen, tavern
  sign, archway, keep. Everything is sized from the footprint it is handed. Extending the roster is
  one enum value plus one method, not a new generator.
- The fence is procedural too; `CreateFenceModel` draws the straight run along Z and the corner
  joining south+east, which is what `FenceShape` requires.

**The trap that cost a whole rebuild: `CreateLitMaterial` deletes the asset at that path before
writing a new one.** Asking for the same NAME twice therefore destroyed the material the first
caller was already using, and Unity draws that renderer bright magenta with no error, no warning and
every test green. The entire fence shipped pink. Fixed twice over -- the fence makes its material
once, and CreateLitMaterial now returns the same material for a repeated name. Guarded by
`AuthoredModelTests.NoBuildingDrawsWithAMissingMaterial`, which walks every catalogue prefab
including inactive children (verified: it fails on `Fence/PostCentre` against the broken prefabs).

- **Wall fittings must hang below about 0.45 of the building's height.** The hut shell's wall runs
  from 0.1h to roughly 0.52h; the healer's cross, the hay hatch and the tavern sign were pinned at
  0.6-0.9h and floated over the roof.
- **A motif must stay inside its footprint.** The theatre's portico stood a metre out in the
  neighbour's plot before it was pulled inside the front wall.
- Catalogue photographs are `playtest_shots/catalogue-1..4.png`, twelve buildings per shot, in
  hotbar order -- that order is how to tell which building is which in a shot.
- **A wooden cross floating over the barracks in catalogue-2 is the Водяная мельница's wheel**, not
  a bug: it is one of the three water buildings deliberately left alone, standing behind.
- Renderer count per building went from ~1 to 20-40. Nothing has measured the cost on a phone yet;
  combining each prefab's parts into one mesh per material is the obvious next step if it bites.


## The silent failures, answered (2026-08-23, `9113a178`)

The user picked this off the open-threads list. Three things that look EXACTLY like a working
building, two of them from the lost seven-day playtest and one found while fixing them.

- **A gatherer with an empty radius says so on its card AND on its workforce row**, for as long as
  it is true. `ResourceNode.AnyHarvestableWithin` is deliberately a SOFTER question than the one a
  worker's node search asks: a claimed boulder and a still-growing sapling both count, because
  neither is fixed by moving the building. Asked fresh on each redraw (`ProductionBuilding.NothingLeftInReach`)
  rather than mirroring `CitizenVisualsManager`'s latch -- that latch only moves while somebody is
  employed there, so an unstaffed building would report whatever was true when its last worker left.
- **Only the LAST worker lost earns a log line** (`#log_workplace_emptied`, in
  `CitizenManager.LayOffWorkersBeyondWorkforce`). Short-handed is still working; a line per hand
  would bury the one that matters.
- **A destroyed building now hands its workers back**, which it never did. `ProductionBuilding.OnDestroy`
  only unregistered from the visuals manager, so a farm lost to orcs, fire, neglect or the demolish
  button left its crew counted as employed at a building that no longer existed -- the settlement
  permanently lost hireable citizens, silently. `Demolish`'s own comment had claimed workers unwound
  through OnDestroy "exactly as they do when orcs burn the place down"; storage and housing did,
  workers never had. **Read that comment as a claim, not as documentation.**
- Two new localization keys handed over as a file for `A238`/`A239` of the localization tab (the
  sheet's row order is NOT the CSV's -- `#log_nothing_in_radius` sits at 50 in the repo and 237 in
  the sheet, so append rather than matching positions).

**Traps this session added:**

- **EditMode never calls `OnEnable`** on a plain MonoBehaviour, so every self-registering registry
  (`ResourceNode.All`, and anything built like it) is EMPTY there. Six tests written in EditMode
  passed for the wrong reason -- the four expecting "nothing in reach" saw an empty registry. They
  now live in `Tests/PlayMode/GatherRadiusReachTests.cs` and work off a point 5 km from the map so
  the scene's own 740 nodes cannot answer for them.
- **`ProductionBuilding.SetAssignedWorkers` is the SAVE's door and is asymmetric**: it ADDS its
  whole count to `CitizenManager._assignedPopulation` and subtracts nothing on the way down. A test
  fixture built on it leaves the settlement counting workers who do not exist. Use
  `TryAssignWorker`/`TryUnassignWorker` in tests.
- **`EventLogManager` keeps 8 lines and is never cleared between tests.** Both "is the line there"
  and "how many are there" answer about an EARLIER test's building; one line drops off the bottom
  exactly as the new one arrives. Assert on `Entries[0]` with no frame allowed to pass.
- `ForestClickTargetingTests` flaked again (2 of 100) and passed on the next identical run --
  still the unseeded forest, still filed.

## Open threads, most concrete first

**Threads 5, 6 and 8 below are now MEASURED rather than suspected** -- the numbers, and what they
imply, live in [[project_balance_audit]]. Read that before re-deriving any of them.

1. ~~**Two silent failures cost the user a seven-day game**~~ FIXED 2026-08-23, see the last section — see "The first playtest" above. A gatherer with nothing in reach, and a workplace emptied by illness, both look exactly like a working building. A fix was offered and not yet answered.
2. **Nothing about fires or illness has been BALANCED.** The numbers are first-pass: a fully decayed building has a 0,3 chance a day, illness runs 0,01 + 0,15 hungry + 0,12 dry, and a healer treats 2 a day. Nobody has played a game with them.
3. **The user owes design for the food half of the chains**: Фруктовый сад (фрукты), Свиноферма (мясо), Коптильня (мясо→копчёности), whether Рыбацкая хижина switches to рыба and generic `Food` retires. Asked 2026-08-17, still unanswered.
4. **Waiting on the user's models**: `FortifiedFence1-lvl1-Corner.fbx`, and the first `<Id>1-lvl2.fbx` — the wiring for it is written as of 2026-08-21 but has never met a real file, so when one lands check the build's footprint warning and the click box rather than assuming.
5. **Balance beyond food is still the biggest data hole**: producers pay for themselves in 0,1–0,3 of a day, and the army cannot be kept (20 soldiers cost 20 coins a day against 10 coins of tax from 20 citizens). The new tiers make this worse, not better — a man-at-arms is 4 coins a day. Offered four times now; the user has chosen other work each time.
6. **Gathering income was never retuned** after the walk became the income: Sawmill ~60 wood/day, Quarry ~24 stone/day, and the map holds 2800 stone forever.
7. **Most of this session has still not been seen by a human** (the playtest above covered the HUD, the hazards and the economy, but none of the panels). Still unverified by eye: the workforce panel, the Плавильня's metal selector, the Settings screen, the green harvest-radius carpet, the chopping bar after its fix, and now the four tier buttons on the Barracks and the three new soldier silhouettes. PlayMode screenshots CANNOT cover any of it — they capture an off-screen camera, which does not render a Screen Space Overlay canvas. **A single screenshot from the user is worth more than three sessions of green tests; ask for one.**
8. **Departures are nearly unreachable in a real game** — the Town Hall's defence of 20 pins the per-capita defence factor at 100, so a fresh town sits at 48–67 contentment. The fix is the FACTOR WEIGHTS in `HappinessManager`, not the migration curve.
9. **The harvest radius has no in-world feedback while a gatherer is WORKING** — only while placing or when selected. This is no longer theoretical: it is half of what cost the user the game above.
10. **`#unit_militia_flavour` is a dead key** — nothing in the code reads it. No flavour keys were added for the three new tiers.

## Traps a cold session will fall into

- **A batchmode run must happen TWICE after any .cs edit** — the first compiles the new assembly and executes the old one. Warm, `SetupProject.Run` is about 60–100 s and each test suite 15–120 s. **If the edit was to the Editor assembly** (`BalanceImporter`, `SetupProject`), the SetupProject pass itself has to run twice — use `Tools/research/rebuild_twice_and_test.ps1`.
- **`while (Get-Process Unity)` is wrong** — it matches the user's Editor on their other project (`D:\Fork\AC`) and hangs forever. Filter on the command line containing `CityBuilding`.
- **The shell returns before Unity exits.** Check the results XML's `start-time` is fresh; never trust a stale `test_results.xml`.
- **SetupProject rewrites ~200 asset files every run.** Commit the source and balance changes first, then the regenerated assets as their own commit — that is what keeps a diff readable.
- **Iron and Gold are ORE**, not metal, and live in the Склад; the bars live in the Сокровищница.
- **Five buildings still do nothing at all**, down from eight: Коптильня, Свиноферма and Фруктовый сад wait on resources that do not exist; Водяная мельница and Пристань are deliberately empty until the Водные are designed.
- **Trees and rocks are not scattered the same way, and it decides where a town can live.** 140 boulders go over the WHOLE map (`RockSpawner`), ~17 m apart; 600 trees go only inside the map's authored `TreesArea` zone (`TreesAreaSpawner`), all in one place. So a Quarry can be built almost anywhere and a Sawmill only in one part of the map -- which is why their radii differ so much (20 m against 12) and why "no wood is being gathered" is far more often geography than a bug.
- **A building's footprint comes from its model**, not from the number in SetupProject.
