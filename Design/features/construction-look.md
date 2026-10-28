# Construction look (the building wrapped in a net inside scaffolding, the dust finish)

> **Status (2026-10-28):** in code, `PlayMode/ConstructionLookTests` (+ photos; `construction-catalogue-N.png` = every hotbar building under the net). Round 2 after the
> user's photo of a house under renovation: net + scaffolding, the dust finish made «сочнее»; the
> fanfare finish and its settings row were made, rejected and deleted the same day. Not seen on a phone.
> **Read with:** [sandbox-run-systems](sandbox-run-systems.md) (the timed build/upgrade itself,
> `ConstructionSite`), [building-placement](building-placement.md) (the ink placement, `DustPuff`).

## What the user decided (2026-10-28)
- «как будто здание накрыто каким-то полотном, а вокруг строительные леса» (photo: green
  construction net over a facade, scaffolding round it) -> the dust finish, juicier -> the building.
- NOTHING revealed bit by bit (rising/clipping the model rejected: «несёт свои риски»).
- «добавляй в игру на все здания» (2026-10-28): every building with a build time -- all but the
  Castle (0 s, placed at the start) -- except what is walked on: Road and Bridge
  (`isRoad` / `providesWalkableSurface`, `ConstructionLook.Wraps`) get only the hammer timer.
- UPGRADE (the user's pick В of three, 2026-10-28): no net, the building in plain sight, scaffolding
  round it, a hoist on top winching a pallet of stones up and down; NO finish effect at its end
  («эффекты нужны чтобы обозначить новое здание»). An upgrading building already does not work
  (IsOperational: production, towers, hiring, healing, service radius, happiness); its storage and
  its residents stay -- proposed to keep that way, the user did not object.
- Everywhere (sandbox and run), building AND upgrading; scaffolding must not go into a neighbour
  -> it stays inside the building's own cells. No builders walking to it.
- Round 1 (same day) had a stone foundation + begun walls with the model hidden, and two finishes in
  a settings row (Пыль / Салют); the user picked Dust, wanted the net -> both replaced.

## How it works
- `ConstructionSite.Begin` -> `ConstructionLook.Raise(building, upgrade)`; `Finish` applies the level
  (onDone) THEN `ConstructionLook.Complete()`, so the finish shows the new model.
- Upgrade: `Scaffolding(..., hoistGap: true)` leaves the front (local -z) side's decks open over the
  first bay's middle; `Gallows` adds a pole, an arm and a drum with a crank there;
  `ConstructionHoist` (pallet + stones + rope, 3 more draws) cycles 4.2 s on game time: up loaded,
  unload on top, down empty, load. `Complete` plays no effect for an upgrade.
- The building stays visible under the net. Net = 4 hanging sides + a
  sagging cover, around the model's own bounds +0.06 m, clamped to the cells' edge -0.03 m (a model built out to
  its cells' edge gets the net over the poles rather than walls poking through); sags between poles
  and between decks, small ripple. Material: Lit transparent, procedural 32 px texture (green cloth,
  darker threads every 4 px, a darker band), mipmapped, tiled 2.6/m. Height = max(level model
  height, next level's on an upgrade, click box) + 0.08.
- Scaffolding from `footprintSize x CellSize` (local space): poles every <=1.6 m inset 0.06 m,
  a deck and an outer rail every 1.1 m, a brace in every other bay, 0.35 m over the net.
  2 meshes per site (wood, net) = 2 draws.
- `BuildFinishEffect.Play(building, model, footprint, height)` (~2 s, unscaled time, removes
  itself): lobed, top-lit dust puffs in two shades (ground ring rolling out, a column up the walls,
  a late slower puff), a dust ring running along the ground, thrown planks/poles/net scraps/stones
  with gravity and bounces, the model on a damped squash-and-stretch spring
  (y = 1 - 0.3 e^-5.5t cos 13t), four twinkles over the roof. Textures are painted once at runtime;
  billboards use `DustPuff.Quad()` (internal).

## Traps
- A low building's top rail (deck + 0.45) once stood over its poles' tops, in the air (the user's
  screenshots, 2026-10-28): rails are capped at the poles' height and braces kept under it; the
  catalogue test checks no wood is over the poles for every building.
- A building put down by `PlaytestWorld.Place` / `InvasionMode.PlaceFinished` has no site, so no look.
- The net is transparent Lit set up at runtime (`_SURFACE_TYPE_TRANSPARENT`); it relies on Lit being in
  Always Included Shaders with its variants (as Unlit's transparent dust does).
- One finish spawns ~60-110 short-lived GameObjects; fine for one building at a time.

## Open
- Big/special buildings may want their own site models later. Phone check of the net and the dust.
