# Combat core: damage, towers, projectiles, routes

> **2026-11-02 WALKABLE WALLS -- BUILT 704b1803, untested, not seen in game.**
> **2026-11-02 (3252ff6f)**: a wall order's flag/rings stand ON the walk (`MoveOrderMarker.ShowFlagFor(..., onTop)`);
> HERO XP only from his own kills -- `OrcUnit.KilledBy` (the SoldierUnit whose blow/arrow felled it, via
> `TakeDamage(amount, by)`; towers/militia/spells pass null), `Hero.OnOrcDied` checks it (`king_xp_radius` now unread);
> level icons `UI/Icons/{Fence,Gate,Tower,FortifiedTower,FortifiedGate}_L2..5` re-rendered by `render_icons.py`
> from current models (Tower/Bastion 3-5 = their WALL look, Fence 3-5 = the Straight walk piece).
> **51e5cefd, APPROVED + INSTALLED**: `Tools/blender/keep_drafts.py` (no args = install into Castle1-lvl1..5 and
> FortifiedGate1-lvl3..5 + save; `-- <dir>` = renders): the Town Hall is a DONJON (5x5, keep 2.25+ high, corner
> turrets, slate roof -> blue from 3, gold 4-5); the BARBICAN 3-5 = passage + portcullis between two end towers,
> WALKABLE (`WallWalk.BarbicanId`: middle cell a Wall node, end cells Tower nodes = stairs at the walk's height).
> The BASTION and (since 2026-11-03) the plain TOWER wear their wall-tower body from level 3 even away from walls or beside a lvl1-2 fence (`WallTowerLook.lookAlways`, doorways per join).
> Moat: discussed, not built (user asked what "a blocking moat" means).
> Rules (the user): Fence/Gate from `wall_walk_from_level` (3) are thick walls (WALK_T 0.78) with merlons on
> both faces and a walk at `wall_walk_height_3..5` (0.9/1.1/1.3 cells); dark stone, juicy towers (tiled roof
> by level, timber storey, arched lit windows, banners, torches, flags). Tower/Fortified Tower lvl3+ against
> such a wall = the stair up (WallTower / square WallFortTower), doorways only where a wall joins; lvl1-2
> tower is no way up; towers keep shooting. Gate lvl3+ = gatehouse, the walk runs over it (FortifiedGate 3+
> walkable since 51e5cefd). Ranged soldiers + ranged heroes only; one per cell, others pass through. Order: select
> ranged, tap a wall/stair tower (ArmyOrderInput.TryOrderOntoWall); idle ranged ones go up on their own
> when an orc is within `wall_seek_radius_cells` (14) and a free reachable wall cell too. On the wall:
> +`wall_range_bonus_cells` (2) range, never chase; melee orcs can't reach them (OrcUnit.CanReach), throwers
> can; the wall cell destroyed under one -> he falls and dies. A ground move/attack order brings them
> down by the nearest stair first (ArmyGroup.OrderMoveTo/OrderAttack -> LeaveWall). Climb `wall_climb_sec`.
> Code: `Buildings/WallWalk.cs` (cell graph rebuilt on MarkDirty from BuildingInstance init/destroy/level;
> posts; BFS paths; stairs), `Buildings/WallTowerLook.cs` (hidden bodies + per-slot doors built into the
> Tower/FortifiedTower prefabs by `SetupProjectBuildings.AddWallTowerLook`; hides BuildingLevelAppearance via
> SetHidden), `Combat/SoldierUnitWalls.cs` (partial SoldierUnit: phases ToStair/Up/Walking/Posted/Down).
> Models: `Tools/blender/walls_walk.py` (no args = install into Fence1-lvl3..5-*, Gate1-lvl3..5 turned 90,
> WallTower*/WallFortTower* + -Door; `-- <dir>` = render draft images). Steps (ecc24d29): where walk walls of different levels meet, stone steps (WallSteps-3-4/3-5/4-5) stand on the LOWER cell half by the joint (WallWalk.PlaceSteps, parented to that wall, wired via BuildingPlacer.wallSteps); fighters follow them (SoldierUnitWalls.StepHeight); a tower doorway uses the JOINED WALL level, the stair node Top = highest joined wall. Traps: a save made with fighters on
> the walls loads them on the ground; Gate lvl3-5 measures 1.17 cells (open leaves overhang).


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
