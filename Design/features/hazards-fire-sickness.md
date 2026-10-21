# Hazards: fire and sickness

> **Status (2026-10-21):** shipped 2026-08-19, numbers first-pass and NOT balanced (first playtest:
> 3 dead of illness by day 7 — clearly harsh).
> **Read with:** [building-levels-and-decay](building-levels-and-decay.md), [citizens-and-workforce](citizens-and-workforce.md).

## Rules (decided)
- **Fire chance IS decay** (user's call): a maintained town never burns; repair is fire insurance.
  Spread ignores the neighbour's decay (gaps between districts matter).
- An unattended fire burns `fire_burn_seconds`, eats a chunk of the building and stops. Each
  firefighter in reach (Пожарная бригада `service_radius`) subtracts seconds, floored — recomputed
  from the CURRENT crew. Fire damage goes through `BuildingInstance.TryDamage` (Town Hall lost to fire = defeat).
- **Fires are diced; illness is worked out**: illness chance = base + hungry + no well in reach.
  Sick people leave their jobs. The untreated streak counts days the BEDS WERE NOT CLEARED
  (one healer must not immunise a town of forty). Дом лекаря heals `heal_per_healer_per_day`.
- Well, Healer, Brigade share one per-level `service_radius` column (Well 14/18/24, Healer 16/20/26, Brigade 18/24/30).
- No sickness while the town settles in (easy start, 2026-10-20). Fires and illness are off in Invasion.
- Saved: sick count, untreated streak, every fire with ELAPSED time (remaining depends on the crew after load).

## Code
`Buildings/FireManager.cs`, `Buildings/BuildingFire.cs`, `Citizens/SicknessManager.cs`,
`Buildings/BuildingInstance.cs` (`TryDamage`).

## Data
economy: `fire_*`, `sickness_*`, `heal_per_healer_per_day`; buildings `service_radius(_N)`.

## Tests
EditMode `SicknessAndFireTests`; PlayMode `HazardTests`.
