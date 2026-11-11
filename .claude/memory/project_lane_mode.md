---
name: project-lane-mode
description: "A Dota-like lane mode for CityBuilding (2026-10-20): the user's idea, decided as a SEPARATE mode, vs AI first, the King a hero with skills. Concept stage — nothing built."
metadata:
  type: project
---

**The user's idea (2026-10-20):** a corridor (lane) along which bots attack constantly; each player controls only their King and supports their side's push; mini-challenges and extra enemies on the map give gold and XP; with them the player builds extra buildings at the base, levels the hero's skills and upgrades allied troops. Goal: destroy the enemy's main building.

**Decided by the user the same day:**
1. A SEPARATE MODE inside this game (not replacing the city builder).
2. Versus AI first (PvP only later, if the mode proves fun — it would need a server-authoritative sim, lobby/relay, see [[project_pvp_ideas]]).
3. The King is a HERO WITH SKILLS (Dota-style), not a commander.

**My framing (agreed direction):** working title «Королевский тракт». One corridor map; allied and orc waves meet; orc camps (already built: walls, towers, totems, cages, treasury) on the sides as mini-challenges/neutral enemies; gold from camps/kills buys base buildings (barracks, forge, towers) and troop upgrades; XP levels the King's skills; win = destroy the orc main base at the end of the lane (exists); lose = own main building falls. Reuse: combat + projectiles, orc kinds incl. chief, camps/loot, research-like upgrades, building models. New: hero control (virtual joystick + 3-4 skill buttons), skills, XP/levels, wave spawner on a lane, in-match gold economy, mode entry in the main menu.

**How to apply:** next step is a concept for the user to approve (skills, waves, economy, map) before any code; keep numbers in the balance sheet ([[project_balance_pipeline]]); mobile-first ([[feedback_mobile_first_optimization]]).

**Concept I drafted (2026-10-20) — user: «пока что просто пометь себе» (PARKED, not approved):** ~150 m lane, own castle (south) vs orc main base (north), 2 towers per side; 4-6 orc camps beside the lane + a central orc-chief "Roshan"; waves every ~30 s (militia/archers vs grunts/throwers), orc waves grow; King: joystick + 4 skills (Shield Bash stun, War Cry attack-speed aura, Volley AoE arrows, ult "King's Banner" invulnerability/buff), levels 1-10, respawn timer by level; gold from kills/camps/chief buys Barracks (more soldiers), Archery range (archers in waves), Forge (armour/weapons), Tower, 2-3 King items; 10-15 min matches. Build order if ever resumed: flat lane prototype (waves + joystick King) -> skills/XP/gold -> base buildings -> camps/chief -> Blender map.
