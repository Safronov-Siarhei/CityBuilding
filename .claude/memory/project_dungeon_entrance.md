---
name: project-dungeon-entrance
description: "Dungeon work started 2026-10-30 — the entrance is a small grassy mountain with a cave; four proposal models in Blender, user to pick; the dungeon itself is a separate scene the HERO enters."
metadata:
  node_type: memory
  type: project
  originSessionId: 0f587829-fe18-4bb5-a5b7-742486c4675e
  modified: 2026-10-30T20:05:38.633Z
---

User 2026-10-30: dungeon = a SEPARATE SCENE the player's hero walks into; first make only the ENTRANCE on the map.
- Crypt ruins (Tools/blender/crypt_entrance.py, CryptEntranceA/B/C) were shown, then REJECTED: the user wants «небольшая гора со входом-пещерой, зелень, декор».
- Tools/blender/cave_entrance.py builds 4 proposals in collection «Подземелье», 4x4 cells, mouth faces -y:
  CaveEntranceMine (timber props, rails, cart), CaveEntranceLair (fangs, glowing eyes, bones, torches),
  CaveEntranceShrine (carved portal, rune stone, statues), CaveEntranceFairy (crystals, brook+pool, glowing mushrooms).
  Shown 2026-10-30: user LIKES THE MINE but finds it dull -> Tools/blender/mine_entrance.py (imports cave_entrance) builds MineWorking (winch, ore veins, loaded cart, crates), MineAbandoned (boarded, broken beam, skeleton, cobwebs), MineHeadframe (hoist tower, lantern string, spoil heap, campfire); user PICKED MineAbandoned (2026-10-30) but disliked the flat black hole -> cave_entrance.tunnel() + mountain(hollow=True): rings narrowing/darkening inwards, inner props, rails in, lantern deep inside, small black at the back. User OK'd the tunnel. Not exported, not in the game.
- Rock look: stone_* colours draw as BRICK courses by default; the user said the mountain looked like bricks, so cave_entrance.py sets detail.OVERRIDE stone_mid/stone_dark -> PLASTER (rough). Keep stone_light only for carved pieces.
- Trap: the terrain hides anything below z=0, so a way DOWN must live inside a raised platform/mountain (a black floor at z≈0.03).
- Ideas offered for the game side: mist drifting out of the cave, flickering torches/crystals, a marker when the hero is near.
Next: user picks → polish, export FBX (Models/Buildings), icon, place on map, tap → hero enters (dungeon scene a stub). See [[project-maps-roadmap]].

2026-10-30 user rule: the dungeon is ONE level with monsters and a boss -- no themed tiers/floors. Everything else (pause city? squad? layout? rewards?) still open; card Design/features/dungeon.md holds the list. User went to sleep; continue the discussion from the card.
2026-10-31: city PAUSED while inside; ONLY the hero goes in. Layout + rewards: user describing own ideas -- listen, don't re-propose.
2026-10-31 concept + answers are in Design/features/dungeon.md (READ IT): 200x200 labyrinths (test 100x100), 3 per map by distance, several heroes + up to 5 dungeon-only gnome hirelings following the controlled hero, shared treasury, potion shops, respawn only on re-entry, Blender-made layouts (no generator), crystals 1/2/3 per lvl 3/4/5 and scarce (dungeons 30/50/80, orc main base 60, bot city 60; ads/IAP alternative), level label over the entrance.
Test map v1 Tools/blender/dungeon1.py (+dungeon_preview.py) shown 2026-10-31 -- awaiting the user's style verdict.

2026-10-31: the labyrinth PLAN comes first (dungeon_plan.py, seed 7 approved, saved as Tools/blender/dungeon1-plan.json); dungeon1.py v3 builds the 200x200 map FROM that json (~30 s). Shown; verdict pending. Too heavy to export as is (~410k tris with decor).

**Phase 2a (2026-10-31, pushed 75522353):** 3 MineAbandoned entrances on the archipelago (DungeonEntrances.Place, levels 1-3 by distance from start, all -> Dungeon1); hero at the mouth opens DungeonEntranceCard «Войти». Level not passed down yet. Next to agree: monsters+bosses, camp hiring/shop, rewards, per-level dungeon maps.

**Monsters agreed 2026-10-31 (card section «Monsters»):** rats, bats, spider hunter, skeleton miner, kobold blaster, small stone golem + queen/ore golem; hold their lair; levels = same monsters × sheet multipliers; elites = later idea; queen's wall phase rejected, replacement open. Next: Blender models shown beside a hero.
**Models v1 shown 2026-10-31** (monsters.py, collection Подземелье-монстры, game size, King 0.53 tall): verdict pending; queen half-health = cocoon (chosen).

2026-10-31 MONSTERS IN THE GAME (commits 857cc0d6 + eecc0e99): 9 kinds + 2 bosses as OrcUnit kinds, lairs from dungeon_lairs.py spots, each with its animation (bat wings, golem slam, skeleton collapse/get-up), queen cocoon + golem armour, coins/crystals/slain-boss record go home. Sheet rows mon_* and dungeon_* added and verified. Details in the card Design/features/dungeon.md. The user has NOT seen them in the game yet. Next options: patrols, crystals/rewards in rooms, camp hiring + shop, monsters sleeping far from the camera.

2026-10-31 later (commits 050a1d04 + 1cea4a4a): monsters SLEEP off the camera's frame and away from heroes (OrcUnit.Asleep, dungeon_monster_wake_margin). The WAY OUT redone at the user's request («развёрнут к игроку, дать стиль»): a mine adit in the camp's NORTH wall facing the camera, MineAbandoned style but whole (mine_exit/find_exit in dungeon1.py); map half size now from an `edge` spot. User saw only Blender previews; a slanted slab of some passage prop shows behind the brow — check in game.

2026-10-31 way out v3 (commits 642fa596 + 4130815b): the timbered adit was rejected («полная фигня, как будто в пустоту, куча ненужной геометрии»). RULE: the way out is a cave in a SOLID ROCK, nothing round it. Built as the surface mountain layout 2.6x in front of the camp's north wall, rough rock pattern (rough_exit_rock). Blender previews only.

2026-10-31 brightness: user found the dungeon duller than the surface; option 1 chosen «пока что» — palette lifted to the surface's brightness (0.52 -> 0.66, Map 3 0.65). Option 2 (slightly darker cave mood) parked.

2026-10-31 guards+patrols: user rule «каждый кристалл должен быть защищён монстрами»; built guard bands at 7 reward rooms + 6 lone crystal spots, 5 patrols, bigger lairs, lake lair (~140 monsters). Crystals themselves NOT in game yet. Marked top views were the way to agree placement (scratchpad mark.py over dungeon_preview top).

2026-10-31 crystals built: 20 floating crystals (2 per reward room, 1 per lone spot) + bosses 10 = 30 on level 1; hero takes by walking up (guards need not die), saved as dungeonCrystalsTaken, never respawn. Next: camp hire/shop.

2026-11-01 crystal look v2: user wants crystals bright, glowing, juicy, gently swaying — 'a special thing, paid content'. Built own shaders Crystal + CrystalHalo (self-lit gradient, rim, running light band, facet flashes, halo with sparkles), floor glow, flare on pickup. In-game photos: run.ps1 -Graphics with a temporary PlaytestCapture test (playtest_shots/), Blender cannot show game shaders.
2026-11-01: crystal look v2 APPROVED («мне нравится»).

- 2026-11-04: dungeon LEVEL 3 «Огненные глубины» COMPLETE in the game: monsters3.py (8 all-new incl. Dragon, LavaTitan), DungeonMonsters level-3 lairs, dragon breath+flight / titan rise+shed in DungeonMonsterPowers, DungeonTide, DungeonBridges, Dungeon3 POPULATED. Built; user has not seen it; details in Design/features/dungeon.md «Level 3 COMPLETE».
