using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Maps
{
    /// <summary>Runtime lookup over every MeshMapDefinition under Resources/MeshMaps, cached after the first access.</summary>
    public static class MeshMapCatalog
    {
        private const string ResourcesFolder = "MeshMaps";

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
            return !string.IsNullOrEmpty(mapId) && _byId.TryGetValue(mapId, out var map) ? map : null;
        }

        private static void EnsureLoaded()
        {
            if (_byId != null) return;

            _byId = new Dictionary<string, MeshMapDefinition>();
            foreach (var map in UnityEngine.Resources.LoadAll<MeshMapDefinition>(ResourcesFolder))
            {
                _byId[map.MapId] = map;
            }
        }
    }
}
