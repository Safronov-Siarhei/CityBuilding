using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CityBuilder.UI
{
    /// <summary>
    /// The level 3-5 pictures (UI/Icons/&lt;Id&gt;_L3..L5) of the buildings only the sandbox raises that high -- the
    /// same buildings as BuildingLevelKit's (the user, 2026-11-28): in the download pack, not in the install. The cards
    /// (BuildingInfoCard, the Laboratory, the run card, the event feed) look here when their own list has no such key;
    /// while the pack is not on the phone they show the nearest level below, as for a level drawn without a picture.
    /// </summary>
    public class LevelIconKit : ScriptableObject
    {
        public const string Path = "Assets/_Project/Content/LaterMaps/LevelIconKit.asset";
        public const string Address = "LevelIconKit";

        public string[] keys = new string[0];
        public Sprite[] sprites = new Sprite[0];

        private Dictionary<string, Sprite> _byKey;

        private static LevelIconKit _loaded;
        private static bool _failed;

        /// <summary>The picture for the key from the pack, or null (not a later picture, or the pack not here yet).</summary>
        public static Sprite Find(string key)
        {
            var kit = Load();
            if (kit == null || string.IsNullOrEmpty(key)) return null;
            if (kit._byKey == null)
            {
                kit._byKey = new Dictionary<string, Sprite>();
                for (var i = 0; i < kit.keys.Length && i < kit.sprites.Length; i++) kit._byKey[kit.keys[i]] = kit.sprites[i];
            }
            return kit._byKey.TryGetValue(key, out var sprite) ? sprite : null;
        }

        private static LevelIconKit Load()
        {
            if (_loaded != null || _failed) return _loaded;
#if UNITY_EDITOR
            _loaded = AssetDatabase.LoadAssetAtPath<LevelIconKit>(Path);
            if (_loaded == null) _failed = true;
#else
            if (!Core.ContentPacks.IsReady(Core.ContentPacks.Pack.Sandbox)) return null;
            try
            {
                _loaded = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<LevelIconKit>(Address).WaitForCompletion();
            }
            catch (System.Exception e)
            {
                Debug.LogError("LevelIconKit: " + e.Message);
                _failed = true;
            }
#endif
            return _loaded;
        }
    }
}
