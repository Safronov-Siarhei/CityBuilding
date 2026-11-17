using CityBuilder.Buildings;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// «Построить сразу» (the user, 2026-11-17): while a building of the player's is held and still under
    /// construction (a new one or an upgrade), a round button with a crystal and its price stands LEFT of the
    /// «!» in the focus row (the row centres both); a tap pays instant_build_crystals (1) and the construction
    /// finishes now. Short of crystals: the shop opens. Every building alike; not in the tutorial.
    /// </summary>
    public class InstantBuildBar : MonoBehaviour
    {
        private const float RefreshSeconds = 0.2f;

        [SerializeField] private GameObject button;
        [SerializeField] private Text price;

        private float _refreshIn;

        private static int Price => BalanceConfig.Instance != null ? BalanceConfig.Instance.InstantBuildCrystals : 1;

        private static BuildingInstance Target
        {
            get
            {
                if (Tutorial.TutorialMode.IsActive) return null;
                var focus = BuildingFocus.Instance != null && BuildingFocus.Instance.ButtonsShown ? BuildingFocus.Instance.Current : null;
                return focus != null && !focus.IsBot && focus.Site != null && focus.CurrentHealth > 0 ? focus : null;
            }
        }

        /// <summary>The button (a persistent listener).</summary>
        public void Tap()
        {
            var target = Target;
            if (target == null) return;
            var cost = Price;
            if (Wallet.UpgradeCrystals < cost)
            {
                Shop.OfferShopWindow.Show();
                return;
            }
            Wallet.UpgradeCrystals -= cost;
            target.Site.Finish();
            _refreshIn = 0f;
        }

        private void Update()
        {
            var show = Target != null;
            if (button != null && button.activeSelf != show) button.SetActive(show);
            if (!show) return;
            _refreshIn -= Time.unscaledDeltaTime;
            if (_refreshIn > 0f) return;
            _refreshIn = RefreshSeconds;
            if (price != null) price.text = Price.ToString();
        }
    }
}
