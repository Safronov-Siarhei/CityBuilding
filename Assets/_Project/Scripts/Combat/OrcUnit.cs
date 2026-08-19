using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Grid;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Combat
{
    /// <summary>
    /// First-pass enemy combat unit for the Orc raid system (see the design backlog's "Win
    /// condition: portals + Orc faction"). Spawned by OrcRaidManager, paths to the nearest player
    /// building over the same baked NavMesh CitizenAgent uses, and deals damage to it on contact
    /// until either it or the building dies -- see BuildingInstance.TryDamage and
    /// DefensiveBuilding (the only thing that can damage an OrcUnit back, for now). Deliberately
    /// narrow scope for this first slice: no base to clear, no player army to fight yet, and
    /// units only ever engage buildings, never citizens. All combat numbers here are first-pass,
    /// tunable.
    /// </summary>
    public class OrcUnit : MonoBehaviour, IDamageTarget
    {
        // Stats come from the balance sheet's "orc" row (see BalanceConfig); the fields below cache
        // them per orc at Awake. Level scales health and damage on top -- raids spawn level 1, and
        // higher levels currently only come from the OrcSpawn cheat.
        /// <summary>The row this unit reads in the balance sheet's units tab.</summary>
        private const string SheetId = "orc";

        private const float ArrivalThreshold = 0.1f;
        private const float RetargetIntervalSeconds = 5f;
        // Much shorter than RetargetIntervalSeconds: buildings don't move, soldiers do, and a
        // five-second reaction to being attacked reads as the orc simply not noticing.
        private const float SoldierScanIntervalSeconds = 0.4f;
        // How far around itself a stuck orc looks for the thing blocking its way. Roughly one
        // building away: wide enough to catch the wall it just walked up to, narrow enough that it
        // can't reach past that wall for something behind it.
        private const float BlockerReachRadius = 2.5f;

        /// <summary>
        /// How far past its own aggro radius an orc will chase whoever is SHOOTING it, as a
        /// multiple of that radius. Bounded rather than unlimited for two reasons: FightSoldier
        /// closes in a straight line rather than over the NavMesh, which is honest over a few
        /// metres and would walk through a wall over twenty; and an unleashed orc could be kited
        /// across the whole map, away from the town it came for.
        /// </summary>
        private const float RetaliationLeashMultiplier = 2.5f;

        private static readonly List<OrcUnit> _all = new List<OrcUnit>();
        public static IReadOnlyList<OrcUnit> All => _all;

        private static NavMeshPath _sharedPath;

        private CharacterController _controller;
        private BuildingInstance _target;
        private SoldierUnit _soldierTarget;
        private Vector3[] _route = { Vector3.zero };
        private int _routeIndex;
        /// <summary>The route stops short of the target because something unwalkable is in between -- see BuildRoute.</summary>
        private bool _routeBlocked;
        private float _attackTimer;
        private float _retargetTimer;
        private float _soldierScanTimer;

        /// <summary>The soldier it is after was handed over by being hit rather than found by the scan, so the scan must not immediately forget it -- see NotifyAttackedBy.</summary>
        private bool _retaliating;

        public int CurrentHealth { get; private set; }

        // IDamageTarget: what the player's army sees when it looks for something to hit. An orc is
        // a unit, not a structure, so a group set to prioritise structures walks past it.
        Transform IDamageTarget.Transform => this != null ? transform : null;
        bool IDamageTarget.IsAlive => this != null && CurrentHealth > 0;
        bool IDamageTarget.IsStructure => false;
        void IDamageTarget.TakeDamage(int amount) => TakeDamage(amount);

        /// <summary>1 unless raised at spawn time. Scales both health and attack damage linearly.</summary>
        public int Level { get; private set; } = 1;

        private int _maxHealth;
        private int _attackDamage;
        private int _baseMaxHealth;
        private int _baseAttackDamage;
        private float _attackIntervalSeconds;
        // Generous on purpose: a target building's transform sits at its footprint CENTER, and a
        // NavMeshObstacle carve stops the route a bit short of that -- for the biggest buildings
        // (Town Hall, 4x4) the gap can exceed a tight melee range. That's why the sheet gives orcs
        // a wider reach against structures than against soldiers.
        private float _buildingAttackRange;
        private float _soldierAttackRange;
        // A raid is still after the town, not the garrison -- but an orc being stabbed has to hit
        // back, or the player's militia would kill raiders without a scratch. So: a soldier within
        // this radius takes precedence over the building the orc was walking to, and the orc goes
        // back to that building once nothing is close any more. No pursuit beyond the radius.
        private float _soldierAggroRadius;
        private float _walkSpeed;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();

            // In Awake, not Initialize: an orc left uninitialized is a valid level 1 and still
            // needs its stats.
            var stats = BalanceConfig.Instance.Unit(SheetId);
            _buildingAttackRange = stats.attackRangeStructures;
            _soldierAttackRange = stats.attackRangeUnits;
            _soldierAggroRadius = stats.engageRadius;

            // An orc's "level" is the raid's own scaling (see Initialize), not a researched one --
            // the sheet's per-level columns belong to the player's army, and the orcs' row leaves
            // them empty, so level 1 is the whole of their balance.
            var level1 = stats.LevelStats(1);
            _baseMaxHealth = level1.maxHealth;
            _baseAttackDamage = level1.attackDamage;
            _attackIntervalSeconds = level1.attackIntervalSeconds;
            _walkSpeed = level1.walkSpeed;

            _maxHealth = _baseMaxHealth;
            _attackDamage = _baseAttackDamage;
            CurrentHealth = _maxHealth;
        }

        /// <summary>Optional -- an orc left uninitialized is a perfectly valid level 1. Called right after the component is added, before its first Update.</summary>
        public void Initialize(int level)
        {
            Level = Mathf.Max(1, level);
            _maxHealth = _baseMaxHealth * Level;
            _attackDamage = _baseAttackDamage * Level;
            CurrentHealth = _maxHealth;
        }

        /// <summary>Puts a loaded orc back on the health it had, clamped into what its level allows. Never below 1 -- a raider restored dead would stand in the field forever, since only TakeDamage removes one.</summary>
        public void SetCurrentHealth(int health)
        {
            CurrentHealth = Mathf.Clamp(health, 1, _maxHealth);
        }

        private void OnEnable()
        {
            _all.Add(this);
        }

        private void OnDisable()
        {
            _all.Remove(this);
        }

        private void Start()
        {
            AcquireTarget();
        }

        private void Update()
        {
            // Defenders in reach come first -- see SoldierAggroRadius.
            _soldierScanTimer -= Time.deltaTime;
            if (_soldierTarget == null || _soldierTarget.CurrentHealth <= 0 || _soldierScanTimer <= 0f)
            {
                _soldierScanTimer = SoldierScanIntervalSeconds;

                // A target handed over by being SHOT is kept while the chase is still worth it.
                // Without this exception the very next scan -- which looks no further than the
                // aggro radius -- would forget the archer standing eight metres away killing it.
                if (!_retaliating || !StillWorthChasing(_soldierTarget))
                {
                    _retaliating = false;
                    _soldierTarget = FindNearestSoldierInAggroRange();
                }
            }

            if (_soldierTarget != null)
            {
                FightSoldier();
                return;
            }

            _retargetTimer -= Time.deltaTime;
            if (_target == null || _retargetTimer <= 0f)
            {
                AcquireTarget();
                _retargetTimer = RetargetIntervalSeconds;
            }
            if (_target == null) return;

            var toTarget = _target.transform.position - transform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude <= _buildingAttackRange)
            {
                TickAttack();
                return;
            }

            // Walked the whole route and the target is still out of reach: something the orc can't
            // path around is between them. Take it out on that instead of standing there.
            if (_routeBlocked && _routeIndex >= _route.Length - 1 && ReachedLastWaypoint())
            {
                AttackWhateverIsBlockingTheWay();
                return;
            }

            AdvanceAlongRoute();
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || CurrentHealth <= 0) return;

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            if (CurrentHealth <= 0) Destroy(gameObject);
        }

        /// <summary>
        /// Told that a soldier just hit it, and turns on that soldier even when the soldier stands
        /// outside its own aggro radius.
        ///
        /// This is the only thing that stops the archer tier being free. An archer shoots from
        /// eight metres and an orc looks six, so without retaliation a line of archers would take a
        /// raid apart without ever being touched, and the tier would be strictly better than the
        /// others rather than a trade.
        ///
        /// A fight already in progress wins: an orc with a spearman in its face does not turn its
        /// back on him to walk towards an archer.
        /// </summary>
        public void NotifyAttackedBy(SoldierUnit attacker)
        {
            if (attacker == null || attacker.CurrentHealth <= 0 || CurrentHealth <= 0) return;
            if (_soldierTarget != null && _soldierTarget.CurrentHealth > 0) return;

            _soldierTarget = attacker;
            _retaliating = true;
            _soldierScanTimer = SoldierScanIntervalSeconds;
        }

        /// <summary>Whether the soldier it is chasing is alive and still inside the retaliation leash.</summary>
        private bool StillWorthChasing(SoldierUnit soldier)
        {
            if (soldier == null || soldier.CurrentHealth <= 0) return false;

            var leash = _soldierAggroRadius * RetaliationLeashMultiplier;
            return (soldier.transform.position - transform.position).sqrMagnitude <= leash * leash;
        }

        /// <summary>
        /// Closes on the nearby soldier and swings at it. Deliberately a straight-line approach
        /// rather than a NavMesh route: the target is by definition within SoldierAggroRadius, and
        /// planning a path every time a defender shuffles a step would cost more than it buys.
        /// </summary>
        private void FightSoldier()
        {
            var toSoldier = _soldierTarget.transform.position - transform.position;
            toSoldier.y = 0f;
            var distance = toSoldier.magnitude;

            if (distance <= _soldierAttackRange)
            {
                _attackTimer -= Time.deltaTime;
                if (_attackTimer > 0f) return;
                _attackTimer = _attackIntervalSeconds;

                _soldierTarget.TakeDamage(_attackDamage);
                if (_soldierTarget.CurrentHealth <= 0) _soldierTarget = null;
                return;
            }

            var direction = toSoldier / distance;
            if (_controller != null && _controller.enabled)
            {
                _controller.Move(direction * _walkSpeed * Time.deltaTime);
                PinToGroundHeight();
            }
            else
            {
                transform.position += direction * _walkSpeed * Time.deltaTime;
            }

            // The building route was planned from where this orc used to stand; force a replan
            // once the fight is over instead of resuming a route that no longer starts here.
            _retargetTimer = 0f;
        }

        private SoldierUnit FindNearestSoldierInAggroRange()
        {
            SoldierUnit nearest = null;
            var nearestDistSq = _soldierAggroRadius * _soldierAggroRadius;

            foreach (var soldier in SoldierUnit.All)
            {
                if (soldier == null || soldier.CurrentHealth <= 0) continue;

                var distSq = (soldier.transform.position - transform.position).sqrMagnitude;
                if (distSq > nearestDistSq) continue;

                nearestDistSq = distSq;
                nearest = soldier;
            }

            return nearest;
        }

        private void TickAttack()
        {
            _attackTimer -= Time.deltaTime;
            if (_attackTimer > 0f) return;
            _attackTimer = _attackIntervalSeconds;

            _target.TryDamage(_attackDamage);
            // TryDamage updates CurrentHealth synchronously even though the resulting Destroy()
            // is deferred to end of frame -- safe to read right away to drop a dead target instead
            // of waiting a frame to notice.
            if (_target.CurrentHealth <= 0) _target = null;
        }

        /// <summary>Standing on the end of the route, i.e. this is as far as it goes.</summary>
        private bool ReachedLastWaypoint()
        {
            var toWaypoint = _route[_route.Length - 1] - transform.position;
            toWaypoint.y = 0f;
            return toWaypoint.magnitude < ArrivalThreshold * 4f;
        }

        /// <summary>Walks toward the current route waypoint, advancing to the next one on arrival. Purely locomotion -- Update already decided this frame isn't an attack frame before calling this.</summary>
        private void AdvanceAlongRoute()
        {
            var waypoint = _route[_routeIndex];
            var toWaypoint = waypoint - transform.position;
            toWaypoint.y = 0f;
            var distance = toWaypoint.magnitude;

            if (distance < ArrivalThreshold)
            {
                if (_routeIndex < _route.Length - 1) _routeIndex++;
                return;
            }

            var direction = toWaypoint / distance;
            if (_controller != null && _controller.enabled)
            {
                _controller.Move(direction * _walkSpeed * Time.deltaTime);
                PinToGroundHeight();
            }
            else
            {
                transform.position += direction * _walkSpeed * Time.deltaTime;
            }
        }

        private void PinToGroundHeight()
        {
            var grid = GridManager.Instance;
            if (grid == null) return;

            var pos = transform.position;
            pos.y = grid.GroundHeight;
            transform.position = pos;
        }

        /// <summary>Nearest surviving BuildingInstance in the whole scene -- no "aggro range" concept yet, an orc always knows where the closest building is.</summary>
        private void AcquireTarget()
        {
            BuildingInstance nearest = null;
            var nearestDistSq = float.MaxValue;

            foreach (var instance in FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
            {
                // Roads/bridges are technically BuildingInstances too but not real targets -- a
                // raid beelining for the nearest stray road tile instead of an actual building
                // would make combat feel broken rather than threatening.
                if (instance.Data == null || instance.Data.isRoad) continue;
                var distSq = (instance.transform.position - transform.position).sqrMagnitude;
                if (distSq < nearestDistSq)
                {
                    nearestDistSq = distSq;
                    nearest = instance;
                }
            }

            _target = nearest;
            _routeIndex = 0;
            _routeBlocked = false;
            _route = _target != null
                ? BuildRoute(transform.position, _target.transform.position, out _routeBlocked)
                : new[] { transform.position };
        }

        /// <summary>
        /// The nearest building close enough to be what's standing in the way -- called once the
        /// orc has walked its route out and still can't reach what it was heading for. That is what
        /// makes a fence a real obstacle instead of scenery: the raider stops at it and starts
        /// breaking it rather than pretending the way is open.
        /// </summary>
        private void AttackWhateverIsBlockingTheWay()
        {
            BuildingInstance blocker = null;
            var nearestDistSq = BlockerReachRadius * BlockerReachRadius;

            foreach (var instance in FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
            {
                if (instance.Data == null || instance.Data.isRoad) continue;

                var distSq = (instance.transform.position - transform.position).sqrMagnitude;
                if (distSq > nearestDistSq) continue;

                nearestDistSq = distSq;
                blocker = instance;
            }

            if (blocker == null) return;

            _target = blocker;
            _routeIndex = 0;
            _routeBlocked = false;
            _route = new[] { blocker.transform.position };
        }

        /// <summary>
        /// A NavMesh route, and whether it actually gets there. `blocked` is the important half:
        /// buildings carve themselves out of the NavMesh (BuildingInstance.SetupNavMeshObstacle),
        /// so a walled-off target yields a PARTIAL path that stops at the wall. This used to return
        /// a straight line to the target in that case, which walked raiders through solid walls.
        ///
        /// A path that can't be computed at all is different -- that means there is no NavMesh
        /// under this orc (see MeshMapApplier.BuildNavMesh, which logs when the bake fails), not
        /// that something is in the way, so the straight-line fallback still applies there.
        /// </summary>
        private static Vector3[] BuildRoute(Vector3 from, Vector3 to, out bool blocked)
        {
            _sharedPath ??= new NavMeshPath();
            if (NavMesh.CalculatePath(from, to, NavMesh.AllAreas, _sharedPath) && _sharedPath.corners.Length > 0)
            {
                blocked = _sharedPath.status != NavMeshPathStatus.PathComplete;
                return _sharedPath.corners;
            }

            blocked = false;
            return new[] { to };
        }
    }
}
