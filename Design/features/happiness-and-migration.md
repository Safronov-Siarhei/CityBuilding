# Happiness (довольство) and migration

> **2026-10-25: BOTH OFF** (`HappinessManager.InUse`, `MigrationManager.MovesPeople` = false; code kept, the settling-in grace still runs) -- the houses make the people now: [sandbox-run-systems](sandbox-run-systems.md).

> **Status (2026-10-21):** six factors wired, migration reads them. Weights are a flat average and
> the audit says two factors barely move — the fix is FACTOR WEIGHTS, not the migration curve.
> **Read with:** [citizens-and-workforce](citizens-and-workforce.md), [food-and-hunger](food-and-hunger.md).

## What the player sees
A round number top-right (0–100); tap → breakdown per factor. Under it, the migration countdown
(next arrival / departure).

## Rules
- Factors (user's list, 2026-08-12): tax (100 − rate), decay (average over decaying buildings),
  defence (Σ defence×level vs 0.5 per citizen — still a C# const `DefensePerCitizenTarget`, should move
  to the sheet), food variety, recent deaths, entertainment (Σ buildings' `happiness` vs
  `happiness_per_citizen_target` per citizen, settlement-wide, no radius). **Work week (7th)
  deferred** — needs a week. Flat average of whatever exists.
- The only consumer is `MigrationManager`: above `migration_happiness_threshold` settlers arrive
  (happier = sooner), below it people leave (unhappier = faster); the threshold itself is a dead point.
- Departures never count as deaths (no spiral), and the town never drops below
  `migration_min_population`. New towns wait `settling_in_seconds` before migration starts.
- Logs once at ≤30 % and once at ≥70 % (hysteresis).

## Code
`Core/HappinessManager.cs` (pure statics `ComputeTaxScore/DecayScore/DefenseScore`…),
`Citizens/MigrationManager.cs`, `UI/HappinessController.cs`, `UI/MigrationController.cs`.

## Data
economy: `happiness_per_citizen_target`, `migration_*`, `settling_in_seconds`, `deaths_memory_days`,
`happiness_penalty_per_death`, `food_variety_target`; buildings `happiness(_N)`.

## Tests
EditMode `HappinessManagerTests`, `MigrationBalanceTests`, `EntertainmentBalanceTests`; PlayMode `MigrationTests`.

## Open
- Town Hall's defence 20 pins the defence factor at 100 up to ~40 citizens → departures nearly
  unreachable (fresh town 48–67). Proposed: weights + drop the Town Hall from the defence factor.
- Happiness affecting production (backlog) — unbuilt. Seasons/leisure design — open, user to raise.
