---
name: project-construction-look
description: Construction = building under a green net inside scaffolding + juicy dust finish (2026-10-28); card Design/features/construction-look.md
metadata:
  type: project
---
2026-10-28: user wanted construction to look like a building site. Rule: «площадка с лесами -> эффект при окончании таймера -> готовое здание», NO gradual reveal of the model (rejected as risky). Everywhere, build AND upgrade; scaffolding must stay inside the building's own cells (no clipping into neighbours); no walking builders.

Round 1 (f8822fcf): foundation + hidden model, two finishes in settings. Round 2 same day after the user's renovation photo (110a12fa): building VISIBLE under a green construction net inside scaffolding (build and upgrade alike), only the DUST finish, made juicier; fanfare + settings row deleted (CSV + sheet).

Then «добавляй на все здания» (ab390bb9): every building with a build time except Road/Bridge (walked on); net may reach the cells' edge so wide models don't poke through. User approved the look. Upgrade = pick В: scaffolding without net + hoist with a pallet of stones, then the user asked for a small upgrade effect after all -> PlayUpgrade (gold sparks spiral, little dust, nod). Same day: run clock under the minimap (RunClockLabel) and smooth camera follow (RTSCameraController goal + followSharpness) — feel not yet checked on a phone.

**Why:** user picked Dust and wanted «здание накрыто полотном, вокруг леса».
**How to apply:** further tweaks go in ConstructionLook / BuildFinishEffect; card Design/features/construction-look.md. See [[project-current-state]].

Also discussed 2026-10-26 and PARKED: archipelago map 400x400 (home island + 6-7 islands, shipyard with explore/transport/war/trade ships, shared world resources, buildable islands, sandbox only, one hand-made world). Questions left open: start with shipyard or research it; orc ships at sea or plain landings.
