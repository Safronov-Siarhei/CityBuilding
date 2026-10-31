# Dungeon (подземелье)

> Status 2026-10-31: DESIGN. Entrance model chosen (Blender only, not exported); Dungeon 1 map v5
> (200 x 200, organic, from plan v3, halls furnished, props in chunks) built in Blender and shown, awaiting the verdict; nothing in the game yet. Read with: maps-and-terrain (map spots), sandbox-run-systems (heroes).

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
- **Over the entrance:** a label with the dungeon's LEVEL (1/2/3) when it is tapped.
- **Respawn:** monsters come back ONLY when the player leaves the dungeon and enters again (replaces
  «after N minutes»). Bosses never come back.
- **Bosses:** the spider queen and the ore golem are approved. They must be MUCH bigger than a hero.
- **Hirelings:** gnomes approved — the miner (melee), the crossbowman (ranged), the ore guardian
  (a stone golem tank), from «Лагерь гномов-старателей».
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
  - Shown 2026-10-31, verdict pending. They will NOT go into the dungeon map until approved.
- Measured 2026-10-31:
  - The Laboratory asks `research_crystals` = 1 crystal per level topic from level 3
    (`research_crystals_from_level`).
  - 46 buildings x levels 3/4/5 = 138, plus unit levels 3 ≈ 142 crystals for everything.

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

## Files
- `Tools/blender/cave_entrance.py`: mountain, tunnel, the 4 cave proposals; importable, `main(models, row_y)`.
- `Tools/blender/mine_entrance.py`: MineWorking / MineAbandoned / MineHeadframe.
- `Tools/blender/crypt_entrance.py`: the rejected ruins.
- All are in collection «Подземелье» of Buildings-Size1-Models.blend. None are exported.

## Traps
- **SCALE (found 2026-10-31):** the game's world unit is one map CELL, not a metre.
  - Heroes are 0.6-0.8 tall (HeroKing 0.79, HeroQueen 0.62, HeroCaptain 0.81); a level-1 building
    is about 1 x 1-2 cells and 1.2-1.5 tall.
  - The dungeon map and its camp buildings were made thinking a 1.8 m person, so next to a hero
    they are about 2.3x too big. A factor of 0.44 matches them.
  - The user is to choose: scale the whole dungeon (about 90 x 90 cells) or keep 200 cells and
    shrink only the props.
- The terrain hides everything below z=0. A way down must live inside a raised mountain or
  platform, with a black floor at z≈0.03.
- `stone_*` colours draw dressed courses (bricks) unless overridden.

## Open
- The user's answers to the list above.
- Export MineAbandoned with an icon, put it on a map spot, tap → hero enters (dungeon scene a stub).
