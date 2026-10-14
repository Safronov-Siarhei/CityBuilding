using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// The orc camp around one portal (the user's design, 2026-10-14): a camp WITHOUT walls, of a
    /// watchtower that shoots the player's soldiers, a war tent that keeps a garrison standing
    /// guard and refills it while the tent stands, and a totem that SHIELDS the portal -- while the
    /// totem stands the portal takes no damage, so a camp has to be broken before its portal can.
    ///
    /// The camp outlives its portal: closing the portal does not call off the tower or the guards.
    /// It is keyed by the portal's cell, which is what the save uses to put buildings back in the
    /// right camp.
    /// </summary>
    public class OrcCamp : MonoBehaviour
    {
        private static readonly List<OrcCamp> _all = new List<OrcCamp>();

        /// <summary>Every camp on the map, closed portal or not.</summary>
        public static IReadOnlyList<OrcCamp> All => _all;

        private readonly List<OrcCampBuilding> _buildings = new List<OrcCampBuilding>();
        private readonly List<OrcUnit> _guards = new List<OrcUnit>();
        private readonly List<FallenBuilding> _fallen = new List<FallenBuilding>();

        /// <summary>A camp building the player has broken: what it was, where, which way it faced -- all the save needs to put its ruin back.</summary>
        public readonly struct FallenBuilding
        {
            public FallenBuilding(OrcCampBuildingKind kind, Vector2Int cell, float yaw)
            {
                Kind = kind;
                Cell = cell;
                Yaw = yaw;
            }

            public OrcCampBuildingKind Kind { get; }
            public Vector2Int Cell { get; }
            public float Yaw { get; }
        }

        /// <summary>The camp's broken buildings, whose ruins stand where they fell.</summary>
        public IReadOnlyList<FallenBuilding> Fallen => _fallen;

        /// <summary>Records a ruin restored from a save.</summary>
        public void AddFallen(FallenBuilding fallen) => _fallen.Add(fallen);
        private float _refillTimer;

        /// <summary>The cell of the portal this camp was built around -- its key in the save.</summary>
        public Vector2Int PortalCell { get; private set; }

        /// <summary>The portal it guards; null once the player has closed it.</summary>
        public OrcPortal Portal { get; private set; }

        /// <summary>The camp's buildings still standing.</summary>
        public IReadOnlyList<OrcCampBuilding> Buildings => _buildings;

        /// <summary>The garrison still alive.</summary>
        public IReadOnlyList<OrcUnit> Guards => _guards;

        public bool TotemStanding => Has(OrcCampBuildingKind.Totem);
        public bool TentStanding => Has(OrcCampBuildingKind.WarTent);

        public void Initialize(Vector2Int portalCell, OrcPortal portal)
        {
            PortalCell = portalCell;
            Portal = portal;
        }

        public void Add(OrcCampBuilding building)
        {
            if (building != null) _buildings.Add(building);
            UpdateShield();
        }

        private void OnEnable()
        {
            _all.Add(this);
            OrcCampBuilding.OnDestroyed += HandleBuildingDestroyed;
        }

        private void OnDisable()
        {
            _all.Remove(this);
            OrcCampBuilding.OnDestroyed -= HandleBuildingDestroyed;
        }

        /// <summary>Brings the garrison up to strength at once -- a fresh camp, or one just loaded (the guards are not saved; the tent musters them again).</summary>
        public void FillGarrison()
        {
            if (!TentStanding) return;
            while (_guards.Count < BalanceConfig.Instance.OrcGarrisonSize)
            {
                if (!SpawnGuard()) break;
            }
            _refillTimer = BalanceConfig.Instance.OrcGarrisonRespawnSeconds;
        }

        private void Update()
        {
            _guards.RemoveAll(g => g == null || g.CurrentHealth <= 0);
            if (Portal == null || Portal.CurrentHealth <= 0) Portal = null;

            if (!TentStanding || _guards.Count >= BalanceConfig.Instance.OrcGarrisonSize) return;

            // One guard at a time, on the tent's own clock -- a camp the player is fighting refills
            // slowly enough to be worn down, which is what makes breaking the tent worth it.
            _refillTimer -= Time.deltaTime;
            if (_refillTimer > 0f) return;
            _refillTimer = BalanceConfig.Instance.OrcGarrisonRespawnSeconds;
            SpawnGuard();
        }

        private bool SpawnGuard()
        {
            var raids = OrcRaidManager.Instance;
            var tent = Find(OrcCampBuildingKind.WarTent);
            if (raids == null || tent == null) return false;

            // Out of the tent's door, at the strength the raids have grown to.
            var door = tent.transform.position - tent.transform.forward * 1.3f;
            var guard = raids.SpawnGuard(door, OrcRaidManager.ComputeOrcLevel(PlayerProgression.Score()), this);
            if (guard == null) return false;
            _guards.Add(guard);
            return true;
        }

        private void HandleBuildingDestroyed(OrcCampBuilding building)
        {
            if (!_buildings.Remove(building)) return;
            _fallen.Add(new FallenBuilding(building.Kind, building.Cell, building.transform.eulerAngles.y));
            UpdateShield();

            if (building.Kind == OrcCampBuildingKind.Totem && Portal != null)
            {
                EventLogManager.Instance?.Post(GameEvent.At(Localization.Get("#log_totem_destroyed"),
                    EventSeverity.Good, EventGlyph.Portal, null, Portal.transform.position));
            }
        }

        private void UpdateShield()
        {
            if (Portal != null) Portal.IsShielded = TotemStanding;
        }

        private bool Has(OrcCampBuildingKind kind) => Find(kind) != null;

        private OrcCampBuilding Find(OrcCampBuildingKind kind)
        {
            foreach (var building in _buildings)
            {
                if (building != null && building.CurrentHealth > 0 && building.Kind == kind) return building;
            }
            return null;
        }

        /// <summary>Where the guards hold: the camp's middle, the portal's plot.</summary>
        public Vector3 GuardPost => transform.position;
    }
}
