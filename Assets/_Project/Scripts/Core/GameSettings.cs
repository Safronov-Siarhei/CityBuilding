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
    /// The HUD's look, picked in the settings (see UI.HudSkinApplier). Classic, Soft and Glass
    /// differ only in how the pieces are drawn; Dock also moves them -- the build menu sits in a
    /// wooden dock along the bottom and the resources on a rail along the top.
    /// </summary>
    public enum HudSkin
    {
        /// <summary>А: parchment pills, dark wood and gold, round buttons.</summary>
        Classic = 0,

        /// <summary>В: light panels without outlines, soft shadows, orange and blue buttons.</summary>
        Soft = 1,

        /// <summary>Г: translucent dark capsules with a gold hairline.</summary>
        Glass = 2,

        /// <summary>Д: Classic's wood, laid out as a top rail and a bottom dock.</summary>
        Dock = 3,
    }

    /// <summary>
    /// The season the maps are dressed in (Maps.SeasonStyle; Tools/blender/seasons.py). The user,
    /// 2026-10-24: a style per season now, how the player comes by them is decided later -- until
    /// then it is a plain choice in the settings.
    /// </summary>
    public enum MapSeason
    {
        Summer = 0,
        Autumn = 1,
        Winter = 2,
        Spring = 3,
    }

    /// <summary>
    /// Which map an Invasion run is played on (Invasion.InvasionMode.RunMapId). The user,
    /// 2026-10-25: the New Year map is a plain choice in the settings for now; when it is open is
    /// decided later.
    /// </summary>
    public enum InvasionMap
    {
        Island = 0,
        NewYear = 1,
    }

    /// <summary>
    /// The player's own preferences that are not the language (Localization keeps that one), kept
    /// across sessions in PlayerPrefs. A change is announced, so whatever draws by a setting can
    /// follow it at once.
    /// </summary>
    public static class GameSettings
    {
        // The ".v2" is deliberate: the default changed from clouds to parchment (2026-10-14), and
        // a fresh key is what makes everyone who tried the switch while clouds were the default
        // start on parchment once. Nobody had a real choice to lose -- the game is not released.
        private const string FogStyleKey = "settings.fog_style.v2";

        /// <summary>What the fog looks like until the player picks otherwise.</summary>
        public const FogStyle DefaultFogStyle = FogStyle.Parchment;

        private const string HudSkinKey = "settings.hud_skin";

        /// <summary>The HUD's look until the player picks another.</summary>
        public const HudSkin DefaultHudSkin = HudSkin.Classic;

        private const string MapSeasonKey = "settings.map_season";
        private const string InvasionMapKey = "settings.invasion_map";
        private static bool _invasionMapLoaded;
        private static InvasionMap _invasionMap;

        /// <summary>The map the next Invasion run is played on.</summary>
        public static InvasionMap InvasionMap
        {
            get
            {
                if (!_invasionMapLoaded)
                {
                    _invasionMapLoaded = true;
                    var saved = PlayerPrefs.GetInt(InvasionMapKey, (int)InvasionMap.Island);
                    _invasionMap = Enum.IsDefined(typeof(InvasionMap), saved) ? (InvasionMap)saved : InvasionMap.Island;
                }
                return _invasionMap;
            }
            set
            {
                if (_invasionMapLoaded && _invasionMap == value) return;
                _invasionMapLoaded = true;
                _invasionMap = value;
                PlayerPrefs.SetInt(InvasionMapKey, (int)value);
                PlayerPrefs.Save();
                OnChanged?.Invoke();
            }
        }

        private static bool _loaded;
        private static FogStyle _fogStyle;
        private static bool _seasonLoaded;
        private static MapSeason _season;
        private static bool _hudSkinLoaded;
        private static HudSkin _hudSkin;

        public static event Action OnChanged;

        public static FogStyle FogStyle
        {
            get
            {
                if (!_loaded)
                {
                    _loaded = true;
                    var saved = PlayerPrefs.GetInt(FogStyleKey, (int)DefaultFogStyle);
                    _fogStyle = Enum.IsDefined(typeof(FogStyle), saved) ? (FogStyle)saved : DefaultFogStyle;
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

        public static HudSkin HudSkin
        {
            get
            {
                if (!_hudSkinLoaded)
                {
                    _hudSkinLoaded = true;
                    var saved = PlayerPrefs.GetInt(HudSkinKey, (int)DefaultHudSkin);
                    _hudSkin = Enum.IsDefined(typeof(HudSkin), saved) ? (HudSkin)saved : DefaultHudSkin;
                }
                return _hudSkin;
            }
            set
            {
                if (_hudSkinLoaded && _hudSkin == value) return;
                _hudSkinLoaded = true;
                _hudSkin = value;
                PlayerPrefs.SetInt(HudSkinKey, (int)value);
                PlayerPrefs.Save();
                OnChanged?.Invoke();
            }
        }

        /// <summary>The season the next map is dressed in; summer, today's look, until the player picks another.</summary>
        public static MapSeason MapSeason
        {
            get
            {
                if (!_seasonLoaded)
                {
                    _seasonLoaded = true;
                    var saved = PlayerPrefs.GetInt(MapSeasonKey, (int)MapSeason.Summer);
                    _season = Enum.IsDefined(typeof(MapSeason), saved) ? (MapSeason)saved : MapSeason.Summer;
                }
                return _season;
            }
            set
            {
                if (_seasonLoaded && _season == value) return;
                _seasonLoaded = true;
                _season = value;
                PlayerPrefs.SetInt(MapSeasonKey, (int)value);
                PlayerPrefs.Save();
                OnChanged?.Invoke();
            }
        }

        /// <summary>Forgets the cached values so the next read comes from PlayerPrefs again -- for tests.</summary>
        public static void ReloadForTests()
        {
            _loaded = false;
            _hudSkinLoaded = false;
            _seasonLoaded = false;
            _invasionMapLoaded = false;
        }
    }
}
