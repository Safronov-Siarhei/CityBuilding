# Ruins of the player's buildings (Руины)
Status: BUILT 2026-11-05 (look A approved by the user). No tests yet, the user has not seen it in the game.

## The user's rules (2026-11-05)
- A building of the player's destroyed by enemies does not vanish. **Ruins** stay on its plot; they can be selected and have a card.
- The card has three buttons:
  - **Восстановить**, with the price above it: the building is rebuilt;
  - **Снести**: the ruins are removed for good;
  - **Закрыть**: decide later.
- The restore price = the building's full worth × `ruin_restore_cost_fraction` (0.5; sandbox tab). The full worth is its price plus every upgrade up to its level, **coins included** (unlike repair).
- **Ruins from EVERYTHING**, roads, fences, walls and decor included.
- **Restoring works like construction, but faster**: `BuildOnTimer` for (sum of `build_sec` up to its level) × `ruin_restore_time_fraction` (0.5; sandbox tab).
- **Sandbox only**: the Invasion mode stays as it is.
- **The bot's buildings leave ruins too** (2026-11-06): `IsBot` ruins, not the player's (no card, not saved); the bot raises them itself -- see bot.md.
- **The Town Hall gives no ruins**: losing it loses the game.
- **Look A, «обгоревший остов»** (the user's choice).

## How it works (as built)
- `BuildingInstance.TryDamage` → `BuildingRuin.LeavesRuin` (not bot, not Town Hall, not moat, not Invasion) → the cells it held (minus shared ones: road under a gate, vein, moat under a bridge) are kept; a road's cells leave `RoadNetwork`; the building is destroyed as before; `BuildingRuin.LeaveOf` copies its active meshes that use `CityBuilder/BuildingDetail` (its level, a fence's shape; not bars/markers/scaffolding).
- Look: shader `Shaders/BuildingRuin.shader` — the building's own mesh clipped above a jagged world-Y break (`_RuinCut`, `_RuinJag`, `_RuinSeed`), burnt colours (`_Char`), soot near the break, Cull Off with darker insides; shadow/depth passes clip too. Meshes are not readable on the device, so the cut lives on the GPU. Low things (roads, low fences) are burnt but not broken. Rubble (stone boxes and beams) and an ash plate are made in code; none on roads.
- Material `Resources/BuildingRuin.mat` made by `SetupProject.EnsurePaletteMaterial`; one material instance per ruin.
- Shape per ruin is seeded by its cell and name (`StableHash`), so a load looks the same.
- Tap: `BuildingSelector.FindRuin` (BoxCollider on the ruin) → `UI/RuinCard` (built in `SetupProjectPanels.BuildRuinCard`): title, line, «Восстановление:» + cost row, Restore (greyed when unaffordable) / Clear / Close.
- Restore: `TrySpend`, free cells, respawn as a save does (Initialize, SetLevel, occupy, roads), `BuildOnTimer`.
- Save: `GameSaveData.ruins` (`RuinEntry`: name, cell, rotation, level, pieces as prefab paths + local transforms, cells). Load finds the meshes in the prefab by path — nothing of the prefab comes to life.
- Ruins produce nothing; enemies ignore them (not a BuildingInstance). Demolish refunds nothing.

## Files
- Scripts/Buildings/BuildingRuin.cs, Scripts/UI/RuinCard.cs, Shaders/BuildingRuin.shader
- BuildingInstance.TryDamage, BuildingSelector (FindRuin), SaveData (RuinEntry), GameSaveController (save/load), BalanceConfig (RuinRestoreCostFraction/TimeFraction), SetupProject (ruin material), SetupProjectPanels/GameplayUI (card)
- Balance: sandbox `ruin_restore_cost_fraction`, `ruin_restore_time_fraction`; localization `#ruin_*`, `#log_ruin_*`
- Design: Tools/blender/ruins_preview.py, Design/ruins_preview.png (the approved look)

## Traps
- `Resources` inside `CityBuilder.*` is the game's namespace: use `UnityEngine.Resources.Load`.
- A building destroyed while still under its scaffolding has no BuildingDetail meshes showing: its ruin is rubble only.
- Ruins are not BuildingInstances: anything that counts buildings (happiness, raids' targets, tests) does not see them.

## Open
- The user has not seen it; break height (0.30–0.42 of the height) and char strength may need tuning.
- Where repair (`repair_cost_fraction` 0.4) and restore meet is moot now: a damaged building repairs, a destroyed one restores.
- Tests: a check in the general run (destroy → ruin → restore/clear, save/load).
