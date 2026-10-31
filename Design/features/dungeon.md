# Dungeon (подземелье)

> Status 2026-10-30: DESIGN. Entrance model chosen (Blender only, not exported); the dungeon itself
> not started. Read with: maps-and-terrain (map spots), sandbox-run-systems (heroes).

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
- The terrain hides everything below z=0. A way down must live inside a raised mountain or
  platform, with a black floor at z≈0.03.
- `stone_*` colours draw dressed courses (bricks) unless overridden.

## Open
- The user's answers to the list above.
- Export MineAbandoned with an icon, put it on a map spot, tap → hero enters (dungeon scene a stub).
