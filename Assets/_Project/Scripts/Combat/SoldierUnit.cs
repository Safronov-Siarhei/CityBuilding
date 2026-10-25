using System;
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

        /// <summary>How far round itself a soldier whose way is walled off looks for the piece of orc wall to break.</summary>
        private const float BreachReachMeters = 2.5f;

        private static readonly List<SoldierUnit> _all = new List<SoldierUnit>();
        public static IReadOnlyList<SoldierUnit> All => _all;

        private readonly NavRoute _route = new NavRoute();
        private readonly StuckEscape _escape = new StuckEscape();

        private CharacterController _controller;
        private IDamageTarget _target;
        // A piece of an orc camp's wall standing between this soldier and its target: broken
        // first, then the walk goes on (see WalkTowards).
        private OrcCampBuilding _breach;
        private float _attackTimer;
        private float _retargetTimer;
        private float _routeRefreshTimer;
        private Vector3 _routeDestination;
        private bool _hasRoute;

        public ArmyGroup Group { get; private set; }

        /// <summary>Raised when one of this soldier's own blows kills its target -- what the King's experience is counted from (see Hero).</summary>
        public event Action<IDamageTarget> KilledTarget;

        /// <summary>Raised on every blow or shot, as it is dealt -- what a figure's strike is drawn from (FigureAnimator).</summary>
        public event Action Struck;

        /// <summary>Shoots rather than swings: its hits are drawn as arrows (see CombatProjectiles).</summary>
        public bool IsRanged => Type == SoldierType.Archer || Type == SoldierType.Queen;
        public SoldierType Type { get; private set; } = SoldierType.Militia;

        /// <summary>The level whose look the figure wears (ArmyManager: metal at 2, gold at 3).</summary>
        public int LookLevel { get; set; }
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

        /// <summary>What one of its blows does now.</summary>
        public int AttackDamage => _attackDamage;

        private float _shieldLeft, _rageLeft, _rageFactor = 1f;

        /// <summary>Takes no damage: a hero's own shield (Неуязвимость) or the King's Blessing over everything of the player's.</summary>
        public bool IsShielded => _shieldLeft > 0f || HeroBlessing.Active;

        public float ShieldLeft => _shieldLeft;
        public float RageLeft => _rageLeft;

        /// <summary>No damage gets through for this long (a hero's skill).</summary>
        public void Shield(float seconds) => _shieldLeft = Mathf.Max(_shieldLeft, seconds);

        /// <summary>Strikes `factor` times as often and as hard for this long (the King's Ярость).</summary>
        public void Enrage(float factor, float seconds)
        {
            _rageFactor = Mathf.Max(1f, factor);
            _rageLeft = Mathf.Max(_rageLeft, seconds);
        }

        private float RageFactor => _rageLeft > 0f ? _rageFactor : 1f;
        // the Captain's Сила round him adds to every blow struck near him (HeroAuras)
        private int StrikeDamage => Mathf.RoundToInt(_attackDamage * RageFactor * HeroAuras.MightAt(transform.position));

        private Vector3 _retreatTo;
        private float _retreatLeft;

        /// <summary>Backing off (the Queen from a melee enemy): walks there and strikes nobody until arrived or the time is up.</summary>
        public bool IsRetreating => _retreatLeft > 0f;

        public void Retreat(Vector3 to, float seconds)
        {
            _retreatTo = to;
            _retreatLeft = Mathf.Max(0.1f, seconds);
        }
        private float AttackInterval => _attackIntervalSeconds / RageFactor;

        /// <summary>Has an enemy in hand: one it was sent at, or one that came within its engage radius.</summary>
        public bool IsFighting => _target != null && _target.IsAlive;

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

            ReadStats();
            CurrentHealth = MaxHealth;
        }

        /// <summary>
        /// Puts a loaded soldier on the health it was saved with, clamped into what its type can
        /// have today -- a save made before the militia's level 2 was researched holds a smaller
        /// number than the unit now has room for, and one made after holds a bigger one.
        /// Never below 1: a soldier restored dead would stand there unkillable and unable to fight.
        /// </summary>
        public void SetCurrentHealth(int health)
        {
            CurrentHealth = Mathf.Clamp(health, 1, MaxHealth);
        }

        /// <summary>
        /// Re-reads this soldier's numbers at the type's currently researched level, carrying its
        /// damage across rather than healing it for free -- the same rule BuildingInstance.TryUpgrade
        /// follows, and for the same reason: a level researched while a raid is on must not be a full heal.
        /// </summary>
        public void RefreshStats()
        {
            var wasMissing = MaxHealth - CurrentHealth;
            ReadStats();
            CurrentHealth = Mathf.Clamp(MaxHealth - wasMissing, 1, MaxHealth);
            if (!SoldierStats.IsHero(Type)) ArmyManager.Instance?.Redress(this, SoldierStats.CurrentLevel(Type));
        }

        /// <summary>
        /// Numbers set from outside rather than read from the sheet's level: the King's, which
        /// grow with the hero's own level (Hero). Damage taken carries across, as in RefreshStats.
        /// </summary>
        public void SetHeroStats(int maxHealth, int attackDamage)
        {
            var wasMissing = MaxHealth - CurrentHealth;
            MaxHealth = Mathf.Max(1, maxHealth);
            _attackDamage = attackDamage;
            CurrentHealth = Mathf.Clamp(MaxHealth - wasMissing, 1, MaxHealth);
        }

        /// <summary>Back on his feet at full health, with nothing in hand -- the King coming back after a fall.</summary>
        public void Revive()
        {
            CurrentHealth = MaxHealth;
            _target = null;
            _breach = null;
            _hasRoute = false;
            _attackTimer = 0f;
        }

        /// <summary>Every living soldier brought up to date after a level is researched (see CityBuilder.Research.ResearchManager) or a save is loaded.</summary>
        public static void RefreshStatsForAll()
        {
            // Backwards: RefreshStats cannot remove a soldier from the list, but a future caller
            // reading a dead unit would, and the cost is the same.
            for (var i = _all.Count - 1; i >= 0; i--)
            {
                _all[i]?.RefreshStats();
            }
        }

        private void ReadStats()
        {
            MaxHealth = SoldierStats.MaxHealth(Type);

            var stats = SoldierStats.Row(Type);
            _unitAttackRange = stats.attackRangeUnits;
            _structureAttackRange = stats.attackRangeStructures;
            _engageRadius = stats.engageRadius;

            var level = SoldierStats.Stats(Type);
            _walkSpeed = level.walkSpeed;
            _attackIntervalSeconds = level.attackIntervalSeconds;
            _attackDamage = level.attackDamage;
        }

        private void Update()
        {
            if (_shieldLeft > 0f) _shieldLeft -= Time.deltaTime;
            if (_rageLeft > 0f) _rageLeft -= Time.deltaTime;
            if (Group == null) return;

            if (_retreatLeft > 0f)
            {
                _retreatLeft -= Time.deltaTime;
                if (HorizontalDistance(_retreatTo, transform.position) <= SlotArrivalDistance) _retreatLeft = 0f;
                else
                {
                    WalkTowards(_retreatTo);
                    return;
                }
            }

            _retargetTimer -= Time.deltaTime;
            if (_target == null || !_target.IsAlive || _retargetTimer <= 0f)
            {
                _retargetTimer = RetargetIntervalSeconds;
                Group.ClearFinishedAttackOrder();
                _target = AcquireTarget();
            }

            if (_breach != null && _breach.CurrentHealth <= 0) _breach = null;
            if (_breach != null && _target != null && _target.IsAlive)
            {
                if (HorizontalDistance(_breach.transform.position, transform.position) <= _structureAttackRange + BreachSlack(_breach))
                {
                    FaceTowards(_breach.transform.position);
                    TickAttackOn(_breach);
                    return;
                }
                WalkTowards(_breach.transform.position);
                return;
            }
            _breach = null;

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
                // Hitting a shielded portal does nothing (see OrcPortal.IsShielded): left to choose,
                // a soldier goes for what can actually be broken -- the camp around it.
                if (candidate is OrcPortal portal && portal.IsShielded) continue;

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
            foreach (var building in OrcCampBuilding.All)
            {
                yield return building;
            }
        }

        /// <summary>A gate's centre is a cell further off than a palisade cell's; reach it from the wall's face.</summary>
        private static float BreachSlack(OrcCampBuilding wall) => Mathf.Max(wall.Footprint.x, wall.Footprint.y) * 0.5f;

        private void TickAttackOn(OrcCampBuilding wall)
        {
            _attackTimer -= Time.deltaTime;
            if (_attackTimer > 0f) return;
            _attackTimer = AttackInterval;
            Struck?.Invoke();
            if (IsRanged) CombatProjectiles.Fire(ProjectileKind.Arrow, CombatProjectiles.MuzzleOfUnit(transform), wall, StrikeDamage);
            else wall.TakeDamage(StrikeDamage);
            if (wall.CurrentHealth <= 0)
            {
                _breach = null;
                // the way may be open now: plan afresh
                _hasRoute = false;
            }
        }

        /// <summary>The nearest piece of orc wall within reach -- what to break when the way to the target is walled off.</summary>
        private OrcCampBuilding NearestWallWithinReach()
        {
            OrcCampBuilding nearest = null;
            var best = float.MaxValue;
            foreach (var building in OrcCampBuilding.All)
            {
                if (building == null || building.CurrentHealth <= 0 || !building.IsWall) continue;
                var distance = HorizontalDistance(building.transform.position, transform.position) - BreachSlack(building);
                if (distance > BreachReachMeters || distance >= best) continue;
                best = distance;
                nearest = building;
            }
            return nearest;
        }

        private void TickAttack()
        {
            _attackTimer -= Time.deltaTime;
            if (_attackTimer > 0f) return;
            _attackTimer = AttackInterval;
            Struck?.Invoke();

            if (IsRanged)
            {
                // The arrow carries both the damage and the "who shot me" to the orc it lands in
                // (see CombatProjectiles), so the orc turns on the archer when it is hit, not when the arrow leaves.
                CombatProjectiles.Fire(ProjectileKind.Arrow, CombatProjectiles.MuzzleOfUnit(transform), _target, StrikeDamage, this);
                return;
            }

            var struck = _target;
            struck.TakeDamage(StrikeDamage);
            if (!struck.IsAlive)
            {
                _target = null;
                KilledTarget?.Invoke(struck);
                return;
            }

            // An orc has to be able to come after whoever is shooting it, or the archer tier --
            // which outranges an orc's own eyes -- would kill raids from a distance without ever
            // being touched. A portal is not told: a structure cannot come after anyone.
            if (_target is OrcUnit orc) orc.NotifyAttackedBy(this);
        }

        /// <summary>Routes to a destination, replanning when the destination has moved or the plan has gone stale. Movement itself is one step along the current route.</summary>
        private void WalkTowards(Vector3 destination)
        {
            // wedged inside something (two buildings put up round him): through it to free ground first
            if (_escape.Step(transform, _controller, _walkSpeed)) return;
            _routeRefreshTimer -= Time.deltaTime;
            var destinationMoved = HorizontalDistance(destination, _routeDestination) > 0.5f;

            if (!_hasRoute || destinationMoved || _routeRefreshTimer <= 0f || _route.IsFinished)
            {
                _route.SetDestination(transform.position, destination);
                _routeDestination = destination;
                _routeRefreshTimer = RouteRefreshSeconds;
                _hasRoute = true;

                // An orc camp's wall is in the way, and this soldier has walked up to it: break
                // through rather than standing at the wall (or, on the straight-line fallback,
                // walking through it).
                if (_route.Blocked && _breach == null)
                {
                    var wall = NearestWallWithinReach();
                    if (wall != null)
                    {
                        _breach = wall;
                        return;
                    }
                }
            }

            if (!_route.TryGetDirection(transform.position, out var direction)) return;

            var speed = _walkSpeed * Weather.SpeedMultiplier;   // the New Year's blizzard slows everyone
            if (_controller != null && _controller.enabled)
            {
                _controller.Move(direction * speed * Time.deltaTime);
            }
            else
            {
                transform.position += direction * speed * Time.deltaTime;
            }

            PinToGroundHeight();
            FaceTowards(transform.position + direction);
            _escape.Watch(transform, direction, _walkSpeed);
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
            if (amount <= 0 || CurrentHealth <= 0 || IsShielded) return;

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            if (CurrentHealth > 0)
            {
                ShowHealth();
                return;
            }

            // Killed, not disbanded: ArmyManager decides what that costs the settlement (a
            // recruited citizen is gone for good, unlike a disbanded one who walks home).
            ArmyManager.Instance?.NotifySoldierKilled(this);
        }

        private Buildings.BuildingHealthBar _healthBar;

        /// <summary>The bar over a soldier's head once he is hurt, as over an orc (the user, 2026-10-23). A hero's health is on his portrait.</summary>
        private void ShowHealth()
        {
            if (SoldierStats.IsHero(Type)) return;
            if (_healthBar == null)
            {
                var feet = transform.position.y;
                var top = feet + 0.6f;
                foreach (var r in GetComponentsInChildren<Renderer>()) top = Mathf.Max(top, r.bounds.max.y);
                _healthBar = Buildings.BuildingHealthBar.CreateFor(transform, top - feet + 0.2f, 0.7f);
            }
            _healthBar.Report(CurrentHealth / (float)Mathf.Max(1, MaxHealth));
        }

        /// <summary>The health bar over him, or null while he has not been hurt (the death's figure leaves it behind).</summary>
        public Transform HealthBar => _healthBar != null ? _healthBar.transform : null;

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
