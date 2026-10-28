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
    /// How a building's construction ends (Buildings.ConstructionLook; the user, 2026-10-28: both
    /// made in the game to pick from): the site with its scaffolding goes and the building stands.
    /// </summary>
    public enum BuildFinish
    {
        /// <summary>A cloud of dust, the scaffold's planks thrown off, the building springs up.</summary>
        Dust = 0,

        /// <summary>A flash and a ring of gold sparks, the building pops up out of it.</summary>
        Fanfare = 1,
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
    /// The look a map is played in, picked in the menu's sandbox and invasion screens (the user,
    /// 2026-10-25: the round theme buttons; replaces the settings' season and invasion-map rows).
    /// The four seasons dress any map (Maps.SeasonStyle); the New Year is its own Invasion map.
    /// Ordered as the buttons stand.
    /// </summary>
    public enum MapTheme
    {
        Spring = 0,
        Summer = 1,
        Autumn = 2,
        Winter = 3,
        NewYear = 4,
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

        private const string BuildFinishKey = "settings.build_finish";
        private static BuildFinish? _buildFinish;

        /// <summary>How construction ends (the settings' row): dust until the player picks otherwise.</summary>
        public static BuildFinish BuildFinish
        {
            get
            {
                if (_buildFinish == null)
                {
                    var saved = PlayerPrefs.GetInt(BuildFinishKey, (int)BuildFinish.Dust);
                    _buildFinish = Enum.IsDefined(typeof(BuildFinish), saved) ? (BuildFinish)saved : BuildFinish.Dust;
                }
                return _buildFinish.Value;
            }
            set
            {
                if (_buildFinish == value) return;
                _buildFinish = value;
                PlayerPrefs.SetInt(BuildFinishKey, (int)value);
                PlayerPrefs.Save();
                OnChanged?.Invoke();
            }
        }

        private const string SandboxThemeKey = "menu.sandbox_theme";
        private const string InvasionThemeKey = "menu.invasion_theme";
        private static MapTheme? _sandboxTheme, _invasionTheme;

        /// <summary>The theme last picked in the sandbox screen: the next new game's.</summary>
        public static MapTheme SandboxTheme
        {
            get => _sandboxTheme ??= LoadTheme(SandboxThemeKey);
            set => SaveTheme(SandboxThemeKey, ref _sandboxTheme, value);
        }

        /// <summary>The theme last picked in the invasion screen: the next run's (InvasionMode.RunMapId).</summary>
        public static MapTheme InvasionTheme
        {
            get => _invasionTheme ??= LoadTheme(InvasionThemeKey);
            set => SaveTheme(InvasionThemeKey, ref _invasionTheme, value);
        }

        private static MapTheme LoadTheme(string key)
        {
            var saved = PlayerPrefs.GetInt(key, (int)MapTheme.Summer);
            return Enum.IsDefined(typeof(MapTheme), saved) ? (MapTheme)saved : MapTheme.Summer;
        }

        private static void SaveTheme(string key, ref MapTheme? cached, MapTheme value)
        {
            if (cached == value) return;
            cached = value;
            PlayerPrefs.SetInt(key, (int)value);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }

        private static bool _loaded;
        private static FogStyle _fogStyle;
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

        /// <summary>Forgets the cached values so the next read comes from PlayerPrefs again -- for tests.</summary>
        public static void ReloadForTests()
        {
            _loaded = false;
            _hudSkinLoaded = false;
            _sandboxTheme = null;
            _invasionTheme = null;
            _buildFinish = null;
        }
    }
}
