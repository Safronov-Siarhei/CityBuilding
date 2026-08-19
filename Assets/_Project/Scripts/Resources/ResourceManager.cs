using System;
using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CityBuilder.Resources
{
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        public event Action<ResourceType, int> OnResourceChanged;
        public event Action<bool> OnInfiniteResourcesChanged;

        /// <summary>Raised when a storehouse is built, upgraded or lost, i.e. when the ceiling on something moved.</summary>
        public event Action OnCapacityChanged;

        /// <summary>
        /// Extra room built by storehouses, on top of the settlement's base capacity. Buildings add
        /// their share on placement and take it back when destroyed (see BuildingInstance), so this
        /// never needs a scan over the map.
        /// </summary>
        private readonly Dictionary<ResourceStorageGroup, int> _builtCapacity = new Dictionary<ResourceStorageGroup, int>();

        [SerializeField] private List<ResourceAmount> startingResources = new List<ResourceAmount>
        {
            new ResourceAmount { type = ResourceType.Wood, amount = 50 },
            new ResourceAmount { type = ResourceType.Stone, amount = 20 },
            new ResourceAmount { type = ResourceType.Food, amount = 30 },
            new ResourceAmount { type = ResourceType.Gold, amount = 100 },
        };

        [Header("Debug")]
        [SerializeField] private Key infiniteResourcesToggleKey = Key.F9;

        private readonly Dictionary<ResourceType, int> _resources = new Dictionary<ResourceType, int>();

        /// <summary>Debug/testing cheat — toggled with <see cref="infiniteResourcesToggleKey"/>, not exposed to the player.</summary>
        public bool InfiniteResources { get; private set; }

        /// <summary>
        /// Everything the settlement has ever made or gathered, in units, and never decreased --
        /// the "total output over time" term of the player's progression score (see
        /// PlayerProgression), which is what raids are now sized and paced against.
        ///
        /// Deliberately NOT fed by Add. Refunds, the treasury and the cheats all go through Add,
        /// and a progression score that a cancelled research could raise is not measuring
        /// progression. Only the three paths that genuinely make something -- a workshop's output,
        /// a gatherer coming home, and the player's own tap on a tree -- call AddProduced.
        /// </summary>
        public int LifetimeProduced { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            foreach (var starting in startingResources)
            {
                _resources[starting.type] = starting.amount;
            }
        }

        private void Update()
        {
            if (ModalGate.IsBlocked) return;

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard[infiniteResourcesToggleKey].wasPressedThisFrame)
            {
                SetInfiniteResources(!InfiniteResources);
            }
        }

        public void SetInfiniteResources(bool enable)
        {
            if (InfiniteResources == enable) return;
            InfiniteResources = enable;
            Debug.Log(enable ? "[Cheat] Infinite resources ENABLED" : "[Cheat] Infinite resources disabled");
            OnInfiniteResourcesChanged?.Invoke(enable);
        }

        public int GetAmount(ResourceType type)
        {
            return _resources.TryGetValue(type, out var amount) ? amount : 0;
        }

        /// <summary>
        /// How much of this resource the settlement can hold: what it starts with plus every
        /// storehouse built for that group. Population has no ceiling -- it isn't warehoused.
        /// </summary>
        public int GetCapacity(ResourceType type)
        {
            var group = ResourceStorage.GroupOf(type);
            if (group == ResourceStorageGroup.None) return int.MaxValue;

            _builtCapacity.TryGetValue(group, out var built);
            return BaseCapacity(group) + built;
        }

        private static int BaseCapacity(ResourceStorageGroup group)
        {
            var config = BalanceConfig.Instance;
            switch (group)
            {
                case ResourceStorageGroup.Food: return config.BaseCapacityFood;
                case ResourceStorageGroup.Valuables: return config.BaseCapacityValuables;
                case ResourceStorageGroup.Grain: return config.BaseCapacityGrain;
                default: return config.BaseCapacityMaterials;
            }
        }

        public void AddCapacity(ResourceStorageGroup group, int amount)
        {
            if (group == ResourceStorageGroup.None || amount == 0) return;

            _builtCapacity.TryGetValue(group, out var current);
            _builtCapacity[group] = Mathf.Max(0, current + amount);
            OnCapacityChanged?.Invoke();

            // Losing a storehouse can leave more in store than there is now room for. Spill the
            // difference rather than carrying an impossible number around: everything that reads a
            // stockpile would otherwise have to know it might be over the limit.
            if (amount < 0) ClampToCapacity(group);
        }

        private void ClampToCapacity(ResourceStorageGroup group)
        {
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                if (ResourceStorage.GroupOf(type) != group) continue;

                var capacity = GetCapacity(type);
                if (GetAmount(type) <= capacity) continue;

                _resources[type] = capacity;
                OnResourceChanged?.Invoke(type, capacity);
            }
        }

        /// <summary>
        /// Adds what fits and returns how much actually went in -- callers that care about the
        /// difference (a producer filling a full granary) can report it. Spending is never blocked
        /// by a ceiling, so negative amounts pass straight through.
        /// </summary>
        public int Add(ResourceType type, int amount)
        {
            if (amount == 0) return 0;

            _resources.TryGetValue(type, out var current);
            var updated = current + amount;

            if (amount > 0)
            {
                var capacity = GetCapacity(type);
                if (updated > capacity) updated = Mathf.Max(current, capacity);
            }

            var stored = updated - current;
            if (stored == 0) return 0;

            _resources[type] = updated;
            OnResourceChanged?.Invoke(type, updated);
            return stored;
        }

        /// <summary>
        /// Add, plus the tally that says the settlement MADE this rather than being handed it.
        ///
        /// Counts what actually fitted, not what was offered: output that overflowed a full
        /// granary went on the floor, and a town whose stores have been full for an hour has not
        /// been getting richer for that hour.
        /// </summary>
        public int AddProduced(ResourceType type, int amount)
        {
            var stored = Add(type, amount);
            if (stored > 0) LifetimeProduced += stored;
            return stored;
        }

        /// <summary>Puts the lifetime tally back from a save. Without it every reload knocked the player's progression to zero and the next raid arrived as though they had just founded the place -- which would make saving and loading a way to call the war off.</summary>
        public void RestoreLifetimeProduced(int produced)
        {
            LifetimeProduced = Mathf.Max(0, produced);
        }

        public void SetAmount(ResourceType type, int amount)
        {
            _resources[type] = amount;
            OnResourceChanged?.Invoke(type, amount);
        }

        public bool HasEnough(IEnumerable<ResourceAmount> costs)
        {
            if (InfiniteResources) return true;

            foreach (var cost in costs)
            {
                if (GetAmount(cost.type) < cost.amount) return false;
            }
            return true;
        }

        public bool TrySpend(IEnumerable<ResourceAmount> costs)
        {
            if (InfiniteResources) return true;

            var costList = new List<ResourceAmount>(costs);
            if (!HasEnough(costList)) return false;

            foreach (var cost in costList)
            {
                Add(cost.type, -cost.amount);
            }
            return true;
        }
    }
}
