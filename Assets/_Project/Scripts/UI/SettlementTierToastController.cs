using System.Collections;
using CityBuilder.Citizens;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>Flashes a center-screen toast for a few seconds whenever SettlementTierManager reports the settlement crossed into a new population tier.</summary>
    public class SettlementTierToastController : MonoBehaviour
    {
        private const float ShowSeconds = 4f;

        [SerializeField] private SettlementTierManager tierManager;
        [SerializeField] private Text toastText;

        private Coroutine _hideRoutine;

        private void Start()
        {
            if (tierManager != null) tierManager.OnTierChanged += HandleTierChanged;
        }

        private void HandleTierChanged(string tierName)
        {
            if (toastText == null) return;

            toastText.text = $"Поселение выросло. Теперь это: {tierName}.";
            toastText.gameObject.SetActive(true);

            if (_hideRoutine != null) StopCoroutine(_hideRoutine);
            _hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(ShowSeconds);
            if (toastText != null) toastText.gameObject.SetActive(false);
            _hideRoutine = null;
        }
    }
}
