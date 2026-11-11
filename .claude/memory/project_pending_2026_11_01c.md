---
name: project-pending-2026-11-01c
description: "Playtest list 2026-11-01 round 4 (dungeon party, hire window, passability, lake statue, sandbox fog/heroes) — the user's 14 items and their state"
metadata:
  node_type: memory
  type: project
  originSessionId: 0f587829-fe18-4bb5-a5b7-742486c4675e
  modified: 2026-11-01T19:43:24.574Z
---

The user's playtest list of 2026-11-01 (round 4, after the hire camp + potion shop). State per item:

1. Sandbox start: the whole STARTING ISLAND visible, not under fog.
2. The dungeon's way out: tap box too wide — shrink it.
3. Hire camp window like the BARRACKS window: gnome icons (portraits). Hiring works with no hero
   near; the hired gnomes run by the shortest route to the hero.
4. With a hero held, a tap on the camp or the shop is NOT an order: the hero does not walk there,
   the card just opens.
5. Several heroes below: the player controls ONE (any, switchable); the others FOLLOW him. A hero
   who dies rises at the TOWN HALL (not at the dungeon's way out — that idea was bad: death spam).
6. Going down: only heroes NEAR the entrance go down. The entrance card shows the player's heroes'
   icons with «Доступен» / «Далеко».
7. Sandbox (main map only): a hero let go STAYS where he is and patrols a small radius round
   himself (no return to the Town Hall after 10 s). Dungeon keeps its own rules.
8. Dungeon camera 15-20 % closer.
9. Passability: the left passage (screen 3) and two crystals (screens 4, 6) unreachable; check the
   whole map, make some decor walkable, every corner reachable; the King must not hunt for
   non-existent routes first.
10. Remove the bridge across the middle of the lake; put a MONSTER STATUE in the lake's centre with
    a pretty effect.
11. The «Найден кристалл улучшения» notice: its icon must be a crystal.
12. Map edge darkness: too far from the edge — wobble within half to one cell of the edge.
13. Back from a dungeon: the camera on the dungeon's entrance, not on the Town Hall.
14. Old shapeless box rocks on the sandbox map (screen 9): replace with the new rock models.
15. The building panel (category icons, bottom centre) must not show in the dungeon.
16. The way out looks like a plain black hole: options offered (daylight inside + rays, stone stairs up, light shaft with a mine lift, magic arch); CHOSEN 2026-11-02: option 2 stone stairs (see project_pending_2026_11_02).
17. Every hero death takes 1 LEVEL off him (XP of that level gone); a skill whose level he drops below locks again.

STATE 2026-11-01 (commit b8a5d4b0): DONE 1-13, 15, 17 (17: sandbox+dungeon only, Invasion untouched —
CONFIRMED by the user: «пока что там не надо»). 14 DONE (archipelago, the user's answer): the decor
pebbles (4-sided boxes) and crags of every land map now drawn from Models/Decor/DecorRocks.fbx
(decor_rocks.py, Stone1-10 shapes). 9: low relief bumps made walkable (NavMesh), not yet confirmed in game. 10: statue
small, maybe enlarge. OPEN: 16 (way-out look:
awaiting the user's choice of option 1-4).

**Why:** the user's direct playtest feedback; they expect every item done.
**How to apply:** work through them, mark each DONE here with the commit; see [[project-dungeon-entrance]].
