using System;
using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>What a camp building is for. Saved by its number, so new kinds go on the end.</summary>
    public enum OrcCampBuildingKind
    {
        /// <summary>Shoots the player's soldiers that come within range.</summary>
        Watchtower = 0,
        /// <summary>The grunts' spawner: while it stands it keeps the camp's grunts topped up.</summary>
        WarTent = 1,
        /// <summary>The portal's shield: while it stands, the portal takes no damage.</summary>
        Totem = 2,
        /// <summary>One cell of the camp's wall. Blocks the way until it is broken.</summary>
        Palisade = 3,
        /// <summary>The gate in the wall: a wall piece, only weaker.</summary>
        Gate = 4,
        /// <summary>The inner gate of a level-3 camp: a gate that shoots, like a tower.</summary>
        GateTower = 5,
        /// <summary>The loot store (the user's «склад»): a building to break, no more.</summary>
        Storehouse = 6,
        /// <summary>The brutes' spawner.</summary>
        BrutePit = 7,
        /// <summary>The throwers' spawner.</summary>
        ThrowerRange = 8,
        /// <summary>The chief's house on the main base: while it stands, a slain chief comes back.</summary>
        ChiefHouse = 9,
        /// <summary>The prisoners' cage (the user's idea, 2026-10-19): broken, it lets townsfolk out who walk home to the Town Hall.</summary>
        Cage = 10,
        /// <summary>The chief's treasury on the main base (2026-10-19): no part in a fight, a big payout when broken.</summary>
        Treasury = 11,
    }

    /// <summary>
    /// One building of an orc camp (see OrcCamp). The player's army attacks it like a portal -- it
    /// is an IDamageTarget structure -- and once it falls it leaves its ruin (if it has one) and
    /// tells its camp, which is how the totem's fall drops the portal's shield, a spawner's stops
    /// its kind of guard coming back, and a broken wall piece opens the way.
    ///
    /// Self-registering, like OrcPortal, so soldiers can enumerate the camps' buildings as targets
    /// without scanning the scene.
    /// </summary>
    public class OrcCampBuilding : MonoBehaviour, IDamageTarget
    {
        private static readonly List<OrcCampBuilding> _all = new List<OrcCampBuilding>();

        /// <summary>Every camp building still standing.</summary>
        public static IReadOnlyList<OrcCampBuilding> All => _all;

        /// <summary>Raised after a camp building has fallen and left the registry.</summary>
        public static event Action<OrcCampBuilding> OnDestroyed;

        public OrcCampBuildingKind Kind { get; private set; }
        public int Level { get; private set; } = 1;
        public OrcCamp Camp { get; private set; }

        /// <summary>The grid cell (lower-left of the plot) it stands on -- what the save writes.</summary>
        public Vector2Int Cell { get; private set; }

        public Vector2Int Footprint { get; private set; } = Vector2Int.one;

        public int MaxHealth { get; private set; }
        public int CurrentHealth { get; private set; }

        public bool IsWall => OrcBaseLayout.IsWall(Kind);

        private GameObject _ruinModel;
        private float _shotTimer;
        private int _shotDamage;
        private float _shotRange;

        Transform IDamageTarget.Transform => this != null ? transform : null;
        bool IDamageTarget.IsAlive => this != null && CurrentHealth > 0;
        bool IDamageTarget.IsStructure => true;

        /// <summary>Called by OrcRaidManager right after building it.</summary>
        public void Initialize(OrcCampBuildingKind kind, int level, OrcCamp camp, Vector2Int cell, Vector2Int footprint, GameObject ruinModel)
        {
            Kind = kind;
            Level = Mathf.Max(1, level);
            Camp = camp;
            Cell = cell;
            Footprint = footprint;
            _ruinModel = ruinModel;

            var balance = BalanceConfig.Instance;
            MaxHealth = HealthOf(kind, Level, balance);
            CurrentHealth = MaxHealth;
            if (Level >= OrcBaseLayout.MainLevel && kind == OrcCampBuildingKind.Watchtower)
            {
                _shotDamage = balance.OrcMainTowerDamage;
                _shotRange = balance.OrcMainTowerRangeMeters;
            }
            else if (Level >= OrcBaseLayout.MainLevel && kind == OrcCampBuildingKind.GateTower)
            {
                _shotDamage = balance.OrcMainGateDamage;
                _shotRange = balance.OrcMainGateRangeMeters;
            }
            else if (kind == OrcCampBuildingKind.Watchtower)
            {
                _shotDamage = balance.OrcTowerDamageAt(Level);
                _shotRange = balance.OrcTowerRangeAt(Level);
            }
            else if (kind == OrcCampBuildingKind.GateTower)
            {
                _shotDamage = balance.OrcGateTowerDamage;
                _shotRange = balance.OrcGateTowerRangeMeters;
            }
        }

        /// <summary>How much a building of this kind and level can take, from the sheet.</summary>
        public static int HealthOf(OrcCampBuildingKind kind, int level, BalanceConfig balance)
        {
            if (level >= OrcBaseLayout.MainLevel)
            {
                switch (kind)
                {
                    case OrcCampBuildingKind.Watchtower: return balance.OrcMainTowerHealth;
                    case OrcCampBuildingKind.Totem: return balance.OrcMainTotemHealth;
                    case OrcCampBuildingKind.Palisade: return balance.OrcMainWallHealth;
                    case OrcCampBuildingKind.Gate:
                    case OrcCampBuildingKind.GateTower: return balance.OrcMainGateHealth;
                    case OrcCampBuildingKind.Storehouse: return balance.OrcMainStorehouseHealth;
                    case OrcCampBuildingKind.ChiefHouse: return balance.OrcChiefHouseHealth;
                    case OrcCampBuildingKind.Cage: return balance.OrcMainCageHealth;
                    case OrcCampBuildingKind.Treasury: return balance.OrcMainTreasuryHealth;
                    default: return balance.OrcMainSpawnerHealth;
                }
            }
            switch (kind)
            {
                case OrcCampBuildingKind.Watchtower: return balance.OrcTowerHealthAt(level);
                case OrcCampBuildingKind.WarTent: return balance.OrcTentHealth;
                case OrcCampBuildingKind.Totem: return balance.OrcTotemHealthAt(level);
                case OrcCampBuildingKind.Palisade: return balance.OrcPalisadeHealthAt(level);
                case OrcCampBuildingKind.Gate: return balance.OrcGateHealthAt(level);
                case OrcCampBuildingKind.GateTower: return balance.OrcGateTowerHealth;
                case OrcCampBuildingKind.Storehouse: return balance.OrcStorehouseHealthAt(level);
                case OrcCampBuildingKind.BrutePit: return balance.OrcBrutePitHealth;
                case OrcCampBuildingKind.Cage: return balance.OrcCageHealthAt(level);
                default: return balance.OrcThrowerRangeHealth;
            }
        }

        /// <summary>Puts a loaded building back on the health it had. Never below 1 -- only TakeDamage retires one.</summary>
        public void SetCurrentHealth(int health)
        {
            CurrentHealth = Mathf.Clamp(health, 1, MaxHealth);
        }

        private void OnEnable()
        {
            _all.Add(this);
        }

        private void OnDisable()
        {
            _all.Remove(this);
        }

        private void Update()
        {
            if (_shotDamage <= 0 || CurrentHealth <= 0) return;

            _shotTimer -= Time.deltaTime;
            if (_shotTimer > 0f) return;

            var target = NearestSoldierWithin(_shotRange);
            if (target == null) return;

            _shotTimer = BalanceConfig.Instance.OrcTowerIntervalSeconds;
            target.TakeDamage(_shotDamage);
        }

        private SoldierUnit NearestSoldierWithin(float range)
        {
            SoldierUnit nearest = null;
            var best = range * range;
            foreach (var soldier in SoldierUnit.All)
            {
                if (soldier == null || soldier.CurrentHealth <= 0) continue;
                var offset = soldier.transform.position - transform.position;
                offset.y = 0f;
                var distSq = offset.sqrMagnitude;
                if (distSq > best) continue;
                best = distSq;
                nearest = soldier;
            }
            return nearest;
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || CurrentHealth <= 0) return;

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            if (CurrentHealth > 0) return;

            Fall(announce: true);
        }

        /// <summary>Falls without a word in the event feed -- a save putting back what the player had already broken.</summary>
        public void FallQuietly()
        {
            if (CurrentHealth <= 0 && !_all.Contains(this)) return;
            CurrentHealth = 0;
            Fall(announce: false);
        }

        /// <summary>How many townsfolk a cage of this level holds, from the sheet.</summary>
        public static int PrisonersIn(int level, BalanceConfig balance) =>
            level >= OrcBaseLayout.MainLevel ? balance.OrcMainCagePrisoners : balance.OrcCagePrisonersAt(level);

        private void Fall(bool announce)
        {
            _all.Remove(this);
            // Only a cage the player breaks lets anyone out: one a save puts back broken was
            // emptied back then, and its prisoners came back with the save on their own.
            if (announce && Kind == OrcCampBuildingKind.Cage) ReleasePrisoners();
            // the same for the treasury: one a save puts back broken was paid out back then
            if (announce && Kind == OrcCampBuildingKind.Treasury) PayOutTreasury();
            var ruin = SpawnRuin(_ruinModel, Cell, transform.position, transform.rotation);
            if (Camp != null)
            {
                Camp.AdoptRuin(ruin);
                Camp.NotifyFallen(this, announce);
            }
            OnDestroyed?.Invoke(this);
            Destroy(gameObject);
        }

        private void PayOutTreasury()
        {
            var balance = BalanceConfig.Instance;
            var resources = CityBuilder.Resources.ResourceManager.Instance;
            if (resources == null) return;
            // what the storehouses had room for -- what the feed reports is what the player got
            var coins = resources.Add(CityBuilder.Resources.ResourceType.Coins, balance.OrcTreasuryLootCoins);
            var iron = resources.Add(CityBuilder.Resources.ResourceType.IronBar, balance.OrcTreasuryLootIronBars);
            var copper = resources.Add(CityBuilder.Resources.ResourceType.CopperBar, balance.OrcTreasuryLootCopperBars);
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Format("#log_treasury_plundered", coins, iron, copper),
                EventSeverity.Good, EventGlyph.Star, null, transform.position));
        }

        private void ReleasePrisoners()
        {
            var count = PrisonersIn(Level, BalanceConfig.Instance);
            if (count <= 0) return;
            // out of the door, which is the model's front (-Z), a little apart from each other
            var door = transform.position - transform.forward * (Footprint.y * 0.5f + 0.4f);
            for (var i = 0; i < count; i++)
            {
                var side = (i - (count - 1) * 0.5f) * 0.35f;
                FreedPrisoner.Spawn(door + transform.right * side);
            }
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Format("#log_prisoners_freed", count),
                EventSeverity.Good, EventGlyph.Happy, null, door));
        }

        /// <summary>
        /// Takes the building away without a fight -- its camp is being rebuilt at a new level. No
        /// ruin, no event: nothing was broken.
        /// </summary>
        public void Dismantle()
        {
            _all.Remove(this);
            CurrentHealth = 0;
            Destroy(gameObject);
        }

        /// <summary>The fallen building's ruin, on its plot. Scenery only: no collider, nothing to attack.</summary>
        public static GameObject SpawnRuin(GameObject ruinModel, Vector2Int cell, Vector3 position, Quaternion rotation)
        {
            if (ruinModel == null) return null;
            var ruin = Instantiate(ruinModel, position, rotation);
            ruin.name = $"OrcCampRuin {cell.x},{cell.y}";
            return ruin;
        }
    }
}
