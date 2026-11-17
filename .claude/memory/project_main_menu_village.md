---
name: project-main-menu-village
description: Main menu concept 1 «День в деревне Ардана» — stage 1 BUILT 2026-11-17 (village+folk+market+kids+Ардан loop, CityCraft logo); stage 2 house under construction needs renders first; stage 3: levers built (459 draws), phone check pending
metadata:
  type: project
---

2026-11-17: the user wanted the main menu «красивее и сочнее»; concept 1 = the campaign's opening village by day behind the menu.
Stage 1 BUILT (not seen by the user in Unity): looping script Campaign/MenuVillage.json on the film's director; logo CityCraft top centre; old random work scenes deleted.
Stage 2 APPROVED + IN THE GAME 2026-11-17 (builders.py / builders_anim.py / export_builders.py, MenuBuildSite.cs; user not seen in Unity): a house being built (scaffolding + 2 builders) -- NEW models/anims, renders to the user first; the house must NOT grow between visits.
Stage 3 step 1 DONE 2026-11-17 (editor census, numbers in the card: 1 578 draws / 283 k tris in view, figures ~10.6 renderers each = 996 draws); Step 2 DONE 2026-11-17: all four levers built menu-only (MenuVillageQuality; menu_* keys in sheet) -> 459 draws; playtest fix 2026-11-17: twins back on houses (roof holes), Ардан's sack hidden, haystacks sunk, church trees = game trees -> 525 draws / 219 k tris. Next: APK on phones only when the user asks; optional: one-draw figures in film/missions (torch/sack/mage tip rework).
Stage 3: phone check (draw calls, a lighter tier: fewer walkers, less smoke). More scene variants later.

**Why:** the empty old menu scene was the user's main complaint.
**How to apply:** card Design/features/hud-and-ui.md «Main menu concept 1»; trap: film assets made once per setup run (MadeThisRun). See [[project-menu-ui]] [[project-campaign]].
