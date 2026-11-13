using CityBuilder.Citizens;
using CityBuilder.Core;
using CityBuilder.Invasion;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The day, under the resource bar: "День {N}". The settlement's tier name used to stand in
    /// front of it; the user had every mention of tiers taken away from the player (2026-10-14) --
    /// SettlementTierManager still names the tiers, but nothing shows them.
    ///
    /// In an Invasion run the same line shows the waves instead: "Волна N через 0:25" during a
    /// breather, "Волна N: орков M" while one is underway.
    /// </summary>
    public class SettlementStatusController : MonoBehaviour
    {
        [SerializeField] private GameCalendar gameCalendar;
        [SerializeField] private Text statusText;

        private const float WaveRefreshSeconds = 0.25f;

        private float _refreshIn;

        private void Start()
        {
            if (gameCalendar != null) gameCalendar.OnDayPassed += Refresh;
            Refresh();
        }

        private void Update()
        {
            if (!InvasionMode.IsActive && !Campaign.CityDefenceMode.IsActive) return;
            _refreshIn -= Time.unscaledDeltaTime;
            if (_refreshIn > 0f) return;
            _refreshIn = WaveRefreshSeconds;
            Refresh();
        }

        private void Refresh()
        {
            if (statusText == null) return;

            if (InvasionMode.IsActive)
            {
                statusText.text = WaveText(InvasionMode.Instance.Waves);
                return;
            }
            // campaign mission 2: the part's clock (the user, 2026-11-12: «время рисуем на миникарте, как в Нашествии»)
            if (Campaign.CityDefenceMode.IsActive)
            {
                var mode = Campaign.CityDefenceMode.Instance;
                // the courier on his way, then the help on its way (the user, 2026-11-13: one part, the last 3 minutes
                // say Подкрепление через; the capital's cutscene at that turn comes later)
                var help = mode.HelpComing || mode.Current == Campaign.CityDefenceMode.Stage.Won;
                var left = help ? mode.TimeLeft : mode.TimeLeft - mode.ReinforceSeconds;
                var seconds = Mathf.Max(0, Mathf.CeilToInt(left));
                // the plaque is narrow: the words shrink to fit so the time after them shows (the user's eighth
                // playtest: only «Подкрепление» was seen)
                if (!statusText.resizeTextForBestFit)
                {
                    statusText.resizeTextForBestFit = true;
                    statusText.resizeTextMaxSize = statusText.fontSize;
                    statusText.resizeTextMinSize = Mathf.Max(8, statusText.fontSize / 2);
                    statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
                    statusText.verticalOverflow = VerticalWrapMode.Truncate;
                }
                statusText.text = Localization.Format(help ? "#m2_clock_help" : "#m2_clock_courier", $"{seconds / 60}:{seconds % 60:00}");
                return;
            }
            // no days pass below (the user, 2026-11-01: «вкладку День он тоже не должен видеть»)
            if (Dungeon.DungeonMode.IsActive)
            {
                statusText.text = string.Empty;
                return;
            }

            var day = gameCalendar != null ? gameCalendar.CurrentDay : 1;
            statusText.text = Localization.Format("#hud_day", day);
        }

        private static string WaveText(InvasionWaves waves)
        {
            if (waves == null || waves.Current == InvasionWaves.Phase.NotStarted) return string.Empty;
            if (waves.Current == InvasionWaves.Phase.Underway) return Localization.Format("#hud_inv_wave", waves.Wave, waves.OrcsLeft);

            var seconds = Mathf.Max(0, Mathf.CeilToInt(waves.SecondsUntilNextWave));
            return Localization.Format("#hud_inv_wave_in", waves.Wave + 1, $"{seconds / 60}:{seconds % 60:00}");
        }
    }
}
