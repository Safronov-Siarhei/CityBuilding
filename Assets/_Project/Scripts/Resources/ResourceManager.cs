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

        public void Add(ResourceType type, int amount)
        {
            if (amount == 0) return;
            _resources.TryGetValue(type, out var current);
            _resources[type] = current + amount;
            OnResourceChanged?.Invoke(type, _resources[type]);
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
