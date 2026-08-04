using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Resources
{
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        public event Action<ResourceType, int> OnResourceChanged;

        [SerializeField] private List<ResourceAmount> startingResources = new List<ResourceAmount>
        {
            new ResourceAmount { type = ResourceType.Wood, amount = 50 },
            new ResourceAmount { type = ResourceType.Stone, amount = 20 },
            new ResourceAmount { type = ResourceType.Food, amount = 30 },
            new ResourceAmount { type = ResourceType.Gold, amount = 100 },
        };

        private readonly Dictionary<ResourceType, int> _resources = new Dictionary<ResourceType, int>();

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

        public bool HasEnough(IEnumerable<ResourceAmount> costs)
        {
            foreach (var cost in costs)
            {
                if (GetAmount(cost.type) < cost.amount) return false;
            }
            return true;
        }

        public bool TrySpend(IEnumerable<ResourceAmount> costs)
        {
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
