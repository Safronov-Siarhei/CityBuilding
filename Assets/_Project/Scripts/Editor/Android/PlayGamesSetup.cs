using System.IO;
using GooglePlayGames;
using GooglePlayGames.Editor;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// Google Play Games for the Android build, without the plugin's window and without its dependency manager (EDM4U
    /// was left out of the import, 2026-11-25: it needs a network resolve that batchmode cannot be trusted with).
    ///
    ///   Apply (called by SetupProject.Run): the app id from <see cref="AppIdFile"/> -- the number Play Console shows in
    ///     Play Games Services -> Configuration -- into the plugin's settings, its PlayGamesSettings asset and the
    ///     manifest of Assets/Plugins/Android/GooglePlayGamesManifest.androidlib. An empty file = Play Games off.
    ///   Gradle: the Play Games SDK (play-services-games-v2, the version the plugin's dependencies XML names) is added
    ///     to the unityLibrary module -- only while an app id is set, so a build without it carries no Play Games at all
    ///     (PlayGamesCloud does not activate either).
    /// </summary>
    public sealed class PlayGamesSetup : IPostGenerateGradleAndroidProject
    {
        public const string AppIdFile = "Design/store/play_games_app_id.txt";
        public const string Sdk = "com.google.android.gms:play-services-games-v2:22.1.0";

        public static string AppId
        {
            get
            {
                if (!File.Exists(AppIdFile)) return "";
                var id = File.ReadAllText(AppIdFile).Trim();
                foreach (var c in id) if (c < '0' || c > '9') return "";
                return id;
            }
        }

        public static void Apply()
        {
            var id = AppId;
            var settings = GPGSProjectSettings.Instance;
            if (settings.Get(GPGSUtil.APPIDKEY, "") == id && GPGSUtil.AndroidManifestExists() && Loaded() == id) return;
            if (id.Length > 0)
            {
                if (!GPGSAndroidSetupUI.PerformSetup(null, id, null))
                    Debug.LogError("PlayGamesSetup: the plugin refused app id " + id);
                else
                    Debug.Log("PlayGamesSetup: Play Games on, app id " + id);
                return;
            }
            settings.Set(GPGSUtil.APPIDKEY, "");
            settings.Save();
            GPGSUtil.GenerateAndroidManifest();
            AssetDatabase.Refresh();
            Debug.Log("PlayGamesSetup: no app id in " + AppIdFile + " -- Play Games off");
        }

        private static string Loaded()
        {
            var asset = AssetDatabase.LoadAssetAtPath<PlayGamesSettings>("Assets/GooglePlayGames/Resources/PlayGamesSettings.asset");
            return asset != null ? asset.AppId ?? "" : "";
        }

        public int callbackOrder => 2;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            if (AppId.Length == 0) return;
            var gradle = Path.Combine(path, "build.gradle");
            if (!File.Exists(gradle)) { Debug.LogError("PlayGamesSetup: no " + gradle); return; }
            var text = File.ReadAllText(gradle);
            if (text.Contains(Sdk)) return;
            File.AppendAllText(gradle, "\n// Google Play Games (CityBuilder PlayGamesSetup)\ndependencies {\n    implementation '" + Sdk + "'\n}\n");
        }
    }
}
