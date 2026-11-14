using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The main menu's language picker (the user, 2026-11-14; concept v2 approved,
    /// Design/ui/LanguagePicker_Concept.png): a small parchment pill in the top-right corner -- the
    /// language's flag, its own name, a chevron -- that opens a list of every language under it, the
    /// current one marked gold with a tick. A tap outside closes it. The base language is English
    /// (Localization.BaseLanguage); a language the sheet has no column for yet reads the English text.
    /// The list (codes, own names, flags) is SetupProjectMainMenu's, from Tools/ui/language_flags.py.
    /// </summary>
    public class LanguagePicker : MonoBehaviour
    {
        [SerializeField] private Image pillFlag;
        [SerializeField] private Text pillName;
        [SerializeField] private RectTransform chevron;
        [SerializeField] private GameObject list;
        [SerializeField] private string[] codes = new string[0];
        [SerializeField] private string[] names = new string[0];
        [SerializeField] private Sprite[] flags = new Sprite[0];
        [SerializeField] private GameObject[] marks = new GameObject[0];

        private void OnEnable()
        {
            Localization.OnLanguageChanged += Refresh;
            Close();
            Refresh();
        }

        private void OnDisable() => Localization.OnLanguageChanged -= Refresh;

        public bool IsOpen => list != null && list.activeSelf;

        /// <summary>The pill: open or close the list.</summary>
        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open()
        {
            if (list == null) return;
            list.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (list != null) list.SetActive(false);
            if (chevron != null) chevron.localEulerAngles = Vector3.zero;
        }

        /// <summary>A row of the list: that language, then the list closes.</summary>
        public void Pick(int index)
        {
            if (index >= 0 && index < codes.Length) Localization.SetLanguage(codes[index]);
            Close();
            Refresh();
        }

        private void Refresh()
        {
            var current = Current();
            if (current >= 0)
            {
                if (pillFlag != null && current < flags.Length) pillFlag.sprite = flags[current];
                if (pillName != null && current < names.Length) pillName.text = names[current];
            }
            for (var i = 0; i < marks.Length; i++)
                if (marks[i] != null) marks[i].SetActive(i == current);
            if (chevron != null) chevron.localEulerAngles = new Vector3(0f, 0f, IsOpen ? 180f : 0f);
        }

        /// <summary>The row of the language in use; English's when it is not in the list.</summary>
        private int Current()
        {
            var language = Localization.Language;
            var english = -1;
            for (var i = 0; i < codes.Length; i++)
            {
                if (codes[i] == language) return i;
                if (codes[i] == Localization.BaseLanguage) english = i;
            }
            return english;
        }
    }
}
