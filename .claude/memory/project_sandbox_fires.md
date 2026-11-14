---
name: project-sandbox-fires
description: Sandbox fires v2 DECIDED 2026-11-14 (random chance, water 8 cells, touch-only spread, torch orcs from hall lvl2, thunderstorm, bucket firefighters, brigade island-wide variant A, fire banner); BUILT 2026-11-14, setup green, not seen, no tests
metadata:
  type: project
---

Fires are dead in the game since decay went OFF (2026-10-25): chance was decay. The user redesigned them
2026-11-14 for the SANDBOX only; full rules in Design/features/hazards-fire-sickness.md «Sandbox fires v2».
BUILT 2026-11-14 after the user approved the torchbearer model and fire banner v2; code map in the card. Not seen in game, no tests.
Illness start delay becomes 10 min too.

**Why:** user's design; same shape as [[project-plague-cemetery]] and [[project-sandbox-healer-v2]].
**How to apply:** read the card section first; flag and decor DO burn (the user corrected me).

2026-11-14 round 2: old fire keys deleted (code/CSV/sheet); «build a brigade» notice opens the build menu on
it (BuildHints.LeadToBuild(id)); fire bucket ToolFireBucket APPROVED and IN THE GAME (FireManager.bucketModel, Water child while full).
Fire Brigade and Лечебница level 1 open from the start (starts_unlocked 1, the user 2026-11-14).
