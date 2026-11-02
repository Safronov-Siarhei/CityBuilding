# Combat core: damage, towers, projectiles, routes

> **2026-11-02 WALKABLE WALLS -- DECIDED, models in DRAFT (`Tools/blender/walls_walk.py`, not exported), no code yet.**
> The user's rules: wall stays 1x1; lvl1 palisade, lvl2 low stone (as now); from lvl3 a thick wall
> (WALK_T 0.78) with merlons on BOTH faces and a paved walk; height 0.9/1.1/1.3 cells for lvl3/4/5
> (approved), dark stone, no light bands, merlons every 0.2. Ranged soldiers AND ranged heroes walk on
> it; 1 cell = 1 standing fighter, others pass through to free cells. Up only through a Tower/Fortified
> Tower of level 3+ standing next to the wall: its model swaps to the WALL version (stair tower /
> round tower), a doorway ("-Door" piece) ONLY on the sides a wall really joins; a tower of lvl1-2
> stays as it is and is no way up. Towers keep shooting. NO round towers (the user, v3): the 2x2 Fortified wall tower is square so walls butt flat; it can take a doorway per cell of each side. Gate lvl3+ in a wall = gatehouse, the walk
> runs over it. Orders: option A (select ranged units, tap a wall cell, they go via the nearest wall
> tower); in a defence ranged units seek high ground on their own and climb walls if they can.
> Proposed (not yet confirmed): melee orcs can't reach them, throwers can; +range on a wall; a fighter
> falls when his wall cell is destroyed. Movement: a graph of wall cells, not a NavMesh on top.

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
  strikes the fence/gate there. Orc's own route (`OrcUnit.BuildRoute`) snaps its target onto the
  NavMesh first (5 m): a building's middle is inside its carve, the path failed and the straight-line
  fallback walked through gates (2026-10-23). A melee orc hits a building only within
  `orc_melee_reach_meters` of its collider (`CanStrike`/`DistanceToWall`); throwers still from range of
  the middle. The blocker it breaks is the building whose WALL is nearest (fence before tower). A soldier whose route is Blocked attacks
  the nearest orc wall within 2.5 m.

- (2026-11-02) Every creature shows its health bar once it fights, not only once hurt -- heroes too
  (OrcUnit.StrikeAt, SoldierUnit.TickAttack -> ShowHealth). Melee reach adds the target's half-width
  (`SoldierUnit.BodyRadius`, collider extents - 0.3) so a boss is struck at its edge. A hero on the
  player's move order picks up no new fight until he arrives (`SoldierUnit.Marching`). In a dungeon:
  engage radius x `dungeon_engage_factor`, shooters' reach x `dungeon_ranged_range_factor`, no
  target behind rock (NavMesh.Raycast).

- (2026-11-02) No walking over water: when there is no path, the straight-line fallback is refused if
  it crosses a water cell (`MeshMapApplier.LineCrossesWater`) -- `NavRoute` (heroes, soldiers, orcs)
  stays put (Blocked), `CitizenAgent.BuildRoute` "arrives" at its own feet so a gatherer gives the tree
  up. A target out at sea snaps to the nearest land within 40 m, so the partial path ends on the shore.

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
