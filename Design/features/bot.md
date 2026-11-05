# Bot kingdom (Бот)
Status: BUILT up to WAR (2026-11-04; ransom-by-razing + harder later wars + 20 waves 2026-11-05): castle, garrison, Diplomacy Hall, orc raids, rebuild, and the war (guards, sea raids from the botwaves tab, ransom). Not seen in game.

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
  - **Orc side (2026-11-03, user: build orc bases in it too):**
    - A «Сторона» switch, Королевство / Орки, stored in the blueprint as `side: "orcs"`, swaps the palette.
    - Orc ids are `Orc<OrcCampBuildingKind>`, with the footprints of `Combat/OrcBaseLayout`: GateTower and Gate 2x1, ChiefHouse 3x3, Shipyard 3x4. `OrcPortal` is 2x2 and `OrcGreatPortal` 4x4.
    - Guard posts are 1x1 marks: `OrcPostRaider/Brute/Thrower/Boss`. Mountains are `rock` terrain.
    - The orc example is `Design/orc_main_base.json` from `Design/orc_main_base_blueprint.py`, a port of OrcBaseLayout.Main.
    - Its frame: x+19, y = 8 - top cell, so the gate is at the bottom.
    - Nothing in the game reads orc blueprints yet.
  - **Joined walls (2026-11-03, user):** 1x1 walls (Fence, OrcPalisade) are drawn as a thick line joined to their neighbours as in the game (the fence: fences, towers, gates by connectsToFences, FenceShape; the palisade: palisade and gates only, not the watchtowers, OrcCamp.IsWallCell). A 2x2 block is a closed square, then L, T and +. Two passes, all outlines then all fills, so there is no seam.

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
- **Bot colour (2026-11-03, user: the player must see the buildings are not theirs):** every bot building draws through `Models/Palette/BotPalette.mat`. It is a copy of the buildings' palette with all roof cells, the gold (level 5 roofs are gilded) and the banners in neutral straw #D9BC74 / #A6874A (turquoise first, the user swapped it the same day).
  - `Tools/palette/bot_palette.py` writes `BotPalette.png` from `BuildingsPalette.png`.
  - The material is made in `BuildSeasonLooks`.
  - `BotKingdom.Dress` swaps the renderers (hidden level models too) right after `InitializeAsBot`.
  - The colour is a picture, not balance, so it is not in the sheet.
  - **The bot's King's cape (user, 2026-11-03):** it is red on the player's King. On the bot's King it is in the bot's straw.
    - `BotKingPalette.png` is written by the same script (cells banner_red and roof_red_dark only).
    - `BotKingPalette.mat` is a copy of Figures.mat, so it has no snow.
    - `BotGarrison.Raise` calls `BotKingdom.DressKing`.

## Traps
- A colour added to `kit.PALETTE` that is a roof must also go into `bot_palette.py` CELLS, then rerun the script. Wall-walk steps (WallWalk) are instantiated later and keep the town's palette.
- A new bot island must keep a pad like BOT_PAD, or the castle is cut by the coast.
- The harbour is found by the nearest water along the axes from the hall.
- The blueprint's buildings must exist in the placer's catalogue (hotbar + mandatory). A Hidden building is skipped.
- Performance: about 550 buildings + about 29 soldiers + raid orcs far from the camera. Not measured on a phone.

## Built 2026-11-04, part 3: the war (not seen in game, test green)
User decisions:
- attack = **landing and shelling together**: transports land soldiers, war ships shell soldiers and buildings;
- strength **grows over time** — the waves get bigger;
- ransom in **coins**; a town without enough coins can pay in **crystals**;
- the garrison **defends the castle**: it does not go raiding;
- the waves live in a **sheet tab `botwaves`** (user): wave number, unit, count, level. Transports = ceil(units / transport capacity), counted automatically. Past the last row, the last wave repeats.

How it works:
- The war's units are hostile OrcKinds `BotSoldier` / `BotArcher` (IsMonster; stats `mon_bot_*`; the player's soldier models in the bot's straw via `BotKingdom.DressAsBot`).
- `Bot/BotWar.cs`:
  - `Begin`: the hireling garrison stands down (`BotGarrison.StandDown`), and hostile guards stand on `BotGarrison.WarPosts`. They respawn while the hall stands.
  - `RaidOnTheClock`: first raid at `bot_war_raid_first_sec`, then one every `bot_war_raid_interval_sec`, both counted on `BotRelations.WarSeconds` (saved as `botWarSeconds`).
  - `RaidNow(i)` reads `config.BotWave(i+1)` into a manifest, then calls `ShipManager.LaunchBotFleet`. Ships are `ShipKind.BotTransport` / `BotWar`, with the player's models in straw. `Land` dequeues the manifest.
  - `End` removes the BotSide units and the garrison stands back up.
- Ransom: `BotRelations.RansomCoins(hallLevel)` = `bot_war_contribution_base` + `per_min` × whole minutes, less `hall_discount` per Diplomacy Hall level above 1. Crystals = coins / `bot_war_coins_per_crystal`, rounded up.
  - Shown in `DiplomacyWindow` only at war: the PayCoins / PayCrystals buttons.
- At war, the bot's towers shoot the player's soldiers. The player's soldiers hit the bot's buildings through `Bot/BotWarTarget.cs`, which is alive only at war; its Reach is half the footprint.
- The bot's buildings take no blessing or shield.
- Sheet: economy `bot_war_*` keys + `mon_bot_*`; tab `botwaves` gid 1680852949 (BalanceImporter `ReadBotWaves`, `BalanceSheetSettings.botWavesCsvUrl`, `verify_sheet.py`).
- Test: `ArchipelagoTests.TheBotKingdom_...` covers war guards, no hirelings, a wave with a transport, the ransom, and peace clearing the units.

## Built 2026-11-05, part 4: razing pays, later wars start harder (not seen, untested)
User decisions:
- The army razing the bot's buildings **lowers the ransom**: we reward it. Each building razed by the town's army in this war (`BotWarTarget.TakeDamage` → `BotRelations.NoteRazed`) takes `bot_war_contribution_per_building` (5) coins off. The ransom never drops below `bot_war_contribution_min` (50). Orc raids on the bot do not count.
- **Each new war starts at a later wave**: war n starts at wave 1 + (n-1) × `bot_war_start_wave_step` (2), so the 2nd war starts at wave 3. `BotRelations.WarsFought` counts wars on declaration.
- Saved fields: `botWarRazed` and `botWarsFought`.
- The `botwaves` tab now has **20 waves**: from 4+2 at level 2 up to 30+15 at level 5. Past wave 20, wave 20 repeats.

## Open (user, 2026-11-05)
- **Widen the barracks' unit list for the player and for the bot** (new unit types). To discuss later; the botwaves tab then gets the new `unit` ids.
- The Diplomacy window does not show how many buildings were razed; only the price drops.

## Traps (war)
- The fleet sails for the player's **Town Hall** (`TryFindOrcLanding`). With no hall there is no raid, silently.
- BotSide units and the orcs **do not fight each other** (both are the hostile side); the bot's towers skip BotSide.
- The war's manifest (`Aboard`) is not saved. A transport loaded from a save lands soldiers of `bot_war_level`.

## Next
- Trade window, gifts, the reward for razing the hall (later, per the user).
- Playtest the war: raid size against the town, the ransom price.
