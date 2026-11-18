---
name: project-pending-2026-11-18
description: "The user's first PHONE playtest of the new-machine APK (2026-11-18): 27 items across shop, menu, campaign, sandbox; status per item."
metadata:
  node_type: memory
  type: project
  originSessionId: 65820551-ddcb-4e52-a0d1-9eafc8080c74
  modified: 2026-11-18T10:52:27.626Z
---

The user's first test of the APK built on the new machine (2026-11-18). Status: [ ] open, [x] built, [c] concept shown.

1. [x] sound when the daily reward is taken
2. [x] app icon: a small square inside the round launcher mask -- must fill the circle (adaptive icon)
3. [x] APK/app name must be CityCraft
4. [x] shop: scrolling works only when the finger starts on an offer -- scroll from anywhere
5. [x] shop: the bonus % badge overlaps the price button -- put it ABOVE the price
6. [x] shop: sparkles on offers move; the gold frame of the mine and crystal offers pulses gently, each offer on its own timing
7. [x] main menu: FPS drops to 13 on a strong phone -- optimise
8. [x] main menu: the game title moves a little, «Продолжить» pulses slightly
9. [x] campaign cutscene ~25 FPS on a strong phone -- optimise
10. [x] campaign: the joystick does not appear where the finger touches
11. [x] mission 1 ~15 FPS, can drop to 10 -- optimise
12. [x] mission 1: opening the menu does not hide the game HUD, and no settings button top-left
13. [x] campaign darkness = like the dungeon (the player sees nothing)
14. [ ] fallen tree: the crown is made of flat «pancakes» -- make a proper crown
15. [x] level 2 cutscene: orcs coming out of the forest cannot be seen for the darkness (before the horn); cutscene 2 holds 55 FPS
16. [x] all banners (rate us, plague, fire, ...) x1.5 bigger
17. [x] inventory: boosters do not start at the window's left edge like the resource cells
18. [x] a resident can be ordered onto a ship -- must not be possible (residents appear on an island only from houses)
19. [x] tapping an ore deposit or fertile land moves the camera to it, like tapping a building
20. [x] end of mission 1: the darkness hides the castle past the final point
21. [x] day 4 with nothing built but the mandatory buildings -- no «пройди обучение» banner; the condition was added?
22. [c] the move price «1 crystal» is badly visible under the ✓ -- put it ON the button (concept first)
23. [x] 4 notices at once overlap the minimap: queue the 4th until the first ends
24. [x] hovel's spawn bar stuck when the plague began: hide the bar whenever nothing spawns
25. [ ] the farm worker ploughs beside the farm, must work ON it
26. [ ] rename Farm to «Поле»
27. [c] crows circling the island don't look like birds: simple crow models in flight, wings spread (renders for approval)
28. [ ] notice «На острове не хватает мест в лечебнице» tap: open the build menu and point at the Healer (like the hints)
29. [ ] made my own save, it was not saved

**2026-11-18 session state:** items marked [x] are IN THE CODE but NOT BUILT (no setup run, nothing compiled); next: `run.ps1 -Steps setup` twice (Editor code changed), fix compile errors, add sheet row tutorial_offer_day=3 (tab tutorial), then the open items 14 (fallen-tree crown render), 22 (move-price concept), 25-26 (farm worker on field, Ферма->Поле in sheet), 27 (crow renders), 28 (healer notice -> build menu hint), 29 (own save not saved). FPS fixes (7/9/11) are guesses: phone High = 1080p + 2x MSAA, no bloom in menu/play on phones, Gaussian DOF, mission far 45 -- measure on the phone.
