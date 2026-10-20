---
name: project-balance-audit
description: "The 2026-08-23 audit of CityBuilding's mechanics, measured off the balance CSVs and the code: the loop closes in ~25 minutes, coins are a single starved tap, towers make the army pointless, and the agreed order of repair."
metadata: 
  node_type: memory
  type: project
  originSessionId: e5dbfab9-cd65-4f18-b8e4-a5c09cc522aa
  modified: 2026-08-29T15:48:44.143Z
---

Measured on 2026-08-23 against `master 9caffa76` — every number below came out of
`Assets/_Project/Balance/*.csv` and the runtime code, not out of impressions. Published as an
artifact for the user: **https://claude.ai/code/artifact/c2bf6342-f133-4b57-ab61-18da421dafca**
("Где ломается CityBuilding"). Re-publish to that URL rather than making a second page.

Read with [[project_game_design_backlog]] (what the user INTENDED) and [[project_current_state]]
(what is built). This file is the gap between the two, with arithmetic.

## The three findings that matter

1. **The whole game is winnable in ~25 minutes and never needs the rest of itself. FIXED in part
   2026-09-01 (`70ec9ff3`) — five portals now, so the map takes five sieges rather than one.**
   As measured, there was ONE portal (`OrcRaidManager`, `PortalDistanceFromTownHallMeters = 20f`),
   it spawned 20 m from the Town Hall, and `GameOverManager` called the first kill a victory. 320 HP against 5 militia
   (23 dps, 125 coins) = 14 seconds. Everything else — 49 buildings, the chains, 4 soldier tiers,
   8 605 coins of research — is optional content nobody has to touch.
2. **Coins are the only currency of progress and the only tap is tax**: `0,5` per citizen per day
   at 100%, and the rate is subtracted straight from happiness (`TaxScore = 100 − rate`). The full
   tree costs 8 605 coins = 860 game days at 20 citizens, 215 days even at 80 citizens on a
   punitive rate. Army upkeep comes out of the SAME coins: 20 men-at-arms = 80 coins/day against
   20 coins/day of tax from 40 citizens.
3. **A tower deletes the entire military branch.** `DefensiveBuilding` fires once a second for the
   building's whole `defense` stat inside 6 m. Tower = 25 dps for 5 wood + 15 stone, no upkeep;
   an orc has 20 HP. Two towers by the portal end raids permanently and for free (an orc needs
   66 s to break a tower, the tower needs 1 s to kill the orc). Towers do NOT shoot the portal,
   so soldiers exist for exactly one action: the finishing blow.

## The rest, in one line each

- **Gold was a dead end. FIXED 2026-08-31 (`3676bdb2`), and this was the item the user chose
  first.** The Плавильня now knows a fourth recipe, `coins`: 1 GoldBar + 1 Coal -> 4 Coins. It
  burns coal like the other three (keeping the `SmeltingBurnsCoal` rule) and the furnace runs one
  recipe at a time, so minting COSTS you the iron you are not smelting. Rate derived, not guessed:
  a day is 120 s and the furnace ticks every 12, so two workers x 2 batches = 40 batches/day; a
  level-1 chain of one gold mine mints ~120 coins/day, which is the research tree in ~72 days
  against 860. No runtime code changed -- Coins were already in the Valuables group and
  ProductionBuilding is generic over `recipe.output` -- but `MintingTests` (PlayMode) proves it in
  the running game rather than by reading, and all four new tests were verified by breaking.
  **The sheet rows were handed over as a file and are not confirmed pasted.**
- **The farm does not feed anyone.** Only `Bread` and `Food` are edible (`ResourceDiet.Edible`);
  a farm makes `Grain`. The only starting food is the Fisher Hut, 10/day on two workers.
  `food_variety_target = 2` and exactly 2 edible types exist, so the food factor is a three-state
  switch (0 hungry / 50 / 100), not a scale.
- **Stone is finite and there is not enough of it**: 140 rocks × 20 = 2 800 on the map, against
  4 731 stone to build one of everything at level 3.
- **Two of the six happiness factors barely move.** Defence: the Town Hall alone gives 20 against
  a target of `0,5`/citizen, so it is a free 100 up to 40 citizens. Decay: `0,02`/day ÷ level, so
  the production penalty starts on day 35 of a game that ends on day 12.
- **The research tree has no shape**: 31 gated buildings, every single one takes exactly 60
  seconds, `requires` is filled on 8 trivial rows. It is a price list, not a tree.
- **`per_day` / `payback_days` LIE for gatherers.** They are computed from `batches_per_tick`,
  which `ProductionBuilding.Update` never reads for wood/stone (income is the walk). Real output
  is ~46–78 wood or 18–31 stone per day for three workers against the sheet's 120. Any balance
  reasoning off those columns is reasoning off fiction.
- **The first raid lands on day 1**, ~88 s after the Town Hall goes down, while migration is still
  frozen for 420 s and the player has 0 coins (a militia is 25). No tutorial exists at all.
- **What is genuinely good and worth protecting**: gathering-as-a-walk (the only system where
  geography matters), fire chance = decay (a maintained town never burns), illness with a readable
  cause, and the whole text/balance-in-a-sheet pipeline.

## The order of repair the audit proposes

1. 4–5 portals spread over the map, waking one after another. **PARTLY DONE 2026-09-01
   (`70ec9ff3`): five portals now open, spread by bearing around the Town Hall and driven by a
   sheet-tunable template.** Not done: they do not WAKE one after another — all five open at once
   and share one raid clock, and closing one buys the player nothing but a quieter direction. See
   [[project_current_state]] for the open question that leaves.
2. Minting: gold → coins. Then re-price research against real income (target: the full tree in
   60–80 active game days, not 860).
3. Split tower and soldier roles, or the four tiers stay a one-shot button.
4. Weights on the happiness factors; drop the Town Hall out of the defence factor.
5. A grace period for the first raids + four in-line hints for the opening.
6. Meat and fruit (waiting on the user's design since 2026-08-17) — unlocks 3 sleeping buildings
   and turns food variety into a real scale.
7. Honest `per_day` for gatherers.
8. Research times that differ by branch.

**Item 2 (minting) was chosen and shipped on 2026-08-31.** Its second half -- re-pricing research
against the new income -- has NOT been done: the tree is still 8 605 coins, which the arithmetic
above now puts at ~72 days rather than 860, so it may already be close enough to leave alone until
someone plays it.

**Item 1 (portals) was offered on 2026-08-31 and the user picked minting instead.** It is still the
one the audit rates highest, and it is still not agreed work. The user's own design for it (~5
portals, only some active, each inside a base to clear) is in [[project_game_design_backlog]].
