---
name: project_pending_2026_11_19b
description: "Playtest list 2026-11-19 round 2 (9 items, no tests/builds): 8 done in code; menu village houses invisible after sandbox->menu NOT FOUND; tutorial offer never worked (old Input class) FIXED"
metadata:
  type: project
---

The user sent 9 items on 2026-11-19 (after the dev gift / builds). They said to do them with no tests and no builds, and that more items are coming.

**Done in code (not built, not seen):**
- orcs land only where they can walk to the hall;
- citizen fog reveal skips inactive/deep-water agents (probable cause of the open dot in the sea);
- sandbox rocks respawn (`rock_respawn_sec` 180, `rock_respawn_share` 0,5);
- the Lab's upgrade closes its window (the building card already closed since fd300356d);
- an empty-radius sawmill/quarry stands its workers down;
- tutorial boulder on the person's island;
- tutorial build steps let go of a held building;
- precise locked-level labels.

Sheet synced: 2 economy keys and 2 reworded texts. Cards: archipelago-and-ships, fog-of-war, gathering, tutorial, research-laboratory.

**OPEN:** main-menu village houses vanish after coming back to the menu. Not found by reading:
- OpeningVillage builds them in Start from stripped building prefabs;
- no static cache;
- no LOD;
- no global shader state found.

The user said they came back FROM THE SANDBOX.

The Windows player's log (LocalLow/Depigo Games/CityBuilding/Player-prev.log) shows the village built identically both times (the same «no room for house» lines), so the houses ARE created but not drawn. One difference: on the return, «Unloading 1832 unused Assets» runs after the village is built; on the first entry it does not.

Checked and ruled out:
- shader globals;
- shared-material and shared-mesh edits;
- the season swaps;
- LOD;
- layer masks;
- timeScale.

Next: see it (Windows build or a PlayMode probe that counts enabled house renderers).

Found in the same log and FIXED:
- `TutorialOfferBanner` used the old `Input` class and threw every frame under Input-System-only, so the tutorial offer never came;
- `NewsWindow` Escape had the same problem;
- an NRE on scene teardown in `ReleaseAgentToIdle`.

Related: [[project_pending_2026_11_19]], [[project_main_menu_village]]
