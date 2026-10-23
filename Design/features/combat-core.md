# Combat core: damage, towers, projectiles, routes

> **Status (2026-10-21):** ranged shots are DRAWN since 2026-10-20 (`050f61a8`) and damage lands on
> IMPACT. Not seen on a phone.
> **Read with:** [army-and-soldiers](army-and-soldiers.md), [orc-raids-and-portals](orc-raids-and-portals.md).

## Rules
- Everything breakable implements `IDamageTarget` (buildings via `BuildingInstance.TryDamage`, portals,
  orc camp buildings, prisoners).
- `DefensiveBuilding`: any building with `defense > 0` shoots the nearest orc in range every
  `defence_attack_interval_seconds`, damage = its defence stat (same stat the happiness factor reads).
- **Projectiles:** towers, orc watchtowers/gate-towers and archers loose an ARROW; orc throwers a
  JAVELIN. Homing arc, flight 0.22–0.9 s, sticks 0.45 s, 5 chips on impact. A shot in flight at save
  time is lost. Pooled, built lazily in code, no shadows, no per-shot allocs; outside play mode damage
  is instant. The archer's `NotifyAttackedBy` travels with the arrow.
- `OrcUnit.NotifyAttackedBy` makes an orc turn on its attacker (leashed at 2.5× aggro) — without it
  archers are a free win.
- `NavRoute`: the one correct route walker (horizontal distances, skip corners underfoot, reject
  partial paths ending where they start). Target is SamplePosition'd 3 m (a structure's centre is
  inside its carve). Guards use `straightLineFallback:false`. `NavRoute.AreaMask`: an orc's route
  never crosses a gate (`NavAreas.WithoutGates`) -- chasing too, since 2026-10-23 (orcs walked in at a
  run's gate after citizens/soldiers); walled off at the end of a chase, `OrcUnit.BreakThrough`
  strikes the fence/gate there. A soldier whose route is Blocked attacks
  the nearest orc wall within 2.5 m.

## Code
`Combat/IDamageTarget.cs`, `Combat/DefensiveBuilding.cs`, `Combat/CombatProjectiles.cs`
(`ProjectileKind`), `Combat/NavRoute.cs`, `Core/RuntimeShaders.cs`; users in `SoldierUnit`, `OrcUnit`,
`OrcCampBuilding`.

## Tests
PlayMode `CombatProjectileTests` (+ photos `projectile-*.png`), `ArmyCombatTests` (incl.
`SoldiersBreakInRatherThanWalkThroughTheWall`; archer test measures max distance an orc walked alive).

## Traps
- A plain Gate has `defense` 0 on every level since 2026-10-22 (the user: a gate is a way through
  for allies and a wall for orcs, not a turret); the Fortified Gate still shoots.
CitizenAgent and OrcUnit still carry older hand-rolled route copies — port to `NavRoute` if they bug out.

## Open (audit 2026-08-23)
A tower out-classes the army (25 dps, no upkeep, kills an orc in 1 s). Towers don't shoot portals, so
soldiers exist mainly for assaults. Proposed: split tower/soldier roles. Soldier wall-breaching has no test.
