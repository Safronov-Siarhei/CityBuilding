using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// «Остановить чуму» (the user, 2026-11-13): once illness has killed plague_banner_share of an
    /// island since its plague broke out (Citizens/Plague), the game pauses and this asks whether to
    /// end it now -- for a rewarded ad (only while one is ready, Shop.RewardedAds) or for crystals.
    /// Closing it lets the plague run on. The ad is the shop's stand-in: a few real seconds, then done.
    /// </summary>
    public class PlagueBanner : MonoBehaviour
    {
        public static PlagueBanner Instance { get; private set; }

        [SerializeField] private GameObject root;
        [SerializeField] private Text bodyText;
        [SerializeField] private Button adButton;
        [SerializeField] private Text adLabel;
        [SerializeField] private Button crystalsButton;
        [SerializeField] private Text crystalsLabel;

        private int _island = -1;
        private float _timeScale = 1f;
        private float _adLeft = -1f;

        private void Awake()
        {
            Instance = this;
            if (root != null) root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool IsOpen => root != null && root.activeSelf;

        /// <summary>The banner over a paused game, for this island's plague.</summary>
        public void Show(int island)
        {
            if (root == null || IsOpen) return;
            _island = island;
            _adLeft = -1f;
            _timeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            Refresh();
        }

        private void Refresh()
        {
            var price = BalanceConfig.Instance.PlagueEndCrystals;
            if (bodyText != null) bodyText.text = Localization.Get("#plague_banner_text");
            if (adButton != null) adButton.gameObject.SetActive(Shop.RewardedAds.IsReady);
            if (adLabel != null) adLabel.text = _adLeft >= 0f ? Localization.Get("#plague_banner_ad_playing") : Localization.Get("#plague_banner_ad");
            // the price alone, beside the crystal icon (the user, 2026-11-13: «вместо слов просто 30 + иконка»)
            if (crystalsLabel != null) crystalsLabel.text = price.ToString();
            if (crystalsButton != null) crystalsButton.interactable = Wallet.UpgradeCrystals >= price && _adLeft < 0f;
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (_adLeft >= 0f)
            {
                _adLeft -= Time.unscaledDeltaTime;
                if (_adLeft < 0f)
                {
                    EndPlague();
                    return;
                }
            }
            Refresh();
        }

        /// <summary>«Смотреть рекламу»: the stand-in ad plays, then the plague ends.</summary>
        public void WatchAd()
        {
            if (_adLeft >= 0f || !Shop.RewardedAds.IsReady) return;
            _adLeft = Mathf.Max(0.1f, BalanceConfig.Instance.ShopAdStubSeconds);
            Refresh();
        }

        /// <summary>«За N кристаллов».</summary>
        public void PayCrystals()
        {
            var price = BalanceConfig.Instance.PlagueEndCrystals;
            if (_adLeft >= 0f || Wallet.UpgradeCrystals < price) return;
            Wallet.UpgradeCrystals -= price;
            EndPlague();
        }

        private void EndPlague()
        {
            Citizens.Plague.Instance?.EndByBanner(_island);   // ended and shielded for plague_shield_sec
            Close();
        }

        /// <summary>Closed without paying: the plague runs on.</summary>
        public void Close()
        {
            _adLeft = -1f;
            if (root != null) root.SetActive(false);
            Time.timeScale = _timeScale;
        }
    }
}
