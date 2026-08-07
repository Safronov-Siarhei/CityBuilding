using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Maps
{
    /// <summary>Runtime lookup over every MapDefinition under Resources/Maps, cached after the first access.</summary>
    public static class MapCatalog
    {
        private const string ResourcesFolder = "Maps";

        private static Dictionary<string, MapDefinition> _byId;

        public static List<MapDefinition> All
        {
            get
            {
                EnsureLoaded();
                return new List<MapDefinition>(_byId.Values);
            }
        }

        public static MapDefinition GetById(string mapId)
        {
            EnsureLoaded();
            return !string.IsNullOrEmpty(mapId) && _byId.TryGetValue(mapId, out var map) ? map : null;
        }

        private static void EnsureLoaded()
        {
            if (_byId != null) return;

            _byId = new Dictionary<string, MapDefinition>();
            // Fully qualified: this file's enclosing namespace, CityBuilder.Maps, has a sibling
            // CityBuilder.Resources namespace that would otherwise shadow UnityEngine.Resources.
            foreach (var map in UnityEngine.Resources.LoadAll<MapDefinition>(ResourcesFolder))
            {
                _byId[map.MapId] = map;
            }
        }
    }
}
