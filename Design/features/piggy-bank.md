# Piggy bank («Копилка»)

**Status (2026-11-15):** DISCUSSED with the user, rules below DECIDED; placement concept v2 + 3D-render icon sent for
approval (`Design/store/Piggy_Placement_Concept.png`, `Design/store/Piggy_Icon_Concept.png`, by
`Tools/store/piggy_placement_concept.py <photos>`). NOT BUILT. Daily tasks' list: to discuss later (the user).
Prices: proposed, not explicitly confirmed yet.

## Rules (the user, 2026-11-15)
- A PIGGY (pink pig: the shape everyone knows). Corner icon with a shine, the crystals saved on a plaque under it.
  A tap opens its window: banner with the pig and crystals, «Разбить за $X», a line that crystals are saved
  while playing and doing tasks, the daily tasks.
- Filled by: 1 crystal per minute of play (NO daily limit -- long play is rewarded; only active play: not
  paused, not menus, input in the last ~2 min -- proposed) + DAILY TASKS (e.g. play an Invasion battle,
  destroy an orc building in the sandbox, kill 100 orcs), crystals by difficulty; tasks reset at local midnight;
  one free/ad swap a day proposed.
- Breakable from 150 crystals. NO reset/expiry (the 7-day timer was dropped).
- 5 levels, each PURCHASE levels it up (and it costs more); level 5 stays forever.
  Capacity 300 / 500 / 1000 / 1500 / 3000.
- A FULL piggy is +50 % crystals per dollar over the nearest regular pack (so it pays to fill it first).
  Proposed prices $0.99 / $1.49 / $2.99 / $3.99 / $6.99 (+40..57 %; exact +50 % would be $0.99/1.49/2.79/4.19/6.69).
  5 Google Play products. All numbers in the sheet.
- Full: no more crystals in; «Полна!» on the icon; a «Разбей копилку» banner on entering the main menu at most
  every 4 h and whenever a task's crystals are lost.
- Break animation: shake + cracks, bursts into shards with a flash and rays, a fountain of crystals flying into
  the crystal counter (it rolls up), «Уровень N», the next-level pig assembles from shards. The pig = a 3D
  model (renders for approval first); the corner icon = its render. Colour per level proposed (pink..gold).
- Not in «Подожди!» (its value is the savings, not a discount).

## Where the icon stands (v2 concept after the user's notes 2026-11-16, waiting for approval)
Top-right corner everywhere, never a card:
1. Main menu: settings MOVE to the top LEFT, the language picker to the top CENTRE, the piggy top right.
2. Modes screen: under settings. 3. Menu shop: top right; the crystal balance left of it as a plain LIGHT
number + crystal icon (the beige plaque behind it goes). 4. Sandbox Лавка: screen top right next to the window.
5. Sandbox menu window: top right (not on the bare map). 6. Campaign map: under settings.

## The icon (v2)
A RENDER of the 3D piggy (`Tools/blender/store/piggy_bank.py <out> [pink|silver|gold]`, glossy ceramic, coin
slot, 5 crystals FLOATING around -- none stick out of it; render `Design/store/shop_art/Piggy_render.png`) on a soft
golden shine with sparkles; under it JUST the number (light, dark outline) -- no plaque (the user rejected the
flat drawing, crystals in the slot and the plaque). Full: «Полна!» tag + brighter shine.
Concept `Tools/store/piggy_placement_concept.py <photos> <render>`.

## Traps
- The menu shop does not render in a batchmode capture (its blur/ScreenCapture) -- the concept used its mockup.
