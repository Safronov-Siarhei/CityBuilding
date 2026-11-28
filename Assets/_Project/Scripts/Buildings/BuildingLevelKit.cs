using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CityBuilder.Buildings
{
    /// <summary>
    /// The level 3-5 models of the buildings that only the sandbox raises that high (the user, 2026-11-28: the
    /// tutorial, the Invasion and campaign level 1 keep theirs in the install; the rest download right after it --
    /// Design/features/download-size.md). A prefab leaves those levels empty (its height kept) and
    /// BuildingLevelAppearance makes the model from here when the level is shown: the source and where SetupProject
    /// fitted it on the plot. Until the pack is on the phone such a level shows the model of the level below.
    /// </summary>
    public class BuildingLevelKit : ScriptableObject
    {
        public const string Path = "Assets/_Project/Content/LaterMaps/BuildingLevelKit.asset";
        public const string Address = "BuildingLevelKit";

        [System.Serializable]
        public struct Entry
        {
            public string id;
            public int level;
            public GameObject source;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
        }

        public List<Entry> entries = new List<Entry>();

        private Dictionary<(string, int), Entry> _byKey;

        public bool TryGet(string id, int level, out Entry entry)
        {
            if (_byKey == null)
            {
                _byKey = new Dictionary<(string, int), Entry>();
                foreach (var e in entries) if (e.source != null) _byKey[(e.id, e.level)] = e;
            }
            return _byKey.TryGetValue((id, level), out entry);
        }

        private static BuildingLevelKit _loaded;
        private static bool _failed;

        /// <summary>The kit, or null while the pack is not on the phone.</summary>
        public static BuildingLevelKit Load()
        {
            if (_loaded != null || _failed) return _loaded;
#if UNITY_EDITOR
            _loaded = AssetDatabase.LoadAssetAtPath<BuildingLevelKit>(Path);
            if (_loaded == null) _failed = true;
#else
            if (!Core.ContentPacks.Ready) return null;
            try
            {
                _loaded = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<BuildingLevelKit>(Address).WaitForCompletion();
            }
            catch (System.Exception e)
            {
                Debug.LogError("BuildingLevelKit: " + e.Message);
                _failed = true;
            }
#endif
            return _loaded;
        }
    }
}
