using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CityBuilder.Core
{
    /// <summary>The player's graphics choice: Auto picks a tier from the device (GraphicsQuality.Detect).</summary>
    public enum GraphicsTier
    {
        Auto = 0,
        Low = 1,
        Medium = 2,
        High = 3,
    }

    /// <summary>
    /// Quality tiers (the user, 2026-11-09: effects that look fine on the PC looked poor on the phone).
    /// A tier changes only HOW the world is drawn -- resolution, edge smoothing, shadow sharpness and
    /// reach -- never WHAT is in it (project rule for the device fleet).
    ///
    /// Applied to a private COPY of the active URP asset, so neither play in the editor nor a tier
    /// switch ever writes into Assets/Settings. Chosen in the main menu's settings, saved in
    /// PlayerPrefs; Auto reads the device's memory and cores.
    ///
    /// The numbers are rendering technique, not game balance, so they live here (like the camera's
    /// feel constants) rather than in the sheet.
    /// </summary>
    public static class GraphicsQuality
    {
        private const string Key = "graphics_tier";

        // tier: render scale, the cap on the short screen side in pixels, MSAA samples, shadow map, shadow reach
        private static readonly (float scale, int shortSideCap, int msaa, int shadowMap, float shadowDistance)[] Tiers =
        {
            // Low at the screen's own resolution: on the Redmi Pad (Mali-G57) 720p and native 1200p
            // both drew 54 fps, and 720p stretched to the screen shimmered (the user, 2026-11-10);
            // what costs there is MSAA 4x (54 -> 31 fps at any scale).
            // 2026-11-10, three phones (the user: "all points"): 4x took Medium's 6 GB phones down to
            // 21-27 fps (Redmi Note 14 / Mali-G57, Samsung A71 / Adreno 618); 2x is free on Mali (54)
            // but not on Adreno (39), off is 54 on both; scale 0.9 vs 1.0 changed nothing. So Medium
            // is native with 2x and Low native with no MSAA.
            (1f, 1200, 1, 1024, 35f),     // Low
            (1f, 1080, 2, 1024, 50f),     // Medium
            (1f, 1440, 4, 2048, 60f),     // High
        };

        private static GraphicsTier? _chosen;
        private static UniversalRenderPipelineAsset _source, _copy;

        // Auto's memory bands in MB (SystemInfo reports less than the box says: a "4 GB" Redmi Pad
        // reads ~3 800). Measured 2026-11-10: that tablet (Helio G99) drew 21 fps on Medium and
        // 54 on Low, so a 4 GB device is Low; 6 GB and up with 8 cores Medium, 8 GB+ High.
        private const int LowBelowMb = 4500;
        private const int HighFromMb = 7000;

        // ---- the manual settings (the user, 2026-11-10: Auto/Manual with a graphics level and
        // the details under it). Chosen == Auto is the Auto mode; any level is the Manual mode, and the
        // details start from that level's preset and may then be changed one by one.

        /// <summary>One manual setup: frames a second, MSAA samples, render scale in %, shadows (0 off, 1 simple, 2 sharp), shadow reach (0 near .. 2 far).</summary>
        public struct Detail
        {
            public int Fps, Msaa, ScalePercent, Shadows, ShadowReach;

            public bool Same(Detail o) =>
                Fps == o.Fps && Msaa == o.Msaa && ScalePercent == o.ScalePercent && Shadows == o.Shadows && ShadowReach == o.ShadowReach;
        }

        public static readonly int[] FpsChoices = { 30, 60 };
        public static readonly int[] MsaaChoices = { 1, 2, 4 };
        public static readonly int[] ScaleChoices = { 50, 75, 90, 100 };
        private static readonly int[] ShadowMaps = { 1024, 1024, 2048 };
        private static readonly float[] ShadowReaches = { 35f, 50f, 60f };

        /// <summary>The details a level stands for -- what picking it in Manual sets.</summary>
        public static Detail Preset(GraphicsTier tier)
        {
            switch (tier)
            {
                case GraphicsTier.High: return new Detail { Fps = 60, Msaa = 4, ScalePercent = 100, Shadows = 2, ShadowReach = 2 };
                case GraphicsTier.Medium: return new Detail { Fps = 60, Msaa = 2, ScalePercent = 100, Shadows = 1, ShadowReach = 1 };
                default: return new Detail { Fps = 60, Msaa = 1, ScalePercent = 100, Shadows = 1, ShadowReach = 0 };
            }
        }

        private const string DetailKey = "graphics_detail.";
        private static Detail? _detail;

        public static bool Manual => Chosen != GraphicsTier.Auto;

        /// <summary>The manual details (the chosen level's preset until the player changed one).</summary>
        public static Detail Current
        {
            get
            {
                if (_detail == null)
                {
                    var preset = Preset(Chosen);
                    _detail = new Detail
                    {
                        Fps = PlayerPrefs.GetInt(DetailKey + "fps", preset.Fps),
                        Msaa = PlayerPrefs.GetInt(DetailKey + "msaa", preset.Msaa),
                        ScalePercent = PlayerPrefs.GetInt(DetailKey + "scale", preset.ScalePercent),
                        Shadows = PlayerPrefs.GetInt(DetailKey + "shadows", preset.Shadows),
                        ShadowReach = PlayerPrefs.GetInt(DetailKey + "reach", preset.ShadowReach),
                    };
                }
                return _detail.Value;
            }
            set
            {
                _detail = value;
                PlayerPrefs.SetInt(DetailKey + "fps", value.Fps);
                PlayerPrefs.SetInt(DetailKey + "msaa", value.Msaa);
                PlayerPrefs.SetInt(DetailKey + "scale", value.ScalePercent);
                PlayerPrefs.SetInt(DetailKey + "shadows", value.Shadows);
                PlayerPrefs.SetInt(DetailKey + "reach", value.ShadowReach);
                PlayerPrefs.Save();
                Apply();
            }
        }

        /// <summary>The level whose preset the manual details are, or null for the player's own mix ("custom").</summary>
        public static GraphicsTier? MatchingLevel
        {
            get
            {
                var d = Current;
                for (var t = GraphicsTier.Low; t <= GraphicsTier.High; t++)
                    if (Preset(t).Same(d)) return t;
                return null;
            }
        }

        /// <summary>Manual on: starts from the device's own level. Off: back to Auto (the details are kept for next time).</summary>
        public static void SetManual(bool manual)
        {
            if (manual == Manual) return;
            if (!manual) { Chosen = GraphicsTier.Auto; return; }
            if (!PlayerPrefs.HasKey(DetailKey + "fps")) { SetLevel(Detect()); return; }
            // the details the player left last time, under the level they match
            Chosen = MatchingLevel ?? Detect();
        }

        /// <summary>A level in Manual: every detail to its preset.</summary>
        public static void SetLevel(GraphicsTier tier)
        {
            if (tier == GraphicsTier.Auto) { Chosen = GraphicsTier.Auto; return; }
            _chosen = tier;
            PlayerPrefs.SetInt(Key, (int)tier);
            Current = Preset(tier);   // saves and applies
        }

        /// <summary>Frames a second the game is capped at: Manual's choice, 60 in Auto.</summary>
        public static int FrameRate => Manual ? Current.Fps : 60;

        // ---- the FPS counter (the user: it can be switched on in the settings)
        private const string ShowFpsKey = "graphics_show_fps";
        public static event System.Action ShowFpsChanged;

        public static bool ShowFps
        {
            get => PlayerPrefs.GetInt(ShowFpsKey, 0) == 1;
            set
            {
                if (value == ShowFps) return;
                PlayerPrefs.SetInt(ShowFpsKey, value ? 1 : 0);
                PlayerPrefs.Save();
                ShowFpsChanged?.Invoke();
            }
        }

        public static GraphicsTier Chosen
        {
            get
            {
                if (_chosen == null)
                {
                    var saved = PlayerPrefs.GetInt(Key, (int)GraphicsTier.Auto);
                    _chosen = System.Enum.IsDefined(typeof(GraphicsTier), saved) ? (GraphicsTier)saved : GraphicsTier.Auto;
                }
                return _chosen.Value;
            }
            set
            {
                if (_chosen == value) return;
                _chosen = value;
                PlayerPrefs.SetInt(Key, (int)value);
                PlayerPrefs.Save();
                Apply();
            }
        }

        /// <summary>The tier actually drawn: the device's in Auto; in Manual the level the details match (the chosen one for a mix).</summary>
        public static GraphicsTier Effective => !Manual ? Detect() : MatchingLevel ?? Chosen;

        /// <summary>
        /// The device's tier from its memory and cores: a flagship (8+ GB, 8 cores) draws High, a
        /// 4 GB device or less Low, the rest Medium. A desktop is High.
        /// </summary>
        public static GraphicsTier Detect()
        {
            if (!Application.isMobilePlatform) return GraphicsTier.High;
            var memory = SystemInfo.systemMemorySize;
            if (memory > 0 && memory < LowBelowMb) return GraphicsTier.Low;
            if (memory >= HighFromMb && SystemInfo.processorCount >= 8) return GraphicsTier.High;
            return GraphicsTier.Medium;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyAtStart() => Apply();

        // a cheat's override for tuning a tier on a device (CheatsPanel, «Служебное»): 0 / null = the tier's own
        public static float OverrideScale;
        public static int OverrideMsaa;
        public static UpscalingFilterSelection? OverrideFilter;

        /// <summary>The render scale last applied (for the cheat window's readout).</summary>
        public static float AppliedScale => _copy != null ? _copy.renderScale : 1f;
        public static int AppliedMsaa => _copy != null ? _copy.msaaSampleCount : 1;
        public static UpscalingFilterSelection AppliedFilter => _copy != null ? _copy.upscalingFilter : UpscalingFilterSelection.Auto;

        /// <summary>
        /// Draws with the effective tier from now on. Every call builds a FRESH copy of the project's
        /// URP asset and assigns it: on the device, editing the live copy's render scale / MSAA did
        /// nothing until the game restarted (Redmi Pad 2026-11-10: Low picked in-game stayed at 21 fps,
        /// the same Low after a restart drew 54). Assigning a new asset makes URP rebuild its pipeline.
        /// </summary>
        public static void Apply()
        {
            if (_source == null)
            {
                _source = (QualitySettings.renderPipeline ?? GraphicsSettings.defaultRenderPipeline) as UniversalRenderPipelineAsset;
                if (_source == null) return;
            }
            var old = _copy;
            _copy = Object.Instantiate(_source);
            _copy.name = _source.name + " (tier)";

            if (!Manual)
            {
                var tier = Tiers[Mathf.Clamp((int)Detect() - 1, 0, Tiers.Length - 1)];
                // a 1440p phone at full scale would pay for pixels nobody sees: the short side is capped
                var shortSide = Mathf.Max(1, Mathf.Min(Screen.width, Screen.height));
                _copy.renderScale = Mathf.Clamp(Mathf.Min(tier.scale, tier.shortSideCap / (float)shortSide), 0.5f, 1f);
                _copy.msaaSampleCount = tier.msaa;
                _copy.mainLightShadowmapResolution = tier.shadowMap;
                _copy.shadowDistance = tier.shadowDistance;
            }
            else
            {
                var d = Current;
                _copy.renderScale = Mathf.Clamp(d.ScalePercent / 100f, 0.5f, 1f);
                _copy.msaaSampleCount = Mathf.Max(1, d.Msaa);
                var shadows = Mathf.Clamp(d.Shadows, 0, ShadowMaps.Length - 1);
                _copy.mainLightShadowmapResolution = ShadowMaps[shadows];
                // a zero reach is URP's "no main light shadows"
                _copy.shadowDistance = shadows == 0 ? 0f : ShadowReaches[Mathf.Clamp(d.ShadowReach, 0, ShadowReaches.Length - 1)];
            }
            Application.targetFrameRate = FrameRate;
            // no post effects in the game: HDR buys nothing but bandwidth
            _copy.supportsHDR = false;
            if (OverrideScale > 0f) _copy.renderScale = Mathf.Clamp(OverrideScale, 0.3f, 1f);
            if (OverrideMsaa > 0) _copy.msaaSampleCount = OverrideMsaa;
            if (OverrideFilter.HasValue) _copy.upscalingFilter = OverrideFilter.Value;

            QualitySettings.renderPipeline = _copy;
            if (old != null) Object.Destroy(old);
        }
    }
}
