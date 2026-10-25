using System;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Maps
{
    /// <summary>
    /// Dresses the map in its game's season (the menu's theme for a new game, the save's for a loaded
    /// one -- Session; the user, 2026-10-24/25: a style for each season, picked in the menu). Nothing is
    /// remodelled -- the season is materials swapped as the map is laid out:
    ///
    /// - the ground, relief and decor draw through the season's terrain palette;
    /// - each broadleaf tree wears one of the season's crown palettes, picked by where it stands, so
    ///   an autumn wood is gold, orange and red; a conifer wears the season's needles;
    /// - winter sets the building shader's _SeasonSnow, a white cap on whatever faces up (roofs,
    ///   crowns); the figures wear a copy of the material with no snow on it;
    /// - the sun takes the season's tint.
    /// </summary>
    public static class SeasonStyle
    {
        private static SeasonLooks _looks;
        private static bool _searched;
        private static readonly int SeasonSnowId = Shader.PropertyToID("_SeasonSnow");

        /// <summary>The season being laid out: a test may pin one without touching the player's setting.</summary>
        public static MapSeason? Override { get; set; }

        /// <summary>The season of the map being laid out when the map has one of its own (MeshMapDefinition.ForcedSeason).</summary>
        public static MapSeason? MapForced { get; set; }

        /// <summary>The game's own season: set as its map is laid out (MeshMapApplier), from the save or the menu's theme.</summary>
        public static MapSeason Session { get; set; } = MapSeason.Summer;

        public static MapSeason Current => Override ?? MapForced ?? Session;

        private static SeasonLooks Looks
        {
            get
            {
                if (!_searched)
                {
                    _searched = true;
                    _looks = UnityEngine.Resources.Load<SeasonLooks>("SeasonLooks");
                }
                return _looks;
            }
        }

        private static SeasonLooks.Look CurrentLook
        {
            get
            {
                var looks = Looks;
                var index = (int)Current;
                return looks != null && index < looks.looks.Length ? looks.looks[index] : null;
            }
        }

        /// <summary>The scene-wide part: winter's snow and the sun's tint. Called as a map is laid out.</summary>
        public static void ApplyToScene()
        {
            var look = CurrentLook;
            Shader.SetGlobalFloat(SeasonSnowId, look != null ? look.snow : 0f);
            var sun = RenderSettings.sun;
            if (sun == null)
            {
                foreach (var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
                {
                    if (light.type == LightType.Directional) { sun = light; break; }
                }
            }
            if (sun == null) return;
            var tint = sun.GetComponent<SeasonSunTint>();
            if (tint == null) tint = sun.gameObject.AddComponent<SeasonSunTint>();
            tint.Apply(look != null ? look.sunTint : Color.white);
        }

        /// <summary>The season's stand-in for the map's terrain material; anything else is returned as it is.</summary>
        public static Material TerrainMaterial(Material material)
        {
            var looks = Looks;
            var look = CurrentLook;
            if (looks == null || look == null || look.terrain == null || material != looks.terrainSummer) return material;
            return look.terrain;
        }

        /// <summary>Puts the season's terrain palette on every renderer under `root` that draws with the map's own.</summary>
        public static void DressTerrain(GameObject root)
        {
            if (root == null) return;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var material = renderer.sharedMaterial;
                var dressed = TerrainMaterial(material);
                if (dressed != material) renderer.sharedMaterial = dressed;
            }
        }

        /// <summary>
        /// A tree in the season's crowns: `model` is its model's name (Tree1..Tree10), `seed` where
        /// it stands -- the same spot always gets the same of a season's colours.
        /// </summary>
        public static void DressTree(GameObject tree, string model, int seed)
        {
            var looks = Looks;
            var look = CurrentLook;
            if (tree == null || looks == null || look == null) return;
            var conifer = Array.IndexOf(looks.conifers, model) >= 0;
            Material crown;
            if (conifer) crown = look.conifer;
            else if (look.broadleaf != null && look.broadleaf.Length > 0)
                crown = look.broadleaf[(int)((uint)seed % (uint)look.broadleaf.Length)];
            else crown = null;
            if (crown == null) return;
            Swap(tree, looks.buildingsPalette, crown);
        }

        /// <summary>A unit's figure: the buildings' material without the snow, so winter does not whiten a helmet or a shoulder.</summary>
        public static void NoSnow(GameObject figure)
        {
            var looks = Looks;
            if (figure == null || looks == null || looks.figures == null) return;
            Swap(figure, looks.buildingsPalette, looks.figures);
        }

        private static void Swap(GameObject root, Material from, Material to)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.sharedMaterial == from) renderer.sharedMaterial = to;
            }
        }
    }
}
