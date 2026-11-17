---
name: project_lab_redesign
description: "Laboratory window redesign (v1 10-28, v2 10-29; v3 SPLIT 2026-11-17 BUILT, not seen: «Прокачка» button left of «!» + categories column, «!» = lab card v2 with hammer/bin -- the model for ALL building cards next) — rows of 5 level icons per building, tabs Здания/Армия (soldiers+ships), price coins + upgrade crystals."
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

## v3 (2026-11-17) -- BUILT, not seen
- The user picked concept A «Полки» and approved info card v2. Built: round «Прокачка» button LEFT of the held lab's «!»
  opens research with categories on the left (7 build-menu categories + Армия, green counts); «!» opens the lab's own
  card (title, health, scientists, «Улучшить», round hammer, red round bin, «Закрыть»; hammer and bin ask first).
- NEXT: the same v2 card for EVERY building, each shown to the user for approval (their words: «после правок
  лаборатории начинаем делать такие окна для каждого здания с утверждением»).
- The font has no «→»: never use arrows in game texts. Card: Design/features/research-laboratory.md.
- 2026-11-17: v2 cards for the ordinary buildings APPROVED (six kinds + upgrade window, concepts Design/ui/Info_*.png)
  and BUILT (UI/BuildingInfoCard.cs, sandbox only; card hud-and-ui.md «Building card v2»), not seen by the user.
  NEXT: concepts for the special buildings (Town Hall, Barracks, Stable, Workshop, Shipyard, Lighthouse, Healer,
  Tax House, Cemetery, Diplomacy, Fire Brigade, Smelter, Crystal mine), approval first.
- 2026-11-17: concepts for the 13 SPECIAL buildings drawn and sent (Design/ui/Special_*.png, Tools/ui/special_cards_concepts.py),
  WAITING for the user's approval; numbers in them are examples, the game fills its own.
