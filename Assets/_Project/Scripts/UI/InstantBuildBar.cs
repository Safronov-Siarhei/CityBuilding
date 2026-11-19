using CityBuilder.Buildings;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// «Построить сразу» (the user, 2026-11-17): while a building of the player's is held and still under
    /// construction (a new one or an upgrade), a round button with a crystal and its price stands LEFT of the
    /// «!» in the focus row (the row centres both); a tap pays instant_build_crystals (1) for every started
    /// instant_build_step_sec (30) of the seconds left -- under 30 s 1, 30-60 s 2, ... (the user, 2026-11-18) -- and the construction
    /// finishes now. Short of crystals: the shop opens. Every building alike; not in the tutorial, not in an Invasion run.
    /// </summary>
    public class InstantBuildBar : MonoBehaviour
    {
        private const float RefreshSeconds = 0.2f;

        [SerializeField] private GameObject button;
        [SerializeField] private Text price;

        private float _refreshIn;

        private static int PriceOf(BuildingInstance target)
        {
            var config = BalanceConfig.Instance;
            var each = config != null ? config.InstantBuildCrystals : 1;
            var step = config != null ? config.InstantBuildStepSeconds : 30f;
            var left = target != null && target.Site != null ? target.Site.Remaining : 0f;
            return each * (Mathf.FloorToInt(Mathf.Max(0f, left) / step) + 1);
        }

        private static BuildingInstance Target
        {
            get
            {
                // not in the tutorial, not in an Invasion run (the user, 2026-11-19: crystals would change the run's balance)
                if (Tutorial.TutorialMode.IsActive || Invasion.InvasionMode.IsActive) return null;
                var focus = BuildingFocus.Instance != null && BuildingFocus.Instance.ButtonsShown ? BuildingFocus.Instance.Current : null;
                return focus != null && !focus.IsBot && focus.Site != null && focus.CurrentHealth > 0 ? focus : null;
            }
        }

        /// <summary>The button (a persistent listener).</summary>
        public void Tap()
        {
            var target = Target;
            if (target == null) return;
            var cost = PriceOf(target);
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
            var target = Target;
            var show = target != null;
            if (button != null && button.activeSelf != show) button.SetActive(show);
            if (!show) return;
            _refreshIn -= Time.unscaledDeltaTime;
            if (_refreshIn > 0f) return;
            _refreshIn = RefreshSeconds;
            if (price != null) price.text = PriceOf(target).ToString();
        }
    }
}
