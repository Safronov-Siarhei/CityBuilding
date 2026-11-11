---
name: project-tutorial
description: Tutorial mode state -- playtest 2 (ffd6bcfe5) and 3 (6dda4980c, map re-exported with a wider strait) built 2026-11-06, not played
metadata:
  type: project
---
Tutorial mode (card Design/features/tutorial.md) was started on the user's other device; playtest-2 list (19 screens)
BUILT here 2026-11-06 in ffd6bcfe5, TutorialTests green, user has NOT played it.
User picked marker D (gold gem with glow) for the held person -- in ALL modes.
Boarding mark = variant 1 (arrow + ring on the ship) BUILT. Open: other wake looks
(wake now from the stern), "King hits the portal from too far" not reproduced.
**Why:** the user plays the tutorial on device and sends screen lists; each list = one round.
**How to apply:** read the card's «Second playtest» section first; tutorial-only rules go through TutorialMode.IsActive.
Related: [[project-army-control]].

Round 3 (2026-11-06, 6dda4980c): strait widened via Tools/blender/tutorial_plan.py SPREAD + islands.py/export_map.py re-run (Blender on G:\SteamLibrary, blend on H:); 3 orc guards; camera follows every frame. The build rewrites CSVs (quote/order churn) -- harmless.

Round 4 (2026-11-06, 12d426b09): camera/tap locks on gather steps, sail-to-mark only, ship meets troops + spread landing in ALL modes. Open: building destruction effect -- options proposed, user to pick. User: NO tests, he tests himself; build only.
Destruction effect = pick 1 (sink + dust + debris + shake) BUILT c918218d9, all modes, not seen.
Round 5 d4c42eda5: ship always meets troops, portal collapse + 5 s victory delay, all destruction collapses.

- 2026-11-06 59b33728e: raid camera waits tutorial_raid_camera_sec (3) after the hire; victory panel after victory_delay_sec (3, economy; was a 5-s const). Built, not played.

- 2026-11-06: the user declared the tutorial FINISHED. Left for the final pre-release pass (their words): how to present it to the player. Do not raise it earlier.
