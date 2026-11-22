---
name: project-pending-2026-11-22
description: "Playtest 2026-11-22 (tablet + Editor), 17 items: hunting fixes, HUD overlaps, info panels auto-hide, upgrade after buying, FPS counter, campaign minimap/wheat/roads, tablet loading screen; no build/tests asked"
metadata:
  node_type: memory
  type: project
  originSessionId: 65820551-ddcb-4e52-a0d1-9eafc8080c74
  modified: 2026-11-22T06:42:13.290Z
---

The user's list of 2026-11-22 (no build, no tests -- "после этого будет делать другие правки"):
1. Herd notice tap must fly the camera to the herd (said before).
2. Booster icon (potion under the top bar) overlaps the resource chips -- lower it.
3. Herd sign draws over other UI.
4. Citizens info panel: drop «Довольство» (happiness is off), add «Болеет» and «Солдаты».
5. Crystal-mine bubble («3/10») draws over the screen UI.
6. Info panels opened from the top icons close by themselves after 7 s.
7. Hunter's Hut card: hit chance per animal kind.
8. «Herd appeared» notice only when the island has player buildings.
9. FPS counter (settings) visible from the main menu on, never on the studio splash.
10. Minimap tap does nothing in the campaign.
11. Tablet: the map-style mode loading screen is laid out for narrow phones -- compass not visible.
12. Campaign wheat: shadows flicker.
13. Campaign sand roads: joints look sloppy (separate models side by side).
14. Tablet sandbox: centre dot looks angular (low-res).
15. Long white screen before the studio logo.
16. Upgrade with missing resources -> buy offer (ad/crystals): after buying, start the upgrade automatically.
17. Infirmary patient portraits stick out of the circle; green ring uneven.
ALL 17 DONE in code 2026-11-22, NOT compiled (user said no build/tests yet). Notes: 1 = the herd SIGN now flies the
camera too (the feed notice already did); 15 = only CineGroundNoise moved off BeforeSceneLoad, the rest is engine
boot, not measured; 3/5 = world bubble canvases at sortingOrder -5 (HUD is 0). Details in the cards (hunting,
hud-and-ui, campaign, studio-splash, monetization, hazards-fire-sickness, building-placement). Next: setup x2 when
the user's next batch is done.

**Why:** the user's playtest list; [[feedback-do-every-item]].
**How to apply:** do every item, check each in the real code path.
