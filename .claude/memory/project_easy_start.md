---
name: project-easy-start
description: "The user's balance rule (2026-10-20) — an easy, generous start that gets harder gradually; cheap first levels of basic buildings, upgrades keep their price; seven quiet minutes; what shipped and what is still open."
metadata:
  node_type: memory
  type: project
  originSessionId: 0f587829-fe18-4bb5-a5b7-742486c4675e
  modified: 2026-10-20T17:37:21.305Z
---

The user's rule (2026-10-20): the player must enter the game "легко и непринуждённо", with enough resources for the needed buildings so they don't fall into problems at once; difficulty grows gradually. First levels cheap, **upgrades may stay expensive**.

**Shipped (be8d009b):**
- First-level cost of BASIC buildings only, cut 30–50%: Road 3 stone → 1 stone (the user chose stone, not wood or free); Fence 2w; Hovel 7w; Sawmill/Quarry 12w; FisherHut 10w; Farm 10w 2s; Orchard 12w; Well 5w 2s; Warehouse 18w 5s; Granary/Barn 15w 3s. Church, theatre, mines and the rest are unchanged (the user's choice: "только базовые").
- Upgrade columns (up2..up5) are literal numbers in the sheet, not formulas, so they did not move.
- Opening stock lives in economy.csv: start_wood 80, start_stone 30, start_food 50, start_gold 100 (was hard-coded 50/20/30/100 in ResourceManager).
- The settling-in window (settling_in_seconds = 420, 7 min) now also blocks raids (the first raid timer is max(raid interval, settling-in)) and sickness (SicknessManager.FallIll returns early). Before, the first raid came 90 s after the Town Hall, and with no well the Town Hall alone made the town 100% dry: one sick citizen per day from day 1.
- Test: EasyStartTests (the stock buys sawmill + quarry + fisher + well + hovel + 10 roads; no raid or sickness while settling in). All 3 fail with the change reverted.

**Why:** the user felt basic buildings were hard to afford at the start (road "3 камня — дорого"), and early sickness/raids forced crisis management before the player liked the game.

**How to apply:** when tuning further, keep the early game generous and push difficulty into upgrades and later raids. Not done yet and worth offering: a measured 10-minute playthrough of the opening (what gets built, what is left over), and whether early sickness after minute 7 is still too harsh. Related: [[project-balance-pipeline]], [[project_balance_audit]].

**Build trap found on the way:** SetupProject's DeleteIfExists now waits for a deleted asset to be gone. The Windows indexer or the antivirus kept deleted files "pending" and the run died with "Cannot open file …meta for write" / "Failed to delete …asset". A .prefab.meta may still come out truncated (59 bytes, guid only): restore it with `git checkout` before committing.
