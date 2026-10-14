using System;
using UnityEngine;

namespace CityBuilder.Core
{
    /// <summary>How the land the player has not explored yet is drawn (see Maps.FogOfWarManager).</summary>
    public enum FogStyle
    {
        /// <summary>Drifting white cloud.</summary>
        Clouds = 0,

        /// <summary>An old map on parchment: ink outlines of the rivers, hills and forests under it.</summary>
        Parchment = 1,
    }

    /// <summary>
    /// The player's own preferences that are not the language (Localization keeps that one), kept
    /// across sessions in PlayerPrefs. A change is announced, so whatever draws by a setting can
    /// follow it at once.
    /// </summary>
    public static class GameSettings
    {
        private const string FogStyleKey = "settings.fog_style";

        private static bool _loaded;
        private static FogStyle _fogStyle;

        public static event Action OnChanged;

        public static FogStyle FogStyle
        {
            get
            {
                if (!_loaded)
                {
                    _loaded = true;
                    var saved = PlayerPrefs.GetInt(FogStyleKey, (int)FogStyle.Clouds);
                    _fogStyle = Enum.IsDefined(typeof(FogStyle), saved) ? (FogStyle)saved : FogStyle.Clouds;
                }
                return _fogStyle;
            }
            set
            {
                if (_loaded && _fogStyle == value) return;
                _loaded = true;
                _fogStyle = value;
                PlayerPrefs.SetInt(FogStyleKey, (int)value);
                PlayerPrefs.Save();
                OnChanged?.Invoke();
            }
        }

        /// <summary>Forgets the cached value so the next read comes from PlayerPrefs again -- for tests.</summary>
        public static void ReloadForTests()
        {
            _loaded = false;
        }
    }
}
