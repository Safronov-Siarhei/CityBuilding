---
name: project_pending_2026_11_19b
description: "Playtest list 2026-11-19 round 2 (9 items; the user said no tests/builds, more items coming): 8 done in code, not built; menu village houses missing on re-entry NOT FOUND (asked the user)"
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

Asked the user which buildings and from which mode they came back. Next step if needed: a batchmode load of menu → game → menu that counts the houses.

Related: [[project_pending_2026_11_19]], [[project_main_menu_village]]
