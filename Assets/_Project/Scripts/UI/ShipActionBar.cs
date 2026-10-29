using CityBuilder.Core;
using CityBuilder.Ships;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// A selected transport's button where the build menu stands (the user, 2026-10-28: landing is
    /// a button, at a beach only): who is aboard of how many seats; a tap lands them on the beach
    /// the ship stands at, or says why not. Dimmed away from a beach or with nobody aboard. Let the
    /// ship go and the build menu comes back (BuildingPlacerUIVisibility reads Showing).
    /// </summary>
    public class ShipActionBar : MonoBehaviour
    {
        private const float RefreshSeconds = 0.2f;
        private static readonly Color Dimmed = new Color(0.55f, 0.55f, 0.55f, 1f);

        [SerializeField] private GameObject bar;
        [SerializeField] private Button landButton;
        [SerializeField] private Image landIcon;
        [SerializeField] private Text seats;

        /// <summary>Up: a transport is in hand and its button stands where the build menu would.</summary>
        public static bool Showing { get; private set; }

        private float _refreshIn;

        private void Awake()
        {
            if (landButton != null) landButton.onClick.AddListener(() => Land());
            if (bar != null) bar.SetActive(false);
        }

        private void OnDestroy() => Showing = false;

        /// <summary>Lands the selected ship's people; how many landed (0 = said why not).</summary>
        public int Land()
        {
            var ship = ShipInput.Selected;
            var manager = ShipManager.Instance;
            if (ship == null || manager == null) return 0;
            var blocker = manager.DescribeLandBlocker(ship);
            if (blocker != null)
            {
                EventLogManager.Instance?.Post(GameEvent.ForSettlement(blocker, EventSeverity.Warning, EventGlyph.Alert, null));
                return 0;
            }
            _refreshIn = 0f;
            return manager.TryLand(ship);
        }

        private void Update()
        {
            var ship = ShipInput.Selected;
            var show = ship != null && ship.Capacity > 0;
            Showing = show;
            if (bar != null && bar.activeSelf != show) bar.SetActive(show);
            if (!show) return;

            _refreshIn -= Time.unscaledDeltaTime;
            if (_refreshIn > 0f) return;
            _refreshIn = RefreshSeconds;
            if (seats != null) seats.text = $"{ship.Aboard.Count}/{ship.Capacity}";
            if (landIcon != null) landIcon.color = ShipManager.Instance != null && ShipManager.Instance.DescribeLandBlocker(ship) == null ? Color.white : Dimmed;
        }
    }
}
