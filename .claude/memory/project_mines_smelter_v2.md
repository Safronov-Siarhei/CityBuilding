---
name: project-mines-smelter-v2
description: "The user's 2026-11-12/13 ore economy redesign -- Smelter bar buttons, minting removed, mines 1 ore/worker/cycle with per-level cycles, Smelter 1 worker, prices of 4 resource slots, upgrades wood+stone, workers in rounds, resource-shortage mark."
metadata:
  node_type: memory
  type: project
  originSessionId: 65820551-ddcb-4e52-a0d1-9eafc8080c74
  modified: 2026-11-13T17:21:26.795Z
---

BUILT 2026-11-13 (not seen by the user). Card: Design/features/production-chains.md (+ balance-and-localization, citizens-and-workforce).

- Smelter: copper/iron/gold bar buttons beside the «!» (concept A, no price), recipe in pictures on the card, smelt bar over it. Minting REMOVED («Убрать чеканку»). Gold bars have NO use -- the user keeps that question OPEN, don't push it.
- Mines: 1 ore per worker per cycle; cycles coal 20/17/14/11/8, copper 23/.., iron 26/.., gold 29/.. s; workers 2..6. Smelter: 1 worker; copper 1 ore+1 coal, iron 2+1, gold 3 ore+2 coal; 24/27/30 s at lvl 1, -3 s a level.
- Full store = stands; short input = waits + resource-shortage mark (NoResource.png).
- Workers assigned in ROUNDS (1st hand to every building, then 2nd...).
- Prices: sandbox tabs use 4 slot pairs (cost_1/cost_1_n ... up5_4_n, dropdown names). Upgrades wood+stone only and recruiting coins only FOR NOW -- the user will set all prices himself (don't rebalance them unasked).
- Planned, not built: Оружейня (melee weapons from iron bar + coal), Мастерская стрелка (ranged; the user doubts copper there).

**Why:** the user felt ores had no value; [[project-balance-pipeline]] holds the numbers.
**How to apply:** numbers live on the sheet; ask before changing prices.
