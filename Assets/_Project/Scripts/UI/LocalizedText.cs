using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// A label that reads its text from the localization sheet instead of carrying it.
    ///
    /// Attached by SetupProject to every fixed caption it builds -- buttons, titles, headings. The
    /// labels that change while the game runs (worker counts, the event log) are not these: their
    /// controllers call Localization.Format themselves, because the text depends on numbers only
    /// they know.
    ///
    /// Re-reads on enable as well as on a language change, so a panel that was switched off while
    /// the player changed language comes back in the right language rather than the old one.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string key = string.Empty;

        private Text _label;

        public string Key => key;

        /// <summary>Used by SetupProject when it builds the label; also the way a controller can repoint an existing caption at another key.</summary>
        public void SetKey(string localizationKey)
        {
            key = localizationKey;
            Apply();
        }

        private void OnEnable()
        {
            Localization.OnLanguageChanged += Apply;
            Apply();
        }

        private void OnDisable()
        {
            Localization.OnLanguageChanged -= Apply;
        }

        private void Apply()
        {
            if (string.IsNullOrEmpty(key)) return;

            if (_label == null) _label = GetComponent<Text>();
            if (_label != null) _label.text = Localization.Get(key);
        }
    }
}
