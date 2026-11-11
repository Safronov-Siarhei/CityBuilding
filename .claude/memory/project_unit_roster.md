---
name: project-unit-roster
description: "Widening the player's and bot's units (2026-11-05) — user's decisions: roles/counters, crossbow+cavalry+monk+catapult, new Stable/Workshop, bot full mirror"
metadata:
  node_type: memory
  type: project
  originSessionId: 65820551-ddcb-4e52-a0d1-9eafc8080c74
  modified: 2026-11-05T08:51:37.086Z
---

User decided 2026-11-05 (design in Design/features/army-and-soldiers.md «Roster widening»):
roles and counters (not a strength ladder); new units Арбалетчик, Всадник/рыцарь, Монах-лекарь, Катапульта/баллиста;
infantry from the Barracks, cavalry from a new Конюшня, siege from a new Мастерская, healer from the Church;
the bot is a FULL MIRROR of the player's roster in its colours.
Workshop (2026-11-05): catapult, ballista, siege tower, ram (damages ONLY gates); machine = one unit with crew in the model; siege tower docks on ANY fence (gangway over lvl1-2); all but cavalry walk walls; no stair inside = jump down with damage; orcs get no siege/wall walking for now. Machine drafts (siege_drafts.py) and Workshop1-lvl1 (workshop1.py, doors shut) APPROVED 2026-11-05; Workshop levels 2-5 APPROVED too; Stable1-lvl1 (stable1.py: loose boxes under a blue lean-to, horses inside) APPROVED with its levels 2-5 (stable1_lvl2..5.py); crossbowman, monk, knight drafts (soldiers_new_drafts.py) APPROVED with their level 2-3 looks; export, numbers, code still to do.
Machine + Workshop numbers AGREED 2026-11-05 (in the army card). Fighters' numbers AGREED too (crossbow Barracks lvl5 x1.5 vs armour, knight 2 slots + charge x2, spearman x2 vs cavalry, monk heals 2hp/1.5s, Stable 4 knights per level) -- all in the army card.

No coin UPKEEP for the player's army since 2026-11-05 (user: hire once, never pay; ArmyManager.UpkeepCharged=false); soldiers still eat. Army cap raised 20 -> 80 the same day (economy+invasion, sheet synced).

**Why:** 4 player units / 2 bot units felt too few; a strength ladder gave no choice.
**How to apply:** next step is model renders for approval ([[feedback-approve-models-first]]) and the open numbers; related [[project-bot-settlement]].

**IN THE CODE 2026-11-05 (26543b28d, a4d493546; built, 1 PlayMode test green, user not seen):** crossbowman (Barracks lvl5), knight (new Stable building, 3x3, models Stable1-lvl1..5), monk (Church) -- SoldierType 10/11/12, units-tab columns hired_at/army_slots/cap_per_level/armoured/cavalry/vs_armoured/vs_cavalry/charge_mult/charge_run/healer (sheet synced, verify 0 diffs), Combat/SoldierUnitRoles.cs, FocusHireBar Kind.Stable/Church. Workshop + machines exported to FBX but NOT coded yet. Left: machines (split into moving parts, code, sheet columns), siege rules (all but cavalry walk walls, tower docking, jump damage, ram gates only), bot mirror, Stable in Invasion undecided, general test run, no build.
Workshop + ram/ballista/catapult/siege tower IN THE CODE the same day (crew 2, gates_only, splash, min_range, ranged_taken, carrier, recruit_wood columns; sheet synced; 2 PlayMode tests green). Siege tower does not dock yet.
Siege rules IN THE CODE the same day (SoldierUnitSiege.cs, WallWalk docks, economy wall_jump_health_share/siege_gangway_height, sheet synced, test green). Orc camp palisades take no dock.
General test run 2026-11-05 after the roster work: EditMode 461/461, PlayMode 362/383 (18 skipped) -- 2 archipelago failures from the bot town templates (c6f369e45/8b9752170: bot Fence 31.9 off island > 30; an orc landed in the sea), not from the roster; lab army-tab count fixed.
