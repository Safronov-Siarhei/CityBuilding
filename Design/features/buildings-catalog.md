# Buildings catalogue (the roster)

> **Status (2026-10-21):** the 49-building roster is authoritative (user, 2026-08-16); 44 have Blender
> models in levels 1–5; water buildings (Мост, Пристань, Водяная мельница) still procedural.
> **Read with:** [building-placement](building-placement.md), [building-levels-and-decay](building-levels-and-decay.md),
> [art-pipeline](art-pipeline.md), [research-laboratory](research-laboratory.md).

## The roster (id = model base name, `<Id>1-lvl<N>`)
- **Город (City):** Ратуша `Castle1` ⚠, Колодец `Well1`, Лачуга `Hovel1`, Дорога `Road1`, Коттедж
  `Cottage1`, Особняк `Manor1`, Пожарная бригада `FireBrigade1`, Дом лекаря `HealerHouse1`, Лаборатория `Laboratory1`
- **Склады (Storage):** Склад `Warehouse1`, Кладовая `Granary1` ⚠, Сокровищница `Treasury1`, Амбар
  `Barn1`, and Big… of each
- **Развлечения:** Городская площадь `TownSquare1`, Таверна `Tavern1`, Театр `Theater1`, Церковь
  `Church1`, Коллизей `Colosseum1`, Декор. сад `DecGarden1`, Декор. дерево `DecTree1`, Декор. куст `DecBush1`, Флаг `Flag1`
- **Оборонительные:** Забор `Fence1`, Ворота `Gate1`, Башня `Tower1`, Казарма `Barracks1`,
  Укреплённые ворота `FortifiedGate1`, Укреплённая башня `FortifiedTower1` (Укреплённый забор: models only, not a building)
- **Производственные:** Лесопилка `Sawmill1`, Каменоломня `Quarry1`, Железный/Золотой/Медный/Угольный
  рудник `Iron/Gold/Copper/CoalMine1`, Плавильня `Smelter1`
- **Еда:** Фруктовый сад `Orchard1`, Ферма `Farm1`, Рыбацкая хижина `FisherHut1`, Свиноферма
  `PigFarm1`, Коптильня `Smokehouse1`, Ветряк `Windmill1`, Пекарня `Baker1`
- **Водные:** a stub, «требует отдельной долгой проработки» — don't design it alone.

⚠ **Ратуша's model is `Castle1`** (there is no separate Замок). **`Granary1` is the Кладовая**, not a grain silo.

## Rules (decided)
- Tiers are SEPARATE buildings (Забор vs Укреплённый забор, Башня vs Укреплённая башня), and each
  ALSO has levels. Every level changes stats AND model; a missing level model shows the nearest lower one.
- Categories are exactly these seven (`BuildingCategory` City/Storage/Entertainment/Defence/Production/Food/Water).
- Стена and Хижина охотника were removed. Saves may break on id renames — accepted.
- Footprints were measured from the user's boxes and are written in `CreateBuildingData` calls —
  **do not "tidy" them** («сейчас здание 2х2, таким и должно остаться»).

## Code
`Buildings/BuildingData.cs` (ScriptableObject: category, footprint, per-level stats, costs,
`requires`, `connectsToFences`), `Buildings/BuildingIds.cs`, `Buildings/BuildingInstance.cs`
(runtime: level, health, decay, `TryDamage`, `TryUpgrade`, `Demolish`, static counts).
SetupProject: `CreateBuildingData` (all 49 call sites), `CreateAuthoredModelPrefab`,
`AddLevelModels`, `ResolveBuildingRequirements`; procedural fallbacks in `SetupProjectMotifs.cs`
(30 motifs) + `CreateHutPrefab/FortificationPrefab/PlotPrefab/RoadPrefab/WaterBuildingPrefab`.

## Data
`Balance/buildings.csv` — one row per building, numbers only: costs per resource, levels `_2.._5`,
`requires`, `storage_*`, `service_radius`, `happiness`, upgrade costs `up2_..up5_`. Shape (footprint,
height, prefab) stays in SetupProject.

## Tests
EditMode `BuildingDetailTests`; PlayMode `AuthoredModelTests` (click box = drawn model; no missing
material), `RotatedFootprintTests`, `BuildingLookTests`, `BuildingDrawCountTests`.

## Traps
- `CreateLitMaterial` used to delete-and-recreate by path → the whole fence shipped MAGENTA with
  every test green. It now returns the existing material for a repeated name; guarded by
  `AuthoredModelTests.NoBuildingDrawsWithAMissingMaterial`.
- A building's comment is a CLAIM, not documentation (Demolish's comment lied about workers).
