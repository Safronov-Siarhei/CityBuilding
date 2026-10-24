using System;
using UnityEngine;

namespace CityBuilder.Maps
{
    /// <summary>
    /// What one season puts on a map (Resources/SeasonLooks.asset, built by SetupProject from
    /// Tools/blender/seasons.py's palettes): the ground's material, the trees' crowns, how much snow
    /// the building shader lays on, and the sun's tint. Summer's entry is today's look.
    /// </summary>
    public class SeasonLooks : ScriptableObject
    {
        [Serializable]
        public class Look
        {
            public Material terrain;
            public Material conifer;
            public Material[] broadleaf = new Material[0];
            [Range(0f, 1f)] public float snow;
            public Color sunTint = Color.white;
        }

        /// <summary>By MapSeason.</summary>
        public Look[] looks = new Look[4];

        /// <summary>The materials every map is imported with, which a season stands in for.</summary>
        public Material terrainSummer;
        public Material buildingsPalette;

        /// <summary>The buildings' material that never takes snow: the figures wear it.</summary>
        public Material figures;

        /// <summary>The tree models (by name) that keep their needles through autumn and winter.</summary>
        public string[] conifers = new string[0];
    }
}
