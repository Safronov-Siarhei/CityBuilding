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

## Next (skeleton)
- Place the bot castle from the blueprint JSON on the bot island. The bot zone is now half 14 and must grow to fit 37x44 with the harbour on the coast.
  - Bot buildings: BuildingInstance with IsBot. Skip them in the town's sums (counts, storage, housing, houses, TownHall, fog reveal, happiness, research, save, OnlyOne, progression score), and give them a foreign card on tap.
- Rebuild after an orc raid while the bot's hall stands.
- The garrison: King, ranged on the walls, melee patrols; respawn while the hall stands.
- Orc raids on the bot: massive, for show (user 2026-11-03); what they destroy is rebuilt after the raid.
