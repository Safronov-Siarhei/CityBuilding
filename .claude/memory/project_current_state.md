---
name: project-current-state
description: "Where CityBuilding stands NOW (2026-10-21): the project is split into feature cards in Design/features/; latest work, what waits on the user. Short on purpose — per-feature state lives in the cards."
metadata:
  node_type: memory
  type: project
  originSessionId: 00ec56af-41af-4e20-aa76-c5f36bc84d70
  modified: 2026-10-21T16:17:54.556Z
---

**Since 2026-10-21 the project's knowledge lives in feature cards: `Design/features/README.md`
(index) → one card per feature** (status, the user's decisions, code/data/tests, traps, open items).
See [[feedback-feature-cards]]. The long chronological notes that used to be here are frozen in
`Design/features/archive/` (in git).

master after the split: see `git log` (the split commit is "Split the project into feature cards").
Tests: EditMode 446, PlayMode 212 (+4 graphics-only). Sheet == repo (2026-10-20).

**Latest shipped (2026-10-20):** ranged shots drawn (arrows/javelins, damage on impact); map picker +
Map 5 «Кальдера»; four placement looks built behind Classic.

**Waiting on the user (each also sits in its card's Open/Status):**
- pick a placement look (building-placement);
- what closing a portal should buy; raid roster per portal level (orc-raids-and-portals);
- GPU instancing for trees/rocks (performance-mobile); zoom floor 5 → 8 m (camera-and-input);
- meat/fruit chain design (production-chains); next designed map (maps-and-terrain).

Nothing since the main orc base has been on a phone; last APK 49.7 MB vs Telegram's 50 MB cap.
Blender `.blend` files are outside git (`D:/blender projects/Buildings3D/`).
