using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CityBuilder.Campaign
{
    /// <summary>
    /// The films' heavy pieces of campaign missions 2-3 (the M2 cast, the Cap* cast, their clips, props and sets):
    /// in the fast-follow pack, not in the install (the user, 2026-11-28: the install holds the campaign's first
    /// cutscene and level 1 only; Design/features/download-size.md). CityDefenceMode reads them when a mission of its
    /// own starts -- the campaign map waits for the pack before it starts one. Written by SetupProjectCityScene.
    /// Music stays on CityDefenceMode: the first cutscene plays the same tracks, so they are in the install anyway.
    /// </summary>
    public class CampaignFilmKit : ScriptableObject
    {
        public const string Path = "Assets/_Project/Content/LaterMaps/CampaignFilmKit.asset";
        public const string Address = "CampaignFilmKit";

        public GameObject[] introFigures = new GameObject[0];   // Models/Cinematic/Cast/M2*.fbx + CineOrcSpear
        public CineClips[] introClips = new CineClips[0];      // their clips, in the same order
        public GameObject[] introProps = new GameObject[0];    // Models/Cinematic/M2Intro*.fbx
        public GameObject[] capitalFigures = new GameObject[0]; // Models/Cinematic/Cast/Cap*.fbx
        public CineClips[] capitalClips = new CineClips[0];
        public GameObject[] capitalProps = new GameObject[0];  // Models/Cinematic/CapProp*.fbx
        public GameObject[] capitalSets = new GameObject[0];   // M2CapitalSet, M2ThroneSet
        public GameObject[] helpSets = new GameObject[0];      // M2HelpBridgeSet
        public GameObject[] prologueSets = new GameObject[0];  // L3Glade*Set
        public GameObject[] openingSets = new GameObject[0];   // M3RoadSet
        // the films' scripts and takes (Campaign/Mission2*.json, M2Intro/, M2Capital/, M2Help/, L3Prologue/, M3Opening/)
        public TextAsset introScript;
        public TextAsset[] introTakes = new TextAsset[0];
        public TextAsset capitalScript;
        public TextAsset[] capitalTakes = new TextAsset[0];
        public TextAsset helpScript;
        public TextAsset[] helpTakes = new TextAsset[0];
        public TextAsset prologueScript;
        public TextAsset[] prologueTakes = new TextAsset[0];
        public TextAsset openingScript;
        public TextAsset[] openingTakes = new TextAsset[0];

        /// <summary>Is a Models/Cinematic piece one of missions 2-3's (kept out of the install's scenes)?</summary>
        public static bool IsLater(string pieceName) =>
            !string.IsNullOrEmpty(pieceName) && (pieceName.StartsWith("Cap") || pieceName.StartsWith("M2")
                                                 || pieceName.StartsWith("M3") || pieceName.StartsWith("L3"));

        private static CampaignFilmKit _loaded;

        /// <summary>The kit, or null while the pack is not on the phone.</summary>
        public static CampaignFilmKit Load()
        {
            if (_loaded != null) return _loaded;
#if UNITY_EDITOR
            _loaded = AssetDatabase.LoadAssetAtPath<CampaignFilmKit>(Path);
#else
            if (!Core.ContentPacks.Ready) return null;
            try
            {
                _loaded = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<CampaignFilmKit>(Address).WaitForCompletion();
            }
            catch (System.Exception e)
            {
                Debug.LogError("CampaignFilmKit: " + e.Message);
            }
#endif
            return _loaded;
        }
    }
}
