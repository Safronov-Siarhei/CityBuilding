using System;
using UnityEngine;

namespace CityBuilder.Core
{
    /// <summary>
    /// Looks a key up in the player's chosen language.
    ///
    /// Static rather than a MonoBehaviour singleton on purpose: text is asked for from everywhere,
    /// including from objects that run before any scene manager has woken up, and a missing
    /// Instance would mean a game full of blank labels. The config is loaded from Resources on
    /// first use and never touched again.
    ///
    /// A missing key renders as the key itself ("ui.close"), which is deliberately ugly: an
    /// untranslated string should be obvious on the screen of whoever is testing, not silently
    /// blank or silently English.
    /// </summary>
    public static class Localization
    {
        private const string LanguagePrefsKey = "CityBuilder.Language";

        /// <summary>Raised when the player picks another language, so every visible label can re-read itself (see LocalizedText).</summary>
        public static event Action OnLanguageChanged;

        private static LocalizationConfig _config;
        private static string _language;
        private static int _languageIndex;

        public static LocalizationConfig Config
        {
            get
            {
                if (_config == null)
                {
                    _config = UnityEngine.Resources.Load<LocalizationConfig>(LocalizationConfig.ResourcePath);
                    if (_config == null)
                    {
                        Debug.LogError($"Localization: no LocalizationConfig at Resources/{LocalizationConfig.ResourcePath}. " +
                                       "Rebuild it from the CSVs (CityBuilder/Balance/Rebuild Config From CSV). Every label will show its key.");
                    }
                    ResolveLanguage();
                }
                return _config;
            }
        }

        /// <summary>The language code in use ("ru", "en"). Remembered across sessions in PlayerPrefs.</summary>
        public static string Language
        {
            get
            {
                if (_language == null) { var _ = Config; }
                return _language;
            }
        }

        public static void SetLanguage(string code)
        {
            if (string.IsNullOrEmpty(code) || code == Language) return;

            var config = Config;
            if (config == null) return;

            var index = config.IndexOfLanguage(code);
            if (index < 0)
            {
                Debug.LogWarning($"Localization: the sheet has no column for '{code}'. Staying on '{_language}'.");
                return;
            }

            _language = code;
            _languageIndex = index;
            PlayerPrefs.SetString(LanguagePrefsKey, code);
            PlayerPrefs.Save();
            OnLanguageChanged?.Invoke();
        }

        /// <summary>The text for a key, or the key itself when the sheet does not have it.</summary>
        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;

            var config = Config;
            var value = config != null ? config.Find(key, _languageIndex) : null;
            return value ?? key;
        }

        /// <summary>
        /// The text for a key, falling back to something already sensible rather than to the key.
        /// For names that the balance sheet also carries in Russian (a building's display_name):
        /// a row missing from the localization tab then reads as the Russian name instead of
        /// "building.Smelter", which matters because those rows are added building by building.
        /// </summary>
        public static string GetOrDefault(string key, string fallback)
        {
            var config = Config;
            var value = config != null ? config.Find(key, _languageIndex) : null;
            return !string.IsNullOrEmpty(value) ? value : fallback;
        }

        /// <summary>
        /// A key whose text carries {0}, {1} placeholders -- "Рабочие: {0} / {1}". Kept separate
        /// from Get so a translator can see at a glance which strings must keep their braces, and
        /// so a stray brace in an ordinary string can never be read as a format specifier.
        /// </summary>
        public static string Format(string key, params object[] args)
        {
            var text = Get(key);
            if (args == null || args.Length == 0) return text;

            try
            {
                return string.Format(text, args);
            }
            catch (FormatException)
            {
                // A translator typed the braces wrong. Showing the raw string beats throwing from
                // inside a UI refresh, which would take the whole panel down.
                Debug.LogError($"Localization: '{key}' has malformed placeholders for {args.Length} argument(s): \"{text}\"");
                return text;
            }
        }

        /// <summary>Which language to start in: what the player last chose, else the system language if the sheet has it, else the sheet's first column.</summary>
        private static void ResolveLanguage()
        {
            var fallback = _config != null && _config.Languages.Count > 0 ? _config.Languages[0] : "ru";

            var saved = PlayerPrefs.GetString(LanguagePrefsKey, string.Empty);
            var candidate = !string.IsNullOrEmpty(saved) ? saved : SystemLanguageCode();

            _languageIndex = _config != null ? _config.IndexOfLanguage(candidate) : -1;
            if (_languageIndex < 0)
            {
                _language = fallback;
                _languageIndex = 0;
                return;
            }
            _language = candidate;
        }

        private static string SystemLanguageCode()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Russian: return "ru";
                case SystemLanguage.English: return "en";
                default: return string.Empty;
            }
        }

        /// <summary>Drops the cached config and language. For tests, which load a fresh scene per run and must not inherit the previous one's choice.</summary>
        public static void ResetForTests()
        {
            _config = null;
            _language = null;
            _languageIndex = 0;
        }
    }
}
