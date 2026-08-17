using CityBuilder.Citizens;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// Small persistent "{tier name} · День {N}" label -- SettlementTierToast and EventLog both
    /// only surface this momentarily/in passing, so there was no standing place to just glance at
    /// the current day or settlement name otherwise.
    /// </summary>
    public class SettlementStatusController : MonoBehaviour
    {
        [SerializeField] private GameCalendar gameCalendar;
        [SerializeField] private SettlementTierManager tierManager;
        [SerializeField] private Text statusText;

        private void Start()
        {
            if (gameCalendar != null) gameCalendar.OnDayPassed += Refresh;
            if (tierManager != null) tierManager.OnTierChanged += HandleTierChanged;
            Refresh();
        }

        private void HandleTierChanged(string newTierName)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (statusText == null) return;

            var day = gameCalendar != null ? gameCalendar.CurrentDay : 1;
            var tier = tierManager != null ? tierManager.CurrentTierName : string.Empty;
            statusText.text = Localization.Format("hud.status", tier, day);
        }
    }
}
