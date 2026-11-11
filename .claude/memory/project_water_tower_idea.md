---
name: project_water_tower_idea
description: To add (the user, 2026-11-03): a WATER TOWER placed in shallow water that attacks ships and soldiers at sea; not designed or built yet.
metadata:
  type: project
---

The user said on 2026-11-03: «запиши, что нам нужно добавить в игру водную башню».

The Water Tower:
- is placed **in the water, on the shallows**;
- **attacks ships and soldiers at sea**.

Not designed or built yet.

It ties in with:
- the bot's naval attacks at war ([[project_bot_settlement]]);
- orc landings by ship ([[project_archipelago_map]]).

Reusable parts:
- shallow-water placement already exists for the Bridge and the Drawbridge (`isWaterCategory`, the shallow-water zone check in BuildingPlacer);
- the tower's attack is `DefensiveBuilding`.

Open: what counts as "at sea" for soldiers (on a ship, wading?), models (1-5 levels like the towers?), numbers.
