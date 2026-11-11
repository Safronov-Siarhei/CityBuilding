---
name: project-seasonal-maps
description: BUILT 2026-10-24 (palette swap, settings choice); "The user's idea (2026-10-21): maps dress for the REAL-WORLD season (autumn maps in autumn), every map styled for all four seasons, buildings maybe adjusted; my feedback and proposed plan. Idea only, nothing built."
metadata:
  node_type: memory
  type: project
  originSessionId: 0f587829-fe18-4bb5-a5b7-742486c4675e
  modified: 2026-10-21T09:05:21.304Z
---

**The idea (user, 2026-10-21):** the game follows the real calendar: in real autumn the maps look autumnal, in winter wintry, and so on. Every map needs a style for each of the four seasons; buildings may need changes too. Status: idea, discussed; the user said "пока что просто запомни идею" (2026-10-21) -- do NOT start it until asked.

**My feedback given:**
- Good for retention and live-ops, and a natural place for holiday events (New Year, Halloween). Purely visual: by the tier rule ([[project_mobile_device_fleet]]) the look may change, never what the world is.
- Cheap because of how the maps are made: the ground, relief and water take their colours from ONE palette texture (TerrainPalette, Tools/blender/terrain.py). A season = another palette texture swapped at load, no new meshes. That makes 4 palettes shared by all maps, not 4 × N maps.
- Trees and decor: canopy colour per season (green → yellow/orange → bare/snowy → blossom), flower decor → fallen leaves/snow tufts. Variants of the decor kit, or a colour swap on the same props.
- Buildings: autumn and spring need nothing. Winter needs snow on roofs, done in a SHADER (white where the surface faces up), not by remodelling 44 buildings × 5 levels. Chimney smoke is optional.
- Risks: winter white ground cuts contrast with the parchment fog/UI and the ink placement drawing; players in the southern hemisphere have the seasons reversed (take it from the device's region, or let the player choose); 4× visual checks per map.
- Suggested order: autumn first (palette + tree canopy, the cheapest and right now in season), then winter with the roof-snow shader, then spring/summer (summer = today's look). Plus a settings option: "by calendar" (default) or a fixed season.

**How to apply:** if the user approves, start with the palette-swap mechanism plus autumn on Map 3, show renders first. Link: [[project_map3]], [[kc_reference_analysis]] (K&C has in-game seasons, a different thing from calendar ones).

## BUILT (2026-10-24, `2c2a31d5`)
User: «сделай стиль на каждый сезон, потом разберёмся как давать игроку их». User REJECTED the real-calendar delivery (players want their own weather; a returning player misses content) and doubts random-per-run; my proposal (not agreed) = a collection unlocked by play, chosen before a run, plus "random from unlocked".
Done as planned: seasons.py palettes -> Seasons/*.mat -> SeasonLooks.asset -> SeasonStyle at map load; winter snow in BuildingDetail.shader (figures on a no-snow material copy). Temporary choice = settings row «Время года». Details in Design/features/maps-and-terrain.md «Seasons». Open: building lawns stay green, water unchanged, menu background always summer, not seen on a phone.
