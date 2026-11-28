using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CityBuilder.Maps
{
    /// <summary>
    /// Runtime lookup over every MeshMapDefinition: the install's under Resources/MeshMaps, plus -- once the later
    /// content is on the phone (ContentPacks.Ready) -- the ones in the fast-follow pack (label ContentPacks.MapLabel:
    /// the dungeons but the first, the user's small-download rule 2026-11-28). Cached; read again once when the pack
    /// arrives. A map of the pack asked for before then is null: the caller waits (LoadingScreen) or skips it.
    /// </summary>
    public static class MeshMapCatalog
    {
        private const string ResourcesFolder = "MeshMaps";

        /// <summary>Where SetupProject writes the fast-follow pack's map definitions (outside Resources).</summary>
        public const string LaterFolder = "Assets/_Project/Content/LaterMaps";

        private static Dictionary<string, MeshMapDefinition> _byId;
        private static bool _withLater;
        private static bool _laterFailed;

        public static List<MeshMapDefinition> All
        {
            get
            {
                EnsureLoaded();
                return new List<MeshMapDefinition>(_byId.Values);
            }
        }

        public static MeshMapDefinition GetById(string mapId)
        {
            EnsureLoaded();
            // a generated map's id carries its seed; the definition is the archipelago's template
            if (Archipelago.ArchipelagoMapId.IsArchipelago(mapId)) mapId = Archipelago.ArchipelagoMapId.TemplateId;
            return !string.IsNullOrEmpty(mapId) && _byId.TryGetValue(mapId, out var map) ? map : null;
        }

        /// <summary>Is this map on the phone now (in the install, or in the downloaded pack)?</summary>
        public static bool Has(string mapId) => GetById(mapId) != null;

        private static void EnsureLoaded()
        {
            if (_byId != null && (_withLater || !ContentPacks.Ready || _laterFailed)) return;

            _byId = new Dictionary<string, MeshMapDefinition>();
            foreach (var map in UnityEngine.Resources.LoadAll<MeshMapDefinition>(ResourcesFolder))
                _byId[map.MapId] = map;

            _withLater = ContentPacks.Ready || Application.isEditor;
            if (!_withLater) return;
#if UNITY_EDITOR
            // the editor and the tests read the folder itself: no Addressables build needed
            if (UnityEditor.AssetDatabase.IsValidFolder(LaterFolder))
                foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:MeshMapDefinition", new[] { LaterFolder }))
                {
                    var map = UnityEditor.AssetDatabase.LoadAssetAtPath<MeshMapDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                    if (map != null) _byId[map.MapId] = map;
                }
#else
            try
            {
                var handle = Addressables.LoadAssetsAsync<MeshMapDefinition>(ContentPacks.MapLabel, null);
                var maps = handle.WaitForCompletion();
                if (maps != null) foreach (var map in maps) if (map != null) _byId[map.MapId] = map;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("MeshMapCatalog: the later maps did not load: " + e.Message);
                _withLater = false;
                _laterFailed = true;
            }
#endif
        }
    }
}
