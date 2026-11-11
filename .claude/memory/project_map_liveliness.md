---
name: project-map-liveliness
description: Main-map liveliness 2026-11-02 — wind + clouds BUILT (a04c933e); wildlife models approved and BUILT (Maps/Wildlife.cs); none seen in game; glints/farm animals marked only; POIs need model approval; biomes/neutral ships not now.
metadata:
  type: project
---
User 2026-11-02: «основная карта безжизненная», answered my 5-point proposal:
1. Wind — BUILT (commit a04c933e): `_WIND` in BuildingDetail.shader, trees + map props sway. Clouds — user rule: shadows ONLY together with the clouds visible at max zoom; BUILT (CloudLayer.cs, CloudField.hlsl). Sun glints on the sea: MARKED, don't build.
2. Wildlife — YES (gulls, bird flocks off forests, deer/rabbits, jumping fish, rare whale/dolphins). Chickens/cows at farms: MARKED, don't build.
3. Points of interest (wrecks, ruins, landmarks) — liked, but MODELS NEED APPROVAL FIRST.
4. Island biomes — not now. 5. Neutral ships — not now.
Not seen in game; no tests run (user: one general test run later). Card: Design/features/maps-and-terrain.md «Liveliness».

Wildlife models made 2026-11-02 in Tools/blender/fauna.py (collection «Живность» in Buildings-Size1-Models.blend: FaunaGull/Bird/Deer/Rabbit/Fish/Dolphin/Whale, 130-540 tris, jointed parts WingL/R, Leg*, Head, Tail), APPROVED by the user and BUILT same day: exported to Models/Fauna, Maps/Wildlife.cs drives them (see card «Liveliness»); not seen in game, no tests.

**How to apply:** never build glints/farm animals unasked; POI models must be shown for approval first; general test run still pending.

Playtest 2026-11-02 (commit 87cdfd1a): clouds were cut in a rectangle by the fog blanket (same queue) -> clouds Transparent+80 + soft round rim; cloud shadow 0.28->0.12 (user: «как дым», must only slightly darken). USER RULE: animals have FIXED homes/routes from the map layout, switched off out of view, never re-placed (Wildlife.LayOut). Dungeon level plaque hides while its card is open. Open question asked: islands still empty / little reason to settle them — proposals given, awaiting the user's pick.

Islands (user 2026-11-02, answering my 5 options): (1) YES — island riches BUILT 601118be (IslandRiches.cs: nearest iron, next copper, farthest gold, rest by shape stone/fertile/iron/copper; OVERRIDES old «No island per ore»). (2) caches: models CacheChest + CacheRuin APPROVED (treasures.py, not exported). Guards must be a NEW SEA-MONSTER FACTION (like WC Naga, but original) — concept proposed 2026-11-02, awaiting answers; the main-map wildlife stays decorative only. (3) LIGHTHOUSE BUILT 601118be: 2nd mandatory building (free, instant, archipelago only), starts unlocked (lab = levels only), reveals circle lighthouse_light_cells_1..5 = 45..80 (medium island -> large island). (4) and (5) not now. Dungeon card got «Можно найти: [crystal]» (56c6bacc).

2026-11-02 later: sea faction composition APPROVED (crab guard, eel spearman, priestess, boss hermit crab in a ship hull) — models in Tools/blender/tide.py sent for approval; they ONLY guard caches; ruins stay STONE (no corals); faction NAME still open (user asked for more options). One-mine-per-vein already enforced. Water build tab icon = Lighthouse.

Faction NAME = «Стражи Глубин» (user pick). Crab APPROVED; eel/priestess/hermit-in-hull REJECTED («скучные»). Round 2 sent: hammerhead shark, octopus shaman, anglerfish (alt ranged), teal sea serpent boss on gold hoard (tide.py) — awaiting approval. User likes striking silhouettes, not plain humanoids.
Round 3: shaman = human body + octopus head with tentacle beard (user's ask, ref Davy Jones), angler rejected -> pufferfish urchin-thrower. Shark/serpent not commented yet. Awaiting approval.
Round 4 (2026-11-02): ALL FIVE APPROVED by the user (crab, hammerhead, octopus-head shaman, pufferfish, serpent boss), with fixes for floating parts done (checker script: scratchpad check_float.py, BVH signed distance; flat bottom caps can give false 'inside'). Next: export Tide* + caches and build guarded caches in game.
BUILT 2026-11-02: guarded caches + Стражи Глубин in game (TideCaches.cs, OrcKind 20-24, economy rows 313-378, save tideCachesLooted) — not seen in game, no tests (general test run still pending, now also covering caches/lighthouse/island riches/wildlife/clouds).
