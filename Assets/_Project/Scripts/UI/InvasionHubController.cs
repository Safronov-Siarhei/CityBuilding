using System;
using CityBuilder.Core;
using CityBuilder.Invasion;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The Invasion hub's numbers in the main menu, from the records on the device
    /// (InvasionRecords): today's points, how many of today's runs count so far (a pip each), and
    /// the leaderboard tabs -- day, week, month. Until the online leaderboard exists, a tab's
    /// board holds one row, the player's own points for that period.
    /// </summary>
    public class InvasionHubController : MonoBehaviour
    {
        [SerializeField] private Text pointsText;
        [SerializeField] private Text countedText;
        [SerializeField] private Image[] runPips;
        [SerializeField] private GameObject[] tabSelected;
        [SerializeField] private Text playerRowName;
        [SerializeField] private Text playerRowPoints;

        private const float PipOff = 0.25f;

        private int _period;

        private void OnEnable() => Refresh();

        /// <summary>A leaderboard tab: 0 day, 1 week, 2 month.</summary>
        public void SelectPeriod(int period)
        {
            _period = period;
            Refresh();
        }

        public void Refresh()
        {
            var counted = BalanceConfig.Instance.InvasionRunsCountedPerDay;
            var days = InvasionRecords.LoadDays();
            var now = DateTime.Now;
            var today = InvasionRecords.DayPoints(days, now, counted);
            var runs = InvasionRecords.RunsCounted(days, now, counted);

            if (pointsText != null) pointsText.text = today.ToString();
            if (countedText != null) countedText.text = Localization.Format("#invasion_counted", runs, counted);
            if (runPips != null)
            {
                for (var i = 0; i < runPips.Length; i++)
                {
                    if (runPips[i] == null) continue;
                    var c = runPips[i].color;
                    runPips[i].color = new Color(c.r, c.g, c.b, i < runs ? 1f : PipOff);
                }
            }
            if (tabSelected != null)
            {
                for (var i = 0; i < tabSelected.Length; i++)
                {
                    if (tabSelected[i] != null) tabSelected[i].SetActive(i == _period);
                }
            }

            var points = _period == 0 ? today
                : _period == 1 ? InvasionRecords.WeekPoints(days, now, counted)
                : InvasionRecords.MonthPoints(days, now, counted);
            if (playerRowName != null) playerRowName.text = Localization.Get("#invasion_you");
            if (playerRowPoints != null) playerRowPoints.text = points.ToString();
        }
    }
}
