---
name: project-pending-2026-11-02e
description: Playtest list 2026-11-02 round 10 (walls follow-up) — what is done, what waits for the user's answer (donjon, barbican, bastion, moat)
metadata:
  type: project
---
User's 6-item list after walkable walls (2026-11-02):
1. Wall order marker drawn under the wall → DONE 3252ff6f (flag on the walk).
2. Wrong upgrade icons for fence/gate/towers → DONE (re-rendered; Tower/Bastion 3-5 icons show the WALL look).
3. Barbican (=FortifiedGate) and Bastion (=FortifiedTower) "not made" → Barbican walk-wall draft rendered (keep_drafts.py), APPROVED + INSTALLED 51e5cefd, walkable (WallWalk.BarbicanId). Bastion: user said the old model showed -> now wears WallFortTower from lvl3 everywhere (lookAlways); not seen in game.
4. Hero XP only own kills → DONE (OrcUnit.KilledBy).
5. Town Hall → Donjon → APPROVED + INSTALLED 51e5cefd (Castle1-lvl1..5 + icons).
6. Moat → discussed only; user asked to explain 'blocking vs slowing' — explained; waiting for the choice. Not built.
**How to apply:** on the user's answer, install the approved drafts from Tools/blender/keep_drafts.py. Related: [[project-walkable-walls]].

MOAT decisions (user, 2026-11-02): option B (slows, never blocks). Citizens and soldiers are slowed too; with a road available they never path through the moat. Opened in the Laboratory for crystals (number not given yet), ONE level only. Placing a cell costs 25 coins. Open question the user raised: an orc in water "can't attack" -> a moat right against a wall would make the wall unbreakable; proposed fix: in the water an orc can't fight fighters but CAN hit a building it stands against. Waiting for the answer + crystal price before building.

MOAT BUILT c4e31bb4 (2026-11-02), variant A chosen (orc in water can't fight units, can break the wall it's against), 10 crystals, 25 coins (sandbox cost_coins), card = name + Demolish. Card Design/features/moat.md. Untested, not seen.

CROSSINGS BUILT e1d1d617 (2026-11-02): Bridge models (timber 1-2, stone 3-5, long legs) over moat/shallows; new Drawbridge 1x2 gate, leaves rise near orcs. Card section in Design/features/moat.md. Untested, not seen.

923e2c5f: living moat water (MoatWater shader), moat 2 draws/cell, Town Hall icons keep-only, level-icon fallback (Lighthouse), tide monster stats fix, hire camp «!» fix. GENERAL TEST RUN DONE: EditMode 460/460, PlayMode 359/378 (+1 flaky AWaveOrc_KeepsToItsWay passes alone).

## Round 11 (2026-11-03), BUILT a5550b45, not seen in game
- Moat water redone like the sea: dark still water, with the foam line and turquoise band only along the banks.
- Units wading the moat sink to the knee (`moat_wade_depth` 0.25).
- An army order onto a moat walks there instead of selecting it.
- The Tower uses its wall-tower look from lvl3 even beside a lvl1-2 fence.
- The Water Mill and the Dock are hidden.
- The whirlpool idea is recorded in [[project_whirlpool_idea]].
- 2331236a: units walk ON bridge/drawbridge decks (bridge_deck_height 0.22); no tests run, as the user asked.
- Square (Площадь) BUILT 2026-11-03: 2x2 road in Decorations (Entertainment), square.py, not seen in game; separate from TownSquare (fountain).
- Road+Square levels 3-5 BUILT 2026-11-03 (road_levels.py: slabs / brick / marble+gold), not seen in game.
