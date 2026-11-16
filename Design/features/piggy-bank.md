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
2. Modes screen: under settings. 3. Menu shop: top right; the player's crystal balance in the top LEFT corner as a plain
LIGHT number + crystal icon (the beige plaque behind it goes; the user 2026-11-16). 4. Sandbox Лавка: screen top right next to the window.
5. Sandbox menu window: top right (not on the bare map). 6. Campaign map: under settings.

## The icon (v2)
A RENDER of the 3D piggy (`Tools/blender/store/piggy_bank.py <out> [pink|silver|gold]`, glossy ceramic, coin
slot, 5 crystals FLOATING around -- none stick out of it; render `Design/store/shop_art/Piggy_render.png`) on a soft
golden shine with sparkles; the number AT THE PIG'S FEET (light, dark outline; the user 2026-11-16) -- no plaque (the user rejected the
flat drawing, crystals in the slot and the plaque). Full: «Полна!» tag + brighter shine.
Three models were shown (А glossy, Б ROYAL, В blocky; `Design/store/Piggy_Models_Concept.png`): the user CHOSE Б ROYAL
(2026-11-16) -- gold crown with rubies, gold slot rim and hooves.
Concept `Tools/store/piggy_placement_concept.py <photos> <render A> [B] [C]`.

## The button (the user, 2026-11-16; concept `Design/store/Piggy_Button_Concept.webp`; animation APPROVED 2026-11-16)
- Its area is a SQUARE (~200 px in the 1080 reference), the piggy centred, as wide as the settings button (~130).
- Crystals float round it SMOOTHLY and a little CHAOTICALLY (the user 2026-11-16: two waves of unrelated periods on
  up-down, sideways and tilt per crystal) -- in the game they are separate sprites
  (`piggy_bank.py ... gem` renders one crystal), the pig a render without crystals (`*_alone.png`).
- The number at the pig's feet. FULL: «Полна!» tag centred on the button's TOP edge (the user 2026-11-16; 4 crystals -- a 5th by the number was tried and rejected; the top centre left free), the number GOLD and glowing (pulse), the
  pig WOBBLES SOFTLY (0.9 s eased in/out every 1.8 s, ~3 Hz, ±2 px / ±3°; the user: smoother), the shine brighter.
- Script `Tools/store/piggy_button_concept.py <renders> <modes photo>`.

## Levels (APPROVED by the user 2026-11-16, `Design/store/Piggy_Levels_Concept.png`)
Same royal pig, the material changes: 1 pink ceramic (300), 2 bronze (500), 3 silver (1000), 4 gold (1500),
5 amethyst -- glowing purple crystal (3000). `piggy_bank.py <out> <pink|bronze|silver|gold|amethyst> royal`.

## Traps
- GIF bands the pig's smooth shading into lines (256 colours) and a palette per frame flickers: concept animations go as
  full-colour animated WebP. The model itself is smooth -- the lines were never in the render.
- The menu shop does not render in a batchmode capture (its blur/ScreenCapture) -- the concept used its mockup.
