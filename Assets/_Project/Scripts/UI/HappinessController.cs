using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// Readout panel for HappinessManager -- the headline percentage plus its current factor
    /// breakdown. The breakdown is shown (not just the composite) because the model is still
    /// first-pass/tunable (see HappinessManager) -- seeing which factor is dragging the score down
    /// is more useful while balancing than one opaque number.
    /// </summary>
    public class HappinessController : MonoBehaviour
    {
        [SerializeField] private Text happinessLabel;
        [SerializeField] private Text breakdownLabel;

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
                happinessLabel.text = $"Довольство: {HappinessManager.Instance.HappinessPercent}%";
                happinessLabel.color = ColorForScore(HappinessManager.Instance.HappinessPercent);
            }
            if (breakdownLabel != null)
            {
                // Two lines: five factors do not fit across the panel's width at this font size.
                breakdownLabel.text =
                    $"Налог {HappinessManager.Instance.TaxScore} · Ветхость {HappinessManager.Instance.DecayScore} · Оборона {HappinessManager.Instance.DefenseScore}\n" +
                    $"Еда {HappinessManager.Instance.FoodScore} · Потери {HappinessManager.Instance.DeathScore}";
            }
        }

        private static Color ColorForScore(int score)
        {
            if (score >= 70) return new Color(0.55f, 0.85f, 0.45f);
            if (score >= 40) return new Color(0.95f, 0.85f, 0.35f);
            return new Color(0.9f, 0.35f, 0.3f);
        }
    }
}
