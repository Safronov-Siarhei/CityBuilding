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
            if (kind == OrcCampBuildingKind.Watchtower)
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

        private void Fall(bool announce)
        {
            _all.Remove(this);
            var ruin = SpawnRuin(_ruinModel, Cell, transform.position, transform.rotation);
            if (Camp != null)
            {
                Camp.AdoptRuin(ruin);
                Camp.NotifyFallen(this, announce);
            }
            OnDestroyed?.Invoke(this);
            Destroy(gameObject);
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
