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
            if (ResourceManager.Instance != null) ResourceManager.Instance.OnResourceChanged += HandleResourceChanged;
            if (CitizenManager.Instance != null) CitizenManager.Instance.OnPopulationChanged += Refresh;
            Refresh();
        }

        private void HandleResourceChanged(ResourceType type, int amount)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (label == null || ResourceManager.Instance == null) return;

            var pop = CitizenManager.Instance != null
                ? $"{CitizenManager.Instance.TotalPopulation} (своб. {CitizenManager.Instance.IdlePopulation})"
                : "0";

            label.text =
                $"Дерево {ResourceManager.Instance.GetAmount(ResourceType.Wood)}   " +
                $"Камень {ResourceManager.Instance.GetAmount(ResourceType.Stone)}   " +
                $"Еда {ResourceManager.Instance.GetAmount(ResourceType.Food)}   " +
                $"Золото {ResourceManager.Instance.GetAmount(ResourceType.Gold)}   " +
                $"Жители {pop}";
        }
    }
}
