using System;
using CityBuilder.Core;
using CityBuilder.Invasion;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The Invasion's screen in the main menu, «Зал знамён» (the user's pick B2, 2026-11-16), from the
    /// records on the device (InvasionRecords):
    ///
    /// - the picked theme's picture in a gilt frame, its season on the brass plate, the seals under it;
    /// - three period banners -- day, week, month -- each with its points (the sum of its best runs);
    ///   the picked one big and lit with its 5 best runs and the player's line (place: soon, until the
    ///   online board exists), the others small with «нажми, чтобы открыть»;
    /// - the crystals the player holds.
    /// </summary>
    public class InvasionHubController : MonoBehaviour
    {
        [SerializeField] private ThemePicker themes;
        [SerializeField] private Image picture;
        [SerializeField] private Text seasonText;
        /// <summary>MapThemes.Id → its picture (MenuArtBake's Inv_*.png).</summary>
        [SerializeField] private string[] pictureThemes = new string[0];
        [SerializeField] private Sprite[] pictureSprites = new Sprite[0];

        /// <summary>Per period (day, week, month): the big banner, the small one, the big one's glow,
        /// its rows group, the small one's hint, its points.</summary>
        [SerializeField] private GameObject[] bannerBig = new GameObject[0];
        [SerializeField] private GameObject[] bannerSmall = new GameObject[0];
        [SerializeField] private GameObject[] bannerGlow = new GameObject[0];
        [SerializeField] private GameObject[] bannerDetails = new GameObject[0];
        [SerializeField] private GameObject[] bannerHint = new GameObject[0];
        [SerializeField] private Text[] bannerPoints = new Text[0];
        /// <summary>5 per period, period-major.</summary>
        [SerializeField] private Text[] runTexts = new Text[0];
        [SerializeField] private Text crystalsText;

        private InvasionRecords.Period _period = InvasionRecords.Period.Day;

        private void Awake()
        {
            if (themes != null) themes.Changed += _ => ShowTheme();
        }

        private void OnEnable() => Refresh();

        /// <summary>A banner: 0 day, 1 week, 2 month.</summary>
        public void SelectPeriod(int period)
        {
            _period = (InvasionRecords.Period)Mathf.Clamp(period, 0, 2);
            Refresh();
        }

        public void Refresh()
        {
            ShowTheme();
            var counted = BalanceConfig.Instance.InvasionRunsCountedPerDay;
            var days = InvasionRecords.LoadDays();
            var now = DateTime.Now;
            for (var p = 0; p < 3; p++)
            {
                var runs = InvasionRecords.TopRuns(days, now, (InvasionRecords.Period)p, counted);
                var points = 0;
                foreach (var run in runs) points += run;
                var on = p == (int)_period;
                Set(bannerBig, p, on);
                Set(bannerGlow, p, on);
                Set(bannerDetails, p, on);
                Set(bannerSmall, p, !on);
                Set(bannerHint, p, !on);
                if (p < bannerPoints.Length && bannerPoints[p] != null) bannerPoints[p].text = points.ToString();
                if (!on) continue;
                for (var k = 0; k < 5; k++)
                {
                    var i = p * 5 + k;
                    if (i < runTexts.Length && runTexts[i] != null) runTexts[i].text = k < runs.Count ? runs[k].ToString() : "—";
                }
            }
            if (crystalsText != null) crystalsText.text = Wallet.UpgradeCrystals.ToString();
        }

        private static void Set(GameObject[] list, int i, bool on)
        {
            if (i < list.Length && list[i] != null && list[i].activeSelf != on) list[i].SetActive(on);
        }

        private void ShowTheme()
        {
            var id = MapThemes.Id(themes != null ? themes.Current : GameSettings.InvasionTheme);
            if (seasonText != null) seasonText.text = Localization.Get("#theme_" + id);
            if (picture == null) return;
            for (var i = 0; i < pictureThemes.Length && i < pictureSprites.Length; i++)
            {
                if (pictureThemes[i] != id || pictureSprites[i] == null) continue;
                picture.sprite = pictureSprites[i];
                return;
            }
        }
    }
}
