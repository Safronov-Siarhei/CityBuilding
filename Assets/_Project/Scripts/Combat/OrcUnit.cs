using System.Collections.Generic;
using CityBuilder.Buildings;
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
    public class OrcUnit : MonoBehaviour
    {
        // Level 1 values; an orc's actual stats are these scaled by its Level (see Initialize).
        // Raids spawn level 1 -- higher levels currently only come from the OrcSpawn cheat, and
        // are the groundwork for the backlog's "raid strength scales with player progression".
        private const int BaseMaxHealth = 20;
        private const int BaseAttackDamage = 4;
        private const float AttackIntervalSeconds = 1.2f;
        // Generous on purpose: a target building's transform sits at its footprint CENTER, and a
        // NavMeshObstacle carve stops the route a bit short of that -- for the biggest buildings
        // (Town Hall, 4x4) the gap can exceed a tight melee range. Simpler to over-range slightly
        // than to chase exact edge-of-footprint contact for a first pass.
        private const float AttackRange = 3f;
        private const float WalkSpeed = 1.3f;
        private const float ArrivalThreshold = 0.1f;
        private const float RetargetIntervalSeconds = 5f;

        private static readonly List<OrcUnit> _all = new List<OrcUnit>();
        public static IReadOnlyList<OrcUnit> All => _all;

        private static NavMeshPath _sharedPath;

        private CharacterController _controller;
        private BuildingInstance _target;
        private Vector3[] _route = { Vector3.zero };
        private int _routeIndex;
        private float _attackTimer;
        private float _retargetTimer;

        public int CurrentHealth { get; private set; } = BaseMaxHealth;

        /// <summary>1 unless raised at spawn time. Scales both health and attack damage linearly.</summary>
        public int Level { get; private set; } = 1;

        private int _maxHealth = BaseMaxHealth;
        private int _attackDamage = BaseAttackDamage;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        /// <summary>Optional -- an orc left uninitialized is a perfectly valid level 1. Called right after the component is added, before its first Update.</summary>
        public void Initialize(int level)
        {
            Level = Mathf.Max(1, level);
            _maxHealth = BaseMaxHealth * Level;
            _attackDamage = BaseAttackDamage * Level;
            CurrentHealth = _maxHealth;
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
            _retargetTimer -= Time.deltaTime;
            if (_target == null || _retargetTimer <= 0f)
            {
                AcquireTarget();
                _retargetTimer = RetargetIntervalSeconds;
            }
            if (_target == null) return;

            var toTarget = _target.transform.position - transform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude <= AttackRange)
            {
                TickAttack();
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

        private void TickAttack()
        {
            _attackTimer -= Time.deltaTime;
            if (_attackTimer > 0f) return;
            _attackTimer = AttackIntervalSeconds;

            _target.TryDamage(_attackDamage);
            // TryDamage updates CurrentHealth synchronously even though the resulting Destroy()
            // is deferred to end of frame -- safe to read right away to drop a dead target instead
            // of waiting a frame to notice.
            if (_target.CurrentHealth <= 0) _target = null;
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
                _controller.Move(direction * WalkSpeed * Time.deltaTime);
                PinToGroundHeight();
            }
            else
            {
                transform.position += direction * WalkSpeed * Time.deltaTime;
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
            _route = _target != null ? BuildRoute(transform.position, _target.transform.position) : new[] { transform.position };
        }

        private static Vector3[] BuildRoute(Vector3 from, Vector3 to)
        {
            _sharedPath ??= new NavMeshPath();
            if (NavMesh.CalculatePath(from, to, NavMesh.AllAreas, _sharedPath) && _sharedPath.corners.Length > 0)
            {
                return _sharedPath.corners;
            }
            return new[] { to };
        }
    }
}
