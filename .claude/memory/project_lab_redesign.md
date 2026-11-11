---
name: project_lab_redesign
description: "Laboratory window redesign from the user's sketch (v1 2026-10-28, v2 2026-10-29, BOTH BUILT; awaiting the user's feedback on v2) — rows of 5 level icons per building, tabs Здания/Армия (soldiers+ships), price coins + upgrade crystals."
metadata:
  node_type: memory
  type: project
  originSessionId: 0f587829-fe18-4bb5-a5b7-742486c4675e
  modified: 2026-10-28T20:03:38.688Z
---

User's sketch 2026-10-28 (image in chat, not in repo). Decisions:

- Header «Лаборатория уровень N»; two tabs **Здания** and **Армия** (renamed from Воины; Армия holds soldiers AND ships). Selected tab grows and is highlighted.
- One ROW per building: name label above, 5 level icons (lvl1..lvl5 portraits) joined by a line, a «Прокачать» button + price under each icon (price shown like the building info panel). Vertical scroll, same order as the build menu. A building closed even at lvl1 has its lvl1 to research before it appears in the build menu.
- Bottom: scientists + / −, «Улучшить лабораторию», «Закрыть»; a «!» in the corner explains why upgrade the lab and what scientists give.
- States: done = tick, no button; available = green; previous level first = grey; lab too low = grey with «Лаб. ур. N».
- In-progress: timer bar on the icon being researched, its button becomes «Отмена»; one research at a time.
- Price = COINS + **UPGRADE CRYSTALS** (new currency: found in dungeons, rewarded ads, bought in the shop for currency); crystals need their own unique beautiful icon.
- Researching level N only ALLOWS upgrading that building to N (player still upgrades each building).
- Decor and Flag are NOT in the lab. Fence HAS levels (user insists, and wants models per level; in invasion fence lvl2+ is stone; in the sandbox fence has no per-level model, only the silver/gold disc).
- Tapping an icon shows what the level gives before paying.
- Portraits for every building at every level: user said «делай портреты» (render via Tools/blender render_portrait.py). Level models lvl2..5 ARE exported for 47 buildings.
- Soldiers: 3 levels for now. Barracks level opens the soldier TYPE, the lab researches its LEVELS (removes the double key).

Related: [[project_archipelago_map]] (ships in the Армия tab, shipyard levels), [[project_game_design_backlog]].

**BUILT 2026-10-28** (see Design/features/research-laboratory.md): new ResearchPanelController window, upgrade crystals (`Wallet.UpgradeCrystals`, economy research_crystals=1 from level 3), Flag/decor research zeroed, soldier tiers opened by Barracks only (units starts_unlocked=1), fence level icons Fence_L2..5, crystal icon Res_UpgradeCrystal. Photos sent to the user; awaiting their feedback. Not yet: ships tab content, dungeons/shop/ads as crystal sources.

**v2 BUILT 2026-10-29 (fa4bb6db)** — was agreed 2026-10-28 evening — the user's final mockup:
- Top centre under the title: ONLY the player's coins and upgrade crystals (no other resources, no status line).
- PARALLEL research: as many at once as there are SCIENTISTS (each scientist = one slot; answer Б). Each researched level shows its own timer on its icon; the button under it becomes «Отмена» (70% coins + full crystal back).
- No status line: tapping a picture shows what the level gives in a small bubble over that picture; a blocker like "not enough coins" flashes the coin/crystal counter red (and shakes the button).
- «Улучшить лабораторию» and «Отремонтировать» open their OWN small windows: the building's icon (next level's for upgrade), the price and a final confirm button; the upgrade window also says what the next level gives.
- Principle (user): the player must not hit a wall on upgrades — the game is about developing the settlement, not grinding resources.
