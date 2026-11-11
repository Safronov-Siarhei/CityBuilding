---
name: project-pending-2026-11-02
description: "Playtest list 2026-11-02 (round 5) — the user's items to do \"tomorrow\": stone-stairs exit, living water, orc «!», hp bars for all, shop/hire card icons, dungeon aggro/Queen range, floating crags, Queen skill 1, boss reward, grey cube + fog after a dungeon wipe, less fertile land; chest idea"
metadata:
  node_type: memory
  type: project
  originSessionId: 0f587829-fe18-4bb5-a5b7-742486c4675e
  modified: 2026-11-01T21:05:16.055Z
---

Given 2026-11-02; the user then said «1 + 2, можешь начинать» (water options 1+2). ALL 13 BUILT same day, commit 1361e9c6 (126 tests, 1 test updated for the held camp); user has NOT seen them in game. Details in Design/features/dungeon.md «Playtest round 5» and archipelago-and-ships.md. Item 14 (chests) = idea only, in the dungeon card's Open.

1. Dungeon way out = option 2, STONE STAIRS going up, torches, daylight opening at the top (closes item 16 of [[project-pending-2026-11-01c]]).
2. Water round the islands is static, not alive: edges should sway, or offer other options (ask/propose: shoreline foam wobble, normal-scroll ripples, vertex waves on the coastal band, sparkles).
3. Orcs that land on the Town Hall's island get red «!» markers like in Invasion.
4. Every creature (orcs, monsters) shows an Invasion-style hp bar once it enters combat — check whether orcs already have it.
5. Screen 1: potion shop buttons show «30 + coin icon» instead of «Купить: 30».
6. Screen 2: the gnome hire camp card still the old text list — the user expected the new barracks-like format with soldier ICONS (the FocusHireBar HireCamp exists; the card itself must change / or open the bar).
7. Dungeon: heroes' auto-aggro radius too big — the Queen shoots through rocks and runs to enemies; Queen's attack range ~HALF.
8. Screen 3 (said before): decorative crags/rocks may hang in the air by more than half their size — fix (sink to ground / sample lowest point). Dungeon map.
9. Queen skill 1 (kills any enemy in one shot) too cheaty: in the sandbox/dungeon replace with just a STRONG SHOT; bosses die in one shot now. AND killing a boss gave NOTHING — boss reward not finished, do it.
10. A hero sent to a point went another way and fought monsters first, then went to the point: a move order must take priority over auto-aggro.
11. Screen 5: Queen shoots the boss while melee gnomes stand next to it and don't fight (probably out of their attack reach vs the boss's big size — use the target's radius).
12. Screens 6-8: after the party is wiped in the dungeon, back on the main map there is a GREY CUBE (a gatherable rock with no model = RockSpawner cube fallback: rocks restored from save before/without _rockPrefabs on the return trip) and the Town Hall's island is FOGGED (RevealStartIsland not run on that load path).
13. Too much fertile land on the archipelago: should be rare.
14. IDEA (record only): chests in the dungeon holding ordinary resources, bonuses as the player goes through. Also noted in Design/features/dungeon.md.

**Why:** the user's playtest feedback; they expect every item done next session.
**How to apply:** start here next session; mark each DONE with its commit; see [[project-dungeon-entrance]], [[project-archipelago-map]].

ROUND 6 (2026-11-02, commit 750f6be9, NO tests run -- the user: «пока что тесты не делай ... после проведёшь общие тесты»):
no walking over water (LineCrossesWater, NavRoute + CitizenAgent), level-1 dungeon on the start island,
exit opening unlit (ExitGlow object), hire camp = gnome buttons only (DungeonHireCard deleted, DungeonTests
updated to FocusHireBar.HireGnome but not run), lake statue: reward REVERTED by the user (f811d42d) -- no crystal, a violet aura on the statue, the whole lake opens when a hero reaches any shore; crystal chip
redraw fix. PENDING: a GENERAL test run when the user says so (expect ArchipelagoTests on dungeon entrances
and orc-wall/route tests to need a look after the NavRoute change).
