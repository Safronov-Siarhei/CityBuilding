using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Grid;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Combat
{
    /// <summary>
    /// What an orc is, which decides the balance-sheet row it reads (see OrcUnit.SheetIdOf) and the
    /// model it wears. Saved by its number, so new kinds go on the end.
    /// </summary>
    public enum OrcKind
    {
        /// <summary>The ordinary raider: the grunt, drawn as the brute from OrcRaidManager.BruteFromLevel.</summary>
        Raider = 0,

        /// <summary>Throws from range. The raid's answer to an archer line that would otherwise never be touched.</summary>
        Thrower = 1,

        /// <summary>Leads a wave: one per raid from raid_chief_from_level, much tougher than the rest.</summary>
        Chief = 2,

        /// <summary>The heavy of the walled bases: slow, thick, its own row and its own levels (the user's sketches, 2026-10-16).</summary>
        Brute = 3,

        /// <summary>The main base's chief: its miniboss, his own row, comes back from his house while it stands.</summary>
        Boss = 4,

        // The Invasion jungle's monsters (InvasionJungle): not orcs, but they fight the same way --
        // a guard holding its camp -- so they are kinds of this unit. Stats: economy mon_<id>_*.

        /// <summary>A camp of level 1: a pack of wolves.</summary>
        Wolf = 5,

        /// <summary>A camp of level 2: bears.</summary>
        Bear = 6,

        /// <summary>A camp of level 3: rock trolls.</summary>
        Troll = 7,

        /// <summary>The jungle's boss in its southern lair.</summary>
        Giant = 8,

        // The New Year map's own (InvasionNewYear, the user 2026-10-25).

        /// <summary>Runs off with the gifts left on the island, fights no one.</summary>
        Thief = 9,

        /// <summary>The midnight boss: freezes the towers near him.</summary>
        IceShaman = 10,

        // The dungeon's monsters (Dungeon/DungeonMonsters, Tools/blender/monsters.py; the user,
        // 2026-10-31). They hold their lairs like the jungle's; stats economy mon_<id>_*.

        /// <summary>A pack animal: small, quick, weak.</summary>
        Rat = 11,
        /// <summary>Flies, bites and is off again.</summary>
        Bat = 12,
        /// <summary>The hunter: its bite poisons -- the hero slows.</summary>
        Spider = 13,
        /// <summary>The queen's brood.</summary>
        Spiderling = 14,
        /// <summary>A dead miner with a pick: falls once and gets up again.</summary>
        Skeleton = 15,
        /// <summary>Keeps its distance and throws dynamite.</summary>
        Kobold = 16,
        /// <summary>The small golem: slow, a lot of health.</summary>
        StoneGolem = 17,
        /// <summary>Boss: webs a hero, calls her brood, seals herself in a cocoon at half health.</summary>
        SpiderQueen = 18,
        /// <summary>Boss: slams the ground round him, throws boulders, grows ore armour at half health.</summary>
        OreGolem = 19,
    }

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
        /// <summary>The row an orc of this kind reads in the balance sheet's units tab.</summary>
        public static string SheetIdOf(OrcKind kind)
        {
            switch (kind)
            {
                case OrcKind.Wolf: return "wolf";
                case OrcKind.Bear: return "bear";
                case OrcKind.Troll: return "troll";
                case OrcKind.Giant: return "giant";
                case OrcKind.Thrower: return "orc_thrower";
                case OrcKind.Chief: return "orc_chief";
                case OrcKind.Brute: return "orc_brute";
                case OrcKind.Boss: return "orc_chief_boss";
                case OrcKind.Thief: return "orc_thief";
                case OrcKind.IceShaman: return "orc_ice_shaman";
                case OrcKind.Rat: return "rat";
                case OrcKind.Bat: return "bat";
                case OrcKind.Spider: return "spider";
                case OrcKind.Spiderling: return "spiderling";
                case OrcKind.Skeleton: return "skeleton";
                case OrcKind.Kobold: return "kobold";
                case OrcKind.StoneGolem: return "stone_golem";
                case OrcKind.SpiderQueen: return "spider_queen";
                case OrcKind.OreGolem: return "ore_golem";
                default: return "orc";
            }
        }

        /// <summary>One of the Invasion jungle's monsters rather than an orc.</summary>
        public static bool IsMonster(OrcKind kind) => (kind >= OrcKind.Wolf && kind <= OrcKind.Giant) || IsDungeonMonster(kind);

        /// <summary>One of the dungeon's monsters.</summary>
        public static bool IsDungeonMonster(OrcKind kind) => kind >= OrcKind.Rat && kind <= OrcKind.OreGolem;

        /// <summary>A boss: the jungle's giant, the dungeon's queen and ore golem.</summary>
        public static bool IsBossKind(OrcKind kind) => kind == OrcKind.Giant || kind == OrcKind.SpiderQueen || kind == OrcKind.OreGolem;

        private const float ArrivalThreshold = 0.1f;
        private const float RetargetIntervalSeconds = 5f;
        // Much shorter than RetargetIntervalSeconds: buildings don't move, soldiers do, and a
        // five-second reaction to being attacked reads as the orc simply not noticing.
        private const float SoldierScanIntervalSeconds = 0.4f;
        // How far from a building's WALL a stuck orc looks for the thing blocking its way: wide
        // enough to catch the wall it just walked up to, narrow enough that it can't reach past
        // that wall for something behind it.
        private const float BlockerReachRadius = 1.5f;
        // How far from a target off the NavMesh (a building's middle, inside its carve) its route
        // looks for the nearest point on it: half the castle's breadth and some.
        private const float RouteTargetSnapMeters = 5f;

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
        // A freed prisoner on his way home (see FreedPrisoner): fair game, but only when no
        // soldier is there to fight (the user's rule, 2026-10-19).
        private FreedPrisoner _prisonerTarget;
        private float _prisonerScanTimer;
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

        /// <summary>Its full health at its level -- also what striking it down is worth to the King (Hero).</summary>
        public int MaxHealth => _maxHealth;
        public int AttackDamage => _attackDamage;

        // IDamageTarget: what the player's army sees when it looks for something to hit. An orc is
        // a unit, not a structure, so a group set to prioritise structures walks past it.
        Transform IDamageTarget.Transform => this != null ? transform : null;
        bool IDamageTarget.IsAlive => this != null && CurrentHealth > 0;
        bool IDamageTarget.IsStructure => false;
        void IDamageTarget.TakeDamage(int amount) => TakeDamage(amount);

        /// <summary>1 unless raised at spawn time. Scales both health and attack damage linearly.</summary>
        public int Level { get; private set; } = 1;

        public OrcKind Kind { get; private set; } = OrcKind.Raider;

        /// <summary>The building it is marching on or breaking, if any.</summary>
        public BuildingInstance Target => _target;

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

            // In Awake, not Initialize: an orc left uninitialized is a valid level-1 raider and
            // still needs its stats.
            LoadStats(OrcKind.Raider);
        }

        private void LoadStats(OrcKind kind)
        {
            Kind = kind;
            var stats = IsMonster(kind) ? BalanceConfig.Instance.MonsterUnit(SheetIdOf(kind)) : BalanceConfig.Instance.Unit(SheetIdOf(kind));
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

        /// <summary>Optional -- an orc left uninitialized is a perfectly valid level-1 raider. Called right after the component is added, before its first Update.</summary>
        public void Initialize(int level, OrcKind kind = OrcKind.Raider)
        {
            if (kind != Kind) LoadStats(kind);
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
            _lastPosition = transform.position;
            // a guard put on its post as it was made has no building to march on
            if (!_guarding) AcquireTarget();
        }

        // Guard duty (see OrcCamp): a garrison orc holds its camp instead of marching on the town.
        private bool _guarding;
        private Vector3 _guardPost;
        private OrcCamp _camp;
        // Guards walk over the NavMesh, not in straight lines: their camp has walls now, and a
        // straight line would carry them through the palisade.
        private NavRoute _guardRoute;
        private Vector3 _guardRouteDestination;
        private float _guardRouteTimer;
        private const float GuardRouteRefreshSeconds = 0.6f;
        // How far outside its camp a guard keeps after a soldier who is shooting it.
        private const float GuardLeashMeters = 3f;
        // How far round its post a guard without a camp holds; 0 = the sheet's garrison radius.
        private float _guardRadius;

        /// <summary>A camp's garrison, not a raider: it is not saved (its spawner musters it again on load).</summary>
        public bool IsGuard => _guarding;

        /// <summary>The post this guard holds.</summary>
        public Vector3 GuardPost => _guardPost;

        /// <summary>
        /// Puts the orc on guard at a camp: it fights soldiers who come into the camp's square (or
        /// who shoot it from a little further), never chases out of it, and walks back to its post
        /// when the fight is over. It never goes for the player's buildings.
        /// </summary>
        public void SetGuardPost(OrcCamp camp, Vector3 post, float radius = 0f)
        {
            _guarding = true;
            _guardRadius = radius;
            _camp = camp;
            _guardPost = post;
            _target = null;
            _guardRoute = NewRoute();
        }

        /// <summary>
        /// Moved by something else (the New Year's thief, InvasionNewYear): no target of its own, no
        /// fight -- it goes where SteerTo sends it, nothing more.
        /// </summary>
        public bool Steered { get; set; }

        private Vector3 _steerGoal = new Vector3(float.MaxValue, 0f, 0f);
        private float _steerReplan;

        /// <summary>One step toward `goal` over the NavMesh (off the gates), replanning when it moves or every second. False once there.</summary>
        public bool SteerTo(Vector3 goal)
        {
            var to = goal - transform.position;
            to.y = 0f;
            if (to.magnitude <= 0.4f) return false;
            _steerReplan -= Time.deltaTime;
            if (_steerReplan <= 0f || (goal - _steerGoal).sqrMagnitude > 0.25f)
            {
                _steerGoal = goal;
                _steerReplan = 1f;
                _route = BuildRoute(transform.position, goal, out _);
                _routeIndex = 0;
            }
            if (_route == null || _route.Length == 0) return false;
            if (_routeIndex >= _route.Length - 1 && ReachedLastWaypoint()) Step(to.normalized);
            else AdvanceAlongRoute();
            FaceAlong(to);
            return true;
        }

        private void FaceAlong(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(direction);
        }

        private void Update()
        {
            if (Steered) return;
            // sealed in a cocoon, down on the ground: nothing to do until it is over
            if (BusyFor > 0f)
            {
                BusyFor -= Time.deltaTime;
                return;
            }
            if (_guarding)
            {
                GuardUpdate();
                return;
            }

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

            // In a run the orcs go for the island's people as they do for its buildings (the user, 2026-10-22).
            if (HuntCitizen()) return;

            if (HuntPrisoner()) return;

            if (_lane != null)
            {
                if (WalkTheLane()) return;
            }
            else if (_raiding)
            {
                if (Raid()) return;
            }
            else
            {
                _retargetTimer -= Time.deltaTime;
                if (_target == null || _retargetTimer <= 0f)
                {
                    AcquireTarget();
                    _retargetTimer = RetargetIntervalSeconds;
                }
            }
            if (_target == null) return;

            if (CanStrike(_target))
            {
                TickAttack();
                return;
            }

            // Walked the whole route and the target is still out of reach: something the orc can't
            // path around is between them -- a wall, a gate. Take it out on that instead of standing
            // there (the route may even read complete: it ends at the nearest point the NavMesh has
            // to the target, and that can be the far side of a fence).
            if (_routeIndex >= _route.Length - 1 && ReachedLastWaypoint())
            {
                AttackWhateverIsBlockingTheWay();
                return;
            }

            AdvanceAlongRoute();
        }

        /// <summary>
        /// Health times `factor`, full again: an Invasion wave's orcs are this much tougher than a
        /// sandbox raid's (inv_orc_health_mult), or a tower's arrow drops each with one shot; its
        /// blows times `damage` (inv_orc_damage_mult).
        /// </summary>
        public void Toughen(float factor, float damage = 1f)
        {
            _maxHealth = Mathf.Max(1, Mathf.RoundToInt(_maxHealth * factor));
            _attackDamage = Mathf.Max(1, Mathf.RoundToInt(_attackDamage * damage));
            CurrentHealth = _maxHealth;
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || CurrentHealth <= 0) return;

            // ore armour (the ore golem past half health): a share of every blow turned aside
            if (Armour > 0f) amount = Mathf.Max(1, Mathf.RoundToInt(amount * (1f - Mathf.Clamp01(Armour))));
            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            if (CurrentHealth > 0)
            {
                ShowHealth();
                Hurt?.Invoke(this);
                return;
            }
            // a skeleton falls apart once and pulls itself together again (DungeonMonsterPowers)
            if (GetsUpOnce)
            {
                GetsUpOnce = false;
                CurrentHealth = Mathf.Max(1, Mathf.RoundToInt(_maxHealth * Mathf.Clamp01(GetUpHealthShare)));
                BusyFor = GetUpSeconds;
                _soldierTarget = null;
                ShowHealth();
                Collapsed?.Invoke();
                return;
            }
            // the figure falls and sinks on its own, after the unit is gone
            OrcCorpse.Leave(transform, _healthBar != null ? _healthBar.transform : null);
            Died?.Invoke(this);
            Destroy(gameObject);
        }

        // Made on the first hit only: most orcs on a map are never touched by the player.
        private BuildingHealthBar _healthBar;

        /// <summary>A bar over its head once it has been hurt, how much is left (the user, 2026-10-22).</summary>
        private void ShowHealth()
        {
            if (_healthBar == null)
            {
                // over the tallest part of its figure: a wolf's back, a giant's head
                var feet = transform.position.y;
                var top = feet + 0.6f;
                foreach (var r in GetComponentsInChildren<Renderer>()) top = Mathf.Max(top, r.bounds.max.y);
                _healthBar = BuildingHealthBar.CreateFor(transform, top - feet + 0.2f, IsBossKind(Kind) ? 1.2f : 0.7f);
            }
            _healthBar.Report(CurrentHealth / (float)Mathf.Max(1, _maxHealth));
        }

        /// <summary>Raised on each blow it lands -- a jointed figure (FigureAnimator) swings with it.</summary>
        public event System.Action Struck;

        /// <summary>Raised on each blow or shot at a soldier or hero, with him -- the spider's poison rides on it.</summary>
        public event System.Action<SoldierUnit> HitSoldier;

        /// <summary>Raised when it is hurt and still standing -- a boss watches for its half-health turn.</summary>
        public event System.Action<OrcUnit> Hurt;

        /// <summary>Raised as a skeleton falls apart to get up again (FigureAnimator shows the fall and the rise).</summary>
        public event System.Action Collapsed;

        /// <summary>Seconds it does nothing at all -- in a cocoon, lying in pieces. Set by the dungeon's powers.</summary>
        public float BusyFor { get; set; }

        /// <summary>The share of every blow it shrugs off (0 none .. 1 all but one point).</summary>
        public float Armour { get; set; }

        /// <summary>Falls apart once instead of dying, and gets up GetUpSeconds later with GetUpHealthShare of its health.</summary>
        public bool GetsUpOnce { get; set; }
        public float GetUpSeconds { get; set; }
        public float GetUpHealthShare { get; set; }

        /// <summary>Gives back health, never past its full -- the queen's brood feeding her through the cocoon.</summary>
        public void Heal(int amount)
        {
            if (amount <= 0 || CurrentHealth <= 0) return;
            CurrentHealth = Mathf.Min(_maxHealth, CurrentHealth + amount);
            if (_healthBar != null) _healthBar.Report(CurrentHealth / (float)Mathf.Max(1, _maxHealth));
        }

        /// <summary>The soldier or hero it is fighting now, if any.</summary>
        public SoldierUnit Fighting => _soldierTarget != null && _soldierTarget.CurrentHealth > 0 ? _soldierTarget : null;

        /// <summary>Raised as an orc falls, whoever struck it -- the King's experience and a run's gold are counted from it.</summary>
        public static event System.Action<OrcUnit> Died;

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

        private void GuardUpdate()
        {
            _soldierScanTimer -= Time.deltaTime;
            if (_soldierTarget == null || _soldierTarget.CurrentHealth <= 0 || _soldierScanTimer <= 0f)
            {
                _soldierScanTimer = SoldierScanIntervalSeconds;
                if (!_retaliating || !StillWorthChasing(_soldierTarget))
                {
                    _retaliating = false;
                    _soldierTarget = FindNearestSoldierInCamp();
                }
            }

            if (_soldierTarget != null)
            {
                var toSoldier = _soldierTarget.transform.position - transform.position;
                toSoldier.y = 0f;
                if (toSoldier.magnitude <= _soldierAttackRange)
                {
                    _attackTimer -= Time.deltaTime;
                    if (_attackTimer > 0f) return;
                    _attackTimer = _attackIntervalSeconds;
                    Strike(_soldierTarget);
                    if (_soldierTarget.CurrentHealth <= 0) _soldierTarget = null;
                    return;
                }
                GuardWalk(_soldierTarget.transform.position);
                return;
            }

            if (HuntPrisoner()) return;

            var toPost = _guardPost - transform.position;
            toPost.y = 0f;
            if (toPost.magnitude < 0.6f) return;
            GuardWalk(_guardPost);
        }

        /// <summary>
        /// With no soldier to fight, goes for a freed prisoner: a guard for one inside its camp's
        /// zone, a raider for one within its aggro radius. True while it is busy with one.
        /// </summary>
        private bool HuntPrisoner()
        {
            if (FreedPrisoner.All.Count == 0)
            {
                _prisonerTarget = null;
                return false;
            }

            _prisonerScanTimer -= Time.deltaTime;
            if (_prisonerTarget == null || _prisonerTarget.CurrentHealth <= 0 || _prisonerScanTimer <= 0f)
            {
                _prisonerScanTimer = SoldierScanIntervalSeconds;
                _prisonerTarget = FindNearestPrisoner();
            }
            if (_prisonerTarget == null) return false;

            var toPrisoner = _prisonerTarget.transform.position - transform.position;
            toPrisoner.y = 0f;
            var distance = toPrisoner.magnitude;
            if (distance <= _soldierAttackRange)
            {
                _attackTimer -= Time.deltaTime;
                if (_attackTimer > 0f) return true;
                _attackTimer = _attackIntervalSeconds;
                Strike(_prisonerTarget);
                if (_prisonerTarget == null || _prisonerTarget.CurrentHealth <= 0) _prisonerTarget = null;
                return true;
            }

            if (_guarding)
            {
                GuardWalk(_prisonerTarget.transform.position);
                return true;
            }

            Chase(_prisonerTarget.transform.position, toPrisoner / distance);
            _retargetTimer = 0f;
            return true;
        }

        private FreedPrisoner FindNearestPrisoner()
        {
            FreedPrisoner nearest = null;
            var best = _guarding ? float.MaxValue : _soldierAggroRadius * _soldierAggroRadius;
            foreach (var prisoner in FreedPrisoner.All)
            {
                if (prisoner == null || prisoner.CurrentHealth <= 0) continue;
                if (_guarding && !InGuardZone(prisoner.transform.position, OrcCamp.ZoneMarginMeters)) continue;
                var distSq = (prisoner.transform.position - transform.position).sqrMagnitude;
                if (distSq >= best) continue;
                best = distSq;
                nearest = prisoner;
            }
            return nearest;
        }

        private void GuardWalk(Vector3 destination, bool straightLineFallback = false)
        {
            _guardRoute ??= NewRoute();
            _guardRouteTimer -= Time.deltaTime;
            var moved = destination - _guardRouteDestination;
            moved.y = 0f;
            if (_guardRouteTimer <= 0f || moved.sqrMagnitude > 0.25f)
            {
                _guardRoute.SetDestination(transform.position, destination, straightLineFallback);
                _guardRouteDestination = destination;
                _guardRouteTimer = GuardRouteRefreshSeconds;
            }
            if (!_guardRoute.TryGetDirection(transform.position, out var direction)) return;

            var step = direction * _walkSpeed * Time.deltaTime;
            if (_controller != null && _controller.enabled)
            {
                _controller.Move(step);
                PinToGroundHeight();
            }
            else
            {
                transform.position += step;
            }
            transform.rotation = Quaternion.LookRotation(direction);
        }

        private const float ChaseLookAheadMeters = 1.5f;

        /// <summary>
        /// Closes on someone it is fighting: straight at them while the NavMesh ahead is clear (the
        /// cheap case, nearly always), round by a NavMesh route when water, a wall or a building is in
        /// the way -- a straight line used to walk an orc lured by the King across a lake (playtest 4,
        /// 2026-10-22).
        /// </summary>
        private void Chase(Vector3 destination, Vector3 direction)
        {
            var from = transform.position;
            // Never through a gate, here as on the way to the castle (NavAreas): an orc after
            // someone behind a run's fence walked in at its gate (the user, 2026-10-23). Walled
            // off from them, it breaks whatever stands in its way.
            if (NavMesh.Raycast(from, from + direction * ChaseLookAheadMeters, out _, CityBuilder.Maps.NavAreas.WithoutGates))
            {
                GuardWalk(destination, straightLineFallback: false);
                if (_guardRoute.Blocked && _guardRoute.IsFinished) BreakThrough();
                return;
            }
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

        /// <summary>An orc's route: never across a gate's cell -- a gate is a wall to it (NavAreas).</summary>
        private static NavRoute NewRoute() => new NavRoute { AreaMask = CityBuilder.Maps.NavAreas.WithoutGates };

        private BuildingInstance _blocker;

        /// <summary>At the end of a chase the NavMesh cannot finish: strikes the fence, gate or building there (BlockerReachRadius) until it is down.</summary>
        private void BreakThrough()
        {
            if (_blocker == null || _blocker.CurrentHealth <= 0) _blocker = NearestBuildingWithin(BlockerReachRadius);
            if (_blocker == null || !CanStrike(_blocker)) return;

            _attackTimer -= Time.deltaTime;
            if (_attackTimer > 0f) return;
            _attackTimer = _attackIntervalSeconds;
            Strike(_blocker);
            if (_blocker.CurrentHealth <= 0) _blocker = null;
        }

        /// <summary>The building (not a road) whose WALL is nearest this orc, within `radius` of it, or null -- the fence before a tower, not the tower behind it.</summary>
        private BuildingInstance NearestBuildingWithin(float radius)
        {
            BuildingInstance nearest = null;
            var nearestDistance = radius;
            foreach (var instance in FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
            {
                if (instance.Data == null || instance.Data.isRoad) continue;
                var distance = DistanceToWall(instance);
                if (distance > nearestDistance) continue;
                nearestDistance = distance;
                nearest = instance;
            }
            return nearest;
        }

        /// <summary>
        /// A building (not a road) nearer this orc than `target`'s wall and on its side of it: one it
        /// stands in, or one whose wall lies the way the target's does. The nearest such, or null.
        /// </summary>
        private BuildingInstance BuildingInTheWay(BuildingInstance target)
        {
            if (target == null) return null;
            var at = transform.position;
            var targetWall = DistanceToWall(target);
            var toTarget = ClosestPointOf(target) - at;
            toTarget.y = 0f;
            BuildingInstance nearest = null;
            var nearestDistance = targetWall - 0.05f;
            foreach (var instance in FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
            {
                if (instance == target || instance.Data == null || instance.Data.isRoad || instance.CurrentHealth <= 0) continue;
                var distance = DistanceToWall(instance);
                if (distance >= nearestDistance) continue;
                if (distance > 0.15f && toTarget.sqrMagnitude > 0.0001f)
                {
                    var toWall = ClosestPointOf(instance) - at;
                    toWall.y = 0f;
                    if (Vector3.Dot(toWall.normalized, toTarget.normalized) < 0.5f) continue;
                }
                nearestDistance = distance;
                nearest = instance;
            }
            return nearest;
        }

        /// <summary>
        /// Close enough to hit this building: a thrower from its range of the building's middle,
        /// anyone fighting hand to hand only standing at its wall (orc_melee_reach_meters) -- from
        /// its middle, three metres reached over a fence into the tower behind it (the user,
        /// 2026-10-23: only a ranged orc hits a building over another one).
        /// </summary>
        private bool CanStrike(BuildingInstance building)
        {
            if (building == null) return false;
            if (IsRanged)
            {
                var to = building.transform.position - transform.position;
                to.y = 0f;
                return to.magnitude <= _buildingAttackRange;
            }
            return DistanceToWall(building) <= BalanceConfig.Instance.OrcMeleeReachMeters;
        }

        /// <summary>Horizontal metres from this orc to the nearest point of the building's body (its collider), or to its middle when it has none.</summary>
        private float DistanceToWall(BuildingInstance building)
        {
            var at = transform.position;
            var body = building.GetComponent<Collider>();
            var point = body != null ? body.ClosestPoint(new Vector3(at.x, body.bounds.center.y, at.z)) : building.transform.position;
            var to = point - at;
            to.y = 0f;
            return to.magnitude;
        }

        private SoldierUnit FindNearestSoldierInCamp()
        {
            SoldierUnit nearest = null;
            var best = float.MaxValue;
            foreach (var soldier in SoldierUnit.All)
            {
                if (soldier == null || soldier.CurrentHealth <= 0) continue;
                if (!InGuardZone(soldier.transform.position, OrcCamp.ZoneMarginMeters)) continue;
                var distSq = (soldier.transform.position - transform.position).sqrMagnitude;
                if (distSq >= best) continue;
                best = distSq;
                nearest = soldier;
            }
            return nearest;
        }

        /// <summary>Inside the camp's square (give or take a margin) -- or, for a guard without a camp, near its post.</summary>
        private bool InGuardZone(Vector3 position, float margin)
        {
            if (_camp != null) return _camp.IsInZone(position, margin);
            var reach = (_guardRadius > 0f ? _guardRadius : BalanceConfig.Instance.OrcGarrisonGuardRadiusMeters) + margin;
            var fromPost = position - _guardPost;
            fromPost.y = 0f;
            return fromPost.sqrMagnitude <= reach * reach;
        }

        /// <summary>Whether the soldier it is chasing is alive and still inside the retaliation leash -- for a guard, measured from its post.</summary>
        private bool StillWorthChasing(SoldierUnit soldier)
        {
            if (soldier == null || soldier.CurrentHealth <= 0) return false;
            if (_guarding) return InGuardZone(soldier.transform.position, OrcCamp.ZoneMarginMeters + GuardLeashMeters);

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

                Strike(_soldierTarget);
                if (_soldierTarget.CurrentHealth <= 0) _soldierTarget = null;
                return;
            }

            Chase(_soldierTarget.transform.position, toSoldier / distance);

            // The building route was planned from where this orc used to stand; force a replan
            // once the fight is over instead of resuming a route that no longer starts here.
            _retargetTimer = 0f;
        }

        private CityBuilder.Citizens.CitizenAgent _citizenTarget;
        private float _citizenScanTimer;

        /// <summary>
        /// An Invasion run's orc turns on the nearest citizen within its aggro radius, walks up and
        /// strikes; inv_citizen_health of damage kills him. False when there is nobody to go for
        /// (or this is not a run), and the orc carries on to the buildings.
        /// </summary>
        private bool HuntCitizen()
        {
            if (!Invasion.InvasionMode.IsActive) return false;
            var visuals = CityBuilder.Citizens.CitizenVisualsManager.Instance;
            if (visuals == null) return false;

            _citizenScanTimer -= Time.deltaTime;
            if (_citizenTarget == null || !_citizenTarget.gameObject.activeInHierarchy || _citizenScanTimer <= 0f)
            {
                _citizenScanTimer = SoldierScanIntervalSeconds;
                _citizenTarget = null;
                var best = _soldierAggroRadius * _soldierAggroRadius;
                foreach (var agent in visuals.AllAgents)
                {
                    if (agent == null || !agent.gameObject.activeInHierarchy) continue;
                    var d = (agent.transform.position - transform.position).sqrMagnitude;
                    if (d > best) continue;
                    best = d;
                    _citizenTarget = agent;
                }
            }
            if (_citizenTarget == null) return false;

            var to = _citizenTarget.transform.position - transform.position;
            to.y = 0f;
            var distance = to.magnitude;
            if (distance <= _soldierAttackRange)
            {
                _attackTimer -= Time.deltaTime;
                if (_attackTimer > 0f) return true;
                _attackTimer = _attackIntervalSeconds;
                StrikeAt(_citizenTarget.transform.position);
                if (HeroBlessing.Active) return true;   // the King's Blessing covers the people too
                _citizenTarget.Wounds += _attackDamage;
                if (_citizenTarget.Wounds >= BalanceConfig.Instance.InvasionCitizenHealth)
                {
                    visuals.KillAgent(_citizenTarget);
                    _citizenTarget = null;
                }
                return true;
            }

            Chase(_citizenTarget.transform.position, to / Mathf.Max(0.001f, distance));
            _retargetTimer = 0f;
            return true;
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

        /// <summary>A thrower's hit is a javelin seen flying (see CombatProjectiles), landing its damage on impact; every other orc's is a blow, landed at once.</summary>
        public bool IsRanged => Kind == OrcKind.Thrower || Kind == OrcKind.Kobold;

        /// <summary>What a ranged one throws: a thrower's javelin, a kobold's stick of dynamite (a ball in flight).</summary>
        private ProjectileKind Shot => Kind == OrcKind.Kobold ? ProjectileKind.Cannonball : ProjectileKind.Javelin;

        /// <summary>Turns to face the blow's target and swings (the figure's animators hear Struck).</summary>
        private void StrikeAt(Vector3 target)
        {
            var to = target - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(to);
            Struck?.Invoke();
        }

        private const float TurnDegreesPerSecond = 540f;
        private Vector3 _lastPosition;

        /// <summary>Faces the way it walks: a raider used to slide along its route facing wherever it was made.</summary>
        private void LateUpdate()
        {
            var moved = transform.position - _lastPosition;
            _lastPosition = transform.position;
            moved.y = 0f;
            var dt = Time.deltaTime;
            // standing, or nudged by a neighbour: no turn
            if (dt <= 0f || moved.magnitude < 0.2f * dt) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(moved), TurnDegreesPerSecond * Time.deltaTime);
        }

        private void Strike(SoldierUnit soldier)
        {
            StrikeAt(soldier.transform.position);
            if (IsRanged) CombatProjectiles.Fire(Shot, CombatProjectiles.MuzzleOfUnit(transform), soldier, _attackDamage);
            else soldier.TakeDamage(_attackDamage);
            HitSoldier?.Invoke(soldier);
        }

        private void Strike(FreedPrisoner prisoner)
        {
            StrikeAt(prisoner.transform.position);
            if (IsRanged) CombatProjectiles.Fire(Shot, CombatProjectiles.MuzzleOfUnit(transform), prisoner, _attackDamage);
            else prisoner.TakeDamage(_attackDamage);
        }

        private void Strike(BuildingInstance building)
        {
            StrikeAt(building.transform.position);
            if (IsRanged) CombatProjectiles.Fire(Shot, CombatProjectiles.MuzzleOfUnit(transform), building, _attackDamage);
            else building.TryDamage(_attackDamage);
        }

        private void TickAttack()
        {
            _attackTimer -= Time.deltaTime;
            if (_attackTimer > 0f) return;
            _attackTimer = _attackIntervalSeconds;

            // a wall put up between this orc and what it was hitting takes the blow instead (the
            // user, 2026-10-25: a fence laid round a tower under attack, and the orcs hit on
            // through it). Only hand to hand: a thrower hits over a wall.
            if (!IsRanged)
            {
                var between = BuildingInTheWay(_target);
                if (between != null) _target = between;
            }

            Strike(_target);
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

            Step(toWaypoint / distance);
        }

        private void PinToGroundHeight()
        {
            var grid = GridManager.Instance;
            if (grid == null) return;

            var pos = transform.position;
            pos.y = grid.GroundHeight;
            transform.position = pos;
        }

        /// <summary>
        /// Nearest surviving BuildingInstance in the whole scene -- an orc always knows where the
        /// closest building is. In an Invasion run (the user's rule, 2026-10-21) it goes straight
        /// for the castle instead and only turns on a building it passes within
        /// inv_orc_building_radius.
        /// </summary>
        private void AcquireTarget(bool besideTheLane = false)
        {
            BuildingInstance nearest = null;
            var nearestDistSq = float.MaxValue;
            var castle = Invasion.InvasionMode.IsActive ? Invasion.InvasionMode.Instance.Castle : null;
            if (castle != null)
            {
                var radius = BalanceConfig.Instance.InvasionOrcBuildingRadius;
                nearestDistSq = radius * radius;
            }

            foreach (var instance in FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
            {
                // Roads/bridges are technically BuildingInstances too but not real targets -- a
                // raid beelining for the nearest stray road tile instead of an actual building
                // would make combat feel broken rather than threatening.
                if (instance.Data == null || instance.Data.isRoad) continue;
                // A run's walls and gates are obstacles (the user, 2026-10-22): broken only where
                // they stand in the way (AttackWhateverIsBlockingTheWay), never gone after; once
                // one is down the orc walks on through the gap.
                if (castle != null && Invasion.InvasionRules.IsBarrier(instance.Data)) continue;
                var distSq = (instance.transform.position - transform.position).sqrMagnitude;
                if (distSq < nearestDistSq)
                {
                    nearestDistSq = distSq;
                    nearest = instance;
                }
            }

            // On a portal's way only what stands beside it is gone for, and only when it is a short
            // walk off the way; the castle once the way is walked out.
            if (besideTheLane)
            {
                var atTheEnd = _lane.Length - _lane.Project(transform.position, out _) <= LaneEndMeters;
                if (nearest == null && atTheEnd) nearest = castle;
                if (nearest != null && !atTheEnd && !WithinADetour(nearest)) nearest = null;
                _target = nearest;
                _routeIndex = 0;
                _routeBlocked = false;
                _route = _target != null ? BuildRoute(transform.position, _target.transform.position, out _routeBlocked) : new[] { transform.position };
                return;
            }

            _target = nearest != null ? nearest : castle;
            _routeIndex = 0;
            _routeBlocked = false;
            _route = _target != null
                ? BuildRoute(transform.position, _target.transform.position, out _routeBlocked)
                : new[] { transform.position };
        }

        // ------------------------------------------------------------ a raiding party (Invasion)

        // This close to its camp, a party with nothing left to break is home and gone.
        private const float RaidHomeMeters = 2f;

        private bool _raiding;
        private Vector3 _raidHome;
        private Vector3 _raidCastle;
        private float _raidBaseRadius;

        /// <summary>One of a raiding party: goes for the player's buildings outside the base, not for the castle.</summary>
        public bool IsRaiding => _raiding;

        /// <summary>
        /// Sends this orc out with a raiding party (the user, 2026-10-24): it breaks the player's
        /// buildings further than `baseRadius` from the castle, nearest first, fights whoever it
        /// meets on the way, and once none is left walks back to its camp at `home` and is gone.
        /// </summary>
        public void RaidOutsideTheBase(Vector3 home, Vector3 castle, float baseRadius)
        {
            _raiding = true;
            _lane = null;
            _raidHome = home;
            _raidCastle = castle;
            _raidBaseRadius = baseRadius;
            _target = null;
            _retargetTimer = 0f;
        }

        /// <summary>One frame of the raid. False when a building is the target: the ordinary target code walks to it and breaks it.</summary>
        private bool Raid()
        {
            _retargetTimer -= Time.deltaTime;
            if (_target == null || _retargetTimer <= 0f)
            {
                _target = NearestBuildingOutsideTheBase(transform.position, _raidCastle, _raidBaseRadius);
                _routeIndex = 0;
                _routeBlocked = false;
                _route = _target != null ? BuildRoute(transform.position, _target.transform.position, out _routeBlocked) : new[] { transform.position };
                _retargetTimer = RetargetIntervalSeconds;
            }
            if (_target != null) return false;

            // nothing left out there: back to the camp, and gone
            var toHome = _raidHome - transform.position;
            toHome.y = 0f;
            if (toHome.magnitude <= RaidHomeMeters)
            {
                Destroy(gameObject);
                return true;
            }
            GuardWalk(_raidHome, straightLineFallback: false);
            if (_guardRoute.Blocked && _guardRoute.IsFinished) BreakThrough();
            return true;
        }

        /// <summary>The player's building (not a road, not a wall or gate) further than `baseRadius` from the castle and nearest `from`, or null.</summary>
        public static BuildingInstance NearestBuildingOutsideTheBase(Vector3 from, Vector3 castle, float baseRadius)
        {
            BuildingInstance nearest = null;
            var best = float.MaxValue;
            foreach (var instance in FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
            {
                if (instance.Data == null || instance.Data.isRoad || instance.CurrentHealth <= 0) continue;
                if (Invasion.InvasionRules.IsBarrier(instance.Data)) continue;
                var fromCastle = instance.transform.position - castle;
                fromCastle.y = 0f;
                if (fromCastle.magnitude <= baseRadius) continue;
                var distSq = (instance.transform.position - from).sqrMagnitude;
                if (distSq >= best) continue;
                best = distSq;
                nearest = instance;
            }
            return nearest;
        }

        // ------------------------------------------------------------ a portal's way (Invasion)

        // How far ahead down its way an orc walks at: far enough to round the way's corners smoothly.
        private const float LaneLookAheadMeters = 3f;
        // Off the way by more than this (it chased someone), an orc first walks back to it.
        private const float LaneRejoinMeters = 2.5f;
        // How far down the way a way round something in it must come back to.
        private const float LaneDetourLookMeters = 6f;
        // This close to the way's end the castle is what it goes for.
        private const float LaneEndMeters = 4f;
        // How often an orc on its way looks again at what is ahead: walking, and going round or breaking something.
        private const float LaneThinkSeconds = 0.3f;
        private const float LaneThinkBusySeconds = 1f;
        // A building closer than this to the line ahead is in the way; further off, the orc's body slips past it.
        private const float LaneBodyMeters = 0.2f;

        private enum LaneStep { Walk, Detour, Break }

        private OrcLane _lane;
        private LaneStep _laneStep;
        private float _laneThink;
        private Vector3 _laneGoal;
        private NavRoute _laneDetour;
        private BuildingInstance _laneBlocker;
        // Where the last thing it broke stood: walked through first, before the NavMesh has caught
        // up with the gap -- or the wall pieces either side of it are what the way looks blocked by.
        private Vector3? _laneBreach;

        /// <summary>The portal's way this orc keeps to, or null (a sandbox raid, a camp's guard).</summary>
        public OrcLane Lane => _lane;

        /// <summary>
        /// Sends this orc down a portal's way (the user, 2026-10-24): it keeps to it, breaks what is
        /// built across it, turns aside only for a building beside it or someone to fight, and comes
        /// back to it after.
        /// </summary>
        public void FollowLane(OrcLane lane)
        {
            _lane = lane;
            _laneThink = 0f;
            _laneBreach = null;
            _target = null;
            _retargetTimer = 0f;
        }

        /// <summary>One frame on the way. False when a building beside it (or the castle at its end) is the target: the ordinary target code then walks to it and hits it.</summary>
        private bool WalkTheLane()
        {
            _retargetTimer -= Time.deltaTime;
            if (_target == null || _retargetTimer <= 0f)
            {
                AcquireTarget(besideTheLane: true);
                _retargetTimer = RetargetIntervalSeconds;
            }
            if (_target != null) return false;

            _laneThink -= Time.deltaTime;
            if (_laneThink <= 0f) ThinkAboutTheLane();

            switch (_laneStep)
            {
                case LaneStep.Walk:
                    StepToward(_laneGoal);
                    break;
                case LaneStep.Detour:
                    if (_laneDetour.TryGetDirection(transform.position, out var direction)) Step(direction);
                    else _laneThink = 0f;
                    break;
                case LaneStep.Break:
                    if (_laneBlocker == null || _laneBlocker.CurrentHealth <= 0) { _laneThink = 0f; break; }
                    if (!CanStrike(_laneBlocker))
                    {
                        StepToward(ClosestPointOf(_laneBlocker));
                        break;
                    }
                    _attackTimer -= Time.deltaTime;
                    if (_attackTimer > 0f) break;
                    _attackTimer = _attackIntervalSeconds;
                    Strike(_laneBlocker);
                    if (_laneBlocker.CurrentHealth <= 0)
                    {
                        _laneBreach = MiddleOf(_laneBlocker);
                        _laneBlocker = null;
                        _laneThink = 0f;
                    }
                    break;
            }
            return true;
        }

        /// <summary>Decides how to go on down the way: straight on, a short way round, or through whatever is built across it.</summary>
        private void ThinkAboutTheLane()
        {
            _laneThink = LaneThinkSeconds;
            var at = transform.position;

            if (_laneBreach.HasValue)
            {
                var toBreach = _laneBreach.Value - at;
                toBreach.y = 0f;
                if (toBreach.magnitude > ArrivalThreshold * 4f)
                {
                    _laneStep = LaneStep.Walk;
                    _laneGoal = _laneBreach.Value;
                    return;
                }
                _laneBreach = null;
            }

            var along = _lane.Project(at, out var offLane);
            var goal = offLane > LaneRejoinMeters ? _lane.PointAt(along) : _lane.PointAt(along + LaneLookAheadMeters);
            if (!NavMesh.Raycast(at, goal, out _, CityBuilder.Maps.NavAreas.WithoutGates))
            {
                _laneStep = LaneStep.Walk;
                _laneGoal = goal;
                return;
            }

            // Something stands in the way. Round it, when the way round is short (a gap already broken beside the line)...
            _laneThink = LaneThinkBusySeconds;
            var further = _lane.PointAt(along + (offLane > LaneRejoinMeters ? LaneLookAheadMeters : LaneDetourLookMeters));
            _laneDetour ??= NewRoute();
            _laneDetour.SetDestination(at, further, straightLineFallback: false);
            var straight = new Vector3(further.x - at.x, 0f, further.z - at.z).magnitude;
            if (!_laneDetour.Blocked && !_laneDetour.IsFinished &&
                _laneDetour.RemainingLength(at) <= straight + BalanceConfig.Instance.InvasionLaneDetourMeters)
            {
                _laneStep = LaneStep.Detour;
                return;
            }

            // ...else through it: the building on the line ahead is broken.
            // Nothing on the line itself (only the NavMesh's margin round the pieces either side of
            // a gap): walk on -- reaching for the nearest piece here broke the wall beside a gap.
            _laneBlocker = BuildingOnTheLine(at, goal);
            if (_laneBlocker != null)
            {
                _laneStep = LaneStep.Break;
                return;
            }
            _laneStep = LaneStep.Walk;
            _laneGoal = goal;
        }

        /// <summary>A target beside the way is worth turning to only when the walk there is not much longer than the straight line (no going round by another causeway).</summary>
        private bool WithinADetour(BuildingInstance building)
        {
            _laneDetour ??= NewRoute();
            var at = transform.position;
            _laneDetour.SetDestination(at, building.transform.position, straightLineFallback: false);
            var to = building.transform.position - at;
            to.y = 0f;
            return _laneDetour.RemainingLength(at) <= to.magnitude + BalanceConfig.Instance.InvasionLaneDetourMeters;
        }

        /// <summary>The building (not a road) whose body the line from `from` to `to` passes through or by within LaneBodyMeters, nearest `from` first.</summary>
        private static BuildingInstance BuildingOnTheLine(Vector3 from, Vector3 to)
        {
            var line = to - from;
            line.y = 0f;
            var length = line.magnitude;
            if (length < 0.01f) return null;
            var direction = line / length;

            BuildingInstance first = null;
            var firstAt = float.MaxValue;
            foreach (var instance in FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
            {
                if (instance.Data == null || instance.Data.isRoad || instance.CurrentHealth <= 0) continue;
                var body = instance.GetComponent<Collider>();
                if (body == null) continue;
                for (var d = 0f; d <= length + 0.5f && d < firstAt; d += 0.25f)
                {
                    var point = from + direction * d;
                    var on = body.ClosestPoint(new Vector3(point.x, body.bounds.center.y, point.z));
                    if (new Vector3(on.x - point.x, 0f, on.z - point.z).magnitude > LaneBodyMeters) continue;
                    first = instance;
                    firstAt = d;
                    break;
                }
            }
            return first;
        }

        /// <summary>The point of the building's body nearest this orc, at the orc's height.</summary>
        private Vector3 ClosestPointOf(BuildingInstance building)
        {
            var at = transform.position;
            var body = building.GetComponent<Collider>();
            if (body == null) return building.transform.position;
            var point = body.ClosestPoint(new Vector3(at.x, body.bounds.center.y, at.z));
            point.y = at.y;
            return point;
        }

        /// <summary>The middle of the building's body, at this orc's height: where the gap is once it is down.</summary>
        private Vector3 MiddleOf(BuildingInstance building)
        {
            var body = building.GetComponent<Collider>();
            var point = body != null ? body.bounds.center : building.transform.position;
            point.y = transform.position.y;
            return point;
        }

        private void StepToward(Vector3 goal)
        {
            var to = goal - transform.position;
            to.y = 0f;
            var distance = to.magnitude;
            if (distance < ArrivalThreshold) return;
            Step(to / distance);
        }

        private void Step(Vector3 direction)
        {
            // wedged inside something (a building put up round it): through it to free ground first
            if (_escape.Step(transform, _controller, _walkSpeed)) return;
            // the New Year's blizzard slows everyone (Weather)
            var step = direction * _walkSpeed * Weather.SpeedMultiplier * Time.deltaTime;
            if (_controller != null && _controller.enabled)
            {
                _controller.Move(step);
                PinToGroundHeight();
            }
            else
            {
                transform.position += step;
            }
            _escape.Watch(transform, direction, _walkSpeed, CityBuilder.Maps.NavAreas.WithoutGates);
        }

        private readonly StuckEscape _escape = new StuckEscape();


        /// <summary>
        /// The nearest building close enough to be what's standing in the way -- called once the
        /// orc has walked its route out and still can't reach what it was heading for. That is what
        /// makes a fence a real obstacle instead of scenery: the raider stops at it and starts
        /// breaking it rather than pretending the way is open.
        /// </summary>
        private void AttackWhateverIsBlockingTheWay()
        {
            var blocker = NearestBuildingWithin(BlockerReachRadius);
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
            // A building's middle is inside its own carve, and a path to a point off the NavMesh
            // fails outright -- which fell through to the straight line below and walked the
            // waves through a run's gates, whose bodies are triggers (the user, 2026-10-23). Aim at
            // the nearest point the NavMesh has, as NavRoute does: a walled-off target then gives an
            // honest partial path that stops at the wall.
            if (NavMesh.SamplePosition(to, out var onMesh, RouteTargetSnapMeters, CityBuilder.Maps.NavAreas.WithoutGates)) to = onMesh.position;
            // Never through a gate: for an orc it is a wall to break, not a door (NavAreas).
            if (NavMesh.CalculatePath(from, to, CityBuilder.Maps.NavAreas.WithoutGates, _sharedPath) && _sharedPath.corners.Length > 0)
            {
                blocked = _sharedPath.status != NavMeshPathStatus.PathComplete;
                return _sharedPath.corners;
            }

            blocked = false;
            return new[] { to };
        }
    }
}
