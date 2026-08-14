using System;
using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Buildings
{
    public class BuildingInstance : MonoBehaviour
    {
        public const int MaxLevel = 3;

        // Above this fraction, ProductionBuilding scales output down (see its
        // DecayProductionMultiplier) -- exposed here since decay itself is owned by this class.
        public static float DecayPenaltyThreshold => BalanceConfig.Instance.DecayPenaltyThreshold;

        // First-pass pace, tunable: divided by Level, so a level-3 building decays a third as
        // fast as the same building fresh at level 1 -- upgrading doubles as upkeep investment,
        // not just a stat/production bump.
        // Both from the balance sheet's economy tab (decay_per_day_at_level1 / repair_cost_fraction).
        private const float DecayMarkerHeight = 3f;

        public BuildingData Data { get; private set; }
        public Vector2Int OriginCell { get; private set; }
        public int Level { get; private set; } = 1;

        /// <summary>0-3, each a 90-degree step around Y -- set at placement (BuildingPlacer.RotateSelection) and restored on load.</summary>
        public int RotationSteps { get; private set; }

        /// <summary>Current hit points; starts at BuildingData.maxHealth. Reduced by TryDamage (see CityBuilder.Combat.OrcUnit) -- no other damage source exists yet.</summary>
        public int CurrentHealth { get; private set; }

        /// <summary>
        /// 0 (new) to 1 (fully dilapidated). Accrues one step per GameCalendar day (see
        /// HandleDayPassed) for every building except roads/bridges (not really "buildings") and
        /// the Town Hall (its destruction is a defeat condition on its own -- decay auto-removing
        /// it here would be a jarring, half-implemented "game over" with no actual game-over
        /// screen behind it yet). Above DecayPenaltyThreshold, ProductionBuilding's output drops;
        /// at 1 the building is destroyed outright (see HandleFullyDecayed) and must be rebuilt.
        /// </summary>
        public float Decay { get; private set; }

        private GameObject _decayWarningMarker;
        private BuildingHealthBar _healthBar;

        // Self-registering count-by-name registry (same pattern as ResourceNode.All) so
        // BuildingPlacer can answer "does at least one X exist yet" for the requiredBuilding
        // check without a FindObjectsByType scan on every placement/ghost update.
        private static readonly Dictionary<string, int> _countByName = new Dictionary<string, int>();

        public static bool HasAny(string buildingName)
        {
            return !string.IsNullOrEmpty(buildingName) && _countByName.TryGetValue(buildingName, out var count) && count > 0;
        }

        /// <summary>Fired for every building lost to combat damage (see TryDamage), including the Town Hall -- CityBuilder.Core.GameOverManager listens for that specific case to trigger defeat. Not fired for decay destruction (HandleFullyDecayed), which the Town Hall is exempt from anyway.</summary>
        public static event Action<BuildingInstance> OnDestroyedInCombat;

        public void Initialize(BuildingData data, Vector2Int originCell, int rotationSteps = 0)
        {
            Data = data;
            OriginCell = originCell;
            RotationSteps = ((rotationSteps % 4) + 4) % 4;
            CurrentHealth = data != null ? data.maxHealth : 0;
            Decay = 0f;

            // The single hook point for every building instantiation (fresh placement AND
            // save/load), so permanent fog reveal always matches the buildings actually present
            // without needing any separate persistence of its own.
            if (data != null)
            {
                var footprint = data.footprintSize;
                var centerCell = originCell + new Vector2Int(footprint.x / 2, footprint.y / 2);
                FogOfWarManager.Instance?.RevealPermanent(centerCell, data.fogRevealRadius);

                SetupNavMeshObstacle(data);
                // The mirror image of the obstacle above: a Bridge doesn't carve the NavMesh, it
                // ADDS to it, which is the only way water ever becomes crossable (see
                // MeshMapApplier.RegisterWalkableSurface).
                if (data.providesWalkableSurface) MeshMapApplier.Instance?.RegisterWalkableSurface(this);
                ChangeCount(data.buildingName, 1);
            }
        }

        private void Start()
        {
            if (GameCalendar.Instance != null) GameCalendar.Instance.OnDayPassed += HandleDayPassed;
        }

        private void OnDestroy()
        {
            if (GameCalendar.Instance != null) GameCalendar.Instance.OnDayPassed -= HandleDayPassed;
            if (Data != null)
            {
                if (Data.providesWalkableSurface) MeshMapApplier.Instance?.UnregisterWalkableSurface(this);
                ChangeCount(Data.buildingName, -1);
            }
        }

        private static void ChangeCount(string buildingName, int delta)
        {
            _countByName.TryGetValue(buildingName, out var count);
            _countByName[buildingName] = Mathf.Max(0, count + delta);
        }

        /// <summary>Also read by HappinessManager to decide which instances count toward the settlement's decay-based happiness score -- roads/Town Hall never decay, so including them would just dilute the average with artificial zeros.</summary>
        public bool DecaysOverTime => Data != null && !Data.isRoad && Data.buildingName != "TownHall";

        private void HandleDayPassed()
        {
            if (!DecaysOverTime) return;

            Decay = ComputeNextDecay(Decay, Level);
            UpdateDecayWarningMarker();

            if (Decay >= 1f) HandleFullyDecayed();
        }

        /// <summary>Pure formula extracted from HandleDayPassed so it's covered by an EditMode test without needing a live scene/GameCalendar.</summary>
        public static float ComputeNextDecay(float currentDecay, int level)
        {
            return Mathf.Clamp01(currentDecay + BalanceConfig.Instance.DecayPerDayAtLevel1 / Mathf.Max(1, level));
        }

        /// <summary>Grid occupancy, NavMesh obstacle carving, and worker release (via ProductionBuilding.OnDestroy -> CitizenVisualsManager) are all cleaned up automatically by destroying the GameObject -- nothing here needs to touch those systems directly.</summary>
        private void HandleFullyDecayed()
        {
            EventLogManager.Instance?.Log($"Разрушено от ветхости: {Data.displayName}");
            FreeCellsAndDestroy();
        }

        /// <summary>
        /// Damage from combat (see CityBuilder.Combat.OrcUnit) -- unlike decay, this applies to
        /// every building including the Town Hall, since its loss is the game's defeat condition
        /// (see CityBuilder.Core.GameOverManager, which listens for OnDestroyedInCombat).
        /// </summary>
        public void TryDamage(int amount)
        {
            if (amount <= 0 || CurrentHealth <= 0) return;

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            ShowHealthBar();
            if (CurrentHealth > 0) return;

            EventLogManager.Instance?.Log($"Разрушено в бою: {Data.displayName}");
            OnDestroyedInCombat?.Invoke(this);
            FreeCellsAndDestroy();
        }

        /// <summary>Created on first damage rather than up front -- most buildings are never attacked at all, and this way they carry no extra objects or per-frame work for a bar nobody will see.</summary>
        private void ShowHealthBar()
        {
            if (Data == null || Data.maxHealth <= 0) return;

            if (_healthBar == null) _healthBar = BuildingHealthBar.CreateFor(transform);
            _healthBar.Report(CurrentHealth / (float)Data.maxHealth);
        }

        private void FreeCellsAndDestroy()
        {
            if (GridManager.Instance != null) GridManager.Instance.SetAreaOccupied(OriginCell, RotatedFootprint(), false);
            Destroy(gameObject);
        }

        private Vector2Int RotatedFootprint()
        {
            var footprint = Data.footprintSize;
            return RotationSteps % 2 == 0 ? footprint : new Vector2Int(footprint.y, footprint.x);
        }

        /// <summary>Null once there's nothing to repair (Decay already 0).</summary>
        public List<ResourceAmount> GetRepairCost()
        {
            if (Data == null || Decay <= 0f) return null;

            var scaled = new List<ResourceAmount>();
            foreach (var amount in Data.cost)
            {
                scaled.Add(new ResourceAmount { type = amount.type, amount = Mathf.Max(1, Mathf.RoundToInt(amount.amount * BalanceConfig.Instance.RepairCostFraction)) });
            }
            return scaled;
        }

        public bool TryRepair()
        {
            var cost = GetRepairCost();
            if (cost == null || ResourceManager.Instance == null || !ResourceManager.Instance.TrySpend(cost)) return false;

            Decay = 0f;
            UpdateDecayWarningMarker();
            return true;
        }

        /// <summary>Small floating marker above the building while its decay is in the production-penalty band (see DecayPenaltyThreshold) -- a HUD-less, at-a-glance "this needs attention" cue without having to open every building's info panel.</summary>
        private void UpdateDecayWarningMarker()
        {
            var shouldShow = Decay > DecayPenaltyThreshold && Decay < 1f;

            if (shouldShow && _decayWarningMarker == null)
            {
                _decayWarningMarker = CreateDecayWarningMarker();
            }
            if (_decayWarningMarker != null) _decayWarningMarker.SetActive(shouldShow);
        }

        private GameObject CreateDecayWarningMarker()
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "DecayWarningMarker";
            Destroy(marker.GetComponent<BoxCollider>());
            marker.transform.SetParent(transform, false);
            marker.transform.localPosition = new Vector3(0f, DecayMarkerHeight, 0f);
            marker.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            marker.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
            marker.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                color = new Color(0.95f, 0.4f, 0.1f)
            };
            return marker;
        }

        /// <summary>
        /// Carves this building's footprint out of the baked NavMesh (see
        /// MeshMapApplier.BuildNavMesh) so CitizenAgent's pathfinding routes around it instead of
        /// walking straight through -- no re-bake needed, NavMeshObstacle carving is dynamic.
        /// Roads/bridges are excluded: they're walkable ground, not an obstacle, even though they
        /// (like every other building) still occupy their cell in GridManager.
        ///
        /// Wrapped in try/catch: this runs from Initialize, which is itself called from
        /// BuildingPlacer.TryPlace *before* it fires OnBuildingPlaced -- an unhandled exception
        /// here would abort TryPlace mid-way and silently skip that event entirely, which is what
        /// grants a placed Town Hall/House's citizens. NavMesh carving is an enhancement, not
        /// something placement correctness should ever depend on.
        /// </summary>
        private void SetupNavMeshObstacle(BuildingData data)
        {
            if (data.isRoad) return;

            try
            {
                var obstacle = GetComponent<NavMeshObstacle>();
                if (obstacle == null) obstacle = gameObject.AddComponent<NavMeshObstacle>();

                var cellSize = GridManager.Instance != null ? GridManager.Instance.CellSize : 1f;
                obstacle.shape = NavMeshObstacleShape.Box;
                // Local box, unrotated -- the transform this component sits on already carries
                // the building's placement rotation, so the box rotates along with it automatically.
                obstacle.size = new Vector3(data.footprintSize.x * cellSize, 2f, data.footprintSize.y * cellSize);
                obstacle.center = new Vector3(0f, 1f, 0f);
                obstacle.carving = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"BuildingInstance.SetupNavMeshObstacle failed for '{data.buildingName}' -- citizens may walk through it instead of around it. {e}");
            }
        }

        /// <summary>Used by save/load to restore an already-valid level directly, bypassing the resource-spend check.</summary>
        public void SetLevel(int level)
        {
            Level = Mathf.Clamp(level, 1, MaxLevel);
        }

        /// <summary>Used by save/load to restore already-valid runtime condition directly.</summary>
        public void SetCondition(int health, float decay)
        {
            CurrentHealth = Data != null ? Mathf.Clamp(health, 0, Data.maxHealth) : health;
            Decay = Mathf.Clamp01(decay);
            UpdateDecayWarningMarker();
        }

        /// <summary>Null once already at MaxLevel.</summary>
        public List<ResourceAmount> GetUpgradeCost()
        {
            if (Data == null || Level >= MaxLevel) return null;
            return Level == 1 ? Data.upgradeToLevel2Cost : Data.upgradeToLevel3Cost;
        }

        public bool TryUpgrade()
        {
            var cost = GetUpgradeCost();
            if (cost == null || ResourceManager.Instance == null || !ResourceManager.Instance.TrySpend(cost)) return false;

            Level++;
            // Visual style / stat scaling per level is a planned future addition (see
            // BuildingData's Upgrades/Condition headers) -- upgrading only advances Level so far.
            return true;
        }
    }
}
