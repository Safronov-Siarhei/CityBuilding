using CityBuilder.Core;
using CityBuilder.Ships;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// A selected ship's buttons where the build menu stands. LAND (a ship that carries people; the
    /// user, 2026-10-28: landing is a button, at a beach only): who is aboard of how many seats; a
    /// tap lands them on the beach the ship stands at, or says why not -- dimmed away from a beach
    /// or with nobody aboard. REPAIR (a holed ship; the user: repair only for resources): the wood
    /// it costs; a tap mends it to full at a Shipyard, or says why not. Let the ship go and the
    /// build menu comes back (BuildingPlacerUIVisibility reads Showing).
    /// </summary>
    public class ShipActionBar : MonoBehaviour
    {
        private const float RefreshSeconds = 0.2f;
        private static readonly Color Dimmed = new Color(0.55f, 0.55f, 0.55f, 1f);

        [SerializeField] private GameObject bar;
        [SerializeField] private Button landButton;
        [SerializeField] private Image landIcon;
        [SerializeField] private Text seats;
        [SerializeField] private Button repairButton;
        [SerializeField] private Image repairIcon;
        [SerializeField] private Text repairCost;

        /// <summary>Up: a ship is in hand and its buttons stand where the build menu would.</summary>
        public static bool Showing { get; private set; }

        private float _refreshIn;

        private void Awake()
        {
            if (landButton != null) landButton.onClick.AddListener(() => Land());
            if (repairButton != null) repairButton.onClick.AddListener(() => Repair());
            if (bar != null) bar.SetActive(false);
        }

        private void OnDestroy() => Showing = false;

        /// <summary>Lands the selected ship's people; how many landed (0 = said why not).</summary>
        public int Land()
        {
            var ship = ShipInput.Selected;
            var manager = ShipManager.Instance;
            if (ship == null || manager == null) return 0;
            if (Refused(manager.DescribeLandBlocker(ship))) return 0;
            _refreshIn = 0f;
            return manager.TryLand(ship);
        }

        /// <summary>Mends the selected ship at the Shipyard by it; false = said why not.</summary>
        public bool Repair()
        {
            var ship = ShipInput.Selected;
            var manager = ShipManager.Instance;
            if (ship == null || manager == null) return false;
            if (Refused(manager.DescribeRepairBlocker(ship))) return false;
            _refreshIn = 0f;
            return manager.TryRepair(ship);
        }

        private static bool Refused(string blocker)
        {
            if (blocker == null) return false;
            EventLogManager.Instance?.Post(GameEvent.ForSettlement(blocker, EventSeverity.Warning, EventGlyph.Alert, null));
            return true;
        }

        private void Update()
        {
            var ship = ShipInput.Selected;
            var carries = ship != null && ship.Capacity > 0;
            var holed = ship != null && ship.Health < ship.MaxHealth;
            var show = carries || holed;
            Showing = show;
            if (bar != null && bar.activeSelf != show) bar.SetActive(show);
            if (!show) return;

            if (landButton != null && landButton.gameObject.activeSelf != carries) landButton.gameObject.SetActive(carries);
            if (repairButton != null && repairButton.gameObject.activeSelf != holed) repairButton.gameObject.SetActive(holed);

            _refreshIn -= Time.unscaledDeltaTime;
            if (_refreshIn > 0f) return;
            _refreshIn = RefreshSeconds;
            var manager = ShipManager.Instance;
            if (carries)
            {
                if (seats != null) seats.text = $"{ship.Aboard.Count}/{ship.Capacity}";
                if (landIcon != null) landIcon.color = manager != null && manager.DescribeLandBlocker(ship) == null ? Color.white : Dimmed;
            }
            if (holed)
            {
                var cost = ShipManager.RepairCost(ship);
                if (repairCost != null) repairCost.text = cost != null ? cost[0].amount.ToString() : "0";
                if (repairIcon != null) repairIcon.color = manager != null && manager.DescribeRepairBlocker(ship) == null ? Color.white : Dimmed;
            }
        }
    }
}
