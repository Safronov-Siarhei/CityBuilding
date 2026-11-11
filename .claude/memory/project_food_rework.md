---
name: project-food-rework
description: The user's simplified food rules (2026-11-02) — one Food, 6 buildings with flat per-day output, bakery/smokehouse aura bonuses, 1 auto-assigned worker per production building; windmill/barns hidden
metadata:
  type: project
---
User's food redesign 2026-11-02 (mobile: simpler):
- ONE Food resource for every product; NEW icon (apple + bread + meat).
- Fisher Hut: ON THE SHORE, within 1-2 cells of water, never in water. 3 food/day at L1, +1 per level; a fish shoal in its zone x2.
- Orchard: anywhere but the shore; 5/day, +5 per level. DEARER and unlocked LATER via the Laboratory (user picked option b).
- Farm: not on the shore; 3/day, +1 per level; fertile land +1.
- Pig Farm: not on the shore; 5/day, +5 per level; dearer, later via the Laboratory.
- Bakery: every Farm in its radius +L (L1 +1 … L5 +5). Smokehouse: every Fisher Hut and Pig Farm in its radius +L. Bonuses STACK across buildings (an L1 and an L2 bakery = +3).
- Windmill HIDDEN (not available), Barn and Big Barn HIDDEN; only the Pantries (Кладовая) store food.
- Output arrives in whole +1 steps spread over the day: N/day = N times +1 (day = 120 s), shown as popups.
- EVERY production building takes 1 worker, auto-assigned on build, oldest building first; a player who takes a worker off by hand stops the auto-assign there («как в лесопилке»).
- OPEN: the workforce window (keep/remove manual distribution).
- Old saves don't matter (nobody else plays yet).
Status: BUILT 2026-11-02 (da9057d3), setup clean, NOT tested, not seen. Non-food buildings: user picked "only auto-assign" (worker counts unchanged). Orchard research 25/60, PigFarm 30/60; Orchard/PigFarm sandbox costs 40/12 and 45/15; booster service_radius 6. buildings.csv edited locally + in the sheet, NOT committed (standing rule). Related: [[project-game-design-backlog]].
