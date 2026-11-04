# Dungeon (подземелье)

> 2026-11-04 (later): LEVEL-2 MONSTERS + both bosses IN THE GAME (dwarf king, canal serpent; 8 models
> from monsters2.py) -- see «Level-2 monsters IN THE GAME». User not seen them.
> 2026-11-04: level 2 playtest round 1 fixed (bridges, knee-deep wading, levers + door, buoy,
> road map for long routes, fog cheat) -- see «Dungeon level 2». User not seen it yet.
> Status 2026-11-01: IN THE GAME: the walk, 3 entrances, monsters + bosses, crystals and patrols,
> the darkness, the GNOME HIRE CAMP and the POTION SHOP (both 2026-11-01, user not seen them yet).
> Not done: dungeons of levels 2-3 (own maps). Not measured on a phone.
> Read with: maps-and-terrain (map spots), sandbox-run-systems (heroes), orc-faction (OrcUnit).
> **2026-11-03: FOUR level-1 dungeons** (the user: «разные генерации для подземелья уровня 1 ... как с
> островами»). `dungeon_plan.py <png> <seed> <layout>`: hall LAYOUTS A-D (camp always south by the way out;
> bosses, lake, arenas elsewhere; A = the approved plan, reproduced exactly). Plans `dungeon1b/c/d-plan.json`
> = layout B seed 31, C seed 8, D seed 5 (seeds picked so each boss is 650-830 m from the camp, 8 crystal
> rooms; A is ~700). `dungeon1.py -- <blend> variant=b` builds `Dungeon-1B` (blends next to Dungeon-1.blend),
> exported to Models/Dungeon1B..D, maps `Dungeon1B..D` (SetupProjectCityScene loops `DungeonTravel.LevelOneIds`).
> A mouth's dungeon: `DungeonTravel.LevelOneFor(seed, version, k)` -- a v3 map's three mouths lead to three
> different variants, the first by the seed; a v2 map (old save) keeps Dungeon1 (bosses slain / crystals
> taken are kept per dungeon id). Tests: PlayMode `DungeonVariantTests` (each variant loads, both bosses
> reachable along a winding way, no two with bosses in the same halls; mouths differ per map). The
> variants' looks were not reviewed by the user; `DungeonTests` still run on Dungeon1 only.


## What the player sees
On the map: an abandoned mine in a small grassy mountain. The player's HERO walks into it, and the
dungeon opens as a SEPARATE SCENE.

## Rules the user decided
- The entrance is a mountain with a cave, not ruins. The crypt ruins (`Tools/blender/crypt_entrance.py`)
  were rejected.
- The chosen look is `MineAbandoned` (`Tools/blender/mine_entrance.py`):
  - broken sagging beam, mouth boarded across;
  - a skull sign, broken rails, a tipped cart spilling gold;
  - a miner's skeleton, ivy over it all.
- The mouth is a TUNNEL, not a black patch (the user disliked the patch):
  - `cave_entrance.tunnel()` + `mountain(hollow=True)`;
  - rings narrowing and darkening inwards, inner props, a lantern deep in, black only at the end.
- Rock must not look like bricks. The mountain's greys draw the rough pattern
  (`detail.OVERRIDE` in `cave_entrance.py`).
- **ONE level only**: monsters and a boss (the user, 2026-10-30). Do NOT propose themed tiers or
  floors (mine → crystal caves → crypt → lair was rejected).
- The CITY IS PAUSED while the hero is in the dungeon (the user, 2026-10-31).
- The user's CONCEPT (2026-10-31), it replaces «only the hero» and my room/reward proposals:
  - a LABYRINTH about 200 x 200 m the player walks through, fighting monsters;
  - a kill gives the hero coins and experience;
  - hiring buildings inside the labyrinth sell soldiers for coins, "as many as he had on the rest
    of the map" (meaning being clarified);
  - BOSSES in the labyrinth: killing one gives resources, coins and upgrade crystals;
  - upgrade crystals (the Laboratory's) are scattered all over the labyrinth;
  - SEVERAL dungeons on a map;
  - SEVERAL heroes may go into one dungeon together, to deal with the bosses.
- The user's answers (2026-10-31):
  - **Hiring:** up to 5 soldiers per dungeon; the cap is a config key.
    - They are NEW dungeon units from new themed dungeon buildings, not the kingdom's soldiers. The
      army on the map and in the dungeon are unrelated.
    - Hired soldiers can never leave the dungeon.
    - They are not controlled one by one: they follow the hero and help him fight.
    - With several heroes, all of them move together after the hero the player controls.
  - **Coins:** the treasury is shared with the town.
  - **Between visits:** the soldiers wait at the entrance and join the hero again on the next visit.
  - **Persistence:**
    - the dungeon is saved; monsters respawn after N minutes, bosses don't — so it can be farmed;
    - crystals do not respawn;
    - kills give heroes only a little XP. Sandbox hero XP should be made harder overall, because
      the map has many more places to level.
  - **Three dungeons per map,** each with a different structure and different bosses.
  - **Difficulty:** the nearer the Town Hall, the weaker the monsters and the smaller the rewards.
  - **Death:** a dead hero rises at the Town Hall.
  - **Shops in the dungeon** sell, for coins, timed potions for the party: healing, rage, speed,
    defence.
  - **Layout:** dungeon maps are made in BLENDER ahead of time (N of them), and a dungeon picks one.
    No generator — the user judged the map generator's quality low.
  - **Opening:** every dungeon is open from the start.
  - **Test:** a first labyrinth of 100 x 100 m.
  - **Next to discuss:** the crystal economy, i.e. how many crystals the Laboratory needs and how
    many lie in a dungeon.
- **Crystals** (the user, 2026-10-31):
  - **Why they exist:** they give the player a CHOICE. Either collect them in dungeons for free, or
    watch rewarded ads / buy them for real money. So crystals are shared across the whole Sandbox
    mode (Wallet is PlayerPrefs, global) and are deliberately hard to get.
  - **Price per level:** level 3 = 1, level 4 = 2, level 5 = 3 crystals.
  - **Supply on a map:**
    - dungeon level 1 ≈ 30, level 2 ≈ 50, level 3 ≈ 80 (≈160 in total, scattered + bosses);
    - the orcs' MAIN BASE ≈ 60;
    - the BOT CITY ≈ 60.
  - **Tap on the crystals** (the HUD plaque) shows how to get them.
- **The user's answers of 2026-11-01 on the camp:**
  - **Gnome hirelings:** the concept is approved.
    - They can be hired WITHOUT END (a dead one is hired again), but at most 5 at a time.
    - MODELLED and APPROVED 2026-11-01 («устраивает»). `Tools/blender/gnomes.py`, in the models file,
      collection «Подземелье-гномы». Facing -y, jointed like the monsters.
      - GnomeMiner, 0.4 cells tall, 790 tris: a brass helmet with a lamp, a white beard, a blue
        jerkin, an apron, a pickaxe.
      - GnomeCrossbow, 0.42 cells tall, 804 tris: a green pointed hood, a ginger beard, a brown
        coat, a crossbow held ready, a quiver.
      - GnomeGuardian, ~0.6 cells tall, 555 tris: `monsters.golem` at s=0.78 in light stone with
        brass veins and a blue core.
      - Render: `render_figures.py`, next to the King and a skeleton.
      - Exported to `Models/Monsters/Gnome*.fbx` (export_fbx.py routes `Gnome*` there).
      - `monsters.py` now builds only when run, so it can be imported.
  - **Potions** are NOT drunk on purchase.
    - A bought potion becomes an icon under the heroes' portrait in the top-right corner; tapping
      the icon drinks it.
    - They work only in the dungeon and never carry over to the town's inventory.
  - Dungeons of levels 2 and 3: LATER.
- **Over the entrance:** a label with the dungeon's LEVEL (1/2/3) when it is tapped. BUILT
  2026-11-01 as `UI/DungeonLevelLabel`: a parchment plaque «Уровень N» over the mine while it
  is held (`BuildingFocus.SelectEntrance` → `Follow`), gone when it is let go or a card covers it,
  never over the way out.
- **Respawn:** monsters come back ONLY when the player leaves the dungeon and enters again (replaces
  «after N minutes»). Bosses never come back.
- **Bosses:** the spider queen and the ore golem are approved. They must be MUCH bigger than a hero.
- **Hirelings:** gnomes approved — the miner (melee), the crossbowman (ranged), the ore guardian
  (a stone golem tank), from «Лагерь гномов-старателей». IN THE GAME 2026-11-01, see
  «Hire camp in the game».
- **Next step:** the user wants the test MAP (100 x 100) made in Blender to approve its style first;
  it will be redone if needed.
- **Test map v1** (grid of cubes and crags) was REJECTED (the user, 2026-10-31):
  - it had cubic elements, it was monotonous, it looked too simple;
  - the user's rule: a dungeon starts from the MAIN MAP's style -- its geometry and forms;
  - the reference is WARCRAFT: dungeons saturated and bright, greenery welcome even underground.
- **Test map v2** (2026-10-31, shown, awaiting the verdict): `Tools/blender/dungeon1.py -- <Dungeon-1.blend> [preview]`
  - Built the island maps' way (as `map5.py`):
    - passages are a signed-distance field (route capsules + noisy hall ellipses);
    - floor zones along smooth contours (forest floor, lush grass, moss under the walls, dirt
      paths, stony patches, a sandy lake shore);
    - rock as relief: moss at the foot, banded rock up the walls, a dark craggy top with moss;
    - the map's water bands;
    - the map's decor kit (`*-decor.csv`).
  - Props: building palette (`Dungeon-1-Props`) plus the DungeonLight_* Empties.
    - giant glowing mushrooms in blue, purple and red;
    - crystals;
    - the camp, supports, rails, the bridge, the spider lair, the golem hall.
  - Preview: `dungeon_preview.py -- Dungeon1 <out> name:x:y ...`
  - Counts: ground 10k tris, relief 19k, props 13.5k, decor 2.8k items.
  - Its own .blend: `Dungeon-1.blend` next to the Buildings file. Map5's surface code is copied
    in, since map5.py can't be imported (it runs on import).
- **v2 feedback** (the user, 2026-10-31):
  - **SIZE 200 x 200** (100 is too small).
  - **A REAL LABYRINTH:** more passages so the player truly wanders, plus rooms holding the
    crystals.
  - **What the user could not read:**
    - the grey spider egg clutches (they looked like stones);
    - the blue giant mushrooms seen from above (they looked like blobs).
    Both need a clearer look, or should go.
  - **The scheme comes FIRST,** before the 3D.
- **Plan v1** (`Tools/blender/dungeon_plan.py <out.png> [seed]`, seed 7; shown 2026-10-31):
  - A 10 x 10 grid of 20 m cells holding a seeded depth-first maze plus 9 extra loops, so it has
    real dead ends.
  - 2 x 2 halls:
    - the camp (south, the way in);
    - the lake (centre);
    - the golem (north-west) and the spiders (north-east), each with ONE way in.
  - 8 dead-end crystal rooms and 10 monster halls.
  - Writes `<out>.json` for the 3D build to follow.
- **Plan v1** was judged TOO SIMPLE by the user. **Plan v2** (2026-10-31, shown, seed 7):
  - A 20 x 20 grid of 10 m cells, passages about 4 m wide, only 6 loops.
  - Halls: camp 4x2, lake 4x4, the bosses 5x5 in the corners with one way in from their farthest
    side, two 3x3 monster arenas and a 2x2 one.
  - Dead ends: 31 in all. The 8 FARTHEST are crystal rooms; the other 23 are empty, to mislead.
  - 10 monster junctions.
  - The shortest walk: ~1140 m to the spider queen, ~790 m to the golem.
- Plan v2 APPROVED («мне нравится, делай»). Saved as `Tools/blender/dungeon1-plan.json` (seed 7).
- **Map v3** (2026-10-31, shown, awaiting the verdict): `dungeon1.py` now READS the plan json:
  - passages = capsules along every link (half width 2 m, noisy), halls = noisy rounded rectangles
    of the plan's cells, crystal rooms r 4.2, monster junctions r 5.2; a 10 m bucket index of the
    segments keeps the SDF fast (whole build ~30 s);
  - the lake (11 x 7.5 m) with a N-S bridge in the lake hall; the old camp moved to the plan's camp;
  - rails along the camp->lake path, timber supports on ~20 % of maze links, bones at the monster
    junctions and arenas, mushroom groves (taller, 1-2.2 m) in hall corners and at junctions;
  - the spider lair: 14 webs, 7 clutches of pale elongated eggs under webs, cocoons, bones;
  - the golem hall: 8 big blue crystals, a cart, the chest; crystal rooms: 2-3 clusters each;
  - counts: ground 50k tris, relief 80k, props 21k, decor 13.9k items, 156 lights, CHUNKS = 8 --
    far too heavy to export as is; cut before it goes to the game.
  - preview: `dungeon_preview.py` now centres on HALF = 100 (env `DUNGEON_HALF` overrides).
- **v3 feedback** (the user, 2026-10-31):
  - NO right angles and NO square rooms: the labyrinth needs a LIVING shape;
  - FEWER passages, but MORE DETAIL: same-looking passages tire the player;
  - NO crystals lying on the ground like ore. The reward crystal will FLOAT in the air later, so the
    player sees at once that it is a reward.
- **Plan v3** (`dungeon_plan.py`, seed 11, in `dungeon1-plan.json`):
  - a 14 x 14 grid (~14.3 m cells), 4 loops;
  - every node is jittered off its cell centre (`pos`), every passage bowed (`bends`, a quadratic
    bezier, `curve()`);
  - 7 crystal rooms, 17 dead ends, 7 monster junctions, ~700 m to each boss.
- **Map v4** (2026-10-31, shown, awaiting the verdict): `dungeon1.py`:
  - passage width changes along the way (1.5-3.2 m half width, fbm);
  - soft blobs at every turn; everything is joined with a smooth min (`smin`), so no corners;
  - halls are unions of wobbly ellipses (a core, 5 lobes, one lobe at each way in), never
    rectangles;
  - side NICHES on half the passages, each with something in it: a pool with stalagmites,
    mushrooms, a mine prop with rails and a barrel, rubble, bones with a torch, webs with eggs;
  - stalagmites along the sides;
  - the golem hall has boulders instead of crystals;
  - a crystal room has only an Empty `DungeonReward_<n>` at z 1.6, where the floating reward
    will hang;
  - counts: ground 62k, relief 66k, props 32k tris, 14.3k decor, 179 lights.
- **v4 feedback** (the user, 2026-10-31): the rooms are too EMPTY. And the user's own proposal: cut the
  map into SQUARES before it goes to the game, so the player loads only the nearest ones.
- **Map v5** (2026-10-31, shown, awaiting the verdict): `build_halls()` furnishes every hall:
  - **lake hall:** 2 ruined arches, 5 dwarven columns (some broken, with ivy), 3 fallen columns, a
    pier with a boat, an abandoned camp (tent, crates, bonfire);
  - **monster arenas:** a den round a big bonfire, with 2 skull totems, 3 rows of stakes, a cage,
    crates, bones and broken columns;
  - **spiders:** more webs, broken columns under webs, wrapped victims with skulls, webbed carts;
  - **golem:** a rune ring of standing stones where he stands, columns, carts with ore, rail
    stubs, crates, fires;
  - **monster junctions:** a totem each;
  - **crystal rooms:** a stepped pedestal under the floating-reward Empty, with a glow and two
    giant mushrooms.
  - Props are 48.5k tris, 207 lights.
- **Chunks (the user's proposal, agreed):**
  - The ground, relief and now the PROPS are cut into the same 8 x 8 squares of 25 m
    (`CHUNKS = 8`, names `…-c<x><y>`).
  - Props are cut by loose part (`split_by_chunk`), so a prop never splits between two squares.
  - Decor rows and DungeonLight/DungeonReward Empties carry positions, so the game buckets them
    into the same squares.
  - **In the game:** keep only the hero's square plus its neighbours (3 x 3, about 75 m) live, and
    switch the others off. NOT built yet.
- **v5 fixes** (the user, 2026-10-31):
  - The flat web triangles read as shards hanging in the AIR. A web is now an orb of thread rods
    strung upright between two rock spurs, standing on the ground. This raised props to 72.6k tris
    (the webs' rods); trim before export.
  - NO FLAME models and NO GLOW on bonfires, the campfire or torches: the fire and its light are a
    Unity EFFECT at the `DungeonFire_<n>` Empties (`fire_spot()`; not DungeonLight_*).
  - NO potion stall and NO hirelings' camp (the hut, barrels, banner) at the way in for now. Only
    the campfire, logs, torches and the daylight exit remain.
- **Camp buildings, made SEPARATELY for approval** (the user, 2026-10-31: «approve first, add
  them later»). They live in `Tools/blender/dungeon_camp.py`, collection «Подземелье» of the Models
  file, row y = -76; previews come from `render_preview.py`.
  - `DungeonHireCamp` (2.6k tris): a gnome hut (low, steep red roof, round door, chimney) with one
    corner per hireling:
    - pick rack + ore cart = the miner;
    - straw target with bolts + crossbow rack = the crossbowman;
    - stone golem on a plinth = the ore guardian;
    - also a hiring board, a blue banner with a gold hammer, barrels, crates, logs and a cold fire
      ring.
  - `DungeonPotionShop` (4.5k tris): a wagon-shop under a green-white striped awning:
    - four big bottles on the counter with colour tags: healing red, rage orange, speed green,
      defence blue;
    - shelves of potions, a hanging bottle sign, a cauldron of green brew, crates of bottles.
  - APPROVED by the user on 2026-10-31 and SET IN THE MAP. `dungeon1.py` imports the builders from
    `dungeon_camp.py` (its `cave.main` only runs as `__main__`) and makes them whole objects,
    `Dungeon-1-HireCamp` and `Dungeon-1-PotionShop`, not chunked, so the game can tap them.
    - The hire camp is ~6 cells west of the campfire, the shop ~6 cells east; both face the way in.
    - Decor keeps off their pads (`CAMP_SPOTS`).
- Measured 2026-10-31:
  - The Laboratory asks `research_crystals` = 1 crystal per level topic from level 3
    (`research_crystals_from_level`).
  - 46 buildings x levels 3/4/5 = 138, plus unit levels 3 ≈ 142 crystals for everything.

## Dungeon level 2 «Затопленные гномьи чертоги» (the user's answers, 2026-11-03)
- **Theme:** drowned dwarven halls -- dressed stone halls and pillars, water CANALS with bridges,
  ruined forges. Continues the gnome hirelings' story. Not the overgrown mine of level 1.
- **Bosses:** the CURSED DWARF KING (raises stone guards) and the WATER SERPENT in the main canal
  (dives and surfaces).
- **Monsters:** about half new, half the known ones made stronger (rats -> drowned ones, skeletons ->
  armoured dwarf skeletons), to save models and phone budget.
- **New mechanic: LEVER DOORS.** The way to a boss opens once the hero has pulled the levers in two
  far halls -- the labyrinth gets a goal, not just «walk to the end».
- **Size: 1.5x level 1** (the user) -- build 300 x 300 units -> ~132 x 132 cells at SCALE 0.44.
- **Variants:** ONE first, tested, then the others (unlike level 1's four at once).
- **Camp:** the same gnome hire camp and potion shop.
- Order of work (as for level 1): the PLAN first for approval, then the Blender map, then the new
  monster/boss models for approval, then the game.
- **More water, wading passages** (the user, 2026-11-03 «пусть по некоторым проходам игрок ходит по
  воде»): 4 canals; ~1/3 of passages are FLOODED, i.e. shallow water the heroes walk through -- those
  running along a canal and all inside the flooded quarters (`FLOOD_ZONES`). A dry passage crosses a
  canal on a bridge, a flooded one by a ford. **No slow-down in the water** (the user, 2026-11-03):
  wading is only a look -- the shallow floor is ordinary walkable ground under the water surface.
- **Halls are LIVING SHAPES, never rectangles** (the user, a standing rule for every dungeon): each
  big hall is a union of overlapping blobs; the plan json carries them (`hall_blobs`, plus
  `flooded`, `fords`, `flood_zones`) for the Blender build.
- Plan: `Tools/blender/dungeon2_plan.py <png> [seed]`, seed 92: 343 passages, 115 flooded, 17
  bridges, 12 fords, 10 crystal rooms, levers ~1129/629 m, king ~1157 m, serpent ~1186 m. Approved
  2026-11-03 («делай в блендере»).
- **Blender map: `Tools/blender/dungeon2.py`** (a copy of dungeon1.py reworked; ~2.5 min) ->
  `D:/blender projects/Buildings3D/Dungeon-2.blend`, preview via dungeon_preview.py. Sent to the user
  2026-11-03, **awaiting approval; not exported, not in the game yet**.
  - Water: ONE surface at WATER_Z -0.12; the wading floor at SHALLOW_Z -0.42 is still ground
    (above CLIP_Z -0.47 -> walkable, a land cell); deep beds fall below CLIP_Z -> no ground -> a
    water cell. Deep = canals between halls (never inside a hall rect, at a ford) + the serpent's
    pool; shallow = flooded passages, the great and serpent halls, canal banks, fords.
  - Bridges: the deck is GROUND (`bridge_at` in height_at, kept out of relief), the stone bridge
    prop sits on it; banks near a bridge kept dry.
  - Halls: the plan's blobs x0.66 with wobble 0.6 (at full size they merged back into the
    rectangle -- the first build looked square); the hall floor colour and the wet halls follow
    `hall_sd`, never the rect.
  - Props: dwarven columns, throne, levers, forges, bridges, fords; `on_floor` drops a prop onto
    the wading floor. The lever door's bars are their own object `Dungeon-2-Door` (export_map
    exports it) so the game can raise them.
  - Points csv adds `lever` x2, `door`, `throne`, `boss_king`, `boss_serpent`; lairs/crystals/
    patrols via dungeon_lairs' helpers.
  - Budget to watch (mobile): ground 144k, relief 152k, water 45k, props 65k tris, decor 27k
    items (~1M tris) -- ~2.3x level 1's area; check in the game step.
- **IN THE GAME as an EMPTY walk-through** (the user, 2026-11-03: «подключи эту карту в
  подземелье 2 ... пока что не добавляй монстров и ресурсы, я хочу просто пройтись»):
  - exported to `Models/Dungeon2/Dungeon-2-*` (`export_map.py -- <project> Dungeon2 Dungeon-2`),
    `CreateDungeonDefinition("Dungeon2", "Dungeon-2")` in SetupProjectCityScene;
  - `DungeonTravel.LevelTwoId` + `DungeonFor(seed, version, level)`: level 2 -> Dungeon2, levels
    1 and 3 -> a level-1 labyrinth (no level-3 map yet);
  - `dungeon2.py POPULATED = False`: no lair/boss/reward/crystal/patrol points, so no monsters and
    no crystals. Set it True (and give the game king/serpent bosses) when they are made;
  - the lever door's bars (`Dungeon-2-Door.fbx`) are exported but NOT loaded by the game yet, so
    the way to the throne is open; levers do nothing yet;
  - test `DungeonVariantTests.TheLevelTwoDungeon_LoadsEmpty_AndItsHallsAreReachedOnFoot` (renamed ..._HasItsMonsters_... 2026-11-04).
  - Still to do: the new monsters and both bosses (models first, for approval).
- **Level-2 monster roster (the user agreed 2026-11-04 «делай модели»):** 3 known ones made
  stronger (same joints, new look), 3 new, 2 bosses. Sizes in cells (hero ~0.8).
  - Drowned rats 0.3, pack 4-6, attack waders from the water (the rat's joints).
  - Armoured dwarf skeleton 0.6: horned helm, axe, round shield; gets up once + armour to half health.
  - Stone guard 1.2: the king raises them; stand by the lever halls (the small golem's joints).
  - Drowned dwarf 0.6: lies unseen under the water in flooded passages, rises when a hero is near.
  - Cave eel 0.5: only in water, bites waders, never on land.
  - Forge spirit 0.5: from ruined forges, keeps its distance, fire over an area (the kobold's role).
  - Boss CURSED DWARF KING ~2.5: hammer slam, raises 2-3 stone guards, at half health sits on the
    throne and heals while the guards live.
  - Boss CANAL SERPENT ~4 long: dives and surfaces elsewhere in the canal, a water spit knocks back,
    can be hit only while surfaced.
- **Models v1 (2026-11-04, sent, AWAITING APPROVAL; not exported):** `Tools/blender/monsters2.py`,
  collection «Подземелье-2-монстры» in Buildings-Size1-Models.blend (beside the level-1 monsters,
  y=20). Built from monsters.py/gnomes.py parts with colours swapped (`colours()` context) where they
  are kin: MonsterDrownedRat 369 tris, MonsterDwarfSkeleton 829, MonsterStoneGuard 687 (dark slate +
  stone beard + cyan runes, unlike the gnomes' light guardian), MonsterDrownedDwarf 833, MonsterEel 804
  and MonsterCanalSerpent 1596 (origin = WATER SURFACE, foam rings where they break it, only `Head`
  jointed), MonsterForgeSpirit 676 (hovers in an iron brazier, no legs), MonsterDwarfKing 1106 (a
  gnome grown x4.2 via `scaled()`; ~1.7 model units = the ore golem's scale). Cyan (`water`) = the
  drowned curse's colour throughout. Previews: `Design/previews/dungeon2/` (3/4-angle lineup renderer
  kept in the session scratchpad: framed to the tallest figure).
  APPROVED 2026-11-04 («Одобряю, в игру»).
- **Level-2 monsters IN THE GAME (2026-11-04; the user has not seen them):**
  - exported to `Models/Monsters/Monster*.fbx`, wired in SetupProjectOrcs -> OrcRaidManager fields;
    `OrcKind` DrownedRat=25 .. CanalSerpent=32 (`IsDungeonMonster` covers both ranges, the two
    bosses in `IsBossKind`, ForgeSpirit + CanalSerpent ranged with a ball shot); stats `mon_<id>_*`.
  - `dungeon2.py POPULATED = True`, Dungeon2 rebuilt + re-exported: 13 lairs, 10 reward rooms + 6
    lone crystals, 5 patrols, boss_king, boss_serpent, throne.
  - `DungeonMonsters`: a map with `boss_king` is level 2 -> bosses = king (spawned at the THRONE
    spot) + serpent; boss-near themes Guards (stone guards + dwarf skeletons) / Deep (eels + drowned
    dwarfs); cycle Drowned, DwarfUndead (skeletons + forge spirits), Ambush (drowned dwarfs + rats),
    Forge (spirits + skeletons). Counts `dungeon2_lair_*`. An eel is moved to the nearest wading spot
    (`WadingSpots`), a drowned rat where none; `dungeon2_lone_eels` (12) more scattered over the
    flooded passages `dungeon2_eel_spacing` apart (`ScatterEels`). Boss ids `-king` / `-serpent`;
    the slain-boss log names any boss by `#monster_<sheet id>`.
  - Powers (`DungeonMonsterPowers`, numbers `dungeon_*`): dwarf skeleton armour
    (`dungeon_dwarf_skeleton_armour`) until half health, then gets up once; drowned dwarf AMBUSH:
    `OrcUnit.Submerged` + the unit and its controller switched off (out of `OrcUnit.All`, no blow
    lands) until a hero is within `dungeon_ambush_reach`, rises `dungeon_ambush_rise_sec`; eel bites
    only a WADING hero (`OrcUnit.CanReach`); king: hammer slam round him, stone guards every
    `dungeon_king_guards_every_sec`, at half health once the THRONE: busy, `dungeon_king_throne_guards`
    guards rise, each alive heals him `dungeon_king_throne_heal` of max a second, cyan glow
    disc, `#log_dungeon_king_throne` (he heals where he stands -- he was put down by the throne;
    no walk to it); serpent: spit stuns `dungeon_serpent_spit_stun_sec`, dives every
    `dungeon_serpent_dive_every_sec` in a fight (submerged, figure sinks), comes up at another
    wading spot within `dungeon_serpent_surface_reach`.
  - Test `DungeonVariantTests.TheLevelTwoDungeon_HasItsMonsters_AndItsHallsAreReachedOnFoot`
    (was ..._LoadsEmpty_...): every new kind present, no level-1 kind, eels wading, drowned dwarfs
    submerged then rise by a hero, serpent immune dived and up on a wading spot, king on the throne
    with guards and healing; plus the old walk/levers checks.
  - Open: the user to play it; the king's walk to the throne; the serpent's swimming between dives.
- **Playtest round 2 of level 2 (the user, 2026-11-04, 6 screenshots) -- BUILT, not seen, no tests run (the user: «тесты не делай»):**
  - The order BUOY a third of the ships' (MoveOrderMarker `BuoyScale` 0.35, a holder the marker scales).
  - More enemies and more crystal places: `dungeon2.py lair_rows` -- the 12 farthest empty dead ends
    a lone crystal (`LONE_CRYSTALS`), the rest of the dead ends a CHEST, 8 more lone crystals out in
    the passages (`passage_crystals`, farthest-point spread), 10 patrols (`PATROLS`, level 1 has 5);
    every crystal guarded as before. Crystal rooms hold `dungeon2_crystals_per_room` (1) on level 2.
    Floating crystals ~30 + 20 from the bosses = the ~50 agreed.
  - CHESTS (`Dungeon/DungeonChests`, "chest" spots): the caches' chest model (MeshMapApplier
    `CacheChestModel`), a GOLD glint (the crystals' CrystalHalo, `_BaseColor` gold); a hero within
    `dungeon_chest_reach` opens it -- the lid swings up, ONE resource in turn (wood 20 / stone 20 /
    gold 6 / coins 25, `dungeon_chest_*`, times the level) via DungeonMonsters.ResourcesEarned (coins at
    once), `#log_dungeon_chest`. Not guarded, not saved: full again next visit.
  - The EXIT ROCK was walked through: `DungeonMode.MakeExit` adds a carving NavMeshObstacle box
    (`ExitRockCentre` 2.4 behind the exit spot, `ExitRockSize` 4.4 x 2.6) -- both levels.
  - The QUEEN ran off round the corridors: `Hero` flee in a dungeon goes only as far as the floor runs
    straight (NavMesh.Raycast), and not at all if that is under a cell.
  - LEVERS: a tap holds the lever (BuildingFocus ring, no «!»; `DungeonLevers.Highlight`), no order flag
    for it; a hero already by it pulls at once; let go when pulled. Notice reworded
    «Найден рычаг потайного прохода: {0} из {1}». The event feed starts a button higher below
    (DungeonMode: by the citizens' button's freed slot).
  - The SERPENT's dive: a splash going down (`DungeonWake.Splash`, rings x3), sinks over 0.9 s, under
    water a WAKE of rings runs from where it went down to where it comes up (the spot picked at the dive),
    a splash and a 0.9 s rise. Its spit is still the shared ball projectile.
  - BRIDGES REMOVED (concept change; the user: «если не можешь сделать нормально, то надо что-то
    поменять в концепции»): `USE_BRIDGES = False` -- every canal crossing is a FORD. The test now
    asserts no deck.
  - A pack member spawned behind the rock: `DungeonMonsters.SpawnAt` pulls a post the floor does not run
    to straight from the lair's middle (NavMesh.Raycast) back onto the lair's side.
- **Playtest round 3 of level 2 (the user, 2026-11-04) -- BUILT, built once, no tests, not seen:**
  - The QUEEN does not back off at all below (`Hero.TickFlee` returns in a dungeon): she keeps her
    distance like any archer.
  - Heroes ANSWER whoever strikes the group from further off (`SoldierUnit.StrikesGroup`: up to 2.5x
    the engage radius -- the serpent spitting from its canal), and one MARCHING answers whoever hits him
    from close by (`Assailant`) -- a follower pinned by monsters stood and took it.
  - The King's heal (`SetCurrentHealth`) now reports to his health bar at once.
  - Monsters WADE like heroes: `OrcUnit.GuardWalk` and `Chase` (the dungeon's moves) slow by
    `Moat.SpeedAt` and spread `DungeonWake` rings.
  - The king's throne: NO blue disc; his stone guards CLIMB OUT OF THE FLOOR (`RiseFromFloor`, 1.1 s,
    busy till up).
  - ROOT CAUSE of «the serpent just vanishes»: `FigureAnimator` writes the figure's localPosition every
    frame (the bob), so every sink/rise moved by hand was overwritten. It now has `Offset`, and the
    serpent's dive, the drowned dwarf's rise and the guards' climb go through it.
  - Potions: nothing level-specific in DungeonPotions -- not reproduced; ask the user what he saw.
- **Potion shop was INSIDE THE ROCK on level 2** (the user's screenshot, 2026-11-04): level 1's
  "camp + (14, 3)" falls in the rock of this narrower camp hall. `dungeon2.py clear_spot()` now picks
  the dry floor nearest that point where the shop's whole round (4.5) is clear of rock and water and
  11 from the hire camp and the campfire: SHOP_AT (166, 21.3), game spot (7.04, -56.63). Checked in
  Blender (0 relief verts over its floor, 42 before); not seen in the game yet.
- **Playtest round 1 of level 2 (the user walked it in the editor, 2026-11-04; 11 screenshots).**
  Done; the user has not seen the result yet:
  - **Bridges from the BUILT geometry** (`plan_bridges()`): every crossing of a passage between
    halls with a canal; a dry one gets a bridge straight ACROSS the canal if it crosses squarely
    (>=0.8), both ends land on the passage's floor `BRIDGE_DRY` (2.4) past the canal (before any
    rock) and no other bridge is within 18; every other crossing is a FORD. Deep water never lies
    across a passage (`PASS_CLEAR` 4.2) except under its bridge. Was: bridges from the plan's
    straight links -- to nowhere, through rock, crossing each other. Now 4 bridges, 25 fords.
  - The deck (`deck_height`) slopes down to the floor over the outer 30% of each half
    (`BRIDGE_RAMP`): a deck ending a step above a wet bank stopped the hero (screenshot 6).
    A deck only where `pass_sd < 0.6` -- never a tunnel in the rock (screenshot 11).
  - **Knee-deep water:** `SHALLOW_Z = -0.60` (0.48 m = 0.21 cells under the surface), `CLIP_Z
    -0.70`. In the game every walker used to be pinned to the plain's one height (on the water,
    through the decks): `Maps.DungeonFloor` samples the dungeon's floor once a cell at load
    (`MeshMapApplier.TryGetFloorHeight`, ground colliders only), `Moat.StandHeightAt/SpeedAt` ask
    it first. Wading is slower: `dungeon_wade_speed_multiplier` 0.7 (economy). Rings spread round a
    wading walker (`Dungeon/DungeonWake`, one pool of 24, hooked in SoldierUnit/OrcUnit moves).
  - A move order into the water shows the ships' BUOY instead of the flag (`MoveOrderMarker`
    `PickMarker`, `ArmyOrderInput.orderBuoyPrefab` = OrderBuoy.fbx).
  - **Levers and the door** (`Dungeon/DungeonLevers`): new lever model (block, slot, cog wheel,
    chain) + its swinging arm exported alone (`Dungeon-2-LeverArm`), `MeshMapDefinition.LeverArmPrefab`
    / `DoorPrefab`. A lever is a `DungeonEntrance.Place.Lever`: a tap without a hero near posts
    «Отправьте героя к рычагу»; a hero sent to it pulls it (arm swings 60°, «Рычаг опущен: N из 2»);
    both down -> the bars rise 1.6 cells in 2.5 s and the carving NavMeshObstacle goes
    («Решётка поднята: путь к трону открыт»). Not saved: leaving the dungeon resets them.
  - **The king's hall is walled off but for the door** (`throne_sd`, `THRONE_WALL` 3): the plan's
    passages run along the hall's edge and its living shape flowed into them -- a way in past the
    door. Every other passage keeps 3 m of rock from the HALL (not from the door passage -- that
    cut the way up to the door). The build prints `DOOR SHUT: ... throne cut off` and what else
    the shut door cuts off (must be only the serpent's pool centre).
  - The build's reachability walk (`reachability`) now needs a hero's width (4-neighbours walkable)
    and no step steeper than `MAX_STEP` 0.55 m/m -- a 1-vertex neck or a deck's lip passed before.
  - **Long ways round: `Maps.DungeonWaypoints`.** NavMesh.CalculatePath's search runs out of nodes
    on a long detour in a labyrinth this big (thousands of polygons): it returns PARTIAL, ending in
    a dead end near the target, and re-planning finds the same -- the «can't reach the upper-left
    corner». A road map: nodes every 6 cells snapped onto the NavMesh, linked to neighbours by short
    complete paths at load; `NavRoute` asks it when a path comes back partial (Dijkstra, the legs'
    corners strung). The door gates the links through its bars (`Gate`/`Ungate`). The dungeon's
    NavMesh voxel is 0.11 (town 0.073; 0.2 closed narrow passages): ~8k triangles instead of 12k.
  - Cheat «Снять туман подземелья» (CheatsPanelForces, `DungeonFog.RevealAll`).
  - The town's time stays PAUSED while the heroes are below (the user chose «Оставить паузу»).
  - Test `TheLevelTwoDungeon_LoadsEmpty_AndItsHallsAreReachedOnFoot` now also checks: >200 wading
    points knee-deep and slower, decks above the water, both levers reached the way a hero walks
    (NavRoute), the throne NOT reached while the door is shut, both levers pulled by a hero ->
    door open -> throne reached. Broken twice to prove it (door never opening; no road map).

## Proposed, NOT agreed yet (ask, don't build)
- (superseded by the user's concept above: layout from Blender, rewards = coins/XP/crystals/resources)
- Room content:
  - chests (a mimic);
  - traps;
  - levers and locked doors;
  - a buff shrine;
  - healing mushrooms;
  - prisoners who join the town.
- Rhythm:
  - hero death throws the hero out, losing part of the loot;
  - the dungeon refreshes every N minutes;
  - one entrance per map, or several.
- Enemy ideas for an old mine: skeleton miners, bats, rats, spiders, kobolds. Boss ideas: an ore
  golem, a spider queen, a cave troll.

## Monsters (agreed with the user 2026-10-31; in the game, see below)
- Roster «пока что подходит» (sizes in cells, hero ~0.8):
  - cave rats 0.3, a pack of 4-6;
  - bats 0.4, hit and fly off;
  - spider hunter 0.6, leaps from webs, poison slows;
  - skeleton miner 0.8, a pick, slow and tough, gets up once;
  - kobold blaster 0.6, keeps its distance and throws dynamite that hits an area;
  - small stone golem 1.2, a tank, near the golem hall.
- Bosses:
  - spider queen ~3x3: a web spit holds a hero for 2 s, calls spiderlings;
  - ore golem ~3 tall: a ring slam, throws boulders, ore armour at half health.
  - The queen's «on the wall» phase is REJECTED. At half health she seals herself in a COCOON for
    6-8 s and spiderlings run to her; spiderlings left alive heal her through the cocoon (the user chose it).
- Placement: a lair of 3-6 at each of the 7 monster junctions, 2-3 patrols in passages; the
  camp is safe.
- Monsters HOLD THEIR LAIR: they aggro at ~4 cells, chase only a short way, then return.
- Levels 1/2/3: the SAME monsters, stronger and better paid, by multipliers in the sheet.
- Elite (rare stronger) monsters: an idea to keep, NOT now (the user).
- **Models v1 (2026-10-31, shown, «переноси в игре»):** `Tools/blender/monsters.py`, collection
  «Подземелье-монстры» in Buildings-Size1-Models.blend; exported to `Assets/_Project/Models/Monsters/`.
  - The models are built at GAME size: the King is 0.53 cells tall.
  - Sizes: rat 0.22 long; bat 0.3 wingspan, flying at 0.35; spider 0.42 across; spiderling 0.22;
    skeleton 0.5; kobold 0.4; stone golem 0.8; queen ~2 across; cocoon 1.2 tall; ore golem 2 tall.
  - Jointed like `wild.py` (LegL/R or LegFL..BR, Head, ArmStrike/ArmOff, bat WingL/R).
  - 260-660 tris each.
  - Previews: `render_figures.py` with HeroKing.
- Technical plan: build on the orcs' combat code as another hostile side; units rows `monster_*`;
  monsters far from the camera sleep; 30-40 active at most; Blender models first, shown next to
  a hero for scale.

## Monsters in the game (phase 2b, 2026-10-31)
- **What they are:** kinds of `OrcUnit` (`OrcKind` Rat=11 .. OreGolem=19, `IsDungeonMonster`,
  `IsBossKind`), like the jungle's wild animals. Stats are economy rows `mon_<id>_*`
  (rat, bat, spider, spiderling, skeleton, kobold, stone_golem, spider_queen, ore_golem) read by
  `BalanceConfig.MonsterUnit`. The asset's monster list is topped up from `MonsterIds`, so a
  monster added later is never silently left on its C# fallback.
- **Lairs:** `dungeon_lairs.py` writes `lair` spots (the 7 monster halls + 3 arenas) and
  `boss_spiders` / `boss_golem` into `Dungeon-1-Points.csv` (also run by `dungeon1.py`).
  `DungeonMonsters.Begin` (added by `DungeonMode` after the heroes): the `dungeon_boss_theme_lairs`
  lairs nearest each boss take its theme (spiders+spiderlings / stone golems+skeletons); the rest
  cycle rats, skeletons+kobolds, bats, rats+bats. Counts are `dungeon_lair_*`.
- **Hold the lair:** `SetGuardPost(null, post, radius)`; radius `dungeon_monster_guard_radius`
  (4 cells), bosses `dungeon_boss_guard_radius` (7).
- **Levels:** the entrance's level goes through `DungeonTravel.Enter(id, level)` →
  `GameSessionIntent.DungeonLevel` → `DungeonMode.Level`; the monsters' unit level is
  `dungeon_monster_level_N`.
- **Powers** (`DungeonMonsterPowers`, numbers `dungeon_*`): spider poison slows (`SoldierUnit.Slow`);
  skeleton collapses once and gets up (`OrcUnit.GetsUpOnce`); kobold throws (ranged, cannonball
  projectile); queen: web spit stuns (`SoldierUnit.Stun`) with a white web disc, brood calls,
  COCOON at half health (figure hidden, `SpiderCocoon` model, spiderlings heal her each second
  they live); ore golem: ring slam, boulder at a far hero, ore armour at half health (`Armour`, gold
  and copper lumps).
- **Animations** (`FigureAnimator`): walk legs (2 or 4), head, strike arm for all; bat wings flap
  and bob, dive on a strike; `Slam()` both fists for the golems; `OnCollapsed` falls and rises
  (skeleton).
- **Rewards:** coins per kill = ceil(health × `dungeon_coins_per_health`); a boss adds
  `dungeon_boss_coins` and `dungeon_boss_crystals` × level (crystals straight into
  `Wallet.UpgradeCrystals`), posts `#log_dungeon_boss_slain`. XP = health × `dungeon_xp_share`.
  `DungeonTravel.Leave` adds the coins to the town save. Slain bosses (`DungeonRecord`, save field
  `dungeonBossesSlain`, id `{dungeon}-L{level}-{spiders|golem}`) never come back; the rest respawn
  on every visit (each visit is a fresh scene).
- **Guards and patrols** (the user, 2026-10-31: «слишком мало монстров ... каждый кристалл
  должен быть защищён монстрами», then «делай охрану и патрули» after the marked top view v2):
  - Lairs bigger (rats 8, bats 5, spiders 3+4, skeletons 3+3 kobolds, golems 2+3 skeletons,
    rats+bats = `dungeon_lair_pack_share` 0.75 of each); an 11th lair on the lake hall's south shore
    (`LAKE_LAIR` in dungeon_lairs.py; its theme comes out of the cycle: bats).
  - A guard band at every crystal: the 7 `reward` rooms (`dungeon_guard_room_share` 0.5 of a lair)
    and 6 lone `crystal` spots in the farthest empty dead ends (`dungeon_guard_single_share` 0.35).
    Theme: the nearer boss's within `dungeon_guard_boss_reach` 30 cells, else the cycle.
  - 5 patrols (`patrol_<k>` route spots from dungeon_lairs.patrols: corridor runs between
    junctions, longest and farthest from lairs, never by the camp): `dungeon_patrol_size` 3
    monsters walk there and back via `OrcUnit.MovePost`, pausing `dungeon_patrol_pause_sec` at the
    ends; they never move mid-fight or asleep.
  - ~140 monsters in all (was 46).
- **Crystals** (2026-10-31, «делай»): `Dungeon/DungeonCrystals` -- `dungeon_crystals_per_room` (2) at
  every `reward` spot, `dungeon_crystals_lone` (1) at every `crystal` spot: 20 on Dungeon 1 + 10
  from the bosses = the agreed ~30 for level 1. Model `MonsterRewardCrystal` (monsters.py, a blue
  gem with two shards, 0.5 tall, 66 tris; wired through OrcRaidManager.RewardCrystalModel), floating
  `dungeon_crystal_float` cells up, spinning and bobbing. A hero within `dungeon_crystal_pick_reach`
  takes it (a room's pair goes together). A reward spot carries its height (`MapSpot.height`, read
  from Points.csv z): it is the crystal's MIDDLE over the room's pedestal (0.7 up; the pedestal is
  0.22 tall) -- floor + 0.45 would have sunk the crystals into the pedestal. No NavMesh snap: it
  would pull them off the pedestal.
- **Crystal look v2 -- APPROVED «мне нравится» 2026-11-01** (the user, 2026-11-01: «яркие, светящиеся, сочные, слегка покачиваться ...
  особенная вещь, платный контент»; v1 «a blue gem» rejected): model = a cut 8-sided gem 0.7 tall +
  3 shards circling it (106 tris). `Shaders/Crystal` (self-lit violet->cyan gradient, facets by
  the sun, white rim, a light band running up, facet flashes; no bloom), `Shaders/CrystalHalo` (a
  camera-facing breathing halo 1.6 cells with 6 rising twinkling sparkles), an `OreGlow` disc on
  the floor / pedestal top. Materials Crystal/CrystalHalo/CrystalGroundGlow.mat made by
  SetupProjectOrcs and held by OrcRaidManager. Motion: bob 0.07 cells, sway 6 deg, turn 35 deg/s;
  taken, it flares up, rises and melts in 0.45 s. Photographed in the game (run.ps1 -Graphics, a
  temporary PlaytestCapture test, not kept).: +1 `Wallet.UpgradeCrystals`, `#log_dungeon_crystal`, id
  `{dungeon}-L{level}-c{index}` into `DungeonRecord.CrystalsTaken` / save `dungeonCrystalsTaken`
  (also written on Leave) -- never comes back. No need to kill the guards first. Test
  `TheCrystals_FloatGuarded_AndAHeroTakesOne` (break-checked). Not seen in the game by the user.
  - Test `EveryCrystal_IsGuarded_AndThePatrolsWalk`, break-checked (lone guards off; patrol frozen).
- **Sleep** (the user's item 4, 2026-10-31): a monster out of the camera's frame (quarter-screen
  border, `Camera.WorldToViewportPoint`) and more than `dungeon_monster_wake_margin` (6) cells from
  every hero has `OrcUnit.Asleep` = no Update at all (powers too); in a fight, busy, or hurt in the
  last 6 s it stays awake, and any blow wakes it. Checked every 0.3 s by `DungeonMonsters.RefreshSleep`.
  At the camp ~half the dungeon sleeps. Animation was already culled off screen (FigureAnimator).
  Not measured on a phone.
- Tests (`DungeonTests`): `TheMonsters_StandInTheirLairs_EachKindJointedAndAnimated` (+ sleep),
  `ASkeleton_GetsUpOnce`, `TheBosses_CocoonAndArmour_AndTheyPay` (break-checked).

## Brightness (2026-10-31)
- The user: «подземелье тусклее основной карты» -> «вариант 1»: as bright as the surface. Measured
  as the area-weighted luminance of the terrain palette seen from above (scratch script over the
  Ground/Relief meshes): it was 0.52 (moss 32%, rock_deep/rock_dark tops 28%) against Map 3's 0.65;
  now 0.66 (grass_mid, rock, rock_light, path, grass). Same scene light as the surface -- the
  difference was only the palette (`zone_colour`, `relief_colour`, the moss hem in `fields_at`).
- Option 2 (keep it a little darker, ~0.58-0.60, for a cave mood with glowing mushrooms and fires)
  was offered and set aside «пока что».

## Dungeon-mode rules (the user, 2026-11-01, screenshot 9; BUILT, user not seen in game)
- **Hero:**
  - The King comes down HELD (`DungeonMode.SpawnHeroes` selects the first hero).
  - A hero stands where he is left. `Hero.HoldWhereLeft`, set by DungeonMode, turns off the walk
    home after an order (ReturnIn), castle guarding and the round fog reveal.
  - A fallen hero rises at the way out: `Home` = `RiseSpot`, 2 cells in front of the exit.
- **The way out** is a `DungeonEntrance` with `IsExit` (`DungeonMode.Exit`, a trigger box 5x4x5
  at the exit spot).
  - A tap selects it with a ring and the «!»; a hero sent to it (`Await`) opens the card
    «Выход из подземелья», whose «Выйти» calls `DungeonTravel.Leave`.
  - Passing by does nothing: the old walk-out-on-touch is gone.
  - Keys: `#dungeon_exit_*`.
- **Camera:** `RTSCameraController.LockView(dungeon_camera_distance 12, dungeon_camera_pitch 50)`;
  min = max distance, so there is no zoom.
- **HUD:**
  - The top bar shows only Coins, and the people chip turns into the upgrade crystals
    (`ResourceHUDController.ShowCrystals`, sprite `Res_UpgradeCrystal`).
  - The coins are the TOWN's: DungeonMode sets them from the hidden town save, and Leave still
    adds `CoinsEarned` to the saved amount.
  - No «День» (`SettlementStatusController`).
  - The «Выбран …» badge sits in the very corner (`SelectionBadgeController`).
  - `WorkforceButton` is hidden, and `InventoryButton` moves to its place. It still opens the
    town's «Запасы»; a hero inventory does not exist yet.
- **DARKNESS replaces the fog** (the user, 2026-11-01 round 2). The whole dungeon is dark, and
  it lifts only along the passages a hero walks, never behind rock.
  - The town's fog is OFF below (`DisabledCheat`). Its blanket sat 3.5 m over the highest rock,
    and its holes slid off the floor.
  - `Shaders/DungeonDark`: a full-screen quad on the main camera, drawn in the Overlay queue.
    - Each pixel's depth → its world point → its cell in `DungeonFog`'s eased R8 mask, with a
      5-tap soft edge.
    - The void past the map's edge (sky depth) is dark, and the camera clears to black. That is
      the answer to «край карты выглядит странно».
    - Strength 1: in linear light, 0.97 still showed the map at about 20% brightness.
    - The edge is like the parchment's, after the user saw «темнота квадратами»: a 9-tap blur
      1.6 texels wide, then a two-octave value-noise wobble (`_Wobble` 0.35, drifting slowly)
      cut by a smoothstep. No cell steps show.
    - The MAP's edge is never a straight line (the user, 2026-11-01): the darkness eats a
      wobbling band `_MapEdge` (2) cells into it. It must stop short of the camp, ~7 cells
      from the south edge; at 3 cells it ate the hire camp.
  - Needs the depth texture: `GetUniversalAdditionalCameraData(cam).requiresDepthTexture`, in a
    dungeon only. A test photo camera needs the same (`PlaytestCapture.Configure`).
  - The runtime asmdef now references URP (Universal + Core Runtime).
  - Material `DungeonDark` is made in `SetupProjectOrcs` and held by `OrcRaidManager`.
- **Hire camp in the lake (fixed):** the HireCamp/PotionShop FBX carry their spot as a BUILD-unit
  offset, so they landed in the lake hall. `MeshMapApplier.StandOnSpot` now moves each model's
  bounds centre onto its `hirecamp`/`potionshop` spot.
- **Tent:** the lake hall's tent was a 4-sided cone, which the user saw as a «pyramid». It is now a
  ridge tent with a door, poles and guy ropes (`dungeon1.py tent`); the map was rebuilt and re-exported.
- **Sleep:** a monster sleeps until a hero comes within `dungeon_monster_wake_margin` (6) cells.
  - A sleeper in sight shows «Zzz» (`Shaders/SleepZ`, a camera-facing quad made once per monster).
  - A patrol in sight still walks.
  - With graphics, a figure seen by no camera is not animated (`FigureAnimator._visibility`).
- **Fog (2026-11-01 round 1, its parts kept)** (`Dungeon/DungeonFog.cs`):
  - The floor is sampled from the NavMesh once per cell.
  - Every 0.25 s each hero floods `dungeon_reveal_cells` (7) steps over the floor. Each cell
    reached is revealed for good with a 1-cell ring (the walls).
  - A tunnel behind rock is many steps away, so it stays hidden.
  - Revealed cells stay open and live.
- **Minimap:** `DungeonFog` draws one texel per cell into the minimap
  (`MinimapCameraUpdater.ShowPicture`, which renders no camera).
  - Colours: unknown = parchment brown, open floor = light, walls = dark, heroes = gold,
    monsters in the open = red, exit = green.
  - The view marker still works: the minimap camera is only placed, never rendered.
- **Test:** `DungeonTests.TheDungeonRules_HeroHeld_WayOutCard_Fog_FixedCamera_Hud`.

## The way out (2026-10-31, redone)
- The user: «выход ... должен быть развёрнут к игроку, ... дать ему стиль». The old one was a flat
  daylight pane in the camp's SOUTH wall, seen from behind by the camera (which looks north).
- A timbered mine adit (v2, same day) was REJECTED: «полная фигня ... как будто в пустоту и с
  кучей ненужной геометрии». Rule: the way out is a cave in a SOLID ROCK, no props round it.
- Now (v3) `mine_exit()` in `dungeon1.py`: the surface entrance's mountain (cave_entrance.mountain
  layout) built 2.6x as big through `placed()` -- big rock, peak, two shoulders, cheeks, a rock brow,
  turf on top -- standing on the camp floor 2 build units in front of the north wall
  (`find_exit()`), with a dark cave mouth (rim + black) facing the camera. No cut in the terrain.
  `rough_exit_rock()` rewrites its stone faces to the rough (plaster) pattern after
  `detail.add_layers`, since stone_* draws dressed courses everywhere else.
- The `exit` spot is at the cave's foot, ~4 cells north of `start`. A new `edge` spot (middle of
  the south edge) gives the map's half size (`DungeonMode.MapHalf`) -- that used to be read off
  the exit.
- Seen only in Blender previews.

## In the game (phase 1, 2026-10-31)
- **Export:**
  - Run `export_map.py -- G:/Fork/CityBuilding Dungeon1 Dungeon-1` on `Dungeon-1.blend`.
  - It writes `Models/Dungeon1/`: Ground, Relief, Props (each 64 chunks), Water, DecorKit + Decor.csv,
    HireCamp, PotionShop, and `-Points.csv`.
  - `Points.csv` holds kind,x,y,z in Blender axes and game units. The kinds are light_<kind>, fire,
    reward, hirecamp, potionshop, start and exit.
  - `SetupProjectMaps.CreateDungeonDefinition` turns that into `Resources/MeshMaps/Dungeon1.asset`.
    It is a `MeshMapDefinition` with `IsDungeon`, `PropsPrefab`, `BuildingPrefabs`, and the points
    as MapSpots. A dungeon is NoRaids.
- **`MeshMapApplier`:**
  - The ground, relief, water and decor load as on any map.
  - A dungeon gets no ore veins and no boulders.
  - `DressDungeon` adds the props and the camp buildings, plus a `DungeonChunkStreamer`. The
    streamer switches RENDERERS only, never colliders. It draws a square only near the point the
    camera looks at: the camera's square plus a ring, widened as the camera pulls back. At the
    camp that is under half of the 407 renderers.
- **`Dungeon/DungeonTravel`:**
  - `Enter(id)` autosaves, then writes the town into the hidden slot `_dungeon`
    (`SaveSystem.HiddenTownSlot`, not listed). It notes the living heroes (level, XP, health) and
    reloads the game scene on the dungeon's map.
  - `Leave()` writes the heroes' level, XP and health into that slot (a fallen hero comes up with
    1 HP), then loads the town from it under its old name (`GameSessionIntent.SaveNameAfterLoad`).
  - The town does not run while the heroes are below — that is the user's «город на паузе».
- **`Dungeon/DungeonMode`** (on GameManagers, wired in SetupProjectCityScene):
  - It switches off the same managers as Invasion and marks the Town Hall as placed.
  - It hides `BuildingMenu`, `Hotbar` and `TaxRate`, sets the camera's pan bounds and jumps the
    camera to the camp.
  - It restores the heroes at the `start` spot (the King at level 1 if none came).
  - (until 2026-11-01 a hero within 1.6 cells of `exit` took the party home -- now the exit's card does)
  - Autosave and `SaveGame` are blocked in a dungeon.
- **Cheats:** the Мир tab has «Войти в подземелье 1» and «Выйти из подземелья».
- **Tests:** PlayMode `DungeonTests`, 4/4, verified by breaking the streamer and the save guard.
  - The King comes down at level 3, at the camp, on the NavMesh.
  - The dungeon is 88 cells across.
  - No Town Hall prompt appears.
  - Every hall and boss is reachable on foot, by a winding path.
  - Only near squares are drawn.
  - Nothing is saved below.
- **Not yet:**
  - monsters and bosses;
  - hiring at the camp, the shop;
  - rewards (the reward spots exist);
  - fire effects and lights (spots exist);
  - coins/XP for kills;
  - respawn on re-entry;
  - the bridge over the lake (a prop, not walkable — the shore goes round);
  - a dark/cave look (lit like the surface).

## Entrances on the map (phase 2a, 2026-10-31)
- The user: «размести три подземелья, пока что все ведут к одной карте».
- `Maps/Archipelago/DungeonEntrances.Place` (called in `MeshMapApplier.ApplyGenerated` before the
  forest, library maps only):
  - `dungeon_entrance_count` (3) islands, none of them the start's, the orcs' or the bot's,
    nor within 40 cells of the orc base;
  - the nearest, the farthest and evenly between, sorted by distance to the start;
    LEVEL 1 = nearest;
  - on each, a 5 x 5 plain spot (with a 1-cell margin, no beach/shore/relief/cliff) nearest
    the island's middle;
  - the mouth turned to FACE THE CAMERA, which never turns (the user, 2026-10-31);
  - the 5 x 5 must be GRASS by ground colour (`AllGrass`, ray + UV): one stood on sand. An island
    with no grassy spot gives way to its neighbour in the distance list;
  - the cells are no-build and occupied;
  - from the seed alone, nothing saved.
- Model `MineAbandoned` exported to `Models/Buildings` (4.3 x 4.5 x 3.4 cells); wired in
  `SetupProjectMaps.CreateArchipelagoDefinition` (`MeshMapDefinition.DungeonEntrancePrefab`).
- `Dungeon/DungeonEntrance.cs` holds Level, DungeonId (all `Dungeon1` for now) and Cell; `All`.
  - SINCE 2026-11-01 (the user): a hero merely passing within `dungeon_enter_reach_cells` + 2.5 gets
    the feed notice `#log_dungeon_near` «Рядом обнаружено подземелье», never the card; a tap on the
    notice (`GameEvent.Tapped`) lets the hero go and selects the mouth.
  - A tap on the mouth with a HERO held sends him there (`ArmyOrderInput`: `ApproachFrom`, `Await`);
    his arrival opens the card (`DungeonEntrance.Awaited`). Any other order clears it.
- `UI/DungeonEntranceCard.cs`: «Подземелье», «Уровень N», then «Герой у входа» or «Приведите героя
  ко входу»; «Войти» is live only with a hero there and calls `DungeonTravel.Enter`.
  - A tap on the mine SELECTS it like a town building (`BuildingFocus.SelectEntrance`): a gold ring,
    the build menu hidden, the «!» at the bottom; the «!» opens the card (2026-11-01).
- The level is not passed down yet: every entrance opens the same Dungeon 1.
- Test: `ArchipelagoTests.DungeonEntrances_AndTheForest_DrawnNearTheCameraOnly`.

## Hire camp in the game (2026-11-01; user not seen it)
- **Types:** `SoldierType.GnomeMiner/GnomeCrossbow/GnomeGuardian` (7-9), `SoldierStats.IsHireling`;
  units sheet rows 17-19 `gnome_miner/gnome_crossbow/gnome_guardian` (health, damage, range, speed
  2.2 like the King, `recruit_coins` = the price 40/60/100). Level 1 only: left out of the
  Laboratory (ResearchCatalog, ResearchPanelController) and of `soldierModels`.
  `ArmyManager.hirelingModels` (Setup loads `Models/Monsters/Gnome*.fbx`); the crossbowman is
  ranged (`SoldierUnit.IsRanged`).
- **Places:** `DungeonEntrance.Place` = Mouth / Exit / HireCamp / Shop. `DungeonMode.MakeCamp`
  puts a tap box round the model standing on the `hirecamp` / `potionshop` spot; held with the
  ring and «!» (`BuildingFocus` uses `entrance.Half`), a hero sent there opens its card
  (`OpenCard`). No «Уровень N» plaque over camps.
- **`Dungeon/DungeonHirelings`:** one party group of its own (`IsSummoned`, not in the army's
  groups: no icon, no upkeep, not in `SoldierCount`). `Hire(type)` pays through
  `DungeonMode.TrySpendCoins` and spawns at the camp; at most `dungeon_hireling_max` (5) alive; a
  dead one frees his place. Every 0.25 s the party's hold moves to the leader (the held hero, else
  the first standing) once he is `dungeon_hireling_follow_cells` (2.5) away; a gnome further than
  `dungeon_hireling_leash_cells` (9) drops the fight and catches up (`Retreat`).
- **Persistence:** `DungeonTravel.Leave` writes the living gnomes (`HirelingEntry`: dungeonId,
  type, health) into `town.dungeonHirelings` (`DungeonRecord.Hirelings` round-trips them through
  town saves); on the next visit they stand by the way in. The town's coins on the way up =
  + earned − `DungeonMode.CoinsSpent` (never below 0).
- **Card:** `UI/DungeonHireCard` (built in `SetupProjectPanels.BuildDungeonHireCard`): three rows
  name / «Здоровье N, урон N» / «Нанять: N», the count «В отряде гномов: N/5» or «Приведите героя
  к лагерю». Localization rows 514-521.
- **Test:** `DungeonTests.TheHireCamp_HiresUpToFiveGnomes_WhoFollowTheHero`.

## Potion shop in the game (2026-11-01; user not seen it)
- **`Dungeon/DungeonPotions`** (`PotionKind` Healing/Rage/Speed/Defence): `Buy` pays through
  `DungeonMode.TrySpendCoins` and puts it in the bag -- NOT drunk. `Drink` works on the whole
  party (living heroes + gnomes) for `potion_<kind>_seconds`: rage = `SoldierUnit.Enrage(power)`,
  speed = `Haste(power)` (walk factor), defence = `Ward(power)` (share of each blow taken, min 1),
  healing = `power` share of max health back over its seconds (0.5 s ticks, `SoldierUnit.Heal`).
  Only units in the party at the moment of drinking get it. Feed line `#log_potion_drunk`.
- **Sheet:** economy rows 283-294 `potion_<healing|rage|speed|defence>_<coins|seconds|power>`
  (30/10/0.6, 40/20/1.5, 25/30/1.4, 35/20/0.5); localization rows 522-535.
- **UI:** `UI/DungeonShopCard` (four rows: picture, name «(в сумке: N)», what it does, «Купить: N»;
  built in `SetupProjectPanels.BuildDungeonShopCard`). `UI/DungeonPotionBar` (built in
  `SetupProjectHud.BuildDungeonPotionBar`): round icons with a count badge, top right UNDER the
  heroes' portraits (`HeroPortraitController.StackBottom`), only kinds in the bag, packed to the
  right; a tap drinks one. Icons `UI/Icons/Potion*.png` drawn with PIL (flask, cork, liquid).
- **Persistence:** the bag goes into `town.dungeonPotions` on the way up (`DungeonRecord.Potions`
  round-trips it); never into the town's inventory.
- **Test:** `DungeonTests.ThePotionShop_SellsPotions_DrunkFromTheBarForTheParty`.

## Playtest round 4 (2026-11-01, commit b8a5d4b0; user not seen it)
- **One party below:** the held hero leads; the other heroes walk after him
  (`DungeonHirelings.FollowWithHeroes`, places beside him by `FormationOffset`); with nobody held
  the last held one leads (`Leader()`).
- **Death:** a hero who falls below does NOT rise below -- he rises at the Town Hall, in the town:
  `Hero.Update` keeps counting but never `Rise()` while `HoldWhereLeft`; `DungeonTravel.Leave`
  writes `fallen` + `respawnRemaining`. Every hero fallen -> `DungeonMode.Update` takes the party
  up (`LeaveWhenAllFall`, false in the tests). Every death costs a LEVEL (sandbox and dungeon; not
  Invasion -- not asked yet); skills lock again by themselves (`HeroSkills.IsOpen` reads Level).
- **Going down:** only heroes within reach of the mouth (`DungeonEntrance.IsNear`,
  `DungeonTravel.Enter(id, level, from)`). The entrance card shows the King/Queen/Captain faces
  with «Доступен» / «Далеко» / «Пал».
- **Back up:** the camera looks at the mouth (`GameSessionIntent.DungeonMouth` ->
  `LookAtAfterLoad`, jumped 3 frames after `GameSaveController.Start`).
- **Camp and shop:** a tap with a hero held is no order -- the card opens (`ArmyOrderInput`);
  hiring and buying work from anywhere below; a gnome hired far away runs to the party (leash).
  Held, the camp shows a FocusHireBar like the Barracks (`Kind.HireCamp`): gnome portraits
  (`UI/Icons/PortraitGnome*.png`, render_portrait.py now takes a `frame` size) with prices.
- **Passability:** the NavMesh was cut by low relief bumps (a reach map showed 4 086 such cells).
  `MeshMapApplier.AddLowRelief` adds relief triangles wholly under 0.3 m as walkable source in a
  dungeon. Diagnostic: `DungeonTests.Diagnostic_ReachMap` (Explicit) -> playtest_shots/dungeon_reach.png.
- **Lake:** the bridge is gone; `dungeon1.py spider_statue` -- a stone spider queen on a rock
  islet with purple eyes; a `statue` spot; `Dungeon/DungeonStatue` hangs a big turning crystal with
  a halo over it. Statue is small (~1 cell tall) -- maybe enlarge.
- Camera distance 12 -> 10 (economy 277). Way-out tap box 3 x 2.5 x 2.5. Map-edge darkness hugs
  the edge (0..1 cell, `_MapEdge` 1). Crystal notice shows `Res_UpgradeCrystal`. No build menu
  below (`BuildingPlacerUIVisibility`).
- **Open:** the way-out look (options offered, awaiting choice); old box rocks (screen 9) not
  found -- Stone1-10 are crags; asked which map.

## Playtest round 5 (2026-11-02)
- WAY OUT = STONE STAIRS (the user's option 2): `dungeon1.py mine_exit` -- 4 dressed steps
  (`EXIT_STEPS/EXIT_STAIR_RISE/RUN`) with low side walls climb to the mouth raised in the rock's face,
  the mouth filled with daylight (plaster_white, a white heart), a torch either side (DungeonFire spots),
  a `day` light. The map was regenerated and re-exported. The opening is no white wall (the user,
  2026-11-02): gold rim fill, window_light heart, and `Dungeon/DungeonDaylight` -- a warm yellow halo
  (CrystalHalo material, pulled to the camera) at the `light_day` spot.
- Decor stones on slopes sit on the LOWEST ground under their foot (`decor_z(kind, x, y, sc)`,
  `CRAG_FOOT_M`) -- they hung by more than half their size. Stones now the Stone1-10 shapes (terrain.py).
- Fighting below: heroes and gnomes pick a fight within `dungeon_engage_factor` (0.6) of their own
  radius and never at what the NavMesh says is behind rock (`NavMesh.Raycast`); shooters' reach x
  `dungeon_ranged_range_factor` (0.5) -- `SoldierUnit.EngageRadius/UnitAttackRange`.
- A move order beats aggro: `SoldierUnit.Marching` (set by Hero from `_onMoveOrder`) -- no new fight
  until he gets there.
- Melee reach counts the target's body (`BodyRadius`, its collider's half-width - 0.3): gnomes stood
  by the spider queen not striking.
- Queen's «Выстрел» outside Invasion = a strong shot, her blow x sandbox `skill_queen_shot_power` (4,
  +0.2/level); text `#skill_queen_shot_desc_sandbox` (HeroSkillBar.DescriptionKey). Invasion unchanged.
- A boss pays RESOURCES too (crystals and coins as before): `dungeon_boss_wood/stone/gold` (150/150/50) x level, «+N» popups over it incl. the crystals (`ResourceGainPopups.ShowCrystals`)
  (and «+coins» over every monster), carried into the town on the way out (`DungeonMonsters.ResourcesEarned`).
- Every creature shows its health bar once it fights (OrcUnit.StrikeAt / SoldierUnit.TickAttack), heroes too.
- Back from below: the start island is revealed again once the map is built and the hall stands
  (`MeshMapApplier.RevealStartIslandOnceTheHallStands`); a hero rising in the first 8 s of a load does
  not take the camera (`HeroPortraitController.ShowReturn`), and LookAfterLoad jumps to the mouth again at 0.5 s.
- Hire camp: tapping it HOLDS it like the Barracks (gnome portraits + prices in the FocusHireBar); the
  text card only from its «!». Shop buttons read «30 + coin».

## Playtest round 6 (2026-11-02; no tests run -- the user asked for a general run later)
- The way out's opening is its own Blender object `Dungeon-1-ExitGlow` (export_map OPTIONAL), loaded
  as `MeshMapDefinition.ExitGlowPrefab` and drawn UNLIT flat warm yellow by `DungeonDaylight.LightAll`
  (a lit yellow face read as a wall); the halo over it is 5 wide.
- Hire camp: NO «!» and NO card -- `DungeonHireCard` deleted; holding the camp shows only the gnome
  buttons (`FocusHireBar.CampHeld` no longer needs DetailsShown). Keys `#dungeon_hire_title/count/
  stats/buy` are unused now (left in the sheet).
- The LAKE STATUE is no reward and has no crystal (the user, 2026-11-02, reversing a first try):
  `DungeonStatue` = a violet aura with green sparks breathing round the stone (CrystalHalo, its _Size
  pulsed). A hero within 2 cells of a lake's water opens the WHOLE lake and 2 cells round it
  (`DungeonFog.FindLakes/SeeLakesNear`; the lake = cells under the water mesh, rasterised -- its bed
  is ground, so `IsWaterCell` cannot see it; `MeshMapApplier.WaterInstance`).
- The crystal chip on the top bar redraws when the count changes (`ResourceHUDController.OnCrystalsChanged`);
  it used to wait for the coins to move.
- Archipelago: the level-1 entrance stands on the START island, >= 22 cells from the Town Hall's spot
  (`DungeonEntrances.Place`, `AwayFromHallCells`). Since 2026-11-03 (the user: «поставь здание
  подземелья 2 и 3 уровня на стартовом острове») levels 2 and 3 stand there too, each further from
  the hall (`freeOnGrid` keeps 3 cells between mouths); what finds no room goes to other islands.

## Files
- Level 2: `Tools/blender/dungeon2_plan.py` (+ `dungeon2-plan.json`), `dungeon2.py` (build + checks);
  `Scripts/Dungeon/DungeonLevers.cs`, `DungeonWake.cs`; `Scripts/Maps/DungeonFloor.cs`,
  `DungeonWaypoints.cs`; `Combat/NavRoute.cs` (asks DungeonWaypoints on a partial path).
- Monsters: `Tools/blender/monsters.py`, `dungeon_lairs.py`; `Scripts/Dungeon/DungeonMonsters.cs`,
  `DungeonMonsterPowers.cs`; `Combat/OrcUnit.cs`, `OrcRaidManager.cs` (models, boss controller),
  `FigureAnimator.cs`, `SoldierUnit.cs` (Stun/Slow); `Editor/SetupProjectOrcs.cs` (model wiring).
- `Tools/blender/cave_entrance.py`: mountain, tunnel, the 4 cave proposals; importable, `main(models, row_y)`.
- `Tools/blender/mine_entrance.py`: MineWorking / MineAbandoned / MineHeadframe.
- `Tools/blender/crypt_entrance.py`: the rejected ruins.
- All are in collection «Подземелье» of Buildings-Size1-Models.blend. None are exported.

## Traps
- **A long NavMesh path in a big dungeon comes back PARTIAL though the way exists** (the search's
  node budget; proven: camp->A complete, A->B complete, camp->B partial). Never read a partial path
  as "walled off" there -- route through `NavRoute` (DungeonWaypoints) or chain legs.
- A NavMeshObstacle carves a few frames after it is made: wait before asserting a door blocks.
- A monster needs THREE places or it silently falls back: the `mon_*` sheet rows, `BalanceConfig`
  `MonsterIds`, and `OrcRaidManager.MonsterModel` + `SetupProjectOrcs` wiring.
- **SCALE:** the game's unit is one MAP CELL. A hero is 0.6-0.8 cells tall; a level-1 building is
  1 x 1-2 cells and 1.2-1.5 tall. Talk sizes with the user in CELLS (the user, 2026-10-31).
  - Content was built for a 1.8 m person, so it is shrunk by `SCALE = 0.44`:
    - `dungeon1.py`: every mesh, every Empty and the decor CSV;
    - `dungeon_camp.py`: `orcs.scale_since`.
  - The dungeon is therefore **88 x 88 cells** (the user chose this over 200 cells with small
    props). Passages are ~1.3-2.8 cells wide; the walk to each boss is ~300 cells.
  - The hire camp is 4.2 x 4.0 x 1.4; the shop is 3.0 x 3.0 x 1.35.
  - The plan json and the build code stay in build units (0..200). Only the output is scaled;
    `scene["dungeon_half"]` = 44 tells `dungeon_preview.py` the size, so preview spots are given
    in CELLS 0..88 (`name:x:y[:camera distance]`).
  - The scaling loop scales mesh data AND object locations. Objects placed off the origin, like the
    camp buildings, would otherwise stay at their build-unit spot.
- The terrain hides everything below z=0. A way down must live inside a raised mountain or
  platform, with a black floor at z≈0.03.
- `stone_*` colours draw dressed courses (bricks) unless overridden.

## Open
- Monsters asleep off screen: measure the gain on a phone.
- Per-level dungeons (each entrance its own map and bosses), the level label over the mine.
- Elite monsters: an idea, not now.
- IDEA (the user, 2026-11-02): chests in the dungeon with ordinary resources, bonuses as the player goes through.

- 2026-11-02: the «Уровень N» plaque hides while the dungeon's card is open (DungeonLevelLabel.LateUpdate checks DungeonEntranceCard.IsOpen) — the card says the level itself.
- 2026-11-02: the entrance card shows «Можно найти:» + a crystal icon (row CanFind, hidden on the way out; key #dungeon_can_find, localization row 542).
