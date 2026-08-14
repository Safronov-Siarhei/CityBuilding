using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Grid;
using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// One of the player's soldiers. Takes no orders of its own: it reads them off its ArmyGroup
    /// (where to hold, what to attack, which kind of enemy to prefer) and executes its own slice of
    /// them -- walk to my slot in the formation, engage what my group's priority says to engage.
    ///
    /// Two deliberate limits, both matching the decided design:
    /// - With no attack order, a soldier only engages what comes within its engage radius of its slot,
    ///   and never chases beyond it. A group left alone holds its ground; it doesn't trickle across
    ///   the map after raiders and get killed one at a time.
    /// - Soldiers only ever damage IDamageTarget implementors (orcs, portals). They have no path at
    ///   all to a player BuildingInstance, so no targeting bug can turn the army on its own town.
    /// </summary>
    public class SoldierUnit : MonoBehaviour
    {
        /// <summary>Below this (horizontal) the soldier is close enough to its slot to stop shuffling.</summary>
        private const float SlotArrivalDistance = 0.25f;

        /// <summary>Routes are replanned this often while pursuing -- an orc moves, and a route to where it used to be goes stale.</summary>
        private const float RouteRefreshSeconds = 1.5f;

        /// <summary>Re-scanning for a target every frame is wasted work at 20 soldiers x N orcs on a phone; a few times a second is indistinguishable in play.</summary>
        private const float RetargetIntervalSeconds = 0.4f;

        private static readonly List<SoldierUnit> _all = new List<SoldierUnit>();
        public static IReadOnlyList<SoldierUnit> All => _all;

        private readonly NavRoute _route = new NavRoute();

        private CharacterController _controller;
        private IDamageTarget _target;
        private float _attackTimer;
        private float _retargetTimer;
        private float _routeRefreshTimer;
        private Vector3 _routeDestination;
        private bool _hasRoute;

        public ArmyGroup Group { get; private set; }
        public SoldierType Type { get; private set; } = SoldierType.Militia;
        public int MaxHealth { get; private set; } = 1;
        public int CurrentHealth { get; private set; } = 1;

        // Copied out of the balance sheet once, at Initialize, rather than read through
        // BalanceConfig every frame -- with twenty soldiers each running this Update, the numbers
        // have to cost what a const cost.
        private float _unitAttackRange = 1.4f;
        private float _structureAttackRange = 2.6f;
        private float _engageRadius = 7f;
        private float _walkSpeed = 1.5f;
        private float _attackIntervalSeconds = 1.1f;
        private int _attackDamage = 1;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            _all.Add(this);
        }

        private void OnDisable()
        {
            _all.Remove(this);
        }

        /// <summary>Called by ArmyManager right after the GameObject is built, before the first Update.</summary>
        public void Initialize(SoldierType type, ArmyGroup group)
        {
            Type = type;
            Group = group;
            MaxHealth = SoldierStats.MaxHealth(type);
            CurrentHealth = MaxHealth;

            var stats = SoldierStats.Row(type);
            _unitAttackRange = stats.attackRangeUnits;
            _structureAttackRange = stats.attackRangeStructures;
            _engageRadius = stats.engageRadius;
            _walkSpeed = stats.walkSpeed;
            _attackIntervalSeconds = stats.attackIntervalSeconds;
            _attackDamage = stats.attackDamage;
        }

        private void Update()
        {
            if (Group == null) return;

            _retargetTimer -= Time.deltaTime;
            if (_target == null || !_target.IsAlive || _retargetTimer <= 0f)
            {
                _retargetTimer = RetargetIntervalSeconds;
                Group.ClearFinishedAttackOrder();
                _target = AcquireTarget();
            }

            if (_target != null && _target.IsAlive)
            {
                var targetPosition = _target.Transform.position;
                var range = _target.IsStructure ? _structureAttackRange : _unitAttackRange;

                if (HorizontalDistance(targetPosition, transform.position) <= range)
                {
                    FaceTowards(targetPosition);
                    TickAttack();
                    return;
                }

                WalkTowards(targetPosition);
                return;
            }

            var slot = Group.SlotPositionFor(this);
            if (HorizontalDistance(slot, transform.position) > SlotArrivalDistance) WalkTowards(slot);
        }

        /// <summary>
        /// The group's explicit order if it has one, otherwise the best enemy near this soldier's
        /// slot according to the group's priority mode. "Best" means: something in the preferred
        /// bucket if anything in that bucket is in range at all, and the nearest one within it --
        /// so a group set to Structures walks past the defenders to the portal, and one set to
        /// Units clears them first.
        /// </summary>
        private IDamageTarget AcquireTarget()
        {
            if (Group.AttackTarget != null && Group.AttackTarget.IsAlive) return Group.AttackTarget;

            var slot = Group.SlotPositionFor(this);
            IDamageTarget preferred = null;
            var preferredDistance = float.MaxValue;
            IDamageTarget fallback = null;
            var fallbackDistance = float.MaxValue;

            foreach (var candidate in EnumerateEnemies())
            {
                if (candidate == null || !candidate.IsAlive) continue;

                var distance = HorizontalDistance(candidate.Transform.position, slot);
                if (distance > _engageRadius) continue;

                var prefersStructures = Group.Priority == TargetPriority.Structures;
                var isPreferredBucket = candidate.IsStructure == prefersStructures;
                if (isPreferredBucket)
                {
                    if (distance >= preferredDistance) continue;
                    preferredDistance = distance;
                    preferred = candidate;
                }
                else
                {
                    if (distance >= fallbackDistance) continue;
                    fallbackDistance = distance;
                    fallback = candidate;
                }
            }

            return preferred ?? fallback;
        }

        /// <summary>Every hostile thing currently in the world. Both registries are self-maintaining lists, so this allocates nothing per scan beyond the iterator.</summary>
        private static IEnumerable<IDamageTarget> EnumerateEnemies()
        {
            foreach (var orc in OrcUnit.All)
            {
                yield return orc;
            }
            foreach (var portal in OrcPortal.All)
            {
                yield return portal;
            }
        }

        private void TickAttack()
        {
            _attackTimer -= Time.deltaTime;
            if (_attackTimer > 0f) return;
            _attackTimer = _attackIntervalSeconds;

            _target.TakeDamage(_attackDamage);
            if (!_target.IsAlive) _target = null;
        }

        /// <summary>Routes to a destination, replanning when the destination has moved or the plan has gone stale. Movement itself is one step along the current route.</summary>
        private void WalkTowards(Vector3 destination)
        {
            _routeRefreshTimer -= Time.deltaTime;
            var destinationMoved = HorizontalDistance(destination, _routeDestination) > 0.5f;

            if (!_hasRoute || destinationMoved || _routeRefreshTimer <= 0f)
            {
                _route.SetDestination(transform.position, destination);
                _routeDestination = destination;
                _routeRefreshTimer = RouteRefreshSeconds;
                _hasRoute = true;
            }

            if (!_route.TryGetDirection(transform.position, out var direction)) return;

            if (_controller != null && _controller.enabled)
            {
                _controller.Move(direction * _walkSpeed * Time.deltaTime);
            }
            else
            {
                transform.position += direction * _walkSpeed * Time.deltaTime;
            }

            PinToGroundHeight();
            FaceTowards(transform.position + direction);
        }

        private void FaceTowards(Vector3 worldPosition)
        {
            var toTarget = worldPosition - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.LookRotation(toTarget);
        }

        /// <summary>Same fixed-height pinning every other walking thing in this game uses -- gravity would otherwise settle units at slightly different heights and break horizontal distance checks.</summary>
        private void PinToGroundHeight()
        {
            var grid = GridManager.Instance;
            if (grid == null) return;

            var position = transform.position;
            position.y = grid.GroundHeight;
            transform.position = position;
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || CurrentHealth <= 0) return;

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            if (CurrentHealth > 0) return;

            // Killed, not disbanded: ArmyManager decides what that costs the settlement (a
            // recruited citizen is gone for good, unlike a disbanded one who walks home).
            ArmyManager.Instance?.NotifySoldierKilled(this);
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
