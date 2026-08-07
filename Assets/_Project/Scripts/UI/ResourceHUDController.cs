using CityBuilder.Citizens;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    public class ResourceHUDController : MonoBehaviour
    {
        [SerializeField] private Text label;

        private void Start()
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnResourceChanged += HandleResourceChanged;
                ResourceManager.Instance.OnInfiniteResourcesChanged += HandleInfiniteResourcesChanged;
            }
            if (CitizenManager.Instance != null) CitizenManager.Instance.OnPopulationChanged += Refresh;
            Refresh();
        }

        private void HandleResourceChanged(ResourceType type, int amount)
        {
            Refresh();
        }

        private void HandleInfiniteResourcesChanged(bool enabled)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (label == null || ResourceManager.Instance == null) return;

            var pop = CitizenManager.Instance != null
                ? $"{CitizenManager.Instance.TotalPopulation} (своб. {CitizenManager.Instance.IdlePopulation})"
                : "0";

            var infinite = ResourceManager.Instance.InfiniteResources;
            string Amount(ResourceType type) => infinite ? "∞" : ResourceManager.Instance.GetAmount(type).ToString();

            label.text =
                $"Дерево {Amount(ResourceType.Wood)}   " +
                $"Камень {Amount(ResourceType.Stone)}   " +
                $"Еда {Amount(ResourceType.Food)}   " +
                $"Золото {Amount(ResourceType.Gold)}   " +
                $"Жители {pop}" +
                (infinite ? "   [ДЕБАГ: РЕСУРСЫ ∞ — F9]" : string.Empty);
        }
    }
}
