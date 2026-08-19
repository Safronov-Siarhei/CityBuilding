using System;
using System.Collections.Generic;
using CityBuilder.Citizens;
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

        /// <summary>Current hit points; starts at this building's maximum for its level. Reduced by TryDamage (see CityBuilder.Combat.OrcUnit) -- no other damage source exists yet.</summary>
        public int CurrentHealth { get; private set; }

        /// <summary>Hit points at full repair, for the level this building currently stands at.</summary>
        public int MaxHealth => Data != null ? Data.LevelStats(Level).maxHealth : 0;

        /// <summary>Attack damage against nearby raiders (DefensiveBuilding) and the settlement's defence score (HappinessManager), at the current level.</summary>
        public int Defense => Data != null ? Data.LevelStats(Level).defense : 0;

        /// <summary>How far this building serves, at the level it stands at -- see BuildingLevelStats.serviceRadius. Zero for everything that serves nothing.</summary>
        public int ServiceRadius => Data != null ? Data.LevelStats(Level).serviceRadius : 0;

        /// <summary>What this building adds to the settlement's mood, at the level it stands at. Zero for everything that is not entertainment.</summary>
        public int Happiness => Data != null ? Data.LevelStats(Level).happiness : 0;

        /// <summary>How far this building's workers may roam for a tree or a boulder at the level it currently stands at. Zero for anything that does not gather.</summary>
        public int HarvestRadius => Data != null ? Data.LevelStats(Level).harvestRadius : 0;

        /// <summary>How many people this building houses at the level it currently stands at -- handed to CitizenManager as room, not as people.</summary>
        public int HousingCapacity => Data != null ? Data.LevelStats(Level).housingCapacity : 0;

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
        private BuildingLevelAppearance _levelAppearance;

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
            _levelAppearance = GetComponent<BuildingLevelAppearance>();
            RotationSteps = ((rotationSteps % 4) + 4) % 4;
            CurrentHealth = data != null ? data.LevelStats(Level).maxHealth : 0;
            Decay = 0f;

            // The single hook point for every building instantiation (fresh placement AND
            // save/load), so permanent fog reveal always matches the buildings actually present
            // without needing any separate persistence of its own.
            if (data != null)
            {
                // The ROTATED footprint, or a building placed sideways reveals the fog around a
                // point off to one side of itself -- invisible on a square footprint, half a
                // building out on a 4x2 one.
                var footprint = RotatedFootprint();
                var centerCell = originCell + new Vector2Int(footprint.x / 2, footprint.y / 2);
                FogOfWarManager.Instance?.RevealPermanent(centerCell, data.fogRevealRadius);

                SetupNavMeshObstacle(data);
                // The mirror image of the obstacle above: a Bridge doesn't carve the NavMesh, it
                // ADDS to it, which is the only way water ever becomes crossable (see
                // MeshMapApplier.RegisterWalkableSurface).
                if (data.providesWalkableSurface) MeshMapApplier.Instance?.RegisterWalkableSurface(this);
                if (data.connectsToFences) RegisterFenceCells();
                ChangeStoredCapacity(Data.LevelStats(Level).storageCapacity);
                ChangeHousingCapacity(Data.LevelStats(Level).housingCapacity);
                ChangeCount(data.buildingName, 1);
            }
        }

        /// <summary>
        /// Every cell this building covers joins the fence line, so a two-cell gate connects on
        /// both of its tiles rather than only where its origin happens to sit. Registering the
        /// segment's own FenceAppearance (null for anything that just connects) is what lets
        /// FenceNetwork re-shape it when a neighbour appears or is destroyed.
        /// </summary>
        private void RegisterFenceCells()
        {
            var network = FenceNetwork.Instance;
            if (network == null) return;

            var appearance = GetComponent<FenceAppearance>();
            foreach (var cell in OccupiedCells()) network.Register(cell, appearance);
        }

        /// <summary>The cells this building sits on, with the footprint turned to match its placement rotation -- the same swap BuildingPlacer applies before it reserves them.</summary>
        private IEnumerable<Vector2Int> OccupiedCells()
        {
            var footprint = Data.footprintSize;
            if (RotationSteps % 2 != 0) footprint = new Vector2Int(footprint.y, footprint.x);

            for (var x = 0; x < footprint.x; x++)
            {
                for (var z = 0; z < footprint.y; z++)
                {
                    yield return OriginCell + new Vector2Int(x, z);
                }
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
                ChangeStoredCapacity(-Data.LevelStats(Level).storageCapacity);
                ChangeHousingCapacity(-Data.LevelStats(Level).housingCapacity);
                if (Data.connectsToFences && FenceNetwork.Instance != null)
                {
                    // Leaves a real gap: the segments either side re-shape into dead ends, which is
                    // the whole point of a fence an enemy can break through.
                    foreach (var cell in OccupiedCells()) FenceNetwork.Instance.Unregister(cell);
                }
                ChangeCount(Data.buildingName, -1);
            }
        }

        /// <summary>
        /// Hands this building's storage room to the settlement, or takes it back. Paired with the
        /// registration in Initialize and the release in OnDestroy so the ceiling always matches the
        /// storehouses actually standing -- burning down a granary really does spill the surplus.
        /// </summary>
        private void ChangeStoredCapacity(int delta)
        {
            if (delta == 0 || Data == null || Data.storageGroup == ResourceStorageGroup.None) return;
            ResourceManager.Instance?.AddCapacity(Data.storageGroup, delta);
        }

        /// <summary>
        /// Hands this building's housing to the settlement, or takes it back -- the exact mirror of
        /// ChangeStoredCapacity above, paired the same way with Initialize, OnDestroy, SetLevel and
        /// TryUpgrade. Room only: whether anyone comes to live in it is MigrationManager's business,
        /// and losing the house does not evict the people already counted.
        /// </summary>
        private void ChangeHousingCapacity(int delta)
        {
            if (delta == 0) return;
            CitizenManager.Instance?.ChangeCapacity(delta);
        }

        private static void ChangeCount(string buildingName, int delta)
        {
            _countByName.TryGetValue(buildingName, out var count);
            _countByName[buildingName] = Mathf.Max(0, count + delta);
        }

        /// <summary>Also read by HappinessManager to decide which instances count toward the settlement's decay-based happiness score -- roads/Town Hall never decay, so including them would just dilute the average with artificial zeros.</summary>
        public bool DecaysOverTime => Data != null && !Data.isRoad && Data.buildingName != BuildingIds.TownHall;

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
            EventLogManager.Instance?.Log(Localization.Format("#log_destroyed_decay", Data.LocalizedName));
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

            EventLogManager.Instance?.Log(Localization.Format("#log_destroyed_combat", Data.LocalizedName));
            OnDestroyedInCombat?.Invoke(this);
            FreeCellsAndDestroy();
        }

        /// <summary>Created on first damage rather than up front -- most buildings are never attacked at all, and this way they carry no extra objects or per-frame work for a bar nobody will see.</summary>
        private void ShowHealthBar()
        {
            if (Data == null || MaxHealth <= 0) return;

            if (_healthBar == null) _healthBar = BuildingHealthBar.CreateFor(transform);
            _healthBar.Report(CurrentHealth / (float)MaxHealth);
        }

        private void FreeCellsAndDestroy()
        {
            if (GridManager.Instance != null) GridManager.Instance.SetAreaOccupied(OriginCell, RotatedFootprint(), false);
            Destroy(gameObject);
        }

        /// <summary>
        /// This building's footprint as it actually lies on the grid, with X and Z swapped when it
        /// was placed on its side. Anything that asks the grid where this building IS -- its cells,
        /// its centre, the point a worker walks to -- has to use this and not Data.footprintSize:
        /// the two are the same number only for a square building, and a 4x2 turned 90 degrees ends
        /// up a whole building away from where the unrotated one says it is.
        /// </summary>
        public Vector2Int RotatedFootprint()
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
            // Initialize already handed over level 1's storage room, so a building restored at a
            // higher level owes the settlement the difference -- otherwise a loaded save would
            // quietly hold less than the same town did before it was saved.
            var before = Data != null ? Data.LevelStats(Level).storageCapacity : 0;
            var housedBefore = Data != null ? Data.LevelStats(Level).housingCapacity : 0;
            Level = Mathf.Clamp(level, 1, MaxLevel);
            if (Data != null)
            {
                ChangeStoredCapacity(Data.LevelStats(Level).storageCapacity - before);
                ChangeHousingCapacity(Data.LevelStats(Level).housingCapacity - housedBefore);
            }

            // A loaded building has to look its level straight away -- nothing else will tell it to,
            // since it was never upgraded during this session.
            _levelAppearance?.Apply(Level);
        }

        /// <summary>Used by save/load to restore already-valid runtime condition directly.</summary>
        public void SetCondition(int health, float decay)
        {
            CurrentHealth = Data != null ? Mathf.Clamp(health, 0, MaxHealth) : health;
            Decay = Mathf.Clamp01(decay);
            UpdateDecayWarningMarker();
        }

        /// <summary>Null once already at MaxLevel.</summary>
        public List<ResourceAmount> GetUpgradeCost()
        {
            if (Data == null || Level >= MaxLevel) return null;
            return Level == 1 ? Data.upgradeToLevel2Cost : Data.upgradeToLevel3Cost;
        }

        /// <summary>
        /// Whether the Laboratory has permitted the next level yet. True at max level as well --
        /// there is nothing left to gate there, and "is there a next level at all" is what
        /// GetUpgradeCost answers. True with no ResearchManager in the scene, so a scene assembled
        /// without one (an EditMode fixture) is not silently un-upgradeable.
        /// </summary>
        public bool NextLevelResearched
        {
            get
            {
                if (Data == null || Level >= MaxLevel) return true;

                var research = Research.ResearchManager.Instance;
                return research == null || research.IsBuildingLevelResearched(Data.buildingName, Level + 1);
            }
        }

        public bool TryUpgrade()
        {
            // Before the cost, so a player who cannot upgrade yet is never charged for finding out.
            if (!NextLevelResearched) return false;

            var cost = GetUpgradeCost();
            if (cost == null || ResourceManager.Instance == null || !ResourceManager.Instance.TrySpend(cost)) return false;

            var before = Data.LevelStats(Level);
            Level++;
            var after = Data.LevelStats(Level);

            // Carry damage across rather than healing the building for free: a wall upgraded while
            // a raid chews on it gains the extra hit points, it doesn't forget the fight.
            CurrentHealth = Mathf.Clamp(CurrentHealth + (after.maxHealth - before.maxHealth), 1, after.maxHealth);
            if (_healthBar != null) _healthBar.Report(CurrentHealth / (float)after.maxHealth);

            // A roomier house is room, not people: the settlement can now hold more, and whether
            // anyone comes to fill it is migration's business (see MigrationManager).
            ChangeHousingCapacity(after.housingCapacity - before.housingCapacity);

            ChangeStoredCapacity(after.storageCapacity - before.storageCapacity);

            _levelAppearance?.Apply(Level);
            return true;
        }
    }
}
