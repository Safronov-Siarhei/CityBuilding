# Hunting: herds on the islands, the Hunter's Hut (sandbox)

> **Status (2026-11-21): DESIGN AGREED with the user, concepts made, NOTHING BUILT.**
> Concepts: `Design/concepts/hunting/Hunt_1_Notice.png`, `Hunt_2_Herd.png`, `Hunt_3_Sheet.png`
> (`python Tools/ui/hunting_concepts.py`; sprites from the game's own models via `Tools/blender/concept_sprites.py`).
> Before building: the user approves the concepts, then renders of the NEW models (boar, Hunter's Hut lvl 1-5,
> the hunter with a bow + shooting animation, hide icon, three herd signs) -- models first, as always.

Read with: [food-and-hunger](food-and-hunger.md) (food buildings, «+N» popups), [resources-and-storage](resources-and-storage.md)
(hides), [maps-and-terrain](maps-and-terrain.md) (the decorative Wildlife -- deer/hares that never vanish),
[archipelago-and-ships](archipelago-and-ships.md) (islands, residents work on their own island), research/Laboratory card.

## Rules the user decided (2026-11-21) -- do not re-ask
- **Sandbox only.**
- Herds: **hares, deer, boars**. At most **one herd per island**; **3 herds on the map** in all.
- The **first herd** appears **10 min after the game starts on the player's own island** (to show herds exist).
- A herd lives **8 min**, never leaves earlier; then it leaves and a new one appears on ANOTHER island.
- Herds appear on any island **except the orcs' island and the bot's island** (a reason to build on other islands:
  a hunter needs his own hut + houses on that island).
- A herd's **size** is rolled when it appears: small / medium / large, each with its own chance.
- A herd has a **reach** it roams inside after appearing; it may not appear closer than some distance to buildings.
  A built-up island may get no herds -- that is fine («дикие животные не живут, где много людей»).
- Over the herd's centre hangs a **round sign in the air with the animal**. Tap -> every animal of the herd lit
  **yellow**, and a **notice** in the feed: «<herd>: осталось N из M, уйдут через м:сс».
- When a herd appears: a **notice** «На острове появилась стая …»; tapping it flies the camera to the herd.
  **No minimap mark.**
- Each kill gives at once (no carrying): **food and hides 1 / 2 / 3** (hare / deer / boar).
- Hunting is shown: the hunter **shoots with a bow**; every shot hits by the kind's own **chance** (the player cannot
  predict the catch). The herd is finite: enough hunters can wipe it out.
- **Boars may attack and kill the hunter** (a chance).
- **Hunter's Hut**: a new building, **opened in the Laboratory**, **one per island**; levels give **1/2/3/4/5**
  hunters. 1 hunter auto-assigned, more by the usual round-robin; the player may lower the count by hand.
  Hunters **walk round their island** and periodically check it for a herd.
- **Hides**: a plain resource for now (use to be invented later), kept in the ordinary **Warehouse** and shown in
  the **inventory**, **not** in the top bar.
- The decorative deer and hares (Wildlife) stay as they are; a huntable herd is told apart by its sign.

## Numbers proposed by Claude (the user: «поставь значения сам пока что») -- go to the sheet, not C#
| herd | animals small/med/large | hit chance | food | hides |
|---|---|---|---|---|
| hares | 4 / 6 / 8 | 70 % | 1 | 1 |
| deer | 3 / 5 / 7 | 50 % | 2 | 2 |
| boars | 2 / 3 / 5 | 35 % | 3 | 3 |
- size chance small/medium/large 50 / 35 / 15 %; a boar attacks on 10 % of the misses -> the hunter dies.
- Hut research: 40 coins, 60 s. Levels 1-5 cost: 40 wood + 10 stone; 60 + 30; 90 + 60; 130 + 100 + 20 gold; 180 + 150 + 50 gold.
- Hunters check the island every 10 s. Herd reach and the distance to buildings: to set when building (proposed
  reach 6-8 cells, 10-12 cells from any building).

## Open
- The user's approval of the three concepts and of these numbers.
- Models (renders for approval): boar, Hunter's Hut x5 levels, hunter with bow + shot animation; hide icon; herd signs.
- Performance: herds of 2-8 animals, off-view animals switched off like Wildlife -- check on the Redmi Pad.
