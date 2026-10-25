using System;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Combat
{
    /// <summary>
    /// A hero: the King (the user, 2026-10-21), and the Queen and the Captain of the guard bought
    /// in the castle (2026-10-23) -- an Invasion run's, and since 2026-10-25 the sandbox's too, all
    /// three bought in the Town Hall there (HeroRoster). He gains experience from every enemy that
    /// falls near him, levels up -- more health and damage each level, his skills open and grow
    /// (HeroSkills) -- and when he falls he is not lost but gets up again at the castle after a wait
    /// that grows with his level, as in Dota.
    ///
    /// Sits beside the SoldierUnit that does his fighting: everything about walking, targeting and
    /// orders is the army's, so the orcs fight him and the player commands him exactly as they do
    /// any soldier. Experience for a kill is the enemy's health -- a brute is worth more than an
    /// orc without a table to keep. His numbers are the economy tab's &lt;id&gt;_* keys (HeroBalance);
    /// the experience curve, the waits and the guard are the king_* keys, the same for all three.
    ///
    /// The Queen fights from range and backs off from a melee enemy that comes too close (the
    /// user, 2026-10-23): a few metres towards her own, then she shoots again -- unless the player
    /// has her walking somewhere, which comes first.
    /// </summary>
    public class Hero : MonoBehaviour
    {
        private SoldierUnit _unit;
        private CharacterController _controller;
        private Renderer[] _renderers;

        /// <summary>"king", "queen" or "captain": his rows in the sheet and his skills' prefix.</summary>
        public string Id { get; private set; } = "king";

        public SoldierType Type { get; private set; } = SoldierType.King;

        public HeroBalance Balance => BalanceConfig.Instance.Hero(Id);

        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }

        /// <summary>Experience still needed for the next level; 0 at the top level.</summary>
        public int ExperienceToNext => Level >= BalanceConfig.Instance.KingMaxLevel ? 0 : XpForLevel(Level) - Experience;

        /// <summary>Down and waiting to come back.</summary>
        public bool IsFallen { get; private set; }

        /// <summary>Seconds until he is back on his feet; 0 while he stands.</summary>
        public float RespawnRemaining { get; private set; }

        /// <summary>Where he comes back: the castle.</summary>
        public Vector3 Home { get; set; }

        /// <summary>
        /// With no order from the player he defends the base (the user, 2026-10-21): goes for any
        /// enemy that comes within king_guard_radius of the castle and walks back home when it is
        /// clear. A player's order ends it -- he then does what he was told -- and being let go of
        /// starts it again after a wait (ReturnIn).
        /// </summary>
        public bool Guarding { get; private set; } = true;

        /// <summary>The middle of what he guards; the castle's spot. Defaults to Home.</summary>
        public Vector3 GuardCentre { get; set; }

        private const float GuardScanSeconds = 0.5f;
        /// <summary>Slower than this (m/s) and not fighting, he has done what he was told.</summary>
        private const float StillSpeed = 0.2f;
        private bool _revealing;
        private float _guardScan;

        /// <summary>
        /// Let go of after an order (the user, 2026-10-23): he finishes it, then stands
        /// hero_return_wait_sec -- fighting whatever comes close -- and only then goes home to guard.
        /// Negative while not counting.
        /// </summary>
        public float ReturnIn { get; private set; } = -1f;

        private Vector3 _lastPosition;
        /// <summary>Walking to a point the player gave him: the Queen does not run from that.</summary>
        private bool _onMoveOrder;

        public SoldierUnit Unit => _unit;

        /// <summary>Seconds left of the boss's blessing (InvasionJungle): more damage and health while it lasts; 0 without one.</summary>
        public float BuffRemaining { get; private set; }

        private float _buffDamage, _buffHealth;

        /// <summary>Raised on a level gained, a fall and a return -- for the hero's portrait on the HUD.</summary>
        public event Action Changed;

        /// <summary>Experience from one level to the next: king_xp_per_level times the level he is at.</summary>
        public static int XpForLevel(int level) => BalanceConfig.Instance.KingXpPerLevel * Mathf.Max(1, level);

        /// <summary>How long a fall keeps him down at a level.</summary>
        public static float RespawnSecondsAt(int level) =>
            BalanceConfig.Instance.KingRespawnBaseSeconds + BalanceConfig.Instance.KingRespawnPerLevelSeconds * (Mathf.Max(1, level) - 1);

        /// <summary>His health and damage at a level: level 1 is the sheet's &lt;id&gt;_max_health and &lt;id&gt;_attack_damage.</summary>
        public int MaxHealthAt(int level) => Balance.maxHealth + Balance.healthPerLevel * (Mathf.Max(1, level) - 1);

        public int DamageAt(int level) => Balance.attackDamage + Balance.damagePerLevel * (Mathf.Max(1, level) - 1);

        /// <summary>Called by ArmyManager.SpawnHero right after the unit is built.</summary>
        public void Initialize(Vector3 home, SoldierType type)
        {
            Type = type;
            Id = SoldierStats.SheetIdOf(type);
            Home = home;
            GuardCentre = home;
            _unit = GetComponent<SoldierUnit>();
            _controller = GetComponent<CharacterController>();
            _renderers = GetComponentsInChildren<Renderer>(true);
            OrcUnit.Died += OnOrcDied;
            if (_unit.Group != null) _unit.Group.PlayerOrdered += OnPlayerOrdered;
            if (ArmyManager.Instance != null) ArmyManager.Instance.OnSelectionChanged += OnSelectionChanged;
            ApplyLevel();
        }

        private void OnDestroy()
        {
            OrcUnit.Died -= OnOrcDied;
            if (_unit != null && _unit.Group != null) _unit.Group.PlayerOrdered -= OnPlayerOrdered;
            if (ArmyManager.Instance != null) ArmyManager.Instance.OnSelectionChanged -= OnSelectionChanged;
        }

        /// <summary>
        /// Let go of while on an order, he carries it out, waits a while and then goes home to guard
        /// the base (the user, 2026-10-23; he used to turn for home at once). Taken in hand again, the
        /// wait is off.
        /// </summary>
        private void OnSelectionChanged()
        {
            var army = ArmyManager.Instance;
            if (army == null || _unit.Group == null) return;
            if (army.SelectedGroup == _unit.Group)
            {
                ReturnIn = -1f;
                return;
            }
            if (IsFallen || Guarding || ReturnIn >= 0f) return;
            ReturnIn = BalanceConfig.Instance.HeroReturnWaitSeconds;
        }

        /// <summary>The wait after an order: it runs only while he stands with nobody to fight.</summary>
        private void TickReturn()
        {
            var moved = Flat(transform.position - _lastPosition).magnitude;
            _lastPosition = transform.position;
            var dt = Time.deltaTime;
            var still = dt > 0f && moved < StillSpeed * dt;
            // got where the player sent him
            if (_onMoveOrder && _unit.Group != null && Flat(transform.position - _unit.Group.HoldPosition).sqrMagnitude < 2.25f) _onMoveOrder = false;
            if (ReturnIn < 0f) return;
            if (_unit.IsFighting || !still)
            {
                ReturnIn = BalanceConfig.Instance.HeroReturnWaitSeconds;
                return;
            }
            ReturnIn -= dt;
            if (ReturnIn > 0f) return;
            ReturnIn = -1f;
            Guard();
        }

        /// <summary>Experience for one enemy he struck down: its health.</summary>
        public static int ExperienceFor(IDamageTarget target) => target is OrcUnit orc ? Mathf.Max(1, orc.MaxHealth) : 0;

        /// <summary>
        /// As in Dota, the hero learns from every enemy that falls near him, not only from his own
        /// blows: a tower's or a soldier's kill beside him counts (the small base island's towers
        /// would otherwise take most of his kills).
        /// </summary>
        private void OnOrcDied(OrcUnit orc)
        {
            if (IsFallen || orc == null) return;
            var offset = orc.transform.position - transform.position;
            offset.y = 0f;
            var radius = BalanceConfig.Instance.KingXpRadius;
            if (offset.sqrMagnitude <= radius * radius) AddExperience(ExperienceFor(orc));
        }

        /// <summary>Adds experience and takes every level it pays for. Public for the tests and for experience that does not come from a blow of his own.</summary>
        public void AddExperience(int amount)
        {
            if (amount <= 0) return;
            var maxLevel = BalanceConfig.Instance.KingMaxLevel;
            if (Level >= maxLevel) return;

            Experience += amount;
            var levelled = false;
            while (Level < maxLevel && Experience >= XpForLevel(Level))
            {
                Experience -= XpForLevel(Level);
                Level++;
                levelled = true;
            }
            if (Level >= maxLevel) Experience = 0;
            if (!levelled) return;

            ApplyLevel();
            // A level is a fresh breath, as in Dota: what he gains is added to what he has.
            Changed?.Invoke();
        }

        /// <summary>A hero a save puts back (the sandbox keeps its heroes): his level, experience and health, and, if he was down, the wait still left.</summary>
        public void Restore(int level, int experience, int health, bool fallen, float respawnRemaining)
        {
            Level = Mathf.Clamp(level, 1, Mathf.Max(1, BalanceConfig.Instance.KingMaxLevel));
            Experience = Level >= BalanceConfig.Instance.KingMaxLevel ? 0 : Mathf.Max(0, experience);
            ApplyLevel();
            if (health > 0) _unit.SetCurrentHealth(Mathf.Min(health, _unit.MaxHealth));
            if (fallen)
            {
                IsFallen = true;
                RespawnRemaining = Mathf.Max(0.1f, respawnRemaining);
                _unit.enabled = false;
                if (_controller != null) _controller.enabled = false;
                SetVisible(false);
                StopRevealing();
            }
            Changed?.Invoke();
        }

        /// <summary>
        /// The boss's blessing: for that many seconds his damage and health are raised by the given fractions
        /// (0.5 = half as much again). A second one while the first lasts starts it afresh.
        /// </summary>
        public void Empower(float seconds, float damageBonus, float healthBonus)
        {
            BuffRemaining = Mathf.Max(0f, seconds);
            _buffDamage = Mathf.Max(0f, damageBonus);
            _buffHealth = Mathf.Max(0f, healthBonus);
            ApplyLevel();
            Changed?.Invoke();
        }

        private void ApplyLevel()
        {
            var before = _unit.MaxHealth;
            var buffed = BuffRemaining > 0f;
            var health = MaxHealthAt(Level) * (buffed ? 1f + _buffHealth : 1f);
            var damage = DamageAt(Level) * (buffed ? 1f + _buffDamage : 1f);
            _unit.SetHeroStats(Mathf.RoundToInt(health), Mathf.RoundToInt(damage));
            if (_unit.MaxHealth > before) _unit.SetCurrentHealth(_unit.CurrentHealth + (_unit.MaxHealth - before));
        }

        /// <summary>
        /// Struck down: out of the fight (no orc sees him, the army panel's orders do nothing) until
        /// the wait for his level runs out. The body is hidden, not destroyed, so his level and
        /// experience stay where they are. The Captain's Бессмертие, when it is ready, has him back
        /// on his feet where he fell instead (HeroSkills.TryCheatDeath).
        /// </summary>
        public void Fall()
        {
            if (IsFallen) return;
            var skills = GetComponent<HeroSkills>();
            if (skills != null && skills.TryCheatDeath()) return;
            IsFallen = true;
            ReturnIn = -1f;
            _onMoveOrder = false;
            RespawnRemaining = RespawnSecondsAt(Level);
            // a fallen hero takes no orders: let go of him if he was in hand
            var army = ArmyManager.Instance;
            if (army != null && _unit.Group != null && army.SelectedGroup == _unit.Group) army.SelectGroup(null);
            _unit.enabled = false;
            if (_controller != null) _controller.enabled = false;
            // he falls where he stood, as an orc does, and is gone until he rises at the castle
            // (the user, 2026-10-23: the Queen gave her life and just vanished on her feet)
            OrcCorpse.LeaveCopy(transform, null);
            SetVisible(false);
            StopRevealing();
            Changed?.Invoke();
        }

        private void OnPlayerOrdered()
        {
            _onMoveOrder = _unit.Group != null && _unit.Group.AttackTarget == null;
            if (!Guarding) return;
            Guarding = false;
            Changed?.Invoke();
        }

        /// <summary>Back to defending the base, from wherever he was sent: home first, the guard's scan turns him on any orc near the castle.</summary>
        public void Guard()
        {
            ReturnIn = -1f;
            Guarding = true;
            _onMoveOrder = false;
            _guardScan = 0f;
            if (!IsFallen) _unit.Group?.OrderMoveTo(Home);
            Changed?.Invoke();
        }

        private void Update()
        {
            if (BuffRemaining > 0f)
            {
                BuffRemaining -= Time.deltaTime;
                if (BuffRemaining <= 0f)
                {
                    BuffRemaining = 0f;
                    ApplyLevel();
                    Changed?.Invoke();
                }
            }

            if (IsFallen)
            {
                RespawnRemaining -= Time.deltaTime;
                if (RespawnRemaining <= 0f) Rise();
                return;
            }
            RevealAround();
            TickReturn();
            TickFlee();
            if (!Guarding || _unit.Group == null) return;

            _guardScan -= Time.deltaTime;
            if (_guardScan > 0f) return;
            _guardScan = GuardScanSeconds;

            var threat = NearestThreat();
            var group = _unit.Group;
            if (threat != null)
            {
                if (group.AttackTarget != (IDamageTarget)threat) group.OrderAttack(threat);
                return;
            }
            if (group.AttackTarget == null && Flat(group.HoldPosition - Home).sqrMagnitude > 1f) group.OrderMoveTo(Home);
        }

        private float _fleeScan;
        private const float FleeScanSeconds = 0.25f;

        /// <summary>
        /// The Queen backs off from a melee enemy closer than her flee radius: flee distance metres
        /// away from it and towards her own (the nearest soldier, or the castle), on the NavMesh --
        /// then she shoots again. Not while she walks where the player sent her.
        /// </summary>
        private void TickFlee()
        {
            var balance = Balance;
            if (balance == null || balance.fleeRadius <= 0f || _unit.IsRetreating || _onMoveOrder) return;
            _fleeScan -= Time.deltaTime;
            if (_fleeScan > 0f) return;
            _fleeScan = FleeScanSeconds;

            var at = transform.position;
            OrcUnit threat = null;
            var best = balance.fleeRadius * balance.fleeRadius;
            foreach (var orc in OrcUnit.All)
            {
                if (orc == null || orc.CurrentHealth <= 0 || orc.IsRanged) continue;
                var d = Flat(orc.transform.position - at).sqrMagnitude;
                if (d > best) continue;
                best = d;
                threat = orc;
            }
            if (threat == null) return;

            var away = Flat(at - threat.transform.position).normalized;
            var friends = Flat(NearestFriend(at) - at).normalized;
            var way = (away + friends).sqrMagnitude > 0.01f ? (away + friends).normalized : away;
            var goal = at + way * balance.fleeDistance;
            if (!NavMesh.SamplePosition(goal, out var hit, 3f, NavMesh.AllAreas))
            {
                // nowhere to go that way: towards the castle
                goal = at + Flat(Home - at).normalized * balance.fleeDistance;
                if (!NavMesh.SamplePosition(goal, out hit, 3f, NavMesh.AllAreas)) return;
            }
            _unit.Retreat(hit.position, balance.fleeDistance / Mathf.Max(0.5f, balance.walkSpeed) + 1f);
        }

        /// <summary>The nearest of her own -- another soldier or hero standing -- or the castle.</summary>
        private Vector3 NearestFriend(Vector3 at)
        {
            var best = float.MaxValue;
            var friend = Home;
            foreach (var soldier in SoldierUnit.All)
            {
                if (soldier == null || soldier == _unit || soldier.CurrentHealth <= 0 || !soldier.isActiveAndEnabled) continue;
                var d = Flat(soldier.transform.position - at).sqrMagnitude;
                if (d >= best) continue;
                best = d;
                friend = soldier.transform.position;
            }
            return friend;
        }

        /// <summary>
        /// The fog lifts round him while he is there and closes again behind him, as round a
        /// citizen (the user, 2026-10-22) -- king_reveal_radius cells; none while he is down.
        /// </summary>
        private void RevealAround()
        {
            if (_revealing) return;
            var fog = CityBuilder.Maps.FogOfWarManager.Instance;
            if (fog == null) return;
            fog.AddWalker(transform, BalanceConfig.Instance.KingRevealRadius);
            _revealing = true;
        }

        private void StopRevealing()
        {
            if (!_revealing) return;
            CityBuilder.Maps.FogOfWarManager.Instance?.RemoveWalker(transform);
            _revealing = false;
        }

        /// <summary>The enemy nearest the castle among those inside the guard radius, or null.</summary>
        private OrcUnit NearestThreat()
        {
            var radius = BalanceConfig.Instance.KingGuardRadius;
            OrcUnit nearest = null;
            var best = radius * radius;
            foreach (var orc in OrcUnit.All)
            {
                if (orc == null || orc.CurrentHealth <= 0) continue;
                var d = Flat(orc.transform.position - GuardCentre).sqrMagnitude;
                if (d > best) continue;
                best = d;
                nearest = orc;
            }
            return nearest;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        /// <summary>Back at the castle, at full health, holding there.</summary>
        public void Rise()
        {
            if (!IsFallen) return;
            IsFallen = false;
            RespawnRemaining = 0f;
            transform.position = Home;
            _unit.Group?.OrderMoveTo(Home);
            Guarding = true;
            _unit.Revive();
            if (_controller != null) _controller.enabled = true;
            _unit.enabled = true;
            SetVisible(true);
            Changed?.Invoke();
        }

        private void SetVisible(bool visible)
        {
            if (_renderers == null) return;
            foreach (var r in _renderers)
            {
                if (r != null) r.enabled = visible;
            }
        }
    }
}
