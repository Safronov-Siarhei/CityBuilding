using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// Readout for HappinessManager: the percentage alone on a round button in the top-right
    /// corner, coloured by how good it is, and -- one tap away -- the factor breakdown under it
    /// (the user's layout, 2026-10-14: a number, no words, details on demand). The breakdown
    /// matters while the model is still being tuned: it says which factor drags the score down.
    /// </summary>
    public class HappinessController : MonoBehaviour
    {
        [SerializeField] private Text happinessLabel;
        [SerializeField] private Text breakdownLabel;
        [SerializeField] private GameObject breakdownPanel;

        public bool BreakdownShown => breakdownPanel != null && breakdownPanel.activeSelf;

        /// <summary>The round button: shows or hides the breakdown.</summary>
        public void ToggleBreakdown()
        {
            if (breakdownPanel == null) return;
            breakdownPanel.SetActive(!breakdownPanel.activeSelf);
            Refresh();
        }

        private void Start()
        {
            if (HappinessManager.Instance != null) HappinessManager.Instance.OnHappinessChanged += Refresh;
            Refresh();
        }

        private void Refresh()
        {
            if (HappinessManager.Instance == null) return;

            if (happinessLabel != null)
            {
                happinessLabel.text = $"{HappinessManager.Instance.HappinessPercent}%";
                happinessLabel.color = ColorForScore(HappinessManager.Instance.HappinessPercent);
            }
            if (breakdownLabel != null)
            {
                // Two lines: the factors do not fit across the panel's width at this font size.
                // The line break lives in the translation, so a language with longer words can
                // break somewhere else without a code change.
                breakdownLabel.text = Localization.Format("#happiness_breakdown",
                    HappinessManager.Instance.TaxScore, HappinessManager.Instance.DecayScore,
                    HappinessManager.Instance.DefenseScore, HappinessManager.Instance.FoodScore,
                    HappinessManager.Instance.DeathScore, HappinessManager.Instance.EntertainmentScore);
            }
        }

        private static Color ColorForScore(int score)
        {
            if (score >= 70) return UiTheme.Good;
            if (score >= 40) return UiTheme.Warning;
            return UiTheme.Danger;
        }
    }
}
