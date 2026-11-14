---
name: project-language-picker
description: Main menu language picker (flag + own name pill, dropdown) BUILT 2026-11-14, base English; languages to add per the reference-market analysis; translations last; CJK waits for a font
metadata:
  type: project
---

2026-11-14: the user asked which languages matter (refs K&C / Farthest Frontier are PC; we are mobile).
My recommendation: release en ru de fr es pt(BR); then ja ko zh with a CJK font; cheap wave it pl tr id.
Then the user asked for a main-menu language picker: flag + name pill, dropdown, base English; concept
v1 «too big», v2 approved and BUILT (not seen). Own language names, not country names.
Translations are the LAST step (the user) -- untranslated languages read English.

**Why:** release localization planning.
**How to apply:** card hud-and-ui.md «Language picker» and balance-and-localization.md; new language = sheet column + flag in Tools/ui/language_flags.py; CJK needs Noto Sans CJK first. See [[project-localization]].
