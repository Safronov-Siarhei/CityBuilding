using CityBuilder.Resources;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>+/- stepped control for TaxManager.TaxRatePercent -- a button pair (matching the existing +/- worker-assignment idiom) instead of a drag slider, simpler to build reliably and touch-friendly.</summary>
    public class TaxRateController : MonoBehaviour
    {
        private const int StepPercent = 5;

        [SerializeField] private Text rateLabel;

        private void Start()
        {
            if (TaxManager.Instance != null) TaxManager.Instance.OnTaxRateChanged += Refresh;
            Refresh();
        }

        public void Increase()
        {
            if (TaxManager.Instance == null) return;
            TaxManager.Instance.SetTaxRate(TaxManager.Instance.TaxRatePercent + StepPercent);
        }

        public void Decrease()
        {
            if (TaxManager.Instance == null) return;
            TaxManager.Instance.SetTaxRate(TaxManager.Instance.TaxRatePercent - StepPercent);
        }

        private void Refresh()
        {
            if (rateLabel == null || TaxManager.Instance == null) return;
            rateLabel.text = Localization.Format("tax.rate", TaxManager.Instance.TaxRatePercent);
        }
    }
}
