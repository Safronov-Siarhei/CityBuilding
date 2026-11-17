---
name: project-storage-rework
description: Storage rework (sandbox) BUILT IN CODE 2026-11-17 -- StoreLedger, start chain, caps tab, HUD, store card; needs setup,setup + playtest; demolish warning approved and built
metadata:
  type: project
---

The user's storage rework, decided 2026-11-17 in talk (the user: «пока просто говорим») -- NOT BUILT. Full numbers and rules in Design/features/resources-and-storage.md, section «Storage rework».

- Sandbox only; cap per resource per level, each storage building adds its cap; Big tiers kept with bigger caps.
- Start chain Town Hall -> Lighthouse -> Склад -> Кладовая -> Сокровищница (open at start) -> Crystal Mine; then 40 wood/40 stone/20 food/100 coins.
- Destroyed store loses its resources (minus effect), restored ruin returns half. Grain removed. Crystals uncapped.

- Chain buildings free; food 25 kept on purpose (start with 5 residents, growth must be hard); each store holds its own amounts + an inventory button by the «!»; destroyed = lose its contents, ruin restored = half back, rounded up. Prices above caps: PRE-RELEASE check (user rebalances later).

**Why:** the user wants the start to teach where resources live and caps to be readable («10/10» red).
**How to apply:** the code is in (card section «How it is built»); next: the user's general setup (setup,setup: Editor code changed) and playtest; the demolish warning (red «Пропадёт» line) is built too.

2026-11-17 later: overflow lies in the Town Hall; hand demolish moves contents to other stores (warning concept to show first); upgrade keeps old caps; inventory button on the 6 stores only; chain buildings given into the hand with a one-line purpose. New idea DECIDED: MOVE A BUILDING (four-arrows button right of «!», 1 crystal, rebuild time) -- card Design/features/building-relocation.md, not built.

2026-11-17 BUILT (not compiled, no tests, per the user's no-build rule): Resources/StoreLedger.cs, BuildingPlacer.TryBeginChain, sheet tab «Склады: лимиты» gid 2000070 (storage.csv), chain_reward_*/store_full_notice_sec keys, UI/StoreInventoryCard.cs + sack button right of «!». Sheet verified: no differences.
