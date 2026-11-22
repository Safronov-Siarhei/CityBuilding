---
name: project_orc_tribute
description: "Orc tribute scroll + sandbox raids by the «Орки: волны» table (2026-11-22) — built, not seen; card orc-tribute-and-table-waves.md"
metadata:
  node_type: memory
  type: project
  originSessionId: 65820551-ddcb-4e52-a0d1-9eafc8080c74
  modified: 2026-11-22T16:13:23.554Z
---

BUILT 2026-11-22 (setup×2, user not seen, no tests). Card: Design/features/orc-tribute-and-table-waves.md.

The user's decisions:
- **Timing.** First raid 7 min after the Town Hall. Each next one 10 min after the last orc of the previous wave dies.
- **Table.** The game reads however many rows the sheet tab «Орки: волны» (gid 2000079, orcwaves.csv) has; after the last row, the last repeats.
- **Tribute.** Up to 5 resources, never crystals, all-or-nothing. The scroll appears 150 s before the fleet sails.
- **Buttons.** «Биться!» sends the fleet at once. «Отложить» or the cross rolls the scroll into a badge, paired left of the booster badge.
- **Siege machines never cross the moat.**
- **Help after a heavy wave.** If ≥40 % of the landing island's buildings fall (roads, fences and decor not counted), the next wave gets ×0.5 health and attack and comes 5 min later. The help lasts one wave.
- **Retreat.** Town Hall below 25 % hp → the wave retreats.
- **Stuck rule.** Nothing happens for 3 min → the wave is over.

Open:
- the tower's climb is a teleport over the wall;
- the table numbers are guesses.

Related: [[project_orc_faction]], [[project_archipelago_map]], [[project_monetization_ideas]].
