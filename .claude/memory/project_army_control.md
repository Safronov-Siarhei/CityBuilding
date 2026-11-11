---
name: project-army-control
description: Army control by squads (concept agreed 2026-11-05, not built) -- card Design/features/army-control.md
metadata:
  type: project
---
The user designed mobile army control by SQUADS on 2026-11-05 (4 mock-ups): army icon under heroes,
Garrison (auto, stays home) + up to 5 player squads with crests, squad window (pauses), one selection at a
time, machines apart, ship capacity 8-10-12-14-16 per level, no splitting. Everything is in
Design/features/army-control.md. STAGE 1 BUILT 592efc49 2026-11-05 (Garrison, 5 squads, machines alone, army icon + squad row, round cancel, tap selects squad); STAGE 2 squad window BUILT 24c98c9e (runtime-built UI/SquadWindow, refill from Garrison at home); STAGE 3 behaviours+formations BUILT 1cd9f676 (Guard round, Advance, FollowHero; Square/Triangle/Scatter by rank). STAGE 4 ships BUILT (Lab ship levels 2-5, places, whole-squad boarding). STAGE 5 crests approved + in the game. ALL 5 STAGES DONE 2026-11-05; user has not played it. Open: mixed squad has no common march pace. Formations agreed: Квадрат/Треугольник/Врассыпную; behaviours incl. За героем; ship capacity transport 8-16, battleship 12-20, levels in Lab.
Related: [[project-unit-roster]].

2026-11-05 (end): crest commit afc05838f rebased onto the other device's Tutorial commits; 8e4c171a1 makes the tutorial recruit join squad 1 and TutorialDirector check its mode in Start (Awake order trap). TutorialTests+ArmyCombatTests 15/15 green. Not done: a general test run after the rebase; common march pace for mixed squads.
