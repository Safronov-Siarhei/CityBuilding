---
name: project-tax-house
description: 2026-11-02 tax rework — slider removed, 1 coin/citizen/day, Tax House +0.2/level with crystal-only upgrades; start-island stone fix.
metadata:
  type: project
---
BUILT bdacd8a6 (2026-11-02). Untested, and the user has not seen it in game.
- The tax slider is gone; the user agreed it was pointless with contentment off in the sandbox.
- Every citizen pays 1 coin a day.
- The Tax House (City tab) adds +0.2 per level. ONE per town, 150 wood + 200 stone. It is opened and levelled in the Laboratory for crystals only: 3/5/7/9/11 (04427d62). The upgrade at the building costs nothing but time.
- Round 9's "bigger start island" was a typo: the user meant stone. The island is back to 130 and gets 40 extra boulders (`island_start_rocks`).

**Why:** the user's request after round 9.
**How to apply:** card resources-and-storage.md. Decided: only one Tax House; the crystal prices are the user's own numbers. Food rework: [[project-food-rework]].
