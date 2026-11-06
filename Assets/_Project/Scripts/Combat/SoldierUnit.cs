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
    public partial class SoldierUnit : MonoBehaviour
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
        public bool IsRanged => Type == SoldierType.Archer || Type == SoldierType.Crossbowman || Type == SoldierType.Ballista || Type == SoldierType.Catapult
            || Type == SoldierType.Queen || Type == SoldierType.GnomeCrossbow;
        public SoldierType Type { get; private set; } = SoldierType.Militia;

        /// <summary>The level whose look the figure wears (ArmyManager: metal at 2, gold at 3).</summary>
        public int LookLevel { get; set; }
        public int MaxHealth { get; private set; } = 1;
        public int CurrentHealth { get; private set; } = 1;

        // Copied out of the balance sheet once, at Initialize, rather than read through
        // BalanceConfig every frame -- with twenty soldiers each running this Update, the numbers
        // have to cost what a const cost.
        private float _unitAttackRange = 1.4f;
        // the held target's half-width (BodyRadius), added to the reach
        private float _targetBody;

        /// <summary>On the player's move order (Hero): no new fight is picked up until he gets there.</summary>
        public bool Marching { get; set; }

        /// <summary>
        /// The reach against units: an archer's (the Queen's) cut in a dungeon by
        /// dungeon_ranged_range_factor -- its halls are narrow (the user, 2026-11-02: «у королевы
        /// слишком далёкое расстояние для атаки, нужно уменьшить где-то в два раза»).
        /// </summary>
        private float UnitAttackRange => (IsRanged && Dungeon.DungeonMode.IsActive && BalanceConfig.Instance != null
            ? _unitAttackRange * BalanceConfig.Instance.DungeonRangedRangeFactor
            : _unitAttackRange) + WallRangeBonus;

        /// <summary>How far off he picks a fight by himself: cut in a dungeon by dungeon_engage_factor.</summary>
        private float EngageRadius => Dungeon.DungeonMode.IsActive && BalanceConfig.Instance != null
            ? _engageRadius * BalanceConfig.Instance.DungeonEngageFactor
            : _engageRadius;

        /// <summary>A target's half-width on the ground: its collider's, or nothing.</summary>
        private static float BodyRadius(IDamageTarget target)
        {
            if (target == null || target.IsStructure || target.Transform == null) return 0f;
            var body = target.Transform.GetComponent<Collider>();
            if (body == null) return 0f;
            var e = body.bounds.extents;
            // a soldier-sized body is in the reach already
            return Mathf.Max(0f, Mathf.Max(e.x, e.z) - 0.3f);
        }
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

        private float _hasteLeft;
        private float _hasteFactor = 1f;
        private float _wardLeft;
        private float _wardKeep = 1f;

        /// <summary>Walks `factor` times as fast for this long (the dungeon's potion of speed).</summary>
        public void Haste(float factor, float seconds)
        {
            _hasteFactor = Mathf.Max(1f, factor);
            _hasteLeft = Mathf.Max(_hasteLeft, seconds);
        }

        /// <summary>Takes only `keep` of every blow for this long (the dungeon's potion of defence).</summary>
        public void Ward(float keep, float seconds)
        {
            _wardKeep = Mathf.Clamp01(keep);
            _wardLeft = Mathf.Max(_wardLeft, seconds);
        }

        public float HasteLeft => _hasteLeft;
        public float WardLeft => _wardLeft;

        /// <summary>Health back, never past the most he can have (the potion of healing).</summary>
        public void Heal(int amount)
        {
            if (amount <= 0 || CurrentHealth <= 0) return;
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            if (_healthBar != null) _healthBar.Report(CurrentHealth / (float)Mathf.Max(1, MaxHealth));
        }
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
            Buildings.WallWalk.Release(this);
        }

        /// <summary>Called by ArmyManager right after the GameObject is built, before the first Update.</summary>
        public void Initialize(SoldierType type, ArmyGroup group)
        {
            Type = type;
            Group = group;

            ReadStats();
            CurrentHealth = MaxHealth;
        }

        /// <summary>Moved into another group (ArmyManager.MoveSoldier): what he was after belongs to the old one.</summary>
        public void Regroup(ArmyGroup group)
        {
            Group = group;
            _target = null;
            _hasRoute = false;
            Marching = false;
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
            // the bar over him follows at once (the King's heal left it stale until it hid, the user 2026-11-04)
            if (_healthBar != null && _healthBar.IsShown) _healthBar.Report(CurrentHealth / (float)Mathf.Max(1, MaxHealth));
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
            _row = stats;
            _unitAttackRange = stats.attackRangeUnits;
            _structureAttackRange = stats.attackRangeStructures;
            _engageRadius = stats.engageRadius;

            var level = SoldierStats.Stats(Type);
            _walkSpeed = level.walkSpeed;
            // the tutorial's soldier keeps up with the King (the user, 2026-11-06: he crawled)
            if (Tutorial.TutorialMode.IsActive && Type == Tutorial.TutorialRules.Soldier)
                _walkSpeed = Mathf.Max(_walkSpeed, SoldierStats.Stats(SoldierType.King).walkSpeed);
            _attackIntervalSeconds = level.attackIntervalSeconds;
            _attackDamage = level.attackDamage;
        }

        /// <summary>Seconds he stands bound (the spider queen's web): no step, no blow.</summary>
        public float StunnedFor { get; private set; }

        /// <summary>Seconds he walks slowed (a spider's poison), and how much of his speed he keeps.</summary>
        public float SlowedFor { get; private set; }
        private float _slowFactor = 1f;

        /// <summary>Binds him for `seconds` (the longer of this and what is left).</summary>
        public void Stun(float seconds) => StunnedFor = Mathf.Max(StunnedFor, seconds);

        /// <summary>Slows him to `keep` of his speed for `seconds`.</summary>
        public void Slow(float seconds, float keep)
        {
            SlowedFor = Mathf.Max(SlowedFor, seconds);
            _slowFactor = Mathf.Clamp(keep, 0.1f, 1f);
        }

        private void Update()
        {
            if (_shieldLeft > 0f) _shieldLeft -= Time.deltaTime;
            if (_rageLeft > 0f) _rageLeft -= Time.deltaTime;
            if (_hasteLeft > 0f) _hasteLeft -= Time.deltaTime;
            if (_wardLeft > 0f) _wardLeft -= Time.deltaTime;
            if (SlowedFor > 0f) SlowedFor -= Time.deltaTime;
            if (StunnedFor > 0f)
            {
                StunnedFor -= Time.deltaTime;
                return;
            }
            if (Group == null) return;

            // up on a wall, on the way up or coming down (SoldierUnitWalls)
            if (TickWall()) return;
            TrackRun();

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

            // the monk strikes nobody: he heals, or keeps his place (SoldierUnitRoles); the siege
            // tower strikes nobody either: it goes where it is sent
            if (IsHealer || IsCarrier)
            {
                if (IsHealer && TickHealer()) return;
                if (IsCarrier && TickSiegeTower()) return;
                var place = Group.SlotPositionFor(this);
                if (HorizontalDistance(place, transform.position) > SlotArrivalDistance) WalkTowards(place);
                return;
            }

            _retargetTimer -= Time.deltaTime;
            if (_target == null || !_target.IsAlive || _retargetTimer <= 0f)
            {
                _retargetTimer = RetargetIntervalSeconds;
                Group.ClearFinishedAttackOrder();
                _target = AcquireTarget();
                // on the way to it, hemmed in by others: a melee fighter cuts down the one at hand
                // instead of pushing past (the King in a crowd stood not striking, playtest 2026-10-25)
                _targetBody = BodyRadius(_target);
                if (_target != null && !_target.IsStructure && HorizontalDistance(_target.Transform.position, transform.position) > UnitAttackRange + _targetBody)
                {
                    var atHand = OrcAtHand();
                    if (atHand != null)
                    {
                        _target = atHand;
                        _targetBody = BodyRadius(atHand);
                    }
                }
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
                // a big one (a boss) is reached at its edge, not its middle: the gnomes stood
                // beside the spider queen not striking (the user, 2026-11-02)
                var range = _target.IsStructure ? StructureRange(_target) : UnitAttackRange + _targetBody;

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

        /// <summary>A live orc within a melee blow; none for archers.</summary>
        private OrcUnit OrcAtHand()
        {
            if (IsRanged) return null;
            OrcUnit nearest = null;
            var best = UnitAttackRange;
            foreach (var orc in OrcUnit.All)
            {
                if (orc == null || orc.CurrentHealth <= 0) continue;
                var d = HorizontalDistance(orc.transform.position, transform.position);
                if (d > best) continue;
                best = d;
                nearest = orc;
            }
            return nearest;
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
            if (Group.AttackTarget != null && Group.AttackTarget.IsAlive && CanStrike(Group.AttackTarget)) return Group.AttackTarget;
            // sent somewhere by the player: he goes there first, fights after (the user, 2026-11-02) --
            // unless he is caught on the way: one pinned by monsters striking him stood and took it
            // (the user, 2026-11-04), so he answers whoever hits him from close by
            if (Marching) return Assailant(UnitAttackRange + 0.6f);
            if (Group.IsGarrison) return GarrisonTarget();
            var dungeon = Dungeon.DungeonMode.IsActive;

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
                // the ram only at gates, the catapult nothing inside its dead zone (SoldierUnitRoles)
                if (!CanStrike(candidate)) continue;

                var distance = HorizontalDistance(candidate.Transform.position, slot);
                // one striking at the group from further off (the serpent spitting from its canal,
                // the user 2026-11-04: «не реагирует на змея на таком расстоянии») is answered too
                // «Наступать» answers a shooter however far; the rest a short way (the user, 2026-11-05)
                var answer = Group.Behaviour == SquadBehaviour.Advance ? float.MaxValue : EngageRadius * 2.5f;
                if (distance > EngageRadius && !(candidate is OrcUnit striker && StrikesGroup(striker) && distance <= answer)) continue;
                // below, no shooting or running at what stands behind the rock (the Queen did)
                if (dungeon && UnityEngine.AI.NavMesh.Raycast(transform.position, candidate.Transform.position, out _, UnityEngine.AI.NavMesh.AllAreas)) continue;

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

        /// <summary>
        /// The Garrison defends the main island soldier by soldier (the user, 2026-11-05): the enemy
        /// fighter nearest HIM among those within `garrison_defend_cells` of the Town Hall -- fighters
        /// only, never a camp or a portal, which would take the Garrison off on a war of its own.
        /// </summary>
        private IDamageTarget GarrisonTarget()
        {
            var hall = Buildings.BuildingInstance.TownHall;
            var cells = BalanceConfig.Instance != null ? BalanceConfig.Instance.GarrisonDefendCells : 30f;
            var reach = cells * (GridManager.Instance != null ? GridManager.Instance.CellSize : 1f);
            var post = Group.SlotPositionFor(this);
            IDamageTarget best = null;
            var bestDistance = float.MaxValue;
            foreach (var candidate in EnumerateEnemies())
            {
                if (candidate == null || !candidate.IsAlive || candidate.IsStructure || !CanStrike(candidate)) continue;
                var at = candidate.Transform.position;
                // round the Town Hall, or come up to his own post (a post far out, a hall not built)
                var nearHall = hall != null && HorizontalDistance(at, hall.transform.position) <= reach;
                var nearPost = HorizontalDistance(at, post) <= EngageRadius
                    || (candidate is OrcUnit striker && StrikesGroup(striker) && HorizontalDistance(at, post) <= EngageRadius * 2.5f);
                if (!nearHall && !nearPost) continue;
                var d = HorizontalDistance(at, transform.position);
                if (d >= bestDistance) continue;
                bestDistance = d;
                best = candidate;
            }
            return best;
        }

        /// <summary>This orc is fighting someone of this soldier's group.</summary>
        private bool StrikesGroup(OrcUnit orc)
        {
            var foe = orc.Fighting;
            return foe != null && (foe == this || (Group != null && foe.Group == Group));
        }

        /// <summary>The nearest live orc within `reach` that is fighting this soldier, or null.</summary>
        private OrcUnit Assailant(float reach)
        {
            OrcUnit nearest = null;
            var best = reach;
            foreach (var orc in OrcUnit.All)
            {
                if (orc == null || orc.CurrentHealth <= 0 || orc.Fighting != this) continue;
                var d = HorizontalDistance(orc.transform.position, transform.position);
                if (d > best) continue;
                best = d;
                nearest = orc;
            }
            return nearest;
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
        /// <summary>How far past its middle a structure's walls stand: a bot building at war is struck at its walls (Bot/BotWarTarget).</summary>
        private static float StructureReach(IDamageTarget target) => target is Bot.BotWarTarget bot ? bot.Reach : 0f;

        /// <summary>Between a melee fighter and the wall of the building he strikes.</summary>
        private const float MeleeWallGap = 0.8f;

        /// <summary>
        /// How near he must be to strike this building, from its middle. A melee fighter stands at its wall
        /// (half its footprint + MeleeWallGap) when that is nearer than his sheet reach -- the King struck the
        /// portal from well off it (the user, 2026-11-06); a shooter keeps his reach.
        /// </summary>
        private float StructureRange(IDamageTarget target)
        {
            var sheet = _structureAttackRange + StructureReach(target);
            if (IsRanged) return sheet;
            var cell = Grid.GridManager.Instance != null ? Grid.GridManager.Instance.CellSize : 1f;
            float half;
            if (target is OrcPortal portal) half = (portal.IsMain ? OrcRaidManager.MainPortalFootprint.x : OrcRaidManager.PortalFootprint.x) * 0.5f * cell;
            else if (target is OrcCampBuilding building) half = Mathf.Max(building.Footprint.x, building.Footprint.y) * 0.5f * cell;
            else return sheet;
            return Mathf.Min(sheet, half + MeleeWallGap);
        }

        private static float BreachSlack(OrcCampBuilding wall) => Mathf.Max(wall.Footprint.x, wall.Footprint.y) * 0.5f;

        private void TickAttackOn(OrcCampBuilding wall)
        {
            _attackTimer -= Time.deltaTime;
            if (_attackTimer > 0f) return;
            _attackTimer = AttackInterval;
            // every creature in a fight shows its bar, hurt or not (the user, 2026-11-02)
            ShowHealth();
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
            // every creature in a fight shows its bar, hurt or not (the user, 2026-11-02)
            ShowHealth();
            Struck?.Invoke();

            if (IsRanged)
            {
                // The arrow carries both the damage and the "who shot me" to the orc it lands in
                // (see CombatProjectiles), so the orc turns on the archer when it is hit, not when the arrow leaves.
                CombatProjectiles.Fire(Type == SoldierType.Catapult ? ProjectileKind.Cannonball : ProjectileKind.Arrow, CombatProjectiles.MuzzleOfUnit(transform),
                    _target, StrikeDamageOn(_target), this, _row != null ? _row.splash : 0f);
                return;
            }

            var struck = _target;
            var blow = StrikeDamageOn(struck);
            if (struck is OrcUnit felled) felled.TakeDamage(blow, this);
            else struck.TakeDamage(blow);
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
            if (SlowedFor > 0f) speed *= _slowFactor;
            if (_hasteLeft > 0f) speed *= _hasteFactor;
            speed *= Buildings.Moat.SpeedAt(transform.position);   // wading a moat
            if (_controller != null && _controller.enabled)
            {
                _controller.Move(direction * speed * Time.deltaTime);
            }
            else
            {
                transform.position += direction * speed * Time.deltaTime;
            }

            PinToGroundHeight();
            Dungeon.DungeonWake.Step(transform.position, ref _wakeClock);   // rings round a wading walker
            FaceTowards(transform.position + direction);
            _escape.Watch(transform, direction, _walkSpeed);
        }

        private float _wakeClock;

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
            // up on a bridge's deck, or in the moat's water to the knee (the user, 2026-11-03)
            position.y = grid.GroundHeight + Buildings.Moat.StandHeightAt(position);
            transform.position = position;
        }

        /// <summary>The cheat window's switch: the player's soldiers and heroes take no damage.</summary>
        public static bool InvulnerableCheat;

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || CurrentHealth <= 0 || IsShielded || InvulnerableCheat) return;
            if (_wardLeft > 0f) amount = Mathf.Max(1, Mathf.RoundToInt(amount * _wardKeep));

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            // the tutorial's King never dies: low, he heals himself back to full (the user, 2026-11-05)
            if (Type == SoldierType.King && Tutorial.TutorialMode.IsActive && CurrentHealth <= MaxHealth * Tutorial.TutorialRules.KingHealBelow)
                CurrentHealth = MaxHealth;
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

        /// <summary>
        /// The bar over a soldier's head once he is hurt or fights, as over an orc (the user,
        /// 2026-10-23); a hero's too since 2026-11-02 («все существа»), his portrait aside.
        /// </summary>
        private void ShowHealth()
        {
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
