---
name: feedback-concepts-as-screens
description: UI concepts go to the user as coloured full-screen PNGs drawn over their own game screenshot with the real skin pieces, not as chat widgets
metadata:
  type: feedback
---
Show UI concepts as coloured screen-format pictures (1920x1080 PNG over the user's own screenshot, real HUD skin/icons/Rubik font, a caption strip), sent with SendUserFile -- the way Tools/ui/*_concept*.py always did.

**Why:** 2026-11-20 the user got a flat chat widget mockup and asked for «цветные концепции ... в формате экрана», as before.
**How to apply:** for any new look, write/extend a Tools/ui script (see sandbox_hud_concepts.py, move_price_concept.py), output into Design/ui/, preview a downscaled contact sheet yourself, then send. Related: [[feedback-approve-models-first]].
