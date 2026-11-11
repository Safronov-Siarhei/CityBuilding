---
name: project-pending-2026-11-10
description: Playtest list 2026-11-10 (16 items, phone): notifications 4 s, orc-base FPS + ravens, squad icon, quarry/sawmill radius+workers, no bot-vs-orc notices, fog culling of everything, selection buttons, food bonus preview, starving leave one by one, fisher into sea, left HUD anchor, day plaque over building name, load options
metadata:
  type: project
---
The user's phone playtest 2026-11-10 (after the banners):
1. Event notice tapped on phone closes at once -> show its text 4 s; seconds in config.
2. FPS 30 when looking at the orc main base early; remove the ravens circling over it.
3. Army tab: no icon on "create squad"; the squad-creation window needs rework -- DISCUSS with the user first.
4. Quarry/sawmill work radius too big: user tunes it himself if it is in config (else add).
5. No notices about bot-vs-orc fights.
6. Bot and orc buildings/soldiers under fog: not loaded; bot-orc fights only when the player sees them.
7. Citizen selection cleared by the old «Выбран житель ✕» badge -> same round button as the King's; the deselect button for every selection pinned to the bottom-right corner.
8. Placing a food building where its bonus applies: show +1 / +% over it.
9. No food: citizens leave one every 20 s (config), food chip shows a warning.
10. Turn off everything under fog (fish shoals, birds, animals) until revealed.
11. Fisher casts into sand, not the sea.
12. Left HUD column not anchored to the screen's left edge.
13. (rule, see [[feedback-localization-fit-before-release]])
14. Building info window: the Day plaque covers the building's name.
15. Sawmill/quarry workers: lvl1 = 1, +1 per level.
16. Main problem: device load -- propose options.

STATUS 2026-11-10: ALL BUILT d33c5ff62 (0 errors, sheet synced), not seen on a phone, no tests -- except: item 3 squad window rework = to discuss; item 16 = load options proposed in chat (next step: the user picks). Orc-base FPS: crows removed + fog culling; static batching of the base NOT done (FBX read/write unknown) -- one of the proposed options.
