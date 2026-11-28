using System.IO;
using UnityEditor.Android;
using UnityEngine;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// The LevelPlay ads SDK for the Android build, the way <see cref="PlayGamesSetup"/> does Play Games: no dependency
    /// manager (its network resolve in batchmode is not trusted here), the Maven libraries written straight into the
    /// unityLibrary module. The Ads Mediation package (com.unity.services.levelplay) carries the Java bridge as sources,
    /// and those need the SDK to compile, so the libraries go in ALWAYS -- with or without ids (Shop/RewardedAds stays
    /// on its stand-in without them).
    ///
    /// Versions (2026-11-28): the newest the package 9.6.0 allows ([9.6.0, 10.0[) and the newest Unity Ads adapter.
    /// When the package is updated, take the SDK range from its Editor/Json/LevelPlayVersions.json.
    /// </summary>
    public sealed class LevelPlaySetup : IPostGenerateGradleAndroidProject
    {
        public static readonly string[] Libraries =
        {
            "com.unity3d.ads-mediation:mediation-sdk:9.8.0",
            "com.unity3d.ads-mediation:unityads-adapter:5.17.0",
            "com.google.android.gms:play-services-ads-identifier:18.1.0",
        };

        private const string Mark = "// LevelPlay ads (CityBuilder LevelPlaySetup)";

        public int callbackOrder => 3;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var gradle = Path.Combine(path, "build.gradle");
            if (!File.Exists(gradle)) { Debug.LogError("LevelPlaySetup: no " + gradle); return; }
            var text = File.ReadAllText(gradle);
            if (text.Contains(Mark)) return;
            var block = "\n" + Mark + "\ndependencies {\n";
            foreach (var lib in Libraries) block += "    implementation '" + lib + "'\n";
            File.AppendAllText(gradle, block + "}\n");
        }
    }
}
