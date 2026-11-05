using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The card of a ruin of the player's (BuildingRuin; the user, 2026-11-05): what stood here, and
    /// three buttons -- «Восстановить» with its price above it, «Снести», «Закрыть».
    /// </summary>
    public class RuinCard : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text title;
        [SerializeField] private Text body;
        [SerializeField] private Transform costRow;
        [SerializeField] private Button restoreButton;
        [SerializeField] private ResourceIconLibrary iconLibrary;

        public static RuinCard Instance { get; private set; }

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private BuildingRuin _shown;
        private float _nextCheck;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show(BuildingRuin ruin)
        {
            if (ruin == null || panelRoot == null) return;
            _shown = ruin;
            panelRoot.SetActive(true);
            if (title != null) title.text = Localization.Format("#ruin_title", ruin.Data.LocalizedName);
            if (body != null) body.text = Localization.Get("#ruin_body");
            CostRowBuilder.Build(costRow, ruin.RestoreCost(), iconLibrary);
            UpdateAffordable();
        }

        public void Close()
        {
            _shown = null;
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        public void Restore()
        {
            if (_shown == null) return;
            if (_shown.TryRestore() != null) Close();
        }

        public void Clear()
        {
            if (_shown == null) return;
            _shown.Clear();
            Close();
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (_shown == null)
            {
                Close();
                return;
            }
            // a few times a second is plenty for a button's shade
            if (Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + 0.25f;
            UpdateAffordable();
        }

        private void UpdateAffordable()
        {
            if (restoreButton == null || _shown == null) return;
            restoreButton.interactable = ResourceManager.Instance == null || ResourceManager.Instance.HasEnough(_shown.RestoreCost());
        }
    }
}
