---
name: project-pending-2026-11-01
description: "The user's playtest list of 2026-11-01 (9 screenshots): ALL BUILT and pushed the same day except item 4 (floating mountain piece, location unknown -- asked the user). User has not seen the dungeon-mode changes in game yet."
metadata:
  node_type: memory
  type: project
  originSessionId: 0f587829-fe18-4bb5-a5b7-742486c4675e
  modified: 2026-11-01T10:45:48.465Z
---

The user's list, 2026-11-01. Status:

- **Archipelago — DONE (39fc7a56):**
  - no fertile land near the coast;
  - storm-edge clouds ("pyramids") removed;
  - tapping a fish shoal rings it and posts a feed notice;
  - shoals spawn within 4 m of the shore;
  - the orc shipyard stands wholly on the shallows.
- **Item 4, "a piece of a mountain hangs in the air": OPEN.** Not found. The user was asked where it is.
- **Selection — DONE (ba47558e):**
  - a dungeon mouth or an orc building is selected like one's own: ring, build menu hidden, «!» opens the card;
  - a hero passing a dungeon gets a feed notice, and tapping the notice selects the dungeon;
  - a hero sent to a dungeon opens its card on arrival.
- **Portal — BUILT (de2cd608, 607af5d0):** the user chose A+B+D.
  - A: a whirlpool in the arch.
  - B: runes and a glow with embers.
  - D: a flare and a shock ring per wave.
  - The half-glow bug was fixed by pulling the glow towards the camera.
- **Dungeon mode (screenshot 9) — BUILT (24516c5c):** see the card Design/features/dungeon.md, section «Dungeon-mode rules».
  - The user has not seen it in game.
  - The camera distance (12) and pitch (50) are guesses; the user's screenshot was not available to match.
  - The inventory button still opens the town's «Запасы»; a hero inventory does not exist yet.

**Why:** a playtest of the archipelago and dungeon build.
**How to apply:** when the user reports back, tune the sheet keys `dungeon_camera_*`, `dungeon_reveal_cells` and the portal shader numbers first.
