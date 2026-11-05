# Tutorial (обучение)

> Status 2026-11-05: SCENARIO AGREED, map APPROVED, islands built in Blender (not exported); with the user, NOTHING BUILT. Option «Г» of five proposed
> (a separate tutorial scenario on its own two islands). Next: the islands in Blender, then the mode.
> Read with: archipelago-and-ships (islands, ships, shipyard), sandbox-run-systems (Town Hall start,
> houses make people), army-and-soldiers / army-control (hiring; the Garrison is OFF here),
> orc-raids-and-portals (portal, orc landing), hud-and-ui (main-menu mode cards).

## What the player sees
A short guided scenario (aim: 10-15 min) on a tiny map of TWO small islands side by side: the
player's and the orcs'. No fog -- the whole map is visible from the start. Hints come from the
KING (his portrait + a line). It teaches the core loop: build -> gather -> hire -> defend -> ship ->
attack -> destroy the portal -> victory.

## Rules the user decided (2026-11-05)
- **Enemy: orcs** (a portal, a level-1 orc tower, one orc fighter on their island; a landing ship
  brings the first wave of 2 orcs).
- **Hints from the King.**
- **Main menu:** the tutorial card is ALWAYS shown and can be replayed (for testing now; later
  probably first-launch + replay).
- **Build menu: ONE category** with only Ратуша, Лачуга, Казарма, Верфь. Each shows «1/1» once built
  and cannot be built again.
- **Town Hall, Barracks, Shipyard show nothing extra** -- only what the tutorial needs.
- **Buildings take ~5 s.** **Resources are infinite** -- the gathering is shown, never waited for.
- **Town Hall -> the King + 3 people at once** (2 for work, 1 for the soldier). The Лачуга then
  starts filling its birth bar (the mechanic is shown, nobody waits on it).
- **No squads / Garrison:** the hired soldier is selected by hand like the hero.
- **Barracks: only a level-3 soldier** is offered.
- **Reward: +25 crystals.** The main-menu tutorial card shows a «кристаллы +25» badge.
- **Damage:** orcs and the orc tower deal minimal damage -- visible, never critical. The King never
  dies: at low health he heals himself. The soldier CAN die (shows the player that units die).
- **Shipyard:** the player builds it and hires the ship himself.
- **Camera focus steps** (gathering, boarding): the camera centres on the unit; the player follows
  it until he taps it; once picked, the camera centres on the target.

## The script
1. Start: two islands, no fog. King: «Поставьте Ратушу».
2. Town Hall placed -> the King and 3 people appear. King: «Постройте Лачугу».
3. Лачуга (5 s) -> its birth bar starts filling. Hint: houses make people.
4. «Отправьте жителя за деревом»: camera on a person (centre) until tapped -> camera on a tree ->
   tap it -> he walks, chops, carries.
5. Same for stone with the second person.
6. «Постройте Казарму» (5 s).
7. «Наймите солдата»: only the level-3 soldier; the third person goes.
8. An orc landing ship brings 2 orcs. Just a notice: «Отбейте набег» (no selection asked for).
   They hit softly; the King and the soldier kill them.
9. «Постройте Верфь» (5 s), «Наймите корабль».
10. «Посадите на корабль Короля и солдата»: camera focus as in step 4, on the King, then the
    soldier, each onto the ship.
11. «Отправьте корабль к острову орков» (the buoy).
12. Landing: portal, level-1 tower, 1 orc. «Уничтожьте защитников», then the tower. If the soldier
    falls: «Солдаты гибнут -- нанимайте новых». The King cannot die.
13. «Уничтожьте портал» -> victory screen -> on to the sandbox.

## The map (scheme for approval, 2026-11-05)
`Tools/blender/tutorial_plan.py` -> `tutorial-plan.json` (shapes + spots, for the Blender build) and
`tutorial-plan.png`. 64 x 44 cells: TURNED 180° at the user's word: the player's island EAST (~26 x 26, Town Hall zone 9x9 in the
middle, forest east, stones north-east, the Shipyard on the west shore facing the strait, the orc
landing beach north-west), the orcs' island WEST (~16 x 16: landing beach east, tower, guard,
portal). Strait ~10 cells. APPROVED by the user 2026-11-05 (after turning it 180°).

**Blender (2026-11-05, previews sent, not reviewed):** `islands.py` kind `tutorial` (`-- <Islands/Isl-tutorial1.blend> tutorial 1 [preview]`):
both islands in one 76x76 frame drawn from `tutorial-plan.json` (`SHAPE 'plan'`: coast = the plan's blob distance),
flat, no rim, beach slope 1.2 (not 2.6: the islands are small), landing beaches at the plan's orc_landing /
player_landing / shipyard / orc_barge, forest + stones from the plan's points (33 forest, 18 stony cells), flat pads
round the Town Hall zone (r 4.5) and the portal/tower/guard; `-spots.csv` carries every plan spot. ~504 land cells.
**More sea (the user, 2026-11-05):** the plan got a 10-cell margin (map 84 x 64, frame 90), 10 small
grassy islets (`islets`) and 12 bare sea rocks (`sea_rocks`: stone spires up to ~2.5 m, all stony
ground) on the open water, clear of the ship routes. The sea is SOLID here (deep water drawn as `water_deep`;
library islands leave it to the game's Sea layer) -- the user saw holes otherwise. Now ~508 buildable cells, 89% water.
Not exported yet (export + the game's tutorial mode are the next step).

## To build (plan)
- Two islands drawn in Blender, with spots: Town Hall area, trees, rocks, shipyard shore, orc
  landing, portal, tower, orc guard.
- A tutorial mode with its own rules (no fog, no Garrison, 5-s builds, infinite resources, King
  immortal, one build category with 1/1 caps, trimmed building cards).
- Step script: completion condition, camera focus / highlight, the King's line.
- Numbers and texts in the sheet (a `tutorial` tab; localization keys `#tutorial_*`).
- Main-menu card «Обучение».

## Open
- The soldier: the user said «гвардеец или что-то такое» -- read as ЛАТНИК (ManAtArms, the armoured
  one) at level 3; confirm when the Barracks step is built.
- Skip button inside the tutorial; first-launch auto-start -- later.
