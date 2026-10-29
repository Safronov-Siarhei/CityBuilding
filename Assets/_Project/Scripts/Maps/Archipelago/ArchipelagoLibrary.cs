using UnityEngine;

namespace CityBuilder.Maps.Archipelago
{
    /// <summary>Every island of the library, under Resources (ArchipelagoLibrary.asset).</summary>
    public class ArchipelagoLibrary : ScriptableObject
    {
        public const string ResourcePath = "ArchipelagoLibrary";

        [SerializeField] private IslandAsset[] islands = new IslandAsset[0];

        public IslandAsset[] Islands => islands;

        private static ArchipelagoLibrary _loaded;

        /// <summary>The library, or null when no island has been made yet (the generator then draws its own shapes).</summary>
        public static ArchipelagoLibrary Load()
        {
            if (_loaded == null) _loaded = UnityEngine.Resources.Load<ArchipelagoLibrary>(ResourcePath);
            return _loaded != null && _loaded.islands.Length > 0 ? _loaded : null;
        }

#if UNITY_EDITOR
        public void EditorInitialize(IslandAsset[] all) => islands = all;
#endif
    }
}
