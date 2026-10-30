using CityBuilder.Buildings;
using CityBuilder.Ships;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The lighthouse's button beside the «!» while one is in focus: its price in coins on a plaque,
    /// the seconds left while the lantern recovers; a tap puts the next tap on the map in hand to
    /// choose where to light (LighthouseAim). Dimmed while it cannot.
    /// </summary>
    public class LighthouseBar : MonoBehaviour
    {
        private const float RefreshSeconds = 0.2f;
        private static readonly Color Dimmed = new Color(0.55f, 0.55f, 0.55f, 1f);

        [SerializeField] private GameObject button;
        [SerializeField] private Image icon;
        [SerializeField] private Text price;

        private float _refreshIn;

        private static BuildingInstance Target
        {
            get
            {
                var focus = BuildingFocus.Instance != null && BuildingFocus.Instance.DetailsShown ? BuildingFocus.Instance.Current : null;
                return focus != null && focus.Data != null && focus.Data.buildingName == LighthouseAim.BuildingId && focus.IsOperational ? focus : null;
            }
        }

        /// <summary>The button (a persistent listener).</summary>
        public void Tap()
        {
            var target = Target;
            if (target != null) LighthouseAim.Begin(target);
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
            var wait = LighthouseAim.CooldownLeft(target);
            if (price != null) price.text = wait > 0f ? $"{Mathf.CeilToInt(wait)}" : LighthouseAim.Price.ToString();
            if (icon != null) icon.color = LighthouseAim.Aiming != null || LighthouseAim.DescribeBlocker(target) == null ? Color.white : Dimmed;
        }
    }
}
