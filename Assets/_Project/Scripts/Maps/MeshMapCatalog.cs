using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CityBuilder.Maps
{
    /// <summary>
    /// Runtime lookup over every MeshMapDefinition: the install's under Resources/MeshMaps, plus each download pack's
    /// (ContentPacks.PackForMap: the dungeons, the campaign's missions -- the user's small-download rule 2026-11-28),
    /// joined as soon as that pack is on the phone. A map of a pack not here yet is null: the caller waits (LoadingScreen)
    /// or skips it.
    /// </summary>
    public static class MeshMapCatalog
    {
        private const string ResourcesFolder = "MeshMaps";

        /// <summary>Where SetupProject writes the fast-follow pack's map definitions (outside Resources).</summary>
        public const string LaterFolder = "Assets/_Project/Content/LaterMaps";

        /// <summary>Where SetupProject writes the small FIRST pack's map definitions (the start map's level-1 dungeon).</summary>
        public const string FirstFolder = "Assets/_Project/Content/FirstMaps";

        /// <summary>The old designed maps (Map3-5) no player can pick: the editor and the tests only, never in a build (2026-11-28).</summary>
        public const string EditorOnlyFolder = "Assets/_Project/Content/EditorMaps";

        private static Dictionary<string, MeshMapDefinition> _byId;

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

﻿        // which packs' maps are in _byId (the player); a pack that failed is not asked again
        private static bool[] _packLoaded;

        private static void EnsureLoaded()
        {
            if (_byId == null)
            {
                _byId = new Dictionary<string, MeshMapDefinition>();
                foreach (var map in UnityEngine.Resources.LoadAll<MeshMapDefinition>(ResourcesFolder))
                    _byId[map.MapId] = map;
#if UNITY_EDITOR
                // the editor and the tests read the folders themselves: no Addressables build needed
                foreach (var folder in new[] { FirstFolder, LaterFolder, EditorOnlyFolder })
                    if (UnityEditor.AssetDatabase.IsValidFolder(folder))
                        foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:MeshMapDefinition", new[] { folder }))
                        {
                            var map = UnityEditor.AssetDatabase.LoadAssetAtPath<MeshMapDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                            if (map != null) _byId[map.MapId] = map;
                        }
#endif
            }
#if !UNITY_EDITOR
            // each pack's maps join the moment that pack is on the phone (ContentPacks.Pack order)
            var packs = (ContentPacks.Pack[])System.Enum.GetValues(typeof(ContentPacks.Pack));
            if (_packLoaded == null) _packLoaded = new bool[packs.Length];
            foreach (var pack in packs)
            {
                var i = (int)pack;
                if (_packLoaded[i] || pack == ContentPacks.Pack.Sandbox || !ContentPacks.IsReady(pack)) continue;   // the sandbox pack holds no maps
                _packLoaded[i] = true;
                try
                {
                    var maps = Addressables.LoadAssetsAsync<MeshMapDefinition>(ContentPacks.MapLabelOf(pack), null).WaitForCompletion();
                    if (maps != null) foreach (var map in maps) if (map != null) _byId[map.MapId] = map;
                }
                catch (System.Exception e)
                {
                    // a pack with no maps of its own has no such label: nothing to add
                    Debug.Log($"MeshMapCatalog: no maps from {ContentPacks.NameOf(pack)} ({e.Message})");
                }
            }
#endif
        }
    }
}

