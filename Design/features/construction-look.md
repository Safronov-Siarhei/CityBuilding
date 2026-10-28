# Construction look (building site with scaffolding, the finish effect)

> **Status (2026-10-28):** in code, `PlayMode/ConstructionLookTests` (+ photos). Both finishes made for
> the user to pick in the settings (row «Конец стройки»: Пыль / Салют); NOT picked yet. Not seen on a phone.
> **Read with:** [sandbox-run-systems](sandbox-run-systems.md) (the timed build/upgrade itself,
> `ConstructionSite`), [building-placement](building-placement.md) (the ink placement, `DustPuff`).

## What the user decided (2026-10-28)
- «строительная площадка с лесами -> эффект при окончании таймера стройки -> готовое здание».
  NOTHING revealed bit by bit (rising/clipping the model was rejected: «несёт свои риски»).
- Everywhere (sandbox and run), for building AND upgrading. Scaffolding must not go into a neighbour
  (the user's worry) -> it stays inside the building's own cells. No builders walking to it.
- Two finishes shown in the game to choose from (dust / fanfare); the third idea (ink sketch filling
  with colour) dropped with the gradual reveal.

## How it works
- `ConstructionSite.Begin` -> `ConstructionLook.Raise(building, upgrade)`; `Finish` applies the level
  (onDone) THEN `ConstructionLook.Complete()`, so the finish shows the new model.
- Build: the level model's renderers are disabled (`BuildingLevelAppearance.ModelFor`), the site =
  stone foundation, a begun ring of wall, planks and a stone heap, inside scaffolding. Upgrade: the
  building stays, scaffolding round it a little over the roof (`HeightFor(level+1)` vs click box).
- Scaffolding is generated from `footprintSize x CellSize` (local space, so rotation is free): poles
  every <=1.6 m inset 0.06 m, a deck inward of the poles and a rail every 1.1 m, a brace in every
  other bay. Two meshes per site (wood, stone; shared Lit materials) = 2 draws.
- `BuildFinishEffect.Play(..., GameSettings.BuildFinish)`: Dust = billboards round the footprint +
  thrown planks with gravity + the model squashed 0.82 -> 1.12 -> 1; Fanfare = gold flash billboard,
  ring of falling sparks, the model popped 0.15 -> 1.1 -> 1. Uses `DustPuff.Quad/Material` (internal);
  unscaled time; ~1.5 s, removes itself; restores the model's scale if cut short.
- Setting `GameSettings.BuildFinish` (PlayerPrefs `settings.build_finish`, default Dust); the settings
  card grew to 890 for its row (`SettingsPanelController.finishRow`). Keys `#settings_build_finish`,
  `#settings_finish_dust`, `#settings_finish_fanfare` (CSV + sheet).

## Traps
- Only buildings with a `BuildingLevelAppearance` are hidden under their site; one without it just
  gets scaffolding over itself.
- A building put down by `PlaytestWorld.Place` / `InvasionMode.PlaceFinished` has no site, so no look.
- The thrown planks and motes are separate GameObjects for 1.5 s (up to ~55 per finish) -- fine for
  one building; a save loading many half-built buildings shows no finish until each ends.

## Open
- The user picks Dust or Fanfare; then the other one and the settings row can go.
- Big/special buildings may want their own site models later.
