using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CityBuilder.Maps.Archipelago
{
    /// <summary>
    /// The library's islands with their meshes (IslandAsset), in two parts since the small first download (the user,
    /// 2026-11-28): Resources/ArchipelagoLibrary holds the INSTALL's -- the tutorial's island and the islands of the
    /// start map (archipelago_start_seed) -- and <see cref="LaterPath"/> (Addressable, the fast-follow pack) the rest.
    /// The generator lays maps out from ArchipelagoPreviewLibrary (every island's cells, in the install) and looks the
    /// meshes up here by name; <see cref="Find"/> is null for an island of the pack not downloaded yet.
    /// </summary>
    public class ArchipelagoLibrary : ScriptableObject
    {
        public const string ResourcePath = "ArchipelagoLibrary";

        /// <summary>The rest of the islands: an Addressable asset of the fast-follow pack (address <see cref="LaterAddress"/>).</summary>
        public const string LaterPath = MeshMapCatalog.LaterFolder + "/ArchipelagoLibraryLater.asset";
        public const string LaterAddress = "ArchipelagoLibraryLater";

        [SerializeField] private IslandAsset[] islands = new IslandAsset[0];

        public IslandAsset[] Islands => islands;

        private static ArchipelagoLibrary _loaded;
        private static Dictionary<string, IslandAsset> _byName;
        private static bool _withLater;
        private static bool _laterFailed;

        /// <summary>The install's part, or null when no island has been made yet (the generator then draws its own shapes).</summary>
        public static ArchipelagoLibrary Load()
        {
            if (_loaded == null) _loaded = UnityEngine.Resources.Load<ArchipelagoLibrary>(ResourcePath);
            return _loaded != null && _loaded.islands.Length > 0 ? _loaded : null;
        }

        /// <summary>The island of this name with its meshes, or null (not made, or in the pack not downloaded yet).</summary>
        public static IslandAsset Find(string islandName)
        {
            if (string.IsNullOrEmpty(islandName)) return null;
            EnsureNames();
            return _byName.TryGetValue(islandName, out var asset) ? asset : null;
        }

        /// <summary>Can the map of this seed be built now: is every island it places on the phone?</summary>
        public static bool CanBuild(int seed, int version = ArchipelagoGenerator.Version)
        {
            var map = ArchipelagoGenerator.Generate(seed, version, preview: true);
            foreach (var island in map.Islands)
                if (island.Source != null && Find(island.Source.IslandName) == null) return false;
            return true;
        }

        private static void EnsureNames()
        {
            if (_byName != null && (_withLater || !ContentPacks.Ready || _laterFailed)) return;
            _byName = new Dictionary<string, IslandAsset>();
            Add(Load());
            _withLater = ContentPacks.Ready || Application.isEditor;
            if (!_withLater) return;
#if UNITY_EDITOR
            Add(UnityEditor.AssetDatabase.LoadAssetAtPath<ArchipelagoLibrary>(LaterPath));
#else
            try { Add(Addressables.LoadAssetAsync<ArchipelagoLibrary>(LaterAddress).WaitForCompletion()); }
            catch (System.Exception e)
            {
                Debug.LogWarning("ArchipelagoLibrary: the later islands did not load: " + e.Message);
                _withLater = false;
                _laterFailed = true;
            }
#endif
        }

        private static void Add(ArchipelagoLibrary library)
        {
            if (library == null) return;
            foreach (var island in library.islands)
                if (island != null) _byName[island.IslandName] = island;
        }

#if UNITY_EDITOR
        public void EditorInitialize(IslandAsset[] all) => islands = all;
#endif
    }
}
