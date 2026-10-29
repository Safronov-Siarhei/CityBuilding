using CityBuilder.Core;
using CityBuilder.Buildings;
using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// Attached (see SetupProject.CreateBuildingData) to every building with a real
    /// BuildingData.defense stat -- currently Wall/Tower/Barracks/Gate. Periodically attacks the
    /// nearest OrcUnit within range, using the building's own defense value as attack damage --
    /// the same stat HappinessManager already reads for the defense-coverage score, so a
    /// higher-defense settlement is both "happier" and literally hits harder.
    /// </summary>
    public class DefensiveBuilding : MonoBehaviour
    {
        // Both come from the balance sheet's economy tab, cached at Awake. The range is in
        // world-space metres, not cells -- GridManager.CellSize is 1f in this project so the two
        // happen to match, but this is deliberately independent of that.
        private float _attackIntervalSeconds;
        private float _attackRangeMeters;

        private BuildingInstance _buildingInstance;
        private float _timer;

        /// <summary>Seconds left frozen (the Ice Shaman, InvasionNewYear): it does not shoot meanwhile.</summary>
        public float FrozenFor { get; private set; }

        private GameObject _ice;

        /// <summary>Freezes the tower for `seconds` (the longer of this and what is left), a block of ice round it.</summary>
        public void Freeze(float seconds)
        {
            if (seconds <= 0f) return;
            FrozenFor = Mathf.Max(FrozenFor, seconds);
            ShowIce(true);
        }

        private void ShowIce(bool shown)
        {
            if (shown && _ice == null)
            {
                _ice = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _ice.name = "Ice";
                Destroy(_ice.GetComponent<Collider>());
                _ice.transform.SetParent(transform, false);
                var size = _buildingInstance != null && _buildingInstance.Data != null ? _buildingInstance.Data.footprintSize : Vector2Int.one;
                _ice.transform.localScale = new Vector3(size.x * 1.05f, 2.4f, size.y * 1.05f);
                _ice.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                var material = new Material(RuntimeShaders.Unlit) { color = new Color(0.65f, 0.88f, 1f, 0.45f) };
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                _ice.GetComponent<Renderer>().sharedMaterial = material;
                _ice.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            if (_ice != null) _ice.SetActive(shown);
        }

        private void Awake()
        {
            _buildingInstance = GetComponent<BuildingInstance>();
            _attackIntervalSeconds = BalanceConfig.Instance.DefenceAttackIntervalSeconds;
            _attackRangeMeters = BalanceConfig.Instance.DefenceAttackRangeMeters;
        }

        private void Update()
        {
            if (_buildingInstance == null || _buildingInstance.Data == null || _buildingInstance.Defense <= 0) return;
            // frozen by the New Year's Ice Shaman: no arrows till it thaws
            if (FrozenFor > 0f)
            {
                FrozenFor -= Time.deltaTime;
                if (FrozenFor <= 0f) ShowIce(false);
                return;
            }
            // not while it is being built or upgraded (ConstructionSite)
            if (!_buildingInstance.IsOperational) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = _attackIntervalSeconds;

            // the Data is set after Awake, so the range is asked here
            _attackRangeMeters = RangeOf(_buildingInstance);

            var target = FindNearestOrcInRange();
            if (target == null)
            {
                // an orc ship in reach of the shore (the archipelago, phase 4b)
                var ship = Ships.ShipManager.Instance != null ? Ships.ShipManager.Instance.NearestShip(transform.position, _attackRangeMeters, orc: true) : null;
                if (ship != null) CombatProjectiles.Fire(ProjectileKind.Arrow, CombatProjectiles.MuzzleOf(this, ship.transform.position), ship, _buildingInstance.Defense);
                return;
            }
            // Seen flying from the parapet and landing (see CombatProjectiles); the damage lands with it.
            CombatProjectiles.Fire(ProjectileKind.Arrow, CombatProjectiles.MuzzleOf(this, target.transform.position), target, _buildingInstance.Defense);
        }

        /// <summary>How far a building shoots: the sheet's defence radius, castle_attack_range_mult further for the main building (the castle, the Town Hall -- the user, 2026-10-22: +50%).</summary>
        public static float RangeOf(BuildingInstance building)
        {
            // the New Year's blizzard shortens every tower's reach (Weather)
            var range = BalanceConfig.Instance.DefenceAttackRangeMeters * Weather.TowerRangeMultiplier;
            if (building != null && building.Data != null && building.Data.buildingName == BuildingIds.TownHall)
                range *= BalanceConfig.Instance.CastleAttackRangeMultiplier;
            return range;
        }

        private OrcUnit FindNearestOrcInRange()
        {
            OrcUnit nearest = null;
            var nearestDistSq = _attackRangeMeters * _attackRangeMeters;

            foreach (var orc in OrcUnit.All)
            {
                if (orc == null) continue;
                var distSq = (orc.transform.position - transform.position).sqrMagnitude;
                if (distSq > nearestDistSq) continue;

                nearestDistSq = distSq;
                nearest = orc;
            }

            return nearest;
        }
    }
}
