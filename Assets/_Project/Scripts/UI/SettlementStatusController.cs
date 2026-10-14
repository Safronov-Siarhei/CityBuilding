using CityBuilder.Citizens;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The day, under the resource bar: "День {N}". The settlement's tier name used to stand in
    /// front of it; the user had every mention of tiers taken away from the player (2026-10-14) --
    /// SettlementTierManager still names the tiers, but nothing shows them.
    /// </summary>
    public class SettlementStatusController : MonoBehaviour
    {
        [SerializeField] private GameCalendar gameCalendar;
        [SerializeField] private Text statusText;

        private void Start()
        {
            if (gameCalendar != null) gameCalendar.OnDayPassed += Refresh;
            Refresh();
        }

        private void Refresh()
        {
            if (statusText == null) return;

            var day = gameCalendar != null ? gameCalendar.CurrentDay : 1;
            statusText.text = Localization.Format("#hud_day", day);
        }
    }
}
