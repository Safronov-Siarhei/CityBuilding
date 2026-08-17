using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Core
{
    /// <summary>
    /// Every piece of text the player can read, in every language the sheet carries.
    ///
    /// Generated from the localization tab by BalanceImporter, exactly like BalanceConfig is
    /// generated from the other four -- same reasoning too: translations are content, and content
    /// belongs in the spreadsheet where it can be written and reviewed, not scattered through
    /// thirty C# files as string literals.
    /// </summary>
    public class LocalizationConfig : ScriptableObject
    {
        /// <summary>Resources path (no extension) the runtime loads this from.</summary>
        public const string ResourcePath = "LocalizationConfig";

        [Serializable]
        public class Entry
        {
            public string key = string.Empty;

            /// <summary>One string per language, index-aligned with <see cref="languages"/>. A blank means "not translated yet" and falls back to the first language.</summary>
            public List<string> values = new List<string>();
        }

        /// <summary>The language codes the sheet has columns for, in column order: ru, en, ... The first is the fallback for anything untranslated.</summary>
        [SerializeField] private List<string> languages = new List<string> { "ru" };

        [SerializeField] private List<Entry> entries = new List<Entry>();

        public IReadOnlyList<string> Languages => languages;

        /// <summary>
        /// The text for a key in a language, or null when the sheet has no such key. Null rather
        /// than the key itself so the caller can decide -- Localization shows the key, which makes
        /// a missing translation obvious on screen instead of silently blank.
        /// </summary>
        public string Find(string key, int languageIndex)
        {
            if (string.IsNullOrEmpty(key)) return null;

            foreach (var entry in entries)
            {
                if (entry.key != key) continue;

                if (languageIndex >= 0 && languageIndex < entry.values.Count)
                {
                    var value = entry.values[languageIndex];
                    if (!string.IsNullOrEmpty(value)) return value;
                }

                // Untranslated cell: fall back to the first language rather than to nothing, so a
                // half-finished English column shows Russian instead of holes.
                return entry.values.Count > 0 ? entry.values[0] : null;
            }
            return null;
        }

        public int IndexOfLanguage(string code)
        {
            for (var i = 0; i < languages.Count; i++)
            {
                if (string.Equals(languages[i], code, StringComparison.OrdinalIgnoreCase)) return i;
            }
            return -1;
        }

        /// <summary>Editor-side entry point for the CSV importer -- see BalanceImporter.</summary>
        public void OverwriteFrom(List<string> importedLanguages, List<Entry> importedEntries)
        {
            languages = importedLanguages;
            entries = importedEntries;
        }
    }
}
