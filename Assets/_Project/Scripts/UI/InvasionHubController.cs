using System;
using CityBuilder.Core;
using CityBuilder.Invasion;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The Invasion's screen in the main menu (the user's sketch, 2026-10-25), from the records on
    /// the device (InvasionRecords):
    ///
    /// - the picture of the theme picked (a settlement on that theme's map), the themes under it;
    /// - the period -- day, week, month (the tabs under the screen): its points (the sum of its best
    ///   runs) in the corner, and those best runs in a row;
    /// - the leaderboard: until the online one exists, the player's own row, lit;
    /// - the crystals the player holds.
    /// </summary>
    public class InvasionHubController : MonoBehaviour
    {
        [SerializeField] private ThemePicker themes;
        [SerializeField] private Image picture;
        /// <summary>MapThemes.Id → its picture (MenuArtBake's Inv_*.png).</summary>
        [SerializeField] private string[] pictureThemes = new string[0];
        [SerializeField] private Sprite[] pictureSprites = new Sprite[0];

        [SerializeField] private Text pointsText;
        [SerializeField] private Text topHeading;
        [SerializeField] private Text[] topRuns = new Text[0];
        [SerializeField] private GameObject[] tabSelected = new GameObject[0];
        [SerializeField] private Text playerRowPlace;
        [SerializeField] private Text playerRowName;
        [SerializeField] private Text playerRowPoints;
        [SerializeField] private Text playerRowReward;
        [SerializeField] private Text crystalsText;

        private static readonly string[] Headings = { "#invasion_top_day", "#invasion_top_week", "#invasion_top_month" };

        private InvasionRecords.Period _period = InvasionRecords.Period.Day;

        private void Awake()
        {
            if (themes != null) themes.Changed += _ => ShowPicture();
        }

        private void OnEnable() => Refresh();

        /// <summary>A period tab: 0 day, 1 week, 2 month.</summary>
        public void SelectPeriod(int period)
        {
            _period = (InvasionRecords.Period)Mathf.Clamp(period, 0, 2);
            Refresh();
        }

        public void Refresh()
        {
            ShowPicture();
            var counted = BalanceConfig.Instance.InvasionRunsCountedPerDay;
            var days = InvasionRecords.LoadDays();
            var runs = InvasionRecords.TopRuns(days, DateTime.Now, _period, counted);
            var points = 0;
            foreach (var run in runs) points += run;

            if (pointsText != null) pointsText.text = points.ToString();
            if (topHeading != null) topHeading.text = Localization.Get(Headings[(int)_period]);
            for (var i = 0; i < topRuns.Length; i++)
            {
                if (topRuns[i] != null) topRuns[i].text = i < runs.Count ? runs[i].ToString() : "—";
            }
            for (var i = 0; i < tabSelected.Length; i++)
            {
                if (tabSelected[i] != null) tabSelected[i].SetActive(i == (int)_period);
            }

            // until the online leaderboard: the player's own row, no place and no prize yet
            if (playerRowPlace != null) playerRowPlace.text = "—";
            if (playerRowName != null) playerRowName.text = Localization.Get("#invasion_you");
            if (playerRowPoints != null) playerRowPoints.text = points.ToString();
            if (playerRowReward != null) playerRowReward.text = "—";
            if (crystalsText != null) crystalsText.text = Wallet.Crystals.ToString();
        }

        private void ShowPicture()
        {
            if (picture == null) return;
            var id = MapThemes.Id(themes != null ? themes.Current : GameSettings.InvasionTheme);
            for (var i = 0; i < pictureThemes.Length && i < pictureSprites.Length; i++)
            {
                if (pictureThemes[i] != id || pictureSprites[i] == null) continue;
                picture.sprite = pictureSprites[i];
                return;
            }
        }
    }
}
