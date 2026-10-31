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
- ONLY THE HERO goes in -- no soldiers (the user, 2026-10-31).
- Layout and rewards: the user is describing their own ideas (2026-10-31); wait for them, do not
  propose the shuffled-rooms / crystals defaults again.

## Proposed, NOT agreed yet (ask, don't build)
- Layout: shuffled hand-made rooms each visit (recommended), or one fixed labyrinth.
- Rewards: crystals (the Laboratory's lvl 3-5 currency), gold, relics with permanent town bonuses;
  hero gear later.
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
