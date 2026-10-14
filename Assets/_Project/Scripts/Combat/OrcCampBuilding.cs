using System;
using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>What a camp building is for (the user's design, 2026-10-14).</summary>
    public enum OrcCampBuildingKind
    {
        /// <summary>Shoots the player's soldiers that come within range.</summary>
        Watchtower = 0,
        /// <summary>Houses the camp's garrison and keeps refilling it while it stands.</summary>
        WarTent = 1,
        /// <summary>The portal's shield: while it stands, the portal takes no damage.</summary>
        Totem = 2,
    }

    /// <summary>
    /// One building of an orc camp (see OrcCamp): a watchtower, the war tent or the totem. The
    /// player's army attacks it like a portal -- it is an IDamageTarget structure -- and once it
    /// falls it leaves its ruin and tells its camp, which is how the totem's fall drops the
    /// portal's shield and the tent's stops the garrison coming back.
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
        public OrcCamp Camp { get; private set; }

        /// <summary>The grid cell (lower-left of the plot) it stands on -- what the save writes.</summary>
        public Vector2Int Cell { get; private set; }

        public int MaxHealth { get; private set; }
        public int CurrentHealth { get; private set; }

        private GameObject _ruinModel;
        private float _shotTimer;

        Transform IDamageTarget.Transform => this != null ? transform : null;
        bool IDamageTarget.IsAlive => this != null && CurrentHealth > 0;
        bool IDamageTarget.IsStructure => true;

        /// <summary>Called by OrcRaidManager right after building it.</summary>
        public void Initialize(OrcCampBuildingKind kind, OrcCamp camp, Vector2Int cell, GameObject ruinModel)
        {
            Kind = kind;
            Camp = camp;
            Cell = cell;
            _ruinModel = ruinModel;

            var balance = BalanceConfig.Instance;
            MaxHealth = kind switch
            {
                OrcCampBuildingKind.Watchtower => balance.OrcTowerHealth,
                OrcCampBuildingKind.WarTent => balance.OrcTentHealth,
                _ => balance.OrcTotemHealth,
            };
            CurrentHealth = MaxHealth;
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
            if (Kind != OrcCampBuildingKind.Watchtower || CurrentHealth <= 0) return;

            _shotTimer -= Time.deltaTime;
            if (_shotTimer > 0f) return;

            var balance = BalanceConfig.Instance;
            var target = NearestSoldierWithin(balance.OrcTowerRangeMeters);
            if (target == null) return;

            _shotTimer = balance.OrcTowerIntervalSeconds;
            target.TakeDamage(balance.OrcTowerDamage);
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

            _all.Remove(this);
            SpawnRuin(_ruinModel, Cell, transform.position, transform.rotation);
            OnDestroyed?.Invoke(this);
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
