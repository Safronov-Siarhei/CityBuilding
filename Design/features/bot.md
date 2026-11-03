# Bot kingdom (Бот)
Status: CONCEPT (2026-11-03). Nothing is built.

The user's decisions are marked "user"; the rest are my proposals.

## Where it lives
- On the archipelago's mandatory **bot island**. Only the archipelago is in the map picker.
- It starts **already developed** (user): the player should not attack at once, and should see what to aim for.

## The castle
- v2 scheme: `Design/bot_castle_v2.png`, drawn by `Design/bot_castle_v2.py`; 1 square = 1 cell, real footprints.
- Based on the user's v1 sketch: concentric, 35x35 cells, plus a harbour to the south.
- **Centre:**
  - Town Hall at max level (5x5), with a ring road and a cross of roads;
  - 4 Fortified Towers at max level;
  - an inner fort 13x13 with walls and 4 Barbicans at max level;
  - a moat 2 cells wide with 4 Drawbridges at max level.
- **Streets:**
  - a ring of Squares (2x2) round the moat;
  - 4 avenues: a max-level road with Squares on both sides, from the drawbridge to the outer gate. The axis cell is the road, because the town's width is odd.
- **Blocks:**
  - 32 slots of 3x3: 27 Manors, 4 Barracks (by the S/E/W gates), 1 Church.
  - Barracks and Manors are both 3x3, so they swap one for one.
- **Corner zones 6x6:**
  - NW: the bot's **Diplomacy Hall at max level** (user). I drew it 4x4; that is my proposal.
  - NE: 2 healers.
  - SW, SE: a barracks each.
  - The ring corners hold decor blocks: tree, bush, flag, well.
- **Outer wall** at level 4, with towers every 5-6 cells and a level-4 Barbican in the middle of each side.
- **Harbour (south):**
  - a road from the south barbican to the **Shipyard** (3x4) on the shore, for raids (user);
  - a lighthouse;
  - 2 **Water Towers** in the shallows (a future building, see below).

## The garrison (user)
- The bot's **King** walks the town. I drew him walking the ring of squares.
- **Ranged soldiers stand on the walls** (both rings, all level 3+, so the walls are walkable).
- **Melee soldiers walk the town**. I drew them patrolling the avenues.
- Units **respawn while the Town Hall stands**. With no Town Hall the bot restores nothing and its units do not respawn (user).

## Relations (user)
- A new player building: the **Diplomacy Hall (Зал дипломатии)**. No relations with the bot until it is built.
- A **relations scale** (user: yes, a scale). I proposed -100..+100.
- Three states: **Мир / Торговля / Война** (Peace / Trade / War).
- **Deals are made in a window** for now (user), not by ship.
- Hall levels give better trade deals and a smaller war contribution (user).
- **At war the bot attacks** (user) with its barracks and shipyard.
- **A war ends** in one of two ways (user):
  1. the player pays the bot a contribution;
  2. the player razes the bot's Town Hall.
- At good relations the bot sometimes sends **gifts** (user).

Proposed, not agreed:
- **State thresholds:** Trade from about +30; the bot declares war below about -50; gifts above about +60.
- **The scale moves:**
  - up with trade, gifts to the bot, and help against orcs near its island;
  - down with troops at its shore and attacks on it;
  - drifting back to 0 over time.
- **Diplomacy Hall levels:**
  - lvl1 opens Peace and War;
  - lvl2 opens Trade;
  - every level after that gives a better rate, a higher gift chance and a smaller contribution.

## Open
- The Diplomacy Hall's size and model.
- The trade window: what is traded and the rates.
- The contribution formula.
- How often the bot raids at war, and the size of a raid.
- What the player gains from razing the Town Hall: loot? the island?
- The orcs raiding the bot.
- Performance: the bot's citizens are NOT simulated, only a few walkers. The garrison needs a cap.

## Related
- The Water Tower: placed in the shallows, shoots ships and soldiers at sea (user 2026-11-03, to add).

## Built (2026-11-03)
- **Diplomacy Hall.**
  - Tools/blender/diplomacyhall1.py, levels 1-5. 4x4, City category, one a town, opened in the Lab.
  - Sheet rows: buildings 57, sandbox 57, `#building_diplomacyhall`.
- **Relations skeleton:** `Scripts/Diplomacy/BotRelations.cs`.
  - The scale and the stance are saved (`botRelationsSaved`).
  - The scale drifts to 0, ticked from GameCalendar. Reset on a new game: GameCalendar.Awake.
  - Economy keys: `bot_relations_start`, `bot_trade_min_score`, `bot_war_declare_score_loss`, `bot_relations_drift_per_day`.
- **The window:** `UI/DiplomacyWindow.cs`, opened by the Hall's «!» (BuildingFocus.OpenDetails); built in SetupProjectPanels.BuildDiplomacyWindow.
  - Shows the scale, the stance, and the Мир/Торговля/Война buttons; a refused one says why.
  - «Здание» opens the Hall's ordinary card.
- `BuildingInstance.IsBot` is reserved; nothing sets it yet.
- **The base editor:** `Tools/base_editor/base_editor.html`, published as artifact https://claude.ai/artifact/AuMAkdzSMwuHrQxof6i8Sg.
  - Its building catalogue is the game's real footprints.
  - Export format `citybuilder-base/1`: `{name, width, height, buildings:[{id,x,y,w,h,rot,level}], terrain:{"x,y":kind}}`; x right, y down, north up.
  - Example: `Design/bot_castle_v2.json`, made by `Design/bot_castle_v2_blueprint.py`.
  - To refresh the catalogue, regenerate it from ScriptableObjects/Buildings footprints and buildings.csv names.

## Built 2026-11-03, part 2: the castle, the garrison, the raids (not seen in game)
- **The bot islands.** islands.py `BOT_PAD` (-20..20, -27..20): a flat, wood-free pad. Its -y side sits 19-23 m from the coast; a beach is put there. Scale 120 -> 160.
  - Isl-bot1/2 regenerated and exported.
  - ArchipelagoGenerator: `BotHalf` 20, island size 160.
  - `MeshMapApplier.ArchipelagoBotCell`.
- **`Bot/BotKingdom.cs`.** Reads `Assets/_Project/Bot/BotCastle.json` (copied from `Design/bot_castle_v2.json`; the base editor's format). The anchor is the Town Hall's middle cell.
  - The castle is turned so the blueprint's south (the harbour) faces the nearest water along 4 axes (`HarbourTurns`).
  - Each piece goes through `BuildingInstance.InitializeAsBot`. A land piece never stands on water. WaterTower is skipped: it is not in the game.
  - Rebuild: after `bot_rebuild_calm_sec` with no orc within `bot_calm_radius_cells` of the hall, while the hall stands.
  - Hall razed: `HallRazed`, `BotRelations.EndWar`, no more rebuilds or respawns.
  - Saved: `botCastleSaved`, `botHallRazed`, `botMissing` slot indices.
  - Raids: `RaidNow` / public `Raid(count, level)`. Keys `bot_raid_first_sec` 300, `bot_raid_interval_sec` 600, `bot_raid_size` 30 (0 = off), `bot_raid_level` 3. The orcs come up 21 cells out on the north, east or west side (SpawnOrcs) and go for the nearest building.
- **`Bot/BotGarrison.cs`** (added by BotKingdom). Hireling-style soldiers (`ArmyManager.SpawnHireling` into groups of their own): not the army, not selectable, no upkeep. Stats come from `bot_garrison_level`.
  - The King (SoldierType.King, a plain soldier and not a Hero, so no fog reveal) walks the ring of squares.
  - `bot_garrison_patrols` x `bot_garrison_patrol_size` ManAtArms patrol the avenues.
  - `bot_garrison_archers` Archers hold walk-wall cells (GoToWall).
  - A fallen soldier respawns at the hall after `bot_garrison_respawn_sec`. The garrison is not saved: it is manned fresh on load.
- **IsBot filters:** BuildingPlacer (island check, OnlyOne), FireManager, CheatsPanelEconomy, CitizenVisualsManager, SicknessManager, FreedPrisoner, OrcRaidManagerPlacement, PlayerProgression, GameEvent, HappinessManager, InvasionHealing, ResearchManager, GameSaveController (save and lookup), ShipManager (shipyard).
  - GameOverManager ignores a bot Town Hall.
  - No event-log line for bot losses.
  - OrcUnit and WallWalk deliberately see bot buildings.
- **UI:** a tap on a bot building opens `ForeignBuildingCard.Show(BuildingInstance)` (`#foreign_owner_bot`). An army order onto a bot building walks there.
- **Test:** ArchipelagoTests.TheBotKingdom_StandsOnItsIsland_OutsideTheTownsSums_AndRebuilds checks placement over 500, none of it counted, the garrison over 20 with archers on the walls and none in the army, a raid at the castle, and the rebuild.

## Traps
- A new bot island must keep a pad like BOT_PAD, or the castle is cut by the coast.
- The harbour is found by the nearest water along the axes from the hall.
- The blueprint's buildings must exist in the placer's catalogue (hotbar + mandatory). A Hidden building is skipped.
- Performance: about 550 buildings + about 29 soldiers + raid orcs far from the camera. Not measured on a phone.

## Next
- War: the bot attacks (shipyard, barracks); its towers and garrison fight the player's units at war.
- Trade window, contribution, gifts, the reward for razing the hall (all later, per the user).
