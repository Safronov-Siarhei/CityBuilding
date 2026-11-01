using System.Collections.Generic;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Saving;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Dungeon
{
    /// <summary>
    /// The gnomes hired at the dungeon's camp (the user, 2026-10-31/11-01, card
    /// Design/features/dungeon.md): the miner, the crossbowman and the ore guardian, bought for
    /// coins at the hire camp's card (DungeonHireCard) without end but no more than
    /// dungeon_hireling_max alive at once. Nobody orders them one by one: they are one party of
    /// their own -- not one of the army's groups, so no icon, no upkeep, no citizen -- that walks
    /// after the hero the player holds (or the first one standing) and fights what comes within
    /// its reach. One left too far behind drops the fight and catches up. They never leave the
    /// dungeon: going up, the living are written down (DungeonRecord.Hirelings) and on the next
    /// visit they are waiting at the way in, as hurt as they were.
    ///
    /// The heroes below are one party too (the user, 2026-11-01): the player leads ONE of them --
    /// the one held, or the last one held -- and the others walk after him like the gnomes do.
    /// </summary>
    public class DungeonHirelings : MonoBehaviour
    {
        private const float TickSeconds = 0.25f;
        // how long a gnome left behind walks straight back to the hero, ignoring fights (seconds)
        private const float CatchUpSeconds = 2.5f;

        /// <summary>What the camp offers, in the card's order.</summary>
        public static readonly SoldierType[] Offered = { SoldierType.GnomeMiner, SoldierType.GnomeCrossbow, SoldierType.GnomeGuardian };

        public static DungeonHirelings Instance { get; private set; }

        private ArmyGroup _party;
        private string _dungeonId;
        private Vector3 _camp;
        private Vector3 _ordered;
        private bool _hasOrder;
        private float _timer;

        /// <summary>The gnomes alive now.</summary>
        public int Count => _party != null ? _party.Count : 0;

        public IReadOnlyList<SoldierUnit> Members => _party != null ? _party.Members : (IReadOnlyList<SoldierUnit>)new SoldierUnit[0];

        /// <summary>The most alive at once (dungeon_hireling_max).</summary>
        public static int Max => BalanceConfig.Instance != null ? BalanceConfig.Instance.DungeonHirelingMax : 5;

        /// <summary>A gnome's price in coins (units sheet, recruit_coins).</summary>
        public static int Price(SoldierType type) => SoldierStats.StatsAt(type, 1).recruitCoins;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// The party down: those who waited for the heroes (this dungeon's, from the town's save)
        /// stand by `waitAt`; new ones step out of the hire camp at `camp`.
        /// </summary>
        public void Begin(string dungeonId, Vector3 waitAt, Vector3 camp, IEnumerable<HirelingEntry> waiting)
        {
            _dungeonId = dungeonId;
            _camp = camp;
            _party = new ArmyGroup(SoldierType.GnomeMiner, waitAt) { IsSummoned = true };
            if (waiting == null) return;
            var i = 0;
            foreach (var entry in waiting)
            {
                if (entry == null || entry.dungeonId != dungeonId || !SoldierStats.IsHireling(entry.type)) continue;
                if (Count >= Max) break;
                var unit = Spawn(entry.type, waitAt + ArmyGroup.FormationOffset(i++));
                if (unit != null) unit.SetCurrentHealth(entry.currentHealth);
            }
            _party.OrderMoveTo(waitAt);
        }

        /// <summary>Room in the party and the coins for this one.</summary>
        public bool CanHire(SoldierType type)
        {
            if (_party == null || Count >= Max || !SoldierStats.IsHireling(type)) return false;
            var mode = DungeonMode.Instance;
            return mode != null && mode.CanPay(Price(type));
        }

        /// <summary>A gnome bought: paid for and out of the camp's door. Null when there is no room or no coins.</summary>
        public SoldierUnit Hire(SoldierType type)
        {
            if (!CanHire(type) || !DungeonMode.Instance.TrySpendCoins(Price(type))) return null;
            var unit = Spawn(type, _camp);
            // straight to the party's place, not to the camp's
            _hasOrder = false;
            _timer = 0f;
            return unit;
        }

        private SoldierUnit Spawn(SoldierType type, Vector3 at)
        {
            var army = ArmyManager.Instance;
            if (army == null) return null;
            if (NavMesh.SamplePosition(at, out var hit, 3f, NavMesh.AllAreas)) at = hit.position;
            return army.SpawnHireling(type, at, _party);
        }

        /// <summary>The living gnomes as the town's save keeps them.</summary>
        public List<HirelingEntry> Survivors()
        {
            var list = new List<HirelingEntry>();
            foreach (var unit in Members)
            {
                if (unit == null || unit.CurrentHealth <= 0) continue;
                list.Add(new HirelingEntry { dungeonId = _dungeonId, type = unit.Type, currentHealth = unit.CurrentHealth });
            }
            return list;
        }

        private static Hero _lastLed;

        /// <summary>The hero the party follows: the one held, else the last one held while he stands, else the first one standing.</summary>
        public static Hero Leader()
        {
            var roster = HeroRoster.Instance;
            if (roster == null) return null;
            var held = ArmyManager.Instance != null ? ArmyManager.Instance.SelectedGroup : null;
            Hero first = null;
            foreach (var hero in roster.Heroes)
            {
                if (hero == null || hero.IsFallen || hero.Unit == null) continue;
                if (held != null && hero.Unit.Group == held)
                {
                    _lastLed = hero;
                    return hero;
                }
                if (first == null) first = hero;
            }
            if (_lastLed != null && !_lastLed.IsFallen && _lastLed.Unit != null) return _lastLed;
            return first;
        }

        private readonly System.Collections.Generic.Dictionary<Hero, Vector3> _heroOrders = new System.Collections.Generic.Dictionary<Hero, Vector3>();

        /// <summary>The heroes not led walk after the leader, each to his own place beside him.</summary>
        private void FollowWithHeroes(Hero leader, float follow)
        {
            var roster = HeroRoster.Instance;
            if (roster == null) return;
            var at = leader.Unit.transform.position;
            var place = 1;
            foreach (var hero in roster.Heroes)
            {
                if (hero == null || hero == leader || hero.IsFallen || hero.Unit == null || hero.Unit.Group == null) continue;
                var spot = at + ArmyGroup.FormationOffset(place++) * 1.5f;
                if (NavMesh.SamplePosition(spot, out var hit, 2f, NavMesh.AllAreas)) spot = hit.position;
                if (_heroOrders.TryGetValue(hero, out var given) && Flat(given - spot) <= follow && Flat(hero.Unit.transform.position - spot) <= follow * 2f) continue;
                _heroOrders[hero] = spot;
                hero.Unit.Group.OrderMoveTo(spot);
            }
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = TickSeconds;
            var leader = Leader();
            if (leader == null) return;
            var balance = BalanceConfig.Instance;
            var follow = balance != null ? balance.DungeonHirelingFollowCells : 2.5f;
            var leash = balance != null ? balance.DungeonHirelingLeashCells : 9f;
            FollowWithHeroes(leader, follow);
            if (_party == null || _party.Count == 0) return;
            var at = leader.Unit.transform.position;
            if (!_hasOrder || Flat(at - _ordered) > follow)
            {
                _ordered = at;
                _hasOrder = true;
                _party.OrderMoveTo(at);
            }
            foreach (var unit in _party.Members)
            {
                if (unit == null || unit.IsRetreating) continue;
                if (Flat(unit.transform.position - at) > leash) unit.Retreat(at, CatchUpSeconds);
            }
        }

        private static float Flat(Vector3 v)
        {
            v.y = 0f;
            return v.magnitude;
        }
    }
}
