---
name: project-sandbox-healer-v2
description: "The user's 2026-11-13 sandbox Лечебница redesign: beds 2-6 instead of a radius, 30 s to find a bed, 60 s treatment per healer, manual worker count beats auto-assignment; BUILT, not seen."
metadata:
  type: project
---

BUILT 2026-11-13 (concepts approved: plus = free bed, green skull, no captions), not seen by the user, untested. Full rules: Design/features/hazards-fire-sickness.md «Sandbox healer v2».

- Sandbox only. Beds 2/3/4/5/6, healers 1/2/2/3/3, treatment 60 s, 30 s without a bed = death (walk counts), first ill first bedded.
- Bottom bar: 6 bed slots + «!»; notices for no beds / no healer.
- Also decided: the player's manual worker count overrides auto-assignment for EVERY building (0 = never staffed).

**Why:** the radius healer let a town die by the day with no visible cause.
**How to apply:** code Citizens/Infirmary.cs + FocusHireBar.Kind.Healer; a free bed is booked the moment one falls ill (death clock stops). Numbers on the sheet (beds in Здания, healer workers in «Песочница: Здания (Город)», infirmary_* keys). Links [[project-mines-smelter-v2]] (worker rounds).
