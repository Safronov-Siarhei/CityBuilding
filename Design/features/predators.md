# Predators: wolves and bears on the islands (sandbox)

> **Status (2026-11-22): BUILT IN CODE (setup x2 run, NOT seen by the user, no tests).** Models approved after round 2
> («Да, дальше»): `Models/Fauna/FaunaWolf.fbx`, `FaunaBear.fbx`, `FaunaCave.fbx` (Tools/blender/predators.py builders,
> `predators_install.py` puts them in the .blend's «Хищники» collection, then export_fbx.py); notice icon
> `UI/Icons/PredatorBear.png` (cropped from the approved Icon_AngryBear render).
>
> **How it is built:**
> - `Scripts/Maps/Predators.cs` -- the manager (managers object, SetupProjectCityScene; `caveModel`). Picks the cave
>   islands once the Town Hall stands (Hunting.PlayerIsland + random LandIslands, not orc/bot). Cave spot (fix 2 2026-11-22: the mouth to the CAMERA,
>   the OPEN SEA behind it -- land 2+ cells, water by 5, then 6 cells of water, so not a lake in the middle; failing
>   that without grass, then sea on any side; a loaded cave is turned to the camera too. Fix 1
>   the user saw one on the beach): GRASS under it and before its mouth (ArchipelagoMeshes.IsGrassUv; without the grass
>   test only when none is found), the sea 3..5 cells away (not the beach), mouth turned inland onto land, and from
>   before the mouth a full NavMesh path to the Town Hall (player's island) and to 2/3 of 6 random spots of the island
>   (not behind the mountains); none found -> tries again every 5 s. Its cell occupied, FogCulling, `SeasonStyle.DressAny`
>   (the map's theme recolours it, like the dungeons' mouths). Beasts spawn only
>   where they can walk to the cave. Clock (sandbox only, `Infirmary.Active`):
>   first `pred_first_attack_sec`, then `pred_attack_min/max_sec`; an attack goes to a cave island with residents
>   (HousePeople.CitizensOn) and no raid on. `GroupFor`: wolves = ceil(res/10), bears = res>10 ? 1+(res-1)/40 : 0.
> - The beasts are **OrcUnits** of new kinds `OrcKind.PredatorWolf = 44 / PredatorBear = 45` (IsMonster, IsPredator;
>   stats `mon_pred_wolf_*` / `mon_pred_bear_*` via MonsterIds), spawned by `OrcRaidManager.SpawnOrcOfKind`
>   (models `predatorWolfModel/predatorBearModel`, SetupProjectOrcs). So soldiers, heroes, the King, towers fight them,
>   hp bars and corpses come free. `Steered = true` + `PredatorErrand` (same file) drives them: `OrcUnit.DefendSelf()`
>   (new: fights a soldier in aggro radius the usual way), else a hunter in aggro radius, a villager near (bear only
>   within 2.5x reach), the island's nearest Granary/BigGranary (goes to its door), any villager of the island; nothing
>   -> home. Wolf bite = `Infirmary.Wound(agent)` (new: the bitten figure itself becomes the patient,
>   `CitizenVisualsManager.TakeThisForPatient`); bear = `KillAgent` (body, cemetery/plague as they are). Store bite =
>   `TryDamage` then `StoreLedger.Eat(store, Food, after/before)` (new). Success / 4 min -> `SteerTo(cave)` -> Destroy.
> - Rewards on `OrcUnit.Died`: food always; hides only when the killing blow was a hunter's (`Predators.HunterHit`).
> - Hunters (Hunting.cs): health `pred_hunter_health`; predators on their island come FIRST (before herds): walk within
>   `pred_hunter_range_cells`, an arrow every `pred_hunter_shot_sec`, always hits for `pred_hunter_damage`.
>   `HurtHunter` -> `KillHunter(h, "#log_hunter_killed_pred", "PredatorBear")`.
> - Lunge: `FigureAnimator.Lunges` (set for predators in SpawnOrc): a blow = body forward 0.1, nose down 7 deg, head
>   +14 deg (LungeAmount curve); legs only trot with the walk.
> - Not on the minimap (MinimapAttackAlerts skips IsPredator); not saved as orcs (GameSaveController skips them) --
>   the save keeps `predClock`, `predNext`, `predCaves`; a load starts with no beasts out.
> - Notices (`#log_pred_wolves/_bear/_bears`, `#log_pred_bear_killed`, `#log_pred_store` once a raid) with the
>   PredatorBear picture; the appear-notice follows the first beast (tap flies the camera).
> - Cheats «Орки» tab: «Хищники сейчас» (`Predators.AttackNow`) + the countdown.
> - Numbers: `pred_*` + `mon_pred_*` on «Еда и голод» (rows 68-97), texts on «Тексты: журнал событий» (116-121).
>
> **Traps:** never `new NavMeshPath()` in a field initializer of a MonoBehaviour (static ones too): Unity throws
> «InitializeNavMeshPath is not allowed... from a MonoBehaviour constructor» and the class is dead -- no cave was ever set
> (2026-11-22); `Path` is made on first use. a new OrcKind goes at the END of the enum (saves store the int). `IsMonster` now includes predators --
> PiggyRunner / InvasionWaves treat them as monsters (no orc-kill task). The cave's mouth is Blender -y, turned 180 like
> the figures. Hunters' herd logic is skipped while a predator is on their island.

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
- Bears past 50 residents (rule 5's «и так далее») -- Claude's +1 per 40 is built (pred_bear_per_residents), to confirm.
- A hunter with both a herd and a predator on his island: predators first (built that way), to confirm.
- Not seen in the game: the cave's look/placement on the shore, the lunge, the balance (bear vs a 110-hp Кладовая = 3 blows to half).
