using CityBuilder.Core;
using System;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Citizens
{
    /// <summary>
    /// Attached to placed buildings that have worker slots (BuildingData.maxWorkers > 0).
    /// Ticks resource production while workers are assigned to it.
    /// </summary>
    public class ProductionBuilding : MonoBehaviour
    {
        // Above BuildingInstance.DecayPenaltyThreshold, output falls linearly from 100% down to
        // this floor at full (1.0) decay -- a decrepit building still limps along instead of
        // dropping straight to zero the moment it crosses the threshold.
        // From the balance sheet (min_decay_production_multiplier).

        private BuildingData _data;
        private BuildingInstance _buildingInstance;
        private float _timer;

        public int AssignedWorkers { get; private set; }

        /// <summary>Fired whenever AssignedWorkers changes, so CitizenVisualsManager can keep visible workers in sync.</summary>
        public event Action OnAssignedWorkersChanged;

        // BuildingInstance.Initialize() runs a line after Instantiate() returns, which is after
        // this component's own Awake() already ran — so BuildingData isn't available yet at
        // Awake time. Resolving it lazily on first access sidesteps that ordering entirely.
        private BuildingData Data => _data != null ? _data : (_data = (_buildingInstance = GetComponent<BuildingInstance>())?.Data);

        /// <summary>Worker slots at this building's current upgrade level -- an upgraded workshop can take on more hands the moment it finishes.</summary>
        public int MaxWorkers => Data != null ? Data.LevelStats(CurrentLevel).maxWorkers : 0;

        private int CurrentLevel => _buildingInstance != null ? _buildingInstance.Level : 1;
        public string DisplayName => Data != null ? Data.LocalizedName : "?";

        /// <summary>Everything this building knows how to make. One entry for almost everything; the Плавильня is why this is a list.</summary>
        public IReadOnlyList<BuildingRecipe> Recipes => Data != null ? Data.recipes : EmptyRecipes;

        /// <summary>Which of them it is working on. Clamped on read so a building whose recipes changed under it (a rebuilt project, a loaded save) can never point past the end of the list.</summary>
        public int SelectedRecipeIndex
        {
            get => Recipes.Count == 0 ? 0 : Mathf.Clamp(_selectedRecipeIndex, 0, Recipes.Count - 1);
            private set => _selectedRecipeIndex = value;
        }

        public BuildingRecipe SelectedRecipe => Recipes.Count == 0 ? null : Recipes[SelectedRecipeIndex];

        /// <summary>Whether the player is offered a choice at all -- one recipe is not a decision, and a card with a single button on it is just noise.</summary>
        public bool HasRecipeChoice => Recipes.Count > 1;

        /// <summary>
        /// What the selected recipe makes. Wood or Stone here is what sends a worker walking out to
        /// a tree or a rock rather than staying on the plot (see CitizenVisualsManager) -- it is
        /// the one place the resource itself still steers behaviour.
        /// </summary>
        public ResourceType ProducesResource => SelectedRecipe != null ? SelectedRecipe.output : ResourceType.Wood;

        /// <summary>Whether this building produces anything at all -- the Пристань and the Водяная мельница deliberately do not.</summary>
        public bool ProducesAnything => Recipes.Count > 0;

        /// <summary>Batches of the selected recipe one worker gets through per tick, at this building's current level.</summary>
        public int BatchesPerWorkerPerTick => Data != null ? Data.LevelStats(CurrentLevel).batchesPerWorkerPerTick : 0;

        /// <summary>Raised when the player switches what this building is making, so the cards showing it can catch up.</summary>
        public event Action OnRecipeChanged;

        private int _selectedRecipeIndex;
        private static readonly BuildingRecipe[] EmptyRecipes = new BuildingRecipe[0];

        /// <summary>
        /// Switches what this building makes. The part-finished tick is dropped rather than carried
        /// over: it was measured in the old recipe's batches, and paying it out in the new one's
        /// output would be a free ingot for anyone who kept toggling.
        /// </summary>
        public void SelectRecipe(int index)
        {
            if (Recipes.Count == 0 || index < 0 || index >= Recipes.Count) return;
            if (index == SelectedRecipeIndex) return;

            SelectedRecipeIndex = index;
            _timer = 0f;
            _reportedMissingInput = false;
            _reportedOverflow = false;
            OnRecipeChanged?.Invoke();
        }

        /// <summary>Used by save/load to restore the chosen recipe by its stable id -- an index would silently mean a different metal the moment the sheet's row order changed.</summary>
        public void SelectRecipeById(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId)) return;

            for (var i = 0; i < Recipes.Count; i++)
            {
                if (Recipes[i] != null && Recipes[i].id == recipeId)
                {
                    SelectedRecipeIndex = i;
                    return;
                }
            }
        }

        private void Start()
        {
            CitizenVisualsManager.Instance?.RegisterProductionBuilding(this);
        }

        private void OnDestroy()
        {
            CitizenVisualsManager.Instance?.UnregisterProductionBuilding(this);
        }

        private void Update()
        {
            var data = Data;
            if (data == null || AssignedWorkers <= 0 || data.productionIntervalSeconds <= 0f) return;

            _timer += Time.deltaTime;
            if (_timer < data.productionIntervalSeconds) return;
            _timer -= data.productionIntervalSeconds;

            var recipe = SelectedRecipe;
            if (recipe == null) return;

            var resources = ResourceManager.Instance;
            var wanted = AssignedWorkers * data.LevelStats(CurrentLevel).batchesPerWorkerPerTick;
            if (wanted <= 0) return;

            // A workshop runs as many batches as it has material for: a mill with three hands and
            // grain for two mills two sacks' worth this tick, not none.
            var batches = AffordableBatches(wanted, recipe.inputs, StocksFor(recipe, resources));

            if (recipe.HasInputs && !ReportMissingInput(recipe, batches <= 0)) return;

            var amount = Mathf.RoundToInt(batches * recipe.outputAmount * DecayProductionMultiplier());
            if (amount <= 0) return;

            // Nothing is smelted into a store that cannot hold it. A gatherer may waste its output
            // -- there is nothing to lose but the tick -- but a converter would be burning ore to
            // make metal that falls on the floor, which is a bug the player could never see.
            if (recipe.HasInputs && resources != null && resources.GetAmount(recipe.output) >= resources.GetCapacity(recipe.output))
            {
                ReportOverflow(data, amount);
                return;
            }

            if (resources != null)
            {
                foreach (var input in recipe.inputs)
                {
                    resources.Add(input.type, -(batches * input.amount));
                }
            }

            var stored = resources != null ? resources.Add(recipe.output, amount) : amount;
            ReportOverflow(data, amount - stored);
        }

        /// <summary>What the settlement holds of each of this recipe's inputs, in the recipe's own order -- the shape AffordableBatches wants.</summary>
        private static int[] StocksFor(BuildingRecipe recipe, ResourceManager resources)
        {
            var stocks = new int[recipe.inputs.Count];
            for (var i = 0; i < stocks.Length; i++)
            {
                stocks[i] = resources != null ? resources.GetAmount(recipe.inputs[i].type) : 0;
            }
            return stocks;
        }

        /// <summary>
        /// How many batches there is material for, out of the batches the workers could manage.
        /// Every ingredient limits it, so the scarcest one decides -- a furnace with ore for ten
        /// ingots and coal for two smelts two.
        ///
        /// Pure so an EditMode test can pin it down without a scene: "all of them", "some of them"
        /// and "none" are the interesting answers, and the middle one is the easy one to get wrong.
        /// </summary>
        public static int AffordableBatches(int wantedBatches, IReadOnlyList<ResourceAmount> inputs, IReadOnlyList<int> stocks)
        {
            if (wantedBatches <= 0) return 0;
            // A gatherer names no ingredients and is limited only by its workers.
            if (inputs == null || inputs.Count == 0) return wantedBatches;

            var affordable = wantedBatches;
            for (var i = 0; i < inputs.Count && i < stocks.Count; i++)
            {
                if (inputs[i].amount <= 0) continue;
                affordable = Mathf.Min(affordable, stocks[i] / inputs[i].amount);
            }

            return Mathf.Max(0, affordable);
        }

        /// <summary>
        /// Says once that a converter is standing idle for want of raw material, and stays quiet
        /// until it runs again -- same latch as ReportOverflow, and for the same reason: a bakery
        /// with no flour ticks every few seconds forever. Returns whether the building may work.
        /// </summary>
        private bool ReportMissingInput(BuildingRecipe recipe, bool starved)
        {
            if (!starved)
            {
                _reportedMissingInput = false;
                return true;
            }

            if (!_reportedMissingInput)
            {
                _reportedMissingInput = true;
                EventLogManager.Instance?.Log(Localization.Format("#log_no_input", DisplayName, DescribeInputs(recipe)));
            }
            return false;
        }

        private bool _reportedMissingInput;

        /// <summary>Just the ingredient names, for the log line -- the amounts belong on the building's card, not in a running commentary.</summary>
        private static string DescribeInputs(BuildingRecipe recipe)
        {
            var names = new System.Text.StringBuilder();
            foreach (var input in recipe.inputs)
            {
                if (names.Length > 0) names.Append(", ");
                names.Append(ResourceNames.Of(input.type));
            }
            return names.ToString();
        }

        /// <summary>
        /// Says once that this building's output is going to waste for want of storage, and stays
        /// quiet until it has somewhere to put things again. Without the latch a full granary would
        /// write a line to the event log every few seconds for every farm in the settlement.
        /// </summary>
        private void ReportOverflow(BuildingData data, int wasted)
        {
            if (wasted <= 0)
            {
                _reportedOverflow = false;
                return;
            }

            if (_reportedOverflow) return;
            _reportedOverflow = true;
            EventLogManager.Instance?.Log(Localization.Format("#log_no_storage", data.LocalizedName));
        }

        private bool _reportedOverflow;

        /// <summary>1 below BuildingInstance.DecayPenaltyThreshold, falling linearly to MinDecayProductionMultiplier at full decay -- a neglected building still produces something, just less, right up until it's destroyed outright.</summary>
        private float DecayProductionMultiplier()
        {
            var decay = _buildingInstance != null ? _buildingInstance.Decay : 0f;
            return ComputeDecayProductionMultiplier(decay);
        }

        /// <summary>Pure formula extracted from DecayProductionMultiplier so it's covered by an EditMode test without needing a live BuildingInstance.</summary>
        public static float ComputeDecayProductionMultiplier(float decay)
        {
            if (decay <= BuildingInstance.DecayPenaltyThreshold) return 1f;

            var t = (decay - BuildingInstance.DecayPenaltyThreshold) / (1f - BuildingInstance.DecayPenaltyThreshold);
            return Mathf.Lerp(1f, BalanceConfig.Instance.MinDecayProductionMultiplier, t);
        }

        public bool TryAssignWorker()
        {
            if (AssignedWorkers >= MaxWorkers) return false;
            if (CitizenManager.Instance == null || !CitizenManager.Instance.NotifyWorkerAssigned()) return false;
            AssignedWorkers++;
            OnAssignedWorkersChanged?.Invoke();
            return true;
        }

        public bool TryUnassignWorker()
        {
            if (AssignedWorkers <= 0) return false;
            AssignedWorkers--;
            CitizenManager.Instance?.NotifyWorkerUnassigned();
            OnAssignedWorkersChanged?.Invoke();
            return true;
        }

        /// <summary>Used by save/load to restore a worker count directly (already-valid state).</summary>
        public void SetAssignedWorkers(int count)
        {
            AssignedWorkers = Mathf.Clamp(count, 0, MaxWorkers);
            CitizenManager.Instance?.NotifyWorkersAssignedBulk(AssignedWorkers);
            OnAssignedWorkersChanged?.Invoke();
        }
    }
}
