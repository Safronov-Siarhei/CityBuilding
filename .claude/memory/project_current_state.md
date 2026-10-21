---
name: project-current-state
description: "Where CityBuilding stands NOW (2026-10-21): project split into feature cards in Design/features/; work happens on TWO devices; Invasion mode in progress; what waits on the user. Short on purpose — per-feature state lives in the cards."
metadata:
  node_type: memory
  type: project
  originSessionId: 00ec56af-41af-4e20-aa76-c5f36bc84d70
  modified: 2026-10-21T17:02:59.003Z
---

**Since 2026-10-21 the project's knowledge lives in feature cards: `Design/features/README.md`
(index) → one card per feature** (status, the user's decisions, code/data/tests, traps, open items).
See [[feedback-feature-cards]]. The long chronological notes that used to be here are frozen in
`Design/features/archive/` (in git).

**Two devices.** The user also works from another machine (paths like `G:/Fork/CityBuilding`); on
2026-10-21 origin had 55 commits this machine had never seen. `git fetch` at the start of a task and
read new commit messages; the cards were brought up to `c6627577`.

**Latest (2026-10-20/21, mostly from the other device):** Invasion mode («Нашествие») started — its
own log `Design/Invasion.md`; pen-sketch placement look is the only one; roads/fences piece by piece;
1×1 gate on roads; ore veins; node stocks; citizen names + citizens tab; Maps 1–2 deleted, Map 3
redrawn; new main menu with mode cards and a live scene; easy start.

**NEXT SESSION (user, 2026-10-21): work on the Invasion mode.** Start from the invasion-mode card and
`Design/Invasion.md` («Дальше по плану»); next planned step is orc WAVES on the three lanes (breather
with a timer, wave number, gold for kills and waves). `git fetch` first — the other device works on it too.

**Waiting on the user (each also sits in its card):** what closing a portal should buy and the raid
roster per portal level (orc-raids-and-portals); GPU instancing (performance-mobile); zoom floor
5 → 8 m (camera-and-input); meat/fruit chains (production-chains); Invasion next steps (invasion-mode).

**2026-10-21 (`0e2ae107`):** SetupProject.cs and OrcRaidManager.cs were split into partial files by
feature (map in the build-and-test card). If the other device had uncommitted edits in those two
files, its next pull conflicts: re-apply the edit to the method in its new file.

Nothing since the main orc base has been on a phone. Blender `.blend` files are outside git.
