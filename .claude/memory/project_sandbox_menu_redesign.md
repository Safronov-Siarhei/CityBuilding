---
name: project_sandbox_menu_redesign
description: Sandbox menu redesign (2026-11-17) -- concept A «Стол картографа» + purchase banner BUILT in the game 2026-11-17 (not seen); paid «Свобода песочницы» (~$0.99 world settings) look first, functions to discuss; per-map autosave BUILT
metadata:
  type: project
---
The user (2026-11-17) is redoing the Sandbox menu after the Invasion one, together with a PAID feature «Свобода
песочницы» (~$0.99 one-time): set the sandbox world up before creating it and during play (modes, enemy strength...).
Order the user set: concepts first for approval, THEN discuss the feature's functions.

- Concepts A/B/C (`Tools/ui/sandbox_hub_concepts.py`, `Design/ui/SandboxHub_*.png`); user picked **A «Стол картографа»**
  and asked to refine it; v2 = `SandboxHub_A2_*`; v3 after notes = `SandboxHub_A3.png`, `_A3_StartPulse.gif`,
  `_A3_Banner.png` (purchase banner like plague/fire) APPROVED («Утверждаю», 2026-11-17); not built; next = discuss the feature's list, then build. RULES: settings SHOWN OPEN, a change
  attempt opens the purchase banner; «Начать» pulses; ribbon joined like plague/fire banners.
- Settings in the mock-ups are examples only. Open: the list; whether a custom world earns piggy tasks/crystals
  (risk: easy settings farm paid currency -- options A mark world «Свой мир» no tasks, B same rules, C bonus for hard).
- Per-map autosave BUILT 2026-11-17 (user rule: one per map, named «Автосохранение — <seed code> <date>», new replaces
  old, a later manual save on the map deletes it); see Design/features/save-load.md.

Card: Design/features/hud-and-ui.md (SANDBOX MENU REDESIGN). Related: [[project_menu_ui]], [[project_monetization_ideas]].

FEATURE LIST DECIDED 2026-11-17 (card Design/features/sandbox-freedom.md): raids 5 levels, neighbour always-peace/as usual, quiet start 0/7/15/30, fires/storms/plague, start goods, build+research speed, fog; piggy option A; presets parked; in-game scroll under «Сохранить». Not built.

BUILT 2026-11-17: new sandbox screen, rectangular chart (sea at the sides), settings scroll, purchase banner, in-game «Настройки мира» under «Сохранить», piggy line; MapPickerTests 4/4; user not seen it in Unity yet. Details: card sandbox-freedom.md.
