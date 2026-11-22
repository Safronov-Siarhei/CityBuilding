# Predators: wolves and bears on the islands (sandbox)

> **Status (2026-11-22): DESIGN AGREED with the user (15 answers), concepts in work, NOTHING BUILT.**
> Next: concept screens `Design/concepts/predators/` -> the user's yes -> model renders (wolf, bear with walk + attack
> animations, the small animal cave, the «angry bear» notice icon) for approval -> build.

Read with: [hunting](hunting.md) (the Hunter's Hut, hunters, hides), [resources-and-storage](resources-and-storage.md)
(Кладовая = Granary / BigGranary, StoreLedger per-store stock), [hazards-fire-sickness](hazards-fire-sickness.md)
(Лечебница, bodies, cemetery, plague), [combat-core](combat-core.md), [archipelago-and-ships](archipelago-and-ships.md).

## Rules the user decided (2026-11-22) -- do not re-ask
1. **Sandbox only.**
2. **Cave islands.** At game start 3 islands are picked: the player's main island + 2 random others (never the bot's
   or the orcs'). Each gets a small animal **cave** at its edge -- eternal (cannot be destroyed), under the fog like
   everything else. It is the sign that predators live there. Only these islands get predators.
3. **Spawn at random spots** on such an island (no den the threat always comes from), **not closer than 10 cells** to
   any building.
4. **Timing:** the first predators not before minute **15**; then one attack every **6-8 min** on one of the three
   islands. Wolves and a bear come separately; a bear about 1 time in 3 (none while the island is too small, rule 5).
5. **Group size by the island's residents** (the player's people on that island):

   | residents | wolves | bears |
   |---|---|---|
   | up to 10 | 1 | 0 |
   | up to 20 | 2 | 1 |
   | up to 30 | 3 | 1 |
   | up to 40 | 4 | 1 |
   | up to 50 | 5 | 2 |
   | «и так далее» | +1 per 10 | (Claude's extrapolation: +1 per 40 more, to confirm on the sheet) |

6. **Citizens:** a wolf **wounds** (the wounded goes to the Лечебница as a patient), a bear **kills** (a body ->
   cemetery / plague rules as they are).
7. **Stores = food stores only** (Кладовая, Большая кладовая) -- «медведю не нужны доски». Damage takes the stock
   **in proportion to the hp lost** (half the store's hp = half its food), gone for good (eaten), not carried, not
   recoverable. The damaged store stays damaged until repaired the usual way.
8. **Success -> back to the cave:** a wolf pack once it has wounded **10 %** of the island's residents OR dealt a store
   half its hp; a bear once it has dealt a store half its hp. With no success they go to the cave after **4 min**
   (sheet value). In the cave they vanish.
9. **Defenders:** soldiers, heroes, the King and towers fight them. **Hunters** of the island's Hunter's Hut attack
   predators on their island by themselves.
10. **Combat numbers (sheet):** hunter hp 60, damage 12 every 1.5 s, bow range 5 cells; wolf hp 40, damage 6; bear hp
    220, damage 25. One hunter against a bear will likely die; three or four kill it.
11. **Kill reward:** food always (wolf +2, bear +6); **hides only if a hunter killed it** (wolf +2, bear +5).
12. **Signals:** a notice «На острове появились волки / медведь!» -- tap flies the camera; the notice icon is an
    **angry bear** (the user); hp bars over the beasts; no minimap mark.
13. **Models:** wolf and bear, each with walk AND attack animations; the small animal cave. Renders for approval first.

## Open
- Bears past 50 residents (rule 5's «и так далее») -- confirm Claude's +1 per 40.
- What a hunter does against a predator vs a herd at the same time (proposed: predators first).
