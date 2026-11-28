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

        /// <summary>Where SetupProject writes the small FIRST pack's map definitions (the start map's level-1 dungeon).</summary>
        public const string FirstFolder = "Assets/_Project/Content/FirstMaps";

        /// <summary>Label of the map definitions in the first pack.</summary>
        public const string FirstMapLabel = "first_map";

        private static bool _withFirst;

        /// <summary>The old designed maps (Map3-5) no player can pick: the editor and the tests only, never in a build (2026-11-28).</summary>
        public const string EditorOnlyFolder = "Assets/_Project/Content/EditorMaps";

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
            if (_byId != null && (_withLater || !ContentPacks.Ready || _laterFailed))
            {
                // the first pack lands before the big one: its maps join without a full reload
                if (!_withFirst && ContentPacks.FirstReady && !Application.isEditor) LoadFirst();
                return;
            }

            _byId = new Dictionary<string, MeshMapDefinition>();
            foreach (var map in UnityEngine.Resources.LoadAll<MeshMapDefinition>(ResourcesFolder))
                _byId[map.MapId] = map;

            _withFirst = false;
            _withLater = ContentPacks.Ready || Application.isEditor;
            if (!_withLater)
            {
                if (ContentPacks.FirstReady) LoadFirst();
                return;
            }
#if UNITY_EDITOR
            // the editor and the tests read the folder itself: no Addressables build needed
            _withFirst = true;
            foreach (var folder in new[] { FirstFolder, LaterFolder, EditorOnlyFolder })
            if (UnityEditor.AssetDatabase.IsValidFolder(folder))
                foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:MeshMapDefinition", new[] { folder }))
                {
                    var map = UnityEditor.AssetDatabase.LoadAssetAtPath<MeshMapDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                    if (map != null) _byId[map.MapId] = map;
                }
#else
            try
            {
                LoadFirst();
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

        private static void LoadFirst()
        {
            _withFirst = true;
#if !UNITY_EDITOR
            try
            {
                var maps = Addressables.LoadAssetsAsync<MeshMapDefinition>(FirstMapLabel, null).WaitForCompletion();
                if (maps != null) foreach (var map in maps) if (map != null) _byId[map.MapId] = map;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("MeshMapCatalog: the first pack's maps did not load: " + e.Message);
            }
#endif
        }
    }
}
