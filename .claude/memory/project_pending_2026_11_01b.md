---
name: project-pending-2026-11-01b
description: "The user's second playtest list of 2026-11-01: ALL BUILT and pushed (28404e79, 5a04f64c) — dungeon DARKNESS with a smooth parchment-like edge, Zzz sleepers waking on approach, hire camp moved out of the lake, ridge tent, no day plaque, shoal spacing; the 5-crystals question answered (account-wide wallet). User not seen it in game."
metadata:
  node_type: memory
  type: project
  originSessionId: 0f587829-fe18-4bb5-a5b7-742486c4675e
  modified: 2026-11-01T18:15:12.253Z
---

Round 2 of 2026-11-01. All of it is built (see `Design/features/dungeon.md`). The user has not seen it in game yet.

1. Shoal spacing: `fish_shoal_min_spacing` (14 m). DONE.
2. Dungeon DARKNESS (`Shaders/DungeonDark`, a depth-read full-screen pass). DONE.
   - The user then said «темнота квадратами, сделай как пергамент». Now it has a wide blur plus a
     drifting noise edge.
3. The empty day plaque in the dungeon is hidden. DONE.
4. "5 crystals at the start": they are an account-wide PlayerPrefs wallet (paid currency).
   The user said KEEP IT SO (2026-11-01).
5. The map edge: the void past it is now black darkness. DONE.
6. The "pyramid" was the lake hall's tent in the dungeon, not on the archipelago. It is now a ridge
   tent; the map was rebuilt and re-exported. DONE.
7. The hire camp was in the lake: the FBX offset was in build units. `MeshMapApplier.StandOnSpot`
   fixes it. DONE.
8. Zzz over sleepers; they wake when a hero comes within 6 cells. DONE.

Round 1, item 4 (the floating mountain piece): the user said FORGET IT for now (2026-11-01).
The user LIKES the darkness. Last tweak: the map's edge was a straight line; the darkness now eats
a wobbling band into it (`_MapEdge`, 3 cells).

## Camp decisions (2026-11-01, round 3)
- Gnome hirelings:
  - can be hired without end, at most 5 at a time;
  - models APPROVED («устраивает»), exported, and the hire camp BUILT 2026-11-01 (13a369db):
    card with 3 gnomes, N/5, they follow the hero, wait below between visits. User not seen it.
- Potions:
  - a bought potion is NOT drunk at once: its icon sits under the heroes' portrait (top right),
    and tapping the icon drinks it;
  - potions work in the dungeon only and never go to the town's inventory.
  - BUILT 2026-11-01 (9517d284): shop card, bag, icons under the portraits, party buffs; unused
    potions saved for the next visit (my choice, tell the user if asked). User not seen it.
- «Уровень N» over a held dungeon mouth: BUILT (81d79329).
- Dungeons of levels 2 and 3: later.
