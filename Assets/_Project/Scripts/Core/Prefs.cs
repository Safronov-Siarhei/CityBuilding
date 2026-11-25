using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Core
{
    /// <summary>
    /// Every PlayerPrefs WRITE of the game goes through here (reads stay PlayerPrefs.Get*). Unity cannot list the keys
    /// PlayerPrefs holds, so this keeps the list itself -- each written key with its type, in one key of its own --
    /// and the cloud save (CloudSave, 2026-11-25: the user, «весь прогресс аккаунта» into Google Play Games) builds its
    /// snapshot from that list. A key never written through here is invisible to the cloud.
    /// </summary>
    public static class Prefs
    {
        /// <summary>The list itself: lines «i:key», «f:key», «s:key».</summary>
        public const string IndexKey = "prefs.index";

        private static Dictionary<string, char> _index;

        /// <summary>Every key written so far and its type: 'i' int, 'f' float, 's' string.</summary>
        public static IReadOnlyDictionary<string, char> Index
        {
            get { Load(); return _index; }
        }

        public static void SetInt(string key, int value) { PlayerPrefs.SetInt(key, value); Note(key, 'i'); }

        public static void SetFloat(string key, float value) { PlayerPrefs.SetFloat(key, value); Note(key, 'f'); }

        public static void SetString(string key, string value) { PlayerPrefs.SetString(key, value); Note(key, 's'); }

        public static void DeleteKey(string key)
        {
            PlayerPrefs.DeleteKey(key);
            Load();
            if (_index.Remove(key)) SaveIndex();
        }

        /// <summary>Read the list again from PlayerPrefs (after the cloud snapshot was put in place).</summary>
        public static void Reload() => _index = null;

        private static void Note(string key, char type)
        {
            Load();
            if (_index.TryGetValue(key, out var had) && had == type) return;
            _index[key] = type;
            SaveIndex();
        }

        private static void Load()
        {
            if (_index != null) return;
            _index = new Dictionary<string, char>();
            var text = PlayerPrefs.GetString(IndexKey, "");
            foreach (var line in text.Split('\n'))
                if (line.Length > 2 && line[1] == ':') _index[line.Substring(2)] = line[0];
        }

        private static void SaveIndex()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var pair in _index) sb.Append(pair.Value).Append(':').Append(pair.Key).Append('\n');
            PlayerPrefs.SetString(IndexKey, sb.ToString());
        }
    }
}
