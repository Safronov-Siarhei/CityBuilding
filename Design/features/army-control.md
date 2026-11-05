# Army control: squads (concept, 2026-11-05)

> **Status:** CONCEPT AGREED with the user 2026-11-05 (their 4 mock-ups + answers), NOTHING BUILT.
> Replaces the right-side army panel (one group per soldier type) described in
> [army-and-soldiers](army-and-soldiers.md). **Read with:** [army-and-soldiers](army-and-soldiers.md),
> [archipelago-and-ships](archipelago-and-ships.md), [hud-and-ui](hud-and-ui.md).

## HUD
- **Army icon** under the hero portraits (right edge); with fewer than 3 heroes it moves up under the last one.
- Tap it: drops any hero/building selection, opens the **squad row** bottom-centre: Garrison crest, the
  player's squads (max **5**), «＋» (opens the squad window on the «Создание отрядов» tab). With a squad
  selected a «!» appears right of «＋» (opens «Управление отрядами» for it).
- **Machines** have their own smaller icons in a row ABOVE the squad icons; each is selected and ordered
  alone; no «!» for a machine.
- A squad icon = its **crest** (changeable in «Управление») with the soldier count under it.
- With a squad selected: 3 **formation** buttons on the right under the army icon, and a **cancel
  selection** button below them. The same cancel button replaces the «Выбран король» plaque for heroes.
- One squad (or one machine) selected at a time; no multi-select, no «all».
- Tap on a soldier in the world selects HIS squad; his card moves to a long press.

## Orders (variant B)
- Selected squad: tap ground = go, tap enemy = attack, tap wall = go up, tap own ship = board.
- **Deselecting changes nothing**: the squad finishes the order, then follows its behaviour where it is.
  To stop it: tap the ground beside it.
- **Fight rule**: fight while an enemy is within the squad's reach; none left -> back to its point.
  Shot from beyond reach -> goes at the shooter: a short way on «Стоять», without limit on «Наступать».

## Garrison
- Every new recruit joins it. No size limit. Always at home on the main island, defends it, soldiers act
  one by one. No behaviour choice, no formation, cannot board / go to another island: trying shows a hint.

## Squad window (pauses the game, like the menu)
- Tab «Создание отрядов»: two squads side by side, each picked from a drop-down (the drop-down also
  makes a new squad); pencil = rename, then delete (soldiers go back to the Garrison); tap a soldier's
  card = he moves to the other column; scrolling list with a visible scroll bar; «!» = short help.
  Card: portrait, type, level, health.
- Tab «Управление отрядами»: one squad from the drop-down; crest; behaviour (3-4 buttons); formation (3).
- **Auto-refill** switch: a squad takes soldiers of the types it lost from the Garrison; works only while
  the squad is on the main island (hint when it is not). Not auto-hiring.

## Behaviours and formations (proposed by Claude, the user's examples were placeholders)
- Behaviour: Стоять (hold, short chase) / Охранять город (patrol near the hall and walls, chase in a zone)
  / Наступать (seek enemies on the island) / За героем (follow a chosen hero) -- 4th not yet agreed.
- Formation: Колонна (march) / Линия (infantry ahead, ranged + monk behind) / Каре (infantry outside,
  ranged + monk inside). Slots computed once per order, not per frame.

## Ships
- Capacity grows with the ship's level: **8-10-12-14-16**. A squad bigger than the ship does NOT board
  and NOT split: notice «Уменьшите отряд или улучшите корабль».
- Squad selected + tap the ship = it boards when it fits; the ship then has a «Высадить десант» icon by a shore.
- A machine takes **4 places**; boards the same way.
- No «bring the squads home» button on a raid (the user: return them by hand or grow the Garrison).

## Open
- Which ships the 8-16 ladder is for (now transport 8, battleship 8, scout/war 0) and whether ship
  levels already exist (Laboratory) -- asked.
- Crest set (how many, art); behaviour «За героем» yes/no; heroes in squads or apart (apart now).
- Squads in save data; the bot and Invasion keep their own automatic armies.
