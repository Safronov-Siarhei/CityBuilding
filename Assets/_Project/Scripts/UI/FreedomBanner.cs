using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The purchase banner «Свобода песочницы» (approved by the user 2026-11-17, `Design/ui/SandboxHub_A3_Banner.png`),
    /// built like the plague and fire banners: a wood window over the sunny chart with the King and the Queen, a teal
    /// ribbon, a parchment with what it opens, «Открыть за $0.99» and «Закрыть». It comes up when the player tries to
    /// change a world setting without the purchase (WorldSettingsScroll); the screen behind it is hidden. One per scene,
    /// built by SetupProjectMainMenuScreens / SetupProjectHud (the art: Tools/ui/sandbox_hub_art.py).
    /// </summary>
    public class FreedomBanner : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text buyLabel;
        [SerializeField] private Button buyButton;
        [SerializeField] private Button closeButton;
        /// <summary>What is hidden while the banner is up (the screen's front).</summary>
        [SerializeField] private CanvasGroup[] hide = new CanvasGroup[0];

        private static FreedomBanner _instance;

        private void Awake()
        {
            _instance = this;
            if (buyButton != null) buyButton.onClick.AddListener(Buy);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (root != null) root.SetActive(false);
        }

        private void OnEnable() => Store.Granted += OnGranted;

        private void OnDisable() => Store.Granted -= OnGranted;

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        /// <summary>Puts the banner up (nothing to sell when it is bought already).</summary>
        public static void Open()
        {
            if (_instance == null || Wallet.FreedomOwned) return;
            _instance.Show(true);
        }

        private void Show(bool on)
        {
            if (root != null) root.SetActive(on);
            foreach (var g in hide)
            {
                if (g == null) continue;
                g.alpha = on ? 0f : 1f;
                g.blocksRaycasts = !on;
            }
            if (on && buyLabel != null) buyLabel.text = Localization.Format("#freedom_buy", Store.PriceText(Store.Freedom));
        }

        private void Buy() => Store.Buy(Store.Freedom);

        private void OnGranted(string id)
        {
            if (id == Store.Freedom) Close();
        }

        public void Close() => Show(false);
    }
}
