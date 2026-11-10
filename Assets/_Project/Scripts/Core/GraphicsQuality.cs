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
            (0.75f, 720, 2, 1024, 35f),   // Low
            (0.9f, 1080, 4, 1024, 50f),   // Medium
            (1f, 1440, 4, 2048, 60f),     // High
        };

        private static GraphicsTier? _chosen;
        private static UniversalRenderPipelineAsset _source, _copy;

        // Auto's memory bands in MB (SystemInfo reports less than the box says: a "4 GB" Redmi Pad
        // reads ~3 800). Measured 2026-11-10: that tablet (Helio G99) drew 21 fps on Medium and
        // 54 on Low, so a 4 GB device is Low; 6 GB and up with 8 cores Medium, 8 GB+ High.
        private const int LowBelowMb = 4500;
        private const int HighFromMb = 7000;

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

        /// <summary>The tier actually drawn: the chosen one, or the device's for Auto.</summary>
        public static GraphicsTier Effective => Chosen == GraphicsTier.Auto ? Detect() : Chosen;

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

            var tier = Tiers[Mathf.Clamp((int)Effective - 1, 0, Tiers.Length - 1)];
            // a 1440p phone at full scale would pay for pixels nobody sees: the short side is capped
            var shortSide = Mathf.Max(1, Mathf.Min(Screen.width, Screen.height));
            _copy.renderScale = Mathf.Clamp(Mathf.Min(tier.scale, tier.shortSideCap / (float)shortSide), 0.5f, 1f);
            _copy.msaaSampleCount = tier.msaa;
            _copy.mainLightShadowmapResolution = tier.shadowMap;
            _copy.shadowDistance = tier.shadowDistance;
            // no post effects in the game: HDR buys nothing but bandwidth
            _copy.supportsHDR = false;

            QualitySettings.renderPipeline = _copy;
            if (old != null) Object.Destroy(old);
        }
    }
}
